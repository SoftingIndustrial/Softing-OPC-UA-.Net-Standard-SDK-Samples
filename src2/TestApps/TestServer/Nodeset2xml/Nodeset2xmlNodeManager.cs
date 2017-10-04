using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Opc.Ua.Export;

namespace TestServer
{
    class Nodeset2xmlNodeManager : CustomNodeManager2
    {
        public Nodeset2xmlNodeManager(IServerInternal server, ApplicationConfiguration configuration):
            base(server, configuration, Namespaces.Nodeset2xmlModule)
        { 
            
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                BaseObjectState root = new BaseObjectState(null);

                root.NodeId = new NodeId("ImportNodeset", NamespaceIndex);
                root.BrowseName = new QualifiedName("ImportNodeset", NamespaceIndex);
                root.DisplayName = root.BrowseName.Name;

                // ensure root can be found via the Objects object. 
                IList<IReference> references = null;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                root.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;

                AddPredefinedNode(SystemContext, root);

                #region Create Import method
                MethodState method = new MethodState(root);
                method.NodeId = new NodeId("Import", NamespaceIndex);
                method.BrowseName = new QualifiedName("Import", NamespaceIndex);
                method.DisplayName = method.BrowseName.Name;
                method.Executable = true;
                method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
                root.AddChild(method);

                // create the input arguments.
                PropertyState<Argument[]> inputArguments = new PropertyState<Argument[]>(method);
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
                    new Argument("file path", DataTypeIds.String, ValueRanks.Scalar, null)
                };

                method.InputArguments = inputArguments;

                AddPredefinedNode(SystemContext, method);

                // register handler.
                method.OnCallMethod = new GenericMethodCalledEventHandler(OnImportNodeSet);
                #endregion
            }
        }

        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            // This override will receive a calback every time a new node is added
            // e.g. The extension data can be received in predefinedNode.Extensions
            return predefinedNode;
        }

        private ServiceResult OnImportNodeSet(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            try
            {
                string filePath = (string)inputArguments[0];

                // Import the NodeSet file.
                XmlElement[] extensions = ImportNodeSet(context, filePath);
            }
            catch(Exception ex)
            {
                throw new ServiceResultException(ex, StatusCodes.Bad);
            }

            return ServiceResult.Good;
        }

        private XmlElement[] ImportNodeSet(ISystemContext context, string filePath)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            List<string> newNamespaceUris = new List<string>();

            XmlElement[] extensions = LoadFromNodeSet2Xml(context, filePath, true, newNamespaceUris, predefinedNodes);

            // Add the node set to the node manager.
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

            // Ensure the reverse references exist.
            Dictionary<NodeId, IList<IReference>> externalReferences = new Dictionary<NodeId, IList<IReference>>();
            AddReverseReferences(externalReferences);

            foreach (var item in externalReferences)
            {
                Server.NodeManager.AddReferences(item.Key, item.Value);
            }

            return extensions;
        }

        /// <summary>
        /// Loads the NodeSet2.xml file and returns the Extensions data of the node set.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="updateTables">if set to <c>true</c> the namespace and server tables are updated with any new URIs.</param>
        /// <param name="namespaceUris">Returns the NamespaceUris defined in the node set.</param>
        /// <param name="predefinedNodes">The required NodeStateCollection</param>
        /// <returns>The collection of global extensions of the NodeSet2.xml file.</returns>
        private XmlElement[] LoadFromNodeSet2Xml(ISystemContext context, string filePath, bool updateTables, List<string> namespaceUris, NodeStateCollection predefinedNodes)
        {
            if (filePath == null) throw new ArgumentNullException("filePath");

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
        /// Updates the registration of the node manager in case of nodeset2.xml import.
        /// </summary>
        /// <param name="nodeManager">The node manager that performed the import.</param>
        /// <param name="newNamespaceUris">The new namespace uris that were imported.</param>
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
    }
}