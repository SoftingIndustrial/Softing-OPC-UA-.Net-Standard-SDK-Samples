/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA_SIA_EN
 * 
 * ======================================================================*/

using System;
using System.IO;
using Opc.Ua;
using Opc.Ua.PubSub;
using Opc.Ua.PubSub.Configuration;
using Opc.Ua.PubSub.Transport;

namespace SamplePublisher
{
    public class Program
    {
        #region Fields
        // constant DateTime that represents the initial time when the metadata for the configuration was created
        private static readonly DateTime kTimeOfConfiguration = new DateTime(2021, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        private const string SamplePublisherLogFile = "Softing/OpcUaNetStandardToolkit/logs/SamplePublisher.log";
        #endregion

        /// <summary>
        /// Entry point for application
        /// </summary>
        static void Main()
        {
            DataStoreValuesGenerator dataStoreValuesGenerator = null;
            try
            {
                LoadTraceLogger();

                string configurationFileName = "SamplePublisher_MQTT_JSON.Config.xml";
                //string configurationFileName = "SamplePublisher_UDP_UADP.Config.xml";
                //string configurationFileName = "SamplePublisher_UDP_UADP.AllTypes.Config.xml";
                //string configurationFileName = "SamplePublisher_MQTT_UADP.Config.xml";

                string[] commandLineArguments = Environment.GetCommandLineArgs();
                if (commandLineArguments.Length > 1)
                {
                    if (File.Exists(commandLineArguments[1]))
                    {
                        configurationFileName = commandLineArguments[1];
                    }
                }

                // generate the config using code
                //var config = CreateConfiguration_UdpUadp();
                //UaPubSubConfigurationHelper.SaveConfiguration(config, configurationFileName);

                // Create the PubSub application
                using (UaPubSubApplication uaPubSubApplication = UaPubSubApplication.Create(configurationFileName))
                { 
                    // the PubSub application can be also created from an instance of PubSubConfigurationDataType returned by CreateConfiguration() method
                    //PubSubConfigurationDataType pubSubConfiguration = CreateConfiguration_MqttJson();
                    //using (UaPubSubApplication uaPubSubApplication = UaPubSubApplication.Create(pubSubConfiguration)){

                    // Start values simulator
                    dataStoreValuesGenerator = new DataStoreValuesGenerator(uaPubSubApplication);
                    dataStoreValuesGenerator.Start();

                    Console.WriteLine("SamplePublisher started at:{0} with configurationFileName:{1}",
                        DateTime.Now.ToLongTimeString(), configurationFileName);
                    PrintCommandParameters();

                    // start application
                    uaPubSubApplication.Start();
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
            finally
            {
                if (dataStoreValuesGenerator != null)
                {
                    dataStoreValuesGenerator.Dispose();
                }
            }
        }

        #region Create configuration objects

        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically for MqttJson profile
        /// </summary>
        /// <returns></returns>
        private static PubSubConfigurationDataType CreateConfiguration_MqttJson()
        {
            // Define a PubSub connection with PublisherId 10
            PubSubConnectionDataType pubSubConnection1 = new PubSubConnectionDataType();
            pubSubConnection1.Name = "MqttJsonConnection_Publisher";
            pubSubConnection1.Enabled = true;
            pubSubConnection1.PublisherId = (UInt16)10;
            pubSubConnection1.TransportProfileUri = Profiles.PubSubMqttJsonTransport;
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            // Leave empty for MQTT.
            address.NetworkInterface = String.Empty;
            address.Url = "mqtt://localhost:1883";
            pubSubConnection1.Address = new ExtensionObject(address);

            // Configure the mqtt specific configuration with the MQTT broker
            ITransportProtocolConfiguration mqttConfiguration = new MqttClientProtocolConfiguration(version: EnumMqttProtocolVersion.V500);
            pubSubConnection1.ConnectionProperties = mqttConfiguration.ConnectionProperties;

            // set the BrokerConnectionTransportDataType TransportSettings
            pubSubConnection1.TransportSettings = new ExtensionObject()
            {
                Body = new BrokerConnectionTransportDataType()
            };

            string brokerQueueName = "Json_WriterGroup_1";
            string brokerMetaData = "$Metadata";
            string metaDataQueueName = $"{brokerQueueName}/{brokerMetaData}";

            #region Define WriterGroup11
            WriterGroupDataType writerGroup11 = new WriterGroupDataType();
            writerGroup11.Name = "WriterGroup 11";
            writerGroup11.Enabled = true;
            writerGroup11.WriterGroupId = 11;
            writerGroup11.PublishingInterval = 5000;
            writerGroup11.KeepAliveTime = 5000;
            writerGroup11.MaxNetworkMessageSize = 1500;

            JsonWriterGroupMessageDataType jsonMessageSettings = new JsonWriterGroupMessageDataType()
            {
                NetworkMessageContentMask = (uint)(JsonNetworkMessageContentMask.NetworkMessageHeader
                       | JsonNetworkMessageContentMask.DataSetMessageHeader
                        | JsonNetworkMessageContentMask.PublisherId
                        | JsonNetworkMessageContentMask.DataSetClassId
                        | JsonNetworkMessageContentMask.ReplyTo)
            };

            writerGroup11.MessageSettings = new ExtensionObject(jsonMessageSettings);
            writerGroup11.TransportSettings = new ExtensionObject(new BrokerWriterGroupTransportDataType()
            {
                QueueName = brokerQueueName,
                RequestedDeliveryGuarantee = BrokerTransportQualityOfService.BestEffort,
            });

            // Define DataSetWriter 'Simple'
            DataSetWriterDataType dataSetWriter111 = new DataSetWriterDataType();
            dataSetWriter111.Name = "Writer 111";
            dataSetWriter111.DataSetWriterId = 111;
            dataSetWriter111.Enabled = true;
            dataSetWriter111.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetWriter111.DataSetName = "Simple";
            dataSetWriter111.KeyFrameCount = 1;

            JsonDataSetWriterMessageDataType jsonDataSetWriterMessage = new JsonDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = (uint)(JsonDataSetMessageContentMask.DataSetWriterId
                | JsonDataSetMessageContentMask.MetaDataVersion
                | JsonDataSetMessageContentMask.SequenceNumber
                | JsonDataSetMessageContentMask.Status
                | JsonDataSetMessageContentMask.Timestamp),
            };
            dataSetWriter111.MessageSettings = new ExtensionObject(jsonDataSetWriterMessage);

            BrokerDataSetWriterTransportDataType jsonDataSetWriterTransport = new BrokerDataSetWriterTransportDataType()
            {
                QueueName = brokerQueueName,
                RequestedDeliveryGuarantee = BrokerTransportQualityOfService.BestEffort,
                MetaDataQueueName = metaDataQueueName,
                MetaDataUpdateTime = 0,
            };
            dataSetWriter111.TransportSettings = new ExtensionObject(jsonDataSetWriterTransport);

            writerGroup11.DataSetWriters.Add(dataSetWriter111);

            // Define DataSetWriter 'AllTypes'
            DataSetWriterDataType dataSetWriter112 = new DataSetWriterDataType();
            dataSetWriter112.Name = "Writer 112";
            dataSetWriter112.DataSetWriterId = 112;
            dataSetWriter112.Enabled = true;
            dataSetWriter112.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetWriter112.DataSetName = "AllTypes";
            dataSetWriter112.KeyFrameCount = 1;

            jsonDataSetWriterMessage = new JsonDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = (uint)(JsonDataSetMessageContentMask.DataSetWriterId
                | JsonDataSetMessageContentMask.MetaDataVersion
                | JsonDataSetMessageContentMask.SequenceNumber
                | JsonDataSetMessageContentMask.Status
                | JsonDataSetMessageContentMask.Timestamp),
            };
            dataSetWriter112.TransportSettings = new ExtensionObject(jsonDataSetWriterTransport);

            jsonDataSetWriterTransport = new BrokerDataSetWriterTransportDataType()
            {
                QueueName = brokerQueueName,
                RequestedDeliveryGuarantee = BrokerTransportQualityOfService.BestEffort,
                MetaDataQueueName = metaDataQueueName,
                MetaDataUpdateTime = 0,
            };
            dataSetWriter112.MessageSettings = new ExtensionObject(jsonDataSetWriterMessage);

            writerGroup11.DataSetWriters.Add(dataSetWriter112);

            // Define DataSetWriter 'MassTest'
            DataSetWriterDataType dataSetWriter113 = new DataSetWriterDataType();
            dataSetWriter113.Name = "Writer 113";
            dataSetWriter113.DataSetWriterId = 113;
            dataSetWriter113.Enabled = true;
            dataSetWriter113.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetWriter113.DataSetName = "MassTest";
            dataSetWriter113.KeyFrameCount = 1;
            jsonDataSetWriterMessage = new JsonDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = (uint)(JsonDataSetMessageContentMask.DataSetWriterId
               | JsonDataSetMessageContentMask.MetaDataVersion
               | JsonDataSetMessageContentMask.SequenceNumber
               | JsonDataSetMessageContentMask.Status
               | JsonDataSetMessageContentMask.Timestamp),
            };
            dataSetWriter113.MessageSettings = new ExtensionObject(jsonDataSetWriterMessage);

            jsonDataSetWriterTransport = new BrokerDataSetWriterTransportDataType()
            {
                QueueName = brokerQueueName,
                RequestedDeliveryGuarantee = BrokerTransportQualityOfService.BestEffort,
                MetaDataQueueName = metaDataQueueName,
                MetaDataUpdateTime = 0,
            };
            dataSetWriter113.TransportSettings = new ExtensionObject(jsonDataSetWriterTransport);

            writerGroup11.DataSetWriters.Add(dataSetWriter113);

            pubSubConnection1.WriterGroups.Add(writerGroup11);
            #endregion          

            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };
            pubSubConfiguration.PublishedDataSets = new PublishedDataSetDataTypeCollection()
                {
                    GetPublishedDataSetSimple(), GetPublishedDataSetAllTypes(), GetPublishedDataSetMassTest()
                };

