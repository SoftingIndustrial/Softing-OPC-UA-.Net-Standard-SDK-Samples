using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// Holds the file stream instance created to a specified time
    /// </summary>
    public class FileStreamTracker
    {
        #region Private Fields
        #endregion

        #region Constructors

        public FileStreamTracker(string path, FileMode fileMode, FileAccess fileAccess)
        {
            FileStream = new FileStream(path, fileMode, fileAccess);
            LastAccessTime = DateTime.Now;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// File stream reference
        /// </summary>
        public FileStream FileStream { get; private set; }

        /// <summary>
        /// The time when the file stream was accessed (used) 
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        #endregion
    }
}
