using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// TemporaryFileTransferState handler class
    /// </summary>
    internal class TempFileStateHandler : FileStateHandler
    {
        #region Public Members

        private const uint defaultFileHandle = 1;
        private FileTransferNodeManager m_nodeManager;

        #endregion

        #region Constructor

        public TempFileStateHandler(FileTransferNodeManager nodeManager, string filePath, FileState fileState,
            bool writePermission) : base(filePath, fileState, writePermission)
        {
            m_nodeManager = nodeManager;
        }

        #endregion

        #region Public Properties

        public NodeId FileNodeId
        {
            get
            {
                if (m_fileState != null)
                {
                    return m_fileState.NodeId;
                }

                return null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize: set write permission atributes and set callbacks;
        /// Set timer to release temporary file states after 'ClientProcessingTimeout' time period  
        /// </summary>
        /// <param name="clientProcessingTimeoutPeriod"></param>
        public void Initialize(uint clientProcessingTimeoutPeriod)
        {
            base.Initialize();

            // Reset the timer period to the 'ClientProcessingTimeout' set on 'FileTransfer' node by the server
            // In this way the temporary file transfer nodes will be released when 'ClientProcessingTimeout' exceeds this period
            base.SetExpireFileStreamAvailabilityTime(clientProcessingTimeoutPeriod);
        }

        /// <summary>
        /// Get file stream 
        /// </summary>
        /// <returns></returns>
        public FileStream GetTemporaryFileStream()
        {
            if (m_fileHandles.ContainsKey(defaultFileHandle))
            {
                return m_fileHandles[defaultFileHandle].FileStream;
            }

            return null;
        }

        /// <summary>
        /// Set file offset position to the begining  
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public StatusCode SetBeginPosition(ISystemContext context,
            MethodState method)
        {
            try
            {
                if (m_fileState != null)
                {
                    ServiceResult readResult =
                        m_fileState.SetPosition.OnCall(context, method, m_fileState.NodeId, defaultFileHandle, 0);
                    if (readResult == null)
                    {
                        throw new Exception("The Temporary Read file state method failed.");
                    }
                    else
                    {
                        return readResult.StatusCode;
                    }
                }
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }

            return new StatusCode(StatusCodes.Good);
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
            FileStateMode fileStateMode,
            ref NodeId fileNodeId,
            ref uint fileHandle)
        {
            try
            {
                if (m_fileState != null)
                {
                    fileNodeId = m_fileState.NodeId;
                    ServiceResult openResult = m_fileState.Open.OnCall(context, method, fileNodeId,
                        (byte) fileStateMode, ref fileHandle);
                    if (openResult == null)
                    {
                        throw new Exception("The Temporary Open file state method failed.");
                    }
                    else
                    {
                        return openResult.StatusCode;
                    }
                }
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }

            return new StatusCode(StatusCodes.Good);
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
                    ServiceResult closeResult =
                        m_fileState.Close.OnCall(context, method, m_fileState.NodeId, defaultFileHandle);
                    if (closeResult == null)
                    {
                        throw new Exception("The Temporary Close file state method failed.");
                    }
                    else
                    {
                        return closeResult.StatusCode;
                    }
                }
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }

            return new StatusCode(StatusCodes.Good);
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Check if the time between File State calls exeeds the maximum timeout allowed 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private bool HasAccessExpired(ISystemContext context)
        {
            if (m_nodeManager != null && (uint)context.SessionId.Identifier > 0)
            {
                if (m_fileHandles.ContainsKey(defaultFileHandle))
                {
                    DateTime lastAccessTime = m_fileHandles[defaultFileHandle].LastAccessTime;
                    TimeSpan elapsedTime = (DateTime.Now - lastAccessTime);
                    if (elapsedTime.TotalMilliseconds < ExpireFileStreamAvailabilityTime)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region Protected Callback Methods

        /// <summary>
        /// Read method callback
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override ServiceResult OnReadMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            int length,
            ref byte[] data)
        {
            try
            {
                if (HasAccessExpired(context))
                {
                    return StatusCodes.BadUserAccessDenied;
                }

                return base.OnReadMethodCall(context, method, objectId, fileHandle, length, ref data);
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }
        }

        /// <summary>
        /// Write method callback
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override ServiceResult OnWriteMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            byte[] data)
        {
            try
            {
                if (HasAccessExpired(context))
                {
                    return StatusCodes.BadUserAccessDenied;
                }

                return base.OnWriteMethodCall(context, method, objectId, fileHandle, data);
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }
        }

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

                // Remove temporary file state nodes from server address space
                if(fileNodeId == null)
                {
                    if (m_fileState != null)
                    {
                        fileNodeId = m_fileState.NodeId;
                    }
                }
                if (m_nodeManager != null)
                {
                    m_nodeManager.DeleteTemporaryNode(fileNodeId);
                }

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
