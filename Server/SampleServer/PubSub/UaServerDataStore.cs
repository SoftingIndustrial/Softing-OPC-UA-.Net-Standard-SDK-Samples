/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Server;
using Opc.Ua.PubSub;
using Softing.Opc.Ua.Server;
using System.Collections.Generic;

namespace SampleServer.PubSub
{
    /// <summary>
    /// Local Implementation of <see cref="IUaPubsubDataStore"/>
    /// </summary>
    public class UaServerDataStore : IUaPubSubDataStore
    {
        /// <summary>
        /// reference to a node manager used to locate nodes 
        /// </summary>
        private NodeManager m_associatedNodeManager;

        /// <summary>
        /// Create new instance of <see cref="UaServerDataStore"/> 
        /// </summary>
        /// <param name="associatedNodeManager"></param>
        public UaServerDataStore(NodeManager associatedNodeManager)
        {
            m_associatedNodeManager = associatedNodeManager;
        }

        #region IUaPubSubDataStore Implementation
        /// <summary>
        /// Read the DataValue stored for a specific NodeId and Attribute.
        /// </summary>
        /// <param name="nodeId">NodeId identifier of node</param>
        /// <param name="attributeId">Default value is <see cref="Attributes.Value"/></param>
        /// <returns></returns>
        public DataValue ReadPublishedDataItem(NodeId nodeId, uint attributeId = 13)
        {
            if (nodeId == null)
            {
                return null;
            }
            if (m_associatedNodeManager != null)
            {
                INodeManager typeDefinitionNodeManager = null;
                m_associatedNodeManager.Server.NodeManager.GetManagerHandle(nodeId, out typeDefinitionNodeManager);

                if (typeDefinitionNodeManager is CustomNodeManager2)
                {
                    NodeState nodeState = ((CustomNodeManager2)typeDefinitionNodeManager).FindPredefinedNode(nodeId, typeof(object));

                    DataValue dataValue = new DataValue();
                    nodeState.ReadAttribute(m_associatedNodeManager.SystemContext, attributeId, NumericRange.Empty, null, dataValue);
                    return dataValue;
                }
            }
            return null;
        }

        public DataValue ReadPublishedDataItem(NodeId nodeId, uint attributeId = Attributes.Value, bool deltaFrame = false)
        {
            return null;
        }

        public void UpdateMetaData(PublishedDataSetDataType publishedDataSet)
        {
           // todo:
        }

        /// <summary>
        /// Update metadata info in server definitions
        /// </summary>
        /// <param name="serverNodeId"></param>
        /// <param name="subscriberConnection"></param>
        /// <param name="dataSetMetaData"></param>
        public void UpdateMetaData(NodeId serverNodeId, PubSubConnectionDataType subscriberConnection, DataSetMetaDataType dataSetMetaData)
        {
            // \Objects\Server\PublishSubscribe\subscriber_connection_name\ReaderGroup x\Reader y\DataSetMetaData
            if (m_associatedNodeManager != null)
            {
                INodeManager typeDefinitionNodeManager = null;
                var a = m_associatedNodeManager.Server.NodeManager.GetManagerHandle(serverNodeId, out typeDefinitionNodeManager);

                if (typeDefinitionNodeManager is CustomNodeManager2)
                {
                    PublishSubscribeState publishSubscribeState = ((CustomNodeManager2)typeDefinitionNodeManager).FindPredefinedNode(ObjectIds.PublishSubscribe, typeof(PublishSubscribeState)) as PublishSubscribeState;
                    if (publishSubscribeState != null)
                    {
                        //string browsePaths = @"MqttUadpConnection_Subscriber\ReaderGroup 1\Reader 1";

                        // m_sessionManager is null !?
                        //ResponseHeader responseHeader = m_session.TranslateBrowsePathsToNodeIds(
                        //    null,
                        //    browsePaths,
                        //    out results,
                        //    out diagnosticInfos);

                        //var publishSubscibeManagerHandle = m_associatedNodeManager.Server.NodeManager.GetManagerHandle(publishSubscribeState.NodeId, out typeDefinitionNodeManager);

                        //RelativePathElement relativePathElement = new RelativePathElement();
                        //relativePathElement.TargetName = browsePaths;
                        //relativePathElement.IncludeSubtypes = true;
                        //IList<ExpandedNodeId> targetIds = new List<ExpandedNodeId>();
                        //IList<NodeId> nodeIds = new List<NodeId>();
                        //typeDefinitionNodeManager.TranslateBrowsePath(null, publishSubscibeManagerHandle, relativePathElement, targetIds, nodeIds);

                        PubSubConnectionState pubSubConnectionState = publishSubscribeState.FindChildBySymbolicName(null, subscriberConnection.Name) as PubSubConnectionState;
                        if (pubSubConnectionState != null)
                        {
                            foreach(ReaderGroupDataType readerGroupDataType in subscriberConnection.ReaderGroups)
                            {
                                ReaderGroupState readerGroupState = pubSubConnectionState.FindChildBySymbolicName(null, readerGroupDataType.Name) as ReaderGroupState;
                                if (readerGroupState != null)
                                {
                                    foreach(DataSetReaderDataType dataSetReaderDataType in readerGroupDataType.DataSetReaders)
                                    {
                                        DataSetReaderState dataSetReaderState = readerGroupState.FindChildBySymbolicName(null, dataSetReaderDataType.Name) as DataSetReaderState;
                                        if (dataSetReaderState != null)
                                        {
                                            dataSetReaderState.DataSetMetaData.Value = dataSetMetaData;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Write a DataValue to the DataStore. 
        /// The DataValue is identified by node NodeId and Attribute.
        /// </summary>
        /// <param name="nodeId">NodeId identifier for DataValue that will be stored</param>
        /// <param name="attributeId">Default value is <see cref="Attributes.Value"/>.</param>
        /// <param name="dataValue">Default value is null. </param>
        public void WritePublishedDataItem(NodeId nodeId, uint attributeId = 13, DataValue dataValue = null)
        {
            if (nodeId == null)
            {
                return;
            }
            if (m_associatedNodeManager != null)
            {
                INodeManager typeDefinitionNodeManager = null;
                m_associatedNodeManager.Server.NodeManager.GetManagerHandle(nodeId, out typeDefinitionNodeManager);

                if (typeDefinitionNodeManager is CustomNodeManager2)
                {
                    NodeState nodeState = ((CustomNodeManager2)typeDefinitionNodeManager).FindPredefinedNode(nodeId, typeof(object));
                                        
                    nodeState.WriteAttribute(m_associatedNodeManager.SystemContext, attributeId, NumericRange.Empty, dataValue);
                    nodeState.ClearChangeMasks(m_associatedNodeManager.SystemContext, false);
                }
            }
        }
        #endregion
    }
}
