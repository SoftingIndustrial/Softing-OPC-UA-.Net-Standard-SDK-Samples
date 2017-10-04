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
    /// Specifies the attributes which belong to object type nodes.
    /// ObjectType nodes provide definitions for Objects.
    /// </summary>
    public class ObjectTypeNode : BaseNode
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectTypeNode"/> class.
        /// </summary>
        internal ObjectTypeNode()
        {
            AttributeStatusCodes.Add(AttributeId.IsAbstract, new StatusCode(StatusCodes.GoodNoData));
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether this instance is abstract.
        /// </summary>
        /// <remarks>
        ///  If an Object Type is not abstract Objects of this type can exists. If is not abstract Objects of this type shall exist only through its subtypes.
        /// </remarks>
        public bool IsAbstract
        {
            get;
            internal set;
        }
        #endregion
    }
}
