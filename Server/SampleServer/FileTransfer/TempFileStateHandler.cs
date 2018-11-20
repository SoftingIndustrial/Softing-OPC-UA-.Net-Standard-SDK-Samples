using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Opc.Ua;
using Opc.Ua.Server;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// TemporaryFileTransferState handler class
    /// </summary>
    internal class TempFileStateHandler : FileStateHandler
    {
        #region Public Members
        public delegate void FileStateEventHandler(object sender, FileStateEventArgs e);
        public event FileStateEventHandler FileStateEvent = null;
        #endregion

        #region Constructor
        public TempFileStateHandler(ISystemContext context, string filePath) : base(filePath)
        {
            Context = context;
        }
        #endregion

        #region Properties
        /// <summary>
        /// System context reference
        /// </summary>
        protected ISystemContext Context { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set temporary FileState callbacks
        /// </summary>
        /// <param name="fileState"></param>
        public override void SetCallbacks(FileState fileState)
        {
            if (fileState != null)
            {
                m_fileState = fileState;

                m_fileState.Open.OnCall = OnOpenMethodCall;
                m_fileState.Read.OnCall = OnReadMethodCall;
                m_fileState.Close.OnCall = OnCloseMethodCall;
                m_fileState.Write.OnCall = OnWriteMethodCall;
                m_fileState.Size.OnSimpleReadValue = OnReadSize;

                // The following callbacks are not necesary. They are not mention into documentation
                // m_fileState.GetPosition.OnCall = OnGetPositionMethodCall;
                // m_fileState.SetPosition.OnCall = OnSetPositionMethodCall;
            }
        }

        /// <summary>
        /// Open file state stream
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="fileAccessMode"></param>
        /// <param name="fileNodeId"></param>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public StatusCode Open(ISystemContext context,
            MethodState method,
            FileAccess fileAccessMode,
            ref NodeId fileNodeId,
            ref uint fileHandle)
        {
            try
            {
                if (m_fileState != null)
                {
                    fileNodeId = m_fileState.NodeId;
                    ServiceResult openResult = m_fileState.Open.OnCall(context, method, fileNodeId,
                        (byte)fileAccessMode, ref fileHandle);
                    if (openResult == null)
                    {
                        throw new Exception("The Open file state method failed.");
                    }
                    else
                    {
                        return openResult.StatusCode;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
            }

            return new StatusCode(1);
        }
        
        /// <summary>
        /// Close file state stream
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public StatusCode Close(ISystemContext context, MethodState method)
        {
            try
            {
                if (m_fileState != null)
                {
                    ServiceResult closeResult = m_fileState.Close.OnCall(context, method, m_fileState.NodeId, 1);
                    if (closeResult == null)
                    {
                        throw new Exception("The Close file state method failed.");
                    }
                    else
                    {
                        return closeResult.StatusCode;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
            }

            return new StatusCode(1);
        }

        /// <summary>
        /// Check if file state GenerateForWriteFile type
        /// </summary>
        /// <returns></returns>
        public bool IsGenerateForWriteFileType()
        {
            if (m_fileState != null)
            {
                return m_fileState.Writable != null && m_fileState.Writable.Value == true;
            }

            return false;
        }

        /// <summary>
        /// Get the temporary file stream
        /// </summary>
        /// <returns></returns>
        public FileStream GetTmpFileStream()
        {
            return base.GetFileStream(1);
        }

        /// <summary>
        /// Get file state node Id
        /// </summary>
        /// <returns></returns>
        public NodeId GetFileStateNodeId()
        {
            if (m_fileState != null)
            {
                return m_fileState.NodeId;
            }

            return null;
        }

        /// <summary>
        /// Remove temporary file state nodes from server adress space 
        /// </summary>
        /// <param name="fileNodeId"></param>
        public void RemoveFileStateNodes(NodeId fileNodeId)
        {
            OnRemoveFileStateNodes(Context, fileNodeId);
        }
        #endregion

        #region Protected Callback Methods

        /// <summary>
        /// Close method callback
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        protected override ServiceResult OnCloseMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId fileNodeId,
            uint fileHandle)
        {
            try
            {
                base.OnCloseMethodCall(context, method, fileNodeId, fileHandle);
                if (File.Exists(base.FilePath))
                {
                    File.Delete(base.FilePath);
                }

                // Remove temporary file state nodes from server adress space
                if(fileNodeId == null)
                {
                    if (m_fileState != null)
                    {
                        fileNodeId = m_fileState.NodeId;
                    }
                } 
                OnRemoveFileStateNodes(Context, fileNodeId);

                return StatusCodes.Good;
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }
        }

        #endregion

        #region Private Event Handler(s)
        /// <summary>
        /// Trigger remove temporary file state nodes from server adress space 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fileStateNodeId"></param>
        private void OnRemoveFileStateNodes(ISystemContext context, NodeId fileStateNodeId)
        {
            if(FileStateEvent != null)
            {
                FileStateEvent(this, new FileStateEventArgs(context, fileStateNodeId));
            }
        }
        #endregion
    }
}
