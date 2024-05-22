/* ========================================================================
 * Copyright © 2011-2024 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Opc.Ua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// File state mode
    /// </summary>
    public enum FileStateMode
    {
        Read = 1,
        Write = 2,
        EraseExisting = 4,
        Append = 8
        /* options for bits position 4-7 are reserved for future version */
    }

    /// <summary>
    /// File state handler class
    /// </summary>
    internal class FileStateHandler : IDisposable
    {
        #region Private Members

        protected string m_filePath;
        protected FileState m_fileState;
        private bool m_writePermission;
        private uint m_nextFileHandle;
        protected Timer m_timer;
        protected Dictionary<uint, FileStreamTracker> m_fileHandles;

        /// <summary>
        /// Clean up threshold period (milliseconds) until all opened streams will be closed
        /// </summary>
        protected const uint CheckFileStreamAvailabilityPeriod = 60*1000;  
        #endregion

        #region Constructors

        private FileStateHandler()
        {
            m_nextFileHandle = 0;
            m_fileHandles = new Dictionary<uint, FileStreamTracker>();
        }
        public FileStateHandler(string filePath, FileState fileState, bool writePermission) : this()
        {
            m_filePath = filePath;
            m_fileState = fileState;
            m_writePermission = writePermission;

            SetExpireFileStreamAvailabilityTime(CheckFileStreamAvailabilityPeriod);
        }

        #endregion

        #region Properties

        /// <summary>
        /// File Path
        /// </summary>
        protected string FilePath
        {
            get { return m_filePath; }
        }

        /// <summary>
        /// Clean up expire time threshold period (milliseconds) reference 
        /// </summary>
        protected uint ExpireFileStreamAvailabilityTime { get; set; }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose the opened session or/and file handlers
        /// </summary>
        public void Dispose()
        {
            if (m_timer != null)
            {
                m_timer.Dispose();
                m_timer = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize: set write permission attributes and set callbacks
        /// </summary>
        public void Initialize()
        {
            m_fileState.WriteMask = AttributeWriteMask.None;
            m_fileState.UserWriteMask = AttributeWriteMask.None;

            m_fileState.Writable.Value = m_writePermission;
            m_fileState.UserWritable.Value = m_writePermission;

            m_fileState.Open.OnCall = OnOpenMethodCall;
            m_fileState.Read.OnCall = OnReadMethodCall;
            m_fileState.Close.OnCall = OnCloseMethodCall;
            m_fileState.Write.OnCall = OnWriteMethodCall;
            m_fileState.GetPosition.OnCall = OnGetPositionMethodCall;
            m_fileState.SetPosition.OnCall = OnSetPositionMethodCall;
            m_fileState.Size.OnSimpleReadValue = OnReadSize;
        }

        #endregion

        #region Protected Methods
        
        /// <summary>
        /// Set/reset the time period until the file streams will be released (if they are not in use)
        /// </summary>
        /// <param name="expireFileStreamAvailTime"></param>
        protected void SetExpireFileStreamAvailabilityTime(uint expireFileStreamAvailTime)
        {
            if (m_timer == null)
            {
                m_timer = new Timer(CheckFileStreamAvailability, null, 0, expireFileStreamAvailTime);
            }
            else
            {
                m_timer.Change(0, expireFileStreamAvailTime);
            }
            ExpireFileStreamAvailabilityTime = expireFileStreamAvailTime;
        }

        /// <summary>
        /// Check when the stream was last time accessed and close it
        /// </summary>
        /// <param name="state"></param>
        protected virtual void CheckFileStreamAvailability(object state)
        {
            foreach (KeyValuePair<uint, FileStreamTracker> entry in m_fileHandles.ToList())
            {
                TimeSpan duration = DateTime.Now - entry.Value.LastAccessTime;
                if (duration.TotalMilliseconds > ExpireFileStreamAvailabilityTime)
                {
                    try
                    {
                        uint fileHandle = entry.Key;
                        ServiceResult closeResult = m_fileState.Close.OnCall(null, null, null, fileHandle);
                        if (StatusCode.IsBad(closeResult.StatusCode))
                        {
                            throw new Exception(string.Format(
                                "Error closing the file state for the file handle: {0}", fileHandle));
                        }
                    }
                    catch (Exception e)
                    {
                        throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
                    }
                }
            }
        }
        #endregion

        #region Private Callback Methods

        /// <summary>
        /// Read the size of the file.
        /// </summary>
        private ServiceResult OnReadSize(
            ISystemContext context,
            NodeState node,
            ref object value)
        {
            try
            {
                FileInfo fi = new FileInfo(m_filePath);
                if (fi != null && fi.Exists)
                {
                    ulong size = (ulong)fi.Length;

                    value = size;

                    return ServiceResult.Good;
                }
                else
                {
                    return new ServiceResult(StatusCodes.BadUnexpectedError,
                        string.Format("The file: {0} was not found!", m_filePath));
                }
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }
        }

        /// <summary>
        /// Open method callback
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="mode"></param>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        private ServiceResult OnOpenMethodCall(
           ISystemContext context,
           MethodState method,
           NodeId objectId,
           byte mode,
           ref uint fileHandle)
        {
            FileMode fileMode = FileMode.Open;
            FileAccess fileAccess = FileAccess.Write;

            if (mode > 0xF)
            {
                // not supported at this time; options for bits position 4-7 are reserved for future version
                return StatusCodes.BadInvalidArgument;
            }

            FileStateMode fileStateMode = (FileStateMode)mode;
            bool isModeValid = false;
            bool isReadMode = false;
            if ((fileStateMode & FileStateMode.Read) == FileStateMode.Read)
            {
                isReadMode = true;
                fileAccess = FileAccess.Read;
                isModeValid = true;
            }
            if ((fileStateMode & FileStateMode.Write) == FileStateMode.Write)
            {
                if (fileAccess == FileAccess.Read)
                {
                    fileAccess = FileAccess.ReadWrite;
                }
                fileMode = FileMode.Open;
                isModeValid = true;
            }
            bool isEraseExistingMode = false;
            if ((fileStateMode & FileStateMode.EraseExisting) == FileStateMode.EraseExisting)
            {
                isEraseExistingMode = true;
                fileMode = FileMode.Truncate;
                isModeValid = true;
            }
            bool isAppendMode = false;
            if ((fileStateMode & FileStateMode.Append) == FileStateMode.Append)
            {
                isAppendMode = true;
                fileMode = FileMode.Append;
                isModeValid = true;
            }
            if (isReadMode && (isEraseExistingMode || isAppendMode))
            {
                isModeValid = false;
            }
            if (isEraseExistingMode && isAppendMode)
            {
                isModeValid = false;
            }

            if (isModeValid == false)
            {
                return StatusCodes.BadInvalidArgument;
            }

            if (fileAccess != FileAccess.Read && m_fileState.Writable.Value == false)
            {
                return StatusCodes.BadNotWritable;
            }

            try
            {
                FileStreamTracker fileStreamTracker = new FileStreamTracker(m_filePath, fileMode, fileAccess);

                // increment OpenCount
                ushort openCount = (ushort) m_fileState.OpenCount.Value;
                m_fileState.OpenCount.Value = ++openCount;
                m_fileState.OpenCount.ClearChangeMasks(null, true);

                fileHandle = ++m_nextFileHandle;
                m_fileHandles[fileHandle] = fileStreamTracker;

                return ServiceResult.Good;
            }
            catch (FileNotFoundException e)
            {
                throw new ServiceResultException(StatusCodes.BadNotFound, e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new ServiceResultException(StatusCodes.BadNotWritable, e);
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadNotReadable, e);
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
        protected virtual ServiceResult OnGetPositionMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            ref ulong position)
        {
            try
            {
                if (!m_fileHandles.ContainsKey(fileHandle))
                {
                    return StatusCodes.BadInvalidArgument;
                }

                FileStreamTracker fileStreamTracker = m_fileHandles[fileHandle];
                fileStreamTracker.LastAccessTime = DateTime.Now;
                position = (ulong)fileStreamTracker.FileStream.Position;

                return StatusCodes.Good;
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
        protected virtual ServiceResult OnSetPositionMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            ulong position)
        {
            try
            {
                if (!m_fileHandles.ContainsKey(fileHandle))
                {
                    return StatusCodes.BadInvalidArgument;
                }

                FileStreamTracker fileStreamTracker = m_fileHandles[fileHandle];
                fileStreamTracker.LastAccessTime = DateTime.Now;
                long offsetPosition = (long)position;
                if (offsetPosition < fileStreamTracker.FileStream.Length)
                {
                    fileStreamTracker.FileStream.Position = offsetPosition;
                }
                else
                {
                    fileStreamTracker.FileStream.Position = fileStreamTracker.FileStream.Length;
                }

                return StatusCodes.Good;
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
        protected virtual ServiceResult OnReadMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle,
            int length,
            ref byte[] data)
        {
            try
            {
                if (!m_fileHandles.ContainsKey(fileHandle))
                {
                    return StatusCodes.BadInvalidArgument;
                }

                FileStreamTracker fileStreamTracker = m_fileHandles[fileHandle];
                if (fileStreamTracker.FileStream.CanRead)
                {
                    fileStreamTracker.LastAccessTime = DateTime.Now;

                    data = new byte[length];
                    int cRead = fileStreamTracker.FileStream.Read(data, 0, length);

                    // if the amount of available bytes is less than the length requested
                    // we need to strip the buffer
                    if (cRead < length)
                    {
                        byte[] readData = new byte[cRead];
                        Array.Copy(data, readData, cRead);
                        data = readData;
                    }
                }
                else
                {
                    return StatusCodes.BadInvalidState;
                }

                return StatusCodes.Good;
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
        protected virtual ServiceResult OnWriteMethodCall(
          ISystemContext context,
          MethodState method,
          NodeId objectId,
          uint fileHandle,
          byte[] data)
        {
            try
            {
                if (!m_fileHandles.ContainsKey(fileHandle))
                {
                    return StatusCodes.BadInvalidArgument;
                }

                if (m_fileState.Writable.Value == false)
                {
                    return StatusCodes.BadInvalidState; 
                }

                FileStreamTracker fileStreamTracker = m_fileHandles[fileHandle];
                if (fileStreamTracker.FileStream.CanWrite)
                {
                    fileStreamTracker.LastAccessTime = DateTime.Now;
                    fileStreamTracker.FileStream.Write(data, 0, data.Length);
                }
                else
                {
                    return StatusCodes.BadInvalidState;
                }

                return StatusCodes.Good;
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadNotWritable, e.Message);
            }
        }

        /// <summary>
        /// Close method callback
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="fileNodeId"></param>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        protected virtual ServiceResult OnCloseMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId fileNodeId,
            uint fileHandle)
        {
            try
            {
                if (!m_fileHandles.ContainsKey(fileHandle))
                {
                    return StatusCodes.BadInvalidArgument;
                }

                FileStream fileStream = m_fileHandles[fileHandle].FileStream;
                m_fileHandles.Remove(fileHandle);

                //if the file was opened with Write access
                //we need to refresh the Size property
                bool bWasWritable = fileStream.CanWrite;
                fileStream.Close();

                if (bWasWritable)
                {
                    FileInfo fi = new FileInfo(m_filePath);

                    m_fileState.Size.Value = (ulong)fi.Length;
                    m_fileState.Size.ClearChangeMasks(null, false);
                }

                // decrement OpenCount
                ushort openCount = (ushort)m_fileState.OpenCount.Value;
                m_fileState.OpenCount.Value = --openCount;
                m_fileState.OpenCount.ClearChangeMasks(null, true);

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
