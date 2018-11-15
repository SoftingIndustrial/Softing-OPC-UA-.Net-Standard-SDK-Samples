using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace SampleServer.FileTransfer
{
    internal class TempFileStateData 
    {
        #region Constructor
        public TempFileStateData(TempFileStateHandler tmpFileStateHandler, NodeId fileNodeId)
        {
            FileStateHandler = tmpFileStateHandler;
            FileNodeId = fileNodeId;
        }
        #endregion

        #region Properties

        public TempFileStateHandler FileStateHandler { get; private set; }
        
        public NodeId FileNodeId { get; private set; }

        #endregion
    }
}
