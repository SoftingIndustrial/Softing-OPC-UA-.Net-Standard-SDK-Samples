using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;

namespace TestServer.StatisticsModule
{
    public class StatisticNodeManager : TestNodeManager
    {
        #region Private Fields

        private StatisticModule m_module;
        private TestServerConfiguration m_configuration;
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public StatisticNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.TestServer)
        {
            SystemContext.NodeIdFactory = this;

            //get the configuration for the node manager
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

        #region Properties

        public StatisticModule StatisticModule
        {
            get { return m_module; }
        }

        #endregion

        #region Init

        public void Init(StatisticModule module)
        {
            m_module = module;
            m_module.Init();
        }

        #endregion

        #region Override
        
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
            base.CreateAddressSpace(externalReferences);

            // get the root node
            BaseObjectState root = GetStatisticNode(externalReferences);

            StatisticFolder readFolder = CreateStatisticFolder("Read");
            RegisterNode(readFolder, root);
            m_module.ReadFolder = readFolder;

            StatisticFolder writeFolder = CreateStatisticFolder("Write");
            RegisterNode(writeFolder, root);
            m_module.WriteFolder = writeFolder;

            StatisticFolder dataChangeFolder = CreateStatisticFolder("DataChange");
            RegisterNode(dataChangeFolder, root);
            m_module.DataChangeFolder = dataChangeFolder;

            StatisticFolder simulationChangeFolder = CreateStatisticFolder("SimulationChange");
            RegisterNode(simulationChangeFolder, root);
            m_module.SimulationChangeFolder = simulationChangeFolder;

            MethodState startMethod = CreateMethodNode("Start");
            startMethod.Description = "Starts the statistics module.";
            
            // add the input arguments.
            PropertyState<Argument[]> inputArguments = new PropertyState<Argument[]>(startMethod);

            inputArguments.NodeId = GenerateNodeId("StatisticModuleStart_InputArguments");
            inputArguments.BrowseName = new QualifiedName(BrowseNames.InputArguments);
            inputArguments.DisplayName = inputArguments.BrowseName.Name;
            inputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
            inputArguments.DataType = DataTypeIds.Argument;
            inputArguments.ValueRank = ValueRanks.OneDimension;
            inputArguments.MinimumSamplingInterval = MinimumSamplingIntervals.Continuous;
            inputArguments.AccessLevel = AccessLevels.CurrentRead;
            inputArguments.UserAccessLevel = AccessLevels.CurrentRead;
            inputArguments.Historizing = false;
            inputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;

            inputArguments.Value = new Argument[] 
            {
                new Argument() { Name = "Id", Description = "Time in millisecs of statistic interval (accepted from 100 to 1000000)",  DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar }
            };

            startMethod.InputArguments = inputArguments;

            startMethod.OnCallMethod = OnStart;
            RegisterNode(startMethod, root);

            MethodState stopMethod = CreateMethodNode("Stop");
            stopMethod.Description = "Stops the statistics module.";
            stopMethod.OnCallMethod = OnStop;
            RegisterNode(stopMethod, root);

            MethodState resetMethod = CreateMethodNode("Reset");
            resetMethod.Description = "Resets the statistics module.";
            resetMethod.OnCallMethod = OnReset;
            RegisterNode(resetMethod, root);
        }

        #endregion

        #region Private Methods

        private StatisticFolder CreateStatisticFolder(string nodeName)
        {
            //StatisticFolder statisticNode = new StatisticFolder(m_module, null);
            StatisticFolder statisticNode = new StatisticFolder(null);
            //statisticNode.Name = folderName;

            statisticNode.NodeId = GenerateNodeId(nodeName);
            statisticNode.BrowseName = new QualifiedName(nodeName, NamespaceIndex);
            statisticNode.DisplayName = new LocalizedText("de", statisticNode.BrowseName.Name);
            statisticNode.Description = String.Empty;
            
            statisticNode.AvgVar    = CreateStatisticVariable(statisticNode, "Avg");
            statisticNode.MinVar    = CreateStatisticVariable(statisticNode, "Min");
            statisticNode.MaxVar    = CreateStatisticVariable(statisticNode, "Max");
            statisticNode.ArrayVar  = CreateStatisticVariable(statisticNode, "Array");
            statisticNode.LastVar   = CreateStatisticVariable(statisticNode, "Last");

            return statisticNode ;
        }

        private StatisticVariable CreateStatisticVariable(BaseInstanceState parent, string variableName)
        {
            StatisticVariable variableNode = new StatisticVariable(m_module, null);
            variableNode.Name = String.Format("{0}.{1}", parent.DisplayName, variableName);

            variableNode.NodeId = GenerateNodeId(variableNode.Name);
            variableNode.BrowseName = new QualifiedName(variableName, NamespaceIndex);
            //variableNode.DisplayName = new LocalizedText("de", variableNode.BrowseName.Name);
            variableNode.DisplayName = new LocalizedText("de", variableNode.Name);

            variableNode.Description = string.Empty;
            variableNode.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variableNode.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variableNode.DataType = DataTypeIds.Double;
            variableNode.ValueRank = ValueRanks.Scalar;
            variableNode.Value = 0.0;
            variableNode.StatusCode = StatusCodes.Good;
            variableNode.Timestamp = DateTime.Now;

            RegisterNode(variableNode, parent);

            return variableNode;
        }

        private MethodState CreateMethodNode(string methodName)
        {
            MethodState methodNode = new MethodState(null);

            methodNode.NodeId = GenerateNodeId(String.Format("StatisticModule{0}", methodName));
            methodNode.BrowseName = new QualifiedName(methodName, NamespaceIndex);
            methodNode.DisplayName = new LocalizedText("de", methodNode.BrowseName.Name);
            methodNode.Description = String.Empty;
            methodNode.Executable = true;
            methodNode.UserExecutable = true;
            methodNode.ReferenceTypeId = ReferenceTypeIds.HasComponent;

            return methodNode;
        }
        
        #region Method Calls

        /// <summary>
        /// Handles the Start method.
        /// </summary>
        protected ServiceResult OnStart(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            ServiceResult result = null;
            uint interval = 1000;

            switch (inputArguments.Count)
            {
                case 0: // use default defined above 

                    m_module.StartStatistics(interval);
                    result = StatusCodes.Good;

                    break;
                case 1:
                    interval = Convert.ToUInt32(inputArguments[0]);
                    // we do not allow intervals les than 100 ms or greater than 1.000.000 ms! 
                    if (interval < 100 || interval > 1000000)
                    {
                        result = StatusCodes.BadInvalidArgument;
                    }
                    else
                    {
                        m_module.StartStatistics(interval);
                        result = StatusCodes.Good;
                    }
                    break;
                default: //invalid Args count
                    result = StatusCodes.BadInvalidArgument;
                    break;
            }

            return result;
        }


        /// <summary>
        /// Handles the Stop method.
        /// </summary>
        protected ServiceResult OnStop(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            ServiceResult result = null;

            m_module.StopStatistics();

            result = StatusCodes.Good;
            return result;
        }


        /// <summary>
        /// Handles the Reset method.
        /// </summary>
        protected ServiceResult OnReset(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            ServiceResult result = null;

            m_module.ResetStatisticValues();

            result = StatusCodes.Good;

            return result;
        }

        #endregion

        #endregion
    }
}