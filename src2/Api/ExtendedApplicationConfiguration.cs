using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// Application configuration that contains extra settings neede by toolkit  
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaConfig)]
    public class ExtendedApplicationConfiguration : ApplicationConfiguration
    {
        /// <summary>
        /// Create new instance of ExtendedApplicationConfiguration
        /// </summary>
        public ExtendedApplicationConfiguration()
        {
            // set default values            
            SessionReconnectDelay = 5000;
            DefaultKeepAliveInterval = 5000;
            DefaultKeepAliveTimeout = 10000;
            DiscoveryOperationTimeout = 5000;

            DefaultSessionTimeout = 0;

            DefaultSubscriptionPublishingInterval = 1000;
            DefaultSubscriptionKeepAliveCount = 10;
            DefaultSubscriptionLifeTimeCount = 1000;
            DefaultSubscriptionMaxNotificationsPerPublish = 0;
            DefaultSubscriptionPriority = 255;
            DefaultMiSamplingInterval = 1000;
            DefaultMiQueueSize = 1;
            DefaultEventMiQueueSize = 0;
            DefaultValuesLogCacheSize = 1000;

            MaxDisplayedArraySize = 10000;
            DisplayPicoSeconds = false;
        }

        #region Properties
        /// <summary>
        /// Gets or sets the discovery operation timeout.
        /// </summary>
        /// <value>
        /// The discovery operation timeout.
        /// </value>
        [DataMember(IsRequired = false, Order = 13)]
        public int DiscoveryOperationTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default KeepAliveInterval used when creating a session object.
        /// This represents how frequently the server is pinged to see if communication is still working.
        /// </summary>
        /// <value>
        /// The default KeepAliveInterval.
        /// </value>
        [DataMember(IsRequired = false, Order = 14)]
        public int DefaultKeepAliveInterval
        {
            get;
            set;
        }

        /// <summary>
        ///  Gets or sets the default waiting time (in milliseconds) for KeepAlive read requests to return from the server.
        /// </summary>
        /// <value>
        /// The default DefaultKeepAliveTimeout.
        /// </value>
        [DataMember(IsRequired = false, Order = 15)]
        public uint DefaultKeepAliveTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the session reconnect delay.
        /// The period of time at which the Session will do a reconnect attempt if disconnected.
        /// </summary>
        /// <value>
        /// The reconnect delay
        /// </value>
        [DataMember(IsRequired = false, Order = 16)]
        public int SessionReconnectDelay
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default session timeout.
        /// Time that a session shall remain open without activity.
        /// </summary>
        /// <value>
        /// The default session timeout.
        /// </value>
        [DataMember(IsRequired = false, Order = 17)]
        public uint DefaultSessionTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default subscription publishing interval.
        /// The period at which the Subscription will send Notification messages.
        /// </summary>
        /// <value>
        /// The default Subscription publishing interval.
        /// </value>
        [DataMember(IsRequired = false, Order = 18)]
        public int DefaultSubscriptionPublishingInterval
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default subscription keep alive count.
        /// Counts the number of consecutive publishing cycles in which there have been no Notifications to report to the Client. 
        /// When the maximum keep-alive count is reached, a Publish request is de-queued and used to return a keep-alive Message.
        /// </summary>
        /// <value>
        /// The default subscription keep alive count.
        /// </value>
        [DataMember(IsRequired = false, Order = 19)]
        public uint DefaultSubscriptionKeepAliveCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default subscription life time count.
        /// This parameter sets how many times the publishing interval can expire before the subscription is terminated.
        /// </summary>
        /// <value>
        /// The default subscription life time count.
        /// </value>
        [DataMember(IsRequired = false, Order = 20)]
        public uint DefaultSubscriptionLifeTimeCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default subscription max notifications per publish.
        /// A value of zero indicates that there is no limit. The number of notifications per Publish is the sum of monitoredItems in the
        /// DataChangeNotification and events in the EventNotificationList.
        /// </summary>
        /// <value>
        /// The default subscription max notifications per publish.
        /// </value>
        [DataMember(IsRequired = false, Order = 21)]
        public uint DefaultSubscriptionMaxNotificationsPerPublish
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default subscription priority.<br/>
        /// When more than one Subscription needs to send Notifications, the Server should de-queue a Publish
        /// request to the Subscription with the highest priority number. For Subscriptions with equal priority the Server should de-queue Publish requests in a round-robin
        /// fashion. A Client that does not require special priority settings should set this value to zero.
        /// </summary>
        [DataMember(IsRequired = false, Order = 22)]
        public byte DefaultSubscriptionPriority
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default MI sampling interval.
        /// Each MonitoredItem created by the Client is assigned a sampling interval that is either inherited from
        /// the publishing interval of the Subscription or that is defined specifically to override that rate. A
        /// negative number indicates that the default sampling interval defined by the publishing interval of the
        /// Subscription is requested. The sampling interval indicates the fastest rate at which the Server should
        /// sample its underlying source for data changes.
        /// </summary>
        /// <value>
        /// The default MI sampling interval.
        /// </value>
        [DataMember(IsRequired = false, Order = 23)]
        public int DefaultMiSamplingInterval
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default size of the mi queue.
        /// </summary>
        /// <value>
        /// The default size of the mi queue.
        /// </value>
        [DataMember(IsRequired = false, Order = 24)]
        public uint DefaultMiQueueSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default size of the event mi queue.
        /// </summary>
        /// <value>
        /// The default size of the event mi queue.
        /// </value>
        [DataMember(IsRequired = false, Order = 25)]
        public uint DefaultEventMiQueueSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the max size for array types, this will limit the array size that can be seen by the user.
        /// </summary>
        [DataMember(IsRequired = false, Order = 26)]
        public int MaxDisplayedArraySize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets if ServerPicoseconds and SourcePicoseconds are displayed in interface when Read a Value Attribute.
        /// </summary>
        [DataMember(IsRequired = false, Order = 27)]
        public bool DisplayPicoSeconds
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default size of the values log cache.
        /// </summary>
        /// <value>
        /// The default size of the values log cache.
        /// </value>
        [DataMember(IsRequired = false, Order = 28)]
        public int DefaultValuesLogCacheSize
        {
            get;
            set;
        }

        #endregion
    }
}
