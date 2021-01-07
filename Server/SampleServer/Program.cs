/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 *  
 * ======================================================================*/

using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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

                Softing.Opc.Ua.PubSub.LicensingStatus pubSubLicensingStatus = Softing.Opc.Ua.PubSub.LicensingStatus.Ok;

                // TODO - PubSub binary license activation
                // Fill in your Server or Client binary license activation keys here
                // pubSubLicensingStatus = Softing.Opc.Ua.PubSub.License.ActivateLicense(Softing.Opc.Ua.PubSub.LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
                // pubSubLicensingStatus = Softing.Opc.Ua.PubSub.License.ActivateLicense(Softing.Opc.Ua.PubSub.LicenseFeature.Server, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");

                if (pubSubLicensingStatus != Softing.Opc.Ua.PubSub.LicensingStatus.Ok)
                {
                    Console.WriteLine("PubSub license status is: {0}!", pubSubLicensingStatus);
                    Console.ReadKey();
                    return;
                }

                // Start the server
                await sampleServer.Start(configurationFile);

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