            return pubSubConfiguration;
        }

        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically for UdpUadp profile
        /// </summary>
        /// <returns></returns>
        private static PubSubConfigurationDataType CreateConfiguration_UdpUadp()
        {
            // Define a PubSub connection with PublisherId 20
            PubSubConnectionDataType pubSubConnection1 = new PubSubConnectionDataType();
            pubSubConnection1.Name = "UdpUadpConnection_Publisher";
            pubSubConnection1.Enabled = true;
            pubSubConnection1.PublisherId = (UInt16)20;
            pubSubConnection1.TransportProfileUri = Profiles.PubSubUdpUadpTransport;
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            address.NetworkInterface = string.Empty;
            address.Url = "opc.udp://239.0.0.1:4840";
            pubSubConnection1.Address = new ExtensionObject(address);
            // set the DatagramConnectionTransportDataType TransportSettings
            pubSubConnection1.TransportSettings = new ExtensionObject()
            {
                Body = new DatagramConnectionTransportDataType()
                {
                    DiscoveryAddress = new ExtensionObject()
                    {
                        Body = new NetworkAddressUrlDataType()
                        {
                            Url = "opc.udp://224.0.2.15:4840"
                        }
                    }
                }
            };

            #region Define WriterGroup21
            WriterGroupDataType writerGroup21 = new WriterGroupDataType();
            writerGroup21.Name = "WriterGroup 21";
            writerGroup21.Enabled = true;
            writerGroup21.WriterGroupId = 21;
            writerGroup21.PublishingInterval = 5000;
            writerGroup21.KeepAliveTime = 5000;
            writerGroup21.MaxNetworkMessageSize = 1500;
            writerGroup21.HeaderLayoutUri = "UADP-Cyclic-Fixed";
            UadpWriterGroupMessageDataType messageSettings = new UadpWriterGroupMessageDataType()
            {
                DataSetOrdering = DataSetOrderingType.Undefined,
                GroupVersion = 0,
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId 
                    | UadpNetworkMessageContentMask.GroupHeader
                    | UadpNetworkMessageContentMask.WriterGroupId 
                    | UadpNetworkMessageContentMask.GroupVersion
                    | UadpNetworkMessageContentMask.NetworkMessageNumber 
                    | UadpNetworkMessageContentMask.SequenceNumber
                    | UadpNetworkMessageContentMask.PayloadHeader)
            };

