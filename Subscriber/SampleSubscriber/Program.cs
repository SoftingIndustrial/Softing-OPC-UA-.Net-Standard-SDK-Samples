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
using Softing.Opc.Ua.Private;
using Softing.Opc.Ua.PubSub;
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
        private static TraceConfiguration m_traceConfiguration;

        // It should match the namespace index from configuration file
        public const ushort NamespaceIndexSimple = 2;
        public const ushort NamespaceIndexAllTypes = 3;
        public const ushort NamespaceIndexMassTest = 4;
        private static object m_lock = new object();
        #endregion

        /// <summary>
        /// Entry point for application
        /// </summary>
        static void Main()
        {            
            try
            {
                LicensingStatus licensingStatus = LicensingStatus.Ok;
                // TODO - design time license activation
                // Fill in your design time license activation keys here Client or Server
                //licensingStatus = m_pubSubApplication.ActivateLicense(LicenseFeature.Server, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
                //licensingStatus = m_pubSubApplication.ActivateLicense(LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");

                if (licensingStatus == LicensingStatus.Expired)
                {
                    Console.WriteLine("License period expired!");
                    Console.ReadKey();
                    return;
                }
                if (licensingStatus == LicensingStatus.Invalid)
                {
                    Console.WriteLine("Invalid License key!");
                    Console.ReadKey();
                    return;
                }

                LoadTraceLogger();
                
                string configurationFileName = "SampleSubscriber.Config.xml";
                // Create the PubSub application
                using (UaPubSubApplication pubSubApplication = UaPubSubApplication.Create(configurationFileName))
                {
                    // subscribe to data events 
                    pubSubApplication.DataReceived += PubSubApplication_DataReceived;
                    
                    PrintCommandParameters();

                    //start application
                    pubSubApplication.Start();
                    Console.WriteLine("Subscriber started");
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
                            Console.WriteLine("Connections Status:");
                            foreach (var connection in pubSubApplication.PubSubConnections)
                            {
                                Console.WriteLine("\tConnection '{0}' - Running={1}, DataSetReadersCount={2}",
                                    connection.PubSubConnectionConfiguration.Name, connection.IsRunning);
                            }
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
                Console.WriteLine("Data Arrived, DataSet count = {0}", e.DataSets.Count);
                int index = 0;
                foreach (DataSet dataSet in e.DataSets)
                {
                    Console.WriteLine("\tDataSet {0}, Name = {1}, DataSetWriterId = {2}", index++, dataSet.Name, dataSet.DataSetWriterId);
                    for (int i = 0; i < dataSet.Fields.Length; i++)
                    {
                        Console.WriteLine("\t\tTargetNodeId: {0}, Attribute: {1}, Value: {2}",
                            dataSet.Fields[i].TargetNodeId, dataSet.Fields[i].TargetAttribute, dataSet.Fields[i].Value);
                        if (i > 10)
                        {
                            Console.WriteLine("\t\t... the rest of {0} elements are ommited.", dataSet.Fields.Length - i);
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
            pubSubConnection1.TransportProfileUri = "http://opcfoundation.org/UA-Profile/Transport/pubsub-udp-uadp";
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
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
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32Fast",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "DateTime",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.DateTime,
                        ValueRank = ValueRanks.Scalar
                    }
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
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Byte",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Byte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int16",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "SByte",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.SByte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt16",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.UInt16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt32",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.UInt32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Float",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Float,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Double",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
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
           
            readerGroup1.Enabled = true;
            readerGroup1.MaxNetworkMessageSize = 1500;       
            readerGroup1.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup1.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'Simple' for PublisherId = (UInt16)10, DataSetWriterId = 1
            DataSetReaderDataType dataSetReaderSimple = new DataSetReaderDataType();
            dataSetReaderSimple.PublisherId = (UInt16)10;
            dataSetReaderSimple.WriterGroupId = 0;
            dataSetReaderSimple.DataSetWriterId = 1;
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
            dataSetReaderAllTypes.PublisherId = (UInt16)10;
            dataSetReaderAllTypes.WriterGroupId = 0;
            dataSetReaderAllTypes.DataSetWriterId = 2;
            dataSetReaderAllTypes.Enabled = true;
            dataSetReaderAllTypes.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes.KeyFrameCount = 1;
            dataSetReaderAllTypes.DataSetMetaData = allTypesMetaData;

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 15,
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
            dataSetReaderMassTest.PublisherId = (UInt16)10;
            dataSetReaderMassTest.WriterGroupId = 0;
            dataSetReaderMassTest.DataSetWriterId = 3;
            dataSetReaderMassTest.Enabled = true;
            dataSetReaderMassTest.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderMassTest.KeyFrameCount = 1;
            dataSetReaderMassTest.DataSetMetaData = massTestMetaData;

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                DataSetOffset = 15,
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

            readerGroup2.Enabled = true;
            readerGroup2.MaxNetworkMessageSize = 1500;
            readerGroup2.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup2.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'Simple' for PublisherId = (UInt64)20, DataSetWriterId = 11
            DataSetReaderDataType dataSetReaderSimple2 = new DataSetReaderDataType();
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
                DataSetOffset = 15,
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
                DataSetOffset = 15,
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
                DataSetOffset = 15,
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
        #endregion

        #region Private Methods
        /// <summary>
        /// Print command line parameters for this console application
        /// </summary>
        private static void PrintCommandParameters()
        {
            Console.WriteLine("Press:\n\ts: connections status");
            Console.WriteLine("\tx,q: shutdown the Subscriber\n\n");
        }

        /// <summary>
        /// Load trace configuration for logging
        /// </summary>
        private static void LoadTraceLogger()
        {
            m_traceConfiguration = new TraceConfiguration();
            m_traceConfiguration.OutputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), SampleSubscriberLogFile);
            m_traceConfiguration.DeleteOnLoad = true;
            m_traceConfiguration.TraceMasks = 1;
            m_traceConfiguration.ApplySettings();
        }
        #endregion

    }
}
