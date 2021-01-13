/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using Opc.Ua;
using XamarinSampleClient.Helpers;
using XamarinSampleClient.Models;
using Softing.Opc.Ua.Client;

namespace XamarinSampleClient.ViewModels
{
    /// <summary>
    /// View Model for EventsPage
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class EventsViewModel : BaseViewModel
    {

        #region Private Fields
        private const string SessionName = "EventsClient Session";
        private const string SubscriptionName = "EventsClient Subscription";

        private string m_sampleServerUrl;
        private ClientSession m_session;
        private string m_sessionStatusText;
        private string m_operationStatusText;

        private ClientSubscription m_subscription;
        private ClientMonitoredItem m_eventMonitoredItem;
        private readonly ObservableCollection<string> m_eventDataList;

        private bool m_canCreate;
        private bool m_canDelete;
        private int m_eventsCount;
        #endregion

        #region Constructors

        /// <summary>
        /// Create new instance of EventsViewModel
        /// </summary>
        public EventsViewModel()
        {
            Title = "Events sample";
            m_sampleServerUrl = App.DefaultSampleServerUrl;

            m_eventDataList = new ObservableCollection<string>();
            CanCreate = true;
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// SampleServer Url
        /// </summary>
        public string SampleServerUrl
        {
            get { return m_sampleServerUrl; }
            set
            {
                if (value != m_sampleServerUrl)
                {
                    //disconnect existing session
                    DisconnectSession();
                    App.DefaultSampleServerUrl = value;
                }
                SetProperty(ref m_sampleServerUrl, value);
            }
        }

        /// <summary>
        /// Text that indicates session status
        /// </summary>
        public string SessionStatusText
        {
            get { return m_sessionStatusText; }
            set { SetProperty(ref m_sessionStatusText, value); }
        }

        /// <summary>
        /// Text that indicates operation status
        /// </summary>
        public string OperationStatusText
        {
            get { return m_operationStatusText; }
            set { SetProperty(ref m_operationStatusText, value); }
        }

        /// <summary>
        /// List of event data received
        /// </summary>
        public ObservableCollection<string> EventDataList
        {
            get { return m_eventDataList; }
        }

        /// <summary>
        /// Flag that indicates if Monitored item can be created
        /// </summary>
        public bool CanCreate
        {
            get { return m_canCreate && !IsBusy; }
            set { SetProperty(ref m_canCreate, value); }
        }

        /// <summary>
        /// Flag that indicates if Monitored item can be deleted
        /// </summary>
        public bool CanDelete
        {
            get { return m_canDelete && !IsBusy; }
            set { SetProperty(ref m_canDelete, value); }
        }
        /// <summary>
        /// Flag that indicates if view is busy
        /// </summary>
        public new bool IsBusy
        {
            get { return base.IsBusy; }
            set
            {
                base.IsBusy = value;
                OnPropertyChanged("CanCreate");
                OnPropertyChanged("CanDelete");
            }
        }

        /// <summary>
        /// Events count in a session
        /// </summary>
        public int EventsCount
        {
            get { return m_eventsCount; }
            set { SetProperty(ref m_eventsCount, value); }
        }
        #endregion

        #region Event Monitored Item Methods

        /// <summary>
        /// Creates the event monitored item.
        /// </summary>
        public void CreateEventMonitoredItem()
        {
            EventsCount = 0;
            EventDataList.Clear();
            //try to initialize session
            InitializeSession();
            if (m_session == null)
            {
                OperationStatusText = "CreateEventMonitoredItem no session available.";
                return;
            }
            if (m_eventMonitoredItem != null)
            {
                OperationStatusText = "EventMonitoredItem already created";
                return;
            }

            try
            {
                CanCreate = false;
                //create evenbts filter
                EventFilterEx filter = filter = new EventFilterEx();
                filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.EventId));
                filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.SourceName));
                filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.Message));
                filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.Severity));                

                //ObjectIds.Server BrowsePath: Root\Objects\Server
                m_eventMonitoredItem = new ClientMonitoredItem(m_subscription, ObjectIds.Server, "Sample Event Monitored Item", filter);
                m_eventMonitoredItem.EventsReceived += EventMonitoredItem_EventsReceived;
                OperationStatusText = "Event mi is created.";

                
                CanDelete = true;
            }
            catch (Exception e)
            {
                CanCreate = true;
                CanDelete = false;
                OperationStatusText = "CreateEventMonitoredItem error:" + e.Message;
            }
        }

        /// <summary>
        /// Deletes the event monitored item.
        /// </summary>
        public void DeleteEventMonitoredItem()
        {
            //try to initialize session
            InitializeSession();
            if (m_session == null)
            {
                OperationStatusText = "DeleteEventMonitoredItem no session available.";
                return;
            }
            if (m_eventMonitoredItem == null)
            {
                OperationStatusText = "EventMonitoredItem already deleted";
                return;
            }
            try
            {
                CanDelete = false;
                //delete event monitored item
                m_eventMonitoredItem.EventsReceived -= EventMonitoredItem_EventsReceived;
                m_eventMonitoredItem.Delete();
                m_eventMonitoredItem = null;
                OperationStatusText = "Event mi was deleted.";

                CanCreate = true;
                
            }
            catch (Exception ex)
            {
                CanCreate = false;
                CanDelete = true;
                OperationStatusText = "DeleteEventMonitoredItem Error: {0}" + ex.Message;
            }
        }

        #endregion

        #region Initialize & DisconnectSession

        /// <summary>
        /// Initialize session object
        /// </summary>
        public void InitializeSession()
        {
            IsBusy = true;
            if (m_session != null && m_session.CurrentState == State.Disconnected)
            {
                m_session.Dispose();
                m_session = null;
            }
            if (m_session == null)
            {
                try
                {
                    // create the session object with no security and anonymous login    
                    m_session = SampleApplication.UaApplication.CreateSession(SampleServerUrl);
                    m_session.SessionName = SessionName;

                    m_session.Connect(false, true);

                    //create the subscription
                    m_subscription = new ClientSubscription(m_session, SubscriptionName);

                    // set the Publishing interval for this subscription
                    m_subscription.PublishingInterval = 500;
                    SessionStatusText = "Connected";
                }
                catch (Exception ex)
                {
                    SessionStatusText = "Not connected - CreateSession Error: " + ex.Message;

                    if (m_session != null)
                    {
                        m_session.Dispose();
                        m_session = null;
                    }
                    m_subscription = null;
                }
            }
            IsBusy = false;
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public void DisconnectSession()
        {
            SessionStatusText = "";
            //disconnect subscription

            if (m_session == null)
            {
                SessionStatusText = "The Session was not created.";
                return;
            }
            try
            {
                if (m_subscription != null)
                {
                    m_subscription.Disconnect(true);
                    m_subscription.Delete();
                    m_subscription = null;
                }
                m_session.Disconnect(true);
                m_session.Dispose();
                m_session = null;

                SessionStatusText = "Disconnected";
            }
            catch (Exception ex)
            {
                SessionStatusText = "DisconnectSession Error: " + ex.Message;
            }
        }

        #endregion

        #region Public Override Methods
        /// <summary>
        /// Perform operations required when closing a view
        /// </summary>
        public override void Close()
        {
            DisconnectSession();
            base.Close();
        }
        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Notification event of the eventMonitoredItem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventsNotificationEventArgs"/> instance containing the event data.</param>
        private void EventMonitoredItem_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
            foreach (var eventNotification in e.EventNotifications)
            {
                IList<SelectOperandEx> listOfOperands = ((EventFilterEx)m_eventMonitoredItem.Filter).SelectOperandList;
                
                StringBuilder displayNotification = new StringBuilder();
                for (int i = 0; i < listOfOperands.Count; i++)
                {
                    displayNotification.AppendFormat("{0}:{1}\n",
                        listOfOperands[i].PropertyName.Name,
                        eventNotification.EventFields[i]);
                }
                EventDataList.Insert(0, displayNotification.ToString().Trim());
                if (EventDataList.Count > MonitoredItemViewModel.MaxEventDataListCount)
                {
                    EventDataList.RemoveAt(EventDataList.Count - 1);
                }
                EventsCount = EventsCount + 1;
            }
        }

        #endregion
    }
}
