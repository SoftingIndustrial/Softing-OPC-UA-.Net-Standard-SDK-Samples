/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using Opc.Ua;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Softing.Opc.Ua;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Trace;
using ClientSession = Softing.Opc.Ua.Client.ClientSession;

namespace SampleClient
{
    class Client
    {

        #region Private Members
        private const string m_demoServerUrl = "opc.tcp://[::1]:51510/UA/DemoServer";
        //Browse path: Root\Objects\Data\Static\Scalar\Int16Value
        private const string m_readWriteNodeId = "ns=3;i=10219";
        //Browse path: Root\Objects\Data\Dynamic\Scalar\ByteValue
        private const string m_monitoredItemNodeId = "ns=3;i=10846";
        // Configured Nodes
        private string m_alarmsModuleNodeId = "ns=2;i=1"; // Objects\Alarms Module

        private UaApplication m_application;
        private ClientSession m_session = null;
        private NamespaceTable m_namespaceUris;
        private ClientSubscription m_subscription = null;       
        private List<ClientMonitoredItem> m_monitoredItems = new List<ClientMonitoredItem>();
        private ClientMonitoredItem m_eventMonitoredItem;
        private ClientMonitoredItem m_alarmsMonitoredItem;
        private ClientMonitoredItem m_readWriteMonitoredItem = null;

        //Browse path: Root\Objects\Server
        private static NodeId m_eventSourceNodeId = new NodeId("ns=0;i=2253");

        //Browse path: Root\Types\ObjectTypes\BaseObjectType\BaseEventType
        private static NodeId m_selectEventTypeId = new NodeId("ns=0;i=2041");

        //Browse path: Root\Types\ObjectTypes\BaseObjectType\BaseEventType
        private static NodeId m_eventTypeId = new NodeId("ns=5;i=265");

        private NodeId m_historianNodeId = new NodeId("ns=2;s=StaticHistoricalDataItem_Historian1");
        private static QualifiedName m_eventPropertyName = new QualifiedName("FluidLevel", 5);       
        
        private Dictionary<NodeId, EventDetails> m_retainedAlarms = new Dictionary<NodeId, EventDetails>();
        private Random m_randomGenerator = new Random();
        private int m_callIdentifier;

       

        #endregion

        public Client(UaApplication application)
        {
            m_application = application;
        }

        #region Session
        /// <summary>
        /// Creates a new session and connects it to the server.
        /// </summary>
        internal void CreateSession()
        {
            if (m_session != null)
            {
                Console.WriteLine("Session already created.");
                return;
            }

            UserNameIdentityToken userToken = new UserNameIdentityToken();
            userToken.UserName = "usr";
            userToken.DecryptedPassword = "pwd";


            UserIdentity userIdentity = new UserIdentity();
            // create the session object.            
            m_session = m_application.CreateSession(
               m_demoServerUrl,
               MessageSecurityMode.None,
               SecurityPolicy.None,
               MessageEncoding.Binary,
               userIdentity,
               null);
            m_session.CallCompleted += Session_CallCompleted;
            m_session.ContinuationPointReached += Session_ContinuationPointReached;
            m_session.SessionName = "Softing Browse Sample Client";

            try
            {
                m_session.Connect(false, true);
                Console.WriteLine("Session is connected.");

                m_namespaceUris = new NamespaceTable(m_session.NamespaceUris);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("CreateSession Error: {0}", ex));
            }

            // m_session.ContinuationPointReached += Session_ContinuationPointReached;
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        internal void DisconnectSession()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                m_subscription = null;
                m_monitoredItems.Clear();
                m_readWriteMonitoredItem = null;
                m_eventMonitoredItem = null;
                m_alarmsMonitoredItem = null;

