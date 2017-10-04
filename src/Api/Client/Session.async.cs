using Opc.Ua.Toolkit.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    public partial class Session
    {
        #region Events

        /// <summary>
        /// Occurs when the asynchronous execution of a method call has completed.
        /// </summary>
        public event EventHandler<MethodExecutionArgs> CallCompleted;

        #endregion Events

        #region Public Asynchronous Methods

        /// <summary>
        /// Calls the specified method on the current session asynchronously.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <param name="methodId">The method id.</param>
        /// <param name="inputArgs">The input arguments.</param>
        /// <param name="cookie">A cookie specified by the caller. This cookie can be used to identify the service in the asynchronous response.</param>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="CallAsync"]/*'/>
        public virtual void CallAsync(NodeId objectId, NodeId methodId, IList<object> inputArgs, object cookie)
        {
            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Call method while in the Disconnected state", StatusCodes.BadInvalidState);
            }           
            

            try
            {
                CallMethodRequest request = new CallMethodRequest();
                request.ObjectId = objectId;
                request.MethodId = methodId;

                VariantCollection inputArguments = null;
                if (inputArgs != null)
                {
                    List<object> inputArgumentOjects = new List<object>(inputArgs);
                    inputArguments = new VariantCollection();
                    foreach (var inputObj in inputArgumentOjects)
                    {
                        inputArguments.Add(new Variant(inputObj));
                    }
                }

                request.InputArguments = inputArguments;

                // Create methods to call
                CallMethodRequestCollection requests = new CallMethodRequestCollection();
                requests.Add(request);

                // Call the Read Service asynchronously
                IAsyncResult result = m_session.BeginCall(null, requests, OnCallComplete, cookie);
            }           
            catch (Exception exception)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.CallAsync",exception);

                throw new BaseException("Session CallAsync error", exception);
            }
        }

        #endregion Public Asynchronous Methods

        #region Private/Internal Asynchronous Handlers

        /// <summary>
        ///  Finishes the asynchronous invocation of the Call service.
        /// </summary>
        private void OnCallComplete(IAsyncResult result)
        {
            try
            {
                // Retrieve the read values
                CallMethodResultCollection values = new CallMethodResultCollection();
                DiagnosticInfoCollection diagnosticInfos = new DiagnosticInfoCollection();

                m_session.EndCall(
                    result,
                    out values,
                    out diagnosticInfos);

                List<object> outputArguments = new List<object>();
                StatusCode statusCode = new StatusCode();

                if (values.Count > 0)
                {
                    statusCode = values[0].StatusCode;
                    if (values[0] != null && values[0].OutputArguments != null)
                    {
                        for (int i = 0; i < values[0].OutputArguments.Count; i++)
                        {
                            object outputArgument = values[0].OutputArguments[i].Value;
                            outputArguments.Add(outputArgument);
                        }
                    }
                }

                MethodExecutionArgs args = new MethodExecutionArgs(outputArguments, statusCode, result.AsyncState);
                RaiseCallCompleted(this, args);
            }            
            catch (Exception exception)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.OnCallComplete",  exception);

                throw new BaseException("Session OnCallComplete error", exception);
            }
        }

        internal void RaiseCallCompleted(Session sender, MethodExecutionArgs args)
        {
            if (CallCompleted == null)
            {
                return;
            }

            try
            {
                CallCompleted(sender, args);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.RaiseCallCompleted", ex);
            }
        }

        #endregion Private/Internal Asynchronous Handlers
    }    
}
