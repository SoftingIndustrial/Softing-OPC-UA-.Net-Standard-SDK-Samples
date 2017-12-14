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
using System.Reflection;
using System.Xml;
using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;

namespace SampleServer.NodeSetImport
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class NodeSetImportNodeManager : CustomNodeManager2
    {
        #region Constructors
        /// <summary>
        /// Initializes the node manager. 
        /// </summary>
        public NodeSetImportNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.Refrigerators)
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
            return node.NodeId;
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
                Import(SystemContext, m_InitialModelFilePath);

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
            return Import(context, m_SecondaryModelFilePath);
        }

        private ServiceResult OnImportNodeSet(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            string filePath = (string)inputArguments[0];
            return Import(context, filePath);
        }

        private XmlElement[] ImportNodeSet(ISystemContext context, string filePath)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            List<string> newNamespaceUris = new List<string>();

            XmlElement[] extensions = LoadFromNodeSet2Xml(context, filePath, true, newNamespaceUris, predefinedNodes);

            // Add the node set to the node manager
            for (int ii = 0; ii < predefinedNodes.Count; ii++)
            {
                AddPredefinedNode(context, predefinedNodes[ii]);
            }

            foreach (var item in NamespaceUris)
            {
                if (newNamespaceUris.Contains(item))
                {
                    newNamespaceUris.Remove(item);
                }
            }

            if (newNamespaceUris.Count > 0)
            {
                List<string> allNamespaceUris = newNamespaceUris.ToList();
                allNamespaceUris.AddRange(NamespaceUris);

                SetNamespaces(allNamespaceUris.ToArray());
            }

            UpdateRegistration(this, newNamespaceUris);

            // Ensure the reverse references exist
            Dictionary<NodeId, IList<IReference>> externalReferences = new Dictionary<NodeId, IList<IReference>>();
            AddReverseReferences(externalReferences);

            foreach (var item in externalReferences)
            {
                Server.NodeManager.AddReferences(item.Key, item.Value);
            }

            return extensions;
        }

        /// <summary>
        /// Loads the NodeSet2.xml file and returns the Extensions data of the node set
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="updateTables">if set to <c>true</c> the namespace and server tables are updated with any new URIs.</param>
        /// <param name="namespaceUris">Returns the NamespaceUris defined in the node set.</param>
        /// <param name="predefinedNodes">The required NodeStateCollection</param>
        /// <returns>The collection of global extensions of the NodeSet2.xml file.</returns>
        private XmlElement[] LoadFromNodeSet2Xml(ISystemContext context, string filePath, bool updateTables, List<string> namespaceUris, NodeStateCollection predefinedNodes)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            byte[] readAllBytes = File.ReadAllBytes(filePath);
            MemoryStream istrm = new MemoryStream(readAllBytes);

            if (istrm == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError, "Could not load nodes from resource: {0}", filePath);
            }

            return LoadFromNodeSet2(context, istrm, updateTables, namespaceUris, predefinedNodes);
        }

        /// <summary>
        /// Reads the schema information from a NodeSet2 XML document
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="istrm">The data stream containing a UANodeSet file.</param>
        /// <param name="updateTables">If set to <c>true</c> the namespace and server tables are updated with any new URIs.</param>
        /// <param name="namespaceUris">Returns the NamespaceUris defined in the node set.</param>
        /// /// <param name="predefinedNodes">The required NodeStateCollection</param>
        /// <returns>The collection of global extensions of the node set.</returns>
        private XmlElement[] LoadFromNodeSet2(ISystemContext context, Stream istrm, bool updateTables, List<string> namespaceUris, NodeStateCollection predefinedNodes)
        {
            UANodeSet nodeSet = UANodeSet.Read(istrm);

            if (nodeSet != null)
            {
                // Update namespace table
                if (updateTables)
                {
                    if (nodeSet.NamespaceUris != null && context.NamespaceUris != null)
                    {
                        for (int ii = 0; ii < nodeSet.NamespaceUris.Length; ii++)
                        {
                            context.NamespaceUris.GetIndexOrAppend(nodeSet.NamespaceUris[ii]);
                            namespaceUris.Add(nodeSet.NamespaceUris[ii]);
                        }
                    }
                }

                // Update server table
                if (updateTables)
                {
                    if (nodeSet.ServerUris != null && context.ServerUris != null)
                    {
                        for (int ii = 0; ii < nodeSet.ServerUris.Length; ii++)
                        {
                            context.ServerUris.GetIndexOrAppend(nodeSet.ServerUris[ii]);
                        }
                    }
                }

                // Load nodes
                nodeSet.Import(context, predefinedNodes);

                return nodeSet.Extensions;
            }

            return null;
        }

        /// <summary>
        /// Updates the registration of the node manager in case of nodeset2.xml import
        /// </summary>
        /// <param name="nodeManager">The node manager that performed the import.</param>
        /// <param name="newNamespaceUris">The new namespace Uris that were imported.</param>
        private void UpdateRegistration(INodeManager nodeManager, List<string> newNamespaceUris)
        {
            if (nodeManager == null || newNamespaceUris == null)
            {
                return;
            }

            int index = -1;
            int arrayLength = 0;
            foreach (var namespaceUri in newNamespaceUris)
            {
                index = Server.NamespaceUris.GetIndex(namespaceUri);
                if (index == -1)
                {
                    // Something bad happened
                    Utils.Trace(Utils.TraceMasks.Error, "Nodeset2xmlNodeManager.UpdateRegistration", "Namespace uri: " + namespaceUri + " was not found in the server's namespace table.");

                    continue;
                }

                // m_namespaceManagers is declared Private in MasterNodeManager, therefore we must use Reflection to access it
                FieldInfo fieldInfo = Server.NodeManager.GetType().GetField("m_namespaceManagers", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);

                if (fieldInfo != null)
                {
                    var namespaceManagers = fieldInfo.GetValue(Server.NodeManager) as INodeManager[][];

                    if (namespaceManagers != null)
                    {
                        if (index <= namespaceManagers.Length - 1)
                        {
                            arrayLength = namespaceManagers[index].Length;
                            Array.Resize(ref namespaceManagers[index], arrayLength + 1);
                            namespaceManagers[index][arrayLength] = nodeManager;
                        }
                        else
                        {
                            Array.Resize(ref namespaceManagers, namespaceManagers.Length + 1);
                            namespaceManagers[namespaceManagers.Length - 1] = new INodeManager[] { nodeManager };
                        }

                        fieldInfo.SetValue(Server.NodeManager, namespaceManagers);
                    }
                }
            }
        }
        #endregion

        #region Private Members
        private string m_InitialModelFilePath = @"NodeSetImport\Refrigerators.NodeSet2.xml";
        private string m_SecondaryModelFilePath = @"NodeSetImport\Refrigerators2.NodeSet2.xml";
        #endregion
    }
}