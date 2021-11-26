/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.PubSub;
using Opc.Ua.PubSub.Configuration;
using Opc.Ua.PubSub.Encoding;
using Opc.Ua.PubSub.PublishedData;
using Opc.Ua.PubSub.Transport;
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
        private const int MaximumNumberOfFieldsDisplayed = 15;
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

                string configurationFileName = "SampleSubscriber_MQTT_JSON.Config.xml";
                //string configurationFileName = "SampleSubscriber_UDP_UADP.Config.xml";
                //string configurationFileName = "SampleSubscriber_UDP_UADP.AllTypes.Config.xml";
                //string configurationFileName = "SampleSubscriber_MQTT_UADP.Config.xml";
                
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
                    // the PubSub application can be also created from an instance of PubSubConfigurationDataType
                    // PubSubConfigurationDataType pubSubConfiguration = CreateConfiguration_MqttJson();
                    // using (UaPubSubApplication uaPubSubApplication = UaPubSubApplication.Create(pubSubConfiguration)){

                    // subscribe to data events 
                    uaPubSubApplication.DataReceived += PubSubApplication_DataReceived;
                    uaPubSubApplication.RawDataReceived += PubSubApplication_RawDataReceived;
                    uaPubSubApplication.MetaDataReceived += PubSubApplication_MetaDataReceived;

                    //start application
                    uaPubSubApplication.Start();

                    Console.WriteLine("SampleSubscriber started at:{0} with configurationFileName:{1}",
                        DateTime.Now.ToLongTimeString(), configurationFileName);
                    PrintCommandParameters();

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
        private static void PubSubApplication_DataReceived(object sender, SubscribedDataEventArgs e)
        {
            lock (m_lock)
            {
                if (e.NetworkMessage is UadpNetworkMessage)
                {
                    Console.WriteLine("UADP Network message was received from Source={0}, SequenceNumber={1}, DataSet count={2}",
                            e.Source, ((UadpNetworkMessage)e.NetworkMessage).SequenceNumber, e.NetworkMessage.DataSetMessages.Count);
                }
                else if (e.NetworkMessage is JsonNetworkMessage)
                {
                    Console.WriteLine("JSON Network message was received from Source={0}, MessageId={1}, DataSet count={2}",
                            e.Source, ((JsonNetworkMessage)e.NetworkMessage).MessageId, e.NetworkMessage.DataSetMessages.Count);
                }

                foreach (UaDataSetMessage dataSetMessage in e.NetworkMessage.DataSetMessages)
                {
                    DataSet dataSet = dataSetMessage.DataSet;
                    Console.WriteLine("\tSequencenumber={0}, DataSet.Name={1}, DataSetWriterId={2}",
                        dataSetMessage.SequenceNumber, dataSet.Name, dataSet.DataSetWriterId);
                    for (int i = 0; i < dataSet.Fields.Length; i++)
                    {
                        Console.WriteLine("\t\tFieldName:{0}, Value:{1}",
                            dataSet.Fields[i].FieldMetaData.Name, dataSet.Fields[i].Value);
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

        /// <summary>
        /// Handler for <see cref="UaPubSubApplication.RawDataReceived" /> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PubSubApplication_RawDataReceived(object sender, RawDataReceivedEventArgs e)
        {
            lock (m_lock)
            {
                Console.WriteLine("RawDataReceived bytes:{0}, Source:{1}, TransportProtocol:{2}, MessageMapping:{3}",
                    e.Message.Length, e.Source, e.TransportProtocol, e.MessageMapping);

                Console.WriteLine("------------------------------------------------");
            }
        }

        /// <summary>
        /// Handler for <see cref="UaPubSubApplication.MetaDataDataReceived" /> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PubSubApplication_MetaDataReceived(object sender, SubscribedDataEventArgs e)
        {
            lock (m_lock)
            {
                Console.WriteLine("MetaDataDataReceived event:");
                if (e.NetworkMessage is JsonNetworkMessage)
                {
                    Console.WriteLine("JSON Network MetaData Message: Source={0}, PublisherId={1}, DataSetWriterId={2} Fields count={3}\n",
                         e.Source,
                         ((JsonNetworkMessage)e.NetworkMessage).PublisherId,
                         ((JsonNetworkMessage)e.NetworkMessage).DataSetWriterId,
                         e.NetworkMessage.DataSetMetaData.Fields.Count);
                }
                if (e.NetworkMessage is UadpNetworkMessage)
                {
                    Console.WriteLine("UADP Network MetaData Message: Source={0}, PublisherId={1}, DataSetWriterId={2} Fields count={3}\n",
                         e.Source,
                         ((UadpNetworkMessage)e.NetworkMessage).PublisherId,
                         ((UadpNetworkMessage)e.NetworkMessage).DataSetWriterId,
                         e.NetworkMessage.DataSetMetaData.Fields.Count);
                }

                Console.WriteLine("\tMetaData.Name={0}, MajorVersion={1} MinorVersion={2}",
                    e.NetworkMessage.DataSetMetaData.Name,
                    e.NetworkMessage.DataSetMetaData.ConfigurationVersion.MajorVersion,
                    e.NetworkMessage.DataSetMetaData.ConfigurationVersion.MinorVersion);

                for (int i = 0; i < e.NetworkMessage.DataSetMetaData.Fields.Count; i++)
                {
                    FieldMetaData metaDataField = e.NetworkMessage.DataSetMetaData.Fields[i];
                    Console.WriteLine("\t\t{0, -20} DataType:{1, 10}, ValueRank:{2, 5}", metaDataField.Name, metaDataField.DataType, metaDataField.ValueRank);
                    if (i > MaximumNumberOfFieldsDisplayed)
                    {
                        Console.WriteLine("\t\t... the rest of {0} elements are omitted.", e.NetworkMessage.DataSetMetaData.Fields.Count - i);
                        break;
                    }
                }
                Console.WriteLine("------------------------------------------------");
            }
        }
        #endregion

        #region Create configuration objects

        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically for mqtt-json transport profile
        /// </summary>
        /// <returns></returns>
        private static PubSubConfigurationDataType CreateConfiguration_MqttJson()
        {
            // Define a PubSub connection with PublisherId 10
            PubSubConnectionDataType pubSubConnection1 = new PubSubConnectionDataType();
            pubSubConnection1.Name = "MqttJsonConnection_Subscriber";
            pubSubConnection1.Enabled = true;
            pubSubConnection1.PublisherId = (UInt16)10;
            pubSubConnection1.TransportProfileUri = Profiles.PubSubMqttJsonTransport;
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            // Leave empty for MQTT.
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

            string brokerQueueName = "Json_WriterGroup_1";
            string brokerMetaData = "$Metadata";
            string metaDataQueueName = $"{brokerQueueName}/{brokerMetaData}";

            #region Define ReaderGroup1
            ReaderGroupDataType readerGroup1 = new ReaderGroupDataType();
            readerGroup1.Name = "ReaderGroup 11";
            readerGroup1.Enabled = true;
            readerGroup1.MaxNetworkMessageSize = 1500;
            readerGroup1.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup1.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'Simple' for PublisherId = (UInt16)10, WriterGroupId = 11 DataSetWriterId = 111            
            DataSetReaderDataType dataSetReaderSimple = new DataSetReaderDataType();
            dataSetReaderSimple.Name = "Reader 111";
            dataSetReaderSimple.PublisherId = (UInt16)10;
            dataSetReaderSimple.WriterGroupId = 11;
            dataSetReaderSimple.DataSetWriterId = 111;
            dataSetReaderSimple.Enabled = true;
            dataSetReaderSimple.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderSimple.KeyFrameCount = 1;

            BrokerDataSetReaderTransportDataType brokerTransportSettings = new BrokerDataSetReaderTransportDataType()
            {
                QueueName = brokerQueueName,
                RequestedDeliveryGuarantee = BrokerTransportQualityOfService.AtLeastOnce,
                MetaDataQueueName = metaDataQueueName
            };

            dataSetReaderSimple.TransportSettings = new ExtensionObject(brokerTransportSettings);

            JsonDataSetReaderMessageDataType jsonDataSetReaderMessage = new JsonDataSetReaderMessageDataType()
            {
                NetworkMessageContentMask = (uint)(uint)(JsonNetworkMessageContentMask.NetworkMessageHeader
                        | JsonNetworkMessageContentMask.DataSetMessageHeader
                        | JsonNetworkMessageContentMask.PublisherId
                        | JsonNetworkMessageContentMask.DataSetClassId
                        | JsonNetworkMessageContentMask.ReplyTo),
                DataSetMessageContentMask = (uint)(JsonDataSetMessageContentMask.DataSetWriterId
                        | JsonDataSetMessageContentMask.MetaDataVersion | JsonDataSetMessageContentMask.SequenceNumber
                        | JsonDataSetMessageContentMask.Status | JsonDataSetMessageContentMask.Timestamp),
            };
            dataSetReaderSimple.MessageSettings = new ExtensionObject(jsonDataSetReaderMessage);
            
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderSimple);

            #region Define DataSetReader 'AllTypes' for PublisherId = (UInt16)10, , WriterGroupId = 11 DataSetWriterId = 112

            DataSetReaderDataType dataSetReaderAllTypes = new DataSetReaderDataType();
            dataSetReaderAllTypes.Name = "Reader 112";
            dataSetReaderAllTypes.PublisherId = (UInt16)10;
            dataSetReaderAllTypes.WriterGroupId = 11;
            dataSetReaderAllTypes.DataSetWriterId = 112;
            dataSetReaderAllTypes.Enabled = true;
            dataSetReaderAllTypes.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes.KeyFrameCount = 1;
            brokerTransportSettings = new BrokerDataSetReaderTransportDataType()
            {
                QueueName = brokerQueueName,
                RequestedDeliveryGuarantee = BrokerTransportQualityOfService.AtLeastOnce,
                MetaDataQueueName = metaDataQueueName
            };

            dataSetReaderAllTypes.TransportSettings = new ExtensionObject(brokerTransportSettings);

            jsonDataSetReaderMessage = new JsonDataSetReaderMessageDataType()
            {
                NetworkMessageContentMask = (uint)(uint)(JsonNetworkMessageContentMask.NetworkMessageHeader
                        | JsonNetworkMessageContentMask.DataSetMessageHeader
                        | JsonNetworkMessageContentMask.PublisherId
                        | JsonNetworkMessageContentMask.DataSetClassId
                        | JsonNetworkMessageContentMask.ReplyTo),
                DataSetMessageContentMask = (uint)(JsonDataSetMessageContentMask.DataSetWriterId
                        | JsonDataSetMessageContentMask.MetaDataVersion | JsonDataSetMessageContentMask.SequenceNumber
                        | JsonDataSetMessageContentMask.Status | JsonDataSetMessageContentMask.Timestamp),
            };
            dataSetReaderAllTypes.MessageSettings = new ExtensionObject(jsonDataSetReaderMessage);
            
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderAllTypes);

            #region Define DataSetReader 'MassTest' for PublisherId = (UInt16)10, , WriterGroupId = 11 DataSetWriterId = 113
            DataSetReaderDataType dataSetReaderMassTest = new DataSetReaderDataType();
            dataSetReaderMassTest.Name = "Reader 113";
            dataSetReaderAllTypes.PublisherId = (UInt16)10;
            dataSetReaderMassTest.WriterGroupId = 11;
            dataSetReaderMassTest.DataSetWriterId = 113;
            dataSetReaderMassTest.Enabled = true;
            dataSetReaderMassTest.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderMassTest.KeyFrameCount = 1;

            jsonDataSetReaderMessage = new JsonDataSetReaderMessageDataType()
            {
                NetworkMessageContentMask = (uint)(uint)(JsonNetworkMessageContentMask.NetworkMessageHeader
                        | JsonNetworkMessageContentMask.DataSetMessageHeader
                        | JsonNetworkMessageContentMask.PublisherId
                        | JsonNetworkMessageContentMask.DataSetClassId
                        | JsonNetworkMessageContentMask.ReplyTo),
                DataSetMessageContentMask = (uint)(JsonDataSetMessageContentMask.DataSetWriterId
                        | JsonDataSetMessageContentMask.MetaDataVersion | JsonDataSetMessageContentMask.SequenceNumber
                        | JsonDataSetMessageContentMask.Status | JsonDataSetMessageContentMask.Timestamp),
            };

            dataSetReaderMassTest.MessageSettings = new ExtensionObject(jsonDataSetReaderMessage);
            brokerTransportSettings = new BrokerDataSetReaderTransportDataType()
            {
                QueueName = brokerQueueName,
                RequestedDeliveryGuarantee = BrokerTransportQualityOfService.AtLeastOnce,
                MetaDataQueueName = metaDataQueueName
            };

            dataSetReaderMassTest.TransportSettings = new ExtensionObject(brokerTransportSettings);            
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderMassTest);
            #endregion
            pubSubConnection1.ReaderGroups.Add(readerGroup1);

            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };

            return pubSubConfiguration;
        }

        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically for udp-uadp transport profile
        /// </summary>
        /// <returns></returns>
        private static PubSubConfigurationDataType CreateConfiguration_UdpUadp()
        {
            // Define a PubSub connection with PublisherId 20
            PubSubConnectionDataType pubSubConnection1 = new PubSubConnectionDataType();
            pubSubConnection1.Name = "UdpUadpConnection_Subscriber";
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

            #region Define ReaderGroup21
            ReaderGroupDataType readerGroup21 = new ReaderGroupDataType();
            readerGroup21.Name = "ReaderGroup 21";
            readerGroup21.Enabled = true;
            readerGroup21.MaxNetworkMessageSize = 1500;
            readerGroup21.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup21.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'Simple' for PublisherId = (UInt16)20, WriterGroupId = 0, DataSetWriterId = 0            
            DataSetReaderDataType dataSetReaderSimple1 = new DataSetReaderDataType();
            dataSetReaderSimple1.Name = "Reader 211";
            dataSetReaderSimple1.PublisherId = (UInt16)20;
            dataSetReaderSimple1.WriterGroupId = 21;
            dataSetReaderSimple1.DataSetWriterId = 211;
            dataSetReaderSimple1.Enabled = true;
            dataSetReaderSimple1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderSimple1.KeyFrameCount = 1;           

            UadpDataSetReaderMessageDataType uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId 
                    | UadpNetworkMessageContentMask.GroupHeader
                    | UadpNetworkMessageContentMask.WriterGroupId 
                    | UadpNetworkMessageContentMask.GroupVersion
                    | UadpNetworkMessageContentMask.NetworkMessageNumber 
                    | UadpNetworkMessageContentMask.SequenceNumber
                    | UadpNetworkMessageContentMask.PayloadHeader),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderSimple1.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);

            #endregion
            readerGroup21.DataSetReaders.Add(dataSetReaderSimple1);

            #region Define DataSetReader 'AllTypes' for PublisherId = (UInt16)20, WriterGroupId = 0, DataSetWriterId = 0

            DataSetReaderDataType dataSetReaderAllTypes1 = new DataSetReaderDataType();
            dataSetReaderAllTypes1.Name = "Reader 212";
            dataSetReaderAllTypes1.PublisherId = (UInt16)20;
            dataSetReaderAllTypes1.WriterGroupId = 21;
            dataSetReaderAllTypes1.DataSetWriterId = 212;
            dataSetReaderAllTypes1.Enabled = true;
            dataSetReaderAllTypes1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes1.KeyFrameCount = 1;            

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId 
                    | UadpNetworkMessageContentMask.GroupHeader
                    | UadpNetworkMessageContentMask.WriterGroupId 
                    | UadpNetworkMessageContentMask.GroupVersion
                    | UadpNetworkMessageContentMask.NetworkMessageNumber 
                    | UadpNetworkMessageContentMask.SequenceNumber),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderAllTypes1.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);            
            #endregion
            readerGroup21.DataSetReaders.Add(dataSetReaderAllTypes1);

            #region Define DataSetReader 'MassTest' for PublisherId = (UInt16)20, WriterGroupId = 0, DataSetWriterId = 0
            DataSetReaderDataType dataSetReaderMassTest1 = new DataSetReaderDataType();
            dataSetReaderMassTest1.Name = "Reader 213";
            dataSetReaderMassTest1.PublisherId = (UInt16)20;
            dataSetReaderMassTest1.WriterGroupId = 21;
            dataSetReaderMassTest1.DataSetWriterId = 213;
            dataSetReaderMassTest1.Enabled = true;
            dataSetReaderMassTest1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderMassTest1.KeyFrameCount = 1;          

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                NetworkMessageNumber = 0,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId 
                    | UadpNetworkMessageContentMask.GroupHeader
                    | UadpNetworkMessageContentMask.WriterGroupId 
                    | UadpNetworkMessageContentMask.GroupVersion
                    | UadpNetworkMessageContentMask.NetworkMessageNumber 
                    | UadpNetworkMessageContentMask.SequenceNumber),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderMassTest1.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);            
            #endregion
            readerGroup21.DataSetReaders.Add(dataSetReaderMassTest1);
            #endregion
            pubSubConnection1.ReaderGroups.Add(readerGroup21);
           
            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };

            return pubSubConfiguration;
        }

        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically for all data types using udp-uadp transport profile
        /// </summary>
        /// <returns></returns>
        private static PubSubConfigurationDataType CreateConfigurationAllDataTypes_UdpUadp()
        {
            // Define a PubSub connection with PublisherId 20
            PubSubConnectionDataType pubSubConnection1 = new PubSubConnectionDataType();
            pubSubConnection1.Name = "UdpUadpConection2";
            pubSubConnection1.Enabled = true;
            pubSubConnection1.PublisherId = (UInt16)20;
            pubSubConnection1.TransportProfileUri = Profiles.PubSubUdpUadpTransport;
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            address.NetworkInterface = string.Empty;
            address.Url = "opc.udp://239.0.0.13:4840";
            pubSubConnection1.Address = new ExtensionObject(address);

            #region Define ReaderGroup23
            ReaderGroupDataType readerGroup23 = new ReaderGroupDataType();
            readerGroup23.Name = "ReaderGroup 23";
            readerGroup23.Enabled = true;
            readerGroup23.MaxNetworkMessageSize = 1500;
            readerGroup23.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup23.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'AllTypes' for PublisherId = (UInt16)20, WriterGroupId = 0, DataSetWriterId = 0
            DataSetReaderDataType dataSetReaderAllTypes1 = new DataSetReaderDataType();
            dataSetReaderAllTypes1.Name = "Reader For Writer 231";
            dataSetReaderAllTypes1.PublisherId = (UInt16)20;
            dataSetReaderAllTypes1.WriterGroupId = 0;
            dataSetReaderAllTypes1.DataSetWriterId = 0;
            dataSetReaderAllTypes1.Enabled = true;
            dataSetReaderAllTypes1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes1.KeyFrameCount = 1;

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
            dataSetReaderAllTypes1.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            
            #endregion
            readerGroup23.DataSetReaders.Add(dataSetReaderAllTypes1);

            #endregion
            pubSubConnection1.ReaderGroups.Add(readerGroup23);

            #region Define ReaderGroup24
            ReaderGroupDataType readerGroup24 = new ReaderGroupDataType();
            readerGroup24.Name = "ReaderGroup 24";
            readerGroup24.Enabled = true;
            readerGroup24.MaxNetworkMessageSize = 1500;
            readerGroup24.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup24.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'AllTypes' for PublisherId = (UInt64)20, WriterGroupId = 0, DataSetWriterId = 241
            DataSetReaderDataType dataSetReaderAllTypes2 = new DataSetReaderDataType();
            dataSetReaderAllTypes2.Name = "Reader For Writer 241";
            dataSetReaderAllTypes2.PublisherId = (UInt64)20;
            dataSetReaderAllTypes2.WriterGroupId = 0;
            dataSetReaderAllTypes2.DataSetWriterId = 241;
            dataSetReaderAllTypes2.Enabled = true;
            dataSetReaderAllTypes2.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes2.KeyFrameCount = 1;
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

            #endregion
            readerGroup24.DataSetReaders.Add(dataSetReaderAllTypes2);
            #endregion
            pubSubConnection1.ReaderGroups.Add(readerGroup24);

            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };

            return pubSubConfiguration;
        }

        /// <summary>
        /// Create a PubSubConfigurationDataType object programmatically for mqtt-uadp transport profile
        /// </summary>
        /// <returns></returns>
        private static PubSubConfigurationDataType CreateConfiguration_MqttUadp()
        {
            // Define a PubSub connection with PublisherId 30
            PubSubConnectionDataType pubSubConnection1 = new PubSubConnectionDataType();
            pubSubConnection1.Name = "MqttUadpConnection_Subscriber";
            pubSubConnection1.Enabled = true;
            pubSubConnection1.PublisherId = (UInt16)30;
            pubSubConnection1.TransportProfileUri = Profiles.PubSubMqttUadpTransport;
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            // Leave empty for MQTT.
            address.NetworkInterface = String.Empty;
            address.Url = "mqtt://localhost:1883";
            pubSubConnection1.Address = new ExtensionObject(address);

            // Configure the mqtt specific configuration with the MQTTbroker
            ITransportProtocolConfiguration mqttConfiguration = new MqttClientProtocolConfiguration(version: EnumMqttProtocolVersion.V500);
            pubSubConnection1.ConnectionProperties = mqttConfiguration.ConnectionProperties;

            string brokerQueueName = "Uadp_WriterGroup_1";
            string brokerMetaData = "$Metadata";
            string metaDataQueueName = $"{brokerQueueName}/{brokerMetaData}";

            #region Define ReaderGroup1
            ReaderGroupDataType readerGroup1 = new ReaderGroupDataType();
            readerGroup1.Name = "ReaderGroup 31";
            readerGroup1.Enabled = true;
            readerGroup1.MaxNetworkMessageSize = 1500;
            readerGroup1.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup1.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            #region Define DataSetReader 'Simple' for PublisherId = (UInt16)30, WriterGroupId = 31 DataSetWriterId = 311            
            DataSetReaderDataType dataSetReaderSimple = new DataSetReaderDataType();
            dataSetReaderSimple.Name = "Reader 311";
            dataSetReaderSimple.PublisherId = (UInt16)30;
            dataSetReaderSimple.WriterGroupId = 31;
            dataSetReaderSimple.DataSetWriterId = 311;
            dataSetReaderSimple.Enabled = true;
            dataSetReaderSimple.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderSimple.KeyFrameCount = 1;

            BrokerDataSetReaderTransportDataType brokerTransportSettings = new BrokerDataSetReaderTransportDataType()
            {
                QueueName = brokerQueueName,
                MetaDataQueueName = metaDataQueueName
            };
            dataSetReaderSimple.TransportSettings = new ExtensionObject(brokerTransportSettings);

            UadpDataSetReaderMessageDataType uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                NetworkMessageNumber = 1,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId
                       | UadpNetworkMessageContentMask.GroupHeader
                       | UadpNetworkMessageContentMask.WriterGroupId
                       | UadpNetworkMessageContentMask.PayloadHeader
                       | UadpNetworkMessageContentMask.GroupVersion
                       | UadpNetworkMessageContentMask.NetworkMessageNumber
                       | UadpNetworkMessageContentMask.SequenceNumber),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderSimple.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);            
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderSimple);

            #region Define DataSetReader 'AllTypes' for PublisherId = (UInt16)30, WriterGroupId = 31 DataSetWriterId = 312

            DataSetReaderDataType dataSetReaderAllTypes = new DataSetReaderDataType();
            dataSetReaderAllTypes.Name = "Reader 312";
            dataSetReaderAllTypes.PublisherId = (UInt16)30;
            dataSetReaderAllTypes.WriterGroupId = 31;
            dataSetReaderAllTypes.DataSetWriterId = 312;
            dataSetReaderAllTypes.Enabled = true;
            dataSetReaderAllTypes.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderAllTypes.KeyFrameCount = 1;

            brokerTransportSettings = new BrokerDataSetReaderTransportDataType()
            {
                QueueName = brokerQueueName,
                RequestedDeliveryGuarantee = BrokerTransportQualityOfService.AtLeastOnce,
                MetaDataQueueName = metaDataQueueName
            };

            dataSetReaderAllTypes.TransportSettings = new ExtensionObject(brokerTransportSettings);

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                NetworkMessageNumber = 1,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId
                       | UadpNetworkMessageContentMask.GroupHeader
                       | UadpNetworkMessageContentMask.WriterGroupId
                       | UadpNetworkMessageContentMask.PayloadHeader
                       | UadpNetworkMessageContentMask.GroupVersion
                       | UadpNetworkMessageContentMask.NetworkMessageNumber
                       | UadpNetworkMessageContentMask.SequenceNumber),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderAllTypes.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderAllTypes);

            #region Define DataSetReader 'MassTest' for PublisherId = (UInt16)30, WriterGroupId = 31 DataSetWriterId = 313
            DataSetReaderDataType dataSetReaderMassTest = new DataSetReaderDataType();
            dataSetReaderMassTest.Name = "Reader 313";
            dataSetReaderMassTest.PublisherId = (UInt16)30;
            dataSetReaderMassTest.WriterGroupId = 31;
            dataSetReaderMassTest.DataSetWriterId = 313;
            dataSetReaderMassTest.Enabled = true;
            dataSetReaderMassTest.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReaderMassTest.KeyFrameCount = 1;

            brokerTransportSettings = new BrokerDataSetReaderTransportDataType()
            {
                QueueName = brokerQueueName,
                RequestedDeliveryGuarantee = BrokerTransportQualityOfService.AtLeastOnce,
                MetaDataQueueName = metaDataQueueName
            };
            dataSetReaderMassTest.TransportSettings = new ExtensionObject(brokerTransportSettings);

            uadpDataSetReaderMessage = new UadpDataSetReaderMessageDataType()
            {
                GroupVersion = 0,
                NetworkMessageNumber = 1,
                NetworkMessageContentMask = (uint)(uint)(UadpNetworkMessageContentMask.PublisherId
                       | UadpNetworkMessageContentMask.GroupHeader
                       | UadpNetworkMessageContentMask.WriterGroupId
                       | UadpNetworkMessageContentMask.PayloadHeader
                       | UadpNetworkMessageContentMask.GroupVersion
                       | UadpNetworkMessageContentMask.NetworkMessageNumber
                       | UadpNetworkMessageContentMask.SequenceNumber),
                DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            };
            dataSetReaderMassTest.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);            
            
            #endregion
            readerGroup1.DataSetReaders.Add(dataSetReaderMassTest);
            #endregion

            pubSubConnection1.ReaderGroups.Add(readerGroup1);

            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };

            return pubSubConfiguration;
        }
        #endregion Create configuration objects

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
                    Console.WriteLine("  ReaderGroup Name ='{0}'\t\t\t\t\t-ConfigId={1}, State={2}",
                        readerGroup.Name, configurator.FindIdForObject(readerGroup), configurator.FindStateForObject(readerGroup));
                    foreach (var dataSetReader in readerGroup.DataSetReaders)
                    {
                        Console.WriteLine("    DataSetReader Name ='{0}'\t\t\t\t\t-ConfigId={1}, State={2}",
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

        #endregion Private Methods
    }
}
