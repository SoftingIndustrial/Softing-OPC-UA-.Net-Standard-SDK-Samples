/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/
 
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
        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public DynamicASNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration)
        {
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
                // Create a root node and add a reference to external Server Objects Folder
                FolderState root = CreateFolder(null, "NodeManagement");
                root.Description = "UA Node Management Server Root";
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);
                
                
                // Add some initial nodes
                FolderState node1 = CreateFolder(root, "Node1");

                FolderState node11 = CreateFolder(node1, "Node1_1");
                AddVariable(node11, "Variable1_1_1", BuiltInType.Int32);

                FolderState node12 = CreateFolder(node1, "Node1_2");
                AddVariable(node12, "Variable1_2_1", BuiltInType.Int32);

                FolderState node13 = CreateFolder(node1, "Node1_3");
                AddVariable(node13, "Variable1_3_1", BuiltInType.Int32);
                AddVariable(node13, "Variable1_3_2", BuiltInType.Int32);
                
            }
        }        

        /// <summary>
        /// Creates a new variable and adds it to the specified parent.
        /// </summary>
        private void AddVariable(NodeState parent, string name, BuiltInType dataType, int valueRank = ValueRanks.Scalar)
        {
            BaseDataVariableState variable = CreateVariable(parent, name, (uint)dataType, valueRank);
            
            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;                       
        }

        #endregion

        #region NodeManagement Methods

        /// <summary>
        /// Validates an AddNodesItem request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeToAdd"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Validates a DeleteNodesItem request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeToDelete"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Validates an AddReferencesItem request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="referenceToAdd"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Validates a DeleteReferencesItem request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="referenceToDelete"></param>
        /// <returns></returns>
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