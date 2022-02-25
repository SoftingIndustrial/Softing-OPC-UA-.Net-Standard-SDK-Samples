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
    /// A monitored variable with an NonExclusiveRateOfChangeAlarm attached.
    /// </summary>
    class TrustListOutOfDateMonitor : BaseAlarmMonitor
    {
        #region Private Members

        private TrustListOutOfDateAlarmState m_alarm;

        #endregion

        #region Constructors
        public TrustListOutOfDateMonitor(
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
                normalValueVariable);

            m_alarm.OnAcknowledge += AcknowledgeableConditionMonitor.AlarmMonitor_OnAcknowledge;
        }
        #endregion

        #region Private Methods

        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            BaseDataVariableState normalValueVariable)
        {
            // Create the alarm object
            m_alarm = new TrustListOutOfDateAlarmState(this);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // Setup the NormalState
            AddChild(normalValueVariable);
            m_alarm.NormalState.Value = normalValueVariable.NodeId;

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            // Set state values
            m_alarm.SetSuppressedState(context, false);
            m_alarm.SetActiveState(context, false);

            // Set trust list out of date mandatory fields
            m_alarm.TrustListId.Value = Variables.TrustListOutOfDateAlarmType_TrustListId;
            m_alarm.LastUpdateTime.Value = DateTime.UtcNow; 
            m_alarm.UpdateFrequency.Value = 10000;
        }
 
        #endregion

        #region Prrotected Methods
        protected override void ProcessVariableChanged(ISystemContext context, object value)
        {
            BaseVariableState normalValVar = (BaseVariableState)AlarmsNodeManager.FindNodeInAddressSpace(m_alarm.NormalState.Value);
            OffNormalAlarmMonitor.ProcessVariableChanged(context, value, m_alarm, normalValVar.Value);
        }

        #endregion

    }
}
