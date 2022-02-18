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
using System.Text;
using System.Threading.Tasks;
using Opc.Ua;

namespace SampleServer.Alarms
{
    abstract class BaseAlarmMonitor : BaseDataVariableState<double>
    {
        #region Private Members
        protected List<ConditionState> m_conditions;
        //private ConditionState alarm;
        #endregion

        #region Properties
        public List<ConditionState> ConditionStates
        {
            get { return m_conditions; }
        }
        #endregion

        #region Constructors
        public BaseAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            double initialValue) : base(parent)
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

            StateChanged += AlarmMonitor_StateChanged;
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
            alarm.ClientUserId = new PropertyState<string>(alarm);
            alarm.AddComment = new AddCommentMethodState(alarm);
            alarm.Comment.Value = new LocalizedText("en", alarmName);
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

        protected void AlarmMonitor_StateChanged(ISystemContext context, NodeState node, NodeStateChangeMasks changes)
        {
            if ((changes & NodeStateChangeMasks.Value) != 0)
            {
                ProcessVariableChanged(context, Value);
            }
        }

        protected virtual void ProcessVariableChanged(ISystemContext context, object value)
        {
        }
        #endregion

    }
}
