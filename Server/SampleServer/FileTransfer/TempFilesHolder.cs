using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Opc.Ua;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// Temporary file states holder class
    /// </summary>
    internal class TempFilesHolder
    {
        #region Private Members

        private uint m_nextFileHandle;
        private Dictionary<uint, TempFileStateData> m_tmpFileStateData;

        #endregion

        #region Constructor

        public TempFilesHolder()
        {
            m_nextFileHandle = 0;
            m_tmpFileStateData = new Dictionary<uint, TempFileStateData>();
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Add new temporary file state node
        /// </summary>
        /// <param name="fileNodeId"></param>
        /// <param name="tmpFileStateHandler"></param>
        /// <returns></returns>
        public uint Add(NodeId fileNodeId, TempFileStateHandler tmpFileStateHandler)
        {
            if (!m_tmpFileStateData.Values.Any(tmp => tmp.FileNodeId == fileNodeId))
            {
                m_tmpFileStateData.Add(++m_nextFileHandle, new TempFileStateData(tmpFileStateHandler, fileNodeId));
                return m_nextFileHandle;
            }
            
            return 0;
        }

        /// <summary>
        /// Check if file handle exists in the current holder
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public bool Exists(uint fileHandle)
        {
            return fileHandle <= m_nextFileHandle;
        }

        /// <summary>
        /// Get file state data identified by file handle
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        public TempFileStateData Get(uint fileHandle)
        {
            if (Exists(fileHandle))
            {
                if (m_tmpFileStateData.ContainsKey(fileHandle))
                {
                    return m_tmpFileStateData[fileHandle];
                }
            }

            return null;
        }

        /// <summary>
        /// Remove file state data reference from the holder
        /// </summary>
        /// <param name="fileHandle"></param>
        public void Remove(uint fileHandle)
        {
            if (m_tmpFileStateData.ContainsKey(fileHandle))
            {
                m_tmpFileStateData.Remove(fileHandle);
            }
        }

        /// <summary>
        /// Remove temporary file state nodes from server address space
        /// </summary>
        public void RemoveFileStateNodes()
        {
            lock (this)
            {
                foreach (TempFileStateData tmpFileStateData in m_tmpFileStateData.Values)
                {
                    tmpFileStateData.RemoveFileStateNodes();
                }

                m_tmpFileStateData.Clear();
            }
        }
        #endregion
    }
}
