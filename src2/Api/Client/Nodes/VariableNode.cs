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
using System.Text;

namespace Opc.Ua.Toolkit.Client.Nodes
{
    /// <summary>
    ///  Specifies the attributes which belong to variable nodes.
    /// </summary>
    /// <remarks>
    ///  Variables are used to represent values which may be simple or complex. Variables are defined by Variable Types see <see cref="VariableTypeNode"/>
    ///  Variables are always defined as Properties or DataVariables of other Nodes in the AddressSpace.
    ///  They are never defined by themselves. A Variable is always part of at least one other Node, but may be related to any number of other Nodes.
    /// </remarks>
    public class VariableNode : BaseNode
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="VariableNode"/> class.
        /// </summary>
        internal VariableNode()
        {
            AttributeStatusCodes.Add(AttributeId.Value, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.DataType, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.ArrayDimensions, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.AccessLevel, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.UserAccessLevel, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.MinimumSamplingInterval, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.ValueRank, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.Historizing, new StatusCode(StatusCodes.GoodNoData));
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the most recent value of the Variable that the Server has. Its data type is defined by the DataType Attribute <see cref="VariableNode.DataType"/>. It is the only Attribute that
        /// does not have a data type associated with it. This allows all Variables to have a value defined by the same Value Attribute
        /// </summary>
        public DataValue Value
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets whether the Value Attribute of the Variable is an array and how many dimensions the array has. See <see cref="ValueRanks"/>
        /// </summary>
        /// <remarks>
        /// Note that all DataTypes are considered to be scalar, even if they have array-like semantics like ByteString and String.
        /// </remarks>
        public ValueRanks ValueRank
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the human readable text representing the name of node DataType identified by <see cref="DataTypeId"/> .  
        /// </summary>
        public string DataType
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the Node Identifier of the DataType definition.
        /// </summary>
        public NodeId DataTypeId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the length of each dimension for an array value.
        /// </summary>
        /// <remarks>
        /// The Attribute is intended to describe the capability of the Variable, not the current size. 
        /// The number of elements shall be equal to the value of the ValueRank Attribute. 
        /// </remarks>
        /// <value>
        /// Shall be null if ValueRank  is less than or equal to 0.<br/>
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
        ///  The array dimensions are represented as list of numbers separated by  '|'
        ///  i.e. : 4 | 6 | 8
        /// </remarks>
        public string ArrayDimensionsText
        {
            get
            {
                if (ArrayDimensions.Count == 0)
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

        /// <summary>
        /// Gets how the Value of a Variable can be accessed (read/write) and if it contains current and/or historic data.
        /// The AccessLevel does not take any user access rights into account.
        /// </summary>
        /// <value>
        /// Bit 0 : Indicates if the current value is readable (0 means not readable, 1 means readable). <br/>
        /// Bit 1 : Indicates if the current value is writable (0 means not writable, 1 means writable).<br/>
        /// Bit 2 : Indicates if the history of the value is readable (0 means not readable, 1 means readable). <br/>
        /// Bit 3 : Indicates if the history of the value is writable (0 means not writable, 1 means writable). <br/>
        /// Bit 4 : Indicates if the Variable used as Property generates SemanticChangeEvents  <br/>
        /// Bits 5-7 : Reserved 
        /// </value>
        public byte AccessLevel
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the access level see <see cref="AccessLevel"/> as text.
        /// </summary>
        /// <value>
        /// A list of human readable access right separated by " | "
        /// i.e : Readable | Writable
        /// </value>
        public string AccessLevelText
        {
            get { return GetAccessLevelText(AccessLevel); }
        }

        /// <summary>
        /// Gets how the Value of a Variable can be accessed (read/write) and if it contains current or
        /// historic data taking user access rights into account.
        /// </summary>
        /// <value>
        /// The value has similar meaning with the value returned by <see cref="AccessLevel"/>
        /// </value>
        public byte UserAccessLevel
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the user access level see <see cref="UserAccessLevel"/> as text.
        /// </summary>
        /// <value>
        /// The value has similar format as value returned by <see cref="AccessLevelText"/>
        /// </value>
        public string UserAccessLevelText
        {
            get { return GetAccessLevelText(UserAccessLevel); }
        }

        /// <summary>
        /// Gets how the Value of the Variable will be kept. It specifies in milliseconds how fast the Server can reasonably sample the value for changes.
        /// </summary>
        /// <value>
        /// A MinimumSamplingInterval of 0 indicates that the Server is to monitor the item continuously. <br/>
        /// A MinimumSamplingInterval of -1 means indeterminate.
        /// </value>
        public double MinimumSamplingInterval
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the minimum sampling interval see <see cref="MinimumSamplingInterval"/> as a text.
        /// </summary>
        /// <value>
        /// For indeterminate the 'Indeterminate' string will be returned. <br/>
        /// For continuous the 'Continuous' string will be returned. <br/>
        /// In other cases a number in string format will be returned.
        /// </value>
        public string MinimumSamplingIntervalText
        {
            get 
            {
                if (MinimumSamplingInterval == MinimumSamplingIntervals.Indeterminate)
                {
                    return "Indeterminate";
                }
                else if (MinimumSamplingInterval == MinimumSamplingIntervals.Continuous)
                {
                    return "Continuous";
                }

                return string.Format("{0}", MinimumSamplingInterval);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Server is actively collecting data for the history of the Variable. This differs from the
        /// AccessLevel Attribute which identifies if the Variable has any historical data. 
        /// </summary>
        /// <value>
        /// A value of <c>True</c> indicates that the Server is actively collecting data. <br/>
        /// A value of <c>False</c> indicates the Server is not actively collecting data. Default value is <c>False</c>.
        /// </value>
        public bool Historizing
        {
            get;
            internal set;
        }
        #endregion

        #region Private Methods
        private static string GetAccessLevelText(byte accessLevel)
        {
            StringBuilder bits = new StringBuilder();

            if ((accessLevel & AccessLevels.CurrentRead) != 0)
            {
                bits.Append("Readable");
            }

            if ((accessLevel & AccessLevels.CurrentWrite) != 0)
            {
                if (bits.Length > 0)
                {
                    bits.Append(" | ");
                }

                bits.Append("Writeable");
            }

            if ((accessLevel & AccessLevels.HistoryRead) != 0)
            {
                if (bits.Length > 0)
                {
                    bits.Append(" | ");
                }

                bits.Append("History Read");
            }

            if ((accessLevel & AccessLevels.HistoryWrite) != 0)
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
        #endregion
    }
}
