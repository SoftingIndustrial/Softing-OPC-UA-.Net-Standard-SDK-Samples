using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class StatusChangeCommand : SimulationModuleCommand
    {
        #region Constructors

        public StatusChangeCommand(SimulationModule module) : base(module)
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
