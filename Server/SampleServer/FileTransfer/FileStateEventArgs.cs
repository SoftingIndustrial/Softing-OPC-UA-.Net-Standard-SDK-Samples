using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// Event arguments class used to pass the temporary file state node from server address space 
    /// </summary>
    internal class FileStateEventArgs
    {
        #region Constructor
        public FileStateEventArgs(ISystemContext context, NodeId fileStateNodeId)
        {
            Context = context;
            FileStateNodeId = fileStateNodeId;
        }
        #endregion

        #region Public Properties

        public ISystemContext Context { get; private set; }

        public NodeId FileStateNodeId { get; private set; }
        
        #endregion
    }
}
