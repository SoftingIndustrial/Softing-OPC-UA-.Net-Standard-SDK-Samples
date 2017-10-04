using System.Collections.Generic;
using TestServer.SimulationModule;
using Opc.Ua.Server;
using Opc.Ua;

namespace TestServer
{
    public class TestNodeManager : CustomNodeManager2
    {
        #region Private Members

        private BaseObjectState m_commandsNode;
        private BaseObjectState m_testVariablesNode;
        private BaseObjectState m_complexBrowseNode;
        private BaseObjectState m_statisticsNode;

        #endregion
        
        #region Constructors

        public TestNodeManager(IServerInternal server, ApplicationConfiguration configuration, string moduleNamespace) : base(server, configuration, moduleNamespace)            
        {}

        #endregion

        #region Register Command

        protected void RegisterCommand(SimulationModuleCommand command, IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            BaseObjectState commandsSubNode = new BaseObjectState(null);

            commandsSubNode.NodeId = GenerateNodeId(command.Name);
            commandsSubNode.BrowseName = new QualifiedName(command.Name, NamespaceIndex);
            LocalizedText localizedText = new LocalizedText("de", commandsSubNode.BrowseName.Name);
            commandsSubNode.DisplayName = localizedText;

            string executeCommandName = "Execute";
            LocalizedText executeDisplayName = new LocalizedText("de", executeCommandName);
            VariableCommand executeVariable = new VariableCommand(commandsSubNode);
            executeVariable.ServerCommand = command;
            executeVariable.Create(SystemContext, GenerateNodeId(command.Name + "." + executeCommandName),
                                    new QualifiedName(executeCommandName, NamespaceIndex), executeDisplayName, true);
            executeVariable.Description = string.Empty;
            executeVariable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            executeVariable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            executeVariable.DataType = DataTypeIds.Boolean;
            executeVariable.ValueRank = ValueRanks.Scalar;
            executeVariable.Value = false;
            executeVariable.StatusCode = StatusCodes.Good;

            commandsSubNode.AddChild(executeVariable);

            foreach (CommandParameter parameter in command.Parameters)
            {
                VariableParameter variable = new VariableParameter(commandsSubNode);
                LocalizedText parameterVariableDisplayName = new LocalizedText("de", parameter.Name);
                // instantiate based on the type model. assigns ids automatically using SystemContext.NodeIdFactory
                variable.Create(
                    SystemContext,
                    GenerateNodeId(command.Name + "." + parameter.Name),
                    new QualifiedName(parameter.Name, NamespaceIndex),
                    parameterVariableDisplayName,
                    true);
                variable.DataType = parameter.ParameterType;
                variable.ValueRank = ValueRanks.Scalar;
                variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
                variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                variable.Description = string.Empty;
                TestUtils.SetDefaultValue(variable);
                // tightly coupled.
                commandsSubNode.AddChild(variable);

                command.AddVariableParameter(parameter.Name, variable);
            }

            BaseObjectState commandsNode = GetCommandsNode(externalReferences);
            commandsNode.AddChild(commandsSubNode);
            // save the node for later lookup.
            AddPredefinedNode(SystemContext, commandsNode);
        }
        
        #endregion

        #region Register Variable

        protected void RegisterBrowseVariable(ServerVariable variable, IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            BaseObjectState complexBrowseNode = GetComplexBrowseNode(externalReferences);
            RegisterVariable(variable, externalReferences, complexBrowseNode);
        }

        protected void RegisterVariable(ServerVariable variable, IDictionary<NodeId, IList<IReference>> externalReferences, BaseInstanceState parentNode)
        {
            parentNode.AddChild(variable);

            // save the node for later lookup.
            AddPredefinedNode(SystemContext, parentNode);
        }

        protected void RegisterNode(BaseInstanceState node, BaseInstanceState parentNode)
        {
            parentNode.AddChild(node);

            // save the node for later lookup.
            AddPredefinedNode(SystemContext, parentNode);
        }