            writerGroup21.MessageSettings = new ExtensionObject(messageSettings);
            writerGroup21.TransportSettings = new ExtensionObject(new DatagramWriterGroupTransportDataType());

            // Define DataSetWriter 'Simple'
            DataSetWriterDataType dataSetReaderSimple1 = new DataSetWriterDataType();
            dataSetReaderSimple1.Name = "Writer 211";
            dataSetReaderSimple1.DataSetWriterId = 211;
            dataSetReaderSimple1.Enabled = true;
            dataSetReaderSimple1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderSimple1.DataSetName = "Simple";
            dataSetReaderSimple1.KeyFrameCount = 1;
            UadpDataSetWriterMessageDataType uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderSimple1.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup21.DataSetWriters.Add(dataSetReaderSimple1);

            // Define DataSetWriter 'AllTypes'
            DataSetWriterDataType dataSetReaderAllTypes1 = new DataSetWriterDataType();
            dataSetReaderAllTypes1.Name = "Writer 212";
            dataSetReaderAllTypes1.DataSetWriterId = 212;
            dataSetReaderAllTypes1.Enabled = true;
            dataSetReaderAllTypes1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes1.DataSetName = "AllTypes";
            dataSetReaderAllTypes1.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderAllTypes1.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup21.DataSetWriters.Add(dataSetReaderAllTypes1);

