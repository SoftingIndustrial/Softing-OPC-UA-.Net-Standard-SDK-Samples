﻿/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using Opc.Ua;
using Softing.Opc.Ua.Client;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleClient.Samples
{
    public class PubSubStateConfigurationReader
    {
        #region Public Methods
        /// <summary>
        /// Read PubSub configuration
        /// </summary>
        /// <param name="clientSession"></param>
        /// <returns></returns>
        public static PubSubConfigurationDataType PubSubConfigurationRead(ClientSession clientSession)
        {

            NodeId publishSubscribeBase = ObjectIds.PublishSubscribe;

            PubSubConfigurationDataType pubSubConfiguration = new PubSubConfigurationDataType();

            IList<ReferenceDescriptionEx> referenceDescriptions = clientSession.Browse(publishSubscribeBase);

            // Set the state from Status Node 
            var enabledStateNode = (from refDsc in referenceDescriptions
                                    where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                          (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                    select refDsc).First();

            ReadValueId readValue = new ReadValueId();
            NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
            IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { BrowseNames.State });
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

            // Handle connections
            foreach (var referenceDescription in connectionNodes)
            {
                HandleAddConnection(pubSubConfiguration, clientSession, referenceDescription);
            }

            // Handle the published data
            HandleAddPublishedDataSets(pubSubConfiguration, clientSession, referenceDescriptions, new StringCollection());

            return pubSubConfiguration;
        }


        #endregion

        #region Private Methods
        /// <summary>
        /// Add PublishedDataSets from PubSub configuration
        /// </summary>
        /// <param name="pubSubConfiguration"></param>
        /// <param name="clientSession"></param>
        /// <param name="referenceDescriptions"></param>
        /// <param name="dataSetFolder"></param>
        private static void HandleAddPublishedDataSets(PubSubConfigurationDataType pubSubConfiguration,
                                                       ClientSession clientSession,
                                                       IList<ReferenceDescriptionEx> referenceDescriptions,
                                                       StringCollection dataSetFolder)
        {

            IEnumerable<ReferenceDescriptionEx> publishedDataSetsNodes = from refDsc in referenceDescriptions
                                                           where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                                 (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.DataSetFolderType
                                                           select refDsc;

            foreach (var publishedDataSetsNode in publishedDataSetsNodes)
            {
                StringCollection dataSetFolderValue = (StringCollection)dataSetFolder.MemberwiseClone();
                dataSetFolderValue.Add(publishedDataSetsNode.BrowseName.Name);

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
                    var translateResults = clientSession.TranslateBrowsePathToNodeIds(publishedDataItemNodeId, new List<QualifiedName> { BrowseNames.ConfigurationVersion });
                    var readConfigurationVersion = new ReadValueId();
                    readConfigurationVersion.NodeId = translateResults?.First();
                    readConfigurationVersion.AttributeId = Attributes.Value;

                    translateResults = clientSession.TranslateBrowsePathToNodeIds(publishedDataItemNodeId, new List<QualifiedName> { BrowseNames.DataSetMetaData });
                    var readDataSetMetaData = new ReadValueId();
                    readDataSetMetaData.NodeId = translateResults?.First();
                    readDataSetMetaData.AttributeId = Attributes.Value;

                    translateResults = clientSession.TranslateBrowsePathToNodeIds(publishedDataItemNodeId, new List<QualifiedName> { BrowseNames.PublishedData });
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

                    // Read ExtensionFields
                    var pubDataItemReferences = clientSession.Browse(publishedDataItemNodeId);
                    var extensionFieldReference = (from  refDsc in pubDataItemReferences
                                                   where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                   (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.ExtensionFieldsType
                                                   select refDsc).FirstOrDefault();
                   
                    var readExtensionFieldsNodeId = (NodeId)extensionFieldReference?.NodeId;
                    var allExtensionFieldsReferences = clientSession.Browse(readExtensionFieldsNodeId);
                    var varExtensionFieldsReferences = from reff in allExtensionFieldsReferences
                                                       where reff.TypeDefinition.IdType == IdType.Numeric &&
                                                       (uint)reff.TypeDefinition.Identifier == (uint)VariableTypeIds.BaseDataVariableType.Identifier
                                                       select reff;


                    KeyValuePairCollection extensionFieldsValues = new KeyValuePairCollection();

                    foreach (var extensionFieldRef in varExtensionFieldsReferences)
                    {
                        NodeId extensionFieldNodeId = (NodeId)extensionFieldRef.NodeId;
                        var readValue = new ReadValueId
                        {
                            AttributeId = Attributes.Value,
                            NodeId = extensionFieldNodeId, 
                        };
                        DataValueEx dataValue = clientSession.Read(readValue);

                        Opc.Ua.KeyValuePair newExtensionField = new Opc.Ua.KeyValuePair
                        {
                            Key = extensionFieldReference.BrowseName.Name,
                            Value = new Variant(dataValue.Value),
                        };

                        extensionFieldsValues.Add(newExtensionField);
                    }
                    // End Read ExtensionFields

                  
                    PublishedDataSetDataType publishedDataSetDataType = new PublishedDataSetDataType
                    {
                        Name = publishedDataItemReference.BrowseName.Name,
                        DataSetFolder = dataSetFolderValue,
                        DataSetMetaData = dataSetMetaDataValue,
                        DataSetSource = new ExtensionObject(new PublishedDataItemsDataType
                        {
                            PublishedData = publishedVariables,
                        }),
                        ExtensionFields = extensionFieldsValues,
                    };

                    pubSubConfiguration.PublishedDataSets.Add(publishedDataSetDataType);
                }
                // Recurse on its children
                HandleAddPublishedDataSets(pubSubConfiguration, clientSession, publishedDataSetFoldersReferenceDescriptions, dataSetFolderValue);
            }
        }

        /// <summary>
        /// Add connection from PubSub configuration
        /// </summary>
        /// <param name="pubSubConfiguration"></param>
        /// <param name="clientSession"></param>
        /// <param name="referenceDescription"></param>
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
            IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { BrowseNames.State });
            readValue.NodeId = translateResults?.First();
            readValue.AttributeId = Attributes.Value;

            DataValueEx dataValue = clientSession.Read(readValue);
            PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

            // Read PublisherId
            var publisherIdNodeId = (from refDsc in connectionReferenceDescriptions
                                     where refDsc.BrowseName.Name.Equals(BrowseNames.PublisherId)
                                     select refDsc).First();
            readValue = new ReadValueId();
            readValue.NodeId = (NodeId)publisherIdNodeId.NodeId;
            readValue.AttributeId = Attributes.Value;

            dataValue = clientSession.Read(readValue);
            
            Variant publisherIdValue = Variant.Null;
            if (dataValue.Value.GetType() == typeof(UInt16))
            {
                publisherIdValue = (UInt16)dataValue.Value;
            }
            else if (dataValue.Value.GetType() == typeof(UInt64))
            {
                publisherIdValue = (UInt64)dataValue.Value;
            }

            // Read TransportProfileUri
            var transportProfileUriNodeId = (from refDsc in connectionReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals(BrowseNames.TransportProfileUri)
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

            translateResults = clientSession.TranslateBrowsePathToNodeIds(addressNodeNodeId, new List<QualifiedName> { BrowseNames.NetworkInterface });
            var readValueNetworkInterface = new ReadValueId();
            readValueNetworkInterface.NodeId = translateResults?.First();
            readValueNetworkInterface.AttributeId = Attributes.Value;

            translateResults = clientSession.TranslateBrowsePathToNodeIds(addressNodeNodeId, new List<QualifiedName> { BrowseNames.Url });
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

            HandleAddWriterGroups(pubSubConnectionDataType, clientSession, connectionReferenceDescriptions);
            HandleAddReaderGroups(pubSubConnectionDataType, clientSession, connectionReferenceDescriptions);

        }

        /// <summary>
        /// Add ReaderGroups from PubSub configuration
        /// </summary>
        /// <param name="pubSubConnectionDataType"></param>
        /// <param name="clientSession"></param>
        /// <param name="connectionReferenceDescriptions"></param>
        private static void HandleAddReaderGroups(PubSubConnectionDataType pubSubConnectionDataType, ClientSession clientSession, IList<ReferenceDescriptionEx> connectionReferenceDescriptions)
        {

            // Read ReaderGroups
            var readerGroupsReferences = from refDsc in connectionReferenceDescriptions
                                         where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                               (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.ReaderGroupType
                                         select refDsc;
            foreach (var readerGroupReference in readerGroupsReferences)
            {
                if (HasPlaceHolderTags(readerGroupReference))
                {
                    // ignore the readerGroupReference with place holder tag
                    continue;
                }

                NodeId readerGroupNodeId = (NodeId)readerGroupReference.NodeId;
                var readerGroupReferenceDescriptions = clientSession.Browse(readerGroupNodeId);

                // Read Enabled State from Status Node  
                var enabledStateNode = (from refDsc in readerGroupReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                        select refDsc).First();

                ReadValueId readValue = new ReadValueId();
                NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
                IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { BrowseNames.State });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                DataValueEx dataValue = clientSession.Read(readValue);
                PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

                // Read MaxNetworkMessageSize
                var maxNetworkMessageSizeNodeId = (from refDsc in readerGroupReferenceDescriptions
                                                   where refDsc.BrowseName.Name.Equals(BrowseNames.MaxNetworkMessageSize)
                                                   select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)maxNetworkMessageSizeNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint maxNetworkMessageSizeValue = (uint)dataValue.Value;

                //Read TransportSettings depending on type
                var transportSettingsNode = (from refDsc in readerGroupReferenceDescriptions
                                             where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                   ((uint)refDsc.TypeDefinition.Identifier == ObjectTypes.ReaderGroupTransportType)
                                             select refDsc)?.First();
                readValue = new ReadValueId();
                NodeId transportSettingsNodeId = (NodeId)transportSettingsNode.NodeId;
                NodeId transportSettingsType = (NodeId)transportSettingsNode.TypeDefinition;


                ReaderGroupDataType readerGroup = new ReaderGroupDataType
                {
                    Name = readerGroupReference.BrowseName.Name,
                    Enabled = pubSubStateValue != PubSubState.Disabled,
                    MaxNetworkMessageSize = maxNetworkMessageSizeValue,
                    MessageSettings = new ExtensionObject(new ReaderGroupMessageDataType()),
                    TransportSettings = new ExtensionObject(new ReaderGroupTransportDataType()),
                };

                pubSubConnectionDataType.ReaderGroups.Add(readerGroup);
                
                HandleAddReaders(pubSubConnectionDataType.TransportProfileUri, readerGroup, clientSession, readerGroupReferenceDescriptions);

            }
        }

        /// <summary>
        /// Add Readers from PubSub configuration
        /// </summary>
        /// <param name="transportProfileUri"></param>
        /// <param name="readerGroup"></param>
        /// <param name="clientSession"></param>
        /// <param name="readerGroupReferenceDescriptions"></param>
        private static void HandleAddReaders(string transportProfileUri, ReaderGroupDataType readerGroup, ClientSession clientSession, IList<ReferenceDescriptionEx> readerGroupReferenceDescriptions)
        {
            // Read DataSetReaders
            var dataSetReaderReferences = from refDsc in readerGroupReferenceDescriptions
                                          where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                      (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.DataSetReaderType
                                          select refDsc;
            foreach (var dataSetReaderReference in dataSetReaderReferences)
            {
                if (HasPlaceHolderTags(dataSetReaderReference))
                {
                    // ignore the dataSetReaderReference with place holder tag
                    continue;
                }

                NodeId dataSetReaderNodeId = (NodeId)dataSetReaderReference.NodeId;
                var dataSetReaderReferenceDescriptions = clientSession.Browse(dataSetReaderNodeId);

                // Read Enabled State from Status Node  
                var enabledStateNode = (from refDsc in dataSetReaderReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                        select refDsc).First();
                ReadValueId readValue = new ReadValueId();
                NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
                IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { BrowseNames.State });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                DataValueEx dataValue = clientSession.Read(readValue);
                PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

                // Read PublisherId
                var publisherIdNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                         where refDsc.BrowseName.Name.Equals(BrowseNames.PublisherId)
                                                   select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)publisherIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);

                Variant publisherIdValue = Variant.Null;
                if (dataValue.Value.GetType() == typeof(UInt16))
                {
                    publisherIdValue = (UInt16)dataValue.Value;
                }
                else if (dataValue.Value.GetType() == typeof(UInt64))
                {
                    publisherIdValue = (UInt64)dataValue.Value;
                }

                // Read WriterGroupId
                var writerGroupIdNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                           where refDsc.BrowseName.Name.Equals(BrowseNames.WriterGroupId)
                                         select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)writerGroupIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort writerGroupIdValue = (ushort)dataValue.Value;

                // Read DataSetWriterId
                var dataSetWriterIdNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals(BrowseNames.DataSetWriterId)
                                           select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetWriterIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort dataSetWriterIdValue = (ushort)dataValue.Value;

                // Read DataSetMetaData
                var dataSetMetaDataNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals(BrowseNames.DataSetMetaData)
                                             select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetMetaDataNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                DataSetMetaDataType dataSetMetaDataValue = (DataSetMetaDataType)((ExtensionObject)dataValue.Value).Body;

                // Read DataSetFieldContentMask
                var dataSetFieldContentMaskNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                                     where refDsc.BrowseName.Name.Equals(BrowseNames.DataSetFieldContentMask)
                                             select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetFieldContentMaskNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint dataSetFieldContentMaskValue = (uint)dataValue.Value;

                // Read MessageReceiveTimeout
                var messageReceiveTimeoutNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                                   where refDsc.BrowseName.Name.Equals(BrowseNames.MessageReceiveTimeout)
                                                     select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)messageReceiveTimeoutNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                double messageReceiveTimeoutValue = (double)dataValue.Value;

                // Read KeyFrameCount
                var keyFrameCountNodeId = (from refDsc in dataSetReaderReferenceDescriptions
                                                   where refDsc.BrowseName.Name.Equals(BrowseNames.KeyFrameCount)
                                                   select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)keyFrameCountNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint keyFrameCountValue = (uint)dataValue.Value;

                // Read TargetVariables array from SubscribedDataSet Node  
                var subscribedDataSetNode = (from refDsc in dataSetReaderReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.TargetVariablesType
                                        select refDsc).First();
                readValue = new ReadValueId();
                NodeId subscribedDataSetNodeId = (NodeId)subscribedDataSetNode.NodeId;
                translateResults = clientSession.TranslateBrowsePathToNodeIds(subscribedDataSetNodeId, new List<QualifiedName> { BrowseNames.TargetVariables });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                FieldTargetDataTypeCollection targetVariables = new FieldTargetDataTypeCollection();
                foreach (var extensionObject in (ExtensionObject[])dataValue.Value)
                {
                    FieldTargetDataType fieldTargetDataType = (FieldTargetDataType)extensionObject.Body;
                    targetVariables.Add(fieldTargetDataType);
                }

                // MessageSettings
                DataSetReaderMessageDataType messageSettings = null;
                if (transportProfileUri == Profiles.PubSubMqttJsonTransport)
                {
                    var messageSettingsNode = (from refDsc in dataSetReaderReferenceDescriptions
                                               where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                     (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.JsonDataSetReaderMessageType
                                               select refDsc).First();
                    readValue = new ReadValueId();
                    NodeId messageSettingsNodeNodeId = (NodeId)messageSettingsNode.NodeId;
                    // Read DataSetMessageContentMask from MessageSettings Node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.DataSetMessageContentMask });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint dataSetMessageContentMaskValue = (uint)dataValue.Value;

                    // Read NetworkMessageContentMask from MessageSettings Node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.NetworkMessageContentMask });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint networkMessageContentMaskValue = (uint)dataValue.Value;

                    messageSettings = new JsonDataSetReaderMessageDataType
                    {
                        DataSetMessageContentMask = dataSetMessageContentMaskValue,
                        NetworkMessageContentMask = networkMessageContentMaskValue,
                    };
                }

                if (transportProfileUri == Profiles.PubSubUdpUadpTransport || transportProfileUri == Profiles.PubSubMqttUadpTransport)
                {
                    // Read GroupVersion from MessageSettings Node  
                    var messageSettingsNode = (from refDsc in dataSetReaderReferenceDescriptions
                                               where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                     (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.UadpDataSetReaderMessageType
                                               select refDsc).First();
                    readValue = new ReadValueId();
                    NodeId messageSettingsNodeNodeId = (NodeId)messageSettingsNode.NodeId;
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.GroupVersion });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint groupVersionValue = (uint)dataValue.Value;

                    // Read DataSetOffset from MessageSettings Node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.DataSetOffset });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    ushort dataSetOffsetValue = (ushort)dataValue.Value;

                    // Read NetworkMessageNumber from MessageSettings Node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.NetworkMessageNumber });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    ushort networkMessageNumberValue = (ushort)dataValue.Value;

                    // Read DataSetMessageContentMask from MessageSettings Node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.DataSetMessageContentMask });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint dataSetMessageContentMaskValue = (uint)dataValue.Value;

                    // Read NetworkMessageContentMask from MessageSettings Node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.NetworkMessageContentMask });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint networkMessageContentMaskValue = (uint)dataValue.Value;

                    messageSettings = new UadpDataSetReaderMessageDataType
                    {
                        GroupVersion = groupVersionValue,
                        DataSetOffset = dataSetOffsetValue,
                        NetworkMessageNumber = networkMessageNumberValue,
                        DataSetMessageContentMask = dataSetMessageContentMaskValue,
                        NetworkMessageContentMask = networkMessageContentMaskValue,
                    };
                }

                // TransportSettings
                DataSetReaderTransportDataType transportSettings = new DataSetReaderTransportDataType();
                if (transportProfileUri == Profiles.PubSubMqttJsonTransport || transportProfileUri == Profiles.PubSubMqttUadpTransport)
                {
                    var transportSettingsNode = (from refDsc in dataSetReaderReferenceDescriptions
                                                 where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                 (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.BrokerDataSetReaderTransportType
                                                 select refDsc)?.First();
                    readValue = new ReadValueId();
                    NodeId transportSettingsNodeId = (NodeId)transportSettingsNode.NodeId;
                    NodeId transportSettingsType = (NodeId)transportSettingsNode.TypeDefinition;

                    // Read TransportSettings->QueueName from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.QueueName });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    string queueName = (string)dataValue.Value;

                    // Read TransportSettings->MetaDataQueueName from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.MetaDataQueueName });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    string metaDataQueueName = (string)dataValue.Value;

                    // Read TransportSettings->RequestedDeliveryGuarantee from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.RequestedDeliveryGuarantee });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    BrokerTransportQualityOfService requestedDeliveryGuarantee = (BrokerTransportQualityOfService)dataValue.Value;

                    // Read TransportSettings->ResourceUri from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.ResourceUri });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    string resourceUri = (string)dataValue.Value;

                    // Read TransportSettings->AuthenticationProfileUri from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.AuthenticationProfileUri });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    string authenticationProfileUri = (string)dataValue.Value;

                    transportSettings = new BrokerDataSetReaderTransportDataType
                    {
                        QueueName = queueName,
                        MetaDataQueueName = metaDataQueueName,
                        RequestedDeliveryGuarantee = requestedDeliveryGuarantee,
                        ResourceUri = resourceUri,
                        AuthenticationProfileUri = authenticationProfileUri
                    };
                }

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
                    KeyFrameCount = keyFrameCountValue,
                    SubscribedDataSet = new ExtensionObject(new TargetVariablesDataType
                    {
                        TargetVariables = targetVariables,
                    }),
                    MessageSettings = new ExtensionObject(messageSettings),
                    TransportSettings = new ExtensionObject(transportSettings)
                };

                readerGroup.DataSetReaders.Add(readerDataType);
            }
        }

        /// <summary>
        /// Add WriterGroups from PubSub configuration
        /// </summary>
        /// <param name="pubSubConnectionDataType"></param>
        /// <param name="clientSession"></param>
        /// <param name="connectionReferenceDescriptions"></param>
        private static void HandleAddWriterGroups(PubSubConnectionDataType pubSubConnectionDataType, ClientSession clientSession, IList<ReferenceDescriptionEx> connectionReferenceDescriptions)
        {
            // Read ReaderGroups
            var writerGroupsReferences = from refDsc in connectionReferenceDescriptions
                                         where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                               (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.WriterGroupType
                                         select refDsc;
            foreach (var writerGroupReference in writerGroupsReferences)
            {
                if (HasPlaceHolderTags(writerGroupReference))
                {
                    // ignore the writerGroupReference with place holder tag
                    continue;
                }

                NodeId writerGroupNodeId = (NodeId)writerGroupReference.NodeId;
                var writerGroupReferenceDescriptions = clientSession.Browse(writerGroupNodeId);

                // Read Enabled State from Status Node  
                var enabledStateNode = (from refDsc in writerGroupReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                        select refDsc).First();

                ReadValueId readValue = new ReadValueId();
                NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
                IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { BrowseNames.State });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;

                DataValueEx dataValue = clientSession.Read(readValue);
                PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

                // Read MaxNetworkMessageSize
                var maxNetworkMessageSizeNodeId = (from refDsc in writerGroupReferenceDescriptions
                                                   where refDsc.BrowseName.Name.Equals(BrowseNames.MaxNetworkMessageSize)
                                                   select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)maxNetworkMessageSizeNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint maxNetworkMessageSizeValue = (uint)dataValue.Value;

                // Read HeaderLayoutUri
                var headerLayoutUriNodeId = (from refDsc in writerGroupReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals(BrowseNames.HeaderLayoutUri)
                                             select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)headerLayoutUriNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                string headerLayoutUriValue = (string)dataValue.Value;

                // Read PublishingInterval
                var publishingIntervalNodeId = (from refDsc in writerGroupReferenceDescriptions
                                                where refDsc.BrowseName.Name.Equals(BrowseNames.PublishingInterval)
                                                select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)publishingIntervalNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                double publishingIntervalValue = (double)dataValue.Value;

                // Read WriterGroupId
                var writerGroupIdNodeId = (from refDsc in writerGroupReferenceDescriptions
                                           where refDsc.BrowseName.Name.Equals(BrowseNames.WriterGroupId)
                                           select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)writerGroupIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort writerGroupIdValue = (ushort)dataValue.Value;

                // Read KeepAliveTime
                var keepAliveTimeNodeId = (from refDsc in writerGroupReferenceDescriptions
                                           where refDsc.BrowseName.Name.Equals(BrowseNames.KeepAliveTime)
                                           select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)keepAliveTimeNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                double keepAliveTimeValue = (double)dataValue.Value;

                WriterGroupDataType writerGroup = null;

                // Read TransportSettings depending on type
                var transportSettingsNode = (from refDsc in writerGroupReferenceDescriptions
                                             where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                   ((uint)refDsc.TypeDefinition.Identifier == ObjectTypes.DatagramWriterGroupTransportType ||
                                                      (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.BrokerWriterGroupTransportType)
                                             select refDsc)?.First();
                readValue = new ReadValueId();
                NodeId transportSettingsNodeId = (NodeId)transportSettingsNode.NodeId;
                NodeId transportSettingsType = (NodeId)transportSettingsNode.TypeDefinition;

                WriterGroupTransportDataType transportSettings = null;
                switch ((uint)transportSettingsType.Identifier)
                {
                    case ObjectTypes.DatagramWriterGroupTransportType:
                        // Read TransportSettings->MessageRepeatCount from TransportSettings node
                        readValue = new ReadValueId();
                        translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.MessageRepeatCount });
                        readValue.NodeId = translateResults?.First();
                        readValue.AttributeId = Attributes.Value;
                        dataValue = clientSession.Read(readValue);
                        byte messageRepeatCount = (byte)dataValue.Value;

                        // Read TransportSettings->MessageRepeatDelay from TransportSettings node
                        readValue = new ReadValueId();
                        translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.MessageRepeatDelay });
                        readValue.NodeId = translateResults?.First();
                        readValue.AttributeId = Attributes.Value;
                        dataValue = clientSession.Read(readValue);
                        double messageRepeatDelay = (double)dataValue.Value;

                        transportSettings = new DatagramWriterGroupTransportDataType
                        {
                            MessageRepeatCount = messageRepeatCount,
                            MessageRepeatDelay = messageRepeatDelay
                        };
                        break;
                    case ObjectTypes.BrokerWriterGroupTransportType:

                        // Read TransportSettings->QueueName from TransportSettings node
                        readValue = new ReadValueId();
                        translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.QueueName });
                        readValue.NodeId = translateResults?.First();
                        readValue.AttributeId = Attributes.Value;
                        dataValue = clientSession.Read(readValue);
                        string queueName = (string)dataValue.Value;

                        // Read TransportSettings->RequestedDeliveryGuarantee from TransportSettings node
                        readValue = new ReadValueId();
                        translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.RequestedDeliveryGuarantee });
                        readValue.NodeId = translateResults?.First();
                        readValue.AttributeId = Attributes.Value;
                        dataValue = clientSession.Read(readValue);
                        BrokerTransportQualityOfService requestedDeliveryGuarantee = (BrokerTransportQualityOfService)dataValue.Value;

                        // Read TransportSettings->ResourceUri from TransportSettings node
                        readValue = new ReadValueId();
                        translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.ResourceUri });
                        readValue.NodeId = translateResults?.First();
                        readValue.AttributeId = Attributes.Value;
                        dataValue = clientSession.Read(readValue);
                        string resourceUri = (string)dataValue.Value;

                        // Read TransportSettings->AuthenticationProfileUri from TransportSettings node
                        readValue = new ReadValueId();
                        translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.AuthenticationProfileUri });
                        readValue.NodeId = translateResults?.First();
                        readValue.AttributeId = Attributes.Value;
                        dataValue = clientSession.Read(readValue);
                        string authenticationProfileUri = (string)dataValue.Value;

                        transportSettings = new BrokerWriterGroupTransportDataType
                        {
                            QueueName = queueName,
                            ResourceUri = resourceUri,
                            AuthenticationProfileUri = authenticationProfileUri,
                            RequestedDeliveryGuarantee = requestedDeliveryGuarantee
                        };

                        break;
                }

                WriterGroupMessageDataType messageSettings = null;

                if (pubSubConnectionDataType.TransportProfileUri == Profiles.PubSubMqttJsonTransport)
                {
                    var messageNode = (from refDsc in writerGroupReferenceDescriptions
                                       where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                       (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.JsonWriterGroupMessageType
                                       select refDsc).First();
                    readValue = new ReadValueId();
                    NodeId messageNodeNodeId = (NodeId)messageNode.NodeId;
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { BrowseNames.NetworkMessageContentMask });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint networkMessageContentMaskValue = (uint)dataValue.Value;

                    messageSettings = new JsonWriterGroupMessageDataType
                    {
                        NetworkMessageContentMask = networkMessageContentMaskValue
                    };
                }

                if (pubSubConnectionDataType.TransportProfileUri == Profiles.PubSubUdpUadpTransport ||
                    pubSubConnectionDataType.TransportProfileUri == Profiles.PubSubMqttUadpTransport)
                {
                    // Read MessageSettings->DataSetOrdering from MessageSettings node
                    var messageNode = (from refDsc in writerGroupReferenceDescriptions
                                       where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                       (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.UadpWriterGroupMessageType
                                       select refDsc).First();
                    readValue = new ReadValueId();
                    NodeId messageNodeNodeId = (NodeId)messageNode.NodeId;
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { BrowseNames.DataSetOrdering });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    DataSetOrderingType dataSetOrderingValue = (DataSetOrderingType)dataValue.Value;

                    // Read MessageSettings->GroupVersion from MessageSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { BrowseNames.GroupVersion });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint groupVersionValue = (uint)dataValue.Value;

                    // Read MessageSettings->NetworkMessageContentMask from MessageSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { BrowseNames.NetworkMessageContentMask });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint networkMessageContentMaskValue = (uint)dataValue.Value;

                    // Read MessageSettings->PublishingOffset from MessageSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { BrowseNames.PublishingOffset });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    double[] publishingOffsetValue = (double[])dataValue.Value;

                    // Read MessageSettings->SamplingOffset from MessageSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageNodeNodeId, new List<QualifiedName> { BrowseNames.SamplingOffset });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    double samplingOffsetValue = (double)dataValue.Value;

                    messageSettings = new UadpWriterGroupMessageDataType
                    {
                        DataSetOrdering = dataSetOrderingValue,
                        GroupVersion = groupVersionValue,
                        NetworkMessageContentMask = networkMessageContentMaskValue,
                        //PublishingOffset = { publishingOffsetValue },
                        SamplingOffset = samplingOffsetValue
                    };
               }

                writerGroup = new WriterGroupDataType
                {
                    Name = writerGroupReference.BrowseName.Name,
                    Enabled = pubSubStateValue != PubSubState.Disabled,
                    MessageSettings = new ExtensionObject(messageSettings),
                    TransportSettings = new ExtensionObject(transportSettings),
                    HeaderLayoutUri = headerLayoutUriValue,
                    // LocaleIds = 
                    // Priority =
                    PublishingInterval = publishingIntervalValue,
                    WriterGroupId = writerGroupIdValue,
                    KeepAliveTime = keepAliveTimeValue,
                    MaxNetworkMessageSize = maxNetworkMessageSizeValue,

                };

                pubSubConnectionDataType.WriterGroups.Add(writerGroup);

                HandleAddWriters(pubSubConnectionDataType.TransportProfileUri, writerGroup, clientSession, writerGroupReferenceDescriptions);
            }
        }

        /// <summary>
        /// Add Writers from PubSub configuration
        /// </summary>
        /// <param name="transportProfileUri"></param>
        /// <param name="writerGroup"></param>
        /// <param name="clientSession"></param>
        /// <param name="writerGroupReferenceDescriptions"></param>
        private static void HandleAddWriters(string transportProfileUri, WriterGroupDataType writerGroup, ClientSession clientSession, IList<ReferenceDescriptionEx> writerGroupReferenceDescriptions)
        {
            // Read DataSetReaders
            var dataSetWriterReferences = from refDsc in writerGroupReferenceDescriptions
                                         where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                      (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.DataSetWriterType
                                         select refDsc;
            foreach (var dataSetWriterReference in dataSetWriterReferences)
            {
                if (HasPlaceHolderTags(dataSetWriterReference))
                {
                    // ignore the dataSetWriterReference with place holder tag
                    continue;
                }

                NodeId dataSetWriterNodeId = (NodeId)dataSetWriterReference.NodeId;

                var dataSetWriterReferenceDescriptions = clientSession.Browse(dataSetWriterNodeId,
                    new BrowseDescriptionEx { BrowseDirection = BrowseDirection.Both });

                // Read Enabled State from Status Node  
                var enabledStateNode = (from refDsc in dataSetWriterReferenceDescriptions
                                        where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                              (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.PubSubStatusType
                                        select refDsc).First();
                ReadValueId readValue = new ReadValueId();
                NodeId statusNodeId = (NodeId)enabledStateNode.NodeId;
                IList<Opc.Ua.NodeId> translateResults = clientSession.TranslateBrowsePathToNodeIds(statusNodeId, new List<QualifiedName> { BrowseNames.State });
                readValue.NodeId = translateResults?.First();
                readValue.AttributeId = Attributes.Value;
                DataValueEx dataValue = clientSession.Read(readValue);
                PubSubState pubSubStateValue = (PubSubState)dataValue.Value;

                // Read DataSetWriterId
                var dataSetWriterIdNodeId = (from refDsc in dataSetWriterReferenceDescriptions
                                             where refDsc.BrowseName.Name.Equals(BrowseNames.DataSetWriterId)
                                             select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetWriterIdNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                ushort dataSetWriterIdValue = (ushort)dataValue.Value;

                // Read DataSetFieldContentMask
                var dataSetFieldContentMaskNodeId = (from refDsc in dataSetWriterReferenceDescriptions
                                                     where refDsc.BrowseName.Name.Equals(BrowseNames.DataSetFieldContentMask)
                                                     select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetFieldContentMaskNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint dataSetFieldContentMaskValue = (uint)dataValue.Value;

                // Read KeyFrameCount
                var keyFrameCountNodeId = (from refDsc in
                                               dataSetWriterReferenceDescriptions
                                           where refDsc.BrowseName.Name.Equals(BrowseNames.KeyFrameCount)
                                           select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)keyFrameCountNodeId.NodeId;
                readValue.AttributeId = Attributes.Value;
                dataValue = clientSession.Read(readValue);
                uint keyFrameCountValue = (uint)dataValue.Value;

                // Read DataSetName
                var dataSetNodeId = (from refDsc in dataSetWriterReferenceDescriptions
                                     where ((uint)refDsc.ReferenceTypeId.Identifier == (uint)ReferenceTypeIds.DataSetToWriter.Identifier)
                                     select refDsc).First();
                readValue = new ReadValueId();
                readValue.NodeId = (NodeId)dataSetNodeId.NodeId;
                readValue.AttributeId = Attributes.BrowseName;
                dataValue = clientSession.Read(readValue);
                string dataSetNameValue = ((QualifiedName)dataValue.Value).Name;

                // MessageSettings
                DataSetWriterMessageDataType messageSettings = null;
                if (transportProfileUri == Profiles.PubSubMqttJsonTransport)
                {
                    // Read DataSetMessageContentMask from MessageSettings Node
                    var messageSettingsNode = (from refDsc in dataSetWriterReferenceDescriptions
                                               where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                     (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.JsonDataSetWriterMessageType
                                               select refDsc).First();
                    readValue = new ReadValueId();
                    NodeId messageSettingsNodeNodeId = (NodeId)messageSettingsNode.NodeId;
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.DataSetMessageContentMask });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint dataSetMessageContentMaskValue = (uint)dataValue.Value;

                    messageSettings = new JsonDataSetWriterMessageDataType
                    {
                        DataSetMessageContentMask = dataSetMessageContentMaskValue
                    };
                }

                if (transportProfileUri == Profiles.PubSubUdpUadpTransport || transportProfileUri == Profiles.PubSubMqttUadpTransport)
                {
                    // Read DataSetOffset from MessageSettings Node  
                    var messageSettingsNode = (from refDsc in dataSetWriterReferenceDescriptions
                                               where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                     (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.UadpDataSetWriterMessageType
                                               select refDsc).First();
                    readValue = new ReadValueId();
                    NodeId messageSettingsNodeNodeId = (NodeId)messageSettingsNode.NodeId;
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.DataSetOffset });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    ushort dataSetOffsetValue = (ushort)dataValue.Value;

                    // Read NetworkMessageNumber from MessageSettings Node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.NetworkMessageNumber });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    ushort networkMessageNumberValue = (ushort)dataValue.Value;

                    // Read DataSetMessageContentMask from MessageSettings Node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.DataSetMessageContentMask });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    uint dataSetMessageContentMaskValue = (uint)dataValue.Value;

                    // Read ConfiguredSize from MessageSettings Node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(messageSettingsNodeNodeId, new List<QualifiedName> { BrowseNames.ConfiguredSize });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    ushort configuredSizeValue = (ushort)dataValue.Value;

                    messageSettings = new UadpDataSetWriterMessageDataType
                    {
                        DataSetOffset = dataSetOffsetValue,
                        NetworkMessageNumber = networkMessageNumberValue,
                        DataSetMessageContentMask = dataSetMessageContentMaskValue,
                        ConfiguredSize = configuredSizeValue,
                    };
                }

                // TransportSettings
                DataSetWriterTransportDataType transportSettings = new DataSetWriterTransportDataType();
                if (transportProfileUri == Profiles.PubSubMqttJsonTransport || transportProfileUri == Profiles.PubSubMqttUadpTransport)
                {
                    var transportSettingsNode = (from refDsc in dataSetWriterReferenceDescriptions
                                                 where refDsc.TypeDefinition.IdType == IdType.Numeric &&
                                                 (uint)refDsc.TypeDefinition.Identifier == ObjectTypes.BrokerDataSetWriterTransportType
                                                 select refDsc)?.First();
                    readValue = new ReadValueId();
                    NodeId transportSettingsNodeId = (NodeId)transportSettingsNode.NodeId;
                    NodeId transportSettingsType = (NodeId)transportSettingsNode.TypeDefinition;

                    // Read TransportSettings->QueueName from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.QueueName });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    string queueName = (string)dataValue.Value;

                    // Read TransportSettings->MetaDataQueueName from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.MetaDataQueueName });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    string metaDataQueueName = (string)dataValue.Value;

                    // Read TransportSettings->RequestedDeliveryGuarantee from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.RequestedDeliveryGuarantee });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    BrokerTransportQualityOfService requestedDeliveryGuarantee = (BrokerTransportQualityOfService)dataValue.Value;

                    // Read TransportSettings->ResourceUri from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.ResourceUri });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    string resourceUri = (string)dataValue.Value;

                    // Read TransportSettings->AuthenticationProfileUri from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.AuthenticationProfileUri });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    string authenticationProfileUri = (string)dataValue.Value;

                    // Read TransportSettings->MetaDataUpdateTime from TransportSettings node
                    readValue = new ReadValueId();
                    translateResults = clientSession.TranslateBrowsePathToNodeIds(transportSettingsNodeId, new List<QualifiedName> { BrowseNames.MetaDataUpdateTime });
                    readValue.NodeId = translateResults?.First();
                    readValue.AttributeId = Attributes.Value;
                    dataValue = clientSession.Read(readValue);
                    double metaDataUpdateTime = (double)dataValue.Value;

                    transportSettings = new BrokerDataSetWriterTransportDataType
                    {
                        QueueName = queueName,
                        MetaDataQueueName = metaDataQueueName,
                        RequestedDeliveryGuarantee = requestedDeliveryGuarantee,
                        ResourceUri = resourceUri,
                        AuthenticationProfileUri = authenticationProfileUri,
                        MetaDataUpdateTime = metaDataUpdateTime
                    };
                }

                DataSetWriterDataType writerDataType = new DataSetWriterDataType
                {
                    Name = dataSetWriterReference.BrowseName.Name,
                    Enabled = pubSubStateValue != PubSubState.Disabled,
                    DataSetWriterId = dataSetWriterIdValue,
                    DataSetFieldContentMask = dataSetFieldContentMaskValue,
                    MessageSettings = new ExtensionObject(messageSettings),
                    TransportSettings = new ExtensionObject(transportSettings),
                    KeyFrameCount = keyFrameCountValue,
                    DataSetName = dataSetNameValue,
                };

                writerGroup.DataSetWriters.Add(writerDataType);

            }
        }

        /// <summary>
        /// Verify if the item includes a place holder
        /// </summary>
        /// <param name="referenceDescription"></param>
        /// <returns></returns>
        private static bool HasPlaceHolderTags(ReferenceDescriptionEx referenceDescription)
        {
            if (referenceDescription.BrowseName != null &&
                referenceDescription.BrowseName.Name != null)
            {
                return System.Text.RegularExpressions.Regex.IsMatch(referenceDescription.BrowseName.Name, "<+[a-zA-Z1-9_]+>");
            }
            return false;
        }
        #endregion
    } 
}