                m_session.CallCompleted -= Session_CallCompleted;
                m_session.ContinuationPointReached -= Session_ContinuationPointReached;
                m_session.Disconnect(false);
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("DisconnectSession Error: {0}", ex.Message));
            }

            m_session.Dispose();
            m_session = null;
        }

        #endregion

        #region Subscription
        /// <summary>
        /// Creates a subscription. The subscription is activated in the constructor if the session is active as well.
        /// </summary>
        internal void CreateSubscription()
        {
            if (m_session != null)
            {
                if (m_subscription == null)
                {
                    try
                    {
                        m_subscription = new ClientSubscription(m_session, "SampleSubscription");

                        // set the Publishing interval for this subscription
                        m_subscription.PublishingInterval = 500;
                        m_subscription.DataChangesReceived += Subscription_DataChangesReceived;
                        m_subscription.EventsReceived += Subscription_EventsReceived;
                        Console.WriteLine("Subscription created");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Subscription already created");
                }
            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
            }
        }

        private void Subscription_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
        }

        /// <summary>
        /// Deletes the current subscription.
        /// </summary>
        internal void DeleteSubscription()
        {
            if (m_session != null)
            {
                if (m_subscription != null)
                {
                    m_monitoredItems.Clear();
                    m_readWriteMonitoredItem = null;
                    m_eventMonitoredItem = null;
                    m_alarmsMonitoredItem = null;

                    m_subscription.DataChangesReceived -= Subscription_DataChangesReceived;
                    m_subscription.EventsReceived -= Subscription_EventsReceived;
                    m_session.DeleteSubscription(m_subscription);
                    m_subscription = null;
                    Console.WriteLine("Subscription deleted");
                }
                else
                {
                    Console.WriteLine("Subscription is not created, please use \"s\" command");
                }

            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
            }
        }


        private void Subscription_DataChangesReceived(object sender, DataChangesNotificationEventArgs e)
        {
            foreach (var dataChangeNotification in e.DataChangeNotifications)
            {
                Console.WriteLine(" {0} Received data value change for monitored item: '{1}' in subscription '{2}':", dataChangeNotification.SequenceNo, dataChangeNotification.MonitoredItem.DisplayName, ((ClientSubscription)sender).DisplayName);
                Console.WriteLine("    Value : {0} ", dataChangeNotification.Value);
                Console.WriteLine("    StatusCode : {0} ", dataChangeNotification.Value.StatusCode);
                Console.WriteLine("    ServerTimestamp : {0}", dataChangeNotification.Value.ServerTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt"));
                Console.WriteLine("    SourceTimestamp : {0}", dataChangeNotification.Value.SourceTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt"));
            }
        }
        #endregion

        #region MonitoredItem
        /// <summary>
        /// Creates a monitoredItem. The monitored item is activated in the constructor if the subscription is active as well.
        /// </summary>
        internal void CreateMonitoredItem()
        {
            if (m_session != null)
            {
                if (m_subscription != null)
                {
                    try
                    {
                        NodeId node = new NodeId(m_monitoredItemNodeId);
                        ClientMonitoredItem monitoredItem = new ClientMonitoredItem(m_subscription, node, AttributeId.Value, null, "Sample Monitored Item" + m_monitoredItems.Count);
                        monitoredItem.DataChangesReceived += Monitoreditem_DataChangesReceived;
                        m_monitoredItems.Add(monitoredItem);

                        Console.WriteLine("Monitored item created. Data value changes are shown:");
                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Subscription is not created, please use \"s\" command");
                }
            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
            }
        }

        /// <summary>
        /// Invokes the ConditionRefresh method in order to receive all retained conditions
        /// </summary>
        public void ConditionRefresh()
        {
            try
            {
                if (m_session != null && m_alarmsMonitoredItem != null)
                {
                    // Clear the local list of alarms
                    m_retainedAlarms.Clear();

                    // Invoke the ConditionRefresh method on the server passing the sessionId
                    // After this call the server should send new event notifications for all the retained (active) alarms
                    IList<object> outputArgs;
                    m_session.Call(ObjectTypeIds.ConditionType, MethodIds.ConditionType_ConditionRefresh, new List<object>(1) { m_subscription.Id }, out outputArgs);

                    Console.WriteLine("ConditionRefresh method invoked.");
                }
                else
                {
                    Console.WriteLine("Session not connected or subscription not created!");
                }
            }
            catch (Exception exception)
            {
                // Log Error
                string logMessage = String.Format("ConditionRefresh Error : {0}.", exception.Message);
                TraceService.Log(TraceSources.User3, "AlarmsClient.ConditionRefresh", exception);
                Console.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Allows user to acknowledge alarm
        /// </summary>
        public void AcknowledgeAlarm()
        {
            try
            {
                if (m_session != null && m_subscription != null)
                {
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
                        Console.WriteLine(String.Format("{0} - Alarm with SourceName = {1}", index, alarmDetails.SourceName));

                        alarmsList[index] = alarmDetails.SourceNode;
                        index++;
                    }

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
                    IList<object> outputArgs;

                    m_session.Call(selectedAlarm.EventNode, MethodIds.AcknowledgeableConditionType_Acknowledge,
                                   inputArgs, out outputArgs);
                    Console.WriteLine(String.Format("Acknowledge request sent for alarm with SourceName = {0}", selectedAlarm.SourceName));
                }
                else
                {
                    Console.WriteLine("Session not connected!");
                }
            }
            catch (Exception exception)
            {
                // Log Error
                string logMessage = String.Format("AcknowledgeAlarms Error : {0}.", exception.Message);
                
                Console.WriteLine(logMessage);
            }
        }
        /// <summary>
        /// Handles the Notification event of the Monitoreditem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Softing.Opc.Ua.Toolkit.Client.MonitoredItemNotificationEventArgs"/> instance containing the event data.</param>
        private void Monitoreditem_DataChangesReceived(object sender, DataChangesNotificationEventArgs e)
        {
            foreach (var dataChangeNotification in e.DataChangeNotifications)
            {
                Console.WriteLine(" {0} Received data value change for monitored item:", dataChangeNotification.SequenceNo);
                Console.WriteLine("    Value : {0} ", dataChangeNotification.Value);
                Console.WriteLine("    StatusCode : {0} ", dataChangeNotification.Value.StatusCode);
                Console.WriteLine("    ServerTimestamp : {0}", dataChangeNotification.Value.ServerTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt"));
                Console.WriteLine("    SourceTimestamp : {0}", dataChangeNotification.Value.SourceTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt"));
            }
        }

        /// <summary>
        /// Reads the value of a node, using a monitored item.
        /// </summary>
        /// <returns>The datavalue for the read monitored item</returns>
        public DataValue ReadMonitoredItem()
        {
            if (m_session != null)
            {
                if (m_subscription != null)
                {
                    if (!m_subscription.MonitoredItems.Contains(m_readWriteMonitoredItem))
                    {
                        NodeId readWriteNodeId = new NodeId(m_readWriteNodeId);
                        try
                        {
                            m_readWriteMonitoredItem = new ClientMonitoredItem(m_subscription, readWriteNodeId, AttributeId.Value, null, "SampleReadWriteMI");
                            Console.WriteLine("Created Monitored Item for read/write.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Monitored item could not created: " + ex.Message);
                        }
                    }
                    try
                    {
                        return m_readWriteMonitoredItem.Read();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("Subscription is not created, please use \"s\" command");
                    return null;
                }

            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return null;
            }
        }

        /// <summary>
        /// Writes a value to a node, using a monitored item.
        /// </summary>
        /// <returns>The status code of the write operation.</returns>
        public StatusCode WriteMonitoredItem()
        {
            if (m_session != null)
            {
                if (m_subscription != null)
                {
                    if (!m_subscription.MonitoredItems.Contains(m_readWriteMonitoredItem))
                    {
                        NodeId readWriteNodeId = new NodeId(m_readWriteNodeId);
                        try
                        {
                            m_readWriteMonitoredItem = new ClientMonitoredItem(m_subscription, readWriteNodeId, AttributeId.Value, null, "SampleReadWriteMI");
                            Console.WriteLine("Created Monitored Item for read/write.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Monitored item could not created: " + ex.Message);
                        }
                    }
                    DataValue m_dataValue = new DataValue();
                    m_dataValue.Value = (Int16)m_randomGenerator.Next(1, 755);
                  //  m_dataValue.ValueRank = ValueRanks.Scalar;
                    m_dataValue.StatusCode = new StatusCode();
                    Console.WriteLine("Generated value: {0} to write.", m_dataValue.Value);
                    try
                    {
                        return m_readWriteMonitoredItem.Write(m_dataValue);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return StatusCodes.Bad;
                    }
                }
                else
                {
                    Console.WriteLine("Subscription is not created, please use \"s\" command");
                    return StatusCodes.Bad;
                }

            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return StatusCodes.Bad;
            }
        }

        /// <summary>
        /// Deletes the current MonitoredItem.
        /// </summary>
        internal void DeleteMonitoredItem()
        {
            if (m_monitoredItems.Count == 0)
            {
                Console.WriteLine("Monitored item is not created, please use \"m\" command");
                return;
            }
            try
            {
                ClientMonitoredItem monitoredItem = m_monitoredItems[m_monitoredItems.Count - 1];
                monitoredItem.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item unsubscribed from receiving data change notifications.");
                monitoredItem.Delete();
                m_monitoredItems.Remove(monitoredItem);
                Console.WriteLine("Monitored item deleted. ");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Monitored deleted error: " + ex.Message);
            }
        }
        #endregion

        #region Event Monitored Items *
        /// <summary>
        /// Creates the event monitored item.
        /// </summary>
        public void CreateEventMonitoredItem()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created.");
                return;
            }
            if (m_subscription == null)
            {
                Console.WriteLine("Subscription is not created.");
                return;
            }
            if (m_eventMonitoredItem != null)
            {
                Console.WriteLine("EventMonitoredItem is already created.");
                return;
            }

            try
            {
                m_eventMonitoredItem = new ClientMonitoredItem(m_subscription, m_eventSourceNodeId, "Sample Event Monitored Item", null);
                m_eventMonitoredItem.EventsReceived += m_eventMonitoredItem_EventsReceived;
                Console.WriteLine("Event Monitored Item is created and connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Creates the event monitored item filter.
        /// </summary>
        public void ApplyEventMonitoredItemFilter()
        {
            if (m_eventMonitoredItem == null)
            {
                Console.WriteLine("EventMonitoredItem is not created.");
                return;
            }

            EventFilterEx filter = (EventFilterEx)m_eventMonitoredItem.Filter;
            if (filter != null)
            {
                foreach (var selectOperand in filter.SelectOperandList)
                {
                    if (selectOperand.EventTypeId == m_selectEventTypeId &&
                        selectOperand.PropertyName.Equals(m_eventPropertyName))
                    {
                        Console.WriteLine("Filter is already applied.");
                        return;
                    }
                }

                filter.AddSelectClause(m_selectEventTypeId, m_eventPropertyName);
                filter.EventTypeIdFilter = m_eventTypeId;
                try
                {
                    m_eventMonitoredItem.ApplyFilter();
                    Console.WriteLine("Filter is applied on Event Monitored Item.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Filter could not be applied.Error: " + ex.Message);
                }
            }
            
        }

        /// <summary>
        /// Deletes the event monitored item.
        /// </summary>
        public void DeleteEventMonitoredItem()
        {
            if (m_eventMonitoredItem == null)
            {
                Console.WriteLine("EventMonitoredItem is not created.");
                return;
            }
            try
            {
                m_eventMonitoredItem.Delete();
                m_eventMonitoredItem.EventsReceived -= m_eventMonitoredItem_EventsReceived;
                m_eventMonitoredItem = null;
                Console.WriteLine("Event Monitored Item was disconnected and deleted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Handles the Notification event of the eventMonitoredItem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Softing.Opc.Ua.Toolkit.Client.MonitoredItemNotificationEventArgs"/> instance containing the event data.</param>
        private void m_eventMonitoredItem_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
            foreach (var eventNotification in e.EventNotifications)
            {
                Console.WriteLine("Event notification received for {0}.\n", eventNotification.MonitoredItem.DisplayName);
                string displayNotification = string.Empty;
                IList<SelectOperandEx> listOfOperands = ((EventFilterEx)m_eventMonitoredItem.Filter).SelectOperandList;
                for (int i = 0; i < listOfOperands.Count; i++)
                {
                    displayNotification += listOfOperands[i].PropertyName.NamespaceIndex + ":" + listOfOperands[i].PropertyName.Name 
                        + " : " + eventNotification.EventFields[i].ToString() + "\n";
                }

                Console.WriteLine(displayNotification);
            }
        }
        #endregion

        #region Alarms
        public void CreateAlarmsMonitoredItem()
        {
            try
            {
                if (m_session != null)
                {
                    if (m_subscription != null)
                    {                     
                        // Configure the event filter
                        EventFilterEx filter = new EventFilterEx();
                        if (filter != null)
                        {

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

                            // Create the MonitoredItem used to receive event notifications
                            m_alarmsMonitoredItem = new ClientMonitoredItem(m_subscription, m_alarmsModuleNodeId, "Alarms monitor item", filter);
                            m_alarmsMonitoredItem.SamplingInterval = 0;
                            m_alarmsMonitoredItem.QueueSize = 1000;

                            m_alarmsMonitoredItem.EventsReceived += m_alarmsMonitoredItem_EventsReceived;

                        }
                        // Log MonitoredItem Created event
                        string logMessage = String.Format("New MonitoredItemCreated for NodeId ({0}).", m_alarmsMonitoredItem.NodeId);
                        TraceService.Log(TraceMasks.Information, TraceSources.User3, "AlarmsClient.CreateMonitoredItems", logMessage);
                        Console.WriteLine(logMessage);

                        ConditionRefresh();
                        return;
                    }
                }
                Console.WriteLine("Session not connected or subscription not created!");
            }
            catch (Exception exception)
            {
                // Log Error
                TraceService.Log(TraceSources.User3, "AlarmsClient.CreateMonitoredItems", exception);
                Console.WriteLine(exception.Message);
            }
        }

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
        private void m_alarmsMonitoredItem_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
            try
            {
                if (m_session == null)
                {
                    return;
                }

                // Check for event notification

                foreach(EventFieldListEx eventNotification in e.EventNotifications)
                {
                    INode eventType = m_alarmsMonitoredItem.GetEventType(eventNotification);

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
                    string eventFields = "\r\nNew alarm notification received: \r\n" +
                        String.Format("EventId      = {0}\r\n", eventNotification.EventFields[1]) +
                        String.Format("EventType    = {0}\r\n", eventType) +
                        String.Format("SourceNode   = {0}\r\n", eventNotification.EventFields[3]) +
                        String.Format("SourceName   = {0}\r\n", eventNotification.EventFields[4]) +
                        String.Format("Time         = {0:HH:mm:ss.fff}\r\n", ((DateTime)eventNotification.EventFields[5].Value).ToLocalTime()) +
                        String.Format("Message      = {0}\r\n", eventNotification.EventFields[6]) +
                        String.Format("Severity     = {0}\r\n", (EventSeverity)((ushort)eventNotification.EventFields[7].Value)) +
                        String.Format("EnabledState = {0}\r\n", eventNotification.EventFields[8]) +
                        String.Format("ActiveState  = {0}\r\n", eventNotification.EventFields[9]) +
                        String.Format("AckedState   = {0}\r\n", eventNotification.EventFields[10]) +
                        String.Format("Comment      = {0}\r\n", eventNotification.EventFields[11]) +
                        String.Format("Retain       = {0}\r\n", eventNotification.EventFields[12]) +
                        "\r\n";

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
                // Log Error
                string logMessage = String.Format("MonitoredItem Notification Error : {0}.", exception.Message);
                TraceService.Log(TraceSources.User3, "AlarmsClient.MonitoredItem_Notification", exception);
                Console.WriteLine(logMessage);
            }
        }

        #endregion

        #region Method Call

        /// <summary>
        /// Call the method.
        /// </summary>
        /// <returns>The list of output arguments returned by the method call.</returns>
        internal void CallMethod()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            /*Select the method from the address space*/
            //Browse Path: Root\Objects\Server\Data\Static\MethodTest
            NodeId parentObjectId = new NodeId(10755, 3);
            //Browse Path: Root\Objects\Server\Data\Static\MethodTest\ScalarMethod1
            NodeId methodId = new NodeId(10756, 3);

            /*initialize input arguments*/
            bool Arg1 = true;
            sbyte Arg2 = -100;
            byte Arg3 = 200;
            Int16 Arg4 = -1200;
            UInt16 Arg5 = 1200;
            Int32 Arg6 = -56000;
            UInt32 Arg7 = 125066;
            Int64 Arg8 = -25000666;
            UInt64 Arg9 = 40444000;
            float Arg10 = 3455.67f;
            Double Arg11 = -1.7976;

            List<object> InputArguments = new List<object> { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11 };
            Console.WriteLine("\nMethod is called with the following arguments:\n");
            for (int i = 0; i < InputArguments.Count; i++)
            {
                Console.WriteLine("InArg[" + i + "]= " + InputArguments[i]);
            }

            IList<object> output = new List<object>();
            StatusCode statusCode = new StatusCode();
            try
            {
                statusCode = m_session.Call(parentObjectId, methodId, InputArguments, out output);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Method call exception: " + ex.Message);
            }
            Console.WriteLine("\nOutput arguments are:\n");
            for (int i = 0; i < output.Count; i++)
            {
                Console.WriteLine("OutArg[" + i + "]= " + output[i]);
            }
            Console.WriteLine(string.Format("\nStatus Code is: {0}\n", statusCode));
        }

        internal void AsyncCallMethod()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            /*Select the method from the address space*/
            //Browse Path: Root\Objects\Server\Data\Static\MethodTest
            NodeId parentObjectId = new NodeId(10755, 3);
            //Browse Path: Root\Objects\Server\Data\Static\MethodTest\ScalarMethod1
            NodeId methodId = new NodeId(10756, 3);

            /*initialize input arguments*/
            bool Arg1 = true;
            sbyte Arg2 = -100;
            byte Arg3 = 200;
            Int16 Arg4 = -1200;
            UInt16 Arg5 = 1200;
            Int32 Arg6 = -56000;
            UInt32 Arg7 = 125066;
            Int64 Arg8 = -25000666;
            UInt64 Arg9 = 40444000;
            float Arg10 = 3455.67f;
            Double Arg11 = -1.7976;

            List<object> InputArguments = new List<object> { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11 };
            Console.WriteLine("\nMethod is called with the following arguments:\n");
            for (int i = 0; i < InputArguments.Count; i++)
            {
                Console.WriteLine("InArg[" + i + "]= " + InputArguments[i]);
            }

            try
            {
                m_callIdentifier++;
                m_session.CallAsync(parentObjectId, methodId, InputArguments, m_callIdentifier);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Asynchronous method call exception: " + ex.Message);
            }
        }       

        void Session_CallCompleted(object sender, MethodExecutionEventArgs e)
        {
            Console.WriteLine(string.Format("\nCall returned for: {0}", e.Cookie));
            Console.WriteLine("Output arguments are:");
            for (int i = 0; i < e.OutputParameters.Count; i++)
            {
                Console.WriteLine("OutArg[" + i + "]= " + e.OutputParameters[i]);
            }
            Console.WriteLine(string.Format("\nStatus Code is: {0}\n", e.Result));
        }
        #endregion

        #region History Read
        public void HistoryReadRaw()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created.");
                return;
            }
            
            ReadRawModifiedDetails argument = new ReadRawModifiedDetails()
            {
                IsReadModified = false,
                StartTime = new DateTime(2011, 1, 1, 12, 0, 0),
                EndTime = new DateTime(2011, 1, 1, 12, 1, 40),               
                NumValuesPerNode = 3,
                ReturnBounds = false
            };

            TimestampsToReturn timestampsToReturn = TimestampsToReturn.Both;           

            List<DataValue> results = null;
            try
            {
                results = m_session.HistoryReadRaw(m_historianNodeId, argument, timestampsToReturn, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No results");
                return;
            }

            string value = null;
            for (int i = 0; i < results.Count; i++)
            {
                value = results[i].Value == null ? "NULL" : results[i].Value.ToString();
                Console.WriteLine("[" + i + "]"
                    + " " + "Value: " + value
                    + " " + "ServerTimestamp: " + results[i].ServerTimestamp
                    + " " + "SourceTimestamp: " + results[i].SourceTimestamp
                    + " " + "StatusCode: " + results[i].StatusCode
                    + " " + "HistoryInfo:" + results[i].StatusCode.AggregateBits);
            }
        }

        public void HistoryReadAtTime()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created.");
                return;
            }

            DateTimeCollection requiredTimes = new DateTimeCollection();
            requiredTimes.Add(new DateTime(2011, 1, 1, 12, 0, 0));
            requiredTimes.Add(new DateTime(2011, 7, 1, 12, 1, 0));   
            ReadAtTimeDetails argument = new ReadAtTimeDetails()
            {
                ReqTimes = requiredTimes,
                UseSimpleBounds = true
            };

            TimestampsToReturn timestampsToReturn = TimestampsToReturn.Both;

            List<DataValue> results = null;
            try
            {
                results = m_session.HistoryReadAtTime(m_historianNodeId, argument, timestampsToReturn, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No results");
                return;
            }

            string value = null;
            for (int i = 0; i < results.Count; i++)
            {
                value = results[i].Value == null ? "NULL" : results[i].Value.ToString();
                Console.WriteLine("[" + i + "]"
                    + " " + "Value: " + value
                    + " " + "ServerTimestamp: " + results[i].ServerTimestamp
                    + " " + "SourceTimestamp: " + results[i].SourceTimestamp
                    + " " + "StatusCode: " + results[i].StatusCode
                    + " " + "HistoryInfo:" + results[i].StatusCode.AggregateBits);
            }
        }

        public void HistoryReadProcessed()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created.");
                return;
            }
            
            NodeIdCollection aggregateTypes = new NodeIdCollection();
            aggregateTypes.Add(new NodeId(2342)); //aggregate function average           

            ReadProcessedDetails argument = new ReadProcessedDetails()
            {
                StartTime = new DateTime(2011, 1, 1, 12, 0, 0),
                EndTime = new DateTime(2011, 1, 1, 12, 1, 40),
                ProcessingInterval = 10000,
                AggregateType = aggregateTypes
            };
            TimestampsToReturn timestampsToReturn = TimestampsToReturn.Both;

            List<DataValue> results = null;
            try
            {
                results = m_session.HistoryReadProcessed(m_historianNodeId, argument, timestampsToReturn, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No results");
                return;
            }

            string value = null;
            for (int i = 0; i < results.Count; i++)
            {
                value = results[i].Value == null ? "NULL" : results[i].Value.ToString();
                Console.WriteLine("[" + i + "]"
                    + " " + "Value: " + value
                    + " " + "ServerTimestamp: " + results[i].ServerTimestamp
                    + " " + "SourceTimestamp: " + results[i].SourceTimestamp
                    + " " + "StatusCode: " + results[i].StatusCode
                    + " " + "HistoryInfo:" + results[i].StatusCode.AggregateBits);
            }
        }

        void HistoryContinuationPointReached(object sender, HistoryReadContinuationEventArgs e)
        {
            Console.WriteLine("Continuation point reached.");

            //one can cancel here the history read for new continuation points
            //e.Cancel = true;

            //one can identify the history read call that raised the continuation point by checking the cookie passed to the method
            //e.Cookie
        }
        #endregion

        #region Browse *
        /// <summary>
        /// The BrowseTheServer method uses the Browse method with two parameters, in this case the browse options will be taken from the Session object.
        /// If there are no browse options on the Session object the browse will be done with the default options.
        /// </summary>
        internal void BrowseTheServer()
        {
            try
            {                
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = Browse(null, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (ReferenceDescription rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            IList<ReferenceDescriptionEx> objectReferenceDescriptions = new List<ReferenceDescriptionEx>();
                            objectReferenceDescriptions = Browse(ExpandedNodeId.ToNodeId(rootReferenceDescription.NodeId, m_namespaceUris), null);
                            foreach (ReferenceDescription objectReferenceDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("     -" + objectReferenceDescription.DisplayName);
                                if (objectReferenceDescription.BrowseName.Name == "Server")
                                {
                                    IList<ReferenceDescriptionEx> serverReferenceDescriptions = new List<ReferenceDescriptionEx>();
                                    serverReferenceDescriptions = Browse(ExpandedNodeId.ToNodeId(objectReferenceDescription.NodeId, m_namespaceUris), null);
                                    foreach (ReferenceDescription serverReferenceDescription in serverReferenceDescriptions)
                                    {
                                        Console.WriteLine("        -" + serverReferenceDescription.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse Error: " + ex.Message);
            }
        }

        /// <summary>
        /// The BrowseWithOptions method uses the Browse method with three parameters, in this case the browse options will be given as a parameer.
        /// A BrowseDescription object is created first, on which browse options can be set, and given as parameter to the Browse method.
        /// In this case any browse options on the Session object will be ignored.
        /// </summary>
        internal void BrowseWithOptions()
        {
            BrowseDescriptionEx options = new BrowseDescriptionEx();
            options.MaxReferencesReturned = 3;
            try
            {
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = BrowseOptions(null, null, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName + " ***** " + rootReferenceDescription.ReferenceTypeName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            IList<ReferenceDescriptionEx> objectReferenceDescriptions = new List<ReferenceDescriptionEx>();
                            objectReferenceDescriptions = BrowseOptions(ExpandedNodeId.ToNodeId(rootReferenceDescription.NodeId, m_namespaceUris), options, rootReferenceDescription);
                            foreach (var objectReferenceDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("     -" + objectReferenceDescription.DisplayName + " ***** " + objectReferenceDescription.ReferenceTypeName);
                                if (objectReferenceDescription.BrowseName.Name == "Server")
                                {
                                    IList<ReferenceDescriptionEx> serverReferenceDescriptions = new List<ReferenceDescriptionEx>();
                                    serverReferenceDescriptions = BrowseOptions(ExpandedNodeId.ToNodeId(objectReferenceDescription.NodeId, m_namespaceUris), options, objectReferenceDescription);
                                    foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                    {
                                        Console.WriteLine("        -" + serverReferenceDescription.DisplayName + " ***** " + serverReferenceDescription.ReferenceTypeName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse Error: " + ex.Message);
            }
        }
        /// <summary>
        /// Browses the specified node id and returns its list of references.
        /// This method uses browse options set on the Session object
        /// </summary>
        internal IList<ReferenceDescriptionEx> Browse(NodeId nodeId, object sender)
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return new List<ReferenceDescriptionEx>();
            }
            IList<ReferenceDescriptionEx> results = null;

            try
            {
                results = m_session.Browse(nodeId, sender);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse error: " + ex.Message);
            }
            return results;
        }

        /// <summary>
        /// Browses the specified node id and returns its list of references.
        /// This method uses browse options as an input parameter.
        /// </summary>
        internal IList<ReferenceDescriptionEx> BrowseOptions(NodeId nodeId, BrowseDescriptionEx browseOptions, object sender)
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return new List<ReferenceDescriptionEx>();
            }
            IList<ReferenceDescriptionEx> results = null;

            try
            {
                results = m_session.Browse(nodeId, browseOptions, sender);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse error: " + ex.Message);
            }
            return results;
        }

        /// <summary>
        /// Handle the ContinuationPointReached event.
        /// This event is raised when a continuation point is reached.
        /// For example if from Browse Options the MaxReferencesReturned is set to x, then when browsing every x references returned this event will be thrown.
        /// </summary>
        private void Session_ContinuationPointReached(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
        #endregion

        #region TranslateBrowsePathToNodeIds *

        /// <summary>
        /// Translates the specified browse path to its corresponding NodeId.
        /// </summary>
        internal void TranslateBrowsePathToNodeIds()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                // define the starting node as the "Objects\Data" node.
                NodeId startingNode = new NodeId("ns=3;i=10157");

                // define the BrowsePath to the "Static\Scalar\Int32Value" node.
                List<QualifiedName> browsePath = new List<QualifiedName>();
                browsePath.Add(new QualifiedName("Static", 3));
                browsePath.Add(new QualifiedName("Scalar", 3));
                browsePath.Add(new QualifiedName("Int32Value", 3));

                // invoke the TranslateBrowsePath service.
                IList<NodeId> translateResults = m_session.TranslateBrowsePathToNodeIds(startingNode, browsePath);

                if (translateResults != null)
                {
                    Console.WriteLine("TranslateBrowsePath returned {0} result(s):", translateResults.Count);

                    foreach (NodeId result in translateResults)
                    {
                        Console.WriteLine("    {0}", result);
                    }
                }
                else
                {
                    Console.WriteLine("TranslateBrowsePath returned null value");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TranslateBrowsePath error: " + ex.Message);
            }
        }

        /// <summary>
        /// Translates the specified list of browse paths to corresponding NodeIds.
        /// </summary>
        internal void TranslateBrowsePathsToNodeIds()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                // define the list of requests.
                List<BrowsePathEx> browsePaths = new List<BrowsePathEx>();

                // define the starting node as the "Objects" node.
                BrowsePathEx browsePath = new BrowsePathEx();
                browsePath.StartingNode = new NodeId("ns=0;i=85");

                // define the relative browse path to the "Data\Static\Scalar\Int32Value" node.
                browsePath.RelativePath.Add(new QualifiedName("Data", 3));
                browsePath.RelativePath.Add(new QualifiedName("Static", 3));
                browsePath.RelativePath.Add(new QualifiedName("Scalar", 3));
                browsePath.RelativePath.Add(new QualifiedName("Int32Value", 3));
                browsePaths.Add(browsePath);

                // define the starting node as the "Objects" node.
                browsePath = new BrowsePathEx();
                browsePath.StartingNode = new NodeId("ns=0;i=85");

                // define the relative browse path to the "Data\Static\Array\UInt32Value" node.
                browsePath.RelativePath.Add(new QualifiedName("Data", 3));
                browsePath.RelativePath.Add(new QualifiedName("Static", 3));
                browsePath.RelativePath.Add(new QualifiedName("Array", 3));
                browsePath.RelativePath.Add(new QualifiedName("UInt32Value", 3));
                browsePaths.Add(browsePath);

                // invoke the TranslateBrowsePathsToNodeIds service.
                IList<BrowsePathResultEx> translateResults = m_session.TranslateBrowsePathsToNodeIds(browsePaths);

                // display the results.
                Console.WriteLine("TranslateBrowsePaths returned {0} result(s):", translateResults.Count);

                foreach (BrowsePathResultEx browsePathResult in translateResults)
                {
                    Console.Write("    StatusCode = {0} ; Target Nodes = ", browsePathResult.StatusCode);

                    foreach (NodeId targetNode in browsePathResult.TargetIds)
                    {
                        Console.Write("{0} ;", targetNode);
                    }

                    Console.WriteLine("\b \b");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TranslateBrowsePaths error: " + ex.Message);
            }
        }
        #endregion

        
    }
}
