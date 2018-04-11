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
using System.Threading;
using Opc.Ua.Server;
using Opc.Ua;

namespace SampleServer.HistoricalDataAccess
{
    class SampleHDANodeManager : CustomNodeManager2
    {
        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public SampleHDANodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.HistoricalDataAccess)
        {
            SystemContext.NodeIdFactory = this;
        }

        #endregion      

        #region Overridden Methods
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            base.CreateAddressSpace(externalReferences);

            try
            {
                // Create the root folder
                FolderState root = new FolderState(null);

                root.NodeId = new NodeId("HistoricalDataAccess", NamespaceIndex);
                root.BrowseName = new QualifiedName("HistoricalDataAccess", NamespaceIndex);
                root.DisplayName = root.BrowseName.Name;
                root.TypeDefinitionId = ObjectTypeIds.FolderType;

                // Ensure root can be found via the Objects object
                IList<IReference> references = null;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                root.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, root.NodeId));

                // Save the node for later lookup
                AddPredefinedNode(SystemContext, root);

                // Historical Access
                FolderState dynamicHistoricals = new FolderState(null);
                dynamicHistoricals.NodeId = new NodeId("DynamicHistoricalDataItems", NamespaceIndex);
                dynamicHistoricals.BrowseName = new QualifiedName("DynamicHistoricalDataItems", NamespaceIndex);
                dynamicHistoricals.DisplayName = dynamicHistoricals.BrowseName.Name;
                dynamicHistoricals.TypeDefinitionId = ObjectTypeIds.FolderType;
                root.AddReference(ReferenceTypeIds.Organizes, false, dynamicHistoricals.NodeId);
                dynamicHistoricals.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);
                AddPredefinedNode(SystemContext, dynamicHistoricals);
                
                FolderState staticHistoricals = new FolderState(null);
                staticHistoricals.NodeId = new NodeId("StaticHistoricalDataItems", NamespaceIndex);
                staticHistoricals.BrowseName = new QualifiedName("StaticHistoricalDataItems", NamespaceIndex);
                staticHistoricals.DisplayName = staticHistoricals.BrowseName.Name;
                staticHistoricals.TypeDefinitionId = ObjectTypeIds.FolderType;
                root.AddReference(ReferenceTypeIds.Organizes, false, staticHistoricals.NodeId);
                staticHistoricals.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);
                AddPredefinedNode(SystemContext, staticHistoricals);
                
            }
            catch (Exception ex)
            {
                Utils.Trace(Utils.TraceMasks.Error, "HistoricalAccess.HistoricalDataAccess.SampleHDANodeManager.CreateAddressSpace", ex);
                throw;
            }
        }
        #endregion
    }
}