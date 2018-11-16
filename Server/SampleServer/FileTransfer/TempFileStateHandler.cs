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
        public TempFileStateHandler(string filePath) : base(filePath)
        {
        }

        #region Public Methods
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

                // the following callbacks are not necesary. they are not mention into documentation
                //m_fileState.GetPosition.OnCall = OnGetPositionMethodCall;
                //m_fileState.SetPosition.OnCall = OnSetPositionMethodCall;
            }
        }

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

        public bool IsGenerateForWriteFileType()
        {
            if (m_fileState != null)
            {
                return m_fileState.Writable != null && m_fileState.Writable.Value == true;
            }

            return false;
        }

        public FileStream GetTmpFileStream()
        {
            return base.GetFileStream(1);
        }

        public NodeId GetFileStateNodeId()
        {
            return m_fileState.NodeId;
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
            NodeId objectId,
            uint fileHandle)
        {
            try
            {
                base.OnCloseMethodCall(context, method, objectId, fileHandle);
                if (File.Exists(base.FilePath))
                {
                    File.Delete(base.FilePath);
                }

                // todo: add remove temporary nodes

                
                return StatusCodes.Good;
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }
        }

        #endregion
    }
}
