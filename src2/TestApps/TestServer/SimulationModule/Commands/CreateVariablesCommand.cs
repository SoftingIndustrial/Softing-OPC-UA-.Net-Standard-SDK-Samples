using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class CreateVariablesCommand : SimulationModuleCommand
    {
        #region Constructors

        public CreateVariablesCommand(SimulationModule module) : base(module)
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
                uint nodeIdTypeVariable = ParameterVariables[Parameters[2].Name].GetUIntValue();
                string patternVariable = ParameterVariables[Parameters[3].Name].GetStringValue();
                uint varTypeVariable = ParameterVariables[Parameters[4].Name].GetUIntValue();

                NodeIdInfo nodeIdInfo = new NodeIdInfo()
                {
                    StartIndex = startIndexVariable,
                    EndIndex = endIndexVariable,
                    NodeType = (NodeIdType)nodeIdTypeVariable,
                    Pattern = patternVariable,
                    VariableDataType = (VarType)varTypeVariable
                };

                (Module as SimulationModule).CreateAllTestVariables(nodeIdInfo);
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