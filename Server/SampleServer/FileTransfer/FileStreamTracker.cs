/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
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
        /// The time when the file stream was accessed (last used) 
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        #endregion
    }
}
