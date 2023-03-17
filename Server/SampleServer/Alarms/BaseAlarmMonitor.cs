/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
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
    abstract class BaseAlarmMonitor<T> : BaseDataVariableState<double> where T : class
    {
        #region Private Members
        protected List<ConditionState> m_conditions;
        protected AlarmsNodeManager m_alarmsNodeManager;
        protected T m_alarm;
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
        

        #endregion

        #region Protected Methods

        /// <summary>
        /// Initialize base alarm default properties
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="alarmName"></param>
        protected virtual void InitializeAlarmMonitor(
           ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName)
        {
            if (m_alarm != null)
            {
                ConditionState alarm = m_alarm as ConditionState;
                if (alarm != null)
                {
                    // Add optional components
                    alarm.LocalTime = new PropertyState<TimeZoneDataType>(alarm);

                    alarm.Comment = new ConditionVariableState<LocalizedText>(alarm);
                    alarm.Comment.Value = new LocalizedText("en", alarmName);

                    alarm.AddComment = new AddCommentMethodState(alarm);
                    alarm.OnAddComment += Alarm_OnAddComment;

                    alarm.ClientUserId = new PropertyState<string>(alarm);

                    alarm.EnabledState = new TwoStateVariableState(alarm);
                    alarm.OnEnableDisable += OnEnableDisable;

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
                    alarm.EventId.Value = Guid.NewGuid().ToByteArray();
                    alarm.ConditionName.Value = alarm.SymbolicName;
                    alarm.AutoReportStateChanges = true;
                    alarm.Time.Value = DateTime.UtcNow;
                    alarm.ReceiveTime.Value = alarm.Time.Value;
                    alarm.LocalTime.Value = Utils.GetTimeZoneInfo();

                    var processCondTypeNode = AlarmsNodeManager.FindNodeInAddressSpace(ObjectTypeIds.ProcessConditionClassType);
                    alarm.ConditionClassId.Value = ObjectTypeIds.ProcessConditionClassType;
                    alarm.ConditionClassName.Value = processCondTypeNode.DisplayName;

                    AlarmConditionState alarmConditionState = m_alarm as AlarmConditionState;
                    if (alarmConditionState != null)
                    {
                        alarmConditionState.SetActiveState(context, false);
                    }

                    // If alarm is AcknowledgeableConditionState setup the initial values
                    AcknowledgeableConditionState acknowledgeableConditionState = m_alarm as AcknowledgeableConditionState;
                    if (acknowledgeableConditionState != null)
                    {
                        acknowledgeableConditionState.SetAcknowledgedState(context, true);
                        acknowledgeableConditionState.SetConfirmedState(context, true);
                        acknowledgeableConditionState.Retain.Value = false;

                        acknowledgeableConditionState.OnAcknowledge += AlarmMonitor_OnAcknowledge;
                        acknowledgeableConditionState.OnConfirm += AlarmMonitor_OnConfirm;
                    }

                    alarm.ClientUserId.Value = "Anonymous";

                    // set default severity
                    alarm.SetSeverity(context, EventSeverity.Medium);

                    // Set state values
                    alarm.SetEnableState(context, false);

                    #region disable unused properties

                    alarm.ConditionSubClassId = DisablePropertyUsage<NodeId[]>(alarm.ConditionSubClassId);
                    alarm.ConditionSubClassName = DisablePropertyUsage<LocalizedText[]>(alarm.ConditionSubClassName);

                    #endregion

                    alarm.BranchId.Value = new NodeId(0, 0);

                }
            }
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
        /// Process the variable value update
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        protected void ProcessVariableValueUpdate(ISystemContext context, object value)
        {
            foreach (ConditionState condition in m_conditions)
            {
                // Set event data
                condition.EventId.Value = Guid.NewGuid().ToByteArray();
                condition.Time.Value = DateTime.UtcNow;
                condition.ReceiveTime.Value = condition.Time.Value;

                // Skip reporting if EnabledState is false
                if (condition.EnabledState.Id.Value == false)
                {
                    return;
                }

                // Report changes to node attributes
                condition.ClearChangeMasks(context, true);

                // Check if events are being monitored for the source
                if (condition.AreEventsMonitored)
                {
                    // Create a snapshot
                    InstanceStateSnapshot e = new InstanceStateSnapshot();
                    e.Initialize(context, condition);

                    // Report the event
                    ReportEvent(context, e);
                }
            }
        }

        /// <summary>
        /// Process the variable value change
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        protected virtual void ProcessVariableChanged(ISystemContext context, object value)
        {
            ProcessVariableValueUpdate(context, value);
        }

        /// <summary>
        /// Validate (check) alarm acknowledge, confirm states based on active state
        /// </summary>
        /// <param name="context"></param>
        /// <param name="alarm"></param>
        /// <param name="nonActiveState"></param>
        protected void ValidateActiveStateFlags(ISystemContext context, AlarmConditionState alarm, bool nonActiveState)
        {
            // If ActiveState = Inactive reset also AcknowledgedState and/or ConfirmedState
            if (nonActiveState)
            {
                alarm.SetActiveState(context, false);
            }
            else
            {
                alarm.SetActiveState(context, true);

                if (alarm.AckedState.Id.Value == true)
                {
                    alarm.SetAcknowledgedState(context, false);
                }

                if (alarm.Retain.Value == false)
                {
                    alarm.Retain.Value = true;
                }
            }
        }

        /// <summary>
        /// Disable optional usage of alarm PropertyState in address space
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="propertyState"></param>
        protected PropertyState<S> DisablePropertyUsage<S>(PropertyState<S> propertyState)
        {
            if (propertyState != null)
            {
                propertyState.ReferenceTypeId = null;

                propertyState = null;
            }
            return propertyState;
        }

        /// <summary>
        /// Disable optional usage of alarm TwoStateVariableState in address space
        /// </summary>
        /// <param name="dataVariableState"></param>
        /// <returns></returns>
        protected TwoStateVariableState DisablePropertyUsage(TwoStateVariableState dataVariableState)
        {
            if (dataVariableState != null)
            {
                dataVariableState.ReferenceTypeId = null;

                if(dataVariableState.Id != null)
                {
                    dataVariableState.Id.ReferenceTypeId = null;
                    dataVariableState.Id = null;
                }
                
                if(dataVariableState.EffectiveTransitionTime != null)
                {
                    dataVariableState.EffectiveTransitionTime.ReferenceTypeId = null;
                    dataVariableState.EffectiveTransitionTime = null;
                }
                
                if(dataVariableState.TransitionTime != null)
                {
                    dataVariableState.TransitionTime.ReferenceTypeId = null;
                    dataVariableState.TransitionTime = null;
                }
                
                if(dataVariableState.TrueState != null)
                {
                    dataVariableState.TrueState.ReferenceTypeId = null;
                    dataVariableState.TrueState = null;
                }
                
                if(dataVariableState.FalseState != null)
                {
                    dataVariableState.FalseState.ReferenceTypeId = null;
                    dataVariableState.FalseState = null;
                }
                
                dataVariableState = null;
            }

            return dataVariableState;
        }

        /// <summary>
        /// Disable optional usage of alarm AudioVariableState in address space
        /// </summary>
        /// <param name="dataVariableState"></param>
        /// <returns></returns>
        protected AudioVariableState DisablePropertyUsage(AudioVariableState dataVariableState)
        {
            if (dataVariableState != null)
            {
                dataVariableState.ReferenceTypeId = null;

                if(dataVariableState.ListId != null)
                {
                    dataVariableState.ListId.ReferenceTypeId = null;
                    dataVariableState.ListId = null;
                }
                
                if(dataVariableState.AgencyId != null)
                {
                    dataVariableState.AgencyId.ReferenceTypeId = null;
                    dataVariableState.AgencyId = null;
                }
                
                if(dataVariableState.VersionId != null)
                {
                    dataVariableState.VersionId.ReferenceTypeId = null;
                    dataVariableState.VersionId = null;
                }
                               
                dataVariableState = null;
            }

            return dataVariableState;
        }

        /// <summary>
        /// Disable optional usage of alarm BaseDataVariableState in address space
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="dataVariableState"></param>
        /// <returns></returns>
        protected BaseDataVariableState<R> DisablePropertyUsage<R>(BaseDataVariableState<R> dataVariableState)
        {
            if (dataVariableState != null)
            {
                dataVariableState.ReferenceTypeId = null;

                dataVariableState = null;
            }

            return dataVariableState;
        }

        /// <summary>
        /// Disable optional usage of alarm ConditionVariableState in address space
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="conditionVariableState"></param>
        protected ConditionVariableState<U> DisablePropertyUsage<U>(ConditionVariableState<U> conditionVariableState)
        {
            if (conditionVariableState != null)
            {
                conditionVariableState.ReferenceTypeId = null;
                
                conditionVariableState = null;
            }

            return conditionVariableState;
        }

        /// <summary>
        /// Disable optional usage of alarm ShelvedStateMachineState in address space
        /// </summary>
        /// <param name="shelvedStateMachine"></param>
        /// <returns></returns>
        protected ShelvedStateMachineState DisablePropertyUsage(ShelvedStateMachineState shelvedStateMachine)
        {
            if (shelvedStateMachine != null)
            {
                shelvedStateMachine.ReferenceTypeId = null;

                if(shelvedStateMachine.UnshelveTime != null)
                {
                    shelvedStateMachine.UnshelveTime.ReferenceTypeId = null;
                    shelvedStateMachine.UnshelveTime = null;
                }
                
                if(shelvedStateMachine.TimedShelve != null)
                {
                    shelvedStateMachine.TimedShelve.ReferenceTypeId = null;
                    shelvedStateMachine.TimedShelve = null;
                }
                
                if(shelvedStateMachine.Unshelve != null)
                {
                    shelvedStateMachine.Unshelve.ReferenceTypeId = null;
                    shelvedStateMachine.Unshelve = null;
                }
                
                if(shelvedStateMachine.OneShotShelve != null)
                {
                    shelvedStateMachine.OneShotShelve.ReferenceTypeId = null;
                    shelvedStateMachine.OneShotShelve = null;
                }
                
                // base FiniteStateMachineState 
                if (shelvedStateMachine.CurrentState != null)
                {
                    if(shelvedStateMachine.CurrentState.Id != null)
                    {
                        shelvedStateMachine.CurrentState.Id.ReferenceTypeId = null;
                        shelvedStateMachine.CurrentState.Id = null;
                    }
                    
                    shelvedStateMachine.CurrentState = null;
                }

                if (shelvedStateMachine.LastTransition != null)
                {
                    if(shelvedStateMachine.LastTransition.Id != null)
                    {
                        shelvedStateMachine.LastTransition.Id.ReferenceTypeId = null;
                        shelvedStateMachine.LastTransition.Id = null;
                    }
                    
                    shelvedStateMachine.LastTransition = null;
                }

                if(shelvedStateMachine.AvailableStates != null)
                {
                    shelvedStateMachine.AvailableStates.ReferenceTypeId = null;
                    shelvedStateMachine.AvailableStates = null;
                }
                
                if(shelvedStateMachine.AvailableTransitions != null)
                {
                    shelvedStateMachine.AvailableTransitions.ReferenceTypeId = null;
                    shelvedStateMachine.AvailableTransitions = null;
                }
                                
                shelvedStateMachine = null;
            }

            return shelvedStateMachine;
        }

        /// <summary>
        /// Disable optional usage of alarm AlarmGroupState in address space
        /// </summary>
        /// <param name="alarmGroupState"></param>
        /// <returns></returns>
        protected AlarmGroupState DisablePropertyUsage(AlarmGroupState alarmGroupState)
        {
            if (alarmGroupState != null)
            {
                alarmGroupState.ReferenceTypeId = null;

                alarmGroupState = null;
            }

            return alarmGroupState;
        }

        /// <summary>
        /// Disable optional usage of alarm BaseObjectState in address space
        /// </summary>
        /// <param name="objectState"></param>
        protected BaseObjectState DisablePropertyUsage(BaseObjectState objectState)
        {
            if (objectState != null)
            {
                objectState.ReferenceTypeId = null;

                objectState = null;
            }

            return objectState;
        }

        /// <summary>
        /// Disable optional usage of alarm MethodState in address space
        /// </summary>
        /// <param name="methodState"></param>
        protected MethodState DisablePropertyUsage(MethodState methodState)
        {
            if (methodState != null)
            {
                methodState.ReferenceTypeId = null;

                methodState = null;
            }

            return methodState;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Custom handler for <see cref="AcknowledgeableConditionState.OnAcknowledge" />
        /// </summary>
        /// <param name="context"></param>
        /// <param name="condition"></param>
        /// <param name="eventId"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        private static ServiceResult AlarmMonitor_OnAcknowledge(ISystemContext context,
             ConditionState condition,
             byte[] eventId,
             LocalizedText comment)
        {          
            Func<AcknowledgeableConditionState, ServiceResult> applyStateChange = (alarm) =>
            {
                if (alarm.AckedState.Id.Value == false)
                {
                    alarm.SetAcknowledgedState(context, true);
                    alarm.Retain.Value = false;

                    alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm AckedState = {0}", alarm.AckedState?.Value));

                    return ServiceResult.Good;
                }
                else
                {
                    return StatusCodes.BadConditionBranchAlreadyAcked;
                }
            };

            return SetAcknowledgeConfirmState(condition, eventId, applyStateChange);
        }

        /// <summary>
        /// Custom handler for <see cref="AcknowledgeableConditionState.OnConfirm" />
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="inputArgs"></param>
        /// <param name="outputArgs"></param>
        /// <returns></returns>
        private static ServiceResult AlarmMonitor_OnConfirm(
            ISystemContext context,
            ConditionState condition,
            byte[] eventId,
            LocalizedText comment)
        {
            Func<AcknowledgeableConditionState, ServiceResult> applyStateChange = (alarm) =>
            {
                if (alarm.ConfirmedState.Id.Value == false)
                {
                    alarm.SetConfirmedState(context, true);
                    alarm.Retain.Value = false;

                    alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm ConfirmedState = {0}", alarm.ConfirmedState?.Value));

                    return ServiceResult.Good;
                }
                else
                {
                    return StatusCodes.BadConditionBranchAlreadyConfirmed;
                }

                
            };
            return SetAcknowledgeConfirmState(condition, eventId, applyStateChange);
        }

        /// <summary>
        /// Blueprint for changing Acknowledge and Confirm states
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="eventId"></param>
        /// <param name="applyFunc"></param>
        /// <returns></returns>
        private static ServiceResult SetAcknowledgeConfirmState(ConditionState condition,
            byte[] eventId,
            Func<AcknowledgeableConditionState, ServiceResult> applyStateChange)
        {
            // check the EventId 
            if ((condition.EventId.Value == null) || !Enumerable.SequenceEqual(condition.EventId.Value, eventId))
            {
                return StatusCodes.BadEventIdUnknown;
            }
            AcknowledgeableConditionState alarm = condition as AcknowledgeableConditionState;
            if (alarm != null)
            {
                return applyStateChange(alarm);
            }
            return ServiceResult.Good;
        }

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
        private static ServiceResult Alarm_OnAddComment(
             ISystemContext context,
             ConditionState condition,
             byte[] eventId,
             LocalizedText comment)
        {
            // check the EventId 
            if ((condition.EventId.Value == null) || !Enumerable.SequenceEqual(condition.EventId.Value, eventId))
            {
                return StatusCodes.BadEventIdUnknown;
            }
            // check the Comment 
            if (condition.Comment.Value.Text != comment.Text)
            {
                condition.Comment.Value = comment;
            }
            return ServiceResult.Good;
        }

        /// <summary>
        /// Handler for <see cref="ConditionState.OnEnableDisable"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="condition"></param>
        /// <param name="enabling"></param>
        /// <returns></returns>
        protected static ServiceResult OnEnableDisable(
            ISystemContext context,
            ConditionState condition,
            bool enabling)
        {
            //condition.SetEnableState(context, enabling);
            condition.Retain.Value = condition.EnabledState.Id.Value;

            return ServiceResult.Good;
        }


        #endregion

    }
}
