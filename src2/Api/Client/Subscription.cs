using Opc.Ua.Client;
using Opc.Ua.Toolkit.Trace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using CoreMonitoredItem = Opc.Ua.Client.MonitoredItem;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// The delegate used to receive data change notifications via a direct function call instead of a .NET Event.
    /// </summary>
    //public delegate void FastDataChangeNotificationEventHandler(Subscription subscription, DataChangeNotification notification, IList<string> stringTable);

    /// <summary>
    /// The delegate used to receive event notifications via a direct function call instead of a .NET Event.
    /// </summary>
   // public delegate void FastEventNotificationEventHandler(Subscription subscription, EventNotificationList notification, IList<string> stringTable);

    /// <summary>
    /// Represents a client subscription instance. It manages an OPC UA subscription and it is used as a container for the monitored items collection.
    /// It creates a cyclic update of a server's node attributes or events through monitored items.
    /// </summary>
    public class Subscription : BaseStateManagement
    {
        #region Fields

        private readonly List<MonitoredItem> m_monitoredItems = new List<MonitoredItem>();
        private readonly ReadOnlyCollection<MonitoredItem> m_readonlyMonitoredItems;
        private int m_publishingInterval;
        private uint m_lifeTimeCount;
        private uint m_maxKeepAliveCount;
        private uint m_maxNotificationsPerPublish;
        private byte m_priority;
        private string m_displayName;
        private TimestampsToReturn m_timestampsToReturn;

        // Toolkit members
        private Opc.Ua.Client.Subscription m_subscription;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class.
        /// </summary>
        /// <param name="session">The parent Session that owns the Subscription.</param>
        /// <param name="displayName">The human readable name of the Subscription.</param>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/constructor[@name="Subscription"]/*' />
        public Subscription(Session session, string displayName)
            : base(session)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            m_readonlyMonitoredItems = new ReadOnlyCollection<MonitoredItem>(m_monitoredItems);

            m_publishingInterval = session.ApplicationConfiguration.DefaultSubscriptionPublishingInterval;
            m_maxKeepAliveCount = session.ApplicationConfiguration.DefaultSubscriptionKeepAliveCount;
            m_lifeTimeCount = session.ApplicationConfiguration.DefaultSubscriptionLifeTimeCount;
            m_maxNotificationsPerPublish = session.ApplicationConfiguration.DefaultSubscriptionMaxNotificationsPerPublish;
            m_priority = session.ApplicationConfiguration.DefaultSubscriptionPriority;
            m_timestampsToReturn = TimestampsToReturn.Both;

            session.AddSubscription(this);

            m_displayName = displayName;

            State sessionTargetState = session.TargetState;

            if (sessionTargetState != State.Disconnected)
            {
                try
                {
                    Connect(false, (sessionTargetState == State.Active));
                }
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.Subscription", ex, "Connect error: ");
                }
            }
        }

        /// <summary>
        /// Constructor used only by the RedundantSubscription class in order to skip initializing/registering the subscription in the SDK (empty subscription shell required as it implements the Decorator design pattern).
        /// </summary>
        /// <param name="parent">Parent RedundantSession as Session.</param>
        protected internal Subscription(Session parent) : base(parent)
        {
            m_readonlyMonitoredItems = new ReadOnlyCollection<MonitoredItem>(m_monitoredItems);
        }

        #endregion Constructors

        #region Public Events

        /// <summary>
        /// This event occurs when data change notifications are received for any owned MonitoredItems.
        /// </summary>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/event[@name="DataChangesReceived"]/*' />
        public event EventHandler<DataChangesNotificationEventArgs> DataChangesReceived;

        /// <summary>
        /// This event occurs when event notifications are received for any owned MonitoredItems.
        /// </summary>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/event[@name="EventsReceived"]/*' />
        public event EventHandler<EventsNotificationEventArgs> EventsReceived;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Returns a reference to the Session that owns this Subscription.
        /// </summary>
        public virtual Session Session
        {
            get
            {
                return Parent as Session;
            }
        }

        /// <summary>
        /// Gets or sets the client-requested publishing interval in milliseconds.
        /// </summary>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/property[@name="PublishingInterval"]/*'/>
        public virtual int PublishingInterval //todo investigate why it was double in the prev version
        {
            get
            {
                return m_publishingInterval;
            }
            set
            {
                if (CurrentState != State.Disconnected)
                {
                    try
                    {
                        //todo check why it is defined as double
                        m_subscription.PublishingInterval = value;
                        m_subscription.Modify();
                    }
                    catch (Exception e)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.PublishingInterval", e, "Subscription modify error: ");
                        throw new BaseException(string.Format("PublishingInterval property update error: {0}", e.Message), e);
                    }
                }

                m_publishingInterval = value;

                SetModified();
            }
        }

        /// <summary>
        /// Gets the server-accepted (revised) publishing interval in milliseconds.<br/>
        /// </summary>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/property[@name="RevisedPublishingInterval"]/*'/>
        public virtual double RevisedPublishingInterval
        {
            get
            {
                if (m_subscription != null)
                {
                    return m_subscription.CurrentPublishingInterval;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the client-requested life time count.
        /// </summary>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/property[@name="LifeTimeCount"]/*'/>
        public virtual uint LifeTimeCount
        {
            get
            {
                return m_lifeTimeCount;
            }
            set
            {
                if (CurrentState != State.Disconnected)
                {
                    try
                    {
                        m_subscription.LifetimeCount = value;
                        m_subscription.Modify();
                    }
                    catch (Exception e)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.LifeTimeCount", e, "Subscription modify error: ");
                        throw new BaseException(string.Format("LifetimeCount property update error: {0}", e.Message), e);
                    }
                }

                m_lifeTimeCount = value;

                SetModified();
            }
        }

        /// <summary>
        /// Gets the server-accepted (revised) life time count.
        /// </summary>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/property[@name="RevisedLifeTimeCount"]/*'/>
        public virtual uint RevisedLifeTimeCount
        {
            get
            {
                if (m_subscription != null)
                {
                    return m_subscription.CurrentLifetimeCount;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the requested maximum keep-alive count.
        /// </summary>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/property[@name="MaxKeepAliveCount"]/*'/>
        public virtual uint MaxKeepAliveCount
        {
            get
            {
                return m_maxKeepAliveCount;
            }
            set
            {
                if (CurrentState != State.Disconnected)
                {
                    try
                    {
                        m_subscription.KeepAliveCount = value;
                        m_subscription.Modify();
                    }
                    catch (Exception e)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.MaxKeepAliveCount",  e, "Subscription modify error: ");
                        throw new BaseException(string.Format("MaxKeepAliveCount property update error: {0}", e.Message), e);
                    }
                }

                m_maxKeepAliveCount = value;

                SetModified();
            }
        }

        /// <summary>
        /// Gets the server-accepted (revised) maximum keep-alive count.
        /// </summary>
        public virtual uint RevisedMaxKeepAliveCount
        {
            get
            {
                if (m_subscription != null)
                {
                    return m_subscription.CurrentKeepAliveCount;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the maximum notifications per publish.
        /// </summary>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/property[@name="MaxNotificationsPerPublish"]/*'/>
        public virtual uint MaxNotificationsPerPublish
        {
            get
            {
                return m_maxNotificationsPerPublish;
            }
            set
            {
                if (CurrentState != State.Disconnected)
                {
                    try
                    {
                        m_subscription.MaxNotificationsPerPublish = value;
                        m_subscription.Modify();
                    }
                    catch (Exception e)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.MaxNotificationsPerPublish", e, "Subscription modify error: ");
                        throw new BaseException(string.Format("MaxNotificationsPerPublish property update error: {0}", e.Message), e);
                    }
                }

                m_maxNotificationsPerPublish = value;

                SetModified();
            }
        }

        /// <summary>
        /// Gets or sets the priority. Indicates the relative priority of the Subscription.
        /// </summary>
        /// <include file='Doc\Client\Subscription.xml' path='class[@name="Subscription"]/property[@name="Priority"]/*'/>
        public virtual byte Priority
        {
            get
            {
                return m_priority;
            }
            set
            {
                if (CurrentState != State.Disconnected)
                {
                    try
                    {
                        m_subscription.Priority = value;
                        m_subscription.Modify();
                    }
                    catch (Exception e)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.Priority", e, "Subscription modify error: ");
                        throw new BaseException(string.Format("Priority property update error: {0}", e.Message), e);
                    }
                }

                m_priority = value;

                SetModified();
            }
        }

        /// <summary>
        /// Gets the subscription ID.<br/>
        /// Represents the server-assigned identifier for the Subscription.
        /// </summary>
        public virtual uint Id
        {
            get
            {
                return m_subscription != null ? m_subscription.Id : 0;
            }
        }

        /// <summary>
        /// Gets or sets the subscription display name.<br/>
        /// Represents the human readable name of the Subscription.
        /// </summary>
        public virtual string DisplayName
        {
            get
            {
                return m_displayName;
            }
            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("This property can be changed only when in the Disconnected state", StatusCodes.BadInvalidState);
                }

                m_displayName = value;
            }
        }

        /// <summary>
        /// Gets the monitored items owned by this Subscription. This is a read-only collection.
        /// </summary>
        public virtual ReadOnlyCollection<MonitoredItem> MonitoredItems
        {
            get
            {
                return m_readonlyMonitoredItems;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating which timestamp values(server/source/both/neither) should be returned in the data changes. If the subscription is not disconnected, the change will be made on the server.
        /// </summary>
        /// <value>
        /// The value of the timestamps to return enumeration.
        /// </value>
        public virtual TimestampsToReturn TimestampsToReturn
        {
            get { return m_timestampsToReturn; }
            set
            {
                if (CurrentState != State.Disconnected)
                {
                    try
                    {
                        m_subscription.TimestampsToReturn = value;
                        //todo investigate how to manage SetModifiedAttributes method missing
                        //lock (((ICollection)m_monitoredItems).SyncRoot)
                        //{
                        //    foreach (var item in m_monitoredItems)
                        //    {
                        //        if (item.CurrentState != State.Disconnected)
                        //        {
                        //            item.CoreMonitoredItem.SetModifiedAttributes(true);
                        //        }
                        //    }
                        //}
                        m_subscription.ApplyChanges();
                    }
                    catch (Exception e)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.TimestampsToReturn", e, "Subscription modify error: ");
                        throw new BaseException(string.Format("TimestampsToReturn property update error: {0}", e.Message), e);
                    }
                }

                m_timestampsToReturn = value;

                SetModified();
            }
        }

        #endregion Public Properties

        #region Internal Properties

        /// <summary>
        /// Gets the Core Subscription.
        /// </summary>
        internal virtual Opc.Ua.Client.Subscription CoreSubscription
        {
            get
            {
                return m_subscription;
            }
        }

        internal bool HasDataChangesReceivedHandler
        {
            get
            {
                return DataChangesReceived != null;
            }
        }

        internal bool HasEventsReceivedHandler
        {
            get
            {
                return EventsReceived != null;
            }
        }
        #endregion Internal Propeties

        #region Public Methods

        /// <summary>
        /// Deletes this Subscription instance and removes it from its parent Session. All its contained MonitoredItems will be also deleted.
        /// </summary>
        public virtual void Delete()
        {
            Disconnect(true);

            if (Session != null)
            {
                Session.RemoveSubscription(this);
            }

            Parent = null;
        }

        /// <summary>
        /// Deletes the specified MonitoredItems from the server. In order to be deleted, the specified MonitoredItems must be children of this Subscription.<br/>
        /// The provided items will be erased in a single server call.
        /// </summary>
        /// <param name="monitoredItems">The list of MonitoredItems to be deleted.</param>
        public virtual void DeleteItems(IEnumerable<MonitoredItem> monitoredItems)
        {
            if (monitoredItems == null)
            {
                throw new ArgumentNullException("monitoredItems");
            }

            List<MonitoredItem> monitoredItemsToDelete = new List<MonitoredItem>(monitoredItems);
            List<CoreMonitoredItem> coreItemsToDelete = new List<CoreMonitoredItem>();

            foreach (var item in monitoredItemsToDelete)
            {
                if (item.CoreMonitoredItem != null)
                {
                    coreItemsToDelete.Add(item.CoreMonitoredItem);
                }
            }

            foreach (var monitoredItem in monitoredItemsToDelete)
            {
                monitoredItem.InternalDelete();

                lock (((ICollection)m_monitoredItems).SyncRoot)
                {
                    m_monitoredItems.Remove(monitoredItem);
                }
            }

            if (coreItemsToDelete.Count > 0 && m_subscription != null)
            {
                try
                {
                    m_subscription.RemoveItems(coreItemsToDelete);
                    m_subscription.ApplyChanges();
                }               
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.DeleteItems", ex);

                    throw new BaseException("Delete monitored items error", ex);
                }
                finally
                {
                    SetModified();
                }

                foreach (CoreMonitoredItem item in coreItemsToDelete)
                {
                    LogErrorsForBadStatusCodes(false, item, "DeleteItems");
                }
            }
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Perform internal initialization/actions associated with connection/activation of the internal SDK subscription object.
        /// </summary>
        /// <param name="targetState">Target state to advance to.</param>
        /// <param name="reconnecting">Whether this is a reconnecting call or not.</param>
        internal override void InternalConnect(State targetState, bool reconnecting)
        {
            try
            {
                // reconstruct active flag from target state
                bool active = (targetState == State.Active);

                if (CurrentState == State.Disconnected)
                {
                    if (m_subscription == null || (Session != null && Session.CoreSession != m_subscription.Session))
                    {
                        m_subscription = new Ua.Client.Subscription();

                        Session.CoreSession.AddSubscription(m_subscription);

                        if (m_displayName != null)
                        {
                            m_subscription.DisplayName = m_displayName;
                        }

                        m_subscription.LifetimeCount = m_lifeTimeCount;
                        m_subscription.KeepAliveCount = m_maxKeepAliveCount;
                        m_subscription.MaxNotificationsPerPublish = m_maxNotificationsPerPublish;
                        m_subscription.PublishingInterval = m_publishingInterval;
                        m_subscription.Priority = m_priority;
                        m_subscription.PublishingEnabled = active;
                        m_subscription.TimestampsToReturn = m_timestampsToReturn;

                        m_subscription.FastDataChangeCallback = new FastDataChangeNotificationEventHandler(OnDataChange);  //new FastDataChangeNotificationEventHandler(OnDataChange);
                        m_subscription.FastEventCallback = new FastEventNotificationEventHandler(OnEventNotification);

                        try
                        {
                            m_subscription.Create();
                        }
                        catch
                        {
                            // Destroy the core subscription if create fails.
                            Session.CoreSession.RemoveSubscription(m_subscription);
                            m_subscription = null;
                            throw;
                        }
                    }

                    // verify duplicate Subscription ID for current session
                    Subscription duplicateSubscription = Session.Subscriptions.FirstOrDefault(s => s.Id != 0 && s.Id.Equals(Id) && s != this);

                    if (duplicateSubscription != null)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.InternalConnect", 
                            "Duplicate Subscription ID for Subscription: {0} and Subscription: {1}", duplicateSubscription.DisplayName, DisplayName);
                    }

                    // add children in bulk
                    lock (((ICollection)m_monitoredItems).SyncRoot)
                    {
                        foreach (var child in m_monitoredItems)
                        {
                            // fix for flat connect after children are disconnected
                            if (child.TargetState != State.Disconnected)
                            {
                                child.InternalCreateCore(targetState, false);
                            }
                        }
                    }

                    m_subscription.ApplyChanges();

                    // find duplicates ID's for MI and show log
                    IEnumerable<uint> allServerHandles;

                    lock (((ICollection)m_monitoredItems).SyncRoot)
                    {
                        allServerHandles = m_monitoredItems.Select(t => { return t.ServerHandle; });
                    }

                    IEnumerable<uint> duplicates = allServerHandles.GroupBy(s => s).Where(s => s.Key != 0 && s.Count() > 1).Select(s => s.Key);

                    if (duplicates.Count() > 0)
                    {
                        foreach (uint value in duplicates)
                        {
                            TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.InternalConnect", "Multiple Monitored Items have same ID: {0}" , value);
                        }
                    }
                }

                if (m_subscription.PublishingEnabled != active)
                {
                    m_subscription.SetPublishingMode(active);
                }
            }
            
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.Connect",ex);
                throw new BaseException("Subscription Connect error", ex);
            }
        }

        /// <summary>
        /// Perform internal actions associated with disconnection of the internal SDK subscription object.
        /// </summary>
        /// <param name="reconnecting">Whether this is a reconnecting call or not.</param>
        internal override void InternalDisconnect(bool reconnecting)
        {
            if (!reconnecting)
            {
                try
                {
                    // When session is disconnected flat the SDK subscriptions are not available for this SDK session.
                    if (m_subscription != null && m_subscription.Session != null && m_subscription.Session.SubscriptionCount > 0)
                    {
                        m_subscription.Session.RemoveSubscription(m_subscription);
                    }
                }
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.Disconnect",  ex);
                    throw new BaseException("Subscription Disconnect error", ex);
                }
                finally
                {
                    if (m_subscription != null)
                    {
                        m_subscription.Dispose();
                    }

                    m_subscription = null;
                }
            }
        }

        /// <summary>
        /// Gets the children list.
        /// </summary>
        /// <returns>A list of MonitoredItems as BaseStateManagement objects.</returns>
        internal override List<BaseStateManagement> GetChildren()
        {
            lock (((ICollection)m_monitoredItems).SyncRoot)
            {
                return new List<BaseStateManagement>(m_monitoredItems.ToArray());
            }
        }

        /// <summary>
        /// Adds a MonitoredItem.
        /// </summary>
        /// <param name="monitoredItem">The MonitoredItem to be added.</param>
        internal virtual void AddItem(MonitoredItem monitoredItem)
        {
            lock (((ICollection)m_monitoredItems).SyncRoot)
            {
                m_monitoredItems.Add(monitoredItem);
            }

            SetModified();
        }

        /// <summary>
        /// Removes a MonitoredItem.
        /// </summary>
        /// <param name="monitoredItem">The MonitoredItem to be added</param>
        internal virtual void RemoveItem(MonitoredItem monitoredItem)
        {
            lock (((ICollection)m_monitoredItems).SyncRoot)
            {
                m_monitoredItems.Remove(monitoredItem);
            }

            SetModified();
        }

        /// <summary>
        /// Raises the DataChangesReceived event.<br/>
        /// This method is only called by the inheriting RedundantSubscription.
        /// </summary>
        /// <param name="e">The received Subscription notification event arguments.</param>
        internal void RaiseDataChangesReceived(DataChangesNotificationEventArgs e)
        {
            if (DataChangesReceived == null)
            {
                return;
            }

            try
            {
                DataChangesReceived(this, e);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.RaiseDataChangesReceived",  ex);
            }
        }

        /// <summary>
        /// Raises the EventsReceived event.<br/>
        /// This method is only called by the inheriting RedundantSubscription.
        /// </summary>
        /// <param name="e">The received Subscription event notification event arguments.</param>
        internal void RaiseEventsReceived(EventsNotificationEventArgs e)
        {
            if (EventsReceived == null)
            {
                return;
            }

            try
            {
                EventsReceived(this, e);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.RaiseEventsReceived",  ex);
            }
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Traces errors for bad status codes associated with certain MonitoredItem operations.
        /// </summary>
        /// <param name="showID">Whether to show the MonitoredItem ID or not.</param>
        /// <param name="item">The MonitoredItem to check the status code for.</param>
        /// <param name="calledInMethod">The method name, as string literal.</param>
        private void LogErrorsForBadStatusCodes(bool showID, CoreMonitoredItem item, string calledInMethod)
        {
            if (item.Status.Error != null)
            {
                StatusCode code = item.Status.Error.StatusCode;
                string errorNameText = code.ToString();

                if (showID)
                {
                    errorNameText += " for Monitored Item with ID: " + item.Status.Id;
                }

                if (StatusCode.IsBad(code))
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription." + calledInMethod, "A Bad Status Code received: {0}", errorNameText);
                }
                else if (StatusCode.IsUncertain(code))
                {
                    TraceService.Log(TraceMasks.Information, TraceSources.ClientAPI, "Subscription." + calledInMethod, "An Uncertain Status Code received: {0}", errorNameText);
                }
            }
        }

        #region Event Handlers

        /// <summary>
        /// Method handling the Core SDK Subscription's FastDataChangeCallback event.
        /// </summary>
        /// <param name="subscription">Core SDK Subscription element.</param>
        /// <param name="dataChangeNotification">Received message (data change).</param>
        /// <param name="stringTable">Message's string table.</param>
        private void OnDataChange(Opc.Ua.Client.Subscription subscription, Opc.Ua.DataChangeNotification dataChangeNotification, IList<string> stringTable)
        {
            try
            {
                if (DataChangesReceived != null && dataChangeNotification != null)
                {
                    List<ExtendedMonitoredItemNotification> extendedMonitoredItemNotifications = new List<ExtendedMonitoredItemNotification>();
                    //todo refasctor  this code
                    foreach (MonitoredItemNotification monitoredItemNotification in dataChangeNotification.MonitoredItems)
                    {
                        MonitoredItem monitoredItem = null;

                        lock (((ICollection)m_monitoredItems).SyncRoot)
                        {
                            monitoredItem = m_monitoredItems.Find(item => item.ClientHandle == (monitoredItemNotification.ClientHandle));
                        }

                        if (monitoredItem != null)
                        {
                            extendedMonitoredItemNotifications.Add( new ExtendedMonitoredItemNotification(monitoredItemNotification, monitoredItem));                            
                        }
                    }

                    DataChangesReceived(this, new DataChangesNotificationEventArgs(extendedMonitoredItemNotifications));
                }
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.OnDataChange",  ex);
            }
        }

        /// <summary>
        /// Method handling the Core SDK Subscription's FastEventCallback event.
        /// </summary>
        /// <param name="subscription">Core SDK Subscription element.</param>
        /// <param name="message">Received message (event).</param>
        /// <param name="stringTable">Message's string table.</param>
        private void OnEventNotification(Opc.Ua.Client.Subscription subscription, EventNotificationList message, IList<string> stringTable)
        {
            try
            {
                if (EventsReceived != null && message != null)
                {
                    List<EventNotification> eventNotifications = new List<EventNotification>();

                    foreach (EventFieldList eventFieldList in message.Events)
                    {
                        MonitoredItem monitoredItem = null;

                        lock (((ICollection)m_monitoredItems).SyncRoot)
                        {
                            monitoredItem = m_monitoredItems.Find(item => item.ClientHandle == (eventFieldList.ClientHandle));
                        }

                        eventNotifications.Add(new EventNotification(eventFieldList, monitoredItem));
                    }

                    EventsReceived(this, new EventsNotificationEventArgs(eventNotifications));
                }
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Subscription.OnEventNotification", ex);
            }
        }

        #endregion Event Handlers

        #endregion Private Methods
    }
}
