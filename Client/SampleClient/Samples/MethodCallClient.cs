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
    /// Class that contains sample code for method call functionality
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
                Console.WriteLine("CallMethod: The session is not initialized!");
                return;
            }

            /*Select the method from the address space*/
            //Browse Path: Root\Objects\Server\Methods
            NodeId parentObjectId = new NodeId("ns=5;i=1");

            //Browse Path: Root\Objects\Server\Methods\Add
            string methodPath = "Root\\Objects\\Server\\Methods\\Add";
            NodeId methodId = new NodeId("ns=5;s=Add");

            /*initialize input arguments*/
            float arg1 = 123.123f;
            UInt32 arg2 = 100;

            List<object> inputArguments = new List<object> {arg1, arg2};
            Console.WriteLine("\nMethod '{0}' is called with the following arguments:", methodPath);
            for (int i = 0; i < inputArguments.Count; i++)
            {
                Console.WriteLine("input[{0}]= {1}", i, inputArguments[i]);
            }
            
            StatusCode statusCode = new StatusCode();
            try
            {
                IList<object> outputArgs;
                statusCode = m_session.Call(parentObjectId, methodId, inputArguments, out outputArgs);

                Console.WriteLine("Output arguments are:");
                for (int i = 0; i < outputArgs.Count; i++)
                {
                    Console.WriteLine("output[{0}]= {1}", i, outputArgs[i]);
                }
                Console.WriteLine("Status Code is: {0}", statusCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Method call exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Call a methods on server asynchronously
        /// </summary>
        internal void AsyncCallMethod()
        {
            if (m_session == null)
            {
                Console.WriteLine("AsyncCallMethod: The session is not initialized!");
                return;
            }

            /*Select the method from the address space*/
            //Browse Path: Root\Objects\Server\Methods
            NodeId parentObjectId = new NodeId(1, 5);
            //Browse Path: Root\Objects\Server\Methods\Multiply
            string methodPath = "Root\\Objects\\Server\\Methods\\Multiply";
            NodeId methodId = new NodeId("ns=5;s=Multiply");

            /*initialize input arguments*/
            Int16 arg1 = -34;
            UInt16 arg2 = 100;

            List<object> inputArguments = new List<object> {arg1, arg2};
            m_callIdentifier++;
            Console.WriteLine("\nMethod '{0}' (identifier ={1}) is called asynchronously with the following arguments:", methodPath, m_callIdentifier);
            for (int i = 0; i < inputArguments.Count; i++)
            {
                Console.WriteLine("input[{0}]= {1}", i, inputArguments[i]);
            }

            try
            {
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
            if (m_session == null)
            {
                // create the session object with no security and anonymous login    
                m_session = m_application.CreateSession(Constants.ServerUrl);
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
                    Console.WriteLine("CreateSession Error: {0}", ex.Message);
                    m_session.Dispose();
                    m_session = null;
                }
            }
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public void DisconnectSession()
        {
            if (m_session == null)
            {
                Console.WriteLine("The Session was not created.");
                return;
            }

            try
            {
                //remove handler for CallCompleted event
                m_session.CallCompleted -= Session_CallCompleted;

                m_session.Disconnect(true);
                m_session.Dispose();
                m_session = null;

                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DisconnectSession Error: {0}", ex.Message);
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
            Console.WriteLine("\nCall returned for method with identifier = {0}", e.Cookie);
            Console.WriteLine("Output arguments are:");
            for (int i = 0; i < e.OutputParameters.Count; i++)
            {
                Console.WriteLine("output[{0}]= {1}", i, e.OutputParameters[i]);
            }
            Console.WriteLine("Status Code is: {0}", e.Result);
        }

        #endregion
    }
}
