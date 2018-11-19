using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Linq;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// File state handler class
    /// </summary>
    internal class FileStateHandler 
    {
        #region Private Members

        protected FileState m_fileState;
        protected string m_filePath;
        private uint m_nextFileHandle;
        private Timer m_Timer;
        private Dictionary<uint, FileStreamTracker> m_fileHandles;

        /// <summary>
        /// Clean up threshold period till all opened streams will be closed
        /// </summary>
        private const int CheckFileStreamAvailabilityPeriod = 50; // seconds
        #endregion

        #region Constructors

        private FileStateHandler()
        {
            m_nextFileHandle = 0;
            m_fileHandles = new Dictionary<uint, FileStreamTracker>();
            m_Timer = new Timer(CheckFileStreamAvailability, null, 0, CheckFileStreamAvailabilityPeriod); // remove opened files after 
        }
        public FileStateHandler(string filePath) : this()
        {
            m_filePath = filePath;
            Name = Path.GetFileName(filePath);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// File State reference
        /// </summary>
        protected FileState FileState
        {
            get { return m_fileState; }
        }
        /// <summary>
        /// File name info
        /// </summary>
        public string Name { get; set; }

        protected string FilePath
        {
            get { return m_filePath; }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Set FileState callbacks
        /// </summary>
        /// <param name="fileState"></param>
        public virtual void SetCallbacks(FileState fileState)
        {
            if (fileState != null)
            {
                m_fileState = fileState;

                m_fileState.Open.OnCall = OnOpenMethodCall;
                m_fileState.Read.OnCall = OnReadMethodCall;
                m_fileState.Close.OnCall = OnCloseMethodCall;
                m_fileState.Write.OnCall = OnWriteMethodCall;
                m_fileState.GetPosition.OnCall = OnGetPositionMethodCall;
                m_fileState.SetPosition.OnCall = OnSetPositionMethodCall;
                m_fileState.Size.OnSimpleReadValue = OnReadSize;
            }
        }

        #endregion
        /// <summary>
        /// Get file stream related to a file handle
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        protected FileStream GetFileStream(uint fileHandle)
        {
            if (m_fileHandles.ContainsKey(fileHandle))
            {
                return m_fileHandles[fileHandle].FileStream;
            }

            return null;
        }

        #region Private Callback Methods

        /// <summary>
        /// Get Position method callback
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private ServiceResult OnGetPositionMethodCall(
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
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
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
        private ServiceResult OnSetPositionMethodCall(
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
                fileStreamTracker.FileStream.Position = (long)position;

                return StatusCodes.Good;
            }
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
            }
        }
        #endregion

        #region Protected Callback Methods

        /// <summary>
        /// Read the size of the file.
        /// </summary>
        protected ServiceResult OnReadSize(
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
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
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
        protected ServiceResult OnOpenMethodCall(
           ISystemContext context,
           MethodState method,
           NodeId objectId,
           byte mode,
           ref uint fileHandle)
        {
            FileMode fileMode;
            FileAccess fileAccess;

            switch (mode & 3)
            {
                case 1: fileAccess = FileAccess.Read; break;
                case 2: fileAccess = FileAccess.Write; break;
                case 3: fileAccess = FileAccess.ReadWrite; break;
                default: fileAccess = FileAccess.Read; break;
            }

            if ((mode & 4) == 4)
                fileMode = FileMode.Truncate;
            else
                fileMode = FileMode.Open;

            if (fileAccess != FileAccess.Read && m_fileState.Writable.Value == false)
            {
                return StatusCodes.BadWriteNotSupported;
            }

            try
            {
                FileStreamTracker fileStreamTracker = new FileStreamTracker(m_filePath, fileMode, fileAccess);

                //increment OpenCount.
                ushort openCount = (ushort)m_fileState.OpenCount.Value;
                m_fileState.OpenCount.Value = ++openCount;

                fileHandle = ++m_nextFileHandle;
                m_fileHandles[fileHandle] = fileStreamTracker;

                return ServiceResult.Good;
            }
            catch (FileNotFoundException ex)
            {
                throw new ServiceResultException(StatusCodes.BadNotFound, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ServiceResultException(StatusCodes.BadNotWritable, ex.Message);
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
        protected ServiceResult OnReadMethodCall(
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

                return StatusCodes.Good;
            }
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
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
        protected ServiceResult OnWriteMethodCall(
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
                    return StatusCodes.BadWriteNotSupported;
                }

                FileStreamTracker fileStreamTracker = m_fileHandles[fileHandle];
                fileStreamTracker.LastAccessTime = DateTime.Now;
                fileStreamTracker.FileStream.Write(data, 0, data.Length);

                return StatusCodes.Good;
            }
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
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
        protected virtual ServiceResult OnCloseMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
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

                return StatusCodes.Good;
            }
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
            }
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Check when the stream was last time accessed and close it
        /// </summary>
        /// <param name="state"></param>
        private void CheckFileStreamAvailability(object state)
        {
            lock (this)
            {
                foreach (KeyValuePair<uint, FileStreamTracker> entry in m_fileHandles.ToList())
                {
                    TimeSpan duration = DateTime.Now - entry.Value.LastAccessTime;

                    if (duration.TotalSeconds > CheckFileStreamAvailabilityPeriod)
                    {
                        if (FileState != null)
                        {
                            try
                            {
                                uint fileHandle = entry.Key;
                                ServiceResult writeResult = FileState.Close.OnCall(null, null, null, fileHandle);
                                if (StatusCode.IsBad(writeResult.StatusCode))
                                {
                                    throw new Exception(string.Format(
                                        "Error closing the file state for the file handle: {0}", fileHandle));
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new ServiceResultException(StatusCodes.BadUnexpectedError, ex.Message);
                            }
                        }
                        else
                        {
                            m_fileHandles.Remove(entry.Key);
                            entry.Value.FileStream.Close();
                        }
                    }
                }
            }
        }

        #endregion
    }

}
