/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Threading;
using Range = Opc.Ua.Range;

namespace SampleServer.DataAccess
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class DataAccessNodeManager : NodeManager
    {
        #region Private Members
        private DataItemState m_doorOpened;
        private DataItemState m_doorClosed;
        private DataItemState m_lightStatus;
        private AnalogItemState m_motorTemperature;
        private Timer m_simulationTimer;
        private FolderState m_dataAccessRoot;
        private uint m_timerInterval = 1000;
        private BaseEventState m_motorTemperatureEvent;
        private NodeIdDictionary<MonitoredNode2> m_monitoredNodes;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public DataAccessNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.DataAccess)
        {
            //parse custom configuration extension 
            SampleServerConfiguration sampleServerConfiguration = configuration.ParseExtension<SampleServerConfiguration>();
            if (sampleServerConfiguration != null)
            {
                m_timerInterval = sampleServerConfiguration.TimerInterval;
            }
        }
        #endregion

        #region INodeManager Members
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

                // Create a root node and add a reference to external Server Objects Folder
                FolderState root = CreateObjectFromType(null, "DataAccess", ObjectTypeIds.FolderType, ReferenceTypeIds.Organizes) as FolderState;
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                root.EventNotifier |= EventNotifiers.SubscribeToEvents;

                AddRootNotifier(root);

                CreateRefrigerator(SystemContext, root);                

                // Initialize timer for data changes simulation
                m_simulationTimer = new Timer(DoSimulation, null, m_timerInterval, m_timerInterval);
                //remember data access root 
                m_dataAccessRoot = root;


                FolderState registeredNodes = CreateFolder(root, "NodesForRegister");

                var node0 = CreateVariable(registeredNodes, "Node0", DataTypeIds.Int32);
                node0.Value = 100;

                var node1 = CreateVariable(registeredNodes, "Node1", DataTypeIds.Int32);
                node1.Value = 200;

                var node2 = CreateVariable(registeredNodes, "Node2", DataTypeIds.Int32);
                node2.Value = 300;

                // create the table of monitored nodes.
                // these are created by the node manager whenever a client subscribe to an attribute of the node.
                m_monitoredNodes = new NodeIdDictionary<MonitoredNode2>();              
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates the refrigerator instance with all its components
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="parent">The folder</param>
        private void CreateRefrigerator(ServerSystemContext context, FolderState parent)
        {
            BaseObjectState refrigerator = CreateObject(parent, "Refrigerator");
            refrigerator.EventNotifier |= EventNotifiers.SubscribeToEvents;

            // Create CoolingMotorRunning variable
            DataItemState coolingMotorRunning = CreateVariableFromType(refrigerator, "CoolingMotorRunning", VariableTypeIds.DataItemType, ReferenceTypeIds.Organizes) as DataItemState;
            coolingMotorRunning.DataType = DataTypeIds.Boolean;
            coolingMotorRunning.ValueRank = ValueRanks.Scalar;
            coolingMotorRunning.Value = true;

            // Create DoorMotor variable
            DataItemState doorMotor = CreateDataItemVariable(refrigerator, "DoorMotor", DataTypes.Double);
            doorMotor.Value = 11.2;

            // Create LightStatus variable
            m_lightStatus = CreateDataItemVariable(refrigerator, "LightStatus", DataTypes.Boolean);
            m_lightStatus.Value = true;

            // Create DoorClosed variable
            m_doorClosed = CreateDataItemVariable(refrigerator, "DoorClosed", DataTypes.Boolean);
            m_doorClosed.Value = false;

            // Create DoorOpened variable
            m_doorOpened = CreateDataItemVariable(refrigerator, "DoorOpened", DataTypes.Boolean);
            m_doorOpened.Value = true;

            // Create ActualTemperature variable
            AnalogItemState actualTemperature = CreateAnalogVariable(refrigerator, "ActualTemperature", DataTypeIds.Double,ValueRanks.Scalar, new Range(90, 10), null);
            actualTemperature.InstrumentRange.Value = new Range(100, 0);
            actualTemperature.Value = 2.7;

            // Create MotorTemperature variable
            m_motorTemperature = CreateAnalogVariable(refrigerator, "MotorTemperature", DataTypeIds.Double, ValueRanks.Scalar, new Range(90, 10), null);
            m_motorTemperature.InstrumentRange.Value = new Range(100, 0);
            m_motorTemperature.Value = 47.6;


            // Create SetpointOfTheTemperature variable
            AnalogItemState setpointOfTheTemperature = CreateAnalogVariable(refrigerator, "SetpointOfTheTemperature", DataTypeIds.Double, ValueRanks.Scalar, new Range(90, 10), null);
            setpointOfTheTemperature.Value = 3.2;
            setpointOfTheTemperature.InstrumentRange.Value = new Range(100, 0);

            // Create OpenCloseDoor method
            Argument[] inputArgs = new Argument[]
                 {
                    new Argument { Name = "OpenCloseDoor", Description = "Opens/closes the door.",  DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar }
                };
            MethodState openCloseDoorMethod = CreateMethod(refrigerator, "OpenCloseDoor", inputArguments: inputArgs);
            openCloseDoorMethod.OnCallMethod = DoOpenCloseDoorCall;


            // create an instance of BaseEventType to be used when reporting MotorTemperature events
            m_motorTemperatureEvent = CreateObjectFromType(m_motorTemperature, "MotorTemperatureEvent", ObjectTypeIds.BaseEventType) as BaseEventState;
        }

        /// <summary>
        /// Creates a new set of monitored items for a set of variables.
        /// </summary>
        /// <remarks>
        /// This method only handles data change subscriptions. Event subscriptions are created by the SDK.
        /// </remarks>
        public override void CreateMonitoredItems(
            OperationContext context,
            uint subscriptionId,
            double publishingInterval,
            TimestampsToReturn timestampsToReturn,
            IList<MonitoredItemCreateRequest> itemsToCreate,
            IList<ServiceResult> errors,
            IList<MonitoringFilterResult> filterErrors,
            IList<IMonitoredItem> monitoredItems,
            bool createDurable,
            ref long globalIdCounter)
        {
            ServerSystemContext systemContext = SystemContext.Copy(context);
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
            List<NodeHandle> nodesToValidate = new List<NodeHandle>();
            List<IMonitoredItem> createdItems = new List<IMonitoredItem>();

            for (int ii = 0; ii < itemsToCreate.Count; ii++)
            {
                MonitoredItemCreateRequest itemToCreate = itemsToCreate[ii];

                // skip items that have already been processed.
                if (itemToCreate.Processed)
                {
                    continue;
                }

                ReadValueId itemToMonitor = itemToCreate.ItemToMonitor;

                // check for valid handle.
                NodeHandle handle = GetManagerHandle(systemContext, itemToMonitor.NodeId, operationCache);

                if (handle == null)
                {
                    continue;
                }

                // owned by this node manager.
                itemToCreate.Processed = true;

                // must validate node in a separate operation.
                errors[ii] = StatusCodes.BadNodeIdUnknown;

                handle.Index = ii;
                nodesToValidate.Add(handle);
            }

            // check for nothing to do.
            if (nodesToValidate.Count == 0)
            {
                return;
            }

            // validates the nodes (reads values from the underlying data source if required).
            for (int ii = 0; ii < nodesToValidate.Count; ii++)
            {
                NodeHandle handle = nodesToValidate[ii];

                MonitoringFilterResult filterResult = null;
                IMonitoredItem monitoredItem = null;

                lock (Lock)
                {
                    // validate node.
                    NodeState source = ValidateNode(systemContext, handle, operationCache);

                    if (source == null)
                    {
                        continue;
                    }

                    MonitoredItemCreateRequest itemToCreate = itemsToCreate[handle.Index];

                    // create monitored item.
                    errors[handle.Index] = CreateMonitoredItem(
                        systemContext,
                        handle,
                        subscriptionId,
                        publishingInterval,
                        context.DiagnosticsMask,
                        timestampsToReturn,
                        itemToCreate,
                        createDurable,
                        ref globalIdCounter,
                        out filterResult,
                        out monitoredItem);
                }

                // save any filter error details.
                filterErrors[handle.Index] = filterResult;

                if (ServiceResult.IsBad(errors[handle.Index]))
                {
                    continue;
                }

                // save the monitored item.
                monitoredItems[handle.Index] = monitoredItem;
                createdItems.Add(monitoredItem);
            }

            // do any post processing.
            OnCreateMonitoredItemsComplete(systemContext, createdItems);
        }

        /// <summary>
        /// Restore a set of monitored items after a restart.
        /// </summary>
        public override void RestoreMonitoredItems(
            IList<IStoredMonitoredItem> itemsToRestore,
            IList<IMonitoredItem> monitoredItems,
            IUserIdentity savedOwnerIdentity)
        {
            if (itemsToRestore == null) throw new ArgumentNullException(nameof(itemsToRestore));
            if (monitoredItems == null) throw new ArgumentNullException(nameof(monitoredItems));

            if (Server.IsRunning)
            {
                throw new InvalidOperationException("Subscription restore can only occur on startup");
            }

            ServerSystemContext systemContext = SystemContext.Copy();
            IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
            List<NodeHandle> nodesToValidate = new List<NodeHandle>();

            for (int ii = 0; ii < itemsToRestore.Count; ii++)
            {
                IStoredMonitoredItem itemToCreate = itemsToRestore[ii];

                // skip items that have already been processed.
                if (itemToCreate.IsRestored)
                {
                    continue;
                }

                // check for valid handle.
                NodeHandle handle = GetManagerHandle(systemContext, itemToCreate.NodeId, operationCache);

                if (handle == null)
                {
                    continue;
                }

                // owned by this node manager.
                itemToCreate.IsRestored = true;

                handle.Index = ii;
                nodesToValidate.Add(handle);
            }

            // check for nothing to do.
            if (nodesToValidate.Count == 0)
            {
                return;
            }

            // validates the nodes (reads values from the underlying data source if required).
            for (int ii = 0; ii < nodesToValidate.Count; ii++)
            {
                NodeHandle handle = nodesToValidate[ii];

                bool success = false;
                IMonitoredItem monitoredItem = null;

                lock (Lock)
                {
                    // validate node.
                    NodeState source = ValidateNode(systemContext, handle, operationCache);

                    if (source == null)
                    {
                        continue;
                    }

                    IStoredMonitoredItem itemToCreate = itemsToRestore[handle.Index];

                    // create monitored item.
                    success = RestoreMonitoredItem(
                        systemContext,
                        handle,
                        itemToCreate,
                        out monitoredItem);
                }

                if (!success)
                {
                    continue;
                }

                // save the monitored item.
                monitoredItems[handle.Index] = monitoredItem;
            }

            // do any post processing.
            OnCreateMonitoredItemsComplete(systemContext, monitoredItems);
        }

        /// <summary>
        /// Restore a single monitored Item after a restart
        /// </summary>
        /// <returns>true if successfully restored</returns>
        protected override bool RestoreMonitoredItem(
            ServerSystemContext context,
            NodeHandle handle,
            IStoredMonitoredItem storedMonitoredItem,
            out IMonitoredItem monitoredItem)
        {
            monitoredItem = null;

            // validate attribute.
            if (!Attributes.IsValid(handle.Node.NodeClass, storedMonitoredItem.AttributeId))
            {
                return false;
            }

            // check if the node is already being monitored.
            MonitoredNode2 monitoredNode = null;

            if (!m_monitoredNodes.TryGetValue(handle.Node.NodeId, out monitoredNode))
            {
                NodeState cachedNode = AddNodeToComponentCache(context, handle, handle.Node);
                m_monitoredNodes[handle.Node.NodeId] = monitoredNode = new MonitoredNode2(this, cachedNode);
            }

            handle.Node = monitoredNode.Node;
            handle.MonitoredNode = monitoredNode;

            // put an upper limit on queue size.
            storedMonitoredItem.QueueSize = storedMonitoredItem.QueueSize;

            storedMonitoredItem.SamplingInterval = storedMonitoredItem.SamplingInterval;

            // create the item.
            MonitoredItem datachangeItem = new MonitoredItem(
                Server,
                this,
                handle,
                storedMonitoredItem);

            // update monitored item list.
            monitoredItem = datachangeItem;

            // save the monitored item.
            monitoredNode.Add(datachangeItem);

            // report change.
            OnMonitoredItemCreated(context, handle, datachangeItem);

            return true;
        }

        /// <summary>
        /// Transfers a collection of monitored items, ensuring their state is updated and resending initial values as needed. 
        /// Already processed items are skipped.
        /// </summary>
        /// <param name="context">The current operation context.</param>
        /// <param name="sendInitialValues">Indicates if the subscription should resend initial values after transfer.</param>
        /// <param name="monitoredItems">The set of monitoring items to update.</param>
        /// <param name="processedItems">A list of bool indicating items that have already been processed.</param>
        /// <param name="errors">Any errors that occur.</param>
        public override void TransferMonitoredItems(
            OperationContext context,
            bool sendInitialValues,
            IList<IMonitoredItem> monitoredItems,
            IList<bool> processedItems,
            IList<ServiceResult> errors)
        {
            var transferredMonitoredItems = new List<IMonitoredItem>();

            lock (Lock)
            {
                int index = 0;
                foreach (var monitoredItem in monitoredItems)
                {
                    // Skip already processed or null items
                    if (processedItems[index] || monitoredItem == null || monitoredItem.ManagerHandle == null)
                    {
                        index++;
                        continue;
                    }

                    errors[index] = StatusCodes.Good;
                    processedItems[index] = true;
                    transferredMonitoredItems.Add(monitoredItem);

                    if (sendInitialValues)
                    {
                        monitoredItem.SetupResendDataTrigger();
                    }

                    index++;
                }

            }
            OnMonitoredItemsTransferred(SystemContext.Copy(context), transferredMonitoredItems);
        }

        /// <summary>
        /// Handles the method call of the OpenCloseDoor method
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="method">The method state.</param>
        /// <param name="inputArguments">The input arguments.</param>
        /// <param name="outputArguments">The output arguments.</param>
        /// <returns></returns>
        private ServiceResult DoOpenCloseDoorCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments == null)
            {
                return new ServiceResult(StatusCodes.BadArgumentsMissing);
            }

            if (inputArguments.Count == 0)
            {
                return new ServiceResult(StatusCodes.BadArgumentsMissing);
            }

            bool? input = inputArguments[0] as bool?;
            if (!input.HasValue)
            {
                return new ServiceResult(StatusCodes.BadArgumentsMissing);
            }

            m_doorOpened.Value = input.Value;
            m_doorOpened.ClearChangeMasks(context, false);
            m_doorClosed.Value = !input.Value;
            m_doorClosed.ClearChangeMasks(context, false);

            if ((bool)m_doorClosed.Value)
            {
                m_lightStatus.Value = false;
            }
            else
            {
                m_lightStatus.Value = true;
            }

            m_lightStatus.ClearChangeMasks(context, false);

            return new ServiceResult(StatusCodes.Good);
        }

        /// <summary>
        /// Simulate changes in variable nodes
        /// </summary>
        /// <param name="state"></param>
        private void DoSimulation(object state)
        {
            try
            {
                lock (Lock)
                {
                    m_motorTemperature.Value = GetNewValue(0, 100);
                    m_motorTemperature.Timestamp = DateTime.UtcNow;
                    m_motorTemperature.ClearChangeMasks(SystemContext, false);
                }

                // Report an event at on DataAccess node
                string eventMessage = String.Format("Motor temperature changed to {0}", m_motorTemperature.Value);  
                ReportEvent(m_motorTemperature, m_motorTemperatureEvent, new LocalizedText(eventMessage), EventSeverity.Medium);
            }
            catch (Exception e)
            {
                Utils.Trace(e, "DataAccess.DataAccessNodeManager.DoSimulation: Unexpected error doing simulation.");
            }
        }

        /// <summary>
        /// Generate new value in provided bounds
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <returns></returns>
        private double GetNewValue(double minimum, double maximum)
        {
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
        #endregion       

        #region IDisposable Implementation
        /// <summary>
        /// /// <summary>
        /// An overrideable version of the Dispose
        /// </summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Utils.SilentDispose(m_simulationTimer);
                m_simulationTimer = null;
            }

            base.Dispose(disposing);
        }
        #endregion
    }
}