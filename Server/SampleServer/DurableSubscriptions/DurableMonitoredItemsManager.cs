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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace SampleServer.DurableSubscriptions
{
    /// <summary>
    /// A factory for <see cref="IDataChangeMonitoredItemQueue"> and </see> <see cref="IEventMonitoredItemQueue"/>
    /// </summary>
    public class DurableMonitoredItemsManager : IMonitoredItemQueueFactory
    {
        #region Fields

        private static readonly string QueuesDirectory = "Queues";
        private const string MonitorItemPrefix = "MI_";
        private const string SubscriptionNamePrefix = "Sub_";

        private ConcurrentDictionary<uint, DurableDataChangesQueue> m_dataChangeQueues = new ConcurrentDictionary<uint, DurableDataChangesQueue>();
        private ConcurrentDictionary<uint, DurableEventChangesQueue> m_eventQueues = new ConcurrentDictionary<uint, DurableEventChangesQueue>();

        #endregion

        #region Constructor

        /// <summary>
        /// Durable subscription constructor
        /// </summary>
        public DurableMonitoredItemsManager(int m_maxDurableNotificationQueueSize, int maxDurableEventQueueSize)
        {
            m_dataChangeQueues = new ConcurrentDictionary<uint, DurableDataChangesQueue>(m_maxDurableNotificationQueueSize, maxDurableEventQueueSize);
            m_eventQueues = new ConcurrentDictionary<uint, DurableEventChangesQueue>(maxDurableEventQueueSize, maxDurableEventQueueSize);

            SupportsDurableQueues = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// If true durable queues can be created by the factory, if false only regular queues with small queue sizes are returned
        /// </summary>
        public bool SupportsDurableQueues { get; }

        #endregion

        #region Public methods

        /// <summary>
        /// Creates an empty queue for data values.
        /// </summary>
        public IDataChangeMonitoredItemQueue CreateDataChangeQueue(bool createDurable, uint monitoredItemId)
        {
            //use durable queue only if MI is durable
            if (createDurable)
            {
                var queue = new DurableDataChangesQueue(createDurable, monitoredItemId);
                queue.Disposed += DataChangeQueueDisposed;
                m_dataChangeQueues.AddOrUpdate(monitoredItemId, queue, (key, oldValue) => queue);
                return queue;
            }
            else
                return new DataChangeMonitoredItemQueue(createDurable, monitoredItemId);
        }

        /// <summary>
        /// Creates an empty queue for events.
        /// </summary>
        public IEventMonitoredItemQueue CreateEventQueue(bool createDurable, uint monitoredItemId)
        {
            //use durable queue only if MI is durable
            if (createDurable)
            {
                var queue = new DurableEventChangesQueue(createDurable, monitoredItemId);
                queue.Disposed += EventQueueDisposed;
                m_eventQueues.AddOrUpdate(monitoredItemId, queue, (key, oldValue) => queue);
                return queue;
            }
            else
            {
                return new EventMonitoredItemQueue(createDurable, monitoredItemId);
            }
        }

        /// <summary>
        /// Handles the disposal of a data change queue.
        /// </summary>
        private void DataChangeQueueDisposed(object sender, EventArgs eventArgs)
        {
            if (sender is DataChangeMonitoredItemQueue queue)
                m_dataChangeQueues.TryRemove(queue.MonitoredItemId, out _);
        }

        /// <summary>
        /// Handles the disposal of an event queue.
        /// </summary>
        private void EventQueueDisposed(object sender, EventArgs eventArgs)
        {
            if (sender is EventMonitoredItemQueue queue)
                m_eventQueues.TryRemove(queue.MonitoredItemId, out _);
        }

        /// <summary>
        /// Persist the queues of the monitored items with the provided ids
        /// </summary>
        public void SaveQueueToFile(string subscriptionPath)
        {
            try
            {
                if (Directory.Exists(subscriptionPath) && m_dataChangeQueues != null)
                {
                    foreach (var monitorItemId in m_dataChangeQueues.Keys)
                    {
                        if (!Directory.Exists($@"{subscriptionPath}\{QueuesDirectory}"))
                            Directory.CreateDirectory($@"{subscriptionPath}\{QueuesDirectory}");

                        string monitorItemFilePath = Path.Combine($@"{subscriptionPath}\{QueuesDirectory}", $"{MonitorItemPrefix}{monitorItemId}_datachange.txt");

                        // Ensure file exists
                        File.Create(monitorItemFilePath).Dispose();

                        // Try to convert and serialize if possible
                        if (m_dataChangeQueues.TryGetValue(monitorItemId, out var monitorDataChange) &&
                            monitorDataChange is DurableDataChangesQueue durableDataChangeQueue)
                        {
                            var storableDataChange = durableDataChangeQueue.ToStorableQueue();
                            if (storableDataChange != null)
                            {
                                string dataChangesTxt = JsonConvert.SerializeObject(storableDataChange, DurableSettings.JsonSettings);
                                string updatedDataChangesTxt = ReplaceTextToValue(dataChangesTxt);
                                File.WriteAllText(monitorItemFilePath, updatedDataChangesTxt);
                            }
                        }
                    }
                }
                if (Directory.Exists(subscriptionPath) && m_eventQueues != null)
                {
                    foreach (var monitorItemId in m_eventQueues.Keys)
                    {
                        if (!Directory.Exists($@"{subscriptionPath}\{QueuesDirectory}"))
                            Directory.CreateDirectory($@"{subscriptionPath}\{QueuesDirectory}");

                        string monitorItemFilePath = Path.Combine($@"{subscriptionPath}\{QueuesDirectory}", $"{MonitorItemPrefix}{monitorItemId}_eventchange.txt");

                        // Ensure file exists
                        File.Create(monitorItemFilePath).Dispose();

                        // Try to convert and serialize if possible
                        if (m_eventQueues.TryGetValue(monitorItemId, out var monitorEventChange) &&
                            monitorEventChange is DurableEventChangesQueue durableEventChangeQueue)
                        {
                            var storableEventChange = durableEventChangeQueue.ToStorableQueue();
                            if (storableEventChange != null)
                            {
                                string dataChangesTxt = JsonConvert.SerializeObject(storableEventChange, DurableSettings.JsonSettings);
                                string updatedDataChangesTxt = ReplaceTextToValue(dataChangesTxt);
                                File.WriteAllText(monitorItemFilePath, updatedDataChangesTxt);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(ex, "DurableMonitoredItemsHolder.Save error: ");
            }
        }

        /// <summary>
        /// Restore an Event queue
        /// </summary>
        public IEventMonitoredItemQueue RestoreEventQueue(uint id, string baseDirectory)
        {
            if (Directory.Exists($@"{baseDirectory}\{QueuesDirectory}"))
            {
                string[] folders = Directory.GetDirectories(baseDirectory);
                foreach (string folder in folders)
                {
                    string monitorItemFileName = $"{MonitorItemPrefix}{id}_eventchange.txt";
                    if (folder.Contains(SubscriptionNamePrefix))
                    {
                        var subsFolder = Path.Combine(baseDirectory, folder);
                        Directory.SetCurrentDirectory(subsFolder);
                        string[] files = Directory.GetFiles(subsFolder);
                        string monitorFileNameFound = files.FirstOrDefault(file => file.Contains(monitorItemFileName));
                        if (File.Exists(monitorFileNameFound))
                        {
                            string monitorItemTxt = File.ReadAllText(monitorFileNameFound);
                            string updatedMonitorItemTxt = ReplaceValueToText(monitorItemTxt);

                            StorableEventQueue storageEvent = JsonConvert.DeserializeObject<StorableEventQueue>(updatedMonitorItemTxt, DurableSettings.JsonSettings);
                            if (storageEvent != null)
                            {
                                DurableEventChangesQueue queue = new DurableEventChangesQueue(storageEvent);
                                m_eventQueues.AddOrUpdate(id, queue, (key, oldValue) => queue);

                                return queue;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Restore a DataChange queue
        /// </summary>
        public IDataChangeMonitoredItemQueue RestoreDataChangeQueue(uint id, string baseDirectory)
        {
            if (Directory.Exists($@"{baseDirectory}\{QueuesDirectory}"))
            {
                string[] folders = Directory.GetDirectories(baseDirectory);
                foreach (string folder in folders)
                {
                    string monitorItemFileName = $"{MonitorItemPrefix}{id}_datachange.txt";
                    if (folder.Contains(SubscriptionNamePrefix))
                    {
                        var subsFolder = Path.Combine(baseDirectory, folder);
                        Directory.SetCurrentDirectory(subsFolder);
                        string[] files = Directory.GetFiles(subsFolder);
                        string monitorFileNameFound = files.FirstOrDefault(file => file.Contains(monitorItemFileName));
                        if (File.Exists(monitorFileNameFound))
                        {
                            string monitorItemTxt = File.ReadAllText(monitorFileNameFound);
                            StorableDataChangeQueue storageDataChange = JsonConvert.DeserializeObject<StorableDataChangeQueue>(monitorItemTxt, DurableSettings.JsonSettings);
                            if (storageDataChange != null)
                            {
                                DurableDataChangesQueue durableDataChanges = new DurableDataChangesQueue(storageDataChange);
                                m_dataChangeQueues.AddOrUpdate(id, durableDataChanges, (key, oldValue) => durableDataChanges);
                                return durableDataChanges;
                            }
                        }
                    }
                }
            }

            return null;
        }
        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Dispose the queues
        /// </summary>
        public void Dispose()
        {
            foreach (DurableEventChangesQueue queue in m_eventQueues.Values)
                Utils.SilentDispose(queue);
            foreach (DurableDataChangesQueue queue in m_dataChangeQueues.Values)
                Utils.SilentDispose(queue);
            m_dataChangeQueues = null;
            m_eventQueues = null;
        }
        #endregion

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
    }
}