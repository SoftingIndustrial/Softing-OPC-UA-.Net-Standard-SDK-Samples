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
    /// A monitored variable with an <see cref="LimitAlarmState"/> attached.
    /// </summary>
    class LimitAlarmMonitor<T> : BaseAlarmMonitor<T> where T : class
    {
        #region Constructor

        /// <summary>
        /// Create new instance of <see cref="LimitAlarmMonitor"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        public LimitAlarmMonitor(
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

            //m_alarm.OnAcknowledge += AlarmMonitor_OnAcknowledge;
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
            try
            {
                string currentUserId = string.Empty;
                IOperationContext operationContext = context as IOperationContext;

                if (operationContext != null && operationContext.UserIdentity != null)
                {
                    currentUserId = operationContext.UserIdentity.DisplayName;
                }

                double? newValue = Convert.ToDouble(value);
                if (m_alarm is LimitAlarmState)
                {
                    LimitAlarmState alarm = m_alarm as LimitAlarmState;
                    if (alarm != null)
                    {
                        bool updateRequired = false;

                        if (alarm.LowLowLimit != null 
                            && newValue <= alarm.LowLowLimit.Value)
                        {
                            alarm.SetComment(context, new LocalizedText("en-US", "LowLowLimit exceeded."), currentUserId);
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", LimitAlarmStates.LowLow));
                            alarm.SetSeverity(context, EventSeverity.Min);

                            updateRequired = true;
                        }
                        else if (alarm.LowLimit != null
                                 && newValue > alarm.LowLowLimit.Value
                                 && newValue <= alarm.LowLimit.Value)
                        {
                            alarm.SetComment(context, new LocalizedText("en-US", "LowLimit exceeded."), currentUserId);
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", LimitAlarmStates.Low));
                            alarm.SetSeverity(context, EventSeverity.Low);

                            updateRequired = true;
                        }
                        else if (alarm.HighHighLimit != null
                                 && newValue >= alarm.HighHighLimit.Value)
                        {
                            alarm.SetComment(context, new LocalizedText("en-US", "HighHighLimit exceeded."), currentUserId);
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", LimitAlarmStates.HighHigh));
                            alarm.SetSeverity(context, EventSeverity.MediumHigh);

                            updateRequired = true;
                        }
                        else if (alarm.HighLimit != null 
                                 && newValue < alarm.HighHighLimit.Value
                                 && newValue >= alarm.HighLimit.Value)
                        {
                            alarm.SetComment(context, new LocalizedText("en-US", "HighLimit exceeded."), currentUserId);
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", LimitAlarmStates.High));
                            alarm.SetSeverity(context, EventSeverity.High);
                            updateRequired = true;
                        }
                        else if (alarm.ActiveState.Id.Value != false
                                 && alarm.LowLimit != null && newValue > alarm.LowLimit.Value
                                 && alarm.HighLimit != null && newValue < alarm.HighLimit.Value)
                        {
                            alarm.SetComment(context, new LocalizedText("en-US", "Alarm inactive."), currentUserId);
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", LimitAlarmStates.Inactive));
                            alarm.SetSeverity(context, 0);

                            updateRequired = true;
                        }

                        if (updateRequired)
                        {
                            // Set event data
                            alarm.EventId.Value = Guid.NewGuid().ToByteArray();
                            alarm.Time.Value = DateTime.UtcNow;
                            alarm.ReceiveTime.Value = alarm.Time.Value;

                            alarm.ConditionClassId.Value = ObjectTypeIds.BaseConditionClassType;
                            alarm.ConditionClassName.Value = new LocalizedText("BaseConditionClassType");
                            alarm.BranchId.Value = new NodeId();

                            bool nonActiveState = newValue > alarm.LowLimit.Value && newValue < alarm.HighLimit.Value;
                            alarm.SetActiveState(context, !nonActiveState);

                            // Not interested in disabled or inactive alarms
                            if (!alarm.EnabledState.Id.Value || !alarm.ActiveState.Id.Value)
                            {
                                alarm.Retain.Value = false;
                            }
                            else
                            {
                                alarm.Retain.Value = true;
                            }

                            // Report changes to node attributes
                            alarm.ClearChangeMasks(context, true);

                            // Check if events are being monitored for the source
                            if (alarm.AreEventsMonitored)
                            {
                                // Create a snapshot
                                InstanceStateSnapshot e = new InstanceStateSnapshot();
                                e.Initialize(context, alarm);

                                // Report the event
                                ReportEvent(context, e);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Utils.Trace(exception, "Alarms.LimitAlarmMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
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
        protected virtual void InitializeAlarmMonitor(
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
            if (m_alarm == null)
            {
                m_alarm = (T)Activator.CreateInstance(typeof(T), new object[] { this });
            }

            if (m_alarm is LimitAlarmState)
            {
                LimitAlarmState alarm = m_alarm as LimitAlarmState;
                if (alarm != null)
                {
                    // Declare limit components
                    alarm.HighHighLimit = new PropertyState<double>(alarm);
                    alarm.HighLimit = new PropertyState<double>(alarm);
                    alarm.LowLimit = new PropertyState<double>(alarm);
                    alarm.LowLowLimit = new PropertyState<double>(alarm);

                    base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName);

                    // Set input node
                    alarm.InputNode.Value = NodeId;

                    // set acknowledge state
                    alarm.SetAcknowledgedState(context, false);
                    alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

                    // Set state values
                    // m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
                    alarm.SetSuppressedState(context, false);
                    alarm.SetActiveState(context, false);

                    // Define limit values
                    alarm.HighLimit.Value = highLimit;
                    alarm.HighHighLimit.Value = highHighLimit;
                    alarm.LowLimit.Value = lowLimit;
                    alarm.LowLowLimit.Value = lowLowLimit;

                    // Disable this property 
                    alarm.LatchedState = null;

                    alarm.OnAcknowledge += AlarmMonitor_OnAcknowledge;
                }
            }
        }

        #endregion
    }
}
