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
using Opc.Ua.Client;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.DurableSubscriptions.Models;
using Softing.Opc.Ua.Client.DurableSubscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SampleClient.DurableSubscriptions.Models;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that contains sample code for Durable Subscriptions functionality
    /// </summary>
    class DurableSubscriptionsClient
    {
        #region Private Fields

        private const string SessionName = "DurableSubscriptions Session";
        private const string SubscriptionName1 = "Durable Subscription";
        private const string SubscriptionName2 = "Subscription";

        //"Server\\ServerStatus\\CurrentTime";
        private readonly NodeId m_miCurrentTimeNodeId = VariableIds.Server_ServerStatus_CurrentTime;

        // "CTT\\Scalar\\Simulation\\Int64";
        private readonly NodeId m_miInt64NodeId = new NodeId("ns=7;s=CTT_Scalar_Simulation_Int64");

        //"DataAccess\\Refrigerator\\MotorTemperature";
        private readonly NodeId m_miMotorTemperatureNodeId = new NodeId("ns=3;i=24");

        private readonly UaApplication m_application;
        private bool m_isDisposed = false;

        private ClientSession m_session;
        private ClientSession m_transferSession;
        private ClientSession m_currentSession;

        private ClientSubscription m_subscription1;
        private ClientSubscription m_subscription2;

        private ClientMonitoredItem m_miCurrentTime_1;
        private ClientMonitoredItem m_miInt64_1;
        private ClientMonitoredItem m_eventMonitoredItem;
        private ClientMonitoredItem m_miMotorTemperature_1;

        private ClientMonitoredItem m_miCurrentTime_2;
        private ClientMonitoredItem m_miInt64_2;
        private ClientMonitoredItem m_miMotorTemperature_2;

        const int publishingInterval = 1000;
        const int samplingInterval = 1000;
        const uint keepAliveCount = 100u;
        const uint lifetimeCount = 10000u;
        const uint queueSize = 1000u;
        const uint lifetimeInHours = 1u;

        private List<ClientSubscription> subscriptionsToPersist;

        // refers a Dictionary<subscriptionId, revisedLifetimeInHours>
        private Dictionary<uint, uint> m_revisedLifetimeInHours = new Dictionary<uint, uint>();

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of DurableSubscriptionsClient
        /// </summary>
        /// <param name="application"></param>
        public DurableSubscriptionsClient(UaApplication application)
        {
            m_application = application;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize session and subscription
        /// </summary>
        public async Task Initialize(bool withEvents = true)
        {
            try
            {
                // create the session object with user identity
                m_session = CreateSession(SessionName, MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                //connect session
                await m_session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine("Session is connected.");
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
                m_subscription1 = null;
                m_subscription2 = null;
            }
        }

        internal void CreateDurableSubscriptions()
        {
            //create the subscriptions
            m_subscription1 = CreateSubscription(SubscriptionName1, setDurable: true, republishAfterTransfer: true);

            // Give some time to allow for the true browse of items
            Thread.Sleep(500);

            m_subscription2 = CreateSubscription(SubscriptionName2, setDurable: false, republishAfterTransfer: true);

            // Give some time to allow for the true browse of items
            Thread.Sleep(500);
        }

        /// <summary>
        /// Create subscription and set to durable mode
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="setDurable"></param>
        /// <param name="subscriptionName"></param>
        /// <param name="republishAfterTransfer"></param>
        private ClientSubscription CreateSubscription(string subscriptionName, bool setDurable, bool republishAfterTransfer, bool connect = true)
        {
            m_currentSession = GetCurrentSession();

            ClientSubscription subscription = new ClientSubscription(m_currentSession, subscriptionName);

            subscription.PublishingInterval = publishingInterval;
            subscription.KeepAliveCount = keepAliveCount;
            subscription.LifeTimeCount = lifetimeCount;

            subscription.RepublishAfterTransfer = republishAfterTransfer;

            if (setDurable)
            {
                bool isDurable = subscription.SetDurable(lifetimeInHours, out uint revisedLifetimeInHours);

                // OR async method:

                //bool durableResult = await subscription.SetDurableAsync(lifetimeInHours, new CancellationToken(default)).ConfigureAwait(false);

                if (isDurable)
                {
                    Console.WriteLine($"{subscriptionName} with SubscriptionId {subscription.Id} is set to durable mode.");

                    // save the revisedLifetimeInHours after the method SetDurable is called
                    if (!m_revisedLifetimeInHours.ContainsKey(subscription.Id))
                    {
                        m_revisedLifetimeInHours.Add(subscription.Id, subscription.RevisedLifetimeInHours);
                    }

                    subscriptionsToPersist = new List<ClientSubscription>();
                }
            }

            Console.WriteLine($"{subscriptionName} created. SubscriptionId: {subscription.Id} - SessionId: {m_currentSession.Id}.");

            if (connect)
            {
                subscription.Connect(false, true);
                Console.WriteLine("Subscription is connected.");
            }

            return subscription;
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

        /// <summary>
        /// Creates a monitoredItem. The monitored item is activated in the constructor if the subscription is active as well.
        /// </summary>
        internal void CreateMonitoredItems(bool withEvents = false)
        {
            m_currentSession = GetCurrentSession();

            if (m_currentSession == null)
            {
                Console.WriteLine($"CreateMonitoredItem: The session {m_currentSession.SessionName} is not initialized!");
                return;
            }

            if (m_subscription1 == null || m_subscription2 == null)
            {
                Console.WriteLine("CreateMonitoredItem: Subscription not created. Create a subscription first!");
                return;
            }

            if (m_subscription1.CurrentState == State.Disconnected &&
                m_subscription2.CurrentState == State.Disconnected && !m_isDisposed)
            {
                Console.WriteLine($"CreateMonitoredItem: The session {m_currentSession.SessionName} is not connected!");
                return;
            }

            try
            {
                CreateMultipleMonitoredItems(withEvents);

                // Note: the user is responsible to provide a secure path for durable subscriptions metadata
                // no callback logic used
                string durableSubscriptionsPath = m_application.ClientToolkitConfiguration.DurableSubscriptionsPath;
                string durableSubscriptionMetadataFileName = m_application.ClientToolkitConfiguration.DurableSubscriptionMetadataFileName;
                string durableSubscriptionsRevisedLifetimeFileName = m_application.ClientToolkitConfiguration.DurableSubscriptionsRevisedLifetimeFileName;
                string storagePath = Path.Combine(Environment.CurrentDirectory, durableSubscriptionsPath);

                bool savedSubscriptions = ((IPersister)m_currentSession).Persist(
                    Path.Combine(storagePath, durableSubscriptionMetadataFileName),
                    subscriptionsToPersist,
                    new[] { typeof(List<ClientSubscription>), typeof(EventFilterEx) });

                // OR:

                //bool savedSubscriptions = PersistModel(durableSubscriptionsPath, durableSubscriptionMetadataFileName);

                StoreRevisedLifetimeInHours(storagePath, durableSubscriptionsRevisedLifetimeFileName);
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateMonitoredItem", ex);
            }
        }

        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public async Task Disconnect()
        {
            try
            {
                m_currentSession = GetCurrentSession();

                //disconnect subscriptions
                if (m_subscription1 != null)
                {
                    await m_subscription1.DisconnectAsync(true).ConfigureAwait(false);
                    m_subscription1.Delete();
                    m_subscription1 = null;
                    Console.WriteLine($"{SubscriptionName1} is deleted.");
                }
                if (m_subscription2 != null)
                {
                    await m_subscription2.DisconnectAsync(true).ConfigureAwait(false);
                    m_subscription2.Delete();
                    m_subscription2 = null;
                    Console.WriteLine($"{SubscriptionName2} is deleted.");
                }
                if (m_currentSession != null)
                {
                    Console.WriteLine($"Session {m_currentSession.SessionName} is disconnected.");
                    await m_currentSession.DisconnectAsync(true).ConfigureAwait(false);
                    m_currentSession.Dispose();
                    m_currentSession = null;
                    m_isDisposed = true;
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DisconnectSession", ex);
            }
        }

        /// <summary>
        /// Deletes the current MonitoredItem.
        /// </summary>
        internal void DeleteMonitoredItems()
        {
            m_currentSession = GetCurrentSession();

            if (m_currentSession.CurrentState == State.Disconnected && !m_isDisposed)
            {
                Console.WriteLine($"DeleteMonitoredItems: The session {m_currentSession.SessionName} is not connected!");
                return;
            }

            if (m_isDisposed)
            {
                // delete the subscription and remove it from its parent session after transfer subscriptions successfully performed.
                foreach (ClientSubscription subscription in m_currentSession.Subscriptions.ToList())
                {
                    foreach (ClientMonitoredItem monitoredItem in subscription.MonitoredItems.ToList())
                    {
                        UnsubscribeDataChangeNotifications(monitoredItem);
                    }
                    subscription.Delete();
                }
                return;
            }

            if ((m_miCurrentTime_1 == null || m_miInt64_1 == null || m_miMotorTemperature_1 == null) & m_eventMonitoredItem == null ||
                    (m_miCurrentTime_2 == null || m_miInt64_2 == null || m_miMotorTemperature_2 == null) &
                    m_eventMonitoredItem == null)
            {
                Console.WriteLine("Monitored items are not created.");
                return;
            }
            try
            {
                if (m_miCurrentTime_1.Subscription != null && m_miInt64_1.Subscription != null &&
                    m_miMotorTemperature_1.Subscription != null && m_miCurrentTime_2.Subscription != null &&
                    m_miInt64_2.Subscription != null && m_miMotorTemperature_2.Subscription != null)
                {
                    UnsubscribeDataChangeNotifications(m_miCurrentTime_1);
                    UnsubscribeDataChangeNotifications(m_miInt64_1);
                    UnsubscribeDataChangeNotifications(m_miMotorTemperature_1);
                    UnsubscribeDataChangeNotifications(m_miCurrentTime_2);
                    UnsubscribeDataChangeNotifications(m_miInt64_2);
                    UnsubscribeDataChangeNotifications(m_miMotorTemperature_2);
                }
                else
                {
                    Console.WriteLine("There was no Monitored Item to be deleted.");
                }

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
                Program.PrintException("DeleteMonitoredItem", ex);
            }
        }

        /// <summary>
        /// Persist detached durable subscriptions on new session
        /// On reconnect the client can use the transfer subscription service to gain access to the missed values.
        /// </summary>
        internal void TransferDurableSubscriptionsOnCurrentSession()
        {
            if (m_session == null)
            {
                Console.WriteLine("TransferDurableSubscriptionsOnCurrentSession: The session is not initialized!");
                return;
            }
            try
            {
                string durableSubscriptionsPath = m_application.ClientToolkitConfiguration.DurableSubscriptionsPath;
                string durableSubscriptionMetadataFileName = m_application.ClientToolkitConfiguration.DurableSubscriptionMetadataFileName;
                string durableSubscriptionsRevisedLifetimeFileName = m_application.ClientToolkitConfiguration.DurableSubscriptionsRevisedLifetimeFileName;

                string durableSubscriptionsDirectory = Path.Combine(Environment.CurrentDirectory, durableSubscriptionsPath);
                string metadataPath = Path.Combine(durableSubscriptionsDirectory, durableSubscriptionMetadataFileName);
                string revisionLifetimeFilePath = Path.Combine(durableSubscriptionsDirectory, durableSubscriptionsRevisedLifetimeFileName);

                if (File.Exists(metadataPath))
                {
                    m_session.DeleteSubscriptionsOnClose = false;
                    m_session.TransferSubscriptionsOnReconnect = true;

                    // Note: It is the user's responsibility to ensure no values are artificially injected into the files and to
                    // verify data integrity during subscription restoration.
                    List<ClientSubscription> restoredSubscriptions = ((ILoader)m_session).Load<ClientSubscription>(
                        metadataPath, true, new[] { typeof(List<ClientSubscription>), typeof(EventFilterEx) }).ToList();

                    // OR: 

                    //List<ClientSubscription> restoredSubscriptions = LoadModel(durableSubscriptionsDirectory, durableSubscriptionMetadataFilePath);

                    if (!restoredSubscriptions.Any())
                    {
                        Console.WriteLine("Subscriptions not loaded!");
                    }
                    else
                    {
                        // Note: Do not forget to properly clean up expired subscriptions.
                        bool expiredSubscription = CleanupIfExpiredSubscriptions(restoredSubscriptions, durableSubscriptionsDirectory, revisionLifetimeFilePath);

                        if (!expiredSubscription)
                        {
                            List<ClientSubscription> durableSubscriptions = GetDurableSubscriptions(restoredSubscriptions);

                            foreach (var subscription in durableSubscriptions)
                            {
                                Console.WriteLine($"[BEFORE TRANSFER]: SubscriptionID:{subscription.TransferId}!");
                            }

                            HandleNotifications(restoredSubscriptions);

                            Debug.WriteLine($"Transferring subscriptions...");

                            bool isTransferred = m_session.TransferSubscriptions(restoredSubscriptions, true);

                            if (isTransferred)
                            {
                                // Change the publishing enabled state for the subscriptions
                                foreach (var subscription in restoredSubscriptions)
                                {
                                    subscription.SetPublishingMode(true);
                                }

                                Console.WriteLine("Transfer subscriptions completed successfully!");

                                Console.WriteLine($"Subscriptions transferred to current SessionID:{m_session.Id}!");
                                Utils.LogTrace($"Subscriptions transferred to current SessionID:{m_session.Id}!");

                                List<ClientSubscription> transferredDurableSubscriptions = GetDurableSubscriptions(m_session.Subscriptions.ToList());

                                foreach (var subscription in transferredDurableSubscriptions)
                                {
                                    Console.WriteLine($"[AFTER TRANSFER]: SubscriptionID:{subscription.Id}!");
                                }

                                // do not delete folder if you want to keep transferring the durable subscriptions
                                CleanupDurableStorage(durableSubscriptionsDirectory);
                                m_isDisposed = true;
                            }
                            else
                            {
                                Console.WriteLine("Transfer durable subscriptions failed!");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Subscription is expired on server!");
                            try
                            {
                                if (File.Exists(metadataPath))
                                {
                                    File.Delete(metadataPath);
                                    Directory.Delete(metadataPath, true);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{durableSubscriptionMetadataFileName} file is missing!");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TransferDurableSubscriptionsOnCurrentSession", ex);
            }
        }

        /// <summary>
        /// Persist detached durable subscriptions on new session
        /// On reconnect the client can use the transfer subscription service to gain access to the missed values.
        /// </summary>
        internal void TransferDurableSubscriptionsOnNewSession()
        {
            if (m_session == null)
            {
                Console.WriteLine("TransferDurableSubscriptionsOnNewSession: The session is not initialized!");
                return;
            }
            try
            {
                string durableSubscriptionsPath = m_application.ClientToolkitConfiguration.DurableSubscriptionsPath;
                string durableSubscriptionMetadataFilePath = m_application.ClientToolkitConfiguration.DurableSubscriptionMetadataFileName;
                string durableSubscriptionsRevisedLifetimeFilePath = m_application.ClientToolkitConfiguration.DurableSubscriptionsRevisedLifetimeFileName;

                string durableSubscriptionsDirectory = Path.Combine(Environment.CurrentDirectory, durableSubscriptionsPath);
                string metadataPath = Path.Combine(durableSubscriptionsDirectory, durableSubscriptionMetadataFilePath);
                string revisionLifetimePath = Path.Combine(durableSubscriptionsDirectory, durableSubscriptionsRevisedLifetimeFilePath);

                if (File.Exists(metadataPath))
                {
                    m_session.DeleteSubscriptionsOnClose = false;
                    m_session.TransferSubscriptionsOnReconnect = true;

                    m_transferSession = CreateSession("Transferred Session", MessageSecurityMode.None, SecurityPolicy.None, new UserIdentity("usr", "pwd"));

                    m_transferSession.Connect(false, true);
                    Console.WriteLine($"{m_transferSession.SessionName} is connected.");

                    m_transferSession.TransferSubscriptionsOnReconnect = true;

                    // Note: It is the user's responsibility to ensure no values are artificially injected into the files and to
                    // verify data integrity during subscription restoration.
                    List<ClientSubscription> restoredSubscriptions = ((ILoader)m_transferSession).Load<ClientSubscription>(metadataPath, true,
                        new[] { typeof(List<ClientSubscription>), typeof(EventFilterEx) }).ToList();

                    // OR: 

                    //List<ClientSubscription> restoredSubscriptions = LoadModel(durableStoragePath, durableSubscriptionMetadataPath);

                    if (!restoredSubscriptions.Any())
                    {
                        Console.WriteLine("Durable Subscriptions not loaded!");
                    }
                    else
                    {
                        // Note: Do not forget to properly clean up expired subscriptions.
                        bool expiredSubscription = CleanupIfExpiredSubscriptions(restoredSubscriptions, durableSubscriptionsPath, revisionLifetimePath);

                        if (!expiredSubscription)
                        {
                            List<ClientSubscription> durableSubscriptions = GetDurableSubscriptions(restoredSubscriptions);

                            foreach (var subscription in durableSubscriptions)
                            {
                                Console.WriteLine($"[BEFORE TRANSFER]: SubscriptionID:{subscription.TransferId}!");
                            }

                            HandleNotifications(restoredSubscriptions);

                            Debug.WriteLine($"Transferring subscriptions...");

                            bool isTransferred = m_transferSession.TransferSubscriptions(restoredSubscriptions, true);

                            if (isTransferred)
                            {
                                // Change the publishing enabled state for the subscriptions
                                foreach (var subscription in restoredSubscriptions)
                                {
                                    subscription.SetPublishingMode(true);
                                }
                                Console.WriteLine("Transfer subscriptions completed successfully!");

                                Console.WriteLine($"Subscriptions transferred to SessionID:{m_transferSession.Id}!");
                                Utils.LogTrace($"Subscriptions transferred to SessionID:{m_transferSession.Id}!");

                                List<ClientSubscription> transferredDurableSubscriptions = GetDurableSubscriptions(m_transferSession.Subscriptions.ToList());

                                foreach (var subscription in transferredDurableSubscriptions)
                                {
                                    Console.WriteLine($"[AFTER TRANSFER]: SubscriptionID:{subscription.Id}!");
                                }

                                CleanupDurableStorage(durableSubscriptionsPath);

                                m_session.Disconnect(false);
                                m_session.Dispose();
                                m_isDisposed = true;
                            }
                            else
                            {
                                Console.WriteLine("Transfer durable subscriptions failed!");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Subscription is expired on server!");

                            try
                            {
                                if (File.Exists(metadataPath))
                                {
                                    File.Delete(metadataPath);
                                    Directory.Delete(metadataPath, true);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{durableSubscriptionMetadataFilePath} file is missing!");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TransferDurableSubscriptionsOnNewSession", ex);
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Cleanup durable  storage
        /// </summary>
        /// <param name="storagePath"></param>
        private void CleanupDurableStorage(string storagePath)
        {
            if (Directory.Exists(storagePath))
                try
                {
                    Directory.Delete(storagePath, true);
                }
                catch (Exception ex)
                {
                    Utils.LogWarning(ex, "Failed to cleanup files for durable storage.");
                }
        }

        /// <summary>
        /// Hook notifications for log output
        /// </summary>
        /// <param name="transferSubscriptions"></param>
        private void HandleNotifications(ICollection<ClientSubscription> transferSubscriptions)
        {
            foreach (ClientSubscription clientSubscription in transferSubscriptions)
            {
                foreach (ClientMonitoredItem monitoredItem in clientSubscription.MonitoredItems)
                {
                    monitoredItem.DataChangesReceived += MonitoredItem_DataChangesReceived;
                    monitoredItem.EventsReceived += EventMonitoredItem_EventsReceived;
                }
            }
        }

        /// <summary>
        /// Handles the Notification event of the MonitoredItem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataChangesNotificationEventArgs"/> instance containing the event data.</param>
        private void MonitoredItem_DataChangesReceived(object sender, DataChangesNotificationEventArgs e)
        {
            foreach (var dataChangeNotification in e.DataChangeNotifications)
            {
                Console.WriteLine(" {0} Received data value change for '{1}':", dataChangeNotification.SequenceNo, dataChangeNotification.MonitoredItem.DisplayName);
                Console.WriteLine("    Value : {0} ", dataChangeNotification.Value);
                Console.WriteLine("    SessionId : {0} ", dataChangeNotification.MonitoredItem.Subscription.Session?.Id);
                Console.WriteLine("    SubscriptionId : {0} ", dataChangeNotification.MonitoredItem.Subscription.Id);
                Console.WriteLine("    SubscriptionName : {0} ", dataChangeNotification.MonitoredItem.Subscription.DisplayName);
                Console.WriteLine("    StatusCode : {0} ", dataChangeNotification.Value.StatusCode);
                Console.WriteLine("    ServerTimestamp : {0:hh:mm:ss.fff tt}", dataChangeNotification.Value.ServerTimestamp.ToLocalTime());
                Console.WriteLine("    SourceTimestamp : {0:hh:mm:ss.fff tt}", dataChangeNotification.Value.SourceTimestamp.ToLocalTime());
            }
        }

        /// <summary>
        /// Handles the Notification event of the EventMonitoredItem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventsNotificationEventArgs"/> instance containing the event data.</param>
        private void EventMonitoredItem_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
            foreach (var eventNotification in e.EventNotifications)
            {
                Console.WriteLine("Event notification received for {0}.\n", eventNotification.MonitoredItem.DisplayName);

                StringBuilder displayNotification = new StringBuilder();

                var filter = eventNotification.MonitoredItem.Filter as EventFilter;
                if (filter == null)
                {
                    Console.WriteLine("EventFilter is null or not of expected type.");
                    continue;
                }

                for (int i = 0; i < filter.SelectClauses.Count && i < eventNotification.EventFields.Count; i++)
                {
                    var operand = filter.SelectClauses[i];
                    var value = eventNotification.EventFields[i];

                    displayNotification.AppendFormat("{0}:{1}:{2}\n",
                        operand.BrowsePath.Count > 0 ? operand.BrowsePath[0].NamespaceIndex : 0,
                        operand.BrowsePath.Count > 0 ? operand.BrowsePath[0].Name : "(No Name)",
                        value != string.Empty ? value.ToString() : "(null)");
                }

                Console.WriteLine(displayNotification.ToString());
            }
        }

        /// <summary>
        /// Unsubscribe receiving data change notifications
        /// </summary>
        /// <param name="clientMonitoredItem"></param>
        private void UnsubscribeDataChangeNotifications(ClientMonitoredItem clientMonitoredItem)
        {
            if (clientMonitoredItem != null)
            {
                clientMonitoredItem.DataChangesReceived -= MonitoredItem_DataChangesReceived;
                Console.WriteLine("Monitored item '{0}' unsubscribed from receiving data change notifications.", clientMonitoredItem.DisplayName);
                clientMonitoredItem.Delete();
                Console.WriteLine("Monitored item '{0}' deleted.", clientMonitoredItem.DisplayName);
                clientMonitoredItem = null;
            }
        }

        /// <summary>
        /// Save subscriptions list for persistence
        /// </summary>
        /// <param name="clientSubscription"></param>
        private void CreateSubscriptionList(ClientSubscription clientSubscription)
        {
            if (!subscriptionsToPersist.Any(dsb => dsb.Id == clientSubscription.Id))
            {
                if (clientSubscription.IsDurable)
                {
                    subscriptionsToPersist.Add(clientSubscription);
                }
            }
        }

        private void CreateMultipleMonitoredItems(bool withEvents)
        {
            // Subscription1
            // Create #1 monitored item for server CurrentTime
            m_miCurrentTime_1 = CreateSpecificMonitoredItem(m_subscription1, m_miCurrentTime_1, m_miCurrentTimeNodeId, "Monitored Item Server CurrentTime", samplingInterval, queueSize);

            // Create #2 monitored item - for Int64 variable
            m_miInt64_1 = CreateSpecificMonitoredItem(m_subscription1, m_miInt64_1, m_miInt64NodeId, "Monitored Item Int64", samplingInterval, queueSize);

            // Create #3 monitored item - for MotorTemperature variable
            m_miMotorTemperature_1 = CreateSpecificMonitoredItem(m_subscription1, m_miMotorTemperature_1, m_miMotorTemperatureNodeId, "Monitored Item Custom MotorTemperature", samplingInterval, queueSize);

            // Subscription2
            // Create #1 monitored item for server CurrentTime
            m_miCurrentTime_2 = CreateSpecificMonitoredItem(m_subscription2, m_miCurrentTime_1, m_miCurrentTimeNodeId, "Monitored Item Server CurrentTime", samplingInterval, queueSize);

            // Create #2 monitored item - for Int64 variable
            m_miInt64_2 = CreateSpecificMonitoredItem(m_subscription2, m_miInt64_1, m_miInt64NodeId, "Monitored Item Int64", samplingInterval, queueSize);

            // Create #3 monitored item - for MotorTemperature variable
            m_miMotorTemperature_2 = CreateSpecificMonitoredItem(m_subscription2, m_miMotorTemperature_1, m_miMotorTemperatureNodeId, "Monitored Item Custom MotorTemperature", samplingInterval, queueSize);

            if (withEvents)
            {
                //ObjectIds.Server BrowsePath: Root\Objects\Server
                m_eventMonitoredItem = new ClientMonitoredItem(m_subscription1, ObjectIds.Server, "Durable Event Monitored Item", null, true);
                m_eventMonitoredItem.QueueSize = queueSize;
                m_eventMonitoredItem.EventsReceived += EventMonitoredItem_EventsReceived;
                Console.WriteLine("Event Monitored Item created for NodeId ({0}) with state ({1}).", m_eventMonitoredItem.NodeId, m_eventMonitoredItem.CurrentState);

                Thread.Sleep(1000);
            }

            CreateSubscriptionList(m_subscription1);
            CreateSubscriptionList(m_subscription2);

            m_currentSession.DeleteSubscriptionsOnClose = false;

            foreach (ClientSubscription subscriptionToPersist in subscriptionsToPersist)
            {
                subscriptionToPersist.RepublishAfterTransfer = true;
            }

            m_subscription1.ConnectAsync(true, true).ConfigureAwait(false);
            Console.WriteLine("Subscription is connected.");
        }

        /// <summary>
        /// Create monitored item for a specific subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="clientMonitoredItem"></param>
        /// <param name="monitoredItemNodeId"></param>
        /// <param name="samplingInterval"></param>
        private ClientMonitoredItem CreateSpecificMonitoredItem(
            ClientSubscription subscription,
            ClientMonitoredItem clientMonitoredItem,
            NodeId monitoredItemNodeId,
            string displayName,
            int samplingInterval,
            uint queueSize)
        {
            m_currentSession.TransferSubscriptionsOnReconnect = true;
            m_currentSession.DeleteSubscriptionsOnClose = false;

            clientMonitoredItem = new ClientMonitoredItem(subscription, monitoredItemNodeId, $"Monitored Item Server {displayName}");
            clientMonitoredItem.DataChangesReceived += MonitoredItem_DataChangesReceived;
            clientMonitoredItem.SamplingInterval = samplingInterval;
            clientMonitoredItem.QueueSize = queueSize;

            if (clientMonitoredItem.CurrentState == State.Active)
            {
                Console.WriteLine("Monitored item '{0}' created. Data value changes are shown:", clientMonitoredItem.DisplayName);
            }
            else
            {
                Console.WriteLine("Monitored item '{0}' created with state {1}", clientMonitoredItem.DisplayName, clientMonitoredItem.CurrentState);
            }

            m_subscription1?.ApplyMonitoredItemsChanges();
            m_subscription2?.ApplyMonitoredItemsChanges();
            Console.WriteLine("Subscription was updated with new monitor items added.");

            return clientMonitoredItem;
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
        /// Store in file subscriptionId and revisedLifetimeInHours of the durable subscription
        /// </summary>
        private void StoreRevisedLifetimeInHours(string storagePath, string durableSubscriptionsRevisedLifetime)
        {
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            string subscriptionRevisedLifetimePath = Path.Combine(storagePath, durableSubscriptionsRevisedLifetime);

            if (!File.Exists(subscriptionRevisedLifetimePath))
                File.Create(subscriptionRevisedLifetimePath).Dispose();

            using (StreamWriter streamWriter = new StreamWriter(subscriptionRevisedLifetimePath))
            {
                foreach (var subscription in subscriptionsToPersist)
                {
                    uint hours = m_revisedLifetimeInHours.ContainsKey(subscription.Id)
                        ? m_revisedLifetimeInHours[subscription.Id]
                        : 0;
                    streamWriter.WriteLine($"{subscription.Id}:{hours}");
                }
            }
        }

        /// <summary>
        /// Restore revisedLifetimeInHours from file and cleanup if subscription is expired based on revisedLifetimeInHours value
        /// </summary>
        /// <param name="restoredSubscriptions"></param>
        /// <returns>Flag indicating subscription is expired</returns>
        private bool CleanupIfExpiredSubscriptions(List<ClientSubscription> restoredSubscriptions,
            string durableSubscriptionsDirectory, string revisionLifetimeFilePath)
        {
            var revisedLifetime = new Dictionary<string, int>();

            string subscriptionRevisedLifetimePath = Path.Combine(durableSubscriptionsDirectory, revisionLifetimeFilePath);

            if (!File.Exists(subscriptionRevisedLifetimePath))
                Console.WriteLine($"File {subscriptionRevisedLifetimePath} does not exist!");

            DateTime fileTimestamp = File.GetLastWriteTimeUtc(subscriptionRevisedLifetimePath);

            foreach (var line in File.ReadAllLines(subscriptionRevisedLifetimePath))
            {
                var parts = line.Split(':');
                if (parts.Length == 2)
                {
                    string id = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int hours))
                    {
                        revisedLifetime[id] = hours;
                    }
                }
            }

            foreach (ClientSubscription subscription in restoredSubscriptions.Where(cs => cs.IsDurable))
            {
                int revisedLifetimeInHours = revisedLifetime[subscription.TransferId.ToString()];

                bool expired = IsSubscriptionExpired(fileTimestamp, revisedLifetimeInHours);
                if (expired)
                {
                    Console.WriteLine("Subscription expired on server!");
                    CleanupDurableStorage(durableSubscriptionsDirectory);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if durable subscription is expired based on revisedLifetimeInHours value
        /// </summary>
        /// <param name="fileTimestamp"></param>
        /// <param name="revisedLifetimeInHours"></param>
        /// <returns></returns>
        private static bool IsSubscriptionExpired(DateTime fileTimestamp, int revisedLifetimeInHours)
        {
            DateTime expirationTime = fileTimestamp.AddHours(revisedLifetimeInHours);
            return DateTime.UtcNow > expirationTime;
        }

        /// <summary>
        /// Get durable subscriptions from the restored subscription list based on the name
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <returns>If Subscription displayName contains 'Durable', return True</returns>
        private List<ClientSubscription> GetDurableSubscriptions(List<ClientSubscription> restoredSubscriptions)
        {
            List<ClientSubscription> durableSubscriptions = new List<ClientSubscription>();

            foreach (ClientSubscription subscription in restoredSubscriptions)
            {
                if (subscription.DisplayName.Contains("Durable"))
                {
                    subscription.IsDurable = true;
                    durableSubscriptions.Add(subscription);
                }
            }
            return durableSubscriptions;
        }

        /// <summary>
        /// Performs implementation of persist subscriptions model.
        /// </summary>
        /// <returns>Indicates whether the subscriptions were saved successfully.</returns>
        private bool PersistModel(string durableStoragePath, string durableSubscriptionMetadataPath)
        {
            if (durableStoragePath is null) throw new ArgumentNullException(nameof(durableStoragePath));
            if (durableSubscriptionMetadataPath is null) throw new ArgumentNullException(nameof(durableSubscriptionMetadataPath));

            if (subscriptionsToPersist.Any(subscription => subscription.IsDurable))
            {
                m_currentSession = GetCurrentSession();
                ClientSession.PersistCallback = (data, knownTypes) =>
                {
                    string storagePath = Path.Combine(Environment.CurrentDirectory, durableStoragePath);
                    return m_currentSession.Persist<ClientSubscription>(data, storagePath, durableSubscriptionMetadataPath, knownTypes);
                };

                return m_currentSession.PersistModel(subscriptionsToPersist, new[] { typeof(List<ClientSubscription>), typeof(EventFilterEx) });
            }
            return false;
        }

        private List<ClientSubscription> LoadModel(string durableSubscriptionsDirectory, string durableSubscriptionMetadataPath)
        {
            if (durableSubscriptionsDirectory is null) throw new ArgumentNullException(nameof(durableSubscriptionsDirectory));
            if (durableSubscriptionMetadataPath is null) throw new ArgumentNullException(nameof(durableSubscriptionMetadataPath));

            m_currentSession = GetCurrentSession();
            ClientSession.LoaderCallback = (data, knownTypes) =>
            {
                var callback = m_currentSession.Load<ClientSubscription>(durableSubscriptionsDirectory, durableSubscriptionMetadataPath, knownTypes);
                return (IEnumerable<ClientSubscription>)callback;
            };

            List<ClientSubscription> restoredSubscriptions =
                (List<ClientSubscription>)m_currentSession.LoadModel(default, new[] { typeof(List<ClientSubscription>), typeof(EventFilterEx) });

            return restoredSubscriptions;
        }

        #endregion Private methods

        #region Custom Session Model Persist/Load Implementation

        /// <summary>
        /// Example for Save and Load of a Custom Monitored Item Model
        /// Note: If CustomClientSessionModel or CustomClientSubscriptionModel is needed than they have to be wrapped in their corresponding model hierarchy.
        /// </summary>
        internal void CustomSaveLoadSessionModel(bool withEvents = false)
        {
            m_currentSession = GetCurrentSession();

            //create the subscriptions
            m_subscription1 = CreateSubscription(SubscriptionName1, setDurable: true, republishAfterTransfer: true);
            m_subscription2 = CreateSubscription(SubscriptionName2, setDurable: false, republishAfterTransfer: true);

            // Give some time to allow for the true browse of items
            Thread.Sleep(500);

            if (m_currentSession == null)
            {
                Console.WriteLine($"CreateMonitoredItem: The session {m_currentSession.SessionName} is not initialized!");
                return;
            }

            if (m_subscription1 == null)
            {
                Console.WriteLine("CreateMonitoredItem: Subscription not created. Create a subscription first!");
                return;
            }

            if (m_subscription1.CurrentState == State.Disconnected &&
                m_subscription2.CurrentState == State.Disconnected && !m_isDisposed)
            {
                Console.WriteLine($"CreateMonitoredItem: The session {m_currentSession.SessionName} is not connected!");
                return;
            }

            try
            {
                CreateMultipleMonitoredItems(withEvents: true);

                // Note: the user is responsible to provide a secure path for durable subscriptions metadata
                // no callback logic used
                string durableSubscriptionsPath = Path.Combine(Environment.CurrentDirectory, m_application.ClientToolkitConfiguration.DurableSubscriptionsPath);
                string sessionFilePath = Path.Combine(durableSubscriptionsPath, "CustomSessionModel.txt");

                bool savedSession = PersistCustomSessionModel(durableSubscriptionsPath, sessionFilePath);
                if (savedSession)
                {
                    var restoredSession = LoadCustomSessionModel(durableSubscriptionsPath, sessionFilePath);
                }

                CleanupDurableStorage(durableSubscriptionsPath);
            }
            catch (Exception ex)
            {
                Program.PrintException("CustomSaveLoadSessionModel", ex);
            }
        }

        /// <summary>
        /// Performs implementation of persist custom session model.
        /// </summary>
        /// <returns>List of subscriptions loaded.</returns>
        private bool PersistCustomSessionModel(string durableStoragePath, string customSessionFilePath)
        {
            if (durableStoragePath is null) throw new ArgumentNullException(nameof(durableStoragePath));
            if (customSessionFilePath is null) throw new ArgumentNullException(nameof(customSessionFilePath));

            m_currentSession = GetCurrentSession();

            if (m_currentSession != null)
            {
                ClientSession.PersistCallback = (data, knownTypes) =>
                {
                    string storagePath = Path.Combine(Environment.CurrentDirectory, durableStoragePath);
                    return Persist<ClientSession>(data, storagePath, customSessionFilePath, knownTypes);
                };

                return m_currentSession.PersistModel(m_currentSession,
                    new[] { typeof(CustomClientSessionModel), typeof(SubscriptionCollection), typeof(CustomClientSubscriptionModel),
                            typeof(List<ClientMonitoredItem>), typeof(EventFilterEx), typeof(CustomClientMonitoredItemModel) });
            }
            return false;
        }

        /// <summary>
        /// Performs implementation of load custom session model.
        /// </summary>
        /// <returns>List of subscriptions loaded.</returns>
        private List<CustomClientSessionModel> LoadCustomSessionModel(string durableSubscriptionsDirectory, string durableSubscriptionMetadataPath)
        {
            if (durableSubscriptionsDirectory is null) throw new ArgumentNullException(nameof(durableSubscriptionsDirectory));
            if (durableSubscriptionMetadataPath is null) throw new ArgumentNullException(nameof(durableSubscriptionMetadataPath));
            List<CustomClientSessionModel> customClientSessionModelList = new List<CustomClientSessionModel>();

            m_currentSession = GetCurrentSession();
            ClientSession.LoaderCallback = (data, knownTypes) =>
            {
                var callback = Load<ClientSession>(durableSubscriptionsDirectory, durableSubscriptionMetadataPath, knownTypes);
                customClientSessionModelList.Add((CustomClientSessionModel)callback);
                return customClientSessionModelList;
            };

            List<CustomClientSessionModel> restoredSession =
                (List<CustomClientSessionModel>)m_currentSession.LoadModel(default,
                new[] { typeof(EventFilterEx), typeof(CustomClientMonitoredItemModel), typeof(CustomClientSubscriptionModel), typeof(CustomClientSessionModel) });

            return restoredSession;
        }

        #endregion

        #region Custom Subscription Model Persist/Load Implementation

        /// <summary>
        /// Example for Save and Load of a Custom Monitored Item Model
        /// Note: If CustomClientSessionModel or CustomClientSubscriptionModel is needed than they have to be wrapped in their corresponding model hierarchy.
        /// </summary>
        internal void CustomSaveLoadSubscriptionModel(bool withEvents = false)
        {
            m_currentSession = GetCurrentSession();

            //create the subscriptions
            m_subscription1 = CreateSubscription(SubscriptionName1, setDurable: true, republishAfterTransfer: true);
            m_subscription2 = CreateSubscription(SubscriptionName2, setDurable: false, republishAfterTransfer: true);

            // Give some time to allow for the true browse of items
            Thread.Sleep(500);

            if (m_currentSession == null)
            {
                Console.WriteLine($"CreateMonitoredItem: The session {m_currentSession.SessionName} is not initialized!");
                return;
            }

            if (m_subscription1 == null)
            {
                Console.WriteLine("CreateMonitoredItem: Subscription not created. Create a subscription first!");
                return;
            }

            if (m_subscription1.CurrentState == State.Disconnected &&
                m_subscription2.CurrentState == State.Disconnected && !m_isDisposed)
            {
                Console.WriteLine($"CreateMonitoredItem: The session {m_currentSession.SessionName} is not connected!");
                return;
            }

            try
            {
                CreateMultipleMonitoredItems(withEvents: withEvents);

                // Note: the user is responsible to provide a secure path for durable subscriptions metadata
                // no callback logic used
                string durableSubscriptionsPath = Path.Combine(Environment.CurrentDirectory, m_application.ClientToolkitConfiguration.DurableSubscriptionsPath);
                string subscriptionFilePath = Path.Combine(durableSubscriptionsPath, "CustomSubscriptionModel.txt");

                bool savedSubscriptions = PersistCustomSubscriptionModel(durableSubscriptionsPath, subscriptionFilePath);
                if (savedSubscriptions)
                {
                    List<ClientSubscriptionModel> restoredSubscriptions = LoadCustomSubscriptionModel(durableSubscriptionsPath, subscriptionFilePath);
                }

                CleanupDurableStorage(durableSubscriptionsPath);
            }
            catch (Exception ex)
            {
                Program.PrintException("CustomSaveLoadSubscriptionModel", ex);
            }
        }

        /// <summary>
        /// Performs implementation of persist custom subscription model.
        /// </summary>
        private bool PersistCustomSubscriptionModel(string durableStoragePath, string customSubscriptionsFilePath)
        {
            if (durableStoragePath is null) throw new ArgumentNullException(nameof(durableStoragePath));
            if (customSubscriptionsFilePath is null) throw new ArgumentNullException(nameof(customSubscriptionsFilePath));

            if (subscriptionsToPersist.Any(subscription => subscription.IsDurable))
            {
                m_currentSession = GetCurrentSession();
                ClientSession.PersistCallback = (data, knownTypes) =>
                {
                    string storagePath = Path.Combine(Environment.CurrentDirectory, durableStoragePath);
                    return Persist<ClientSubscription>(data, storagePath, customSubscriptionsFilePath, knownTypes);
                };

                return m_currentSession.PersistModel(subscriptionsToPersist,
                    new[] { typeof(SubscriptionCollection), typeof(Subscription), typeof(CustomClientSubscriptionModel), typeof(EventFilterEx),
                            typeof(ListOfMonitoredItemModel), typeof(ClientSubscriptionModel), typeof(List<ClientMonitoredItem>), typeof(CustomClientMonitoredItemModel) });
            }
            return false;
        }

        /// <summary>
        /// Performs implementation of load custom subscription model.
        /// </summary>
        /// <returns>List of custom subscriptions loaded.</returns>
        private List<ClientSubscriptionModel> LoadCustomSubscriptionModel(string durableSubscriptionsDirectory, string customSubscriptionsFilePath)
        {
            if (durableSubscriptionsDirectory is null) throw new ArgumentNullException(nameof(durableSubscriptionsDirectory));
            if (customSubscriptionsFilePath is null) throw new ArgumentNullException(nameof(customSubscriptionsFilePath));

            m_currentSession = GetCurrentSession();
            ClientSession.LoaderCallback = (data, knownTypes) =>
            {
                var callback = Load<ClientSubscription>(durableSubscriptionsDirectory, customSubscriptionsFilePath, knownTypes);
                return (IEnumerable<ClientSubscriptionModel>)callback;
            };

            List<ClientSubscriptionModel> restoredSubscriptions =
                (List<ClientSubscriptionModel>)m_currentSession.LoadModel(default,
                new[] { typeof(EventFilterEx), typeof(Subscription), typeof(List<ClientSubscription>),
                    typeof(ClientSubscriptionModel), typeof(CustomClientMonitoredItemModel), typeof(CustomClientSubscriptionModel) });

            return restoredSubscriptions;
        }

        #endregion

        #region Custom Monitored Item Model Persist/Load Implementation

        /// <summary>
        /// Example for Save and Load of a Custom Monitored Item Model
        /// </summary>
        internal void CustomSaveLoadMonitoredItemsModel(bool withEvents = false)
        {
            m_currentSession = GetCurrentSession();

            //create the subscriptions
            m_subscription1 = CreateSubscription(SubscriptionName1, setDurable: true, republishAfterTransfer: true);
            m_subscription2 = CreateSubscription(SubscriptionName2, setDurable: false, republishAfterTransfer: true);

            // Give some time to allow for the true browse of items
            Thread.Sleep(500);

            if (m_currentSession == null)
            {
                Console.WriteLine($"CreateMonitoredItem: The session {m_currentSession.SessionName} is not initialized!");
                return;
            }

            if (m_subscription1 == null)
            {
                Console.WriteLine("CreateMonitoredItem: Subscription not created. Create a subscription first!");
                return;
            }

            if (m_subscription1.CurrentState == State.Disconnected &&
                m_subscription2.CurrentState == State.Disconnected && !m_isDisposed)
            {
                Console.WriteLine($"CreateMonitoredItem: The session {m_currentSession.SessionName} is not connected!");
                return;
            }

            try
            {
                CreateMultipleMonitoredItems(withEvents: withEvents);

                // Note: the user is responsible to provide a secure path for durable subscriptions metadata
                // no callback logic used
                string durableSubscriptionsPath = Path.Combine(Environment.CurrentDirectory, m_application.ClientToolkitConfiguration.DurableSubscriptionsPath);
                string monitoredItemFilePath = Path.Combine(durableSubscriptionsPath, "CustomMonitoredItems.txt");

                bool savedMonitoredItems = PersistCustomMonitoredItemModel(durableSubscriptionsPath, monitoredItemFilePath);

                if (savedMonitoredItems)
                {
                    List<ClientMonitoredItemModel> restoredMonitoredItems = LoadCustomMonitoredItemModel(durableSubscriptionsPath, monitoredItemFilePath);
                }

                CleanupDurableStorage(durableSubscriptionsPath);
            }
            catch (Exception ex)
            {
                Program.PrintException("CustomSaveLoadMonitoredItemsModel", ex);
            }
        }

        /// <summary>
        /// Performs implementation of persist custom monitored item model.
        /// </summary>
        private bool PersistCustomMonitoredItemModel(string durableStoragePath, string customMonitoredItemFilePath)
        {
            if (durableStoragePath is null) throw new ArgumentNullException(nameof(durableStoragePath));
            if (customMonitoredItemFilePath is null) throw new ArgumentNullException(nameof(customMonitoredItemFilePath));

            if (subscriptionsToPersist.Any(subscription => subscription.IsDurable))
            {
                m_currentSession = GetCurrentSession();
                ClientSession.PersistCallback = (data, knownTypes) =>
                {
                    string storagePath = Path.Combine(Environment.CurrentDirectory, durableStoragePath);
                    return Persist<ClientMonitoredItem>(data, storagePath, customMonitoredItemFilePath, knownTypes);
                };

                List<ClientMonitoredItem> mis = subscriptionsToPersist.SelectMany(x => x.MonitoredItems).ToList();

                return m_currentSession.PersistModel(mis, new[] { typeof(List<ClientMonitoredItem>), typeof(EventFilterEx), typeof(CustomClientMonitoredItemModel) });
            }
            return false;
        }

        /// <summary>
        /// Performs implementation of load custom monitored item model.
        /// </summary>
        /// <returns>List of custom monitored items loaded.</returns>
        private List<ClientMonitoredItemModel> LoadCustomMonitoredItemModel(string durableSubscriptionsDirectory, string customMonitoredItemFilePath)
        {
            if (durableSubscriptionsDirectory is null) throw new ArgumentNullException(nameof(durableSubscriptionsDirectory));
            if (customMonitoredItemFilePath is null) throw new ArgumentNullException(nameof(customMonitoredItemFilePath));

            m_currentSession = GetCurrentSession();
            ClientSession.LoaderCallback = (data, knownTypes) =>
            {
                var callback = Load<ClientMonitoredItem>(durableSubscriptionsDirectory, customMonitoredItemFilePath, knownTypes);
                return (IEnumerable<ClientMonitoredItemModel>)callback;
            };

            List<ClientMonitoredItemModel> restoredMIs =
                (List<ClientMonitoredItemModel>)m_currentSession.LoadModel(default,
                new[] { typeof(EventFilterEx), typeof(CustomClientMonitoredItemModel) });

            return restoredMIs;
        }

        #endregion


        /// <summary>
        /// Persistence of specific Custom model
        /// </summary>
        /// <param name="data">The object data to save.</param>
        /// <param name="storage_path">The directory where to save.</param>
        /// <param name="filename">The file name where to save.</param>
        /// <param name="knownTypes"></param>
        /// <returns>Indicates whether the subscriptions were saved successfully.</returns>
        public bool Persist<T>(object data, string storage_path, string filename, IEnumerable<Type> knownTypes)
        {
            try
            {
                if (!Directory.Exists(storage_path))
                    Directory.CreateDirectory(storage_path);

                string filePath = Path.Combine(storage_path, filename);

                if (!File.Exists(filePath))
                    File.Create(filePath).Dispose();

                if (typeof(T) == typeof(ClientSession))
                {
                    var sessionToSave = (ClientSession)data;
                    CustomClientSessionModel sessionModel = new CustomClientSessionModel(sessionToSave);

                    var serializer = new DataContractSerializer(typeof(ClientSessionModel), knownTypes);
                    return ModelUtils.Save<ClientSessionModel>(filePath, sessionModel, serializer);
                }
                else if (typeof(T) == typeof(ClientSubscription))
                {
                    var subscriptionsToSave = (List<ClientSubscription>)data;

                    ListOfSubscriptionModel listOfSubscriptionModel = new ListOfSubscriptionModel();
                    foreach (ClientSubscription subscription in subscriptionsToSave)
                    {
                        CustomClientSubscriptionModel subscriptionModel = new CustomClientSubscriptionModel(subscription);
                        listOfSubscriptionModel.Add(subscriptionModel);
                    }

                    var serializer = new DataContractSerializer(typeof(ListOfSubscriptionModel), knownTypes);
                    return ModelUtils.Save<ListOfSubscriptionModel>(filePath, listOfSubscriptionModel, serializer);
                }
                else if (typeof(T) == typeof(ClientMonitoredItem))
                {
                    var monitoredItemsToSave = (List<ClientMonitoredItem>)data;

                    ListOfMonitoredItemModel listOfMonitoredItemsModel = new ListOfMonitoredItemModel();
                    foreach (ClientMonitoredItem mi in monitoredItemsToSave)
                    {
                        CustomClientMonitoredItemModel miModel = new CustomClientMonitoredItemModel(mi);
                        listOfMonitoredItemsModel.Add(miModel);
                    }

                    var serializer = new DataContractSerializer(typeof(ListOfMonitoredItemModel), knownTypes);
                    return ModelUtils.Save<ListOfMonitoredItemModel>(filePath, listOfMonitoredItemsModel, serializer);
                }

                return false;
            }
            catch (Exception ex)
            {
                Utils.Trace($"Error during Persist: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load specific model
        /// </summary>
        /// <param name="storage_path">The directory from where to load.</param>
        /// <param name="filename">The file name from where to load.</param>
        /// <param name="knownTypes"></param>
        /// <returns>A list of objects loaded.</returns>
        public object Load<T>(string storage_path, string filename, IEnumerable<Type> knownTypes)
        {
            try
            {
                string filePath = Path.Combine(storage_path, filename);
                if (File.Exists(filePath))
                {
                    if (typeof(T) == typeof(ClientSession))
                    {
                        var serializer = new DataContractSerializer(typeof(ClientSessionModel), knownTypes);
                        ClientSessionModel clientSessionModel = ModelUtils.Load<ClientSessionModel>(filePath, serializer);

                        return clientSessionModel;
                    }
                    else if (typeof(T) == typeof(ClientSubscription))
                    {
                        var serializer = new DataContractSerializer(typeof(ListOfSubscriptionModel), knownTypes);
                        ListOfSubscriptionModel subscriptionsModel = ModelUtils.Load<ListOfSubscriptionModel>(filePath, serializer);

                        return subscriptionsModel;
                    }
                    else if (typeof(T) == typeof(ClientMonitoredItem))
                    {
                        var serializer = new DataContractSerializer(typeof(ListOfMonitoredItemModel), knownTypes);
                        ListOfMonitoredItemModel monitoredItemsModel = ModelUtils.Load<ListOfMonitoredItemModel>(filePath, serializer);

                        return monitoredItemsModel;
                    }
                }
                return new List<T>();
            }
            catch (Exception ex)
            {
                Utils.Trace($"Error during Load: {ex.Message}");
                return new List<T>();
            }
        }
    }
}
