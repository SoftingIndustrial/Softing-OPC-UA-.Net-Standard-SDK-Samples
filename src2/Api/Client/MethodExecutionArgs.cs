using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Represents the event arguments for the <see cref="Session.CallCompleted"/> event.
    /// </summary>
    public class MethodExecutionArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodExecutionArgs"/> class.
        /// </summary>
        /// <param name="outputParameters">The output parameters.</param>
        /// <param name="result">The result.</param>
        /// <param name="cookie">The sender.</param>
        internal MethodExecutionArgs(List<object> outputParameters, StatusCode result, object cookie)
        {
            OutputParameters = outputParameters;
            Result = result;
            Cookie = cookie;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the output parameters of the asynchronous call.
        /// </summary>
        public List<object> OutputParameters { get; private set; }

        /// <summary>
        /// Gets the result of the asynchronous call.
        /// </summary>
        public StatusCode Result { get; private set; }

        /// <summary>
        /// Gets the sender cookie of the asynchronous call.
        /// </summary>
        public object Cookie { get; private set; }

        #endregion Public Properties
    }
}
