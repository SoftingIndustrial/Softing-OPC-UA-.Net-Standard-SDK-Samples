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
using Opc.Ua;
using Opc.Ua.Server;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the Alarms and Conditions OPC UA feature
    /// </summary>
    public class AlarmsNodeManager : CustomNodeManager2
    {
        #region Constructors
        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public AlarmsNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.Alarms)
        {
            SystemContext.NodeIdFactory = this;
        }
        #endregion

        #region INodeIdFactory Members
        /// <summary>
        /// Creates the NodeId for the specified node.
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
                // Create the root of the node manager in the AddressSpace
                FolderState root = new FolderState(null);

                // Set root object data 
                root.NodeId = GenerateNodeId();
                root.BrowseName = new QualifiedName("Alarms", NamespaceIndex);
                root.DisplayName = root.BrowseName.Name;
                root.Description = "Alarms";
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                root.TypeDefinitionId = ObjectTypeIds.FolderType;

                // Ensure the process object can be found via the server object
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                root.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, root.NodeId));

                // Add a folder representing the monitored device.
                BaseObjectState machine = AddObject(root, "Machine A");

                // Create an alarm monitor for a temperature sensor 1.
                ExclusiveLimitMonitor temperatureMonitor1 = new ExclusiveLimitMonitor(
                    SystemContext,
                    machine,
                    NamespaceIndex,
                    "TemperatureSensor 1",
                    "TemperatureMonitor 1",
                    30.0,
                    80.0,
                    100.0,
                    20.0,
                    10.0);

                // Create an alarm monitor for a temperature sensor 2.
                ExclusiveLimitMonitor temperatureMonitor2 = new ExclusiveLimitMonitor(
                    SystemContext,
                    machine,
                    NamespaceIndex,
                    "TemperatureSensor 2",
                    "TemperatureMonitor 2",
                    50.0,
                    90.0,
                    120.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a pressure sensor 1.
                ExclusiveLimitMonitor pressureMonitor1 = new ExclusiveLimitMonitor(
                    SystemContext,
                    machine,
                    NamespaceIndex,
                    "PressureSensor 1",
                    "PressureMonitor 1",
                    5.0,
                    10.0,
                    15.0,
                    2.0,
                    1.0);
                
                // Create an alarm monitor for a pressure sensor 2.
                ExclusiveLimitMonitor pressureMonitor2 = new ExclusiveLimitMonitor(
                    SystemContext,
                    machine,
                    NamespaceIndex,
                    "PressureSensor 2",
                    "PressureMonitor 2",
                    6.0,
                    15.0,
                    20.0,
                    4.0,
                    2.0);

                // Add Support for Event Notifiers
                // Creating notifier ensures events propogate up the hierarchy when they are produced
                AddRootNotifier(root);

                // Add link to server object
                if (!externalReferences.TryGetValue(ObjectIds.Server, out references))
                {
                    externalReferences[ObjectIds.Server] = references = new List<IReference>();
                }
                references.Add(new NodeStateReference(ReferenceTypeIds.HasNotifier, false, root.NodeId));

                // Add sub-notifiers
                root.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, false, machine);
                machine.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, true, root);

                // Save the node for later lookup (all tightly coupled children are added with this call)
                AddPredefinedNode(SystemContext, root);
            }
        }

        /// <summary>
        /// Creates a new object node and adds it to the specified parent
        /// </summary>
        private BaseObjectState AddObject(NodeState parent, string name)
        {
            BaseObjectState objectNode = new BaseObjectState(parent);

            objectNode.NodeId = GenerateNodeId();
            objectNode.BrowseName = new QualifiedName(name, NamespaceIndex);
            objectNode.DisplayName = objectNode.BrowseName.Name;
            objectNode.Description = String.Empty;
            objectNode.EventNotifier = EventNotifiers.SubscribeToEvents;

            objectNode.ReferenceTypeId = ReferenceTypes.Organizes;
            objectNode.TypeDefinitionId = ObjectTypeIds.BaseObjectType;

            if (parent != null)
            {
                parent.AddChild(objectNode);
            }

            return objectNode;
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
        /// Verifies that the specified node exists.
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

            return null;
        }

        private NodeId GenerateNodeId()
        {
            return new NodeId(++m_nextNodeId, NamespaceIndex);
        }
        #endregion

        #region Private Members
        private uint m_nextNodeId = 0;
        #endregion
    }
}