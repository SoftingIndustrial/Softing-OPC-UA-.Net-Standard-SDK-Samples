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
    public static class Program
    {
        /// <summary>
        /// Entry point for application
        /// </summary>
        static void Main()
        {
            try
            {
                PubSubConfigurationDataType pubSubConfiguration = CreateConfiguration();
                                
                // Create the PubSub application
                UaPubSubApplication pubSubApplication = new UaPubSubApplication();
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


        /// <summary>
        /// Create static pubsub configuration
        /// </summary>
        /// <returns></returns>
        public static PubSubConfigurationDataType CreateConfiguration()
        {
            // Define a PubSub connection
            PubSubConnectionDataType pubSubConnection = new PubSubConnectionDataType();
            pubSubConnection.Name = "UDPConection1";
            pubSubConnection.Enabled = true;
            pubSubConnection.PublisherId = (UInt16)10;
            pubSubConnection.TransportProfileUri = "http://opcfoundation.org/UA-Profile/Transport/pubsub-udp-uadp";
            NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
            address.Url = "opc.udp://239.0.0.1:4840";
            pubSubConnection.Address = new ExtensionObject(address);

            // Define a WriterGroup - UADP-Cyclic-Fixed
            WriterGroupDataType writerGroup1 = new WriterGroupDataType();
            writerGroup1.Enabled = true;
            writerGroup1.WriterGroupId = 1;
            writerGroup1.PublishingInterval = 5000; //in pdf config value is 100
            writerGroup1.KeepAliveTime = 5000;
            writerGroup1.MaxNetworkMessageSize = 1500;
            writerGroup1.HeaderLayoutUri = "UADP-Cyclic-Fixed"; //todo: investigate whast this setting does 
            UadpWriterGroupMessageDataType messageSettings = new UadpWriterGroupMessageDataType()
            {
                DataSetOrdering = DataSetOrderingType.AscendingWriterId,
                GroupVersion = 0,
                    NetworkMessageContentMask = 0x0000003ff
            };
            writerGroup1.MessageSettings = new ExtensionObject(messageSettings);
            DatagramWriterGroupTransportDataType transportSettings = new DatagramWriterGroupTransportDataType();
            writerGroup1.TransportSettings = new ExtensionObject(transportSettings);

            // Define DataSetWriter 'Simple'
            DataSetWriterDataType dataSetWriterSimple = new DataSetWriterDataType();
            dataSetWriterSimple.DataSetWriterId = 1;
            dataSetWriterSimple.Enabled = true;
            dataSetWriterSimple.DataSetFieldContentMask = 0x00000020;
            dataSetWriterSimple.DataSetName = "Simple";
            dataSetWriterSimple.KeyFrameCount = 1;
            UadpDataSetWriterMessageDataType uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = 0x00000024,
                ConfiguredSize = 22,
                DataSetOffset = 15,
                NetworkMessageNumber = 1
            };
            dataSetWriterSimple.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup1.DataSetWriters.Add(dataSetWriterSimple);

            // Define DataSetWriter 'AllTypes'
            DataSetWriterDataType dataSetWriterAllTypes = new DataSetWriterDataType();
            dataSetWriterAllTypes.DataSetWriterId = 2;
            dataSetWriterAllTypes.Enabled = true;
            dataSetWriterAllTypes.DataSetFieldContentMask = 0x00000020;
            dataSetWriterAllTypes.DataSetName = "AllTypes";
            dataSetWriterAllTypes.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = 0x00000024,
                ConfiguredSize = 32,
                DataSetOffset = 37,
                NetworkMessageNumber = 1
            };
            dataSetWriterAllTypes.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup1.DataSetWriters.Add(dataSetWriterAllTypes);

            // Define DataSetWriter 'MassTest'
            DataSetWriterDataType dataSetWriterMassTest = new DataSetWriterDataType();
            dataSetWriterMassTest.DataSetWriterId = 3;
            dataSetWriterMassTest.Enabled = true;
            dataSetWriterMassTest.DataSetFieldContentMask = 0x00000020;
            dataSetWriterMassTest.DataSetName = "MassData";
            dataSetWriterMassTest.KeyFrameCount = 1;
            uadpDataSetWriterMessage = new UadpDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = 0x00000024,
                ConfiguredSize = 405,
                DataSetOffset = 69,
                NetworkMessageNumber = 1
            };
            dataSetWriterMassTest.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage);
            writerGroup1.DataSetWriters.Add(dataSetWriterMassTest);

            //  pubSubConnection.WriterGroups.Add(writerGroup1);


            // Define a WriterGroup - UADP-Dynamic
            WriterGroupDataType writerGroup2 = new WriterGroupDataType();
            writerGroup2.Enabled = true;
            writerGroup2.WriterGroupId = 2;
            writerGroup2.PublishingInterval = 5000;//in pdf config value is 100
            writerGroup2.KeepAliveTime = 5000;
            writerGroup2.MaxNetworkMessageSize = 1500;
            writerGroup2.HeaderLayoutUri = "UADP-Cyclic-Fixed"; //todo: investigate whast this setting does 
            UadpWriterGroupMessageDataType messageSettings2 = new UadpWriterGroupMessageDataType()
            {
                DataSetOrdering = DataSetOrderingType.AscendingWriterId,
                GroupVersion = 0,
                NetworkMessageContentMask = 0x00000043
            };
            writerGroup2.MessageSettings = new ExtensionObject(messageSettings2);
            DatagramWriterGroupTransportDataType transportSettings2 = new DatagramWriterGroupTransportDataType();
            writerGroup2.TransportSettings = new ExtensionObject(transportSettings2);

            // Define DataSetWriter 'Simple'
            DataSetWriterDataType dataSetWriterSimple2 = new DataSetWriterDataType();
            dataSetWriterSimple2.DataSetWriterId = 11;
            dataSetWriterSimple2.Enabled = true;
                dataSetWriterSimple2.DataSetFieldContentMask = 0;
            dataSetWriterSimple2.DataSetName = "Simple";
            dataSetWriterSimple2.KeyFrameCount = 1;
            UadpDataSetWriterMessageDataType uadpDataSetWriterMessage2 = new UadpDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = 0x00000035,
            };
            dataSetWriterSimple2.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage2);
            writerGroup2.DataSetWriters.Add(dataSetWriterSimple2);

            // Define DataSetWriter 'AllTypes'
            DataSetWriterDataType dataSetWriterAllTypes2 = new DataSetWriterDataType();
            dataSetWriterAllTypes2.DataSetWriterId = 12;
            dataSetWriterAllTypes2.Enabled = true;
            dataSetWriterAllTypes2.DataSetFieldContentMask = 0;
            dataSetWriterAllTypes2.DataSetName = "AllTypes";
            dataSetWriterAllTypes2.KeyFrameCount = 1;
            uadpDataSetWriterMessage2 = new UadpDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = 0x00000035,
            };
            dataSetWriterAllTypes.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage2);
            writerGroup2.DataSetWriters.Add(dataSetWriterAllTypes);

            // Define DataSetWriter 'MassTest'
            DataSetWriterDataType dataSetWriterMassTest2 = new DataSetWriterDataType();
            dataSetWriterMassTest2.DataSetWriterId = 13;
            dataSetWriterMassTest2.Enabled = true;
                dataSetWriterMassTest2.DataSetFieldContentMask = 0;
            dataSetWriterMassTest2.DataSetName = "MassData";
            dataSetWriterMassTest2.KeyFrameCount = 1;
            uadpDataSetWriterMessage2 = new UadpDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = 0x00000035,
            };
            dataSetWriterMassTest2.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage2);
            writerGroup2.DataSetWriters.Add(dataSetWriterMassTest2);

            pubSubConnection.WriterGroups.Add(writerGroup2);

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
            publishedDataSetSimple.ExtensionFields = new KeyValuePairCollection()
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

            PublishedDataItemsDataType publishedDataSetSimpleSource = new PublishedDataItemsDataType();
            publishedDataSetSimpleSource.PublishedData = new PublishedVariableDataTypeCollection()
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
                        Name = "BooleanValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "ByteValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Byte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int16Value",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "Int32Value",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "SByteValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.SByte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt16Value",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.UInt16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "UInt32Value",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.UInt32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "FloatValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Float,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaData()
                    {
                        Name = "DoubleValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Double,
                        ValueRank = ValueRanks.Scalar
                    }
                };
            //initialize Extension fields collection
            publishedDataSetAllTypes.ExtensionFields = new KeyValuePairCollection()
                {
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("BooleanValue"),
                        Value = true
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("ByteValue"),
                        Value = (byte)100
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("Int16Value"),
                        Value = (short)50
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("Int32Value"),
                        Value = (int)1
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("SByteValue"),
                        Value = (sbyte)11
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("UInt16Value"),
                        Value = (ushort)111
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("UInt32Value"),
                        Value = (uint)1111
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("FloatValue"),
                        Value = (float)1.1
                    },
                    new Opc.Ua.KeyValuePair()
                    {
                        Key =  new QualifiedName("DoubleValue"),
                        Value = (double)1.11
                    }
                };

            PublishedDataItemsDataType publishedDataSetAllTypesSource = new PublishedDataItemsDataType();
            publishedDataSetAllTypesSource.PublishedData = new PublishedVariableDataTypeCollection()
                {
                    new PublishedVariableDataType()
                    {
                        SubstituteValue = new QualifiedName("BooleanValue")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("ByteValue")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("Int16Value")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("Int32Value")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("SByteValue")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("UInt16Value")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("UInt32Value")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("FloatValue")
                    },
                    new PublishedVariableDataType()
                    {
                        SubstituteValue =  new QualifiedName("DoubleValue")
                    },
                };
            publishedDataSetAllTypes.DataSetSource = new ExtensionObject(publishedDataSetAllTypesSource);
            #endregion

            #region  Define PublishedDataSet MassData
            PublishedDataSetDataType publishedDataSetMassData = new PublishedDataSetDataType();
            publishedDataSetMassData.Name = "MassData"; //name shall be unique in a configuration
                                                        // Define  publishedDataSetMassData.DataSetMetaData
            publishedDataSetMassData.DataSetMetaData = new DataSetMetaDataType();
            publishedDataSetMassData.DataSetMetaData.DataSetClassId = new Uuid(Guid.Empty);
            publishedDataSetMassData.DataSetMetaData.Name = publishedDataSetAllTypes.Name;
            publishedDataSetMassData.DataSetMetaData.Fields = new FieldMetaDataCollection();
            //initialize Extension fields collection
            publishedDataSetMassData.ExtensionFields = new KeyValuePairCollection();
            PublishedDataItemsDataType publishedDataSetMassDataSource = new PublishedDataItemsDataType();
            publishedDataSetMassDataSource.PublishedData = new PublishedVariableDataTypeCollection();
            for (int i = 0; i < 100; i++)
            {
                string name = "Value" + i;
                publishedDataSetMassData.DataSetMetaData.Fields.Add(new FieldMetaData()
                {
                    Name = name,
                    DataSetFieldId = new Uuid(Guid.NewGuid()),
                    DataType = DataTypeIds.UInt32,
                    ValueRank = ValueRanks.Scalar
                });

                publishedDataSetMassData.ExtensionFields.Add(new Opc.Ua.KeyValuePair()
                {
                    Key = new QualifiedName(name),
                    Value = (uint)i
                });

                publishedDataSetMassDataSource.PublishedData.Add(new PublishedVariableDataType()
                {
                    SubstituteValue = new QualifiedName(name)
                });
            }
            publishedDataSetMassData.DataSetSource = new ExtensionObject(publishedDataSetMassDataSource);
            #endregion

            //create  pub sub configuration root object
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();
            pubSubConfiguration.Connections = new PubSubConnectionDataTypeCollection()
                {
                    pubSubConnection
                };
            pubSubConfiguration.PublishedDataSets = new PublishedDataSetDataTypeCollection()
                {
                    publishedDataSetSimple, publishedDataSetAllTypes, publishedDataSetMassData
                };
            return pubSubConfiguration;
        } 
    }
}
