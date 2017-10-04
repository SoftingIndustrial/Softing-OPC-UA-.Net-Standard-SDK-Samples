using System;
using System.Collections.Generic;
using Opc.Ua;
using Opc.Ua.Server;

namespace TestServer.ComplexBrowseModule
{
    public class ComplexBrowseNodeManager : TestNodeManager
    {
        #region Private Fields

        private List<ServerVariable> m_variables = new List<ServerVariable>();
        private global::TestServer.ComplexBrowseModule.ComplexBrowseModule m_module;
        private TestServerConfiguration m_configuration;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public ComplexBrowseNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.TestServer)
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

            int nrOfVariables = 10;
            int nrOfChilds = 2;
            Random randomVal = new Random();

            for (int i = 0; i < nrOfVariables; i++)
            {
                ServerVariable variableNode = CreateVariableNode(String.Format("CB Var {0}", i));                
                variableNode.Value = (uint)randomVal.Next(0,100);

                m_variables.Add(variableNode);
                RegisterBrowseVariable(variableNode, externalReferences);

                for(uint j = 1; j<= nrOfChilds; j++)
                {
                    ServerVariable childNode = CreateVariableNode(String.Format("CB Var {0}_child{1}", i, j));
                    childNode.Value = j + 10;

                    RegisterVariable(childNode, externalReferences, variableNode);
                }
            }            
        }

        #endregion

        #region Private Methods

        private ServerVariable CreateVariableNode(string variableName)
        {
            ServerVariable variableNode = new ServerVariable(m_module, null);
            variableNode.Name = variableName;

            variableNode.NodeId = GenerateNodeId(variableNode.Name);
            variableNode.BrowseName = new QualifiedName(variableNode.Name, NamespaceIndex);
            variableNode.DisplayName = new LocalizedText("de", variableNode.BrowseName.Name);

            variableNode.Description = string.Empty;
            variableNode.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variableNode.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variableNode.DataType = DataTypeIds.UInt32;
            variableNode.ValueRank = ValueRanks.Scalar;
            variableNode.Value = (uint)0;
            variableNode.StatusCode = StatusCodes.Good;

            return variableNode;
        }

        #endregion

        #region Init

        public void Init(global::TestServer.ComplexBrowseModule.ComplexBrowseModule module)
        {
            m_module = module;
        }

        #endregion
    }
}