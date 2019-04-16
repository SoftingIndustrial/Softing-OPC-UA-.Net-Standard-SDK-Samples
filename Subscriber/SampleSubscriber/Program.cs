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
using System.Text;

namespace SampleSubscriber
{
    static class Program
    {
        // It should match the namespace index from configuration file
        public const ushort NamespaceIndexSimple = 2;
        public const ushort NamespaceIndexAllTypes = 3;
        public const ushort NamespaceIndexMassTest = 4;
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

                // PubSubConfigurationDataType config = CreateConfiguration();
                string configurationFileName = "SampleSubscriber.Config.xml";
                // UaPubSubConfigurationHelper.SaveConfiguration(config, configurationFileName);
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
                                Console.WriteLine("\tConnection '{0}' - Running={1}, PublishersCount={2}",
                                    connection.PubSubConnectionConfiguration.Name, connection.IsRunning, connection.Publishers.Count);
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
            Console.WriteLine("Data Arrived, DataSet count = {0}", e.DataSets.Count);
            int index = 0;
            foreach(DataSet dataSet in e.DataSets)
            {
                Console.WriteLine("\tDataSet {0}, Name = {1}", index++, dataSet.Name);
                for(int i =0; i < dataSet.Fields.Length; i++)
                {
                    Console.WriteLine("\t\tTargetNodeId: {0}, Attribute: {1}, Value: {2}", 
                        dataSet.Fields[i].TargetNodeId, dataSet.Fields[i].TargetAttribute, dataSet.Fields[i].Value);
                }
            }
            Console.WriteLine("------------------------------------------------");
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

