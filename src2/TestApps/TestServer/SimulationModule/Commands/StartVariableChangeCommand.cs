using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class StartVariableChangeCommand : SimulationModuleCommand
    {
        #region Constructors

        public StartVariableChangeCommand(SimulationModule module) : base(module)
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
                uint repeatCountVariable = ParameterVariables[Parameters[4].Name].GetUIntValue();
                uint intervalVariable = ParameterVariables[Parameters[5].Name].GetUIntValue();
                double incrementVariable = ParameterVariables[Parameters[6].Name].GetDoubleValue();

                NodeIdType nodeType = stringIdVariable ? NodeIdType.StringNodeId : NodeIdType.NumericNodeId;
                NodeIdInfo nodeIdInfo = new NodeIdInfo()
                {
                    StartIndex = startIndexVariable,
                    EndIndex = endIndexVariable,
                    NodeType = nodeType,
                    Pattern = patternVariable,
                    RepeatCount = repeatCountVariable,
                    Interval = intervalVariable,
                    Increment = incrementVariable
                };

                (Module as SimulationModule).StartSimulation(nodeIdInfo);
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