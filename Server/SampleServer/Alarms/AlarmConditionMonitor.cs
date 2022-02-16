using Opc.Ua;
using System;
using System.Collections.Generic;

namespace SampleServer.Alarms
{
    internal class AlarmConditionMonitor : BaseDataVariableState<double>
    {

        #region Private Members

        private AlarmConditionState m_alarm;
        private List<ConditionState> m_conditions;

        #endregion

        #region Public Members
        public List<ConditionState> ConditionStates
        {
            get { return m_conditions; }
        }
        #endregion
        public AlarmConditionMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue)
             : base(parent)
        {

            Create(context, null, new QualifiedName(name, namespaceIndex), null, true);

            ReferenceTypeId = ReferenceTypeIds.HasComponent;
            Value = initialValue;
            StatusCode = StatusCodes.Good;
            Timestamp = DateTime.UtcNow;

            if (parent != null)
            {
                parent.AddChild(this);

                // Define event source.
                parent.AddNotifier(context, ReferenceTypeIds.HasEventSource, false, this);
                AddNotifier(context, ReferenceTypeIds.HasEventSource, true, parent);
            }

            // Attach the alarm monitor.
            InitializeAlarmMonitor(
                context,
                namespaceIndex,
                alarmName,
                initialValue);

            StateChanged += AlarmConditionMonitor_StateChanged;
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
            ushort namespaceIndex,
            string alarmName,
            double initialValue)
        {
            // Create the alarm object
            m_alarm = new AlarmConditionState(this);

            // Add optional components
            m_alarm.LocalTime = new PropertyState<TimeZoneDataType>(m_alarm);
            m_alarm.Comment = new ConditionVariableState<LocalizedText>(m_alarm);
            m_alarm.ClientUserId = new PropertyState<string>(m_alarm);
            m_alarm.AddComment = new AddCommentMethodState(m_alarm);
            m_alarm.EnabledState = new TwoStateVariableState(m_alarm);

            // Specify reference type between the source and the alarm.
            m_alarm.ReferenceTypeId = ReferenceTypeIds.Organizes;

            // This call initializes the condition from the type model (i.e. creates all of the objects
            // and variables required to store its state). The information about the type model was 
            // incorporated into the class when the class was created.
            //
            // This method also assigns new NodeIds to all of the components by calling the INodeIdFactory.New
            // method on the INodeIdFactory object which is part of the system context. The NodeManager provides
            // the INodeIdFactory implementation used here.
            m_alarm.Create(context, null, new QualifiedName(alarmName, namespaceIndex), null, true);

            // Add the alarm with the HasComponent reference to the variable
            AddChild(m_alarm);

            // Add the variable as source node of the alarm
            AddCondition(m_alarm);

            // Initialize alarm information
            m_alarm.SymbolicName = alarmName;
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
        }

        private void AddCondition(ConditionState condition)
        {
            if (condition == null)
            {
                return;
            }

            if (m_conditions == null)
            {
                m_conditions = new List<ConditionState>();
            }

            bool foundCondition = false;
            for (int i = 0; i < m_conditions.Count; i++)
            {
                if (m_conditions[i].NodeId == condition.NodeId)
                {
                    foundCondition = true;
                    break;
                }
            }

            if (condition.SourceNode != null)
            {
                condition.SourceNode.Value = NodeId;
            }

            if (DisplayName != null && condition.SourceName != null)
            {
                condition.SourceName.Value = DisplayName.Text;
            }

            if (!foundCondition)
            {
                m_conditions.Add(condition);

                AddReference(ReferenceTypeIds.HasCondition, false, condition.NodeId);
                condition.AddReference(ReferenceTypeIds.HasCondition, true, NodeId);
            }
        }


        private void AlarmConditionMonitor_StateChanged(ISystemContext context, NodeState node, NodeStateChangeMasks changes)
        {
            if ((changes & NodeStateChangeMasks.Value) != 0)
            {
                ProcessVariableChanged(context, Value);
            }
        }

        private void ProcessVariableChanged(ISystemContext context, object value)
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
