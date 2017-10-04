using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{/// <summary>
 /// Represents the event monitored item notification class, used in the event notification event argument of the subscription and monitored item.
 /// </summary>
    public class EventNotification : EventFieldList
    {
        #region Fields
        
        private MonitoredItem m_monitoredItem;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedEventFieldList"/> class.
        /// </summary>
        public EventNotification()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedEventFieldList"/> class.
        /// </summary>
        /// <param name="eventFieldList">The event field list.</param>
        /// <param name="monitoredItem">The monitored item that the notification belongs to</param>
        internal EventNotification(EventFieldList eventFieldList, MonitoredItem monitoredItem)
        {
            this.ClientHandle = eventFieldList.ClientHandle;
            this.EventFields = eventFieldList.EventFields;
            this.Handle = eventFieldList.Handle;
            m_monitoredItem = monitoredItem;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the monitored item that the notification belongs to.
        /// </summary>
        public virtual MonitoredItem MonitoredItem
        {
            get
            {
                return m_monitoredItem;
            }
        }     
       
        #endregion Public Properties
       
    }
}
