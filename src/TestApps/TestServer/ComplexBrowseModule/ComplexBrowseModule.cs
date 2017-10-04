using Opc.Ua;
using Opc.Ua.Server;

namespace TestServer.ComplexBrowseModule
{
    public class ComplexBrowseModule : Module
    {
        private ComplexBrowseNodeManager m_nodeManager;

        public override INodeManager GetNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            if (m_nodeManager == null)
            {
                m_nodeManager = new ComplexBrowseNodeManager(server, configuration);
                m_nodeManager.Init(this);
            }

            return m_nodeManager;
        }
    }
}