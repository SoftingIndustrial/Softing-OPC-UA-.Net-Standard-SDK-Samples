using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class ArrayValueChangeCommand : SimulationModuleCommand
    {
        #region Constructors

        public ArrayValueChangeCommand(SimulationModule module) : base(module)
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