using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.IO;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;
using System.Reflection;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// FileTransfer node manager: creates and manage the file transfer nodes  
    /// </summary>
    public class FileTransferNodeManager : NodeManager
    {
        #region Private Members

        private string DownloadFilePath = Path.Combine("FileTransfer", "Files", "DownloadFile.xml");
        private string UploadFilePath = Path.Combine("FileTransfer", "Files", "UploadFile.xml");

        private string ByteStringFilePath = Path.Combine("FileTransfer", "Files", "ByteStringFile.xml");

        private string DownloadTemporaryFilePath = Path.Combine("FileTransfer", "Files", "DownloadTemporaryFile.xml");
        private string UploadTemporaryFilePath = Path.Combine("FileTransfer", "Files", "UploadTemporaryFile.xml");

        private const string FileTransferName = "FileTransfer";
        private const string ByteStringName = "ByteString";
        private const string TemporaryFileName = "TemporaryFile";

        /// <summary>
        /// The maximum time in milliseconds the Server accepts between Method calls necessary
        /// to complete a file read transfer or a file write transfer transactions 
        /// </summary>
        public const int ClientProcessingTimeoutPeriod = 10*1000; 

        private TempFilesHolder m_tmpFilesHolder;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public FileTransferNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server,
            configuration, Namespaces.FileTransfer)
        {
            m_tmpFilesHolder = new TempFilesHolder();
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
                FolderState root = CreateFolder(null, FileTransferName);
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                CreateFileState(root, DownloadFilePath, false);
                CreateFileState(root, UploadFilePath, true);

                CreateByteStringVariable(root, ByteStringName, ByteStringFilePath);

                CreateTemporaryFileTransferState(root, TemporaryFileName);

                AddRootNotifier(root);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Delete temporary file nodes from the address space
        /// </summary>
        /// <param name="fileNodeId"></param>
        public void DeleteTemporaryNode(NodeId fileNodeId)
        {
            // Remove temporary file state from holder
            m_tmpFilesHolder.RemoveNode(fileNodeId);

            // Remove temporary file state node from address space
            DeleteNode(SystemContext, fileNodeId);
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
                FileState fileState =
                    CreateObjectFromType(root, Path.GetFileName(filename), ObjectTypeIds.FileType) as FileState;
                if (fileState != null)
                {
                    FileStateHandler fileTypeHandler = new FileStateHandler(filename, fileState, writePermission);
                    fileTypeHandler.Initialize();
                }

                return fileState;
            }
            catch (FileNotFoundException)
            {
                throw new Exception("File state could not found be created exception.");
            }
        }

        /// <summary>
        /// Creates temporary file state node in address space
        /// </summary>
        /// <param name="root"></param>
        /// <param name="context"></param>
        /// <param name="filename"></param>
        /// <param name="writePermission"></param>
        /// <returns></returns>
        private TempFileStateHandler CreateTempFileState(FolderState root, ISystemContext context, string filename,
            bool writePermission)
        {
            try
            {
                FileState fileState =
                    CreateObjectFromType(root, Path.GetFileName(filename), ObjectTypeIds.FileType) as FileState;
                if (fileState != null)
                {
                    TempFileStateHandler fileTypeHandler =
                        new TempFileStateHandler(this, filename, fileState, writePermission);
                    fileTypeHandler.Initialize(ClientProcessingTimeoutPeriod);

                    return fileTypeHandler;
                }
            }
            catch (FileNotFoundException)
            {
                throw new Exception("Temporary File state could not be created exception.");
            }

            return null;
        }

        /// <summary>
        /// Creates temporary file state node in address space
        /// </summary>
        /// <param name="root"></param>
        /// <param name="filename"></param>
        /// <param name="writePermission"></param>
        /// <returns></returns>
        private TemporaryFileTransferState CreateTemporaryFileTransferState(FolderState root, string filename)
        {
            try
            {
                TemporaryFileTransferState tmpFileState = CreateObjectFromType(root, Path.GetFileName(filename), ObjectTypeIds.TemporaryFileTransferType) as TemporaryFileTransferState;

                tmpFileState.ClientProcessingTimeout.Value = ClientProcessingTimeoutPeriod;
                tmpFileState.GenerateFileForRead.OnCall = OnGenerateFileForReadCall;
                tmpFileState.GenerateFileForWrite.OnCall = OnGenerateFileForWriteCall;
                tmpFileState.CloseAndCommit.OnCall = OnCloseAndCommitCall;
                
                return tmpFileState;
            }
            catch (FileNotFoundException)
            {
                throw new Exception("Create Temporary File state node in address space could not be created exception.");
            }
        }

        /// <summary>
        /// Creates byte string node
        /// </summary>
        /// <param name="root"></param>
        /// <param name="byteStringName"></param>
        /// <param name="byteStringPath"></param>
        /// <returns></returns>
        private BaseDataVariableState CreateByteStringVariable(FolderState root, string byteStringName, string byteStringPath)
        {
            BaseDataVariableState byteString = CreateVariable(root, byteStringName, DataTypeIds.ByteString);
            byteString.AccessLevel = AccessLevels.CurrentRead;
            byteString.UserAccessLevel = AccessLevels.CurrentRead;
            // Link the node handle to a file handler
            byteString.Handle = byteStringPath;
            // Read the file content as byte array
            byteString.OnSimpleReadValue = OnReadFile; 
            
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
                catch (Exception e)
                {
                    // file access error
                    throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
                }
            }

            return StatusCodes.Good;
        }

        /// <summary>
        /// Creates a temporary file, fill the temporary file content with "DownloadTemporaryFilePath" data, open it in read mode and pass its handler to the client
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="generateOptions"></param>
        /// <param name="fileNodeId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="completionStateMachine"></param>
        /// <returns></returns>
        private ServiceResult OnGenerateFileForReadCall(ISystemContext context,
            MethodState method,
            NodeId objectId,
            object generateOptions,
            ref NodeId fileNodeId,
            ref uint fileHandle,
            ref NodeId completionStateMachine)
        {
            StatusCode generateFileForReadStatusCode = new StatusCode();

            try
            {
                lock (Lock)
                {
                    #region Demo optional step

                    // Creates and copy data content from "DownloadTemporaryFilePath" to a temporary file that it will be read by client
                    string tmpFileName = Path.GetTempFileName();
                    using (FileStream fileStream = new FileStream(DownloadTemporaryFilePath, FileMode.Open))
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

                    #endregion

                    TempFileStateHandler fileStateHandler = CreateTempFileState(null, context, tmpFileName, false);
                    if (fileStateHandler != null)
                    {
                        // At this step the user can change and define its own application logic of what he wants to keep into temporary file state node 
                        // It could be an application memory, firmware file, stream or special device ...

                        generateFileForReadStatusCode = fileStateHandler.Open(context, method, FileStateMode.Read,
                            ref fileNodeId, ref fileHandle);
                        if (StatusCode.IsGood(generateFileForReadStatusCode))
                        {
                            uint readFileHandle = m_tmpFilesHolder.Add(fileNodeId, fileStateHandler);
                            if (readFileHandle != 0)
                            {
                                fileHandle = readFileHandle;
                            }
                            else
                            {
                                throw new Exception(string.Format("{0}: The temporary file is already opened!",
                                    tmpFileName));
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("{0}: Open temporary file call failed!", tmpFileName));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }

            return generateFileForReadStatusCode;
        }

        /// <summary>
        /// Creates a temporary file, open it in write mode and pass its handler to the client to fill up with client data
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="generateOptions"></param>
        /// <param name="fileNodeId"></param>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        private ServiceResult OnGenerateFileForWriteCall(ISystemContext context, 
            MethodState method, 
            NodeId objectId, 
            object generateOptions, 
            ref NodeId fileNodeId, 
            ref uint fileHandle)
        {
            StatusCode generateFileForWriteStatusCode = new StatusCode();

            try
            {
                lock (Lock)
                {
                    // Creates a temporary file (used by client to persist client file content data)
                    string tmpFileName = Path.GetTempFileName();

                    TempFileStateHandler fileStateHandler = CreateTempFileState(null, context, tmpFileName, true);
                    if (fileStateHandler != null)
                    {
                        generateFileForWriteStatusCode = fileStateHandler.Open(context, method,
                            FileStateMode.Read | FileStateMode.Write, ref fileNodeId, ref fileHandle);
                        if (StatusCode.IsGood(generateFileForWriteStatusCode))
                        {
                            uint readFileHandle = m_tmpFilesHolder.Add(fileNodeId, fileStateHandler);
                            if (readFileHandle != 0)
                            {
                                fileHandle = readFileHandle;
                            }
                            else
                            {
                                throw new Exception(string.Format("{0}: The temporary file is already opened!",
                                    tmpFileName));
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("{0}: Open temporary file call failed!", tmpFileName));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }

            return generateFileForWriteStatusCode;
        }

        /// <summary>
        /// Apply(save) the content of the written file by the client (to a dedicated server file) then close and delete the temporary file after the completion of the transaction
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="completionStateMachine"></param>
        /// <returns></returns>
        private ServiceResult OnCloseAndCommitCall(ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            ref NodeId completionStateMachine)
        {
            try
            {
                lock (Lock)
                {
                    TempFileStateHandler fileStateHandler = m_tmpFilesHolder.Get(fileHandle);
                    if (fileStateHandler != null)
                    {
                        if (!fileStateHandler.IsUserAccessAllowed(context))
                        {
                            return StatusCodes.BadUserAccessDenied;
                        }

                        if (fileStateHandler.IsGenerateForWriteFileType())
                        {
                            // At this step the user can change and define its own application logic of what he wants to receive from temporary file state node
                            // performed on client side. It could be an application memory, firmware file, stream or special device ...

                            #region Demo optional step

                            using (FileStream fileStreamTmp = File.OpenWrite(UploadTemporaryFilePath))
                            {
                                FileStream fileStream = fileStateHandler.GetTemporaryFileStreamEntry().FileStream;
                                if (fileStream != null)
                                {
                                    // The Client filled the temporary file with client data content
                                    // The File state (offset)position should be set at the beginning to read all its content
                                    fileStateHandler.SetBeginPosition(context, method);

                                    byte[] bytes = new byte[fileStream.Length];
                                    fileStream.Read(bytes, 0, bytes.Length);
                                    fileStreamTmp.Write(bytes, 0, bytes.Length);
                                    fileStream.Close();
                                }
                                else
                                {
                                    throw new Exception("The temporary file state was released!");
                                }

                                fileStreamTmp.Close();
                            }

                            StatusCode closeStatusCode = fileStateHandler.Close(context, method);
                            if (StatusCode.IsBad(closeStatusCode))
                            {
                                throw new Exception("Close temporary file state failed.");
                            }

                            #endregion
                        }
                        else
                        {
                            string notSupportedType =
                                "The GenerateFileForRead node types are not allowed to use CloseAndCommit!";
                            Console.WriteLine(notSupportedType);
                            throw new Exception(notSupportedType);
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format(
                            "The temporary file state related to the file handler '{0}' was removed!",
                            fileHandle));
                    }
                }
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }

            return StatusCodes.Good;
        }

        #endregion
    }
}
