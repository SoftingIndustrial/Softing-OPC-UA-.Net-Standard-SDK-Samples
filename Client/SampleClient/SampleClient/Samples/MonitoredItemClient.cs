﻿/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Opc.Ua;
using Softing.Opc.Ua;
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

        // Browse path: Root\\Objects\\DataAccess\Refrigerator\MotorTemperature
        private static readonly string m_monitoredItemNodeId = "ns=3;i=28";
        private static readonly string m_monitoredItemBrowsePath = "Root\\Objects\\DataAccess\\Refrigerator\\MotorTemperature";
        
        private readonly UaApplication m_application;
        private readonly List<ClientMonitoredItem> m_monitoredItems;
        private readonly Random m_randomGenerator;

        private ClientSession m_session;
        private ClientSubscription m_subscription;
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of MonitoredItemClient
        /// </summary>
        /// <param name="application"></param>
        public MonitoredItemClient(UaApplication application)
        {
            m_application = application;
            m_monitoredItems = new List<ClientMonitoredItem>();
            m_randomGenerator = new Random();
        }
        #endregion

        #region Initialize & Disconnect Session
        /// <summary>
        /// Initialize session and subscription
        /// </summary>
        private void InitializeSession()
        {
            // create the session object.            
            m_session = m_application.CreateSession(
                Constants.SampleServerUrlOpcTcp,
                MessageSecurityMode.None,
                SecurityPolicy.None,
                MessageEncoding.Binary,
                new UserIdentity(),
                null);
            m_session.SessionName = SessionName;

            try
            {
                //connect session
                m_session.Connect(false, true);
                Console.WriteLine("Session is connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateSession Error: {0}", ex.Message);
                m_session.Dispose();
                m_session = null;
                return;
            }

            //create the subscription
            m_subscription = new ClientSubscription(m_session, SubscriptionName);

            // set the Publishing interval for this subscription
            m_subscription.PublishingInterval = 500;
            Console.WriteLine("Subscription created");
        }

        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public virtual void DisconnectSession()
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
            if (m_session == null || m_subscription == null)
            {
                InitializeSession();
            }
            if (m_session == null)
            {
                Console.WriteLine("CreateMonitoredItem: The session is not initialized!");
                return;
            }
            try
            {
                NodeId node = new NodeId(m_monitoredItemNodeId);
                ClientMonitoredItem monitoredItem = new ClientMonitoredItem(m_subscription, node, AttributeId.Value,
                    null, "Sample Monitored Item" + m_monitoredItems.Count);
                monitoredItem.DataChangesReceived += Monitoreditem_DataChangesReceived;
                m_monitoredItems.Add(monitoredItem);

                Console.WriteLine("Monitored item {0} on browse path '{1}' created. Data value changes are shown:", m_monitoredItems.Count, m_monitoredItemBrowsePath);
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
            if (m_monitoredItems.Count == 0)
            {
                Console.WriteLine("There is no Monitored item to be deleted.");
                return;
            }
            if (m_session == null)
            {
                Console.WriteLine("DeleteMonitoredItem: The session is not initialized!");
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

        #region Event Handlers
        /// <summary>
        /// Handles the Notification event of the Monitoreditem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataChangesNotificationEventArgs"/> instance containing the event data.</param>
        private void Monitoreditem_DataChangesReceived(object sender, DataChangesNotificationEventArgs e)
        {
            Task.Delay(10000).Wait();
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
