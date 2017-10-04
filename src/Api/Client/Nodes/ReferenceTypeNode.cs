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
    /// Specifies the attributes which belong to reference type nodes.
    /// </summary>
    /// <remarks>
    /// References are defined as instances of ReferenceType Nodes. ReferenceType Nodes are visible in the AddressSpace.
    /// In contrast, a Reference is an inherent part of a Node and no NodeClass is used to represent References. 
    /// A set of ReferenceTypes is defined in standard as an inherent part of the OPC UA Address Space Model. Servers may also define ReferenceTypes. 
    /// NodeManagement Services  allow Clients to add ReferenceTypes to the AddressSpace.
    /// </remarks>
    public class ReferenceTypeNode : BaseNode
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceTypeNode"/> class.
        /// </summary>
        internal ReferenceTypeNode()
        {
            AttributeStatusCodes.Add(AttributeId.IsAbstract, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.InverseName, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.Symmetric, new StatusCode(StatusCodes.GoodNoData));        
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceTypeNode"/> class.
        /// </summary>
        /// <param name="referenceTypeNode">The Opc Ua reference type node.</param>
        internal ReferenceTypeNode(Opc.Ua.ReferenceTypeNode referenceTypeNode) :
            base(referenceTypeNode)
        {
            if (referenceTypeNode != null)
            {
                IsAbstract = referenceTypeNode.IsAbstract;
                InverseName = new LocalizedText(referenceTypeNode.InverseName);
                Symmetric = referenceTypeNode.Symmetric;
            }
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether this reference is abstract.
        /// </summary>
        /// <remarks>
        ///  If an Reference Type is not abstract References of this type can exists. If is not abstract References of this type shall exist only through its subtypes.
        /// </remarks>
        public bool IsAbstract
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether ReferenceType <see cref="ReferenceTypeNode"/> is the same as seen from both the SourceNode and the TargetNode.
        /// </summary>
        /// <value>
        /// <c>True</c> : the meaning of the ReferenceType is the same as seen from both the SourceNode and the TargetNode. <br/>
        /// <c>False</c> : the meaning of the ReferenceType as seen from the TargetNode is the inverse of that as seen from the SourceNode
        /// </value>
        public bool Symmetric
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the inverse name of the reference that is the meaning of the ReferenceType as seen from the TargetNode.
        /// </summary>
        public LocalizedText InverseName
        {
            get;
            internal set;
        }
        #endregion
    }
}
