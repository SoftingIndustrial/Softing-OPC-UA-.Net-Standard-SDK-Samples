using System;
using System.Collections.Generic;
using System.Threading;
using Opc.Ua.Server;

namespace TestServer
{
	class ApplicationModule
	{
		static ApplicationModule m_instance;
        static List<INodeManager> m_nodeManagers = new List<INodeManager>();

		TimerThread m_timerThread;
		SimVarManager m_simVarManager;
		AutoResetEvent m_stopApplication;
		UInt32 m_stopDelay;

		ApplicationModule()
		{
			m_stopApplication = new AutoResetEvent(false);
			m_timerThread = new TimerThread();
			m_simVarManager = new SimVarManager();
		}

		static public ApplicationModule Instance
		{
			get
            {
                if (m_instance == null)
                {
                    m_instance = new ApplicationModule();
                }

				return m_instance;
			}
		}

		public TimerThread TimerThread
		{
			get
			{
				return m_timerThread;
			}
		}

		public SimVarManager SimVarManager
		{
			get
			{
				return m_simVarManager;
			}
		}

		public AutoResetEvent StopApplicationEvent
		{
			get
			{
				return m_stopApplication;
			}
		}

		public UInt32 StopDelay
		{
			get
			{
				return m_stopDelay;
			}
		}

        public void RegisterNodeManager(INodeManager nodeManager)
        {
            m_nodeManagers.Add(nodeManager);
        }

        public T GetNodeManager<T>() where T : class
        {
            foreach (INodeManager nodeManager in m_nodeManagers)
            {
                if (nodeManager.GetType().Equals(typeof(T)))
                {
                    return nodeManager as T;
                }
            }

            return null;
        }

		public void StopApplication(UInt32 delay)
		{
			m_stopDelay = delay;
			m_stopApplication.Set();
		}
	}
}