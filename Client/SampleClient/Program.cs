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
        public static string ServerUrlHttps { get; private set; }

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
                ServerUrlHttps = sampleClientConfiguration.ServerUrlHttps;
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
            // display certificate info
            Console.WriteLine("Certificate could not be validated!");
            Console.WriteLine("Subject: {0}", e.Certificate.Subject);
            Console.WriteLine("Issuer: {0}", ((e.Certificate.Subject == e.Certificate.Issuer) ? "Self-signed" : e.Certificate.Issuer));
            Console.WriteLine("Valid From: {0}", e.Certificate.NotBefore);
            Console.WriteLine("Valid To: {0}", e.Certificate.NotAfter);
            Console.WriteLine("Thumbprint: {0}", e.Certificate.Thumbprint);
            Console.WriteLine("Validation error(s): ");

            // intialize the value for e.AcceptAll with true. This means that all status codes are accepted.
            bool isCertificateAccepted = true;

            // Implement the logic that decides if the certificate can be accepted or not and set isCertificateAccepted flag accordingly.
            // The certificate can be retrieved from the e.Certificate field. 
            ServiceResult error = e.Error;
            while (error != null)
            {
                Console.WriteLine(error);

                // decide if error is acceptable and certificate can be trusted

                // move to InnerResult
                error = error.InnerResult;
            }

            if (isCertificateAccepted)
            {
                Console.WriteLine("*** The Certificate was accepted! ***");
            }
            else
            {
                Console.WriteLine("*** The Certificate was NOT accepted! ***");
            }

            // Set the AcceptAll flag to signal the CertificateValidator if the Certificate shall be accepted.
            // If this flag is set on true the CertificateValidation events for e.Error.InnerResult will be supressed.
            e.AcceptAll = isCertificateAccepted;
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
