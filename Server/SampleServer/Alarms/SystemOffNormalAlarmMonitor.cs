/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an <see cref="SystemOffNormalAlarmMonitor"/> attached.
    /// </summary>
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

            // Disable this property 
            m_alarm.LatchedState = null;
        }

        protected override void ProcessVariableChanged(ISystemContext context, object value)
        {
            BaseVariableState normalValVar = (BaseVariableState)AlarmsNodeManager.FindNodeInAddressSpace(m_alarm.NormalState.Value);
            OffNormalAlarmMonitor.ProcessVariableChanged(context, value, m_alarm, normalValVar.Value);
        }
    }
}
