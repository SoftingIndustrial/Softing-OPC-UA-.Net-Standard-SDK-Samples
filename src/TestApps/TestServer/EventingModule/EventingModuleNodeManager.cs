using System;
using System.Collections.Generic;
using System.Reflection;
using Opc.Ua;
using Opc.Ua.Server;

namespace TestServer.EventingModule
{
	class EventingModuleNodeManager : CustomNodeManager2
	{
		public EventingModuleNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.EventingModule)
		{
		}

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
			lock(Lock)
			{
				base.CreateAddressSpace(externalReferences);

				// create the notificationArea object
				BaseObjectState notificationArea = new BaseObjectState(null);

				notificationArea.NodeId = new NodeId("TestServer.NotificationArea", NamespaceIndex);
				notificationArea.BrowseName = new QualifiedName("NotificationArea", NamespaceIndex);
				notificationArea.DisplayName = notificationArea.BrowseName.Name;
				notificationArea.EventNotifier = EventNotifiers.SubscribeToEvents;

				// ensure root can be found via the Objects object
				IList<IReference> references = null;
				if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
				{
					externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
				}

				notificationArea.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
				references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, notificationArea.NodeId));

				notificationArea.AddReference(ReferenceTypeIds.HasNotifier, true, ObjectIds.Server);
				
				// add notifier reference to the Server object
				IList<IReference> serverReferences = null;
				if (!externalReferences.TryGetValue(ObjectIds.Server, out serverReferences))
				{
					externalReferences[ObjectIds.Server] = serverReferences = new List<IReference>();
				}
				serverReferences.Add(new NodeStateReference(ReferenceTypeIds.HasNotifier, false, notificationArea));

				//the events raised/caught by the notificationArea node, will also be reported to the server node if an event monitored item is created on the server
				AddRootNotifier(notificationArea);
                
				// save the node for later lookup
				AddPredefinedNode(SystemContext, notificationArea);
                
				// create the Boiler object
				BaseObjectState boiler = new BaseObjectState(null);

				boiler.NodeId = new NodeId("TestServer.NotificationArea.Boiler", NamespaceIndex);
				boiler.BrowseName = new QualifiedName("Boiler", NamespaceIndex);
				boiler.DisplayName = boiler.BrowseName.Name;
				boiler.EventNotifier = EventNotifiers.SubscribeToEvents;

				boiler.AddReference(ReferenceTypeIds.Organizes, true, notificationArea.NodeId);
				notificationArea.AddReference(ReferenceTypeIds.Organizes, false, boiler.NodeId);
				boiler.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, true, notificationArea);
				notificationArea.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, false, boiler);

				// save the node for later lookup
				AddPredefinedNode(SystemContext, boiler);

				// create the TriggerEventSink object
				m_triggerEventSink = new BaseObjectState(null);

				m_triggerEventSink.NodeId = new NodeId("TestServer.TriggerEventSink", NamespaceIndex);
				m_triggerEventSink.BrowseName = new QualifiedName("TriggerEventSink", NamespaceIndex);
				m_triggerEventSink.DisplayName = m_triggerEventSink.BrowseName.Name;
				m_triggerEventSink.EventNotifier = EventNotifiers.SubscribeToEvents;

				m_triggerEventSink.AddReference(ReferenceTypeIds.Organizes, true, notificationArea.NodeId);
				notificationArea.AddReference(ReferenceTypeIds.Organizes, false, m_triggerEventSink.NodeId);
				m_triggerEventSink.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, true, notificationArea);
				notificationArea.AddNotifier(SystemContext, ReferenceTypeIds.HasNotifier, false, m_triggerEventSink);

				// save the node for later lookup
				AddPredefinedNode(SystemContext, m_triggerEventSink);
				

				// create the TriggerEvent method
				MethodState method = new MethodState(m_triggerEventSink);
				method.NodeId = new NodeId("TestServer.TriggerEventSink.TriggerEventMethod", NamespaceIndex);
				method.BrowseName = new QualifiedName("TriggerEventMethod", NamespaceIndex);
				method.DisplayName = method.BrowseName.Name;
				method.ReferenceTypeId = ReferenceTypeIds.HasComponent;
				m_triggerEventSink.AddChild(method);

				// create the input arguments.
				PropertyState<Argument[]> inputArguments = new PropertyState<Argument[]>(method);
				inputArguments.NodeId = new NodeId("TestServer.TriggerConditionSink.TriggerEventMethod_InputArguments", NamespaceIndex);
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
                    new Argument("typeId", DataTypeIds.NodeId, ValueRanks.Scalar, null) ,
					new Argument("initType", DataTypeIds.UInt32, ValueRanks.Scalar, null)
                };

				method.InputArguments = inputArguments;
				method.OnCallMethod = new GenericMethodCalledEventHandler(DoTriggerEventCall);

				AddPredefinedNode(SystemContext, method);


				ApplicationModule.Instance.TimerThread.AddAction(new ScheduledEventAction<BaseEventState>(boiler, SystemContext));
                ApplicationModule.Instance.TimerThread.AddAction(new ScheduledEventAction<SystemEventState>(boiler, SystemContext));
			}
		#endregion
		}

		/// <summary>
		/// Reports an event on the node with NodeId sourceNodeId(1, _T("TestServer.TriggerEventSink")).
		/// arg0 : event type node id
		/// arg1 : what kind of init to use:
		/// arg1 == 0 : EnumStatusCode EventNotification::init(Server::BaseNode* instanceOrType) [ instance ]
		/// arg1 == 1 : EnumStatusCode EventNotification::initFromTypeId(const INodeId* eventType)
		/// arg1 == 2 : EnumStatusCode EventNotification::init(Server::BaseNode* instanceOrType) [ type ]
		/// </summary>
		private ServiceResult DoTriggerEventCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
		{
			NodeId typeId = (NodeId)inputArguments[0];
			UInt32 initType = (UInt32)inputArguments[1];

			FieldInfo[] fields = typeof(ObjectTypeIds).GetFields(BindingFlags.Public | BindingFlags.Static);

			string simpleTypeName = null;

			foreach(FieldInfo field in fields)
			{
				if (typeId.Equals(field.GetValue(null)))
				{
					simpleTypeName = field.Name.Substring(0, field.Name.Length - 4);
					break;
				}
			}

            if (simpleTypeName == null)
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }

			string assemblyName = typeof(BaseEventState).AssemblyQualifiedName;
			string typeName = "Softing.Opc.Ua.Sdk." + simpleTypeName + "State"
				+ assemblyName.Substring(assemblyName.IndexOf(','));

			Type type = Type.GetType(typeName);

			object state = Activator.CreateInstance(type, new object[] { (NodeState)null });

			BaseEventState e = state as BaseEventState;

			e.Initialize(SystemContext, m_triggerEventSink, EventSeverity.Medium,
				new LocalizedText("en", string.Format("Triggered event of type {0}.", simpleTypeName)));

			m_triggerEventSink.ReportEvent(SystemContext, e);

			return ServiceResult.Good;
		}

		BaseObjectState m_triggerEventSink;
	}
}