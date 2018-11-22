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
        /// to complete a file read transfer or a file write transfer transactions
        /// </summary>
        private const double ClientProcessingTimeoutPeriod = 10; // seconds 

        private TempFilesHolder m_tmpFilesHolder;

        private const int ChunkSize = 512;

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
                FolderState root = CreateObjectFromType(null, FileTransferName, ObjectTypeIds.FolderType, ReferenceTypeIds.Organizes) as FolderState;
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                CreateFileState(root, DownloadFilePath, false);
                CreateFileState(root, UploadFilePath, true);

                CreateByteString(root, ByteStringName, ByteStringFilePath);

                CreateTemporaryFileTransferState(root, TemporaryFileName);
                
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
                throw new Exception("File state could not found be created exception.");
            }
        }

        /// <summary>
        /// Creates temporary file state node
        /// </summary>
        /// <param name="root"></param>
        /// <param name="context"></param>
        /// <param name="filename"></param>
        /// <param name="writePermission"></param>
        /// <returns></returns>
        private TempFileStateHandler CreateTempFileState(FolderState root, ISystemContext context, string filename, bool writePermission)
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

                TempFileStateHandler fileTypeHandler = new TempFileStateHandler(context, filename);
                fileTypeHandler.SetCallbacks(fileState);

                return fileTypeHandler;
            }
            catch (FileNotFoundException)
            {
                throw new Exception("Temporary File state could not be created exception.");
            }
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
                TemporaryFileTransferState tmpFileState = CreateObjectFromType(root, Path.GetFileName(filename), ObjectTypeIds.TemporaryFileTransferType, ReferenceTypeIds.HasComponent) as TemporaryFileTransferState;

                tmpFileState.ClientProcessingTimeout.Value = ClientProcessingTimeoutPeriod;
                tmpFileState.GenerateFileForRead.OnCall = OnGenerateFileForReadCall;
                tmpFileState.GenerateFileForWrite.OnCall = GenerateFileForWriteCall;
                tmpFileState.CloseAndCommit.OnCall = CloseAndCommitCall;
                
                return tmpFileState;
            }
            catch (FileNotFoundException)
            {
                throw new Exception("Create Temporary File state node in adress space could not be created exception.");
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
                catch (Exception e)
                {
                    // file access error
                    throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
                }
            }

            return StatusCodes.Good;
        }

        /// <summary>
        /// Creates a temporary file, fill the temporary file content with "ReadTemporaryFilePath" data, open it in read mode and pass its handler to the client
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
                // ignore generateOptions option 
                // completionStateMachine for asyncronously read mode (not used in this sample)
                // FileTransferStateMachineState completionStateMachineState = CreateObjectFromType(null, "CompletionStateMachine", ObjectTypeIds.FileTransferStateMachineType, ReferenceTypeIds.HasComponent) as FileTransferStateMachineState;
                // completionStateMachine = completionStateMachineState.NodeId;

                bool isWriteFailed = false;
                DateTime startTime = DateTime.Now;

                // Creates and copy data content from "ReadTemporaryFilePath" to a temporary file
                string tmpFileName = Path.GetTempFileName();
                using (FileStream fileStream = new FileStream(ReadTemporaryFilePath, FileMode.Open))
                {
                    using (FileStream fileStreamTmp = File.OpenWrite(tmpFileName))
                    {
                        ulong totalSize = (ulong) fileStream.Length;


                        // Copy the file in chunks of <chunkSize> bytes from server
                        ulong cTotalRead = 0;
                        while (cTotalRead < totalSize)
                        {
                            int cRead = totalSize - cTotalRead > ChunkSize
                                ? ChunkSize
                                : (int) (totalSize - cTotalRead);
                            byte[] buffer = new byte[cRead];

                            int readValue = fileStream.Read(buffer, 0, cRead);
                            /*if (readValue != -1)
                            {
                                fileStream.Close();
                                throw new Exception(string.Format("{0}: Read file call failed!", tmpFileName));
                            }
                            */

                            TimeSpan elapsedTime = (DateTime.Now - startTime);
                            if (elapsedTime.TotalSeconds < ClientProcessingTimeoutPeriod)
                            {
                                fileStreamTmp.Write(buffer, 0, cRead);
                                cTotalRead += (ulong) cRead;
                                Console.Write("\rWriting client content to temporary file {0} bytes of {1} - {2}% complete", cTotalRead, totalSize,
                                    cTotalRead * 100 / totalSize);
                            }
                            else
                            {
                                isWriteFailed = true;
                                Console.WriteLine(
                                    "\nWriting the server temporary file content to '{0}' timeout exceeded. \n'ClientProcessingTimeout' period = {1} seconds",
                                    ReadTemporaryFilePath, ClientProcessingTimeoutPeriod);
                                break; // no more writes from server 
                            }
                        }

                        Console.WriteLine();

                        fileStream.Close();
                    }
                }

                if (isWriteFailed)
                {
                    Console.WriteLine("The file content temporary copy was not finalized due to 'ClientProcessingTimeout' period limit.");
                }
                else
                {
                    TempFileStateHandler fileStateHandler = CreateTempFileState(null, context, tmpFileName, false);
                    if (fileStateHandler != null)
                    {
                        fileStateHandler.FileStateEvent += RemoveFileStatePredefinedNodes;
                        generateFileForReadStatusCode = fileStateHandler.Open(context, method, FileAccess.Read,
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
                                throw new Exception(string.Format("{0}: The file is already opened!", tmpFileName));
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("{0}: Open file call failed!", tmpFileName));
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
        private ServiceResult GenerateFileForWriteCall(ISystemContext context, 
            MethodState method, 
            NodeId objectId, 
            object generateOptions, 
            ref NodeId fileNodeId, 
            ref uint fileHandle)
        {
            StatusCode generateFileForWriteStatusCode = new StatusCode();

            try
            {
                // ignore generateOptions option 
                
                // Creates a temporary file (used by client to persist client file content data)
                string tmpFileName = Path.GetTempFileName();
                
                TempFileStateHandler fileStateHandler = CreateTempFileState(null, context, tmpFileName, true);
                if (fileStateHandler != null)
                {
                    fileStateHandler.FileStateEvent += RemoveFileStatePredefinedNodes;
                    generateFileForWriteStatusCode = fileStateHandler.Open(context, method, FileAccess.ReadWrite, ref fileNodeId, ref fileHandle);
                    if (StatusCode.IsGood(generateFileForWriteStatusCode))
                    {
                        uint readFileHandle = m_tmpFilesHolder.Add(fileNodeId, fileStateHandler);
                        if (readFileHandle != 0)
                        {
                            fileHandle = readFileHandle;
                        }
                        else
                        {
                            throw new Exception(string.Format("{0}: The file is already opened!", tmpFileName));
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("{0}: Open file call failed!", tmpFileName));
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
        private ServiceResult CloseAndCommitCall(ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            ref NodeId completionStateMachine)
        {
            StatusCode closeAndCommitStatusCode = new StatusCode();

            try
            {
                // completionStateMachine for asyncronously close and commit mode (not used in this sample)
                // FileTransferStateMachineState completionStateMachineState = CreateObjectFromType(null, "CompletionStateMachine", ObjectTypeIds.FileTransferStateMachineType, ReferenceTypeIds.HasComponent) as FileTransferStateMachineState;
                // completionStateMachine = completionStateMachineState.NodeId;

                TempFileStateData tmpFileStateData = m_tmpFilesHolder.Get(fileHandle);
                if (tmpFileStateData != null)
                {
                    NodeId fileNodeId = tmpFileStateData.FileNodeId;
                    if (fileNodeId == null)
                    {
                        throw new Exception(string.Format("The file Node id related to file handler '{0}' was already removed!", fileHandle));
                    }
                    TempFileStateHandler fileStateHandler = tmpFileStateData.FileStateHandler;
                    if (fileStateHandler != null)
                    {
                        if (fileStateHandler.IsGenerateForWriteFileType())
                        {
                            // Commit(save) on server the file data filled on client
                            bool isReadFailed = false;
                            DateTime startTime = DateTime.Now;

                            using (FileStream fileStreamTmp = File.OpenWrite(WriteTemporaryFilePath))
                            {
                                // to be entirely red the offset of file position should be at the begining
                                fileStateHandler.SetBeginPosition(context, method);

                                ulong totalSize = (ulong) fileStateHandler.GetFileSize();
                                ulong cTotalRead = 0;
                                while (cTotalRead < totalSize)
                                {
                                    int cRead = totalSize - cTotalRead > ChunkSize
                                        ? ChunkSize
                                        : (int) (totalSize - cTotalRead);
                                    byte[] buffer = new byte[cRead];

                                    StatusCode readStatusCode =
                                        fileStateHandler.Read(context, method, cRead, ref buffer);
                                    if (StatusCode.IsBad(readStatusCode))
                                    {
                                        Console.WriteLine(string.Format("\nRead status code is: {0}\n", readStatusCode));
                                        break;
                                    }

                                    TimeSpan elapsedTime = (DateTime.Now - startTime);
                                    if (elapsedTime.TotalSeconds < ClientProcessingTimeoutPeriod)
                                    {
                                        fileStreamTmp.Write(buffer, 0, cRead);
                                        cTotalRead += (ulong) cRead;
                                        Console.Write("\rReading temporary file data filled by client {0} bytes of {1} - {2}% complete", cTotalRead,
                                            totalSize,
                                            cTotalRead * 100 / totalSize);
                                    }
                                    else
                                    {
                                        isReadFailed = true;
                                        Console.WriteLine(
                                            "\nReading the server temporary file content to '{0}' timeout exceeded. \n'ClientProcessingTimeout' period = {1} seconds",
                                            WriteTemporaryFilePath, ClientProcessingTimeoutPeriod);
                                        break; // no more writes from server 
                                    }
                                }

                                Console.WriteLine();

                                fileStreamTmp.Close();
                            }

                            if (isReadFailed)
                            {
                                File.Delete(WriteTemporaryFilePath);
                            }

                            closeAndCommitStatusCode = fileStateHandler.Close(context, method);
                            if (StatusCode.IsBad(closeAndCommitStatusCode))
                            {
                                throw new Exception("Close file state failed.");
                            }

                            m_tmpFilesHolder.Remove(fileHandle);
                        }
                        else
                        {
                            string notSupportedType =
                                "The GenerateFileForRead node types are not allowed to use CloseAndCommit! \nPlease use GenerateFileForWrite type file handle.";

                            Console.Write(notSupportedType);
                            throw new Exception(notSupportedType);
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("The file state related to the file handler '{0}' was removed!", fileHandle));
                    }
                }
                else
                {
                    throw new Exception(string.Format("The file related to the handler number '{0}' was already removed!", fileHandle));
                }
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }

            return closeAndCommitStatusCode;
        }

        #endregion

        #region Class Handlers
        /// <summary>
        /// Remove temporary file state nodes from server address space
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveFileStatePredefinedNodes(object sender, FileStateEventArgs e)
        {
            lock (this)
            {
                if (sender != null)
                {
                    NodeId fileNodeId = e.FileStateNodeId;
                    ISystemContext context = e.Context;
                    if (context != null && fileNodeId != null)
                    {
                        NodeState fileState = FindPredefinedNode(fileNodeId, null);
                        if (fileState != null)
                        {
                            RemovePredefinedNode(context, fileState, new List<LocalReference>());

                            if (sender is TempFileStateHandler)
                            {
                                ((TempFileStateHandler) sender).FileStateEvent -= RemoveFileStatePredefinedNodes;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove all temporary file state nodes from current session that is closing
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sessionId"></param>
        /// <param name="deleteSubscriptions"></param>
        public override void SessionClosing(OperationContext context, NodeId sessionId, bool deleteSubscriptions)
        {
            m_tmpFilesHolder.RemoveFileStateNodes();
        }
        #endregion
    }
}
