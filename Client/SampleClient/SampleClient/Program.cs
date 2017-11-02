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

namespace SampleClient
{
    class Program
    {
        private const string ConfigFileName = "SampleClient.config";

        static void Main(string[] args)
        {
            Console.Title = string.Format("SampleClient [uses Server: {0}]", Constants.SampleServerUrlOpcTcp);

            //create the UaApplication object from config file
            UaApplication application = UaApplication.CreateConfiguredApplication(configFileName: ConfigFileName).Result;
            if (application.Configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                application.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            }

            //Create the process object that will execute user commands
            Process process = new Process(application);
            while (process.CurrentState != State.Terminated)
            {
                // Read commands
                string inputCommand = Console.ReadKey().KeyChar.ToString();
                //execute command
                process.ExecuteCommand(inputCommand);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

       

        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
            e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
        }
    }
}