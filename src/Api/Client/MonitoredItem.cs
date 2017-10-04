using Opc.Ua.Toolkit.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{ /// <summary>
  /// Represents an object used for monitoring value changes and events.
  /// The object is used for cyclic update of a server node or for receiving server events.
  /// </summary>
  /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/*' />
    public class MonitoredItem : BaseStateManagement
    {
        #region Fields

        private NodeId m_nodeId;
        private AttributeId m_attributeId;
        private bool m_discardOldest;
        private uint m_queueSize;
        private string m_indexRange;
        private string m_displayName;
        private double m_samplingInterval;
        private MonitoringFilter m_filter;
        private bool m_connectedIsSampling;
        private DataValue m_lastValue;
        private StatusCode m_error = new StatusCode(StatusCodes.Good);
        private ValueRanks m_valueRank = ValueRanks.Scalar;
        private object m_handle;

        // Toolkit members
        private Opc.Ua.Client.MonitoredItem m_monitoredItem;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class for receiving data change notifications.
        /// </summary>
        /// <param name="subscription">The subscription used to receive notifications.</param>
        /// <param name="nodeId">The NodeId of the monitored node.</param>
        /// <param name="attributeId">The AttributeId of the monitored node.</param>
        /// <param name="indexRange">The IndexRange.</param>
        /// <param name="displayName">The local name of the item.</param>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/constructor[@name="MonitoredItem1"]/*' />
        public MonitoredItem(Subscription subscription, NodeId nodeId, AttributeId attributeId, string indexRange, string displayName)
            : base(subscription)
        {
            if (subscription == null || nodeId == null)
            {
                throw new ArgumentNullException();
            }

            if (subscription.Session == null)
            {
                throw new ArgumentNullException("subscription.Parent");
            }

            m_queueSize = subscription.Session.ApplicationConfiguration.DefaultEventMiQueueSize;
            m_discardOldest = false;
            m_samplingInterval = subscription.Session.ApplicationConfiguration.DefaultMiSamplingInterval;

            m_nodeId = nodeId;
            m_attributeId = attributeId;
            m_indexRange = indexRange;
            m_displayName = displayName;

            m_lastValue = null;
            ValueRank = ValueRanks.Scalar;

            subscription.AddItem(this);

            if (!nodeId.IsNullNodeId && subscription.TargetState != State.Disconnected)
            {
                try
                {
                    Connect(false, subscription.TargetState == State.Active);
                }
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.MonitoredItem +Attr", ex, "Connect error: ");
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class for receiving event notifications.
        /// </summary>
        /// <param name="subscription">The subscription used to receive notifications.</param>
        /// <param name="nodeId">The NodeId of the monitored node</param>
        /// <param name="displayName">The local name of the item.</param>
        /// <param name="eventFilter">The event filter.</param>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/constructor[@name="MonitoredItem2"]/*' />
        public MonitoredItem(Subscription subscription, NodeId nodeId, string displayName, ExtendedEventFilter eventFilter)
            : base(subscription)
        {
            if (subscription == null || nodeId == null)
            {
                throw new ArgumentNullException();
            }
            
            if (subscription.Session == null)
            {
                throw new ArgumentNullException("subscription.Parent");
            }

            m_queueSize = subscription.Session.ApplicationConfiguration.DefaultEventMiQueueSize;
            m_discardOldest = false;
            m_samplingInterval = 0;

            m_nodeId = nodeId;
            m_attributeId = AttributeId.EventNotifier;
            m_displayName = displayName;

            m_lastValue = null;
            ValueRank = ValueRanks.Scalar;

            m_filter = eventFilter;

            if (m_filter == null)
            {
                m_filter = GetDefaultEventFilter();
            }

            subscription.AddItem(this);

            if (!nodeId.IsNullNodeId && subscription.TargetState != State.Disconnected)
            {
                try
                {
                    Connect(false, subscription.TargetState == State.Active);
                }
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.MonitoredItem", ex, "Connect error: ");
                }
            }
        }

        /// <summary>
        /// Constructor used only by the RedundantMonitoredItem class in order to skip initializing/registering the monitored item in the SDK (empty monitored item shell required as it implements the Decorator design pattern).
        /// </summary>
        /// <param name="parent">Parent RedundantSubscription as Subscription.</param>
        protected internal MonitoredItem(Subscription parent) : base(parent) { }

        #endregion Constructors

        #region Public Events

        /// <summary>
        /// This event occurs when data change notifications are received for this item.
        /// </summary>
        public event EventHandler<DataChangesNotificationEventArgs> DataChangesReceived;

        /// <summary>
        /// This event occurs when event notifications are received for this item.
        /// </summary>
        public event EventHandler<EventsNotificationEventArgs> EventsReceived;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Gets or sets the attribute id of the monitored item.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="AttributeId"]/*' />
        public virtual AttributeId AttributeId
        {
            get
            {
                return m_attributeId;
            }
            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("This property can be changed only when in the Disconnected state", StatusCodes.BadInvalidState);
                }

                ResetDataType();
                m_attributeId = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the monitored item from the server should discard oldest value or not (discard the newest), in case of a client/server communication problem,
        /// if the item's value queue fills up (see <see cref="QueueSize"/> for configuring the MonitoredItem server queue size).<br/>
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="DiscardOldest"]/*' />
        public virtual bool DiscardOldest
        {
            get
            {
                return m_discardOldest;
            }
            set
            {
                m_discardOldest = value;

                if (CurrentState != State.Disconnected)
                {
                    try
                    {
                        m_monitoredItem.DiscardOldest = value;
                        Subscription.CoreSubscription.ModifyItems();
                        LogErrorsForBadStatusCodes(false, Error);
                    }
                    catch (Exception e)
                    {
                        throw new BaseException($"DiscardOldest property update error: {e.Message}", e);
                    }
                }

                SetModified();
            }
        }

        /// <summary>
        /// Gets or sets the index range.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="IndexRange"]/*' />
        public virtual string IndexRange
        {
            get
            {
                return m_indexRange;
            }
            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("This property can be changed only when in the Disconnected state", StatusCodes.BadInvalidState);
                }

                m_indexRange = value;
            }
        }

        /// <summary>
        /// Gets or sets the monitoring filter. The changes made on this filter remain on the client side, until the ApplyFilter method is called, or the monitored item is connected (again).
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="Filter"]/*' />
        public virtual MonitoringFilter Filter
        {
            get
            {
                return m_filter;
            }

            set
            {
                // validate filter
                if (m_filter is EventFilter)
                {
                    if (!(value is EventFilter))
                    {
                        throw new BaseException("Filter property update error: EventFilter type expected");
                    }
                }

                m_filter = value;

                SetModified();
            }
        }

        /// <summary>
        /// Gets or sets the NodeId of the monitored node.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="NodeId"]/*' />
        public virtual NodeId NodeId
        {
            get
            {
                return m_nodeId;
            }
            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("This property can be changed only when in the Disconnected state", StatusCodes.BadInvalidState);
                }

                if (value == null)
                {
                    throw new ArgumentNullException("NodeId");
                }

                ResetDataType();
                m_nodeId = value;
            }
        }

        /// <summary>
        /// Gets or sets the size of the queue.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="QueueSize"]/*' />
        public virtual uint QueueSize
        {
            get
            {
                return m_queueSize;
            }
            set
            {
                m_queueSize = value;

                if (CurrentState != State.Disconnected)
                {
                    try
                    {
                        m_monitoredItem.QueueSize = value;
                        Subscription.CoreSubscription.ModifyItems();
                        LogErrorsForBadStatusCodes(false, Error);
                    }
                    catch (Exception e)
                    {
                        throw new BaseException($"QueueSize property update error: {e.Message}", e);
                    }
                    finally
                    {
                        SetModified();
                    }
                }

                SetModified();
            }
        }

        /// <summary>
        /// Gets the revised size of the queue.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="RevisedQueueSize"]/*' />
        public virtual uint RevisedQueueSize
        {
            get
            {
                if (m_monitoredItem != null)
                {
                    if (m_monitoredItem.Status != null)
                    {
                        return m_monitoredItem.Status.QueueSize;
                    }

                    return m_monitoredItem.QueueSize;
                }

                return m_queueSize;
            }
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        public virtual string DisplayName
        {
            get
            {
                return m_displayName;
            }
            set
            {
                m_displayName = value;

                if (CurrentState != State.Disconnected)
                {
                    m_monitoredItem.DisplayName = value;
                }

                SetModified();
            }
        }

        /// <summary>
        /// Gets the error condition associated with the monitored item.
        /// </summary>
        public virtual StatusCode Error
        {
            get
            {
                if (m_monitoredItem != null)
                {
                    if (m_monitoredItem.Status.Error != null)
                    {
                        return m_monitoredItem.Status.Error.Code;
                    }

                    return new StatusCode(StatusCodes.Good);
                }

                return m_error;
            }
        }

        /// <summary>
        /// Gets or sets the sampling interval.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="SamplingInterval"]/*' />
        public virtual double SamplingInterval
        {
            get
            {
                return m_samplingInterval;
            }
            set
            {
                m_samplingInterval = value;

                if (CurrentState != State.Disconnected)
                {
                    try
                    {
                        //todo investrigate why type is double
                        m_monitoredItem.SamplingInterval = (int)value;
                        Subscription.CoreSubscription.ModifyItems();
                        LogErrorsForBadStatusCodes(false, Error);
                        CheckRevisedValues();
                    }
                    catch (Exception e)
                    {
                        throw new BaseException($"SamplingInterval property update error: {e.Message}", e);
                    }
                    finally
                    {
                        SetModified();
                    }
                }

                SetModified();
            }
        }

        /// <summary>
        /// Gets the revised sampling interval.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="RevisedSamplingInterval"]/*' />
        public virtual double RevisedSamplingInterval
        {
            get
            {
                if (m_monitoredItem != null)
                {
                    if (m_monitoredItem.Status != null)
                    {
                        return m_monitoredItem.Status.SamplingInterval;
                    }

                    return m_monitoredItem.SamplingInterval;
                }

                return m_samplingInterval;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the MonitoredItem is Sampling or not when in connected state.
        /// </summary>
        public virtual bool ConnectedIsSampling
        {
            get
            {
                return m_connectedIsSampling;
            }

            set
            {
                if (CurrentState == State.Connected)
                {
                    // reconstruct monitoring mode from target state
                    Opc.Ua.MonitoringMode monitoringMode = value ? Opc.Ua.MonitoringMode.Sampling : Opc.Ua.MonitoringMode.Disabled;

                    if (m_monitoredItem.MonitoringMode != monitoringMode)
                    {
                        Subscription.CoreSubscription.SetMonitoringMode(monitoringMode,
                            new Opc.Ua.Client.MonitoredItem[] { m_monitoredItem });
                    }
                }

                m_connectedIsSampling = value;
                SetModified();
            }
        }

        /// <summary>
        /// Gets the subscription used to receive notifications.
        /// </summary>
        public virtual Subscription Subscription
        {
            get
            {
                return Parent as Subscription;
            }
        }

        /// <summary>
        /// Gets the last value of the monitored node.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="LastValue"]/*' />
        public virtual DataValue LastValue
        {
            get
            {
                return m_lastValue;
            }

            internal set
            {
                m_lastValue = value;
            }
        }

        /// <summary>
        /// Gets the client handle of the monitored item.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="ClientHandle"]/*' />
        public virtual uint ClientHandle
        {
            get
            {
                if (m_monitoredItem != null)
                {
                    return m_monitoredItem.ClientHandle;
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets the server handle of the monitored item. Represents the Server-assigned identifier for the monitored item.
        /// </summary>
        public virtual uint ServerHandle
        {
            get
            {
                if (m_monitoredItem != null && m_monitoredItem.Status != null)
                {
                    return m_monitoredItem.Status.Id;
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets the data type of the monitored node.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="DataType"]/*' />
        public virtual NodeId DataType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value rank of the monitored node.
        /// </summary>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/property[@name="ValueRank"]/*' />
        public virtual ValueRanks ValueRank
        {
            get { return m_valueRank; }
            private set { m_valueRank = value; }
        }

        #endregion Public Properties

        #region Internal Properties

        /// <summary>
        /// Gets the Core Monitored Item.
        /// </summary>
        internal virtual Opc.Ua.Client.MonitoredItem CoreMonitoredItem
        {
            get
            {
                return m_monitoredItem;
            }
        }

        internal object Handle
        {
            get
            {
                return m_handle;
            }

            set
            {
                m_handle = value;
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

        #region Read

        /// <summary>
        /// Reads the specified monitored items from server. Optimized read for a list of monitored items. The read is performed as a single read on each session with multiple read requests.
        /// The read requests are grouped by the sessions that own the monitored items. The read values are stored in the LastValue property of each monitored item.
        /// </summary>
        /// <param name="monitoredItems">The monitored items.</param>
        /// <returns>A dictionary containing a service result status code for each failed session read.
        /// The monitored item list passed as parameter can contain monitored items from different sessions. Some grouping is made on session and
        /// for each group of monitored items a read is called.If the read for a session has a service result different from Good, the service result is registered in the dictionary. </returns>
        public static Dictionary<Session, StatusCode> Read(IEnumerable<MonitoredItem> monitoredItems)
        {
            if (monitoredItems == null)
            {
                throw new System.ArgumentNullException("monitoredItems");
            }

            List<MonitoredItem> monitoredItemsToRead = new List<MonitoredItem>(monitoredItems);

            foreach (var mi in monitoredItemsToRead)
            {
                if (mi.CurrentState == State.Disconnected)
                {
                    throw new BaseException("Cannot Read while in the Disconnected state", StatusCodes.BadInvalidState);
                }
            }

            var sessions = monitoredItemsToRead.GroupBy(t => t.Subscription.Session);
            Dictionary<Session, StatusCode> returnDictionary = new Dictionary<Session, StatusCode>(sessions.Count());
            List<ReadValueId> readValueIds = null;

            foreach (var group in sessions)
            {
                readValueIds = new List<ReadValueId>(group.Count());

                foreach (var mi in group)
                {
                    ReadValueId readValueId = new ReadValueId();
                    readValueId.AttributeId = (uint)mi.AttributeId;
                    readValueId.NodeId = mi.NodeId;
                    readValueId.IndexRange = mi.IndexRange;
                    readValueIds.Add(readValueId);
                }

                try
                {
                    var dataValues = group.Key.Read(readValueIds, 0, TimestampsToReturn.Both);
                    if (dataValues.Count == group.Count())
                    {
                        for (int i = 0; i < dataValues.Count; i++)
                        {
                            var mi = group.ElementAt(i);

                            mi.LastValue = dataValues[i];
                        }
                    }
                }
                catch (BaseException ex)
                {
                    returnDictionary.Add(group.Key, ex.StatusCode);
                }
                catch (Exception)
                {
                    returnDictionary.Add(group.Key, new StatusCode(0x80000000));
                }
            }
            return returnDictionary;
        }

        /// <summary>
        /// Reads the monitored attribute, DataType and ValueRank for the specified MonitoredItems.
        /// </summary>
        /// <param name="monitoredItems">The MonitoredItems.</param>
        public static void ReadDatatype(IEnumerable<MonitoredItem> monitoredItems)
        {
            if (monitoredItems == null)
            {
                throw new System.ArgumentNullException("monitoredItems");
            }

            foreach (var mi in monitoredItems)
            {
                if (mi.CurrentState == State.Disconnected)
                {
                    throw new BaseException("Cannot Read while in the Disconnected state", StatusCodes.BadInvalidState);
                }
            }

            Dictionary<Session, List<MonitoredItem>> sessions = new Dictionary<Session, List<MonitoredItem>>();

            foreach (var mi in monitoredItems)
            {
                if (mi.Subscription != null && mi.Subscription.Session != null)
                {
                    List<MonitoredItem> readValues;
                    if (!sessions.TryGetValue(mi.Subscription.Session, out readValues))
                    {
                        readValues = sessions[mi.Subscription.Session] = new List<MonitoredItem>();
                    }

                    readValues.Add(mi);
                }
            }

            foreach (var pair in sessions)
            {
                List<ReadValueId> readValueIds = new List<ReadValueId>(pair.Value.Count * 3);

                foreach (var mi in pair.Value)
                {
                    readValueIds.Add(new ReadValueId()
                    {
                        NodeId = mi.NodeId,
                        AttributeId = (uint)mi.AttributeId,
                        IndexRange = mi.IndexRange
                    });
                    readValueIds.Add(new ReadValueId()
                    {
                        NodeId = mi.NodeId,
                        AttributeId = (uint)AttributeId.DataType
                    });
                    readValueIds.Add(new ReadValueId()
                    {
                        NodeId = mi.NodeId,
                        AttributeId = (uint)AttributeId.ValueRank
                    });
                }

                try
                {
                    var dataValues = pair.Key.Read(readValueIds, 0, TimestampsToReturn.Both);

                    if (dataValues.Count == pair.Value.Count * 3)
                    {
                        for (int i = 0; i < pair.Value.Count; i++)
                        {
                            MonitoredItem mi = pair.Value[i];

                            mi.LastValue = dataValues[i * 3];
                            mi.DataType = (NodeId)dataValues[(i * 3) + 1].Value;
                            mi.ValueRank = (ValueRanks)dataValues[(i * 3) + 2].Value;

                            if (mi.LastValue != null && mi.DataType != null)
                            {
                                mi.LastValue.Value = TryConvertToEnumValue(mi.LastValue.Value, mi.DataType, mi.ValueRank, mi.Subscription.Session);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.ReadDatatype", ex);
                }
            }
        }

        /// <summary>
        /// Reads the value of the monitored attribute from the server.
        /// </summary>
        /// <returns>A <see cref="DataValue"/> that represents the last value measured for this Monitored Item.</returns>
        public virtual DataValue Read()
        {
            if (CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot Read while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            if (m_nodeId == null)
            {
                return null;
            }

            ReadValueId readValueId = new ReadValueId()
            {
                NodeId = m_nodeId,
                AttributeId = (uint)m_attributeId,
                IndexRange = m_indexRange
            };

            m_lastValue = Subscription.Session.Read(readValueId, DataType, ValueRank);

            return m_lastValue;
        }

        /// <summary>
        /// Reads the monitored attribute, DataType and ValueRank for the MonitoredItem.
        /// </summary>
        public virtual void ReadDatatype()
        {
            if (CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot Read while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            if (m_nodeId == null)
            {
                return;
            }

            List<ReadValueId> readValueIds = new List<ReadValueId>(3);
            readValueIds.Add(new ReadValueId()
            {
                NodeId = m_nodeId,
                AttributeId = (uint)m_attributeId,
                IndexRange = m_indexRange
            }
            );
            readValueIds.Add(new ReadValueId()
            {
                NodeId = m_nodeId,
                AttributeId = (uint)AttributeId.DataType
            });
            readValueIds.Add(new ReadValueId()
            {
                NodeId = m_nodeId,
                AttributeId = (uint)AttributeId.ValueRank
            });

            IList<DataValue> dataValues = Subscription.Session.Read(readValueIds, 0, TimestampsToReturn.Both);

            if (dataValues.Count == 3)
            {
                m_lastValue = dataValues[0];
                DataType = (NodeId)dataValues[1].Value;
                if (dataValues[2].Value != null)
                {
                    ValueRank = (ValueRanks)dataValues[2].Value;
                }

                if (m_lastValue != null && DataType != null)
                {
                    //todo refactor for enum values
                    //m_lastValue.TryConvertToEnumValue(DataType, ValueRank, Subscription.Session);
                }
            }
        }

        #endregion Read

        #region Write

        /// <summary>
        /// Writes the specified data value for the monitored attribute in the server.
        /// </summary>
        /// <param name="dataValue">The value to write.</param>
        /// <returns>A numeric code <see cref="StatusCode.Code"/> that describes the result of the write.</returns>
        /// <include file='Doc\Client\MonitoredItem.xml' path='class[@name="MonitoredItem"]/method[@name="Write"]/*'/>
        public virtual StatusCode Write(DataValue dataValue)
        {
            if (CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot Write while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            WriteValue writeValue = new WriteValue();
            writeValue.AttributeId = (uint)m_attributeId;
            writeValue.NodeId = m_nodeId;
            writeValue.Value = dataValue;
            writeValue.IndexRange = m_indexRange;

            return Subscription.Session.Write(writeValue);
        }

        #endregion Write

        /// <summary>
        /// Deletes this instance. The monitored item is removed from the parent subscription.
        /// </summary>
        public virtual void Delete()
        {
            Disconnect(false);

            // possible to call this multiple times on a fast delete Bug TT 745
            if (Subscription != null)
            {
                Subscription.RemoveItem(this);
            }

            Parent = null;
        }

        /// <summary>
        /// Modifies the filter on the server for this monitored item. The filter that it is applied is taken from the Filter property of this monitored item.
        /// </summary>
        public virtual void ApplyFilter()
        {
            if (CurrentState == State.Disconnected)
            {
                throw new BaseException("The filter cannot be changed in the Disconnected state", StatusCodes.BadInvalidState);
            }

            if (m_filter != null)
            {
                try
                {
                    m_monitoredItem.Filter = m_filter;
                    Subscription.CoreSubscription.ModifyItems();
                    LogErrorsForBadStatusCodes(false, Error);
                }
                catch (Exception e)
                {
                    throw new BaseException($"Filter property update error: {e.Message}", e);
                }

                if (m_monitoredItem.Status.Error != null)
                {
                    throw new BaseException($"Filter property update error: {m_monitoredItem.Status.Error.StatusCode}",  (uint)m_monitoredItem.Status.Error.StatusCode);
                }
            }
        }

        #endregion Public Methods

        #region Internal Methods

        internal void CheckRevisedValues()
        {
            if (m_monitoredItem.Status.SamplingInterval == double.NaN)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.SamplingInterval", "RevisedSamplingInterval is a NaN.");
            }
            else if (m_monitoredItem.Status.SamplingInterval < -1)
            {
                TraceService.Log(TraceMasks.Information, TraceSources.ClientAPI, "MonitoredItem.SamplingInterval", 
						"RevisedSamplingInterval is a negative value: {0}.", m_monitoredItem.Status.SamplingInterval);
            }
        }

        /// <summary>
        /// Perform internal initialization/actions associated with connection/activation of the internal SDK monitored item object.
        /// </summary>
        /// <param name="targetState">Target state to advance to.</param>
        /// <param name="reconnecting">Whether this is a reconnecting call or not.</param>
        internal override void InternalConnect(State targetState, bool reconnecting)
        {
            try
            {
                // reconstruct monitoring mode from target state
                Opc.Ua.MonitoringMode monitoringMode = (targetState == State.Active ? Opc.Ua.MonitoringMode.Reporting : (m_connectedIsSampling ? Opc.Ua.MonitoringMode.Sampling : Opc.Ua.MonitoringMode.Disabled));

                InternalCreateCore(targetState, true);

                CheckRevisedValues();

                if (m_monitoredItem.Status.Error != null)
                {
                    m_error = m_monitoredItem.Status.Error.StatusCode;
                    LogErrorsForBadStatusCodes(false, Error);
                }
                else
                {
                    m_error = new StatusCode(StatusCodes.Good);
                }

                if (m_monitoredItem.Status.Id == 0)
                {
                    Subscription.CoreSubscription.RemoveItem(m_monitoredItem);

                    m_monitoredItem = null;

                    if (m_error == null || StatusCode.IsGood(m_error))
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.InternalConnect", "Server sent Monitored Item ID 0 for an item with Good StatusCode.");
                    }

                    throw new BaseException("Error creating monitored item on the server." + m_error);
                }

                if (m_monitoredItem.MonitoringMode != monitoringMode)
                {
                    List<Opc.Ua.ServiceResult> errors = Subscription.CoreSubscription.SetMonitoringMode(monitoringMode,
                                                                       new Opc.Ua.Client.MonitoredItem[] { m_monitoredItem });

                    if (errors != null && errors.Count > 0)
                    {
                        m_error = errors[0].StatusCode;
                        LogErrorsForBadStatusCodes(true, m_error);
                    }
                }
            }            
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.InternalConnect", ex);
                throw new BaseException("MonitoredItem Connect error", ex);
            }
        }

        /// <summary>
        /// Perform internal actions associated with disconnection of the internal SDK monitored item object.
        /// </summary>
        /// <param name="reconnecting">Whether this is a reconnecting call or not.</param>
        internal override void InternalDisconnect(bool reconnecting)
        {
            if (!reconnecting)
            {
                try
                {
                    if (Subscription.CurrentState != State.Disconnected)
                    {
                        Subscription.CoreSubscription.RemoveItem(m_monitoredItem);
                        Subscription.CoreSubscription.ApplyChanges();
                        LogErrorsForBadStatusCodes(false, Error);
                    }
                }                
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.Disconnect", ex);

                    throw new BaseException("MonitoredItem Disconnect error", ex);
                }

                m_monitoredItem = null;
                m_error = new StatusCode(StatusCodes.Good);
            }
        }

        /// <summary>
        /// Creates and initializes the SDK monitored item. This creates the server-side monitored item object.
        /// </summary>
        /// <param name="targetState">The monitored item`s target state.</param>
        /// <param name="applyChanges">Whether to apply subscription changes immediately (single item transaction) or not (multiple item transaction later on when ApplyChanges is called).</param>
        /// <returns>True if a new instance of core monitored item was created.</returns>
        internal bool InternalCreateCore(State targetState, bool applyChanges)
        {
            bool createNew = false;
            // if current state is disconnected
            if (m_monitoredItem == null || (Subscription != null && Subscription.CoreSubscription != m_monitoredItem.Subscription))
            {
                createNew = true;

                // reconstruct monitoring mode from target state
                Opc.Ua.MonitoringMode monitoringMode = (targetState == State.Active ? Opc.Ua.MonitoringMode.Reporting : (m_connectedIsSampling ? Opc.Ua.MonitoringMode.Sampling : Opc.Ua.MonitoringMode.Disabled));

                m_monitoredItem = new Opc.Ua.Client.MonitoredItem();
                m_monitoredItem.StartNodeId = m_nodeId;
                m_monitoredItem.AttributeId = (uint)m_attributeId;
                m_monitoredItem.MonitoringMode = monitoringMode;
                m_monitoredItem.QueueSize = m_queueSize;
                m_monitoredItem.DiscardOldest = m_discardOldest;
                m_monitoredItem.SamplingInterval = (int) m_samplingInterval;
                if (m_filter != null)
                {
                    m_monitoredItem.Filter = m_filter;
                }
                m_monitoredItem.IndexRange = m_indexRange;
                m_monitoredItem.DisplayName = m_displayName;
                m_monitoredItem.Notification += OnNotification;

                Subscription.CoreSubscription.AddItem(m_monitoredItem);

                if (applyChanges)
                {
                    Subscription.CoreSubscription.ApplyChanges();
                }
            }

            return createNew;
        }

        /// <summary>
        /// Sets the monitored item to the deleted state. Method called after the monitored item was deleted from the server.
        /// </summary>
        internal void InternalDelete()
        {
            lock (StateTransitionSync)
            {
                TargetState = State.Disconnected;
                CurrentState = State.Disconnected;
                m_monitoredItem = null;
                Parent = null;
                m_error = new StatusCode(StatusCodes.Good);
            }
        }

        private void RaiseDataChangesReceived(DataChangesNotificationEventArgs e)
        {
            if (DataChangesReceived == null)
            {
                return;
            }
            try
            {
                foreach (MonitoredItemNotification monitoredItemNotification in e.DataChangeNotifications)
                {
                    if (monitoredItemNotification.Value != null && monitoredItemNotification.Value.ServerTimestamp > DateTime.UtcNow)
                    {
                        TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "MonitoredItem.OnNotification",
                            "WARNING: Received ServerTimestamp {0} is in the future for MonitoredItemId {1}.", 
							monitoredItemNotification.Value.ServerTimestamp.ToLocalTime(), ServerHandle);
                    }
                    if (monitoredItemNotification.Value != null && monitoredItemNotification.Value.SourceTimestamp > DateTime.UtcNow)
                    {
                        TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "MonitoredItem.OnNotification",
                            "ERROR: Received SourceTimestamp {0} is in the future for MonitoredItemId {1}.", 
							monitoredItemNotification.Value.SourceTimestamp.ToLocalTime(), ServerHandle);
                    }                   
                }
                DataChangesReceived(this, e);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.RaiseEventsReceived", ex);
            }           
        }

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
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.RaiseEventsReceived", ex);
            }
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Creates and returns a default event filter.
        /// </summary>
        /// <returns>A <see cref="EventFilter"/> representing the DefaultFilter</returns>
        private static EventFilter GetDefaultEventFilter()
        {
            ExtendedEventFilter filter = filter = new ExtendedEventFilter();

            filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.EventId));
            filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.EventType));
            filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.SourceNode));
            filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.SourceName));
            filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.Time));
            filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.ReceiveTime));
            filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.LocalTime));
            filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.Message));
            filter.AddSelectClause(new NodeId(ObjectTypes.BaseEventType), new QualifiedName(BrowseNames.Severity));

            return filter;
        }

        private void LogErrorsForBadStatusCodes(bool showID, StatusCode error)
        {
            string errorNameText = error.ToString();
            if (showID)
            {
                errorNameText += " for Monitored Item with ID: " + ServerHandle;
            }
            if (StatusCode.IsBad(error))
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "MonitoredItem.InternalConnect", 
					"A Bad Status Code received: {0}." , errorNameText);
            }
            else if (StatusCode.IsUncertain(error))
            {
                TraceService.Log(TraceMasks.Information, TraceSources.ClientAPI, "MonitoredItem.InternalConnect", 
					"An Uncertain Status Code received: {0}.", errorNameText);
            }
        }

        private void ResetDataType()
        {
            DataType = null;
        }

        #region Event Handlers

        private void OnNotification(Opc.Ua.Client.MonitoredItem monitoredItem, Opc.Ua.Client.MonitoredItemNotificationEventArgs e)
        {
            if (e != null)
            {
                // check if is data change notification
                MonitoredItemNotification monitoredItemNotification = e.NotificationValue as MonitoredItemNotification;                

                if (monitoredItemNotification != null)
                {
                    ExtendedMonitoredItemNotification extendedMonitoredItemNotification = new ExtendedMonitoredItemNotification(monitoredItemNotification, this);
                    
                    m_lastValue = monitoredItemNotification.Value;

                    if (DataType != null)
                    {
                        //tiodo refactor enum values
                        // m_lastValue.TryConvertToEnumValue(DataType, ValueRank, Subscription.Session);
                    }

                    RaiseDataChangesReceived(new DataChangesNotificationEventArgs(new List<ExtendedMonitoredItemNotification>() { extendedMonitoredItemNotification }));
                   
                }

                Opc.Ua.EventFieldList fieldList = e.NotificationValue as Opc.Ua.EventFieldList;

                if (fieldList != null)
                {
                    if (EventsReceived != null)
                    {
                        EventsReceived(this, new EventsNotificationEventArgs(new List<EventNotification>() { new EventNotification(fieldList, this) }));
                    }
                }
            }
        }

        #endregion Event Handlers


        /// <summary>
        /// Tries to convert the value of the current data value to the specified enum value with the specified value rank. 
        /// If the conversion is not possible the value is not changed.
        /// </summary>
        /// <param name="enumTypeId">Type of the enum.</param>
        /// <param name="valueRank">The value rank of the enum.</param>
        /// <param name="session">The session.</param>
        private static object TryConvertToEnumValue(object value, NodeId enumTypeId, ValueRanks valueRank, Session session)
        {
            return null;
            //todo refactor
            //if (enumTypeId == null)
            //{
            //    throw new ArgumentNullException("dataType");
            //}
            //if (session == null)
            //{
            //    throw new ArgumentNullException("session");
            //}

            //// check to see if it's an enumerated type
            //if (enumTypeId != null)
            //{
            //    try
            //    {
            //        if (valueRank < 0)
            //        {
            //            if (value is int)
            //            {
            //                EnumValue enumValue = Argument.GetDefaultValueForDatatype(enumTypeId, valueRank, session) as EnumValue;
            //                if (enumValue == null)
            //                {
            //                    Type type = Argument.GetSystemType(enumTypeId, session.Factory);
            //                    if (type != null && type.IsEnum)
            //                    {
            //                        enumValue = new EnumValue(type);
            //                    }
            //                }
            //                if (enumValue != null)
            //                {
            //                    enumValue.Value = (int)value;
            //                    value = enumValue;
            //                }
            //                return enumValue;
            //            }
            //        }
            //        else
            //        {
            //            int[] intValues = value as int[];
            //            if (intValues != null)
            //            {
            //                Type type = Argument.GetSystemType(enumTypeId, session.Factory);
            //                if (type != null && type.IsEnum)
            //                {
            //                    EnumValue[] enumValueArray = new EnumValue[intValues.Length];
            //                    for (int i = 0; i < intValues.Length; i++)
            //                    {
            //                        enumValueArray[i] = new EnumValue(type);
            //                        enumValueArray[i].Value = intValues[i];
            //                    }

            //                    Value = enumValueArray;
            //                }
            //                else
            //                {
            //                    EnumValue enumValue = Argument.GetDefaultValueForDatatype(enumTypeId, ValueRanks.Scalar, session) as EnumValue;
            //                    if (enumValue != null)
            //                    {
            //                        EnumValue[] enumValueArray = new EnumValue[intValues.Length];
            //                        for (int i = 0; i < intValues.Length; i++)
            //                        {
            //                            enumValueArray[i] = enumValue.Clone();
            //                            enumValueArray[i].Value = intValues[i];
            //                        }

            //                        Value = enumValueArray;
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    catch (NotSupportedException ex)
            //    {
            //        TraceService.Log(TraceLevels.Warning, TraceMasks.ClientAPI, "DataValue.ChangeValueToEnumValue", ex.Message, ex);
            //    }
            //}

            //DataType = enumTypeId;
            //ValueRank = valueRank;
        }
        #endregion Private Methods
    }
}
