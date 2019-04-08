using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Opc.Ua;
using Softing.Opc.Ua.PubSub;

namespace SamplePublisher
{
    /// <summary>
    /// Initialize and change datastore data 
    /// </summary>
    public class DataStoreValuesGenerator : IDisposable
    {
        #region Fields

        // simulate for BoolToogle changes to 3 seconds
        private static int m_boolToogleCount = 0;
        private const int BoolToogleLimit = 2;
        private const int SimpleInt32Limit = 10000;

        private const string DataSetNameSimple = "Simple";
        private const string DataSetNameAllTypes = "AllTypes";
        private const string DataSetNameMassTest = "MassTest";

        private static FieldMetaDataCollection m_simpleFields = new FieldMetaDataCollection();
        private static FieldMetaDataCollection m_allTypesFields = new FieldMetaDataCollection();
        private static FieldMetaDataCollection m_massTestFields = new FieldMetaDataCollection();

        // It should match the namespace index from configuration file
        public const ushort NamespaceIndexSimple = 2;
        public const ushort NamespaceIndexAllTypes = 3;
        public const ushort NamespaceIndexMassTest = 4;

        private static UaPubSubApplication m_pubSubApplication;

        private Timer m_updateValuesTimer;

        private static object m_lock = new object();

        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pubSubApplication"></param>
        public DataStoreValuesGenerator(UaPubSubApplication pubSubApplication)
        {
            m_pubSubApplication = pubSubApplication;
        }
        #endregion

        #region IDisposable

