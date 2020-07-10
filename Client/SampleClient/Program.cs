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
using Opc.Ua;
using Softing.Opc.Ua.Client;
using SampleClient.StateMachine;
using System.IO;
using Opc.Ua.Client;
using System.Threading;

namespace SampleClient
{
    static class Program
    {
        public static string ServerUrl { get; private set; }

        /// <summary>
        /// Entry point for application
        /// </summary>
        static void Main()
        {
            // Create the UaApplication object from config file
            UaApplication application = UaApplication.Create("SampleClient.Config.xml").Result;

            


            // Get the Sample Client custom parameters
            SampleClientConfiguration sampleClientConfiguration = application.Configuration.ParseExtension<SampleClientConfiguration>();
            if (sampleClientConfiguration != null)
            {
                ServerUrl = sampleClientConfiguration.ServerUrl;
            }
            string reverseConnectUrl = "opc.tcp://localhost:65300";
            string serverApplicationUri = "urn:lboaw10:Softing:UANETStandardToolkit:SampleServer";
            for (int i = 0; i < 2; i++)
            {
                ClientSession clientSession = application.CreateReverseConnectSession(reverseConnectUrl, serverApplicationUri, 60000).Result;
                if (clientSession != null)
                {
                    Console.WriteLine("session created with reverse connect " + clientSession.SessionName);
                    clientSession.Connect(true, true);
                   // clientSession.Disconnect(true);
                   // clientSession.Dispose();
                }
                else
                {
                    Console.WriteLine("session NOT created with reverse connect ");
                }
            }




            //Uri ReverseConnectUri = new Uri("opc.tcp://localhost:65300");
            //ReverseConnectManager reverseConnectManager = null;
            //if (ReverseConnectUri != null)
            //{
            //    // start the reverse connection manager
            //    reverseConnectManager = new ReverseConnectManager();
            //    reverseConnectManager.AddEndpoint(ReverseConnectUri);
            //    reverseConnectManager.StartService(application.Configuration);
            //}
            //Console.Title = string.Format("SampleClient [ServerUrl: {0}]", ServerUrl);


            //Console.WriteLine("   Waiting for reverse connection.");
            //while (true)
            //{
            //    try
            //    {

            //        ITransportWaitingConnection connection = reverseConnectManager.WaitForConnection(
            //           ReverseConnectUri, "urn:wboaw10:Softing:UANETStandardToolkit:SampleServer", new CancellationTokenSource(60000).Token).Result;
            //        if (connection != null)
            //        {
            //            ClientSession clientSession = application.CreateSession(ServerUrl, connection);
            //            clientSession.Connect(true, true);
            //            clientSession.Disconnect(true);
            //            clientSession.Dispose();
            //        }

            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //}
            // Subscribe to certificate validation error event
            application.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            LicensingStatus clientLicensingStatus = LicensingStatus.Ok;

            // TODO - design time license activation
            // Fill in your design time license activation keys here
            //clientLicensingStatus = application.ActivateLicense(LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");

            if (clientLicensingStatus == LicensingStatus.Expired)
            {
                Console.WriteLine("Client license period expired!");
                Console.ReadKey();
                return;
            }
            if (clientLicensingStatus == LicensingStatus.Invalid)
            {
                Console.WriteLine("Invalid Client license key!");
                Console.ReadKey();
                return;
            }

            // Create the process object that will execute user commands
            Process process = new Process(application);
            while (process.CurrentState != SampleClient.StateMachine.State.Exit)
            {
                // Read commands
                string inputCommand = Console.ReadKey().KeyChar.ToString();
                // Execute command
                process.ExecuteCommand(inputCommand);
            }
        }

        /// <summary>
        /// Event handler received when a certificate validation error occurs.
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="e"></param>
        public static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
            e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted  
                || e.Error.StatusCode == StatusCodes.BadCertificateChainIncomplete);
        }

        /// <summary>
        /// Print out all information from exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public static void PrintException(string message, Exception ex)
        {
            Console.Write("-----------------------------------\nException - {0}:\n", message);
            while(ex != null)
            {
                if (ex is ServiceResultException)
                {
                    Console.Write("StatusCode = {0}, ", StatusCodes.GetBrowseName(((ServiceResultException)ex).StatusCode));
                }
                Console.WriteLine("{0}, ", ex.Message);
                ex = ex.InnerException;
            }
            Console.WriteLine("-----------------------------------");
        }
    }
}
