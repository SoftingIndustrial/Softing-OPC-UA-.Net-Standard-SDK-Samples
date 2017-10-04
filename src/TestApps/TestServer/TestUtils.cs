using System;
using Opc.Ua;

namespace TestServer
{
    public class TestUtils
    {
        public static void SetDefaultValue(BaseVariableState variable)
        {
            if (variable.DataType == null)
                throw new Exception("The data type of the variable is not set!");

            if (variable.DataType == DataTypeIds.String)
                variable.Value = string.Empty;
            if (variable.DataType == DataTypeIds.Boolean)
                variable.Value = false;
            if (variable.DataType == DataTypeIds.SByte)
                variable.Value = (byte)0;
            if (variable.DataType == DataTypeIds.Byte)
                variable.Value = (byte)0;
            if (variable.DataType == DataTypeIds.UInt16)
               variable.Value = (UInt16)0;
            if (variable.DataType == DataTypeIds.Int16)
                variable.Value = (Int16)0;
            if (variable.DataType == DataTypeIds.UInt32)
                variable.Value = (UInt32)0;
            if (variable.DataType == DataTypeIds.Int32)
                variable.Value = (int)0;
            if (variable.DataType == DataTypeIds.UInt64)
                variable.Value = (UInt64)0;
            if (variable.DataType == DataTypeIds.Int64)
                variable.Value = (Int64)0;
            if (variable.DataType == DataTypeIds.Float)
                variable.Value = (float)0;
            if (variable.DataType == DataTypeIds.Double)
                variable.Value = (double)0;
        }

        public static VarType DataTypeToVarType(NodeId dataType)
        {
            VarType result = VarType.Unknown;
            if (dataType == null)
                throw new Exception("The data type is null!");

            //if (dataType == DataTypeIds.String)
            //    result = VarType.;
            //if (dataType == DataTypeIds.Boolean)
            //    variable.Value = false;
            if (dataType == DataTypeIds.Double)
                result = VarType.Double;
            if (dataType == DataTypeIds.SByte)
                result = VarType.Uint8;
            if (dataType == DataTypeIds.Byte)
                result = VarType.Uint8;
            if (dataType == DataTypeIds.UInt16)
                result = VarType.Uint16;
            if (dataType == DataTypeIds.Int16)
                result = VarType.Int16;
            if (dataType == DataTypeIds.UInt32)
                result = VarType.Uint32;
            if (dataType == DataTypeIds.Int32)
                result = VarType.Int32;
            if (dataType == DataTypeIds.UInt64)
                result = VarType.Uint64;
            if (dataType == DataTypeIds.Int64)
                result = VarType.Int64;
            if (dataType == DataTypeIds.Float)
                result = VarType.Float;

            if (result == VarType.Unknown)
                throw new Exception("Unknown data type for the requested node");

            return result;
        }

        public static void Trace(string message)
        {
            Console.WriteLine(message);
        }
    }
}