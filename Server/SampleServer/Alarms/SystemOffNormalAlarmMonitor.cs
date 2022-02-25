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
            AlarmsNodeManager alarmsNodeManager,
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue)
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

            m_alarm.OnAcknowledge += AlarmMonitor_OnAcknowledge;
        }

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
        //    m_alarm.SymbolicName = "OffNormalAlarm";
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
            double initialValue,
            BaseDataVariableState normalValueVariable)
        {
            // Create the alarm object
            m_alarm = new SystemOffNormalAlarmState(this);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // Setup the NormalState
            AddChild(normalValueVariable);
            m_alarm.NormalState.Value = normalValueVariable.NodeId;

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            m_alarm.SetActiveState(context, false); 
        }

        protected override void ProcessVariableChanged(ISystemContext context, object value)
        {
            BaseVariableState normalValVar = (BaseVariableState)AlarmsNodeManager.FindNodeInAddressSpace(m_alarm.NormalState.Value);
            OffNormalAlarmMonitor.ProcessVariableChanged(context, value, m_alarm, normalValVar.Value);
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
