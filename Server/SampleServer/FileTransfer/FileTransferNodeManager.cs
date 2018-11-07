using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.FileTransfer
{
    public class FileTransferNodeManager : NodeManager
    {
        #region Private Members

        private const string DownloadFilePath = @"FileTransfer\Files\DownloadFile.xml";
        private const string UploadFilePath = @"FileTransfer\Files\UploadFile.xml";
        private const string ByteStringFilePath = @"FileTransfer\Files\ByteStringFile.xml";

        
        private const string FileTransferName = "FileTransfer";
        private const string VariableName = "ByteString";

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

                BaseDataVariableState byteStringNode = CreateVariable(root, VariableName, DataTypeIds.ByteString);
                byteStringNode.Handle = ByteStringFilePath;
                byteStringNode.OnSimpleReadValue = OnReadFile;

                AddRootNotifier(root);
            }
        }

        #endregion

        #region Private Methods
        private void CreateFileState(FolderState root, string filename, bool writePermission)
        {
            try
            {
                //get the application path
                //string applicationFolder =
                //    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                //string filepath = Path.Combine(applicationFolder, filename);
                
                FileStateHandler fileTypeHandler = new FileStateHandler(filename);
                fileTypeHandler.CreateFileState(this, root, writePermission);
            }
            catch (FileNotFoundException)
            {
                throw new Exception("File not found exception.");
            }
        }

        #endregion

        #region Private File type handlers
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
                        Byte[] bytes = new byte[fileStream.Length];
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
        #endregion
    }
}
