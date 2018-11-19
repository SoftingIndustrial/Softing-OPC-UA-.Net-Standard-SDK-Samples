using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Opc.Ua;

namespace SampleServer.FileTransfer
{
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

        public uint Add(NodeId fileNodeId, TempFileStateHandler tmpFileStateHandler)
        {
            if (!m_tmpFileStateData.Values.Any(tmp => tmp.FileNodeId == fileNodeId))
            {
                m_tmpFileStateData.Add(++m_nextFileHandle, new TempFileStateData(tmpFileStateHandler, fileNodeId));
                return m_nextFileHandle;
            }
            
            return 0;
        }

        public bool Exists(uint fileHandle)
        {
            return fileHandle <= m_nextFileHandle;
        }

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

        public void Remove(uint fileHandle)
        {
            if (m_tmpFileStateData.ContainsKey(fileHandle))
            {
                m_tmpFileStateData.Remove(fileHandle);
            }
        }

        public void RemoveFileStateNodes()
        {
            lock (this)
            {
                foreach (TempFileStateData tmpFileStateData in m_tmpFileStateData.Values)
                {
                    tmpFileStateData.RemoveFileStateNodes();
                }
            }
        }
        #endregion
    }
}
