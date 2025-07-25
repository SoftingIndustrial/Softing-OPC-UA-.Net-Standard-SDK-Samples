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
using Softing.Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        // "CTT\\Scalar\\Simulation\\Int16";
        private readonly NodeId m_miInt16NodeId = new NodeId("ns=7;s=CTT_Scalar_Simulation_Int16");

        // "CTT\\Scalar\\Simulation\\Int32";
        private readonly NodeId m_miInt32NodeId = new NodeId("ns=7;s=CTT_Scalar_Simulation_Int32");

        //"DataAccess\\Refrigerator\\MotorTemperature";
        private readonly NodeId m_miMotorTemperatureNodeId = new NodeId("ns=3;i=24");

        private readonly UaApplication m_application;

        private ClientSession m_session;
        private ClientSubscription m_subscription;
        private ClientMonitoredItem m_miCurrentTime;
        private ClientMonitoredItem m_miInt64;
        private ClientMonitoredItem m_miInt16;
        private ClientMonitoredItem m_miInt32;

        private ClientMonitoredItem m_miMotorTemperature;

        private bool m_isDisposed = false;

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
        public async Task Initialize(bool withSubscription = true)
        {
            try
            {
                // create the session object with no security and anonymous login    
                // m_session = CreateSession(SessionName, MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity());

                // create the session object with user identity
                m_session = CreateSession(SessionName, MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                // create the session object with certificate
                // m_session = CreateSessionWithCertificate(SessionName, null as string, "opcuser.pfx");

                // create the session object with certificate password
                // m_session = CreateSessionWithCertificate(SessionName, "User_Pwd", "opcuserPwd.pfx");

                // create the session object with security
                // m_session = CreateSession(SessionName, MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256, new UserIdentity());

                //connect session
                await m_session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine("Session is connected.");

                if (withSubscription)
                {
                    m_session.DeleteSubscriptionsOnClose = false;
                    m_session.TransferSubscriptionsOnReconnect = true;

                    //create the subscription
                    m_subscription = new ClientSubscription(m_session, SubscriptionName);
                    // set the Publishing interval for this subscription
                    m_subscription.PublishingInterval = 500;

                    m_subscription.RepublishAfterTransfer = true;

                    Console.WriteLine("Subscription created");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateSession", ex);

                if (m_session != null)
                {
                    m_session.Dispose();
                    m_session = null;
                    m_isDisposed = true;
                }
                m_subscription = null;
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
                    m_isDisposed = true;
                    Console.WriteLine("Session is disconnected.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DisconnectSession", ex);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a monitoredItem. The monitored item is activated in the constructor if the subscription is active as well.
        /// </summary>
        internal void CreateMonitoredItems()
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
            if (m_miCurrentTime != null)
            {
                Console.WriteLine("MonitoredItem {0} already created", m_miCurrentTime.DisplayName);
                return;
            }
            if (m_miInt64 != null)
            {
                Console.WriteLine("MonitoredItem {0} already created", m_miInt64.DisplayName);
                return;
            }
            if (m_miMotorTemperature != null)
            {
                Console.WriteLine("MonitoredItem {0} already created", m_miMotorTemperature.DisplayName);
                return;
            }
            try
            {
                #region Create #1 monitored item for server CurrentTime
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

                #endregion

                #region Create #2 monitored item - for Int64 variable

                m_miInt64 = new ClientMonitoredItem(m_subscription, m_miInt64NodeId, "Monitored Item Int64");
                m_miInt64.DataChangesReceived += Monitoreditem_DataChangesReceived;
                //set sampling interval to 3 seconds
                m_miInt64.SamplingInterval = 3000;

                if (m_miInt64.CurrentState == State.Active)
                {
                    Console.WriteLine("Monitored item '{0}' created. Data value changes are shown:", m_miInt64.DisplayName);
                }
                else
                {
                    Console.WriteLine("Monitored item '{0}' created with state {1}", m_miInt64.DisplayName, m_miInt64.CurrentState);
                }
                #endregion

                #region Create #3 monitored item - for MotorTemperature variable

                m_miMotorTemperature = new ClientMonitoredItem(m_subscription, m_miMotorTemperatureNodeId, "Monitored Item Custom MotorTemperature");
                m_miMotorTemperature.DataChangesReceived += Monitoreditem_DataChangesReceived;

                //set sampling interval to 3 seconds
                m_miMotorTemperature.SamplingInterval = 3000;

                if (m_miMotorTemperature.CurrentState == State.Active)
                {
                    Console.WriteLine("Monitored item '{0}' created. Data value changes are shown:", m_miMotorTemperature.DisplayName);
                }
                else
                {
                    Console.WriteLine("Monitored item '{0}' created with state {1}", m_miMotorTemperature.DisplayName, m_miMotorTemperature.CurrentState);
                }
                #endregion
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateMonitoredItem", ex);
            }
        }

        /// <summary>
        /// Deletes the current MonitoredItem.
        /// </summary>
        internal void DeleteMonitoredItems()
        {
            if (m_session == null)
            {
                Console.WriteLine("DeleteMonitoredItems: The session is not initialized!");
            }
            if (m_session.CurrentState == State.Disconnected)
            {
                Console.WriteLine("DeleteMonitoredItems: The session is not connected!");
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

                m_miMotorTemperature.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item '{0}' unsubscribed from receiving data change notifications.", m_miMotorTemperature.DisplayName);
                m_miMotorTemperature.Delete();
                Console.WriteLine("Monitored item '{0}' deleted.", m_miMotorTemperature.DisplayName);
                m_miMotorTemperature = null;
            }
            catch (Exception ex)
            {
                Program.PrintException("DeleteMonitoredItem", ex);
            }
        }

        /// <summary>
        /// Create monitored items before subscription connects. The monitored items are not activated in the constructor and the subscription is not active as well.
        /// The subscription is activated in the end only after all monitor items were added to it.
        /// </summary>
        internal void CreateMonitoredItemsBeforeSubscriptionConnect()
        {
            if (m_session == null)
            {
                Console.WriteLine("CreateMonitoredItemsBeforeSubscriptionConnect: The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("CreateMonitoredItemsBeforeSubscriptionConnect: The session is not disconnected!");
                return;
            }
            if (m_miCurrentTime != null)
            {
                Console.WriteLine("MonitoredItem {0} already created", m_miCurrentTime.DisplayName);
                return;
            }
            if (m_miInt64 != null)
            {
                Console.WriteLine("MonitoredItem already created", m_miInt64.DisplayName);
                return;
            }
            try
            {
                //create monitored item for server CurrentTime
                m_miCurrentTime = new ClientMonitoredItem(m_subscription, m_miCurrentTimeNodeId, "Monitored Item Server CurrentTime", connect: false);
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
                m_miInt64 = new ClientMonitoredItem(m_subscription, m_miInt64NodeId, "Monitored Item Int64", connect: false);
                m_miInt64.DataChangesReceived += Monitoreditem_DataChangesReceived;
                //set sampling interval to 3 seconds
                m_miInt64.SamplingInterval = 3000;

                if (m_miInt64.CurrentState == State.Active)
                {
                    Console.WriteLine("Monitored item '{0}' created. Data value changes will pe shown on:", m_miInt64.DisplayName);
                }
                else
                {
                    Console.WriteLine("Monitored item '{0}' created with state {1}", m_miInt64.DisplayName, m_miInt64.CurrentState);
                }

                m_subscription.ConnectAsync(true, true).ConfigureAwait(false);
                Console.WriteLine("Subscription is connected.");
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateMonitoredItemsBeforeSubscriptionConnect", ex);
            }
        }

        /// <summary>
        /// Deletes the active MonitoredItems on the subscription that became connected after all monitor items were previously added.
        /// </summary>
        internal void DeleteMonitoredItemsCreatedBeforeSubscriptionConnect()
        {
            if (m_session == null)
            {
                Console.WriteLine("DeleteMonitoredItemsCreatedBeforeSubscriptionConnect: The session is not initialized!");
                return;
            }
            if (m_session.CurrentState == State.Disconnected)
            {
                Console.WriteLine("DeleteMonitoredItemsCreatedBeforeSubscriptionConnect: The session is not connected!");
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

                m_miInt64.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item '{0}' unsubscribed from receiving data change notifications.", m_miInt64.DisplayName);

                m_subscription.DeleteItems(new List<ClientMonitoredItem>() { m_miCurrentTime, m_miInt64 });
                Console.WriteLine("Monitored items: '{0} and {1}' were deleted.", m_miCurrentTime.DisplayName, m_miInt64.DisplayName);
                m_miCurrentTime = null;
                m_miInt64 = null;
            }
            catch (Exception ex)
            {
                Program.PrintException("DeleteMonitoredItemsCreatedBeforeSubscriptionConnect", ex);
            }
        }

        /// <summary>
        /// Creates new monitored items on active subscription that was already connected.
        /// </summary>
        internal void CreateNewMonitoredItemsAfterSubscriptionConnect()
        {
            if (m_session == null)
            {
                Console.WriteLine("CreateNewMonitoredItemsAfterSubscriptionConnect: The session is not initialized!");
                return;
            }
            if (m_subscription != null && m_subscription.CurrentState == State.Disconnected)
            {
                Console.WriteLine("CreateNewMonitoredItemsAfterSubscriptionConnect: The session is not connected!");
                return;
            }
            if (m_miInt16 != null)
            {
                Console.WriteLine("MonitoredItem {0} already created", m_miInt16.DisplayName);
                return;
            }
            if (m_miInt32 != null)
            {
                Console.WriteLine("MonitoredItem {0} already created", m_miInt32.DisplayName);
                return;
            }
            try
            {
                //create monitored item for Int16 variable
                m_miInt16 = new ClientMonitoredItem(m_subscription, m_miInt16NodeId, "Monitored Item Int16", connect: false);
                m_miInt16.DataChangesReceived += Monitoreditem_DataChangesReceived;
                //set sampling interval to 3 seconds
                m_miInt16.SamplingInterval = 3000;

                if (m_miInt16.CurrentState == State.Active)
                {
                    Console.WriteLine("Monitored item '{0}' created. Data value changes are shown:", m_miInt16.DisplayName);
                }
                else
                {
                    Console.WriteLine("Monitored item '{0}' created with state {1}", m_miInt16.DisplayName, m_miInt16.CurrentState);
                }

                //create monitored item for Int16 variable
                m_miInt32 = new ClientMonitoredItem(m_subscription, m_miInt32NodeId, "Monitored Item Int32", connect: false);
                m_miInt32.DataChangesReceived += Monitoreditem_DataChangesReceived;
                //set sampling interval to 3 seconds
                m_miInt32.SamplingInterval = 3000;

                if (m_miInt32.CurrentState == State.Active)
                {
                    Console.WriteLine("Monitored item '{0}' created. Data value changes are shown:", m_miInt32.DisplayName);
                }
                else
                {
                    Console.WriteLine("Monitored item '{0}' created with state {1}", m_miInt32.DisplayName, m_miInt32.CurrentState);
                }

                m_subscription.ApplyMonitoredItemsChanges();
                Console.WriteLine("Subscription was updated with new monitor items added.");
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateNewMonitoredItemsAfterSubscriptionConnect", ex);
            }
        }

        /// <summary>
        /// Deletes the new monitored items created on the subscription that was connected after all monitor items were previously added.
        /// </summary>
        internal void DeleteNewMonitoredItemsCreatedAfterSubscriptionConnect()
        {
            if (m_session == null)
            {
                Console.WriteLine("DeleteNewMonitoredItemsCreatedAfterSubscriptionConnect: The session is not initialized!");
                return;
            }
            if (m_session.CurrentState == State.Disconnected)
            {
                Console.WriteLine("DeleteNewMonitoredItemsCreatedAfterSubscriptionConnect: The session is not connected!");
                return;
            }
            if (m_miCurrentTime == null || m_miInt64 == null)
            {
                Console.WriteLine("Monitored items are not created.");
                return;
            }
            try
            {
                m_miInt16.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item '{0}' unsubscribed from receiving data change notifications.", m_miInt16.DisplayName);

                m_miInt32.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item '{0}' unsubscribed from receiving data change notifications.", m_miInt32.DisplayName);

                m_subscription.DeleteItems(new List<ClientMonitoredItem>() { m_miInt16, m_miInt32 });
                Console.WriteLine("New Monitored items: '{0} and {1}' were deleted.", m_miInt16.DisplayName, m_miInt32.DisplayName);
                m_miInt16 = null;
                m_miInt32 = null;
            }
            catch (Exception ex)
            {
                Program.PrintException("DeleteNewMonitoredItemsCreatedAfterSubscriptionConnect", ex);
            }
        }

        /// <summary>
        /// Creates a new session with the specified parameters.
        /// </summary>   
        private ClientSession CreateSession(string sessionName, MessageSecurityMode securityMode, SecurityPolicy securityPolicy,
            UserIdentity userId)
        {
            try
            {
                Console.WriteLine("\r\nCreating the session {0} (SecurityMode = {1}, SecurityPolicy = {2}, \r\n\t\t\t\t\t\tUserIdentity = {3})...",
                    sessionName, securityMode, securityPolicy, userId.GetIdentityToken());

                // Create the Session object.
                ClientSession session = m_application.CreateSession(Program.ServerUrl, securityMode, securityPolicy, MessageEncoding.Binary, userId);
                session.SessionName = sessionName;
                return session;
            }
            catch (Exception ex)
            {
                Program.PrintException("ConnectClient.CreateSession", ex);
                return null;
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
                Console.WriteLine("    SessionId : {0} ", dataChangeNotification.MonitoredItem.Subscription.Session?.Id);
                Console.WriteLine("    SubscriptionId : {0} ", dataChangeNotification.MonitoredItem.Subscription.Id);
                Console.WriteLine("    StatusCode : {0} ", dataChangeNotification.Value.StatusCode);
                Console.WriteLine("    ServerTimestamp : {0:hh:mm:ss.fff tt}", dataChangeNotification.Value.ServerTimestamp.ToLocalTime());
                Console.WriteLine("    SourceTimestamp : {0:hh:mm:ss.fff tt}", dataChangeNotification.Value.SourceTimestamp.ToLocalTime());
            }
        }
        #endregion
    }
}
