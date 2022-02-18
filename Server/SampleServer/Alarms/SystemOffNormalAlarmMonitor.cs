using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer.Alarms
{
    internal class SystemOffNormalAlarmMonitor : BaseAlarmMonitor
    {

        #region Private Members

        private SystemOffNormalAlarmState m_alarm;

        #endregion

        public SystemOffNormalAlarmMonitor(
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

        public void UpdateConditionAlarmMonitor(
            ISystemContext context,
            double newValue,
            bool enableFlag)
        {
            // Update alarm information
            //m_alarm.AutoReportStateChanges = true; // always reports changes
            m_alarm.Time.Value = DateTime.UtcNow;
            m_alarm.ReceiveTime.Value = m_alarm.Time.Value;
            m_alarm.LocalTime.Value = Utils.GetTimeZoneInfo();
            //m_alarm.BranchId.Value = null; // ignore BranchId

            // Set state values
            m_alarm.SetEnableState(context, enableFlag);
            m_alarm.Comment.Value = new LocalizedText(enableFlag.ToString());
            m_alarm.Message.Value = new LocalizedText(enableFlag.ToString());

            // Add the variable as source node of the alarm
            AddCondition(m_alarm);

            // Initialize alarm information
            m_alarm.SymbolicName = "OffNormalAlarm";
            m_alarm.EventType.Value = m_alarm.TypeDefinitionId;
            m_alarm.ConditionName.Value = m_alarm.SymbolicName;
            m_alarm.AutoReportStateChanges = true;
            m_alarm.Time.Value = DateTime.UtcNow;
            m_alarm.ReceiveTime.Value = m_alarm.Time.Value;
            m_alarm.LocalTime.Value = Utils.GetTimeZoneInfo();
            m_alarm.BranchId.Value = null;

            // Set state values
            m_alarm.SetEnableState(context, true);
            m_alarm.Retain.Value = false;

            m_alarm.Validate(context);

            Value = newValue;
            ProcessVariableChanged(context, newValue);
        }

        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            double initialValue)
        {
            // Create the alarm object
            m_alarm = new SystemOffNormalAlarmState(this);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Setup the NormalState
            m_alarm.NormalState.Value = new NodeId();
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
                Utils.Trace(exception, "Alarms.SystemOffNormalAlarmMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }
    }
}
