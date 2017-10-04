using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Represents the event argument for a new data change notification message.`
    /// </summary>
    public class DataChangesNotificationEventArgs : EventArgs
    {
        #region Fields

        private IList<ExtendedMonitoredItemNotification> m_dataChangeNotifications;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataChangesNotificationEventArgs"/> class.
        /// </summary>
        /// <param name="dataChangeNotifications">The data change notifications.</param>
        internal DataChangesNotificationEventArgs(IList<ExtendedMonitoredItemNotification> dataChangeNotifications)
        {
            m_dataChangeNotifications = dataChangeNotifications;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the data change notifications.
        /// </summary>
        public IList<ExtendedMonitoredItemNotification> DataChangeNotifications
        {
            get { return m_dataChangeNotifications; }
        }

        #endregion Public Properties
    }
}
