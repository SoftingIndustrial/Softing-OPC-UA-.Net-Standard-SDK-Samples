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
using System.Threading;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the Alarms and Conditions OPC UA feature
    /// </summary>
    public class AlarmsNodeManager : NodeManager
    {
        #region Private Fields
        private const int AlarmTimeout = 30000;
        //private Timer m_timer;
        private Timer m_exclusiveLimitAlarmTrigger;
        private Timer m_conditionAlarmTrigger;
        private Timer m_dialogConditionAlarmTrigger;
        private Timer m_acknowledgeableConditionAlarmTrigger;

        private Timer m_AllAlarmsTrigger;
        //private static NodeId m_alarmNodeId;

        //private readonly Random m_random = new Random();
        //private bool m_valueChanged = false;

        Dictionary<string, NodeId> m_exclusiveLimitMonitors = new Dictionary<string, NodeId>();

        List<ConditionState> m_conditionInstances = new List<ConditionState>();
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

                // Create an alarm monitor for a temperature sensor 1.
                CreateExclusiveLimitMonitor(
                    machine,
                    "TemperatureSensor 1",
                    "TemperatureMonitor 1",
                    30.0,
                    80.0,
                    100.0,
                    20.0,
                    10.0);

                // Create an alarm monitor for a temperature sensor 2.
                CreateExclusiveLimitMonitor(
                    machine,
                    "TemperatureSensor 2",
                    "TemperatureMonitor 2",
                    50.0,
                    90.0,
                    120.0,
                    30.0,
                    20.0);

                // Create an alarm monitor for a pressure sensor 1.
                CreateExclusiveLimitMonitor(
                    machine,
                    "PressureSensor 1",
                    "PressureMonitor 1",
                    5.0,
                    10.0,
                    15.0,
                    2.0,
                    1.0);

                // Create an alarm monitor for a pressure sensor 2.
                CreateExclusiveLimitMonitor(
                    machine,
                    "PressureSensor 2",
                    "PressureMonitor 2",
                    6.0,
                    15.0,
                    20.0,
                    4.0,
                    2.0);

                CreateConditionMonitor(
                    machine,
                    "ConditionSensor 1",
                    "ConditionMonitor 1",
                    7.0);

                CreateDialogConditionMonitor(
                    machine,
                    "DialogConditionSensor 1",
                    "DialogConditionMonitor 1",
                    7.0);

                CreateAcknowledgeableConditionMonitor(
                    machine,
                    "AcknowledgeableConditionSensor 1",
                    "AcknowledgeableConditionMonitor 1",
                    7.0);

                CreateAlarmConditionMonitor(
                    machine,
                    "AlarmConditionSensor 1",
                    "AlarmConditionMonitor 1",
                    7.0);

                // Add sub-notifiers
                AddNotifier(ServerNode, root, false);
                AddNotifier(root, machine, true);

                #region Create Trigger Alarm Method
                Argument[] inputArgumentsAdd = new Argument[]
                {
                    new Argument() {Name = "Alarm NodeId", Description = "Alarm NodeId", DataType = DataTypeIds.NodeId, ValueRank = ValueRanks.Scalar},
                    new Argument() {Name = "Alarm Enable/Disable", Description = "Alarm Enable/Disable", DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar},
                    new Argument() {Name = "Alarm Timeout", Description = "Alarm Timeout", DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar},
                };
               
                CreateMethod(root, "TriggerAlarm", inputArgumentsAdd, null, OnTriggerAlarmCall);


                Argument[] inputArgumentsStart = new Argument[]
                {
                    new Argument() {Name = "Interval (s)", Description = "Alarm Trigerring Interval", DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar},
                };
                CreateMethod(root, "StartAllAlarms", inputArgumentsStart, null, OnTriggerAllConditionsStartCall);

                CreateMethod(root, "StopAllAlarms", null, null, OnTriggerAllConditionsStop);
                #endregion

                // perhaps it might be used for stress test!?
                // m_timer = new Timer(new TimerCallback(OnTimeout), null, AlarmTimeout, AlarmTimeout);
            }
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
                lowLowLimit);

            if(exclusiveLimitMonitor != null)
            {
                m_exclusiveLimitMonitors.Add(alarmName, exclusiveLimitMonitor.NodeId);

                m_conditionInstances.AddRange(exclusiveLimitMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, exclusiveLimitMonitor);
        }

        private void CreateConditionMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a temperature sensor 1.
            ConditionMonitor conditionMonitor = new ConditionMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue);

            if (conditionMonitor != null)
            {
                m_conditionInstances.AddRange(conditionMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, conditionMonitor);
        }

        private void CreateDialogConditionMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a temperature sensor 1.
            DialogConditionMonitor conditionMonitor = new DialogConditionMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue);

            if (conditionMonitor != null)
            {
                m_conditionInstances.AddRange(conditionMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, conditionMonitor);
        }

        private void CreateAcknowledgeableConditionMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a temperature sensor 1.
            AcknowledgeableConditionMonitor conditionMonitor = new AcknowledgeableConditionMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue);

            if (conditionMonitor != null)
            {
                m_conditionInstances.AddRange(conditionMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, conditionMonitor);
        }

        private void CreateAlarmConditionMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue)
        {

            // Create an alarm monitor for a temperature sensor 1.
            AlarmConditionMonitor conditionMonitor = new AlarmConditionMonitor(
                SystemContext,
                parent,
                NamespaceIndex,
                name,
                alarmName,
                initialValue);

            if (conditionMonitor != null)
            {
                m_conditionInstances.AddRange(conditionMonitor.ConditionStates);
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, conditionMonitor);
        }

        private void UpdateExclusiveLimitMonitor(NodeState parent,
            string name,
            string alarmName,
            double initialValue,
            double highLimit,
            double highHighLimit,
            double lowLimit,
            double lowLowLimit)
        {
        }
        #endregion

        #region Timer methods


        /// <summary>
        /// Handles exclusive limit monitor changed event.
        /// </summary>
        private void OnTimeout(object state)
        {
            try
            {
                foreach(string alarmName in m_exclusiveLimitMonitors.Keys)
                {
                    NodeId exclusiveLimitMonitorNodeId = m_exclusiveLimitMonitors[alarmName];
                    if(exclusiveLimitMonitorNodeId != null)
                    {
                        ExclusiveLimitMonitor exclusiveLimitMonitor = (ExclusiveLimitMonitor)FindPredefinedNode(
                          ExpandedNodeId.ToNodeId(exclusiveLimitMonitorNodeId, Server.NamespaceUris),
                          typeof(ExclusiveLimitMonitor));

                        if(exclusiveLimitMonitor != null)
                        {
                            Opc.Ua.ExclusiveLimitAlarmState exclusiveLimitMonitorState = exclusiveLimitMonitor.FindChildBySymbolicName(SystemContext, alarmName) as Opc.Ua.ExclusiveLimitAlarmState;
                            if(exclusiveLimitMonitorState != null)
                            {
                                double exclusiveLimitMonitorValue = exclusiveLimitMonitor.Value;

                                double highLimit = exclusiveLimitMonitorState.HighLimit.Value;
                                double highHighLimit = exclusiveLimitMonitorState.HighHighLimit.Value;
                                double lowLimit = exclusiveLimitMonitorState.LowLimit.Value;
                                double lowLowLimit = exclusiveLimitMonitorState.LowLowLimit.Value;

                                if(exclusiveLimitMonitorValue > highHighLimit)
                                {
                                    exclusiveLimitMonitorValue = lowLowLimit - 0.5;
                                }
                                else if (exclusiveLimitMonitorValue < highHighLimit && exclusiveLimitMonitorValue > highLimit)
                                {
                                    exclusiveLimitMonitorValue = highHighLimit + 0.5;
                                }
                                else if (exclusiveLimitMonitorValue < highLimit && exclusiveLimitMonitorValue > lowLimit)
                                {
                                    exclusiveLimitMonitorValue = highLimit + 0.5;
                                }
                                else if (exclusiveLimitMonitorValue < lowLimit && exclusiveLimitMonitorValue > lowLowLimit)
                                {
                                    exclusiveLimitMonitorValue = lowLimit + 0.5;
                                }
                                else if (exclusiveLimitMonitorValue < lowLowLimit)
                                {
                                    exclusiveLimitMonitorValue = lowLowLimit + 0.5;
                                }

                                double newValue = exclusiveLimitMonitorValue;

                                // todo: add logic to change alarm limits!

                                exclusiveLimitMonitor.UpdateExclusiveLimitAlarmMonitor(SystemContext,
                                    newValue,
                                    highLimit,
                                    highHighLimit,
                                    lowLimit,
                                    lowLowLimit);

                                Console.WriteLine("Alarm '{0}' changed value: {1}", alarmName, newValue);
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Alarm. exception: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handles alarm enabled event.
        /// </summary>
        /// <param name="state"></param>
        private void OnExclusiveLimitAlarmEnabled(object state)
        {
            try
            {
                NodeId alarmNodeId = state as NodeId;
                if (alarmNodeId == null)
                {
                    throw new Exception("Alarm NodeId is missing!");
                }
                    
                Opc.Ua.ExclusiveLimitAlarmState exclusiveLimitMonitorState = (Opc.Ua.ExclusiveLimitAlarmState)FindPredefinedNode(
                         ExpandedNodeId.ToNodeId(alarmNodeId, Server.NamespaceUris),
                         typeof(Opc.Ua.ExclusiveLimitAlarmState));

                if (exclusiveLimitMonitorState != null)
                {
                    ExclusiveLimitMonitor exclusiveLimitMonitor = exclusiveLimitMonitorState.Parent as ExclusiveLimitMonitor;
                    if (exclusiveLimitMonitor != null)
                    {
                        double exclusiveLimitMonitorValue = exclusiveLimitMonitor.Value;

                        double highLimit = exclusiveLimitMonitorState.HighLimit.Value;
                        double highHighLimit = exclusiveLimitMonitorState.HighHighLimit.Value;
                        double lowLimit = exclusiveLimitMonitorState.LowLimit.Value;
                        double lowLowLimit = exclusiveLimitMonitorState.LowLowLimit.Value;

                        if (exclusiveLimitMonitorValue > highHighLimit)
                        {
                            exclusiveLimitMonitorValue = lowLowLimit - 0.5;
                        }
                        else if (exclusiveLimitMonitorValue < highHighLimit && exclusiveLimitMonitorValue > highLimit)
                        {
                            exclusiveLimitMonitorValue = highHighLimit + 0.5;
                        }
                        else if (exclusiveLimitMonitorValue < highLimit && exclusiveLimitMonitorValue > lowLimit)
                        {
                            exclusiveLimitMonitorValue = highLimit + 0.5;
                        }
                        else if (exclusiveLimitMonitorValue < lowLimit && exclusiveLimitMonitorValue > lowLowLimit)
                        {
                            exclusiveLimitMonitorValue = lowLimit + 0.5;
                        }
                        else if (exclusiveLimitMonitorValue < lowLowLimit)
                        {
                            exclusiveLimitMonitorValue = lowLowLimit + 0.5;
                        }

                        double newValue = exclusiveLimitMonitorValue;

                        // todo: add logic to change alarm limits!

                        exclusiveLimitMonitor.UpdateExclusiveLimitAlarmMonitor(SystemContext,
                            newValue,
                            highLimit,
                            highHighLimit,
                            lowLimit,
                            lowLowLimit);

                        Console.WriteLine("Alarm '{0}' changed value: {1}", exclusiveLimitMonitorState.DisplayName, newValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Alarm exception: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handles alarm enabled event.
        /// </summary>
        /// <param name="state"></param>
        private void OnConditionEnabled(object state)
        {
            try
            {
                NodeId alarmNodeId = state as NodeId;
                if (alarmNodeId == null)
                {
                    throw new Exception("Alarm NodeId is missing!");
                }


                Opc.Ua.ConditionState conditionMonitorState = (Opc.Ua.ConditionState)FindPredefinedNode(
                    ExpandedNodeId.ToNodeId(alarmNodeId, Server.NamespaceUris),
                    typeof(Opc.Ua.ConditionState));

                if (conditionMonitorState != null)
                {
                    ConditionMonitor conditionMonitor = conditionMonitorState.Parent as ConditionMonitor;
                    if (conditionMonitor != null)
                    {
                        bool alarmEnabled = false;
                        double newValue = conditionMonitor.Value;
                        LocalizedText conditionMonitorEnabled = conditionMonitorState.EnabledState.Value;
                        if (string.Equals(conditionMonitorEnabled.Text, "Enabled"))
                        {
                            conditionMonitorState.EnabledState.Value = "False";
                            alarmEnabled = false;
                        }
                        else
                        {
                            conditionMonitorState.EnabledState.Value = "True";
                            alarmEnabled = true;
                        }

                        conditionMonitor.UpdateConditionAlarmMonitor(SystemContext,
                            newValue++,
                            alarmEnabled);

                        Console.WriteLine("Alarm '{0}' enable state changed: {1}", conditionMonitorState.DisplayName, alarmEnabled);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Alarm exception: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handles alarm enabled event.
        /// </summary>
        /// <param name="state"></param>
        private void OnDialogConditionEnabled(object state)
        {
            try
            {
                NodeId alarmNodeId = state as NodeId;
                if (alarmNodeId == null)
                {
                    throw new Exception("Alarm NodeId is missing!");
                }


                Opc.Ua.DialogConditionState conditionMonitorState = (Opc.Ua.DialogConditionState)FindPredefinedNode(
                    ExpandedNodeId.ToNodeId(alarmNodeId, Server.NamespaceUris),
                    typeof(Opc.Ua.DialogConditionState));

                if (conditionMonitorState != null)
                {
                    ConditionMonitor conditionMonitor = conditionMonitorState.Parent as ConditionMonitor;
                    if (conditionMonitor != null)
                    {
                        bool alarmEnabled = false;
                        double newValue = conditionMonitor.Value;
                        LocalizedText conditionMonitorEnabled = conditionMonitorState.EnabledState.Value;
                        if (string.Equals(conditionMonitorEnabled.Text, "Enabled"))
                        {
                            conditionMonitorState.EnabledState.Value = "False";
                            alarmEnabled = false;
                        }
                        else
                        {
                            conditionMonitorState.EnabledState.Value = "True";
                            alarmEnabled = true;
                        }

                        conditionMonitor.UpdateConditionAlarmMonitor(SystemContext,
                            newValue++,
                            alarmEnabled);

                        Console.WriteLine("Alarm '{0}' enable state changed: {1}", conditionMonitorState.DisplayName, alarmEnabled);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Alarm exception: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handles alarm enabled event.
        /// </summary>
        /// <param name="state"></param>
        private void OnAcknowledgeableConditionEnabled(object state)
        {
            try
            {
                NodeId alarmNodeId = state as NodeId;
                if (alarmNodeId == null)
                {
                    throw new Exception("Alarm NodeId is missing!");
                }


                Opc.Ua.DialogConditionState conditionMonitorState = (Opc.Ua.DialogConditionState)FindPredefinedNode(
                    ExpandedNodeId.ToNodeId(alarmNodeId, Server.NamespaceUris),
                    typeof(Opc.Ua.DialogConditionState));

                if (conditionMonitorState != null)
                {
                    ConditionMonitor conditionMonitor = conditionMonitorState.Parent as ConditionMonitor;
                    if (conditionMonitor != null)
                    {
                        bool alarmEnabled = false;
                        double newValue = conditionMonitor.Value;
                        LocalizedText conditionMonitorEnabled = conditionMonitorState.EnabledState.Value;
                        if (string.Equals(conditionMonitorEnabled.Text, "Enabled"))
                        {
                            conditionMonitorState.EnabledState.Value = "False";
                            alarmEnabled = false;
                        }
                        else
                        {
                            conditionMonitorState.EnabledState.Value = "True";
                            alarmEnabled = true;
                        }

                        conditionMonitor.UpdateConditionAlarmMonitor(SystemContext,
                            newValue++,
                            alarmEnabled);

                        Console.WriteLine("Alarm '{0}' enable state changed: {1}", conditionMonitorState.DisplayName, alarmEnabled);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Alarm exception: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handles all alarms enabled event.
        /// </summary>
        /// <param name="state"></param>
        private void OnAllConditionsStart(object state)
        {
            try
            {
                var inputs = new List<Variant>();

                foreach (ConditionState ci in m_conditionInstances)
                {
                    MethodState enableMethod = (MethodState)ci.FindChild(Server.DefaultSystemContext, new QualifiedName("Enable"));
                    if (enableMethod != null)
                    {
                        enableMethod.Call(Server.DefaultSystemContext, ci.NodeId, inputs, null, null);
                    }
                    MethodState disableMethod = (MethodState)ci.FindChild(Server.DefaultSystemContext, new QualifiedName("Disable"));
                    if (enableMethod != null)
                    {
                        disableMethod.Call(Server.DefaultSystemContext, ci.NodeId, inputs, null, null);
                    }
                }
                

            }
            catch (Exception ex)
            {
                Console.WriteLine("Alarm exception: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handles all alarms enabled event.
        /// </summary>
        /// <param name="state"></param>
        private ServiceResult OnTriggerAllConditionsStop(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            try
            {
                if (m_AllAlarmsTrigger != null)
                {
                    m_AllAlarmsTrigger.Dispose();
                    m_AllAlarmsTrigger = null;
                }
                return ServiceResult.Good;
            }
            catch (Exception ex)
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        #endregion

        #region Private Methods - OnCall Event Handlers

        /// <summary>
        /// Handles the trigger alarm method call
        /// </summary>
        private ServiceResult OnTriggerAlarmCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            // All arguments must be provided
            if (inputArguments.Count < 3)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                NodeId alarmNodeId = (NodeId)inputArguments[0]; 
                bool alarmEnabled = (bool)inputArguments[1]; // "Enabled/Disabled";
                int alarmTimeout = (int)inputArguments[2]; // "Alarm Timeout";

                Opc.Ua.ExclusiveLimitAlarmState exclusiveLimitMonitorState = (Opc.Ua.ExclusiveLimitAlarmState)FindPredefinedNode(
                         ExpandedNodeId.ToNodeId(alarmNodeId, Server.NamespaceUris),
                         typeof(Opc.Ua.ExclusiveLimitAlarmState));

                Opc.Ua.ConditionState conditionMonitorState = (Opc.Ua.ConditionState)FindPredefinedNode(
                         ExpandedNodeId.ToNodeId(alarmNodeId, Server.NamespaceUris),
                         typeof(Opc.Ua.ConditionState));

                Opc.Ua.DialogConditionState dialogConditionMonitorState = (Opc.Ua.DialogConditionState)FindPredefinedNode(
                         ExpandedNodeId.ToNodeId(alarmNodeId, Server.NamespaceUris),
                         typeof(Opc.Ua.DialogConditionState));

                Opc.Ua.DialogConditionState acknowledgeableConditionMonitorState = (Opc.Ua.DialogConditionState)FindPredefinedNode(
                         ExpandedNodeId.ToNodeId(alarmNodeId, Server.NamespaceUris),
                         typeof(Opc.Ua.AcknowledgeableConditionState));

                if (alarmEnabled)
                {
                    if (exclusiveLimitMonitorState != null)
                    {
                        m_exclusiveLimitAlarmTrigger = new Timer(new TimerCallback(OnExclusiveLimitAlarmEnabled), alarmNodeId, alarmTimeout, alarmTimeout);
                    }
                    if (conditionMonitorState != null)
                    {
                        m_conditionAlarmTrigger = new Timer(new TimerCallback(OnConditionEnabled), alarmNodeId, alarmTimeout, alarmTimeout);
                    }
                    if (dialogConditionMonitorState != null)
                    {
                        m_dialogConditionAlarmTrigger = new Timer(new TimerCallback(OnDialogConditionEnabled), alarmNodeId, alarmTimeout, alarmTimeout);
                    }
                    if (acknowledgeableConditionMonitorState != null)
                    {
                        m_acknowledgeableConditionAlarmTrigger = new Timer(new TimerCallback(OnAcknowledgeableConditionEnabled), alarmNodeId, alarmTimeout, alarmTimeout);
                    }
                }
                else
                {
                    if (exclusiveLimitMonitorState != null)
                    {
                        m_exclusiveLimitAlarmTrigger.Dispose();
                        m_exclusiveLimitAlarmTrigger = null;
                    }
                    if (conditionMonitorState != null)
                    {
                        m_conditionAlarmTrigger.Dispose();
                        m_conditionAlarmTrigger = null;
                    }
                    if (dialogConditionMonitorState != null)
                    {
                        m_dialogConditionAlarmTrigger.Dispose();
                        m_dialogConditionAlarmTrigger = null;
                    }
                    if (acknowledgeableConditionMonitorState != null)
                    {
                        m_acknowledgeableConditionAlarmTrigger.Dispose();
                        m_acknowledgeableConditionAlarmTrigger = null;
                    }

                }
                
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }

        }

      
        /// <summary>
        /// Handles the trigger alarm method call
        /// </summary>
        private ServiceResult OnTriggerAllConditionsStartCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            // All arguments must be provided
            if (inputArguments.Count != 1)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
               
                int triggerInterval = (int)inputArguments[0]; // "Alarm Timeout";

                m_AllAlarmsTrigger = new Timer(new TimerCallback(OnAllConditionsStart), null, triggerInterval, triggerInterval);

                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }

        }
        #endregion
    }
}