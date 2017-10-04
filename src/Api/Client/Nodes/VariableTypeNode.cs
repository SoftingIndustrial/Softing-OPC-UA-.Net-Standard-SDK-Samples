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
using System.Linq;

namespace Opc.Ua.Toolkit.Client.Nodes
{
    /// <summary>
    /// Specifies the attributes which belong to variable type nodes.
    /// </summary>
    /// <remarks>
    /// VariableTypes are used to provide type definitions for Variables.
    /// </remarks>
    public class VariableTypeNode : BaseNode
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="VariableTypeNode"/> class.
        /// </summary>
        internal VariableTypeNode()
        {
            AttributeStatusCodes.Add(AttributeId.Value, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.DataType, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.ArrayDimensions, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.IsAbstract, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.ValueRank, new StatusCode(StatusCodes.GoodNoData));
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the default Value for instances of this type.
        /// </summary>
        public DataValue Value
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets NodeId of the data type definition for instances of this type.
        /// </summary>
        public NodeId DataTypeId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the name of node identified by <see cref="DataTypeId"/>.
        /// </summary>
        public string DataType 
        { 
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is abstract.
        /// </summary>
        /// <remarks>
        ///  If an Variable Type is not abstract Variables of this type can exists. If is not abstract Variables of this type shall exist only through its subtypes.
        /// </remarks>
        public bool IsAbstract
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether the Value Attribute of the VariableType is an array and how many dimensions the array has. See <see cref="ValueRanks"/> enumeration.
        /// </summary>
        public ValueRanks ValueRank
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value specifyifing the length of each dimension for an array value. 
        /// </summary>
        /// <remarks>
        /// The Attribute is intended to describe the capability of the Variable, not the current size. 
        /// The number of elements shall be equal to the value of the ValueRank Attribute. 
        /// </remarks>
        /// <value>
        /// Shall be null if ValueRank is less than or equal to 0.<br/>
        /// A value of 0 for an individual dimension indicates that the dimension
        /// has a variable length.
        /// </value>
        public List<uint> ArrayDimensions
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the array dimensions as a text see <see cref="ArrayDimensions"/>
        /// </summary>
        /// <remarks>
        /// The array dimensions are represented as list of numbers separated by  '|'
        /// i.e. : 4 | 6 | 8
        /// </remarks>
        public string ArrayDimensionsText
        {
            get
            {
                if (ArrayDimensions == null || ArrayDimensions.Count == 0)
                {
                    return string.Empty;
                }

                string text = string.Empty;
                List<uint> arrayDimmensions = ArrayDimensions.ToList();
                for (int i = 0; i < arrayDimmensions.Count; i++)
                {
                    if (i + 1 == arrayDimmensions.Count)
                    {
                        text += arrayDimmensions[i];
                    }
                    else
                    {
                        text += arrayDimmensions[i] + " | ";
                    }
                }
                return text;
            }
        }
        #endregion
    }
}
