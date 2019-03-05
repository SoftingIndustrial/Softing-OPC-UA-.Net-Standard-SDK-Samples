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
using System.Collections.Generic;
using System.Text;
using Opc.Ua;
using Softing.Opc.Ua.PubSub;

namespace SamplePublisher
{
    static class Program
    {
        /// <summary>
        /// Entry point for application
        /// </summary>
        static void Main()
        {
            try
            {	
                // Create the PubSub application
                UaPubSubApplication pubSubApplication = new UaPubSubApplication();                

                // Define a PubSub connection
                PubSubConnectionDataType pubSubConnection = new PubSubConnectionDataType();
                pubSubConnection.Name = "UDPConection1";
                pubSubConnection.Enabled = true;
                pubSubConnection.PublisherId = (UInt16)10;
                pubSubConnection.TransportProfileUri = "http://opcfoundation.org/UA-Profile/Transport/pubsub-udp-uadp";
                NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
                address.Url = "opc.udp://239.0.0.1:4840";
                pubSubConnection.Address = new ExtensionObject(address);

                // Define a WriterGroup
                WriterGroupDataType writerGroup = new WriterGroupDataType();
                writerGroup.Enabled = true;
                writerGroup.WriterGroupId = 1;
                writerGroup.PublishingInterval = 5000;
                writerGroup.KeepAliveTime = 1500;
                writerGroup.HeaderLayoutUri = "UADP-Cyclic-Fixed";

                // Define a DataSetWriter
                DataSetWriterDataType dataSetWriter = new DataSetWriterDataType();
                dataSetWriter.Enabled = true;
                dataSetWriter.DataSetName = "DataSetWriterSimple";
                dataSetWriter.DataSetWriterId = 1;
                dataSetWriter.KeyFrameCount = 1;

                writerGroup.DataSetWriters.Add(dataSetWriter);
                pubSubConnection.WriterGroups.Add(writerGroup);

                //Define a PublishedDataSet
                PublishedDataSetDataType publishedDataSet1 = new PublishedDataSetDataType();
                publishedDataSet1.Name = "Simple"; //name shall be unique in a configuration
                // Define  publishedDataSet1.DataSetMetaData
                publishedDataSet1.DataSetMetaData = new DataSetMetaDataType();
                publishedDataSet1.DataSetMetaData.Name = publishedDataSet1.Name;
                publishedDataSet1.DataSetMetaData.Fields = new FieldMetaDataCollection()
                {
                    new FieldMetaData()
                    {
                        Name = "BooleanValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Scalar.Int32.X",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Scalar.Int32.Y",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "DateTimeValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.DateTime,
                        ValueRank = ValueRanks.Scalar
                    }
                };
                //initialize Extension fields collection
                publishedDataSet1.ExtensionFields = new KeyValuePairCollection()
                {
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("BooleanValue"),
                        Value = true
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("Scalar.Int32.X"),
                        Value = (int)100
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("Scalar.Int32.Y"),
                        Value = (int)50
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("DateTimeValue"),
                        Value = DateTime.Today
                    }
                };

                PublishedDataItemsDataType publishedDataSetSource = new PublishedDataItemsDataType();
                publishedDataSetSource.PublishedData = new PublishedVariableDataTypeCollection()
                {
                    new PublishedVariableDataType()
                    {
                        SubstituteValue = new QualifiedName("BooleanValue")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("Scalar.Int32.X")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("Scalar.Int32.Y")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("DateTimeValue")
                    }
                };
                publishedDataSet1.DataSetSource = new ExtensionObject(publishedDataSetSource);   

                //create  pub sub configuration root object
                PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
                pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection
                };
                pubSubConfiguration.PublishedDataSets = new PublishedDataSetDataTypeCollection()
                {
                    publishedDataSet1
                };


                // Add the connection to the application
                pubSubApplication.LoadConfiguration(pubSubConfiguration);

                Console.WriteLine("Publisher started");
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
                //pubSubApplication.Stop();
            }
        }

        private static void PrintCommandParameters()
        {
            Console.WriteLine("Press:\n\ts: connections status");
            Console.WriteLine("\tx,q: shutdown the server\n\n");
        }	
    }
}
