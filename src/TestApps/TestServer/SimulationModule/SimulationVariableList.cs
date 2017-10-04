using Opc.Ua;
using System.Collections.Generic;

namespace TestServer.SimulationModule
{
    public class SimulationVariableList: List<SimulationVariable>
    {
        public BaseInstanceState TestVariablesFolder { get; set; }

	    /*! @brief True if the simulation is running, for this set of variables */
        public bool SimulationIsRunning { get; set; }

	    //SimVarChangeAction m_simAction;

        public uint IntervalMilliSecs { get; set; }

	    /*! @brief  */
        public uint SimulationCylce { get; set; }

	    /*! @brief Cepeat count for simulation */
        public uint SimulationRepeatCount { get; set; }

	    /*! @brief Namespace index */
        public ushort NameSpaceIndex { get; set; }

	    /*! @brief Counts number of simulations (It is being reset when a new startSimulation is called) */
        public uint SimulationNr { get; set; }

	    /*! @brief value to increment the variables.*/
        public double Increment { get; set; }

	    /** @brief information about how the nodeIds were created */
        public NodeIdInfo NodeIdInfo;
    }
}