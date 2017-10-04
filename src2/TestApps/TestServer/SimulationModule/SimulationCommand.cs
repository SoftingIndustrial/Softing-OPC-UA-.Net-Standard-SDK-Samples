namespace TestServer.SimulationModule
{
    public class SimulationCommand : ServerCommand
    {
        #region Constructors

        public SimulationCommand(SimulationModule module) : base(module)
        { }

        #endregion

        #region Properties

        public string Name { get; set; }

        #endregion
    }
}