using Opc.Ua;
using Opc.Ua.Server;

namespace TestServer
{
    public abstract class Module
    {
        public abstract INodeManager GetNodeManager(IServerInternal server, ApplicationConfiguration configuration);
    }
}