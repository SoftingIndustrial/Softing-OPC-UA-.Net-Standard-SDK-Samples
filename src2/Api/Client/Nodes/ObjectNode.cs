/* ========================================================================
 * Copyright © 2011-2017 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

namespace Opc.Ua.Toolkit.Client.Nodes
{
    /// <summary>
    ///  Specifies the attributes which belong to object nodes.
    /// </summary>
    /// <remarks>
    /// Object Node can define DataVariables, Objects, Methods, Properties, Event Sources and Notifiers of an Object.
    /// </remarks>
    public class ObjectNode : BaseNode
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectNode"/> class.
        /// </summary>
        internal ObjectNode()
        {
            AttributeStatusCodes.Add(AttributeId.EventNotifier, new StatusCode(StatusCodes.GoodNoData));
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets an attribute used to indicate if the Node can be used to subscribe to Events or the read / write historic Events.
        /// </summary>
        /// <value>
        /// Bit 0 - SubscribeToEvents (0 means cannot be used to subscribe to Events, 1 means can be used to subscribe to Events). <br/>
        /// Bit 2 - HistoryRead (Indicates if the history of the Events is readable:  0 means not readable, 1 means readable). <br/>
        /// Bit 3 - HistoryWrite ( Indicates if the history of the Events is writable: 0 means not writable, 1 means writable). <br/>
        /// Other bits - Reserved <br/>
        /// The second two bits also indicate if the history of the Events is available via the OPC UA Server.
        /// </value>
        public byte EventNotifier
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the EventNotifier as a human readable text.
        /// </summary>
        /// <value>
        /// The value can contain a text like this: 'Subscribe | History | History Update'
        /// or 'No Access' if not bits are set
        /// </value>
        public string EventNotifierText
        {
            get 
            {
                byte notifier = EventNotifier;

                System.Text.StringBuilder bits = new System.Text.StringBuilder();

                if ((notifier & EventNotifiers.SubscribeToEvents) != 0)
                {
                    bits.Append("Subscribe");
                }

                if ((notifier & EventNotifiers.HistoryRead) != 0)
                {
                    if (bits.Length > 0)
                    {
                        bits.Append(" | ");
                    }

                    bits.Append("History");
                }

                if ((notifier & EventNotifiers.HistoryWrite) != 0)
                {
                    if (bits.Length > 0)
                    {
                        bits.Append(" | ");
                    }

                    bits.Append("History Update");
                }

                if (bits.Length == 0)
                {
                    bits.Append("No Access");
                }

                return string.Format("{0}", bits);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has event notifier and can be used to subscribe to events.
        /// </summary>
        /// <value>
        /// <c>True</c> can be used to subscribe to events <br/>
        /// <c>False</c> cannot be used to subscribe to events
        /// </value>
        public bool HasEventNotifier
        {
            get
            {
                return ((EventNotifier & EventNotifiers.SubscribeToEvents) != 0);
            }
        }
        #endregion
    }
}
