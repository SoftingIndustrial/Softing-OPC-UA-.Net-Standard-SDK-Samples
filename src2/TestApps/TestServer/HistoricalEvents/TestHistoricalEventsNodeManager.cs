/* ========================================================================
 * Copyright © 2011-2017 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * The Software is based on the OPC Foundation, Inc.’s software. This 
 * original OPC Foundation’s software can be found here:
 * http://www.opcfoundation.org
 * 
 * The original OPC Foundation’s software is subject to the OPC Foundation
 * MIT License 1.00, which can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * 
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace TestServer.HistoricalEvents
{
	class TestHistoricalEventsNodeManager : HistoricalEventsNodeManager
	{
		public TestHistoricalEventsNodeManager(IServerInternal server, ApplicationConfiguration configuration)
			: base(server, configuration, HistoricalEvents.Namespaces.HistoricalEvents)
		{
		}

		#region Overridden Methods
		/// <summary>
		/// Loads a node set from a file or resource and addes them to the set of predefined nodes.
		/// </summary>
		protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
		{
			NodeStateCollection predefinedNodes = new NodeStateCollection();

			BaseObjectTypeState wellTestReportState = new BaseObjectTypeState();
			wellTestReportState.NodeId = new NodeId(ObjectTypes.WellTestReportType, NamespaceIndex);
			wellTestReportState.SuperTypeId = Opc.Ua.ObjectTypeIds.BaseEventType;
			wellTestReportState.BrowseName = BrowseNames.WellTestReportType;
            wellTestReportState.DisplayName = BrowseNames.WellTestReportType;

			BaseObjectTypeState fluidLevelTestReportState = new BaseObjectTypeState();
			fluidLevelTestReportState.NodeId = new NodeId(ObjectTypes.FluidLevelTestReportType, NamespaceIndex);
			fluidLevelTestReportState.SuperTypeId = wellTestReportState.NodeId;
			fluidLevelTestReportState.BrowseName = BrowseNames.FluidLevelTestReportType;
            fluidLevelTestReportState.DisplayName = BrowseNames.FluidLevelTestReportType;

			BaseObjectTypeState injectionTestReportState = new BaseObjectTypeState();
			injectionTestReportState.NodeId = new NodeId(ObjectTypes.InjectionTestReportType, NamespaceIndex);
			injectionTestReportState.SuperTypeId = wellTestReportState.NodeId;
			injectionTestReportState.BrowseName = BrowseNames.InjectionTestReportType;
            injectionTestReportState.DisplayName = BrowseNames.InjectionTestReportType;

			predefinedNodes.Add(wellTestReportState);
			predefinedNodes.Add(fluidLevelTestReportState);
			predefinedNodes.Add(injectionTestReportState);

			return predefinedNodes;
		}

		public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
		{
			base.CreateAddressSpace(externalReferences);

			try
			{
				FolderState root = new FolderState(null);
				root.NodeId = new NodeId("HistoricalEvents", NamespaceIndex);
				root.BrowseName = new QualifiedName("HistoricalEvents", NamespaceIndex);
				root.DisplayName = root.BrowseName.Name;
				root.TypeDefinitionId = Opc.Ua.ObjectTypeIds.FolderType;

				NodeId rootNodeId = new NodeId("TestModule", (ushort) Server.NamespaceUris.GetIndex(global::TestServer.Namespaces.TestServer));

				root.AddReference(ReferenceTypeIds.Organizes, true, rootNodeId);

				IList<IReference> references = null;
				if (!externalReferences.TryGetValue(rootNodeId, out references))
				{
					externalReferences[rootNodeId] = references = new List<IReference>();
				}

				references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, root.NodeId));


				FolderState area51 = CreateArea(SystemContext, root, "Area51");
				FolderState area99 = CreateArea(SystemContext, root, "Area99");

				CreateWell(SystemContext, area51, "Well_24412", "Jupiter");
				CreateWell(SystemContext, area51, "Well_48306", "Titan");
				CreateWell(SystemContext, area99, "Well_91423", "Mars");
				CreateWell(SystemContext, area99, "Well_86234", "Saturn");

                
				AddPredefinedNode(SystemContext, root);
			   

                //LoadEvents();
            }
			catch(Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}
		#endregion

		/// <summary>
		/// Creates a new area.
		/// </summary>
		private FolderState CreateArea(SystemContext context, BaseObjectState root, string areaName)
		{
			FolderState area = new FolderState(root);

			area.NodeId = new NodeId(areaName, NamespaceIndex);
			area.BrowseName = new QualifiedName(areaName, NamespaceIndex);
			area.DisplayName = area.BrowseName.Name;
			area.EventNotifier = EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead | EventNotifiers.HistoryWrite;
			area.TypeDefinitionId = Opc.Ua.ObjectTypeIds.FolderType;

			root.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, false, area);
			area.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, true, root);

			AddPredefinedNode(SystemContext, area);

			return area;
		}

		/// <summary>
		/// Creates a new well.
		/// </summary>
		private void CreateWell(SystemContext context, BaseObjectState area, string wellId, string wellName)
		{
			WellState well = new WellState(area);

			well.NodeId = new NodeId(wellId, NamespaceIndex);
			well.BrowseName = new QualifiedName(wellName, NamespaceIndex);
			well.DisplayName = wellName;
			well.EventNotifier = EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead | EventNotifiers.HistoryWrite;
			well.TypeDefinitionId = new NodeId(ObjectTypes.WellType, NamespaceIndex);

			area.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, false, well);
			well.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, true, area);

			AddPredefinedNode(SystemContext, well);
		}

		// Initialize dataset with tables for two types of events
		private void LoadEvents()
		{
			m_dataset = new DataSet();

			m_dataset.Tables.Add("FluidLevelTests");
			m_dataset.Tables[0].Columns.Add(Opc.Ua.BrowseNames.EventId, typeof(string));
			m_dataset.Tables[0].Columns.Add(Opc.Ua.BrowseNames.Time, typeof(DateTime));
			m_dataset.Tables[0].Columns.Add(BrowseNames.NameWell, typeof(string));
			m_dataset.Tables[0].Columns.Add(BrowseNames.UidWell, typeof(string));
			m_dataset.Tables[0].Columns.Add(BrowseNames.TestDate, typeof(string));
			m_dataset.Tables[0].Columns.Add(BrowseNames.TestReason, typeof(string));
			m_dataset.Tables[0].Columns.Add(BrowseNames.FluidLevel, typeof(double));
			m_dataset.Tables[0].Columns.Add(Opc.Ua.BrowseNames.EngineeringUnits, typeof(string));
			m_dataset.Tables[0].Columns.Add(BrowseNames.TestedBy, typeof(string));

			m_dataset.Tables.Add("InjectionTests");
			m_dataset.Tables[1].Columns.Add(Opc.Ua.BrowseNames.EventId, typeof(string));
			m_dataset.Tables[1].Columns.Add(Opc.Ua.BrowseNames.Time, typeof(DateTime));
			m_dataset.Tables[1].Columns.Add(BrowseNames.NameWell, typeof(string));
			m_dataset.Tables[1].Columns.Add(BrowseNames.UidWell, typeof(string));
			m_dataset.Tables[1].Columns.Add(BrowseNames.TestDate, typeof(string));
			m_dataset.Tables[1].Columns.Add(BrowseNames.TestReason, typeof(string));
			m_dataset.Tables[1].Columns.Add(BrowseNames.TestDuration, typeof(double));
			m_dataset.Tables[1].Columns.Add(Opc.Ua.BrowseNames.EngineeringUnits, typeof(string));
			m_dataset.Tables[1].Columns.Add(BrowseNames.InjectedFluid, typeof(string));

			//load events
			try
			{
				using(StreamReader sr = new StreamReader(@"HistoryData\Events.txt"))
				{
					bool readingFluidEvents = true;
					string line;
					char[] separators = new char[] { '\t' };

					while((line = sr.ReadLine()) != null)
					{
						if (line == "[FluidLevel]")
							readingFluidEvents = true;
						else if (line == "[Injection]")
							readingFluidEvents = false;
						else
						{
							string[] columns = line.Split(separators);
							DataRow row;
							int count;

							if (readingFluidEvents)
							{
								row = m_dataset.Tables[0].NewRow();
								count = m_dataset.Tables[0].Columns.Count;
							}
							else
							{
								row = m_dataset.Tables[1].NewRow();
								count = m_dataset.Tables[1].Columns.Count;
							}

							for(int i = 0; i < count; i++)
							{
								row[i] = columns[i];
							}

							if (readingFluidEvents)
							{
								m_dataset.Tables[0].Rows.Add(row);
							}
							else
							{
								m_dataset.Tables[1].Rows.Add(row);
							}
						}
					}

					m_dataset.AcceptChanges();
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}
	}
}
