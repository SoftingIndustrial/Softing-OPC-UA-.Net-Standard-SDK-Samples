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
using System.IO;
using System.Linq;
using System.Xml;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.NodeSetImport
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class NodeSetImportNodeManager : NodeManager
    {
        #region Constructors
        /// <summary>
        /// Initializes the node manager. 
        /// </summary>
        public NodeSetImportNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.Refrigerators)
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
                // Import the initial data model from a NodeSet file
                Import(SystemContext, Path.Combine(m_InitialModelFilePath));

                try
                {
                    // Find the "NodeSetImport" node
                    NodeState nodeSetImportNode = PredefinedNodes.Values.FirstOrDefault(x => x.BrowseName.Name == "NodeSetImport");

                    // Add a method for creating a secondary refrigerator from file
                    if (nodeSetImportNode != null)
                    {
                        MethodState addDeviceMethod = new MethodState(nodeSetImportNode);

                        addDeviceMethod.NodeId = new NodeId("AddSecondaryRefrigerator", NamespaceIndex);
                        addDeviceMethod.BrowseName = new QualifiedName("AddSecondaryRefrigerator", NamespaceIndex);
                        addDeviceMethod.DisplayName = addDeviceMethod.BrowseName.Name;
                        addDeviceMethod.ReferenceTypeId = ReferenceTypeIds.HasComponent;
                        addDeviceMethod.UserExecutable = true;
                        addDeviceMethod.Executable = true;

                        nodeSetImportNode.AddChild(addDeviceMethod);

                        AddPredefinedNode(SystemContext, addDeviceMethod);

                        // Register handler
                        addDeviceMethod.OnCallMethod = OnAddRefrigerator;
                    }

                    // Add a method for importing a NodeSet
                    if (nodeSetImportNode != null)
                    {
                        MethodState importMethod = new MethodState(nodeSetImportNode);
                        importMethod.NodeId = new NodeId("Import", NamespaceIndex);
                        importMethod.BrowseName = new QualifiedName("Import", NamespaceIndex);
                        importMethod.DisplayName = importMethod.BrowseName.Name;
                        importMethod.Executable = true;
                        importMethod.ReferenceTypeId = ReferenceTypeIds.HasComponent;
                        nodeSetImportNode.AddChild(importMethod);

                        // Create the input arguments
                        PropertyState<Argument[]> inputArguments = new PropertyState<Argument[]>(importMethod);
                        inputArguments.NodeId = new NodeId("Import_InputArguments", NamespaceIndex);
                        inputArguments.BrowseName = new QualifiedName(BrowseNames.InputArguments);
                        inputArguments.DisplayName = inputArguments.BrowseName.Name;
                        inputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                        inputArguments.DataType = DataTypeIds.Argument;
                        inputArguments.ValueRank = ValueRanks.OneDimension;
                        inputArguments.AccessLevel = AccessLevels.CurrentRead;
                        inputArguments.UserAccessLevel = AccessLevels.CurrentRead;
                        inputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;

                        inputArguments.Value = new Argument[]
                        {
                        new Argument("File path", DataTypeIds.String, ValueRanks.Scalar, null)
                        };

                        importMethod.InputArguments = inputArguments;

                        AddPredefinedNode(SystemContext, importMethod);

                        // Register handler
                        importMethod.OnCallMethod = OnImportNodeSet;
                    }
                }
                catch (Exception ex)
                {
                    Utils.Trace(Utils.TraceMasks.Error, "NodeSetImportNodeManager.CreateAddressSpace", "Error adding methods:", ex.Message);
                }
            }
        }

        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            // This override will receive a callback every time a new node is added
            // e.g. The extension data can be received in predefinedNode.Extensions
            return predefinedNode;
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
        #endregion

        #region Private Methods
        /// <summary>
        /// Imports into the address space an xml file containing the model structure
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filePath">The path of the NodeSet XML file</param>
        private ServiceResult Import(ISystemContext context, string filePath)
        {
            try
            {
                ImportNodeSet(context, filePath);
            }
            catch (Exception ex)
            {
                Utils.Trace(Utils.TraceMasks.Error, "NodeSetImportNodeManager.Import", "Error loading node set: {0}", ex.Message);
                throw new ServiceResultException(ex, StatusCodes.Bad);
            }

            return ServiceResult.Good;
        }
        
        /// <summary>
        /// Handles the AddDeviceMethodCall
        /// </summary>
        private ServiceResult OnAddRefrigerator(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            return Import(context, Path.Combine(m_SecondaryModelFilePath));
        }

        private ServiceResult OnImportNodeSet(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            string filePath = (string)inputArguments[0];
            return Import(context, filePath);
        }
        #endregion

        #region Private Members
        private readonly string[] m_InitialModelFilePath = { "NodeSetImport","Refrigerators.NodeSet2.xml" };
        private readonly string[] m_SecondaryModelFilePath = { "NodeSetImport","Refrigerators2.NodeSet2.xml" };
        #endregion
    }
}