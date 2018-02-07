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
    class SampleHDANodeManager : HistoricalDataAccessNodeManager
    {
        #region Private Members
        private Timer m_simulationTimer;
        readonly List<ArchiveItemState> m_simulatedNodes;
        #endregion

        #region Constructor
        public SampleHDANodeManager(IServerInternal server, ApplicationConfiguration configuration): base(server, configuration, Namespaces.HistoricalDataAccess)
        {
            m_simulatedNodes = new List<ArchiveItemState>();
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
                if(!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
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

                CreateDynamicHistoricalVariables(dynamicHistoricals);

                FolderState staticHistoricals = new FolderState(null);
                staticHistoricals.NodeId = new NodeId("StaticHistoricalDataItems", NamespaceIndex);
                staticHistoricals.BrowseName = new QualifiedName("StaticHistoricalDataItems", NamespaceIndex);
                staticHistoricals.DisplayName = staticHistoricals.BrowseName.Name;
                staticHistoricals.TypeDefinitionId = ObjectTypeIds.FolderType;
                root.AddReference(ReferenceTypeIds.Organizes, false, staticHistoricals.NodeId);
                staticHistoricals.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);
                AddPredefinedNode(SystemContext, staticHistoricals);

                CreateStaticHistoricalVariables(staticHistoricals);

                StartSimulation();
            }
            catch(Exception ex)
            {
                Utils.Trace(Utils.TraceMasks.Error, "HistoricalAccess.HistoricalDataAccess.SampleHDANodeManager.CreateAddressSpace", ex);
                throw;
            }
        }
        #endregion

        #region Private Methods
        private void CreateStaticHistoricalVariables(BaseObjectState root)
        {
            // Historian nodes
            ArchiveItem itemHistorian1 = new ArchiveItem("StaticHistoricalDataItem_Historian1", new FileInfo(Path.Combine("HistoricalDataAccess","Data","Sample","Historian1.txt")));
            ArchiveItemState nodeHistorian1 = new ArchiveItemState(SystemContext, itemHistorian1, NamespaceIndex);
            nodeHistorian1.ReloadFromSource(SystemContext);

            root.AddReference(ReferenceTypeIds.Organizes, false, nodeHistorian1.NodeId);
            nodeHistorian1.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

            AddPredefinedNode(SystemContext, nodeHistorian1);


            ArchiveItem itemHistorian2 = new ArchiveItem("StaticHistoricalDataItem_Historian2", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "Historian2.txt")));
            ArchiveItemState nodeHistorian2 = new ArchiveItemState(SystemContext, itemHistorian2, NamespaceIndex);
            nodeHistorian2.ReloadFromSource(SystemContext);

            root.AddReference(ReferenceTypeIds.Organizes, false, nodeHistorian2.NodeId);
            nodeHistorian2.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

            AddPredefinedNode(SystemContext, nodeHistorian2);


            ArchiveItem itemHistorian3 = new ArchiveItem("StaticHistoricalDataItem_Historian3", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "Historian3.txt")));
            ArchiveItemState nodeHistorian3 = new ArchiveItemState(SystemContext, itemHistorian3, NamespaceIndex);
            nodeHistorian3.ReloadFromSource(SystemContext);

            root.AddReference(ReferenceTypeIds.Organizes, false, nodeHistorian3.NodeId);
            nodeHistorian3.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

            AddPredefinedNode(SystemContext, nodeHistorian3);


            ArchiveItem itemHistorian4 = new ArchiveItem("StaticHistoricalDataItem_Historian4", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "Historian4.txt")));
            ArchiveItemState nodeHistorian4 = new ArchiveItemState(SystemContext, itemHistorian4, NamespaceIndex);
            nodeHistorian4.ReloadFromSource(SystemContext);

            root.AddReference(ReferenceTypeIds.Organizes, false, nodeHistorian4.NodeId);
            nodeHistorian4.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

            AddPredefinedNode(SystemContext, nodeHistorian4);
        }

        private void CreateDynamicHistoricalVariables(BaseObjectState root)
        {
            // Historian nodes
            ArchiveItem itemDouble = new ArchiveItem("StaticHistoricalDataItem_Double", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Dynamic", "Double.txt")));
            ArchiveItemState nodeDouble = new ArchiveItemState(SystemContext, itemDouble, NamespaceIndex);
            nodeDouble.ReloadFromSource(SystemContext);

            root.AddReference(ReferenceTypeIds.Organizes, false, nodeDouble.NodeId);
            nodeDouble.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

            AddPredefinedNode(SystemContext, nodeDouble);
            m_simulatedNodes.Add(nodeDouble);

            ArchiveItem itemInt32 = new ArchiveItem("StaticHistoricalDataItem_Int32", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Dynamic", "Int32.txt")));
            ArchiveItemState nodeInt32 = new ArchiveItemState(SystemContext, itemInt32, NamespaceIndex);
            nodeInt32.ReloadFromSource(SystemContext);

            root.AddReference(ReferenceTypeIds.Organizes, false, nodeInt32.NodeId);
            nodeInt32.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

            AddPredefinedNode(SystemContext, nodeInt32);
            m_simulatedNodes.Add(nodeInt32);
        }

        /// <summary>
        /// Start the simulation
        /// </summary>
        private void StartSimulation()
        {
            int freq = int.MaxValue;

            if (m_simulatedNodes.Count == 0)
            {
                return;
            }

            foreach(ArchiveItemState item in m_simulatedNodes)
            {
                if (freq > item.HistoricalDataConfiguration.MinTimeInterval.Value)
                {
                    freq = (int) item.HistoricalDataConfiguration.MinTimeInterval.Value;
                }
            }

            m_simulationTimer = new Timer(DoSimulation, null, freq, freq);
        }

        /// <summary>
        /// Runs the simulation
        /// </summary>
        private void DoSimulation(object state)
        {
            try
            {
                lock(Lock)
                {
                    foreach(ArchiveItemState item in m_simulatedNodes)
                    {
                        if(item.ArchiveItem.LastLoadTime.AddSeconds(10) < DateTime.UtcNow)
                        {
                            item.LoadConfiguration(SystemContext);
                        }

                        foreach(DataValue value in item.NewSamples(SystemContext))
                        {
                            item.WrappedValue = value.WrappedValue;
                            item.Timestamp = value.SourceTimestamp;
                            item.StatusCode = value.StatusCode;
                            item.ClearChangeMasks(SystemContext, true);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Utils.Trace(Utils.TraceMasks.Error, "HistoricalAccess.HistoricalDataAccess.SampleHDANodeManager.DoSimulation", "Unexpected error during simulation", ex);
            }
        }
        #endregion
    }
}