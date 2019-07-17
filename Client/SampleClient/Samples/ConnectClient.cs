/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class providing sample code for connect operations with different configuration parameters.
    /// 
    /// This class also provides sample code for creating ApplicationConfiguration object by code 
    /// </summary>
    public class ConnectClient
    {
        #region Private Fields

        private readonly UaApplication m_application;

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of ConnectClient
        /// The constructor will create a new instance of UaApplication with an 
        /// </summary>
        public ConnectClient()
        {
            // Alternatively, instead of providing a config XML file, we can provide instead, the custom configuration set below
            //ApplicationConfiguration configuration = CreateAplicationConfiguration();
            //m_application = UaApplication.Create(configuration).Result;
            m_application = UaApplication.Create("SampleClient.Config.xml").Result;
        }

        #endregion

        #region Connect Methods

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and anonymous user identity.
        /// </summary>
        public void CreateOpcTcpSessionWithNoSecurity()
        {
            // create the session object.
            using (ClientSession session = CreateSession("UaBinaryNoSecuritySession", Program.ServerUrl,
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
            using (ClientSession session = CreateSession("UaBinaryUserIdSession", Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity("usr", "pwd")))
            {
                ConnectTest(session);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with security and anonimous user identity.
        /// </summary>
        /// <param name="messageSecurityMode"> Desired security mode</param>
        /// <param name="securityPolicy"> Desired security policy</param>
        public void CreateOpcTcpSessionWithSecurity(MessageSecurityMode messageSecurityMode, SecurityPolicy securityPolicy)
        {
            // create the session object.
            using (ClientSession session = CreateSession("UaBinarySecureSession", Program.ServerUrl,
                messageSecurityMode, securityPolicy, MessageEncoding.Binary,
                new UserIdentity()))
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
            using (ClientSession session = CreateSession("HttpsAnonymousUserIdSession", Program.ServerUrl,
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
            using (ClientSession session = CreateSession("HttpsUserIdSession", Program.ServerUrl,
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
                Console.WriteLine("\r\nDiscovering available endpoints...");
                IList<EndpointDescriptionEx> endpoints = m_application.GetEndpoints(Program.ServerUrl);

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
                    // create the session object for selectedEndpoint
                    using (ClientSession session = CreateSession("UaDiscoverySession", selectedEndpoint.DiscoveryEndpointUrl,
                        selectedEndpoint.SecurityMode,
                        (SecurityPolicy) Enum.Parse(typeof(SecurityPolicy), selectedEndpoint.SecurityPolicy),
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
        /// Creates Application's ApplicationConfiguration programmatically
        /// </summary>
        /// <returns></returns>
        private ApplicationConfiguration CreateAplicationConfiguration()
        {
            Console.WriteLine("Creating ApplicationConfiguration for current UaApplication...");
            ApplicationConfiguration configuration = new ApplicationConfiguration();

            configuration.ApplicationName = "UA Sample Client";
            configuration.ApplicationType = ApplicationType.Client;
            configuration.ApplicationUri = $"urn:{Utils.GetHostName()}:OPCFoundation:SampleClient";
            configuration.TransportConfigurations = new TransportConfigurationCollection();
            configuration.TransportQuotas = new TransportQuotas {OperationTimeout = 15000};
            configuration.ClientConfiguration = new ClientConfiguration {DefaultSessionTimeout = 60000};

            ClientToolkitConfiguration clientTkConfigration = new ClientToolkitConfiguration();
            clientTkConfigration.DefaultSessionTimeout = 60000;
            clientTkConfigration.DiscoveryOperationTimeout = 10000;
            configuration.UpdateExtension<ClientToolkitConfiguration>(new System.Xml.XmlQualifiedName("ClientToolkitConfiguration"), clientTkConfigration);

            configuration.TraceConfiguration = new TraceConfiguration()
            {
                OutputFilePath = @"%CommonApplicationData%\Softing\OpcUaNetStandardToolkit\logs\SampleClient.log",
                TraceMasks = 519
            };

            configuration.SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"%CommonApplicationData%\Softing\OpcUaNetStandardToolkit\pki\own"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"%CommonApplicationData%\Softing\OpcUaNetStandardToolkit\pki\trusted",
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"%CommonApplicationData%\Softing\OpcUaNetStandardToolkit\pki\issuer",
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"%CommonApplicationData%\Softing\OpcUaNetStandardToolkit\pki\rejected",
                },
                AutoAcceptUntrustedCertificates = true
            };

            return configuration;
        }

        /// <summary>
        /// Creates and connects an new session with the specified parameters.
        /// </summary>        
        private ClientSession CreateSession(string sessionName, string serverUrl, MessageSecurityMode securityMode,
            SecurityPolicy securityPolicy, MessageEncoding messageEncoding, UserIdentity userId)
        {
            try
            {
                Console.WriteLine("\r\nCreating the session {0} (SecurityMode = {1}, SecurityPolicy = {2}, \r\n\t\t\t\t\t\tUserIdentity = {3})...",
                    sessionName, securityMode, securityPolicy, userId.GetIdentityToken());
                // Create the Session object.
                ClientSession session = m_application.CreateSession(serverUrl, securityMode, securityPolicy, messageEncoding, userId);

                session.SessionName = sessionName;
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
                Console.WriteLine("Connecting session {0}...", session.SessionName);
                session.Connect(false, true);
                Console.WriteLine("Session state = {0}. Success!", session.CurrentState);

                // Disconnect the session.
                session.Disconnect(true);
            }
            catch (Exception e)
            {
                Console.WriteLine("ConnectTest Error: {0}", e.Message);
            }
        }

        #endregion
    }
}
