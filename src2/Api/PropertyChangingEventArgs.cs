using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// Event args class for proeprty changed events
    /// </summary>
    public class PropertyChangingEventArgs : EventArgs
    {
        /// <summary>
        /// Get/set old value of changing property
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// Get/Set new value of changing property
        /// </summary>
        public object NewValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the event should be canceled
        /// </summary>
        public bool Cancel { get; set; }
    }
}
