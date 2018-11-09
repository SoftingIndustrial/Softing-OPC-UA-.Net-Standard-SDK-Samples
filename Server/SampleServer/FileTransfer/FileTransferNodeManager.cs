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

        private const string DownloadFilePath = @"FileTransfer\Files\DownloadFile.xml";
        private const string UploadFilePath = @"FileTransfer\Files\UploadFile.xml";
        private const string ByteStringFilePath = @"FileTransfer\Files\ByteStringFile.xml";
        
        private const string FileTransferName = "FileTransfer";
        private const string ByteStringName = "ByteString";

        private const string FileTransferTmpName = "FileTransferTmp";
        private const string ByteStringTmpName = "TmpByteString";
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public FileTransferNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server,
            configuration, Namespaces.FileTransfer)
        {
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

                FolderState rootTmp = CreateObjectFromType(null, FileTransferTmpName, ObjectTypeIds.FolderType, ReferenceTypeIds.Organizes) as FolderState;
                AddReference(rootTmp, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                CreateTmpFileState(rootTmp, DownloadFilePath, false);
                CreateTmpFileState(rootTmp, UploadFilePath, true);

                CreateTmpByteString(rootTmp, ByteStringTmpName, ByteStringFilePath);

                AddRootNotifier(root);
                AddRootNotifier(rootTmp);
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
                FileStateHandler fileTypeHandler = new FileStateHandler(filename);
                return fileTypeHandler.CreateFileState(this, root, writePermission);
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
        private FileState CreateTmpFileState(FolderState root, string filename, bool writePermission)
        {
            try
            {
                // Creates and copy data content to a temporary file
                string tmpFileName = Path.GetTempFileName();
                using (FileStream fileStream = new FileStream(filename, FileMode.Open))
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

                FileStateHandler fileTypeHandler = new FileStateHandler(tmpFileName, Path.GetFileNameWithoutExtension(filename));
                return fileTypeHandler.CreateFileState(this, root, writePermission);
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
            byteString.Handle = byteStringPath; // link the node handle to a file handler
            byteString.OnSimpleReadValue = OnReadFile; // read the file content as byte array
            byteString.OnWriteValue = OnWriteFile; // write the variable data as a byte array

            return byteString;
        }
        /// <summary>
        /// Creates temporary byte string node
        /// </summary>
        /// <param name="tmpRoot"></param>
        /// <param name="byteStringName"></param>
        /// <param name="byteStringPath"></param>
        /// <returns></returns>
        private BaseDataVariableState CreateTmpByteString(FolderState tmpRoot, string byteStringName, string byteStringPath)
        {
            string tmpByteStringPath = String.Empty;
            try
            {
                // Creates and copy data content to a temporary file
                tmpByteStringPath = Path.GetTempFileName();
                using (FileStream fileStream = new FileStream(byteStringPath, FileMode.Open))
                {
                    using (Stream fileStreamTmp = File.OpenWrite(tmpByteStringPath))
                    {
                        byte[] bytes = new byte[fileStream.Length];
                        fileStream.Read(bytes, 0, bytes.Length);
                        fileStreamTmp.Write(bytes, 0, bytes.Length);
                        fileStreamTmp.Close();
                    }
                    fileStream.Close();
                }
            }
            catch (FileNotFoundException)
            {
                throw new Exception("File not found exception.");
            }

            return CreateByteString(tmpRoot, byteStringName, tmpByteStringPath);
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
        /// Persist the node value received to the related 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="indexRange"></param>
        /// <param name="dataEncoding"></param>
        /// <param name="value"></param>
        /// <param name="statusCode"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        private ServiceResult OnWriteFile(ISystemContext context, NodeState node, NumericRange indexRange,
            QualifiedName dataEncoding, ref object value, ref StatusCode statusCode, ref DateTime timestamp)
        {
            if (context != null && context.SessionId != null)
            {
                // read the file from the disk
                string filePath = node.Handle as string;

                try
                {
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        byte[] bytes = value as byte[];
                        fileStream.Write(bytes, 0, bytes.Length);
                        fileStream.Close();
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

        #endregion
    }
}
