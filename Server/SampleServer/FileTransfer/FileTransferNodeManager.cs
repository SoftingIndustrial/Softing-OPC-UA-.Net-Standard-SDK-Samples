using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// FileTransfer node manager: creates and manage the file transfer nodes  
    /// </summary>
    public class FileTransferNodeManager : NodeManager
    {
        #region Private Members

        private const string DownloadNodeID = "ns=9;i=2";

        private const string DownloadFilePath = @"FileTransfer\Files\DownloadFile.xml";
        private const string UploadFilePath = @"FileTransfer\Files\UploadFile.xml";
        
        private const string ByteStringFilePath = @"FileTransfer\Files\ByteStringFile.xml";

        private const string ReadTemporaryFilePath = @"FileTransfer\Files\ReadTemporaryFile.xml";
        private const string WriteTemporaryFilePath = @"FileTransfer\Files\WriteTemporaryFile.xml";

        private const string FileTransferName = "FileTransfer";
        private const string ByteStringName = "ByteString";
        private const string TemporaryFileName = "TemporaryFile";

        /// <summary>
        /// The maximum time in milliseconds the Server accepts between Method calls necessary
        /// to complete a file read transfer or a file write transfer transaction
        /// </summary>
        private const double ClientProcessingTimeoutPeriod = 100; // seconds 

        private Dictionary<uint, FileState> m_tmpWriteFileHandles;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public FileTransferNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server,
            configuration, Namespaces.FileTransfer)
        {
            m_tmpWriteFileHandles = new Dictionary<uint, FileState>();
        }

        #endregion

        #region INodeManager Members

        /// <summary>
        /// Creates the address space
        /// </summary>
        /// <param name="externalReferences"></param>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // Execute base class CreateAddressSpace
                base.CreateAddressSpace(externalReferences);

                // Create a root node and add a reference to external Server Objects Folder
                FolderState root = CreateObjectFromType(null, FileTransferName, ObjectTypeIds.FolderType, ReferenceTypeIds.Organizes) as FolderState;
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                CreateFileState(root, DownloadFilePath, false);
                CreateFileState(root, UploadFilePath, true);

                CreateByteString(root, ByteStringName, ByteStringFilePath);

                CreateTmpFileState(root, TemporaryFileName);
                
                AddRootNotifier(root);
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Creates file state node
        /// </summary>
        /// <param name="root"></param>
        /// <param name="filename"></param>
        /// <param name="writePermission"></param>
        /// <returns></returns>
        private FileState CreateFileState(FolderState root, string filename, bool writePermission)
        {
            try
            {
                FileState fileState = CreateObjectFromType(root, Path.GetFileName(filename), ObjectTypeIds.FileType, ReferenceTypeIds.HasComponent) as FileState;
                if (fileState != null)
                {
                    fileState.WriteMask = AttributeWriteMask.None;
                    fileState.UserWriteMask = AttributeWriteMask.None;

                    fileState.Writable.Value = writePermission;
                    fileState.UserWritable.Value = writePermission;
                }

                FileStateHandler fileTypeHandler = new FileStateHandler(filename);
                fileTypeHandler.SetCallbacks(fileState);

                return fileState;
            }
            catch (FileNotFoundException)
            {
                throw new Exception("File not found exception.");
            }
        }
        /// <summary>
        /// Creates temporary file state node
        /// </summary>
        /// <param name="root"></param>
        /// <param name="filename"></param>
        /// <param name="writePermission"></param>
        /// <returns></returns>
        private TemporaryFileTransferState CreateTmpFileState(FolderState root, string filename)
        {
            try
            {
                TemporaryFileTransferState tmpFileState = CreateObjectFromType(root, Path.GetFileName(filename), ObjectTypeIds.TemporaryFileTransferType, ReferenceTypeIds.HasComponent) as TemporaryFileTransferState;

                tmpFileState.ClientProcessingTimeout = CreateProperty<double>(tmpFileState, "ClientProcessingTimeout");
                tmpFileState.ClientProcessingTimeout.Value = ClientProcessingTimeoutPeriod; 
                tmpFileState.GenerateFileForRead.OnCall = OnGenerateFileForReadCall;
                tmpFileState.GenerateFileForWrite.OnCall = GenerateFileForWriteCall;
                tmpFileState.CloseAndCommit.OnCall = CloseAndCommitCall;

                return tmpFileState;
            }
            catch (FileNotFoundException)
            {
                throw new Exception("File not found exception.");
            }
        }

        /// <summary>
        /// Creates byte string node
        /// </summary>
        /// <param name="root"></param>
        /// <param name="byteStringName"></param>
        /// <param name="byteStringPath"></param>
        /// <returns></returns>
        private BaseDataVariableState CreateByteString(FolderState root, string byteStringName, string byteStringPath)
        {
            BaseDataVariableState byteString = CreateVariable(root, byteStringName, DataTypeIds.ByteString);
            byteString.AccessLevel = AccessLevels.CurrentRead;
            byteString.UserAccessLevel = AccessLevels.CurrentRead;
            byteString.Handle = byteStringPath; // link the node handle to a file handler
            byteString.OnSimpleReadValue = OnReadFile; // read the file content as byte array
            
            return byteString;
        }

        #endregion

        #region Private File type handlers


        /// <summary>
        /// Read content of the file related to the Bytestring node and pass it to the client
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private ServiceResult OnReadFile(
            ISystemContext context,
            NodeState node,
            ref object value)
        {
            if (context != null && context.SessionId != null)
            {
                // read the file from the disk
                string filePath = node.Handle as string;

                try
                {
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        byte[] bytes = new byte[fileStream.Length];
                        fileStream.Read(bytes, 0, bytes.Length);
                        fileStream.Close();

                        value = bytes;
                    }
                }
                catch (Exception ex)
                {
                    // file access error
                    throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
                }
            }

            return StatusCodes.Good;
        }

        private ServiceResult OnGenerateFileForReadCall(ISystemContext context,
            MethodState method,
            NodeId objectId,
            object generateOptions,
            ref NodeId fileNodeId,
            ref uint fileHandle,
            ref NodeId completionStateMachine)
        {
            StatusCode openStatusCode = new StatusCode();

            // use generateOptions option !?

            // Creates and copy data content from "ReadTemporaryFilePath" to a temporary file
            string tmpFileName = Path.GetTempFileName();
            using (FileStream fileStream = new FileStream(ReadTemporaryFilePath, FileMode.Open))
            {
                using (Stream fileStreamTmp = File.OpenWrite(tmpFileName))
                {
                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, bytes.Length);
                    fileStreamTmp.Write(bytes, 0, bytes.Length);
                    fileStreamTmp.Close();
                }
                fileStream.Close();
            }

            FileState fileState = CreateFileState(null, tmpFileName, false);
            if (fileState != null)
            {
                fileNodeId = fileState.NodeId;
                ServiceResult openResult = fileState.Open.OnCall(context, null, fileNodeId, (byte)FileAccess.Read, ref fileHandle);
                openStatusCode = openResult.StatusCode;
                if (StatusCode.IsGood(openStatusCode))
                {
                    // prepare completionStateMachine node
                }
            }


            return openStatusCode;
        }

        private ServiceResult GenerateFileForWriteCall(ISystemContext context, 
            MethodState method, 
            NodeId objectId, 
            object generateOptions, 
            ref NodeId fileNodeId, 
            ref uint fileHandle)
        {
            StatusCode writeStatusCode = new StatusCode();

            // use generateOptions option !?

            // Creates and copy data content from "WriteTemporaryFilePath" to a temporary file
            string tmpFileName = Path.GetTempFileName();
            
            FileState fileState = CreateFileState(null, tmpFileName, true);
            if (fileState != null)
            {
                fileNodeId = fileState.NodeId;
                ServiceResult writeResult = fileState.Open.OnCall(context, null, fileNodeId, (byte)FileAccess.Write, ref fileHandle);
                writeStatusCode = writeResult.StatusCode;
                if (StatusCode.IsGood(writeStatusCode))
                {
                    if (m_tmpWriteFileHandles.ContainsKey(fileHandle))
                    {
                        m_tmpWriteFileHandles.Add(fileHandle, fileState);
                    }
                }
            }

            return writeStatusCode;
        }

        private ServiceResult CloseAndCommitCall(ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            ref NodeId completionStateMachine)
        {
            StatusCode closeAndCommitStatusCode = new StatusCode();
            try
            {
                if (m_tmpWriteFileHandles.ContainsKey(fileHandle))
                {
                    FileState fileState = m_tmpWriteFileHandles[fileHandle];
                    if (fileState != null)
                    {
                        // todo: initiate a saving content before closing and deleting the handler

                        ServiceResult closeResult = fileState.Close.OnCall(context, null, objectId, fileHandle);
                        closeAndCommitStatusCode = closeResult.StatusCode;
                        if (StatusCode.IsGood(closeAndCommitStatusCode))
                        {
                            string tempPath = Path.GetTempPath();
                            if (Directory.Exists(tempPath))
                            {
                                string tmpFileName = Path.Combine(tempPath, fileState.SymbolicName);
                                if (File.Exists(tmpFileName))
                                {
                                    File.Delete(tmpFileName);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
                throw ;
            }

            return closeAndCommitStatusCode;
        }

        #endregion
    }
}
