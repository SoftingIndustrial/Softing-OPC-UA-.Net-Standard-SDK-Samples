using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Represents the event argument for a new event notification message.
    /// </summary>
    public class EventsNotificationEventArgs : EventArgs
    {
        #region Fields

        private IList<EventNotification> m_eventNotifications;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventsNotificationEventArgs"/> class.
        /// </summary>
        /// <param name="eventNotifications">The event notifications.</param>
        internal EventsNotificationEventArgs(IList<EventNotification> eventNotifications)
        {
            m_eventNotifications = eventNotifications;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the event notifications.
        /// </summary>
        public IList<EventNotification> EventNotifications
        {
            get { return m_eventNotifications; }
        }

        #endregion Public Properties
    }
}