        public void Dispose()
        {
            m_updateValuesTimer.Dispose();
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize PublisherData with information from configuration and start timer to update data 
        /// </summary>
        public void Start()
        {
            if (m_pubSubApplication != null)
            {
                // Remember the fields to be updated 
                foreach (var publishedDataSet in m_pubSubApplication.PubSubConfiguration.PublishedDataSets)
                {
                    switch (publishedDataSet.Name)
                    {
                        case DataSetNameSimple:
                            m_simpleFields.AddRange(publishedDataSet.DataSetMetaData.Fields);
                            break;
                        case DataSetNameAllTypes:
                            m_allTypesFields.AddRange(publishedDataSet.DataSetMetaData.Fields);
                            break;
                        case DataSetNameMassTest:
                            m_massTestFields.AddRange(publishedDataSet.DataSetMetaData.Fields);
                            break;
                    }
                }
            }

            LoadInitialData();

            m_updateValuesTimer = new Timer(UpdateValues, null, 1000, 1000);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load initial demo data
        /// </summary>
        private static void LoadInitialData()
        {
            #region DataSet 'Simple' fill with data
            WriteFieldData("BoolToggle", NamespaceIndexSimple, new DataValue(new Variant(false), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Int32", NamespaceIndexSimple, new DataValue(new Variant(0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Int32Fast", NamespaceIndexSimple, new DataValue(new Variant(0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("DateTime", NamespaceIndexSimple, new DataValue(new Variant(DateTime.UtcNow), StatusCodes.Good, DateTime.UtcNow));

            #endregion

            #region DataSet 'AllTypes' fill with data

            WriteFieldData("BoolToggle", NamespaceIndexAllTypes, new DataValue(new Variant(true), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Byte", NamespaceIndexAllTypes, new DataValue(new Variant((byte)0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Int16", NamespaceIndexAllTypes, new DataValue(new Variant((Int16)0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Int32", NamespaceIndexAllTypes, new DataValue(new Variant(0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("SByte", NamespaceIndexAllTypes, new DataValue(new Variant((sbyte)0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("UInt16", NamespaceIndexAllTypes, new DataValue(new Variant((UInt16)0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("UInt32", NamespaceIndexAllTypes, new DataValue(new Variant((UInt32)0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Float", NamespaceIndexAllTypes, new DataValue(new Variant((float)0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Double", NamespaceIndexAllTypes, new DataValue(new Variant((double)0), StatusCodes.Good, DateTime.UtcNow));

            #endregion

            #region DataSet 'MassTest' fill with data

            uint offset = 0;
            for (uint index = 0; index < 100; index++)
            {
                string massName = string.Format("Mass_{0}", index);
                WriteFieldData(massName, NamespaceIndexMassTest, new DataValue(new Variant((UInt32)offset), StatusCodes.Good, DateTime.UtcNow));
                offset += 100;
            }
            #endregion
        }

        /// <summary>
        /// Read field data
        /// </summary>
        /// <param name="metaDatafieldName"></param>
        /// <returns></returns>
        private static DataValue ReadFieldData(string metaDatafieldName, ushort namespaceIndex)
        {
            return m_pubSubApplication.DataStore.ReadPublishedDataItem(new NodeId(metaDatafieldName, namespaceIndex), Attributes.Value);
        }

        /// <summary>
        /// Write (update) field data
        /// </summary>
        /// <param name="metaDatafieldName"></param>
        /// <param name="dataValue"></param>
        private static void WriteFieldData(string metaDatafieldName, ushort namespaceIndex, DataValue dataValue)
        {
            m_pubSubApplication.DataStore.WritePublishedDataItem(new NodeId(metaDatafieldName, namespaceIndex), Attributes.Value, dataValue);
        }

        /// <summary>
        /// Simulate value changes in dynamic nodes
        /// </summary>
        /// <param name="state"></param>
        private static void UpdateValues(object state)
        {
            try
            {
                lock (m_lock)
                {
                    foreach (FieldMetaData variable in m_simpleFields)
                    {
                        switch (variable.Name)
                        {
                            case "BoolToggle":
                                m_boolToogleCount++;
                                if (m_boolToogleCount == BoolToogleLimit)
                                {
                                    m_boolToogleCount = 0;
                                    IncrementValue(variable, NamespaceIndexSimple);
                                }
                                break;
                            case "Int32":
                                DataValue int32DataValue = ReadFieldData(variable.Name, NamespaceIndexSimple);
                                Int32 int32Value = (Int32)int32DataValue.Value;
                                if (int32Value > SimpleInt32Limit)
                                {
                                    int32DataValue.Value = 0; 
                                    WriteFieldData(variable.Name, NamespaceIndexSimple, int32DataValue);
                                }
                                else
                                {
                                    IncrementValue(variable, NamespaceIndexSimple);
                                }
                                break;
                            case "Int32Fast":
                                DataValue int32FastDataValue = ReadFieldData(variable.Name, NamespaceIndexSimple);
                                Int32 int32FastValue = (Int32)int32FastDataValue.Value;
                                if (int32FastValue > SimpleInt32Limit)
                                {
                                    int32FastDataValue.Value = 0;
                                    WriteFieldData(variable.Name, NamespaceIndexSimple, int32FastDataValue);
                                }
                                IncrementValue(variable, NamespaceIndexSimple, 100);
                                break;
                            case "DateTime":
                                IncrementValue(variable, NamespaceIndexSimple);
                                break;
                        }
                    }

                    foreach (FieldMetaData variable in m_allTypesFields)
                    {
                        IncrementValue(variable, NamespaceIndexAllTypes);
                    }

                    foreach (FieldMetaData variable in m_massTestFields)
                    {
                        IncrementValue(variable, NamespaceIndexMassTest);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error doing simulation.");
            }
        }

        /// <summary>
        /// Increment value 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="namespaceIndex"></param>
        private static void IncrementValue(FieldMetaData variable, ushort namespaceIndex, int step = 0)
        {
            DataValue dataValue = ReadFieldData(variable.Name, namespaceIndex);
            if (dataValue.Value == null)
            {
                return;
            }

            bool isIncremented = false;

            BuiltInType expectedType = TypeInfo.GetBuiltInType(variable.DataType, null);
            switch (expectedType)
            {
                case BuiltInType.Boolean:
                    Boolean boolValue = Convert.ToBoolean(dataValue.Value);
                    dataValue.Value = !boolValue;
                    isIncremented = true;
                    break;
                case BuiltInType.Byte:
                    Byte byteValue = Convert.ToByte(dataValue.Value);
                    dataValue.Value = ++byteValue;
                    isIncremented = true;
                    break;
                case BuiltInType.Int16:
                    Int16 intValue = Convert.ToInt16(dataValue.Value);
                    if (intValue == Int16.MaxValue)
                    {
                        intValue = 0;
                    }
                    else
                    {
                        intValue++;
                    }
                    dataValue.Value = intValue;
                    isIncremented = true;
                    break;
                case BuiltInType.Int32:
                    Int32 int32Value = Convert.ToInt32(dataValue.Value);
                    if (int32Value == Int32.MaxValue)
                    {
                        Interlocked.CompareExchange(ref int32Value, 0, Int32.MaxValue);
                    }
                    else
                    {
                        if (step > 0)
                        {
                            int32Value += step;
                        }
                        else
                        {
                            int32Value = Interlocked.Increment(ref int32Value);
                        }
                    }
                    dataValue.Value = int32Value;
                    break;
                case BuiltInType.SByte:
                    SByte sbyteValue = Convert.ToSByte(dataValue.Value);
                    if (sbyteValue == SByte.MaxValue)
                    {
                        sbyteValue = 0;
                    }
                    else
                    {
                        sbyteValue++;
                    }
                    isIncremented = true;
                    dataValue.Value = sbyteValue;
                    break;
                case BuiltInType.UInt16:
                    UInt16 uint16Value = Convert.ToUInt16(dataValue.Value);
                    if (uint16Value == UInt16.MaxValue)
                    {
                        uint16Value = 0;
                    }
                    else
                    {
                        uint16Value++;
                    }
                    dataValue.Value = uint16Value;
                    isIncremented = true;
                    break;
                case BuiltInType.UInt32:
                    UInt32 uint32Value = Convert.ToUInt32(dataValue.Value);
                    if (uint32Value == UInt32.MaxValue)
                    {
                        uint32Value = 0;
                    }
                    else
                    {
                        uint32Value++;
                    }
                    dataValue.Value = uint32Value;
                    isIncremented = true;
                    break;
                case BuiltInType.Float:
                    float floatValue = Convert.ToSingle(dataValue.Value);
                    if (floatValue == float.MaxValue)
                    {
                        Interlocked.CompareExchange(ref floatValue, 0, float.MaxValue);
                    }
                    else
                    {
                        floatValue++;
                    }
                    dataValue.Value = floatValue;
                    isIncremented = true;
                    break;
                case BuiltInType.DataValue:
                    double doubleValue = Convert.ToDouble(dataValue.Value);
                    if (doubleValue == float.MaxValue)
                    {
                        Interlocked.CompareExchange(ref doubleValue, 0, double.MaxValue);
                    }
                    else
                    {
                        doubleValue++;
                    }
                    dataValue.Value = doubleValue;
                    isIncremented = true;
                    break;
                case BuiltInType.DateTime:
                    dataValue.Value = DateTime.UtcNow;
                    isIncremented = true;
                    break;
            }

            if (isIncremented)
            {
                WriteFieldData(variable.Name, namespaceIndex, dataValue);
            }
        }

        #endregion
    }
}
