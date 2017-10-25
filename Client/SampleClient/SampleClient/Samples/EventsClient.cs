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
using System.Collections.Generic;
using System.Text;
using Opc.Ua;
using Softing.Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Sample Client class that provides events functionality
    /// </summary>
    public class EventsClient
    {
        #region Private Fields
        private const string SessionName = "EventsClient Session";
        private const string SubscriptionName = "EventsClient Subscription";
        private static readonly QualifiedName EventPropertyName = new QualifiedName("FluidLevel", 5);

        private readonly UaApplication m_application;
        private ClientSession m_session;
        private ClientSubscription m_subscription;
        private ClientMonitoredItem m_eventMonitoredItem;
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

        #region Event Monitored Item Methods
        /// <summary>
        /// Creates the event monitored item.
        /// </summary>
        public void CreateEventMonitoredItem()
        {
            if (m_eventMonitoredItem != null)
            {
                Console.WriteLine("EventMonitoredItem is already created.");
                return;
            }
            
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
                Console.WriteLine("CreateSession Error: {0}", ex);
            }

            //create the subscription
            m_subscription = new ClientSubscription(m_session, SubscriptionName);

            // set the Publishing interval for this subscription
            m_subscription.PublishingInterval = 500;
            Console.WriteLine("Subscription created");

            try
            {
                //ObjectIds.Server BrowsePath: Root\Objects\Server
                m_eventMonitoredItem = new ClientMonitoredItem(m_subscription, ObjectIds.Server, "Sample Event Monitored Item", null);
                m_eventMonitoredItem.EventsReceived += m_eventMonitoredItem_EventsReceived;
                Console.WriteLine("Event Monitored Item is created and connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Creates the event monitored item filter.
        /// </summary>
        public void ApplyEventMonitoredItemFilter()
        {
            //chek if events monitored item exists and create it if necessary
            if (m_eventMonitoredItem == null)
            {
                CreateEventMonitoredItem();
            }

            EventFilterEx filter = (EventFilterEx) m_eventMonitoredItem.Filter;
            if (filter != null)
            {
                //check if filter already applied
                foreach (var selectOperand in filter.SelectOperandList)
                {
                    //ObjectTypeIds.BaseObjectType - BrowsePath: Root\Types\ObjectTypes\BaseObjectType\BaseEventType
                    if (selectOperand.EventTypeId == ObjectTypeIds.BaseObjectType &&
                        selectOperand.PropertyName.Equals(EventPropertyName))
                    {
                        Console.WriteLine("Filter is already applied.");
                        return;
                    }
                }

                filter.AddSelectClause(ObjectTypeIds.BaseObjectType, EventPropertyName);

                //ObjectTypeIds.BaseEventType BrowsePath: Root\Types\ObjectTypes\BaseObjectType\BaseEventType
                filter.EventTypeIdFilter = ObjectTypeIds.BaseEventType;
                try
                {
                    m_eventMonitoredItem.ApplyFilter();
                    Console.WriteLine("Filter is applied on Event Monitored Item.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Filter could not be applied.Error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Deletes the event monitored item.
        /// </summary>
        public void DeleteEventMonitoredItem()
        {
            if (m_eventMonitoredItem != null)
            {

                //detele event monitored item
                m_eventMonitoredItem.EventsReceived -= m_eventMonitoredItem_EventsReceived;
                m_eventMonitoredItem.Delete();
                m_eventMonitoredItem = null;
                Console.WriteLine("Event Monitored Item was disconnected and deleted.");
            }
            try
            {
                //delete subscription
                m_session.DeleteSubscription(m_subscription);
                m_subscription = null;
                Console.WriteLine("Subscription deleted");

                //disconnect session
                m_session.Disconnect(true);
                m_session.Dispose();
                m_session = null;
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the Notification event of the eventMonitoredItem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventsNotificationEventArgs"/> instance containing the event data.</param>
        private void m_eventMonitoredItem_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
            foreach (var eventNotification in e.EventNotifications)
            {
                Console.WriteLine("Event notification received for {0}.\n", eventNotification.MonitoredItem.DisplayName);

                StringBuilder displayNotification = new StringBuilder();
                IList<SelectOperandEx> listOfOperands = ((EventFilterEx)m_eventMonitoredItem.Filter).SelectOperandList;
                for (int i = 0; i < listOfOperands.Count; i++)
                {
                    displayNotification.AppendFormat("{0}:{1}:{2}\n", 
                        listOfOperands[i].PropertyName.NamespaceIndex , 
                        listOfOperands[i].PropertyName.Name,
                        eventNotification.EventFields[i]);
                }

                Console.WriteLine(displayNotification);
            }
        }
        #endregion
    }
}
