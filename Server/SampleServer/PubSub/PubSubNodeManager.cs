/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.PubSub;
using Softing.Opc.Ua.PubSub.Configuration;
using Softing.Opc.Ua.PubSub.PublishedData;
using Softing.Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Threading;
using KeyValuePair = Opc.Ua.KeyValuePair;

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
        // maps config id to the corresponding NodeState object created from it
        private Dictionary<uint, NodeState> m_configIdToNodeState = new Dictionary<uint, NodeState>();

        // simulation for value nodes
        private Timer m_simulationTimer;
        private readonly List<BaseDataVariableState> m_dynamicNodes;
        private bool m_canStartPubSubApplication = false;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public PubSubNodeManager(IServerInternal server, ApplicationConfiguration configuration, bool canStartPubSubApplication = false) : base(server, configuration, Namespaces.PubSub)
        {
            m_canStartPubSubApplication = canStartPubSubApplication;
            m_dynamicNodes = new List<BaseDataVariableState>();
        }

        #endregion

        #region CreateAddressSpace Method
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

                //update m_publishSubscribeState.SupportedTransportProfiles with existing implementations in PubSub library
                m_publishSubscribeState.SupportedTransportProfiles.Value = UaPubSubApplication.SupportedTransportProfiles;

                // create PubSub application     
                UaPubSubApplication pubSubApplication = UaPubSubApplication.Create(new UaServerDataStore(this));
                // remember reference to UaPubSubConfigurator
                m_uaPubSubConfigurator = pubSubApplication.UaPubSubConfigurator;

                //attach to events
                m_uaPubSubConfigurator.PubSubStateChanged += UaPubSubConfigurator_PubSubStateChanged;
                m_uaPubSubConfigurator.PublishedDataSetAdded += UaPubSubConfigurator_PublishedDataSetAdded;
                m_uaPubSubConfigurator.PublishedDataSetRemoved += UaPubSubConfigurator_PublishedDataSetRemoved;
                m_uaPubSubConfigurator.ExtensionFieldAdded += UaPubSubConfigurator_ExtensionFieldAdded;
                m_uaPubSubConfigurator.ExtensionFieldRemoved += UaPubSubConfigurator_ExtensionFieldRemoved;
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

                // add OnCall handlers for m_publishSubscribeState methods 
                m_publishSubscribeState.AddConnection.OnCall = OnCallAddConnectionMethodHandler;
                m_publishSubscribeState.RemoveConnection.OnCall = OnCallRemoveConnectionHandler;

                InitializeDataSetFolderState(m_publishSubscribeState.PublishedDataSets);

                // create Publisher Source Nodes
                FolderState root = CreateFolder(null, "PubSub");
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                #region Add Publisher Source Nodes
                FolderState publisher = CreateFolder(root, "Publisher");

                BaseDataVariableState variable = CreateVariable(publisher, "BoolToggle", DataTypeIds.Boolean, ValueRanks.Scalar, new NodeId("Pub_BoolToggle", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "Int32", DataTypeIds.Int32, ValueRanks.Scalar, new NodeId("Pub_Int32", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "Int32Fast", DataTypeIds.Int32, ValueRanks.Scalar, new NodeId("Pub_Int32Fast", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar, new NodeId("Pub_DateTime", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "Byte", DataTypeIds.Byte, ValueRanks.Scalar, new NodeId("Pub_Byte", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "Int16", DataTypeIds.Int16, ValueRanks.Scalar, new NodeId("Pub_Int16", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "SByte", DataTypeIds.SByte, ValueRanks.Scalar, new NodeId("Pub_SByte", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar, new NodeId("Pub_UInt16", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar, new NodeId("Pub_UInt32", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "Float", DataTypeIds.Float, ValueRanks.Scalar, new NodeId("Pub_Float", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                variable = CreateVariable(publisher, "Double", DataTypeIds.Double, ValueRanks.Scalar, new NodeId("Pub_Double", NamespaceIndex));
                m_dynamicNodes.Add(variable);
                for(int i= 0; i < 100; i++)
                {
                    string name = "Mass_" + i;
                    variable = CreateVariable(publisher, name, DataTypeIds.UInt32, ValueRanks.Scalar, new NodeId("Pub_" + name, NamespaceIndex));
                    m_dynamicNodes.Add(variable);
                }
                m_simulationTimer = new Timer(DoSimulation, null, 1000, 1000);
                #endregion

                #region Add Subscriber Destination Nodes
                FolderState subscriber = CreateFolder(root, "Subscriber");

                variable = CreateVariable(subscriber, "BoolToggle", DataTypeIds.Boolean, ValueRanks.Scalar, new NodeId("Sub_BoolToggle", NamespaceIndex));
                variable = CreateVariable(subscriber, "Int32", DataTypeIds.Int32, ValueRanks.Scalar, new NodeId("Sub_Int32", NamespaceIndex));
                variable = CreateVariable(subscriber, "Int32Fast", DataTypeIds.Int32, ValueRanks.Scalar, new NodeId("Sub_Int32Fast", NamespaceIndex));
                variable = CreateVariable(subscriber, "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar, new NodeId("Sub_DateTime", NamespaceIndex));
                variable = CreateVariable(subscriber, "Byte", DataTypeIds.Byte, ValueRanks.Scalar, new NodeId("Sub_Byte", NamespaceIndex));
                variable = CreateVariable(subscriber, "Int16", DataTypeIds.Int16, ValueRanks.Scalar, new NodeId("Sub_Int16", NamespaceIndex));
                variable = CreateVariable(subscriber, "SByte", DataTypeIds.SByte, ValueRanks.Scalar, new NodeId("Sub_SByte", NamespaceIndex));
                variable = CreateVariable(subscriber, "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar, new NodeId("Sub_UInt16", NamespaceIndex));
                variable = CreateVariable(subscriber, "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar, new NodeId("Sub_UInt32", NamespaceIndex));
                variable = CreateVariable(subscriber, "Float", DataTypeIds.Float, ValueRanks.Scalar, new NodeId("Sub_Float", NamespaceIndex));
                variable = CreateVariable(subscriber, "Double", DataTypeIds.Double, ValueRanks.Scalar, new NodeId("Sub_Double", NamespaceIndex));
                for (int i = 0; i < 100; i++)
                {
                    string name = "Mass_" + i;
                    variable = CreateVariable(subscriber, name, DataTypeIds.UInt32, ValueRanks.Scalar, new NodeId("Sub_" + name, NamespaceIndex));
                }
                #endregion

                if (m_canStartPubSubApplication)
                {
                    pubSubApplication.DataReceived += PubSubApplication_DataReceived;
                    pubSubApplication.Start();
                }
            }
        }
        
        #endregion

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

        #region UaPubSubConfigurator Event Handlers
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
            // set new state to the status node
            if (pubSubStatusState != null)
            {
                pubSubStatusState.State.Value = e.NewState;
                pubSubStatusState.State.ClearChangeMasks(SystemContext, false);
            }
        }

        /// <summary>
        /// Handler for <see cref="UaPubSubConfigurator.PublishedDataSetAdded"/> event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_PublishedDataSetAdded(object sender, PublishedDataSetEventArgs e)
        {
            DataSetFolderState parentDataSetFolderState = m_publishSubscribeState.PublishedDataSets;

            PublishedDataItemsDataType publishedDataItemsDataType = ExtensionObject.ToEncodeable(e.PublishedDataSetDataType.DataSetSource)
                      as PublishedDataItemsDataType;
            if (publishedDataItemsDataType != null)
            {
                // locate parent folder
                if (e.PublishedDataSetDataType.DataSetFolder != null)
                {
                    foreach(string folderName in e.PublishedDataSetDataType.DataSetFolder)
                    {                        
                        DataSetFolderState folder = parentDataSetFolderState.FindChild(SystemContext, new QualifiedName(folderName, NamespaceIndex)) as DataSetFolderState;
                        if (folder == null)
                        {
                            folder = CreateObjectFromType(parentDataSetFolderState, folderName, ObjectTypeIds.DataSetFolderType, ReferenceTypeIds.Organizes) as DataSetFolderState;
                        }
                        parentDataSetFolderState = folder;
                    }
                }

                //create published data set and add it to the address space
                PublishedDataItemsState publishedDataItemsState = CreateObjectFromType(parentDataSetFolderState, e.PublishedDataSetDataType.Name,
                    ObjectTypeIds.PublishedDataItemsType, ReferenceTypeIds.HasComponent) as PublishedDataItemsState;

                //copy properties of configuration into node state object
                publishedDataItemsState.ConfigurationVersion.Value = e.PublishedDataSetDataType.DataSetMetaData.ConfigurationVersion;
                publishedDataItemsState.DataSetMetaData.Value = e.PublishedDataSetDataType.DataSetMetaData;
                publishedDataItemsState.PublishedData.Value = publishedDataItemsDataType.PublishedData.ToArray();

                if (publishedDataItemsState.ExtensionFields == null)
                {
                    publishedDataItemsState.ExtensionFields = CreateObjectFromType(publishedDataItemsState, BrowseNames.ExtensionFields,
                        ObjectTypeIds.ExtensionFieldsType, ReferenceTypeIds.HasComponent) as ExtensionFieldsState;                    
                    if (publishedDataItemsState.ExtensionFields.AddExtensionField == null)
                    {
                        Argument[] inputArguments = new Argument[]
                         {
                             new Argument(){Name = "FieldName", DataType = DataTypeIds.QualifiedName, ValueRank = ValueRanks.Scalar, Description = "Name of the field to add."},
                             new Argument(){Name = "FieldValue", DataType = DataTypeIds.BaseDataType,  ValueRank = ValueRanks.Scalar, Description = "The value of the field to add."},
                         };
                        Argument[] outputArguments = new Argument[]
                        {
                         new Argument(){Name = "FieldId", DataType = DataTypeIds.NodeId,  ValueRank = ValueRanks.Scalar, Description = "The NodeId of the added field Property."},
                        };
                        publishedDataItemsState.ExtensionFields.AddExtensionField = CreateMethod(publishedDataItemsState.ExtensionFields, BrowseNames.AddExtensionField, inputArguments, outputArguments, null,
                            typeof(AddExtensionFieldMethodState), MethodIds.ExtensionFieldsType_AddExtensionField) as AddExtensionFieldMethodState;
                    }
                    publishedDataItemsState.ExtensionFields.AddExtensionField.OnCall = OnCallAddExtensionFieldHandler;
                    if (publishedDataItemsState.ExtensionFields.RemoveExtensionField == null)
                    {
                        Argument[] inputArguments = new Argument[]
                        {
                         new Argument(){Name = "FieldId", DataType = DataTypeIds.NodeId,  ValueRank = ValueRanks.Scalar, Description = "The NodeId field Property to remove."},
                        };
                        publishedDataItemsState.ExtensionFields.RemoveExtensionField = CreateMethod(publishedDataItemsState.ExtensionFields, BrowseNames.AddExtensionField, inputArguments, null, null,
                            typeof(RemoveExtensionFieldMethodState), MethodIds.ExtensionFieldsType_RemoveExtensionField) as RemoveExtensionFieldMethodState;
                    }
                    publishedDataItemsState.ExtensionFields.RemoveExtensionField.OnCall = OnCallRemoveExtensionFieldHandler;
                }
                InitializePublishedDataItemsState(publishedDataItemsState);

                MapConfigIdToPubSubNodeState(e.PublishedDataSetId, publishedDataItemsState);
            }
        }

        /// <summary>
        /// Handler for Removed events from <see cref="UaPubSubConfigurator"/>.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_PublishedDataSetRemoved(object sender, PublishedDataSetEventArgs e)
        {
            // locate the UOP UA Server node and delete it from address space
            BaseInstanceState nodeToRemove = FindPubSubNodeState(e.PublishedDataSetId) as BaseInstanceState;
            RemoveNodeFromAddressSpace(nodeToRemove);
        }

        /// <summary>
        /// Handler for ExtensionFieldAdded events from <see cref="UaPubSubConfigurator"/>.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_ExtensionFieldAdded(object sender, ExtensionFieldEventArgs e)
        {
            // find parent published data set 
            PublishedDataSetState publishedDataSetState = FindPubSubNodeState(e.PublishedDataSetId) as PublishedDataSetState;
            if (publishedDataSetState != null)
            {
                NodeId dataTypeId = TypeInfo.GetDataTypeId(e.ExtensionField.Value);
                var variable = CreateVariable(publishedDataSetState.ExtensionFields, e.ExtensionField.Key.Name, dataTypeId);
                variable.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                variable.Value = e.ExtensionField.Value;

                MapConfigIdToPubSubNodeState(e.ExtensionFieldId, variable);
            }           
        }

        /// <summary>
        /// Handler for ExtensionFieldAdded events from <see cref="UaPubSubConfigurator"/>.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UaPubSubConfigurator_ExtensionFieldRemoved(object sender, ExtensionFieldEventArgs e)
        {
            // locate the extensionField node and delete it from address space
            BaseInstanceState extensionFieldNode = FindPubSubNodeState(e.ExtensionFieldId) as BaseInstanceState;
            RemoveNodeFromAddressSpace(extensionFieldNode);
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

            //copy properties of configuration to node state object
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

            pubSubConnectionState.AddWriterGroup.OnCall = OnCallAddWriterGroupHandler;
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
            RemoveNodeFromAddressSpace(pubSubConnectionState);
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

                //copy properties of configuration to node state object
                writerGroupState.WriterGroupId.Value = e.WriterGroupDataType.WriterGroupId;
                writerGroupState.PublishingInterval.Value = e.WriterGroupDataType.PublishingInterval;
                writerGroupState.KeepAliveTime.Value = e.WriterGroupDataType.KeepAliveTime;
                writerGroupState.HeaderLayoutUri.Value = e.WriterGroupDataType.HeaderLayoutUri;
                writerGroupState.MaxNetworkMessageSize.Value = e.WriterGroupDataType.MaxNetworkMessageSize;
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
            RemoveNodeFromAddressSpace(writerGroupState);
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

                //copy properties of configuration into node state object
                dataSetWriterState.DataSetWriterId.Value = e.DataSetWriterDataType.DataSetWriterId;
                dataSetWriterState.DataSetFieldContentMask.Value = e.DataSetWriterDataType.DataSetFieldContentMask;
                dataSetWriterState.KeyFrameCount.Value = e.DataSetWriterDataType.KeyFrameCount;
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

                // add reference to published data set
                /*The Object has a list of DataSetWriters. A DataSetWriter sends DataSetMessages created from DataSets through a Message Oriented Middleware. 
                 * The link between the PublishedDataSet Object and a DataSetWriter shall be created when an instance of the DataSetWriterType is created. 
                 * The DataSetWriterType is defined in 9.1.7.2. If a DataSetWriter is created for the PublishedDataSet, it is added to the list using the ReferenceType DataSetToWriter. 
                 * The DataSetToWriter ReferenceType is defined in 9.1.4.2.5. If a DataSetWriter for the PublishedDataSet is removed from a group, 
                 * the Reference to this DataSetWriter shall also be removed from this list. The group model is defined in 9.1.6.*/

                PublishedDataSetDataType publishedDataSetDataType = m_uaPubSubConfigurator.FindPublishedDataSetByName(e.DataSetWriterDataType.DataSetName);
                if (publishedDataSetDataType != null)
                {
                    NodeState publishedDataSetNode = FindPubSubNodeState(m_uaPubSubConfigurator.FindIdForObject(publishedDataSetDataType));
                    if (publishedDataSetNode != null)
                    {
                        AddReference(publishedDataSetNode, ReferenceTypeIds.DataSetToWriter, false, dataSetWriterState.NodeId, true);
                    }
                }

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
            RemoveNodeFromAddressSpace(dataSetWriterState);

            //remove reference from published dataset node
            PublishedDataSetDataType publishedDataSetDataType = m_uaPubSubConfigurator.FindPublishedDataSetByName(e.DataSetWriterDataType.DataSetName);
            if (publishedDataSetDataType != null)
            {
                NodeState publishedDataSetNode = FindPubSubNodeState(m_uaPubSubConfigurator.FindIdForObject(publishedDataSetDataType));
                if (publishedDataSetNode != null)
                {
                    List<IReference> references = new List<IReference>();
                    publishedDataSetNode.GetReferences(SystemContext, references, ReferenceTypeIds.DataSetToWriter, false);

                    foreach (var reference in references.ToArray())
                    {
                        NodeId writerNodeId = ExpandedNodeId.ToNodeId(reference.TargetId, SystemContext.NamespaceUris);
                        if (writerNodeId == dataSetWriterState.NodeId)
                        {
                            publishedDataSetNode.RemoveReference(reference.ReferenceTypeId, reference.IsInverse, reference.TargetId);
                        }                       
                    }
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

                // ReaderGroupState.MaxNetworkMessageSize
                readerGroupState.MaxNetworkMessageSize.Value = e.ReaderGroupDataType.MaxNetworkMessageSize;

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
            RemoveNodeFromAddressSpace(readerGroupState);
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

                //copy properties of configuration to node state object
                dataSetreaderState.PublisherId.Value = e.DataSetReaderDataType.PublisherId;
                dataSetreaderState.WriterGroupId.Value = e.DataSetReaderDataType.WriterGroupId;
                dataSetreaderState.DataSetWriterId.Value = e.DataSetReaderDataType.DataSetWriterId;
                dataSetreaderState.DataSetMetaData.Value = e.DataSetReaderDataType.DataSetMetaData;
                dataSetreaderState.DataSetFieldContentMask.Value = e.DataSetReaderDataType.DataSetFieldContentMask;
                dataSetreaderState.MessageReceiveTimeout.Value = e.DataSetReaderDataType.MessageReceiveTimeout;
                dataSetreaderState.KeyFrameCount.Value = e.DataSetReaderDataType.KeyFrameCount;
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
            RemoveNodeFromAddressSpace(dataSetReaderState);
        }

        #endregion

        #region PubSubApplication_DataReceived event handler
        /// <summary>
        /// Hander for <see cref="UaPubSubApplication.DataReceived"/> event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PubSubApplication_DataReceived(object sender, SubscribedDataEventArgs e)
        {
            foreach(DataSet dataSet in e.DataSets)
            {
                foreach(Field field in dataSet.Fields)
                {
                    ((UaPubSubApplication)sender).DataStore.WritePublishedDataItem(field.TargetNodeId, field.TargetAttribute, field.Value);                    
                }
            }
        }
        #endregion

        #region OnCall method handlers for OPC UA Server Method nodes
        /// <summary>
        /// Handler for OnCall event of <see cref="DataSetFolderState.AddDataSetFolder"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="name"></param>
        /// <param name="dataSetFolderNodeId"></param>
        /// <returns></returns>
        private ServiceResult OnCallAddDataSetFolderHandler(ISystemContext context, MethodState method, NodeId objectId, string name, ref NodeId dataSetFolderNodeId)
        {
            // find folder
            DataSetFolderState parentFolder = method.Parent as DataSetFolderState;
            if (parentFolder != null)
            {
                //check children names for duplicate name
                if (parentFolder.FindChild(SystemContext, new QualifiedName(name, NamespaceIndex)) != null)
                {
                    return StatusCodes.BadBrowseNameDuplicated;
                }               
                
                DataSetFolderState dataSetFolderState = CreateObjectFromType(parentFolder, name, ObjectTypeIds.DataSetFolderType, ReferenceTypeIds.Organizes) as DataSetFolderState;
                if (dataSetFolderState!= null)
                {
                    InitializeDataSetFolderState(dataSetFolderState);
                    dataSetFolderNodeId = dataSetFolderState.NodeId;
                    return StatusCodes.Good;
                }
            }
            return StatusCodes.Bad;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="DataSetFolderState.RemoveDataSetFolder"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="dataSetFolderNodeId"></param>
        /// <returns></returns>
        private ServiceResult OnCallRemoveDataSetFolderHandler(ISystemContext context, MethodState method, NodeId objectId, NodeId dataSetFolderNodeId)
        {
            // find folder
            DataSetFolderState dataSetFolderState = FindNodeInAddressSpace(dataSetFolderNodeId) as DataSetFolderState;
            if (dataSetFolderState != null)
            {
                IList<BaseInstanceState> children = new List<BaseInstanceState>();
                dataSetFolderState.GetChildren(SystemContext, children);

                //remove all published datasets from folder
                foreach(var child in children)
                {
                    if (child is PublishedDataItemsState)
                    {
                        NodeId dataSetNodeId = null;
                        OnCallRemovePublishedDataSetHandler(context, dataSetFolderState.RemovePublishedDataSet, dataSetFolderState.NodeId, dataSetNodeId);
                    }
                }
                RemoveNodeFromAddressSpace(dataSetFolderState);
                return StatusCodes.Good;
            }
            return StatusCodes.BadNodeIdInvalid;
        }
        /// <summary>
        /// Handler for OnCall event of <see cref="DataSetFolderState.AddPublishedDataItems"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="name"></param>
        /// <param name="fieldNameAliases"></param>
        /// <param name="fieldFlags"></param>
        /// <param name="variablesToAdd"></param>
        /// <param name="dataSetNodeId"></param>
        /// <param name="configurationVersion"></param>
        /// <param name="addResults"></param>
        /// <returns></returns>
        private ServiceResult OnCallAddPublishedDataItemsHandler(ISystemContext context, MethodState method, NodeId objectId, string name, string[] fieldNameAliases, ushort[] fieldFlags,
                PublishedVariableDataType[] variablesToAdd, ref NodeId dataSetNodeId, ref ConfigurationVersionDataType configurationVersion, ref StatusCode[] addResults)
        {
            //validate parameters
            if (name == null)
            {
                return StatusCodes.BadInvalidArgument;
            }
            if (fieldNameAliases == null || fieldFlags == null || variablesToAdd == null
                || fieldNameAliases.Length != fieldFlags.Length || fieldFlags.Length != variablesToAdd.Length 
                || variablesToAdd.Length != fieldNameAliases.Length)
            {
                return StatusCodes.BadInvalidArgument;
            }

            PublishedDataSetDataType publishedDataSetDataType = new PublishedDataSetDataType();
            publishedDataSetDataType.Name = name;           
            //locate parent folder
            DataSetFolderState dataSetFolderState = method.Parent as DataSetFolderState;            
            if (dataSetFolderState == null)
            {
                return StatusCodes.BadInvalidArgument;
            }
            // build DataSetFolder collection if needed
            if (dataSetFolderState != m_publishSubscribeState.PublishedDataSets)
            {
                // set folder value
                publishedDataSetDataType.DataSetFolder = new StringCollection();
               
                while (dataSetFolderState.Parent is DataSetFolderState && dataSetFolderState != m_publishSubscribeState.PublishedDataSets)
                {
                    publishedDataSetDataType.DataSetFolder.Add(dataSetFolderState.BrowseName.Name);
                    dataSetFolderState = dataSetFolderState.Parent as DataSetFolderState;                    
                }
            }
           
            //set DataSetSource
            PublishedDataItemsDataType publishedDataItemsDataType = new PublishedDataItemsDataType();
            publishedDataItemsDataType.PublishedData = new PublishedVariableDataTypeCollection();
            publishedDataItemsDataType.PublishedData.AddRange(variablesToAdd);
            publishedDataSetDataType.DataSetSource = new ExtensionObject(publishedDataItemsDataType);

            // set DataSetMetaData
            publishedDataSetDataType.DataSetMetaData = new DataSetMetaDataType();
            publishedDataSetDataType.DataSetMetaData.DataSetClassId = Uuid.Empty;
            publishedDataSetDataType.DataSetMetaData.Name = name;
            publishedDataSetDataType.DataSetMetaData.Fields = new FieldMetaDataCollection();
            addResults = new StatusCode[fieldNameAliases.Length];

            for (int i =0; i < fieldNameAliases.Length; i++)
            {
                FieldMetaData fieldMetaData = new FieldMetaData()
                {
                    Name = fieldNameAliases[i],
                    DataSetFieldId = new Uuid(Guid.NewGuid()),
                };
                
                BaseDataVariableState variableState = FindNodeInAddressSpace(variablesToAdd[i].PublishedVariable) as BaseDataVariableState;
                if (variableState == null)
                {
                    addResults[i] = StatusCodes.BadNodeIdUnknown;
                }
                else
                {                    
                    fieldMetaData.DataType = variableState.DataType;
                    fieldMetaData.BuiltInType = (byte)TypeInfo.GetBuiltInType(variableState.DataType, Server.TypeTree);
                    fieldMetaData.ValueRank = variableState.ValueRank;
                }
                publishedDataSetDataType.DataSetMetaData.Fields.Add(fieldMetaData);                
            }
            publishedDataSetDataType.DataSetMetaData.ConfigurationVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = 1,
                MajorVersion = 1
            };

            StatusCode resultStatusCode = m_uaPubSubConfigurator.AddPublishedDataSet(publishedDataSetDataType);
            if (StatusCode.IsBad(resultStatusCode))
            {
                return resultStatusCode;
            }           
            configurationVersion = publishedDataSetDataType.DataSetMetaData.ConfigurationVersion;
            uint publishedDataSetConfigId = m_uaPubSubConfigurator.FindIdForObject(publishedDataSetDataType);
            //find node state created by PublishedDataSetAdded event handler in address space 
            NodeState dataSetNodeState = FindPubSubNodeState(publishedDataSetConfigId);
            if (dataSetNodeState != null)
            {
                dataSetNodeId = dataSetNodeState.NodeId;
                return StatusCodes.Good;
            }           
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="DataSetFolderState.RemovePublishedDataSet"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="dataSetNodeId"></param>
        /// <returns></returns>
        private ServiceResult OnCallRemovePublishedDataSetHandler(ISystemContext context, MethodState method, NodeId objectId, NodeId dataSetNodeId)
        {
            // find node to be removed
            NodeState publishedDataSetState = FindNodeInAddressSpace(dataSetNodeId);
            if (publishedDataSetState != null && publishedDataSetState.Handle is uint)
            {
                uint publishedDataSetConfigId = (uint)publishedDataSetState.Handle;
                return m_uaPubSubConfigurator.RemovePublishedDataSet(publishedDataSetConfigId);
            }
            return StatusCodes.BadNodeIdUnknown;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="ExtensionFieldsState.AddExtensionField"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        /// <param name="fieldId"></param>
        /// <returns></returns>
        private ServiceResult OnCallAddExtensionFieldHandler(ISystemContext context, MethodState method, NodeId objectId, QualifiedName fieldName, object fieldValue, ref NodeId fieldId)
        {
            //locate parent published data set
            ExtensionFieldsState extensionFieldsState = method.Parent as ExtensionFieldsState;
            if (extensionFieldsState != null)
            {
                PublishedDataSetState publishedDataSetState = extensionFieldsState.Parent as PublishedDataSetState;
                if (publishedDataSetState != null && publishedDataSetState.Handle is uint)
                {
                    uint publishedDataSetConfigId = (uint)publishedDataSetState.Handle;
                    KeyValuePair newExtensionField = new KeyValuePair()
                    {
                        Key = fieldName,
                        Value = new Variant(fieldValue)
                    };
                    StatusCode resultStatusCode = m_uaPubSubConfigurator.AddExtensionField(publishedDataSetConfigId, newExtensionField);
                    uint configId = m_uaPubSubConfigurator.FindIdForObject(newExtensionField);
                    //find node state created by PublishedDataSetAdded event handler in address space 
                    NodeState extensionFieldNodeState = FindPubSubNodeState(configId);
                    if (extensionFieldNodeState != null)
                    {
                        fieldId = extensionFieldNodeState.NodeId;
                        return StatusCodes.Good;
                    }
                }
            }
            return StatusCodes.BadInvalidArgument;
        }

        /// <summary>
        ///  Handler for OnCall event of <see cref="ExtensionFieldsState.RemoveExtensionField"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fieldId"></param>
        /// <returns></returns>
        private ServiceResult OnCallRemoveExtensionFieldHandler(ISystemContext context, MethodState method, NodeId objectId, NodeId fieldId)
        {
            // locate the extension field node
            NodeState extensionFieldNodeState = FindNodeInAddressSpace(fieldId);
            if (extensionFieldNodeState != null && extensionFieldNodeState.Handle is uint)
            {
                uint extensionFieldConfigId = (uint)extensionFieldNodeState.Handle;

                //locate parent published data set
                ExtensionFieldsState extensionFieldsState = method.Parent as ExtensionFieldsState;
                if (extensionFieldsState != null)
                {
                    PublishedDataSetState publishedDataSetState = extensionFieldsState.Parent as PublishedDataSetState;
                    if (publishedDataSetState != null && publishedDataSetState.Handle is uint)
                    {
                        uint publishedDataSetConfigId = (uint)publishedDataSetState.Handle;
                        return m_uaPubSubConfigurator.RemoveExtensionField(publishedDataSetConfigId, extensionFieldConfigId);
                    }
                }
            }
            return StatusCodes.BadNodeIdUnknown;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="PublishedDataItemsState.AddVariables"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="configurationVersion"></param>
        /// <param name="fieldNameAliases"></param>
        /// <param name="promotedFields"></param>
        /// <param name="variablesToAdd"></param>
        /// <param name="newConfigurationVersion"></param>
        /// <param name="addResults"></param>
        /// <returns></returns>
        private ServiceResult OnCallPublishedDataItemsAddVariablesHandler(ISystemContext context, MethodState method, NodeId objectId,
            ConfigurationVersionDataType configurationVersion, string[] fieldNameAliases,
            bool[] promotedFields, PublishedVariableDataType[] variablesToAdd,
            ref ConfigurationVersionDataType newConfigurationVersion, ref StatusCode[] addResults)
        {
            return StatusCodes.BadNotImplemented;
        }

        /// <summary>
        /// Handler for OnCall event of <see cref="PublishSubscribeState.AddConnection"/> method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="configuration"></param>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        private ServiceResult OnCallAddConnectionMethodHandler(ISystemContext context, MethodState method, NodeId objectId, PubSubConnectionDataType configuration, ref NodeId connectionId)
        {
            if (configuration != null)
            {
                //force uadp connection - temporary fix for usage from datafeed client
                configuration.TransportProfileUri = UaPubSubApplication.UdpUadpTransportProfileUri;
                StatusCode resultStatusCode = m_uaPubSubConfigurator.AddConnection(configuration);
                if (StatusCode.IsBad(resultStatusCode))
                {
                    return resultStatusCode;
                }
                uint connectionConfigId = m_uaPubSubConfigurator.FindIdForObject(configuration);
                //find node state created by ConnectionAdded event handler in address space 
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
        private ServiceResult OnCallRemoveConnectionHandler(ISystemContext context, MethodState method, NodeId objectId, NodeId connectionId)
        {
            // find connection node to be removed
            PubSubConnectionState pubSubConnectionState = FindNodeInAddressSpace(connectionId) as PubSubConnectionState;
            if (pubSubConnectionState != null && pubSubConnectionState.Handle is uint)
            {
                uint connectionConfigId = (uint)pubSubConnectionState.Handle;
                return m_uaPubSubConfigurator.RemoveConnection(connectionConfigId);
            }
            return StatusCodes.BadNodeIdUnknown;
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
        private ServiceResult OnCallAddWriterGroupHandler(ISystemContext context, MethodState method, NodeId objectId, WriterGroupDataType configuration, ref NodeId groupId)
        {
            if (configuration != null)
            {
                //locate parent connection
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
            return StatusCodes.BadNodeIdUnknown;
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
                //locate parent connection
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
            return StatusCodes.BadNodeIdUnknown;
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

            if (groupNodeState != null && groupNodeState.Handle is uint)
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
        private ServiceResult OnCallDisableHandler(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (method != null && method.Handle is uint)
            {
                //find corresponding configObjectId 
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
        private ServiceResult OnCallEnableHandler(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (method != null && method.Handle is uint)
            {
                //find corresponding configObjectId 
                uint configId = (uint)method.Handle;

                return m_uaPubSubConfigurator.Enable(configId);
            }
            return StatusCodes.Bad;
        }

        #endregion

        #region Private Helper Methods
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
                statusNode.Enable = CreateMethod(statusNode, BrowseNames.Enable, null, null, OnCallEnableHandler);
                statusNode.Enable.Handle = configId;
            }
            if (statusNode.Disable == null)
            {
                statusNode.Disable = CreateMethod(statusNode, BrowseNames.Disable, null, null, OnCallDisableHandler);
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
        /// Initializes the methods and what is needed for an instance of <see cref="DataSetFolderState"/>
        /// </summary>
        /// <param name="dataSetFolderState"></param>
        private void InitializeDataSetFolderState(DataSetFolderState dataSetFolderState)
        {            
            if (dataSetFolderState.AddDataSetFolder == null)
            {
                Argument[] inputArguments = new Argument[]
                {
                    new Argument(){Name = "Name", DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar, Description = "Name of the Object to create."}
                };
                Argument[] outputArguments = new Argument[]
                {
                    new Argument(){Name = "DataSetFolderNodeId", DataType = DataTypeIds.NodeId,  ValueRank = ValueRanks.Scalar, Description = "NodeId of the created DataSetFolderType Object."}
                };
                dataSetFolderState.AddDataSetFolder = CreateMethod(dataSetFolderState, BrowseNames.AddDataSetFolder, inputArguments, outputArguments, null,
                    typeof(AddDataSetFolderMethodState), MethodIds.DataSetFolderType_AddDataSetFolder) as AddDataSetFolderMethodState;               
            }
            dataSetFolderState.AddDataSetFolder.OnCall = OnCallAddDataSetFolderHandler;
            if (dataSetFolderState.RemoveDataSetFolder == null)
            {
                Argument[] inputArguments = new Argument[]
                {
                    new Argument(){Name = "DataSetFolderNodeId", DataType = DataTypeIds.NodeId,  ValueRank = ValueRanks.Scalar, Description = "NodeId of the DataSetFolderType Object to remove from the Server."}
                };
                dataSetFolderState.RemoveDataSetFolder = CreateMethod(dataSetFolderState, BrowseNames.RemoveDataSetFolder, inputArguments, null, null,
                    typeof(RemoveDataSetFolderMethodState), MethodIds.DataSetFolderType_RemoveDataSetFolder) as RemoveDataSetFolderMethodState;
            }
            dataSetFolderState.RemoveDataSetFolder.OnCall = OnCallRemoveDataSetFolderHandler;
            if (dataSetFolderState.AddPublishedDataItems == null)
            {
                Argument[] inputArguments = new Argument[]
                {
                    new Argument(){Name = "Name", DataType = DataTypeIds.String,  ValueRank = ValueRanks.Scalar, Description = "Name of the Object to create."},
                    new Argument(){Name = "FieldNameAliases", DataType = DataTypeIds.String,  ValueRank = ValueRanks.OneDimension, Description = "The names assigned to the selected Variables for the fields in the DataSetMetaData and in the DataSetMessages for tagged message encoding."},
                    new Argument(){Name = "FieldFlags", DataType = DataTypeIds.DataSetFieldFlags,  ValueRank = ValueRanks.OneDimension, Description = "The field flags assigned to the selected Variables for the fields in the DataSetMetaData."},
                    new Argument(){Name = "VariablesToAdd", DataType = DataTypeIds.PublishedVariableDataType,  ValueRank = ValueRanks.OneDimension, Description = "Array of Variables to add to PublishedData and the related configuration settings."},
                };
                Argument[] outputArguments = new Argument[]
                {
                    new Argument(){Name = "DataSetNodeId", DataType = DataTypeIds.NodeId,  ValueRank = ValueRanks.Scalar, Description = "NodeId of the created PublishedDataSets Object."},
                    new Argument(){Name = "ConfigurationVersion", DataType = DataTypeIds.ConfigurationVersionDataType,  ValueRank = ValueRanks.Scalar, Description = "Returns the initial configuration version of the DataSet."},
                    new Argument(){Name = "AddResults", DataType = DataTypeIds.StatusCode,  ValueRank = ValueRanks.OneDimension, Description = "The result codes for the variables to add."},
                };
                dataSetFolderState.AddPublishedDataItems = CreateMethod(dataSetFolderState, BrowseNames.AddPublishedDataItems, inputArguments, outputArguments, null,
                    typeof(AddPublishedDataItemsMethodState), MethodIds.DataSetFolderType_AddPublishedDataItems) as AddPublishedDataItemsMethodState;                
            }
            dataSetFolderState.AddPublishedDataItems.OnCall = OnCallAddPublishedDataItemsHandler;
            if (dataSetFolderState.RemovePublishedDataSet == null)
            {
                Argument[] inputArguments = new Argument[]
                {
                    new Argument(){Name = "DataSetNodeId", DataType = DataTypeIds.NodeId,  ValueRank = ValueRanks.Scalar, Description = "NodeId of the PublishedDataSets Object to remove from the Server."},
                };
                dataSetFolderState.RemovePublishedDataSet = CreateMethod(dataSetFolderState, BrowseNames.RemovePublishedDataSet, inputArguments, null, null,
                    typeof(RemovePublishedDataSetMethodState), MethodIds.DataSetFolderType_RemovePublishedDataSet) as RemovePublishedDataSetMethodState;                
            }
            dataSetFolderState.RemovePublishedDataSet.OnCall = OnCallRemovePublishedDataSetHandler;
            if (dataSetFolderState.AddPublishedDataItemsTemplate != null)
            {
                dataSetFolderState.AddPublishedDataItemsTemplate = null;
            }
            if (dataSetFolderState.AddPublishedEvents != null)
            {
                dataSetFolderState.AddPublishedEvents = null;
            }
            if (dataSetFolderState.AddPublishedEventsTemplate != null)
            {
                dataSetFolderState.AddPublishedEventsTemplate = null;
            }
        }

        /// <summary>
        /// Initializes methods of a <see cref="PublishedDataItemsState"/> instance
        /// </summary>
        /// <param name="publishedDataItemsState"></param>
        private void InitializePublishedDataItemsState(PublishedDataItemsState publishedDataItemsState)
        {
            /* if (publishedDataItemsState.AddVariables == null)
             {
                 Argument[] inputArguments = new Argument[]
                 {
                     new Argument(){Name = "ConfigurationVersion", DataType = DataTypeIds.ConfigurationVersionDataType, ValueRank = ValueRanks.Scalar, Description = "Configuration version of the DataSet."},
                     new Argument(){Name = "FieldNameAliases", DataType = DataTypeIds.String,  ValueRank = ValueRanks.OneDimension, Description = "The names assigned to the selected Variables for the fields in the DataSetMetaData and in the DataSetMessages for tagged message encoding."},
                     new Argument(){Name = "PromotedFields", DataType = DataTypeIds.Boolean,  ValueRank = ValueRanks.OneDimension, Description = "The flags indicating if the corresponding field is promoted to the DataSetMessage header."},
                     new Argument(){Name = "VariablesToAdd", DataType = DataTypeIds.PublishedVariableDataType,  ValueRank = ValueRanks.OneDimension, Description = "Array of Variables to add to PublishedData and the related configuration settings."},
                 };
                 Argument[] outputArguments = new Argument[]
                 {
                     new Argument(){Name = "NewConfigurationVersion", DataType = DataTypeIds.ConfigurationVersionDataType,  ValueRank = ValueRanks.Scalar, Description = "Returns the new configuration version of the PublishedDataSet."},
                     new Argument(){Name = "AddResults", DataType = DataTypeIds.StatusCode,  ValueRank = ValueRanks.OneDimension, Description = "The result codes for the variables to add."},
                 };
                 publishedDataItemsState.AddVariables = CreateMethod(publishedDataItemsState, BrowseNames.AddVariables, inputArguments, outputArguments, null,
                     typeof(PublishedDataItemsAddVariablesMethodState), MethodIds.PublishedDataItemsType_AddVariables) as PublishedDataItemsAddVariablesMethodState;
             }
             publishedDataItemsState.AddVariables.OnCall = OnCallPublishedDataItemsAddVariablesHandler;*/
            publishedDataItemsState.AddVariables = null;
            publishedDataItemsState.RemoveVariables = null;
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
                //throw exception or something
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

        /// <summary>
        /// Removes a node from the address space
        /// </summary>
        /// <param name="nodeToRemove"></param>
        private void RemoveNodeFromAddressSpace(BaseInstanceState nodeToRemove)
        {
            if (nodeToRemove != null && nodeToRemove.Parent != null)
            {
                // remove from children list
                nodeToRemove.Parent.RemoveChild(nodeToRemove);
                // remove from predefined nodes
                PredefinedNodes.Remove(nodeToRemove.NodeId);
                RemoveConfigIdToPubSubNodeStateMapping(nodeToRemove);
            }
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        private BaseDataVariableState CreateVariable(NodeState parent, string name, NodeId dataType, int valueRank = ValueRanks.Scalar, NodeId nodeId = null)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent);
            if (nodeId == null)
            {
                nodeId = New(SystemContext, variable);
            }
            variable.NodeId = nodeId;
            variable.BrowseName = new QualifiedName(name, NamespaceIndex);
            variable.DisplayName = variable.BrowseName.Name;
            variable.SymbolicName = variable.BrowseName.Name;

            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;

            variable.DataType = dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            if (parent != null)
            {
                parent.AddChild(variable);
            }
            AddPredefinedNode(SystemContext, variable);


            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.Value = GetDefaultValueForDatatype(dataType);

            return variable;
        }


        #endregion

        #region Data Changes Simulation
        /// <summary>
        /// Simulate value changes in dynamic nodes
        /// </summary>
        /// <param name="state"></param>
        private void DoSimulation(object state)
        {
            try
            {
                lock (Lock)
                {
                    foreach (BaseDataVariableState variable in m_dynamicNodes)
                    {
                        variable.Value = GetNewValue(variable);
                        variable.Timestamp = DateTime.UtcNow;
                        variable.ClearChangeMasks(SystemContext, false);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error doing simulation.");
            }
        }

        /// <summary>
        /// Generates new value for a specific variable node
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        private object GetNewValue(BaseDataVariableState variable)
        {
            switch (variable.BrowseName.Name)
            {
                case "BoolToggle":
                    bool boolValue = (bool)variable.Value;
                    return !boolValue;
                case "Int32":
                    int intValue = Convert.ToInt32(variable.Value);
                    return (int)(intValue + 1);
                case "UInt32":
                    uint uintValue = Convert.ToUInt32(variable.Value);
                    return (uint)(uintValue + 1);
                case "Int32Fast":
                    intValue = Convert.ToInt32(variable.Value);
                    return (int)(intValue + 100);
                case "DateTime":
                    return DateTime.Now;
                case "Byte":
                    byte byteValue = Convert.ToByte(variable.Value);
                    return (byte)(byteValue + 1);
                case "Int16":
                    Int16 int16Value = Convert.ToInt16(variable.Value);
                    return (Int16)(int16Value + 1);
                case "UInt16":
                    UInt16 uint16Value = Convert.ToUInt16(variable.Value);
                    return (UInt16)(uint16Value + 1);
                case "SByte":
                    sbyte sbyteValue = Convert.ToSByte(variable.Value);
                    return (sbyte)(sbyteValue + 1);
                case "Float":
                    float floatValue = (float)variable.Value;
                    return (float)(floatValue + 1);
                case "Double":
                    double doubleValue = (double)variable.Value;
                    return (double)(doubleValue + 1);
                default:
                    if (variable.BrowseName.Name.StartsWith("Mass"))
                    {
                        uintValue = Convert.ToUInt32(variable.Value);
                        return (uint)(uintValue + 1);
                    }
                    break;
            }

            return null;
        }
        #endregion
    }
}
