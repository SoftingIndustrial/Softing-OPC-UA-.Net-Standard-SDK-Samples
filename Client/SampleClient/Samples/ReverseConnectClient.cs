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
using Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class providing sample code for reverse connect operations with different configuration parameters.
    /// </summary>
    class ReverseConnectClient
    {
        private string m_reverseConnectUrl;
        private string m_serverApplicationUri;

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

            // Get the Sample Client reverse connect custom parameters
            SampleClientConfiguration sampleClientConfiguration = application.Configuration.ParseExtension<SampleClientConfiguration>();
            if (sampleClientConfiguration != null)
            {
                m_reverseConnectUrl = sampleClientConfiguration.ReverseConnectUrl;
                m_serverApplicationUri = sampleClientConfiguration.ReverseConnectServerApplicationUri;
            }
        }
        #endregion
        /// <summary>
        /// Get all server endpoints suing Reverse connect mechanism and then create a Reverse Connect session to each of them
        /// </summary>
        public void GetEndpointsAndReverseConnect()
        {
            try
            {
                Console.WriteLine("Get Endpoints of '{0}' using reverse connection endpoint '{1}'", m_serverApplicationUri, m_reverseConnectUrl);
                var endpoints = m_application.GetEndpoints(m_reverseConnectUrl, m_serverApplicationUri);
                Console.WriteLine("The server returned {0} endpoints.", endpoints.Count);
                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        string endpointToString = string.Format("{0} - {1} - {2}",
                                endpoint.EndpointUrl,
                                endpoint.SecurityMode,
                                endpoint.SecurityPolicy);
                        Console.WriteLine("\n\tCreate session to endpoint: {0}", endpointToString);
                        using (ClientSession session = CreateReverseConnectSession("ReverseConnectSession",
                            endpoint.SecurityMode, (SecurityPolicy)Enum.Parse(typeof(SecurityPolicy), endpoint.SecurityPolicy),
                            endpoint.Encoding[0], new UserIdentity()))
                        {                            
                            session.InitializeWithDiscoveryEndpointDescription(endpoint);
                            ConnectClient.ConnectTest(session);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.PrintException("ExecuteReverseConnectSample.CreateConnection to endpoint:" + endpoint, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ExecuteReverseConnectSample", ex);
            }
        }

        /// <summary>
        /// Creates and connects a reverse session on opc.tcp protocol with no security and anonymous user identity.
        /// </summary>
        public void CreateOpcTcpSessionWithNoSecurity()
        {
            // create the session object.
            using (ClientSession session = CreateReverseConnectSession("UaBinaryNoSecurityReverseConnectSession", 
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity()))
            {
                ConnectClient.ConnectTest(session);
            }
        }

        /// <summary>
        /// Creates a new reverse connect session with the specified parameters.
        /// </summary>        
        private ClientSession CreateReverseConnectSession(string sessionName, MessageSecurityMode securityMode,
            SecurityPolicy securityPolicy, MessageEncoding messageEncoding, UserIdentity userId)
        {
            try
            {
                Console.WriteLine("\r\nCreating the reverse connect session {0} (SecurityMode = {1}, SecurityPolicy = {2}, UserIdentity = {3})...",
                    sessionName, securityMode, securityPolicy, userId.GetIdentityToken());
                // Create the Reverse Connect Session object.
                ClientSession session = m_application.CreateReverseConnectSession(m_reverseConnectUrl, m_serverApplicationUri,
                    securityMode, securityPolicy, messageEncoding, userId);
                Console.WriteLine("The session was created.");
                session.SessionName = sessionName;
                return session;
            }
            catch (Exception ex)
            {
                Program.PrintException("ConnectClient.CreateSession", ex);
                return null;
            }
        }


    }
}
