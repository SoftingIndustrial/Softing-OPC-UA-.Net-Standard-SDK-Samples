/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System;
using System.IO;
using Opc.Ua;
using Softing.Opc.Ua.Private;
using Softing.Opc.Ua.PubSub;

namespace SamplePublisher
{
    public class Program
    {
        #region Fields
        private const string SamplePublisherLogFile = "Softing/OpcUaNetStandardToolkit/logs/SamplePublisher.log";
        private static TraceConfiguration m_traceConfiguration;

        private static FieldMetaDataCollection m_dynamicFields = new FieldMetaDataCollection();
        private static UaPubSubApplication m_pubSubApplication;
        #endregion

        /// <summary>
        /// Init and generate data for publishers
        /// </summary>
        private static DataStoreValuesGenerator m_dataStoreValuesGenerator;

        /// <summary>
        /// Entry point for application
        /// </summary>
        static void Main()
        {
            try
            {
                LoadTraceLogger();

                string configurationFileName = "SamplePublisher.Config.xml";
                // Create the PubSub application
                m_pubSubApplication = UaPubSubApplication.Create(configurationFileName);

                // the PubSub application can be also created from an instance of PubSubConfigurationDataType
                //PubSubConfigurationDataType pubSubConfiguration = CreateConfiguration();
                //m_pubSubApplication = UaPubSubApplication.Create(pubSubConfiguration);

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

                // Start publishing data 
                m_dataStoreValuesGenerator = new DataStoreValuesGenerator(m_pubSubApplication);
                m_dataStoreValuesGenerator.Start();

                Console.WriteLine("Publisher started");
                PrintCommandParameters();

                // start application
                m_pubSubApplication.Start();
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
                        foreach (var connection in m_pubSubApplication.PubSubConnections)
                        {
                            Console.WriteLine("\tConnection '{0}' - Running={1}", 
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
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadKey();
                Environment.Exit(-1);
            }
            finally
            {
                m_dataStoreValuesGenerator.Dispose();
                m_pubSubApplication.Dispose();
            }
        }

        
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

            #region Define WriterGroup1
            WriterGroupDataType writerGroup1 = new WriterGroupDataType();
            writerGroup1.Enabled = true;
            writerGroup1.WriterGroupId = 1;
            writerGroup1.PublishingInterval = 5000;
            writerGroup1.KeepAliveTime = 5000;
            writerGroup1.MaxNetworkMessageSize = 1500;
            writerGroup1.HeaderLayoutUri = "UADP-Cyclic-Fixed";
            UadpWriterGroupMessageDataType messageSettings = new UadpWriterGroupMessageDataType()
            {
                DataSetOrdering = DataSetOrderingType.AscendingWriterId,
                GroupVersion = 0,
                NetworkMessageContentMask = (uint) (UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.GroupHeader
                        | UadpNetworkMessageContentMask.WriterGroupId | UadpNetworkMessageContentMask.GroupVersion
                        | UadpNetworkMessageContentMask.NetworkMessageNumber | UadpNetworkMessageContentMask.SequenceNumber)
            };

            writerGroup1.MessageSettings = new ExtensionObject(messageSettings);
            writerGroup1.TransportSettings = new ExtensionObject(new DatagramWriterGroupTransportDataType());

            // Define DataSetWriter 'Simple'
            DataSetWriterDataType dataSetWriter1 = new DataSetWriterDataType();
            dataSetWriter1.DataSetWriterId = 1;
            dataSetWriter1.Enabled = true;
            dataSetWriter1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetWriter1.DataSetName = "Simple";
            dataSetWriter1.KeyFrameCount = 1;
            UadpDataSetWriterMessageDataType uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                ConfiguredSize = 22,
                DataSetOffset = 15, 
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetWriter1.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup1.DataSetWriters.Add(dataSetWriter1);

            // Define DataSetWriter 'AllTypes'
            DataSetWriterDataType dataSetWriter2 = new DataSetWriterDataType();
            dataSetWriter2.DataSetWriterId = 2;
            dataSetWriter2.Enabled = true;
            dataSetWriter2.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetWriter2.DataSetName = "AllTypes";
            dataSetWriter2.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                ConfiguredSize = 32,
                DataSetOffset = 37,
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetWriter2.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup1.DataSetWriters.Add(dataSetWriter2);

            // Define DataSetWriter 'MassTest'
            DataSetWriterDataType dataSetWriter3 = new DataSetWriterDataType();
            dataSetWriter3.DataSetWriterId = 3;
            dataSetWriter3.Enabled = true;
            dataSetWriter3.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetWriter3.DataSetName = "MassTest";
            dataSetWriter3.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                ConfiguredSize = 405,
                DataSetOffset = 69,
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetWriter3.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup1.DataSetWriters.Add(dataSetWriter3);

            pubSubConnection1.WriterGroups.Add(writerGroup1);
            #endregion

            // Define a PubSub connection with PublisherId 10
            PubSubConnectionDataType pubSubConnection2 = new PubSubConnectionDataType();
            pubSubConnection2.Name = "UADPConection2";
            pubSubConnection2.Enabled = true;
            pubSubConnection2.PublisherId = (UInt64)20;
            pubSubConnection2.TransportProfileUri = "http://opcfoundation.org/UA-Profile/Transport/pubsub-udp-uadp";
            address = new NetworkAddressUrlDataType();
            address.Url = "opc.udp://239.0.0.1:4840";
            pubSubConnection2.Address = new ExtensionObject(address);

            #region Define WriterGroup2
            WriterGroupDataType writerGroup2 = new WriterGroupDataType();
            writerGroup2.Enabled = true;
            writerGroup2.WriterGroupId = 2;
            writerGroup2.PublishingInterval = 5000;
            writerGroup2.KeepAliveTime = 5000;
            writerGroup2.MaxNetworkMessageSize = 1500;
            writerGroup2.HeaderLayoutUri = "UADP-Dynamic";
            messageSettings = new UadpWriterGroupMessageDataType()
            {
                DataSetOrdering = DataSetOrderingType.Undefined,
                GroupVersion = 0,
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.PayloadHeader)
            };

            writerGroup2.MessageSettings = new ExtensionObject(messageSettings);
            writerGroup2.TransportSettings = new ExtensionObject(new DatagramWriterGroupTransportDataType());

            // Define DataSetWriter 'Simple'
            DataSetWriterDataType dataSetWriter11 = new DataSetWriterDataType();
            dataSetWriter11.DataSetWriterId = 11;
            dataSetWriter11.Enabled = true;
            dataSetWriter11.DataSetFieldContentMask = (uint)DataSetFieldContentMask.None; //Variant encoding
            dataSetWriter11.DataSetName = "Simple";
            dataSetWriter11.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                //DataValue Encoding
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
                        | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetWriter11.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup2.DataSetWriters.Add(dataSetWriter11);

            // Define DataSetWriter 'AllTypes'
            DataSetWriterDataType dataSetWriter12 = new DataSetWriterDataType();
            dataSetWriter12.DataSetWriterId = 12;
            dataSetWriter12.Enabled = true;
            dataSetWriter12.DataSetFieldContentMask = (uint)DataSetFieldContentMask.None; //Variant encoding
            dataSetWriter12.DataSetName = "AllTypes";
            dataSetWriter12.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
                        | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetWriter12.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup2.DataSetWriters.Add(dataSetWriter12);

            // Define DataSetWriter 'MassTest'
            DataSetWriterDataType dataSetWriter13 = new DataSetWriterDataType();
            dataSetWriter13.DataSetWriterId = 13;
            dataSetWriter13.Enabled = true;
            dataSetWriter13.DataSetFieldContentMask = (uint)DataSetFieldContentMask.None; //Variant encoding
            dataSetWriter13.DataSetName = "MassTest";
            dataSetWriter13.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                //DataValue Encoding
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
                        | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetWriter13.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup2.DataSetWriters.Add(dataSetWriter13);

            pubSubConnection2.WriterGroups.Add(writerGroup2);
            #endregion            

            #region  Define PublishedDataSet Simple
            PublishedDataSetDataType publishedDataSetSimple = new PublishedDataSetDataType();
            publishedDataSetSimple.Name = "Simple"; //name shall be unique in a configuration
            // Define  publishedDataSetSimple.DataSetMetaData
            publishedDataSetSimple.DataSetMetaData = new DataSetMetaDataType();
            publishedDataSetSimple.DataSetMetaData.DataSetClassId = new Uuid(Guid.Empty);
            publishedDataSetSimple.DataSetMetaData.Name = publishedDataSetSimple.Name;
            publishedDataSetSimple.DataSetMetaData.Fields = new FieldMetaDataCollection()
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
            publishedDataSetSimple.DataSetMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = 1,
                MajorVersion = 1
            };

