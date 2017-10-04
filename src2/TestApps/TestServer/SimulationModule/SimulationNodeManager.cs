using Opc.Ua;
using Opc.Ua.Server;
using System.Collections.Generic;
using TestServer.SimulationModule.Commands;

namespace TestServer.SimulationModule
{
    public class SimulationNodeManager : TestNodeManager
    {
        #region Private Fields

        private List<SimulationModuleCommand> m_commands = new List<SimulationModuleCommand>();
        private SimulationModule m_module;
        private TestServerConfiguration m_configuration;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public SimulationNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.TestServer)            
        {
            SystemContext.NodeIdFactory = this; 

            // get the configuration for the node manager.
            m_configuration = configuration.ParseExtension<TestServerConfiguration>();

            // use suitable defaults if no configuration exists.
            if (m_configuration == null)
            {
                m_configuration = new TestServerConfiguration();
            }
        }
        #endregion
        
        #region IDisposable Members

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected override void Dispose(bool disposing)
        {  
            if (disposing)
            {
                // TBD
            }
        }

        #endregion

        #region Override

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            base.CreateAddressSpace(externalReferences);
            
            CreateVariablesCommand createVariablesCommand = new CreateVariablesCommand(m_module);
            createVariablesCommand.Name = "CreateVariables";
            createVariablesCommand.Parameters.Add(new CommandParameter() { Name = "StartIndex", ParameterType = DataTypeIds.UInt32 });
            createVariablesCommand.Parameters.Add(new CommandParameter() { Name = "EndIndex", ParameterType = DataTypeIds.UInt32 });
            createVariablesCommand.Parameters.Add(new CommandParameter() { Name = "NodeIdType", ParameterType = DataTypeIds.UInt32 });
            createVariablesCommand.Parameters.Add(new CommandParameter() { Name = "Pattern", ParameterType = DataTypeIds.String });
            createVariablesCommand.Parameters.Add(new CommandParameter() { Name = "VarType", ParameterType = DataTypeIds.UInt32 });
            m_commands.Add(createVariablesCommand);
            RegisterCommand(createVariablesCommand, externalReferences);

            CreateVariablesOldCommand createVariablesOldCommand = new CreateVariablesOldCommand(m_module);
            createVariablesOldCommand.Name = "CreateVariablesOld";
            createVariablesOldCommand.Parameters.Add(new CommandParameter() { Name = "Count", ParameterType = DataTypeIds.UInt32 });
            createVariablesOldCommand.Parameters.Add(new CommandParameter() { Name = "StringIds", ParameterType = DataTypeIds.Boolean });
            m_commands.Add(createVariablesOldCommand);
            RegisterCommand(createVariablesOldCommand, externalReferences);

            StartVariableChangeCommand startVariableChangeCommand = new StartVariableChangeCommand(m_module);
            startVariableChangeCommand.Name = "StartVariableChange";
            startVariableChangeCommand.Parameters.Add(new CommandParameter() { Name = "StartIndex", ParameterType = DataTypeIds.UInt32 });
            startVariableChangeCommand.Parameters.Add(new CommandParameter() { Name = "EndIndex", ParameterType = DataTypeIds.UInt32 });
            startVariableChangeCommand.Parameters.Add(new CommandParameter() { Name = "StringIds", ParameterType = DataTypeIds.Boolean });
            startVariableChangeCommand.Parameters.Add(new CommandParameter() { Name = "Pattern", ParameterType = DataTypeIds.String });
            startVariableChangeCommand.Parameters.Add(new CommandParameter() { Name = "RepeatCount", ParameterType = DataTypeIds.UInt32 });
            startVariableChangeCommand.Parameters.Add(new CommandParameter() { Name = "Interval", ParameterType = DataTypeIds.UInt32 });
            startVariableChangeCommand.Parameters.Add(new CommandParameter() { Name = "Increment", ParameterType = DataTypeIds.Double });
            m_commands.Add(startVariableChangeCommand);
            RegisterCommand(startVariableChangeCommand, externalReferences);

