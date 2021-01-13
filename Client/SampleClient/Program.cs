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
using Opc.Ua;
using Softing.Opc.Ua.Client;
using SampleClient.StateMachine;

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
                // use ServerUrl from client configuration Extension
                ServerUrl = sampleClientConfiguration.ServerUrl;
            }
            
            // Subscribe to certificate validation error event
            application.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            LicensingStatus clientLicensingStatus = LicensingStatus.Ok;

            // TODO - Client binary license activation
            // Fill in your Client binary license activation keys here
            // clientLicensingStatus = application.ActivateLicense(LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
            if (clientLicensingStatus != LicensingStatus.Ok)
            {
                Console.WriteLine("Client license status is: {0}!", clientLicensingStatus);
                Console.ReadKey();
                return;
            }

            Console.WriteLine("SampleClient started at:{0}", DateTime.Now.ToLongTimeString());

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
                Console.WriteLine(ex.Message);
                ex = ex.InnerException;
            }
            Console.WriteLine("-----------------------------------");
        }
    }
}
