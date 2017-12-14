/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Server;

namespace SampleServer.DataAccess
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class DataAccessNodeManager : CustomNodeManager2
    {
        #region Private Members
        private uint m_nextNodeId;
        private DataItemState<bool> m_doorOpened;
        private DataItemState<bool> m_doorClosed;
        private DataItemState<bool> m_lightStatus;

        AnalogItemState<double> m_motorTemperature;

        private Timer m_simulationTimer;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public DataAccessNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.DataAccess)
        {
            SystemContext.NodeIdFactory = this;
        }
        #endregion

        #region INodeIdFactory Members
        /// <summary>
        /// Creates the NodeId for the specified node
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            return GenerateNodeId();
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
                // Create the root folder
                FolderState folder = new FolderState(null);
                folder.NodeId = new NodeId("DataAccess", NamespaceIndex);
                folder.BrowseName = new QualifiedName("DataAccess", NamespaceIndex);
                folder.DisplayName = folder.BrowseName.Name;
                folder.TypeDefinitionId = ObjectTypeIds.FolderType;

                IList<IReference> references;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, folder.NodeId));
                folder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);

                // Save the node for later lookup
                AddPredefinedNode(SystemContext, folder);

                // Create the refrigerator instance object
                CreateRefrigerator(SystemContext, folder);

                m_simulationTimer = new Timer(DoSimulation, null, 1000, 1000);
            }
        }

        /// <summary>
        /// Frees any resources allocated for the address space
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                // TBD
            }
        }

        /// <summary>
        /// Returns a unique handle for the node
        /// </summary>
        protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                // Quickly exclude nodes that are not in the namespace
                if (!IsNodeIdInNamespace(nodeId))
                {
                    return null;
                }

                NodeState node = null;

                if (PredefinedNodes != null && !PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    return null;
                }

                NodeHandle handle = new NodeHandle();

                handle.NodeId = nodeId;
                handle.Node = node;
                handle.Validated = true;

                return handle;
            }
        }

        /// <summary>
        /// Verifies that the specified node exists
        /// </summary>
        protected override NodeState ValidateNode(ServerSystemContext context, NodeHandle handle, IDictionary<NodeId, NodeState> cache)
        {
            // Not valid if no root
            if (handle == null)
            {
                return null;
            }

            // Check if previously validated
            if (handle.Validated)
            {
                return handle.Node;
            }

            // TBD
            return null;
        }

        private NodeId GenerateNodeId()
        {
            return new NodeId(++m_nextNodeId, NamespaceIndex);
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
            BaseObjectState refrigerator = new BaseObjectState(parent);
         
            refrigerator.Create(context, null, new QualifiedName("Refrigerator", NamespaceIndex), null, true);

            parent.AddReference(ReferenceTypeIds.Organizes, false, refrigerator.NodeId);
            refrigerator.AddReference(ReferenceTypeIds.Organizes, true, parent.NodeId);
            
            // Create CoolingMotorRunning variable
            DataItemState<bool> coolingMotorRunning = new DataItemState<bool>(refrigerator);
            coolingMotorRunning.Create(SystemContext, GenerateNodeId(), new QualifiedName("CoolingMotorRunning", NamespaceIndex), null, true);
            coolingMotorRunning.AccessLevel = AccessLevels.CurrentReadOrWrite;
            coolingMotorRunning.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            coolingMotorRunning.Value = true;
            refrigerator.AddChild(coolingMotorRunning);

            // Create DoorMotor variable
            DataItemState<double> doorMotor = new DataItemState<double>(refrigerator);
            doorMotor.Create(SystemContext, GenerateNodeId(), new QualifiedName("DoorMotor", NamespaceIndex), null, true);
            doorMotor.AccessLevel = AccessLevels.CurrentReadOrWrite;
            doorMotor.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            doorMotor.Value = 11.2;
            refrigerator.AddChild(doorMotor);

            // Create LightStatus variable
            m_lightStatus = new DataItemState<bool>(refrigerator);
            m_lightStatus.Create(SystemContext, GenerateNodeId(), new QualifiedName("LightStatus", NamespaceIndex), null, true);
            m_lightStatus.AccessLevel = AccessLevels.CurrentReadOrWrite;
            m_lightStatus.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            m_lightStatus.Value = true;
            refrigerator.AddChild(m_lightStatus);

            // Create DoorClosed variable
            m_doorClosed = new DataItemState<bool>(refrigerator);
            m_doorClosed.Create(SystemContext, GenerateNodeId(), new QualifiedName("DoorClosed", NamespaceIndex), null, true);
            m_doorClosed.AccessLevel = AccessLevels.CurrentReadOrWrite;
            m_doorClosed.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            refrigerator.AddChild(m_doorClosed);

            // Create DoorOpened variable
            m_doorOpened = new DataItemState<bool>(refrigerator);
            m_doorOpened.Create(SystemContext, GenerateNodeId(), new QualifiedName("DoorOpened", NamespaceIndex), null, true);
            m_doorOpened.AccessLevel = AccessLevels.CurrentReadOrWrite;
            m_doorOpened.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            m_doorOpened.Value = true;
            refrigerator.AddChild(m_doorOpened);

            // Create ActualTemperature variable
            AnalogItemState<double> actualTemperature = new AnalogItemState<double>(refrigerator);
            actualTemperature.InstrumentRange = new PropertyState<Range>(actualTemperature);
            actualTemperature.Create(SystemContext, GenerateNodeId(), new QualifiedName("ActualTemperature", NamespaceIndex), null, true);
            actualTemperature.AccessLevel = AccessLevels.CurrentReadOrWrite;
            actualTemperature.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            actualTemperature.EURange.Value = new Range(90, 10);
            actualTemperature.InstrumentRange.Value = new Range(100, 0);
            actualTemperature.Value = 2.7;
            refrigerator.AddChild(actualTemperature);

            // Create MotorTemperature variable
            m_motorTemperature = new AnalogItemState<double>(refrigerator);
            m_motorTemperature.InstrumentRange = new PropertyState<Range>(m_motorTemperature);
            m_motorTemperature.Create(SystemContext, GenerateNodeId(), new QualifiedName("MotorTemperature", NamespaceIndex), null, true);
            m_motorTemperature.AccessLevel = AccessLevels.CurrentReadOrWrite;
            m_motorTemperature.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            m_motorTemperature.Value = 47.6;
            m_motorTemperature.EURange.Value = new Range(90, 10);
            m_motorTemperature.InstrumentRange.Value = new Range(100, 0);
            refrigerator.AddChild(m_motorTemperature);

            // Create SetpointOfTheTemperature variable
            AnalogItemState<double> setpointOfTheTemperature = new AnalogItemState<double>(refrigerator);
            setpointOfTheTemperature.InstrumentRange = new PropertyState<Range>(setpointOfTheTemperature);
            setpointOfTheTemperature.Create(SystemContext, GenerateNodeId(), new QualifiedName("SetpointOfTheTemperature", NamespaceIndex), null, true);
            setpointOfTheTemperature.Value = 3.2;
            setpointOfTheTemperature.EURange.Value = new Range(90, 10);
            setpointOfTheTemperature.InstrumentRange.Value = new Range(100, 0);
            refrigerator.AddChild(setpointOfTheTemperature);

            // Create OpenCloseDoor method
            MethodState openCloseDoorMethod = new MethodState(refrigerator);
            openCloseDoorMethod.NodeId = GenerateNodeId();
            openCloseDoorMethod.BrowseName = new QualifiedName("OpenCloseDoor", NamespaceIndex);
            openCloseDoorMethod.DisplayName = openCloseDoorMethod.BrowseName.Name;
            openCloseDoorMethod.Executable = true;
            openCloseDoorMethod.UserExecutable = true;
            openCloseDoorMethod.ReferenceTypeId = ReferenceTypeIds.HasComponent;
            
            // Create the input arguments for the method
            PropertyState<Argument[]> inputArguments = new PropertyState<Argument[]>(openCloseDoorMethod);
            inputArguments.NodeId = GenerateNodeId();
            inputArguments.BrowseName = new QualifiedName(BrowseNames.InputArguments);
            inputArguments.DisplayName = inputArguments.BrowseName.Name;
            inputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            inputArguments.DataType = DataTypeIds.Argument;
            inputArguments.ValueRank = ValueRanks.OneDimension;
            inputArguments.MinimumSamplingInterval = MinimumSamplingIntervals.Continuous;
            inputArguments.AccessLevel = AccessLevels.CurrentRead;
            inputArguments.UserAccessLevel = AccessLevels.CurrentRead;
            inputArguments.Historizing = false;
            inputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            inputArguments.Value = new Argument[] 
                {
                    new Argument { Name = "OpenCloseDoor", Description = "Opens/closes the door.",  DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar }
                };
            openCloseDoorMethod.InputArguments = inputArguments;
            openCloseDoorMethod.OnCallMethod = DoOpenCloseDoorCall;

            refrigerator.AddChild(openCloseDoorMethod);

            AddPredefinedNode(context, refrigerator);
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

            if (m_doorClosed.Value)
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

                // Report an event at Server level

                string eventMessage = String.Format("Motor temperature changed to {0}", m_motorTemperature.Value);
                BaseEventState temperatureChangeEvent = new BaseEventState(m_motorTemperature);

                temperatureChangeEvent.Initialize(SystemContext, m_motorTemperature, EventSeverity.Medium,
                    new LocalizedText(eventMessage));

                Server.ReportEvent(temperatureChangeEvent);
            }
            catch (Exception e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "DataAccess.DataAccessNodeManager.DoSimulation", "Unexpected error doing simulation.", e);
            }
        }

        private double GetNewValue(double minimum, double maximum)
        {
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
        #endregion
    }
}