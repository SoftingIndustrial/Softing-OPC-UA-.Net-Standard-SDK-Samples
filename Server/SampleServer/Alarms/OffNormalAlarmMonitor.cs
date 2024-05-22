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
    /// A monitored variable with an <see cref="OffNormalAlarmState"/> attached.
    /// </summary>
    class OffNormalAlarmMonitor : BaseAlarmMonitor<OffNormalAlarmState>
    {
        #region Constructor
        /// <summary>
        /// Create new instance of <see cref="OffNormalAlarmMonitor"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="alarmsNodeManager"></param>
        public OffNormalAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue,
            AlarmsNodeManager alarmsNodeManager)
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
            BaseVariableState normalValVar = (BaseVariableState)m_alarmsNodeManager.FindNodeInAddressSpace(m_alarm.NormalState.Value);
            if (normalValVar != null && normalValVar.Value != null)
            {
                object normalValue = normalValVar.Value;

                try
                {
                    string currentUserId = string.Empty;
                    IOperationContext operationContext = context as IOperationContext;

                    if (operationContext != null && operationContext.UserIdentity != null)
                    {
                        currentUserId = operationContext.UserIdentity.DisplayName;
                    }

                    m_alarm.SetSeverity(context, EventSeverity.Medium);

                    double? dValue = Convert.ToDouble(value);
                    double? dNormalValue = Convert.ToDouble(normalValue);

                    bool offNormal = dValue != dNormalValue; 
                    bool prevState = m_alarm.ActiveState.Id.Value;

                    // Update alarm data

                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    ValidateActiveStateFlags(context, m_alarm, !offNormal);

                    string message = String.Format("Alarm ActiveState = {0}, AckedState = {1}, ConfirmedState = {2}",
                        m_alarm.ActiveState?.Value,
                        m_alarm.AckedState?.Value,
                        m_alarm.ConfirmedState?.Value);

                    m_alarm.Message.Value = new LocalizedText("en-US", message);
                    m_alarm.SetSeverity(context, EventSeverity.Low);

                    if (offNormal != prevState)
                    {
                        base.ProcessVariableChanged(context, value);
                    }

                }
                catch (Exception exception)
                {
                    Utils.Trace(exception, "Alarms.{0}.ProcessVariableChanged: Unexpected error processing value changed notification.", m_alarm.GetType());
                }
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

            base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // Setup the NormalState
            AddChild(normalValueVariable);
            m_alarm.NormalState.Value = normalValueVariable.NodeId;

            m_alarm.Retain.Value = false;

            #region disable unused properties

            m_alarm.LatchedState = DisablePropertyUsage(m_alarm.LatchedState);

            m_alarm.SuppressedState = DisablePropertyUsage(m_alarm.SuppressedState);
            m_alarm.OutOfServiceState = DisablePropertyUsage(m_alarm.OutOfServiceState);

            m_alarm.MaxTimeShelved = DisablePropertyUsage<double>(m_alarm.MaxTimeShelved);

            m_alarm.AudibleEnabled = DisablePropertyUsage<bool>(m_alarm.AudibleEnabled);
            m_alarm.AudibleSound = DisablePropertyUsage(m_alarm.AudibleSound);

            m_alarm.SilenceState = DisablePropertyUsage(m_alarm.SilenceState);

            m_alarm.OnDelay = DisablePropertyUsage<double>(m_alarm.OnDelay);
            m_alarm.OffDelay = DisablePropertyUsage<double>(m_alarm.OffDelay);

            m_alarm.FirstInGroupFlag = DisablePropertyUsage<bool>(m_alarm.FirstInGroupFlag);
            m_alarm.FirstInGroup = DisablePropertyUsage(m_alarm.FirstInGroup);

            m_alarm.ReAlarmTime = DisablePropertyUsage<double>(m_alarm.ReAlarmTime);
            m_alarm.ReAlarmRepeatCount = DisablePropertyUsage<short>(m_alarm.ReAlarmRepeatCount);

            m_alarm.Silence = DisablePropertyUsage(m_alarm.Silence);
            m_alarm.Suppress = DisablePropertyUsage(m_alarm.Suppress);
            m_alarm.Unsuppress = DisablePropertyUsage(m_alarm.Unsuppress);
            m_alarm.RemoveFromService = DisablePropertyUsage(m_alarm.RemoveFromService);
            m_alarm.PlaceInService = DisablePropertyUsage(m_alarm.PlaceInService);
            m_alarm.Reset = DisablePropertyUsage(m_alarm.Reset);

            m_alarm.ShelvingState = DisablePropertyUsage(m_alarm.ShelvingState);

            #endregion

        }
        #endregion        
    }
}
