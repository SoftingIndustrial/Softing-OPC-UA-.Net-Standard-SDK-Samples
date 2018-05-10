﻿/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using Opc.Ua;
using System;

namespace SampleServerToolkit
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
            SampleServer sampleServerPort = new SampleServer();
            SampleServer sampleServer = new SampleServer();
            try
            {               
                UserTokenPolicyCollection userTokens = new UserTokenPolicyCollection()
                {
                    new UserTokenPolicy()
                    {
                        TokenType = UserTokenType.Anonymous,
                        SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None",
                    },
                    new UserTokenPolicy()
                    {
                        TokenType = UserTokenType.UserName,                        
                        SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256",
                    }
                };
               // await sampleServerPort.Start(61521);
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
