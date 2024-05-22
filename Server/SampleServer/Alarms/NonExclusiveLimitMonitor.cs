/* ========================================================================
 * Copyright © 2011-2024 Softing Industrial Automation GmbH. 
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
    /// A monitored variable with an <see cref="NonExclusiveLimitAlarmState"/> attached.
    /// </summary>
    class NonExclusiveLimitMonitor : LimitAlarmMonitor<NonExclusiveLimitAlarmState>
    {
        #region Constructors

        public NonExclusiveLimitMonitor(ISystemContext context,
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
              : base(context, parent, namespaceIndex, name, alarmName,
                    initialValue, highLimit, highHighLimit, lowLimit, lowLowLimit, alarmsNodeManager)
        {

        }
        #endregion

        #region Base Class Overrides

        /// <summary>
        /// Handle the Variable value change
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

                if (m_alarm.LowLowLimit != null && m_alarm.LowLowState.Id.Value == false 
                    && newValue <= m_alarm.LowLowLimit.Value)
                {
                    m_alarm.LowLowState.Id.Value = true;

                    m_alarm.SetLimitState(context, LimitAlarmStates.LowLow);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, State LowLow = {1}", m_alarm.ActiveState.Value, m_alarm.LowLowState?.Value));
                    m_alarm.SetSeverity(context, EventSeverity.Low);

                    updateRequired = true;
                }
                else if (m_alarm.LowLimit != null && m_alarm.LowState.Id.Value == false
                         && newValue > m_alarm.LowLowLimit.Value
                         && newValue <= m_alarm.LowLimit.Value)
                {
                    m_alarm.LowState.Id.Value = true;
                    m_alarm.HighState.Id.Value = true;

                    m_alarm.SetLimitState(context, LimitAlarmStates.Low);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, State Low & High = {1} & {2}", m_alarm.ActiveState.Value, m_alarm.LowState?.Value, m_alarm.HighState?.Value));
                    m_alarm.SetSeverity(context, EventSeverity.MediumLow);

                    updateRequired = true;
                }
                else if (m_alarm.HighHighLimit != null && m_alarm.HighHighState.Id.Value == false
                         && newValue >= m_alarm.HighHighLimit.Value)
                {
                    m_alarm.HighHighState.Id.Value = true;

                    m_alarm.SetLimitState(context, LimitAlarmStates.HighHigh);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, State HighHigh = {1}", m_alarm.ActiveState.Value, m_alarm.HighHighState?.Value));
                    m_alarm.SetSeverity(context, EventSeverity.MediumHigh);

                    updateRequired = true;
                }
                else if (m_alarm.HighLimit != null && m_alarm.HighState.Id.Value == false
                         && newValue < m_alarm.HighHighLimit.Value
                         && newValue >= m_alarm.HighLimit.Value)
                {
                    m_alarm.LowState.Id.Value = true;
                    m_alarm.HighState.Id.Value = true;

                    m_alarm.SetLimitState(context, LimitAlarmStates.High);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, State High & Low = {1} & {2}", m_alarm.ActiveState.Value, m_alarm.HighState?.Value, m_alarm.LowState?.Value));
                    m_alarm.SetSeverity(context, EventSeverity.High);
                    updateRequired = true;
                }
                else if (isAlarmActive != false
                         && m_alarm.LowLimit != null && newValue > m_alarm.LowLimit.Value
                         && m_alarm.HighLimit != null && newValue < m_alarm.HighLimit.Value)
                {
                    m_alarm.LowState.Id.Value = false;
                    m_alarm.LowLowState.Id.Value = false;
                    m_alarm.HighState.Id.Value = false;
                    m_alarm.HighHighState.Id.Value = false;

                    m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ActiveState = {0}, State = {1}", m_alarm.ActiveState.Value, LimitAlarmStates.Inactive));
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

                    if(m_alarm.LowLowState.Id.Value)
                    {
                        m_alarm.LowState.Id.Value = false;
                        m_alarm.HighState.Id.Value = false;
                        m_alarm.HighHighState.Id.Value = false;
                    }
                    else if (m_alarm.LowState.Id.Value && m_alarm.HighState.Id.Value)
                    {
                        m_alarm.LowLowState.Id.Value = false;
                        m_alarm.HighHighState.Id.Value = false;
                    }
                    else if (m_alarm.LowState.Id.Value || m_alarm.HighState.Id.Value)
                    {
                        m_alarm.LowState.Id.Value = true;
                        m_alarm.HighState.Id.Value = true;

                        m_alarm.LowLowState.Id.Value = false;
                        m_alarm.HighHighState.Id.Value = false;
                    }
                    else if (m_alarm.HighHighState.Id.Value)
                    {
                        m_alarm.LowLowState.Id.Value = false;
                        m_alarm.LowState.Id.Value = false;
                        m_alarm.HighState.Id.Value = false;
                    }

                    ProcessVariableValueUpdate(context, value);
                }
            }
            catch (Exception exception)
            {
                Utils.Trace(exception, "Alarms.NonExclusiveLimitMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
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
        /// <param name="initialValue"></param>
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
            m_alarm = new NonExclusiveLimitAlarmState(this);


            base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, highLimit, highHighLimit, lowLimit, lowLowLimit);

            // Set state values
            m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
        }
        #endregion
    }
}
