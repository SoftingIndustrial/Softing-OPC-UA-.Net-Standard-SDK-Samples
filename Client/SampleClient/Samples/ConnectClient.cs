/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Opc.Ua;
using Softing.Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class providing sample code for connect operations with different configuration parameters.
    /// 
    /// This class also provides sample code for creating ApplicationConfiguration object by code 
    /// </summary>
    public class ConnectClient
    {
        /// <summary>
        /// The default reconnect period in ms.
        /// </summary>
        public const int DefaultReconnectPeriod = 10000;

        #region Private Fields

        private readonly UaApplication m_application;

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of ConnectClient
        /// The constructor will create a new instance of UaApplication
        /// </summary>
        public ConnectClient()
        {
            // Alternatively, instead of providing a config XML file, we can provide instead, the custom configuration set below
            // ApplicationConfiguration configuration = CreateAplicationConfiguration();
            // m_application = UaApplication.Create(configuration).Result;
            m_application = UaApplication.Create("SampleClient.Config.xml").Result;

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = false;
        }

        /// <summary>
        /// Create an sintance of ConnectioClient that uses the provided<see cref="UaApplication"/>.
        /// </summary>
        /// <param name="uaApplication"></param>
        public ConnectClient(UaApplication uaApplication)
        {
            m_application = uaApplication;
        }

        #endregion

        #region Connect Methods

        /// <summary>
        /// Creates and connects a session on opc.tcp or https protocol using endpoint and discovery URLs.
        /// </summary>
        public async Task CreateSessionAndConnectToEndpointWithDiscovery()
        {
            string discoveryUrl = Program.ServerUrl;
            string endpointUrl = Program.ServerUrl;

            var session = m_application.CreateSession(
                discoveryUrl,
                endpointUrl,
                MessageSecurityMode.None,
                SecurityPolicy.None,
                MessageEncoding.Binary,
                new UserIdentity(),
                null);

            // Connect the session.
            await ConnectTest(session).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and anonymous user identity.
        /// </summary>
        public async Task CreateOpcTcpSessionWithNoSecurity()
        {
            // create the session object.
            using (ClientSession session = CreateSession("UaBinaryNoSecuritySession", Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity()))
            {
                await ConnectTest(session).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and a username/password user identity.
        /// </summary>
        public async Task CreateOpcTcpSessionWithUserId()
        {
            // create the session object.
            using (ClientSession session = CreateSession("UaBinaryUserIdSession", Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity("usr", "pwd")))
            {
                await ConnectTest(session).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and a certificate user identity.
        /// </summary>
        public async Task CreateOpcTcpSessionWithCertificate()
        {
            try
            {
                // use the opcuser.pfx certificate file located in Files folder
                string certificateFilePath = Path.Combine("Files", "opcuser.pfx");
                if (!File.Exists(certificateFilePath))
                {
                    Console.WriteLine("The user certificate file is missing ('{0}').", certificateFilePath);
                    return;
                }
                // load the certificate from file
                X509Certificate2 certificate = new X509Certificate2(certificateFilePath,
                               null as string,
                               X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

                if (certificate != null)
                {
                    // create UserIdentity from certificate
                    UserIdentity certificateUserIdentity = new UserIdentity(certificate);

                    Console.WriteLine("\r\nCreate session using certificate located at '{0}'", certificateFilePath);
                    // create the session object.
                    using (ClientSession session = CreateSession("UaBinaryUserCertificateSession", Program.ServerUrl,
                        MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, certificateUserIdentity))
                    {
                        await ConnectTest(session).ConfigureAwait(false);
                    }
                }
                else
                {
                    Console.WriteLine("Cannot load certificate from '{0}'", certificateFilePath);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateOpcTcpSessionWithCertificate", ex);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with user identity and ECC.
        /// </summary>
        /// <param name="securityPolicy"></param>
        /// <returns></returns>
        public async Task CreateOpcTcpSessionWithUserIdUsingECC(SecurityPolicy securityPolicy)
        {
            bool isECCSupported = false;
            switch (securityPolicy)
            {
                case SecurityPolicy.ECC_nistP256:
                case SecurityPolicy.ECC_nistP384:
                case SecurityPolicy.ECC_brainpoolP256r1:
                case SecurityPolicy.ECC_brainpoolP384r1:
                    isECCSupported = true;
                    break;
            }

            if (!isECCSupported)
            {
                Console.WriteLine("The user certificate file with specified ECC security policy ('{0}') not supported.", securityPolicy);
                return;
            }

            // create the session object.
            using (ClientSession session = CreateSession("UaBinaryUserIdSession", Program.ServerUrl,
                MessageSecurityMode.SignAndEncrypt, securityPolicy, MessageEncoding.Binary, new UserIdentity("usr", "pwd")))
            {
                await ConnectTest(session).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and a certificate user identity using ECC.
        /// </summary>
        public async Task CreateOpcTcpSessionWithCertificateUsingECC(SecurityPolicy securityPolicy)
        {
            try
            {
                bool isECCSupported = false;
                switch (securityPolicy)
                {
                    case SecurityPolicy.ECC_nistP256:
                    case SecurityPolicy.ECC_nistP384:
                    case SecurityPolicy.ECC_brainpoolP256r1:
                    case SecurityPolicy.ECC_brainpoolP384r1:
                        isECCSupported = true;
                        break;
                }

                if (!isECCSupported)
                {
                    Console.WriteLine("The user certificate file with specified ECC security policy ('{0}') not supported.", securityPolicy);
                    return;
                }

                // use the opcuser.pfx certificate file located in Files folder
                string certificateFilePath = Path.Combine(@"Files\ECC", "opcuser_nistP256.pfx");
                if (!File.Exists(certificateFilePath))
                {
                    Console.WriteLine("The user certificate file is missing ('{0}').", certificateFilePath);
                    return;
                }
                // load the certificate from file
                X509Certificate2 certificate = new X509Certificate2(certificateFilePath,
                               null as string,
                               X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

                if (certificate != null)
                {
                    // create UserIdentity from certificate
                    UserIdentity certificateUserIdentity = new UserIdentity(certificate);


                    Console.WriteLine("\r\nCreate session using certificate located at '{0}'", certificateFilePath);
                    // create the session object.
                    using (ClientSession session = CreateSession("UaBinaryUserCertificateSession", Program.ServerUrl,
                        MessageSecurityMode.SignAndEncrypt, securityPolicy, MessageEncoding.Binary, certificateUserIdentity))
                    {
                        await ConnectTest(session).ConfigureAwait(false);
                    }
                }
                else
                {
                    Console.WriteLine("Cannot load certificate from '{0}'", certificateFilePath);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateOpcTcpSessionWithCertificate", ex);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and a certificate with password user identity.
        /// </summary>
        public async Task CreateOpcTcpSessionWithCertificatePassword()
        {
            try
            {
                // use the opcuserPwd.pfx certificate file located in Files folder
                string certificateFilePath = Path.Combine("Files", "opcuserPwd.pfx");
                if (!File.Exists(certificateFilePath))
                {
                    Console.WriteLine("The user certificate file is missing ('{0}').", certificateFilePath);
                    return;
                }
                // load the certificate with password from file
                X509Certificate2 certificate = new X509Certificate2(certificateFilePath,
                               "User_Pwd",
                               X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

                if (certificate != null)
                {
                    // create UserIdentity from certificate
                    UserIdentity certificateUserIdentity = new UserIdentity(certificate);

                    Console.WriteLine("\r\nCreate session using certificate located at '{0}'", certificateFilePath);
                    // create the session object.
                    using (ClientSession session = CreateSession("UaBinaryUserCertificateWithPasswordSession", Program.ServerUrl,
                        MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, certificateUserIdentity))
                    {
                        await ConnectTest(session).ConfigureAwait(false);
                    }
                }
                else
                {
                    Console.WriteLine("Cannot load certificate from '{0}'", certificateFilePath);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("UaBinaryUserCertificateWithPasswordSession", ex);
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with security and anonymous user identity.
        /// </summary>
        /// <param name="messageSecurityMode"> Desired security mode</param>
        /// <param name="securityPolicy"> Desired security policy</param>
        public async Task CreateOpcTcpSessionWithSecurity(MessageSecurityMode messageSecurityMode, SecurityPolicy securityPolicy)
        {
            // create the session object.
            using (ClientSession session = CreateSession("UaBinarySecureSession", Program.ServerUrl,
                messageSecurityMode, securityPolicy, MessageEncoding.Binary,
                new UserIdentity()))
            {
                await ConnectTest(session).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates and connects a session on HTTPS protocol with anonymous user identity and using xml message encoding.
        /// </summary>
        public async Task CreateHttpsSessionWithAnonymousUserId()
        {
            // create the session object.
            using (ClientSession session = CreateSession("HttpsAnonymousUserIdSession", Program.ServerUrlHttps,
                MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256, MessageEncoding.Binary, new UserIdentity()))
            {
                await ConnectTest(session).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates and connects a session on HTTPS protocol with a username/password user identity.
        /// </summary>
        public async Task CreateHttpsSessionWithUserId()
        {
            // create the session object.
            using (ClientSession session = CreateSession("HttpsUserIdSession", Program.ServerUrlHttps,
                MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256, MessageEncoding.Binary, new UserIdentity("usr", "pwd")))
            {
                await ConnectTest(session).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates and connects a session using the Discovery process.
        /// </summary>
        public async Task CreateSessionUsingDiscovery()
        {
            try
            {
                // Retrieve the list of available server connection channels by calling GetEndpoints on the Server's discovery endpoint.
                Console.WriteLine("\r\nDiscovering available endpoints...");
                IList<EndpointDescriptionEx> endpoints = await m_application.GetEndpointsAsync(Program.ServerUrl).ConfigureAwait(false);

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
                        (SecurityPolicy)Enum.Parse(typeof(SecurityPolicy), selectedEndpoint.SecurityPolicy),
                        selectedEndpoint.Encoding[0],
                        new UserIdentity()))
                    {
                        await ConnectTest(session).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ConnectClient.CreateSessionUsingDiscovery", ex);
            }
        }

        /// <summary>
        /// Create session, Connect and Reconnect using SessionReconnectHandler
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAndReconnectUsingSessionReconnectHandler()
        {
            ClientSession session = CreateSession("SessionReconnectHandler", Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity());

            ClientSession.CurrentReconnectMode = ReconnectMode.SessionReconnectHandler;

            session.ReconnectPeriod = DefaultReconnectPeriod;

            Console.WriteLine("Connecting session {0}...", session.SessionName);
            await session.ConnectAsync(false, true).ConfigureAwait(false);
            Console.WriteLine("Session state = {0}. Success!", session.CurrentState);
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
            configuration.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };
            configuration.ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 };

            ClientToolkitConfiguration clientTkConfiguration = new ClientToolkitConfiguration();
            clientTkConfiguration.DiscoveryOperationTimeout = 10000;
            configuration.UpdateExtension<ClientToolkitConfiguration>(new System.Xml.XmlQualifiedName("ClientToolkitConfiguration"), clientTkConfiguration);

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
        /// Creates a new session with the specified parameters.
        /// </summary>        
        private ClientSession CreateSession(string sessionName, string serverUrl, MessageSecurityMode securityMode,
            SecurityPolicy securityPolicy, MessageEncoding messageEncoding, UserIdentity userId)
        {
            try
            {
                Console.WriteLine("\r\nCreating the session {0} (SecurityMode = {1}, SecurityPolicy = {2}, \r\n\t\t\t\t\t\tUserIdentity = {3})...",
                    sessionName, securityMode, securityPolicy, userId.GetIdentityToken());
                // Create the Session object.
                ClientSession session = m_application.CreateSession(serverUrl, serverUrl, securityMode, securityPolicy, messageEncoding, userId);

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
        private async Task ConnectTest(ClientSession session)
        {
            try
            {
                ClientSession.CurrentReconnectMode = ReconnectMode.LegacyImplementation;

                // Attempt to connect to server.
                Console.WriteLine("Connecting session {0}...", session.SessionName);
                await session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine("Session state = {0}. Success!", session.CurrentState);

                // Disconnect the session.
                await session.DisconnectAsync(true).ConfigureAwait(false);
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Program.PrintException("ConnectClient.ConnectTest", ex);
            }
        }
        #endregion
    }
}