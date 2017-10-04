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
    /// This class specifies the attributes which belong to data type nodes.
    /// </summary>
    /// <remarks>
    /// Data type definitions allow industry specific data types to be used
    /// </remarks>
    public class DataTypeNode : BaseNode
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeNode"/> class.
        /// </summary>
        internal DataTypeNode()
        {
            AttributeStatusCodes.Add(AttributeId.IsAbstract, new StatusCode(StatusCodes.GoodNoData));        
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether this instance is abstract.
        /// </summary>
        /// <remarks>
        /// Abstract DataTypes can be used in the AddressSpace, i.e. Variables and VariableTypes can point with their DataType Attribute to an
        /// abstract DataType. However, concrete values can never be of an abstract DataType and shall always be of a concrete subtype of the abstract DataType.
        /// </remarks>
        public bool IsAbstract
        {
            get;
            internal set;
        }
        #endregion
    }
}
