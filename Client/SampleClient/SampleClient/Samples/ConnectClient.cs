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
    /// Class providing support for connect operations with different configuration parametes.
    /// </summary>
    public class ConnectClient
    {
        #region Private Fields
        private readonly UaApplication m_application;
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of ConnectClient
        /// </summary>
        /// <param name="application"></param>
        public ConnectClient(UaApplication application)
        {
            m_application = application;
        }
        #endregion
        
        #region Connect Methods
        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and anonymous user identity.
        /// </summary>
        public void CreateOpcTcpSessionWithNoSecurity()
        {
            // create the session object.
            using (ClientSession session = CreateSession("UaBinaryNoSecuritySession", Constants.SampleServerUrlOpcTcp,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity()))
            {
                ConnectTest(session);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and a username/password user identity.
        /// </summary>
        public void CreateOpcTcpSessionWithUserId()
        {
            // create the session object.
            using (ClientSession session = CreateSession("UaBinaryUserIdSession", Constants.SampleServerUrlOpcTcp,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity("usr", "pwd")))
            {
                ConnectTest(session);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with security and a username/password user identity.
        /// </summary>
        public void CreateOpcTcpSessionWithSecurity()
        {
            // create the session object.
            using (ClientSession session = CreateSession("UaBinarySecureSession", Constants.SampleServerUrlOpcTcp,
                MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic128Rsa15, MessageEncoding.Binary,
                new UserIdentity("usr", "pwd")))
            {
                ConnectTest(session);
            }
        }

        /// <summary>
        /// Creates and connects a session on HTTPS protocol with anonymous user identity and using xml message encoding.
        /// </summary>
        public void CreateHttpsSessionWithAnomymousUserId()
        {
            // create the session object.
            using (ClientSession session = CreateSession("HttpsAnonymousUserIdSession", Constants.SampleServerUrlHttps,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Xml, new UserIdentity()))
            {
                ConnectTest(session);
            }
        }

        /// <summary>
        /// Creates and connects a session on HTTPS protocol with a username/password user identity.
        /// </summary>
        public void CreateHttpsSessionWithUserId()
        {
            // create the session object.
            using (ClientSession session = CreateSession("HttpsUserIdSession", Constants.SampleServerUrlHttps,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity("usr", "pwd")))
            {
                ConnectTest(session);
            }
        }

        /// <summary>
        /// Creates and connects a session using the Discovery process.
        /// </summary>
        public void CreateSessionUsingDiscovery()
        {
            try
            {
                // Retrieve the list of available server connection channels by calling GetEndpoints on the Server's discovery endpoint.

                Console.WriteLine("Discovering available endpoints...");
                DiscoveryService discoveryService = new DiscoveryService(m_application.Configuration);
                IList<EndpointDescriptionEx> endpoints = discoveryService.GetEndpoints(Constants.SampleServerUrlOpcTcp);

                Console.WriteLine("GetEndpoints returned {0} endpoints.", endpoints.Count);

                // Iterate the list of available endpoints and select the endpoint with expected configuration.
                // In this example we search for an endpoint with opc.tcp protocol and no security.
                EndpointDescriptionEx selectedEndpoint = null;

                foreach (var endpoint in endpoints)
                {
                    if ((endpoint.EndpointUrl.StartsWith("opc.tcp://") &&
                         endpoint.SecurityMode == MessageSecurityMode.None))
                    {
                        selectedEndpoint = endpoint;
                        break;
                    }
                }

                // perform a connect test for the selected endpoint.
                if (selectedEndpoint != null)
                {
                    // create the session object.
                    using (ClientSession session = CreateSession("UaDiscoverySession", Constants.SampleServerUrlOpcTcp,
                        selectedEndpoint.SecurityMode,
                        (SecurityPolicy)Enum.Parse(typeof(SecurityPolicy), selectedEndpoint.SecurityPolicy),
                        selectedEndpoint.Encoding[0],
                        new UserIdentity()))
                    {
                        ConnectTest(session);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Creates and connects an new session with the specified parameters.
        /// </summary>        
        private ClientSession CreateSession(string sessionName, string serverUrl, MessageSecurityMode securityMode,
            SecurityPolicy securityPolicy, MessageEncoding messageEncoding, UserIdentity userId)
        {
            try
            {
                // Create the Session object.
                ClientSession session = m_application.CreateSession(serverUrl, securityMode, securityPolicy,
                    messageEncoding, userId, null);

                session.SessionName = sessionName;

                Console.WriteLine("New session object created with url {0}", session.Url);
                return session;
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateSession Error: {0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Performs a Connect/Disconnect test for the specified session.
        /// </summary>
        private void ConnectTest(ClientSession session)
        {
            try
            {
                // Attempt to connect to server.
                Console.WriteLine("Connecting the session {0}...", session.SessionName);
                session.Connect(false, true);
                Console.WriteLine("Session state = {0}", session.CurrentState);

                // Disconnect the session.
                Console.WriteLine("Disconnecting the session...");
                session.Disconnect(true);
                Console.WriteLine("Session state = {0}", session.CurrentState);
            }
            catch (Exception e)
            {
                Console.WriteLine("ConnectTest Error: {0}", e.Message);
            }
        } 
        #endregion
    }
}
