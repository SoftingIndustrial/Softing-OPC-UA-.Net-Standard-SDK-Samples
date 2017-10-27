﻿/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;
using Softing.Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that contains sample code for alarms functionality
    /// </summary>
    class AlarmsClient
    {
        #region Private Fields
        private const string SessionName = "AlarmsClient Session";
        private const string SubscriptionName = "AlarmsClient Subscription";
        // Objects\Alarms
        private static readonly string m_alarmsModuleNodeId = "ns=2;i=2"; 

        private readonly UaApplication m_application;
        private ClientSession m_session;
        private readonly Dictionary<NodeId, EventDetails> m_retainedAlarms;
        private ClientSubscription m_subscription;
        private ClientMonitoredItem m_alarmsMonitoredItem;
       
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of AlarmsClient
        /// </summary>
        /// <param name="application"></param>
        public AlarmsClient(UaApplication application)
        {
            m_application = application;
            m_retainedAlarms = new Dictionary<NodeId, EventDetails>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Invokes the ConditionRefresh method in order to receive all retained conditions
        /// </summary>
        public void ConditionRefresh()
        {
            if (m_alarmsMonitoredItem == null)
            {
                InitializeAlarmsMonitoredItem();
            }
            try
            {
                if (m_session != null && m_alarmsMonitoredItem != null)
                {
                    // Clear the local list of alarms
                    m_retainedAlarms.Clear();

                    // Invoke the ConditionRefresh method on the server passing the sessionId
                    // After this call the server should send new event notifications for all the retained (active) alarms
                    m_session.Call(ObjectTypeIds.ConditionType,
                        MethodIds.ConditionType_ConditionRefresh,
                        new List<object>(1) { m_subscription.Id },
                        out var outputArgs);

                    Console.WriteLine("ConditionRefresh method invoked.");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("ConditionRefresh Error : {0}.", exception.Message);
            }
        }

        /// <summary>
        /// Allows user to add comment to alarm
        /// </summary>
        public void AddCommentToAlarm()
        {
            if (m_session == null)
            {
                Initialize();
            }
            try
            {
                //check to see if there are any known alarms 
                if (m_retainedAlarms.Count == 0)
                {
                    Console.WriteLine("The list of active alarms is empty!");
                    return;
                }

                Dictionary<int, NodeId> alarmsList = new Dictionary<int, NodeId>();
                int index = 1;

                // Prompt the user to select the alarm from the list of active alarms
                Console.WriteLine("Please select the alarm to acknowledge:");

                foreach (EventDetails alarmDetails in m_retainedAlarms.Values)
                {
                    Console.WriteLine("{0} - Alarm with SourceName = {1}", index, alarmDetails.SourceName);

                    //remember alarms in list with index
                    alarmsList[index] = alarmDetails.SourceNode;
                    index++;
                }
                //read user option
                int selectedIndex = Convert.ToInt32(Console.ReadLine());

                if (!alarmsList.ContainsKey(selectedIndex))
                {
                    Console.WriteLine("Invalid option.\r\n");
                    return;
                }

                EventDetails selectedAlarm = m_retainedAlarms[alarmsList[selectedIndex]];

                Console.Write("Please insert a comment: ");
                string comment = Console.ReadLine();

                // Invoke Acknowledge method
                List<object> inputArgs = new List<object>(2);
                inputArgs.Add(selectedAlarm.EventId);
                inputArgs.Add(new LocalizedText(comment));

                m_session.Call(selectedAlarm.EventNode, MethodIds.ConditionType_AddComment,
                    inputArgs, out var outputArgs);
                Console.WriteLine("AddComment request sent for alarm with SourceName = {0}", selectedAlarm.SourceName);
            }
            catch (Exception exception)
            {
                Console.WriteLine("AcknowledgeAlarms Error : {0}.", exception);
            }
        }

        /// <summary>
        /// Allows user to acknowledge alarm
        /// </summary>
        public void AcknowledgeAlarm()
        {
            if (m_session == null)
            {
                Initialize();
            }
            try
            {
                //check to see if there are any known alarms 
                if (m_retainedAlarms.Count == 0)
                {
                    Console.WriteLine("The list of active alarms is empty!");
                    return;
                }

                Dictionary<int, NodeId> alarmsList = new Dictionary<int, NodeId>();
                int index = 1;

                // Prompt the user to select the alarm from the list of active alarms
                Console.WriteLine("Please select the alarm to acknowledge:");

                foreach (EventDetails alarmDetails in m_retainedAlarms.Values)
                {
                    Console.WriteLine("{0} - Alarm with SourceName = {1}", index, alarmDetails.SourceName);

                    //remember alarms in list with index
                    alarmsList[index] = alarmDetails.SourceNode;
                    index++;
                }
                //read user option
                int selectedIndex = Convert.ToInt32(Console.ReadLine());

                if (!alarmsList.ContainsKey(selectedIndex))
                {
                    Console.WriteLine("Invalid option.\r\n");
                    return;
                }

                EventDetails selectedAlarm = m_retainedAlarms[alarmsList[selectedIndex]];

                Console.Write("Please insert a comment: ");
                string comment = Console.ReadLine();

                // Invoke Acknowledge method
                List<object> inputArgs = new List<object>(2);
                inputArgs.Add(selectedAlarm.EventId);
                inputArgs.Add(new LocalizedText(comment));

                m_session.Call(selectedAlarm.EventNode, MethodIds.AcknowledgeableConditionType_Acknowledge,
                    inputArgs, out var outputArgs);
                Console.WriteLine("Acknowledge request sent for alarm with SourceName = {0}", selectedAlarm.SourceName);
            }
            catch (Exception exception)
            {
                Console.WriteLine("AcknowledgeAlarms Error : {0}.", exception);
            }
        }

        /// <summary>
        /// delete the MonitoredItem for the Alarms node
        /// </summary>
        public void DeleteAlarmsMonitoredItem()
        {
            if (m_alarmsMonitoredItem != null)
            {
                m_alarmsMonitoredItem.EventsReceived -= m_alarmsMonitoredItem_EventsReceived;
                m_alarmsMonitoredItem.Disconnect(true);

                m_alarmsMonitoredItem = null;
                Console.WriteLine("Alarms monitored item was deleted!");
            }
            else
            {
                Console.WriteLine("Alarms monitored item is not created!");
            }
        } 
        #endregion

        #region Initialize & Disconnect Session
        /// <summary>
        /// Initialize session and subscription
        /// </summary>
        public void Initialize()
        {
            if (m_session == null)
            {
                // create the session object.            
                m_session = m_application.CreateSession(
                    Constants.SampleServerUrlOpcTcp,
                    //"opc.tcp://localhost:62550/AlarmsServer",
                    MessageSecurityMode.None,
                    SecurityPolicy.None,
                    MessageEncoding.Binary,
                    new UserIdentity(),
                    null);
                m_session.SessionName = SessionName;

                try
                {
                    //connect session
                    m_session.Connect(false, true);
                    Console.WriteLine("Session is connected.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("CreateSession Error: {0}", ex);
                }
            }

            if (m_subscription == null)
            {
                //create the subscription
                m_subscription = new ClientSubscription(m_session, SubscriptionName);

                // set the Publishing interval for this subscription
                m_subscription.PublishingInterval = 500;
                Console.WriteLine("Subscription created");
            }

            InitializeAlarmsMonitoredItem();
        }

        /// <summary>
        /// Create a MonitoredItem for the alarms node
        /// </summary>
        public void InitializeAlarmsMonitoredItem()
        {
            try
            {
                // Configure the event filter
                EventFilterEx filter = new EventFilterEx();

                // specify the required fields of the events
                filter.AddSelectClause(ObjectTypes.BaseEventType, String.Empty, Attributes.NodeId);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.EventId);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.EventType);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.SourceNode);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.SourceName);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Time);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Message);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Severity);
                filter.AddSelectClause(ObjectTypeIds.AcknowledgeableConditionType, BrowseNames.EnabledState);
                filter.AddSelectClause(ObjectTypeIds.AcknowledgeableConditionType, BrowseNames.ActiveState);
                filter.AddSelectClause(ObjectTypeIds.AcknowledgeableConditionType, BrowseNames.AckedState);
                filter.AddSelectClause(ObjectTypeIds.AcknowledgeableConditionType, BrowseNames.Comment);
                filter.AddSelectClause(ObjectTypeIds.AcknowledgeableConditionType, BrowseNames.Retain);

                // filter only for condition related events ( e.g in order to avoid audit events)
                filter.WhereClause.Push(FilterOperator.OfType, ObjectTypeIds.ConditionType);

                // Create the MonitoredItem used to receive event notifications and pass the filter object
                m_alarmsMonitoredItem = new ClientMonitoredItem(m_subscription, m_alarmsModuleNodeId, "Alarms monitor item", filter);
                m_alarmsMonitoredItem.SamplingInterval = 100;
                m_alarmsMonitoredItem.QueueSize = 1000;

                m_alarmsMonitoredItem.EventsReceived += m_alarmsMonitoredItem_EventsReceived;

                Console.WriteLine("Alarms MonitoredItem created for NodeId ({0}).", m_alarmsMonitoredItem.NodeId);
            }
            catch (Exception exception)
            {
                // Log Error
                Console.WriteLine("Create Alarms MonitoredItem: {0}", exception);
            }
        }

        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public virtual void DisconnectSession()
        {
            try
            {
                if (m_alarmsMonitoredItem != null)
                {
                    DeleteAlarmsMonitoredItem();
                }
                //disconnect subscription
                if (m_subscription != null)
                {
                    m_subscription.Disconnect(true);
                    m_subscription.Delete();
                    m_subscription = null;
                    Console.WriteLine("Subscription is deleted.");
                }
                if (m_session != null)
                {
                    m_session.Disconnect(true);
                    m_session.Dispose();
                    m_session = null;
                    Console.WriteLine("Session is disconnected.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DisconnectSession Error: {0}", ex.Message);
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle EventsReceived event for alarms MonitoredItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_alarmsMonitoredItem_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
            try
            {
                if (m_session == null)
                {
                    return;
                }

                // Check for event notification

                foreach (EventFieldListEx eventNotification in e.EventNotifications)
                {
                    INode eventType = m_alarmsMonitoredItem.GetEventType(eventNotification);

                    if (eventType == null)
                    {
                        Console.WriteLine("Event cannot be processed.");
                        return;
                    }

                    if (eventType.NodeId == ObjectTypeIds.RefreshStartEventType)
                    {
                        Console.WriteLine("\r\nRefreshStart event received.");
                        return;
                    }

                    if (eventType.NodeId == ObjectTypeIds.RefreshEndEventType)
                    {
                        Console.WriteLine("RefreshEnd event received.");
                        return;
                    }

                    // Display the list of event fields
                    StringBuilder eventFields = new StringBuilder();
                    eventFields.Append("\r\nNew alarm notification received: \r\n");
                    eventFields.AppendFormat("EventId      = {0}\r\n", eventNotification.EventFields[1]);
                    eventFields.AppendFormat("EventType    = {0}\r\n", eventType);
                    eventFields.AppendFormat("SourceNode   = {0}\r\n", eventNotification.EventFields[3]);
                    eventFields.AppendFormat("SourceName   = {0}\r\n", eventNotification.EventFields[4]);
                    eventFields.AppendFormat("SourceName   = {0}\r\n", eventNotification.EventFields[4]);
                    eventFields.AppendFormat("Time         = {0:HH:mm:ss.fff}\r\n",
                        ((DateTime) eventNotification.EventFields[5].Value).ToLocalTime());
                    eventFields.AppendFormat("Message      = {0}\r\n", eventNotification.EventFields[6]);
                    eventFields.AppendFormat("Severity     = {0}\r\n",
                        (EventSeverity) ((ushort) eventNotification.EventFields[7].Value));
                    eventFields.AppendFormat("EnabledState = {0}\r\n", eventNotification.EventFields[8]);
                    eventFields.AppendFormat("ActiveState  = {0}\r\n", eventNotification.EventFields[9]);
                    eventFields.AppendFormat("AckedState   = {0}\r\n", eventNotification.EventFields[10]);
                    eventFields.AppendFormat("Comment      = {0}\r\n", eventNotification.EventFields[11]);
                    eventFields.AppendFormat("Retain       = {0}\r\n", eventNotification.EventFields[12]);
                      
                    //Display alarm information
                    Console.WriteLine(eventFields);
                    // check if Retain and SourceNode fields are received
                    if (eventNotification.EventFields[3] != Variant.Null && eventNotification.EventFields[12] != Variant.Null)
                    {
                        bool retain = (bool)eventNotification.EventFields[12].Value;
                        NodeId sourceNode = (NodeId)eventNotification.EventFields[3].Value;

                        // Update the list of active alarms and store only the events with "retain" bit set to true.
                        if (retain)
                        {
                            EventDetails eventDetails = new EventDetails();
                            eventDetails.EventNode = (NodeId)eventNotification.EventFields[0].Value;
                            eventDetails.EventId = (byte[])eventNotification.EventFields[1].Value;
                            eventDetails.SourceNode = sourceNode;
                            eventDetails.SourceName = eventNotification.EventFields[4].Value.ToString();
                            eventDetails.Message = (LocalizedText)eventNotification.EventFields[6].Value;
                            eventDetails.Severity = (EventSeverity)((ushort)eventNotification.EventFields[7].Value);
                            eventDetails.Comment = (LocalizedText)eventNotification.EventFields[11].Value;

                            m_retainedAlarms[sourceNode] = eventDetails;
                        }
                        else
                        {
                            m_retainedAlarms.Remove(sourceNode);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("MonitoredItem Notification Error : {0}.", exception);
            }
        } 
        #endregion
    }
}
