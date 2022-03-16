/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Opc.Ua;
using System;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an <see cref="OffNormalAlarmState"/> attached.
    /// </summary>
    internal class OffNormalAlarmMonitor : BaseAlarmMonitor
    {

        #region Private Members

        protected OffNormalAlarmState m_alarm;

        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of <see cref="OffNormalAlarmMonitor"/>
        /// </summary>
        /// <param name="alarmsNodeManager"></param>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        public OffNormalAlarmMonitor(
            AlarmsNodeManager alarmsNodeManager,
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue)
             : base(context, parent, namespaceIndex, name, initialValue, alarmsNodeManager)
        {
            BaseDataVariableState normalValueVariable = alarmsNodeManager.CreateVariable<double>(this, "NormalValueVariable");
            normalValueVariable.Value = initialValue;

            // Attach the alarm monitor.
            InitializeAlarmMonitor(
                context,
                parent,
                namespaceIndex,
                alarmName,
                initialValue,
                normalValueVariable);

            m_alarm.OnAcknowledge += AlarmMonitor_OnAcknowledge;
        }

        #endregion

        #region Virtual Methods
        /// <summary>
        /// Create an instance of the appropriate alarm state to be used by current monitor
        /// </summary>
        /// <returns></returns>
        protected virtual OffNormalAlarmState GetInstanceOfAlarmState()
        {
            return new OffNormalAlarmState(this);
        }

        #endregion

        #region Base Class Overrides

        /// <summary>
        /// Hendle the Variable value change
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        protected override void ProcessVariableChanged(ISystemContext context, object value)
        {
            BaseVariableState normalValVar = (BaseVariableState) m_alarmsNodeManager.FindNodeInAddressSpace(m_alarm.NormalState.Value);
            object normalValue = normalValVar.Value;            

            try
            {
                double? dValue = Convert.ToDouble(value);
                double? dNormalValue = Convert.ToDouble(normalValue);

                bool offNormal = dValue != dNormalValue;

                // Update alarm data
                m_alarm.SetActiveState(context, offNormal);

                // Not interested in disabled or inactive alarms
                if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                {
                    m_alarm.Retain.Value = false;
                }
                else
                {
                    m_alarm.Retain.Value = true;
                }

                if (offNormal)
                {
                    // Set event data
                    m_alarm.EventId.Value = Guid.NewGuid().ToByteArray();
                    m_alarm.Time.Value = DateTime.UtcNow;
                    m_alarm.ReceiveTime.Value = m_alarm.Time.Value;

                    // Reset the acknowledged flag
                    m_alarm.SetAcknowledgedState(context, false);

                    // Report changes to node attributes
                    m_alarm.ClearChangeMasks(context, true);

                    // Check if events are being monitored for the source
                    if (m_alarm.AreEventsMonitored)
                    {
                        // Create a snapshot
                        InstanceStateSnapshot e = new InstanceStateSnapshot();
                        e.Initialize(context, m_alarm);

                        // Report the event
                        m_alarm.ReportEvent(context, e);
                    }
                }
            }
            catch (Exception exception)
            {
                Utils.Trace(exception, "Alarms.{0}.ProcessVariableChanged: Unexpected error processing value changed notification.", m_alarm.GetType());
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the alarm monitor 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="normalValueVariable"></param>
        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            double initialValue,
            BaseDataVariableState normalValueVariable)
        {
            // Create the alarm object
            m_alarm = GetInstanceOfAlarmState();

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // Setup the NormalState
            AddChild(normalValueVariable);
            m_alarm.NormalState.Value = normalValueVariable.NodeId;

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            m_alarm.SetActiveState(context, false);

            // Disable this property 
            m_alarm.LatchedState = null;
        }
        #endregion        
    }
}
