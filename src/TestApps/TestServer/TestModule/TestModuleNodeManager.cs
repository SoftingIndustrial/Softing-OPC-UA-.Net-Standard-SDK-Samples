using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using Opc.Ua;
using Opc.Ua.Server;
using TestServer.EventingModule;
using TestServer.SimulationModule;

namespace TestServer.TestModule
{
	partial class TestModuleNodeManager : CustomNodeManager2
	{
		DynamicVariableChange m_dynamicVariableChangeAction;
		HashSet<ushort> m_simulationNamespaceIndexes;
        const uint m_viewId = 555;

		#region Constructors
		/// <summary>
		/// Initializes the node manager.
		/// </summary>
		public TestModuleNodeManager(IServerInternal server, ApplicationConfiguration configuration) :
			base(server, configuration, Namespaces.TestServer, Namespaces.TestModule, "urn:localhost:softing.com:TestServer")
		{
			m_dynamicVariableChangeAction = new DynamicVariableChange();
			m_simulationNamespaceIndexes = new HashSet<ushort>();

			SystemContext.NodeIdFactory = this;
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
                base.CreateAddressSpace(externalReferences);

                // create the root folder.
                BaseObjectState root = new BaseObjectState(null);

                root.NodeId = new NodeId("TestModule", NamespaceIndex);
                root.BrowseName = new QualifiedName("TestModule", NamespaceIndex);
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

                // save the node for later lookup.
                AddPredefinedNode(SystemContext, root);
                AuditAddNodesEventState eventToRaise = new AuditAddNodesEventState(null);
                eventToRaise.Initialize(SystemContext, root, EventSeverity.Medium, new LocalizedText("en", "New node added to the address space."));
                eventToRaise.NodesToAdd = new PropertyState<AddNodesItem[]>(eventToRaise);
                
                AddNodesItem addNodesItem = new AddNodesItem();
                addNodesItem.BrowseName = new QualifiedName("node1", NamespaceIndex);
                addNodesItem.RequestedNewNodeId = new ExpandedNodeId("ns="+NamespaceIndex+";s=node1");
                addNodesItem.NodeClass = NodeClass.Method;
                addNodesItem.ParentNodeId = new ExpandedNodeId("i=84");
                List<AddNodesItem> addNodesItems = new List<AddNodesItem>();
                addNodesItems.Add(addNodesItem);

                eventToRaise.NodesToAdd.Value = addNodesItems.ToArray();

                //eventToRaise.NodesToAdd.Value
                ApplicationModule.Instance.TimerThread.AddAction(new ScheduledEventAction<AuditAddNodesEventState>(root, SystemContext, eventToRaise));

                //create Static Folder
                FolderState statics = new FolderState(null);
                statics.NodeId = new NodeId("Static", NamespaceIndex);
                statics.BrowseName = new QualifiedName("Static", NamespaceIndex);
                statics.DisplayName = statics.BrowseName.Name;
                statics.TypeDefinitionId = ObjectTypeIds.FolderType;
                root.AddReference(ReferenceTypeIds.Organizes, false, statics.NodeId);
                statics.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);
                AddPredefinedNode(SystemContext, statics);

                //create AnalogItems Folder
                FolderState analogItems = new FolderState(null);
                analogItems.NodeId = new NodeId("AnalogItems", NamespaceIndex);
                analogItems.BrowseName = new QualifiedName("AnalogItems", NamespaceIndex);
                analogItems.DisplayName = analogItems.BrowseName.Name;
                analogItems.TypeDefinitionId = ObjectTypeIds.FolderType;
                root.AddReference(ReferenceTypeIds.Organizes, false, analogItems.NodeId);
                analogItems.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);
                AddPredefinedNode(SystemContext, analogItems);

				try
				{
					//scalar types
					CreateStaticVariables(statics);
					//array types
					CreateStaticArrayVariables(statics);

                    CreateAnalogItemVariables(analogItems);
					FolderState dynamics = new FolderState(null);
					dynamics.NodeId = new NodeId("Dynamic", NamespaceIndex);
					dynamics.BrowseName = new QualifiedName("Dynamic", NamespaceIndex);
					dynamics.DisplayName = dynamics.BrowseName.Name;
					dynamics.TypeDefinitionId = ObjectTypeIds.FolderType;
					root.AddReference(ReferenceTypeIds.Organizes, false, dynamics.NodeId);
					dynamics.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);
					AddPredefinedNode(SystemContext, dynamics);

					//scalar dynamic types
					CreateDynamicVariables(dynamics);
					//array dynamic types
					CreateDynamicArrayVariables(dynamics);

					CreateNodeIdTestVariables(root);
                    CreateMaxAgeNode(root);
					CreateSimulationMethods(root);
					CreateSimulationVariables(root);

					CreateViews(root, externalReferences, statics);

					//Historical Access
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

					CreateStaticHistoricalVariables(staticHistoricals);

                    // Create TestFolder
                    CreateTestFolder(root);
                    AddPredefinedNode(SystemContext, root);

