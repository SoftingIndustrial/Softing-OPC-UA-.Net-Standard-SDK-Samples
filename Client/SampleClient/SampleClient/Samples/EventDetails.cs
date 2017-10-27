/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

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
