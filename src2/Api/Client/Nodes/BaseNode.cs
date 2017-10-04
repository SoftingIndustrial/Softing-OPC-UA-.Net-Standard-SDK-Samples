/* ========================================================================
 * Copyright © 2011-2017 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System.Collections.Generic;

namespace Opc.Ua.Toolkit.Client.Nodes
{
    /// <summary>
    /// The base node class for all the nodes in the AddressSpace. Specifies the common attributes which belong to all nodes.
    /// </summary>
    /// <remarks> 
    /// Attributes defined in this class allows identification, classification and naming. Each derived class inherits these attributes and may additionally define its own attributes.
    /// </remarks>
    public abstract class BaseNode
    {
        #region Fields
        private Dictionary<AttributeId, StatusCode> m_attributeStatusCodes = new Dictionary<AttributeId, StatusCode>();
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNode"/> class.
        /// </summary>
        internal BaseNode()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNode"/> class.
        /// </summary>
        /// <param name="baseNode">A <see cref="Opc.Ua.Sdk.Node"/> base node.</param>
        internal BaseNode(Node baseNode)
        {
            if (baseNode != null)
            {
                NodeId = new NodeId(baseNode.NodeId);
                NodeClass = baseNode.NodeClass;
                BrowseName = new QualifiedName(baseNode.BrowseName);
                DisplayName = new LocalizedText(baseNode.DisplayName);
                Description = new LocalizedText(baseNode.Description);
                WriteMask = baseNode.WriteMask;
                UserWriteMask = baseNode.UserWriteMask;
            }
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the node identifier
        /// </summary>
        public NodeId NodeId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the node type as a node class.
        /// </summary>
        public NodeClass NodeClass
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the fully-qualified name of the node used on browsing.
        /// </summary>
        public QualifiedName BrowseName
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the human readable name of the node.
        /// </summary>
        /// <remarks>
        /// This name can be the same for a couple of nodes in the address space , only BrowseName is different 
        /// for that nodes because BrowseName contains also the namespace index.
        /// </remarks>
        public LocalizedText DisplayName
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a short description of the node.
        /// </summary>
        public LocalizedText Description
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a mask indicating which attributes are writeable.
        /// </summary>
        /// <value>
        /// Bit 0  - 20 indicates if this Attributes are writable in this order: AccessLevel, 
        /// ArrayDimensions, BrowseName, ContainsNoLoops, DataType,
        /// Description, DisplayName, EventNotifier, Executable, Historizing,
        /// InverseName, IsAbstract, MinimumSamplingInterval, NodeClass, NodeId ,Symmetric, UserAccessLevel
        /// UserExecutable, UserWriteMask, ValueRank, WriteMask. <br/>
        /// Bit 21 - indicates if the Value Attribute is writable for a VariableType. It does not apply for
        /// Variables since this is handled by the AccessLevel and UserAccessLevel Attributes for the Variable. For Variables this bit shall be set to 0.<br/>
        /// Bits 22:31 Reserved for future use. Shall always be zero.
        /// </value>
        public uint WriteMask
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a mask indicating which attributes are writeable by current user.
        /// </summary>
        /// <value>
        ///  Same value format as for <see cref="WriteMask"/>
        /// </value>
        public uint UserWriteMask
        {
            get;
            internal set;
        }
        #endregion        

        #region Internal Properties
        /// <summary>
        /// Gets the attribute status codes.
        /// </summary>
        protected internal Dictionary<AttributeId, StatusCode> AttributeStatusCodes
        {
            get 
            {
                return m_attributeStatusCodes; 
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the status code for the specified attribute id of the node.
        /// </summary>
        /// <param name="attributeId">A <see cref="AttributeId"/> representing attribute identifier.</param>
        /// <returns>A numeric code that describes the result of an operation or service</returns>
        public StatusCode GetStatusCode(AttributeId attributeId)
        {
            if (!AttributeStatusCodes.ContainsKey(attributeId))
            {
                throw new BaseException("Attribute Id not found in node instance!");
            }    
            return AttributeStatusCodes[attributeId];
        }
        #endregion

        #region Internal Methods
        
        /// <summary>
        /// Sets the attribute status code.
        /// </summary>
        /// <param name="attributeId">The attribute id.</param>
        /// <param name="statusCode">The status code.</param>
        internal void SetAttibuteStatusCode(AttributeId attributeId, StatusCode statusCode)
        {
            if (!AttributeStatusCodes.ContainsKey(attributeId))
            {
                throw new BaseException("Attribute Id not found in node instance!");
            }

            AttributeStatusCodes[attributeId] = statusCode;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void Initialize()
        {
            AttributeStatusCodes.Add(AttributeId.NodeId, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.NodeClass, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.BrowseName, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.DisplayName, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.Description, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.WriteMask, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.UserWriteMask, new StatusCode(StatusCodes.GoodNoData));
        }
        #endregion
    }
}
