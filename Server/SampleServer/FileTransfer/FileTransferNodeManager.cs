﻿using System;
using System.Collections.Generic;
using System.Data;
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
                throw new Exception("File not found exception.");
            }
        }

        private TempFileStateHandler CreateTempFileState(FolderState root, string filename, bool writePermission)
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

                TempFileStateHandler fileTypeHandler = new TempFileStateHandler(filename);
                fileTypeHandler.SetCallbacks(fileState);

                return fileTypeHandler;
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
        private TemporaryFileTransferState CreateTemporaryFileTransferState(FolderState root, string filename)
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
            StatusCode openStatusCode = new StatusCode();

            try
            {
                // ignore generateOptions option 
                // completionStateMachine for asyncronously read mode (not used in this sample)

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

                TempFileStateHandler fileStateHandler = CreateTempFileState(null, tmpFileName, false);
                if (fileStateHandler != null)
                {
                    FileState fileState = fileStateHandler.FileState;
                    if (fileState != null)
                    {
                        fileNodeId = fileState.NodeId;
                        ServiceResult openResult = fileState.Open.OnCall(context, method, fileNodeId,
                            (byte) FileAccess.Read, ref fileHandle);
                        if (openResult != null)
                        {
                            openStatusCode = openResult.StatusCode;
                            if (StatusCode.IsGood(openStatusCode))
                            {
                                uint writeFileHandle = m_tmpFilesHolder.Add(fileNodeId, fileStateHandler);
                                if (writeFileHandle != 0)
                                {
                                    fileHandle = writeFileHandle;
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
                        else
                        {
                            throw new Exception("Open file call failed!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
            }

            return openStatusCode;
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
            StatusCode writeStatusCode = new StatusCode();

            try
            {
                // ignore generateOptions option 
                // completionStateMachine for asyncronously write mode (not used in this sample)
                
                // Creates a temporary file (used by client to persist client file content data)
                string tmpFileName = Path.GetTempFileName();
                
                TempFileStateHandler fileStateHandler = CreateTempFileState(null, tmpFileName, true);
                if (fileStateHandler != null)
                {
                    FileState fileState = fileStateHandler.FileState;
                    if (fileState != null)
                    {
                        fileNodeId = fileState.NodeId;

                        ServiceResult writeResult = fileState.Open.OnCall(context, method, fileNodeId,
                            (byte) FileAccess.ReadWrite, ref fileHandle);
                        writeStatusCode = writeResult.StatusCode;
                        if (StatusCode.IsGood(writeStatusCode))
                        {
                            uint writeFileHandle = m_tmpFilesHolder.Add(fileNodeId, fileStateHandler);
                            if (writeFileHandle != 0)
                            {
                                fileHandle = writeFileHandle;
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
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
            }

            return writeStatusCode;
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

                TempFileStateData tmpFileStateData = m_tmpFilesHolder.Get(fileHandle);
                if (tmpFileStateData != null)
                {
                    NodeId fileNodeId = tmpFileStateData.FileNodeId;
                    if (fileNodeId == null)
                    {
                        throw new Exception(string.Format("The file Node id related to file handler {0} was removed!", fileHandle));
                    }
                    TempFileStateHandler fileStateHandler = tmpFileStateData.FileStateHandler;
                    if (fileStateHandler != null)
                    {
                        FileState fileState = fileStateHandler.FileState;
                        if (fileState != null)
                        {
                            if (fileState.Writable != null && fileState.Writable.Value == false)
                            {
                                string notSupportedType =
                                    "The GenerateFileForRead node types are not allowed to use CloseAndCommit! Please use GenerateFileForWrite type file handle.";

                                Console.Write(notSupportedType);
                                throw new Exception(notSupportedType);
                            }

                            // Commit(save) on server the file data filled on client
                            FileStream fileStream = fileStateHandler.GetTmpFileStream(1);
                            if (fileStream != null)
                            {
                                if (fileStream.Length > 0)
                                {
                                    using (fileStream)
                                    {
                                        using (Stream fileStreamTmp = File.OpenWrite(WriteTemporaryFilePath))
                                        {
                                            byte[] bytes = new byte[fileStream.Length];
                                            fileStream.Read(bytes, 0, bytes.Length);
                                            fileStreamTmp.Write(bytes, 0, bytes.Length);
                                            fileStreamTmp.Close();
                                        }

                                        fileStream.Close();
                                    }
                                }
                                else
                                {
                                    Console.Write(
                                        "\rThe file {0} is empty. No copy of file content on server performed.",
                                        fileStream.Name);
                                }
                            }

                            // Close the handler and delete the temporary file
                            ServiceResult closeResult = fileState.Close.OnCall(context, method, fileNodeId, 1);
                            closeAndCommitStatusCode = closeResult.StatusCode;
                            if (StatusCode.IsBad(closeAndCommitStatusCode))
                            {
                                throw new Exception("Close file state failed.");
                            }

                            m_tmpFilesHolder.Remove(fileHandle);
                        }
                        else
                        {
                            throw new Exception(string.Format("The file state related to the file handler {0} was removed!", fileHandle));
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("The file state related to the file handler {0} was removed!", fileHandle));
                    }
                }
                else
                {
                    throw new Exception(string.Format("The file related to the handler number {0} was already removed!", fileHandle));
                }
            }
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
            }

            return closeAndCommitStatusCode;
        }

        #endregion

        public override void SessionClosing(OperationContext context, NodeId sessionId, bool deleteSubscriptions)
        {
            // remove temporary nodes
            List<LocalReference> list = new List<LocalReference>();
            //foreach (NodeState nodeState in m_tmpWriteFileHandles.Values)
            //{
                //RemovePredefinedNode((SystemContext)context, nodeState, list);
            //}
            
        }

    }
}
