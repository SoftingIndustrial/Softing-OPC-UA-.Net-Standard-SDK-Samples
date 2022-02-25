/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using Opc.Ua;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an NonExclusiveDeviationAlarm attached.
    /// </summary>
    class NonExclusiveDeviationMonitor : BaseAlarmMonitor
    {
        #region Private Members

        private NonExclusiveDeviationAlarmState m_alarm;

        #endregion

        #region Constructors
        public NonExclusiveDeviationMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
            : base(context, parent, namespaceIndex, name, initialValue)
        {
            // Attach the alarm monitor.
            InitializeAlarmMonitor(
                context,
                parent,
                namespaceIndex,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit);

            m_alarm.OnAcknowledge += AcknowledgeableConditionMonitor.AlarmMonitor_OnAcknowledge;
        }
        #endregion

        #region Public Methods

        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create the alarm object
            m_alarm = new NonExclusiveDeviationAlarmState(this);

            // Declare limit components
            m_alarm.HighHighLimit = new PropertyState<double>(m_alarm);
            m_alarm.HighLimit = new PropertyState<double>(m_alarm);
            m_alarm.LowLimit = new PropertyState<double>(m_alarm);
            m_alarm.LowLowLimit = new PropertyState<double>(m_alarm);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            // Set state values
            m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
            m_alarm.SetSuppressedState(context, false);
            m_alarm.SetActiveState(context, false);

            // Set Setpoint node
            m_alarm.SetpointNode.Value = NodeId;
            // optional
            m_alarm.BaseSetpointNode.Value = NodeId;
            
            // Define limit values
            m_alarm.HighLimit.Value = highLimit;
            m_alarm.HighHighLimit.Value = highHighLimit;
            m_alarm.LowLimit.Value = lowLimit;
            m_alarm.LowLowLimit.Value = lowLowLimit;
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

                if (m_alarm.LowLowLimit != null && m_alarm.LowLowState.Id.Value == false //ObjectIds.ExclusiveLimitStateMachineType_LowLow
                    && newValue <= m_alarm.LowLowLimit.Value)
                {
                    m_alarm.LowLowState.Id.Value = true;

                    m_alarm.SetLimitState(context, LimitAlarmStates.LowLow);
                    m_alarm.SetComment(context, new LocalizedText("en-US", "LowLowLimit exceeded."), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", m_alarm.LowLowState.Value.Text));
                    m_alarm.SetSeverity(context, EventSeverity.Max);

                    updateRequired = true;
                }
                else if (m_alarm.LowLimit != null && m_alarm.LowState.Id.Value == false // ObjectIds.ExclusiveLimitStateMachineType_Low
                         && newValue > m_alarm.LowLowLimit.Value
                         && newValue <= m_alarm.LowLimit.Value)
                {
                    m_alarm.LowState.Id.Value = true;

                    m_alarm.SetLimitState(context, LimitAlarmStates.Low);
                    m_alarm.SetComment(context, new LocalizedText("en-US", "LowLimit exceeded."), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State Low set to {0}", m_alarm.LowState.Value.Text));
                    m_alarm.SetSeverity(context, EventSeverity.High);

                    updateRequired = true;
                }
                else if (m_alarm.HighHighLimit != null && m_alarm.HighHighState.Id.Value == false // ObjectIds.ExclusiveLimitStateMachineType_HighHigh
                         && newValue >= m_alarm.HighHighLimit.Value)
                {
                    m_alarm.HighState.Id.Value = true;
                    m_alarm.HighHighState.Id.Value = true;

                    m_alarm.SetLimitState(context, LimitAlarmStates.HighHigh);
                    m_alarm.SetComment(context, new LocalizedText("en-US", "HighHighLimit exceeded."), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State HighHigh & High set to: {0} & {1}", m_alarm.HighHighState.Value.Text, m_alarm.HighState.Value.Text));
                    m_alarm.SetSeverity(context, EventSeverity.Max);

                    updateRequired = true;
                }
                else if (m_alarm.HighLimit != null && m_alarm.HighState.Id.Value == false // ObjectIds.ExclusiveLimitStateMachineType_High
                         && newValue < m_alarm.HighHighLimit.Value
                         && newValue >= m_alarm.HighLimit.Value)
                {
                    m_alarm.HighState.Id.Value = true;
                    m_alarm.HighHighState.Id.Value = true;

                    m_alarm.SetLimitState(context, LimitAlarmStates.High);
                    m_alarm.SetComment(context, new LocalizedText("en-US", "HighLimit exceeded."), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State High & HighHigh set to: {0} & {1}", m_alarm.HighState.Value.Text, m_alarm.HighHighState.Value.Text));
                    m_alarm.SetSeverity(context, EventSeverity.High);
                    updateRequired = true;
                }
                else if (m_alarm.ActiveState.Id.Value != false
                         && m_alarm.LowLimit != null && newValue > m_alarm.LowLimit.Value
                         && m_alarm.HighLimit != null && newValue < m_alarm.HighLimit.Value)
                {
                    m_alarm.LowState.Id.Value = false;
                    m_alarm.LowLowState.Id.Value = false;
                    m_alarm.HighState.Id.Value = false;
                    m_alarm.HighHighState.Id.Value = false;

                    m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
                    m_alarm.SetComment(context, new LocalizedText("en-US", "Alarm inactive."), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", LimitAlarmStates.Inactive));
                    m_alarm.SetSeverity(context, EventSeverity.Low);

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

                    bool nonActiveState = newValue > m_alarm.LowLimit.Value && newValue < m_alarm.HighLimit.Value;
                    m_alarm.SetActiveState(context, !nonActiveState);

                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    if (m_alarm.LowLowState.Id.Value)
                    {
                        m_alarm.LowState.Id.Value = false;
                        m_alarm.HighState.Id.Value = false;
                        m_alarm.HighHighState.Id.Value = false;
                    }
                    else if (m_alarm.LowState.Id.Value)
                    {
                        m_alarm.LowLowState.Id.Value = false;
                        m_alarm.HighState.Id.Value = false;
                        m_alarm.HighHighState.Id.Value = false;
                    }
                    else if (m_alarm.HighState.Id.Value && m_alarm.HighHighState.Id.Value)
                    {
                        m_alarm.LowState.Id.Value = false;
                        m_alarm.LowLowState.Id.Value = false;
                    }
                    else if (m_alarm.HighState.Id.Value || m_alarm.HighHighState.Id.Value)
                    {
                        m_alarm.HighState.Id.Value = true;
                        m_alarm.HighHighState.Id.Value = true;

                        m_alarm.LowState.Id.Value = false;
                        m_alarm.LowLowState.Id.Value = false;
                    }

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
                        ReportEvent(context, e);
                    }
                }
            }
            catch (Exception exception)
            {
                Utils.Trace(exception, "Alarms.NonExclusiveLevelMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }
        #endregion
    }
}
