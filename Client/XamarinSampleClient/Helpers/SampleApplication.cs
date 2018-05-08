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
using System.IO;
using System.Threading.Tasks;
using Opc.Ua;
using Softing.Opc.Ua.Client;

namespace XamarinSampleClient.Helpers
{
    /// <summary>
    /// Application for the Sample project. 
    /// 
    /// Creates and maintains a reference to UaApplication object used to execute samples 
    /// </summary>
    public class SampleApplication
    {
        private static bool m_isBusy;
        public static UaApplication UaApplication { get; private set; }

        /// <summary>
        /// Initializes UaApplication object for this runtime
        /// </summary>
        /// <returns></returns>
        public static async Task InitializeUaApplication()
        {
            if (!m_isBusy & UaApplication == null)
            {
                m_isBusy = true;
                await Task.Run(() =>
                {
                        //Create the UaApplication object from config file
                        UaApplication = UaApplication.Create(CreateAplicationConfiguration()).Result;
                });

                //Subscribe to certificate validation error event
                UaApplication.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

                m_isBusy = false;
            }
        }


        /// <summary>
        /// Creates Application's ApplicationConfiguration programmatically
        /// </summary>
        /// <returns></returns>
        private static ApplicationConfigurationEx CreateAplicationConfiguration()
        {            
            ApplicationConfigurationEx configuration = new ApplicationConfigurationEx();

            configuration.ApplicationName = "UA Xamarin Sample Client";
            configuration.ApplicationType = ApplicationType.Client;
            configuration.ApplicationUri = $"urn:{Utils.GetHostName()}:UANETStandardToolkit:XamarinSampleClient";
            configuration.ProductUri = "http://industrial.softing.com/OpcUaNetStandardToolkit/XamarinSampleClient";
            configuration.TransportConfigurations = new TransportConfigurationCollection();
            configuration.TransportQuotas = new TransportQuotas { OperationTimeout = 15000, MaxByteStringLength = 4194304 , MaxMessageSize = 4194304 };
            configuration.ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 15000 };
            configuration.DefaultSessionTimeout = 15000;
            configuration.DiscoveryOperationTimeout = 6500;
            //enable read/write complex types
            configuration.DecodeCustomDataTypes = true;
                       
            configuration.TraceConfiguration = new TraceConfiguration()
            {
                OutputFilePath = @"/storage/emulated/0/Softing/logs/XamarinSampleClient.log",
                TraceMasks = 519
            };

            configuration.SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"/storage/emulated/0/Softing/pki/own",
                    SubjectName = "SoftingOpcUaXamarinSampleClient"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"/storage/emulated/0/Softing/pki/trusted",
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"/storage/emulated/0/Softing/pki/issuer",
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = @"/storage/emulated/0/Softing/pki/rejected",
                },
                AutoAcceptUntrustedCertificates = true
            };

            return configuration;
        }

        /// <summary>
        /// Event handler received when a certificate validation error occurs.
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="e"></param>
        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
        }
    }
}
