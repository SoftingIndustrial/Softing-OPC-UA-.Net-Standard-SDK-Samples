using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class CreateVariablesOldCommand : SimulationModuleCommand
    {
        #region Constructors

        public CreateVariablesOldCommand(SimulationModule module) : base(module)
        { }

        #endregion

        #region Execute

        public override ServiceResult Execute()
        {
            ServiceResult result = StatusCodes.Good;
            try
            {
                uint countVariable = ParameterVariables[Parameters[0].Name].GetUIntValue();
                bool stringIdsVariable = ParameterVariables[Parameters[1].Name].GetBoolValue();

                (Module as SimulationModule).CreateTestVariablesOld(countVariable, stringIdsVariable);
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