/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class providing sample code for reverse connect operations with different configuration parameters.
    /// </summary>
    class ReverseConnectClient
    {
        #region Private Fields

        private readonly UaApplication m_application;

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of <see cref="ReverseConnectClient"/>
        /// </summary>
        public ReverseConnectClient(UaApplication application)
        {
            m_application = application;
        }
        #endregion

        /// <summary>
        /// Sample code for creating reverse connect sessions
        /// </summary>
        public void ExecuteReverseConnectSample()
        {
            string reverseConnectUrl = "opc.tcp://localhost:65300";
            string serverApplicationUri = "urn:wboaw10:Softing:UANETStandardToolkit:SampleServer";
            try
            {
                Console.WriteLine("Get Endpoints of '{0}'' using reverse connection endpoint '{1}'", serverApplicationUri, reverseConnectUrl);
                var endpoints = m_application.GetEndpoints(reverseConnectUrl, serverApplicationUri);
                Console.WriteLine("The server returned {0} endpoints.", endpoints.Count);
                foreach(var endpoint in endpoints)
                {
                    try
                    {
                        string endpointToString = string.Format("{0} - {1} - {2}",
                                endpoint.EndpointUrl,
                                endpoint.SecurityMode,
                                endpoint.SecurityPolicy);
                        Console.WriteLine("\tCreate session to endpoint:", endpointToString);
                        ClientReverseConnectSession session = m_application.CreateReverseConnectSession(reverseConnectUrl, serverApplicationUri,
                            endpoint.SecurityMode, (SecurityPolicy)Enum.Parse(typeof(SecurityPolicy), endpoint.SecurityPolicy),
                            endpoint.Encoding[0]);
                        session.InitializeWithDiscoveryEndpointDescription(endpoint);
                        session.SessionName = "ReverseConnect Session " + endpointToString;
                        Console.WriteLine("\t\tThe session '{0}' was created.", session.SessionName);
                        session.Connect(true, true);
                        Console.WriteLine("\t\tThe session '{0}' is connected and active.", session.SessionName);
                        session.Disconnect(true);
                        Console.WriteLine("\t\tThe session is disconnected.");
                        session.Dispose();
                    }
                    catch(Exception ex)
                    {
                        Program.PrintException("ExecuteReverseConnectSample.CreateConnection to endpoint:" + endpoint, ex);
                    }
                }
            }
            catch(Exception ex)
            {
                Program.PrintException("ExecuteReverseConnectSample", ex);
            }
        }
    }
}
