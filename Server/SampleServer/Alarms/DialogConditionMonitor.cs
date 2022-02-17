using Opc.Ua;
using System;
using System.Collections.Generic;

namespace SampleServer.Alarms
{
    internal class DialogConditionMonitor : BaseAlarmMonitor
    {

        #region Private Members

        private DialogConditionState m_alarm;
        
        #endregion

        
        public DialogConditionMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue)
              : base(context, parent, namespaceIndex, name, initialValue)
        {
            // Attach the alarm monitor.
            InitializeAlarmMonitor(
                context,
                parent,
                namespaceIndex,
                alarmName,
                initialValue);

            StateChanged += AlarmMonitor_StateChanged;
        }

        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            double initialValue)
        {
            // Create the alarm object
            m_alarm = new DialogConditionState(this);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Manadatory fields initialization
            m_alarm.DialogState.Value = new LocalizedText("en", "DialogStateSample");
            m_alarm.Prompt.Value = new LocalizedText("en", "PromptSample");
            m_alarm.ResponseOptionSet.Value = new LocalizedText[] { new LocalizedText("en", "ResponseSample") };
            m_alarm.DefaultResponse.Value = (int)StatusCodes.Good;
            m_alarm.LastResponse.Value = (int)StatusCodes.Good;
            m_alarm.OkResponse.Value = (int)StatusCodes.Good;
            m_alarm.CancelResponse.Value = (int)StatusCodes.Good;
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

                // Not interested in disabled or inactive alarms
                if (!m_alarm.EnabledState.Id.Value)
                {
                    m_alarm.Retain.Value = false;
                }
                else
                {
                    m_alarm.Retain.Value = true;
                }

                m_alarm.SetEnableState(context, false);

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
            catch (Exception exception)
            {
                Utils.Trace(exception, "Alarms.DialogConditionMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
    }
}
}