        protected void RegisterStatisticVariable(ServerVariable variable, IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            BaseObjectState statisticNode = GetStatisticNode(externalReferences);
            RegisterVariable(variable, externalReferences, statisticNode);
        }

        #endregion

        #region GenerateNodeId

        protected NodeId GenerateNodeId(string nodeIdName)
        {
            return new NodeId(nodeIdName, NamespaceIndex);
        }

        #endregion

        #region GetCommandsNode

        private BaseObjectState GetCommandsNode(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            if (m_commandsNode == null)
            {
                string objectName = "Commands";
                m_commandsNode = new BaseObjectState(null);

                m_commandsNode.NodeId = GenerateNodeId(objectName);
                m_commandsNode.BrowseName = new QualifiedName(objectName, NamespaceIndex);
                LocalizedText localizedText = new LocalizedText("de", m_commandsNode.BrowseName.Name);
                m_commandsNode.DisplayName = localizedText;
                //m_commandsNode.TypeDefinitionId = ObjectTypeIds.FolderType;

                // ensure root can be found via the server object. 
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                m_commandsNode.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, m_commandsNode.NodeId));
            }

            return m_commandsNode;
        }

        #endregion

        #region GetTestVariablesNode

        public BaseInstanceState GetTestVariablesNode(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            if (m_testVariablesNode == null)
            {
                string objectName = "TestVariables";
                m_testVariablesNode = new BaseObjectState(null);

                m_testVariablesNode.NodeId = GenerateNodeId(objectName);
                m_testVariablesNode.BrowseName = new QualifiedName(objectName, NamespaceIndex);
                LocalizedText localizedText = new LocalizedText("de", m_testVariablesNode.BrowseName.Name);
                m_testVariablesNode.DisplayName = localizedText;

                // ensure root can be found via the server object. 
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                m_testVariablesNode.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, m_testVariablesNode.NodeId));
                AddPredefinedNode(SystemContext, m_testVariablesNode);
            }

            return m_testVariablesNode;
        }

        #endregion

        #region GetComplexBrowseNode

        private BaseObjectState GetComplexBrowseNode(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            if (m_complexBrowseNode == null)
            {
                string objectName = "ComplexBrowse";
                m_complexBrowseNode = new BaseObjectState(null);

                m_complexBrowseNode.NodeId = GenerateNodeId(objectName);
                m_complexBrowseNode.BrowseName = new QualifiedName(objectName, NamespaceIndex);
                m_complexBrowseNode.DisplayName = new LocalizedText("de", m_complexBrowseNode.BrowseName.Name); ;
                //m_complexBrowseNode.TypeDefinitionId = ObjectTypeIds.FolderType;

                // ensure root can be found via the server object. 
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                m_complexBrowseNode.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, m_complexBrowseNode.NodeId));
            }

            return m_complexBrowseNode;
        }

        #endregion

        #region GetStatisticsNode

        protected BaseObjectState GetStatisticNode(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            if (m_statisticsNode == null)
            {
                string objectName = "Statistics";
                m_statisticsNode = new BaseObjectState(null);

                m_statisticsNode.NodeId = GenerateNodeId(objectName);
                m_statisticsNode.BrowseName = new QualifiedName(objectName, NamespaceIndex);
                m_statisticsNode.DisplayName = new LocalizedText("de", m_statisticsNode.BrowseName.Name); ;
                //m_statisticsNode.TypeDefinitionId = ObjectTypeIds.FolderType;

                // ensure root can be found via the server object. 
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                //m_statisticsNode.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, m_statisticsNode.NodeId));
            }

            return m_statisticsNode;

        }

        #endregion

        #region Properties

        public BaseObjectState TestVariablesNode
        {
            get { return m_testVariablesNode; }
        }

        #endregion

        #region AddPredefinedNode

        internal void AddPredefinedNode(NodeState node)
        {
            base.AddPredefinedNode(SystemContext, node);
        }

        #endregion
    }
}