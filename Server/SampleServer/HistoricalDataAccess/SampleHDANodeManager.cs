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
using System.IO;
using System.Threading;
using Opc.Ua.Server;
using Opc.Ua;

namespace SampleServer.HistoricalDataAccess
{
    /// <summary>
    /// Saple implementation of a histtorical data access node manager
    /// </summary>
    class SampleHDANodeManager : HistoricalDataAccessNodeManager
    {
        #region Private Members
        private Timer m_simulationTimer;
        readonly List<ArchiveItemState> m_simulatedNodes;
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of SampleHDANodeManager
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        public SampleHDANodeManager(IServerInternal server, ApplicationConfiguration configuration): base(server, configuration, Namespaces.HistoricalDataAccess)
        {
            m_simulatedNodes = new List<ArchiveItemState>();
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Create address space for current node manager
        /// Invoked during the initialisation of the address space.
        /// </summary>
        /// <param name="externalReferences"></param>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            base.CreateAddressSpace(externalReferences);

            try
            {
                // Create a root node and add a reference to external Server Objects Folder
                FolderState root = CreateFolder(null, "HistoricalDataAccess");
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);                

                // Historical Access
                FolderState dynamicHistoricals = CreateFolder(root, "DynamicHistoricalDataItems");
              //  AddReference(dynamicHistoricals, ReferenceTypeIds.Organizes, true, dynamicHistoricals.NodeId, true);

                CreateDynamicHistoricalVariables(dynamicHistoricals);

                FolderState staticHistoricals = CreateFolder(root, "StaticHistoricalDataItems");
               // AddReference(root, ReferenceTypeIds.Organizes, true, staticHistoricals.NodeId, true);

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
        /// <summary>
        /// Create a set of statc historical variables and add them to the porovided root node
        /// </summary>
        /// <param name="root"></param>
        private void CreateStaticHistoricalVariables(BaseObjectState root)
        {
            // Historian nodes
            ArchiveItem itemHistorian1 = new ArchiveItem("StaticHistoricalDataItem_Historian1", new FileInfo(Path.Combine("HistoricalDataAccess","Data","Sample","Historian1.txt")));
            ArchiveItemState nodeHistorian1 = new ArchiveItemState(SystemContext, itemHistorian1, NamespaceIndex);
            nodeHistorian1.ReloadFromSource(SystemContext);   
            AddPredefinedNode(SystemContext, nodeHistorian1);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorian1.NodeId, true);

            ArchiveItem itemHistorian2 = new ArchiveItem("StaticHistoricalDataItem_Historian2", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "Historian2.txt")));
            ArchiveItemState nodeHistorian2 = new ArchiveItemState(SystemContext, itemHistorian2, NamespaceIndex);
            nodeHistorian2.ReloadFromSource(SystemContext);            
            AddPredefinedNode(SystemContext, nodeHistorian2);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorian2.NodeId, true);

            ArchiveItem itemHistorian3 = new ArchiveItem("StaticHistoricalDataItem_Historian3", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "Historian3.txt")));
            ArchiveItemState nodeHistorian3 = new ArchiveItemState(SystemContext, itemHistorian3, NamespaceIndex);
            nodeHistorian3.ReloadFromSource(SystemContext);
            AddPredefinedNode(SystemContext, nodeHistorian3);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorian3.NodeId, true);

            ArchiveItem itemHistorian4 = new ArchiveItem("StaticHistoricalDataItem_Historian4", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "Historian4.txt")));
            ArchiveItemState nodeHistorian4 = new ArchiveItemState(SystemContext, itemHistorian4, NamespaceIndex);
            nodeHistorian4.ReloadFromSource(SystemContext);
            AddPredefinedNode(SystemContext, nodeHistorian4);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorian4.NodeId, true);
        }

        /// <summary>
        /// Create a set of dynamic histyorical variables and add them to the provided root node
        /// </summary>
        /// <param name="root"></param>
        private void CreateDynamicHistoricalVariables(BaseObjectState root)
        {
            // Historian nodes
            ArchiveItem itemDouble = new ArchiveItem("DynamicHistoricalDataItem_Double", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Dynamic", "Double.txt")));
            ArchiveItemState nodeDouble = new ArchiveItemState(SystemContext, itemDouble, NamespaceIndex);
            nodeDouble.ReloadFromSource(SystemContext);   
            AddPredefinedNode(SystemContext, nodeDouble);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeDouble.NodeId, true);

            m_simulatedNodes.Add(nodeDouble);

            ArchiveItem itemInt32 = new ArchiveItem("DynamicHistoricalDataItem_Int32", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Dynamic", "Int32.txt")));
            ArchiveItemState nodeInt32 = new ArchiveItemState(SystemContext, itemInt32, NamespaceIndex);
            nodeInt32.ReloadFromSource(SystemContext);     
            AddPredefinedNode(SystemContext, nodeInt32);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeInt32.NodeId, true);

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