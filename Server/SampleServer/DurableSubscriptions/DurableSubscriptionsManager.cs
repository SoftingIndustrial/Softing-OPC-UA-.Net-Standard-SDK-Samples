/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server.DurableSubscriptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SampleServer.DurableSubscriptions
{
    /// <summary>
    /// Durable subscription manager
    /// </summary>
    public partial class DurableSubscriptionsManager : ISubscriptionStore
    {
        private string m_durableSubscriptionsPath;
        private string m_durableSubscriptionsRevisedPath;
        private string m_durableSubscriptionsStore;
        private string m_durableSubscriptionsRevisedLifetime;
        private readonly DurableMonitoredItemsManager m_durableMonitoredItemQueueFactory;
        private Dictionary<uint, uint> m_revisedLifetimeInHours;
        private IServerInternal m_server;
        private ServerDurableConfiguration m_serverDurableConfiguration;

        #region Constructor(s)

        /// <summary>
        /// Constructor
        /// </summary>
        public DurableSubscriptionsManager(IServerInternal server, ServerDurableConfiguration serverDurableConfiguration)
        {
            m_revisedLifetimeInHours = new Dictionary<uint, uint>();

            if (server != null)
            {
                m_server = server;

                m_serverDurableConfiguration = serverDurableConfiguration;
                m_durableMonitoredItemQueueFactory = server.MonitoredItemQueueFactory as DurableMonitoredItemsManager;
            }

            SetSubscriptionDurableMethodState setSubscriptionDurable =
                (SetSubscriptionDurableMethodState)m_server?.NodeManager?.ConfigurationNodeManager.FindPredefinedNode(
                        MethodIds.Server_SetSubscriptionDurable,
                        typeof(SetSubscriptionDurableMethodState));

            if (setSubscriptionDurable != null)
            {
                setSubscriptionDurable.OnCall = OnSetSubscriptionDurable;
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize durable subscriptions manager settings
        ///</summary>
        public void Initialize()
        {
            try
            {
                if (m_serverDurableConfiguration != null)
                {
                    m_durableSubscriptionsPath = Path.Combine(Environment.CurrentDirectory, m_serverDurableConfiguration.DurableSubscriptionsPath);
                    m_durableSubscriptionsRevisedPath = Path.Combine(Environment.CurrentDirectory, m_serverDurableConfiguration.DurableSubscriptionsRevisedPath);
                    m_durableSubscriptionsStore = m_serverDurableConfiguration.DurableSubscriptionsStoreFilePath;
                    m_durableSubscriptionsRevisedLifetime = m_serverDurableConfiguration.DurableSubscriptionsRevisedLifetimeFilePath;
                }

                SetSubscriptionDurableMethodState setSubscriptionDurable = (SetSubscriptionDurableMethodState)m_server?.NodeManager?.ConfigurationNodeManager.FindPredefinedNode(
                        MethodIds.Server_SetSubscriptionDurable,
                        typeof(SetSubscriptionDurableMethodState));

                if (setSubscriptionDurable != null)
                {
                    setSubscriptionDurable.OnCall = OnSetSubscriptionDurable;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(ex, "DurableSubscriptionsManager.Initialize error:");
            }


        }
        /// <summary>
        /// Store subscriptions in storage, called on server shutdown
        /// </summary>
        /// <param name="subscriptions">the subscription templates to store</param>
        /// <returns>true if storing was successful</returns>
        public bool StoreSubscriptions(IEnumerable<IStoredSubscription> subscriptions)
        {
            try
            {
                string result = JsonConvert.SerializeObject(subscriptions, DurableSettings.JsonSettings);

                if (!Directory.Exists(m_durableSubscriptionsPath))
                    Directory.CreateDirectory(m_durableSubscriptionsPath);

                string updatedResult = ReplaceTextToValue(result);

                File.WriteAllText(Path.Combine(m_durableSubscriptionsPath, m_durableSubscriptionsStore), updatedResult);

                if (m_durableMonitoredItemQueueFactory != null)
                {
                    IEnumerable<uint> ids = subscriptions.SelectMany(s => s.MonitoredItems.Select(m => m.Id));
                    m_durableMonitoredItemQueueFactory.SaveQueueToFile(m_durableSubscriptionsPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                Utils.LogWarning(ex, "Failed to store subscriptions");
            }
            return false;
        }

        /// <summary>
        /// Restore subscriptions from storage, called on server startup
        /// </summary>
        /// <returns>the result of the restore operation</returns>
        public RestoreSubscriptionResult RestoreSubscriptions()
        {
            string filePath = Path.Combine(m_durableSubscriptionsPath, m_durableSubscriptionsStore);
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);

                    string updatedResult = ReplaceValueToText(json);

                    List<IStoredSubscription> storedSubscription = JsonConvert.DeserializeObject<List<IStoredSubscription>>(updatedResult, DurableSettings.JsonSettings);

                    // Note: Do not forget to properly clean up expired subscriptions.
                    CheckExpiredSubscriptions(filePath, storedSubscription);

                    if (storedSubscription.Any(subscription => subscription.IsDurable))
                    {
                        var monitoredItems = storedSubscription.SelectMany(s => s.MonitoredItems).ToList();

                        foreach (var storedMonitoredItem in monitoredItems)
                        {
                            #region Restore event filter correction
                            EventFilter eventFilter = storedMonitoredItem.FilterToUse as EventFilter;
                            if (eventFilter != null)
                            {
                                SimpleAttributeOperandCollection simpleAttributes = eventFilter.SelectClauses;
                                if (simpleAttributes != null)
                                {
                                    FilterContext context = new FilterContext(m_server.NamespaceUris, m_server.TypeTree);

                                    SimpleAttributeOperandCollection simpleAttributeOperands = new SimpleAttributeOperandCollection();
                                    foreach (SimpleAttributeOperand operand in simpleAttributes)
                                    {
                                        SimpleAttributeOperand cleanOperand =
                                            new SimpleAttributeOperand()
                                            {
                                                TypeDefinitionId = operand.TypeDefinitionId,
                                                BrowsePath = operand.BrowsePath,
                                                AttributeId = operand.AttributeId,
                                                IndexRange = operand.IndexRange
                                            };
                                        // reset operand.ParsedIndexRange that is changed on save
                                        cleanOperand.Validate(context, -1);
                                        simpleAttributeOperands.Add(cleanOperand);
                                    }
                                    // override the restored eventFilter.SelectClauses
                                    eventFilter.SelectClauses = simpleAttributeOperands;
                                }
                            }
                            #endregion
                        }

                        File.Delete(filePath);
                    }
                    return new RestoreSubscriptionResult(true, storedSubscription);
                }
            }
            catch (Exception ex)
            {
                Utils.LogWarning(ex, "Failed to restore subscriptions");
            }

            return new RestoreSubscriptionResult(false, null);
        }

        /// <summary>
        /// Prevent enqueuing values on restore if durable subscriptions are expired.
        /// </summary>
        private void CheckExpiredSubscriptions(string filePath, List<IStoredSubscription> subscriptions)
        {
            DateTime fileTimestamp = File.GetLastWriteTimeUtc(Path.Combine(m_durableSubscriptionsRevisedPath, m_durableSubscriptionsRevisedLifetime));
            List<Dictionary<uint, uint>> listOfRevisedSubscriptions = RestoreSubscriptionRevisedLifetime(subscriptions);
            foreach (var revisedSubscription in listOfRevisedSubscriptions)
            {
                foreach (var revisedValue in revisedSubscription)
                {
                    bool expired = IsSubscriptionExpired(fileTimestamp, revisedValue.Value);

                    if (expired)
                    {
                        Console.WriteLine("Subscription expired on server!");

                        // Do not enqueue values if subscription is expired
                        foreach (var subscription in subscriptions.ToList())
                        {
                            subscription.MonitoredItems = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if durable subscription is expired based on revisedLifetimeInHours value
        /// </summary>
        /// <param name="fileTimestamp"></param>
        /// <param name="revisedLifetimeInHours"></param>
        /// <returns>Flag indicating subscription is expired</returns>
        private static bool IsSubscriptionExpired(DateTime fileTimestamp, uint revisedLifetimeInHours)
        {
            DateTime expirationTime = fileTimestamp.AddHours(revisedLifetimeInHours);
            return DateTime.UtcNow > expirationTime;
        }

        /// <summary>
        /// Restore subscriptions revised lifetime in hours from storage, called on server startup
        /// </summary>
        /// <returns>Returns a list of key-value pairs where each dictionary maps a subscriptionId to its corresponding revisedLifetime.</returns>
        private List<Dictionary<uint, uint>> RestoreSubscriptionRevisedLifetime(List<IStoredSubscription> subscriptions)
        {
            List<Dictionary<uint, uint>> result = new List<Dictionary<uint, uint>>();

            string filePath = Path.Combine(m_durableSubscriptionsRevisedPath, m_durableSubscriptionsRevisedLifetime);
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);

                    Dictionary<uint, uint> dict = new Dictionary<uint, uint>();

                    foreach (var line in File.ReadLines(filePath))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue; // Skip empty lines

                        var parts = line.Split(':');
                        if (parts.Length == 2 &&
                            uint.TryParse(parts[0].Trim(), out uint subId) &&
                            uint.TryParse(parts[1].Trim(), out uint revisedValue))
                        {
                            m_revisedLifetimeInHours[subId] = revisedValue;
                        }
                    }

                    foreach (var subscription in subscriptions.ToList())
                    {
                        if (m_revisedLifetimeInHours.ContainsKey(subscription.Id))
                        {
                            dict[subscription.Id] = m_revisedLifetimeInHours[subscription.Id];
                            result.Add(dict);
                        }
                    }

                    try
                    {
                        File.Delete(filePath);
                        Directory.Delete(m_durableSubscriptionsRevisedPath, true);
                    }
                    catch (Exception ex)
                    {
                        Utils.LogWarning(ex, "Failed to cleanup files for stored subscription");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                Utils.LogWarning(ex, "Failed to restore subscriptions revised values");
            }

            return new List<Dictionary<uint, uint>>();
        }

        /// <summary>
        /// Store in file subscriptionId and revisedLifetimeInHours of the durable subscription
        /// </summary>
        private void StoreRevisedLifetimeInHours(uint subscriptionId)
        {
            string subscriptionRevisedLifetimePath = Path.Combine(m_durableSubscriptionsRevisedPath, m_durableSubscriptionsRevisedLifetime);
            if (!Directory.Exists(m_durableSubscriptionsRevisedPath))
                Directory.CreateDirectory(m_durableSubscriptionsRevisedPath);

            if (!File.Exists(subscriptionRevisedLifetimePath))
            {
                File.Create(subscriptionRevisedLifetimePath).Dispose();
            }

            using (StreamWriter streamWriter = new StreamWriter(subscriptionRevisedLifetimePath))
            {
                uint hours = m_revisedLifetimeInHours.ContainsKey(subscriptionId)
                    ? m_revisedLifetimeInHours[subscriptionId]
                    : 0;
                streamWriter.WriteLine($"{subscriptionId}:{hours}");
            }
        }


        /// <summary>
        /// Replace text property to value in the json file.
        /// </summary>
        private static string ReplaceTextToValue(string json)
        {
            JObject obj = JObject.Parse(json);

            // Find all properties named "#text" and rename them to "#value"
            foreach (var token in obj.SelectTokens("$..#text").ToList())
            {
                JProperty property = (JProperty)token.Parent;
                property.Replace(new JProperty("#value", property.Value));
            }

            return obj.ToString();
        }


        /// <summary>
        /// Replace value property to text in the json file.
        /// </summary>
        private static string ReplaceValueToText(string json)
        {
            JObject obj = JObject.Parse(json);

            // Find all properties named "#text" and rename them to "#value"
            foreach (var token in obj.SelectTokens("$..#value").ToList())
            {
                JProperty property = (JProperty)token.Parent;
                property.Replace(new JProperty("#text", property.Value));
            }

            return obj.ToString();
        }

        /// <summary>
        /// Restore a DataChangeMonitoredItemQueue from storage
        /// </summary>
        /// <param name="monitoredItemId">Id of the MonitoredItem owning the the queue</param>
        /// <returns>the queue</returns>
        public IDataChangeMonitoredItemQueue RestoreDataChangeMonitoredItemQueue(uint monitoredItemId)
        {
            return m_durableMonitoredItemQueueFactory?.RestoreDataChangeQueue(monitoredItemId, m_durableSubscriptionsPath);
        }

        /// <summary>
        /// Restore an EventMonitoredItemQueue from storage
        /// </summary>
        /// <param name="monitoredItemId">Id of the MonitoredItem owning the queue</param>
        /// <returns>the queue</returns>
        public IEventMonitoredItemQueue RestoreEventMonitoredItemQueue(uint monitoredItemId)
        {
            return m_durableMonitoredItemQueueFactory?.RestoreEventQueue(monitoredItemId, m_durableSubscriptionsPath);
        }

        /// <summary>
        /// Called when a client sets a subscription as durable.
        /// </summary>
        public ServiceResult OnSetSubscriptionDurable(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint subscriptionId,
            uint lifetimeInHours,
            ref uint revisedLifetimeInHours)
        {
            revisedLifetimeInHours = 0;
            ServiceResult serviceResult = m_server.NodeManager.ConfigurationNodeManager.OnSetSubscriptionDurable(context, method, objectId, subscriptionId, lifetimeInHours, ref revisedLifetimeInHours);

            // save the revisedLifetimeInHours after the method is called
            // this method is called after the subscription was already created on SubscriptionCreated callback
            if (!m_revisedLifetimeInHours.ContainsKey(subscriptionId))
            {
                m_revisedLifetimeInHours.Add(subscriptionId, revisedLifetimeInHours);
                StoreRevisedLifetimeInHours(subscriptionId);
                return serviceResult;
            }

            return new ServiceResult(StatusCodes.BadDataUnavailable);
        }

        /// <summary>
        /// Signals created Subscription ids incl. MonitoredItem ids to the SubscriptionStore instance, to signal cleanup can take place
        /// The store shall clean all stored subscriptions, monitoredItems, and only keep the persistent queues for the monitoredItem ids provided
        /// <param name="createdSubscriptions"> key = subscription id, value = monitoredItem ids </param>
        /// </summary>
        public void OnSubscriptionRestoreComplete(Dictionary<uint, uint[]> createdSubscriptions)
        {
            if (Directory.Exists(m_durableSubscriptionsPath))
                try
                {
                    Directory.Delete(m_durableSubscriptionsPath, true);
                }
                catch (Exception ex)
                {
                    Utils.LogWarning(ex, "Failed to cleanup files for stored subscription");
                }
        }

        /// <summary>
        /// Remove storage data
        /// </summary>
        public void RemoveStorageData()
        {
            if (Directory.Exists(m_durableSubscriptionsPath))
            {
                try
                {
                    Directory.SetCurrentDirectory(m_durableSubscriptionsPath);
                    string[] folders = Directory.GetDirectories(m_durableSubscriptionsPath);
                    foreach (string folder in folders)
                    {
                        Directory.Delete(folder, true);
                    }

                    DirectoryInfo durableStorageInfo = Directory.GetParent(m_durableSubscriptionsPath);
                    if (durableStorageInfo != null && durableStorageInfo.Exists)
                    {
                        Directory.SetCurrentDirectory(durableStorageInfo.FullName);
                        Directory.Delete(m_durableSubscriptionsPath, true);
                    }
                    else
                    {
                        string subscriptionListFile = Path.Combine(m_durableSubscriptionsPath, m_durableSubscriptionsStore);
                        if (File.Exists(subscriptionListFile))
                        {
                            File.Delete(subscriptionListFile);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogWarning(ex, "DurableSubscriptionsManager.RemoveStorageData: Failed to cleanup files for stored subscriptions.");
                }
            }
        }

        #endregion
    }

}
