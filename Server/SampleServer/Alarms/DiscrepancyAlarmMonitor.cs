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
    /// A monitored variable with an <see cref="DiscrepancyAlarmState"/> attached.
    /// </summary>
    class DiscrepancyAlarmMonitor : BaseAlarmMonitor<DiscrepancyAlarmState>
    {
        #region Private Members

        double? m_value = 0;

        #endregion

        #region Constructors
        public DiscrepancyAlarmMonitor(
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
                alarmName);

            m_alarm.OnAcknowledge += AlarmMonitor_OnAcknowledge;
        }
        #endregion        

        #region Base Class Overrides

        /// <summary>
        /// Hendle the Variable value change
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
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
                    bool activeState = newValue % 2 == 0;
                    m_alarm.SetActiveState(context, activeState);

                    // Bring back AcknowledgedState and ConfirmedState
                    if (m_alarm.AckedState.Id.Value && activeState)
                    {
                        m_alarm.SetAcknowledgedState(context, false);
                        m_alarm.SetConfirmedState(context, false);
                    }

                    m_alarm.ExpectedTime.Value = (double)DateTime.UtcNow.Ticks;
                    m_alarm.Tolerance.Value = newValue.Value;

                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value || !m_alarm.ActiveState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    m_alarm.SetComment(context, new LocalizedText("en-US", String.Format("Alarm AckedState = {0}, ExpectedTime = {1}, Tolerance = {2}",
                        m_alarm.AckedState.Value.Text, m_alarm.ExpectedTime.Value, m_alarm.Tolerance.Value)), currentUserId);
                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm AckedState = {0}, ExpectedTime = {1}, Tolerance = {2}",
                        m_alarm.AckedState.Value.Text, m_alarm.ExpectedTime.Value, m_alarm.Tolerance.Value));
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
                Utils.Trace(exception, "Alarms.DiscrepancyAlarmConditionMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the alarm monitor 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        protected override void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName)
        {
            // Create the alarm object
            m_alarm = new DiscrepancyAlarmState(this);

            base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            // Set state values
            m_alarm.SetSuppressedState(context, false);
            m_alarm.SetActiveState(context, false);
            
            // error in predefined or in ctt?
            //m_alarm.AudibleSound.ReferenceTypeId = ReferenceTypeIds.HasProperty;

            m_alarm.TargetValueNode.Value = NodeId;
            m_alarm.ExpectedTime.Value = (double)DateTime.UtcNow.Ticks;
            m_alarm.Tolerance.Value = 0;

            // Disable this property 
            m_alarm.LatchedState = null;
        }

        #endregion
    }
}
