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
using Opc.Ua;
using Opc.Ua.Server;

namespace SampleServer.FileSystem
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class FileSystemNodeManager : CustomNodeManager2
    {
        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public FileSystemNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.FileSystem)
        {
        }

        #endregion

        #region INodeManager Members

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <param name="externalReferences"></param>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            base.CreateAddressSpace(externalReferences);
            lock(Lock)
            {
                FolderState root = new FolderState(null);

                DriveInfo drive = new DriveInfo(m_driveName);
                ParsedNodeId nodeid = new ParsedNodeId();
                nodeid.RootId = drive.Name;
                nodeid.RootType = 0;
                nodeid.NamespaceIndex = NamespaceIndex;

                root.NodeId = nodeid.Construct();
                root.BrowseName = new QualifiedName("FileSystem", NamespaceIndex);
                root.DisplayName = root.BrowseName.Name;
                root.TypeDefinitionId = ObjectTypeIds.FolderType;

                // Ensure root can be found via the server object. 
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                root.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, root.NodeId));
                
                // Save the node for later lookup
                AddPredefinedNode(SystemContext, root);

                // Handle the on populate browser event
                root.OnPopulateBrowser = NodeStatePopulateBrowserHandler;
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

            // Lookup in operation cache
            NodeState target = FindNodeInCache(context, handle, cache);

            if (target != null)
            {
                handle.Node = target;
                handle.Validated = true;
                return handle.Node;
            }

            // Create the node if it was not allready created and stored in the component cache
            target = CreateExternalNode(context, handle);

            if (target == null)
            {
                return null;
            }

            // Put node into operation cache
            if (cache != null)
            {
                cache[handle.NodeId] = target;
            }

            handle.Node = target;
            handle.Validated = true;
            return handle.Node;
        }
        
        /// <summary>
        /// Creates the external node if not found in cache
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="handle">The handle of the node to find/create.</param>
        /// <returns></returns>
        private NodeState CreateExternalNode(ISystemContext context, NodeHandle handle)
        {
            string uniqueId = handle.RootId.Identifier as string;

            if (String.IsNullOrEmpty(uniqueId))
            {
                return null;
            }

            // Lookup in persistent cache
            NodeState target = LookupNodeInComponentCache(context, handle);

            if (target != null)
            {
                return target;
            }
            
            ParsedNodeId nodeid = ParsedNodeId.Parse(handle.NodeId);

            if (nodeid.RootType == 0)
            {
                // Create folder node
                FolderState folderTarget = new FolderState(null);
                folderTarget.NodeId = handle.NodeId;
                DirectoryInfo dir = new DirectoryInfo(nodeid.RootId);
                folderTarget.BrowseName = new QualifiedName(dir.Name, NamespaceIndex);
                folderTarget.DisplayName = folderTarget.BrowseName.Name;
                folderTarget.TypeDefinitionId = ObjectTypeIds.FolderType;

                folderTarget.OnPopulateBrowser = NodeStatePopulateBrowserHandler;

                target = folderTarget;
            }
            else if (nodeid.RootType == 1)
            {
                // Create file node
                BaseObjectState fileTarget = new BaseObjectState(null);
                fileTarget.NodeId = handle.NodeId;

                DirectoryInfo dir = new DirectoryInfo(nodeid.RootId);

                fileTarget.BrowseName = new QualifiedName(dir.Name, NamespaceIndex);
                fileTarget.DisplayName = fileTarget.BrowseName.Name;
                fileTarget.TypeDefinitionId = ObjectTypeIds.BaseObjectType;
                fileTarget.OnPopulateBrowser = new NodeStatePopulateBrowserEventHandler(NodeStatePopulateBrowserHandler);
                target = fileTarget;
            }
            else
            {
                // Create FileSize property node; this node is referenced only by file type nodes
                PropertyState<int> property = new PropertyState<int>(null);

                property.NodeId = handle.NodeId;
                property.BrowseName = new QualifiedName("FileSize", NamespaceIndex);
                property.DisplayName = property.BrowseName.Name;
                property.TypeDefinitionId = VariableTypeIds.PropertyType;
                property.DataType = DataTypeIds.Int32;
                FileInfo file = new FileInfo(nodeid.RootId);
                property.Value = (int)file.Length;
                property.ValueRank = ValueRanks.Scalar;
                property.MinimumSamplingInterval = MinimumSamplingIntervals.Continuous;
                property.AccessLevel = AccessLevels.CurrentRead;
                property.UserAccessLevel = AccessLevels.CurrentRead;
                property.Historizing = false;
                property.ReferenceTypeId = ReferenceTypeIds.HasProperty;

                target = property;
            }

            if (target != null)
            {
                AddNodeToComponentCache(context, handle, target);
            }

            return target;
        }

        /// <summary>
        /// Returns a unique handle for the node
        /// </summary>
        /// <param name="context">The system context.</param>
        /// <param name="nodeId">The node id.</param>
        /// <param name="cache">The operation cache.</param>
        /// <returns></returns>
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

                // Check cache (the cache is used because the same node id can appear many times in a single request)
                if (cache != null)
                {
                    if (cache.TryGetValue(nodeId, out node))
                    {
                        return new NodeHandle(nodeId, node);
                    }
                }

                // Look up predefined node
                if (PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    NodeHandle handle = new NodeHandle(nodeId, node);

                    if (cache != null)
                    {
                        cache.Add(nodeId, node);
                    }

                    return handle;
                }

                // Parse the node id and return an unvalidated handle
                if (nodeId.IdType == IdType.String)
                {
                    return ParseNodeId(nodeId);
                }

                // Node not found
                return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the populate browser event
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="node">The current browsed node.</param>
        /// <param name="browser">The node browser entity.</param>
        private void NodeStatePopulateBrowserHandler(ISystemContext context, NodeState node, NodeBrowser browser)
        {
            ParsedNodeId nodeid = ParsedNodeId.Parse(node.NodeId);
            if (nodeid.RootType == 0)
            {
                DirectoryInfo di = new DirectoryInfo(nodeid.RootId);
                DirectoryInfo[] dirs = null;
                try
                {
                    dirs = di.GetDirectories();
                }
                catch (Exception e)
                {
                    Utils.Trace(Utils.TraceMasks.Error, "FileSystemNodeManager.NodeStatePopulateBrowserHandle", string.Format("Access to the path {0} is denied!", nodeid.RootId), e);
                    return;
                }
                foreach (DirectoryInfo dir in dirs)
                {
                    if (!string.IsNullOrEmpty(dir.FullName))
                    {
                        ParsedNodeId nodeIdDir = new ParsedNodeId();
                        nodeIdDir.RootId = dir.FullName;
                        nodeIdDir.RootType = 0;
                        nodeIdDir.NamespaceIndex = NamespaceIndex;

                        if (browser.IsRequired(ReferenceTypeIds.Organizes, false))
                        {
                            browser.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, nodeIdDir.Construct()));
                        }
                    }
                }

                FileInfo[] files = di.GetFiles();

                foreach (FileInfo file in files)
                {
                    ParsedNodeId nodeIdFile = new ParsedNodeId();
                    nodeIdFile.RootId = file.FullName;
                    nodeIdFile.RootType = 1;
                    nodeIdFile.NamespaceIndex = NamespaceIndex;
                    if (browser.IsRequired(ReferenceTypeIds.Organizes, false))
                    {
                        browser.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, nodeIdFile.Construct()));
                    }
                }
            }
            else
            {
                ParsedNodeId nodeIdProperty = new ParsedNodeId();
                nodeIdProperty.RootId = nodeid.RootId;
                nodeIdProperty.RootType = 2;
                nodeIdProperty.NamespaceIndex = NamespaceIndex;
                if (browser.IsRequired(ReferenceTypeIds.HasProperty, false))
                {
                    browser.Add(new NodeStateReference(ReferenceTypeIds.HasProperty, false, nodeIdProperty.Construct()));
                }
            }
        }

        /// <summary>
        /// Parses the node id for an external node. Creates an unvalidated handle.
        /// </summary>
        private NodeHandle ParseNodeId(NodeId nodeId)
        {
            if (NodeId.IsNull(nodeId))
            {
                return null;
            }

            string id = nodeId.Identifier as string;

            if (String.IsNullOrEmpty(id))
            {
                return null;
            }

            // Create an unvalidated handle
            NodeHandle handle = new NodeHandle();
            handle.NodeId = nodeId;
            handle.Validated = false;
            handle.RootId = new NodeId(id, NamespaceIndex);

            return handle;
        }

        #endregion

        #region Private Fields

        private string m_driveName = "C"; // Holds the drive name to browse

        #endregion
    }
}
