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
using Softing.Opc.Ua.Client.DurableSubscriptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that contains sample code for TransferSubscription functionality
    /// </summary>
    class TransferSubscriptionClient
    {
        #region Private Fields

        private const string SessionName = "TransferSubscriptionClient Session";
        private const string SubscriptionName = "TransferSubscriptionClient Subscription";
        private const string TransferSessionName = "TransferSubscriptionClient Transferred Session";

        private readonly UaApplication m_application;

        private ClientSession m_session;
        private ClientSession m_transferSession;
        private ClientSubscription m_subscription;

        private ClientMonitoredItem m_miCurrentTime;
        private ClientMonitoredItem m_miInt64;
        private ClientMonitoredItem m_miMotorTemperature;

        private bool m_isDisposed = false;

        //"Server\\ServerStatus\\CurrentTime";
        private readonly NodeId m_miCurrentTimeNodeId = VariableIds.Server_ServerStatus_CurrentTime;

        // "CTT\\Scalar\\Simulation\\Int64";
        private readonly NodeId m_miInt64NodeId = new NodeId("ns=7;s=CTT_Scalar_Simulation_Int64");

        //"DataAccess\\Refrigerator\\MotorTemperature";
        private readonly NodeId m_miMotorTemperatureNodeId = new NodeId("ns=3;i=24");

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of TransferSubscriptionClient
        /// </summary>
        /// <param name="application"></param>
        public TransferSubscriptionClient(UaApplication application)
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
                //m_session = CreateSession(SessionName, MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256, new UserIdentity());

                //connect session
                await m_session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine("Session is connected.");

                if (withSubscription)
                {
                    m_session.DeleteSubscriptionsOnClose = false;
                    m_session.TransferSubscriptionsOnReconnect = true;

                    //create the subscription
                    m_subscription = CreateSubscription(m_session, SubscriptionName);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateSession", ex);

                if (m_session != null)
                {
                    DeleteSession(ref m_session);
                }

                m_subscription = null;
            }
        }

        /// <summary>
        /// Delete subscription
        /// </summary>
        /// <returns></returns>
        internal async Task DeleteSubscription()
        {
            if (m_subscription != null)
            {
                string subscriptionName = m_subscription.DisplayName;

                await m_subscription.DisconnectAsync(true).ConfigureAwait(false);
                m_subscription.Delete();
                m_subscription = null;

                Console.WriteLine("Subscription {0} was deleted.", subscriptionName);
            }
        }

        /// <summary>
        /// Disconnect and delete the session, transfer session, subscriptions and monitored items.
        /// </summary>
        public async Task DisconnectAndDispose()
        {
            try
            {
                DeleteMonitoredItems();

                //delete transfer subscription
                await DeleteSubscription().ConfigureAwait(false);

                // delete sessions
                DeleteSession(ref m_session);
                DeleteSession(ref m_transferSession);
            }
            catch (Exception ex)
            {
                Program.PrintException("DisconnectAndDispose", ex);
            }
        }

        /// <summary>
        ///  Create subscription
        /// </summary>
        /// <param name="session"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        internal ClientSubscription CreateSubscription(ClientSession session, string subscriptionName)
        {
            //create the subscription
            ClientSubscription subscription = new ClientSubscription(session, subscriptionName) { DeleteOnDisconnect = false };

            Console.WriteLine("Subscription {0} created.", subscriptionName);

            // set the Publishing interval for this subscription
            subscription.PublishingInterval = 500;

            subscription.RepublishAfterTransfer = true;

            Console.WriteLine("Subscription created");

            return subscription;
        }

        /// <summary>
        /// Creates a monitoredItem. The monitored item is activated in the constructor if the subscription is active as well.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="displayName"></param>
        /// <param name="samplingInterval"></param>
        /// <returns></returns>
        internal ClientMonitoredItem CreateMonitoredItem(NodeId nodeId, string displayName, int samplingInterval)
        {
            ClientMonitoredItem clientMonitoredItem = new ClientMonitoredItem(m_subscription, nodeId, displayName) { DeleteOnDisconnect = false };
            clientMonitoredItem.DataChangesReceived += Monitoreditem_DataChangesReceived;
            //set sampling interval to 'samplingInterval' seconds
            clientMonitoredItem.SamplingInterval = samplingInterval;

            if (clientMonitoredItem.CurrentState == State.Active)
            {
                Console.WriteLine("Monitored item '{0}' created. Data value changes are shown:", clientMonitoredItem.DisplayName);
            }
            else
            {
                Console.WriteLine("Monitored item '{0}' created with state {1}", clientMonitoredItem.DisplayName, clientMonitoredItem.CurrentState);
            }
            return clientMonitoredItem;
        }

        /// <summary>
        /// Creates a monitoredItems.
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
                Console.WriteLine("CreateMonitoredItem: The subscription is not connected!");
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
                m_miCurrentTime = CreateMonitoredItem(m_miCurrentTimeNodeId, "Monitored Item Server CurrentTime", 1000);
                #endregion

                #region Create #2 monitored item - for Int64 variable
                m_miInt64 = CreateMonitoredItem(m_miInt64NodeId, "Monitored Item Int64", 3000);
                #endregion

                #region Create #3 monitored item - for MotorTemperature variable
                m_miMotorTemperature = CreateMonitoredItem(m_miMotorTemperatureNodeId, "Monitored Item Custom MotorTemperature", 3000);
                #endregion
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateMonitoredItem", ex);
            }
        }

        /// <summary>
        /// Deletes the specified MonitoredItem.
        /// </summary>
        /// <param name="clientMonitoredItem"></param>
        internal void DeleteMonitoredItem(ref ClientMonitoredItem clientMonitoredItem)
        {
            if (clientMonitoredItem != null)
            {
                clientMonitoredItem.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item '{0}' unsubscribed from receiving data change notifications.", clientMonitoredItem.DisplayName);
                clientMonitoredItem.Delete();
                Console.WriteLine("Monitored item '{0}' deleted.", clientMonitoredItem.DisplayName);
                clientMonitoredItem = null;
            }
        }

        /// <summary>
        /// Deletes the current MonitoredItems.
        /// </summary>
        internal void DeleteMonitoredItems()
        {
            if (m_session == null)
            {
                Console.WriteLine("DeleteMonitoredItems: The session is not initialized!");

                if (m_transferSession == null)
                {
                    Console.WriteLine("DeleteMonitoredItem: The transfer session is not initialized!");
                    return;
                }
            }

            if (m_miCurrentTime == null || m_miInt64 == null || m_miMotorTemperature == null)
            {
                Console.WriteLine("Monitored items are not created.");
                return;
            }

            try
            {
                var m_currentSession = GetCurrentSession();

                if (m_currentSession?.CurrentState == State.Active || m_currentSession?.CurrentState == State.Connected)
                {
                    List<ClientMonitoredItem> monitoredItems = m_currentSession.Subscriptions.SelectMany(subscription => subscription.MonitoredItems).ToList();

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

                DeleteMonitoredItem(ref m_miCurrentTime);

                DeleteMonitoredItem(ref m_miInt64);

                DeleteMonitoredItem(ref m_miMotorTemperature);
            }
            catch (Exception ex)
            {
                Program.PrintException("DeleteMonitoredItem", ex);
            }
        }

        /// <summary>
        /// Get the current session.
        /// </summary>
        private ClientSession GetCurrentSession()
        {
            if (!m_isDisposed)
            {
                return m_session;
            }
            else if (m_session?.CurrentState == State.Disconnected)
            {
                return m_transferSession;
            }
            else if (m_transferSession?.CurrentState == State.Disconnected)
            {
                return m_session;
            }

            // return the default session
            return m_session;
        }

        #endregion

        #region Transfer Subscriptions Methods

        /// <summary>
        /// Transfer existing subscriptions from one session to another. 
        /// No subscriptions have been saved since the current subscription is transferred. 
        /// This allows you to maintain the monitoring of data changes or events without losing information when switching sessions.
        /// The client creates a new session in the same process and transfers subscriptions into the new session while the old session is still active.
        /// </summary>
        public void TransferSubscription()
        {
            try
            {
                bool validSourceSession = ValidateSourceSession();

                if (validSourceSession)
                {
                    m_transferSession = CreateSession(TransferSessionName, MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                    m_transferSession.Connect(false, false);

                    Console.WriteLine();
                    Console.WriteLine($"{m_transferSession.SessionName} is connected.");

                    if (IsValidStateForTransfer())
                    {
                        Console.WriteLine("Transferring subscriptions...");

                        bool isTransferred = m_transferSession.TransferSubscriptions(new Collection<ClientSubscription> { m_subscription }, false);

                        bool successfulTransfer = ValidateSuccessfulTransfer();

                        if (isTransferred && successfulTransfer)
                        {
                            Console.WriteLine("Transfer subscriptions completed successfully!");

                            ReplaceWithTransferSession();
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
        /// Asynchronously transfer existing subscriptions from one session to another. 
        /// This allows you to maintain the monitoring of data changes or events without losing information when switching sessions.
        /// No subscriptions have been saved since the current subscription is transferred. 
        /// </summary>
        public async Task TransferSubscriptionAsync()
        {
            try
            {
                bool validSourceSession = ValidateSourceSession();

                if (validSourceSession)
                {
                    m_transferSession = CreateSession(TransferSessionName, MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                    m_transferSession.Connect(false, false);
                    Console.WriteLine();
                    Console.WriteLine($"{m_transferSession.SessionName} is connected.");

                    if (IsValidStateForTransfer())
                    {
                        Console.WriteLine("Transferring subscriptions...");

                        bool isTransferred = await m_transferSession.TransferSubscriptionsAsync(new Collection<ClientSubscription> { m_subscription }, false).ConfigureAwait(false);

                        bool successfulTransfer = ValidateSuccessfulTransfer();

                        if (isTransferred && successfulTransfer)
                        {
                            Console.WriteLine("Transfer subscriptions completed successfully!");

                            ReplaceWithTransferSession();
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
        /// Create a secondary Session.
        /// Create a subscription with a monitored item.
        /// Close session, but do not delete subscriptions.
        /// Persist and load subscriptions to be transferred.
        /// Transfer subscription from closed session to the other.
        /// </summary>
        public void TransferSubscriptionSessionClosed()
        {
            // The client closes the old session but has the new DeleteSubscriptionsOnClose property set to false.
            // The old session is closed, but the subscriptions remain abandoned on the server and keep collecting samples.
            // The client creates a new session and transfers the abandoned subscriptions to the new session,
            // for which the client library has still all information available.

            try
            {
                bool validSourceSession = ValidateSourceSession();

                if (validSourceSession)
                {
                    m_session.DeleteSubscriptionsOnClose = false;
                    m_session.TransferSubscriptionsOnReconnect = true;

                    // support transfer
                    m_subscription.RepublishAfterTransfer = true;
                    Console.WriteLine("RepublishAfterTransfer {0}", m_subscription.RepublishAfterTransfer);

                    string transferSubscriptionsPath = m_application.ClientToolkitConfiguration.TransferSubscriptionsPath;
                    if (string.IsNullOrEmpty(m_application.ClientToolkitConfiguration.TransferSubscriptionsPath))
                    {
                        transferSubscriptionsPath = "Transfer Subscriptions";
                    }

                    string transferSubscriptionsDirectory = Path.Combine(Environment.CurrentDirectory, transferSubscriptionsPath);
                    if (!Directory.Exists(transferSubscriptionsDirectory))
                    {
                        Directory.CreateDirectory(transferSubscriptionsDirectory);
                    }

                    string transferSubscriptionsFileName = Path.Combine(transferSubscriptionsDirectory, m_application.ClientToolkitConfiguration.TransferSubscriptionsFileName);

                    // Note: the user is responsible to provide a secure path for durable subscriptions metadata
                    bool savedSubscriptions = ((IPersister)m_session).Persist<List<ClientSubscription>>(
                        transferSubscriptionsFileName,
                        new List<ClientSubscription> { m_subscription },
                        new[] { typeof(List<ClientSubscription>) });


                    DeleteSession(ref m_session);
                    Console.WriteLine("{0} is closed.", SessionName);

                    m_transferSession = CreateSession(TransferSessionName, MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                    m_transferSession.Connect(false, false);

                    m_transferSession.TransferSubscriptionsOnReconnect = true;
                    Console.WriteLine("TransferSubscriptionsOnReconnect {0}", m_transferSession.TransferSubscriptionsOnReconnect);

                    Func<List<ClientSubscription>> createSubscriptionList = () => new List<ClientSubscription>();

                    List<ClientSubscription> subscriptionsRestored = new List<ClientSubscription>();

                    if (File.Exists(transferSubscriptionsFileName))
                    {
                        subscriptionsRestored = ((ILoader)m_transferSession).Load<ClientSubscription>(
                            transferSubscriptionsFileName, true, new[] { typeof(List<ClientSubscription>) }).ToList();

                        if (subscriptionsRestored != null && subscriptionsRestored.Any() || subscriptionsRestored.Count != 0)
                        {
                            #region reset the DeleteOnDisconnect flag
                            foreach (ClientSubscription subscription in subscriptionsRestored)
                            {
                                subscription.DeleteOnDisconnect = false;
                                foreach (ClientMonitoredItem mi in subscription.MonitoredItems)
                                {
                                    mi.DeleteOnDisconnect = false;
                                }
                            }
                            #endregion

                            //HandleNotifications(subscriptionsRestored);
                            Console.WriteLine($"Transferring subscriptions...");

                            bool isTransferred = m_transferSession.TransferSubscriptions(subscriptionsRestored, true);

                            if (isTransferred)
                            {
                                Console.WriteLine("Transfer subscriptions completed successfully after closed session!");

                                // Note: Do not forget to properly clean up subscriptions saved after transfer.
                                CleanupSavedSubscriptions(transferSubscriptionsDirectory, transferSubscriptionsFileName);

                                // Change the monitor items from imported instances in order to receive notifications
                                foreach (var subscription in m_transferSession.Subscriptions)
                                {
                                    foreach (ClientMonitoredItem mi in subscription.MonitoredItems)
                                    {
                                        mi.DataChangesReceived += Monitoreditem_DataChangesReceived;
                                        if (mi.ClientHandle == m_miCurrentTime.ClientHandle)
                                        {
                                            DeleteMonitoredItem(ref m_miCurrentTime);
                                            m_miCurrentTime = mi;
                                        }
                                        if (mi.ClientHandle == m_miInt64.ClientHandle)
                                        {
                                            DeleteMonitoredItem(ref m_miInt64);
                                            m_miInt64 = mi;
                                            m_miInt64.DataChangesReceived += Monitoreditem_DataChangesReceived;
                                        }
                                        if (mi.ClientHandle == m_miMotorTemperature.ClientHandle)
                                        {
                                            DeleteMonitoredItem(ref m_miMotorTemperature);
                                            m_miMotorTemperature = mi;
                                        }
                                    }
                                }

                                ReplaceWithTransferSession();

                                bool successfulTransfer = ValidateSuccessfulTransfer();
                                if (successfulTransfer)
                                {
                                    Console.WriteLine("Transfer subscriptions completed successfully!");

                                    m_session.Disconnect(false);
                                    m_session.Connect(false, false);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Transfer subscriptions failed to complete!");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Subscriptions not loaded. No subscription available for transfer!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{transferSubscriptionsFileName} file is missing!");
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TransferSubscriptionSessionClosed", ex);
            }
        }

        /// <summary>
        /// Save session subscriptions to a specific file. Useful for subscriptions persistence when session is closed.
        /// </summary>
        public void SaveSubscriptionsForTransfer()
        {
            try
            {
                Console.WriteLine("Save subscriptions for transfer...");

                m_session.DeleteSubscriptionsOnClose = false;

                string transferSubscriptionsPath = m_application.ClientToolkitConfiguration.TransferSubscriptionsPath;
                if (string.IsNullOrEmpty(m_application.ClientToolkitConfiguration.TransferSubscriptionsPath))
                {
                    transferSubscriptionsPath = "Transfer Subscriptions";
                }

                string transferSubscriptionsDirectory = Path.Combine(Environment.CurrentDirectory, transferSubscriptionsPath);
                if (!Directory.Exists(transferSubscriptionsDirectory))
                {
                    Directory.CreateDirectory(transferSubscriptionsDirectory);
                }

                string transferSubscriptionsFileName = Path.Combine(transferSubscriptionsDirectory, m_application.ClientToolkitConfiguration.TransferSubscriptionsFileName);

                // Note: the user is responsible to provide a secure path for durable subscriptions metadata
                bool savedSubscriptions = ((IPersister)m_session).Persist<List<ClientSubscription>>(
                    transferSubscriptionsFileName,
                    default,
                    new[] { typeof(List<ClientSubscription>) });

                Console.WriteLine(savedSubscriptions ? "Subscriptions saved!" : "Subscriptions not saved!");
            }
            catch (Exception ex)
            {
                Program.PrintException("SaveSubscriptionsForTransfer", ex);
            }
        }

        /// <summary>
        /// Restore subscriptions from specific file.
        /// </summary>
        public void LoadSubscriptionsForTransfer()
        {
            try
            {
                string transferSubscriptionsPath = m_application.ClientToolkitConfiguration.TransferSubscriptionsPath;
                if (string.IsNullOrEmpty(m_application.ClientToolkitConfiguration.TransferSubscriptionsPath))
                {
                    transferSubscriptionsPath = "Transfer Subscriptions";
                }

                string transferSubscriptionsDirectory = Path.Combine(Environment.CurrentDirectory, transferSubscriptionsPath);
                if (!Directory.Exists(transferSubscriptionsDirectory))
                {
                    Console.WriteLine($"{transferSubscriptionsPath} path is missing! No subscriptions loaded!");
                }

                string transferSubscriptionsFileName = Path.Combine(transferSubscriptionsDirectory, m_application.ClientToolkitConfiguration.TransferSubscriptionsFileName);
                if (File.Exists(transferSubscriptionsFileName))
                {
                    List<ClientSubscription> subscriptionsRestored = ((ILoader)m_session).Load<ClientSubscription>(
                        transferSubscriptionsFileName, true, new[] { typeof(List<ClientSubscription>) }).ToList();

                    if (subscriptionsRestored.Any() || subscriptionsRestored.Count() != 0)
                    {
                        Console.WriteLine("Subscriptions loaded successfully!");

                        // Note: Do not forget to properly clean up subscriptions saved.
                        CleanupSavedSubscriptions(transferSubscriptionsDirectory, transferSubscriptionsFileName);
                    }
                }
                else
                {
                    Console.WriteLine($"{transferSubscriptionsFileName} file is missing! No subscriptions loaded!");
                }

            }
            catch (Exception ex)
            {
                Program.PrintException("LoadSubscriptionsForTransfer", ex);
            }
        }

        /// <summary>
        /// Transfer subscriptions to a session with identical secrets and validate the transfer.
        /// No subscriptions have been saved since the current subscription is transferred. 
        /// </summary>
        public async Task TransferSubscriptionsWithUserId()
        {
            try
            {
                bool validSourceSession = ValidateSourceSession();

                if (validSourceSession)
                {
                    // create the second session object with username and password
                    m_transferSession = CreateSession(TransferSessionName, MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                    await m_transferSession.ConnectAsync(false, true).ConfigureAwait(false);
                    Console.WriteLine($"{m_transferSession.SessionName} is connected.");

                    m_transferSession.DeleteSubscriptionsOnClose = false;
                    m_subscription.RepublishAfterTransfer = true;

                    List<ClientSubscription> transferSubscriptions = new List<ClientSubscription> { m_subscription };

                    HandleNotifications(transferSubscriptions);

                    bool successfulTransfer = m_transferSession.TransferSubscriptions(transferSubscriptions, false);
                    if (successfulTransfer)
                    {
                        Console.WriteLine("Transfer subscriptions completed successfully!");

                        ReplaceWithTransferSession();
                    }
                    else
                    {
                        Console.WriteLine("BadUserAccessDenied: Transfer subscriptions failed to complete!");

                        await DisconnectAndDispose().ConfigureAwait(false);
                        await Initialize().ConfigureAwait(false);
                    }
                }
                else
                {
                    Console.WriteLine("MonitoredItemClient.TransferSubscriptionsWithUserId: Transfer subscription did not start. Invalid session!");
                }

            }
            catch (Exception ex)
            {
                Program.PrintException("TransferSubscriptionsWithUserId", ex);
            }
        }

        /// <summary>
        /// Transfer subscriptions to a session with certificate and validate the transfer
        /// No subscriptions have been saved since the current subscription is transferred. 
        /// </summary>
        public async Task TransferSubscriptionsWithCertificate()
        {
            try
            {
                bool validSourceSession = ValidateSourceSession();

                if (validSourceSession)
                {
                    // create the second session object with username and password
                    m_transferSession = CreateSessionWithCertificate(TransferSessionName, null as string, "opcuser.pfx");

                    await m_transferSession.ConnectAsync(false, true).ConfigureAwait(false);
                    Console.WriteLine($"{m_transferSession.SessionName} is connected.");

                    m_transferSession.DeleteSubscriptionsOnClose = false;
                    m_subscription.RepublishAfterTransfer = true;

                    List<ClientSubscription> transferSubscriptions = new List<ClientSubscription> { m_subscription };

                    HandleNotifications(transferSubscriptions);

                    bool successfulTransfer = m_transferSession.TransferSubscriptions(transferSubscriptions, false);
                    if (successfulTransfer)
                    {
                        Console.WriteLine("Transfer subscriptions completed successfully!");

                        ReplaceWithTransferSession();
                    }
                    else
                    {
                        Console.WriteLine("BadUserAccessDenied: Transfer subscriptions failed to complete!");

                        await DisconnectAndDispose().ConfigureAwait(false);
                        await Initialize().ConfigureAwait(false);
                    }
                }
                else
                {
                    Console.WriteLine("TransferSubscriptionClient.TransferSubscriptionsWithCertificate: Transfer subscription did not start. Invalid session!");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TransferSubscriptionsWithCertificate", ex);
            }
        }

        /// <summary>
        /// Transfer subscriptions to a session with certificate password and validate the transfer
        /// No subscriptions have been saved since the current subscription is transferred. 
        /// </summary>
        public async Task TransferSubscriptionsWithCertificatePassword()
        {
            try
            {
                bool validSourceSession = ValidateSourceSession();

                if (validSourceSession)
                {
                    // create the second session object with username and password
                    m_transferSession = CreateSessionWithCertificate(TransferSessionName, "User_Pwd", "opcuserPwd.pfx");

                    await m_transferSession.ConnectAsync(false, true).ConfigureAwait(false);
                    Console.WriteLine($"{m_transferSession.SessionName} is connected.");

                    m_transferSession.DeleteSubscriptionsOnClose = false;
                    m_subscription.RepublishAfterTransfer = true;

                    List<ClientSubscription> transferSubscriptions = new List<ClientSubscription> { m_subscription };

                    HandleNotifications(transferSubscriptions);

                    bool successfulTransfer = m_transferSession.TransferSubscriptions(transferSubscriptions, false);
                    if (successfulTransfer)
                    {
                        Console.WriteLine("Transfer subscriptions completed successfully!");

                        ReplaceWithTransferSession();
                    }
                    else
                    {
                        Console.WriteLine("BadUserAccessDenied: Transfer subscriptions failed to complete!");

                        await DisconnectAndDispose().ConfigureAwait(false);
                        await Initialize().ConfigureAwait(false);
                    }
                }
                else
                {
                    Console.WriteLine("TransferSubscriptionClient.TransferSubscriptionsWithCertificatePassword: Transfer subscription did not start. Invalid session!");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TransferSubscriptionsWithCertificatePassword", ex);
            }
        }

        /// <summary>
        /// Transfer subscriptions to a session with security and validate the transfer.
        /// No subscriptions have been saved since the current subscription is transferred. 
        /// </summary>
        public async Task TransferSubscriptionsWithSecurity()
        {
            try
            {
                bool validSourceSession = ValidateSourceSession();

                if (validSourceSession)
                {
                    // create the second session object with username and password
                    m_transferSession = CreateSession(TransferSessionName, MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256, new UserIdentity());

                    await m_transferSession.ConnectAsync(false, true).ConfigureAwait(false);
                    Console.WriteLine($"{m_transferSession.SessionName} is connected.");

                    m_transferSession.DeleteSubscriptionsOnClose = false;
                    m_subscription.RepublishAfterTransfer = true;

                    List<ClientSubscription> transferSubscriptions = new List<ClientSubscription> { m_subscription };

                    HandleNotifications(transferSubscriptions);

                    bool successfulTransfer = m_transferSession.TransferSubscriptions(transferSubscriptions, false);
                    if (successfulTransfer)
                    {
                        Console.WriteLine("Transfer subscriptions completed successfully!");

                        ReplaceWithTransferSession();
                    }
                    else
                    {
                        Console.WriteLine("BadUserAccessDenied: Transfer subscriptions failed to complete!");

                        await DisconnectAndDispose().ConfigureAwait(false);
                        await Initialize().ConfigureAwait(false);
                    }
                }
                else
                {
                    Console.WriteLine("TransferSubscriptionClient.TransferSubscriptionsWithSecurity: Transfer subscription did not start. Invalid session!");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TransferSubscriptionsWithSecurity", ex);
            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Ensure that the source session (from which the subscription is being transferred) is valid and active
        /// </summary>
        /// <returns></returns>
        private bool ValidateSourceSession()
        {
            if (m_session == null || m_session?.Id == null)
            {
                Console.WriteLine("TransferSubscriptionClient.ValidateSourceSession: Invalid session.");
                return false;
            }

            // Switch active session to the Active state
            if (m_session?.CurrentState == State.Connected)
            {
                m_session.Disconnect(false);
                m_session.Connect(false, true);
            }

            if (m_session?.CurrentState != State.Active)
            {
                Console.WriteLine("TransferSubscriptionClient.ValidateSourceSession: Session not active.");
                return false;
            }

            if (m_miCurrentTime == null || m_miInt64 == null || m_miMotorTemperature == null)
            {
                Console.WriteLine("TransferSubscription: Monitored items are not created!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a new session with the specified parameters.
        /// </summary>
        /// <param name="sessionName"></param>
        /// <param name="securityMode"></param>
        /// <param name="securityPolicy"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private ClientSession CreateSession(string sessionName, MessageSecurityMode securityMode, SecurityPolicy securityPolicy,
            UserIdentity userId)
        {
            try
            {
                Console.WriteLine("\r\nCreating the session {0} (SecurityMode = {1}, SecurityPolicy = {2}, \r\n\t\t\t\t\t\tUserIdentity = {3})...",
                    sessionName, securityMode, securityPolicy, userId.GetIdentityToken());

                // Create the Session object.
                ClientSession session = m_application.CreateSession(Program.ServerUrl, securityMode, securityPolicy, MessageEncoding.Binary, userId);
                if (session != null)
                {
                    session.DeleteOnDisconnect = false;
                }

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
        /// Delete a specified session
        /// </summary>
        /// <param name="session"></param>
        private void DeleteSession(ref ClientSession session)
        {
            // release session
            if (session != null)
            {
                string sessionName = session.SessionName;

                session.DisconnectAsync(true).ConfigureAwait(false);
                session.Dispose();
                session = null;

                //m_isDisposed = true;

                Console.WriteLine("Session '{0}' is disconnected and deleted.", sessionName);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and a certificate user identity.
        /// </summary>
        /// <param name="sessionName"></param>
        /// <param name="password"></param>
        /// <param name="certificatePath"></param>
        /// <returns></returns>
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
                X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(certificateFilePath,
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
        /// Ensure that both the source session (from which the subscription is being transferred)
        /// and the target session (to which the subscription is being transferred) are valid and active.        
        /// To be checked before Transfer Subscriptions
        /// </summary>
        /// <returns>True if sessions are valid in order to be used for Transfer Subscriptions</returns>
        /// <exception cref="ServiceResultException"></exception>
        private bool ValidateSessions()
        {
            if (m_session == null || m_transferSession == null || m_session?.Id == null || m_transferSession?.Id == null)
            {
                throw new ServiceResultException(StatusCodes.BadSessionIdInvalid, "TransferSubscriptionClient.ValidateSessions: Transfer subscription attempt on invalid session.");
            }

            if (m_session.CurrentState != State.Active && m_transferSession.CurrentState != State.Active)
            {
                throw new ServiceResultException(StatusCodes.BadSessionNotActivated, "TransferSubscriptionClient.ValidateSessions: Session not active.");
            }

            // Check if both sessions have the same security policies and user tokens
            if (m_session.SecurityPolicy != m_transferSession.SecurityPolicy)
            {
                throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "TransferSubscriptionClient.ValidateSessions: Sessions have not the same security policies.");
            }

            // Verify that the session from which the subscription is being transferred has been closed
            if (m_isDisposed)
            {
                throw new ServiceResultException(StatusCodes.BadSessionClosed, "TransferSubscriptionClient.ValidateSessions: Transfer subscription attempt on session disposed.");
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
                throw new ServiceResultException("TransferSubscriptionClient.ValidateSubscriptions: Invalid subscription.");
            }

            // Ensure that the subscription ID being transferred exists and is valid.
            if (m_subscription == null || m_subscription?.Id == null)
            {
                throw new ServiceResultException(StatusCodes.BadNoSubscription, "TransferSubscriptionClient.ValidateSubscriptions: Invalid subscription.");
            }

            // Confirm that the subscription is in an active state.
            if (m_subscription.CurrentState != State.Active)
            {
                throw new ServiceResultException(StatusCodes.BadNoSubscription, "TransferSubscriptionClient.ValidateSubscriptions: Inactive or deleted subscriptions cannot be transferred.");
            }

            // Validate that the subscription being transferred is owned by the current session.
            if (m_subscription.Session.Id != m_session.Id)
            {
                throw new ServiceResultException("TransferSubscriptionClient.ValidateSubscriptions: Current subscriptions are not owned by the current session.");
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
                throw new ServiceResultException("TransferSubscriptionClient.ValidateMonitoredItems: Monitored items not available for transfer.");
            }

            // Validate that the monitored items are properly linked to the subscription.
            if (m_miInt64.Subscription.Session.Id != m_session.Id ||
                m_miCurrentTime.Subscription.Session.Id != m_session.Id ||
                m_miMotorTemperature.Subscription.Session.Id != m_session.Id)
            {
                throw new ServiceResultException("TransferSubscriptionClient.ValidateMonitoredItems: Monitored items are not properly linked to the subscription.");
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
            if (m_subscription.Session.Id != m_transferSession.Id)
            {
                throw new ServiceResultException("TransferSubscriptionClient.ValidateSuccessfulTransfer: Current subscriptions are not owned by the current session.");
            }

            // Validate that the monitored items are properly linked to the subscription of the transferred session.
            if (m_miCurrentTime.Subscription.Session.Id != m_transferSession.Id)
            {
                throw new ServiceResultException($"TransferSubscriptionClient.ValidateSuccessfulTransfer: Monitored item: '{m_miCurrentTime.DisplayName}' is not properly linked to the transferred subscription.");
            }
            if (m_miInt64.Subscription.Session.Id != m_transferSession.Id)
            {
                throw new ServiceResultException($"TransferSubscriptionClient.ValidateSuccessfulTransfer: Monitored item: '{m_miInt64.DisplayName}' is not properly linked to the transferred subscription.");
            }
            if (m_miMotorTemperature.Subscription.Session.Id != m_transferSession.Id)
            {
                throw new ServiceResultException($"TransferSubscriptionClient.ValidateSuccessfulTransfer: Monitored item: '{m_miMotorTemperature.DisplayName}' is not properly linked to the transferred subscription.");
            }

            int kMonitoredItems = m_transferSession.Subscriptions.Sum(subscription => subscription.MonitoredItems.Count());

            // Validate partial monitored items transfer failures
            if (m_subscription.MonitoredItems.Count != kMonitoredItems)
            {
                throw new ServiceResultException("TransferSubscriptionClient.ValidateSuccessfulTransfer: Some monitored items failed to transfer.");
            }

            return true;
        }

        /// <summary>
        /// Delete the subscriptions file and its containing directory.
        /// </summary>
        /// <param name="transferSubscriptionsDirectory"></param>
        /// <param name="subscriptionsFilePath"></param>
        private void CleanupSavedSubscriptions(string transferSubscriptionsDirectory, string subscriptionsFilePath)
        {
            try
            {
                if (File.Exists(subscriptionsFilePath))
                {
                    File.Delete(subscriptionsFilePath);
                    Directory.Delete(transferSubscriptionsDirectory, true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        ///// <summary>
        ///// Remove current session, replace it with transfer session instance and connect all 
        ///// </summary>
        ///// <returns></returns>
        private void ReplaceWithTransferSession()
        {
            if (m_transferSession != null && m_subscription != null)
            {
                DeleteSession(ref m_session);

                m_session = m_transferSession;
                m_session.ConnectAsync(false, true).ConfigureAwait(false);

                ClientSubscription subscription = m_session.Subscriptions.FirstOrDefault();
                if (subscription != null)
                {
                    if (subscription.Id == m_subscription.Id)
                    {
                        m_subscription = subscription;
                        m_subscription.ConnectAsync(false, true).ConfigureAwait(false);

                        foreach (ClientMonitoredItem clientMonitoredItem in m_subscription.MonitoredItems)
                        {
                            clientMonitoredItem.ConnectAsync(false, true).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Transfer subscriptions does not match {0} - {1}", subscription.Id, m_subscription.Id);
                    }
                }
                else
                {
                    Console.WriteLine("No transfer subscription found in the transfer session!");
                }
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
                Console.WriteLine("    SessionId : {0} ", dataChangeNotification.MonitoredItem.Subscription?.Session?.Id);
                Console.WriteLine("    SubscriptionId : {0} ", dataChangeNotification.MonitoredItem.Subscription?.Id);
                Console.WriteLine("    MonitorId : {0} ", dataChangeNotification.ClientHandle);
                Console.WriteLine("    StatusCode : {0} ", dataChangeNotification.Value.StatusCode);
                Console.WriteLine("    ServerTimestamp : {0:hh:mm:ss.fff tt}", dataChangeNotification.Value.ServerTimestamp.ToLocalTime());
                Console.WriteLine("    SourceTimestamp : {0:hh:mm:ss.fff tt}", dataChangeNotification.Value.SourceTimestamp.ToLocalTime());
            }
        }

        /// <summary>
        /// Hook the notification event handler to the monitored items
        /// </summary>
        /// <param name="transferSubscriptions"></param>
        private void HandleNotifications(ICollection<ClientSubscription> transferSubscriptions)
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
