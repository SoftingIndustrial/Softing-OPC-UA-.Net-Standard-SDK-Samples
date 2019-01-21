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
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.Private;
using SampleClient.StateMachine;
using System.IO;

namespace SampleClient
{
    static class Program
    {
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

            Console.Title = string.Format("SampleClient [ServerUrl: {0}]", ServerUrl);

            // Subscribe to certificate validation error event
            application.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            LicenseClientStatus result = LicenseClientStatus.Ok;

            // TODO - design time license activation
            // Fill in your design time license activation keys here
            //result = application.ActivateLicense(LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
            
            if (result == LicenseClientStatus.Expired)
            {
                Console.WriteLine("License period expired!");
                Console.ReadKey();
                return;
            }
            if (result == LicenseClientStatus.Invalid)
            {
                Console.WriteLine("Invalid License key!");
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
        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
            e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
        }

        public static string ServerUrl { get; private set; }
    }
}