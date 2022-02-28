using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer.Alarms
{
    internal class LimitAlarmMonitor : BaseAlarmMonitor
    {

        #region Private Members

        private LimitAlarmState m_alarm;

        #endregion

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

            m_alarm.OnAcknowledge += AcknowledgeableConditionMonitor.AlarmMonitor_OnAcknowledge;
        }

        private void InitializeAlarmMonitor(
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
            m_alarm = new LimitAlarmState(this);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            m_alarm.ConditionClassId.Value = ObjectTypeIds.BaseConditionClassType;
            m_alarm.ConditionClassName.Value = new LocalizedText("BaseConditionClassType");
            m_alarm.BranchId.Value = new NodeId();

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            // Set state values
            m_alarm.SetSuppressedState(context, false);
            m_alarm.SetActiveState(context, false);

            // Define limit values
            m_alarm.HighLimit.Value = highLimit;
            m_alarm.HighHighLimit.Value = highHighLimit;
            m_alarm.LowLimit.Value = lowLimit;
            m_alarm.LowLowLimit.Value = lowLowLimit;

            // Disable this property 
            m_alarm.LatchedState = null;
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

                // Set event data
                m_alarm.EventId.Value = Guid.NewGuid().ToByteArray();
                m_alarm.Time.Value = DateTime.UtcNow;
                m_alarm.ReceiveTime.Value = m_alarm.Time.Value;

                m_alarm.ConditionClassId.Value = ObjectTypeIds.BaseConditionClassType;
                m_alarm.ConditionClassName.Value = new LocalizedText("BaseConditionClassType");
                m_alarm.BranchId.Value = new NodeId();

                bool nonActiveState = newValue > m_alarm.LowLimit.Value && newValue < m_alarm.HighLimit.Value;
                m_alarm.SetActiveState(context, !nonActiveState);

                // Not interested in disabled or inactive alarms
                if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                {
                    m_alarm.Retain.Value = false;
                }
                else
                {
                    m_alarm.Retain.Value = true;
                }
                
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
                Utils.Trace(exception, "Alarms.LimitAlarmMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }
    }
}
