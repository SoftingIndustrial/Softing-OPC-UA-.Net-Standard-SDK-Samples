using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class ValueChangeCommand : SimulationModuleCommand
    {
        #region Constructors

        public ValueChangeCommand(SimulationModule module) : base(module)
        { }

        #endregion

        #region Execute

        public override ServiceResult Execute()
        {
            return StatusCodes.Good;
        }

        #endregion
    }
}
