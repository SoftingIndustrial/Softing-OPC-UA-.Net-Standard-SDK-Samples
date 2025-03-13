/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Sample Client class that provides events functionality
    /// 
    /// This sample also demonstrates handling for ClientSession.KeepAlive event
    /// </summary>
    public class EventsClient
    {
        #region Private Fields

        private const string SessionName = "EventsClient Session";
        private const string SubscriptionName = "EventsClient Subscription";

        private readonly UaApplication m_application;
        private ClientSession m_session;
        private ClientSubscription m_subscription;
        private ClientMonitoredItem m_eventMonitoredItem;
        private ClientMonitoredItem m_doubleFilteringEventMonitoredItem;
        private static readonly NodeId m_doubleFilteringEventMonitoredItemNodeId = new NodeId("ns=10;i=1");
        private static readonly NodeId m_doubleFilteringEventTypeNodeId = new NodeId("ns=10;s=106");
        private ServerState m_currentServerState = ServerState.Unknown;

        // "\\HistoricalDataAccess\DynamicHistoricalDataItems EventNotifier=SubscribeToEvents folder";
        private readonly NodeId m_eventDoubleNodeId = new NodeId("ns=4;s=HistoricalDataAccess?DynamicHistoricalDataItems");
        private ClientMonitoredItem m_eventMonitoredItemAddNew;

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of BrowseClientSample
        /// </summary>
        /// <param name="application"></param>
        public EventsClient(UaApplication application)
        {
            m_application = application;
        }

        #endregion

        #region Initialize & Disconnect Session

        /// <summary>
        /// Initialize session and subscription
        /// </summary>
        public async Task Initialize()
        {
            try
            {
                // create the session object with no security and anonymous login    
                m_session = m_application.CreateSession(Program.ServerUrl);
                m_session.SessionName = SessionName;

                m_session.KeepAlive += Session_KeepAlive;
                //connect session
                await m_session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine("Session is connected.");

                //create the subscription
                m_subscription = new ClientSubscription(m_session, SubscriptionName);

                // set the Publishing interval for this subscription
                m_subscription.PublishingInterval = 500;
                Console.WriteLine("Subscription created");
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateSession", ex);

                if (m_session != null)
                {
                    m_session.Dispose();
                    m_session = null;
                }
            }
        }

        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public async Task Disconnect()
        {
            try
            {
                //disconnect subscription
                if (m_subscription != null)
                {
                    await m_subscription.DisconnectAsync(true).ConfigureAwait(false);
                    m_subscription.Delete();
                    m_subscription = null;
                    Console.WriteLine("Subscription is deleted.");
                }
                if (m_session != null)
                {
                    await m_session.DisconnectAsync(true).ConfigureAwait(false);
                    m_session.Dispose();
                    m_session = null;
                    Console.WriteLine("Session is disconnected.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DisconnectSession", ex);
            }

        }

        #endregion

        #region Event Monitored Item Methods

        /// <summary>
        /// Creates the event monitored item.
        /// </summary>
        public void CreateEventMonitoredItem()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("CreateEventMonitoredItem: The session is not connected!");
                return;
            }
            if (m_eventMonitoredItem != null)
            {
                Console.WriteLine("EventMonitoredItem is already created.");
                return;
            }

            try
            {
                //ObjectIds.Server BrowsePath: Root\Objects\Server
                m_eventMonitoredItem = new ClientMonitoredItem(m_subscription, ObjectIds.Server, "Sample Event Monitored Item", null);
                m_eventMonitoredItem.EventsReceived += EventMonitoredItem_EventsReceived;

                Console.WriteLine("Event Monitored Item is created with state {0}.", m_eventMonitoredItem.CurrentState);
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateEventMonitoredItem", ex);
            }
        }

        /// <summary>
        /// Deletes the event monitored item.
        /// </summary>
        public void DeleteEventMonitoredItem()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("DeleteEventMonitoredItem: The session is not connected!");
                return;
            }
            try
            {
                if (m_eventMonitoredItem != null)
                {
                    //delete event monitored item
                    m_eventMonitoredItem.EventsReceived -= EventMonitoredItem_EventsReceived;
                    m_eventMonitoredItem.Delete();
                    m_eventMonitoredItem = null;
                    Console.WriteLine("Event Monitored Item was disconnected and deleted.");
                }
                else
                {
                    Console.WriteLine("There was no Event Monitored Item to be deleted.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DeleteEventMonitoredItem", ex);
            }
        }

        /// <summary>
        /// Creates the event monitored item before subscription connect.
        /// </summary>
        public void CreateEventMonitoredItemBeforeSubscriptionConnect()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("CreateEventMonitoredItemBeforeSubscriptionConnect: The session is not connected!");
                return;
            }
            if (m_eventMonitoredItem != null)
            {
                Console.WriteLine("EventMonitoredItem is already created.");
                return;
            }

            try
            {
                //ObjectIds.Server BrowsePath: Root\Objects\Server
                m_eventMonitoredItem = new ClientMonitoredItem(m_subscription, ObjectIds.Server, "Sample Event Monitored Item", null, false);
                m_eventMonitoredItem.EventsReceived += EventMonitoredItem_EventsReceived;

                Console.WriteLine("Event Monitored Item is created with state {0}.", m_eventMonitoredItem.CurrentState);

                m_subscription.ConnectAsync(true, true).ConfigureAwait(false);
                Console.WriteLine("Subscription is connected.");

                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateEventMonitoredItemBeforeSubscriptionConnect", ex);
            }
        }

        /// <summary>
        /// Deletes the event monitored item for a previous event monitor items that was not connected.
        /// </summary>
        public void DeleteEventMonitoredItemCreatedBeforeSubscriptionConnect()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("DeleteEventMonitoredItemCreatedBeforeSubscriptionConnect: The session is not connected!");
                return;
            }
            try
            {
                if (m_eventMonitoredItem != null)
                {
                    //delete event monitored item
                    m_eventMonitoredItem.EventsReceived -= EventMonitoredItem_EventsReceived;
                    Console.WriteLine("Event Monitored item: '{0}' unsubscribed from receiving event notifications.", m_eventMonitoredItem.DisplayName);

                    m_subscription.DeleteItems(new List<ClientMonitoredItem>() { m_eventMonitoredItem });
                    Console.WriteLine("Event Monitored item: '{0}' was deleted.", m_eventMonitoredItem.DisplayName);
                    m_eventMonitoredItem = null;
                }
                else
                {
                    Console.WriteLine("There was no Event Monitored Item to be deleted.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DeleteEventMonitoredItemCreatedBeforeSubscriptionConnect", ex);
            }
        }

        /// <summary>
        /// Creates new event monitored item on the active subscription that was already connected.
        /// </summary>
        internal void CreateNewEventMonitoredItemAfterSubscriptionConnect()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("CreateNewEventMonitoredItemAfterSubscriptionConnect: The session is not connected!");
                return;
            }
            if (m_eventMonitoredItemAddNew != null)
            {
                Console.WriteLine("EventMonitoredItem is already created.");
                return;
            }

            try
            {
                // Double.NodeId BrowsePath: Root\Objects\HistoricalDataAccess\DynamicHistoricalDataItems\Double
                m_eventMonitoredItemAddNew = new ClientMonitoredItem(m_subscription, m_eventDoubleNodeId, "Sample History Event Monitored Item", null, false);
                m_eventMonitoredItemAddNew.EventsReceived += EventMonitoredItem_EventsReceived;

                Console.WriteLine("Event Monitored Item is created with state {0}.", m_eventMonitoredItemAddNew.CurrentState);

                m_subscription.ApplyMonitoredItemsChanges();
                Console.WriteLine("Subscription was updated with new event monitor item added.");
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateNewEventMonitoredItemAfterSubscriptionConnect", ex);
            }
        }

        /// <summary>
        /// Deletes the event monitored items on the subscription that was connected after all monitor items were previously added.
        /// </summary>
        internal void DeleteNewEventMonitoredItemCreatedAfterSubscriptionConnect()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("DeleteNewEventMonitoredItemCreatedAfterSubscriptionConnect: The session is not connected!");
                return;
            }
            try
            {
                if (m_eventMonitoredItemAddNew != null)
                {
                    //delete event monitored item
                    m_eventMonitoredItemAddNew.EventsReceived -= EventMonitoredItem_EventsReceived;
                    Console.WriteLine("Event Monitored item: '{0}' unsubscribed from receiving event notifications.", m_eventMonitoredItemAddNew.DisplayName);

                    m_subscription.DeleteItems(new List<ClientMonitoredItem>() { m_eventMonitoredItemAddNew });
                    Console.WriteLine("Event Monitored item: '{0}' was deleted.", m_eventMonitoredItemAddNew.DisplayName);
                    m_eventMonitoredItemAddNew = null;
                }
                else
                {
                    Console.WriteLine("There was no Event Monitored Item to be deleted.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DeleteNewEventMonitoredItemCreatedAfterSubscriptionConnect", ex);
            }
        }

        /// <summary>
        /// Creates the double filtering event monitored item.
        /// </summary>
        public void CreateDoubleFilteringEventMonitoredItem()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("CreateEventMonitoredItem: The session is not connected!");
                return;
            }
            if (m_doubleFilteringEventMonitoredItem != null)
            {
                Console.WriteLine("DoubleFilteringEventMonitoredItem is already created.");
                return;
            }

            try
            {
                // Configure the event filter
                EventFilterEx filter = new EventFilterEx();

                // specify the required fields of the events
                filter.AddSelectClause(ObjectTypes.BaseEventType, String.Empty, Attributes.NodeId);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Time);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.ReceiveTime);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Severity);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.EventType);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.Message);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.SourceNode);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.EventId);
                filter.AddSelectClause(ObjectTypes.BaseEventType, BrowseNames.SourceName);

                m_doubleFilteringEventMonitoredItem = new ClientMonitoredItem(m_subscription, m_doubleFilteringEventMonitoredItemNodeId, "Double Filtering Sample Event Monitored Item", filter);
                m_doubleFilteringEventMonitoredItem.EventsReceived += EventDoubleFilteringMonitoredItem_EventsReceived;

                int selectClausePos = 0;
                // based on the entry log above first time is added the clause 'DoubleFiltering' with namespaceindex = 10 followed by 'Count' property
                filter.AddSelectClause(m_doubleFilteringEventTypeNodeId, new QualifiedName("DoubleFiltering", 10));
                selectClausePos = filter.SelectClauses.Count - 1;
                filter.SelectOperandList[selectClausePos].BrowsePath.Add(new QualifiedName("Count", 10));

                m_doubleFilteringEventMonitoredItem.Filter = filter;

                // this is a must
                m_doubleFilteringEventMonitoredItem.ApplyFilter();

                Console.WriteLine("Double filtering Event Monitored Item is created with state {0}.", m_doubleFilteringEventMonitoredItem.CurrentState);
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateDoubleFilteringEventMonitoredItem", ex);
            }
        }

        /// <summary>
        /// Deletes the event monitored item.
        /// </summary>
        public void DeleteDoubleFilteringEventMonitoredItem()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("DeleteDoubleFilteringEventMonitoredItem: The session is not connected!");
                return;
            }
            try
            {
                if (m_doubleFilteringEventMonitoredItem != null)
                {
                    //delete event monitored item
                    m_doubleFilteringEventMonitoredItem.EventsReceived -= EventDoubleFilteringMonitoredItem_EventsReceived;
                    m_doubleFilteringEventMonitoredItem.Delete();
                    m_doubleFilteringEventMonitoredItem = null;
                    Console.WriteLine("Double filtering Event Monitored Item was disconnected and deleted.");
                }
                else
                {
                    Console.WriteLine("There was no Event Monitored Item to be deleted.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DeleteDoubleFilteringEventMonitoredItem", ex);
            }
        }
        #endregion

        #region Event Handlers

        /// <summary>
        /// Handler for Session KeepAlive event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Session_KeepAlive(object sender, Opc.Ua.Client.KeepAliveEventArgs e)
        {
            if (e.CurrentState != m_currentServerState)
            {
                m_currentServerState = e.CurrentState;
                Console.WriteLine("Session KeepAlive Server state changed to: {0}", m_currentServerState);
            }
        }

        /// <summary>
        /// Handles the Notification event of the eventMonitoredItem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventsNotificationEventArgs"/> instance containing the event data.</param>
        private void EventMonitoredItem_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
            foreach (var eventNotification in e.EventNotifications)
            {
                Console.WriteLine("Event notification received for {0}.\n", eventNotification.MonitoredItem.DisplayName);

                StringBuilder displayNotification = new StringBuilder();
                IList<SelectOperandEx> listOfOperands = ((EventFilterEx)m_eventMonitoredItem.Filter).SelectOperandList;
                for (int i = 0; i < listOfOperands.Count; i++)
                {
                    displayNotification.AppendFormat("{0}:{1}:{2}\n",
                        listOfOperands[i].PropertyName.NamespaceIndex,
                        listOfOperands[i].PropertyName.Name,
                        eventNotification.EventFields[i]);
                }

                Console.WriteLine(displayNotification);
            }
        }

        /// <summary>
        /// Handles the Notification event of the doubleFilteringEventMonitoredItem.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventDoubleFilteringMonitoredItem_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
            foreach (var eventNotification in e.EventNotifications)
            {
                Console.WriteLine("Double Filtering Event notification received for {0}.\n", eventNotification.MonitoredItem.DisplayName);

                StringBuilder displayNotification = new StringBuilder();
                IList<SelectOperandEx> listOfOperands = ((EventFilterEx)m_doubleFilteringEventMonitoredItem.Filter).SelectOperandList;
                for (int i = 0; i < listOfOperands.Count; i++)
                {
                    displayNotification.AppendFormat("{0}:{1}:{2}\n",
                        listOfOperands[i].PropertyName.NamespaceIndex,
                        listOfOperands[i].PropertyName.Name,
                        eventNotification.EventFields[i]);
                }

                Console.WriteLine(displayNotification);
            }
        }

        #endregion
    }
}
