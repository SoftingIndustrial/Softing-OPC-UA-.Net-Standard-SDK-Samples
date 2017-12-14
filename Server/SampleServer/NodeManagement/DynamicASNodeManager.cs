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

namespace SampleServer.NodeManagement
{
    /// <summary>
    /// A node manager for a server that manages a dynamically created address space
    /// </summary>
    class DynamicASNodeManager : NodeManagementNodeManager
    {
        #region Private Members

        private uint m_nextNodeId;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public DynamicASNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration)
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
                root.BrowseName = new QualifiedName("NodeManagement", NamespaceIndex);
                root.DisplayName = root.BrowseName.Name;
                root.Description = "UA Node Management Server Root";
                root.TypeDefinitionId = ObjectTypeIds.FolderType;

                // Ensure the process object can be found via the server object. 
                IList<IReference> references;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                root.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, root.NodeId));

                // Add some initial nodes
                FolderState node1 = AddFolder(root, "Node1");

                FolderState node11 = AddFolder(node1, "Node1_1");
                AddVariable(node11, "Variable1_1_1", BuiltInType.Int32, ValueRanks.Scalar);

                FolderState node12 = AddFolder(node1, "Node1_2");
                AddVariable(node12, "Variable1_2_1", BuiltInType.Int32, ValueRanks.Scalar);

                FolderState node13 = AddFolder(node1, "Node1_3");
                AddVariable(node13, "Variable1_3_1", BuiltInType.Int32, ValueRanks.Scalar);
                AddVariable(node13, "Variable1_3_2", BuiltInType.Int32, ValueRanks.Scalar);

                // Save the node for later lookup (all tightly coupled children are added with this call)
                AddPredefinedNode(SystemContext, root);
            }
        }

        /// <summary>
        /// Creates a new folder and adds it to the specified parent
        /// </summary>
        private FolderState AddFolder(NodeState parent, string name)
        {
            FolderState folder = new FolderState(parent);

            folder.NodeId = GenerateNodeId();
            folder.BrowseName = new QualifiedName(name, NamespaceIndex);
            folder.DisplayName = folder.BrowseName.Name;
            folder.Description = String.Empty;
            folder.EventNotifier = EventNotifiers.None;

            folder.ReferenceTypeId = ReferenceTypes.Organizes;
            folder.TypeDefinitionId = ObjectTypeIds.FolderType;

            if (parent != null)
            {
                parent.AddChild(folder);

                parent.AddReference(ReferenceTypeIds.Organizes, false, folder.NodeId);
                folder.AddReference(ReferenceTypeIds.Organizes, true, parent.NodeId);
            }

            return folder;
        }

        /// <summary>
        /// Creates a new variable and adds it to the specified parent.
        /// </summary>
        private void AddVariable(NodeState parent, string name, BuiltInType dataType, int valueRank)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent);

            variable.NodeId = GenerateNodeId();
            variable.BrowseName = new QualifiedName(name, NamespaceIndex);
            variable.DisplayName = variable.BrowseName.Name;
            variable.Description = String.Empty;
            variable.Value = new Variant(0);
            variable.DataType = (uint) dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;

            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;

            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (parent != null)
            {
                parent.AddChild(variable);

                parent.AddReference(ReferenceTypeIds.Organizes, false, variable.NodeId);
                variable.AddReference(ReferenceTypeIds.Organizes, true, parent.NodeId);
            }
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                // TBD
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
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

        #region NodeManagement Methods

        // Validates an AddNodesItem request
        public override ServiceResult ValidateAddNodeRequest(OperationContext context, AddNodesItem nodeToAdd)
        {
            if (context.UserIdentity == null || context.UserIdentity.TokenType == UserTokenType.Anonymous)
            {
                // Anonymous users not allowed to add nodes.
                // A custom logic for validating access rights can be implemented in this place.
                return new ServiceResult(StatusCodes.BadUserAccessDenied, "User cannot add nodes.");
            }

            if (!nodeToAdd.RequestedNewNodeId.IsNull && nodeToAdd.RequestedNewNodeId.ServerIndex != 0)
            {
                // Do not allow remote nodes to be added
                return new ServiceResult(StatusCodes.BadNodeIdRejected, "Remote nodes not allowed.");
            }

            if (!nodeToAdd.RequestedNewNodeId.IsNull)
            {
                if ((ExpandedNodeId.ToNodeId(nodeToAdd.RequestedNewNodeId, null).NamespaceIndex != NamespaceIndex))
                {
                    // Allow only nodes with local NamespaceIndex
                    return new ServiceResult(StatusCodes.BadNodeIdRejected, "NamespaceIndex not allowed");
                }
            }

            // Allow AddNodes service requests
            return ServiceResult.Good;
        }

        // Validates a DeleteNodesItem request
        public override ServiceResult ValidateDeleteNodesRequest(OperationContext context, DeleteNodesItem nodeToDelete)
        {
            if (context.UserIdentity == null || context.UserIdentity.TokenType == UserTokenType.Anonymous)
            {
                // Anonymous users not allowed to delete nodes.
                // A custom logic for validating access rights can be implemented in this place.
                return new ServiceResult(StatusCodes.BadUserAccessDenied, "User cannot delete nodes.");
            }

            // Allow DeleteNodes service requests
            return ServiceResult.Good;
        }

        // Validates an AddReferencesItem request
        public override ServiceResult ValidateAddReferencesRequest(OperationContext context, AddReferencesItem referenceToAdd)
        {
            if (context.UserIdentity == null || context.UserIdentity.TokenType == UserTokenType.Anonymous)
            {
                // Anonymous users not allowed to add references.
                // A custom logic for validating access rights can be implemented in this place.
                return new ServiceResult(StatusCodes.BadUserAccessDenied, "User cannot add references.");
            }

            // Allow AddReferences service requests
            return ServiceResult.Good;
        }

        // Validates a DeleteReferencesItem request
        public override ServiceResult ValidateDeleteReferencesRequest(OperationContext context, DeleteReferencesItem referenceToDelete)
        {
            if (context.UserIdentity == null || context.UserIdentity.TokenType == UserTokenType.Anonymous)
            {
                // Anonymous users not allowed to add references.
                // A custom logic for validating access rights can be implemented in this place.
                throw new ServiceResultException(StatusCodes.BadUserAccessDenied, "User cannot add references.");
            }

            // Allow DeleteReferences service requests
            return ServiceResult.Good;
        }

        #endregion
    }
}