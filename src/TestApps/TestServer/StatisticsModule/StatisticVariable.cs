using Opc.Ua;

namespace TestServer.StatisticsModule
{
    public class StatisticVariable : ServerVariable
    {
        public StatisticVariable(Module module, NodeState parent) : base(module, parent)
        {
        }
    }
}