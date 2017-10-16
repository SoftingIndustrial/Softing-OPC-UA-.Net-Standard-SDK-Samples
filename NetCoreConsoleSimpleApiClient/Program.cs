using System;
using Opc.Ua;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Opc.Ua.Configuration;
using Opc.Ua.Toolkit;

namespace NetCoreConsoleSimpleApiClient
{
    class Program
    {
        private static string ApplicationName = ".Net Core Sample for Simple API";
        private static string DemoServerUrl = $"opc.tcp://[::1]:51510/UA/DemoServer";//$"opc.tcp://{Utils.GetHostName()}:51510/UA/DemoServer";
        static void Main(string[] args)
        {
            Console.WriteLine(DemoServerUrl);

            Task.Run(async () =>
            {

                UaApplication application = await UaApplication.CreateConfiguredApplication(configFileName: "NetCoreConsoleSimpleApiClient.config");

                if (application.Configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    application.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
                }

                //UaApplication application2 = await CreateApplication();

                DiscoveryService discoveryService = new DiscoveryService(application.Configuration);

                var endpoints = discoveryService.GetEndpoints(DemoServerUrl);

                var servers = discoveryService.DiscoverServers("opc.tcp://localhost:4840");


            }).GetAwaiter().GetResult();

            

            Console.ReadKey();
        }

        private static async Task<UaApplication> CreateApplication()
        {
            UaApplication application = await UaApplication.CreateConfiguredApplication();   

            application.Configuration.ApplicationName = ApplicationName;
            application.Configuration.ApplicationType = ApplicationType.Client;
            application.Configuration.ApplicationUri = $"urn:{Utils.GetHostName()}:OPCFoundation:NetCoreConsoleSimpleApiClient";
            application.Configuration.TransportConfigurations = new TransportConfigurationCollection();
            application.Configuration.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };
            application.Configuration.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };
            application.Configuration.ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 };

            application.Configuration.TraceConfiguration = new TraceConfiguration()
            {
                OutputFilePath = @"%LocalFolder%\Logs\Opc.Ua.SampleClient.log",
                TraceMasks = 519
            };
            application.Configuration.TraceConfiguration.ApplySettings();

            application.Configuration.SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.X509Store,
                    StorePath = "CurrentUser\\UA_MachineDefault",
                    SubjectName = ApplicationName
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "OPC Foundation/CertificateStores/UA Applications",
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "OPC Foundation/CertificateStores/UA Certificate Authorities",
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "OPC Foundation/CertificateStores/RejectedCertificates",
                },
                AutoAcceptUntrustedCertificates = true
            };
            await application.Configuration.Validate(ApplicationType.Client);

            await application.ApplicationInstance.CheckApplicationInstanceCertificate(false, 0);
               

            if (application.Configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                application.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            }

            return application;
        }


        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
            e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
        }

    }
}