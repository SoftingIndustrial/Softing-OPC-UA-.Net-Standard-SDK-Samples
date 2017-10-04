using System.Collections.Generic;

namespace TestServer
{
    public abstract class ServerCommand
    {
        #region Private

        private List<CommandParameter> m_parameters = new List<CommandParameter>();
        private Module m_module;

        #endregion

        #region Constructors

        public ServerCommand(Module module)
        {
            m_module = module;
        }

        #endregion

        #region Properties

        internal List<CommandParameter> Parameters
        {
            get { return m_parameters; }
        }

        internal Module Module
        {
            get { return m_module; }
        }

        #endregion
    }
}