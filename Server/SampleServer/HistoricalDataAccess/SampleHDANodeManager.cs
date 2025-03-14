/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
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
    /// Sample implementation of a historical data access node manager
    /// </summary>
    class SampleHDANodeManager : HistoricalDataAccessNodeManager
    {
        #region Private Members
        private Timer m_simulationTimer;
        readonly List<ArchiveItemState> m_simulatedNodes;
        private BaseEventState m_historicalDataItemDoubleEvent;
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
        /// Invoked during the initialization of the address space.
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
                // AddReference(dynamicHistoricals, ReferenceTypeIds.Organizes, true, dynamicHistoricals.NodeId, true);

                CreateDynamicHistoricalVariables(dynamicHistoricals);

                FolderState staticHistoricals = CreateFolder(root, "StaticHistoricalDataItems");
                // AddReference(root, ReferenceTypeIds.Organizes, true, staticHistoricals.NodeId, true);

                CreateStaticHistoricalVariables(staticHistoricals);

                StartSimulation();
            }
            catch(Exception ex)
            {
                Utils.Trace(ex, "HistoricalAccess.HistoricalDataAccess.SampleHDANodeManager.CreateAddressSpace");
                throw;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Create a set of static historical variables and add them to the provided root node
        /// </summary>
        /// <param name="root"></param>
        private void CreateStaticHistoricalVariables(BaseObjectState root)
        {
            // Historian nodes
            ArchiveItem itemHistorian1 = new ArchiveItem("StaticHistoricalDataItem_Historian1", new FileInfo(Path.Combine("HistoricalDataAccess","Data","Sample","Historian1.txt")));
            ArchiveItemState nodeHistorian1 = new ArchiveItemState(SystemContext, itemHistorian1, NamespaceIndex);
            nodeHistorian1.ReloadFromSource(SystemContext);
            nodeHistorian1.Value = GetDefaultValueForDatatype(nodeHistorian1.DataType);
            AddPredefinedNode(SystemContext, nodeHistorian1);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorian1.NodeId, true);

            AddReference(nodeHistorian1.HistoricalDataConfiguration.AggregateFunctions, ReferenceTypeIds.HasComponent, false,
                ObjectIds.AggregateFunction_Count, false);

            AddReference(nodeHistorian1.HistoricalDataConfiguration.AggregateFunctions, ReferenceTypeIds.HasComponent, false,
                ObjectIds.AggregateFunction_Interpolative, false);

            ArchiveItem itemHistorian2 = new ArchiveItem("StaticHistoricalDataItem_Historian2", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "Historian2.txt")));
            ArchiveItemState nodeHistorian2 = new ArchiveItemState(SystemContext, itemHistorian2, NamespaceIndex);
            nodeHistorian2.ReloadFromSource(SystemContext);
            nodeHistorian2.Value = GetDefaultValueForDatatype(nodeHistorian2.DataType);
            AddPredefinedNode(SystemContext, nodeHistorian2);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorian2.NodeId, true);

            ArchiveItem itemHistorian3 = new ArchiveItem("StaticHistoricalDataItem_Historian3", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "Historian3.txt")));
            ArchiveItemState nodeHistorian3 = new ArchiveItemState(SystemContext, itemHistorian3, NamespaceIndex);
            nodeHistorian3.ReloadFromSource(SystemContext);
            nodeHistorian3.Value = GetDefaultValueForDatatype(nodeHistorian3.DataType);
            AddPredefinedNode(SystemContext, nodeHistorian3);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorian3.NodeId, true);

            ArchiveItem itemHistorian4 = new ArchiveItem("StaticHistoricalDataItem_Historian4", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "Historian4.txt")));
            ArchiveItemState nodeHistorian4 = new ArchiveItemState(SystemContext, itemHistorian4, NamespaceIndex);
            nodeHistorian4.ReloadFromSource(SystemContext);
            nodeHistorian4.Value = GetDefaultValueForDatatype(nodeHistorian4.DataType);
            AddPredefinedNode(SystemContext, nodeHistorian4);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorian4.NodeId, true);

            // boolean nodes
            ArchiveItem itemHistorianBool1 = new ArchiveItem("StaticHistoricalDataItem_HistorianBool1", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "HistorianBoolean1.txt")));
            
            ArchiveItemState nodeHistorianBool1 = new ArchiveItemState(SystemContext, itemHistorianBool1, NamespaceIndex);
            nodeHistorianBool1.ReloadFromSource(SystemContext);
            nodeHistorianBool1.Value = GetDefaultValueForDatatype(nodeHistorianBool1.DataType);
            AddPredefinedNode(SystemContext, nodeHistorianBool1);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorianBool1.NodeId, true);

            ArchiveItem itemHistorianBool2 = new ArchiveItem("StaticHistoricalDataItem_HistorianBool2", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "HistorianBoolean2.txt")));

            ArchiveItemState nodeHistorianBool2 = new ArchiveItemState(SystemContext, itemHistorianBool2, NamespaceIndex);
            nodeHistorianBool2.ReloadFromSource(SystemContext);
            nodeHistorianBool2.Value = GetDefaultValueForDatatype(nodeHistorianBool1.DataType);
            AddPredefinedNode(SystemContext, nodeHistorianBool2);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorianBool2.NodeId, true);

            // integer nodes
            ArchiveItem itemHistorianInt1 = new ArchiveItem("StaticHistoricalDataItem_HistorianInt1", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "HistorianInteger1.txt")));

            ArchiveItemState nodeHistorianInt1 = new ArchiveItemState(SystemContext, itemHistorianInt1, NamespaceIndex);
            nodeHistorianInt1.ReloadFromSource(SystemContext);
            nodeHistorianInt1.Value = GetDefaultValueForDatatype(nodeHistorianInt1.DataType);
            AddPredefinedNode(SystemContext, nodeHistorianInt1);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorianInt1.NodeId, true);

            ArchiveItem itemHistorianInt2 = new ArchiveItem("StaticHistoricalDataItem_HistorianInt2", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "HistorianInteger2.txt")));

            ArchiveItemState nodeHistorianInt2 = new ArchiveItemState(SystemContext, itemHistorianInt2, NamespaceIndex);
            nodeHistorianInt2.ReloadFromSource(SystemContext);
            nodeHistorianInt2.Value = GetDefaultValueForDatatype(nodeHistorianInt2.DataType);
            AddPredefinedNode(SystemContext, nodeHistorianInt2);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorianInt2.NodeId, true);

            // string nodes
            ArchiveItem itemHistorianString1 = new ArchiveItem("StaticHistoricalDataItem_HistorianString1", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "HistorianString1.txt")));

            ArchiveItemState nodeHistorianString1 = new ArchiveItemState(SystemContext, itemHistorianString1, NamespaceIndex);
            nodeHistorianString1.ReloadFromSource(SystemContext);
            nodeHistorianString1.Value = GetDefaultValueForDatatype(nodeHistorianString1.DataType);
            AddPredefinedNode(SystemContext, nodeHistorianString1);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorianString1.NodeId, true);

            ArchiveItem itemHistorianString2 = new ArchiveItem("StaticHistoricalDataItem_HistorianString2", new FileInfo(Path.Combine("HistoricalDataAccess", "Data", "Sample", "HistorianString2.txt")));

            ArchiveItemState nodeHistorianString2 = new ArchiveItemState(SystemContext, itemHistorianString2, NamespaceIndex);
            nodeHistorianString2.ReloadFromSource(SystemContext);
            nodeHistorianString2.Value = GetDefaultValueForDatatype(nodeHistorianString2.DataType);
            AddPredefinedNode(SystemContext, nodeHistorianString2);
            AddReference(root, ReferenceTypeIds.Organizes, false, nodeHistorianString2.NodeId, true);
        }

        /// <summary>
        /// Create a set of dynamic historical variables and add them to the provided root node
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

            try
            {
                root.EventNotifier |= EventNotifiers.SubscribeToEvents;

                // enable event notifications
                root.AddChild(nodeDouble);
            }
            catch
            { }

            // create an instance of BaseEventType to be used when reporting HistoricalDataItemDouble event
            m_historicalDataItemDoubleEvent = CreateObjectFromType(nodeDouble, "HistoricalDataItemDoubleEvent", ObjectTypeIds.BaseEventType) as BaseEventState;

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

                    // Report an event at on DataAccess node
                    string eventMessage = String.Format("Dynamic Historical Double data changed to {0}", m_simulatedNodes[0].Value);
                    ReportEvent(m_simulatedNodes[0], m_historicalDataItemDoubleEvent, new LocalizedText(eventMessage), EventSeverity.Medium);
                }
            }
            catch(Exception ex)
            {
                Utils.Trace(ex, 
                    "HistoricalAccess.HistoricalDataAccess.SampleHDANodeManager.DoSimulation: Unexpected error during simulation");
            }
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// /// <summary>
        /// An overrideable version of the Dispose
        /// </summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Utils.SilentDispose(m_simulationTimer);
                m_simulationTimer = null;
            }

            base.Dispose(disposing);
        }
        #endregion
    }
}