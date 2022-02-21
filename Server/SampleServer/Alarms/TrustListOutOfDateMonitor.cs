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
           : base(context, parent, namespaceIndex, name, initialValue)
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

            StateChanged += AlarmMonitor_StateChanged;
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

            // Set state values
            m_alarm.SetEnableState(context, true);
            m_alarm.SetSuppressedState(context, false);
            m_alarm.SetAcknowledgedState(context, false);
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

                // Update alarm data


                if (updateRequired)
                {
                    // Set event data
                    m_alarm.EventId.Value = Guid.NewGuid().ToByteArray();
                    m_alarm.Time.Value = DateTime.UtcNow;
                    m_alarm.ReceiveTime.Value = m_alarm.Time.Value;

                    m_alarm.ConditionClassId.Value = ObjectTypeIds.BaseConditionClassType;
                    m_alarm.ConditionClassName.Value = new LocalizedText("BaseConditionClassType");
                    m_alarm.BranchId.Value = new NodeId();

                    m_alarm.SetActiveState(context, true);

                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    // Reset the acknowledged flag
                    m_alarm.SetAcknowledgedState(context, false);

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
                Utils.Trace(exception, "Alarms.TrustListOutOfDateMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }

        #endregion

    }
}
