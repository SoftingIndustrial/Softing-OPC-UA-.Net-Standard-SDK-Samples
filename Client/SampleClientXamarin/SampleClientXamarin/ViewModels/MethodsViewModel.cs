/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Threading;
using Opc.Ua;
using SampleClientXamarin.Helpers;
using Softing.Opc.Ua.Client;

namespace SampleClientXamarin.ViewModels
{
    /// <summary>
    /// View Model for MethodsSamplePage
    /// </summary>
    class MethodsViewModel : BaseViewModel
    {
        #region Private Fields
        //Browse Path: Root\Objects\Server\Methods\Add
        private readonly NodeId m_methodNodeId = new NodeId("ns=5;s=Add");
        private readonly NodeId m_parentObjectId = new NodeId("ns=5;i=1");

        private const string SessionName = "MethodCallClient Session";
        private ClientSession m_session;
        private int m_callIdentifier;
        private string m_sampleServerUrl;
        private float m_floatValue;
        private UInt32 m_uint32Value;
        private float? m_resultValue;
        private string m_sessionStatusText;
        private string m_statusCode;
        #endregion

        #region Constructors

        /// <summary>
        /// Create new instance of MethodsViewModel
        /// </summary>
        public MethodsViewModel()
        {
            Title = "Method calls sample";
            SampleServerUrl = "opc.tcp://192.168.150.166:61510/SampleServer";
            ThreadPool.QueueUserWorkItem(o => InitializeSession());
            FloatValue = 10.77f;
            UInt32Value = 100;
        }

        #endregion

        #region Properties
        /// <summary>
        /// SampleServer Url
        /// </summary>
        public string SampleServerUrl
        {
            get { return m_sampleServerUrl; }
            set
            {
                SetProperty(ref m_sampleServerUrl, value);
                //disconnect existing session
                DisconnectSession();
            }
        }

        /// <summary>
        /// Text that indicates session status
        /// </summary>
        public string SessionStatusText
        {
            get { return m_sessionStatusText; }
            set { SetProperty(ref m_sessionStatusText, value); }
        }

        /// <summary>
        /// Float value parameter
        /// </summary>
        public float FloatValue
        {
            get { return m_floatValue; }
            set
            {
                SetProperty(ref m_floatValue, value);
                ResultValue = null;
                StatusCode = "";
            }
        }

        /// <summary>
        /// UInt32 value parameter
        /// </summary>
        public UInt32 UInt32Value
        {
            get { return m_uint32Value; }
            set
            {
                SetProperty(ref m_uint32Value, value);
                ResultValue = null;
                StatusCode = "";
            }
        }

        /// <summary>
        /// Method call result 
        /// </summary>
        public float? ResultValue
        {
            get { return m_resultValue; }
            set { SetProperty(ref m_resultValue, value); }
        }

        /// <summary>
        /// Method call status code
        /// </summary>
        public string StatusCode
        {
            get { return m_statusCode; }
            set { SetProperty(ref m_statusCode, value); }
        }
        #endregion

        #region Method Call Methods

        /// <summary>
        /// Call the a method on server.
        /// </summary>
        internal void CallMethod()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    return;
                }
            }
            /*initialize input arguments*/
            List<object> inputArguments = new List<object> { FloatValue, UInt32Value };

            StatusCode statusCode = new StatusCode();
            try
            {
                IList<object> outputArgs;
                statusCode = m_session.Call(m_parentObjectId, m_methodNodeId, inputArguments, out outputArgs);
                if (outputArgs != null && outputArgs.Count == 1)
                {
                    ResultValue = outputArgs[0] as float?;
                    StatusCode = statusCode.ToString();
                }
                else
                {
                    StatusCode = "Unexpected result. Output parameters in wrong format";
                }
            }
            catch (Exception ex)
            {
                StatusCode = "Method call exception: " + ex.Message;
            }
        }

        /// <summary>
        /// Call a methods on server asynchronously
        /// </summary>
        internal void AsyncCallMethod()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    return;
                }
            }
            //set busy indicator
            IsBusy = true;
            /*initialize input arguments*/
            List<object> inputArguments = new List<object> { FloatValue, UInt32Value };
            m_callIdentifier++;
            try
            {
                m_session.CallAsync(m_parentObjectId, m_methodNodeId, inputArguments, m_callIdentifier);
            }
            catch (Exception ex)
            {
                StatusCode = "Asynchronous method call exception: " + ex.Message;
            }
        }

        #endregion

        #region Initialize & DisconnectSession

        /// <summary>
        /// Initialize session object
        /// </summary>
        public void InitializeSession()
        {
            IsBusy = true;
            if (m_session == null)
            {
                try
                {
                    // create the session object with no security and anonymous login    
                    m_session = SampleApplication.UaApplication.CreateSession(SampleServerUrl);
                    m_session.SessionName = SessionName;

                    m_session.Connect(false, true);

                    //add handler for CallCompleted event
                    m_session.CallCompleted += Session_CallCompleted;
                    SessionStatusText = "Connected";
                }
                catch (Exception ex)
                {
                    SessionStatusText = "Not connected - CreateSession Error: " + ex.Message;

                    if (m_session != null)
                    {
                        m_session.Dispose();
                        m_session = null;
                    }
                }
            }
            IsBusy = false;
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public void DisconnectSession()
        {
            ResultValue = null;
            StatusCode = "";
            SessionStatusText = "";
            if (m_session == null)
            {
                SessionStatusText = "The Session was not created.";
                return;
            }

            try
            {
                //remove handler for CallCompleted event
                m_session.CallCompleted -= Session_CallCompleted;

                m_session.Disconnect(true);
                m_session.Dispose();
                m_session = null;

                SessionStatusText = "Disconnected";
            }
            catch (Exception ex)
            {
                SessionStatusText = "DisconnectSession Error: " + ex.Message;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handler for CallComplete event from Session
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Session_CallCompleted(object sender, MethodExecutionEventArgs e)
        {
            if (e.OutputParameters != null && e.OutputParameters.Count == 1)
            {
                ResultValue = e.OutputParameters[0] as float?;
                StatusCode = e.Result.ToString();
            }
            else
            {
                StatusCode = "Unexpected result. Output parameters in wrong format";
            }
            Thread.Sleep(3000);
            IsBusy = false;
        }

        #endregion
    }
}
