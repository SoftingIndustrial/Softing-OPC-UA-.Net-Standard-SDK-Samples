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
    /// Specifies the attributes which belong to view nodes.
    /// </summary>
    /// <remarks>
    /// View are used for clients that have an interest in only a specific subset of the data.
    /// Each View defines a subset of the Nodes in the AddressSpace. The entire AddressSpace is the default View. 
    /// Each Node in a View may contain only a subset of its References, as defined by the creator of the View. 
    /// The View Node acts as the root for the Nodes in the View.
    /// </remarks>
    public class ViewNode : BaseNode
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewNode"/> class.
        /// </summary>
        internal ViewNode()
        {
            AttributeStatusCodes.Add(AttributeId.ContainsNoLoops, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.EventNotifier, new StatusCode(StatusCodes.GoodNoData));
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether References in the context of the View may lead to loops
        /// i.e. starting from a Node “A” contained in the View and following the forward References in the
        ///  context of the View if Node “A” will be reached again
        /// </summary>
        /// <value>
        ///   <c>True</c> - contains no loops. It does not specify that there is only one path starting from the View Node to
        ///   reach a Node contained in the View <br/>
        ///   <c>False</c> - indicates that following References in the context of the View may lead to loops.
        /// </value>
        public bool ContainsNoLoops
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether the Node can be used to subscribe to Events or to read / write historic Events.
        /// </summary>
        /// <value>
        ///  Same value format returned by <see cref="ObjectNode.EventNotifier"/>
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
        #endregion
    }
}
