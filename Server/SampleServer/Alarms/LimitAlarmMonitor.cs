/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
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
        /// <param name="alarmsNodeManager"></param>
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
            double lowLowLimit,
            AlarmsNodeManager alarmsNodeManager)
             : base(context, parent, namespaceIndex, name, initialValue, alarmsNodeManager)
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

                        bool isAlarmActive = alarm.ActiveState.Id.Value;

                        bool nonActiveState = newValue > alarm.LowLimit.Value && newValue < alarm.HighLimit.Value;

                        ValidateActiveStateFlags(context, alarm, nonActiveState);

                        if (alarm.LowLowLimit != null
                            && newValue <= alarm.LowLowLimit.Value)
                        {
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Limit alarm ActiveState = {0}, State = {1}", alarm.ActiveState.Value, LimitAlarmStates.LowLow));
                            alarm.SetSeverity(context, EventSeverity.Low);

                            updateRequired = true;
                        }
                        else if (alarm.LowLimit != null
                                 && newValue > alarm.LowLowLimit.Value
                                 && newValue <= alarm.LowLimit.Value)
                        {
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Limit alarm ActiveState = {0}, State = {1}", alarm.ActiveState.Value, LimitAlarmStates.Low));
                            alarm.SetSeverity(context, EventSeverity.MediumLow);

                            updateRequired = true;
                        }
                        else if (alarm.HighHighLimit != null
                                 && newValue >= alarm.HighHighLimit.Value)
                        {
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Limit alarm ActiveState = {0}, State = {1}", alarm.ActiveState.Value, LimitAlarmStates.HighHigh));
                            alarm.SetSeverity(context, EventSeverity.MediumHigh);

                            updateRequired = true;
                        }
                        else if (alarm.HighLimit != null
                                 && newValue < alarm.HighHighLimit.Value
                                 && newValue >= alarm.HighLimit.Value)
                        {
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Limit alarm ActiveState = {0}, State = {1}", alarm.ActiveState.Value, LimitAlarmStates.High));
                            alarm.SetSeverity(context, EventSeverity.High);

                            updateRequired = true;
                        }
                        else if (isAlarmActive != false 
                                 && alarm.LowLimit != null && newValue > alarm.LowLimit.Value
                                 && alarm.HighLimit != null && newValue < alarm.HighLimit.Value)
                        {
                            alarm.Message.Value = new LocalizedText("en-US", String.Format("Limit alarm ActiveState = {0}, State = {1}", alarm.ActiveState.Value, LimitAlarmStates.Inactive));
                            alarm.SetSeverity(context, EventSeverity.Min);

                            updateRequired = true;
                        }

                        if (updateRequired)
                        {
                            // Not interested in disabled or inactive alarms
                            if (!alarm.EnabledState.Id.Value || !alarm.ActiveState.Id.Value)
                            {
                                alarm.Retain.Value = false;
                            }
                            else
                            {
                                alarm.Retain.Value = true;
                            }

                            base.ProcessVariableChanged(context, value);
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
                                        
                    // Define limit values
                    alarm.HighLimit.Value = highLimit;
                    alarm.HighHighLimit.Value = highHighLimit;
                    alarm.LowLimit.Value = lowLimit;
                    alarm.LowLowLimit.Value = lowLowLimit;

                    #region disable unused properties

                    alarm.LatchedState = DisablePropertyUsage(alarm.LatchedState);

                    alarm.SuppressedState = DisablePropertyUsage(alarm.SuppressedState);
                    alarm.OutOfServiceState = DisablePropertyUsage(alarm.OutOfServiceState);

                    alarm.BaseHighHighLimit = DisablePropertyUsage<double>(alarm.BaseHighHighLimit);
                    alarm.BaseHighLimit = DisablePropertyUsage<double>(alarm.BaseHighLimit);
                    alarm.BaseLowLimit = DisablePropertyUsage<double>(alarm.BaseLowLimit);
                    alarm.BaseLowLowLimit = DisablePropertyUsage<double>(alarm.BaseLowLowLimit);

                    alarm.MaxTimeShelved = DisablePropertyUsage<double>(alarm.MaxTimeShelved);

                    alarm.AudibleEnabled = DisablePropertyUsage<bool>(alarm.AudibleEnabled);
                    alarm.AudibleSound = DisablePropertyUsage(alarm.AudibleSound);

                    alarm.SilenceState = DisablePropertyUsage(alarm.SilenceState);

                    alarm.OnDelay = DisablePropertyUsage<double>(alarm.OnDelay);
                    alarm.OffDelay = DisablePropertyUsage<double>(alarm.OffDelay);

                    alarm.FirstInGroupFlag = DisablePropertyUsage<bool>(alarm.FirstInGroupFlag);
                    alarm.FirstInGroup = DisablePropertyUsage(alarm.FirstInGroup);

                    alarm.ReAlarmTime = DisablePropertyUsage<double>(alarm.ReAlarmTime);
                    alarm.ReAlarmRepeatCount = DisablePropertyUsage<short>(alarm.ReAlarmRepeatCount);

                    alarm.Silence = DisablePropertyUsage(alarm.Silence);
                    alarm.Suppress = DisablePropertyUsage(alarm.Suppress);
                    alarm.Unsuppress = DisablePropertyUsage(alarm.Unsuppress);
                    alarm.RemoveFromService = DisablePropertyUsage(alarm.RemoveFromService);
                    alarm.PlaceInService = DisablePropertyUsage(alarm.PlaceInService);
                    alarm.Reset = DisablePropertyUsage(alarm.Reset);

                    alarm.ShelvingState = DisablePropertyUsage(alarm.ShelvingState);

                    #endregion
                }
            }
        }

        #endregion
  
    }
}
