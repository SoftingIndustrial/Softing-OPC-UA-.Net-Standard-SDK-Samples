using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class StopVariableChangeByNodeIdCommand : SimulationModuleCommand
    {
        #region Constructors

        public StopVariableChangeByNodeIdCommand(SimulationModule module) : base(module)
        { }

        #endregion

        #region Execute

        public override ServiceResult Execute()
        {
            ServiceResult result = StatusCodes.Good;
            try
            {
                uint startIndexVariable = ParameterVariables[Parameters[0].Name].GetUIntValue();
                uint endIndexVariable = ParameterVariables[Parameters[1].Name].GetUIntValue();
                bool stringIdVariable = ParameterVariables[Parameters[2].Name].GetBoolValue();
                string patternVariable = ParameterVariables[Parameters[3].Name].GetStringValue();

                NodeIdType nodeType = stringIdVariable ? NodeIdType.StringNodeId : NodeIdType.NumericNodeId;
                NodeIdInfo nodeIdInfo = new NodeIdInfo()
                {
                    StartIndex = startIndexVariable,
                    EndIndex = endIndexVariable,
                    NodeType = nodeType,
                    Pattern = patternVariable
                };

                (Module as SimulationModule).StopSimulation(nodeIdInfo);
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
