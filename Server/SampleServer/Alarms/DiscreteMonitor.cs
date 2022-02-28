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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an <see cref="DiscreteAlarmState"/> attached.
    /// </summary>
    internal class DiscreteMonitor : BaseAlarmMonitor
    {
        #region Private Members

        private DiscreteAlarmState m_alarm;
        double? m_value = 0;
        #endregion

        #region Constructors
        public DiscreteMonitor(ISystemContext context, NodeState parent, ushort namespaceIndex, string name, string alarmName, double initialValue)
           : base(context, parent, namespaceIndex, name, initialValue)
        {
            // Attach the alarm monitor.
            InitializeAlarmMonitor(
                context,
                parent,
                namespaceIndex,
                alarmName,
                initialValue);

            m_alarm.OnAcknowledge += AlarmMonitor_OnAcknowledge;
        }

        #endregion

        /// <summary>
        /// Initialize the alarm monitor 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void InitializeAlarmMonitor(
           ISystemContext context,
           NodeState parent,
           ushort namespaceIndex,
           string alarmName,
           double initialValue)
        {
            // Create the alarm object
            m_alarm = new DiscreteAlarmState(this);

            base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            m_alarm.SetActiveState(context, false);

            // Disable this property 
            m_alarm.LatchedState = null;
        }

        protected override void ProcessVariableChanged(ISystemContext context, object value)
        {
            try
            {
                string currentUserId = string.Empty;
                IOperationContext operationContext = context as IOperationContext;

                if (operationContext != null && operationContext.UserIdentity != null)
                {
                    currentUserId = operationContext.UserIdentity.DisplayName;
                }

                double? newValue = Convert.ToDouble(value);

                bool updateRequired = false;

                if (m_value != newValue)
                {
                    m_value = newValue;
                    updateRequired = true;
                }

                if (updateRequired)
                {
                    // Set event data
                    m_alarm.EventId.Value = Guid.NewGuid().ToByteArray();
                    m_alarm.Time.Value = DateTime.UtcNow;
                    m_alarm.ReceiveTime.Value = m_alarm.Time.Value;

                    m_alarm.ConditionClassId.Value = ObjectTypeIds.BaseConditionClassType;
                    m_alarm.ConditionClassName.Value = new LocalizedText("BaseConditionClassType");
                    m_alarm.BranchId.Value = new NodeId();

                    // Generate alarm if number is even
                    //bool generateEvent = newValue % 2 == 0;
                    ////m_alarm.SetEnableState(context, generateEvent);
                    //m_alarm.SetActiveState(context, generateEvent);

                    // Generate alarm if number is even
                    bool activeState = newValue % 2 == 0;
                    m_alarm.SetActiveState(context, activeState);

                    // Bring back AcknowledgedState and ConfirmedState
                    if (m_alarm.AckedState.Id.Value && activeState)
                    {
                        m_alarm.SetAcknowledgedState(context, false);
                        m_alarm.SetConfirmedState(context, false);
                    }

                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    m_alarm.SetComment(context, new LocalizedText("en-US", String.Format("Alarm AckedState = {0}", m_alarm.AckedState.Value.Text)), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm AckedState = {0}", m_alarm.AckedState.Value.Text));
                    m_alarm.SetSeverity(context, 0);

                    // Report changes to node attributes
                    m_alarm.ClearChangeMasks(context, true);

                    // Check if events are being monitored for the source
                    if (m_alarm.AreEventsMonitored)
                    {
                        // Create a snapshot
                        InstanceStateSnapshot e = new InstanceStateSnapshot();
                        e.Initialize(context, m_alarm);

                        // Report the event
                        ReportEvent(context, e);
                    }
                }
            }
            catch (Exception exception)
            {
                Utils.Trace(exception, "Alarms.AlarmConditionMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }
    }
}