            // Define DataSetWriter 'MassTest'
            DataSetWriterDataType dataSetReaderMassTest1 = new DataSetWriterDataType();
            dataSetReaderMassTest1.Name = "Writer 213";
            dataSetReaderMassTest1.DataSetWriterId = 213;
            dataSetReaderMassTest1.Enabled = true;
            dataSetReaderMassTest1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderMassTest1.DataSetName = "MassTest";
            dataSetReaderMassTest1.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderMassTest1.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup21.DataSetWriters.Add(dataSetReaderMassTest1);

            pubSubConnection1.WriterGroups.Add(writerGroup21);
            #endregion                        

            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };
            pubSubConfiguration.PublishedDataSets = new PublishedDataSetDataTypeCollection()
                {
                    GetPublishedDataSetSimple(), GetPublishedDataSetAllTypes(), GetPublishedDataSetMassTest()
                };

            return pubSubConfiguration;
        }

        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically for a dataset with all data types for the UdpUadp profile
        /// </summary>
        /// <returns></returns>
        private static PubSubConfigurationDataType CreateConfigurationAllDataTypes_UdpUadp()
        {
            // Define a PubSub connection with PublisherId 20
            PubSubConnectionDataType pubSubConnection3 = new PubSubConnectionDataType();
            pubSubConnection3.Name = "UdpUadpConection3";
            pubSubConnection3.Enabled = true;
            pubSubConnection3.PublisherId = (UInt16)20;
            pubSubConnection3.TransportProfileUri = Profiles.PubSubUdpUadpTransport;
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            address.NetworkInterface = string.Empty;
            address.Url = "opc.udp://239.0.0.13:4840";
            pubSubConnection3.Address = new ExtensionObject(address);

            #region Define WriterGroup23
            WriterGroupDataType writerGroup23 = new WriterGroupDataType();
            writerGroup23.Name = "WriterGroup 23";
            writerGroup23.Enabled = true;
            writerGroup23.WriterGroupId = 23;
            writerGroup23.PublishingInterval = 5000;
            writerGroup23.KeepAliveTime = 5000;
            writerGroup23.MaxNetworkMessageSize = 1500;
            writerGroup23.HeaderLayoutUri = "UADP-Cyclic-Fixed";
            UadpWriterGroupMessageDataType messageSettings = new UadpWriterGroupMessageDataType()
            {
                DataSetOrdering = DataSetOrderingType.AscendingWriterId,
                GroupVersion = 0,
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.GroupHeader
                        | UadpNetworkMessageContentMask.WriterGroupId | UadpNetworkMessageContentMask.GroupVersion
                        | UadpNetworkMessageContentMask.NetworkMessageNumber | UadpNetworkMessageContentMask.SequenceNumber)
            };

            writerGroup23.MessageSettings = new ExtensionObject(messageSettings);
            writerGroup23.TransportSettings = new ExtensionObject(new DatagramWriterGroupTransportDataType());

            // Define DataSetWriter 'AllTypes'
            DataSetWriterDataType dataSetReaderAllTypes1 = new DataSetWriterDataType();
            dataSetReaderAllTypes1.Name = "Writer 231";
            dataSetReaderAllTypes1.DataSetWriterId = 231;
            dataSetReaderAllTypes1.Enabled = true;
            dataSetReaderAllTypes1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes1.DataSetName = "AllTypes";
            dataSetReaderAllTypes1.KeyFrameCount = 1;
            UadpDataSetWriterMessageDataType uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                ConfiguredSize = 0,
                DataSetOffset = 15,
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderAllTypes1.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup23.DataSetWriters.Add(dataSetReaderAllTypes1);

            pubSubConnection3.WriterGroups.Add(writerGroup23);
            #endregion

            // Define a PubSub connection with PublisherId 20
            PubSubConnectionDataType pubSubConnection4 = new PubSubConnectionDataType();
            pubSubConnection4.Name = "UdpUadpConection4";
            pubSubConnection4.Enabled = true;
            pubSubConnection4.PublisherId = (UInt64)20;
            pubSubConnection4.TransportProfileUri = Profiles.PubSubUdpUadpTransport;
            address = new NetworkAddressUrlDataType();
            address.NetworkInterface = string.Empty;
            address.Url = "opc.udp://239.0.0.13:4840";
            pubSubConnection4.Address = new ExtensionObject(address);

            #region Define WriterGroup24
            WriterGroupDataType writerGroup24 = new WriterGroupDataType();
            writerGroup24.Name = "WriterGroup 24";
            writerGroup24.Enabled = true;
            writerGroup24.WriterGroupId = 24;
            writerGroup24.PublishingInterval = 5000;
            writerGroup24.KeepAliveTime = 5000;
            writerGroup24.MaxNetworkMessageSize = 1500;
            writerGroup24.HeaderLayoutUri = "UADP-Dynamic";
            messageSettings = new UadpWriterGroupMessageDataType()
            {
                DataSetOrdering = DataSetOrderingType.Undefined,
                GroupVersion = 0,
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.PayloadHeader)
            };

            writerGroup24.MessageSettings = new ExtensionObject(messageSettings);
            writerGroup24.TransportSettings = new ExtensionObject(new DatagramWriterGroupTransportDataType());

            // Define DataSetWriter 'AllTypes'
            DataSetWriterDataType dataSetReaderAllTypes2 = new DataSetWriterDataType();
            dataSetReaderAllTypes2.Name = "Writer 241";
            dataSetReaderAllTypes2.DataSetWriterId = 241;
            dataSetReaderAllTypes2.Enabled = true;
            dataSetReaderAllTypes2.DataSetFieldContentMask = (uint)DataSetFieldContentMask.None; //Variant encoding
            dataSetReaderAllTypes2.DataSetName = "AllTypes";
            dataSetReaderAllTypes2.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                //DataValue Encoding
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
                        | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderAllTypes2.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup24.DataSetWriters.Add(dataSetReaderAllTypes2);

            pubSubConnection4.WriterGroups.Add(writerGroup24);
            #endregion                        

            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection3, pubSubConnection4
                };
            pubSubConfiguration.PublishedDataSets = new PublishedDataSetDataTypeCollection()
                {
                   GetPublishedDataSetAllTypes()
                };

            return pubSubConfiguration;
        }
                
        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically for MqttUadp profile
        /// </summary>
        /// <returns></returns>
        private static PubSubConfigurationDataType CreateConfiguration_MqttUadp()
        {
            // Define a PubSub connection with PublisherId 30
            PubSubConnectionDataType pubSubConnection1 = new PubSubConnectionDataType();
            pubSubConnection1.Name = "MqttUadpConnection_Publisher";
            pubSubConnection1.Enabled = true;
            pubSubConnection1.PublisherId = (UInt16)30;
            pubSubConnection1.TransportProfileUri = Profiles.PubSubMqttUadpTransport;
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            // Specify the local Network interface name to be used
            // e.g. address.NetworkInterface = "Ethernet";
            // Leave empty to publish on all available local interfaces.
            address.NetworkInterface = String.Empty;
            address.Url = "mqtt://localhost:1883";
            pubSubConnection1.Address = new ExtensionObject(address);

            // Configure the mqtt specific configuration with the MQTTbroker
            ITransportProtocolConfiguration mqttConfiguration = new MqttClientProtocolConfiguration(version: EnumMqttProtocolVersion.V500);
            pubSubConnection1.ConnectionProperties = mqttConfiguration.ConnectionProperties;

            // set the BrokerConnectionTransportDataType TransportSettings
            pubSubConnection1.TransportSettings = new ExtensionObject()
            {
                Body = new BrokerConnectionTransportDataType()
            };

            string brokerQueueName = "Uadp_WriterGroup_1";
            string brokerMetaData = "$Metadata";
            string metaDataQueueName = $"{brokerQueueName}/{brokerMetaData}";

            #region Define WriterGroup1
            WriterGroupDataType writerGroup1 = new WriterGroupDataType();
            writerGroup1.Name = "WriterGroup 31";
            writerGroup1.Enabled = true;
            writerGroup1.WriterGroupId = 31;
            writerGroup1.PublishingInterval = 5000;
            writerGroup1.KeepAliveTime = 5000;
            writerGroup1.MaxNetworkMessageSize = 1500;
            writerGroup1.HeaderLayoutUri = "UADP-Cyclic-Fixed";
            UadpWriterGroupMessageDataType messageSettings = new UadpWriterGroupMessageDataType()
            {
                DataSetOrdering = DataSetOrderingType.Undefined,
                GroupVersion = 0,
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId 
                    | UadpNetworkMessageContentMask.GroupHeader
                    | UadpNetworkMessageContentMask.WriterGroupId 
                    | UadpNetworkMessageContentMask.GroupVersion
                    | UadpNetworkMessageContentMask.NetworkMessageNumber 
                    | UadpNetworkMessageContentMask.SequenceNumber
                    | UadpNetworkMessageContentMask.PayloadHeader)
            };

            writerGroup1.MessageSettings = new ExtensionObject(messageSettings);
            // initialize Broker transport settings
            writerGroup1.TransportSettings = new ExtensionObject(new BrokerWriterGroupTransportDataType()
            {
                QueueName = brokerQueueName,
            });

            // Define DataSetWriter 'Simple'
            DataSetWriterDataType dataSetWriter1 = new DataSetWriterDataType();
            dataSetWriter1.Name = "Writer 311";
            dataSetWriter1.DataSetWriterId = 311;
            dataSetWriter1.Enabled = true;
            dataSetWriter1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetWriter1.DataSetName = "Simple";
            dataSetWriter1.KeyFrameCount = 1;
            UadpDataSetWriterMessageDataType uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                ConfiguredSize = 32,
                DataSetOffset = 15,
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };

            dataSetWriter1.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            BrokerDataSetWriterTransportDataType uadpDataSetWriterTransport = new BrokerDataSetWriterTransportDataType()
            {
                QueueName = brokerQueueName,
                MetaDataQueueName = metaDataQueueName,
                MetaDataUpdateTime = 60000
            };
            dataSetWriter1.TransportSettings = new ExtensionObject(uadpDataSetWriterTransport);
            
            writerGroup1.DataSetWriters.Add(dataSetWriter1);

            // Define DataSetWriter 'AllTypes'
            DataSetWriterDataType dataSetWriter2 = new DataSetWriterDataType();
            dataSetWriter2.Name = "Writer 312";
            dataSetWriter2.DataSetWriterId = 312;
            dataSetWriter2.Enabled = true;
            dataSetWriter2.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetWriter2.DataSetName = "AllTypes";
            dataSetWriter2.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                ConfiguredSize = 32,
                DataSetOffset = 47,
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };

            dataSetWriter2.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            dataSetWriter2.TransportSettings = new ExtensionObject(uadpDataSetWriterTransport);

            writerGroup1.DataSetWriters.Add(dataSetWriter2);

            // Define DataSetWriter 'MassTest'
            DataSetWriterDataType dataSetWriter3 = new DataSetWriterDataType();
            dataSetWriter3.Name = "Writer 313";
            dataSetWriter3.DataSetWriterId = 313;
            dataSetWriter3.Enabled = true;
            dataSetWriter3.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetWriter3.DataSetName = "MassTest";
            dataSetWriter3.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                ConfiguredSize = 405,
                DataSetOffset = 79,
                NetworkMessageNumber = 1,
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };

            dataSetWriter3.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            dataSetWriter3.TransportSettings = new ExtensionObject(uadpDataSetWriterTransport);
            writerGroup1.DataSetWriters.Add(dataSetWriter3);

            pubSubConnection1.WriterGroups.Add(writerGroup1);
            #endregion

            //create  the PubSub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };
            pubSubConfiguration.PublishedDataSets = new PublishedDataSetDataTypeCollection()
                {
                    GetPublishedDataSetSimple(), GetPublishedDataSetAllTypes(), GetPublishedDataSetMassTest()
                };

            return pubSubConfiguration;
        }
        
        /// <summary>
        /// Creates and returns an instance of <see cref="PublishedDataSetDataType"/> for Simple DataSet
        /// </summary>
        private static PublishedDataSetDataType GetPublishedDataSetSimple()
        {
            PublishedDataSetDataType publishedDataSetSimple = new PublishedDataSetDataType();
            publishedDataSetSimple.Name = "Simple"; //name shall be unique in a configuration
            // Define  publishedDataSetSimple.DataSetMetaData
            publishedDataSetSimple.DataSetMetaData = new DataSetMetaDataType();
            publishedDataSetSimple.DataSetMetaData.DataSetClassId = Uuid.Empty;
            publishedDataSetSimple.DataSetMetaData.Name = publishedDataSetSimple.Name;
            publishedDataSetSimple.DataSetMetaData.Fields = new FieldMetaDataCollection()
                {
                    new FieldMetaData()
                    {
                        Name = "BoolToggle",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.Boolean,
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.Int32,
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32Fast",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.Int32,
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "DateTime",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.DateTime,
                        DataType = DataTypeIds.DateTime,
                        ValueRank = ValueRanks.Scalar
                    },
                };

            // set the ConfigurationVersion relative to kTimeOfConfiguration constant
            publishedDataSetSimple.DataSetMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = ConfigurationVersionUtils.CalculateVersionTime(kTimeOfConfiguration),
                MajorVersion = ConfigurationVersionUtils.CalculateVersionTime(kTimeOfConfiguration)
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
                    ,
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("QualifiedName"),
                        Value = new QualifiedName("QualifiedNameValue")
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

            return publishedDataSetSimple;
        }

        /// <summary>
        /// Creates and returns an instance of <see cref="PublishedDataSetDataType"/> for AllTypes DataSet
        /// </summary>
        private static PublishedDataSetDataType GetPublishedDataSetAllTypes()
        {
            PublishedDataSetDataType publishedDataSetAllTypes = new PublishedDataSetDataType();
            publishedDataSetAllTypes.Name = "AllTypes"; //name shall be unique in a configuration
            // Define  publishedDataSetAllTypes.DataSetMetaData
            publishedDataSetAllTypes.DataSetMetaData = new DataSetMetaDataType();
            publishedDataSetAllTypes.DataSetMetaData.DataSetClassId = Uuid.Empty;
            publishedDataSetAllTypes.DataSetMetaData.Name = publishedDataSetAllTypes.Name;
            publishedDataSetAllTypes.DataSetMetaData.Fields = new FieldMetaDataCollection()
                {
                    new FieldMetaData()
                    {
                        Name = "BoolToggle",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.Boolean,
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Byte",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.Byte,
                        DataType = DataTypeIds.Byte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int16",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.Int16,
                        DataType = DataTypeIds.Int16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.Int32,
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "SByte",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.SByte,
                        DataType = DataTypeIds.SByte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt16",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.UInt16,
                        DataType = DataTypeIds.UInt16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt32",
                        DataSetFieldId = new Uuid(Guid.Empty),
                         BuiltInType = (byte)DataTypes.UInt32,
                        DataType = DataTypeIds.UInt32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Float",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.Float,
                        DataType = DataTypeIds.Float,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Double",
                        DataSetFieldId = new Uuid(Guid.Empty),
                        BuiltInType = (byte)DataTypes.Double,
                        DataType = DataTypeIds.Double,
                        ValueRank = ValueRanks.Scalar
                    },
                };
            
            // set the ConfigurationVersion relative to kTimeOfConfiguration constant
            publishedDataSetAllTypes.DataSetMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = ConfigurationVersionUtils.CalculateVersionTime(kTimeOfConfiguration),
                MajorVersion = ConfigurationVersionUtils.CalculateVersionTime(kTimeOfConfiguration)
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

            return publishedDataSetAllTypes;
        }

        /// <summary>
        /// Creates and returns an instance of <see cref="PublishedDataSetDataType"/> for MassTest DataSet
        /// </summary>
        private static PublishedDataSetDataType GetPublishedDataSetMassTest()
        {
            PublishedDataSetDataType publishedDataSetMassTest = new PublishedDataSetDataType();
            publishedDataSetMassTest.Name = "MassTest"; //name shall be unique in a configuration
            // Define  publishedDataSetMassTest.DataSetMetaData
            publishedDataSetMassTest.DataSetMetaData = new DataSetMetaDataType();
            publishedDataSetMassTest.DataSetMetaData.DataSetClassId = Uuid.Empty;
            publishedDataSetMassTest.DataSetMetaData.Name = publishedDataSetMassTest.Name;
            publishedDataSetMassTest.DataSetMetaData.Fields = new FieldMetaDataCollection();

            // set the ConfigurationVersion relative to kTimeOfConfiguration constant
            publishedDataSetMassTest.DataSetMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = ConfigurationVersionUtils.CalculateVersionTime(kTimeOfConfiguration),
                MajorVersion = ConfigurationVersionUtils.CalculateVersionTime(kTimeOfConfiguration)
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
                    DataSetFieldId = new Uuid(Guid.Empty),
                    BuiltInType = (byte)DataTypes.UInt32,
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

            return publishedDataSetMassTest;
        }

        #endregion Create configuration objects

        #region Private methods

        /// <summary>
        /// Print command line parameters for this console application
        /// </summary>
        private static void PrintCommandParameters()
        {
            Console.WriteLine("Press:\n\ts: display configuration status");
            Console.WriteLine("\te: enable configuration object specified by id");
            Console.WriteLine("\td: disable configuration object specified by id");
            Console.WriteLine("\tx,q: shutdown the Publisher\n\n");
        }

        /// <summary>
        /// Handle Enable configuration method call from command line
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
            Console.WriteLine("\nConfiguration  \t\t\t\t\t\t\t\t-ConfigId={0}, State={1}",
                configurator.FindIdForObject(configurationObject), configurator.FindStateForObject(configurationObject));
            foreach (var connection in configurator.PubSubConfiguration.Connections)
            {
                Console.WriteLine("Connection '{0}'\t\t\t\t-ConfigId={1}, State={2}",
                    connection.Name, configurator.FindIdForObject(connection), configurator.FindStateForObject(connection));
                foreach (var writerGroup in connection.WriterGroups)
                {
                    Console.WriteLine("  WriterGroup Name ='{0}' WriterGroupId={1}\t\t\t-ConfigId={2}, State={3}",
                        writerGroup.Name, writerGroup.WriterGroupId, configurator.FindIdForObject(writerGroup), configurator.FindStateForObject(writerGroup));
                    foreach (var dataSetWriter in writerGroup.DataSetWriters)
                    {
                        Console.WriteLine("    DataSetWriter Name ='{0}' DataSetWriterId={1}\t\t-ConfigId={2}, State={3}",
                           dataSetWriter.Name, dataSetWriter.DataSetWriterId, configurator.FindIdForObject(dataSetWriter), configurator.FindStateForObject(dataSetWriter));
                    }
                }
                foreach (var readerGroup in connection.ReaderGroups)
                {
                    Console.WriteLine("  ReaderGroup Name ='{0}'\t\t\t-ConfigId={1}, State={2}",
                        readerGroup.Name, configurator.FindIdForObject(readerGroup), configurator.FindStateForObject(readerGroup));
                    foreach (var dataSetReader in readerGroup.DataSetReaders)
                    {
                        Console.WriteLine("    DataSetReader Name ='{0}'\t\t-ConfigId={1}, State={2}",
                            dataSetReader.Name, configurator.FindIdForObject(dataSetReader), configurator.FindStateForObject(dataSetReader));
                    }
                }
            }
        }
        
        /// <summary>
        /// Load trace configuration for logging
        /// </summary>
        private static TraceConfiguration LoadTraceLogger()
        {
            TraceConfiguration traceConfiguration = new TraceConfiguration();
            traceConfiguration.OutputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), SamplePublisherLogFile);
            traceConfiguration.DeleteOnLoad = true;
            traceConfiguration.TraceMasks = Utils.TraceMasks.Error; // Use other flag if necessary (e.g Utils.TraceMasks.All)
            traceConfiguration.ApplySettings();

            return traceConfiguration;
        }

        #endregion Private methods
    }
}
