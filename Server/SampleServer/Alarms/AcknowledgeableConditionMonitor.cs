using Opc.Ua;
using System;
using System.Collections.Generic;

namespace SampleServer.Alarms
{
    internal class AcknowledgeableConditionMonitor : BaseAlarmMonitor
    {

        #region Private Members

        private AcknowledgeableConditionState m_alarm;
        double? m_value = 0;

        #endregion

        public AcknowledgeableConditionMonitor(
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
        }

        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            double initialValue)
        {
            // Create the alarm object
            m_alarm = new AcknowledgeableConditionState(this);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Mandatory fields
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            // Optional fields
            m_alarm.SetConfirmedState(context, false);
            m_alarm.ConfirmedState.Value = new LocalizedText("en-US", ConditionStateNames.Unconfirmed);
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

                    bool valueState = newValue % 2 == 0;
                    m_alarm.SetAcknowledgedState(context, valueState);
                    m_alarm.AckedState.Value = new LocalizedText("en-US", valueState ? ConditionStateNames.Acknowledged : ConditionStateNames.Unacknowledged);

                    m_alarm.SetConfirmedState(context, false);
                    m_alarm.ConfirmedState.Value = new LocalizedText("en-US", valueState ? ConditionStateNames.Confirmed : ConditionStateNames.Unconfirmed);

                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    //m_alarm.SetEnableState(context, false);
                    m_alarm.SetComment(context, new LocalizedText("en-US", String.Format("Alarm ConfirmedState = {0}", m_alarm.ConfirmedState.Value.Text)), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm AckedState = {0}", m_alarm.AckedState.Value.Text));
                    m_alarm.SetSeverity(context, valueState ? EventSeverity.Low : EventSeverity.Min);

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
                Utils.Trace(exception, "Alarms.AcknowledgeableConditionMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
    }
}
}
