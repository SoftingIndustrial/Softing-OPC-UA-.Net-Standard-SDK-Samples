
using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class DeleteNodeCommand : SimulationModuleCommand
    {
        #region Constructors

        public DeleteNodeCommand(SimulationModule module) : base(module)
        { }

        #endregion

        #region Execute

        public override ServiceResult Execute()
        {
            ServiceResult result = StatusCodes.Good;
            try
            {
                bool isStringIdsVariable = ParameterVariables[Parameters[0].Name].GetBoolValue();
                NodeId nodeId = null;
                if (isStringIdsVariable)
                {
                    string nodeIdStringVariable = ParameterVariables[Parameters[1].Name].GetStringValue();
                    nodeId = new NodeId(nodeIdStringVariable, 2);
                }
                else
                {

                    uint nodeIdNumericVariable = ParameterVariables[Parameters[2].Name].GetUIntValue();
                    nodeId = new NodeId(nodeIdNumericVariable, 2);
                }

                (Module as SimulationModule).DeleteNode(nodeId);
            }
            catch
            {
                result = StatusCodes.Bad;
            }

            return result;
        }

        #endregion
    }
}