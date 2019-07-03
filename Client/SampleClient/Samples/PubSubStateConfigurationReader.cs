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
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.PubSub.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleClient.Samples
{
    class PubSubStateCfgReader
    {
        #region Public Methods
        public static void PubSubConfigurationRead(UaPubSubConfigurator pubSubConfigurator, ClientSession clientSession)
        {

            NodeId publishSubscribeBase = new NodeId("ns=0;i=14443");
            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();

            IList<ReferenceDescriptionEx> referenceDescriptions = clientSession.Browse(publishSubscribeBase);

            // Set the state from Status Node 
            var enabledStateNode = (from refDsc in referenceDescriptions
                                    where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                          (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                    select refDsc).First();

            ReadValueId readValue = new ReadValueId();
            NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
            IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { "State" });
            readValue.NodeId = translateResults?.First();
            readValue.AttributeId = Attributes.Value;

            DataValueEx dataValue = clientSession.Read(readValue);
            PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

            pubSubConfiguration.Enabled = pubSubStateValue != PubSubState.Disabled;


            // Handle Connections
            IEnumerable<ReferenceDescriptionEx> connectionNodes = from refDsc in referenceDescriptions
                                                                  where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                                        (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubConnectionType
                                                                  select refDsc;

            foreach (var referenceDescription in connectionNodes)
            {
                HandleAddConnection(pubSubConfiguration, clientSession, referenceDescription);
            }

            // Handle the published data
            HandleAddPublishedDataSets(pubSubConfiguration, clientSession, referenceDescriptions);

            pubSubConfigurator.LoadConfiguration(pubSubConfiguration);
        }


        #endregion

        #region Private Methods
        private static void HandleAddPublishedDataSets(PubSubConfigurationDataType pubSubConfiguration, ClientSession clientSession, IList<ReferenceDescriptionEx> referenceDescriptions)
        {
            ReferenceDescriptionEx publishedDataSetsNode = (from refDsc in referenceDescriptions
                                                            where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                                  (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.DataSetFolderType
                                                            select refDsc).First();
            NodeId publishedDataSetFolderNodeId = (NodeId)publishedDataSetsNode.NodeId;
            var publishedDataSetFoldersReferenceDescriptions = clientSession.Browse(publishedDataSetFolderNodeId);


            var publishedDataItemsReferenceDescriptions = from refDsc in publishedDataSetFoldersReferenceDescriptions
                                                          where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                                (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PublishedDataItemsType
                                                          select refDsc;

            foreach (var publishedDataItemReference in publishedDataItemsReferenceDescriptions)
            {
                NodeId publishedDataItemNodeId = (NodeId)publishedDataItemReference.NodeId;

                //Read ConfigurationVersion, DataSetMetaData and PublishedData
                var translateResults = clientSession.TranslateBrowsePathToNodeIds(publishedDataItemNodeId, new List<QualifiedName> { "ConfigurationVersion" });
                var readConfigurationVersion = new ReadValueId();
                readConfigurationVersion.NodeId = translateResults?.First();
                readConfigurationVersion.AttributeId = Attributes.Value;

                translateResults = clientSession.TranslateBrowsePathToNodeIds(publishedDataItemNodeId, new List<QualifiedName> { "DataSetMetaData" });
                var readDataSetMetaData = new ReadValueId();
                readDataSetMetaData.NodeId = translateResults?.First();
                readDataSetMetaData.AttributeId = Attributes.Value;

                translateResults = clientSession.TranslateBrowsePathToNodeIds(publishedDataItemNodeId, new List<QualifiedName> { "PublishedData" });
                var readPublishedData = new ReadValueId();
                readPublishedData.NodeId = translateResults?.First();
                readPublishedData.AttributeId = Attributes.Value;

                var results = clientSession.Read(new List<ReadValueId> { readConfigurationVersion, readDataSetMetaData, readPublishedData });
                ConfigurationVersionDataType configurationVersionDataType = (ConfigurationVersionDataType)((ExtensionObject)results?.ElementAt(0).Value).Body;
                DataSetMetaDataType dataSetMetaDataValue = (DataSetMetaDataType)((ExtensionObject)results?.ElementAt(1).Value).Body;

                PublishedVariableDataTypeCollection publishedVariables = new PublishedVariableDataTypeCollection();
                foreach (ExtensionObject publishedVariableDataType in (ExtensionObject[])results?.ElementAt(2).Value)
                {
                    publishedVariables.Add((PublishedVariableDataType)publishedVariableDataType.Body);
                }

                PublishedDataSetDataType publishedDataSetDataType = new PublishedDataSetDataType
                {
                    Name = publishedDataItemReference.BrowseName.Name,
                    DataSetMetaData = dataSetMetaDataValue,
                    DataSetSource = new ExtensionObject(new PublishedDataItemsDataType
                    {
                        PublishedData = publishedVariables,
                    }),
                };

                pubSubConfiguration.PublishedDataSets.Add(publishedDataSetDataType);
            }
        }
        private static void HandleAddConnection(PubSubConfigurationDataType pubSubConfiguration, ClientSession clientSession, ReferenceDescriptionEx referenceDescription)
        {
            NodeId nodeId = new NodeId(referenceDescription.NodeId.Identifier, referenceDescription.NodeId.NamespaceIndex);
            var connectionReferenceDescriptions = clientSession.Browse(nodeId);

            // Read Enabled State from Status Node  
            var enabledStateNode = (from refDsc in connectionReferenceDescriptions
                                    where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                          (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                    select refDsc).First();

            ReadValueId readValue = new ReadValueId();
            NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
            IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { "State" });
            readValue.NodeId = translateResults?.First();
            readValue.AttributeId = Attributes.Value;

            DataValueEx dataValue = clientSession.Read(readValue);
            PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

            // Read PublisherId
            var publisherIdNodeId = (from refDsc in connectionReferenceDescriptions
                                     where refDsc.BrowseName.Name.Equals("PublisherId")
                                     select refDsc).First();
            readValue = new ReadValueId();
            readValue.NodeId = (NodeId)publisherIdNodeId.NodeId;
            readValue.AttributeId = Attributes.Value;

            dataValue = clientSession.Read(readValue);
            var publisherIdValue = dataValue.Value.GetType() == typeof(UInt16) ? (UInt16)dataValue.Value : (UInt64)dataValue.Value;

            // Read TransportProfileUri
            var transportProfileUriNodeId = (from refDsc in connectionReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals("TransportProfileUri")
                                             select refDsc).First();
            readValue = new ReadValueId();
            readValue.NodeId = (NodeId)transportProfileUriNodeId.NodeId;
            readValue.AttributeId = Attributes.Value;

            dataValue = clientSession.Read(readValue);
            string transportProfileUriValue = (string)dataValue.Value;

            // Read NetworkInterface and Url from AddressNode
            var addressNode = (from refDsc in connectionReferenceDescriptions
                               where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                     (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.NetworkAddressUrlType
                               select refDsc).First();

            NodeId addressNodeNodeId = (NodeId)addressNode.NodeId;

            translateResults = clientSession.TranslateBrowsePathToNodeIds(addressNodeNodeId, new List<QualifiedName> { "NetworkInterface" });
            var readValueNetworkInterface = new ReadValueId();
            readValueNetworkInterface.NodeId = translateResults?.First();
            readValueNetworkInterface.AttributeId = Attributes.Value;

            translateResults = clientSession.TranslateBrowsePathToNodeIds(addressNodeNodeId, new List<QualifiedName> { "Url" });
            var readValueUrl = new ReadValueId();
            readValueUrl.NodeId = translateResults?.First();
            readValueUrl.AttributeId = Attributes.Value;

            var results = clientSession.Read(new List<ReadValueId> { readValueNetworkInterface, readValueUrl });
            string networkIfValue = (string)results?.ElementAt(0).Value;
            string urlValue = (string)results?.ElementAt(1).Value;

            PubSubConnectionDataType pubSubConnectionDataType = new PubSubConnectionDataType
            {
                Name = referenceDescription.BrowseName.Name,
                Enabled = pubSubStateValue != PubSubState.Disabled,
                PublisherId = publisherIdValue,
                TransportProfileUri = transportProfileUriValue,
                Address = new ExtensionObject(new NetworkAddressUrlDataType
                {
                    NetworkInterface = networkIfValue,
                    Url = urlValue,
                })

            };
            pubSubConfiguration.Connections.Add(pubSubConnectionDataType);

            HandleAddWritterGroups(pubSubConnectionDataType, clientSession, connectionReferenceDescriptions);
            HandleAddReaderGroups(pubSubConnectionDataType, clientSession, connectionReferenceDescriptions);

        }

        private static void HandleAddReaderGroups(PubSubConnectionDataType pubSubConnectionDataType, ClientSession clientSession, IList<ReferenceDescriptionEx> connectionReferenceDescriptions)
        {

            // Read ReaderGroups
            var readerGroupsReferences = from refDsc in connectionReferenceDescriptions
                                         where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                               (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.ReaderGroupType
                                         select refDsc;
            foreach (var readerGroupReference in readerGroupsReferences)
            {
                NodeId readerGroupNodeId = (NodeId)readerGroupReference.NodeId;
                var readerGroupReferenceDescriptions = clientSession.Browse(readerGroupNodeId);

                // Read Enabled State from Status Node  
                var enabledStateNode = (from refDsc in readerGroupReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                        select refDsc).First();

                ReadValueId readValue = new ReadValueId();
                NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
                IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { "State" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                DataValueEx dataValue = clientSession.Read(readValue);
                PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

                // Read MaxNetworkMessageSize
                var maxNetworkMessageSizeNodeId = (from refDsc in readerGroupReferenceDescriptions
                                                   where refDsc.BrowseName.Name.Equals("MaxNetworkMessageSize")
                                                   select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)maxNetworkMessageSizeNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint maxNetworkMessageSizeValue = (uint)dataValue.Value;


                ReaderGroupDataType readerGroup = new ReaderGroupDataType
                {
                    Name = readerGroupReference.BrowseName.Name,
                    Enabled = pubSubStateValue != PubSubState.Disabled,
                    MaxNetworkMessageSize = maxNetworkMessageSizeValue,
                    MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType()),
                };

                pubSubConnectionDataType.ReaderGroups.Add(readerGroup);
                
                HandleAddReaders(readerGroup, clientSession, readerGroupReferenceDescriptions);

            }
        }

        private static void HandleAddReaders(ReaderGroupDataType readerGroup, ClientSession clientSession, IList<ReferenceDescriptionEx> readerGroupReferenceDescriptions)
        {
            // Read DataSetReaders
            var dataSetReaderReferences = from refDsc in readerGroupReferenceDescriptions
                                          where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                      (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.DataSetReaderType
                                          select refDsc;
            foreach (var dataSetReaderReference in dataSetReaderReferences)
            {
                NodeId dataSetReaderNodeId = (NodeId)dataSetReaderReference.NodeId;
                var dataSetReaderReferenceDescriptions = clientSession.Browse(dataSetReaderNodeId);

                // Read Enabled State from Status Node  
                var enabledStateNode = (from refDsc in dataSetReaderReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                        select refDsc).First();
                ReadValueId readValue = new ReadValueId();
                NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
                IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { "State" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                DataValueEx dataValue = clientSession.Read(readValue);
                PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

                // Read PublisherId
                var publisherIdNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                         where refDsc.BrowseName.Name.Equals("PublisherId")
                                                   select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)publisherIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                var publisherIdValue = dataValue.Value.GetType() == typeof(UInt16) ? (UInt16)dataValue.Value : (UInt64)dataValue.Value; 

                // Read WriterGroupId
                var writerGroupIdNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                           where refDsc.BrowseName.Name.Equals("WriterGroupId")
                                         select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)writerGroupIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort writerGroupIdValue = (ushort)dataValue.Value;

                // Read DataSetWriterId
                var dataSetWriterIdNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals("DataSetWriterId")
                                           select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetWriterIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort dataSetWriterIdValue = (ushort)dataValue.Value;

                // Read DataSetMetaData
                var dataSetMetaDataNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals("DataSetMetaData")
                                             select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetMetaDataNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                DataSetMetaDataType dataSetMetaDataValue = (DataSetMetaDataType)((ExtensionObject)dataValue.Value).Body;

                // Read DataSetFieldContentMask
                var dataSetFieldContentMaskNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                                     where refDsc.BrowseName.Name.Equals("DataSetFieldContentMask")
                                             select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetFieldContentMaskNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint dataSetFieldContentMaskValue = (uint)dataValue.Value;

                // Read MessageReceiveTimeout
                var messageReceiveTimeoutNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                                   where refDsc.BrowseName.Name.Equals("MessageReceiveTimeout")
                                                     select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)messageReceiveTimeoutNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                double messageReceiveTimeoutValue = (double)dataValue.Value;

                // Read TargetVariables array from SubscribedDataSet Node  
                var subscribedDataSetNode = (from refDsc in dataSetReaderReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.TargetVariablesType
                                        select refDsc).First();
                readValue = new ReadValueId();
                NodeId subscribedDataSetNodeId = (NodeId)subscribedDataSetNode.NodeId;
                translateResults = clientSession.TranslateBrowsePathToNodeIds(subscribedDataSetNodeId, new List<QualifiedName> { "TargetVariables" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                FieldTargetDataTypeCollection targetVariables = new FieldTargetDataTypeCollection();
                foreach (var extensionObject in (ExtensionObject[])dataValue.Value)
                {
                    FieldTargetDataType fieldTargetDataType = (FieldTargetDataType)extensionObject.Body;
                    targetVariables.Add(fieldTargetDataType);
                }

                // Read GroupVersion from MessageSettings Node  
                var messageSettingsNode = (from refDsc in dataSetReaderReferenceDescriptions
                                             where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                   (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.UadpDataSetReaderMessageType
                                             select refDsc).First();
                readValue = new ReadValueId();
                NodeId messageSettingsNodeNodeId = (NodeId)messageSettingsNode.NodeId;
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { "GroupVersion" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint groupVersionValue = (uint)dataValue.Value;

                // Read DataSetOffset from MessageSettings Node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { "DataSetOffset" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort dataSetOffsetValue = (ushort)dataValue.Value;

                // Read NetworkMessageNumber from MessageSettings Node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { "NetworkMessageNumber" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort networkMessageNumberValue = (ushort)dataValue.Value;

                // Read DataSetMessageContentMask from MessageSettings Node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { "DataSetMessageContentMask" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint dataSetMessageContentMaskValue = (uint)dataValue.Value;

                // Read NetworkMessageContentMask from MessageSettings Node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { "NetworkMessageContentMask" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint networkMessageContentMaskValue = (uint)dataValue.Value;

                DataSetReaderDataType readerDataType = new DataSetReaderDataType
                {
                    Name = dataSetReaderReference.BrowseName.Name,
                    Enabled = pubSubStateValue != PubSubState.Disabled,
                    PublisherId = publisherIdValue,
                    WriterGroupId = writerGroupIdValue,
                    DataSetWriterId = dataSetWriterIdValue,
                    DataSetMetaData = dataSetMetaDataValue,
                    DataSetFieldContentMask = dataSetFieldContentMaskValue,
                    MessageReceiveTimeout = messageReceiveTimeoutValue,
                    SubscribedDataSet = new ExtensionObject(new TargetVariablesDataType
                    {
                        TargetVariables = targetVariables,
                    }),
                    MessageSettings = new ExtensionObject(new UadpDataSetReaderMessageDataType
                    {
                        GroupVersion = groupVersionValue,
                        DataSetOffset = dataSetOffsetValue,
                        NetworkMessageNumber = networkMessageNumberValue,
                        DataSetMessageContentMask = dataSetMessageContentMaskValue,
                        NetworkMessageContentMask = networkMessageContentMaskValue,
                    }),
                };

                readerGroup.DataSetReaders.Add(readerDataType);
            }
        }

        private static void HandleAddWritterGroups(PubSubConnectionDataType pubSubConnectionDataType, ClientSession clientSession, IList<ReferenceDescriptionEx> connectionReferenceDescriptions)
        {
            // Read ReaderGroups
            var writerGroupsReferences = from refDsc in connectionReferenceDescriptions
                                         where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                               (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.WriterGroupType
                                         select refDsc;
            foreach (var writerGroupReference in writerGroupsReferences)
            {
                NodeId writerGroupNodeId = (NodeId)writerGroupReference.NodeId;
                var writerGroupReferenceDescriptions = clientSession.Browse(writerGroupNodeId);

                // Read Enabled State from Status Node  
                var enabledStateNode = (from refDsc in writerGroupReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                        select refDsc).First();

                ReadValueId readValue = new ReadValueId();
                NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
                IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { "State" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;

                DataValueEx dataValue = clientSession.Read(readValue);
                PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

                // Read MaxNetworkMessageSize
                var maxNetworkMessageSizeNodeId = (from refDsc in writerGroupReferenceDescriptions
                                                   where refDsc.BrowseName.Name.Equals("MaxNetworkMessageSize")
                                                   select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)maxNetworkMessageSizeNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint maxNetworkMessageSizeValue = (uint)dataValue.Value;


                // Read MessageSettings->DataSetOrdering from MessageSettings node
                var messageNode = (from refDsc in writerGroupReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.UadpWriterGroupMessageType
                                        select refDsc)?.First();
                readValue = new ReadValueId();
                NodeId messageNodeNodeId = (NodeId)messageNode.NodeId;
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { "DataSetOrdering" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                DataSetOrderingType dataSetOrderingValue = (DataSetOrderingType)dataValue.Value;

                // Read MessageSettings->GroupVersion from MessageSettings node
                readValue = new ReadValueId();  
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { "GroupVersion" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint groupVersionValue = (uint)dataValue.Value;

                // Read MessageSettings->NetworkMessageContentMask from MessageSettings node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { "NetworkMessageContentMask" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint networkMessageContentMaskValue = (uint)dataValue.Value;

                // Read MessageSettings->PublishingOffset from MessageSettings node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { "PublishingOffset" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                double publishingOffsetValue = (double)dataValue.Value;

                // Read MessageSettings->SamplingOffset from MessageSettings node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { "SamplingOffset" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                double samplingOffsetValue = (double)dataValue.Value;

                // Read HeaderLayoutUri
                var headerLayoutUriNodeId = (from refDsc in writerGroupReferenceDescriptions
                                                   where refDsc.BrowseName.Name.Equals("HeaderLayoutUri")
                                                   select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)headerLayoutUriNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                string headerLayoutUriValue = (string)dataValue.Value;

                // Read PublishingInterval
                var publishingIntervalNodeId = (from refDsc in writerGroupReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals("PublishingInterval")
                                             select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)publishingIntervalNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                double publishingIntervalValue = (double)dataValue.Value;

                // Read WriterGroupId
                var writerGroupIdNodeId = (from refDsc in writerGroupReferenceDescriptions
                                                where refDsc.BrowseName.Name.Equals("WriterGroupId")
                                                select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)writerGroupIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort writerGroupIdValue = (ushort)dataValue.Value;

                // Read KeepAliveTime
                var keepAliveTimeNodeId = (from refDsc in writerGroupReferenceDescriptions
                                           where refDsc.BrowseName.Name.Equals("KeepAliveTime")
                                           select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)keepAliveTimeNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                double keepAliveTimeValue = (double)dataValue.Value;
               
                WriterGroupDataType writerGroup = new WriterGroupDataType
                {
                    Name = writerGroupReference.BrowseName.Name,
                    Enabled = pubSubStateValue != PubSubState.Disabled,
                    MessageSettings = new ExtensionObject(new UadpWriterGroupMessageDataType
                    {
                        DataSetOrdering = dataSetOrderingValue,
                        GroupVersion = groupVersionValue,
                        NetworkMessageContentMask = networkMessageContentMaskValue,
                        //PublishingOffset = publishingOffsetValue,
                        SamplingOffset = samplingOffsetValue
                    }),
                    // TransportSettings = new ExtensionObject(new ...
                    HeaderLayoutUri = headerLayoutUriValue,
                    // LocaleIds = 
                    // Priority =
                    PublishingInterval = publishingIntervalValue,
                    WriterGroupId = writerGroupIdValue,
                    KeepAliveTime = keepAliveTimeValue,
                    MaxNetworkMessageSize = maxNetworkMessageSizeValue,
                };

                pubSubConnectionDataType.WriterGroups.Add(writerGroup);

                HandleAddWriters(writerGroup, clientSession, writerGroupReferenceDescriptions);
            }
        }

        private static void HandleAddWriters(WriterGroupDataType writerGroup, ClientSession clientSession, IList<ReferenceDescriptionEx> writerGroupReferenceDescriptions)
        {
            // Read DataSetReaders
            var dataSetWriterReferences = from refDsc in writerGroupReferenceDescriptions
                                         where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                      (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.DataSetWriterType
                                         select refDsc;
            foreach (var dataSetWriterReference in dataSetWriterReferences)
            {
                NodeId dataSetWriterNodeId = (NodeId)dataSetWriterReference.NodeId;
                var dataSetReaderReferenceDescriptions = clientSession.Browse(dataSetWriterNodeId);

                // Read Enabled State from Status Node  
                var enabledStateNode = (from refDsc in dataSetReaderReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                        select refDsc).First();
                ReadValueId readValue = new ReadValueId();
                NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
                IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { "State" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                DataValueEx dataValue = clientSession.Read(readValue);
                PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

                // Read DataSetWriterId
                var dataSetWriterIdNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals("DataSetWriterId")
                                             select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetWriterIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort dataSetWriterIdValue = (ushort)dataValue.Value;

                // Read DataSetFieldContentMask
                var dataSetFieldContentMaskNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                                     where refDsc.BrowseName.Name.Equals("DataSetFieldContentMask")
                                                     select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetFieldContentMaskNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint dataSetFieldContentMaskValue = (uint)dataValue.Value;

                // Read DataSetOffset from MessageSettings Node  
                var messageSettingsNode = (from refDsc in dataSetReaderReferenceDescriptions
                                           where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                 (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.UadpDataSetWriterMessageType
                                           select refDsc).First();
                readValue = new ReadValueId();
                NodeId messageSettingsNodeNodeId = (NodeId)messageSettingsNode.NodeId;
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { "DataSetOffset" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort dataSetOffsetValue = (ushort)dataValue.Value;

                // Read NetworkMessageNumber from MessageSettings Node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { "NetworkMessageNumber" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort networkMessageNumberValue = (ushort)dataValue.Value;

                // Read DataSetMessageContentMask from MessageSettings Node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { "DataSetMessageContentMask" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint dataSetMessageContentMaskValue = (uint)dataValue.Value;

                // Read ConfiguredSize from MessageSettings Node
                readValue = new ReadValueId();
                translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { "ConfiguredSize" });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort configuredSizeValue = (ushort)dataValue.Value;

                DataSetWriterDataType writerDataType = new DataSetWriterDataType
                {
                    Name = dataSetWriterReference.BrowseName.Name,
                    Enabled = pubSubStateValue != PubSubState.Disabled,
                    DataSetWriterId = dataSetWriterIdValue,
                    DataSetFieldContentMask = dataSetFieldContentMaskValue,
                    MessageSettings = new ExtensionObject(new UadpDataSetWriterMessageDataType
                    {
                        DataSetOffset = dataSetOffsetValue,
                        NetworkMessageNumber = networkMessageNumberValue,
                        DataSetMessageContentMask = dataSetMessageContentMaskValue,
                        ConfiguredSize = configuredSizeValue,
                    }),
                    
                };

                writerGroup.DataSetWriters.Add(writerDataType);

            }
        }
        #endregion
    } 
}
