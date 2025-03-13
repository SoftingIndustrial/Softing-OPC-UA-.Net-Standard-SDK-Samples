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
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

        // "CTT\\Scalar\\Simulation\\Int16";
        private readonly NodeId m_miInt16NodeId = new NodeId("ns=7;s=CTT_Scalar_Simulation_Int16");

        // "CTT\\Scalar\\Simulation\\Int32";
        private readonly NodeId m_miInt32NodeId = new NodeId("ns=7;s=CTT_Scalar_Simulation_Int32");

        private readonly NodeId m_miMotorTemperatureNodeId = new NodeId("ns=3;i=24");

        private readonly UaApplication m_application;

        private ClientSession m_session;
        private ClientSession m_transfer_session;
        private ClientSubscription m_subscription;
        private ClientMonitoredItem m_miCurrentTime;
        private ClientMonitoredItem m_miInt64;
        private ClientMonitoredItem m_miInt16;
        private ClientMonitoredItem m_miInt32;

        private ClientMonitoredItem m_miMotorTemperature;

        private bool m_isDisposed = false;

        // persist the subscription state
        string m_filePathSubscriptions = @"D:\subscriptions.txt";

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
                    m_session.RepublishAfterTransfer = true;
                    m_session.TransferSubscriptionsOnReconnect = true;

                    //create the subscription
                    m_subscription = new ClientSubscription(m_session, SubscriptionName);

                    // set the Publishing interval for this subscription
                    m_subscription.PublishingInterval = 500;
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
            if (m_miInt64 != null)
            {
                Console.WriteLine("MonitoredItem already created");
                return;
            }
            if (m_miMotorTemperature != null)
            {
                Console.WriteLine("MonitoredItem already created");
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

                if (m_transfer_session == null)
                {
                    Console.WriteLine("DeleteMonitoredItem: The transfer session is not initialized!");
                    return;
                }
            }
            if (m_session.CurrentState == State.Disconnected)
            {
                Console.WriteLine("DeleteMonitoredItems: The session is not connected!");

                if (m_transfer_session.CurrentState == State.Disconnected)
                {
                    Console.WriteLine("DeleteMonitoredItem: The session is not connected!");
                    return;
                }
            }
            if (m_miCurrentTime == null || m_miInt64 == null)
            {
                Console.WriteLine("Monitored items are not created.");
                if (m_transfer_session?.CurrentState == State.Active)
                {
                    List<ClientMonitoredItem> monitoredItems = m_transfer_session.Subscriptions.SelectMany(subscription => subscription.MonitoredItems).ToList();

                    try
                    {
                        foreach (var monitoredItem in monitoredItems)
                        {
                            monitoredItem.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                            Console.WriteLine("Monitored item '{0}' unsubscribed from receiving data change notifications.", monitoredItem.DisplayName);
                            monitoredItem.Delete();
                            Console.WriteLine("Monitored item '{0}' deleted.", monitoredItem.DisplayName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.PrintException("DeleteMonitoredItem", ex);
                    }
                }
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
            if (m_miInt64 != null)
            {
                Console.WriteLine("MonitoredItem already created");
                return;
            }
            try
            {
                //create monitored item for server CurrentTime
                m_miCurrentTime = new ClientMonitoredItem(m_subscription, m_miCurrentTimeNodeId, "Monitored Item Server CurrentTime", false);
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
                m_miInt64 = new ClientMonitoredItem(m_subscription, m_miInt64NodeId, "Monitored Item Int64", false);
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
                m_miInt16 = new ClientMonitoredItem(m_subscription, m_miInt16NodeId, "Monitored Item Int16", false);
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
                m_miInt32 = new ClientMonitoredItem(m_subscription, m_miInt32NodeId, "Monitored Item Int32", false);
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
        #endregion

        #region Transfer Subscriptions

        /// <summary>
        /// Transfer existing subscriptions from one session to another. 
        /// This allows you to maintain the monitoring of data changes or events without losing information when switching sessions.
        /// The client creates a new session in the same process and transfers subscriptions into the new session while the old session 
        /// is still active.
        /// </summary>
        public void TransferSubscription()
        {
            if (m_session == null)
            {
                Console.WriteLine("TransferSubscription: The session is not initialized!");
                return;
            }
            try
            {
                bool validSourceSession = ValidateSourceSession();

                if (validSourceSession)
                {
                    m_transfer_session = CreateSession("Transferred Session", MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                    m_transfer_session.Connect(false, false);
                    Console.WriteLine($"{m_transfer_session.SessionName} is connected.");

                    if (IsValidStateForTransfer())
                    {
                        Console.WriteLine("Transferring subscriptions...");

                        bool isTransferred = m_transfer_session.TransferSubscriptions(new Collection<ClientSubscription> { m_subscription }, false);

                        bool successfulTransfer = ValidateSuccessfulTransfer();

                        if (isTransferred && successfulTransfer)
                        {
                            Console.WriteLine("Transfer subscriptions completed successfully!");

                            m_session.Disconnect(false);
                            m_session.Dispose();
                            m_isDisposed = true;
                        }
                        else
                        {
                            Console.WriteLine("Transfer subscriptions failed to complete!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("MonitoredItemClient.TransferSubscription: Transfer subscription did not start. Invalid session!");
                    }
                }
                else
                {
                    Console.WriteLine("MonitoredItemClient.TransferSubscription: Transfer subscription did not start. Invalid session!");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TransferSubscription", ex);
            }
        }

        /// <summary>
        /// Create a secondary Session.
        /// Create a subscription with a monitored item.
        /// Close session, but do not delete subscriptions.
        /// Transfer subscription from closed session to the other.
        /// </summary>
        public async Task TransferSubscriptionSessionClosed()
        {
            // The client closes the old session but has the new DeleteSubscriptionsOnClose property set to false.
            // The old session is closed, but the subscriptions remain abandoned on the server and keep collecting samples.
            // The client creates a new session and transfers the abandoned subscriptions to the new session,
            // for which the client library has still all information available.
            if (m_session == null)
            {
                Console.WriteLine("TransferSubscriptions: The session is not initialized!");
                return;
            }
            try
            {
                m_session.DeleteSubscriptionsOnClose = false;

                bool savedSubscriptions = m_session.PersistSubscriptions(m_filePathSubscriptions, new[] { typeof(List<ClientSubscription>) });

                await m_session.DisconnectAsync(false).ConfigureAwait(false);
                Console.WriteLine("{0} is disconnected.", SessionName);

                m_transfer_session = CreateSession("Transferred Session", MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                await m_transfer_session.ConnectAsync(false, false).ConfigureAwait(false);

                // support transfer
                m_transfer_session.RepublishAfterTransfer = true;

                Func<List<ClientSubscription>> createSubscriptionList = () => new List<ClientSubscription>();

                List<ClientSubscription> subscriptionsRestored =
                    m_transfer_session.LoadSubscriptions(m_filePathSubscriptions, true, new[] { typeof(List<ClientSubscription>) });

                HookNotificationForLogOutput(subscriptionsRestored);

                bool successfulTransfer = m_transfer_session.TransferSubscriptions(subscriptionsRestored, true);

                if (successfulTransfer)
                {
                    Console.WriteLine("Transfer subscriptions completed successfully after closed session!");

                    m_session.Disconnect(false);
                    m_session.Dispose();
                    m_isDisposed = true;
                }
                else
                {
                    Console.WriteLine("Transfer subscriptions failed to complete!");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TransferSubscriptionSessionClosed", ex);
            }
        }

        /// <summary>
        /// Asynchronously transfer existing subscriptions from one session to another. 
        /// This allows you to maintain the monitoring of data changes or events without losing information when switching sessions. 
        /// </summary>
        public async Task TransferSubscriptionAsync()
        {
            if (m_session == null)
            {
                Console.WriteLine("TransferSubscriptionAsync: The session is not initialized!");
                return;
            }
            try
            {
                bool validSourceSession = ValidateSourceSession();

                if (validSourceSession)
                {
                    m_transfer_session = CreateSession("Transferred Session", MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                    m_transfer_session.Connect(false, false);
                    Console.WriteLine($"{m_transfer_session.SessionName} is connected.");

                    if (IsValidStateForTransfer())
                    {
                        Console.WriteLine("Transferring subscriptions...");

                        bool isTransferred = await m_transfer_session.TransferSubscriptionsAsync(new Collection<ClientSubscription> { m_subscription }, false).ConfigureAwait(false);

                        bool successfulTransfer = ValidateSuccessfulTransfer();

                        if (isTransferred && successfulTransfer)
                        {
                            Console.WriteLine("Transfer subscriptions completed successfully!");

                            //m_session.Disconnect(false);
                            //m_session.Dispose();
                            //m_isDisposed = true;
                        }
                        else
                        {
                            Console.WriteLine("Transfer subscriptions failed to complete!");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("MonitoredItemClient.TransferSubscriptionAsync: Transfer subscription did not start. Invalid session!");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TransferSubscriptionAsync", ex);
            }
        }

        /// <summary>
        /// Save session subscriptions to a specific file. Useful for subscriptions persistence when session is closed.
        /// </summary>
        public void SaveSubscriptionsForTransfer()
        {
            Console.WriteLine("Save subscriptions for transfer...");

            m_session.DeleteSubscriptionsOnClose = false;

            //bool savedSubscriptionsWithCallback = m_session.PersistSubscriptions(m_filePathSubscriptions, new[] { typeof(List<ClientSubscription>) }, MyPersistCallback);

            bool savedSubscriptions = m_session.PersistSubscriptions(m_filePathSubscriptions, new[] { typeof(List<ClientSubscription>) });

            if (savedSubscriptions)
            {
                Console.WriteLine("Subscriptions saved!");
            }
            else
            {
                Console.WriteLine("Subscriptions not saved!");
            }
        }

        /// <summary>
        /// Restore subscriptions from specific file and transfer subscriptions to a new session. 
        /// </summary>
        /// <returns></returns>
        public async Task LoadSubscriptionsForTransfer()
        {
            m_transfer_session = CreateSession("Transferred Session", MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

            await m_transfer_session.ConnectAsync(false, true).ConfigureAwait(false);
            Console.WriteLine($"{m_transfer_session.SessionName} is connected.");

            m_transfer_session.DeleteSubscriptionsOnClose = false;
            m_transfer_session.RepublishAfterTransfer = true;

            Console.WriteLine("Loading subscriptions for transfer...");

            //List<ClientSubscription> subscriptionsRestoredWithCallback = m_transfer_session.LoadSubscriptions(m_filePathSubscriptions,
            //    true,
            //    new[] { typeof(List<ClientSubscription>) },
            //    () => { return new List<ClientSubscription>() { m_subscription }; }
            //);

            List<ClientSubscription> subscriptionsRestored = m_transfer_session.LoadSubscriptions(m_filePathSubscriptions,
                true,
                new[] { typeof(List<ClientSubscription>) },
                null
            );

            if (!subscriptionsRestored.Any())
            {
                Console.WriteLine("Subscriptions not loaded!");
            }
            else
            {
                Console.WriteLine("Transferring subscriptions...");

                HookNotificationForLogOutput(subscriptionsRestored);

                bool successfulTransfer = m_transfer_session.TransferSubscriptions(subscriptionsRestored, true);

                if (successfulTransfer)
                {
                    Console.WriteLine("Transfer subscriptions completed successfully!");
                    await m_session.DisconnectAsync(false);
                    m_session.Dispose();
                }
                else
                {
                    Console.WriteLine("Transfer subscriptions failed to complete!");
                }
            }
        }

        /// <summary>
        /// Transfer subscriptions to a session with identical secrets and validate the transfer
        /// </summary>
        /// <returns></returns>
        public async Task TransferSubscriptionsWithUserId()
        {
            bool validSourceSession = ValidateSourceSession();

            if (validSourceSession)
            {
                // create the second session object with username and password
                m_transfer_session = CreateSession("Transferred Session", MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                await m_transfer_session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine($"{m_transfer_session.SessionName} is connected.");

                m_transfer_session.DeleteSubscriptionsOnClose = false;
                m_transfer_session.RepublishAfterTransfer = true;

                List<ClientSubscription> transferSubscriptions = new List<ClientSubscription> { m_subscription };

                HookNotificationForLogOutput(transferSubscriptions);

                bool successfulTransfer = m_transfer_session.TransferSubscriptions(transferSubscriptions, false);
                if (successfulTransfer)
                {
                    Console.WriteLine("Transfer subscriptions completed successfully!");

                    m_session.Disconnect(false);
                    m_session.Dispose();
                    m_isDisposed = true;
                }
                else
                {
                    Console.WriteLine("BadUserAccessDenied: Transfer subscriptions failed to complete!");
                }
            }
            else
            {
                Console.WriteLine("MonitoredItemClient.TransferSubscriptionsWithUserId: Transfer subscription did not start. Invalid session!");
            }
        }

        /// <summary>
        /// Transfer subscriptions to a session with certificate and validate the transfer
        /// </summary>
        /// <returns></returns>
        public async Task TransferSubscriptionsWithCertificate()
        {
            bool validSourceSession = ValidateSourceSession();

            if (validSourceSession)
            {
                // create the second session object with username and password
                m_transfer_session = CreateSessionWithCertificate("Transferred Session", null as string, "opcuser.pfx");

                await m_transfer_session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine($"{m_transfer_session.SessionName} is connected.");

                m_transfer_session.DeleteSubscriptionsOnClose = false;
                m_transfer_session.RepublishAfterTransfer = true;

                List<ClientSubscription> transferSubscriptions = new List<ClientSubscription> { m_subscription };

                HookNotificationForLogOutput(transferSubscriptions);

                bool successfulTransfer = m_transfer_session.TransferSubscriptions(transferSubscriptions, false);
                {
                    Console.WriteLine("Transfer subscriptions completed successfully!");

                    m_session.Disconnect(false);
                    m_session.Dispose();
                    m_isDisposed = true;
                }
                if (!successfulTransfer)
                {
                    Console.WriteLine("BadUserAccessDenied: Transfer subscriptions failed to complete!");
                }
            }
            else
            {
                Console.WriteLine("MonitoredItemClient.TransferSubscriptionsWithCertificate: Transfer subscription did not start. Invalid session!");
            }
        }

        /// <summary>
        /// Transfer subscriptions to a session with certificate password and validate the transfer
        /// </summary>
        /// <returns></returns>
        public async Task TransferSubscriptionsWithCertificatePassword()
        {
            bool validSourceSession = ValidateSourceSession();

            if (validSourceSession)
            {
                // create the second session object with username and password
                m_transfer_session = CreateSessionWithCertificate("Transferred Session", "User_Pwd", "opcuserPwd.pfx");

                await m_transfer_session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine($"{m_transfer_session.SessionName} is connected.");

                m_transfer_session.DeleteSubscriptionsOnClose = false;
                m_transfer_session.RepublishAfterTransfer = true;

                List<ClientSubscription> transferSubscriptions = new List<ClientSubscription> { m_subscription };

                HookNotificationForLogOutput(transferSubscriptions);

                bool successfulTransfer = m_transfer_session.TransferSubscriptions(transferSubscriptions, false);
                {
                    Console.WriteLine("Transfer subscriptions completed successfully!");

                    m_session.Disconnect(false);
                    m_session.Dispose();
                    m_isDisposed = true;
                }
                if (!successfulTransfer)
                {
                    Console.WriteLine("BadUserAccessDenied: Transfer subscriptions failed to complete!");
                }
            }
            else
            {
                Console.WriteLine("MonitoredItemClient.TransferSubscriptionsWithCertificatePassword: Transfer subscription did not start. Invalid session!");
            }
        }

        /// <summary>
        /// Transfer subscriptions to a session with security and validate the transfer
        /// </summary>
        /// <returns></returns>
        public async Task TransferSubscriptionsWithSecurity()
        {
            bool validSourceSession = ValidateSourceSession();

            if (validSourceSession)
            {
                // create the second session object with username and password
                m_transfer_session = CreateSession("Transferred Session", MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256, new UserIdentity());

                await m_transfer_session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine($"{m_transfer_session.SessionName} is connected.");

                m_transfer_session.DeleteSubscriptionsOnClose = false;
                m_transfer_session.RepublishAfterTransfer = true;

                List<ClientSubscription> transferSubscriptions = new List<ClientSubscription> { m_subscription };

                HookNotificationForLogOutput(transferSubscriptions);

                bool successfulTransfer = m_transfer_session.TransferSubscriptions(transferSubscriptions, false);
                {
                    Console.WriteLine("Transfer subscriptions completed successfully!");

                    m_session.Disconnect(false);
                    m_session.Dispose();
                    m_isDisposed = true;
                }
                if (!successfulTransfer)
                {
                    Console.WriteLine("BadUserAccessDenied: Transfer subscriptions failed to complete!");
                }
            }
            else
            {
                Console.WriteLine("MonitoredItemClient.TransferSubscriptionsWithSecurity: Transfer subscription did not start. Invalid session!");
            }
        }

        /// <summary>
        /// Ensure that the source session (from which the subscription is being transferred) is valid and active
        /// </summary>
        /// <returns></returns>
        private bool ValidateSourceSession()
        {
            if (m_session == null || m_session?.Id == null)
            {
                Console.WriteLine("MonitoredItemClient.ValidateSourceSession: Invalid session.");
                return false;
            }

            if (m_session?.CurrentState != State.Active)
            {
                Console.WriteLine("MonitoredItemClient.ValidateSourceSession: Session not active.");
                return false;
            }

            return true;

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

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and a certificate user identity.
        /// </summary>
        private ClientSession CreateSessionWithCertificate(string sessionName, string password, string certificatePath)
        {
            try
            {
                // use the pfx certificate file located in Files folder
                string certificateFilePath = Path.Combine("Files", certificatePath);
                if (!File.Exists(certificateFilePath))
                {
                    Console.WriteLine("The user certificate file is missing ('{0}').", certificateFilePath);
                    return null;
                }
                // load the certificate with password from file
                X509Certificate2 certificate = new X509Certificate2(certificateFilePath,
                               password,
                               X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

                if (certificate != null)
                {
                    // create UserIdentity from certificate
                    UserIdentity certificateUserIdentity = new UserIdentity(certificate);

                    Console.WriteLine("\r\nCreate session using certificate located at '{0}'", certificateFilePath);

                    ClientSession session = CreateSession(
                        sessionName, MessageSecurityMode.None, SecurityPolicy.None, certificateUserIdentity);

                    session.SessionName = sessionName;


                    return session;
                }
                else
                {
                    Console.WriteLine("Cannot load certificate from '{0}'", certificateFilePath);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Program.PrintException(sessionName, ex);
                return null;
            }
        }

        /// <summary>
        /// Validate sessions (source and transfer session), subscriptions and monitored items used for transfer. 
        /// To be checked before Transfer Subscriptions
        /// </summary>
        /// <returns>True if the sessions, subscriptions and monitored items are valid for transfer</returns>
        private bool IsValidStateForTransfer()
        {
            if (ValidateSessions() && ValidateSubscriptions() && ValidateMonitoredItems())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        // Ensure that both the source session (from which the subscription is being transferred)
        // and the target session (to which the subscription is being transferred) are valid and active.        
        /// To be checked before Transfer Subscriptions
        /// </summary>
        /// <returns>True if sessions are valid in order to be used for Transfer Subscriptions</returns>
        /// <exception cref="ServiceResultException"></exception>
        private bool ValidateSessions()
        {
            if (m_session == null || m_transfer_session == null || m_session?.Id == null || m_transfer_session?.Id == null)
            {
                throw new ServiceResultException(StatusCodes.BadSessionIdInvalid, "MonitoredItemClient.ValidateSessions: Transfer subscription attempt on invalid session.");
            }

            if (m_session.CurrentState != State.Active && m_transfer_session.CurrentState != State.Active)
            {
                throw new ServiceResultException(StatusCodes.BadSessionNotActivated, "MonitoredItemClient.ValidateSessions: Session not active.");
            }

            // Check if both sessions have the same security policies and user tokens
            if (m_session.SecurityPolicy != m_transfer_session.SecurityPolicy)
            {
                throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "MonitoredItemClient.ValidateSessions: Sessions have not the same security policies.");
            }

            // Verify that the session from which the subscription is being transferred has been closed
            if (m_isDisposed)
            {
                throw new ServiceResultException(StatusCodes.BadSessionClosed, "MonitoredItemClient.ValidateSessions: Transfer subscription attempt on session disposed.");
            }

            return true;
        }

        /// <summary>
        /// Ensure subscriptions are valid before the transfer
        /// To be checked before Transfer Subscriptions
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private bool ValidateSubscriptions()
        {
            if (!m_session.Subscriptions.Select(subscription => subscription.Session.Id).Contains(m_session.Id))
            {
                throw new ServiceResultException("MonitoredItemClient.ValidateSubscriptions: Invalid subscription.");
            }

            // Ensure that the subscription ID being transferred exists and is valid.
            if (m_subscription == null || m_subscription?.Id == null)
            {
                throw new ServiceResultException(StatusCodes.BadNoSubscription, "MonitoredItemClient.ValidateSubscriptions: Invalid subscription.");
            }

            // Confirm that the subscription is in an active state.
            if (m_subscription.CurrentState != State.Active)
            {
                throw new ServiceResultException(StatusCodes.BadNoSubscription, "MonitoredItemClient.ValidateSubscriptions: Inactive or deleted subscriptions cannot be transferred.");
            }

            // Validate that the subscription being transferred is owned by the current session.
            if (m_subscription.Session.Id != m_session.Id)
            {
                throw new ServiceResultException("MonitoredItemClient.ValidateSubscriptions: Current subscriptions are not owned by the current session.");
            }
            return true;
        }

        /// <summary>
        /// Ensure monitored items are valid before the transfer
        /// To be checked before Transfer Subscriptions
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private bool ValidateMonitoredItems()
        {
            // Validate that the monitored items exist and are active
            if (m_miInt64 == null || m_miInt64.CurrentState != State.Active ||
                m_miCurrentTime == null || m_miCurrentTime.CurrentState != State.Active ||
                m_miMotorTemperature == null || m_miMotorTemperature.CurrentState != State.Active)
            {
                throw new ServiceResultException("MonitoredItemClient.ValidateMonitoredItems: Monitored items not available for transfer.");
            }

            // Validate that the monitored items are properly linked to the subscription.
            if (m_miInt64.Subscription.Session.Id != m_session.Id ||
                m_miCurrentTime.Subscription.Session.Id != m_session.Id ||
                m_miMotorTemperature.Subscription.Session.Id != m_session.Id)
            {
                throw new ServiceResultException("MonitoredItemClient.ValidateMonitoredItems: Monitored items are not properly linked to the subscription.");
            }

            return true;
        }

        /// <summary>
        /// Validate Transfer subscriptions results after the transfer
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private bool ValidateSuccessfulTransfer()
        {
            // Validate that the subscription being transferred is owned by the current session.
            if (m_subscription.Session.Id != m_transfer_session.Id)
            {
                throw new ServiceResultException("MonitoredItemClient.ValidateSuccessfulTransfer: Current subscriptions are not owned by the current session.");
            }

            // Validate that the monitored items are properly linked to the subscription of the transferred session.
            if (m_miInt64.Subscription.Session.Id != m_transfer_session.Id || m_miCurrentTime.Subscription.Session.Id != m_transfer_session.Id)
            {
                throw new ServiceResultException("MonitoredItemClient.ValidateSuccessfulTransfer: Monitored items are not properly linked to the transferred subscription.");
            }
            if (m_miCurrentTime.Subscription.Session.Id != m_transfer_session.Id || m_miCurrentTime.Subscription.Session.Id != m_transfer_session.Id)
            {
                throw new ServiceResultException("MonitoredItemClient.ValidateSuccessfulTransfer: Monitored items are not properly linked to the transferred subscription.");
            }
            if (m_miMotorTemperature.Subscription.Session.Id != m_transfer_session.Id || m_miCurrentTime.Subscription.Session.Id != m_transfer_session.Id)
            {
                throw new ServiceResultException("MonitoredItemClient.ValidateSuccessfulTransfer: Monitored items are not properly linked to the transferred subscription.");
            }

            int kMonitoredItems = m_transfer_session.Subscriptions.Sum(subscription => subscription.MonitoredItems.Count());

            // Validate partial monitored items transfer failures
            if (m_subscription.MonitoredItems.Count != kMonitoredItems)
            {
                throw new ServiceResultException("MonitoredItemClient.ValidateSuccessfulTransfer: Some monitored items failed to transfer.");
            }

            return true;
        }

        /// <summary>
        /// Example of save subscriptions callback to be implemented
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="knownTypes"></param>
        /// <returns></returns>
        private bool MyPersistCallback(string filePath, IEnumerable<Type> knownTypes)
        {
            Console.WriteLine($"Callback is invoked!");
            return true;
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

        private void HookNotificationForLogOutput(ICollection<ClientSubscription> transferSubscriptions)
        {
            foreach (ClientSubscription clientSubscription in transferSubscriptions)
            {
                foreach (ClientMonitoredItem monitoredItem in clientSubscription.MonitoredItems)
                {
                    monitoredItem.DataChangesReceived += Monitoreditem_DataChangesReceived;
                }
            }
        }
        #endregion
    }
}
