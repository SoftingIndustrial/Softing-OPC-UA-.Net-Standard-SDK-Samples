/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using Opc.Ua;
using Softing.Opc.Ua.PubSub;
using Softing.Opc.Ua.PubSub.Configuration;
using Softing.Opc.Ua.PubSub.PublishedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SampleSubscriber
{
    static class Program
    {
        #region Fields
        private const string SampleSubscriberLogFile = "Softing/OpcUaNetStandardToolkit/logs/SampleSubscriber.log";
       
        // It should match the namespace index from configuration file
        public const ushort NamespaceIndexSimple = 2;
        public const ushort NamespaceIndexAllTypes = 3;
        public const ushort NamespaceIndexMassTest = 4;
        private const int MaximumNumberOfFieldsDisplayed = 20;
        private static object m_lock = new object();
        #endregion

        /// <summary>
        /// Entry point for application
        /// </summary>
        static void Main()
        {            
            try
            {
                LoadTraceLogger();

                string configurationFileName = "SampleSubscriber.Config.xml";

                string[] commandLineArguments = Environment.GetCommandLineArgs();
                if (commandLineArguments.Length > 1)
                {
                    if (File.Exists(commandLineArguments[1]))
                    {
                        configurationFileName = commandLineArguments[1];
                    }
                }

                //var config = CreateConfigurationAllDataTypes();
                //UaPubSubConfigurationHelper.SaveConfiguration(config, configurationFileName);

                // Create the PubSub application
                using (UaPubSubApplication uaPubSubApplication = UaPubSubApplication.Create(configurationFileName))
                {
                    #region Licensing
                    LicensingStatus pubSubLicensingStatus = LicensingStatus.Ok;

                    // TODO - design time license activation
                    // Fill in your design time license activation keys here Client or Server
                    //pubSubLicensingStatus = uaPubSubApplication.ActivateLicense(LicenseFeature.Server, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
                    //pubSubLicensingStatus = uaPubSubApplication.ActivateLicense(LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");

                    if (pubSubLicensingStatus == LicensingStatus.Expired)
                    {
                        Console.WriteLine("PubSub license period expired!");
                        Console.ReadKey();
                        return;
                    }
                    if (pubSubLicensingStatus == LicensingStatus.Invalid)
                    {
                        Console.WriteLine("Invalid PubSub license key!");
                        Console.ReadKey();
                        return;
                    }
                    #endregion
                    
                    // the PubSub application can be also created from an instance of PubSubConfigurationDataType
                    //PubSubConfigurationDataType pubSubConfiguration = CreateConfiguration();
                    //using (UaPubSubApplication uaPubSubApplication = UaPubSubApplication.Create(pubSubConfiguration)) {

                    // subscribe to data events 
                    uaPubSubApplication.DataReceived += PubSubApplication_DataReceived;
                    
                    PrintCommandParameters();

                    //start application
                    uaPubSubApplication.Start();

                    Console.WriteLine("SampleSubscriber started at:{0}", DateTime.Now.ToLongTimeString());
                    do
                    {
                        ConsoleKeyInfo key = Console.ReadKey();
                        if (key.KeyChar == 'q' || key.KeyChar == 'x')
                        {
                            Console.WriteLine("\nShutting down...");
                            break;
                        }
                        else if (key.KeyChar == 's')
                        {
                            // list connection status
                            DisplayConfigurationState(uaPubSubApplication.UaPubSubConfigurator);
                        }
                        else if (key.KeyChar == 'e')
                        {
                            // list connection status
                            EnableConfigurationObjectById(uaPubSubApplication.UaPubSubConfigurator);
                        }
                        else if (key.KeyChar == 'd')
                        {
                            // list connection status
                            DisableConfigurationObjectById(uaPubSubApplication.UaPubSubConfigurator);
                        }
                        else
                        {
                            PrintCommandParameters();
                        }
                    }
                    while (true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        #region Data Received Event Handler
        /// <summary>
        /// Handle <see cref="UaPubSubApplication.DataReceived"/> event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PubSubApplication_DataReceived(object sender, SubscribedDataEventArgs e)
        {
            lock (m_lock)
            {
                Console.WriteLine("Data Arrived from Source={0}, SequenceNumber={1}, DataSet count={2}", e.SourceEndPoint, e.NetworkMessageSequenceNumber, e.DataSets.Count);
                foreach (DataSet dataSet in e.DataSets)
                {
                    Console.WriteLine("\tDataSet.Name={0}, DataSetWriterId={1}", dataSet.Name, dataSet.DataSetWriterId);
                    for (int i = 0; i < dataSet.Fields.Length; i++)
                    {
                        Console.WriteLine("\t\tTargetNodeId:{0}, Attribute:{1}, Value:{2}",
                            dataSet.Fields[i].TargetNodeId, dataSet.Fields[i].TargetAttribute, dataSet.Fields[i].Value);
                        if (i > MaximumNumberOfFieldsDisplayed)
                        {
                            Console.WriteLine("\t\t... the rest of {0} elements are omitted.", dataSet.Fields.Length - i);
                            break;
                        }
                    }
                }
                Console.WriteLine("------------------------------------------------");
            }
        }
        #endregion

        #region Create configuration object
        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically
        /// </summary>
        /// <returns></returns>
        public static PubSubConfigurationDataType CreateConfiguration()
        {
            // Define a PubSub connection with PublisherId 10
            PubSubConnectionDataType pubSubConnection1 = new PubSubConnectionDataType();
            pubSubConnection1.Name = "UADPConection1";
            pubSubConnection1.Enabled = true;
            pubSubConnection1.PublisherId = (UInt16)10;
            pubSubConnection1.TransportProfileUri = UaPubSubApplication.UadpTransportProfileUri;
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            address.NetworkInterface = "Ethernet";
            address.Url = "opc.udp://239.0.0.1:4840";
            pubSubConnection1.Address = new ExtensionObject(address);
            
            #region  Define  'Simple' MetaData
            DataSetMetaDataType simpleMetaData = new DataSetMetaDataType();
            simpleMetaData.DataSetClassId = new Uuid(Guid.Empty);
            simpleMetaData.Name = "Simple";
            simpleMetaData.Fields = new FieldMetaDataCollection()
                {
                    new FieldMetaData()
                    {
                        Name = "BoolToggle",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Boolean,
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Int32,
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32Fast",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Int32,
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "DateTime",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.DateTime,
                        DataType = DataTypeIds.DateTime,
                        ValueRank = ValueRanks.Scalar
                    }, 
                };
            simpleMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = 1,
                MajorVersion = 1
            };
            #endregion

            #region Define 'AllTypes' Metadata
            DataSetMetaDataType allTypesMetaData = new DataSetMetaDataType();
            allTypesMetaData.DataSetClassId = new Uuid(Guid.Empty);
            allTypesMetaData.Name = "AllTypes";
            allTypesMetaData.Fields = new FieldMetaDataCollection()
            {
                 new FieldMetaData()
                    {
                        Name = "BoolToggle",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Boolean,
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Byte",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Byte,
                        DataType = DataTypeIds.Byte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int16",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Int16,
                        DataType = DataTypeIds.Int16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Int32,
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "SByte",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.SByte,
                        DataType = DataTypeIds.SByte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt16",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.UInt16,
                        DataType = DataTypeIds.UInt16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt32",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.UInt32,
                        DataType = DataTypeIds.UInt32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Float",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Float,
                        DataType = DataTypeIds.Float,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Double",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Double,
                        DataType = DataTypeIds.Double,
                        ValueRank = ValueRanks.Scalar
                    },

            };
            allTypesMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = 1,
                MajorVersion = 1
            };
            #endregion

            #region Define 'MassTest' MetaData
            DataSetMetaDataType massTestMetaData = new DataSetMetaDataType();
            massTestMetaData.DataSetClassId = new Uuid(Guid.Empty);
            massTestMetaData.Name = "MassData";
            massTestMetaData.Fields = new FieldMetaDataCollection();
            for (int i = 0; i < 100; i++)
            {
                string name = "Mass_" + i;
                massTestMetaData.Fields.Add(new FieldMetaData()
                {
                    Name = name,
                    DataSetFieldId = new Uuid(Guid.NewGuid()),
                    BuiltInType = (byte)DataTypes.UInt32,
                    DataType = DataTypeIds.UInt32,
                    ValueRank = ValueRanks.Scalar
                });              
            }
            massTestMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = 1,
                MajorVersion = 1
            };
            #endregion

            #region Define ReaderGroup1
            ReaderGroupDataType readerGroup1 = new ReaderGroupDataType();
            readerGroup1.Name = "ReaderGroup 1";
            readerGroup1.Enabled = true;
            readerGroup1.MaxNetworkMessageSize = 1500;       
            readerGroup1.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup1.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'Simple' for PublisherId = (UInt16)10, DataSetWriterId = 1
            DataSetReaderDataType dataSetReaderSimple = new DataSetReaderDataType();
            dataSetReaderSimple.Name = "Reader 1";
            dataSetReaderSimple.PublisherId = (UInt16)10;
            dataSetReaderSimple.WriterGroupId = 0;
            dataSetReaderSimple.DataSetWriterId = 0;
            dataSetReaderSimple.Enabled = true;
            dataSetReaderSimple.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderSimple.KeyFrameCount = 1;
            dataSetReaderSimple.DataSetMetaData = simpleMetaData;

            UadpDataSetReaderMessageDataType uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 15,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.GroupHeader
                        | UadpNetworkMessageContentMask.WriterGroupId | UadpNetworkMessageContentMask.GroupVersion
                        | UadpNetworkMessageContentMask.NetworkMessageNumber | UadpNetworkMessageContentMask.SequenceNumber),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderSimple.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            TargetVariablesDataType subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            foreach (var fieldMetaData in simpleMetaData.Fields)
            {
                subscribedDataSet.TargetVariables.Add(new FieldTargetDataType()
                {
                    DataSetFieldId = fieldMetaData.DataSetFieldId,
                    TargetNodeId = new NodeId(fieldMetaData.Name, NamespaceIndexSimple),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = new Variant(TypeInfo.GetDefaultValue(fieldMetaData.DataType, (int)ValueRanks.Scalar))
                });
            }           

            dataSetReaderSimple.SubscribedDataSet = new ExtensionObject(subscribedDataSet);
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderSimple);

            #region Define DataSetReader 'AllTypes' for PublisherId = (UInt16)10, DataSetWriterId = 2
            DataSetReaderDataType dataSetReaderAllTypes = new DataSetReaderDataType();
            dataSetReaderAllTypes.Name = "Reader 2";
            dataSetReaderAllTypes.PublisherId = (UInt16)10;
            dataSetReaderAllTypes.WriterGroupId = 0;
            dataSetReaderAllTypes.DataSetWriterId = 0;
            dataSetReaderAllTypes.Enabled = true;
            dataSetReaderAllTypes.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes.KeyFrameCount = 1;
            dataSetReaderAllTypes.DataSetMetaData = allTypesMetaData;

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 47,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.GroupHeader
                        | UadpNetworkMessageContentMask.WriterGroupId | UadpNetworkMessageContentMask.GroupVersion
                        | UadpNetworkMessageContentMask.NetworkMessageNumber | UadpNetworkMessageContentMask.SequenceNumber),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderAllTypes.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            foreach(var fieldMetaData in allTypesMetaData.Fields)
            {
                subscribedDataSet.TargetVariables.Add(new FieldTargetDataType()
                {
                    DataSetFieldId = fieldMetaData.DataSetFieldId,
                    TargetNodeId = new NodeId(fieldMetaData.Name, NamespaceIndexAllTypes),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = new Variant(TypeInfo.GetDefaultValue(fieldMetaData.DataType, (int)ValueRanks.Scalar))
                });
            }

            dataSetReaderAllTypes.SubscribedDataSet = new ExtensionObject(subscribedDataSet);
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderAllTypes);

            #region Define DataSetReader 'MassTest' for PublisherId = (UInt16)10, DataSetWriterId = 3
            DataSetReaderDataType dataSetReaderMassTest = new DataSetReaderDataType();
            dataSetReaderMassTest.Name = "Reader 3";
            dataSetReaderMassTest.PublisherId = (UInt16)10;
            dataSetReaderMassTest.WriterGroupId = 0;
            dataSetReaderMassTest.DataSetWriterId = 0;
            dataSetReaderMassTest.Enabled = true;
            dataSetReaderMassTest.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderMassTest.KeyFrameCount = 1;
            dataSetReaderMassTest.DataSetMetaData = massTestMetaData;

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 79,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.GroupHeader
                        | UadpNetworkMessageContentMask.WriterGroupId | UadpNetworkMessageContentMask.GroupVersion
                        | UadpNetworkMessageContentMask.NetworkMessageNumber | UadpNetworkMessageContentMask.SequenceNumber),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderMassTest.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            foreach (var fieldMetaData in massTestMetaData.Fields)
            {
                subscribedDataSet.TargetVariables.Add(new FieldTargetDataType()
                {
                    DataSetFieldId = fieldMetaData.DataSetFieldId,
                    TargetNodeId = new NodeId(fieldMetaData.Name, NamespaceIndexMassTest),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = new Variant(TypeInfo.GetDefaultValue(fieldMetaData.DataType, (int)ValueRanks.Scalar))
                });
            }

            dataSetReaderMassTest.SubscribedDataSet = new ExtensionObject(subscribedDataSet);
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderMassTest);
            #endregion
            pubSubConnection1.ReaderGroups.Add(readerGroup1);

            #region Define ReaderGroup2
            ReaderGroupDataType readerGroup2 = new ReaderGroupDataType();
            readerGroup2.Name = "ReaderGroup 2";
            readerGroup2.Enabled = true;
            readerGroup2.MaxNetworkMessageSize = 1500;
            readerGroup2.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup2.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'Simple' for PublisherId = (UInt64)20, DataSetWriterId = 11
            DataSetReaderDataType dataSetReaderSimple2 = new DataSetReaderDataType();
            dataSetReaderSimple2.Name = "Reader 11";
            dataSetReaderSimple2.PublisherId = (UInt64)20;
            dataSetReaderSimple2.WriterGroupId = 0;
            dataSetReaderSimple2.DataSetWriterId = 11;
            dataSetReaderSimple2.Enabled = true;
            dataSetReaderSimple2.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderSimple2.KeyFrameCount = 1;
            dataSetReaderSimple2.DataSetMetaData = simpleMetaData;

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 0,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.PayloadHeader),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
                        | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderSimple2.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            foreach (var fieldMetaData in simpleMetaData.Fields)
            {
                subscribedDataSet.TargetVariables.Add(new FieldTargetDataType()
                {
                    DataSetFieldId = fieldMetaData.DataSetFieldId,
                    TargetNodeId = new NodeId(fieldMetaData.Name, NamespaceIndexSimple),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = new Variant(TypeInfo.GetDefaultValue(fieldMetaData.DataType, (int)ValueRanks.Scalar))
                });
            }

            dataSetReaderSimple2.SubscribedDataSet = new ExtensionObject(subscribedDataSet);
            #endregion
            readerGroup2.DataSetReaders.Add(dataSetReaderSimple2);

            #region Define DataSetReader 'AllTypes' for PublisherId = (UInt64)20, DataSetWriterId = 12
            DataSetReaderDataType dataSetReaderAllTypes2 = new DataSetReaderDataType();
            dataSetReaderAllTypes2.Name = "Reader 12";
            dataSetReaderAllTypes2.PublisherId = (UInt64)20;
            dataSetReaderAllTypes2.WriterGroupId = 0;
            dataSetReaderAllTypes2.DataSetWriterId = 12;
            dataSetReaderAllTypes2.Enabled = true;
            dataSetReaderAllTypes2.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes2.KeyFrameCount = 1;
            dataSetReaderAllTypes2.DataSetMetaData = allTypesMetaData;

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 0,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.PayloadHeader),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
                        | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderAllTypes2.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            foreach (var fieldMetaData in allTypesMetaData.Fields)
            {
                subscribedDataSet.TargetVariables.Add(new FieldTargetDataType()
                {
                    DataSetFieldId = fieldMetaData.DataSetFieldId,
                    TargetNodeId = new NodeId(fieldMetaData.Name, NamespaceIndexAllTypes),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = new Variant(TypeInfo.GetDefaultValue(fieldMetaData.DataType, (int)ValueRanks.Scalar))
                });
            }

            dataSetReaderAllTypes2.SubscribedDataSet = new ExtensionObject(subscribedDataSet);
            #endregion
            readerGroup2.DataSetReaders.Add(dataSetReaderAllTypes2);

            #region Define DataSetReader 'MassTest' for PublisherId = (UInt64)20,  DataSetWriterId = 13
            DataSetReaderDataType dataSetReaderMassTest2 = new DataSetReaderDataType();
            dataSetReaderMassTest2.Name = "Reader 13";
            dataSetReaderMassTest2.PublisherId = (UInt64)20;
            dataSetReaderMassTest2.WriterGroupId = 0;
            dataSetReaderMassTest2.DataSetWriterId = 13;
            dataSetReaderMassTest2.Enabled = true;
            dataSetReaderMassTest2.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderMassTest2.KeyFrameCount = 1;
            dataSetReaderMassTest2.DataSetMetaData = massTestMetaData;

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 0,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.PayloadHeader),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
                        | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderMassTest2.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            foreach (var fieldMetaData in massTestMetaData.Fields)
            {
                subscribedDataSet.TargetVariables.Add(new FieldTargetDataType()
                {
                    DataSetFieldId = fieldMetaData.DataSetFieldId,
                    TargetNodeId = new NodeId(fieldMetaData.Name, NamespaceIndexMassTest),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = new Variant(TypeInfo.GetDefaultValue(fieldMetaData.DataType, (int)ValueRanks.Scalar))
                });
            }

            dataSetReaderMassTest2.SubscribedDataSet = new ExtensionObject(subscribedDataSet);
            #endregion
            readerGroup2.DataSetReaders.Add(dataSetReaderMassTest2);
            #endregion
            pubSubConnection1.ReaderGroups.Add(readerGroup2);
            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };           

            return pubSubConfiguration;
        }

        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically for all data types
        /// </summary>
        /// <returns></returns>
        public static PubSubConfigurationDataType CreateConfigurationAllDataTypes()
        {
            // Define a PubSub connection with PublisherId 10
            PubSubConnectionDataType pubSubConnection1 = new PubSubConnectionDataType();
            pubSubConnection1.Name = "UADPConection1";
            pubSubConnection1.Enabled = true;
            pubSubConnection1.PublisherId = (UInt16)11;
            pubSubConnection1.TransportProfileUri = UaPubSubApplication.UadpTransportProfileUri;
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            address.NetworkInterface = "Ethernet";
            address.Url = "opc.udp://239.0.0.13:4840";
            pubSubConnection1.Address = new ExtensionObject(address);
            
            #region Define 'AllTypes' Metadata
            DataSetMetaDataType allTypesMetaData = new DataSetMetaDataType();
            allTypesMetaData.DataSetClassId = new Uuid(Guid.Empty);
            allTypesMetaData.Name = "AllTypes";
            allTypesMetaData.Fields = new FieldMetaDataCollection()
            {
                 new FieldMetaData()
                    {
                        Name = "BoolToggle",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Boolean,
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Byte",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Byte,
                        DataType = DataTypeIds.Byte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int16",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Int16,
                        DataType = DataTypeIds.Int16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Int32,
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "SByte",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.SByte,
                        DataType = DataTypeIds.SByte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt16",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.UInt16,
                        DataType = DataTypeIds.UInt16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt32",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.UInt32,
                        DataType = DataTypeIds.UInt32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Float",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Float,
                        DataType = DataTypeIds.Float,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Double",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte) DataTypes.Double,
                        DataType = DataTypeIds.Double,
                        ValueRank = ValueRanks.Scalar
                    },
                    //enumeration from Opc.Ua
                    new FieldMetaData()
                    {
                        Name = "NodeClass",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Enumeration,
                        DataType = DataTypeIds.NodeClass,
                        ValueRank = ValueRanks.Scalar
                    },
                    //structure data type from opc.ua
                    new FieldMetaData()
                    {
                        Name = "EUInformation",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Structure,
                        DataType = DataTypeIds.EUInformation,
                        ValueRank = ValueRanks.Scalar
                    },
                    //DataTypes derived from built-in types have
                    new FieldMetaData()
                    {
                        Name = "Time",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.String,
                        DataType = DataTypeIds.Time,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "String",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.String,
                        DataType = DataTypeIds.String,
                        ValueRank = ValueRanks.Scalar,
                        MaxStringLength = 5,
                    },
                    new FieldMetaData()
                    {
                        Name = "ByteString",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.ByteString,
                        DataType = DataTypeIds.ByteString,
                        ValueRank = ValueRanks.Scalar,
                        MaxStringLength = 3,
                    },
                    new FieldMetaData()
                    {
                        Name = "BoolToggleArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Boolean,
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "ByteArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Byte,
                        DataType = DataTypeIds.Byte,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "Int16Array",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Int16,
                        DataType = DataTypeIds.Int16,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32Array",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Int32,
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "SByteArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.SByte,
                        DataType = DataTypeIds.SByte,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt16Array",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.UInt16,
                        DataType = DataTypeIds.UInt16,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt32Array",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.UInt32,
                        DataType = DataTypeIds.UInt32,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "FloatArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Float,
                        DataType = DataTypeIds.Float,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "DoubleArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Double,
                        DataType = DataTypeIds.Double,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "NodeClassArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Enumeration,
                        DataType = DataTypeIds.NodeClass,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "TimeArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.String,
                        DataType = DataTypeIds.Time,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "EUInformationArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.Structure,
                        DataType = DataTypeIds.EUInformation,
                        ValueRank = ValueRanks.OneDimension
                    },
                    new FieldMetaData()
                    {
                        Name = "StringArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.String,
                        DataType = DataTypeIds.String,
                        ValueRank = ValueRanks.OneDimension,
                        MaxStringLength = 14
                    },
                    new FieldMetaData()
                    {
                        Name = "ByteStringArray",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        BuiltInType = (byte)DataTypes.ByteString,
                        DataType = DataTypeIds.ByteString,
                        ValueRank = ValueRanks.OneDimension,
                        MaxStringLength = 5,
                    }
            };
            allTypesMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = 1,
                MajorVersion = 1
            };
            #endregion

            #region Define ReaderGroup1
            ReaderGroupDataType readerGroup1 = new ReaderGroupDataType();
            readerGroup1.Name = "ReaderGroup1";
            readerGroup1.Enabled = true;
            readerGroup1.MaxNetworkMessageSize = 1500;
            readerGroup1.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup1.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'Simple' for PublisherId = (UInt16)11, DataSetWriterId = 1
            DataSetReaderDataType dataSetReaderSimple = new DataSetReaderDataType();
            dataSetReaderSimple.Name = "Reader Simple";
            dataSetReaderSimple.PublisherId = (UInt16)11;
            dataSetReaderSimple.WriterGroupId = 0;
            dataSetReaderSimple.DataSetWriterId = 0;
            dataSetReaderSimple.Enabled = true;
            dataSetReaderSimple.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderSimple.KeyFrameCount = 1;
            dataSetReaderSimple.DataSetMetaData = allTypesMetaData;

            UadpDataSetReaderMessageDataType uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 15,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.GroupHeader
                        | UadpNetworkMessageContentMask.WriterGroupId | UadpNetworkMessageContentMask.GroupVersion
                        | UadpNetworkMessageContentMask.NetworkMessageNumber | UadpNetworkMessageContentMask.SequenceNumber),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderSimple.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            TargetVariablesDataType subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            foreach (var fieldMetaData in allTypesMetaData.Fields)
            {
                subscribedDataSet.TargetVariables.Add(new FieldTargetDataType()
                {
                    DataSetFieldId = fieldMetaData.DataSetFieldId,
                    TargetNodeId = new NodeId(fieldMetaData.Name, NamespaceIndexAllTypes),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = new Variant(TypeInfo.GetDefaultValue(fieldMetaData.DataType, (int)ValueRanks.Scalar))
                });
            }

            dataSetReaderSimple.SubscribedDataSet = new ExtensionObject(subscribedDataSet);
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderSimple);
           
            #endregion
            pubSubConnection1.ReaderGroups.Add(readerGroup1);

            #region Define ReaderGroup2
            ReaderGroupDataType readerGroup2 = new ReaderGroupDataType();
            readerGroup2.Name = "ReaderGroup 2";
            readerGroup2.Enabled = true;
            readerGroup2.MaxNetworkMessageSize = 1500;
            readerGroup2.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup2.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'AllTypes' for PublisherId = (UInt64)21, DataSetWriterId = 11
            DataSetReaderDataType dataSetReaderSimple2 = new DataSetReaderDataType();
            dataSetReaderSimple2.Name = "Reader Simple";
            dataSetReaderSimple2.PublisherId = (UInt64)21;
            dataSetReaderSimple2.WriterGroupId = 0;
            dataSetReaderSimple2.DataSetWriterId = 11;
            dataSetReaderSimple2.Enabled = true;
            dataSetReaderSimple2.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderSimple2.KeyFrameCount = 1;
            dataSetReaderSimple2.DataSetMetaData = allTypesMetaData;

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 0,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.PayloadHeader),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
                        | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderSimple2.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            foreach (var fieldMetaData in allTypesMetaData.Fields)
            {
                subscribedDataSet.TargetVariables.Add(new FieldTargetDataType()
                {
                    DataSetFieldId = fieldMetaData.DataSetFieldId,
                    TargetNodeId = new NodeId(fieldMetaData.Name, NamespaceIndexAllTypes),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = new Variant(TypeInfo.GetDefaultValue(fieldMetaData.DataType, (int)ValueRanks.Scalar))
                });
            }

            dataSetReaderSimple2.SubscribedDataSet = new ExtensionObject(subscribedDataSet);
            #endregion
            readerGroup2.DataSetReaders.Add(dataSetReaderSimple2);           
            #endregion
            pubSubConnection1.ReaderGroups.Add(readerGroup2);
            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };

            return pubSubConfiguration;
        }        
        #endregion

        #region Private Methods
        /// <summary>
        /// Print command line parameters for this console application
        /// </summary>
        private static void PrintCommandParameters()
        {
            Console.WriteLine("Press:\n\ts: display configuration status");
            Console.WriteLine("\te: enable configuration object specified by id");
            Console.WriteLine("\td: disable configuration object specified by id");
            Console.WriteLine("\tx,q: shutdown the Subscriber\n\n");
        }

        /// <summary>
        /// Handle Enable config method call from command line
        /// </summary>
        /// <param name="uaPubSubConfigurator"></param>
        private static void EnableConfigurationObjectById(UaPubSubConfigurator uaPubSubConfigurator)
        {
            DisplayConfigurationState(uaPubSubConfigurator);
            Console.Write("\nEnter the ConfigId of the object you want to enable:");
            string idStr = Console.ReadLine();
            uint id = 0;
            if (uint.TryParse(idStr, out id))
            {
                var configurationObject = uaPubSubConfigurator.FindObjectById(id);
                if (configurationObject != null)
                {
                    var result = uaPubSubConfigurator.Enable(configurationObject);
                    Console.WriteLine("\nThe Enable method returned code: {0}\n", result);
                    DisplayConfigurationState(uaPubSubConfigurator);
                    return;
                }
            }
            Console.WriteLine("\nCould not find the object with the specified id: {0}", idStr);
        }

        /// <summary>
        /// Handle Disable config method call from command line
        /// </summary>
        /// <param name="uaPubSubConfigurator"></param>
        private static void DisableConfigurationObjectById(UaPubSubConfigurator uaPubSubConfigurator)
        {
            DisplayConfigurationState(uaPubSubConfigurator);
            Console.Write("\nEnter the ConfigId of the object you want to disable:");
            string idStr = Console.ReadLine();
            uint id = 0;
            if (uint.TryParse(idStr, out id))
            {
                var configurationObject = uaPubSubConfigurator.FindObjectById(id);
                if (configurationObject != null)
                {
                    var result = uaPubSubConfigurator.Disable(configurationObject);
                    Console.WriteLine("\nThe Disable method returned code: {0}\n", result);
                    DisplayConfigurationState(uaPubSubConfigurator);
                    return;
                }
            }
            Console.WriteLine("\nCould not find the object with the specified id: {0}", idStr);
        }

        /// <summary>
        /// Display state for configured objects
        /// </summary>
        /// <param name="configurator"></param>
        private static void DisplayConfigurationState(UaPubSubConfigurator configurator)
        {
            object configurationObject = configurator.PubSubConfiguration;
            Console.WriteLine("\nConfiguration  \t\t\t\t-ConfigId={0}, State={1}",
                configurator.FindIdForObject(configurationObject), configurator.FindStateForObject(configurationObject));
            foreach (var connection in configurator.PubSubConfiguration.Connections)
            {
                Console.WriteLine("Connection '{0}'\t\t-ConfigId={1}, State={2}",
                    connection.Name, configurator.FindIdForObject(connection), configurator.FindStateForObject(connection));
                foreach (var writerGroup in connection.WriterGroups)
                {
                    Console.WriteLine("  WriterGroup Name ='{0}' WriterGroupId={1}\t-ConfigId={2}, State={3}",
                        writerGroup.Name, writerGroup.WriterGroupId, configurator.FindIdForObject(writerGroup), configurator.FindStateForObject(writerGroup));
                    foreach (var dataSetWriter in writerGroup.DataSetWriters)
                    {
                        Console.WriteLine("    DataSetWriter Name ='{0}' DataSetWriterId={1}\t-ConfigId={2}, State={3}",
                           dataSetWriter.Name, dataSetWriter.DataSetWriterId, configurator.FindIdForObject(dataSetWriter), configurator.FindStateForObject(dataSetWriter));
                    }
                }
                foreach (var readerGroup in connection.ReaderGroups)
                {
                    Console.WriteLine("  ReaderGroup Name ='{0}'\t-ConfigId={1}, State={2}",
                        readerGroup.Name, configurator.FindIdForObject(readerGroup), configurator.FindStateForObject(readerGroup));
                    foreach (var dataSetReader in readerGroup.DataSetReaders)
                    {
                        Console.WriteLine("    DataSetReader Name ='{0}'\t-ConfigId={1}, State={2}",
                            dataSetReader.Name, configurator.FindIdForObject(dataSetReader), configurator.FindStateForObject(dataSetReader));
                    }
                }
            }
        }
        /// <summary>
        /// Load trace configuration for logging
        /// </summary>
        private static void LoadTraceLogger()
        {
            TraceConfiguration traceConfiguration = new TraceConfiguration();
            traceConfiguration.OutputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), SampleSubscriberLogFile);
            traceConfiguration.DeleteOnLoad = true;
            traceConfiguration.TraceMasks = Utils.TraceMasks.All;
            traceConfiguration.ApplySettings();
        }
        #endregion
    }
}
