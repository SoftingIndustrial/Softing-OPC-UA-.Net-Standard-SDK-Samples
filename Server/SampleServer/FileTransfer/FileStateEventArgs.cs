using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace SampleServer.FileTransfer
{
    internal class FileStateEventArgs
    {
        #region Constructor
        public FileStateEventArgs(ISystemContext context, NodeId fileStateNodeId)
        {
            Context = context;
            FileStateNodeId = fileStateNodeId;
        }
        #endregion

        #region Properties

        public ISystemContext Context { get; private set; }

        public NodeId FileStateNodeId { get; private set; }
        
        #endregion
    }
}
