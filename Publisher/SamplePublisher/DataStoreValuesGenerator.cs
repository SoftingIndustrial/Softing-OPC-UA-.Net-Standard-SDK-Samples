/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using Opc.Ua;
using Softing.Opc.Ua.PubSub;
using System;
using System.Threading;

namespace SamplePublisher
{
    /// <summary>
    /// Initialize and change datastore data 
    /// </summary>
    public class DataStoreValuesGenerator : IDisposable
    {
        #region Fields
        // It should match the namespace index from configuration file
        public const ushort NamespaceIndexSimple = 2;
        public const ushort NamespaceIndexAllTypes = 3;
        public const ushort NamespaceIndexMassTest = 4;

        private const string DataSetNameSimple = "Simple";
        private const string DataSetNameAllTypes = "AllTypes";
        private const string DataSetNameMassTest = "MassTest";

        // simulate for BoolToogle changes to 3 seconds
        private int m_boolToogleCount = 0;
        private const int BoolToogleLimit = 2;
        private const int SimpleInt32Limit = 10000;
       
        private FieldMetaDataCollection m_simpleFields = new FieldMetaDataCollection();
        private FieldMetaDataCollection m_allTypesFields = new FieldMetaDataCollection();
        private FieldMetaDataCollection m_massTestFields = new FieldMetaDataCollection();      
        
        private PublishedDataSetDataTypeCollection m_publishedDataSets;
        private UaPubSubDataStore m_dataStore;
        private Timer m_updateValuesTimer;

