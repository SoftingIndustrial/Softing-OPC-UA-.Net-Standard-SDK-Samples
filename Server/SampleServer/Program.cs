/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Opc.Ua;
using Softing.Opc.Ua.Configuration;

namespace SampleServer
{
    /// <summary>
    /// SampleServer entry class
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ConsoleUtils.WindowsConsoleUtils.WindowsConsole.DisableQuickEdit();
            }

            StartServer();
        }

        /// <summary>
        /// Start Server and listen for console commands 
        /// </summary>
        private static async void StartServer()
        {
            string configurationFile = "SampleServer.Config.xml";
            SampleServer sampleServer = new SampleServer();

            try
            {

                Softing.Opc.Ua.Server.LicensingStatus serverLicensingStatus = Softing.Opc.Ua.Server.LicensingStatus.Ok;

                // TODO - Server binary license activation
                // Fill in your Server binary license activation keys here
                //serverLicensingStatus = Softing.Opc.Ua.Server.License.ActivateLicense(Softing.Opc.Ua.Server.LicenseFeature.Server, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");

                if (serverLicensingStatus != Softing.Opc.Ua.Server.LicensingStatus.Ok)
                {
                    Console.WriteLine("Server license status is: {0}!", serverLicensingStatus);
                    Console.ReadKey();
                    return;
                }

                // Load server default (customized) configuration build with a fluent API
                // ApplicationConfigurationBuilderEx defaultConfiguration = LoadDefaultConfiguration().Result;

                // Start the server using an ApplicationConfiguration build with a fluent API
                // await sampleServer.Start(defaultConfiguration).ConfigureAwait(false);

                // Start the server using a configuration file
                await sampleServer.Start(configurationFile).ConfigureAwait(false);

                PrintServerInformation(sampleServer);

                PrintCommandParameters();
                
                // wait for console commands
                bool exit = false;
                while (!exit)
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    
                    if (key.KeyChar == 'q' || key.KeyChar == 'x')
                    {
                        Console.WriteLine("\nShutting down...");
                        exit = true;
                    }

                    else if (key.KeyChar == 'c' && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        string endpoint = sampleServer.Configuration.ServerConfiguration.BaseAddresses[0];
                        Console.WriteLine(String.Format("\nCopied {0} to clipboard", endpoint));
                        ConsoleUtils.WindowsConsoleUtils.WindowsClipboard.SetTextValue(endpoint);
                    }
                    else if (key.KeyChar == 's')
                    {
                        PrintSessionList(sampleServer);
                    }
                    else
                    {
                        PrintCommandParameters();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadKey();
                Environment.Exit(-1);
            }
            finally
            {
                sampleServer.Stop();
            }
        }

        #region Load customized configuration

        /// <summary>
        /// Load default configuration
        /// </summary>
        /// <returns></returns>
        private static async Task<ApplicationConfigurationBuilderEx> LoadDefaultConfiguration()
        {
            ApplicationConfigurationBuilderEx applicationConfigurationBuilder = new ApplicationConfigurationBuilderEx();


            await applicationConfigurationBuilder
                .Initialize("urn: localhost:Softing: UANETStandardToolkit:SampleServer",
                        "http://industrial.softing.com/OpcUaNetStandardToolkit/SampleServer")
                .SetApplicationName("Softing NET Standard Sample Server")
                .DisableHiResClock(true)
                .SetTransportQuotas(new Opc.Ua.TransportQuotas()
                {
                    OperationTimeout = 600000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                })
                .AsServer(new string[] { "opc.tcp://localhost:61510/SampleServer" })
                    .AddUnsecurePolicyNone()
                    //.AddPolicy(Opc.Ua.MessageSecurityMode.None, "http://opcfoundation.org/UA/SecurityPolicy#None")
                    //.AddSignAndEncryptPolicies()
                    .AddPolicy(Opc.Ua.MessageSecurityMode.Sign, "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256")
                    .AddPolicy(Opc.Ua.MessageSecurityMode.SignAndEncrypt, "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256")
                    .AddPolicy(Opc.Ua.MessageSecurityMode.Sign, "http://opcfoundation.org/UA/SecurityPolicy#Aes128_Sha256_RsaOaep")
                    .AddPolicy(Opc.Ua.MessageSecurityMode.SignAndEncrypt, "http://opcfoundation.org/UA/SecurityPolicy#Aes128_Sha256_RsaOaep")
                    .AddPolicy(Opc.Ua.MessageSecurityMode.Sign, "http://opcfoundation.org/UA/SecurityPolicy#Aes256_Sha256_RsaPss")
                    .AddPolicy(Opc.Ua.MessageSecurityMode.SignAndEncrypt, "http://opcfoundation.org/UA/SecurityPolicy#Aes256_Sha256_RsaPss")
                    .AddUserTokenPolicy(new Opc.Ua.UserTokenPolicy() { TokenType = Opc.Ua.UserTokenType.Anonymous, SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None" })
                    .AddUserTokenPolicy(new Opc.Ua.UserTokenPolicy() { TokenType = Opc.Ua.UserTokenType.UserName, SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256" })
                    .AddUserTokenPolicy(new Opc.Ua.UserTokenPolicy() { TokenType = Opc.Ua.UserTokenType.Certificate, SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256" })
                    .SetDiagnosticsEnabled(true)
                    .SetMaxSessionCount(100)
                    .SetMinSessionTimeout(10000)
                    .SetMaxSessionTimeout(3600000)
                    .SetMaxBrowseContinuationPoints(10)
                    .SetMaxQueryContinuationPoints(10)
                    .SetMaxHistoryContinuationPoints(100)
                    .SetMaxRequestAge(600000)
                    .SetMinPublishingInterval(100)
                    .SetMaxPublishingInterval(3600000)
                    .SetPublishingResolution(50)
                    .SetMaxSubscriptionLifetime(3600000)
                    .SetMaxMessageQueueSize(100)
                    .SetMaxNotificationQueueSize(100)
                    .SetMaxNotificationsPerPublish(1000)
                    .SetMinMetadataSamplingInterval(1000)
                    .SetAvailableSamplingRates(new Opc.Ua.SamplingRateGroupCollection() {
                        new Opc.Ua.SamplingRateGroup(){Start=5, Increment=5, Count=20},
                        new Opc.Ua.SamplingRateGroup(){Start=100, Increment=100, Count=4},
                        new Opc.Ua.SamplingRateGroup(){Start=500, Increment=250, Count=2},
                        new Opc.Ua.SamplingRateGroup(){Start=100, Increment=500, Count=20},
                    })
                    .SetMaxRegistrationInterval(30000)
                    .SetNodeManagerSaveFile("SampleServer.nodes.xml")
                    .SetMinSubscriptionLifetime(10000)
                    .SetMaxPublishRequestCount(100)
                    .SetMaxSubscriptionCount(200)
                    .SetMaxEventQueueSize(10000)
                    .AddServerProfile("http://opcfoundation.org/UA-Profile/Server/StandardUA2017")
                    .AddServerProfile("http://opcfoundation.org/UA-Profile/Server/DataAccess")
                    .AddServerProfile("http://opcfoundation.org/UA-Profile/Server/Methods")
                    .AddServerProfile("http://opcfoundation.org/UA-Profile/Server/ReverseConnect")
                    .SetMaxTrustListSize(0)
                    .SetMultiCastDnsEnabled(false)
                    .SetReverseConnect(new Opc.Ua.ReverseConnectServerConfiguration()
                    {
                        Clients = new Opc.Ua.ReverseConnectClientCollection()
                        {
                            new Opc.Ua.ReverseConnectClient()
                            { EndpointUrl="opc.tcp://localhost:61512", Timeout=30000, MaxSessionCount=0, Enabled=true}
                        },
                        ConnectInterval = 10000,
                        ConnectTimeout = 30000,
                        RejectTimeout = 20000
                    })
                .AddSecurityConfigurationExt(
                    "SoftingOpcUaSampleServer",
                    "%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/pki",
                    "%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/pki",
                    "%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/pki")
                    .SetAddAppCertToTrustedStore(true)
                    .SetAutoAcceptUntrustedCertificates(false)
                    .SetRejectSHA1SignedCertificates(false)
                    .SetRejectUnknownRevocationStatus(false)
                    .SetMinimumCertificateKeySize(1024)
                //.SetUserRoleDirectory("%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/userRoles")
                .AddExtension<SampleServerConfiguration>(new XmlQualifiedName("SampleServerConfiguration"),
                    new SampleServerConfiguration() { TimerInterval = 1000, ClearCachedCertificatesInterval = 30000 })
                .SetTraceMasks(1)
                .SetOutputFilePath("%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/logs/SampleServer.log")
                .SetDeleteOnLoad(true)
                .Create();

            await applicationConfigurationBuilder.CheckApplicationInstanceCertificate(true, 2048);

            return applicationConfigurationBuilder;
        }

        #endregion

        #region Print State Methods

        /// <summary>
        /// Display the Server information including Reverse connections and ActiveListern Uris
        /// </summary>
        /// <param name="sampleServer"></param>
        private static void PrintServerInformation(SampleServer sampleServer)
        {
            // print configured reverse connections
            var reverseConnections = sampleServer.GetReverseConnections();
            if (reverseConnections?.Count > 0)
            {                
                Console.WriteLine("Configured Reverse Connections:");
                foreach (var connection in reverseConnections)
                {
                    Console.WriteLine(connection.Key);
                }
            }

            // print the available server addresses
            List<string> activeListenersUris = sampleServer.GetActiveListenersUris();
            if (activeListenersUris?.Count > 0)
            {
                Console.WriteLine("\nServer Addresses:");
                for (int i = 0; i < sampleServer.Configuration.ServerConfiguration.BaseAddresses.Count; i++)
                {
                    if (activeListenersUris.Contains(sampleServer.Configuration.ServerConfiguration.BaseAddresses[i]))
                    {
                        // the configured base address is available
                        Console.WriteLine(sampleServer.Configuration.ServerConfiguration.BaseAddresses[i]);
                    }
                    else
                    {
                        // the configured base address is not available
                        Console.WriteLine("{0} address not available.", sampleServer.Configuration.ServerConfiguration.BaseAddresses[i]);
                    }
                }
                Console.WriteLine("SampleServer started at: {0}", DateTime.Now.ToLongTimeString());
            }
        }

        /// <summary>
        /// Print command line parameters
        /// </summary>
        private static void PrintCommandParameters()
        {
            Console.WriteLine("Press:");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("\tc: copy endpoint to clipboard");
            }

            Console.WriteLine("\ts: session list");
            Console.WriteLine("\tx,q: shutdown the server\n\n");
        }

        /// <summary>
        /// Create and print to console the session information 
        /// </summary>
        /// <param name="session"></param>
        private static void PrintSessionList(SampleServer sampleServer)
        {

            // list active sessions
            var sessions = sampleServer.CurrentInstance.SessionManager.GetSessions();
            var subscriptions = sampleServer.CurrentInstance.SubscriptionManager.GetSubscriptions();

            if (sessions.Count > 0)
            {
                Console.WriteLine("\nSessions list:");
                foreach (var session in sessions)
                {
                    StringBuilder line = new StringBuilder();

                    line.AppendFormat("{0,20}", session.SessionDiagnostics.SessionName);
                    if (session.SessionDiagnostics != null && session.SessionDiagnostics.ClientConnectionTime != null)
                    {
                        line.AppendFormat(";ConnectionTime: {0}", session.SessionDiagnostics.ClientConnectionTime);
                    }
                    if (session.Identity != null)
                    {
                        line.AppendFormat(";{0,20}", session.Identity.DisplayName);
                    }
                    line.AppendFormat(";Session ID:{0}", session.Id);
                    line.AppendFormat(";Subscriptions:{0}", session.SessionDiagnostics.CurrentSubscriptionsCount);
                    Console.WriteLine(line);
                }
            }
            else
            {
                Console.WriteLine("\nSessions list: empty");
            }
            
        }

        #endregion
    }
}
