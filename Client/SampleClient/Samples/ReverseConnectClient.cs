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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
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
        private CertificateIdentifier m_reverseConnectServerCertificateIdentifier;
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
                m_reverseConnectServerCertificateIdentifier = sampleClientConfiguration.ReverseConnectServerCertificateIdentifier;
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
                Console.WriteLine("\nGet Endpoints of '{0}' using reverse connection endpoint '{1}'", m_serverApplicationUri, m_reverseConnectUrl);
                var endpoints = m_application.GetEndpoints(m_reverseConnectUrl, m_serverApplicationUri);
                Console.WriteLine("The server returned {0} endpoints.", endpoints.Count);
                int index = 0;
                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        string endpointToString = string.Format("{0} - {1} - {2}",
                                endpoint.EndpointUrl,
                                endpoint.SecurityMode,
                                endpoint.SecurityPolicy);
                        Console.WriteLine("\n\tCreate session to endpoint: {0}", endpointToString);
                        using (ClientSession session = CreateReverseConnectSession("ReverseConnectSession" + index++, m_serverApplicationUri,
                            endpoint.SecurityMode, (SecurityPolicy)Enum.Parse(typeof(SecurityPolicy), endpoint.SecurityPolicy),
                            endpoint.Encoding[0], new UserIdentity()))
                        {                            
                            session.InitializeWithDiscoveryEndpointDescription(endpoint);
                            ConnectTest(session);
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
        /// Get all server endpoints suing Reverse connect mechanism and then create a Reverse Connect session to each of them
        /// </summary>
        public async Task GetEndpointsAndReverseConnectAsync()
        {
            try
            {
                Console.WriteLine("Get Endpoints of '{0}' using reverse connection endpoint '{1}'", m_serverApplicationUri, m_reverseConnectUrl);
                var endpoints = m_application.GetEndpoints(m_reverseConnectUrl, m_serverApplicationUri);
                Console.WriteLine("The server returned {0} endpoints.", endpoints.Count);
                int index = 0;
                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        string endpointToString = string.Format("{0} - {1} - {2}",
                                endpoint.EndpointUrl,
                                endpoint.SecurityMode,
                                endpoint.SecurityPolicy);
                        Console.WriteLine("\n\tCreate session to endpoint: {0}", endpointToString);
                        ClientSession session = CreateReverseConnectSession("ReverseConnectSession" + index++, m_serverApplicationUri,
                            endpoint.SecurityMode, (SecurityPolicy)Enum.Parse(typeof(SecurityPolicy), endpoint.SecurityPolicy),
                            endpoint.Encoding[0], new UserIdentity());

                        session.InitializeWithDiscoveryEndpointDescription(endpoint);
                        ConnectClient.ConnectTestAsync(session);
                        
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
        /// This sample will get the server application URI from the server certificate specified in sample client config file
        /// </summary>
        public void CreateOpcTcpSessionWithNoSecurity()
        {
            string reverseConnectServerApplicationUri = m_serverApplicationUri;
            try
            {
                // create the session object using the server certificate identifier from config file
                if (m_reverseConnectServerCertificateIdentifier != null)
                {
                    X509Certificate2 reverseConnectServerCertificate = m_reverseConnectServerCertificateIdentifier.Find(false).Result;

                    if (reverseConnectServerCertificate != null)
                    {
                        // Use utility method to get the applicationUri from a certificate
                        reverseConnectServerApplicationUri = Utils.GetApplicationUriFromCertificate(reverseConnectServerCertificate);
                        Console.WriteLine("The configured ReverseConnectServerCertificateIdentifier has ApplicationUri='{0}'", reverseConnectServerApplicationUri);
                    }
                    else
                    {
                        Console.WriteLine("The configured ReverseConnectServerCertificateIdentifier cannot be found.");
                    }
                }
                else
                {
                    Console.WriteLine("The ReverseConnectServerCertificateIdentifier is NULL.");
                }
            }
            catch(Exception ex)
            {
                Program.PrintException("CreateOpcTcpSessionWithNoSecurity using configured ReverseConnectServerCertificateIdentifier", ex);
            }
            using (ClientSession session = CreateReverseConnectSession("UaBinaryNoSecurityReverseConnectSession", reverseConnectServerApplicationUri,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity()))
            {
                ConnectTest(session);
            }
        }

        /// <summary>
        /// Creates and connects a reverse session on opc.tcp protocol with no security and anonymous user identity.
        /// The session will be connected using the ConnectAsync method.
        /// </summary>
        /// <returns></returns>
        public async Task CreateOpcTcpSessionWithNoSecurityAsync()
        {
            // create the session object.
            ClientSession session = CreateReverseConnectSession("UaBinaryNoSecurityReverseConnectSession", m_serverApplicationUri,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity());

            await ConnectClient.ConnectTestAsync(session);
            Console.WriteLine();
        }

        /// <summary>
        /// Creates a new reverse connect session with the specified parameters.
        /// </summary>        
        private ClientSession CreateReverseConnectSession(string sessionName, string serverApplicationUri, MessageSecurityMode securityMode,
            SecurityPolicy securityPolicy, MessageEncoding messageEncoding, UserIdentity userId)
        {
            try
            {
                Console.WriteLine("\r\nCreating the reverse connect session {0} (SecurityMode = {1}, SecurityPolicy = {2}, UserIdentity = {3})...",
                    sessionName, securityMode, securityPolicy, userId.GetIdentityToken());
                // Create the Reverse Connect Session object.    
                ClientSession session = m_application.CreateReverseConnectSession(m_reverseConnectUrl, serverApplicationUri,
                    securityMode, securityPolicy, messageEncoding, userId);
                Console.WriteLine("The session was created.");
                session.Timeout = 100000;
                session.MaximumWaitForReverseConnectRequest = 100000;
                session.SessionName = sessionName;
                return session;
            }
            catch (Exception ex)
            {
                Program.PrintException("ConnectClient.CreateSession", ex);
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
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Program.PrintException("ConnectClient.ConnectTest", ex);
            }
        }
    }
}
