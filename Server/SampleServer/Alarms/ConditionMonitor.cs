﻿using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer.Alarms
{
    internal class ConditionMonitor : BaseAlarmMonitor
    {

        #region Private Members

        private ConditionState m_alarm;
        
        #endregion

        public ConditionMonitor(
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
            m_alarm.SymbolicName = "Condition Alarm";
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
            m_alarm = new ConditionState(this);

            base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            //// Add optional components
            //m_alarm.LocalTime = new PropertyState<TimeZoneDataType>(m_alarm);
            //m_alarm.Comment = new ConditionVariableState<LocalizedText>(m_alarm);
            //m_alarm.ClientUserId = new PropertyState<string>(m_alarm);
            //m_alarm.AddComment = new AddCommentMethodState(m_alarm);
            //m_alarm.Comment.Value = new LocalizedText("en", alarmName);
            //m_alarm.EnabledState = new TwoStateVariableState(m_alarm);
            //m_alarm.Message = new PropertyState<LocalizedText>(parent);
            //m_alarm.Message.Value = new LocalizedText("en", alarmName);

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

            //// Initialize alarm information
            //m_alarm.SymbolicName = alarmName;
            //m_alarm.EventType.Value = m_alarm.TypeDefinitionId;
            //m_alarm.ConditionName.Value = m_alarm.SymbolicName;
            //m_alarm.AutoReportStateChanges = true;
            //m_alarm.Time.Value = DateTime.UtcNow;
            //m_alarm.ReceiveTime.Value = m_alarm.Time.Value;
            //m_alarm.LocalTime.Value = Utils.GetTimeZoneInfo();
            ////m_alarm.BranchId.Value = null;

            //m_alarm.BranchId.Value = new NodeId(alarmName, namespaceIndex);
            ////m_alarm.ConditionClassId = new PropertyState<NodeId>(parent);
            //m_alarm.ConditionClassId.Value = new NodeId(alarmName, namespaceIndex);
            ////m_alarm.ConditionClassName = new PropertyState<LocalizedText>(parent);
            //m_alarm.ConditionClassName.Value = new LocalizedText("en", alarmName);
            ////m_alarm.ClientUserId = new PropertyState<string>(parent);
            //m_alarm.ClientUserId.Value = "Anonymous";

            //// Set state values
            //m_alarm.SetEnableState(context, true);
            //m_alarm.Retain.Value = false;
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
                Utils.Trace(exception, "Alarms.ConditionMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
    }
}
}
