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

        private static readonly string m_monitoredItemBrowsePath = "Server\\ServerStatus\\CurrentTime";
        private static readonly NodeId m_monitoredItemNodeId = VariableIds.Server_ServerStatus_CurrentTime;
        
        private readonly UaApplication m_application;

        private ClientSession m_session;
        private ClientSubscription m_subscription;
        private ClientMonitoredItem m_monitoredItem;
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
            // create the session object.            
            m_session = m_application.CreateSession(
                Constants.ServerUrl,
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

                //create the subscription
                m_subscription = new ClientSubscription(m_session, SubscriptionName);

                // set the Publishing interval for this subscription
                m_subscription.PublishingInterval = 500;
                Console.WriteLine("Subscription created");
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateSession Error: {0}", ex.Message);
                m_session.Dispose();
                m_session = null;
            }
        }

        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public virtual void Disconnect()
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
            if (m_monitoredItem != null)
            {
                Console.WriteLine("MonitoredItem already created");
                return;
            }
            try
            {
               m_monitoredItem = new ClientMonitoredItem(m_subscription, m_monitoredItemNodeId, Attributes.Value,
                    null, "Monitored Item" + m_monitoredItemBrowsePath);
                m_monitoredItem.DataChangesReceived += Monitoreditem_DataChangesReceived;

                Console.WriteLine("Monitored item '{0}' created. Data value changes are shown:", m_monitoredItem.DisplayName);
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
            if (m_monitoredItem == null)
            {
                Console.WriteLine("Monitored item is not created.");
                return;
            }
            try
            {
                m_monitoredItem.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item unsubscribed from receiving data change notifications.");
                m_monitoredItem.Delete();
                m_monitoredItem = null;
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
