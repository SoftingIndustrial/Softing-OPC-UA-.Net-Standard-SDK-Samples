using Opc.Ua.Toolkit.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Represents an object used to synchronously/asynchronously invoke the call service.<br/>
    /// The service is invoked within the context of an existing session and provides support for passing input and output arguments to/from the server.
    /// </summary>
    public class Method
    {
        #region Fields

        private Session m_session;
        private NodeId m_objectId, m_methodId;
        private string m_displayName;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Method"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="objectId">The NodeId of the object containing the method.</param>
        /// <param name="methodId">The NodeId of the method to invoke.</param>
        /// <param name="displayName">The display name of the method.</param>
        public Method(Session session, NodeId objectId, NodeId methodId, string displayName)
        {
            if (session == null || objectId == null || methodId == null || displayName == null)
            {
                throw new ArgumentNullException();
            }

            m_objectId = objectId;
            m_methodId = methodId;
            m_displayName = displayName;

            session.AddMethod(this);
            m_session = session;
        }

        #endregion Constructors

        #region Public Events

        /// <summary>
        /// Occurs when the asynchronous execution of a method call has completed.
        /// </summary>
        public event EventHandler<MethodExecutionArgs> CallCompleted;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Gets the session of the method.
        /// </summary>
        public Session Session
        {
            get
            {
                return m_session;
            }
        }

        /// <summary>
        /// Gets or sets the NodeId of the method.
        /// </summary>
        public NodeId MethodId
        {
            get
            {
                return m_methodId;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("MethodId");
                }

                m_methodId = value;
            }
        }

        /// <summary>
        /// Gets or sets the NodeId of the object containing the method.
        /// </summary>
        public NodeId ObjectId
        {
            get
            {
                return m_objectId;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ObjectId");
                }

                m_objectId = value;
            }
        }

        /// <summary>
        /// Gets or sets the display name of the method.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return m_displayName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("DisplayName");
                }

                m_displayName = value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Calls the method by invoking the Call service on the server.
        /// It passes the list of input arguments and retrieves the list of output arguments.
        /// </summary>
        /// <param name="inputArgs">The list of input argument values.</param>
        /// <param name="outputArgs">The list of output argument values.</param>
        /// <returns>The StatusCode returned informs if method was called with success, if failed or if the results are uncertain.</returns>
        public StatusCode Call(IList<object> inputArgs, out IList<object> outputArgs)
        {
            if (m_session.CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot Call method while in the Disconnected state", StatusCodes.BadInvalidState);
            }
            
            TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Method.Call", "Call operation started for method {0}.", DisplayName);

            StatusCode resultCode;
            try
            {
                resultCode = m_session.Call(m_objectId, m_methodId, inputArgs, out outputArgs);

                TraceMasks traceMask = TraceMasks.OperationDetail;
                if (StatusCode.IsUncertain(resultCode))
                {
                    traceMask = TraceMasks.Information;
                }
                else if (StatusCode.IsBad(resultCode))
                {
                    traceMask = TraceMasks.Error;
                }

                TraceService.Log(traceMask, TraceSources.ClientAPI, "Method.Call", 
					"Call of method {0} completed with result code {1}.", DisplayName, resultCode);

                return resultCode;
            }           
            catch (Exception exception)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Method.Call", exception);

                throw new BaseException("Method Call error", exception);
            }
        }

        /// <summary>
        /// Calls this method on its session asynchronously.<br/>
        /// It passes the list of input arguments to the asynchronous call.
        /// </summary>
        /// <remarks>
        /// The call operation cannot be performed if the state of the method's session is disconnected, and a <see cref="BaseException"/> exception will be raised.<br/>
        /// In case of call service failure an exception of type <see cref="BaseException"/> will be thrown as well.
        /// </remarks>
        /// <param name="inputArgs">The list of input argument values.</param>
        /// <param name="cookie">The sender object/cookie.</param>
        public void CallAsync(IList<object> inputArgs, object cookie)
        {
            if (m_session.CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot CallAsync method while in the Disconnected state", StatusCodes.BadInvalidState);
            }
            
            TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Method.Call", "CallAsync operation started for method {0}.", DisplayName);

            Opc.Ua.Client.Session session = m_session.CoreSession;

            try
            {
                CallMethodRequest request = new CallMethodRequest();
                request.ObjectId = m_objectId;
                request.MethodId = m_methodId;

                VariantCollection inputArguments = null;

                if (inputArgs != null)
                {
                    inputArguments = new VariantCollection();

                    foreach (var inputObj in inputArgs)
                    {
                        inputArguments.Add(new Variant(inputObj));
                    }
                }

                request.InputArguments = inputArguments;

                // Create method array to be called
                CallMethodRequestCollection requests = new CallMethodRequestCollection();
                requests.Add(request);

                // Call the Read Service asynchronously
                IAsyncResult result = session.BeginCall(
                    null,
                    requests,
                    OnCallComplete,
                    cookie);
            }           
            catch (Exception exception)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Method.CallAsync", exception);

                throw new BaseException("Method CallAsync error", exception);
            }
        }

        /// <summary>
        /// Deletes this method instance from the session.
        /// </summary>
        public void Delete()
        {
            m_session.RemoveMethod(this);
            m_session = null;
        }

        /// <summary>
        /// Returns a list of input and output arguments for this method.
        /// </summary>
        /// <param name="inputArguments">Output parameter that represents the list of input arguments.</param>
        /// <param name="outputArguments">Output parameter that represents the list of output arguments.</param>
        public void ReadArguments(out List<Argument> inputArguments, out List<Argument> outputArguments)
        {
            if (m_session == null)
            {
                throw new BaseException("Cannot read arguments for a deleted method.", StatusCodes.BadInvalidState);
            }

            m_session.GetMethodArguments(m_methodId, out inputArguments, out outputArguments);
        }

        #endregion Public Methods

        #region Private Methods

        #region Event Handlers

        /// <summary>
        /// Event handler for the call complete core 
        /// </summary>
        /// <param name="result">The caller/cookie object.</param>
        private void OnCallComplete(IAsyncResult result)
        {
            try
            {
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Method.OnCallComplete", "OnCallComplete received for method {0}.", DisplayName);

                StatusCode statusCode = new StatusCode();
                List<object> outputArguments = new List<object>();

                Opc.Ua.Client.Session session = m_session.CoreSession;

                // Retrieve the read values
                CallMethodResultCollection values = new CallMethodResultCollection();
                DiagnosticInfoCollection diagnosticInfos = new DiagnosticInfoCollection();

                session.EndCall(
                    result,
                    out values,
                    out diagnosticInfos);

                if (values.Count > 0)
                {
                    statusCode = new StatusCode(values[0].StatusCode.Code);
                    if (values[0].OutputArguments != null)
                    {
                        foreach (var argument in values[0].OutputArguments)
                        {
                            outputArguments.Add(argument.Value);
                        }
                    }
                }


                MethodExecutionArgs args = new MethodExecutionArgs(outputArguments, statusCode, result.AsyncState);
                RaiseMethodExecutionCompleted(args);
            }            
            catch (Exception exception)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Method.OnCallComplete", exception);

                throw new BaseException("Method OnCallComplete error", exception);
            }
        }

        #endregion Event Handlers

        /// <summary>
        /// Raises the CallCompleted event.
        /// </summary>
        /// <param name="args">The MethodExecutionCompletedArgs to be sent to the registered event handler(s).</param>
        private void RaiseMethodExecutionCompleted(MethodExecutionArgs args)
        {
            if (CallCompleted == null)
            {
                return;
            }

            try
            {
                CallCompleted(this, args);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Method.RaiseCallCompleted", ex);
            }
        }

        #endregion Private Methods
    }
}
