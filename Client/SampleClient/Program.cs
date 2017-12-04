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
using SampleClient.StateMachine;
using Softing.Opc.Ua;
using System;
using Softing.Opc.Ua.Private;

namespace SampleClient
{
    class Program
    {
       /// <summary>
       /// Entry point for application
       /// </summary>
       /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.Title = string.Format("SampleClient [ServerUrl: {0}]", Constants.ServerUrl);

            // Create the UaApplication object from config file
            UaApplication application = UaApplication.Create(Constants.ConfigurationFile).Result;

            // Subscribe to certificate validation error event
            application.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            bool result = true;
            // TODO - design time license activation
            // Fill in your design time license activation keys here
            // result = application.ActivateLicense(LicenseFeature.Client, "XXXX-XXXX-XXXX-XXXX-XXXX");
            if (!result)
            {
                return;
            }

            // Create the process object that will execute user commands
            Process process = new Process(application);
            while (process.CurrentState != State.Exit)
            {
                // Read commands
                string inputCommand = Console.ReadKey().KeyChar.ToString();
                // Execute command
                process.ExecuteCommand(inputCommand);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
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
    }
}