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
    /// A monitored variable with an ExclusiveLevelAlarm attached.
    /// </summary>
    class ExclusiveLevelMonitor : BaseAlarmMonitor
    {
        #region Private Members

        private ExclusiveLevelAlarmState m_alarm;

        #endregion


        #region Constructors

        /// <summary>
        /// Initializes the item and attaches an alarm monitor.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="parent">The parent node state.</param>
        /// <param name="namespaceIndex">Index of the namespace.</param>
        /// <param name="name">The DisplayName and BrowseName of the node.</param>
        /// <param name="alarmName">The DisplayName and BrowseName of the alarm node.</param>
        /// <param name="initialValue">The initial value of the node.</param>
        /// <param name="highLimit">The High limit of the alarm.</param>
        /// <param name="highHighLimit">The HighHigh limit of the alarm.</param>
        /// <param name="lowLimit">The Low limit of the alarm.</param>
        /// <param name="lowLowLimit">The LowLow limit of the alarm.</param>
        public ExclusiveLevelMonitor(
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
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit);

            StateChanged += AlarmMonitor_StateChanged;
        }
        #endregion

        #region Private Methods

        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create the alarm object
            m_alarm = new ExclusiveLevelAlarmState(this);

            // Declare limit components
            m_alarm.HighHighLimit = new PropertyState<double>(m_alarm);
            m_alarm.HighLimit = new PropertyState<double>(m_alarm);
            m_alarm.LowLimit = new PropertyState<double>(m_alarm);
            m_alarm.LowLowLimit = new PropertyState<double>(m_alarm);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // Set state values
            m_alarm.SetEnableState(context, true);
            m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
            m_alarm.SetSuppressedState(context, false);
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.SetActiveState(context, false);

            // Define limit values
            m_alarm.HighLimit.Value = highLimit;
            m_alarm.HighHighLimit.Value = highHighLimit;
            m_alarm.LowLimit.Value = lowLimit;
            m_alarm.LowLowLimit.Value = lowLowLimit;


        }

        #endregion

        #region Prrotected Methods
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

                // Update alarm data
                if (m_alarm.LowLowLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_LowLow
                    && newValue <= m_alarm.LowLowLimit.Value)
                {
                    m_alarm.SetLimitState(context, LimitAlarmStates.LowLow);
                    m_alarm.SetComment(context, new LocalizedText("en-US", "LowLowLimit exceeded."), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", m_alarm.LimitState.CurrentState.Value.Text));
                    m_alarm.SetSeverity(context, EventSeverity.Max);
                    updateRequired = true;
                }
                else if (m_alarm.LowLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_Low
                         && newValue > m_alarm.LowLowLimit.Value
                         && newValue <= m_alarm.LowLimit.Value)
                {
                    m_alarm.SetLimitState(context, LimitAlarmStates.Low);
                    m_alarm.SetComment(context, new LocalizedText("en-US", "LowLimit exceeded."), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", m_alarm.LimitState.CurrentState.Value.Text));
                    m_alarm.SetSeverity(context, EventSeverity.High);
                    updateRequired = true;
                }
                else if (m_alarm.HighHighLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_HighHigh
                         && newValue >= m_alarm.HighHighLimit.Value)
                {
                    m_alarm.SetLimitState(context, LimitAlarmStates.HighHigh);
                    m_alarm.SetComment(context, new LocalizedText("en-US", "HighHighLimit exceeded."), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", m_alarm.LimitState.CurrentState.Value.Text));
                    m_alarm.SetSeverity(context, EventSeverity.Max);
                    updateRequired = true;
                }
                else if (m_alarm.HighLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_High
                         && newValue < m_alarm.HighHighLimit.Value
                         && newValue >= m_alarm.HighLimit.Value)
                {
                    m_alarm.SetLimitState(context, LimitAlarmStates.High);
                    m_alarm.SetComment(context, new LocalizedText("en-US", "HighLimit exceeded."), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", m_alarm.LimitState.CurrentState.Value.Text));
                    m_alarm.SetSeverity(context, EventSeverity.High);
                    updateRequired = true;
                }
                else if (m_alarm.ActiveState.Id.Value != false
                         && m_alarm.LowLimit != null && newValue > m_alarm.LowLimit.Value
                         && m_alarm.HighLimit != null && newValue < m_alarm.HighLimit.Value)
                {
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

                    m_alarm.SetActiveState(context, true);

                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
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
                Utils.Trace(exception, "Alarms.ExclusiveLevelMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }


        #endregion
    }
}
