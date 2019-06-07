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
            WriteFieldData("NodeClass", NamespaceIndexAllTypes, new DataValue(new Variant(NodeClass.Object), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("Time", NamespaceIndexAllTypes, new DataValue(new Variant(DateTime.UtcNow.ToString("HH:mm")), StatusCodes.Good, DateTime.UtcNow));
            var euInformation = new EUInformation()
            {
                Description = "Sample EuInformation. Will change UnitId",
                DisplayName = new LocalizedText("Sample"),
                UnitId = 1
            };
            WriteFieldData("EUInformation", NamespaceIndexAllTypes, new DataValue(new ExtensionObject(euInformation), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("String", NamespaceIndexAllTypes, new DataValue(new Variant(""), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("StringOneDimension", NamespaceIndexAllTypes, new DataValue(new Variant(new string[1] { "" }), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("ByteString", NamespaceIndexAllTypes, new DataValue(new Variant(new byte[1]{0}), StatusCodes.Good, DateTime.UtcNow));
            WriteFieldData("ByteStringOneDimension", NamespaceIndexAllTypes, new DataValue(new Variant(new byte[1] { 0 }), StatusCodes.Good, DateTime.UtcNow));
            
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
        private static void IncrementValue(FieldMetaData variable, ushort namespaceIndex, long maxAllowedValue = Int32.MaxValue, int step = 0)
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
                    Int16 int16Value = Convert.ToInt16(dataValue.Value);
                    int intIdentifier = int16Value;
                    Interlocked.CompareExchange(ref intIdentifier, 0, Int16.MaxValue);
                    dataValue.Value = (Int16)Interlocked.Increment(ref intIdentifier);
                    isIncremented = true;
                    break;
                case BuiltInType.Int32:
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
                    isIncremented = true;
                    break;
                case BuiltInType.SByte:
                    SByte sbyteValue = Convert.ToSByte(dataValue.Value);
                    intIdentifier = sbyteValue;
                    Interlocked.CompareExchange(ref intIdentifier, 0, SByte.MaxValue);
                    dataValue.Value = (SByte)Interlocked.Increment(ref intIdentifier);
                    isIncremented = true;
                    break;
                case BuiltInType.UInt16:
                    UInt16 uint16Value = Convert.ToUInt16(dataValue.Value);
                    intIdentifier = uint16Value;
                    Interlocked.CompareExchange(ref intIdentifier, 0, UInt16.MaxValue);
                    dataValue.Value = (UInt16)Interlocked.Increment(ref intIdentifier);
                    isIncremented = true;
                    break;
                case BuiltInType.UInt32:
                    UInt32 uint32Value = Convert.ToUInt32(dataValue.Value);
                    long longIdentifier = uint32Value;
                    Interlocked.CompareExchange(ref longIdentifier, 0, UInt32.MaxValue);
                    dataValue.Value = (UInt32)Interlocked.Increment(ref longIdentifier);
                    isIncremented = true;
                    break;
                case BuiltInType.Float:
                    float floatValue = Convert.ToSingle(dataValue.Value);
                    Interlocked.CompareExchange(ref floatValue, 0, float.MaxValue);
                    dataValue.Value = ++floatValue;
                    isIncremented = true;
                    break;
                case BuiltInType.Double:
                    double doubleValue = Convert.ToDouble(dataValue.Value);
                    Interlocked.CompareExchange(ref doubleValue, 0, double.MaxValue);
                    dataValue.Value = ++doubleValue;
                    isIncremented = true;
                    break;
                case BuiltInType.DateTime:
                    dataValue.Value = DateTime.UtcNow;
                    isIncremented = true;
                    break;
                case (BuiltInType) DataTypes.Time:
                    dataValue.Value = DateTime.UtcNow.ToString("HH:mm");
                    isIncremented = true;
                    break;
                case (BuiltInType) DataTypes.NodeClass:
                    uint value = (uint)((NodeClass)dataValue.Value);
                    dataValue.Value = value == 0? NodeClass.Object: (value == 128 ? NodeClass.Unspecified : (NodeClass)(value * 2));
                    isIncremented = true;
                    break;
                case (BuiltInType)DataTypes.EUInformation:
                    var extensionObject = (ExtensionObject)dataValue.Value;
                    var euInformation = extensionObject.Body as EUInformation;
                    if (euInformation!= null)
                    {
                        euInformation.UnitId = euInformation.UnitId + 1;
                    }
                    isIncremented = true;
                    break;
                case BuiltInType.String:
                    switch (variable.ValueRank)
                    {
                        case ValueRanks.Scalar:
                            dataValue.Value = "Hello World";
                            break;
                        case ValueRanks.OneDimension:
                            dataValue.Value = new string[1] {"One dimension sample!"};
                            break;
                        case ValueRanks.TwoDimensions:
                            dataValue.Value = new string[,] { {"Two dimension 1 sample!", "Two dimension 2 sample!"} };
                            break;
                        case ValueRanks.OneOrMoreDimensions:
                            dataValue.Value = new string[2][,]
                            {
                                new string[,] {{"first string", "second string"}},
                                new string[,] {{"third string", "forth string"}}
                            };
                            break;
                    }

                    isIncremented = true;
                    break;
                case BuiltInType.ByteString:
                    switch (variable.ValueRank)
                    {
                        case ValueRanks.Scalar:
                            dataValue.Value = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
                            break;
                        case ValueRanks.OneDimension:
                            dataValue.Value = new byte[1][]
                            {
                                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                            };
                            break;
                        case ValueRanks.TwoDimensions:
                            dataValue.Value = new byte[,]
                            {
                                {1,1}, {2,2}, {3,3}, {4,4}, {5,5}, {6,6}, {7,7}, {8,8}, {9,9}
                            };
                            break;
                        case ValueRanks.OneOrMoreDimensions:
                            dataValue.Value = new byte[2][,]
                            {
                                new byte[,] { { 2, 2 }, { 3, 3 }, { 4, 4 }, { 5, 5 }, { 6, 6 }, { 7, 7 }, { 8, 8 }, { 9, 9 } },
                                new byte[,] { { 2, 2 }, { 3, 3 }, { 4, 4 }, { 5, 5 }, { 6, 6 }, { 7, 7 }, { 8, 8 }, { 9, 9 } },
                            };
                            break;
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
