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
using Softing.Opc.Ua.PubSub.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SamplePublisher
{
    /// <summary>
    /// Helper class for create/load/save configuration
    /// </summary>
    public static class ConfigurationHelper
    {
        public const int NamespaceIndex = 5;

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
                NetworkMessageContentMask = (uint) // 0x0000003f
                 (UadpNetworkMessageContentMask.PublisherId |
                  UadpNetworkMessageContentMask.GroupHeader |
                  UadpNetworkMessageContentMask.WriterGroupId |
                  UadpNetworkMessageContentMask.GroupVersion |
                  UadpNetworkMessageContentMask.NetworkMessageNumber |
                  UadpNetworkMessageContentMask.SequenceNumber) 
            };

            writerGroup1.MessageSettings = new ExtensionObject(messageSettings);
            DatagramWriterGroupTransportDataType transportSettings = new DatagramWriterGroupTransportDataType();
            writerGroup1.TransportSettings = new ExtensionObject(transportSettings);

            // Define DataSetWriter 'Simple'
            DataSetWriterDataType dataSetWriterSimple = new DataSetWriterDataType();
            dataSetWriterSimple.DataSetWriterId = 1;
            dataSetWriterSimple.Enabled = true;
            dataSetWriterSimple.DataSetFieldContentMask = 0x00000000;
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
            dataSetWriterAllTypes.DataSetFieldContentMask = 0x00000000;
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
            dataSetWriterMassTest.DataSetFieldContentMask = 0x00000000;
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

            pubSubConnection.WriterGroups.Add(writerGroup1);

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
            dataSetWriterSimple2.DataSetFieldContentMask = 0x00000000;
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
            dataSetWriterAllTypes2.DataSetFieldContentMask = 0x00000000;
            dataSetWriterAllTypes2.DataSetName = "AllTypes";
            dataSetWriterAllTypes2.KeyFrameCount = 1;
            uadpDataSetWriterMessage2 = new UadpDataSetWriterMessageDataType()
            {
                DataSetMessageContentMask = 0x00000035,
            };
            dataSetWriterAllTypes2.MessageSettings = new ExtensionObject(uadpDataSetWriterMessage2);
            writerGroup2.DataSetWriters.Add(dataSetWriterAllTypes2);

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
                    new FieldMetaDataEx()
                    {
                        Name = "BooleanValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "Scalar.Int32.X",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "Scalar.Int32.Y",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
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
                    new KeyValuePairEx()
                    {
                        KeyAsQualifiedName =  "BooleanValue",
                        Value = true
                    },
                     new KeyValuePairEx()
                    {
                        KeyAsQualifiedName = "Scalar.Int32.X",
                        Value = (int)100
                    },
                     new KeyValuePairEx()
                    {
                        KeyAsQualifiedName = "Scalar.Int32.Y",
                        Value = (int)50
                    },
                    new KeyValuePairEx()
                    {
                        KeyAsQualifiedName = "DateTimeValue",
                        Value = DateTime.Today
                    }
                };

            PublishedDataItemsDataType publishedDataSetSimpleSource = new PublishedDataItemsDataType();
            publishedDataSetSimpleSource.PublishedData = new PublishedVariableDataTypeCollection();
            //create PublishedData based on metadata names
            foreach (var field in publishedDataSetSimple.DataSetMetaData.Fields)
            {
                publishedDataSetSimpleSource.PublishedData.Add(
                    new PublishedVariableDataTypeEx()
                    {
                        PublishedVariable = new NodeId(field.Name, NamespaceIndex),
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
                    new FieldMetaDataEx()
                    {
                        Name = "BooleanValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Boolean,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "ByteValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Byte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "Int16Value",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "Int32Value",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Int32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "SByteValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.SByte,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "UInt16Value",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.UInt16,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "UInt32Value",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.UInt32,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "FloatValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Float,
                        ValueRank = ValueRanks.Scalar
                    },
                    new FieldMetaDataEx()
                    {
                        Name = "DoubleValue",
                        DataSetFieldId = new Uuid(Guid.NewGuid()),
                        DataType = DataTypeIds.Double,
                        ValueRank = ValueRanks.Scalar
                    }
                };

            PublishedDataItemsDataType publishedDataSetAllTypesSource = new PublishedDataItemsDataType();
            //create PublishedData based on metadata names
            foreach (var field in publishedDataSetAllTypes.DataSetMetaData.Fields)
            {
                publishedDataSetAllTypesSource.PublishedData.Add(
                    new PublishedVariableDataTypeEx()
                    {
                        PublishedVariable = new NodeId(field.Name, NamespaceIndex),
                        AttributeId = Attributes.Value,
                    });
            }
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
                publishedDataSetMassData.DataSetMetaData.Fields.Add(new FieldMetaDataEx()
                {
                    Name = name,
                    DataSetFieldId = new Uuid(Guid.NewGuid()),
                    DataType = DataTypeIds.UInt32,
                    ValueRank = ValueRanks.Scalar
                });

                publishedDataSetMassDataSource.PublishedData.Add(new PublishedVariableDataTypeEx()
                {
                    PublishedVariable = new NodeId(name, NamespaceIndex),
                    AttributeId = Attributes.Value,
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


        /// <summary>
        /// Save a <see cref="PubSubConfigurationDataType"/> instance as XML
        /// </summary>
        /// <param name="pubSubConfiguration"></param>
        /// <param name="filePath"></param>
        public static void SaveConfiguration(PubSubConfigurationDataType pubSubConfiguration, string filePath)
        {
            // Create an XmlRootAttribute.
            XmlRootAttribute root = new XmlRootAttribute("PubSubConfigurationDataType");
            XmlSerializer serializer = new XmlSerializer(typeof(PubSubConfigurationDataType), GetOverrides(),
                new Type[] { typeof(PublishedDataItemsDataType), typeof(NetworkAddressUrlDataType),
                    typeof(DatagramWriterGroupTransportDataType), typeof(UadpWriterGroupMessageDataType),
                    typeof(UadpDataSetWriterMessageDataType ), typeof(QualifiedName), typeof(PublishedVariableDataTypeEx),
                    typeof(KeyValuePairEx), typeof(FieldMetaDataEx)},
                root, Namespaces.OpcUaXsd);

            using (XmlTextWriter t = new XmlTextWriter(filePath, Encoding.Default))
            {
                serializer.Serialize(t, pubSubConfiguration);
            }
        }


        /// <summary>
        /// Save a <see cref="PubSubConfigurationDataType"/> instance as XML
        /// </summary>
        /// <param name="pubSubConfiguration"></param>
        /// <param name="filePath"></param>
        public static PubSubConfigurationDataType LoadConfiguration(string filePath)
        {
            // Create an XmlRootAttribute.
            XmlRootAttribute root = new XmlRootAttribute("PubSubConfigurationDataType");
            XmlSerializer serializer = new XmlSerializer(typeof(PubSubConfigurationDataType), GetOverrides(),
                new Type[] { typeof(PublishedDataItemsDataType), typeof(NetworkAddressUrlDataType),
                    typeof(DatagramWriterGroupTransportDataType), typeof(UadpWriterGroupMessageDataType),
                    typeof(UadpDataSetWriterMessageDataType ), typeof(QualifiedName), typeof(PublishedVariableDataTypeEx),
                    typeof(KeyValuePairEx), typeof(FieldMetaDataEx)},
                root, Namespaces.OpcUaXsd);

            using (XmlTextReader t = new XmlTextReader(filePath))
            {
                return serializer.Deserialize(t) as PubSubConfigurationDataType;
            }

            return null;
        }

        static XmlAttributeOverrides GetOverrides()
        {
            XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
            xmlAttributeOverrides.Add(typeof(EndpointDescription), "ProxyUrl", new XmlAttributes() { XmlIgnore = true });
            xmlAttributeOverrides.Add(typeof(PublishedVariableDataType), "PublishedVariable", new XmlAttributes() { XmlIgnore = true });
            xmlAttributeOverrides.Add(typeof(FieldMetaData), "DataType", new XmlAttributes() { XmlIgnore = true });
            xmlAttributeOverrides.Add(typeof(PublishedDataSetDataType), "Name", new XmlAttributes() { XmlAttribute = new XmlAttributeAttribute() });
            xmlAttributeOverrides.Add(typeof(PubSubConnectionDataType), "Name", new XmlAttributes() { XmlAttribute = new XmlAttributeAttribute() });

            xmlAttributeOverrides.Add(typeof(WriterGroupDataType), "WriterGroupId", new XmlAttributes() { XmlAttribute = new XmlAttributeAttribute() });
            xmlAttributeOverrides.Add(typeof(DataSetWriterDataType), "DataSetWriterId", new XmlAttributes() { XmlAttribute = new XmlAttributeAttribute() });
            xmlAttributeOverrides.Add(typeof(DataSetWriterDataType), "DataSetName", new XmlAttributes() { XmlAttribute = new XmlAttributeAttribute() });
            return xmlAttributeOverrides;
        }
    }
}
