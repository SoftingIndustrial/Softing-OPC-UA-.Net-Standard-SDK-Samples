using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Represents the event argument provided by the history read operation that returns a continuation point.
    /// </summary>
    public class HistoryReadContinuationEventArgs : CancelEventArgs
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryReadContinuationEventArgs"></see> class.
        /// </summary>
        internal HistoryReadContinuationEventArgs(object cookie)
        {
            Cancel = false;
            Cookie = cookie;
        }

        #endregion Constructors

        #region Public properties
        /// <summary>
        /// Gets the cookie used for the history read call.
        /// </summary>
        public object Cookie
        {
            get;
            private set;
        }
        #endregion Public properties
    }
}
