using Opc.Ua;
using System;
using System.Collections.Generic;

namespace SampleServer.Alarms
{
    internal class AlarmConditionMonitor : BaseAlarmMonitor
    {
        #region Private Members

        private AlarmConditionState m_alarm;
        double? m_value = 0;

        #endregion

        #region Constructors
        public AlarmConditionMonitor(
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

            m_alarm.OnAcknowledge += AlarmMonitor_OnAcknowledge;
        }
        #endregion

        //public void UpdateConditionAlarmMonitor(
        //    ISystemContext context,
        //    double newValue,
        //    bool enableFlag)
        //{
        //    // Update alarm information
        //    //m_alarm.AutoReportStateChanges = true; // always reports changes
        //    m_alarm.Time.Value = DateTime.UtcNow;
        //    m_alarm.ReceiveTime.Value = m_alarm.Time.Value;
        //    m_alarm.LocalTime.Value = Utils.GetTimeZoneInfo();
        //    //m_alarm.BranchId.Value = null; // ignore BranchId

        //    // Set state values
        //    m_alarm.SetEnableState(context, enableFlag);
        //    m_alarm.Comment.Value = new LocalizedText(enableFlag.ToString());
        //    m_alarm.Message.Value = new LocalizedText(enableFlag.ToString());

        //    // Add the variable as source node of the alarm
        //    AddCondition(m_alarm);

        //    // Initialize alarm information
        //    m_alarm.SymbolicName = "Condition Alarm";
        //    m_alarm.EventType.Value = m_alarm.TypeDefinitionId;
        //    m_alarm.ConditionName.Value = m_alarm.SymbolicName;
        //    m_alarm.AutoReportStateChanges = true;
        //    m_alarm.Time.Value = DateTime.UtcNow;
        //    m_alarm.ReceiveTime.Value = m_alarm.Time.Value;
        //    m_alarm.LocalTime.Value = Utils.GetTimeZoneInfo();
        //    m_alarm.BranchId.Value = null;

        //    // Set state values
        //    m_alarm.SetEnableState(context, true);
        //    m_alarm.Retain.Value = false;

        //    m_alarm.Validate(context);

        //    Value = newValue;
        //    ProcessVariableChanged(context, newValue);
        //}

        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            double initialValue)
        {
            // Create the alarm object
            m_alarm = new AlarmConditionState(this);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            // Mandatory fields
            // Set input node
            m_alarm.InputNode.Value = NodeId;
            m_alarm.SetActiveState(context, false);

            // optional fields
            m_alarm.SuppressedState.Value = new LocalizedText("en-US", ConditionStateNames.Unsuppressed);
            m_alarm.OutOfServiceState.Value = new LocalizedText("en-US", Boolean.FalseString);

            // error in predefined or in ctt?
            //m_alarm.AudibleSound.ReferenceTypeId = ReferenceTypeIds.HasProperty;
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
                    bool valueState = newValue % 2 == 0;
                    m_alarm.SetActiveState(context, valueState);
                    // m_alarm.SetEnableState(context, false);

                    m_alarm.SuppressedState.Value = new LocalizedText("en-US", valueState ? ConditionStateNames.Suppressed : ConditionStateNames.Unsuppressed);
                    m_alarm.OutOfServiceState.Value = new LocalizedText("en-US", valueState ? Boolean.TrueString : Boolean.FalseString);

                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    m_alarm.SetComment(context, new LocalizedText("en-US", String.Format("Alarm AckedState = {0}, SuppressedState = {1}, OutOfServiceState = {2}",
                        m_alarm.AckedState.Value.Text, m_alarm.SuppressedState.Value.Text, m_alarm.OutOfServiceState.Value.Text)), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm AckedState = {0}, SuppressedState = {1}, OutOfServiceState = {2}",
                        m_alarm.AckedState.Value.Text, m_alarm.SuppressedState.Value.Text, m_alarm.OutOfServiceState.Value.Text));
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

        protected ServiceResult AlarmMonitor_OnAcknowledge(ISystemContext context,
             ConditionState condition,
             byte[] eventId,
             LocalizedText comment)
        {
            return AcknowledgeableConditionMonitor.OnAcknowledge(context,
                condition,
                eventId,
                comment,
                m_alarm);
        }
    }
}
