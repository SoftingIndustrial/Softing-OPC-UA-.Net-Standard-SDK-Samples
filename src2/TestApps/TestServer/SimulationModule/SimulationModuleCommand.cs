using Opc.Ua;
using System.Collections.Generic;

namespace TestServer.SimulationModule
{
    public abstract class SimulationModuleCommand : ServerCommand
    {
        #region Private Members

        private Dictionary<string, VariableParameter> m_parameterVariables = new Dictionary<string, VariableParameter>();

        #endregion

        #region Constructors

        public SimulationModuleCommand(SimulationModule module) : base(module)
        { }

        #endregion

        #region Properties
        public string Name  { get; set; }

        #endregion
        
        public abstract ServiceResult Execute();

        internal void AddVariableParameter(string parameterName, VariableParameter variableParameter)
        {
            m_parameterVariables.Add(parameterName, variableParameter);
        }

        protected Dictionary<string, VariableParameter> ParameterVariables
        {
            get { return m_parameterVariables; }
        }
    }
}