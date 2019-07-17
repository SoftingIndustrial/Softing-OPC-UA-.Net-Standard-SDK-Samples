/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.NodeManagement
{
    /// <summary>
    /// A node manager for a server that manages the NodeManagement Service Set
    /// </summary>
    public class NodeManagementNodeManager : NodeManager
    {
        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public NodeManagementNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.NodeManagement)
        {
        }

        #endregion       

        #region AddNodes Service

        /// <summary>
        /// Handle AddNodes service request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToAdd"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        public void AddNodes(OperationContext context, AddNodesItemCollection nodesToAdd, out AddNodesResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            // Validate nodesToAdd parameter
            if (nodesToAdd == null)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The nodesToAdd parameter is null.");
            }

            // Create result lists
            results = new AddNodesResultCollection(nodesToAdd.Count);
            diagnosticInfos = new DiagnosticInfoCollection(nodesToAdd.Count);

            Utils.Trace(Utils.TraceMasks.Information, "NodeManagementNodeManager.AddNodes", string.Format("NodeManagementNodeManager.AddNodes - Count={0}", nodesToAdd.Count));

            for (int ii = 0; ii < nodesToAdd.Count; ii++)
            {
                // Call AddNode and update results
                AddNodesResult addResult;
                DiagnosticInfo diagnosticInfo;

                AddNode(context, nodesToAdd[ii], out addResult, out diagnosticInfo);

                results.Add(addResult);
                diagnosticInfos.Add(diagnosticInfo);
            }
        }

        /// <summary>
        /// Validates the AddNode request and adds the node in the address space
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeToAdd"></param>
        /// <param name="result"></param>
        /// <param name="diagnosticInfo"></param>
        private void AddNode(OperationContext context, AddNodesItem nodeToAdd, out AddNodesResult result, out DiagnosticInfo diagnosticInfo)
        {
            result = new AddNodesResult();
            diagnosticInfo = new DiagnosticInfo();

            try
            {
                // Pre-validate the request
                ServiceResult error = ValidateAddNodesItem(context, nodeToAdd);

                if (ServiceResult.IsBad(error))
                {
                    result.StatusCode = error.Code;

                    // Add diagnostics if requested
                    if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
                    {
                        diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());
                    }

                    return;
                }

                // Perform the custom validation of the request
                error = ValidateAddNodeRequest(context, nodeToAdd);

                if (ServiceResult.IsBad(error))
                {
                    result.StatusCode = error.Code;

                    // Add diagnostics if requested
                    if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
                    {
                        diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());
                    }
                }
                else
                {
                    NodeState parentNode = null;

                    if (!PredefinedNodes.TryGetValue(ExpandedNodeId.ToNodeId(nodeToAdd.ParentNodeId, null), out parentNode))
                    {
                        // ParentNodeId not found in address space
                        throw new ServiceResultException(StatusCodes.BadParentNodeIdInvalid, "The specified ParentNodeId not found in address space.");
                    }

                    switch (nodeToAdd.NodeClass)
                    {
                        case NodeClass.Object:
                            // Create object node
                            result.AddedNodeId = AddObject(nodeToAdd, parentNode);
                            result.StatusCode = StatusCodes.Good;
                            break;
                        case NodeClass.Variable:
                            // Create variable node
                            result.AddedNodeId = AddVariable(nodeToAdd, parentNode);
                            result.StatusCode = StatusCodes.Good;
                            break;
                        default:
                            result.AddedNodeId = null;
                            result.StatusCode = StatusCodes.BadNodeClassInvalid;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                // Handle exception                
                ServiceResult error = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, e.Message);
                diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());

                result.StatusCode = error.StatusCode;
                result.AddedNodeId = NodeId.Null;
            }
        }

        /// <summary>
        ///  Validates if an AddNodesItem request structure respects the specification of the AddNodes service
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeToAdd"></param>
        /// <returns></returns>
        private ServiceResult ValidateAddNodesItem(OperationContext context, AddNodesItem nodeToAdd)
        {
            // Check parentNodeId
            if (nodeToAdd.ParentNodeId.IsNull)
            {
                return new ServiceResult(StatusCodes.BadParentNodeIdInvalid, "The specified ParentNodeId is null.");
            }

            NodeId parentNodeId = ExpandedNodeId.ToNodeId(nodeToAdd.ParentNodeId, null);

            if (parentNodeId.IsNullNodeId)
            {
                return new ServiceResult(StatusCodes.BadParentNodeIdInvalid, "The specified ParentNodeId is null.");
            }

            NodeState parentNode = null;

            if (!PredefinedNodes.TryGetValue(parentNodeId, out parentNode))
            {
                // ParentNodeId not found in address space
                return new ServiceResult(StatusCodes.BadParentNodeIdInvalid, "The specified ParentNodeId not found in address space.");
            }

            if (!nodeToAdd.RequestedNewNodeId.IsNull)
            {
                NodeState existingNode = null;

                if (PredefinedNodes.TryGetValue(ExpandedNodeId.ToNodeId(nodeToAdd.RequestedNewNodeId, null), out existingNode))
                {
                    // Requested NodeId already present in address space
                    return new ServiceResult(StatusCodes.BadNodeIdExists, "The requested node id is already used by another node.");
                }
            }

            // Check referenceTypeId                
            IReferenceType referenceType = Server.CoreNodeManager.GetLocalNode(nodeToAdd.ReferenceTypeId) as IReferenceType;

            if (referenceType == null)
            {
                // ReferenceTypeId not found in address space
                return new ServiceResult(StatusCodes.BadReferenceTypeIdInvalid, "The specified referenceTypeId not found in address space.");
            }

            // Check BrowseName
            if (nodeToAdd.BrowseName == null)
            {
                return new ServiceResult(StatusCodes.BadBrowseNameInvalid, "The specified BrowseName parameter is null");
            }

            // Check NodeClass
            if (nodeToAdd.NodeClass == NodeClass.Unspecified)
            {
                return new ServiceResult(StatusCodes.BadNodeClassInvalid, "The NodeClass parameter is not specified");
            }

            // Check NodeAttributes
            if (ExtensionObject.IsNull(nodeToAdd.NodeAttributes) && (nodeToAdd.NodeClass == NodeClass.Object || nodeToAdd.NodeClass == NodeClass.Variable))
            {
                return new ServiceResult(StatusCodes.BadNodeAttributesInvalid, "The specified NodeAttributes parameter is null");
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Validates an AddNodesItem request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeToAdd"></param>
        /// <returns></returns>
        public virtual ServiceResult ValidateAddNodeRequest(OperationContext context, AddNodesItem nodeToAdd)
        {
            // Return BadNotSupported
            // This method should be overriden in the derived class in order to allow clients to use the AddNodes service
            return new ServiceResult(StatusCodes.BadNotSupported, "Server does not allow nodes to be added by client.");
        }

        /// <summary>
        /// Creates an object node according to AddNodes operation request
        /// </summary>
        /// <param name="nodeToAdd"></param>
        /// <param name="parentNode"></param>
        /// <returns></returns>
        private NodeId AddObject(AddNodesItem nodeToAdd, NodeState parentNode)
        {
            // Check NodeAttributes
            ObjectAttributes attributes = nodeToAdd.NodeAttributes.Body as ObjectAttributes;

            if (attributes == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeAttributesInvalid, "The node Attributes are not valid for the node class.");
            }

            // Check TypeDefinition
            if (nodeToAdd.TypeDefinition == null)
            {
                throw new ServiceResultException(StatusCodes.BadTypeDefinitionInvalid, "The TypeDefinition parameter is required for object nodes");
            }

            if (!Server.TypeTree.IsKnown(nodeToAdd.TypeDefinition))
            {
                throw new ServiceResultException(StatusCodes.BadTypeDefinitionInvalid, "The TypeDefinition parameter is not valid.");
            }

            // Attempt to create the node according to specified TypeDefinition
            BaseObjectState objectToAdd = SystemContext.NodeStateFactory.CreateInstance(
                SystemContext,
                parentNode,
                nodeToAdd.NodeClass,
                nodeToAdd.BrowseName,
                nodeToAdd.ReferenceTypeId,
                ExpandedNodeId.ToNodeId(nodeToAdd.TypeDefinition, null)) as BaseObjectState;

            if (objectToAdd == null)
            {
                objectToAdd = new BaseObjectState(parentNode);
            }

            // Create the object and assign the nodeId returned by NodeIdFactory.New() method
            objectToAdd.Create(SystemContext, ExpandedNodeId.ToNodeId(nodeToAdd.RequestedNewNodeId, null), nodeToAdd.BrowseName, null, true);

            // Assign the requested NodeId if specified
            if (!nodeToAdd.RequestedNewNodeId.IsNull)
            {
                objectToAdd.NodeId = ExpandedNodeId.ToNodeId(nodeToAdd.RequestedNewNodeId, null);
            }

            // DisplayName
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.DisplayName) != 0)
            {
                objectToAdd.DisplayName = attributes.DisplayName;
            }

            // Description
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.Description) != 0)
            {
                objectToAdd.Description = attributes.Description;
            }

            // EventNotifier
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.EventNotifier) != 0)
            {
                objectToAdd.EventNotifier = attributes.EventNotifier;
            }

            // WriteMask
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.WriteMask) != 0)
            {
                objectToAdd.WriteMask = (AttributeWriteMask) attributes.WriteMask;
            }

            // UserWriteMask
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.UserWriteMask) != 0)
            {
                objectToAdd.UserWriteMask = (AttributeWriteMask) attributes.UserWriteMask;
            }

            objectToAdd.TypeDefinitionId = ExpandedNodeId.ToNodeId(nodeToAdd.TypeDefinition, null);
            objectToAdd.ReferenceTypeId = nodeToAdd.ReferenceTypeId;

            if (parentNode != null)
            {
                parentNode.AddChild(objectToAdd);

                parentNode.AddReference(nodeToAdd.ReferenceTypeId, false, objectToAdd.NodeId);
                objectToAdd.AddReference(nodeToAdd.ReferenceTypeId, true, parentNode.NodeId);
            }

            AddPredefinedNode(SystemContext, objectToAdd);

            return objectToAdd.NodeId;
        }

        /// <summary>
        /// Creates a variable node according to AddNodes operation request
        /// </summary>
        /// <param name="nodeToAdd"></param>
        /// <param name="parentNode"></param>
        /// <returns></returns>
        private NodeId AddVariable(AddNodesItem nodeToAdd, NodeState parentNode)
        {
            // Check NodeAttributes
            VariableAttributes attributes = nodeToAdd.NodeAttributes.Body as VariableAttributes;

            if (attributes == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeAttributesInvalid, "The node Attributes are not valid for the node class.");
            }

            // Check TypeDefinition
            if (nodeToAdd.TypeDefinition == null)
            {
                throw new ServiceResultException(StatusCodes.BadTypeDefinitionInvalid, "The TypeDefinition parameter is required for variable nodes.");
            }

            if (!Server.TypeTree.IsKnown(nodeToAdd.TypeDefinition))
            {
                throw new ServiceResultException(StatusCodes.BadTypeDefinitionInvalid, "The TypeDefinition parameter is not valid.");
            }

            // Attempt to create the node according to specified TypeDefinition
            BaseDataVariableState variableToAdd = SystemContext.NodeStateFactory.CreateInstance(
                SystemContext,
                parentNode,
                nodeToAdd.NodeClass,
                nodeToAdd.BrowseName,
                nodeToAdd.ReferenceTypeId,
                ExpandedNodeId.ToNodeId(nodeToAdd.TypeDefinition, null)) as BaseDataVariableState;

            if (variableToAdd == null)
            {
                variableToAdd = new BaseDataVariableState(parentNode);
            }

            // Create the variable and assign the nodeId returned by NodeIdFactory.New() method
            variableToAdd.Create(SystemContext, ExpandedNodeId.ToNodeId(nodeToAdd.RequestedNewNodeId, null), nodeToAdd.BrowseName, null, true);

            // Assign the requested NodeId if specified
            if (!nodeToAdd.RequestedNewNodeId.IsNull)
            {
                variableToAdd.NodeId = ExpandedNodeId.ToNodeId(nodeToAdd.RequestedNewNodeId, null);
            }

            // DisplayName
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.DisplayName) != 0)
            {
                variableToAdd.DisplayName = attributes.DisplayName;
            }
            else
            {
                variableToAdd.DisplayName = nodeToAdd.BrowseName.Name;
            }

            // Description
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.Description) != 0)
            {
                variableToAdd.Description = attributes.Description;
            }

            // Value
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.Value) != 0)
            {
                variableToAdd.Value = attributes.Value;
            }

            // DataType
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.DataType) != 0)
            {
                variableToAdd.DataType = attributes.DataType;
            }

            // ValueRank
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.ValueRank) != 0)
            {
                variableToAdd.ValueRank = attributes.ValueRank;
            }

            // ArrayDimensions
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.ArrayDimensions) != 0)
            {
                variableToAdd.ArrayDimensions = new ReadOnlyList<uint>(attributes.ArrayDimensions);
            }

            // AccessLevel
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.AccessLevel) != 0)
            {
                variableToAdd.AccessLevel = attributes.AccessLevel;
            }

            // UserAccessLevel
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.UserAccessLevel) != 0)
            {
                variableToAdd.UserAccessLevel = attributes.UserAccessLevel;
            }

            // MinimumSamplingInterval
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.MinimumSamplingInterval) != 0)
            {
                variableToAdd.MinimumSamplingInterval = attributes.MinimumSamplingInterval;
            }

            // Historizing
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.Historizing) != 0)
            {
                variableToAdd.Historizing = attributes.Historizing;
            }

            // WriteMask
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.WriteMask) != 0)
            {
                variableToAdd.WriteMask = (AttributeWriteMask) attributes.WriteMask;
            }

            // UserWriteMask
            if ((attributes.SpecifiedAttributes & (uint) NodeAttributesMask.UserWriteMask) != 0)
            {
                variableToAdd.UserWriteMask = (AttributeWriteMask) attributes.UserWriteMask;
            }

            variableToAdd.StatusCode = StatusCodes.Good;
            variableToAdd.Timestamp = DateTime.UtcNow;

            variableToAdd.TypeDefinitionId = ExpandedNodeId.ToNodeId(nodeToAdd.TypeDefinition, null);
            variableToAdd.ReferenceTypeId = nodeToAdd.ReferenceTypeId;

            if (parentNode != null)
            {
                parentNode.AddChild(variableToAdd);

                parentNode.AddReference(nodeToAdd.ReferenceTypeId, false, variableToAdd.NodeId);
                variableToAdd.AddReference(nodeToAdd.ReferenceTypeId, true, parentNode.NodeId);
            }

            AddPredefinedNode(SystemContext, variableToAdd);

            return variableToAdd.NodeId;
        }

        #endregion

        #region DeleteNodes Service

        /// Handle DeleteNodes service request
        public void DeleteNodes(OperationContext context, DeleteNodesItemCollection nodesToDelete, out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            // Validate nodesToDelete parameter
            if (nodesToDelete == null)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The nodesToDelete parameter is null.");
            }

            // Create result lists
            results = new StatusCodeCollection(nodesToDelete.Count);
            diagnosticInfos = new DiagnosticInfoCollection(nodesToDelete.Count);

            Utils.Trace(Utils.TraceMasks.Information, "NodeManagementNodeManager.DeleteNodes", string.Format("NodeManagementNodeManager.DeleteNodes - Count={0}", nodesToDelete.Count));

            for (int ii = 0; ii < nodesToDelete.Count; ii++)
            {
                // Call DeleteNode and update results
                StatusCode deleteResult;
                DiagnosticInfo diagnosticInfo;

                DeleteNode(context, nodesToDelete[ii], out deleteResult, out diagnosticInfo);

                results.Add(deleteResult);
                diagnosticInfos.Add(diagnosticInfo);
            }
        }

        // Validates the DeleteNode request and deletes the node from address space
        private void DeleteNode(OperationContext context, DeleteNodesItem nodeToDelete, out StatusCode result, out DiagnosticInfo diagnosticInfo)
        {
            result = new StatusCode();
            diagnosticInfo = new DiagnosticInfo();

            try
            {
                // Pre-validate the request
                ServiceResult error = ValidateDeleteNodesItem(context, nodeToDelete);

                if (ServiceResult.IsBad(error))
                {
                    result = error.Code;

                    // Add diagnostics if requested
                    if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
                    {
                        diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());
                    }

                    return;
                }

                // Perform the custom validation of the request
                error = ValidateDeleteNodesRequest(context, nodeToDelete);

                if (ServiceResult.IsBad(error))
                {
                    result = error.Code;

                    // Add diagnostics if requested.
                    if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
                    {
                        diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());
                    }
                }
                else
                {
                    NodeState node = null;

                    if (!PredefinedNodes.TryGetValue(nodeToDelete.NodeId, out node))
                    {
                        // NodeId not found in address space
                        throw new ServiceResultException(StatusCodes.BadNodeIdInvalid, "The specified NodeId was not found in address space.");
                    }

                    List<LocalReference> referencesToRemove = new List<LocalReference>();

                    // Remove the specified node from address space
                    lock (Lock)
                    {
                        // Remove from predefined nodes
                        PredefinedNodes.Remove(node.NodeId);

                        node.UpdateChangeMasks(NodeStateChangeMasks.Deleted);
                        node.ClearChangeMasks(SystemContext, false);
                        OnNodeRemoved(node);

                        // Remove from the parent
                        BaseInstanceState instance = node as BaseInstanceState;

                        if (instance != null && instance.Parent != null)
                        {
                            instance.Parent.RemoveChild(instance);
                        }

                        // Retrieve node references
                        List<IReference> references = new List<IReference>();
                        node.GetReferences(SystemContext, references);

                        for (int ii = 0; ii < references.Count; ii++)
                        {
                            IReference reference = references[ii];

                            if (reference.TargetId.IsAbsolute)
                            {
                                continue;
                            }

                            if (reference.IsInverse && !nodeToDelete.DeleteTargetReferences)
                            {
                                continue;
                            }

                            LocalReference referenceToRemove = new LocalReference(
                                (NodeId) reference.TargetId,
                                reference.ReferenceTypeId,
                                reference.IsInverse,
                                node.NodeId);

                            referencesToRemove.Add(referenceToRemove);
                        }

                        RemoveRootNotifier(node);
                    }

                    // Must release the lock before removing cross references to other node managers
                    if (referencesToRemove.Count > 0)
                    {
                        Server.NodeManager.RemoveReferences(referencesToRemove);
                    }

                    result = StatusCodes.Good;
                }
            }
            catch (Exception e)
            {
                // Handle exception                
                ServiceResult error = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, e.Message);
                diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());

                result = error.StatusCode;
            }
        }

        // Validates if a DeleteNodesItem request structure respects the specification of the DeleteNodesItem service
        private ServiceResult ValidateDeleteNodesItem(OperationContext context, DeleteNodesItem nodeToDelete)
        {
            // Check NodeId
            if (nodeToDelete.NodeId.IsNullNodeId)
            {
                return new ServiceResult(StatusCodes.BadNodeIdInvalid, "The specified NodeId is null.");
            }

            NodeState node = null;

            if (!PredefinedNodes.TryGetValue(nodeToDelete.NodeId, out node))
            {
                // NodeId not found in address space
                return new ServiceResult(StatusCodes.BadNodeIdInvalid, "The specified NodeId was not found in address space.");
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Validates an DeleteNodesItem request
        /// </summary>
        public virtual ServiceResult ValidateDeleteNodesRequest(OperationContext context, DeleteNodesItem nodeToDelete)
        {
            // Return BadNotSupported
            // This method should be overriden in the derived class in order to allow clients to use the DeleteNodes service
            return new ServiceResult(StatusCodes.BadNotSupported, "Server does not allow nodes to be deleted by client.");
        }

        /// <summary>
        /// Called after a node has been deleted
        /// </summary>
        protected override void OnNodeRemoved(NodeState node)
        {
            base.OnNodeRemoved(node);

            // When a deleted node is being monitored, then a Notification containing the status code Bad_NodeIdUnknown
            // should be sent to the monitoring Client indicating that the Node has been deleted.

            if (node.NodeClass == NodeClass.Variable)
            {
                BaseDataVariableState variableNode = node as BaseDataVariableState;

                if (variableNode != null)
                {
                    variableNode.Value = Variant.Null;
                    variableNode.Timestamp = DateTime.Now;
                    variableNode.StatusCode = StatusCodes.BadNodeIdUnknown;

                    // The call back pushes the updated values into the monitored items.
                    variableNode.ClearChangeMasks(SystemContext, true);
                }
            }
        }

        #endregion

        #region AddReferences Service

        /// <summary>
        /// Handle AddReferences service request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="referencesToAdd"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        public void AddReferences(OperationContext context, AddReferencesItemCollection referencesToAdd, out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            // Validate referencesToAdd parameter
            if (referencesToAdd == null)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The referencesToAdd parameter is null.");
            }

            // Create result lists
            results = new StatusCodeCollection(referencesToAdd.Count);
            diagnosticInfos = new DiagnosticInfoCollection(referencesToAdd.Count);

            Utils.Trace(Utils.TraceMasks.Information, "NodeManagementNodeManager.AddReferences", string.Format("NodeManagementNodeManager.AddReferences - Count={0}", referencesToAdd.Count));

            for (int ii = 0; ii < referencesToAdd.Count; ii++)
            {
                // Call AddReference and update results
                StatusCode addResult;
                DiagnosticInfo diagnosticInfo;

                AddReference(context, referencesToAdd[ii], out addResult, out diagnosticInfo);

                results.Add(addResult);
                diagnosticInfos.Add(diagnosticInfo);
            }
        }

        // Validates the AddReference request and adds the requested reference
        private void AddReference(OperationContext context, AddReferencesItem referenceToAdd, out StatusCode result, out DiagnosticInfo diagnosticInfo)
        {
            result = new StatusCode();
            diagnosticInfo = new DiagnosticInfo();

            try
            {
                // Pre-validate the request
                ServiceResult error = ValidateAddReferencesItem(context, referenceToAdd);

                if (ServiceResult.IsBad(error))
                {
                    result = error.Code;

                    // Add diagnostics if requested
                    if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
                    {
                        diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());
                    }

                    return;
                }

                // Perform the custom validation of the request
                error = ValidateAddReferencesRequest(context, referenceToAdd);

                if (ServiceResult.IsBad(error))
                {
                    result = error.Code;

                    // Add diagnostics if requested
                    if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
                    {
                        diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());
                    }
                }
                else
                {
                    NodeState node = null;

                    if (!PredefinedNodes.TryGetValue(referenceToAdd.SourceNodeId, out node))
                    {
                        // SourceNodeId not found in address space
                        throw new ServiceResultException(StatusCodes.BadSourceNodeIdInvalid, "The specified SourceNodeId was not found in address space.");
                    }

                    // Add the reference
                    node.AddReference(referenceToAdd.ReferenceTypeId, !referenceToAdd.IsForward, referenceToAdd.TargetNodeId);
                    node.ClearChangeMasks(SystemContext, false);

                    result = StatusCodes.Good;
                }
            }
            catch (Exception e)
            {
                // Handle exception                
                ServiceResult error = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, e.Message);
                diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());

                result = error.StatusCode;
            }
        }

        // Validates if an AddReferencesItem request structure respects the specification of the AddReferences service
        private ServiceResult ValidateAddReferencesItem(OperationContext context, AddReferencesItem referenceToAdd)
        {
            // Check sourceNodeId
            if (referenceToAdd.SourceNodeId.IsNullNodeId)
            {
                return new ServiceResult(StatusCodes.BadSourceNodeIdInvalid, "The specified SourceNodeId is null.");
            }

            NodeState node = null;

            if (!PredefinedNodes.TryGetValue(referenceToAdd.SourceNodeId, out node))
            {
                // SourceNodeId not found in address space
                return new ServiceResult(StatusCodes.BadSourceNodeIdInvalid, "The specified SourceNodeId was not found in address space.");
            }

            // Check referenceTypeId
            IReferenceType referenceType = Server.CoreNodeManager.GetLocalNode(referenceToAdd.ReferenceTypeId) as IReferenceType;

            if (referenceType == null)
            {
                // ReferenceTypeId not found in address space
                return new ServiceResult(StatusCodes.BadReferenceTypeIdInvalid, "The specified referenceTypeId not found in address space.");
            }

            // Check targetNodeId
            NodeState targetNode = null;

            if (!PredefinedNodes.TryGetValue(ExpandedNodeId.ToNodeId(referenceToAdd.TargetNodeId, null), out targetNode))
            {
                // TargetNodeId not found in address space
                return new ServiceResult(StatusCodes.BadTargetNodeIdInvalid, "The specified TargetNodeId not found in address space.");
            }

            // Check if TargetNodeId is from a remote server
            if (referenceToAdd.TargetNodeId.ServerIndex != 0)
            {
                // Do not allow references to a remote Server
                return new ServiceResult(StatusCodes.BadReferenceLocalOnly, "References to remote servers not allowed.");
            }

            // Check NodeClass
            // The TargetNodeClass is an input parameter that is used to validate that the Reference to be added matches the NodeClass of the TargetNode.
            if (referenceToAdd.TargetNodeClass != targetNode.NodeClass)
            {
                // NodeClass of the targetNode does not match the specified TargetNodeClass
                return new ServiceResult(StatusCodes.BadNodeClassInvalid, "The specified TargetNodeClass does not match the NodeClass of TargetNodeId.");
            }

            // Retrieve node references
            List<IReference> references = new List<IReference>();
            node.GetReferences(SystemContext, references);

            for (int ii = 0; ii < references.Count; ii++)
            {
                IReference reference = references[ii];

                // Check if the reference already exists
                if (reference.ReferenceTypeId == referenceToAdd.ReferenceTypeId && reference.TargetId == referenceToAdd.TargetNodeId)
                {
                    // The requested reference already exists
                    return new ServiceResult(StatusCodes.BadDuplicateReferenceNotAllowed, "The specified reference already exists.");
                }
            }

            return ServiceResult.Good;
        }

        // Validates an AddReferencesItem request
        public virtual ServiceResult ValidateAddReferencesRequest(OperationContext context, AddReferencesItem referenceToAdd)
        {
            // Return BadNotSupported
            // This method should be overriden in the derived class in order to allow clients to use the AddReferences service
            return new ServiceResult(StatusCodes.BadNotSupported, "Server does not allow references to be added by client.");
        }

        #endregion

        #region DeleteReferences Service

        /// <summary>
        /// Handle DeleteReferences service request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="referencesToDelete"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        public void DeleteReferences(OperationContext context, DeleteReferencesItemCollection referencesToDelete, out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            // Validate referencesToDelete parameter
            if (referencesToDelete == null)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The referencesToDelete parameter is null.");
            }

            // Create result lists
            results = new StatusCodeCollection(referencesToDelete.Count);
            diagnosticInfos = new DiagnosticInfoCollection(referencesToDelete.Count);

            Utils.Trace(Utils.TraceMasks.Information, "Opc.Ua.Server.NodeManagementNodeManager.DeleteReferences", string.Format("NodeManagementNodeManager.DeleteReferences - Count={0}", referencesToDelete.Count));

            for (int ii = 0; ii < referencesToDelete.Count; ii++)
            {
                // Call DeleteReference and update results
                StatusCode addResult;
                DiagnosticInfo diagnosticInfo;

                DeleteReference(context, referencesToDelete[ii], out addResult, out diagnosticInfo);

                results.Add(addResult);
                diagnosticInfos.Add(diagnosticInfo);
            }
        }

        // Validates the DeleteReference request and deletes the requested reference
        private void DeleteReference(OperationContext context, DeleteReferencesItem referenceToDelete, out StatusCode result, out DiagnosticInfo diagnosticInfo)
        {
            result = new StatusCode();
            diagnosticInfo = new DiagnosticInfo();

            try
            {
                // Pre-validate the request
                ServiceResult error = ValidateDeleteReferencesItem(context, referenceToDelete);

                if (ServiceResult.IsBad(error))
                {
                    result = error.Code;

                    // Add diagnostics if requested
                    if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
                    {
                        diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());
                    }

                    return;
                }

                // Perform the custom validation of the request
                error = ValidateDeleteReferencesRequest(context, referenceToDelete);

                if (ServiceResult.IsBad(error))
                {
                    result = error.Code;

                    // Add diagnostics if requested
                    if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
                    {
                        diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());
                    }
                }
                else
                {
                    NodeState node = null;

                    if (!PredefinedNodes.TryGetValue(referenceToDelete.SourceNodeId, out node))
                    {
                        // SourceNodeId not found in address space
                        throw new ServiceResultException(StatusCodes.BadSourceNodeIdInvalid, "The specified SourceNodeId was not found in address space.");
                    }

                    // Remove the specified reference
                    node.RemoveReference(referenceToDelete.ReferenceTypeId, !referenceToDelete.IsForward, referenceToDelete.TargetNodeId);

                    if (referenceToDelete.DeleteBidirectional && referenceToDelete.TargetNodeId.ServerIndex == 0)
                    {
                        // Delete also the opposite reference if required
                        node.RemoveReference(referenceToDelete.ReferenceTypeId, referenceToDelete.IsForward, referenceToDelete.TargetNodeId);
                    }

                    result = StatusCodes.Good;
                }
            }
            catch (Exception e)
            {
                // Handle exception                
                ServiceResult error = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, e.Message);
                diagnosticInfo = new DiagnosticInfo(error, context.DiagnosticsMask, false, new StringTable());

                result = error.StatusCode;
            }
        }

        // Validates if a DeleteReferencesItem request structure respects the specification of the DeleteReferences service
        private ServiceResult ValidateDeleteReferencesItem(OperationContext context, DeleteReferencesItem referenceToDelete)
        {
            // Check sourceNodeId
            if (referenceToDelete.SourceNodeId.IsNullNodeId)
            {
                return new ServiceResult(StatusCodes.BadSourceNodeIdInvalid, "The specified SourceNodeId is null.");
            }

            NodeState node = null;

            if (!PredefinedNodes.TryGetValue(referenceToDelete.SourceNodeId, out node))
            {
                // SourceNodeId not found in address space
                return new ServiceResult(StatusCodes.BadSourceNodeIdInvalid, "The specified SourceNodeId was not found in address space.");
            }

            // Check referenceTypeId
            IReferenceType referenceType = Server.CoreNodeManager.GetLocalNode(referenceToDelete.ReferenceTypeId) as IReferenceType;

            if (referenceType == null)
            {
                // ReferenceTypeId not found in address space
                return new ServiceResult(StatusCodes.BadReferenceTypeIdInvalid, "The specified referenceTypeId not found in address space.");
            }

            // Check targetNodeId
            NodeState targetNode = null;

            if (!PredefinedNodes.TryGetValue(ExpandedNodeId.ToNodeId(referenceToDelete.TargetNodeId, null), out targetNode))
            {
                // TargetNodeId not found in address space;
                return new ServiceResult(StatusCodes.BadTargetNodeIdInvalid, "The specified TargetNodeId not found in address space.");
            }

            // Check if TargetNodeId is from a remote server
            if (referenceToDelete.TargetNodeId.ServerIndex != 0)
            {
                // Do not allow to delete references to a remote Server
                return new ServiceResult(StatusCodes.BadTargetNodeIdInvalid, "References to remote servers not allowed.");
            }

            // Check if the reference exists
            if (!node.ReferenceExists(referenceToDelete.ReferenceTypeId, !referenceToDelete.IsForward, referenceToDelete.TargetNodeId))
            {
                // The specified reference does not exist
                return new ServiceResult(StatusCodes.BadNoEntryExists, "The specified reference does not exist.");
            }

            return ServiceResult.Good;
        }

        // Validates a DeleteReferencesItem request.
        public virtual ServiceResult ValidateDeleteReferencesRequest(OperationContext context, DeleteReferencesItem referenceToDelete)
        {
            // Return BadNotSupported
            // This method should be overriden in the derived class in order to allow clients to use the AddReferences service
            return new ServiceResult(StatusCodes.BadNotSupported, "Server does not allow references to be deleted by client.");
        }

        #endregion
    }
}