            //initialize Extension fields collection
            publishedDataSetSimple.ExtensionFields = new KeyValuePairCollection()
                {
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("BoolToggle"),
                        Value = true
                    },
                     new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("Int32"),
                        Value = (int)100
                    },
                     new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("Int32Fast"),
                        Value = (int)50
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("DateTime"),
                        Value = DateTime.Today
                    }
                };

            PublishedDataItemsDataType publishedDataSetSimpleSource = new PublishedDataItemsDataType();
            publishedDataSetSimpleSource.PublishedData = new PublishedVariableDataTypeCollection();
            //create PublishedData based on metadata names
            foreach (var field in publishedDataSetSimple.DataSetMetaData.Fields)
            {
                publishedDataSetSimpleSource.PublishedData.Add(
                    new PublishedVariableDataType()
                    {
                        PublishedVariable = new NodeId(field.Name, DataStoreValuesGenerator.NamespaceIndexSimple),
                        AttributeId = Attributes.Value,
                    });
            }

            publishedDataSetSimple.DataSetSource = new ExtensionObject(publishedDataSetSimpleSource);
            #endregion

            #region  Define PublishedDataSet AllTypes
            PublishedDataSetDataType publishedDataSetAllTypes = new PublishedDataSetDataType();
            publishedDataSetAllTypes.Name = "AllTypes"; //name shall be unique in a configuration
            // Define  publishedDataSetAllTypes.DataSetMetaData
            publishedDataSetAllTypes.DataSetMetaData = new DataSetMetaDataType();
            publishedDataSetAllTypes.DataSetMetaData.DataSetClassId = new Uuid(Guid.Empty);
            publishedDataSetAllTypes.DataSetMetaData.Name = publishedDataSetAllTypes.Name;
            publishedDataSetAllTypes.DataSetMetaData.Fields = new FieldMetaDataCollection()
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
            publishedDataSetAllTypes.DataSetMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = 1,
                MajorVersion = 1
            };
            PublishedDataItemsDataType publishedDataSetAllTypesSource = new PublishedDataItemsDataType();
            //create PublishedData based on metadata names
            foreach (var field in publishedDataSetAllTypes.DataSetMetaData.Fields)
            {
                publishedDataSetAllTypesSource.PublishedData.Add(
                    new PublishedVariableDataType()
                    {
                        PublishedVariable = new NodeId(field.Name, DataStoreValuesGenerator.NamespaceIndexAllTypes),
                        AttributeId = Attributes.Value,
                    });
            }
            publishedDataSetAllTypes.DataSetSource = new ExtensionObject(publishedDataSetAllTypesSource);
            #endregion

            #region  Define PublishedDataSet MassTest
            PublishedDataSetDataType publishedDataSetMassTest = new PublishedDataSetDataType();
            publishedDataSetMassTest.Name = "MassTest"; //name shall be unique in a configuration
            // Define  publishedDataSetMassTest.DataSetMetaData
            publishedDataSetMassTest.DataSetMetaData = new DataSetMetaDataType();
            publishedDataSetMassTest.DataSetMetaData.DataSetClassId = new Uuid(Guid.Empty);
            publishedDataSetMassTest.DataSetMetaData.Name = publishedDataSetMassTest.Name;
            publishedDataSetMassTest.DataSetMetaData.Fields = new FieldMetaDataCollection();
            publishedDataSetMassTest.DataSetMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = 1,
                MajorVersion = 1
            };
            //initialize Extension fields collection
            publishedDataSetMassTest.ExtensionFields = new KeyValuePairCollection();
            PublishedDataItemsDataType publishedDataSetTestDataSource = new PublishedDataItemsDataType();
            publishedDataSetTestDataSource.PublishedData = new PublishedVariableDataTypeCollection();
            for (int i = 0; i < 100; i++)
            {
                string name = "Mass_" + i;
                publishedDataSetMassTest.DataSetMetaData.Fields.Add(new FieldMetaData()
                {
                    Name = name,
                    DataSetFieldId = new Uuid(Guid.NewGuid()),
                    DataType = DataTypeIds.UInt32,
                    ValueRank = ValueRanks.Scalar
                });

                publishedDataSetTestDataSource.PublishedData.Add(new PublishedVariableDataType()
                {
                    PublishedVariable = new NodeId(name, DataStoreValuesGenerator.NamespaceIndexMassTest),
                    AttributeId = Attributes.Value,
                });
            }

            publishedDataSetMassTest.DataSetSource = new ExtensionObject(publishedDataSetTestDataSource);
            #endregion

            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1, pubSubConnection2
                };
            pubSubConfiguration.PublishedDataSets = new PublishedDataSetDataTypeCollection()
                {
                    publishedDataSetSimple, publishedDataSetAllTypes, publishedDataSetMassTest
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
            Console.WriteLine("\tx,q: shutdown the Publisher\n\n");
        }

        /// <summary>
        /// Load trace configuration for logging
        /// </summary>
        private static void LoadTraceLogger()
        {
            m_traceConfiguration = new TraceConfiguration();
            m_traceConfiguration.OutputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), SamplePublisherLogFile);
            m_traceConfiguration.DeleteOnLoad = true;
            m_traceConfiguration.TraceMasks = 1;
            m_traceConfiguration.ApplySettings();
        }
        #endregion
    }
}
