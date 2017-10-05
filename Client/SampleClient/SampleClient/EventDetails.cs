using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleClient
{
    public class EventDetails
    {
        #region Public Interface

        public NodeId EventNode;

        public byte[] EventId;

        public NodeId SourceNode;

        public string SourceName;

        public LocalizedText Message;

        public LocalizedText Comment;

        public EventSeverity Severity;

        #endregion
    }
}
