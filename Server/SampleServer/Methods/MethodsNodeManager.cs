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

namespace SampleServer.Methods
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class MethodsNodeManager : CustomNodeManager2
    {
        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public MethodsNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.Methods)
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
                root.BrowseName = new QualifiedName("Methods", NamespaceIndex);
                root.DisplayName = root.BrowseName.Name;
                root.Description = "Methods";
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

                // Add method nodes
                CreateAddMethod(root);
                CreateMultiplyMethod(root);
               
                // Save the node for later lookup (all tightly coupled children are added with this call)
                AddPredefinedNode(SystemContext, root);
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
                // Quickly exclude nodes that are not in the namespace. 
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
            
            return null;
        }

        private NodeId GenerateNodeId()
        {
            return new NodeId(++m_nextNodeId, NamespaceIndex);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a method in the address space
        /// </summary>
        private MethodState CreateAddMethod(NodeState parent)
        {
            MethodState addMethod = new MethodState(parent);

            addMethod.NodeId = new NodeId("Add", NamespaceIndex);
            addMethod.BrowseName = new QualifiedName("Add", NamespaceIndex);
            addMethod.DisplayName = addMethod.BrowseName.Name;
            addMethod.ReferenceTypeId = ReferenceTypeIds.HasComponent;
            addMethod.UserExecutable = true;
            addMethod.Executable = true;
            // Create the input arguments
            addMethod.InputArguments = new PropertyState<Argument[]>(addMethod);
            addMethod.InputArguments.NodeId = new NodeId(addMethod.BrowseName.Name + "InArgs", NamespaceIndex);
            addMethod.InputArguments.BrowseName = BrowseNames.InputArguments;
            addMethod.InputArguments.DisplayName = addMethod.InputArguments.BrowseName.Name;
            addMethod.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            addMethod.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            addMethod.InputArguments.DataType = DataTypeIds.Argument;
            addMethod.InputArguments.ValueRank = ValueRanks.OneDimension;

            addMethod.InputArguments.Value = new Argument[]
            {
                new Argument() { Name = "Float value", Description = "Float value",  DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar },
                new Argument() { Name = "UInt32 value", Description = "UInt32 value",  DataType = DataTypeIds.UInt32, ValueRank = ValueRanks.Scalar }
            };

            // Set output arguments
            addMethod.OutputArguments = new PropertyState<Argument[]>(addMethod);
            addMethod.OutputArguments.NodeId = new NodeId(addMethod.BrowseName.Name + "OutArgs", NamespaceIndex);
            addMethod.OutputArguments.BrowseName = BrowseNames.OutputArguments;
            addMethod.OutputArguments.DisplayName = addMethod.OutputArguments.BrowseName.Name;
            addMethod.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            addMethod.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            addMethod.OutputArguments.DataType = DataTypeIds.Argument;
            addMethod.OutputArguments.ValueRank = ValueRanks.OneDimension;

            addMethod.OutputArguments.Value = new Argument[]
            {
                new Argument() { Name = "Add Result", Description = "Add Result",  DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar }
            };

            addMethod.OnCallMethod = OnAddCall;

            if (parent != null)
            {
                parent.AddChild(addMethod);
            }

            return addMethod;
        }

        /// <summary>
        /// Handles the method call
        /// </summary>
        private ServiceResult OnAddCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            // All arguments must be provided
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                float floatValue = (float)inputArguments[0];
                UInt32 uintValue = (UInt32)inputArguments[1];

                // Set output parameter
                outputArguments[0] = (float)(floatValue + uintValue);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        /// <summary>
        /// Adds a method in the address space
        /// </summary>
        private MethodState CreateMultiplyMethod(NodeState parent)
        {
            MethodState multiplyMethod = new MethodState(parent);

            multiplyMethod.NodeId = new NodeId("Multiply", NamespaceIndex);
            multiplyMethod.BrowseName = new QualifiedName("Multiply", NamespaceIndex);
            multiplyMethod.DisplayName = multiplyMethod.BrowseName.Name;
            multiplyMethod.ReferenceTypeId = ReferenceTypeIds.HasComponent;
            multiplyMethod.UserExecutable = true;
            multiplyMethod.Executable = true;
            // Create the input arguments
            multiplyMethod.InputArguments = new PropertyState<Argument[]>(multiplyMethod);
            multiplyMethod.InputArguments.NodeId = new NodeId(multiplyMethod.BrowseName.Name + "InArgs", NamespaceIndex);
            multiplyMethod.InputArguments.BrowseName = BrowseNames.InputArguments;
            multiplyMethod.InputArguments.DisplayName = multiplyMethod.InputArguments.BrowseName.Name;
            multiplyMethod.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            multiplyMethod.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            multiplyMethod.InputArguments.DataType = DataTypeIds.Argument;
            multiplyMethod.InputArguments.ValueRank = ValueRanks.OneDimension;

            multiplyMethod.InputArguments.Value = new Argument[]
            {
                new Argument() { Name = "Int16 value", Description = "Int16 value",  DataType = DataTypeIds.Int16, ValueRank = ValueRanks.Scalar },
                new Argument() { Name = "UInt16 value", Description = "UInt16 value",  DataType = DataTypeIds.UInt16, ValueRank = ValueRanks.Scalar }
            };

            // Set output arguments
            multiplyMethod.OutputArguments = new PropertyState<Argument[]>(multiplyMethod);
            multiplyMethod.OutputArguments.NodeId = new NodeId(multiplyMethod.BrowseName.Name + "OutArgs", NamespaceIndex);
            multiplyMethod.OutputArguments.BrowseName = BrowseNames.OutputArguments;
            multiplyMethod.OutputArguments.DisplayName = multiplyMethod.OutputArguments.BrowseName.Name;
            multiplyMethod.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            multiplyMethod.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            multiplyMethod.OutputArguments.DataType = DataTypeIds.Argument;
            multiplyMethod.OutputArguments.ValueRank = ValueRanks.OneDimension;

            multiplyMethod.OutputArguments.Value = new Argument[]
            {
                new Argument() { Name = "Multiply Result", Description = "Multiply Result",  DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar }
            };

            multiplyMethod.OnCallMethod = OnMultiplyCall;

            if (parent != null)
            {
                parent.AddChild(multiplyMethod);
            }

            return multiplyMethod;
        }

        /// <summary>
        /// Handles the method call
        /// </summary>
        private ServiceResult OnMultiplyCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            // All arguments must be provided
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                Int16 op1 = (Int16)inputArguments[0];
                UInt16 op2 = (UInt16)inputArguments[1];

                // Set output parameter
                outputArguments[0] = (Int32)(op1 * op2);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        #endregion

        #region Private Fields

        private uint m_nextNodeId = 0;

        #endregion
    }
}