using Opc.Ua;

namespace TestServer
{
    public class CommandParameter
    {
        public string Name
        { get; set; }

        public NodeId ParameterType
        { get; set; }
    }
}