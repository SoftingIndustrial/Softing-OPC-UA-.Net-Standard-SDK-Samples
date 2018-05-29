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
                bool result = true;
                // TODO - design time license activation
                // Fill in your design time license activation keys here
                //result = License.ActivateLicense(LicenseFeature.Server, "XXXX-XXXX-XXXX-XXXX-XXXX");

                if (!result)
                {
                    return;
                }

                // Start the server
                await sampleServer.Start(configurationFile);
                for (int i = 0; i < sampleServer.Configuration.ServerConfiguration.BaseAddresses.Count; i++)
                {
                    Console.WriteLine(sampleServer.Configuration.ServerConfiguration.BaseAddresses[i]);
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
                sampleServer.Stop();
            }
        }
    }
}