					CreateAccessRightsTestNodes(root);
				}
				catch(Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}				

                ApplicationModule.Instance.TimerThread.AddAction(m_dynamicVariableChangeAction);
            }
        }

        private void CreateMaxAgeNode(BaseObjectState root)
        {
            DataItemState variable = new DataItemState(root);

            variable.NodeId = new NodeId("maxage", root.NodeId.NamespaceIndex);
            variable.BrowseName = new QualifiedName("maxage", root.NodeId.NamespaceIndex);
            variable.DisplayName = variable.BrowseName.Name;
            variable.TypeDefinitionId = VariableTypeIds.DataItemType;
            variable.DataType = DataTypeIds.Int32;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = 1;
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (root != null)
            {
                root.AddChild(variable);
            }

            variable.OnReadValue = OnReadMaxAgeNode;
        }

        private ServiceResult OnReadMaxAgeNode(
           ISystemContext context,
           NodeState node,
           NumericRange indexRange,
           QualifiedName dataEncoding,
           ref object value,
           ref StatusCode statusCode,
           ref DateTime timestamp)
        {
            lock (Lock)
            {
                BaseVariableState variableNode = node as BaseVariableState;

                if (variableNode != null)
                {
                    DateTime lastDeviceRead = (node as DataItemState).Timestamp;

                    if ((DateTime.UtcNow - lastDeviceRead).TotalSeconds > 3)
                    {
                        value = (int)value + 1;
                        statusCode = StatusCodes.GoodClamped;
                        timestamp = DateTime.UtcNow;

                        variableNode.Value = value;
                        variableNode.StatusCode = statusCode;
                        variableNode.Timestamp = timestamp;
                        variableNode.ClearChangeMasks(null, true);
                    }
                }

                return ServiceResult.Good;
            }
        }

        private void CreateTestFolder(BaseObjectState root)
        {
            // create the TestFolder object
            FolderState testFolder = CreateFolder(root, "Test Folder");
            // add CreateItems command
            CreateItemsCommand createItemsCmd = new CreateItemsCommand(testFolder, this);
        }

        public FolderState CreateFolder(NodeState parent, string folderName)
        {
            // create the Folder object
            FolderState folder = new FolderState(parent);
            folder.NodeId = new NodeId(folderName, parent.NodeId.NamespaceIndex);
            folder.BrowseName = new QualifiedName(folderName, parent.NodeId.NamespaceIndex);
            folder.DisplayName = folder.BrowseName.Name;
            folder.TypeDefinitionId = ObjectTypeIds.FolderType;
            folder.ReferenceTypeId = ReferenceTypes.Organizes;

            if (parent != null)
            {
                parent.AddChild(folder);
            }

            return folder;
        }

        public DataItemState CreateVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank)
        {
            DataItemState variable = new DataItemState(parent);

            variable.NodeId = new NodeId(path, parent.NodeId.NamespaceIndex);
            variable.BrowseName = new QualifiedName(name, parent.NodeId.NamespaceIndex);
            variable.DisplayName = variable.BrowseName.Name;
            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.TypeDefinitionId = VariableTypeIds.DataItemType;
            variable.DataType = (uint)dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = TypeInfo.GetDefaultValue((uint)dataType, valueRank);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }


        private void CreateAnalogItemVariables(FolderState analogItems)
        {
            for (int i = 0; i < 20; i++)
            {
                AnalogItemState<double> v1 = new AnalogItemState<double>(analogItems);
                v1.NodeId = new NodeId("TCAnalogItem" + i, NamespaceIndex);
                v1.DataType = DataTypeIds.Double;
                v1.ValueRank = ValueRanks.Scalar;
                v1.Value = 10.0;
                v1.AccessLevel = AccessLevels.CurrentReadOrWrite;
                v1.BrowseName = new QualifiedName("TCAnalogItem_Item" + i, NamespaceIndex);
                v1.DisplayName = v1.BrowseName.Name;

                // Property EURange
                v1.EURange = new PropertyState<Range>(v1);
                v1.EURange.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
                v1.EURange.BrowseName = BrowseNames.EURange;
                v1.EURange.DisplayName = v1.EURange.BrowseName.Name;
                v1.EURange.DataType = DataTypeIds.Range;
                v1.EURange.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                v1.EURange.ValueRank = ValueRanks.Scalar;
                v1.EURange.Value = new Range(1000.0, 0.0);
                // Property InstrumentRange
                v1.InstrumentRange = new PropertyState<Range>(v1);
                v1.InstrumentRange.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
                v1.InstrumentRange.BrowseName = BrowseNames.InstrumentRange;
                v1.InstrumentRange.DisplayName = v1.InstrumentRange.BrowseName.Name;
                v1.InstrumentRange.DataType = DataTypeIds.Range;
                v1.InstrumentRange.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                v1.InstrumentRange.ValueRank = ValueRanks.Scalar;
                v1.InstrumentRange.Value = new Range(9999.99, -9999.99);
                v1.InstrumentRange.AccessLevel = AccessLevels.CurrentReadOrWrite;
                // Property EngineeringUnits
                EUInformation euInformation = new EUInformation();
                euInformation.Description = new LocalizedText("en", "Kilometers per Hour");
                euInformation.DisplayName = new LocalizedText("en", "km/h");
                v1.EngineeringUnits = new PropertyState<EUInformation>(v1);
                v1.EngineeringUnits.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
                v1.EngineeringUnits.BrowseName = BrowseNames.EngineeringUnits;
                v1.EngineeringUnits.DisplayName = v1.EngineeringUnits.BrowseName.Name;
                v1.EngineeringUnits.DataType = DataTypeIds.EUInformation;
                v1.EngineeringUnits.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                v1.EngineeringUnits.ValueRank = ValueRanks.Scalar;
                v1.EngineeringUnits.Value = euInformation;
                v1.EngineeringUnits.AccessLevel = AccessLevels.CurrentReadOrWrite;
                // Property Definition
                v1.Definition = new PropertyState<string>(v1);
                v1.Definition.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
                v1.Definition.BrowseName = BrowseNames.Definition;
                v1.Definition.DisplayName = v1.Definition.BrowseName.Name;
                v1.Definition.DataType = DataTypeIds.String;
                v1.Definition.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                v1.Definition.ValueRank = ValueRanks.Scalar;
                v1.Definition.Value = "unknown";
                // Property ValuePrecision
                v1.ValuePrecision = new PropertyState<double>(v1);
                v1.ValuePrecision.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
                v1.ValuePrecision.BrowseName = BrowseNames.ValuePrecision;
                v1.ValuePrecision.DisplayName = v1.ValuePrecision.BrowseName.Name;
                v1.ValuePrecision.DataType = DataTypeIds.Double;
                v1.ValuePrecision.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                v1.ValuePrecision.ValueRank = ValueRanks.Scalar;
                v1.ValuePrecision.Value = 1.0;

                analogItems.AddChild(v1);
                AddPredefinedNode(SystemContext, v1);
            }
        }

        protected override bool IsNodeInView(ServerSystemContext context, ContinuationPoint continuationPoint, NodeState node)
        {
            if (continuationPoint.View != null)
            {
                if (continuationPoint.View.ViewId == new NodeId(m_viewId, NamespaceIndex))
                {
                    // suppress operations properties.
                    if (node != null && node.BrowseName.NamespaceIndex != 2)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected override bool IsReferenceInView(ServerSystemContext context, ContinuationPoint continuationPoint, IReference reference)
        {
            if (continuationPoint.View != null)
            {
                // find the node.
                NodeState node = FindPredefinedNode((NodeId)reference.TargetId, typeof(NodeState));

                if (node != null)
                {
                    return IsNodeInView(context, continuationPoint, node);
                }
            }

            return true;
        }
		#endregion

        private void CreateStaticVariables(BaseObjectState root)
		{
			CreateOneVariable<byte>("UInt8", DataTypeIds.Byte, root, byte.MaxValue);
			CreateOneVariable<sbyte>("Int8", DataTypeIds.SByte, root, sbyte.MaxValue);
			CreateOneVariable<UInt16>("UInt16", DataTypeIds.UInt16, root, UInt16.MaxValue);
			CreateOneVariable<Int16>("Int16", DataTypeIds.Int16, root, Int16.MaxValue);
			CreateOneVariable<UInt32>("UInt32", DataTypeIds.UInt32, root, UInt32.MaxValue);
			CreateOneVariable<Int32>("Int32", DataTypeIds.Int32, root, Int32.MaxValue);
			CreateOneVariable<UInt64>("UInt64", DataTypeIds.UInt64, root, UInt64.MaxValue);
			CreateOneVariable<Int64>("Int64", DataTypeIds.Int64, root, Int64.MaxValue);
			CreateOneVariable<Double>("Double", DataTypeIds.Double, root, double.MaxValue);
			CreateOneVariable<float>("Float", DataTypeIds.Float, root, float.MaxValue);
			CreateOneVariable<Boolean>("Boolean", DataTypeIds.Boolean, root, false);
			CreateOneVariable<String>("String", DataTypeIds.String, root, "DEMO");
			CreateOneVariable<byte[]>("ByteString", DataTypeIds.ByteString, root, new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99 });

			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml("<xml value=\"test\"/>");
			CreateOneVariable<XmlElement>("XmlElement", DataTypeIds.XmlElement, root, xdoc.DocumentElement);
			CreateOneVariable<NodeId>("NodeId", DataTypeIds.NodeId, root, new NodeId(6783, 1));
			CreateOneVariable<ExpandedNodeId>("ExpandedNodeId", DataTypeIds.ExpandedNodeId, root, new ExpandedNodeId(67836666, 1));
			CreateOneVariable<Guid>("Guid", DataTypeIds.Guid, root, Guid.NewGuid());
			CreateOneVariable<DateTime>("DateTime", DataTypeIds.DateTime, root, DateTime.UtcNow);
			CreateOneVariable<StatusCode>("StatusCode", DataTypeIds.StatusCode, root, StatusCodes.BadOutOfMemory);
			CreateOneVariable<LocalizedText>("LocalizedText", DataTypeIds.LocalizedText, root, new LocalizedText("en", "localizedText"));
			CreateOneVariable<QualifiedName>("QualifiedName", DataTypeIds.QualifiedName, root, new QualifiedName("qualifiedTextName", 1));
			CreateOneVariable<Variant>("Integer", DataTypeIds.BaseDataType, root);
			CreateOneVariable<Variant>("UnsignedInteger", DataTypeIds.BaseDataType, root);
			CreateOneVariable<Int32>("Enumeration", DataTypeIds.Enumeration, root);
            CreateOneVariable<Int32>("Structure", DataTypeIds.Structure, root);

			EUInformation euInf = new EUInformation();
			euInf.Description = new LocalizedText("en", "Hello EU Information world");
			euInf.DisplayName = new LocalizedText("en", "km/h");
			CreateOneVariable<EUInformation>("EUInformation", DataTypeIds.EUInformation, root, euInf);
			CreateOneVariable<Range>("Range", DataTypeIds.Range, root, new Range(10.0, -10.0));
			CreateOneVariable<DataValue>("DataValue", DataTypeIds.DataValue, root, new DataValue(11.1, StatusCodes.GoodCallAgain));

			CreateOneVariable<Argument>("Argument", DataTypeIds.Argument, root, new Argument("Sollution", DataTypeIds.LocalizedText, -1, "What is the ultimare answer?!"));
		}

		private void CreateStaticHistoricalVariables(BaseObjectState root)
		{
			ArchiveItem itemInt32 = new ArchiveItem("StaticHistoricalDataItem_Int32", new FileInfo("HistoryData\\Int32.txt"));
			ArchiveItemState nodeInt32 = new ArchiveItemState(SystemContext, itemInt32, NamespaceIndex);
			nodeInt32.ReloadFromSource(SystemContext);

			root.AddReference(ReferenceTypeIds.Organizes, false, nodeInt32.NodeId);
			nodeInt32.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

			AddPredefinedNode(SystemContext, nodeInt32);

			ArchiveItem itemByte = new ArchiveItem("StaticHistoricalDataItem_Byte", new FileInfo("HistoryData\\Byte.txt"));
			ArchiveItemState nodeByte = new ArchiveItemState(SystemContext, itemByte, NamespaceIndex);
			nodeByte.ReloadFromSource(SystemContext);

			root.AddReference(ReferenceTypeIds.Organizes, false, nodeByte.NodeId);
			nodeByte.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

			AddPredefinedNode(SystemContext, nodeByte);

			ArchiveItem itemUInt32 = new ArchiveItem("StaticHistoricalDataItem_UInt32", new FileInfo("HistoryData\\UInt32.txt"));
			ArchiveItemState nodeUInt32 = new ArchiveItemState(SystemContext, itemUInt32, NamespaceIndex);
			nodeUInt32.ReloadFromSource(SystemContext);

			root.AddReference(ReferenceTypeIds.Organizes, false, nodeUInt32.NodeId);
			nodeUInt32.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

			AddPredefinedNode(SystemContext, nodeUInt32);

			//Arrays
			/*ArchiveItem itemInt32Array = new ArchiveItem("StaticHistoricalDataItem_Int32Array", new FileInfo("HistoryData\\Int32.txt"));
			ArchiveItemState nodeInt32Array = new ArchiveItemState(SystemContext, itemInt32Array, NamespaceIndex);
			nodeInt32.ReloadFromSource(SystemContext);

			root.AddReference(ReferenceTypeIds.Organizes, false, nodeInt32.NodeId);
			nodeInt32.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

			AddPredefinedNode(SystemContext, nodeInt32);*/

			//Historian nodes
			ArchiveItem itemHistorian1 = new ArchiveItem("StaticHistoricalDataItem_Historian1", new FileInfo("HistoryData\\Historian1.txt"));
			ArchiveItemState nodeHistorian1 = new ArchiveItemState(SystemContext, itemHistorian1, NamespaceIndex);
			nodeHistorian1.ReloadFromSource(SystemContext);

			root.AddReference(ReferenceTypeIds.Organizes, false, nodeHistorian1.NodeId);
			nodeHistorian1.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

			AddPredefinedNode(SystemContext, nodeHistorian1);


			ArchiveItem itemHistorian2 = new ArchiveItem("StaticHistoricalDataItem_Historian2", new FileInfo("HistoryData\\Historian2.txt"));
			ArchiveItemState nodeHistorian2 = new ArchiveItemState(SystemContext, itemHistorian2, NamespaceIndex);
			nodeHistorian2.ReloadFromSource(SystemContext);

			root.AddReference(ReferenceTypeIds.Organizes, false, nodeHistorian2.NodeId);
			nodeHistorian2.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

			AddPredefinedNode(SystemContext, nodeHistorian2);


			ArchiveItem itemHistorian3 = new ArchiveItem("StaticHistoricalDataItem_Historian3", new FileInfo("HistoryData\\Historian3.txt"));
			ArchiveItemState nodeHistorian3 = new ArchiveItemState(SystemContext, itemHistorian3, NamespaceIndex);
			nodeHistorian3.ReloadFromSource(SystemContext);

			root.AddReference(ReferenceTypeIds.Organizes, false, nodeHistorian3.NodeId);
			nodeHistorian3.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

			AddPredefinedNode(SystemContext, nodeHistorian3);


			ArchiveItem itemHistorian4 = new ArchiveItem("StaticHistoricalDataItem_Historian4", new FileInfo("HistoryData\\Historian4.txt"));
			ArchiveItemState nodeHistorian4 = new ArchiveItemState(SystemContext, itemHistorian4, NamespaceIndex);
			nodeHistorian4.ReloadFromSource(SystemContext);

			root.AddReference(ReferenceTypeIds.Organizes, false, nodeHistorian4.NodeId);
			nodeHistorian4.AddReference(ReferenceTypeIds.Organizes, true, root.NodeId);

			AddPredefinedNode(SystemContext, nodeHistorian4);
		}

        private void CreateViews(BaseObjectState root, IDictionary<NodeId, IList<IReference>> externalReferences, BaseObjectState viewChildNode)
        {
            CreateView(root, externalReferences, m_viewId, "View1");
            //CreateView(root, externalReferences, "/TestModule/View2", "View2", viewChildNode);
        }

        private ViewState CreateView(NodeState parent, IDictionary<NodeId, IList<IReference>> externalReferences, uint nodeId, string name)
        {
            ViewState type = new ViewState();

            type.SymbolicName = name;
            type.NodeId = new NodeId(nodeId, NamespaceIndex);
            type.BrowseName = new QualifiedName(name, NamespaceIndex);
            type.DisplayName = type.BrowseName.Name;
            type.WriteMask = AttributeWriteMask.None;
            type.UserWriteMask = AttributeWriteMask.None;
            type.ContainsNoLoops = true;

            IList<IReference> references = null;

            if (!externalReferences.TryGetValue(ObjectIds.ViewsFolder, out references))
            {
                externalReferences[ObjectIds.ViewsFolder] = references = new List<IReference>();
            }

            type.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ViewsFolder);
            references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, type.NodeId));

            if (parent != null)
            {
                parent.AddReference(ReferenceTypes.Organizes, false, type.NodeId);
                type.AddReference(ReferenceTypes.Organizes, true, parent.NodeId);
            }

            AddPredefinedNode(SystemContext, type);
            return type;
        }

        private TestVariable CreateOneVariable<T>(string name, NodeId type, BaseObjectState root,
			T defaultValue)
		{
            TestVariable var = new TestVariable(root);
			var.NodeId = new NodeId(name, NamespaceIndex);
            var.BrowseName = new QualifiedName(name, NamespaceIndex);
			var.DisplayName = var.BrowseName.Name;
            var.Description = var.DisplayName;
            var.TypeDefinitionId = VariableTypeIds.DataItemType;
			var.DataType = type;
			var.ValueRank = ValueRanks.Scalar;
			var.AccessLevel = AccessLevels.CurrentReadOrWrite;
			var.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
			//var.ReferenceTypeId = ReferenceTypeIds.HasProperty;
			var.Value = defaultValue;

			root.AddChild(var);

			AddPredefinedNode(SystemContext, var);

			return var;
		}

        private TestVariable CreateOneVariable<T>(string name, NodeId type, BaseObjectState root)
		{
            TestVariable var = new TestVariable(root);
			var.NodeId = new NodeId(name, NamespaceIndex);

			var.BrowseName = new QualifiedName(name, NamespaceIndex);
			var.DisplayName = var.BrowseName.Name;
			var.TypeDefinitionId = VariableTypeIds.DataItemType;
			var.DataType = type;
			var.ValueRank = ValueRanks.Scalar;
			var.AccessLevel = AccessLevels.CurrentReadOrWrite;
			var.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
			//var.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            var.Value = default(T);

			root.AddChild(var);

			AddPredefinedNode(SystemContext, var);

			return var;
		}

		private void CreateStaticArrayVariables(BaseObjectState root)
		{
			CreateOneArrayVariable<byte>("UInt8Array", DataTypeIds.Byte, root, new byte[] { byte.MaxValue, byte.MinValue, 8, 17, 23 });
			CreateOneArrayVariable<sbyte>("Int8Array", DataTypeIds.SByte, root, new sbyte[5] { sbyte.MaxValue, sbyte.MinValue, -8, 17, -23 });
			CreateOneArrayVariable<UInt16>("UInt16Array", DataTypeIds.UInt16, root, new UInt16[] { UInt16.MaxValue, UInt16.MinValue, 16, 32, 64, 128 });
			CreateOneArrayVariable<Int16>("Int16Array", DataTypeIds.Int16, root, new Int16[] { Int16.MaxValue, Int16.MinValue, -16, -32, 32, 16 });
			CreateOneArrayVariable<UInt32>("UInt32Array", DataTypeIds.UInt32, root, new UInt32[] { UInt32.MaxValue, UInt32.MinValue, 32, 64, 128, 256 });
			CreateOneArrayVariable<Int32>("Int32Array", DataTypeIds.Int32, root, new Int32[] { Int32.MaxValue, Int32.MinValue, -32, -64, 64, 32 });
			CreateOneArrayVariable<UInt64>("UInt64Array", DataTypeIds.UInt64, root, new UInt64[] { UInt64.MaxValue, UInt64.MinValue, 64, 128, 256, 512 });
			CreateOneArrayVariable<Int64>("Int64Array", DataTypeIds.Int64, root, new Int64[] { Int64.MaxValue, Int64.MinValue, -64, -128, 128, 64 });
			CreateOneArrayVariable<Double>("DoubleArray", DataTypeIds.Double, root, new double[] { double.MaxValue, DoubleMinValue, -6.28, -3.14, 42.42, 123.45 });
			CreateOneArrayVariable<float>("FloatArray", DataTypeIds.Float, root, new float[] { float.MaxValue, FloatMinValue, -6.28f, -3.14f, 42.42f, 123.45f });
			CreateOneArrayVariable<Boolean>("BooleanArray", DataTypeIds.Boolean, root, new bool[] { true, false, true, true, false, true });
			CreateOneArrayVariable<String>("StringArray", DataTypeIds.String, root, new string[] { "string 1", "string 2", "string 3", "string 4", "string 5", "string 6" });

			CreateOneArrayVariable<byte[]>("ByteStringArray", DataTypeIds.ByteString, root, new byte[][]
				{ new byte[] {0x00,0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88,0x99 },
				new byte[] {0x00,0x10,0x20,0x30,0x40,0x50,0x60,0x70,0x80,0x90 },
				new byte[] {0x12,0x23,0x34,0x45,0x56,0x67,0x78,0x89,0x89,0x01 },
				new byte[] {0x00,0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88,0x99 },
				new byte[] {0x00,0x10,0x20,0x30,0x40,0x50,0x60,0x70,0x80,0x90 },
				new byte[] {0x12,0x23,0x34,0x45,0x56,0x67,0x78,0x89,0x89,0x01 }
				});

			XmlDocument xmlDoc1 = new XmlDocument();
			xmlDoc1.LoadXml("<xml value=\"test1\"/>");
			XmlDocument xmlDoc2 = new XmlDocument();
			xmlDoc2.LoadXml("<xml value=\"test2\"/>");
			XmlDocument xmlDoc3 = new XmlDocument();
			xmlDoc3.LoadXml("<xml value=\"test3\"/>");
			XmlDocument xmlDoc4 = new XmlDocument();
			xmlDoc4.LoadXml("<xml value=\"test4\"/>");
			XmlDocument xmlDoc5 = new XmlDocument();
			xmlDoc5.LoadXml("<xml value=\"test5\"/>");
			CreateOneArrayVariable<XmlElement>("XmlElementArray", DataTypeIds.XmlElement, root,
				new XmlElement[] { xmlDoc1.DocumentElement, xmlDoc2.DocumentElement, xmlDoc3.DocumentElement, xmlDoc4.DocumentElement, xmlDoc5.DocumentElement });

			CreateOneArrayVariable<NodeId>("NodeIdArray", DataTypeIds.NodeId, root, new NodeId[] {
				new NodeId(6783, 1),
				new NodeId(1234, 2),
				new NodeId(5678, 3) });

			CreateOneArrayVariable<ExpandedNodeId>("ExpandedNodeIdArray", DataTypeIds.ExpandedNodeId, root, new ExpandedNodeId[] {
				new ExpandedNodeId(6783, 1),
				new ExpandedNodeId(1234, 2),
				new ExpandedNodeId(5678, 3) });

			CreateOneArrayVariable<Guid>("GuidArray", DataTypeIds.Guid, root, new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });
			CreateOneArrayVariable<DateTime>("DateTimeArray", DataTypeIds.DateTime, root, new DateTime[] { DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow });
			CreateOneArrayVariable<StatusCode>("StatusCodeArray", DataTypeIds.StatusCode, root,
				new StatusCode[] { StatusCodes.Good, StatusCodes.BadInvalidArgument, StatusCodes.BadOutOfMemory });

			CreateOneArrayVariable<LocalizedText>("LocalizedTextArray", DataTypeIds.LocalizedText, root, new LocalizedText[] {
				new LocalizedText("en", "localizedText1"),
				new LocalizedText("de", "localizedText2"),
				new LocalizedText("ro", "localizedText3"),
			});

			CreateOneArrayVariable<QualifiedName>("QualifiedNameArray", DataTypeIds.QualifiedName, root, new QualifiedName[] {
				new QualifiedName("qualifiedTextName1", 1),
				new QualifiedName("qualifiedTextName2", 2),
				new QualifiedName("qualifiedTextName3", 3) });

			EUInformation euInf1 = new EUInformation();
			euInf1.Description = new LocalizedText("en", "1. Hello EU Information world");
			euInf1.DisplayName = new LocalizedText("en", "1. Hello EU Information world");
			euInf1.NamespaceUri = "1. Namespace uri";
			euInf1.UnitId = 1;

			EUInformation euInf2 = new EUInformation();
			euInf2.Description = new LocalizedText("en", "2. Hello EU Information world");
			euInf2.DisplayName = new LocalizedText("en", "2. Hello EU Information world");
			euInf2.NamespaceUri = "2. Namespace uri";
			euInf2.UnitId = 2;

			EUInformation euInf3 = new EUInformation();
			euInf3.Description = new LocalizedText("en", "3. Hello EU Information world");
			euInf3.DisplayName = new LocalizedText("en", "3. Hello EU Information world");
			euInf3.NamespaceUri = "3. Namespace uri";
			euInf3.UnitId = 3;

			CreateOneArrayVariable<EUInformation>("EUInformationArray", DataTypeIds.EUInformation, root,
				new EUInformation[] { euInf1, euInf2, euInf3 });

			CreateOneArrayVariable<Range>("RangeArray", DataTypeIds.Range, root, new Range[] {
				new Range(100.0, -100), new Range(1234.0, -1234.0), new Range(99992.0, -28771.0)
				});

			Variant v1 = new Variant(true);
			Variant v2 = new Variant(2.3);
			Variant v3 = new Variant(new byte[] { 1, 2, 4 });
			Variant v4 = new Variant(new Int64[] { 0x11, 0x22, 0x33, 0x44 });

			CreateOneArrayVariable<Variant>("ValueArray", DataTypeIds.BaseDataType, root, new Variant[] {
				v1, v2, v3, v4 });

			DataValue dv1 = new DataValue(12.3, StatusCodes.GoodCallAgain);
			DataValue dv2 = new DataValue(new LocalizedText("ro", "Ana are mere"), StatusCodes.GoodMoreData);
			DataValue dv3 = new DataValue(new byte[][] {
			 new byte[] {0x00,0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88,0x99 },
			 new byte[] {0x00,0x10,0x20,0x30,0x40,0x50,0x60,0x70,0x80,0x90 } },
				StatusCodes.GoodMoreData);

			DataValue dv4 = new DataValue(new NodeId("I AM!", 42), StatusCodes.GoodMoreData);
			CreateOneArrayVariable<DataValue>("DataValueArray", DataTypeIds.DataValue, root,
				new DataValue[] { dv1, dv2, dv3, dv4 });

			CreateOneArrayVariable<Argument>("ArgumentArray", DataTypeIds.Argument, root, new Argument[] {
				new Argument("Sollution", DataTypeIds.LocalizedText, -1, "What is the ultimare answer?!"),
				new Argument("Check it", DataTypeIds.LocalizedText, -1, "Is the answer true?!"),
				new Argument("The End", DataTypeIds.LocalizedText, -1, "The answer:") });
		}

        private TestVariable CreateOneArrayVariable<T>(string name, NodeId type, BaseObjectState root, T[] defaultValue)
		{
			//AnalogItemState<T[]> variable = CreateOneVariable<T[]>(name, type, root, defaultValue);
            TestVariable variable = CreateOneVariable<T[]>(name, type, root, defaultValue);
			variable.ValueRank = ValueRanks.OneDimension;

			return variable;
		}

        private TestVariable CreateOneArrayVariable<T>(string name, NodeId type, BaseObjectState root)
		{
			return CreateOneArrayVariable<T>(name, type, root, new T[5]);
		}

		private void CreateDynamicVariables(BaseObjectState root)
		{
			byte[] uint8Values = new byte[] { 225, 234, 57, 0, 1, 45, 77, 123 };
			CreateOneDynamicVariable<byte>("Dynamic.UInt8", DataTypeIds.Byte, root, uint8Values);

			sbyte[] int8Values = new sbyte[] { -127, -123, -57, 0, 1, 45, 77, 123 };
			CreateOneDynamicVariable<sbyte>("Dynamic.Int8", DataTypeIds.SByte, root, int8Values);

			UInt16[] uint16Values = new UInt16[] { 127, 133, 57, 0, 1, 45, 77, 123 };
			CreateOneDynamicVariable<UInt16>("Dynamic.UInt16", DataTypeIds.UInt16, root, uint16Values);

			Int16[] int16Values = new Int16[] { -127, -133, -57, 0, 1, 45, 77, 123 };
			CreateOneDynamicVariable<Int16>("Dynamic.Int16", DataTypeIds.Int16, root, int16Values);

			UInt32[] uint32Values = new UInt32[] { 127, 133, 57, 0, 1, 45, 77, 123 };
			CreateOneDynamicVariable<UInt32>("Dynamic.UInt32", DataTypeIds.UInt32, root, uint32Values);

			Int32[] int32Values = new Int32[] { -127, -133, -57, 0, 1, 45, 77, 123 };
			CreateOneDynamicVariable<Int32>("Dynamic.Int32", DataTypeIds.Int32, root, int32Values);

			UInt64[] uint64Values = new UInt64[] { 127, 133, 57, 0, 1, 45, 77, 123 };
			CreateOneDynamicVariable<UInt64>("Dynamic.UInt64", DataTypeIds.UInt64, root, uint64Values);

			Int64[] int64Values = new Int64[] { -127, -133, -57, 0, 1, 45, 77, 123 };
			CreateOneDynamicVariable<Int64>("Dynamic.Int64", DataTypeIds.Int64, root, int64Values);

			double[] doubleValues = new double[] { -2303.3453, -133.24, -57, 0, 0.1, 45.234874, 7723435.34, 12234863845378.3 };
			CreateOneDynamicVariable<Double>("Dynamic.Double", DataTypeIds.Double, root, doubleValues);

			float[] floatValues = new float[] { -2303.3453f, -133.24f, -57f, 0.0f, 0.1f, 45.234874f, 73435.34f, 12235378.3f };
			CreateOneDynamicVariable<float>("Dynamic.Float", DataTypeIds.Float, root, floatValues);

			bool[] boolValues = new bool[] { true, false };
			CreateOneDynamicVariable<bool>("Dynamic.Boolean", DataTypeIds.Boolean, root, boolValues);

			string[] stringValues = new string[] { "Apples", "Pears", "Plums", "Bananas", "Mangos" };
			CreateOneDynamicVariable<string>("Dynamic.String", DataTypeIds.String, root, stringValues);

			byte[][] byteStringValues = new byte[][] { 
				Encoding.UTF8.GetBytes("Apples"), 
				Encoding.UTF8.GetBytes("Pears"),
				Encoding.UTF8.GetBytes("Plums"),
				Encoding.UTF8.GetBytes("Bananas"),
				Encoding.UTF8.GetBytes("Mangos") };
			CreateOneDynamicVariable<byte[]>("Dynamic.ByteString", DataTypeIds.ByteString, root, byteStringValues);

			NodeId[] nodeIdValues = new NodeId[] {
				new NodeId("DummyNodeId", 2),
				new NodeId(12345, 3),
				new NodeId("Apple Pie", 4),
				new NodeId(new Guid(111, 222, 333, 4, 4, 4, 4, 4, 4, 4, 4), 5),
				new NodeId(UTF8Encoding.UTF8.GetBytes("byte string"), 6)
			};
			CreateOneDynamicVariable<NodeId>("Dynamic.NodeId", DataTypeIds.NodeId, root, nodeIdValues);

			DateTime[] dateTimeValues = new DateTime[] { 
				new DateTime(2000, 1, 2, 3, 4, 5),
				new DateTime(2002, 2, 22, 4, 14, 1),
				new DateTime(2005, 7, 12, 5, 24, 55),
				new DateTime(2008, 12, 31, 6, 34, 47),
				new DateTime(2011, 6, 17, 17, 10, 13) };
			CreateOneDynamicVariable<DateTime>("Dynamic.DateTime", DataTypeIds.DateTime, root, dateTimeValues);

			Guid[] guidValues = new Guid[] {
				Guid.NewGuid(),
				Guid.NewGuid(),
				Guid.NewGuid(),
				Guid.NewGuid(),
				Guid.NewGuid() };
			CreateOneDynamicVariable<Guid>("Dynamic.Guid", DataTypeIds.Guid, root, guidValues);

			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml("<xml value=\"test\"/>");
			XmlElement xml1 = xdoc.DocumentElement;
			xdoc.LoadXml("<xml value=\"capre\"/>");
			XmlElement xml2 = xdoc.DocumentElement;
			xdoc.LoadXml("<xml value=\"mere\"/>");
			XmlElement xml3 = xdoc.DocumentElement;
			xdoc.LoadXml("<xml value=\"gaini\"/>");
			XmlElement xml4 = xdoc.DocumentElement;
			xdoc.LoadXml("<xml value=\"prepelite\"/>");
			XmlElement xml5 = xdoc.DocumentElement;

			XmlElement[] xmlElementValues = new XmlElement[] {
				xml1, xml2, xml3, xml4, xml5 };
			CreateOneDynamicVariable<XmlElement>("Dynamic.XmlElement", DataTypeIds.XmlElement, root, xmlElementValues);
		}

        private TestVariable CreateOneDynamicVariable<T>(string name, NodeId type, BaseObjectState root, T[] values)
		{
            TestVariable var = CreateOneVariable<T>(name, type, root);
			DynamicTestVariable dynVar = new DynamicTestVariable(values, var, SystemContext);

			m_dynamicVariableChangeAction.Add(dynVar);

			return var;
		}

		private void CreateDynamicArrayVariables(BaseObjectState root)
		{
			byte[][] uint8Values = new byte[][] {
				new byte[] { 225, 234, 57, 0, 1, 45, 77, 123 },
				new byte[] { 123, 35, 23, 54, 67, 234, 8, 1 },
				new byte[] { 4, 44, 57, 0, 1, 45, 0, 128 },
				new byte[] { 65, 87, 57, 45, 56, 45, 51, 23 },
				new byte[] { 23, 87, 57, 89, 1, 45, 2, 12 } };
			CreateOneDynamicArrayVariable<byte>("Dynamic.UInt8Array", DataTypeIds.Byte, root, uint8Values);

			sbyte[][] int8Values = new sbyte[][] {
				new sbyte[] { -127, -123, -57, 0, 1, 45, 77, 123 },
				new sbyte[] { -12, 123, -57, 34, 10, 65, -77, 0 },
				new sbyte[] { -27, -13, -87, 56, 71, -75, 117, 87 },
				new sbyte[] { 127, 1, 95, 127, 21, 53, 37, 78 },
				new sbyte[] { -1, 43, -117, 45, 18, -35, 47, 49 } };
			CreateOneDynamicArrayVariable<sbyte>("Dynamic.Int8Array", DataTypeIds.SByte, root, int8Values);

			UInt16[][] uint16Values = new UInt16[][] {
				new UInt16[] { 127, 133, 57, 0, 1, 45, 77, 123 },
				new UInt16[] { 258, 530, 857, 120, 1, 45, 7, 123 },
				new UInt16[] { 2127, 13, 57, 0, 1, 45, 77, 23 },
				new UInt16[] { 10127, 183, 6557, 0, 123, 45, 777, 123 },
				new UInt16[] { 127, 2133, 57, 0, 881, 45, 95, 1237 } };
			CreateOneDynamicArrayVariable<UInt16>("Dynamic.UInt16Array", DataTypeIds.UInt16, root, uint16Values);

			Int16[][] int16Values = new Int16[][] {
				new Int16[] { -127, -133, -57, 0, 1, 45, 77, 123 },
				new Int16[] { 258, 536, 857, 120, 1, 45, 7, -1230 },
				new Int16[] { 2127, 13, 57, 0, 1, 45, -770, 23 },
				new Int16[] { 10127, -193, 6557, 0, 123, 45, 777, 12 },
				new Int16[] { 127, 2133, 57, 0, 881, 45, 97, 1237 } };
			CreateOneDynamicArrayVariable<Int16>("Dynamic.Int16Array", DataTypeIds.Int16, root, int16Values);

			UInt32[][] uint32Values = new UInt32[][] {
				new UInt32[] { 127, 133, 57, 0, 1, 45, 77, 123 },
				new UInt32[] { 258, 536, 857, 120, 1, 45, 7, 71230 },
				new UInt32[] { 2127, 13, 57, 0, 1, 45, 34770, 23 },
				new UInt32[] { 10127, 8193, 6557, 0, 123, 45, 777, 12 },
				new UInt32[] { 127, 2133, 57, 0, 881, 45, 97, 1237 } };
			CreateOneDynamicArrayVariable<UInt32>("Dynamic.UInt32Array", DataTypeIds.UInt32, root, uint32Values);

			Int32[][] int32Values = new Int32[][] {
				new Int32[] { -127, -133, -57, 0, 1, 45, 77, 123 },
				new Int32[] { 258, 536, 857, -120, 1, 45, 7, -71230 },
				new Int32[] { -2127, 13, 57, 0, 1, -45, 34770, 23 },
				new Int32[] { 10127, 8193, -6557, 0, 123, 45, -777, 12 },
				new Int32[] { 127, -2133, 57, 0, 881, 45, 97, 1237 }};
			CreateOneDynamicArrayVariable<Int32>("Dynamic.Int32Array", DataTypeIds.Int32, root, int32Values);

			UInt64[][] uint64Values = new UInt64[][] {
				new UInt64[] { 127, 133, 57, 0, 1, 45, 77, 123 },
				new UInt64[] { 258, 536, 857, 120, 1, 45, 7, 71230 },
				new UInt64[] { 2127, 13, 57, 0, 1, 45, 34770, 23 },
				new UInt64[] { 10127, 8193, 6557, 0, 123, 45, 777, 12 },
				new UInt64[] { 127, 2133, 57, 0, 881, 45, 97, 1237 } };
			CreateOneDynamicArrayVariable<UInt64>("Dynamic.UInt64Array", DataTypeIds.UInt64, root, uint64Values);

			Int64[][] int64Values = new Int64[][] {
				new Int64[] { -127, -133, -57, 0, 1, 45, 77, 123 },
				new Int64[] { 258, 536, 857, -120, 1, 45, 7, -71230 },
				new Int64[] { -2127, 13, 57, 0, 1, -45, 34770, 23 },
				new Int64[] { 10127, 8193, -6557, 0, 123, 45, -777, 12 },
				new Int64[] { 127, -2133, 57, 0, 881, 45, 97, 1237 }};
			CreateOneDynamicArrayVariable<Int64>("Dynamic.Int64Array", DataTypeIds.Int64, root, int64Values);

			double[][] doubleValues = new double[][] {
				new double[] { -2303.3453, -133.24, -57, 0, 0.1, 45.234874, 7723435.34, 12234863845378.3 },
				new double[] {-133.24, -57, 7723435.34, 0, 0.1,  -2303.3453, 45.234874, 12234863845378.3 },
				new double[] { 0.1, -2303.3453, -133.24, -57, 0, 7723435.34, 12234863845378.3 },
				new double[] { -57, -2303.3453, 45.234874, -133.24, 0, 0.1, 45.234874, 7723435.34, 12234863845378.3 },
				new double[] { 12234863845378.3, -2303.3453, -133.24, -57, 0, 0.1, 45.234874, 7723435.34 } };
			CreateOneDynamicArrayVariable<Double>("Dynamic.DoubleArray", DataTypeIds.Double, root, doubleValues);

			float[][] floatValues = new float[][] {
				new float[] { -2303.3453f, -133.24f, -57f, 0.0f, 0.1f, 45.234874f, 73435.34f, 12235378.3f },
				new float[] {  -133.24f, -2303.3453f,-57f, 0.0f, 0.1f, 45.234874f, 12235378.3f, 73435.34f } ,
				new float[] { -57f, -2303.3453f, -133.24f, 0.0f, 0.1f, 73435.34f, 12235378.3f, 45.234874f } ,
				new float[] { 0.0f, -2303.3453f, 12235378.3f, -133.24f, -57f, 0.1f, 45.234874f, 73435.34f } ,
				new float[] { 0.1f, -2303.3453f, -133.24f, -57f, 45.234874f, 73435.34f, 12235378.3f, 0.0f } };
			CreateOneDynamicArrayVariable<float>("Dynamic.FloatArray", DataTypeIds.Float, root, floatValues);

			bool[][] boolValues = new bool[][] {
				new bool[] { true, false },
				new bool[] { false, true } ,
				new bool[] { true, true } ,
				new bool[] { false, false } };
			CreateOneDynamicArrayVariable<bool>("Dynamic.BooleanArray", DataTypeIds.Boolean, root, boolValues);

			string[][] stringValues = new string[][] {
				new string[] { "Apples", "Pears", "Plums", "Bananas", "Mangos" },
				new string[] { "Pears", "Apples", "Mangos", "Bananas", "Plums" },
				new string[] { "Plums", "Mangos", "Apples", "Pears", "Bananas" },
				new string[] { "Bananas", "Apples", "Pears", "Plums", "Mangos" },
				new string[] { "Mangos", "Pears", "Plums", "Bananas", "Apples" } };
			CreateOneDynamicArrayVariable<string>("Dynamic.StringArray", DataTypeIds.String, root, stringValues);

			byte[][][] byteStringValues = new byte[][][] { 
				new byte[][] {
				Encoding.UTF8.GetBytes("Apples"), Encoding.UTF8.GetBytes("Pears"),
				Encoding.UTF8.GetBytes("Plums"), Encoding.UTF8.GetBytes("Bananas"),
				Encoding.UTF8.GetBytes("Mangos") },
				new byte[][] {
				Encoding.UTF8.GetBytes("Plums"), Encoding.UTF8.GetBytes("Pears"),
				Encoding.UTF8.GetBytes("Bananas"),
				Encoding.UTF8.GetBytes("Mangos"), Encoding.UTF8.GetBytes("Apples") },
				new byte[][] {
				Encoding.UTF8.GetBytes("Pears"),
				Encoding.UTF8.GetBytes("Bananas"), Encoding.UTF8.GetBytes("Plums"),
				Encoding.UTF8.GetBytes("Mangos"), Encoding.UTF8.GetBytes("Apples") } };
			CreateOneDynamicArrayVariable<byte[]>("Dynamic.ByteStringArray", DataTypeIds.ByteString, root, byteStringValues);

			NodeId[][] nodeIdValues = new NodeId[][] {
				new NodeId[] { new NodeId("DummyNodeId", 2),
				new NodeId(12345, 3),	new NodeId("Apple Pie", 4),
				new NodeId(new Guid(111, 222, 333, 4, 4, 4, 4, 4, 4, 4, 4), 5),
				new NodeId(UTF8Encoding.UTF8.GetBytes("byte string"), 6) },
				new NodeId[] { new NodeId("DumbNodeId", 2),
				new NodeId("Cherry Pie", 4), new NodeId(12345, 3),
				new NodeId(new Guid(333, 111, 222, 4, 4, 4, 4, 4, 4, 4, 4), 5),
				new NodeId(UTF8Encoding.UTF8.GetBytes("string of bytes"), 6) }
			};
			CreateOneDynamicArrayVariable<NodeId>("Dynamic.NodeIdArray", DataTypeIds.NodeId, root, nodeIdValues);

			DateTime[][] dateTimeValues = new DateTime[][] {
				new DateTime[] { 
				new DateTime(2000, 1, 2, 3, 4, 5),
				new DateTime(2002, 2, 22, 4, 14, 1),
				new DateTime(2005, 7, 12, 5, 24, 55),
				new DateTime(2008, 12, 31, 6, 34, 47),
				new DateTime(2011, 6, 17, 17, 10, 13) },
				new DateTime[] { new DateTime(2011, 6, 17, 17, 10, 13),
				new DateTime(2008, 12, 31, 6, 34, 47),
				new DateTime(2005, 7, 12, 5, 24, 55),
				new DateTime(2002, 2, 22, 4, 14, 1),
				new DateTime(2000, 1, 2, 3, 4, 5)} };
			CreateOneDynamicArrayVariable<DateTime>("Dynamic.DateTimeArray", DataTypeIds.DateTime, root, dateTimeValues);

			Guid[][] guidValues = new Guid[][] {
				new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
				new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }};
			CreateOneDynamicArrayVariable<Guid>("Dynamic.GuidArray", DataTypeIds.Guid, root, guidValues);

			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml("<xml value=\"test\"/>");
			XmlElement xml1 = xdoc.DocumentElement;
			xdoc.LoadXml("<xml value=\"capre\"/>");
			XmlElement xml2 = xdoc.DocumentElement;
			xdoc.LoadXml("<xml value=\"mere\"/>");
			XmlElement xml3 = xdoc.DocumentElement;
			xdoc.LoadXml("<xml value=\"gaini\"/>");
			XmlElement xml4 = xdoc.DocumentElement;
			xdoc.LoadXml("<xml value=\"prepelite\"/>");
			XmlElement xml5 = xdoc.DocumentElement;

			XmlElement[][] xmlElementValues = new XmlElement[][] {
				new XmlElement[]{ xml1, xml2, xml3, xml4, xml5 },
				new XmlElement[]{ xml1, xml3, xml5, xml2, xml4 },
				new XmlElement[]{ xml5, xml4, xml3, xml2, xml1 }
			};
			CreateOneDynamicArrayVariable<XmlElement>("Dynamic.XmlElementArray", DataTypeIds.XmlElement, root, xmlElementValues);
		}

        private TestVariable CreateOneDynamicArrayVariable<T>(string name, NodeId type, BaseObjectState root, T[][] values)
		{
            TestVariable var = CreateOneArrayVariable<T>(name, type, root, values[0]);
			DynamicTestVariable dynVar = new DynamicTestVariable(values, var, SystemContext);

			m_dynamicVariableChangeAction.Add(dynVar);

			return var;
		}

		private void CreateNodeIdTestVariables(BaseObjectState root)
		{
			const int namespaceIdx = 3;

            #region wait method
            MethodState waitMethod = new MethodState(root);
            waitMethod.NodeId = new NodeId("waitMethod", namespaceIdx);
            waitMethod.BrowseName = new QualifiedName("WaitMethod", namespaceIdx);
            waitMethod.DisplayName = new LocalizedText("de", waitMethod.BrowseName.Name);
            waitMethod.Executable = true;
            waitMethod.ReferenceTypeId = ReferenceTypeIds.HasComponent;
            root.AddChild(waitMethod);

            // create the input arguments.
            PropertyState<Argument[]> inputArgument = new PropertyState<Argument[]>(waitMethod);
            inputArgument.NodeId = new NodeId("waitMethod_InputArguments", namespaceIdx);
            inputArgument.BrowseName = new QualifiedName(BrowseNames.InputArguments);
            inputArgument.DisplayName = inputArgument.BrowseName.Name;
            inputArgument.TypeDefinitionId = VariableTypeIds.PropertyType;
            inputArgument.DataType = DataTypeIds.Argument;
            inputArgument.ValueRank = ValueRanks.OneDimension;
            inputArgument.AccessLevel = AccessLevels.CurrentRead;
            inputArgument.UserAccessLevel = AccessLevels.CurrentRead;
            inputArgument.ReferenceTypeId = ReferenceTypeIds.HasProperty;

            inputArgument.Value = new Argument[] 
            {
                new Argument("WaitTime", DataTypeIds.UInt32, ValueRanks.Scalar, null)
            };

            waitMethod.InputArguments = inputArgument;

            AddPredefinedNode(SystemContext, waitMethod);

            // register handler.
            waitMethod.OnCallMethod = new GenericMethodCalledEventHandler(DoWaitMethodCall);

            #endregion

            #region test Method
            MethodState method = new MethodState(root);
			method.NodeId = new NodeId("methodId", namespaceIdx);
			method.BrowseName = new QualifiedName("Method", namespaceIdx);
			method.DisplayName = new LocalizedText("de", method.BrowseName.Name);
			method.Executable = true;
			method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
			root.AddChild(method);

			// create the input arguments.
			PropertyState<Argument[]> inputArguments = new PropertyState<Argument[]>(method);
			inputArguments.NodeId = new NodeId("methodId_InputArguments", namespaceIdx);
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
                    new Argument("Input_1", DataTypeIds.UInt32, ValueRanks.Scalar, null) ,
					new Argument("Input_2", DataTypeIds.UInt32, ValueRanks.Scalar, null),
					new Argument("Input_3", DataTypeIds.Double, ValueRanks.Scalar, null)
                };

			method.InputArguments = inputArguments;

            // create the output arguments.
            PropertyState<Argument[]> outputArguments = new PropertyState<Argument[]>(method);
            outputArguments.NodeId = new NodeId("methodId_OutputArguments", namespaceIdx);
            outputArguments.BrowseName = new QualifiedName(BrowseNames.OutputArguments);
            outputArguments.DisplayName = outputArguments.BrowseName.Name;
            outputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            outputArguments.DataType = DataTypeIds.Argument;
            outputArguments.ValueRank = ValueRanks.OneDimension;
            outputArguments.AccessLevel = AccessLevels.CurrentRead;
            outputArguments.UserAccessLevel = AccessLevels.CurrentRead;
            outputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;

            outputArguments.Value = new Argument[] 
                {
                    new Argument("Output_1", DataTypeIds.UInt32, ValueRanks.Scalar, null) ,
					new Argument("Output_2", DataTypeIds.UInt32, ValueRanks.Scalar, null),
					new Argument("Output_3", DataTypeIds.Double, ValueRanks.Scalar, null)
                };

            method.OutputArguments = outputArguments;

			AddPredefinedNode(SystemContext, method);

			// register handler.
            method.OnCallMethod = new GenericMethodCalledEventHandler(DoMethodCallInOutArgs);
			#endregion

			#region test MethodNot
            TestVariable methodNot = new TestVariable(root);
			methodNot.NodeId = new NodeId("methodIdNot", namespaceIdx);
			methodNot.BrowseName = new QualifiedName("MethodNot", namespaceIdx);
			methodNot.DisplayName = new LocalizedText("de", methodNot.BrowseName.Name);
			methodNot.DataType = DataTypeIds.Double;
            methodNot.Value = (double)0.0;

			root.AddChild(methodNot);
			AddPredefinedNode(SystemContext, methodNot);
			#endregion

			#region ShutdownMethod method
			MethodState shudownMethod = new MethodState(root);
			shudownMethod.NodeId = new NodeId("shutDownMethodId", namespaceIdx);
			shudownMethod.BrowseName = new QualifiedName("ShutdownMethod", namespaceIdx);
			shudownMethod.DisplayName = new LocalizedText("de", shudownMethod.BrowseName.Name);
			shudownMethod.Executable = true;
			shudownMethod.ReferenceTypeId = ReferenceTypeIds.HasComponent;
			root.AddChild(shudownMethod);

			// create the input arguments.
			inputArguments = new PropertyState<Argument[]>(shudownMethod);
			inputArguments.NodeId = new NodeId("shutDownMethod_InputArguments", namespaceIdx);
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
                    new Argument("Delay", DataTypeIds.UInt32, ValueRanks.Scalar, null)
                };

			shudownMethod.InputArguments = inputArguments;

			AddPredefinedNode(SystemContext, shudownMethod);

			// register handler.
			shudownMethod.OnCallMethod = new GenericMethodCalledEventHandler(DoShutdownCall);
			#endregion

            #region test enum method
            MethodState enumMethod = new MethodState(root);
            enumMethod.NodeId = new NodeId("testEnumMethodId", namespaceIdx);
            enumMethod.BrowseName = new QualifiedName("testEnumMethod", namespaceIdx);
            enumMethod.DisplayName = new LocalizedText("de", enumMethod.BrowseName.Name);
            enumMethod.Executable = true;
            enumMethod.ReferenceTypeId = ReferenceTypeIds.HasComponent;
            root.AddChild(enumMethod);

            // create the input arguments.
            inputArguments = new PropertyState<Argument[]>(enumMethod);
            inputArguments.NodeId = new NodeId("testEnumMethod_InputArguments", namespaceIdx);
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
                new Argument("ScaleEnumeration", DataTypeIds.AxisScaleEnumeration, ValueRanks.Scalar, null),
                new Argument("MessageSecurityMode", DataTypeIds.MessageSecurityMode, ValueRanks.Scalar, null),
            };

            enumMethod.InputArguments = inputArguments;

            // create the output arguments.
            outputArguments = new PropertyState<Argument[]>(enumMethod);
            outputArguments.NodeId = new NodeId("testEnumMethod_OutputArguments", namespaceIdx);
            outputArguments.BrowseName = new QualifiedName(BrowseNames.OutputArguments);
            outputArguments.DisplayName = outputArguments.BrowseName.Name;
            outputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            outputArguments.DataType = DataTypeIds.Argument;
            outputArguments.ValueRank = ValueRanks.OneDimension;
            outputArguments.AccessLevel = AccessLevels.CurrentRead;
            outputArguments.UserAccessLevel = AccessLevels.CurrentRead;
            outputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;

            outputArguments.Value = new Argument[] 
                {
                    new Argument("NamingRuleType", DataTypeIds.NamingRuleType, ValueRanks.Scalar, null) ,
					new Argument("NodeClass", DataTypeIds.NodeClass, ValueRanks.Scalar, null),
					new Argument("UserTokenType", DataTypeIds.UserTokenType, ValueRanks.Scalar, null)
                };

            enumMethod.OutputArguments = outputArguments;

            AddPredefinedNode(SystemContext, enumMethod);

            // register handler.
            enumMethod.OnCallMethod = new GenericMethodCalledEventHandler(DoTestEnumMethodCall);
            #endregion

			#region Method NoReference
			MethodState methodNoRef = new MethodState(root);
			methodNoRef.NodeId = new NodeId("methodId_NoReference", namespaceIdx);
			methodNoRef.BrowseName = new QualifiedName("Method NoReference", namespaceIdx);
			methodNoRef.DisplayName = new LocalizedText("de", methodNoRef.BrowseName.Name);
			methodNoRef.Executable = true;
			methodNoRef.ReferenceTypeId = ReferenceTypeIds.Organizes;
			root.AddChild(method);

			AddPredefinedNode(SystemContext, methodNoRef);

			// register handler.
			methodNoRef.OnCallMethod = new GenericMethodCalledEventHandler(DoMethodCall);
			#endregion

			#region test Method SubType
			MethodState methodSubtype = new MethodState(root);
			methodSubtype.NodeId = new NodeId("methodId_SubType", namespaceIdx);
			methodSubtype.BrowseName = new QualifiedName("Method SubType", namespaceIdx);
			methodSubtype.DisplayName = new LocalizedText("de", methodSubtype.BrowseName.Name);
			methodSubtype.Executable = true;
			methodSubtype.ReferenceTypeId = ReferenceTypeIds.HasOrderedComponent;
			root.AddChild(methodSubtype);

			AddPredefinedNode(SystemContext, methodSubtype);

			// register handler.
			methodSubtype.OnCallMethod = new GenericMethodCalledEventHandler(DoMethodCall);
			#endregion

			#region StringNodeId
            TestVariable varStringNodeId = new TestVariable(root);
			varStringNodeId.NodeId = new NodeId("stringNodeId", namespaceIdx);
			varStringNodeId.BrowseName = new QualifiedName("VarWithStrNodeID", namespaceIdx);
			varStringNodeId.DisplayName = new LocalizedText("de", varStringNodeId.BrowseName.Name);
			varStringNodeId.TypeDefinitionId = VariableTypeIds.DataItemType;
			varStringNodeId.DataType = DataTypeIds.UInt32;
			varStringNodeId.ValueRank = ValueRanks.Scalar;
			varStringNodeId.AccessLevel = AccessLevels.CurrentReadOrWrite;
			varStringNodeId.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
			varStringNodeId.Value = (uint)12345;
			root.AddChild(varStringNodeId);
			AddPredefinedNode(SystemContext, varStringNodeId);
			#endregion

            #region MinimumSamplingInterval
            TestVariable varMinSamplingInterval = new TestVariable(root);
            varMinSamplingInterval.NodeId = new NodeId("varMinimumSamplingInterval", namespaceIdx);
            varMinSamplingInterval.BrowseName = new QualifiedName("varMinimumSamplingInterval", namespaceIdx);
            varMinSamplingInterval.DisplayName = new LocalizedText("en-US", varMinSamplingInterval.BrowseName.Name);
            varMinSamplingInterval.TypeDefinitionId = VariableTypeIds.DataItemType;
            varMinSamplingInterval.DataType = DataTypeIds.Int32;
            varMinSamplingInterval.ValueRank = ValueRanks.Scalar;
            varMinSamplingInterval.AccessLevel = AccessLevels.CurrentReadOrWrite;
            varMinSamplingInterval.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            varMinSamplingInterval.Value = 555;
            varMinSamplingInterval.MinimumSamplingInterval = 300;

            root.AddChild(varMinSamplingInterval);
            AddPredefinedNode(SystemContext, varMinSamplingInterval);
            #endregion

            #region Numeric NodeId
            TestVariable varNumericNodeId = new TestVariable(root);
			varNumericNodeId.NodeId = new NodeId(6783, namespaceIdx);
			varNumericNodeId.BrowseName = new QualifiedName("VarWithNumNodeID", namespaceIdx);
			varNumericNodeId.DisplayName = new LocalizedText("de", varNumericNodeId.BrowseName.Name);
			varNumericNodeId.TypeDefinitionId = VariableTypeIds.DataItemType;
			varNumericNodeId.DataType = DataTypeIds.UInt32;
			varNumericNodeId.ValueRank = ValueRanks.Scalar;
			varNumericNodeId.AccessLevel = AccessLevels.CurrentReadOrWrite;
			varNumericNodeId.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
			varNumericNodeId.Value = (uint)12345;

			root.AddChild(varNumericNodeId);
			AddPredefinedNode(SystemContext, varNumericNodeId);
			#endregion

			#region Guid NodeId
            TestVariable varGuidNodeId = new TestVariable(root);
			varGuidNodeId.NodeId = new NodeId(new Guid(11, 22, 33, 1, 2, 3, 4, 5, 6, 7, 8), namespaceIdx);
			varGuidNodeId.BrowseName = new QualifiedName("VarWithGuidNodeID", namespaceIdx);
			varGuidNodeId.DisplayName = new LocalizedText("de", varGuidNodeId.BrowseName.Name);
			varGuidNodeId.TypeDefinitionId = VariableTypeIds.DataItemType;
			varGuidNodeId.DataType = DataTypeIds.UInt32;
			varGuidNodeId.ValueRank = ValueRanks.Scalar;
			varGuidNodeId.AccessLevel = AccessLevels.CurrentReadOrWrite;
			varGuidNodeId.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
			varGuidNodeId.Value = (uint)12345;

			root.AddChild(varGuidNodeId);
			AddPredefinedNode(SystemContext, varGuidNodeId);
			#endregion

			#region ByteStringNodeId
			//AnalogItemState<UInt32> varByteStringNodeId = new AnalogItemState<UInt32>(root);
            TestVariable varByteStringNodeId = new TestVariable(root);
			varByteStringNodeId.NodeId = new NodeId(new byte[] { 0xAB, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99 }, namespaceIdx); ;
			varByteStringNodeId.BrowseName = new QualifiedName("VarWithByteStringNodeID", namespaceIdx);
			varByteStringNodeId.DisplayName = new LocalizedText("de", varByteStringNodeId.BrowseName.Name);
			varByteStringNodeId.TypeDefinitionId = VariableTypeIds.DataItemType;
			varByteStringNodeId.DataType = DataTypeIds.UInt32;
			varByteStringNodeId.ValueRank = ValueRanks.Scalar;
			varByteStringNodeId.AccessLevel = AccessLevels.CurrentReadOrWrite;
			varByteStringNodeId.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
			varByteStringNodeId.Value = (uint)12345;

			root.AddChild(varByteStringNodeId);

			AddPredefinedNode(SystemContext, varByteStringNodeId);
			#endregion
		}

		private void CreateAccessRightsTestNodes(BaseObjectState root)
		{
			//Read only access
			TestVariable readOnly = new TestVariable(root);
			readOnly.NodeId = new NodeId("ReadOnlyAccess", NamespaceIndex);
			readOnly.BrowseName = new QualifiedName("ReadOnlyAccess", NamespaceIndex);
			readOnly.DisplayName = readOnly.BrowseName.Name;
			readOnly.DataType = DataTypeIds.Double;
			readOnly.Value = (double) 0.0;
			readOnly.AccessLevel = AccessLevels.CurrentRead;
			readOnly.UserAccessLevel = AccessLevels.CurrentRead;
			root.AddChild(readOnly);
			AddPredefinedNode(SystemContext, readOnly);


			//Write only access
			TestVariable writeOnly = new TestVariable(root);
			writeOnly.NodeId = new NodeId("WriteOnlyAccess", NamespaceIndex);
			writeOnly.BrowseName = new QualifiedName("WriteOnlyAccess", NamespaceIndex);
			writeOnly.DisplayName = writeOnly.BrowseName.Name;
			writeOnly.DataType = DataTypeIds.Double;
			writeOnly.Value = (double) 0.0;
			writeOnly.AccessLevel = AccessLevels.CurrentWrite;
			writeOnly.UserAccessLevel = AccessLevels.CurrentWrite;
			root.AddChild(writeOnly);
			AddPredefinedNode(SystemContext, writeOnly);

			//User read only access
			TestVariable userReadOnly = new TestVariable(root);
			userReadOnly.NodeId = new NodeId("UserReadOnlyAccess", NamespaceIndex);
			userReadOnly.BrowseName = new QualifiedName("UserReadOnlyAccess", NamespaceIndex);
			userReadOnly.DisplayName = userReadOnly.BrowseName.Name;
			userReadOnly.DataType = DataTypeIds.Double;
			userReadOnly.Value = (double) 0.0;
			userReadOnly.AccessLevel = AccessLevels.CurrentReadOrWrite;
			userReadOnly.UserAccessLevel = AccessLevels.CurrentRead;
			root.AddChild(userReadOnly);
			AddPredefinedNode(SystemContext, userReadOnly);

			//User write only access
			TestVariable userWriteOnly = new TestVariable(root);
			userWriteOnly.NodeId = new NodeId("UserWriteOnlyAccess", NamespaceIndex);
			userWriteOnly.BrowseName = new QualifiedName("UserWriteOnlyAccess", NamespaceIndex);
			userWriteOnly.DisplayName = userWriteOnly.BrowseName.Name;
			userWriteOnly.DataType = DataTypeIds.Double;
			userWriteOnly.Value = (double) 0.0;
			userWriteOnly.AccessLevel = AccessLevels.CurrentReadOrWrite;
			userWriteOnly.UserAccessLevel = AccessLevels.CurrentWrite;
			root.AddChild(userWriteOnly);
			AddPredefinedNode(SystemContext, userWriteOnly);
		}

		private void CreateSimulationMethods(BaseObjectState root)
		{
			const int namespaceIdx = 3;

			#region CreateVariablesByRange method
			MethodState method = new MethodState(root);
			method.NodeId = new NodeId("createVariablesByRange", namespaceIdx);
			method.BrowseName = new QualifiedName("CreateVariablesByRange", NamespaceIndex);
			method.DisplayName = method.BrowseName.Name;
			method.Executable = true;
			method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
			root.AddChild(method);

			// create the input arguments.
			PropertyState<Argument[]> inputArguments = new PropertyState<Argument[]>(method);
			inputArguments.NodeId = new NodeId("createVariablesByRange_InputArguments", NamespaceIndex);
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
                    new Argument("nodeIdType", DataTypeIds.SByte, ValueRanks.Scalar, null) ,
					new Argument("count", DataTypeIds.UInt32, ValueRanks.Scalar, null),
					new Argument("namespaceIndex", DataTypeIds.UInt16, ValueRanks.Scalar, null),
                    new Argument("description", DataTypeIds.String, ValueRanks.Scalar, null)
                };

			method.InputArguments = inputArguments;

			// create the output arguments.
			PropertyState<Argument[]> outputArguments = new PropertyState<Argument[]>(method);

			outputArguments.NodeId = new NodeId("createVariablesByRange_OutputArguments", NamespaceIndex);
			outputArguments.BrowseName = new QualifiedName(BrowseNames.OutputArguments);
			outputArguments.DisplayName = outputArguments.BrowseName.Name;
			outputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
			outputArguments.DataType = DataTypeIds.Argument;
			outputArguments.ValueRank = ValueRanks.OneDimension;
			outputArguments.AccessLevel = AccessLevels.CurrentRead;
			outputArguments.UserAccessLevel = AccessLevels.CurrentRead;
			outputArguments.Historizing = false;
			outputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;

			outputArguments.Value = new Argument[] 
                {
                     new Argument("namespaceIndex", DataTypeIds.UInt16, ValueRanks.Scalar, null) ,
					new Argument("simVarSetId", DataTypeIds.UInt32, ValueRanks.Scalar, null),
                };

			method.OutputArguments = outputArguments;

			AddPredefinedNode(SystemContext, method);

			// register handler.
			method.OnCallMethod = new GenericMethodCalledEventHandler(DoCreateVariablesByRangeCall);
			#endregion

			#region CreateBenchmarkVariables method
			method = new MethodState(root);
			method.NodeId = new NodeId("createBenchmarkVariables", namespaceIdx);
			method.BrowseName = new QualifiedName("CreateBenchmarkVariables", NamespaceIndex);
			method.DisplayName = method.BrowseName.Name;
			method.Executable = true;
			method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
			root.AddChild(method);

			// create the input arguments.
			inputArguments = new PropertyState<Argument[]>(method);
			inputArguments.NodeId = new NodeId("createBenchmarkVariables_InputArguments", NamespaceIndex);
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
				new Argument("nodeIdType", DataTypeIds.IdType, ValueRanks.Scalar, null) ,
				new Argument("count", DataTypeIds.UInt32, ValueRanks.Scalar, null),
				new Argument("nodeDataType", DataTypeIds.NodeId, ValueRanks.Scalar, null),
				new Argument("nodeValueRank", DataTypeIds.Int32, ValueRanks.Scalar, null),
				new Argument("nodeArrayLength", DataTypeIds.Int32, ValueRanks.Scalar, null)
			};

			method.InputArguments = inputArguments;

			// create the output arguments.
			outputArguments = new PropertyState<Argument[]>(method);

			outputArguments.NodeId = new NodeId("createBenchmarkVariables_OutputArguments", NamespaceIndex);
			outputArguments.BrowseName = new QualifiedName(BrowseNames.OutputArguments);
			outputArguments.DisplayName = outputArguments.BrowseName.Name;
			outputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
			outputArguments.DataType = DataTypeIds.Argument;
			outputArguments.ValueRank = ValueRanks.OneDimension;
			outputArguments.AccessLevel = AccessLevels.CurrentRead;
			outputArguments.UserAccessLevel = AccessLevels.CurrentRead;
			outputArguments.Historizing = false;
			outputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;

			outputArguments.Value = new Argument[] 
			{
				new Argument("namespaceIndex", DataTypeIds.UInt16, ValueRanks.Scalar, null) ,
				new Argument("simVarSetId", DataTypeIds.UInt32, ValueRanks.Scalar, null),
			};

			method.OutputArguments = outputArguments;

			AddPredefinedNode(SystemContext, method);

			// register handler.
			method.OnCallMethod = new GenericMethodCalledEventHandler(DoCreateBenchmarkVariables);
			#endregion

			#region DeleteVariablesMethod method
			method = new MethodState(root);
			method.NodeId = new NodeId("deleteVariables", namespaceIdx);
			method.BrowseName = new QualifiedName("DeleteVariables", NamespaceIndex);
			method.DisplayName = method.BrowseName.Name;
			method.Executable = true;
			method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
			root.AddChild(method);

			// create the input arguments.
			inputArguments = new PropertyState<Argument[]>(method);
			inputArguments.NodeId = new NodeId("deleteVariables_InputArguments", NamespaceIndex);
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
				new Argument("simVarSetId", DataTypeIds.UInt32, ValueRanks.Scalar, "Id of the variable set to be deleted.")
			};

			method.InputArguments = inputArguments;

			AddPredefinedNode(SystemContext, method);

			// register handler.
			method.OnCallMethod = new GenericMethodCalledEventHandler(DoDeleteVariablesCall);
			#endregion

			#region StartSimulation method
			method = new MethodState(root);
			method.NodeId = new NodeId("startSimulation", namespaceIdx);
			method.BrowseName = new QualifiedName("StartSimulation", NamespaceIndex);
			method.DisplayName = method.BrowseName.Name;
			method.Executable = true;
			method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
			root.AddChild(method);

			// create the input arguments.
			inputArguments = new PropertyState<Argument[]>(method);
			inputArguments.NodeId = new NodeId("startSimulation_InputArguments", NamespaceIndex);
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
				new Argument("simVarSetId", DataTypeIds.UInt32, ValueRanks.Scalar, null) ,
				new Argument("changeInterval", DataTypeIds.UInt32, ValueRanks.Scalar, null),
				new Argument("repeatCount", DataTypeIds.UInt32, ValueRanks.Scalar, null),
				new Argument("increment", DataTypeIds.Double, ValueRanks.Scalar, null),
				new Argument("changeCount", DataTypeIds.UInt32, ValueRanks.Scalar, null)
			};

			method.InputArguments = inputArguments;

			AddPredefinedNode(SystemContext, method);

			// register handler.
			method.OnCallMethod = new GenericMethodCalledEventHandler(DoStartSimulationCall);
			#endregion

			#region resetServer method
			method = new MethodState(root);
			method.NodeId = new NodeId("resetServer", namespaceIdx);
			method.BrowseName = new QualifiedName("resetServer", NamespaceIndex);
			method.DisplayName = method.BrowseName.Name;
			method.Executable = true;
			method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
			root.AddChild(method);

			AddPredefinedNode(SystemContext, method);

			// register handler.
			//method.OnCallMethod = new GenericMethodCalledEventHandler(DoMethodCall);
			#endregion

			#region StopSimulation method
			method = new MethodState(root);
			method.NodeId = new NodeId("stopSimulation", namespaceIdx);
			method.BrowseName = new QualifiedName("StopSimulation", NamespaceIndex);
			method.DisplayName = method.BrowseName.Name;
			method.Executable = true;
			method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
			root.AddChild(method);

			// create the input arguments.
			inputArguments = new PropertyState<Argument[]>(method);
			inputArguments.NodeId = new NodeId("stopSimulation_InputArguments", NamespaceIndex);
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
				new Argument("simVarSetId", DataTypeIds.UInt32, ValueRanks.Scalar, null)
			};

			method.InputArguments = inputArguments;

			AddPredefinedNode(SystemContext, method);

			// register handler.
			method.OnCallMethod = new GenericMethodCalledEventHandler(DoStopSimulationCall);
			#endregion

			#region DumpSimulation method
			method = new MethodState(root);
			method.NodeId = new NodeId("dumpSimulation", namespaceIdx);
			method.BrowseName = new QualifiedName("DumpSimulation", NamespaceIndex);
			method.DisplayName = method.BrowseName.Name;
			method.Executable = true;
			method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
			root.AddChild(method);

			AddPredefinedNode(SystemContext, method);

			// register handler.
			//method.OnCallMethod = new GenericMethodCalledEventHandler(DoMethodCall);
			#endregion
		}

		private void CreateSimulationVariables(BaseObjectState root)
		{
			#region Analogue Value with valid EURange
			AnalogItemState<double> v1 = new AnalogItemState<double>(root);
			v1.NodeId = new NodeId("TCAnalogItem_Item1", NamespaceIndex);
			v1.DataType = DataTypeIds.Double;
			v1.ValueRank = ValueRanks.Scalar;
			v1.Value = 10.0;
			v1.AccessLevel = AccessLevels.CurrentReadOrWrite;
            v1.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
			v1.BrowseName = new QualifiedName("TCAnalogItem_Item1", NamespaceIndex);
			v1.DisplayName = v1.BrowseName.Name;
			
			// Property EURange
			v1.EURange = new PropertyState<Range>(v1);
			v1.EURange.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
			v1.EURange.BrowseName = BrowseNames.EURange;
			v1.EURange.DisplayName = v1.EURange.BrowseName.Name;
			v1.EURange.DataType = DataTypeIds.Range;
			v1.EURange.ReferenceTypeId = ReferenceTypeIds.HasProperty;
			v1.EURange.ValueRank = ValueRanks.Scalar;
			v1.EURange.Value = new Range(1000.0, 0.0);
			// Property InstrumentRange
			v1.InstrumentRange = new PropertyState<Range>(v1);
			v1.InstrumentRange.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
			v1.InstrumentRange.BrowseName = BrowseNames.InstrumentRange;
			v1.InstrumentRange.DisplayName = v1.InstrumentRange.BrowseName.Name;
			v1.InstrumentRange.DataType = DataTypeIds.Range;
			v1.InstrumentRange.ReferenceTypeId = ReferenceTypeIds.HasProperty;
			v1.InstrumentRange.ValueRank = ValueRanks.Scalar;
			v1.InstrumentRange.Value = new Range(9999.99, -9999.99);
			v1.InstrumentRange.AccessLevel = AccessLevels.CurrentReadOrWrite;
			// Property EngineeringUnits
			EUInformation euInformation = new EUInformation();
			euInformation.Description = new LocalizedText("en", "Kilometers per Hour");
			euInformation.DisplayName = new LocalizedText("en", "km/h");
			v1.EngineeringUnits = new PropertyState<EUInformation>(v1);
			v1.EngineeringUnits.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
			v1.EngineeringUnits.BrowseName = BrowseNames.EngineeringUnits;
			v1.EngineeringUnits.DisplayName = v1.EngineeringUnits.BrowseName.Name;
			v1.EngineeringUnits.DataType = DataTypeIds.EUInformation;
			v1.EngineeringUnits.ReferenceTypeId = ReferenceTypeIds.HasProperty;
			v1.EngineeringUnits.ValueRank = ValueRanks.Scalar;
			v1.EngineeringUnits.Value = euInformation;
			v1.EngineeringUnits.AccessLevel = AccessLevels.CurrentReadOrWrite;
			// Property Definition
			v1.Definition = new PropertyState<string>(v1);
			v1.Definition.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
			v1.Definition.BrowseName = BrowseNames.Definition;
			v1.Definition.DisplayName = v1.Definition.BrowseName.Name;
			v1.Definition.DataType = DataTypeIds.String;
			v1.Definition.ReferenceTypeId = ReferenceTypeIds.HasProperty;
			v1.Definition.ValueRank = ValueRanks.Scalar;
			v1.Definition.Value = "unknown";
			// Property ValuePrecision
			v1.ValuePrecision = new PropertyState<double>(v1);
			v1.ValuePrecision.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
			v1.ValuePrecision.BrowseName = BrowseNames.ValuePrecision;
			v1.ValuePrecision.DisplayName = v1.ValuePrecision.BrowseName.Name;
			v1.ValuePrecision.DataType = DataTypeIds.Double;
			v1.ValuePrecision.ReferenceTypeId = ReferenceTypeIds.HasProperty;
			v1.ValuePrecision.ValueRank = ValueRanks.Scalar;
			v1.ValuePrecision.Value = 1.0;

			root.AddChild(v1);
			AddPredefinedNode(SystemContext, v1);
			#endregion

			#region Analogue Value with invalid EURange
			AnalogItemState<double> v2 = new AnalogItemState<double>(root);
			v2.NodeId = new NodeId("TCAnalogItem_Item2", NamespaceIndex);
			v2.DataType = DataTypeIds.Double;
			v2.ValueRank = ValueRanks.Scalar;
			v2.Value = 10.0;
			v2.AccessLevel = AccessLevels.CurrentReadOrWrite;
            v2.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
			v2.BrowseName = new QualifiedName("TCAnalogItem_Item2", NamespaceIndex);
			v2.DisplayName = v2.BrowseName.Name;

			// Property EURange
			v2.EURange = new PropertyState<Range>(v2);
			v2.EURange.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
			v2.EURange.BrowseName = BrowseNames.EURange;
			v2.EURange.DisplayName = v2.EURange.BrowseName.Name;
			v2.EURange.DataType = DataTypeIds.Range;
			v2.EURange.ReferenceTypeId = ReferenceTypeIds.HasProperty;
			v2.EURange.ValueRank = ValueRanks.Scalar;
			v2.EURange.Value = new Range(0.0, 1000.0);

			root.AddChild(v2);
			AddPredefinedNode(SystemContext, v2);
			#endregion
		}

		#region Test method handlers
		private ServiceResult DoMethodCall(ISystemContext context, MethodState method,
			IList<object> inputArguments, IList<object> outputArguments)
		{
			// we repeat the first three input args as outputs
			for(int i = 0; i < inputArguments.Count; i++)
			{
				if (i < 3)
				{
					outputArguments.Add(inputArguments[i]);
				}
				else
				{
				}
			}

			return ServiceResult.Good;
		}

        private ServiceResult DoMethodCallInOutArgs(ISystemContext context, MethodState method,
            IList<object> inputArguments, IList<object> outputArguments)
        {
            // we repeat the first three input args as outputs
            for (int i = 0; i < inputArguments.Count; i++)
            {
                if (outputArguments.Count > i)
                {
                    outputArguments[i] = inputArguments[i];
                }                
            }

            return ServiceResult.Good;
        }

        private ServiceResult DoWaitMethodCall(ISystemContext context, MethodState method,
            IList<object> inputArguments, IList<object> outputArguments)
        {
            uint waitTime = 1000;
            if (inputArguments.Count > 0)
            {
                waitTime = (uint)inputArguments[0];
            }

            Thread.Sleep((int)waitTime);

            return ServiceResult.Good;
        }


		private ServiceResult DoShutdownCall(ISystemContext context, MethodState method,
			IList<object> inputArguments, IList<object> outputArguments)
		{
			UInt32 delay = (UInt32) inputArguments[0];
			ApplicationModule.Instance.StopApplication(delay);

			return ServiceResult.Good;
		}

        private ServiceResult DoTestEnumMethodCall(ISystemContext context, MethodState method,
            IList<object> inputArguments, IList<object> outputArguments)
        {
            outputArguments[0] = NamingRuleType.Mandatory;
            outputArguments[1] = NodeClass.View;
            outputArguments[2] = UserTokenType.IssuedToken;

            return ServiceResult.Good;
        }

		private ServiceResult DoCreateVariablesByRangeCall(ISystemContext context, MethodState method,
			IList<object> inputArguments, IList<object> outputArguments)
		{
			try
			{
				sbyte nodeIdType = (sbyte) inputArguments[0];
				UInt32 count = (UInt32) inputArguments[1];
				UInt16 namespaceIndex /*= (UInt16) inputArguments[2]*/; //cannot be used
				UInt16 nameIndex=10;
                string description = "";
                if (inputArguments.Count > 3)
                    description = inputArguments[3] as string;

				SimVarManager varManager = ApplicationModule.Instance.SimVarManager;
				BaseObjectState root = ApplicationModule.Instance.GetNodeManager<SimulationModule.SimulationNodeManager>().TestVariablesNode;

				if (!varManager.GetNextNamespaceIndex(ref nameIndex))
				{
					return StatusCodes.BadTooManyOperations;
				}

				string namespaceUri = String.Format("http://somecompany.com/TestServer/SimulationManager{0}", nameIndex);

				if (Server.NamespaceUris.GetIndex(namespaceUri) == -1)
				{
					Server.NodeManager.RegisterNamespaceManager(namespaceUri, this);

					namespaceIndex = (ushort) Server.NamespaceUris.GetIndex(namespaceUri);

					lock(m_simulationNamespaceIndexes)
					{
						m_simulationNamespaceIndexes.Add(namespaceIndex);
					}
				}
				else
				{
					namespaceIndex = (ushort) Server.NamespaceUris.GetIndex(namespaceUri);
				}

				SimVarSet simVarSet;
				SimVarRecord pRec = new SimVarRecord();
				pRec.NameIndex = nameIndex;
				pRec.NamespaceIndex = namespaceIndex;

				varManager.CreateVariables(nodeIdType, count, namespaceIndex, pRec, root, out simVarSet);


				foreach(SimulationVariable var in simVarSet)
				{
                    var.Description = description;
					AddPredefinedNode(SystemContext, var);
				}
				
				outputArguments[0] = namespaceIndex;
				outputArguments[1] = pRec.simVarSetId;

				return ServiceResult.Good;
			}
			catch
			{
				return new ServiceResult(StatusCodes.Bad);
			}
		}

		private ServiceResult DoCreateBenchmarkVariables(ISystemContext context, MethodState method,
			IList<object> inputArguments, IList<object> outputArguments)
		{
			try
			{
				IdType nodeIdType = (IdType) inputArguments[0];
				UInt32 count = (UInt32) inputArguments[1];
				NodeId nodeDataType = (NodeId) inputArguments[2];
				Int32 nodeValueRank = (Int32) inputArguments[3];
				Int32 nodeArrayLength = (Int32) inputArguments[4];
				UInt16 namespaceIndex;
				UInt16 nameIndex = 10;
				SimVarManager varManager = ApplicationModule.Instance.SimVarManager;
				BaseObjectState root = ApplicationModule.Instance.GetNodeManager<SimulationModule.SimulationNodeManager>().TestVariablesNode;

				if (!varManager.GetNextNamespaceIndex(ref nameIndex))
				{
					return StatusCodes.BadTooManyOperations;
				}

				string namespaceUri = String.Format("http://somecompany.com/TestServer/SimulationManager{0}", nameIndex);

				if (Server.NamespaceUris.GetIndex(namespaceUri) == -1)
				{
					Server.NodeManager.RegisterNamespaceManager(namespaceUri, this);

					namespaceIndex = (ushort) Server.NamespaceUris.GetIndex(namespaceUri);

					lock(m_simulationNamespaceIndexes)
					{
						m_simulationNamespaceIndexes.Add(namespaceIndex);
					}
				}
				else
				{
					namespaceIndex = (ushort) Server.NamespaceUris.GetIndex(namespaceUri);
				}

				SimVarSet simVarSet;
				SimVarRecord pRec = new SimVarRecord();
				pRec.NameIndex = nameIndex;
				pRec.NamespaceIndex = namespaceIndex;

				varManager.CreateVariables(nodeIdType, count, nodeDataType, nodeValueRank, nodeArrayLength, namespaceIndex, pRec, root, out simVarSet, true);

				foreach(SimulationVariable var in simVarSet)
				{
					AddPredefinedNode(SystemContext, var);
				}


				outputArguments[0] = namespaceIndex;
				outputArguments[1] = pRec.simVarSetId;

				return ServiceResult.Good;
			}
			catch
			{
				return new ServiceResult(StatusCodes.Bad);
			}
		}

		private ServiceResult DoDeleteVariablesCall(ISystemContext context, MethodState method,
			IList<object> inputArguments, IList<object> outputArguments)
		{
				UInt32 simVarSetId = (UInt32) inputArguments[0];

				SimVarManager varManager = ApplicationModule.Instance.SimVarManager;

				BaseObjectState root = ApplicationModule.Instance.GetNodeManager<SimulationModule.SimulationNodeManager>().TestVariablesNode;

				SimVarSet simVarSet;

				if (varManager.DeleteVariables(simVarSetId, root, out simVarSet) == StatusCodes.Good)
				{
					foreach(SimulationVariable var in simVarSet)
					{
						RemovePredefinedNode(SystemContext, var, null);
					}

					return ServiceResult.Good;
				}
				else
					return new ServiceResult(StatusCodes.Bad);
			}

		private ServiceResult DoStartSimulationCall(ISystemContext context, MethodState method,
			IList<object> inputArguments, IList<object> outputArguments)
		{
			UInt32 simVarSetId = (UInt32) inputArguments[0];
			UInt32 changeInterval = (UInt32) inputArguments[1];
			UInt32 repeatCount = (UInt32) inputArguments[2];
			Double increment = (Double) inputArguments[3];
			UInt32 changeCount = (UInt32) inputArguments[4];

			SimVarManager varManager = ApplicationModule.Instance.SimVarManager;

			if (varManager.StartSimulation(simVarSetId, changeInterval, repeatCount, increment, changeCount) == StatusCodes.Good)
				return ServiceResult.Good;
			else
				return new ServiceResult(StatusCodes.Bad);
		}

		private ServiceResult DoStopSimulationCall(ISystemContext context, MethodState method,
			IList<object> inputArguments, IList<object> outputArguments)
		{
			UInt32 simVarSetId = (UInt32) inputArguments[0];

			SimVarManager varManager = ApplicationModule.Instance.SimVarManager;
			BaseObjectState root = ApplicationModule.Instance.GetNodeManager<SimulationModule.SimulationNodeManager>().TestVariablesNode;

			if (varManager.StopSimulation(simVarSetId) == StatusCodes.Good)
				return ServiceResult.Good;
			else
				return new ServiceResult(StatusCodes.Bad);
		}
		#endregion

		/// <summary>
		/// Returns true if the namespace for the node id is one of the namespaces managed by the node manager.
		/// </summary>
		/// <param name="nodeId">The node id to check.</param>
		/// <returns>True if the namespace is one of the nodes.</returns>
		protected override bool IsNodeIdInNamespace(NodeId nodeId)
		{
			if (base.IsNodeIdInNamespace(nodeId))
				return true;

			lock(m_simulationNamespaceIndexes)
			{
				if (m_simulationNamespaceIndexes.Contains(nodeId.NamespaceIndex))
					return true;
			}

			return false;
		}

        public void AddPredefinedNode(NodeState node)
        {
            AddPredefinedNode(SystemContext, node);
        }

		/// <summary>
		/// min numeric double constants differ between C++ and C#
		/// equivalent with minDoubleValue = 2.2250738585072014E-308 C++ debugger string representation
		/// </summary>
		public static double DoubleMinValue
		{
			get	{
				return BitConverter.ToDouble(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, 0);
			}
		}
		/// <summary>
		///  min numeric float constants differ between C++ and C#
		///  equivalent with minFloatValue = (float)1.17549435E-38 C++ debugger string representation
		/// </summary>
		public static float FloatMinValue
		{
			get	{
				return BitConverter.ToSingle(new byte[] { 0x00, 0x00, 0x80, 0x00 }, 0);
			}
		}
	}
}
