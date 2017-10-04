/* ========================================================================
 * Copyright © 2011-2017 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Toolkit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Softing.Opc.Ua.Toolkit.Client.Samples.BrowseClient
{
    class Program
    {
        #region Commands
        private const string m_exitCommand = "x";
        private const string m_createCommand = "c";
        private const string m_browseCommand = "b";
        private const string m_browseOptionsCommand = "o";
        private const string m_translateBrowsePathCommand = "t";
        private const string m_translateMultipleBrowsePathCommand = "m";
        private const string m_disconnectCommand = "d";
        #endregion

        static void Main(string[] arguments)
        {
            if (!LoadApplicationConfiguration().Result)
            {
                return;
            }

            BrowseClient browseClient = new BrowseClient();

            bool result = true;

            //	TODO - design time license activation
            //	Fill in your design time license activation keys here
            //
            //	NOTE: you can activate one or more features at the same time
            //	activate the Client feature            
            //result = Application.ActivateLicense(LicenseFeature.Client, "xxxx-xxxx-xxxx-xxxx-xxxx");

            if (result == false)
            {
                return;
            }

            Console.WriteLine("Warning: Security features are disabled.\r\nAll server application certificates are accepted.\r\n");

            string commandList = "List of commands: \r\n" +
                                "c - Create and Connect the session \r\n" +
                                "d - Disconnect session \r\n" +
                                "b - Browse server \r\n" +
                                "o - Browse server with options \r\n" +
                                "t - TranslateBrowsePathToNodeIds \r\n" +
                                "m - Translate multiple Browse Paths \r\n" +
                                "x - Exit \r\n";

            Console.WriteLine(commandList);
            Console.WriteLine("Enter Commands:\n");

            bool finished = false;
            try
            {
                do
                {
                    // Read commands
                    string inputCommand = Console.ReadLine();
                
                    switch (inputCommand)
                    {
                        case m_exitCommand:
                            finished = true;
                            browseClient.DisconnectSession();
                            break;
                        case m_createCommand:
                            browseClient.CreateSession();
                            break;
                        case m_disconnectCommand:
                            browseClient.DisconnectSession();
                            break;
                        case m_browseCommand:
                            browseClient.BrowseTheServer();
                            break;
                        case m_browseOptionsCommand:
                            Console.WriteLine("\nBrowse with options.\nMaxReferencesReturned is set to 3 before browsing the Server node. \nAfter 3 references returned a continuation point event cancels further browse.\n");
                            browseClient.BrowseWithOptions();
                            break;
                        case m_translateBrowsePathCommand:
                            browseClient.TranslateBrowsePathToNodeIds();
                            break;
                        case m_translateMultipleBrowsePathCommand:
                            browseClient.TranslateBrowsePathsToNodeIds();
                            break;
                        default:
                            Console.WriteLine("Invalid Command!");
                            Console.WriteLine(commandList);
                            break;
                    }
                }
                while (!finished);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Sets all the configuration parameters.
        /// </summary>
        private async static Task<bool> LoadApplicationConfiguration()
        {
            Application application = new Application();

            Application.Configuration.ApplicationName = "SoftingOpcUaBrowseClient";
            Application.Configuration.ProductUri = "http://industrial.softing.com/OpcUaNetToolkit/BrowseClient";

            // security configuration.
            string applicationFolder = Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\..");
            applicationFolder = Path.GetFullPath(applicationFolder);  

            Application.Configuration.SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(applicationFolder, @"pki\own"),
                    SubjectName = Application.Configuration.ApplicationName
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(applicationFolder, @"pki\trusted")
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(applicationFolder, @"pki\issuer")
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(applicationFolder, @"pki\rejected")
                },
                AutoAcceptUntrustedCertificates = true
            };

            Application.CertificateValidation += Application_CertificateValidation;

            // The Validate() method ensures that the specified configuration is valid.
            // It creates the specified folder paths for ApplicationCertificateStore and TrustedCertificateStore if not present.
            // It also checks for a valid application instance certificate and creates a new application instance certificate if not present.
            try
            {
                await Application.Configuration.Validate(ApplicationType.Client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            // trace configuration
            //Application.Configuration.Trace.LogFileName = @"Logs\BrowseClient.txt";
            //Application.Configuration.Trace.LogFileMaxSize = 10;
            //Application.Configuration.Trace.LogFileMaxRollBackups = 5;
            //Application.Configuration.Trace.LogFileTracelevel = TraceLevels.Warning;
            ////enable all masks
            //Application.Configuration.Trace.LogFileTraceMask = 0x00FF00FF;
            //Application.Configuration.Trace.Tracelevel = TraceLevels.Warning;
            ////enable all masks
            //Application.Configuration.Trace.TraceMask = 0x00FF00FF;

            return true;
        }

        /// <summary>
        /// Handles the certificate validation event.
        /// This event is triggered when the certificate received from server during connect is not trusted.
        /// </summary>
        private static void Application_CertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            // A custom logic for validating the server certificate can be implemented here.

            // Accept this certificate during the runtime of the application.
            e.Accept = true;

            Console.WriteLine("Untrusted certificate accepted with \nSubjectName = {0} \nThumbprint = {1}\n", e.Certificate.SubjectName.Name, e.Certificate.Thumbprint);
        }
    }
}