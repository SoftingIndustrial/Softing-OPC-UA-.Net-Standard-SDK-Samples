/* ========================================================================
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

            //create the UaApplication object from config file
            UaApplication application = UaApplication.Create(Constants.ConfigurationFile).Result;

            if (application.Configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                application.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            }

            //Create the process object that will execute user commands
            Process process = new Process(application);
            while (process.CurrentState != State.Exit)
            {
                // Read commands
                string inputCommand = Console.ReadKey().KeyChar.ToString();
                //execute command
                process.ExecuteCommand(inputCommand);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Event handler for CertificateValidator CertificateValidation event
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