            #region Define ReaderGroup
            ReaderGroupDataType readerGroup1 = new ReaderGroupDataType();
            readerGroup1.Enabled = true;
            readerGroup1.MaxNetworkMessageSize = 1500;       
            readerGroup1.MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType());
            readerGroup1.TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType());

            // Define DataSetReader 'Simple'
            DataSetReaderDataType dataSetReader1 = new DataSetReaderDataType();
            dataSetReader1.PublisherId = (UInt16)10;
            dataSetReader1.WriterGroupId = 1;
            dataSetReader1.DataSetWriterId = 1;
            dataSetReader1.Enabled = true;
            dataSetReader1.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            dataSetReader1.KeyFrameCount = 1;
            // Define  dataSetReader1.DataSetMetaData
            dataSetReader1.DataSetMetaData = new DataSetMetaDataType();           
            dataSetReader1.DataSetMetaData.DataSetClassId = new Uuid(Guid.Empty);
            dataSetReader1.DataSetMetaData.Name = "Simple";
            dataSetReader1.DataSetMetaData.Fields = new FieldMetaDataCollection()
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
            dataSetReader1.DataSetMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = 1,
                MajorVersion = 1
            };

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
            dataSetReader1.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            TargetVariablesDataType subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection()
            {
                new FieldTargetDataType()
                {
                    DataSetFieldId = dataSetReader1.DataSetMetaData.Fields[0].DataSetFieldId,
                    TargetNodeId = new NodeId(dataSetReader1.DataSetMetaData.Fields[0].Name, NamespaceIndexSimple),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue, 
                    OverrideValue = true
                },
                 new FieldTargetDataType()
                {
                    DataSetFieldId = dataSetReader1.DataSetMetaData.Fields[1].DataSetFieldId,
                    TargetNodeId = new NodeId(dataSetReader1.DataSetMetaData.Fields[1].Name, NamespaceIndexSimple),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = 111
                },
                  new FieldTargetDataType()
                {
                    DataSetFieldId = dataSetReader1.DataSetMetaData.Fields[2].DataSetFieldId,
                    TargetNodeId = new NodeId(dataSetReader1.DataSetMetaData.Fields[2].Name, NamespaceIndexSimple),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = 5555
                },
                   new FieldTargetDataType()
                {
                    DataSetFieldId = dataSetReader1.DataSetMetaData.Fields[3].DataSetFieldId,
                    TargetNodeId = new NodeId(dataSetReader1.DataSetMetaData.Fields[3].Name, NamespaceIndexSimple),
                    AttributeId = Attributes.Value,
                    OverrideValueHandling = OverrideValueHandling.OverrideValue,
                    OverrideValue = DateTime.MinValue
                },
            };

            dataSetReader1.SubscribedDataSet = new ExtensionObject(subscribedDataSet);
            readerGroup1.DataSetReaders.Add(dataSetReader1);
            pubSubConnection1.ReaderGroups.Add(readerGroup1);

            //// Define DataSetWriter 'AllTypes'
            //DataSetWriterDataType dataSetWriter2 = new DataSetWriterDataType();
            //dataSetWriter2.DataSetWriterId = 2;
            //dataSetWriter2.Enabled = true;
            //dataSetWriter2.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            //dataSetWriter2.DataSetName = "AllTypes";
            //dataSetWriter2.KeyFrameCount = 1;
            //uadpDataSetReaderMessage = new UadpDataSetWriterMessageDataType()
            //{
            //    ConfiguredSize = 32,
            //    DataSetOffset = 37,
            //    NetworkMessageNumber = 1,
            //    DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            //};
            //dataSetWriter2.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            //readerGroup1.DataSetWriters.Add(dataSetWriter2);

            //// Define DataSetWriter 'MassTest'
            //DataSetWriterDataType dataSetWriter3 = new DataSetWriterDataType();
            //dataSetWriter3.DataSetWriterId = 3;
            //dataSetWriter3.Enabled = true;
            //dataSetWriter3.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
            //dataSetWriter3.DataSetName = "MassTest";
            //dataSetWriter3.KeyFrameCount = 1;
            //uadpDataSetReaderMessage = new UadpDataSetWriterMessageDataType()
            //{
            //    ConfiguredSize = 405,
            //    DataSetOffset = 69,
            //    NetworkMessageNumber = 1,
            //    DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Status | UadpDataSetMessageContentMask.SequenceNumber),
            //};
            //dataSetWriter3.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            //readerGroup1.DataSetWriters.Add(dataSetWriter3);

            //pubSubConnection1.WriterGroups.Add(readerGroup1);
            //#endregion

            //// Define a PubSub connection with PublisherId 10
            //PubSubConnectionDataType pubSubConnection2 = new PubSubConnectionDataType();
            //pubSubConnection2.Name = "UADPConection1";
            //pubSubConnection2.Enabled = true;
            //pubSubConnection2.PublisherId = (UInt64)20;
            //pubSubConnection2.TransportProfileUri = "http://opcfoundation.org/UA-Profile/Transport/pubsub-udp-uadp";
            //address = new NetworkAddressUrlDataType();
            //address.Url = "opc.udp://239.0.0.1:4840";
            //pubSubConnection2.Address = new ExtensionObject(address);

            //#region Define WriterGroup2
            //WriterGroupDataType writerGroup2 = new WriterGroupDataType();
            //writerGroup2.Enabled = true;
            //writerGroup2.WriterGroupId = 2;
            //writerGroup2.PublishingInterval = 5000;
            //writerGroup2.KeepAliveTime = 5000;
            //writerGroup2.MaxNetworkMessageSize = 1500;
            //writerGroup2.HeaderLayoutUri = "UADP-Dynamic";
            //messageSettings = new UadpWriterGroupMessageDataType()
            //{
            //    DataSetOrdering = DataSetOrderingType.Undefined,
            //    GroupVersion = 0,
            //    NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.PayloadHeader)
            //};

            //writerGroup2.MessageSettings = new ExtensionObject(messageSettings);
            //writerGroup2.TransportSettings = new ExtensionObject(new DatagramWriterGroupTransportDataType());

            //// Define DataSetWriter 'Simple'
            //DataSetWriterDataType dataSetWriter11 = new DataSetWriterDataType();
            //dataSetWriter11.DataSetWriterId = 11;
            //dataSetWriter11.Enabled = true;
            //dataSetWriter11.DataSetFieldContentMask = (uint)DataSetFieldContentMask.None; //Variant encoding
            //dataSetWriter11.DataSetName = "Simple";
            //dataSetWriter11.KeyFrameCount = 1;
            //uadpDataSetReaderMessage = new UadpDataSetWriterMessageDataType()
            //{
            //    //DataValue Encoding
            //    DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
            //            | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            //};
            //dataSetWriter11.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            //writerGroup2.DataSetWriters.Add(dataSetWriter11);

            //// Define DataSetWriter 'AllTypes'
            //DataSetWriterDataType dataSetWriter12 = new DataSetWriterDataType();
            //dataSetWriter12.DataSetWriterId = 12;
            //dataSetWriter12.Enabled = true;
            //dataSetWriter12.DataSetFieldContentMask = (uint)DataSetFieldContentMask.None; //Variant encoding
            //dataSetWriter12.DataSetName = "AllTypes";
            //dataSetWriter12.KeyFrameCount = 1;
            //uadpDataSetReaderMessage = new UadpDataSetWriterMessageDataType()
            //{
            //    DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
            //            | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            //};
            //dataSetWriter12.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            //writerGroup2.DataSetWriters.Add(dataSetWriter12);

            //// Define DataSetWriter 'MassTest'
            //DataSetWriterDataType dataSetWriter13 = new DataSetWriterDataType();
            //dataSetWriter13.DataSetWriterId = 13;
            //dataSetWriter13.Enabled = true;
            //dataSetWriter13.DataSetFieldContentMask = (uint)DataSetFieldContentMask.None; //Variant encoding
            //dataSetWriter13.DataSetName = "MassTest";
            //dataSetWriter13.KeyFrameCount = 1;
            //uadpDataSetReaderMessage = new UadpDataSetWriterMessageDataType()
            //{
            //    //DataValue Encoding
            //    DataSetMessageContentMask = (uint)(UadpDataSetMessageContentMask.Timestamp | UadpDataSetMessageContentMask.Status
            //            | UadpDataSetMessageContentMask.MinorVersion | UadpDataSetMessageContentMask.SequenceNumber),
            //};
            //dataSetWriter13.MessageSettings = new ExtensionObject(uadpDataSetReaderMessage);
            //writerGroup2.DataSetWriters.Add(dataSetWriter13);

            //pubSubConnection2.WriterGroups.Add(writerGroup2);
            #endregion            

            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection1
                };           

            return pubSubConfiguration;
        }
        #endregion

        /// <summary>
        /// Print command line parameters for this console application
        /// </summary>
        private static void PrintCommandParameters()
        {
            Console.WriteLine("Press:\n\ts: connections status");
            Console.WriteLine("\tx,q: shutdown the server\n\n");
        }
    }
}
