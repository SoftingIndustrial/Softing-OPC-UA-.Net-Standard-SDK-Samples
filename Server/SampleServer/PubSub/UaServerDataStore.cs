/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
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
        public DataValue ReadPublishedDataItem(NodeId nodeId, uint attributeId = Attributes.Value)
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

        /// <summary>
        /// Updates the metadata if it has changed from when the DataStore was created.
        /// </summary>
        /// <param name="publishedDataSet"></param>
        public void UpdateMetaData(PublishedDataSetDataType publishedDataSet)
        {
           // todo:
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
