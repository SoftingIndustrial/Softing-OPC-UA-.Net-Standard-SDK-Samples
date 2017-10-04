using System;
using System.Collections.Generic;
using System.Timers;
using Opc.Ua;

namespace TestServer.TestModule
{
    class SimulationFolder
    {
        #region Constructors
        public SimulationFolder(NodeState parent, TestModuleNodeManager testModule, uint itemCount, BuiltInType itemType, bool isAnalogItem)          
        {
            // Add Start/Stop simulation command
            m_startStopCmd = testModule.CreateVariable(parent, parent.BrowseName.Name + "/StartStopSimulationCommand", "StartStopSimulationCommand", BuiltInType.Boolean, ValueRanks.Scalar);
            m_startStopCmd.OnSimpleWriteValue = OnStartStopSimulation;
            // Add Change Interval Param
            m_changeIntervalParam = testModule.CreateVariable(m_startStopCmd, parent.BrowseName.Name + "/ChangeInterval", "ChangeInterval", BuiltInType.UInt32, ValueRanks.Scalar);
            // Add Repeat Count Param
            m_repeatCountParam = testModule.CreateVariable(m_startStopCmd, parent.BrowseName.Name + "/RepeatCount", "RepeatCount", BuiltInType.UInt32, ValueRanks.Scalar);
            // Add Increment Param
            m_incrementParam = testModule.CreateVariable(m_startStopCmd, parent.BrowseName.Name + "/Increment", "Increment", BuiltInType.UInt32, ValueRanks.Scalar);

            // Add delete Items command
            m_deleteItemsCmd = testModule.CreateVariable(parent, parent.BrowseName.Name + "/DeleteItemsCommand", "DeleteItemsCommand", BuiltInType.Boolean, ValueRanks.Scalar);
            m_deleteItemsCmd.OnSimpleWriteValue = OnDeleteItems;


            // Create simulation variables
            m_simulationVariables = new List<DataItemState>();

            for (uint i = 0; i < itemCount; i++)
            {
                DataItemState simulationVariable = testModule.CreateVariable(parent, String.Format("{0}/TV_{1}",parent.BrowseName.Name, i), String.Format("TV_{0}",i), itemType, ValueRanks.Scalar);
                m_simulationVariables.Add(simulationVariable); 
            }            
        }
        #endregion

        #region Private Methods
        
        protected ServiceResult OnStartStopSimulation(
            ISystemContext context,
            NodeState node,
            ref object value)
        {
            try
            {
                bool startCommand = (bool)value;

                if (startCommand)
                {
                    // Execute Start simulation command
                    return  StartSimulation();
                }
                else
                {
                    // Execute Stop simulation command
                    return StopSimulation();
                }
            }
            catch (Exception e)
            {
                // Error
                return ServiceResult.Create(e, StatusCodes.Bad, "Start/Stop Simulation error!");
            }
        }

        public StatusCode StartSimulation()
        {
            try
            {
                // Get param values
                uint changeInterval = (uint)m_changeIntervalParam.Value;
                uint repeatCount = (uint)m_repeatCountParam.Value;
                uint increment = (uint)m_incrementParam.Value;
                

                // Check if all parameters are set
                if (changeInterval != 0 && increment != 0)
                {
                    // set simulation parameters
                    m_simulationInterval = changeInterval;
                    m_repeatCount = repeatCount;
                    m_increment = increment;
                    m_continousSimulation = (repeatCount == 0);

                    // Set timer
                    if (m_simulationTimer == null)
                    {
                        m_simulationTimer = new Timer();
                        m_simulationTimer.Elapsed += new ElapsedEventHandler(SimulateValues);
                    }

                    // Start simulation                    
                    m_simulationTimer.Stop();
                    m_simulationTimer.Interval = (double)m_simulationInterval;
                    m_simulationTimer.Start();

                    return StatusCodes.Good;
                }
                else
                {
                    // Parameters not set correctly
                    return StatusCodes.Bad;
                }                
            }
            catch
            {
                return StatusCodes.Bad;
            }
        }

