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

                // TODO - design time license activation
                // Fill in your design time license activation keys here
                //serverLicensingStatus = Softing.Opc.Ua.Server.License.ActivateLicense(Softing.Opc.Ua.Server.LicenseFeature.Server, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
                
                if (serverLicensingStatus == Softing.Opc.Ua.Server.LicensingStatus.Expired)
                {
                    Console.WriteLine("Server license period expired!");
                    Console.ReadKey();
                    return;
                }
                if (serverLicensingStatus == Softing.Opc.Ua.Server.LicensingStatus.Invalid)
                {
                    Console.WriteLine("Invalid Server license key!");
                    Console.ReadKey();
                    return;
                }

                Softing.Opc.Ua.PubSub.LicensingStatus pubsubLicensingStatus = Softing.Opc.Ua.PubSub.LicensingStatus.Ok;

                // TODO - design time license activation
                // Fill in your design time license activation keys here
                //pubsubLicensingStatus = Softing.Opc.Ua.PubSub.License.ActivateLicense(Softing.Opc.Ua.PubSub.LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
                //pubsubLicensingStatus = Softing.Opc.Ua.PubSub.License.ActivateLicense(Softing.Opc.Ua.PubSub.LicenseFeature.Server, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");

                if (pubsubLicensingStatus == Softing.Opc.Ua.PubSub.LicensingStatus.Expired)
                {
                    Console.WriteLine("PubSub license period expired!");
                    Console.ReadKey();
                    return;
                }
                if (pubsubLicensingStatus == Softing.Opc.Ua.PubSub.LicensingStatus.Invalid)
                {
                    Console.WriteLine("Invalid PubSub license key!");
                    Console.ReadKey();
                    return;
                }

                // Start the server
                await sampleServer.Start(configurationFile);

                var reverseConnections = sampleServer.GetReverseConnections();
                if (reverseConnections?.Count > 0)
                {
                    // print configured reverse connections
                    Console.WriteLine("Configured Reverse Connections:");
                    foreach (var connection in reverseConnections)
                    {
                        Console.WriteLine(connection.Key);
                    }
                }

                Console.WriteLine("\nServer Addresses:");

                for (int i = 0; i < sampleServer.Configuration.ServerConfiguration.BaseAddresses.Count; i++)
                {
                    Console.WriteLine(sampleServer.Configuration.ServerConfiguration.BaseAddresses[i]);
                }
                Console.WriteLine("Server started");

                PrintCommandParameters();
                
                bool exit = false;
                while (!exit)
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.KeyChar == 'c' && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        string endpoint = sampleServer.Configuration.ServerConfiguration.BaseAddresses[0];
                        Console.WriteLine(String.Format("\nCopied {0} to clipboard", endpoint));
                        ConsoleUtils.WindowsConsoleUtils.WindowsClipboard.SetTextValue(endpoint);
                    }
                    if (key.KeyChar == 'q' || key.KeyChar == 'x')
                    {
                        Console.WriteLine("\nShutting down...");
                        exit = true;
                    }
                    else if (key.KeyChar == 's')
                    {
                        // list active sessions
                        var sessions = sampleServer.CurrentInstance.SessionManager.GetSessions();
                        var subscriptions = sampleServer.CurrentInstance.SubscriptionManager.GetSubscriptions();

                        if (sessions.Count > 0)
                        {
                            Console.WriteLine("\nSessions list:");
                            foreach (var session in sessions)
                            {
                                PrintSessionStatus(session);
                            }
                        }
                        else
                        {
                            Console.WriteLine("\nSessions list: empty");
                        }
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

        #region Print State
        /// <summary>
        /// Print command line parameters
        /// </summary>
        private static void PrintCommandParameters()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("Press:\n\tc: copy endpoint to clipboard");
            }

            Console.WriteLine("\ts: session list");
            Console.WriteLine("\tx,q: shutdown the server\n\n");
        }

        /// <summary>
        /// Create and print to console the session information 
        /// </summary>
        /// <param name="session"></param>
        private static void PrintSessionStatus(Session session)
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
        #endregion
    }
}
