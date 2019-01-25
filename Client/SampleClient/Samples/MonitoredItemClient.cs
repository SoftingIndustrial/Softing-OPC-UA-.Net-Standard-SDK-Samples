/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System;
using Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that contains sample code for MonitoredItem functionality
    /// </summary>
    class MonitoredItemClient
    {
        #region Private Fields

        private const string SessionName = "MonitoredItemClient Session";
        private const string SubscriptionName = "MonitoredItemClient Subscription";

        //"Server\\ServerStatus\\CurrentTime";
        private readonly NodeId m_miCurrentTimeNodeId = VariableIds.Server_ServerStatus_CurrentTime;

        // "CTT\\Scalar\\Simulation\\Int64";
        private readonly NodeId m_miInt64NodeId = new NodeId("ns=7;s=CTT_Scalar_Simulation_Int64");

        private readonly UaApplication m_application;

        private ClientSession m_session;
        private ClientSubscription m_subscription;
        private ClientMonitoredItem m_miCurrentTime;
        private ClientMonitoredItem m_miInt64;

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of MonitoredItemClient
        /// </summary>
        /// <param name="application"></param>
        public MonitoredItemClient(UaApplication application)
        {
            m_application = application;
        }

        #endregion

        #region Initialize & Disconnect Session

        /// <summary>
        /// Initialize session and subscription
        /// </summary>
        public void Initialize()
        {
            try
            {
                // create the session object with no security and anonymous login    
                m_session = m_application.CreateSession(Program.ServerUrl);
                m_session.SessionName = SessionName;

                //connect session
                m_session.Connect(false, true);
                Console.WriteLine("Session is connected.");

                //create the subscription
                m_subscription = new ClientSubscription(m_session, SubscriptionName);

                // set the Publishing interval for this subscription
                m_subscription.PublishingInterval = 500;
                Console.WriteLine("Subscription created");
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateSession Error: {0}", ex.Message);
                if (m_session != null)
                {
                    m_session.Dispose();
                    m_session = null;
                }
                m_subscription = null;
            }
        }

        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public void Disconnect()
        {
            try
            {
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

        #region Methods

        /// <summary>
        /// Creates a monitoredItem. The monitored item is activated in the constructor if the subscription is active as well.
        /// </summary>
        internal void CreateMonitoredItem()
        {
            if (m_session == null)
            {
                Console.WriteLine("CreateMonitoredItem: The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("CreateMonitoredItem: The session is not connected!");
                return;
            }
            if (m_miInt64 != null)
            {
                Console.WriteLine("MonitoredItem already created");
                return;
            }
            try
            {
                //create monitored item for server CurrentTime
                m_miCurrentTime = new ClientMonitoredItem(m_subscription, m_miCurrentTimeNodeId, "Monitored Item Server CurrentTime");
                m_miCurrentTime.DataChangesReceived += Monitoreditem_DataChangesReceived;
                //set sampling interval to 1 second
                m_miCurrentTime.SamplingInterval = 1000;

                if (m_miCurrentTime.CurrentState == State.Active)
                {
                    Console.WriteLine("Monitored item '{0}' created. Data value changes are shown:", m_miCurrentTime.DisplayName);
                }
                else
                {
                    Console.WriteLine("Monitored item '{0}' created with state {1}", m_miCurrentTime.DisplayName, m_miCurrentTime.CurrentState);
                }
                //create monitored item for Int64 variable
                m_miInt64 = new ClientMonitoredItem(m_subscription, m_miInt64NodeId, "Monitored Item Int64");
                m_miInt64.DataChangesReceived += Monitoreditem_DataChangesReceived;
                //set sampling interval to 3 seconds
                m_miInt64.SamplingInterval = 3000;

                if (m_miCurrentTime.CurrentState == State.Active)
                {
                    Console.WriteLine("Monitored item '{0}' created. Data value changes are shown:", m_miInt64.DisplayName);
                }
                else
                {
                    Console.WriteLine("Monitored item '{0}' created with state {1}", m_miInt64.DisplayName, m_miInt64.CurrentState);
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Deletes the current MonitoredItem.
        /// </summary>
        internal void DeleteMonitoredItem()
        {
            if (m_session == null)
            {
                Console.WriteLine("DeleteMonitoredItem: The session is not initialized!");
                return;
            }
            if (m_session.CurrentState == State.Disconnected)
            {
                Console.WriteLine("DeleteMonitoredItem: The session is not connected!");
                return;
            }
            if (m_miCurrentTime == null || m_miInt64 == null)
            {
                Console.WriteLine("Monitored items are not created.");
                return;
            }
            try
            {
                m_miCurrentTime.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item '{0}' unsubscribed from receiving data change notifications.", m_miCurrentTime.DisplayName);
                m_miCurrentTime.Delete();
                Console.WriteLine("Monitored item '{0}' deleted.", m_miCurrentTime.DisplayName);
                m_miCurrentTime = null;

                m_miInt64.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item '{0}' unsubscribed from receiving data change notifications.", m_miInt64.DisplayName);
                m_miInt64.Delete();
                Console.WriteLine("Monitored item '{0}' deleted.", m_miInt64.DisplayName);
                m_miInt64 = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Monitored item delete error: " + ex.Message);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Notification event of the Monitoreditem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataChangesNotificationEventArgs"/> instance containing the event data.</param>
        private void Monitoreditem_DataChangesReceived(object sender, DataChangesNotificationEventArgs e)
        {
            foreach (var dataChangeNotification in e.DataChangeNotifications)
            {
                Console.WriteLine(" {0} Received data value change for '{1}':", dataChangeNotification.SequenceNo, dataChangeNotification.MonitoredItem.DisplayName);
                Console.WriteLine("    Value : {0} ", dataChangeNotification.Value);
                Console.WriteLine("    StatusCode : {0} ", dataChangeNotification.Value.StatusCode);
                Console.WriteLine("    ServerTimestamp : {0:hh:mm:ss.fff tt}", dataChangeNotification.Value.ServerTimestamp.ToLocalTime());
                Console.WriteLine("    SourceTimestamp : {0:hh:mm:ss.fff tt}", dataChangeNotification.Value.SourceTimestamp.ToLocalTime());
            }
        }

        #endregion
    }
}
