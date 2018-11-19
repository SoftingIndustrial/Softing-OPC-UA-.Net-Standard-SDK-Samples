using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// Temporary file state data class
    /// </summary>
    internal class TempFileStateData
    {
        #region Constructor
        public TempFileStateData(TempFileStateHandler tmpFileStateHandler, NodeId fileNodeId)
        {
            FileStateHandler = tmpFileStateHandler;
            FileNodeId = fileNodeId;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Temporary file state handler reference
        /// </summary>
        public TempFileStateHandler FileStateHandler { get; private set; }

        /// <summary>
        /// File node id reference
        /// </summary>
        public NodeId FileNodeId { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Remove temporary file state nodes from server address space
        /// </summary>
        public void RemoveFileStateNodes()
        {
            if (FileStateHandler != null)
            {
                FileStateHandler.RemoveFileStateNodes(FileNodeId);
            }
        }
        #endregion
    }
}