            StopVariableChangeCommand stopVariableCommand = new StopVariableChangeCommand(m_module);
            stopVariableCommand.Name = "StopVariableChange";
            m_commands.Add(stopVariableCommand);
            RegisterCommand(stopVariableCommand, externalReferences);

            StopVariableChangeByNodeIdCommand svcbniCommand = new StopVariableChangeByNodeIdCommand(m_module);
            svcbniCommand.Name = "StopVariableChangeByNodeId";
            svcbniCommand.Parameters.Add(new CommandParameter() { Name = "StartIndex", ParameterType = DataTypeIds.UInt32 });
            svcbniCommand.Parameters.Add(new CommandParameter() { Name = "EndIndex", ParameterType = DataTypeIds.UInt32 });
            svcbniCommand.Parameters.Add(new CommandParameter() { Name = "StringIds", ParameterType = DataTypeIds.Boolean });
            svcbniCommand.Parameters.Add(new CommandParameter() { Name = "Pattern", ParameterType = DataTypeIds.String });
            m_commands.Add(svcbniCommand);
            RegisterCommand(svcbniCommand, externalReferences);

            DeleteNodeCommand deleteNodeCommand = new DeleteNodeCommand(m_module);
            deleteNodeCommand.Name = "DeleteNode";
            deleteNodeCommand.Parameters.Add(new CommandParameter() { Name = "IsStringId", ParameterType = DataTypeIds.Boolean });
            deleteNodeCommand.Parameters.Add(new CommandParameter() { Name = "NodeIdString", ParameterType = DataTypeIds.String });
            deleteNodeCommand.Parameters.Add(new CommandParameter() { Name = "NodeIdNumeric", ParameterType = DataTypeIds.UInt32 });
            m_commands.Add(deleteNodeCommand);
            RegisterCommand(deleteNodeCommand, externalReferences);

            StatusChangeCommand statusChangeCommand = new StatusChangeCommand(m_module);
            statusChangeCommand.Name = "StatusChange";
            m_commands.Add(statusChangeCommand);
            RegisterCommand(statusChangeCommand, externalReferences);

            ValueChangeCommand valueChangeCommand = new ValueChangeCommand(m_module);
            valueChangeCommand.Name = "ValueChange";
            startVariableChangeCommand.Parameters.Add(new CommandParameter() { Name = "DoubleValue", ParameterType = DataTypeIds.Double });
            m_commands.Add(valueChangeCommand);
            RegisterCommand(valueChangeCommand, externalReferences);

            TimeStampChangeCommand timeStampChangeCommand = new TimeStampChangeCommand(m_module);
            timeStampChangeCommand.Name = "TimeStampChange";
            m_commands.Add(timeStampChangeCommand);
            RegisterCommand(timeStampChangeCommand, externalReferences);

            ArrayValueChangeCommand arrayValueChangeCommand = new ArrayValueChangeCommand(m_module);
            arrayValueChangeCommand.Name = "ArrayValueChange";
            arrayValueChangeCommand.Parameters.Add(new CommandParameter() { Name = "DoubleValue", ParameterType = DataTypeIds.Double });
            m_commands.Add(arrayValueChangeCommand);
            RegisterCommand(arrayValueChangeCommand, externalReferences);

            ArrayChangeCommand arrayChangeCommand = new ArrayChangeCommand(m_module);
            arrayChangeCommand.Name = "ArrayChange";
            m_commands.Add(arrayChangeCommand);
            RegisterCommand(arrayChangeCommand, externalReferences);

            GetTestVariablesNode(externalReferences);
        }

        #endregion

        #region Init

        public void Init(SimulationModule module)
        {
            m_module = module;
        }

        #endregion

        #region DeleteNode

        public void DeleteNode(NodeId nodeId)
        {
            base.DeleteNode(SystemContext, nodeId);
        }

        #endregion
    }
}