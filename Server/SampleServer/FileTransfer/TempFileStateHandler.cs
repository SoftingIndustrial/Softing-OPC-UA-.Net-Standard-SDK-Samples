/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Opc.Ua;
using System;
using System.IO;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// TemporaryFileTransferState handler class
    /// </summary>
    internal class TempFileStateHandler : FileStateHandler
    {
        #region Public Members

        private uint m_openFileHandle;
        private FileTransferNodeManager m_nodeManager;
        private NodeId m_sessionId;

        #endregion

        #region Constructor

        public TempFileStateHandler(FileTransferNodeManager nodeManager, string filePath, FileState fileState,
            bool writePermission) : base(filePath, fileState, writePermission)
        {
            m_openFileHandle = 0;
            m_nodeManager = nodeManager;
            m_sessionId = null;
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
        /// Initialize: set write permission attributes and set callbacks;
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
        /// Get file stream entry
        /// </summary>
        /// <returns></returns>
        public FileStreamTracker GetTemporaryFileStreamEntry()
        {
            if (m_fileHandles.ContainsKey(m_openFileHandle))
            {
                return m_fileHandles[m_openFileHandle];
            }

            return null;
        }

        /// <summary>
        /// Set file offset position to the beginning  
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
                        m_fileState.SetPosition.OnCall(context, method, m_fileState.NodeId, m_openFileHandle, 0);
                    if (readResult == null)
                    {
                        throw new Exception("The Temporary 'SetPosition' file state method failed.");
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
                if (m_openFileHandle > 0)
                {
                    // only one open is allowed for temporary file state
                    return StatusCodes.BadNotSupported;
                }
                if (m_fileState != null)
                {
                    fileNodeId = m_fileState.NodeId;
                    ServiceResult openResult = m_fileState.Open.OnCall(context, method, fileNodeId,
                        (byte) fileStateMode, ref fileHandle);
                    if (openResult == null)
                    {
                        throw new Exception("The Temporary 'Open' file state method failed.");
                    }
                    else
                    {
                        m_openFileHandle = fileHandle;
                        m_sessionId = context.SessionId;
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
                        m_fileState.Close.OnCall(context, method, m_fileState.NodeId, m_openFileHandle);
                    if (closeResult == null)
                    {
                        throw new Exception("The Temporary 'Close' file state method failed.");
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

        /// <summary>
        /// Check if the session id is the same with the one created on open
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool IsUserAccessAllowed(ISystemContext context)
        {
            if (context != null)
            {
                if (context.SessionId == m_sessionId)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Clean data
        /// </summary>
        /// <param name="fileNodeId"></param>
        private void ClearData(NodeId fileNodeId)
        {
            lock (this)
            {
                try
                {
                    if (File.Exists(base.FilePath))
                    {
                        File.Delete(base.FilePath);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            // Remove temporary file state nodes from server address space
            if (fileNodeId == null)
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
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Check when the stream was last time accessed and close it
        /// </summary>
        /// <param name="state"></param>
        protected override void CheckFileStreamAvailability(object state)
        {
            if (m_nodeManager != null)
            {
                lock (m_nodeManager.Lock)
                {
                    FileStreamTracker entry = GetTemporaryFileStreamEntry();
                    if (entry != null)
                    {
                        TimeSpan duration = DateTime.Now - entry.LastAccessTime;
                        if (duration.TotalMilliseconds > ExpireFileStreamAvailabilityTime)
                        {
                            try
                            {
                                // Clear abandoned objects
                                base.OnCloseMethodCall(null, null, m_fileState.NodeId, m_openFileHandle);
                                ClearData(m_fileState.NodeId);
                            }
                            catch { }
                        }
                    }
                }
            }
        }
        #endregion

        #region Protected Callback Methods

        /// <summary>
        /// Get Position method callback
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        protected override ServiceResult OnGetPositionMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            ref ulong position)
        {
            try
            {
                if (!IsUserAccessAllowed(context))
                {
                    return StatusCodes.BadUserAccessDenied;
                }

                return base.OnGetPositionMethodCall(context, method, objectId, m_openFileHandle, ref position);
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }
        }

        /// <summary>
        /// Set position callback
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        protected override ServiceResult OnSetPositionMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            ulong position)
        {
            try
            {
                if (!IsUserAccessAllowed(context))
                {
                    return StatusCodes.BadUserAccessDenied;
                }

                return base.OnSetPositionMethodCall(context, method, objectId, m_openFileHandle, position);
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }
        }

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
                if (!IsUserAccessAllowed(context))
                {
                    return StatusCodes.BadUserAccessDenied;
                }
                
                return base.OnReadMethodCall(context, method, objectId, m_openFileHandle, length, ref data);
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
                if (!IsUserAccessAllowed(context))
                {
                    return StatusCodes.BadUserAccessDenied;
                }

                return base.OnWriteMethodCall(context, method, objectId, m_openFileHandle, data);
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
                if (!IsUserAccessAllowed(context))
                {
                    return StatusCodes.BadUserAccessDenied;
                }

                ServiceResult closeResult = base.OnCloseMethodCall(context, method, fileNodeId, m_openFileHandle);
                if (StatusCode.IsGood(closeResult.StatusCode))
                {
                    ClearData(fileNodeId);
                }
                return closeResult.StatusCode;
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }
        }

        #endregion
    }
}
