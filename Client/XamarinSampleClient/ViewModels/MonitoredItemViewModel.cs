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
using System.Collections.ObjectModel;
using System.Threading;
using Opc.Ua;
using XamarinSampleClient.Helpers;
using XamarinSampleClient.Models;
using Softing.Opc.Ua.Client;
using Xamarin.Forms;

namespace XamarinSampleClient.ViewModels
{
    /// <summary>
    /// View Model for MonitoredItemPage
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class MonitoredItemViewModel : BaseViewModel
    {
        #region Private Fields
        public static int MaxEventDataListCount = 50;
        // "CTT\\Scalar\\Simulation\\Int64";
        private readonly NodeId m_miInt64NodeId = new NodeId("ns=7;s=Scalar_Simulation_Int64");
        //"Server\\ServerStatus\\CurrentTime";
        private readonly NodeId m_miCurrentTimeNodeId = VariableIds.Server_ServerStatus_CurrentTime;
        private const string SessionName = "MonitoredItemClient Session";
        private const string SubscriptionName = "MonitoredItemClient Subscription";
        private string m_sampleServerUrl;
        private ClientSession m_session;
        private string m_sessionStatusText;
        private string m_operationStatusText;

        private ClientSubscription m_subscription;
        private ClientMonitoredItem m_miInt64;
        private ClientMonitoredItem m_miCurrentTime;
        private readonly ObservableCollection<MonitoredItemEventData> m_eventDataList;

        private bool m_canCreate;
        private bool m_canDelete;
        private int m_eventsCount;
        #endregion

        #region Constructors

        /// <summary>
        /// Create new instance of MonitoredItemViewModel
        /// </summary>
        public MonitoredItemViewModel()
        {
            Title = "Monitored item sample";
            m_sampleServerUrl = App.DefaultSampleServerUrl;
            m_eventDataList = new ObservableCollection<MonitoredItemEventData>();
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
        public ObservableCollection<MonitoredItemEventData> EventDataList
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

        #region Methods

        /// <summary>
        /// Creates a monitoredItem. The monitored item is activated in the constructor if the subscription is active as well.
        /// </summary>
        public void CreateMonitoredItem()
        {
            EventsCount = 0;
            EventDataList.Clear();
            //try to initialize session
            InitializeSession();
            if (m_session == null)
            {
                OperationStatusText = "CreateMonitoredItem no session available.";
                return;
            }
            if (m_miInt64 != null)
            {
                OperationStatusText = "MonitoredItem already created";
                return;
            }
            try
            {
                CanCreate = false;
                //create monitored item for server CurrentTime
                m_miCurrentTime = new ClientMonitoredItem(m_subscription, m_miCurrentTimeNodeId, "Monitored Item Server CurrentTime");
                m_miCurrentTime.DataChangesReceived += Monitoreditem_DataChangesReceived;
                //set sampling interval to 1 second
                m_miCurrentTime.SamplingInterval = 1000;

                //create monitored item for Int64 variable
                m_miInt64 = new ClientMonitoredItem(m_subscription, m_miInt64NodeId, "Monitored Item Int64");
                m_miInt64.DataChangesReceived += Monitoreditem_DataChangesReceived;
                //set sampling interval to 3 seconds
                m_miInt64.SamplingInterval = 3000;

                OperationStatusText = "Monitored items are created.";
                
                CanDelete = true;
            }
            catch (Exception e)
            {
                CanCreate = true;
                CanDelete = false;
                OperationStatusText = "CreateMonitoredItem error:" + e.Message;
            }
        }

        /// <summary>
        /// Deletes the current MonitoredItem.
        /// </summary>
        public void DeleteMonitoredItem()
        {
            //try to initialize session
            InitializeSession();
            if (m_session == null)
            {
                OperationStatusText = "DeleteMonitoredItem no session available.";
                return;
            }
            
            if (m_miInt64 == null || m_miCurrentTime == null)
            {
                OperationStatusText = "MonitoredItems are not created";
                return;
            }
            try
            {
                CanDelete = false;
                m_miCurrentTime.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                m_miCurrentTime.Delete();
                m_miCurrentTime = null;

                m_miInt64.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                m_miInt64.Delete();
                m_miInt64 = null;

                CanCreate = true;                
                OperationStatusText = "Monitored items are deleted.";
            }
            catch (Exception e)
            {
                CanCreate = false;
                CanDelete = true;
                OperationStatusText = "DeleteMonitoredItem error:" + e.Message;
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
        /// Handles the Notification event of the Monitoreditem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataChangesNotificationEventArgs"/> instance containing the event data.</param>
        private void Monitoreditem_DataChangesReceived(object sender, DataChangesNotificationEventArgs e)
        {
            foreach (var dataChangeNotification in e.DataChangeNotifications)
            {
                MonitoredItemEventData miEventData = new MonitoredItemEventData();
                miEventData.SequenceNumber = dataChangeNotification.SequenceNo;
                miEventData.MonitoredItemName = dataChangeNotification.MonitoredItem.DisplayName;
                miEventData.Value = dataChangeNotification.Value.ToString();
                miEventData.StatusCode = dataChangeNotification.Value.StatusCode.ToString();
                miEventData.ServerTimeStamp = dataChangeNotification.Value.ServerTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt");
                miEventData.SourceTimestamp = dataChangeNotification.Value.SourceTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt");

                Device.BeginInvokeOnMainThread(() =>
                {
                    //add event data at top of the list
                    EventDataList.Insert(0, miEventData);
                    if (EventDataList.Count > MonitoredItemViewModel.MaxEventDataListCount)
                    {
                        EventDataList.RemoveAt(EventDataList.Count - 1);
                    }
                    EventsCount = EventsCount + 1;
                });
            }
        }

        #endregion
    }
}