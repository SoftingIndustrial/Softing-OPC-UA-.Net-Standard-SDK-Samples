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
using System.Text;
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
        private ServerState m_currentServerState = ServerState.Unknown;

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
                Console.WriteLine("CreateMonitoredItem: The session is not connected!");
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

                Console.WriteLine("Event Monitored Item is created and with state {0}.", m_eventMonitoredItem.CurrentState);
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
                IList<SelectOperandEx> listOfOperands = ((EventFilterEx) m_eventMonitoredItem.Filter).SelectOperandList;
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
