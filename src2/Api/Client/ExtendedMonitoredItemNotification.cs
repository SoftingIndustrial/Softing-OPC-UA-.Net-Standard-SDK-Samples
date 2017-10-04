using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Represents the data change returned in a NotificationMessage, used in the data change notification event argument of the subscription and monitored item.
    /// </summary>
    public class ExtendedMonitoredItemNotification : MonitoredItemNotification
    {
        #region Fields        
        private MonitoredItem m_monitoredItem;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataChangeNotification"/> class.
        /// </summary>
        /// <param name="monitoredItem">The monitored item that the notification belongs to</param>
        internal ExtendedMonitoredItemNotification(MonitoredItemNotification monitoredItemNotification, MonitoredItem monitoredItem)
        {
            if (monitoredItemNotification != null)
            {
                this.ClientHandle = monitoredItemNotification.ClientHandle;
                this.Message = monitoredItemNotification.Message;
                this.DiagnosticInfo = monitoredItemNotification.DiagnosticInfo;
                this.Value = monitoredItemNotification.Value;
            }

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

        /// <summary>
        /// Gets the SequenceNo field.
        /// </summary>
        public uint SequenceNo
        {
            get
            {
                if (Message != null)
                {
                    return Message.SequenceNumber;
                }

                return 0;
            }
        }
        #endregion Public Properties        
    }
}
