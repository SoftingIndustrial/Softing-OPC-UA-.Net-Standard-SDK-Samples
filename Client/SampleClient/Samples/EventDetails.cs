/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 *  
 * ======================================================================*/

using Opc.Ua;

namespace SampleClient.Samples
{
    /// <summary>
    /// Holder for event detail properties
    /// </summary>
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
