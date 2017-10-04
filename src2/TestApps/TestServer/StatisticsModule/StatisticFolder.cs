using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestServer.StatisticsModule
{
    public class StatisticFolder : BaseObjectState
    {
        #region Private Fields

        /// <summary>
        /// Min value
        /// </summary>
        private double m_minValue;

        /// <summary>
        /// Max value
        /// </summary>
        private double m_maxValue;

        /// <summary>
        /// Last value (Current value)
        /// </summary>
        private double m_lastValue;

        /// <summary>
        /// Sum of all measurements
        /// </summary>
        private double m_sumValue;

        /// <summary>
        /// Array for the measured values
        /// </summary>
        private List<double> m_dataArray = new List<double>();

        /// <summary>
        /// Boolean flag when setting the value
        /// </summary>
        private bool m_bInitialCall;

        /// <summary>
        /// Avg statistic variable
        /// </summary>
        private StatisticVariable m_AvgVar;

	    /// <summary>
        /// Min statistic variable
	    /// </summary>
        private StatisticVariable m_MinVar;

	    /// <summary>
        /// Max statistic variable
	    /// </summary>
        private StatisticVariable m_MaxVar;

	    /// <summary>
        /// Array statistic variable
	    /// </summary>
        private StatisticVariable m_ArrayVar;

	    /// <summary>
        /// Last statistic variable
	    /// </summary>
        private StatisticVariable m_LastVar;

	    /// <summary>
        /// Number of reports (reads/writes/datachanges)
	    /// </summary>
        private ulong m_nrOfReports;

	    /// <summary>
        /// Total Number of reports (reads/writes/datachanges)
	    /// </summary>
        private ulong m_totalnrOfReports;

        #endregion

        #region Constructor

        public StatisticFolder(NodeState parent) : base(parent)
        {            
        }

        #endregion

        #region Properties

        public StatisticVariable AvgVar
        {
            get
            {
                return m_AvgVar;
            }
            set
            {
                m_AvgVar = value;
            }
        }

        public StatisticVariable MinVar
        {
            get
            {
                return m_MinVar;
            }
            set
            {
                m_MinVar = value;
            }
        }

        public StatisticVariable MaxVar
        {
            get
            {
                return m_MaxVar;
            }
            set
            {
                m_MaxVar = value;
            }
        }

        public StatisticVariable ArrayVar
        {
            get
            {
                return m_ArrayVar;
            }
            set
            {
                m_ArrayVar = value;
            }
        }

        public StatisticVariable LastVar
        {
            get
            {
                return m_LastVar;
            }
            set
            {
                m_LastVar = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set internally the values for avg, min, max, array and last
        /// </summary>
        public void SetValue()
        {
            double value = m_nrOfReports;
	        m_nrOfReports = 0;

            if (m_dataArray.Count == 0)
            {
                if (m_bInitialCall)
                {
                    m_minValue = m_maxValue = value;
                    m_sumValue = value;
                }
                else
                {
                    m_bInitialCall = true;
                    return;
                }
            }
            else
            {
                m_minValue = Math.Min(m_minValue, value);
                m_maxValue = Math.Max(m_maxValue, value);
                m_sumValue += value;
            }

            if (m_dataArray.Count < 100)
            {
                m_dataArray.Add(value);
            }
            m_lastValue = value;

            //Updating all OPC variables
            UpdateOPCVariables();
        }

        /// <summary>
        /// Increments the reports 
        /// </summary>
        public void IncrementNrOfReports()
        {
            m_nrOfReports++;
            m_totalnrOfReports += m_nrOfReports;
        }

        /// <summary>
        /// Increments the reports
        /// </summary>
        /// <param name="count">Number of new data changes</param>
        public void IncrementNrOfReports(uint count)
        {
            m_nrOfReports = count - m_totalnrOfReports;
            m_totalnrOfReports = count;
        }

        /// <summary>
        /// Resets the reports
        /// </summary>
        public void ResetNrOfReports()
        {
            m_bInitialCall = false;
            m_dataArray.Clear();
            m_minValue = m_maxValue = m_lastValue = m_sumValue = 0;
            m_nrOfReports = 0;
            // also reset the total number of reports(reads/writes/data changes)
            m_totalnrOfReports = 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Update all OPC variables from the folder(Avg, Min, Max, Array, Last)
        /// </summary>
        private void UpdateOPCVariables()
        {
            m_AvgVar.Value = GetAvgValue();
            m_MinVar.Value = m_minValue;
            m_MaxVar.Value = m_maxValue;
            m_LastVar.Value = GetLastValue();

            if (m_dataArray.Count > 0)
            {
                m_ArrayVar.Value = m_dataArray.ToArray();
            } 
        }

        /// <summary>
        /// Getter for the last value
        /// </summary>
        private double GetLastValue()
        {
            if (m_dataArray.Count > 0)
            {
                return m_dataArray.Last();
            }
            return 0;
        }

       /// <summary>
        /// Getter for the average value
       /// </summary>
       /// <returns></returns>
        private double GetAvgValue()
        {
            if (m_dataArray.Count > 0)
            {
                return (double)((double)m_sumValue / (double)m_dataArray.Count);
            }
            return 0;
        }

        #endregion
    }
}