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
using Opc.Ua;
using Softing.Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that conains sample code for method call functionality
    /// </summary>
    class MethodCallClient
    {
        #region Private Fields

        private const string SessionName = "MethodCallClient Session";
        private readonly UaApplication m_application;
        private ClientSession m_session;
        private int m_callIdentifier;

        #endregion

        #region Constructor

        /// <summary>
        /// Create instance of MethodCallClient
        /// </summary>
        /// <param name="application"></param>
        public MethodCallClient(UaApplication application)
        {
            m_application = application;
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
                InitializeSession();
            }

            /*Select the method from the address space*/
            //Browse Path: Root\Objects\Server\Methods
            NodeId parentObjectId = new NodeId(1, 6);
            //Browse Path: Root\Objects\Server\Methods\Add
            string methodPath = "Root\\Objects\\Server\\Methods\\Add";
            NodeId methodId = new NodeId("ns=6;s=Add");

            /*initialize input arguments*/
            float arg1 = 123.123f;
            UInt32 arg2 = 100;

            List<object> inputArguments = new List<object> {arg1, arg2};
            Console.WriteLine("\nMethod '{0}' is called with the following arguments:", methodPath);
            for (int i = 0; i < inputArguments.Count; i++)
            {
                Console.WriteLine("input[{0}]= {1}", i, inputArguments[i]);
            }

            IList<object> output = new List<object>();
            StatusCode statusCode = new StatusCode();
            try
            {
                statusCode = m_session.Call(parentObjectId, methodId, inputArguments, out output);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Method call exception: " + ex.Message);
            }
            Console.WriteLine("\nOutput arguments are:");
            for (int i = 0; i < output.Count; i++)
            {
                Console.WriteLine("output[{0}]= {1}", i, output[i]);
            }
            Console.WriteLine(string.Format("\nStatus Code is: {0}\n", statusCode));
        }

        /// <summary>
        /// Call a methods on server asynchronously
        /// </summary>
        internal void AsyncCallMethod()
        {
            if (m_session == null)
            {
                InitializeSession();
            }

            /*Select the method from the address space*/
            //Browse Path: Root\Objects\Server\Methods
            NodeId parentObjectId = new NodeId(1, 6);
            //Browse Path: Root\Objects\Server\Methods\Multiply
            string methodPath = "Root\\Objects\\Server\\Methods\\Multiply";
            NodeId methodId = new NodeId("ns=6;s=Multiply");

            /*initialize input arguments*/
            Int16 arg1 = -34;
            UInt16 arg2 = 100;

            List<object> inputArguments = new List<object> {arg1, arg2};
            Console.WriteLine("\nMethod '{0}' is called asynchronously with the following arguments:", methodPath);
            for (int i = 0; i < inputArguments.Count; i++)
            {
                Console.WriteLine("input[{0}]= {1}", i, inputArguments[i]);
            }

            try
            {
                m_callIdentifier++;
                m_session.CallAsync(parentObjectId, methodId, inputArguments, m_callIdentifier);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Asynchronous method call exception: " + ex.Message);
            }
        }

        #endregion

        #region Initialize & DisconnectSession

        /// <summary>
        /// Initialize session object
        /// </summary>
        public void InitializeSession()
        {
            UserIdentity userIdentity = new UserIdentity();
            // create the session object.            
            m_session = m_application.CreateSession(Constants.SampleServerUrlOpcTcp,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, userIdentity, null);
            m_session.SessionName = SessionName;

            try
            {
                m_session.Connect(false, true);

                //add handler for CallCompleted event
                m_session.CallCompleted += Session_CallCompleted;
                Console.WriteLine("Session is connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateSession Error: {0}", ex);
            }
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public void DisconnectSession()
        {
            if (m_session == null)
            {
                return;
            }

            try
            {
                //remove handler for CallCompleted event
                m_session.CallCompleted -= Session_CallCompleted;

                m_session.Disconnect(true);
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DisconnectSession Error: {0}", ex.Message);
            }

            m_session.Dispose();
            m_session = null;
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
            Console.WriteLine(string.Format("\nCall returned for: {0}", e.Cookie));
            Console.WriteLine("Output arguments are:");
            for (int i = 0; i < e.OutputParameters.Count; i++)
            {
                Console.WriteLine("output[{0}]= {1}", i, e.OutputParameters[i]);
            }
            Console.WriteLine(string.Format("\nStatus Code is: {0}\n", e.Result));
        }

        #endregion
    }
}
