/* ========================================================================
 * Copyright © 2011-2024 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using Opc.Ua;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that contains sample code for method call functionality
    /// </summary>
    class MethodCallClient
    {
        #region Private Fields

        private const string SessionName = "MethodCallClient Session";
        private static readonly NodeId RefrigeratorStateEnumId = new NodeId("ns=12;i=15002");
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

        #region Call

        /// <summary>
        /// Call method with simple and complex parameters
        /// </summary>
        public void Call()
        {
            MethodCall();

            CountRefrigeratorStatesMethodCall();
            
            MethodCallAsync();
        }

        /// <summary>
        /// Call the a method on server.
        /// </summary>
        private void MethodCall()
        {
            if (m_session == null)
            {
                Console.WriteLine("MethodCall: The session is not initialized!");
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

            List<object> inputArguments = new List<object> { arg1, arg2 };
            Console.WriteLine("\nMethod '{0}' is called with the following arguments:", methodPath);
            for (int i = 0; i < inputArguments.Count; i++)
            {
                Console.WriteLine("input[{0}]= {1}", i, inputArguments[i]);
            }

            try
            {
                IList<object> outputArguments;
                StatusCode statusCode = m_session.Call(parentObjectId, methodId, inputArguments, out outputArguments);
                Console.WriteLine($"Status Code is: {statusCode}");

                Console.WriteLine("Output arguments are:");
                for (int i = 0; i < outputArguments.Count; i++)
                {
                    Console.WriteLine($"  output[{i}]= {outputArguments[i]}");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("MethodCall", ex);
            }
        }

        /// <summary>
        /// Call method with complex parameters
        /// </summary>
        private void CountRefrigeratorStatesMethodCall()
        {
            if (m_session == null)
            {
                Console.WriteLine("CountRefrigeratorStatesMethodCall: The session is not initialized!");
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
            EnumValue[] refrigeratorStateArray = m_session.GetDefaultValueForDatatype(RefrigeratorStateEnumId, ValueRanks.OneDimension, 3) as EnumValue[];

            Console.WriteLine("\nMethod '{0}' is called with the following arguments:", methodPath);
            for (int i = 0; i < refrigeratorStateArray?.Length; i++)
            {
                Console.WriteLine("RefrigeratorStateArray[{0}]= {1}", i, refrigeratorStateArray[i]);
            }

            try
            {
                IList<object> outputArgs;
                StatusCode statusCode = m_session.Call(parentObjectId, methodId, new List<object> { refrigeratorStateArray }, out outputArgs);
                Console.WriteLine($"Status Code is: {statusCode} \n");

                Console.WriteLine("Output arguments are:");
                for (int i = 0; i < outputArgs.Count; i++)
                {
                    Console.WriteLine($"  output[{i}]= {outputArgs[i]}");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("CountRefrigeratorStatesMethodCall", ex);
            }
        }

        #endregion

        #region CallAsync

        /// <summary>
        /// Asynchronously call method with simple and complex parameters
        /// </summary>
        public async Task CallAsync()
        {           
            await MultiplyMethodCallAsync();

            await CountRefrigeratorStatesMethodCallAsync();
        }

        /// <summary>
        /// Call a methods on server asynchronously
        /// </summary>
        public void MethodCallAsync()
        {
            if (m_session == null)
            {
                Console.WriteLine("MethodCallAsync: The session is not initialized!");
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

            List<object> inputArguments = new List<object> { arg1, arg2 };
            m_callIdentifier++;

            Console.WriteLine($"\nMethodCallAsync: Method '{methodPath}' (identifier ={m_callIdentifier}) is called with the following arguments:");

            for (int i = 0; i < inputArguments.Count; i++)
            {
                Console.WriteLine($"  input[{i}]= {inputArguments[i]}");
            }

            try
            {
                m_session.CallAsync(parentObjectId, methodId, inputArguments, m_callIdentifier);
            }
            catch (Exception ex)
            {
                Program.PrintException("MethodCallAsync", ex);
            }
        }

        /// <summary>
        /// Asynchronously call a methods on server
        /// </summary>
        private async Task MultiplyMethodCallAsync()
        {
            if (m_session == null)
            {
                Console.WriteLine("CallAsync: The session is not initialized!");
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

            List<object> inputArguments = new List<object> { arg1, arg2 };
            m_callIdentifier++;

            Console.WriteLine($"\nCallAsync: Method '{methodPath}' (identifier ={m_callIdentifier}) is called with the following arguments:");
            for (int i = 0; i < inputArguments.Count; i++)
            {
                Console.WriteLine($"  input[{i}]= {inputArguments[i]}");
            }

            try
            {
                CallResponse response = await m_session.CallAsync(parentObjectId, methodId, inputArguments).ConfigureAwait(false);
                if (response != null)
                {
                    Console.WriteLine("Output arguments are:");
                    for (int i = 0; i < response.Results?[0]?.OutputArguments?.Count; i++)
                    {
                        Console.WriteLine($"  output[{i}]= {response.Results[0].OutputArguments[i].Value}\n");
                    }
                }
                else
                {
                    Console.WriteLine($"Method '{methodPath}' returned null.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("CallAsync", ex);
            }
        }

        /// <summary>
        /// Asynchronously call method with complex parameters
        /// </summary>
        private async Task CountRefrigeratorStatesMethodCallAsync()
        {
            if (m_session == null)
            {
                Console.WriteLine("CountRefrigeratorStatesMethodCallAsync: The session is not initialized!");
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
            EnumValue[] refrigeratorStateArray = m_session.GetDefaultValueForDatatype(RefrigeratorStateEnumId, ValueRanks.OneDimension, 3) as EnumValue[];

            Console.WriteLine("\nMethod '{0}' is called with the following arguments:", methodPath);
            for (int i = 0; i < refrigeratorStateArray?.Length; i++)
            {
                Console.WriteLine("RefrigeratorStateArray[{0}]= {1}", i, refrigeratorStateArray[i]);
            }

            try
            {
                CallResponse response = await m_session.CallAsync(parentObjectId, methodId, new List<object> { refrigeratorStateArray }).ConfigureAwait(false);
                Console.WriteLine($"Status Code is: {response.ResponseHeader.ServiceResult}");

                if (response != null)
                {
                    Console.WriteLine("Output arguments are:");
                    for (int i = 0; i < response.Results?[0]?.OutputArguments?.Count; i++)
                    {
                        Console.WriteLine($"  output[{i}]= {response.Results[0].OutputArguments[i].Value}\n");
                    }
                }
                else
                {
                    Console.WriteLine($"Method '{methodPath}' returned null.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("CountRefrigeratorStatesMethodCallAsync", ex);
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
                    m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = false;

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
