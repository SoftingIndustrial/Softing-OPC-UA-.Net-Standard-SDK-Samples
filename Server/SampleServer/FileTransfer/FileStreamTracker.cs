using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SampleServer.FileTransfer
{
    public class FileStreamTracker
    {
        #region PrivateFields

        //private FileStream m_fileStream;
        private DateTime m_lastAccessTime;

        #endregion

        #region Constructors

        public FileStreamTracker(string path, FileMode fileMode, FileAccess fileAccess)
        {
            FileStream = new FileStream(path, fileMode, fileAccess);
            m_lastAccessTime = DateTime.Now;
        }

        #endregion

        #region PublicProperties

        public FileStream FileStream { get; private set; }

        public DateTime LastAccessTime { get; set; }

        #endregion
    }
}