        private object m_lock = new object();

        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pubSubApplication"></param>
        public DataStoreValuesGenerator(UaPubSubApplication uaPubSubApplication)
        {
            //m_publishedDataSets = uaPubSubApplication.PubSubConfiguration.PublishedDataSets;
            m_publishedDataSets = uaPubSubApplication.GetPublisherDataSets();
            m_dataStore = uaPubSubApplication.DataStore;
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
            if (m_publishedDataSets != null)
            {
                // Remember the fields to be updated 
                foreach (var publishedDataSet in m_publishedDataSets)
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

            try
            {
                LoadInitialData();
            }
            catch (Exception e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "SamplePublisher.DataStoreValuesGenerator.LoadInitialData wrong field: {0}", e.StackTrace);
            }

            m_updateValuesTimer = new Timer(UpdateValues, null, 1000, 1000);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load initial demo data
        /// </summary>
        private void LoadInitialData()
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
            WriteFieldData("Float", NamespaceIndexAllTypes, new DataValue(new Variant((float)0F), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Double", NamespaceIndexAllTypes, new DataValue(new Variant((double)0.0), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("NodeClass", NamespaceIndexAllTypes, new DataValue(new Variant(NodeClass.Object), StatusCodes.Good, DateTime.UtcNow));
            var euInformation = new EUInformation()
            {
                Description = "Sample EuInformation. Will change UnitId",
                DisplayName = new LocalizedText("Sample"),
                UnitId = 1
            };
            WriteFieldData("EUInformation", NamespaceIndexAllTypes, new DataValue(new ExtensionObject(euInformation), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Time", NamespaceIndexAllTypes, new DataValue(new Variant(DateTime.UtcNow.ToString("HH:mm")), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("String", NamespaceIndexAllTypes, new DataValue(new Variant(""), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("ByteString", NamespaceIndexAllTypes, new DataValue(new Variant(new byte[1] { 0 }), StatusCodes.Good, DateTime.UtcNow));

            WriteFieldData("BoolToggleArray", NamespaceIndexAllTypes, new DataValue(new Variant(new bool[] { true, true, true }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("ByteArray", NamespaceIndexAllTypes, new DataValue(new Variant(new byte[] { 0, 0, 0 }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Int16Array", NamespaceIndexAllTypes, new DataValue(new Variant(new Int16[] { 0, 0, 0 }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Int32Array", NamespaceIndexAllTypes, new DataValue(new Variant(new Int32[] { 0, 0, 0 }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("SByteArray", NamespaceIndexAllTypes, new DataValue(new Variant(new sbyte[] { 0, 0, 0 }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("UInt16Array", NamespaceIndexAllTypes, new DataValue(new Variant(new UInt16[] { 0, 0, 0 }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("UInt32Array", NamespaceIndexAllTypes, new DataValue(new Variant(new UInt32[] { 0, 0, 0 }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("FloatArray", NamespaceIndexAllTypes, new DataValue(new Variant(new float[] { 0F, 0F, 0F }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("DoubleArray", NamespaceIndexAllTypes, new DataValue(new Variant(new double[] { 0.0, 0.0, 0.0 }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("NodeClassArray", NamespaceIndexAllTypes, new DataValue(new Variant[]{new Variant(NodeClass.Object),
                            new Variant(NodeClass.Object),
                            new Variant(NodeClass.Object)},
                            StatusCodes.Good, DateTime.UtcNow));

            WriteFieldData("TimeArray", NamespaceIndexAllTypes, new DataValue(new Variant(new string[] {DateTime.UtcNow.ToString("HH:mm"), DateTime.UtcNow.ToString("HH:mm"), DateTime.UtcNow.ToString("HH:mm") }), StatusCodes.Good, DateTime.UtcNow));
            var euInformation1 = new EUInformation()
            {
                Description = "Sample EuInformation1. Will change UnitId",
                DisplayName = new LocalizedText("Sample"),
                UnitId = 1
            };
            var euInformation2 = new EUInformation()
            {
                Description = "Sample EuInformation2. Will change UnitId",
                DisplayName = new LocalizedText("Sample"),
                UnitId = 1
            };
            var euInformation3 = new EUInformation()
            {
                Description = "Sample EuInformation3. Will change UnitId",
                DisplayName = new LocalizedText("Sample"),
                UnitId = 1
            };
            ExtensionObject[] euInfArray = new ExtensionObject[] { new ExtensionObject(euInformation1), new ExtensionObject(euInformation2), new ExtensionObject(euInformation3) };
            WriteFieldData("EUInformationArray", NamespaceIndexAllTypes, new DataValue(euInfArray, StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("StringArray", NamespaceIndexAllTypes, new DataValue(new Variant(new string[] {"" , "", "" }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("ByteStringArray", NamespaceIndexAllTypes, new DataValue(new Variant(new byte[][]{ new byte[1] {0}, new byte[1] { 0 }, new byte[1] { 0 } }), StatusCodes.Good, DateTime.UtcNow));
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
        private DataValue ReadFieldData(string metaDatafieldName, ushort namespaceIndex)
        {
            return m_dataStore.ReadPublishedDataItem(new NodeId(metaDatafieldName, namespaceIndex), Attributes.Value);
        }

        /// <summary>
        /// Write (update) field data
        /// </summary>
        /// <param name="metaDatafieldName"></param>
        /// <param name="dataValue"></param>
        private void WriteFieldData(string metaDatafieldName, ushort namespaceIndex, DataValue dataValue)
        {
            m_dataStore.WritePublishedDataItem(new NodeId(metaDatafieldName, namespaceIndex), Attributes.Value, dataValue);
        }

        /// <summary>
        /// Simulate value changes in dynamic nodes
        /// </summary>
        /// <param name="state"></param>
        private void UpdateValues(object state)
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
                                IncrementValue(variable, NamespaceIndexSimple,  SimpleInt32Limit);                               
                                break;
                            case "Int32Fast":                                
                                IncrementValue(variable, NamespaceIndexSimple, SimpleInt32Limit, 100);
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
                        IncrementValue(variable, NamespaceIndexMassTest, Int32.MaxValue);
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
        /// maxAllowedValue - maximum incremented value before reset value to beginning
        /// step - the increment amount  
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="maxAllowedValue"></param>
        /// <param name="step"></param>
        private void IncrementValue(FieldMetaData variable, ushort namespaceIndex, long maxAllowedValue = Int32.MaxValue, int step = 0)
        {
            // Read value to be incremented
            DataValue dataValue = ReadFieldData(variable.Name, namespaceIndex);
            if (dataValue.Value == null)
            {
                return;
            }

            bool isIncremented = false;

            BuiltInType expectedType = TypeInfo.GetBuiltInType(variable.DataType);
            switch (expectedType)
            {
                case BuiltInType.Boolean:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        Boolean boolValue = Convert.ToBoolean(dataValue.Value);
                        dataValue.Value = !boolValue;
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        bool[] valueArray = (bool[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            valueArray[i] = !valueArray[i];
                        }
                        dataValue.Value = valueArray;
                    }
                    isIncremented = true;
                    break;
                case BuiltInType.Byte:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        Byte byteValue = Convert.ToByte(dataValue.Value);
                        dataValue.Value = ++byteValue;
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        byte[] valueArray = (byte[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            valueArray[i] = ++valueArray[i];
                        }
                        dataValue.Value = valueArray;
                    }
                    isIncremented = true;
                    break;
                case BuiltInType.Int16:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        Int16 int16Value = Convert.ToInt16(dataValue.Value);
                        int intIdentifier = int16Value;
                        Interlocked.CompareExchange(ref intIdentifier, 0, Int16.MaxValue);
                        dataValue.Value = (Int16)Interlocked.Increment(ref intIdentifier);
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        Int16[] valueArray = (Int16[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            int intIdentifier = valueArray[i];
                            Interlocked.CompareExchange(ref intIdentifier, 0, Int16.MaxValue);
                            valueArray[i] = (Int16)Interlocked.Increment(ref intIdentifier);
                        }
                        dataValue.Value = valueArray;
                    }
                    
                    isIncremented = true;
                    break;
                case BuiltInType.Int32:

                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        Int32 int32Value = Convert.ToInt32(dataValue.Value);
                        if (step > 0)
                        {
                            int32Value += (step - 1);
                        }
                        if (int32Value > maxAllowedValue)
                        {
                            int32Value = 0;
                        }
                        dataValue.Value = Interlocked.Increment(ref int32Value);

                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        Int32[] valueArray = (Int32[])dataValue.Value;
                        for(int i = 0; i < valueArray.Length; i++)
                        {
                            if (step > 0)
                            {
                                valueArray[i] += (step - 1);
                            }
                            if (valueArray[i] > maxAllowedValue)
                            {
                                valueArray[i] = 0;
                            }
                            valueArray[i] = Interlocked.Increment(ref valueArray[i]);
                        }
                        dataValue.Value = valueArray;
                    }
                    isIncremented = true;

                    break;
                case BuiltInType.SByte:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        SByte sbyteValue = Convert.ToSByte(dataValue.Value);
                        int intIdentifier = sbyteValue;
                        Interlocked.CompareExchange(ref intIdentifier, 0, SByte.MaxValue);
                        dataValue.Value = (SByte)Interlocked.Increment(ref intIdentifier);
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        SByte[] valueArray = (SByte[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            int intIdentifier = valueArray[i];
                            Interlocked.CompareExchange(ref intIdentifier, 0, SByte.MaxValue);
                            valueArray[i] = (SByte)Interlocked.Increment(ref intIdentifier);
                        }
                        dataValue.Value = valueArray;
                    }
                    
                    isIncremented = true;
                    break;
                case BuiltInType.UInt16:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        UInt16 uint16Value = Convert.ToUInt16(dataValue.Value);
                        int intIdentifier = uint16Value;
                        Interlocked.CompareExchange(ref intIdentifier, 0, UInt16.MaxValue);
                        dataValue.Value = (UInt16)Interlocked.Increment(ref intIdentifier);
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        UInt16[] valueArray = (UInt16[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            int intIdentifier = valueArray[i];
                            Interlocked.CompareExchange(ref intIdentifier, 0, UInt16.MaxValue);
                            valueArray[i] = (UInt16)Interlocked.Increment(ref intIdentifier);
                        }
                        dataValue.Value = valueArray;
                    }
                    
                    isIncremented = true;
                    break;
                case BuiltInType.UInt32:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        UInt32 uint32Value = Convert.ToUInt32(dataValue.Value);
                        long longIdentifier = uint32Value;
                        Interlocked.CompareExchange(ref longIdentifier, 0, UInt32.MaxValue);
                        dataValue.Value = (UInt32)Interlocked.Increment(ref longIdentifier);
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        UInt32[] valueArray = (UInt32[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            long intIdentifier = valueArray[i];
                            Interlocked.CompareExchange(ref intIdentifier, 0, UInt32.MaxValue);
                            valueArray[i] = (UInt32)Interlocked.Increment(ref intIdentifier);
                        }
                        dataValue.Value = valueArray;
                    }
                   
                    isIncremented = true;
                    break;
                case BuiltInType.Float:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        float floatValue = Convert.ToSingle(dataValue.Value);
                        Interlocked.CompareExchange(ref floatValue, 0, float.MaxValue);
                        dataValue.Value = ++floatValue;
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        float[] valueArray = (float[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            float floatValue = valueArray[i];
                            Interlocked.CompareExchange(ref floatValue, 0, float.MaxValue);
                            valueArray[i] = ++floatValue;
                        }
                        dataValue.Value = valueArray;
                    }
                    
                    isIncremented = true;
                    break;
                case BuiltInType.Double:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        double doubleValue = Convert.ToDouble(dataValue.Value);
                        Interlocked.CompareExchange(ref doubleValue, 0, double.MaxValue);
                        dataValue.Value = ++doubleValue;
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        double[] valueArray = (double[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            double doubleValue = valueArray[i];
                            Interlocked.CompareExchange(ref doubleValue, 0, double.MaxValue);
                            valueArray[i] = ++doubleValue;
                        }
                        dataValue.Value = valueArray;
                    }
                   
                    isIncremented = true;
                    break;
                case BuiltInType.DateTime:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        dataValue.Value = DateTime.UtcNow;
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        DateTime[] valueArray = (DateTime[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            valueArray[i] = DateTime.UtcNow;
                        }
                        dataValue.Value = valueArray;
                    }
                    
                    isIncremented = true;
                    break;
                case (BuiltInType) DataTypes.Time:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        dataValue.Value = DateTime.UtcNow.ToString("HH:mm");
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        string[] valueArray = (string[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            valueArray[i] = DateTime.UtcNow.ToString("HH:mm");
                        }
                        dataValue.Value = valueArray;
                    }
                    
                    isIncremented = true;
                    break;
                case (BuiltInType) DataTypes.NodeClass:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        uint value = (uint)((NodeClass)dataValue.Value);
                        dataValue.Value = value == 0 ? NodeClass.Object : (value == 128 ? NodeClass.Unspecified : (NodeClass)(value * 2));
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        Variant[] varArray = (Variant[])dataValue.Value;
                        for (int i = 0; i < varArray.Length; i++)
                        {
                            NodeClass valNC = (NodeClass)varArray[i].Value;
                            valNC = valNC == 0 ? NodeClass.Object : ((int)valNC == 128 ? NodeClass.Unspecified : ((NodeClass)((int)valNC * 2)));
                            varArray[i].Value = valNC;
                        }
                        dataValue.Value = varArray;
                    }
                   
                    isIncremented = true;
                    break;
                case (BuiltInType)DataTypes.EUInformation:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        var extensionObject = (ExtensionObject)dataValue.Value;
                        if (extensionObject.Body is EUInformation euInformation)
                        {
                            euInformation.UnitId = euInformation.UnitId + 1;
                        }
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        ExtensionObject[] eoArray = (ExtensionObject[])dataValue.Value;
                        for (int i = 0; i < eoArray.Length; i++)
                        {
                            EUInformation euInformation = (EUInformation)eoArray[i].Body;
                            euInformation.UnitId = euInformation.UnitId + 1;
                            eoArray[i].Body = euInformation;
                        }
                        dataValue.Value = eoArray;
                    }
                   
                    isIncremented = true;
                    break;
                case BuiltInType.String:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        dataValue.Value = "Hello World stamped at: " + DateTime.Now.ToString();
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        string[] valueArray = (string[])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            valueArray[i] = "Hello World stamped at: " + DateTime.Now.ToString();
                        }
                        dataValue.Value = valueArray;
                    }
                    
                    isIncremented = true;
                    break;
                case BuiltInType.ByteString:
                    if (variable.ValueRank == ValueRanks.Scalar)
                    {
                        dataValue.Value = new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                    }
                    else if (variable.ValueRank == ValueRanks.OneDimension)
                    {
                        byte[][] valueArray = (byte[][])dataValue.Value;
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            valueArray[i] = new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                        }
                    }
                   
                    isIncremented = true;
                    break;
            }

            if (isIncremented)
            {
                // Save new incremented value to data store
                WriteFieldData(variable.Name, namespaceIndex, dataValue);
            }
        }

        #endregion
    }
}
