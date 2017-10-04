using System;
using System.Collections.Generic;
using System.Text;
using static Opc.Ua.Utils;

namespace Opc.Ua.Toolkit.Trace
{
    /// <summary>
    /// The event arguments provided when a trace logging is called.
    /// </summary>
    public class TraceEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceEventArgs"/> class.
        /// </summary>
        /// <param name="traceMask">The trace mask.</param>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="objectId">The object id.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        internal TraceEventArgs(TraceMasks traceMask, TraceSources traceSource, string objectId, string message, Exception exception)
        {
            TraceMask = traceMask;
            TraceSource = traceSource;
            ObjectId = objectId;
            Message = message;
            Exception = exception;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the trace mask.
        /// </summary>
        public TraceMasks TraceMask { get; private set; }

        /// <summary>
        /// Gets the trace source.
        /// </summary>
        public TraceSources TraceSource { get; private set; }

        /// <summary>
        /// Gets the object id.
        /// </summary>
        public string ObjectId { get; private set; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the exception.
        /// </summary>
        public Exception Exception { get; private set; }
        #endregion
    }
}
