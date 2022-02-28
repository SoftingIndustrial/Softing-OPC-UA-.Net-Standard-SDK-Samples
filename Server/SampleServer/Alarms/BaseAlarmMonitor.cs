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
using System.Collections.Generic;
using System.Linq;
using Opc.Ua;

namespace SampleServer.Alarms
{
    /// <summary>
    /// Base class for a monitored variable with an alarm attached.
    /// </summary>
    abstract class BaseAlarmMonitor : BaseDataVariableState<double>
    {
        #region Private Members
        protected List<ConditionState> m_conditions;
        protected AlarmsNodeManager m_alarmsNodeManager;
        #endregion

        #region Constructors
        /// <summary>
        /// Crate new instance of <see cref="BaseAlarmMonitor"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="name"></param>
        /// <param name="initialValue"></param>
        /// <param name="alarmsNodeManager"></param>
        public BaseAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            double initialValue,
            AlarmsNodeManager alarmsNodeManager = null) : base(parent)
        {
            m_alarmsNodeManager = alarmsNodeManager;

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

            StateChanged += AlarmMonitor_StateChanged;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Get the list of <see cref="ConditionState"/>
        /// </summary>
        public List<ConditionState> ConditionStates
        {
            get { return m_conditions; }
        }

        /// <summary>
        /// Get reference to the <see cref="AlarmsNodeManager"/>
        /// </summary>
        public AlarmsNodeManager AlarmsNodeManager
        {
            get { return m_alarmsNodeManager; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Handler for OnAknowledge method of an aknowlegeable alarm monitor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="condition"></param>
        /// <param name="eventId"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static ServiceResult AlarmMonitor_OnAcknowledge(ISystemContext context,
             ConditionState condition,
             byte[] eventId,
             LocalizedText comment)
        {
            // check for invalid eventId
            if (!eventId.SequenceEqual(condition.EventId.Value))
            {
                return StatusCodes.BadEventIdUnknown;
            }
            condition.Retain.Value = false;

            if (((AcknowledgeableConditionState)condition).AckedState.Id.Value)
            {
                return StatusCodes.BadConditionBranchAlreadyAcked;
            }
            return ServiceResult.Good;
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Initialize base alarm default properties
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="alarmName"></param>
        /// <param name="alarm"></param>
        protected void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            ConditionState alarm)
        {
            if(alarm == null)
            {
                return;
            }

            // Add optional components
            alarm.LocalTime = new PropertyState<TimeZoneDataType>(alarm);

            alarm.Comment = new ConditionVariableState<LocalizedText>(alarm);
            alarm.Comment.Value = new LocalizedText("en", alarmName);

            alarm.AddComment = new AddCommentMethodState(alarm);
            alarm.OnAddComment += Alarm_OnAddComment;
            
            alarm.ClientUserId = new PropertyState<string>(alarm);
                        
            alarm.EnabledState = new TwoStateVariableState(alarm);
            alarm.Message = new PropertyState<LocalizedText>(parent);
            alarm.Message.Value = new LocalizedText("en", alarmName);
            alarm.Description = new LocalizedText("en", alarmName);

            // Specify reference type between the source and the alarm.
            alarm.ReferenceTypeId = ReferenceTypeIds.Organizes;

            // This call initializes the condition from the type model (i.e. creates all of the objects
            // and variables required to store its state). The information about the type model was 
            // incorporated into the class when the class was created.
            //
            // This method also assigns new NodeIds to all of the components by calling the INodeIdFactory.New
            // method on the INodeIdFactory object which is part of the system context. The NodeManager provides
            // the INodeIdFactory implementation used here.
            alarm.Create(context, null, new QualifiedName(alarmName, namespaceIndex), null, true);

            // Add the alarm with the HasComponent reference to the variable
            AddChild(alarm);

            // Add the variable as source node of the alarm
            AddCondition(alarm);

            // Initialize alarm information
            alarm.SymbolicName = alarmName;
            alarm.EventType.Value = alarm.TypeDefinitionId;
            alarm.ConditionName.Value = alarm.SymbolicName;
            alarm.AutoReportStateChanges = true;
            alarm.Time.Value = DateTime.UtcNow;
            alarm.ReceiveTime.Value = alarm.Time.Value;
            alarm.LocalTime.Value = Utils.GetTimeZoneInfo();
            
            alarm.BranchId.Value = new NodeId(alarmName, namespaceIndex);
            alarm.ConditionClassId.Value = VariableIds.ConditionType_ConditionClassId;
            alarm.ConditionClassName.Value = alarm.ConditionClassId.DisplayName;
            alarm.ClientUserId.Value = "Anonymous"; 
                        
            // Set state values
            alarm.SetEnableState(context, true);
            alarm.Retain.Value = false;
        }

        /// <summary>
        /// Add condition state
        /// </summary>
        /// <param name="condition"></param>
        protected void AddCondition(ConditionState condition)
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

        /// <summary>
        /// Process the variable value change
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        protected virtual void ProcessVariableChanged(ISystemContext context, object value)
        {

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handler for <see cref="NodeState.StateChanged"/> event
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="changes"></param>
        private void AlarmMonitor_StateChanged(ISystemContext context, NodeState node, NodeStateChangeMasks changes)
        {
            if ((changes & NodeStateChangeMasks.Value) != 0)
            {
                ProcessVariableChanged(context, Value);
            }
        }

        /// <summary>
        /// Handler for <see cref="ConditionState.OnAddComment"/> 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="condition"></param>
        /// <param name="eventId"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        private ServiceResult Alarm_OnAddComment(
             ISystemContext context,
             ConditionState condition,
             byte[] eventId,
             LocalizedText comment)
        {
            // check the EventId 
            if (condition.EventId.Value != eventId)
            {
                condition.EventId.Value = eventId;
            }
            // check the Comment 
            if (condition.Comment.Value.Text != comment.Text)
            {
                condition.Comment.Value = comment;
            }
            Console.WriteLine("AddComment on alarm '{0}' eventId: {1} value: {2}", condition.DisplayName, BitConverter.ToString(eventId).Replace("-", ""), comment.Text);
            return ServiceResult.Good;
        }

        #endregion

    }
}
