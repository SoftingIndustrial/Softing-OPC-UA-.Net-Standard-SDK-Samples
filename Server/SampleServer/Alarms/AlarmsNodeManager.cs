/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the Alarms and Conditions OPC UA feature
    /// </summary>
    public class AlarmsNodeManager : NodeManager
    {
        #region Private Fields
        private Timer m_allAlarmsChangeValues;
        private List<ConditionState> m_conditionInstances = new List<ConditionState>();
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public AlarmsNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.Alarms)
        {
        }

        #endregion

        #region INodeManager Members

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // Execute base class CreateAddressSpace
                base.CreateAddressSpace(externalReferences);

                // Create a root node and add a reference to external Server Objects Folder
                FolderState root = CreateFolder(null, "Alarms");
                root.EventNotifier = EventNotifiers.SubscribeToEvents;

                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                // Add Support for Event Notifiers
                // Creating notifier ensures events propagate up the hierarchy when they are produced
                AddRootNotifier(root);

                // Add a folder representing the monitored device.
                BaseObjectState machine = CreateObject(root, "Machine A");
                machine.EventNotifier = EventNotifiers.SubscribeToEvents;

                // Create an alarm monitor for a LimitAlarm type.
                CreateLimitAlarmMonitor(
                    machine,
                    "LimitAlarmSensor 1",
                    "LimitAlarmMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a temperature sensor 1.
                CreateExclusiveLimitMonitor(
                    machine,
                    "TemperatureSensor 1",
                    "TemperatureMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a temperature sensor 2.
                CreateExclusiveLimitMonitor(
                    machine,
                    "TemperatureSensor 2",
                    "TemperatureMonitor 2",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a pressure sensor 1.
                CreateExclusiveLimitMonitor(
                    machine,
                    "PressureSensor 1",
                    "PressureMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a pressure sensor 2.
                CreateExclusiveLimitMonitor(
                    machine,
                    "PressureSensor 2",
                    "PressureMonitor 2",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a ExclusiveLevelAlarm type.
                CreateExclusiveLevelMonitor(
                    machine,
                    "ExclusiveLevelSensor 1",
                    "ExclusiveLevelMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a ExclusiveDeviationAlarm type.
                CreateExclusiveDeviationMonitor(
                    machine,
                    "ExclusiveDeviationSensor 1",
                    "ExclusiveDeviationMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a ExclusiveRateOfChangeAlarm type.
                CreateExclusiveRateOfChangeMonitor(
                    machine,
                    "ExclusiveRateOfChangeSensor 1",
                    "ExclusiveRateOfChangeMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a NonExclusiveLimitAlarm type.
                CreateNonExclusiveLimitMonitor(
                    machine,
                    "NonExclusiveLimitSensor 1",
                    "NonExclusiveLimitMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a NonExclusiveLevelAlarm type.
                CreateNonExclusiveLevelMonitor(
                    machine,
                    "NonExclusiveLevelSensor 1",
                    "NonExclusiveLevelMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a NonExclusiveDeviationAlarm type.
                CreateNonExclusiveDeviationMonitor(
                    machine,
                    "NonExclusiveDeviationSensor 1",
                    "NonExclusiveDeviationMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a NonExclusiveRateOfChangeAlarm type.
                CreateNonExclusiveRateOfChangeMonitor(
                    machine,
                    "NonExclusiveRateOfChangeSensor 1",
                    "NonExclusiveRateOfChangeMonitor 1",
                    10.0,
                    50.0,
                    80.0,
                    30.0,
                    20.0);

                // IsAbstract alarm - should not be activated !
                //CreateConditionMonitor(
                //    machine,
                //    "ConditionSensor 1",
                //    "ConditionMonitor 1",
                //    7.0);

                // Create an alarm monitor for a DialogConditionAlarm type.
                CreateDialogConditionMonitor(
                    machine,
                    "DialogConditionSensor 1",
                    "DialogConditionMonitor 1",
                    10.0);

                // Create an alarm monitor for a AcknowledgeableCondition type.
                CreateAcknowledgeableConditionMonitor(
                    machine,
                    "AcknowledgeableConditionSensor 1",
                    "AcknowledgeableConditionMonitor 1",
                    10.0);

                // Create an alarm monitor for a AlarmCondition type.
                CreateAlarmConditionMonitor(
                    machine,
                    "AlarmConditionSensor 1",
                    "AlarmConditionMonitor 1",
                    10.0);

                // Create an alarm monitor for a DiscrepancyAlarmCondition type.
                CreateDiscrepancyAlarmMonitor(
                    machine,
                    "DiscrepancyAlarmSensor 1",
                    "DiscrepancyAlarmMonitor 1",
                    10.0);

                // Create an alarm monitor for a Discrete type.
                CreateDiscreteMonitor(
                    machine,
                    "DiscreteSensor 1",
                    "DiscreteMonitor 1",
                    10.0);

                // Create an alarm monitor for a CertificateExpiratioAlarm type.
                CreateCertificateExpirationMonitor(
                    machine,
                    "CertificateExpirationSensor 1",
                    "CertificateExpirationMonitor 1",
                    10.0);

                // Create an alarm monitor for a TrustListOutOfDateAlarm type.
                CreateTrustListOutOfDateMonitor(
                    machine,
                    "TrustListOutOfDateSensor 1",
                    "TrustListOutOfDateMonitor 1",
                    10.0);

                // Create an alarm monitor for a InstrumentDiagnosticAlarm type.
                CreateInstrumentDiagnosticMonitor(
                    machine,
                    "InstrumentDiagnosticSensor 1",
                    "InstrumentDiagnosticMonitor 1",
                    10.0);

                // Create an alarm monitor for a OffNormalAlarm type.
                CreateOffNormalAlarmMonitor(
                    machine,
                    "OffNormalAlarmSensor 1",
                    "OffNormalAlarmMonitor 1",
                    10.0);

                // Create an alarm monitor for a TripAlarm type.
                CreateTripAlarmMonitor(
                    machine,
                    "TripAlarmSensor 1",
                    "TripAlarmMonitor 1",
                    10.0);

                // Create an alarm monitor for a SystemOffNormalAlarm type.
                CreateSystemOffNormalAlarmMonitor(
                    machine,
                    "SystemOffNormalAlarmSensor 1",
                    "SystemOffNormalAlarmMonitor 1",
                    10.0);

                // Create an alarm monitor for a SystemDiagnostic type.
                CreateSystemDiagnosticMonitor(
                    machine,
                    "SystemDiagnosticAlarmSensor 1",
                    "SystemDiagnosticAlarmMonitor 1",
                    10.0);

                // Calling AddComment on ConditionType is BadNodeIdInvalid because is an event type but not triggered from an alarm
                MethodState conditionTypeAddCommentNode = FindNodeInAddressSpace(Opc.Ua.Methods.ConditionType_AddComment) as MethodState;
                if(conditionTypeAddCommentNode != null)
                {
                    conditionTypeAddCommentNode.OnCallMethod += AddCommentCallMethod;
                }

                MethodState acknowledgeTypeMethodNode = FindNodeInAddressSpace(Opc.Ua.Methods.AcknowledgeableConditionType_Acknowledge) as MethodState;
                if (acknowledgeTypeMethodNode != null)
                {
                    acknowledgeTypeMethodNode.OnCallMethod += OnAcknowledgeCallMethod;
                }

                MethodState confirmTypeMethodNode = FindNodeInAddressSpace(Opc.Ua.Methods.AcknowledgeableConditionType_Confirm) as MethodState;
                if (confirmTypeMethodNode != null)
                {
                    confirmTypeMethodNode.OnCallMethod += OnConfirmCallMethod;
                }


                // Add sub-notifiers
                AddNotifier(ServerNode, root, false);
                AddNotifier(root, machine, true);

                #region Create Trigger Alarms Methods
                
                Argument[] inputArgumentsChange = new Argument[]
                {
                    new Argument() {Name = "Interval (ms)", Description = "Alarm change values interval", DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar},
                };
                CreateMethod(root, "StartAllAlarmsChangeValues", inputArgumentsChange, null, OnStartAllAlarmsChangeValues);

                CreateMethod(root, "StopAllAlarmsChangeValues", null, null, OnStopAllAlarmsChangeValues);

                #endregion

            }
        }

        /// <summary>
        /// Calling AddComment on ConditionType is BadNodeIdInvalid because is an event type but not triggered from an alarm
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        protected ServiceResult AddCommentCallMethod(
           ISystemContext context,
           MethodState method,
           IList<object> inputArguments,
           IList<object> outputArguments)
        {
            if(inputArguments.Count !=2)
            {
                return StatusCodes.BadInvalidArgument;
            }
            byte[] eventId = (byte[])inputArguments[0];
            LocalizedText comment = (LocalizedText)inputArguments[1];

            return StatusCodes.BadNodeIdInvalid;
        }

        /// <summary>
        /// Filter out the alarms properties/components from address space 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        protected override void AddPredefinedNode(ISystemContext context, NodeState node)
        {
            BaseInstanceState instanceState = node as BaseInstanceState;
            if (instanceState != null &&
                instanceState.ReferenceTypeId == null)
            {
                return;
            }
            base.AddPredefinedNode(context, node);
        }

        /// <summary>
        /// Avoid alarm Acknowledge call on types
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        protected ServiceResult OnAcknowledgeCallMethod(
           ISystemContext context,
           MethodState method,
           IList<object> inputArguments,
           IList<object> outputArguments)
        {
            if (inputArguments.Count != 2)
            {
                return StatusCodes.BadInvalidArgument;
            }

            return StatusCodes.BadNodeIdInvalid;
        }

        /// <summary>
        /// Avoid alarm Confirm call on types
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        protected ServiceResult OnConfirmCallMethod(
           ISystemContext context,
           MethodState method,
           IList<object> inputArguments,
           IList<object> outputArguments)
        {
            if (inputArguments.Count != 2)
            {
                return StatusCodes.BadInvalidArgument;
            }

            return StatusCodes.BadNodeIdInvalid;
        }

        /// <summary>
        /// Create an instance of LimitAlarmConditionMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        private void CreateLimitAlarmMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {

            // Create an alarm monitor for a LimitAlarm sensor 1.
            LimitAlarmMonitor<LimitAlarmState> limitAlarmMonitor = new LimitAlarmMonitor<LimitAlarmState>(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit,
                this);

            if (limitAlarmMonitor != null)
            {
                m_conditionInstances.AddRange(limitAlarmMonitor.ConditionStates);
            }

            var alarm = m_conditionInstances[0] as LimitAlarmState;

            //remember node in node manager list
            AddPredefinedNode(SystemContext, limitAlarmMonitor);

        }

        /// <summary>
        /// Create an instance of ExclusiveLimitMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        private void CreateExclusiveLimitMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create an alarm monitor for a temperature sensor 1.
            ExclusiveLimitMonitor exclusiveLimitMonitor = new ExclusiveLimitMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit,
                this);

            if(exclusiveLimitMonitor != null)
            {
                m_conditionInstances.AddRange(exclusiveLimitMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, exclusiveLimitMonitor);
        }

        /// <summary>
        /// Create an instance of ExclusiveLevelMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        private void CreateExclusiveLevelMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create an alarm monitor for a ExclusiveLevel sensor 1.
            ExclusiveLevelMonitor exclusiveLevelMonitor = new ExclusiveLevelMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit,
                this);

            if (exclusiveLevelMonitor != null)
            {
                m_conditionInstances.AddRange(exclusiveLevelMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, exclusiveLevelMonitor);
        }

        /// <summary>
        /// Create an instance of ExclusiveDeviationMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        private void CreateExclusiveDeviationMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create an alarm monitor for a ExclusiveDeviation sensor 1.
            ExclusiveDeviationMonitor exclusiveDeviationMonitor = new ExclusiveDeviationMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit,
                this);

            if (exclusiveDeviationMonitor != null)
            {
                m_conditionInstances.AddRange(exclusiveDeviationMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, exclusiveDeviationMonitor);
        }

        /// <summary>
        /// Create an instance of ExclusiveRateOfChangeMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        private void CreateExclusiveRateOfChangeMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create an alarm monitor for a ExclusiveRateOfChange sensor 1.
            ExclusiveRateOfChangeMonitor exclusiveRateOfChangeMonitor = new ExclusiveRateOfChangeMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit,
                this);

            if (exclusiveRateOfChangeMonitor != null)
            {
                m_conditionInstances.AddRange(exclusiveRateOfChangeMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, exclusiveRateOfChangeMonitor);
        }


        /// <summary>
        /// Create an instance of NonExclusiveLimitMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        private void CreateNonExclusiveLimitMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create an alarm monitor for a NonExclusiveLimit sensor 1.
            NonExclusiveLimitMonitor nonExclusiveLimitMonitor = new NonExclusiveLimitMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit,
                this);

            if (nonExclusiveLimitMonitor != null)
            {
                m_conditionInstances.AddRange(nonExclusiveLimitMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, nonExclusiveLimitMonitor);
        }

        /// <summary>
        /// Create an instance of NonExclusiveLevelMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        private void CreateNonExclusiveLevelMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create an alarm monitor for a NonExclusiveLevel sensor 1.
            NonExclusiveLevelMonitor nonExclusiveLevelMonitor = new NonExclusiveLevelMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit,
                this);

            if (nonExclusiveLevelMonitor != null)
            {
                m_conditionInstances.AddRange(nonExclusiveLevelMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, nonExclusiveLevelMonitor);
        }

        /// <summary>
        /// Create an instance of NonExclusiveDeviationMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        private void CreateNonExclusiveDeviationMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create an alarm monitor for a NonExclusiveDeviation sensor 1.
            NonExclusiveDeviationMonitor nonExclusiveDeviationMonitor = new NonExclusiveDeviationMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit,
                this);

            if (nonExclusiveDeviationMonitor != null)
            {
                m_conditionInstances.AddRange(nonExclusiveDeviationMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, nonExclusiveDeviationMonitor);
        }

        /// <summary>
        /// Create an instance of NonExclusiveRateOfChangeMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="highLimit"></param>
        /// <param name="highHighLimit"></param>
        /// <param name="lowLimit"></param>
        /// <param name="lowLowLimit"></param>
        private void CreateNonExclusiveRateOfChangeMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
            // Create an alarm monitor for a NonExclusiveRateOfChange sensor 1.
            NonExclusiveRateOfChangeMonitor nonExclusiveRateOfChangeMonitor = new NonExclusiveRateOfChangeMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                highLimit,
                highHighLimit,
                lowLimit,
                lowLowLimit,
                this);

            if (nonExclusiveRateOfChangeMonitor != null)
            {
                m_conditionInstances.AddRange(nonExclusiveRateOfChangeMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, nonExclusiveRateOfChangeMonitor);
        }

        /// <summary>
        /// Create an instance of ConditionMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateConditionMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a Condition state sensor 1.
            ConditionMonitor conditionMonitor = new ConditionMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            //if (conditionMonitor != null)
            //{
            //    m_conditionInstances.AddRange(conditionMonitor.ConditionStates);
            //}

            //remember node in node manager list
            AddPredefinedNode(SystemContext, conditionMonitor);
        }

        /// <summary>
        /// Create an instance of AcknowledgeableConditionMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateAcknowledgeableConditionMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a AcknowledgeableCondition sensor 1.
            AcknowledgeableConditionMonitor acknowledgeableMonitor = new AcknowledgeableConditionMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (acknowledgeableMonitor != null)
            {
                m_conditionInstances.AddRange(acknowledgeableMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, acknowledgeableMonitor);
        }

        /// <summary>
        /// Create an instance of AlarmConditionMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateAlarmConditionMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a AlarmCondition sensor 1.
            AlarmConditionMonitor alarmConditionMonitor = new AlarmConditionMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (alarmConditionMonitor != null)
            {
                m_conditionInstances.AddRange(alarmConditionMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, alarmConditionMonitor);
        }

        /// <summary>
        /// Create an instance of DialogConditionMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateDialogConditionMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a DialogCondition sensor 1.
            DialogConditionMonitor dialogConditionMonitor = new DialogConditionMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (dialogConditionMonitor != null)
            {
                m_conditionInstances.AddRange(dialogConditionMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, dialogConditionMonitor);
        }
        
        /// <summary>
        /// Create an instance of DiscrepancyAlarmCondition and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateDiscrepancyAlarmMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a DiscrepancyAlarm sensor 1.
            DiscrepancyAlarmMonitor discrepancyAlarmMonitor = new DiscrepancyAlarmMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (discrepancyAlarmMonitor != null)
            {
                m_conditionInstances.AddRange(discrepancyAlarmMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, discrepancyAlarmMonitor);
        }

        /// <summary>
        /// Create an instance of DiscreteMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateDiscreteMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a Discrete sensor 1.
            DiscreteMonitor discreteMonitor = new DiscreteMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (discreteMonitor != null)
            {
                m_conditionInstances.AddRange(discreteMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, discreteMonitor);
        }

        /// <summary>
        /// Create an instance of OffNormalAlarmConditionMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateOffNormalAlarmMonitor(NodeState parent,
           string name,
           string alarmName,
           double initialValue)
        {
            // Create an alarm monitor for a OffNormalAlarm sensor 1.
            OffNormalAlarmMonitor offNormalAlarmMonitor = new OffNormalAlarmMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (offNormalAlarmMonitor != null)
            {
                m_conditionInstances.AddRange(offNormalAlarmMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, offNormalAlarmMonitor);
        }

        /// <summary>
        /// Create an instance of SystemOffNormalAlarmConditionMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateSystemOffNormalAlarmMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {
            // Create an alarm monitor for a SystemOffNormalAlarm sensor 1.
            SystemOffNormalAlarmMonitor systemOffNormalAlarmMonitor = new SystemOffNormalAlarmMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (systemOffNormalAlarmMonitor != null)
            {
                m_conditionInstances.AddRange(systemOffNormalAlarmMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, systemOffNormalAlarmMonitor);
        }

        /// <summary>
        /// Create an instance of CertificateExpirationMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateCertificateExpirationMonitor(NodeState parent,
           string name,
           string alarmName,
           double initialValue)
        {

            // Create an alarm monitor for a CertificateExpiration sensor 1.
            CertificateExpirationMonitor certificateExpirationMonitor = new CertificateExpirationMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (certificateExpirationMonitor != null)
            {
                m_conditionInstances.AddRange(certificateExpirationMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, certificateExpirationMonitor);
        }

        /// <summary>
        /// Create an instance of TrustListOutOfDateMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateTrustListOutOfDateMonitor(NodeState parent,
           string name,
           string alarmName,
           double initialValue)
        {

            // Create an alarm monitor for a TrustListOutOfDate sensor 1.
            TrustListOutOfDateMonitor trustListOutOfDateMonitor = new TrustListOutOfDateMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (trustListOutOfDateMonitor != null)
            {
                m_conditionInstances.AddRange(trustListOutOfDateMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, trustListOutOfDateMonitor);
        }

        /// <summary>
        /// Create an instance of CreateInstrumentDiagnosticMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateInstrumentDiagnosticMonitor(NodeState parent,
           string name,
           string alarmName,
           double initialValue)
        {

            // Create an alarm monitor for a InstrumentDiagnostic sensor 1.
            InstrumentDiagnosticMonitor instrumentDiagnosticMonitor = new InstrumentDiagnosticMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (instrumentDiagnosticMonitor != null)
            {
                m_conditionInstances.AddRange(instrumentDiagnosticMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, instrumentDiagnosticMonitor);
        }

        /// <summary>
        /// Create an instance of TripAlarmConditionMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateTripAlarmMonitor(NodeState parent,
           string name,
           string alarmName,
           double initialValue)
        {

            // Create an alarm monitor for a TripAlarm sensor 1.
            TripAlarmMonitor tripAlarmMonitor = new TripAlarmMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (tripAlarmMonitor != null)
            {
                m_conditionInstances.AddRange(tripAlarmMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, tripAlarmMonitor);
        }

        

        /// <summary>
        /// Create an instance of SystemDiagnosticConditionMonitor and set provided properties
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        private void CreateSystemDiagnosticMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a SystemDiagnosticAlarm sensor 1.
            SystemDiagnosticAlarmMonitor systemDiagnosticAlarmMonitor = new SystemDiagnosticAlarmMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue,
                this);

            if (systemDiagnosticAlarmMonitor != null)
            {
                m_conditionInstances.AddRange(systemDiagnosticAlarmMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, systemDiagnosticAlarmMonitor);
        }

        #endregion

        #region Private Methods - OnCall Alarm Event Handlers

        /// <summary>
        ///  Handles start change values for all monitors/alarms.
        /// </summary>
        private ServiceResult OnStartAllAlarmsChangeValues(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            // All arguments must be provided
            if (inputArguments.Count != 1)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {

                int triggerInterval = (int)inputArguments[0]; // "Alarm Timeout";

                m_allAlarmsChangeValues = new Timer(new TimerCallback(OnAllAlarmsChangeValues), null, triggerInterval, triggerInterval);

                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }

        }

        /// <summary>
        /// Change monitors/alarms values
        /// </summary>
        /// <param name="state"></param>
        private void OnAllAlarmsChangeValues(object state)
        {
            try
            {
                foreach (ConditionState ci in m_conditionInstances)
                {
                    if (ci.EnabledState.Id.Value == false)
                    {
                        // Enable the connection for this case
                        ci.SetEnableState(SystemContext, true);
                    }

                    if (ci.Parent != null)
                    {
                        BaseVariableState alarmMonitor = ci.Parent as BaseVariableState;
                        if (alarmMonitor != null)
                        {
                            BaseVariableState normalValue = (BaseVariableState)alarmMonitor.FindChild(SystemContext, new QualifiedName("NormalValueVariable", NamespaceIndex));
                            if (normalValue != null)
                            {
                                if (alarmMonitor.Value.Equals(normalValue.Value))
                                {
                                    alarmMonitor.Value = (double)alarmMonitor.Value + 1;
                                }
                                else
                                {
                                    alarmMonitor.Value = (double)normalValue.Value;
                                }
                            }
                            else
                            {
                                if (ci is LimitAlarmState)
                                {
                                    LimitAlarmState limitMonitorState = ci as LimitAlarmState;
                                    if (limitMonitorState != null)
                                    {
                                        double limitMonitorValue = (double)alarmMonitor.Value;

                                        double highLimit = limitMonitorState.HighLimit.Value;
                                        double highHighLimit = limitMonitorState.HighHighLimit.Value;
                                        double lowLimit = limitMonitorState.LowLimit.Value;
                                        double lowLowLimit = limitMonitorState.LowLowLimit.Value;

                                        if (limitMonitorValue > highHighLimit)
                                        {
                                            limitMonitorValue = lowLowLimit - 0.5;
                                        }
                                        else if (limitMonitorValue < highHighLimit && limitMonitorValue > highLimit)
                                        {
                                            limitMonitorValue = highHighLimit + 0.5;
                                        }
                                        else if (limitMonitorValue < highLimit && limitMonitorValue > lowLimit)
                                        {
                                            limitMonitorValue = highLimit + 0.5;
                                        }
                                        else if (limitMonitorValue < lowLimit && limitMonitorValue > lowLowLimit)
                                        {
                                            limitMonitorValue = lowLimit + 0.5;
                                        }
                                        else if (limitMonitorValue < lowLowLimit)
                                        {
                                            limitMonitorValue = lowLowLimit + 0.5;
                                        }

                                        alarmMonitor.Value = limitMonitorValue;
                                    }
                                }
                                else
                                {
                                    alarmMonitor.Value = (double)alarmMonitor.Value + 1;
                                }
                            }
                            alarmMonitor.ClearChangeMasks(SystemContext, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Alarm OnAllChangeMonitorsStart exception: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handles stop change values for all monitors/alarms.
        /// </summary>
        /// <param name="state"></param>
        private ServiceResult OnStopAllAlarmsChangeValues(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            try
            {
                foreach (ConditionState ci in m_conditionInstances)
                {
                    if (ci.EnabledState.Id.Value == true)
                    {
                        // Enable the connection for this case
                        ci.SetEnableState(SystemContext, false);
                    }
                }
                if (m_allAlarmsChangeValues != null)
                {
                    m_allAlarmsChangeValues.Dispose();
                    m_allAlarmsChangeValues = null;
                }
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Alarm OnTriggerChangeMonitorsStop exception: {0}", ex.Message);
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// /// <summary>
        /// An overrideable version of the Dispose
        /// </summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Utils.SilentDispose(m_allAlarmsChangeValues);
                m_allAlarmsChangeValues = null;
            }

            base.Dispose(disposing);
        }
        #endregion
    }
}