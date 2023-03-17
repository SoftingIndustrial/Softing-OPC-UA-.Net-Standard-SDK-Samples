/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using Opc.Ua;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an <see cref="ExclusiveLimitAlarmState"/> attached.
    /// </summary>
    class ExclusiveLimitMonitor : LimitAlarmMonitor<ExclusiveLimitAlarmState>
    {
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
        /// <param name="alarmsNodeManager"></param>
        public ExclusiveLimitMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit,
            AlarmsNodeManager alarmsNodeManager)
            : base(context, parent, namespaceIndex, name,
                  alarmName, initialValue, highLimit, highHighLimit,
                  lowLimit, lowLowLimit, alarmsNodeManager)
        {
        }

        #endregion

        #region Public Methods

        #endregion

        #region Base Class Overrides

        /// <summary>
        /// Hendle the Variable value change
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
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

                bool isAlarmActive = m_alarm.ActiveState.Id.Value;

                bool nonActiveState = newValue > m_alarm.LowLimit.Value && newValue < m_alarm.HighLimit.Value;

                ValidateActiveStateFlags(context, m_alarm, nonActiveState);

                // Update alarm data
                if (m_alarm.LowLowLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_LowLow
                    && newValue <= m_alarm.LowLowLimit.Value)
                {
                    m_alarm.SetLimitState(context, LimitAlarmStates.LowLow);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, LimitState = {1}", m_alarm.ActiveState.Value, m_alarm.LimitState?.CurrentState?.Value));
                    m_alarm.SetSeverity(context, EventSeverity.Low);

                    updateRequired = true;
                }
                else if (m_alarm.LowLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_Low
                         && newValue > m_alarm.LowLowLimit.Value
                         && newValue <= m_alarm.LowLimit.Value)
                {
                    m_alarm.SetLimitState(context, LimitAlarmStates.Low);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, LimitState = {1}", m_alarm.ActiveState.Value, m_alarm.LimitState?.CurrentState?.Value));
                    m_alarm.SetSeverity(context, EventSeverity.MediumLow);

                    updateRequired = true;
                }
                else if (m_alarm.HighHighLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_HighHigh
                         && newValue >= m_alarm.HighHighLimit.Value)
                {
                    m_alarm.SetLimitState(context, LimitAlarmStates.HighHigh);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, LimitState = {1}", m_alarm.ActiveState.Value, m_alarm.LimitState?.CurrentState?.Value));
                    m_alarm.SetSeverity(context, EventSeverity.MediumHigh);

                    updateRequired = true;
                }
                else if (m_alarm.HighLimit != null // && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_High
                         && newValue < m_alarm.HighHighLimit.Value
                         && newValue >= m_alarm.HighLimit.Value)
                {
                    m_alarm.SetLimitState(context, LimitAlarmStates.High);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, LimitState = {1}", m_alarm.ActiveState.Value, m_alarm.LimitState?.CurrentState?.Value));
                    m_alarm.SetSeverity(context, EventSeverity.High);
                    
                    updateRequired = true;
                }
                else if (isAlarmActive != false
                         && m_alarm.LowLimit != null && newValue > m_alarm.LowLimit.Value
                         && m_alarm.HighLimit != null && newValue < m_alarm.HighLimit.Value)
                {
                    m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, LimitState = {1}", m_alarm.ActiveState.Value, LimitAlarmStates.Inactive));
                    m_alarm.SetSeverity(context, EventSeverity.Min);
                    
                    updateRequired = true;
                }

                if (updateRequired)
                {
                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    ProcessVariableValueUpdate(context, value);
                }
            }
            catch (Exception exception)
            {
                Utils.Trace(exception, "Alarms.ExclusiveLimitMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Initialize the alarm monitor 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="alarmName"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        protected override void InitializeAlarmMonitor(
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
            m_alarm = new ExclusiveLimitAlarmState(this);

            base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, highLimit, highHighLimit, lowLimit, lowLowLimit);

            // Set state values
            m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
        }

        #endregion        
    }
}