/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System;
using System.Text;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server.Private;

namespace SampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            StartServer();
        }

        private static async void StartServer()
        {
            string configurationFile = "SampleServer.Config.xml";
            SampleServer sampleServer = new SampleServer();

            try
            {
                LicensingStatus result = LicensingStatus.Ok;
                // TODO - design time license activation
                // Fill in your design time license activation keys here
                //result = License.ActivateLicense(LicenseFeature.Server, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
                
                if (result == LicensingStatus.Expired)
                {
                    Console.WriteLine("License period expired!");
                    Console.ReadKey();
                    return;
                }
                if (result == LicensingStatus.Invalid)
                {
                    Console.WriteLine("Invalid License key!");
                    Console.ReadKey();
                    return;
                }

                // Start the server
                await sampleServer.Start(configurationFile);
                for (int i = 0; i < sampleServer.Configuration.ServerConfiguration.BaseAddresses.Count; i++)
                {
                    Console.WriteLine(sampleServer.Configuration.ServerConfiguration.BaseAddresses[i]);
                }
                Console.WriteLine("Server started");

                PrintCommandParameters();

                // print notification on session events
                sampleServer.CurrentInstance.SessionManager.SessionActivated += SessionStateChanged;
                sampleServer.CurrentInstance.SessionManager.SessionClosing += SessionStateChanged;
                sampleServer.CurrentInstance.SessionManager.SessionCreated += SessionStateChanged;

                do
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.KeyChar == 'q' || key.KeyChar == 'x')
                    {
                        Console.WriteLine("\nShutting down...");
                        break;
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
                while (true);
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
        private static void PrintCommandParameters()
        {
            Console.WriteLine("Press:\n\ts: session list");
            Console.WriteLine("\tx,q: shutdown the server\n\n");
        }
        private static void SessionStateChanged(Session session, SessionEventReason reason)
        {
            PrintSessionStatus(session, reason.ToString());
        }
        private static void PrintSessionStatus(Session session, string reason = null)
        {
           lock (session.DiagnosticsLock)
            {
                StringBuilder line = new StringBuilder();
                if (reason != null)
                {
                    line.AppendFormat("Session {0,9};", reason);
                }
                line.AppendFormat("{0,20}", session.SessionDiagnostics.SessionName);

                if (session.Identity != null)
                {
                    line.AppendFormat(";{0,20}", session.Identity.DisplayName);
                }
                line.AppendFormat(";Session ID:{0}", session.Id);
                if (reason == null)
                {
                    line.AppendFormat(";Subscriptions:{0}", session.SessionDiagnostics.CurrentSubscriptionsCount);
                }
                Console.WriteLine(line);
            }
        }
        #endregion
    }
}