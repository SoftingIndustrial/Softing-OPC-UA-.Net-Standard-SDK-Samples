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
using Opc.Ua;
using Opc.Ua.Configuration;

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
            ApplicationInstance application = new ApplicationInstance();
            application.ApplicationType = ApplicationType.Server;
            application.ConfigSectionName = "SampleServer";

            try
            {
                // Load the application configuration
                await application.LoadApplicationConfiguration(false);

                // Check the application certificate
                await application.CheckApplicationInstanceCertificate(false, 0);

                // Start the server
                await application.Start(new SampleServer());

                for (int i = 0; i < application.ApplicationConfiguration.ServerConfiguration.BaseAddresses.Count; i++)
                {
                    Console.WriteLine(application.ApplicationConfiguration.ServerConfiguration.BaseAddresses[i]);
                }
                Console.WriteLine("Server started");
                Console.WriteLine("Press:\n\tx,q: shutdown the server\n\n");

                do
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.KeyChar == 'q' || key.KeyChar == 'x')
                    {
                        break;
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
                application.Stop();
            }
        }
    }
}