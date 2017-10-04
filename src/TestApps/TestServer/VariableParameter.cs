using Opc.Ua;
using System;

namespace TestServer
{
    public class VariableParameter : DataItemState
    {
        #region Constructors

        public VariableParameter(NodeState parent) : base(parent)
        {   
        }

        #endregion

        public uint GetUIntValue()
        {
            if (DataType != DataTypeIds.UInt32)
            {
                throw new Exception("The data type of the variable is not UInt32!");
            }

            return (uint)Value;
        }

        public string GetStringValue()
        {
            if (DataType != DataTypeIds.String)
            {
                throw new Exception("The data type of the variable is not String!");
            }

            return (string)Value;
        }

        public bool GetBoolValue()
        {
            if (DataType != DataTypeIds.Boolean)
            {
                throw new Exception("The data type of the variable is not Boolean!");
            }

            return (bool)Value;
        }

        public double GetDoubleValue()
        {
            if (DataType != DataTypeIds.Double)
            {
                throw new Exception("The data type of the variable is not Double!");
            }

            return (double)Value;
        }
    }
}