
using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class TimeStampChangeCommand : SimulationModuleCommand
    {
        #region Constructors

        public TimeStampChangeCommand(SimulationModule module) : base(module)
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