/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Opc.Ua;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.Types;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that contains sample code for method call functionality
    /// </summary>
    class MethodCallClient
    {
        #region Private Fields

        private const string SessionName = "MethodCallClient Session";
        private static readonly NodeId RefrgeratorStateEnumId = new NodeId("ns=12;i=15002");
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
            //Browse Path: Root\Objects\Methods
            NodeId parentObjectId = new NodeId("ns=5;i=1");

            //Browse Path: Root\Objects\Methods\Add
            string methodPath = "Root\\Objects\\Methods\\Add";
            NodeId methodId = new NodeId("ns=5;i=2");

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
                Program.PrintException("CallMethod", ex);
            }
        }

        /// <summary>
        /// Call method with complex parameters
        /// </summary>
        internal void CallCountRefrigeratorStatesMethod()
        {
            if (m_session == null)
            {
                Console.WriteLine("CallMethod: The session is not initialized!");
                return;
            }

            /*Select the method from the address space*/
            //Browse Path: Root\Objects\Methods
            NodeId parentObjectId = new NodeId("ns=5;i=1");

            //Browse Path: Root\Objects\Methods\CountRefrigeratorStates
            string methodPath = "Root\\Objects\\Methods\\CountRefrigeratorStates";
            NodeId methodId = new NodeId("ns=5;i=11");

            /*initialize input arguments*/       
            // initialize array of RefrigeratorState enum
            EnumValue[] refrigeratorStateArray = m_session.GetDefaultValueForDatatype(RefrgeratorStateEnumId, ValueRanks.OneDimension, 3) as EnumValue[];           

            Console.WriteLine("\nMethod '{0}' is called with the following arguments:", methodPath);
            for (int i = 0; i < refrigeratorStateArray.Length; i++)
            {
                Console.WriteLine("RefrigeratorStateArray[{0}]= {1}", i, refrigeratorStateArray[i]);
            }

            StatusCode statusCode = new StatusCode();
            try
            {
                IList<object> outputArgs;
                statusCode = m_session.Call(parentObjectId, methodId, new List<object> { refrigeratorStateArray }, out outputArgs);

                Console.WriteLine("Output arguments are:");
                for (int i = 0; i < outputArgs.Count; i++)
                {
                    Console.WriteLine("output[{0}]= {1}", i, outputArgs[i]);
                }
                Console.WriteLine("Status Code is: {0}", statusCode);
            }
            catch (Exception ex)
            {
                Program.PrintException("CallMethod", ex);
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
            //Browse Path: Root\Objects\Methods
            NodeId parentObjectId = new NodeId(1, 5);
            //Browse Path: Root\Objects\Methods\Multiply
            string methodPath = "Root\\Objects\\Methods\\Multiply";
            NodeId methodId = new NodeId("ns=5;i=5");

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
                Program.PrintException("CallMethodAsync", ex);
            }
        }

        #endregion

        #region Initialize & DisconnectSession

        /// <summary>
        /// Initialize session object
        /// </summary>
        public async Task InitializeSession()
        {
            if (m_session == null)
            {
                try
                {
                    m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = true;
                    m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = true;

                    // create the session object with no security and anonymous login    
                    m_session = m_application.CreateSession(Program.ServerUrl);
                    m_session.SessionName = SessionName;

                    await m_session.ConnectAsync(false, true).ConfigureAwait(false);

                    //add handler for CallCompleted event
                    m_session.CallCompleted += Session_CallCompleted;
                    Console.WriteLine("Session is connected.");
                }
                catch (Exception ex)
                {
                    Program.PrintException("CreateSession", ex);

                    if (m_session != null)
                    {
                        m_session.Dispose();
                        m_session = null;
                    }
                }
            }
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public async Task DisconnectSession()
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

                await m_session.DisconnectAsync(true).ConfigureAwait(false);
                m_session.Dispose();
                m_session = null;

                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Program.PrintException("DisconnectSession", ex);
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
