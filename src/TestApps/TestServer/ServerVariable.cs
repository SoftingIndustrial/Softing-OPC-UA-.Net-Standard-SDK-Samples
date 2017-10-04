using Opc.Ua;

namespace TestServer
{
    public class ServerVariable : DataItemState
    {
        #region Private

        private Module m_module;

        #endregion

        #region Constructors
        public ServerVariable(Module module, NodeState parent) : base(parent)
        {
            m_module = module;
        }

        #endregion

        #region Properties

        public string Name
        { get; set; }

        internal Module Module
        {
            get { return m_module; }
        }

        #endregion
    }
}