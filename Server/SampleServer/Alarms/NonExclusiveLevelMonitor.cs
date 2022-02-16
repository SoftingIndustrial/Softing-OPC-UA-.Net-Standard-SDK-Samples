﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua;

namespace SampleServer.Alarms
{
    class NonExclusiveLevelMonitor : BaseAlarmMonitor
    {
        #region Private Members

        private NonExclusiveLevelAlarmState m_alarm;

        #endregion

        #region Constructors
        public NonExclusiveLevelMonitor(
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
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit);

            StateChanged += AlarmMonitor_StateChanged;
        }
        #endregion

        #region Public Methods

        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create the alarm object
            m_alarm = new NonExclusiveLevelAlarmState(this);

            // Declare limit components
            m_alarm.HighHighLimit = new PropertyState<double>(m_alarm);
            m_alarm.HighLimit = new PropertyState<double>(m_alarm);
            m_alarm.LowLimit = new PropertyState<double>(m_alarm);
            m_alarm.LowLowLimit = new PropertyState<double>(m_alarm);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            //// Add optional components
            //m_alarm.LocalTime = new PropertyState<TimeZoneDataType>(m_alarm);
            //m_alarm.Comment = new ConditionVariableState<LocalizedText>(m_alarm);
            //m_alarm.ClientUserId = new PropertyState<string>(m_alarm);
            //m_alarm.AddComment = new AddCommentMethodState(m_alarm);
            //m_alarm.EnabledState = new TwoStateVariableState(m_alarm);

            //// Specify reference type between the source and the alarm.
            //m_alarm.ReferenceTypeId = ReferenceTypeIds.Organizes;

            //// This call initializes the condition from the type model (i.e. creates all of the objects
            //// and variables required to store its state). The information about the type model was 
            //// incorporated into the class when the class was created.
            ////
            //// This method also assigns new NodeIds to all of the components by calling the INodeIdFactory.New
            //// method on the INodeIdFactory object which is part of the system context. The NodeManager provides
            //// the INodeIdFactory implementation used here.
            //m_alarm.Create(context, null, new QualifiedName(alarmName, namespaceIndex), null, true);

            //// Add the alarm with the HasComponent reference to the variable
            //AddChild(m_alarm);

            //// Add the variable as source node of the alarm
            //AddCondition(m_alarm);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            //// Initialize alarm information
            //m_alarm.SymbolicName = alarmName;
            //m_alarm.EventType.Value = m_alarm.TypeDefinitionId;
            //m_alarm.ConditionName.Value = m_alarm.SymbolicName;
            //m_alarm.AutoReportStateChanges = true;
            //m_alarm.Time.Value = DateTime.UtcNow;
            //m_alarm.ReceiveTime.Value = m_alarm.Time.Value;
            //m_alarm.LocalTime.Value = Utils.GetTimeZoneInfo();
            //m_alarm.BranchId.Value = null;

            //// Set state values
            //m_alarm.SetEnableState(context, true);
            //m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
            //m_alarm.SetSuppressedState(context, false);
            //m_alarm.SetAcknowledgedState(context, false);
            //m_alarm.Retain.Value = false;

            // Define limit values
            m_alarm.HighLimit.Value = highLimit;
            m_alarm.HighHighLimit.Value = highHighLimit;
            m_alarm.LowLimit.Value = lowLimit;
            m_alarm.LowLowLimit.Value = lowLowLimit;
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

                // Update alarm data
                // to implement the following conditions
                // highhigh & high
                // low
                // lowlow 
                // derived from NonExclusiveLimitAlarm
                // multiple mutually exclusive limits

                //if (m_alarm.LowLowLimit != null && m_alarm.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_LowLow
                //    && newValue <= m_alarm.LowLowLimit.Value)
                //{
                //    m_alarm.SetLimitState(context, LimitAlarmStates.LowLow);
                //    m_alarm.SetComment(context, new LocalizedText("en-US", "LowLowLimit exceeded."), currentUserId);
                //    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", m_alarm.LimitState.CurrentState.Value.Text));
                //    m_alarm.SetSeverity(context, EventSeverity.Max);
                //    updateRequired = true;
                //}
                //else if (m_alarm.LowLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_Low
                //         && newValue > m_alarm.LowLowLimit.Value
                //         && newValue <= m_alarm.LowLimit.Value)
                //{
                //    m_alarm.SetLimitState(context, LimitAlarmStates.Low);
                //    m_alarm.SetComment(context, new LocalizedText("en-US", "LowLimit exceeded."), currentUserId);
                //    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", m_alarm.LimitState.CurrentState.Value.Text));
                //    m_alarm.SetSeverity(context, EventSeverity.High);
                //    updateRequired = true;
                //}
                //else if (m_alarm.HighHighLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_HighHigh
                //         && newValue >= m_alarm.HighHighLimit.Value)
                //{
                //    m_alarm.SetLimitState(context, LimitAlarmStates.HighHigh);
                //    m_alarm.SetComment(context, new LocalizedText("en-US", "HighHighLimit exceeded."), currentUserId);
                //    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", m_alarm.LimitState.CurrentState.Value.Text));
                //    m_alarm.SetSeverity(context, EventSeverity.Max);
                //    updateRequired = true;
                //}
                //else if (m_alarm.HighLimit != null && m_alarm.LimitState.CurrentState.Id.Value != ObjectIds.ExclusiveLimitStateMachineType_High
                //         && newValue < m_alarm.HighHighLimit.Value
                //         && newValue >= m_alarm.HighLimit.Value)
                //{
                //    m_alarm.SetLimitState(context, LimitAlarmStates.High);
                //    m_alarm.SetComment(context, new LocalizedText("en-US", "HighLimit exceeded."), currentUserId);
                //    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", m_alarm.LimitState.CurrentState.Value.Text));
                //    m_alarm.SetSeverity(context, EventSeverity.High);
                //    updateRequired = true;
                //}
                //else if (m_alarm.ActiveState.Id.Value != false
                //         && m_alarm.LowLimit != null && newValue > m_alarm.LowLimit.Value
                //         && m_alarm.HighLimit != null && newValue < m_alarm.HighLimit.Value)
                //{
                //    m_alarm.SetLimitState(context, LimitAlarmStates.Inactive);
                //    m_alarm.SetComment(context, new LocalizedText("en-US", "Alarm inactive."), currentUserId);
                //    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm State set to {0}", LimitAlarmStates.Inactive));
                //    m_alarm.SetSeverity(context, EventSeverity.Low);
                //    updateRequired = true;
                //}

                if (updateRequired)
                {
                    // Set event data
                    m_alarm.EventId.Value = Guid.NewGuid().ToByteArray();
                    m_alarm.Time.Value = DateTime.UtcNow;
                    m_alarm.ReceiveTime.Value = m_alarm.Time.Value;

                    m_alarm.ConditionClassId.Value = ObjectTypeIds.BaseConditionClassType;
                    m_alarm.ConditionClassName.Value = new LocalizedText("BaseConditionClassType");
                    m_alarm.BranchId.Value = new NodeId();

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
                Utils.Trace(exception, "Alarms.NonExclusiveLevelMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }
        #endregion
    }
}
