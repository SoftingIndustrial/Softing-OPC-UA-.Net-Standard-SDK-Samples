/* ========================================================================
 * Copyright © 2011-2024 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
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
    public class ReverseConnectClient
    {
        #region Private Fields

        private readonly UaApplication m_application;
        private string m_reverseConnectUrl;
        private string m_serverApplicationUri;
        private CertificateIdentifier m_reverseConnectServerCertificateIdentifier;

        private int m_maximumWaitForReverseConnectRequest = 45000;
        private static EndpointsDescriptionSearchingState m_endpointsDescriptionSearchingState;
        private Dictionary<string, EndpointDescriptionEx> m_receivedEndpointsDescription = new Dictionary<string, EndpointDescriptionEx>();

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

        #region Reverse Connect Methods

        /// <summary>
        /// Get all server endpoints using Reverse connect mechanism and then create a Reverse Connect session to each of them
        /// </summary>
        /// <param name="connectAsync">flag that indicates whether the connect will be performed asynchronously</param>
        public async Task GetEndpointsAndReverseConnect(bool connectAsync)
        {
            try
            {
                Console.WriteLine("\nGet Endpoints of '{0}' using reverse connection endpoint '{1}'", m_serverApplicationUri, m_reverseConnectUrl);
                var endpoints = await m_application.GetEndpointsAsync(m_reverseConnectUrl, m_serverApplicationUri).ConfigureAwait(false);
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

                        Console.WriteLine("\n\tCreate session for endpoint: {0}", endpointToString);
                        ClientSession session = CreateReverseConnectSession("ReverseConnectSession" + index++, m_serverApplicationUri,
                            endpoint.SecurityMode, (SecurityPolicy)Enum.Parse(typeof(SecurityPolicy), endpoint.SecurityPolicy),
                            endpoint.Encoding[0], new UserIdentity());

                        // set discovery endpoint on session
                        session.InitializeWithDiscoveryEndpointDescription(endpoint);
                        if (connectAsync)
                        {
                            // trigger connect session asynchronously. The execution will continue immediately and will not wait for the method to complete
                            _ = ConnectTestAsync(session).ConfigureAwait(false);
                        }
                        else
                        {
                            await ConnectTest(session).ConfigureAwait(false);
                            // session was disconnected it is safe to dispose it
                            session.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.PrintException("GetEndpointsAndReverseConnect.CreateConnection to endpoint:" + endpoint, ex);
                    }
                    //break;
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("GetEndpointsAndReverseConnect", ex);
            }
        }

        /// <summary>
        /// Get and connect to the reverse connections detected into a specified interval and then create a Reverse Connect session to each of them
        /// </summary>
        /// <param name="connectAsync">flag that indicates whether the connect will be performed asynchronously</param>
        /// <returns></returns>
        public async Task GetEndpointsAndReverseConnectTimeoutInterval(bool connectAsync)
        {
            try
            {
                Console.WriteLine("\nDetect reverse connection endpoints '{0}' into a specified time interval. \nPlease wait {1} milliseconds to receive detected connection endpoints.", m_reverseConnectUrl, m_maximumWaitForReverseConnectRequest);

                m_receivedEndpointsDescription.Clear();

                Console.WriteLine("\n\tDetecting active endpoints.\n");

                m_endpointsDescriptionSearchingState = new EndpointsDescriptionSearchingState(m_maximumWaitForReverseConnectRequest);
                m_application.GetEndpointsReceived += OnEndpointsReceived;
                var endpoints = await m_application.GetEndpointsAsync(m_reverseConnectUrl, null, m_endpointsDescriptionSearchingState).ConfigureAwait(false);
                m_application.GetEndpointsReceived -= OnEndpointsReceived;

                Console.WriteLine("\n\tDetected active endpoints: {0}.", m_receivedEndpointsDescription.Count);
               
                int index = 0;
                string endpointToString = string.Empty;
                foreach (EndpointDescriptionEx endpoint in m_receivedEndpointsDescription.Values)
                {
                    try
                    {
                        endpointToString = string.Format("{0} - {1} - {2}",
                               endpoint.EndpointUrl,
                               endpoint.SecurityMode,
                               endpoint.SecurityPolicy);

                        Console.WriteLine("\n\tCreate session for endpoint: {0}", endpointToString);
                        ClientSession session = CreateReverseConnectSession("ReverseConnectSession" + index++, endpoint.ApplicationUri,
                            endpoint.SecurityMode, (SecurityPolicy)Enum.Parse(typeof(SecurityPolicy), endpoint.SecurityPolicy),
                            endpoint.Encoding[0], new UserIdentity());

                        // set discovery endpoint on session
                        session.InitializeWithDiscoveryEndpointDescription(endpoint);
                        if (connectAsync)
                        {
                            // trigger connect session asynchronously. The execution will continue immediately and will not wait for the method to complete
                            _ = ConnectTestAsync(session).ConfigureAwait(false);
                        }
                        else
                        {
                            await ConnectTest(session).ConfigureAwait(false);
                            // session was disconnected it is safe to dispose it
                            session.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.PrintException(string.Format("GetEndpointsAndReverseConnectTimeoutInterval.CreateConnection for endpoint {0}:", endpoint), ex);
                    }
                    //break;
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("GetEndpointsAndReverseConnectTimeoutInterval", ex);
            }
        }

        /// <summary>
        /// Creates and connects a reverse session on opc.tcp protocol with no security and anonymous user identity.
        /// This sample will get the server application URI from the server certificate specified in sample client config file
        /// </summary>
        public async Task CreateOpcTcpSessionWithNoSecurity()
        {
            string reverseConnectServerApplicationUri = m_serverApplicationUri;
            try
            {
                // create the session object using the server certificate identifier from config file
                if (m_reverseConnectServerCertificateIdentifier != null)
                {
                    // This method will find the first certificate with the specified CertificateIdentifier.SubjectName from the specified CertificateIdentifier.StorePath
                    X509Certificate2 reverseConnectServerCertificate = await m_reverseConnectServerCertificateIdentifier.Find(false).ConfigureAwait(false);

                    if (reverseConnectServerCertificate != null)
                    {
                        // Use utility method to get the applicationUri from a certificate
                        reverseConnectServerApplicationUri = X509Utils.GetApplicationUriFromCertificate(reverseConnectServerCertificate);
                        Console.WriteLine("The configured ReverseConnectServerCertificateIdentifier has ApplicationUri='{0}'", reverseConnectServerApplicationUri);
                    }
                    else
                    {
                        Console.WriteLine("The configured ReverseConnectServerCertificateIdentifier cannot be found. \nThe ReverseConnectServerApplicationUri='{0}' will be used. ",
                            m_serverApplicationUri);
                    }
                }
                else
                {
                    Console.WriteLine("The ReverseConnectServerCertificateIdentifier is NULL. \nThe ReverseConnectServerApplicationUri='{0}' will be used. ",
                        m_serverApplicationUri);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateOpcTcpSessionWithNoSecurity using configured ReverseConnectServerCertificateIdentifier", ex);
            }

            using (ClientSession session = CreateReverseConnectSession("UaBinaryNoSecurityReverseConnectSession", reverseConnectServerApplicationUri,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity()))
            {
                await ConnectTest(session).ConfigureAwait(false);
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Creates a new reverse connect session with the specified parameters.
        /// </summary>
        /// <param name="sessionName"></param>
        /// <param name="serverApplicationUri"></param>
        /// <param name="securityMode"></param>
        /// <param name="securityPolicy"></param>
        /// <param name="messageEncoding"></param>
        /// <param name="userId"></param>
        /// <returns></returns>        
        private ClientSession CreateReverseConnectSession(string sessionName, string serverApplicationUri, MessageSecurityMode securityMode,
            SecurityPolicy securityPolicy, MessageEncoding messageEncoding, UserIdentity userId)
        {
            try
            {
                Console.WriteLine("\r\nCreating the reverse connect session {0} to ServerApplicationUri: '{1}' \n(SecurityMode = {2}, SecurityPolicy = {3}, UserIdentity = {4})...",
                    sessionName, serverApplicationUri, securityMode, securityPolicy, userId.GetIdentityToken());
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
                Program.PrintException("ReverseConnectClient.CreateReverseConnectSession", ex);
                return null;
            }
        }

        /// <summary>
        /// Performs a Connect/Disconnect test for the specified session.
        /// </summary>
        /// <param name="session"></param>
        private async Task ConnectTest(ClientSession session)
        {
            try
            {
                // Attempt to connect to server.
                Console.WriteLine("Connecting session {0}...", session.SessionName);

                await session.ConnectAsync(false, true).ConfigureAwait(false);

                //Console.WriteLine("Session state = {0}. Success!", session.CurrentState);
                Console.WriteLine("Session state = {0} for endpoint = {1}. Success!", session.CurrentState, session.Url);

                // Disconnect the session.
                await session.DisconnectAsync(true).ConfigureAwait(false);

                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Program.PrintException("ConnectClient.ConnectTest", ex);
            }
        }

        /// <summary>
        /// Performs a Connect/Disconnect asyncronously test for the specified session.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private async Task ConnectTestAsync(ClientSession session)
        {
            try
            {
                if (session == null)
                {
                    Console.WriteLine("Session instance is missing !");
                }

                // handle StateChanged event for session
                session.StateChanged += AsyncSessionStateChanged;

                Console.WriteLine("\nTrigger session.ConnectAsync for session {0}...", session.SessionName);
                await session.ConnectAsync(false, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Program.PrintException("ConnectClient.ConnectTestAsync", ex);
            }
        }

        /// <summary>
        /// Session state changes notifications
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AsyncSessionStateChanged(object sender, System.EventArgs e)
        {
            ClientSession clientSession = sender as ClientSession;

            if (clientSession == null)
            {
                return;
            }

            if (clientSession.CurrentState == State.Connecting)
            {
                Console.WriteLine("Changed session '{0}' state = {1}", clientSession.SessionName, clientSession.CurrentState);
            }
            else
            {
                Console.WriteLine("Changed session '{0}' state = {1} for endpoint = {2}", clientSession.SessionName, clientSession.CurrentState, clientSession.Url);
            }

            try
            {
                if (clientSession.CurrentState == State.Active)
                {
                    // trigger session disconnect
                    Console.WriteLine("\nTrigger session.DisconnectAsync for session {0}...", clientSession.SessionName);
                    await clientSession.DisconnectAsync(true).ConfigureAwait(false);
                }
                else if (clientSession.CurrentState == State.Disconnected)
                {
                    // unregister event
                    clientSession.StateChanged -= AsyncSessionStateChanged;

                    // trigger session Dispose since it already disconnected
                    Console.WriteLine("Trigger session.Dispose for session {0}.\n", clientSession.SessionName);
                    clientSession.Dispose();
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("AsyncSessionStateChanged", ex);
            }
        }

        private void OnEndpointsReceived(object sender, EndpointsDescriptionEventArgs e)
        {
            string endpointToString = string.Empty;
            foreach (var endpoint in e.Endpoints)
            {
                try
                {
                    endpointToString = string.Format("{0} - {1} - {2}",
                           endpoint.EndpointUrl,
                           endpoint.SecurityMode,
                           endpoint.SecurityPolicyUri);

                    if(!m_receivedEndpointsDescription.ContainsKey(endpointToString))
                    {
                        Console.WriteLine("\tDetected reverse connect endpoint: {0}", endpointToString);
                        m_receivedEndpointsDescription.Add(endpointToString, new EndpointDescriptionEx(endpoint));
                    }
                }
                catch (Exception ex)
                {
                    Program.PrintException(string.Format("ExecuteReverseConnectSample.OnEndpointsReceived endpoint {0} :", endpointToString), ex);
                }
            }
        }
        #endregion
    }
}
