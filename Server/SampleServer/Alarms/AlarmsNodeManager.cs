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
        private Timer m_timer;

        //private readonly Random m_random = new Random();
        private bool m_valueChanged = false;

        Dictionary<string, NodeId> m_exclusiveLimitMonitors = new Dictionary<string, NodeId>();
        
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

                // Add sub-notifiers
                AddNotifier(ServerNode, root, false);
                AddNotifier(root, machine, true);

                m_timer = new Timer(new TimerCallback(OnTimeout), null, AlarmTimeout, AlarmTimeout);
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
            }

            //remember node in node manager list
            AddPredefinedNode(SystemContext, exclusiveLimitMonitor);
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

        #region Callbacks methods


        /// <summary>
        /// Handles a file changed event.
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

                                exclusiveLimitMonitor.UpdateAlarmMonitor(SystemContext,
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
                Console.WriteLine("Alarm exception: {0}", ex.Message);
            }
        }
        #endregion
    }
}