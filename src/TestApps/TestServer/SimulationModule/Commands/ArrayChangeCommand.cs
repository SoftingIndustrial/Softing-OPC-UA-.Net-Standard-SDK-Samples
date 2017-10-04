using Opc.Ua;

namespace TestServer.SimulationModule.Commands
{
    public class ArrayChangeCommand : SimulationModuleCommand
    {
        #region Ctors

        public ArrayChangeCommand(SimulationModule module) : base(module)
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