        public StatusCode StopSimulation()
        {
            try
            {
                // Stop items simulation
                if (m_simulationTimer != null)
                {
                    // Destroy the simulation timer
                    m_simulationTimer.Stop();
                    m_simulationTimer.Elapsed -= new ElapsedEventHandler(SimulateValues);
                    m_simulationTimer.Dispose();
                    m_simulationTimer = null;

                    m_continousSimulation = false;
                }

                return StatusCodes.Good;
            }
            catch
            {
                return StatusCodes.Bad;
            }
        }

        /// <summary>
        /// Simulate a value change for the items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimulateValues(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (m_repeatCount > 0 || m_continousSimulation)
                {
                    // increment values
                    foreach (DataItemState item in m_simulationVariables)
                    {
                        IncrementItemValue(item);
                    }

                    if (!m_continousSimulation)
                    {
                        m_repeatCount--;
                    }
                }
                else
                {
                    StopSimulation();
                }
            }
            catch
            {
                // stop simulation
                m_repeatCount = 0;
            }
        }

        private void IncrementItemValue(DataItemState item)
        {
            // get actual value
            object itemValue = item.Value;  //null;
            Type dataType = itemValue.GetType();
            object newValue = null;

            if (dataType == typeof(bool)) newValue = !(bool)itemValue;
            if (dataType == typeof(Byte)) newValue = (Byte)((Byte)itemValue + m_increment);
            if (dataType == typeof(SByte)) newValue = (SByte)((SByte)itemValue + m_increment);
            if (dataType == typeof(UInt16)) newValue = (UInt16)((UInt16)itemValue + m_increment);
            if (dataType == typeof(Int16)) newValue = (Int16)((Int16)itemValue + m_increment);
            if (dataType == typeof(UInt32)) newValue = (UInt32)((UInt32)itemValue + m_increment);
            if (dataType == typeof(Int32)) newValue = (Int32)((Int32)itemValue + m_increment);
            if (dataType == typeof(UInt64)) newValue = (UInt64)((UInt64)itemValue + m_increment);
            if (dataType == typeof(Int64)) newValue = (Int64)((Int64)itemValue + m_increment);
            if (dataType == typeof(Single)) newValue = (Single)((Single)itemValue + m_increment);
            if (dataType == typeof(Double)) newValue = (Double)((Double)itemValue + m_increment);

            // apply new value
            item.Value = newValue;
            item.Timestamp = DateTime.Now;
            item.StatusCode = StatusCodes.Good;
            item.ClearChangeMasks(null, false);
        }

        protected ServiceResult OnDeleteItems(
            ISystemContext context,
            NodeState node,
            ref object value)
        {
            try
            {
                // Stop simulation
                StopSimulation();

                // Delete simulation folder
                TestModuleNodeManager testModule = ApplicationModule.Instance.GetNodeManager<TestModuleNodeManager>();
                if (testModule != null)
                {
                    testModule.DeleteNode(testModule.SystemContext, m_startStopCmd.Parent.NodeId);
                }

                return StatusCodes.Good;
            }
            catch (Exception e)
            {
                // Error
                return ServiceResult.Create(e, StatusCodes.Bad, "Delete Items error!");
            }
        }

        #endregion


        #region Private Members


        private List<DataItemState> m_simulationVariables;
        private DataItemState m_startStopCmd;
        private DataItemState m_changeIntervalParam;
        private DataItemState m_repeatCountParam;
        private DataItemState m_incrementParam;

        private DataItemState m_deleteItemsCmd;

        private Timer m_simulationTimer;
        private uint m_simulationInterval = 0;
        private uint m_repeatCount = 0;
        private uint m_increment = 0;
        private bool m_continousSimulation = false;
        
        #endregion
    }
}