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
    /// A monitored variable with a DiscreteAlarm attached.
    /// </summary>
    internal class DiscreteMonitor : BaseAlarmMonitor
    {
        #region Private Members

        private DiscreteAlarmState m_alarm;

        public DiscreteMonitor(ISystemContext context, NodeState parent, ushort namespaceIndex, string name, string alarmName, double initialValue)
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

        #endregion

        private void InitializeAlarmMonitor(
           ISystemContext context,
           NodeState parent,
           ushort namespaceIndex,
           string alarmName,
           double initialValue)
        {
            // Create the alarm object
            m_alarm = new DiscreteAlarmState(this);

            base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, true);
            m_alarm.AckedState.Value = new LocalizedText("en-US", alarmName);

            m_alarm.SetActiveState(context, false);
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
                Utils.Trace(exception, "Alarms.AlarmConditionMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }
    }
}
