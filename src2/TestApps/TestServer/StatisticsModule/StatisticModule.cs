using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Threading;
using System.Timers;

namespace TestServer.StatisticsModule
{
    public class StatisticModule : Module
    {
        #region Private Fields

        private StatisticNodeManager m_nodeManager;
        
        /// <summary>
        /// True if the statistic thread is running, false if not
        /// </summary>
        private bool m_statisticIsRunning = false;

        /// <summary>
        ///  Read Statistic folder
        /// </summary>
        StatisticFolder m_readFolder;

        /// <summary>
        /// Write Statistic folder
        /// </summary>
        StatisticFolder m_writeFolder;

        /// <summary>
        /// DataChange Statistic folder
        /// </summary>
        StatisticFolder m_dataChangeFolder;

        /// <summary>
        /// SimulationChange Statistic folder
        /// </summary>
        StatisticFolder m_simulationChangeFolder;

        /// <summary>
        /// Statistic thread interval
        /// </summary>
        uint m_intervalMilliSecs = 1000;

        System.Timers.Timer m_statisticTimer;

        #endregion

        public override INodeManager GetNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            if (m_nodeManager == null)
            {
                m_nodeManager = new StatisticNodeManager(server, configuration);
                m_nodeManager.Init(this);
            }

            return m_nodeManager;
        }

        #region Properties

        public StatisticNodeManager NodeManager
        {
            get { return m_nodeManager; }
        }

        private bool StatisticIsRunning
        {
            get { return m_statisticIsRunning; }
        }

        public StatisticFolder ReadFolder
        {
            get 
            {
                return m_readFolder;
            }
            set 
            {
                m_readFolder = value;
            }
        }

        public StatisticFolder WriteFolder
        {
            get
            {
                return m_writeFolder;
            }
            set
            {
                m_writeFolder = value;
            }
        }

        public StatisticFolder DataChangeFolder
        {
            get
            {
                return m_dataChangeFolder;
            }
            set
            {
                m_dataChangeFolder = value;
            }
        }

        public StatisticFolder SimulationChangeFolder
        {
            get
            {
                return m_simulationChangeFolder;
            }
            set
            {
                m_simulationChangeFolder = value;
            }
        }


        #endregion

        #region Public Methods

        public void Init()
        {
            m_statisticIsRunning = false;
            m_statisticTimer = new System.Timers.Timer();
            m_statisticTimer.Enabled = false;
            m_statisticTimer.Elapsed += StatisticsTimerElapsed;
 
        }

        /// <summary>
        /// Starts the statistic operations
        /// </summary>
        /// <param name="interval">Statistic thread sleep interval</param>
        public void StartStatistics(uint interval)
        {
            if (!m_statisticIsRunning)
            {
                m_statisticIsRunning = true;
                // we do not allow intervals les than 100 ms or greater than 1.000.000 ms!
                if (interval > 100 && interval < 1000000)
                {
                    m_intervalMilliSecs = interval;
                }
                else
                {
                    m_intervalMilliSecs = 1000; //use default value (1 sec.)
                }

                m_statisticTimer.Interval = m_intervalMilliSecs;
                m_statisticTimer.Start(); 
            }
        }

        /// <summary>
        /// Stops statistics operations
        /// </summary>
        public void StopStatistics()
        {
            if (m_statisticIsRunning)
            {
                m_statisticIsRunning = false;
                m_statisticTimer.Stop();
            }
        }

        /// <summary>
        /// Resets all: reads, writes and data changes
        /// </summary>
        public void ResetStatisticValues()
        {
            StopStatistics();
            m_readFolder.ResetNrOfReports();
            m_writeFolder.ResetNrOfReports();
            m_dataChangeFolder.ResetNrOfReports();
            m_simulationChangeFolder.ResetNrOfReports();
            StartStatistics(m_intervalMilliSecs);
        }

        /// <summary>
        /// Increments the number of reads
        /// </summary>
        public void RegisterRead()
        {
            m_readFolder.IncrementNrOfReports();
        }

        /// <summary>
        /// Increments the number of writes
        /// </summary>
        public void RegisterWrite()
        {
            m_writeFolder.IncrementNrOfReports();
        }

        /// <summary>
        /// Increments the number of data changes
        /// </summary>
        public void RegisterDataChange()
        {
            m_dataChangeFolder.IncrementNrOfReports();
        }

        /// <summary>
        /// Increments the number of variables changed(value changed)
        /// </summary>
        public void RegisterSimulationChange()
        {
            m_simulationChangeFolder.IncrementNrOfReports();
        }

        #endregion

        #region Private Methods

        private void StatisticsTimerElapsed(object sender, ElapsedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(SetStatistics));
        }

        private void SetStatistics(Object data)
        {
            if (m_statisticIsRunning)
            {
                SetReadValue();
                SetWriteValue();
                SetDataChangeValue();
                SetSimulationChangeValue(); 
            }
        }

        /// <summary>
        ///  Sets the read value(Internal opertaion inside of Read folder)
        /// </summary>
        private void SetReadValue()
        {
            m_readFolder.SetValue();
        }

        /// <summary>
        ///  Sets the write value(Internal opertaion inside of Write folder)
        /// </summary>
        private void SetWriteValue()
        {
            m_writeFolder.SetValue();
        }

        /// <summary>
        /// Sets the data change value(Internal opertaion inside of DataChange folder)
        /// </summary>
        void SetDataChangeValue()
        {
            m_dataChangeFolder.SetValue();
        }

        /// <summary>
        /// Sets the value for the simulation change folder
        /// </summary>
        void SetSimulationChangeValue()
        {
            m_simulationChangeFolder.SetValue();
        }
        
        #endregion
    }
}