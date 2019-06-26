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
using Opc.Ua.Server;
using Softing.Opc.Ua.PubSub;
using Softing.Opc.Ua.PubSub.Configuration;
using Softing.Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer.PubSub
{
    /// <summary>
    /// Node manager specialized in PubSub functionality.
    /// It will create a new instance of <see cref="UaPubSubApplication"/> and associate it with this instance of Server.
    /// </summary>
    public class PubSubNodeManager : NodeManager
    {
        #region Private Fields
        private UaPubSubConfigurator m_uaPubSubConfigurator;
        private PublishSubscribeState m_publishSubscribeState;
        // used to generate unique node ids
        private uint m_nodeIdentifierNumber = 1;
        // maps config id to the corespondiong NodeState object created from it
        private Dictionary<uint, NodeState> m_configIdToNodeState = new Dictionary<uint, NodeState>();
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public PubSubNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.PubSub)
        {

        }

        #endregion

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // Execute base class CreateAddressSpace
                base.CreateAddressSpace(externalReferences);
                m_publishSubscribeState = FindNodeInAddressSpace(ObjectIds.PublishSubscribe) as PublishSubscribeState;

                // initialize PubSub objects     
                UaPubSubApplication pubSubApplication = UaPubSubApplication.Create();
                //remember refernce to UaPubSubConfigurator
                m_uaPubSubConfigurator = pubSubApplication.UaPubSubConfigurator;

                //attach to events
                m_uaPubSubConfigurator.PubSubStateChanged += UaPubSubConfigurator_PubSubStateChanged;
                m_uaPubSubConfigurator.PublishedDataSetAdded += UaPubSubConfigurator_PublishedDataSetAdded;
                m_uaPubSubConfigurator.PublishedDataSetRemoved += UaPubSubConfigurator_PublishedDataSetRemoved;

                m_uaPubSubConfigurator.ConnectionAdded += UaPubSubConfigurator_ConnectionAdded;
                m_uaPubSubConfigurator.ConnectionRemoved += UaPubSubConfigurator_ConnectionRemoved;

                m_uaPubSubConfigurator.WriterGroupAdded += UaPubSubConfigurator_WriterGroupAdded;
                m_uaPubSubConfigurator.WriterGroupRemoved += UaPubSubConfigurator_WriterGroupRemoved;
                m_uaPubSubConfigurator.DataSetWriterAdded += UaPubSubConfigurator_DataSetWriterAdded;
                m_uaPubSubConfigurator.DataSetWriterRemoved += UaPubSubConfigurator_DataSetWriterRemoved;
                m_uaPubSubConfigurator.ReaderGroupAdded += UaPubSubConfigurator_ReaderGroupAdded;                
                m_uaPubSubConfigurator.ReaderGroupRemoved += UaPubSubConfigurator_ReaderGroupRemoved;
                m_uaPubSubConfigurator.DataSetReaderAdded += UaPubSubConfigurator_DataSetReaderAdded;
                m_uaPubSubConfigurator.DataSetReaderRemoved += UaPubSubConfigurator_DataSetReaderRemoved;
                // load configuration
                string pubConfigurationFileName = "SamplePublisher.Config.xml";
                m_uaPubSubConfigurator.LoadConfiguration(pubConfigurationFileName);

                string subConfigurationFileName = "SampleSubscriber.Config.xml";
                m_uaPubSubConfigurator.LoadConfiguration(subConfigurationFileName, false);

                MapConfigIdToPubSubNodeState(m_uaPubSubConfigurator.FindIdForObject(m_uaPubSubConfigurator.PubSubConfiguration), m_publishSubscribeState);
                InitializePubSubStatusStateMethods(m_publishSubscribeState.Status, m_uaPubSubConfigurator.PubSubConfiguration);

                m_publishSubscribeState.AddConnection.OnCall = OnCallAddConnectionMethodHandler;
                m_publishSubscribeState.RemoveConnection.OnCall = OnCallRemoveConnectionMethodHandler;

            }
        }
        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.PubSubStateChanged"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_PubSubStateChanged(object sender, PubSubStateChangedEventArgs e)
        {
            NodeState nodeState = FindPubSubNodeState(e.ConfigurationObjectId);
            if (nodeState == null) return;
            PubSubStatusState pubSubStatusState = null;
            switch (nodeState.GetType().Name)
            {
                case nameof(PublishSubscribeState):
                    pubSubStatusState = ((PublishSubscribeState)nodeState).Status;
                    break;
                case nameof(PubSubConnectionState):
                    pubSubStatusState = ((PubSubConnectionState)nodeState).Status;
                    break;
                case nameof(WriterGroupState):
                    pubSubStatusState = ((WriterGroupState)nodeState).Status;
                    break;
                case nameof(ReaderGroupState):
                    pubSubStatusState = ((ReaderGroupState)nodeState).Status;
                    break;
                case nameof(DataSetWriterState):
                    pubSubStatusState = ((DataSetWriterState)nodeState).Status;
                    break;
                case nameof(DataSetReaderState):
                    pubSubStatusState = ((DataSetReaderState)nodeState).Status;
                    break;
            }
            // set new stoate to the status node
            if (pubSubStatusState != null)
            {
                pubSubStatusState.State.Value = e.NewState;
                pubSubStatusState.State.ClearChangeMasks(SystemContext, false);
            }
        }
        
        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.ConnectionAdded"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_ConnectionAdded(object sender, ConnectionEventArgs e)
        {
            //create connection and add it to the address space
            PubSubConnectionState pubSubConnectionState = CreateObjectFromType(m_publishSubscribeState, e.PubSubConnectionDataType.Name, 
                ObjectTypeIds.PubSubConnectionType, ReferenceTypeIds.HasPubSubConnection) as PubSubConnectionState;

            //copy properties of configuration ito node state object
            pubSubConnectionState.PublisherId.Value = e.PubSubConnectionDataType.PublisherId;
            pubSubConnectionState.TransportProfileUri.Value = e.PubSubConnectionDataType.TransportProfileUri;

            NetworkAddressUrlDataType networkAddressUrlState = ExtensionObject.ToEncodeable(e.PubSubConnectionDataType.Address)
                      as NetworkAddressUrlDataType;
            if (networkAddressUrlState != null)
            {
                NetworkAddressUrlState address = CreateObjectFromType(pubSubConnectionState, BrowseNames.Address, ObjectTypeIds.NetworkAddressUrlType) as NetworkAddressUrlState;
                address.NetworkInterface.Value = networkAddressUrlState.NetworkInterface;
                address.Url.Value = networkAddressUrlState.Url;
                pubSubConnectionState.Address = address;
            }
            
            MapConfigIdToPubSubNodeState(e.ConnectionId, pubSubConnectionState);
            InitializePubSubStatusStateMethods(pubSubConnectionState.Status, e.PubSubConnectionDataType);

            pubSubConnectionState.AddWriterGroup.OnCall = OnCallAddWriterGroupMethodHandler;
            pubSubConnectionState.AddReaderGroup.OnCall = OnCallAddReaderGroupMethodHandler;
            pubSubConnectionState.RemoveGroup.OnCall = OnCallRemoveGroupMethodHandler;
        }

        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.ConnectionRemoved"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_ConnectionRemoved(object sender, ConnectionEventArgs e)
        {
            // locate the connection node and delete it from address space
            PubSubConnectionState pubSubConnectionState = FindPubSubNodeState(e.ConnectionId) as PubSubConnectionState;
            if (pubSubConnectionState != null)
            {
                // remove from children list
                m_publishSubscribeState.RemoveChild(pubSubConnectionState);
                // remove from predefined nodes
                PredefinedNodes.Remove(pubSubConnectionState.NodeId);
                RemoveConfigIdToPubSubNodeStateMapping(pubSubConnectionState);
            }
        }

        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.WriterGroupAdded"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_WriterGroupAdded(object sender, WriterGroupEventArgs e)
        {
            //find parent connection object 
            PubSubConnectionState parentConnectionState = FindPubSubNodeState(e.ConnectionId) as PubSubConnectionState;
            if (parentConnectionState != null)
            {
                // create writer group state and add it to the address space
                WriterGroupState writerGroupState = CreateObjectFromType(parentConnectionState, e.WriterGroupDataType.Name,
                    ObjectTypeIds.WriterGroupType, ReferenceTypeIds.HasComponent) as WriterGroupState;

                //copy properties of configuration ito node state object
                writerGroupState.WriterGroupId.Value = e.WriterGroupDataType.WriterGroupId;
                writerGroupState.PublishingInterval.Value = e.WriterGroupDataType.PublishingInterval;
                writerGroupState.KeepAliveTime.Value = e.WriterGroupDataType.KeepAliveTime;
                writerGroupState.HeaderLayoutUri.Value = e.WriterGroupDataType.HeaderLayoutUri;
                //writerGroupState.MessageSettings
                UadpWriterGroupMessageDataType messageSettings = ExtensionObject.ToEncodeable(e.WriterGroupDataType.MessageSettings)
                      as UadpWriterGroupMessageDataType;
                if (messageSettings != null)
                {
                    UadpWriterGroupMessageState messageSettingsState = CreateObjectFromType(writerGroupState, BrowseNames.MessageSettings, ObjectTypeIds.UadpWriterGroupMessageType) 
                        as UadpWriterGroupMessageState;
                    messageSettingsState.GroupVersion.Value = messageSettings.GroupVersion;
                    messageSettingsState.DataSetOrdering.Value = messageSettings.DataSetOrdering;
                    messageSettingsState.NetworkMessageContentMask.Value = messageSettings.NetworkMessageContentMask;
                    //messageSettingsState.PublishingOffset.Value = messageSettings.PublishingOffset;

                    writerGroupState.MessageSettings = messageSettingsState;
                }

                //writerGroupState.TransportSettings
                DatagramWriterGroupTransportDataType transportSettings = ExtensionObject.ToEncodeable(e.WriterGroupDataType.TransportSettings)
                      as DatagramWriterGroupTransportDataType;
                if (transportSettings != null)
                {
                    DatagramWriterGroupTransportState datagramWriterGroupTransportState = CreateObjectFromType(writerGroupState, BrowseNames.TransportSettings, ObjectTypeIds.DatagramWriterGroupTransportType)
                        as DatagramWriterGroupTransportState;

                    writerGroupState.TransportSettings = datagramWriterGroupTransportState;
                }

                MapConfigIdToPubSubNodeState(e.WriterGroupId, writerGroupState);
                InitializePubSubStatusStateMethods(writerGroupState.Status, e.WriterGroupDataType);

                writerGroupState.AddDataSetWriter.OnCall = OnCallAddDataSetWriterHandler;
                writerGroupState.RemoveDataSetWriter.OnCall = OnCallRemoveDataSetWriterHandler;
            }            
        }

        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.WriterGroupRemoved"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_WriterGroupRemoved(object sender, WriterGroupEventArgs e)
        {
            // locate the writerGroup node and delete it from address space
            WriterGroupState writerGroupState = FindPubSubNodeState(e.WriterGroupId) as WriterGroupState;
            if (writerGroupState != null)
            {
                //find parent connection
                PubSubConnectionState parentConnection = writerGroupState.Parent as PubSubConnectionState;
                if (parentConnection != null)
                {
                    // remove from children list
                    parentConnection.RemoveChild(writerGroupState);
                    // remove from predefined nodes
                    PredefinedNodes.Remove(writerGroupState.NodeId);
                    RemoveConfigIdToPubSubNodeStateMapping(writerGroupState);
                }
            }
        }

        /// <summary>
        ///  Handler for <see cref="UaPubSubConfigurator.DataSetWriterAdded"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_DataSetWriterAdded(object sender, DataSetWriterEventArgs e)
        {
            //find parent writerGroup object 
            WriterGroupState parentWriterGroupState = FindPubSubNodeState(e.WriterGroupId) as WriterGroupState;
            if (parentWriterGroupState != null)
            {
                // create dataset writer state and add it to the address space
                DataSetWriterState dataSetWriterState = CreateObjectFromType(parentWriterGroupState, e.DataSetWriterDataType.Name,
                    ObjectTypeIds.DataSetWriterType, ReferenceTypeIds.HasDataSetWriter) as DataSetWriterState;

                //copy properties of configuration ito node state object
                dataSetWriterState.DataSetWriterId.Value = e.DataSetWriterDataType.DataSetWriterId;
                dataSetWriterState.DataSetFieldContentMask.Value = e.DataSetWriterDataType.DataSetFieldContentMask;
               // dataSetWriterGroupState.KeyFrameCount.Value = e.DataSetWriterDataType.KeyFrameCount;
                //.MessageSettings
                UadpDataSetWriterMessageDataType messageSettings = ExtensionObject.ToEncodeable(e.DataSetWriterDataType.MessageSettings)
                      as UadpDataSetWriterMessageDataType;
                if (messageSettings != null)
                {
                    UadpDataSetWriterMessageState messageSettingsState = CreateObjectFromType(dataSetWriterState, BrowseNames.MessageSettings, ObjectTypeIds.UadpDataSetWriterMessageType)
                        as UadpDataSetWriterMessageState;
                    messageSettingsState.ConfiguredSize.Value = messageSettings.ConfiguredSize;
                    messageSettingsState.DataSetOffset.Value = messageSettings.DataSetOffset;
                    messageSettingsState.DataSetMessageContentMask.Value = messageSettings.DataSetMessageContentMask;
                    messageSettingsState.NetworkMessageNumber.Value = messageSettings.NetworkMessageNumber;

                    dataSetWriterState.MessageSettings = messageSettingsState;
                }

                //.TransportSettings              

                MapConfigIdToPubSubNodeState(e.DataSetWriterId, dataSetWriterState);
                InitializePubSubStatusStateMethods(dataSetWriterState.Status, e.DataSetWriterDataType);
            }
        }

        /// <summary>
        ///  Handler for <see cref="UaPubSubConfigurator.DataSetWriterRemoved"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_DataSetWriterRemoved(object sender, DataSetWriterEventArgs e)
        {
            // locate the Group node and delete it from address space
            DataSetWriterState dataSetWriterState = FindPubSubNodeState(e.DataSetWriterId) as DataSetWriterState;
            if (dataSetWriterState != null)
            {
                //find parent connection
                WriterGroupState parentWriterGroup = dataSetWriterState.Parent as WriterGroupState;
                if (parentWriterGroup != null)
                {
                    // remove from children list
                    parentWriterGroup.RemoveChild(dataSetWriterState);
                    // remove from predefined nodes
                    PredefinedNodes.Remove(dataSetWriterState.NodeId);
                    RemoveConfigIdToPubSubNodeStateMapping(dataSetWriterState);
                }
            }
        }

        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.ReaderGroupAdded"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_ReaderGroupAdded(object sender, ReaderGroupEventArgs e)
        {
            //find parent connection object 
            PubSubConnectionState parentConnectionState = FindPubSubNodeState(e.ConnectionId) as PubSubConnectionState;
            if (parentConnectionState != null)
            {
                // create reader group state and add it to the address space
                ReaderGroupState readerGroupState = CreateObjectFromType(parentConnectionState, e.ReaderGroupDataType.Name,
                    ObjectTypeIds.ReaderGroupType, ReferenceTypeIds.HasComponent) as ReaderGroupState;

                //ReaderGroupState.MessageSettings
                ReaderGroupMessageDataType messageSettings = ExtensionObject.ToEncodeable(e.ReaderGroupDataType.MessageSettings)
                      as ReaderGroupMessageDataType;
                if (messageSettings != null)
                {
                    ReaderGroupMessageState messageSettingsState = CreateObjectFromType(readerGroupState, BrowseNames.MessageSettings, ObjectTypeIds.ReaderGroupMessageType)
                        as ReaderGroupMessageState;
                    readerGroupState.MessageSettings = messageSettingsState;
                }

                //ReaderGroupState.TransportSettings
                ReaderGroupTransportDataType transportSettings = ExtensionObject.ToEncodeable(e.ReaderGroupDataType.TransportSettings)
                      as ReaderGroupTransportDataType;
                if (transportSettings != null)
                {
                    ReaderGroupTransportState readerGroupTransportState = CreateObjectFromType(readerGroupState, BrowseNames.TransportSettings, ObjectTypeIds.ReaderGroupTransportType)
                        as ReaderGroupTransportState;

                    readerGroupState.TransportSettings = readerGroupTransportState;
                }

                MapConfigIdToPubSubNodeState(e.ReaderGroupId, readerGroupState);
                InitializePubSubStatusStateMethods(readerGroupState.Status, e.ReaderGroupDataType);

                readerGroupState.AddDataSetReader.OnCall = OnCallAddDataSetReaderHandler;
                readerGroupState.RemoveDataSetReader.OnCall = OnCallRemoveDataSetReaderHandler;
            }
        }

        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.ReaderGroupRemoved"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_ReaderGroupRemoved(object sender, ReaderGroupEventArgs e)
        {
            // locate the readerGroup node and delete it from address space
            ReaderGroupState readerGroupState = FindPubSubNodeState(e.ReaderGroupId) as ReaderGroupState;
            if (readerGroupState != null)
            {
                //find parent connection
                PubSubConnectionState parentConnection = readerGroupState.Parent as PubSubConnectionState;
                if (parentConnection != null)
                {
                    // remove from children list
                    parentConnection.RemoveChild(readerGroupState);
                    // remove from predefined nodes
                    PredefinedNodes.Remove(readerGroupState.NodeId);
                    RemoveConfigIdToPubSubNodeStateMapping(readerGroupState);
                }
            }
        }

        /// <summary>
        ///  Handler for <see cref="UaPubSubConfigurator.DataSetReaderAdded"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_DataSetReaderAdded(object sender, DataSetReaderEventArgs e)
        {
            //find parent readerGroup object 
            ReaderGroupState parentReaderGroupState = FindPubSubNodeState(e.ReaderGroupId) as ReaderGroupState;
            if (parentReaderGroupState != null)
            {
                // create dataset reader state and add it to the address space
                DataSetReaderState dataSetreaderState = CreateObjectFromType(parentReaderGroupState, e.DataSetReaderDataType.Name,
                    ObjectTypeIds.DataSetReaderType, ReferenceTypeIds.HasDataSetReader) as DataSetReaderState;

                //copy properties of configuration ito node state object
                dataSetreaderState.PublisherId.Value = e.DataSetReaderDataType.PublisherId;
                dataSetreaderState.WriterGroupId.Value = e.DataSetReaderDataType.WriterGroupId;
                dataSetreaderState.DataSetWriterId.Value = e.DataSetReaderDataType.DataSetWriterId;
                dataSetreaderState.DataSetMetaData.Value = e.DataSetReaderDataType.DataSetMetaData;
                dataSetreaderState.DataSetFieldContentMask.Value = e.DataSetReaderDataType.DataSetFieldContentMask;
                dataSetreaderState.MessageReceiveTimeout.Value = e.DataSetReaderDataType.MessageReceiveTimeout;

                //SubscribedDataSet
                TargetVariablesDataType subscribedDataSet = ExtensionObject.ToEncodeable(e.DataSetReaderDataType.SubscribedDataSet)
                      as TargetVariablesDataType;
                if (subscribedDataSet != null)
                {
                    TargetVariablesState targetVariablesState = CreateObjectFromType(dataSetreaderState, BrowseNames.SubscribedDataSet, ObjectTypeIds.TargetVariablesType)
                        as TargetVariablesState;
                    targetVariablesState.TargetVariables.Value = subscribedDataSet.TargetVariables.ToArray();
                    //if (dataSetreaderState.SubscribedDataSet != null)
                    //{
                    //    dataSetreaderState.ReplaceChild(SystemContext, targetVariablesState);
                    //}
                    //else
                    {
                        dataSetreaderState.SubscribedDataSet = targetVariablesState;
                    }
                }

                //.MessageSettings
                UadpDataSetReaderMessageDataType messageSettings = ExtensionObject.ToEncodeable(e.DataSetReaderDataType.MessageSettings)
                    as UadpDataSetReaderMessageDataType;
                if (messageSettings != null)
                {
                    UadpDataSetReaderMessageState messageSettingsState = CreateObjectFromType(dataSetreaderState, BrowseNames.MessageSettings, ObjectTypeIds.UadpDataSetReaderMessageType)
                        as UadpDataSetReaderMessageState;
                    messageSettingsState.GroupVersion.Value = messageSettings.GroupVersion;
                    messageSettingsState.DataSetOffset.Value = messageSettings.DataSetOffset;                    
                    messageSettingsState.NetworkMessageNumber.Value = messageSettings.NetworkMessageNumber;
                    messageSettingsState.DataSetMessageContentMask.Value = messageSettings.DataSetMessageContentMask;
                    messageSettingsState.NetworkMessageContentMask.Value = messageSettings.NetworkMessageContentMask;

                    dataSetreaderState.MessageSettings = messageSettingsState;
                }

                //.TransportSettings              

                MapConfigIdToPubSubNodeState(e.DataSetReaderId, dataSetreaderState);
                InitializePubSubStatusStateMethods(dataSetreaderState.Status, e.DataSetReaderDataType);
            }
        }
        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.DataSetReaderRemoved"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_DataSetReaderRemoved(object sender, DataSetReaderEventArgs e)
        {
            // locate the Group node and delete it from address space
            DataSetReaderState dataSetReaderState = FindPubSubNodeState(e.DataSetReaderId) as DataSetReaderState;
            if (dataSetReaderState != null)
            {
                //find parent group
                ReaderGroupState parentReaderGroup = dataSetReaderState.Parent as ReaderGroupState;
                if (parentReaderGroup != null)
                {
                    // remove from children list
                    parentReaderGroup.RemoveChild(dataSetReaderState);
                    // remove from predefined nodes
                    PredefinedNodes.Remove(dataSetReaderState.NodeId);
                    RemoveConfigIdToPubSubNodeStateMapping(dataSetReaderState);
                }
            }
        }
        
        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.PublishedDataSetRemoved"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_PublishedDataSetRemoved(object sender, PublishedDataSetEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.PublishedDataSetAdded"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_PublishedDataSetAdded(object sender, PublishedDataSetEventArgs e)
        {
            throw new NotImplementedException();
        }

        #region INodeIdFactory Members
        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>

        public override NodeId New(ISystemContext context, NodeState node)
        {
            lock (Lock)
            {
                return new NodeId(m_nodeIdentifierNumber++, NamespaceIndex);
            }
        }
        #endregion

        /// <summary>
        /// Handler for OnCall event of <see cref="PublishSubscribeState.AddConnection"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="configuration"></param>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        private ServiceResult OnCallAddConnectionMethodHandler(ISystemContext context,  MethodState method, NodeId objectId,  PubSubConnectionDataType configuration, ref NodeId connectionId)
        {
            if (configuration != null)
            {
                //force uadp connection - temporary fix for usage from datafeed client
                configuration.TransportProfileUri = UaPubSubApplication.UadpTransportProfileUri;
                StatusCode resultStatusCode = m_uaPubSubConfigurator.AddConnection(configuration);
                if (StatusCode.IsBad(resultStatusCode))
                {
                    return resultStatusCode;                   
                }
                uint connectionConfigId = m_uaPubSubConfigurator.FindIdForObject(configuration);   
                //find node state created by ConnectionAdded event hadler in address space 
                NodeState connectionNodeState = FindPubSubNodeState(connectionConfigId);
                if (connectionNodeState != null)
                {
                    connectionId = connectionNodeState.NodeId;
                    return StatusCodes.Good;
                }                
            }
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="PublishSubscribeState.RemoveConnection"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        private ServiceResult OnCallRemoveConnectionMethodHandler(ISystemContext context, MethodState method, NodeId objectId, NodeId connectionId)
        {
            // find connection node to be removed
            PubSubConnectionState pubSubConnectionState = FindNodeInAddressSpace(connectionId) as PubSubConnectionState;
            if (pubSubConnectionState != null && pubSubConnectionState.Handle is uint)
            {
                uint connectionConfigId = (uint)pubSubConnectionState.Handle;
                return m_uaPubSubConfigurator.RemoveConnection(connectionConfigId);                
            }
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        ///  Handler for OnCall event of <see cref="PubSubConnectionState.AddWriterGroup"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="configuration"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        private ServiceResult OnCallAddWriterGroupMethodHandler(ISystemContext context, MethodState method, NodeId objectId, WriterGroupDataType configuration, ref NodeId groupId)
        {
            if (configuration != null)
            {
                //locate parent connnection
                PubSubConnectionState connectionNodeState = method.Parent as PubSubConnectionState;

                if (connectionNodeState != null && connectionNodeState.Handle is uint)
                {
                    uint connectionConfigId = (uint)connectionNodeState.Handle;
                    StatusCode resultStatusCode = m_uaPubSubConfigurator.AddWriterGroup(connectionConfigId, configuration);
                    if (StatusCode.IsBad(resultStatusCode))
                    {
                        return resultStatusCode;
                    }
                    uint writerGroupConfigId = m_uaPubSubConfigurator.FindIdForObject(configuration);
                    NodeState writerGroupNodeState = FindPubSubNodeState(connectionConfigId);
                    if (writerGroupNodeState != null)
                    {
                        groupId = writerGroupNodeState.NodeId;
                        return StatusCodes.Good;
                    }
                }
            }
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="WriterGroupState.AddDataSetWriter"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="configuration"></param>
        /// <param name="dataSetWriterNodeId"></param>
        /// <returns></returns>
        private ServiceResult OnCallAddDataSetWriterHandler(ISystemContext context, MethodState method, NodeId objectId, DataSetWriterDataType configuration, ref NodeId dataSetWriterNodeId)
        {
            if (configuration != null)
            {
                //locate parent writer group[
                WriterGroupState writerGroupNodeState = method.Parent as WriterGroupState;

                if (writerGroupNodeState != null && writerGroupNodeState.Handle is uint)
                {
                    uint writerGroupConfigId = (uint)writerGroupNodeState.Handle;
                    StatusCode resultStatusCode = m_uaPubSubConfigurator.AddDataSetWriter(writerGroupConfigId, configuration);
                    if (StatusCode.IsBad(resultStatusCode))
                    {
                        return resultStatusCode;
                    }
                    uint dataSetWriterConfigId = m_uaPubSubConfigurator.FindIdForObject(configuration);
                    NodeState dataSetWriterNodeState = FindPubSubNodeState(dataSetWriterConfigId);
                    if (dataSetWriterNodeState != null)
                    {
                        dataSetWriterNodeId = dataSetWriterNodeState.NodeId;
                        return StatusCodes.Good;
                    }
                }
            }
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="WriterGroupState.RemoveDataSetWriter"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="dataSetWriterNodeId"></param>
        /// <returns></returns>
        private ServiceResult OnCallRemoveDataSetWriterHandler(ISystemContext context, MethodState method, NodeId objectId, NodeId dataSetWriterNodeId)
        {
            // find node to be removed
            DataSetWriterState dataSetWriterState = FindNodeInAddressSpace(dataSetWriterNodeId) as DataSetWriterState;
            if (dataSetWriterState != null && dataSetWriterState.Handle is uint)
            {
                uint configId = (uint)dataSetWriterState.Handle;
                return m_uaPubSubConfigurator.RemoveDataSetWriter(configId);
            }
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="PubSubConnectionState.AddReaderGroup"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="configuration"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        private ServiceResult OnCallAddReaderGroupMethodHandler(ISystemContext context, MethodState method, NodeId objectId, ReaderGroupDataType configuration, ref NodeId groupId)
        {
            if (configuration != null)
            {
                //locate parent connnection
                PubSubConnectionState connectionNodeState = method.Parent as PubSubConnectionState;

                if (connectionNodeState != null && connectionNodeState.Handle is uint)
                {
                    uint connectionConfigId = (uint)connectionNodeState.Handle;
                    StatusCode resultStatusCode = m_uaPubSubConfigurator.AddReaderGroup(connectionConfigId, configuration);
                    if (StatusCode.IsBad(resultStatusCode))
                    {
                        return resultStatusCode;
                    }
                    uint readerGroupConfigId = m_uaPubSubConfigurator.FindIdForObject(configuration);
                    NodeState readerGroupNodeState = FindPubSubNodeState(connectionConfigId);
                    if (readerGroupNodeState != null)
                    {
                        groupId = readerGroupNodeState.NodeId;
                        return StatusCodes.Good;
                    }
                }
            }
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="ReaderGroupState.AddDataSetReader"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="configuration"></param>
        /// <param name="dataSetReaderNodeId"></param>
        /// <returns></returns>
        private ServiceResult OnCallAddDataSetReaderHandler(ISystemContext context, MethodState method, NodeId objectId, DataSetReaderDataType configuration, ref NodeId dataSetReaderNodeId)
        {
            if (configuration != null)
            {
                //locate parent writer group[
                ReaderGroupState readerGroupState = method.Parent as ReaderGroupState;

                if (readerGroupState != null && readerGroupState.Handle is uint)
                {
                    uint readerGroupConfigId = (uint)readerGroupState.Handle;
                    StatusCode resultStatusCode = m_uaPubSubConfigurator.AddDataSetReader(readerGroupConfigId, configuration);
                    if (StatusCode.IsBad(resultStatusCode))
                    {
                        return resultStatusCode;
                    }
                    uint dataSetReaderConfigId = m_uaPubSubConfigurator.FindIdForObject(configuration);
                    NodeState dataSetReaderNodeState = FindPubSubNodeState(dataSetReaderConfigId);
                    if (dataSetReaderNodeState != null)
                    {
                        dataSetReaderNodeId = dataSetReaderNodeState.NodeId;
                        return StatusCodes.Good;
                    }
                }
            }
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="ReaderGroupState.RemoveDataSetReader"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="dataSetReaderNodeId"></param>
        /// <returns></returns>
        private ServiceResult OnCallRemoveDataSetReaderHandler(ISystemContext context, MethodState method, NodeId objectId, NodeId dataSetReaderNodeId)
        {
            // find node to be removed
            DataSetReaderState dataSeReaderState = FindNodeInAddressSpace(dataSetReaderNodeId) as DataSetReaderState;
            if (dataSeReaderState != null && dataSeReaderState.Handle is uint)
            {
                uint configId = (uint)dataSeReaderState.Handle;
                return m_uaPubSubConfigurator.RemoveDataSetReader(configId);
            }
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="PubSubConnectionState.RemoveGroup"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        private ServiceResult OnCallRemoveGroupMethodHandler(ISystemContext context, MethodState method, NodeId objectId, NodeId groupId)
        {
            // find group node to be removed
            NodeState groupNodeState = FindNodeInAddressSpace(groupId);

            if (groupNodeState.Handle is uint)
            {
                uint groupConfigId = (uint)groupNodeState.Handle;
                if (groupNodeState is WriterGroupState)
                {
                    return m_uaPubSubConfigurator.RemoveWriterGroup(groupConfigId);
                }
                if (groupNodeState is ReaderGroupState)
                {
                    return m_uaPubSubConfigurator.RemoveReaderGroup(groupConfigId);
                }                 
            }
            return StatusCodes.BadNodeIdUnknown;
        }


        /// <summary>
        /// Handler for OnCall event of <see cref="PubSubStatusState.Disable"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult OnCallPubSubStatusDisableHandler(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (method != null && method.Handle is uint)
            {
                //find coresponding configObjectId 
                uint configId = (uint)method.Handle;

                return m_uaPubSubConfigurator.Disable(configId);
            }
            return StatusCodes.Bad;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="PubSubStatusState.Enable"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult OnCallPubSubStatusEnableHandler(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (method != null && method.Handle is uint)
            {
                //find coresponding configObjectId 
                uint configId = (uint)method.Handle;

                return m_uaPubSubConfigurator.Enable(configId);
            }
            return StatusCodes.Bad;
        }

        /// <summary>
        /// Initialize <see cref="PubSubStatusState.Enable"/> and  <see cref="PubSubStatusState.Disable"/> methods of a <see cref="PubSubStatusState"/> object.
        /// </summary>
        /// <param name="statusNode"></param>
        /// <param name="configurationObject"></param>
        private void InitializePubSubStatusStateMethods(PubSubStatusState statusNode, object configurationObject)
        {
            uint configId = m_uaPubSubConfigurator.FindIdForObject(configurationObject);
            if (statusNode.Enable == null)
            {
                statusNode.Enable = CreateMethod(statusNode, BrowseNames.Enable, null, null, OnCallPubSubStatusEnableHandler);
                statusNode.Enable.Handle = configId;
            }
            if (statusNode.Disable == null)
            {
                statusNode.Disable = CreateMethod(statusNode, BrowseNames.Disable, null, null, OnCallPubSubStatusDisableHandler);
                statusNode.Disable.Handle = configId;
            }

            PubSubState initialState = m_uaPubSubConfigurator.FindStateForObject(configurationObject);
            if (statusNode.State.Value != initialState)
            {
                statusNode.State.Value = initialState;
                statusNode.State.ClearChangeMasks(SystemContext, false);
            }
        }

        /// <summary>
        /// Associates a PubSub NodeState with a configuration Id
        /// Later this node will be available using the <see cref="PubSubNodeManager.FindPubSubNodeState(uint)"/> method.
        /// </summary>
        /// <param name="configId"></param>
        /// <param name="nodeState"></param>
        private void MapConfigIdToPubSubNodeState(uint configId, NodeState nodeState)
        {
            if (m_configIdToNodeState.ContainsKey(configId))
            {
                //throw wxception or something
                return;
            }
            m_configIdToNodeState.Add(configId, nodeState);
            nodeState.Handle = configId;
        }

        /// <summary>
        /// Deletes association between PubSub NodeState and a configuration Id
        /// </summary>
        /// <param name="nodeState"></param>
        private void RemoveConfigIdToPubSubNodeStateMapping(NodeState nodeState)
        {
            if (nodeState.Handle is uint)
            {
                uint configId = (uint)nodeState.Handle;
                nodeState.Handle = null;
                if (m_configIdToNodeState.ContainsKey(configId))
                {
                    m_configIdToNodeState.Remove(configId);
                }
            }
        }

        /// <summary>
        /// Finds a PubSub NodeState based on its assigned confgurationId 
        /// (the NodeState was previously mapped using <see cref="PubSubNodeManager.MapConfigIdToPubSubNodeState(uint, NodeState)"/> method).
        /// </summary>
        /// <param name="configId"></param>
        /// <returns></returns>
        private NodeState FindPubSubNodeState(uint configId)
        {
            if (m_configIdToNodeState.ContainsKey(configId))
            {
                return m_configIdToNodeState[configId];

            }
            return null;
        }
    }
}
