using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class StopVariableChangeCommand : SimulationModuleCommand
    {
        #region Constructors

        public StopVariableChangeCommand(SimulationModule module) : base(module)
        { }

        #endregion

        #region Execute

        public override ServiceResult Execute()
        {
            ServiceResult result = StatusCodes.Good;
            try
            {
                (Module as SimulationModule).StopSimulation();
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
