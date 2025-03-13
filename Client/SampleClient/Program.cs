/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using System.Xml;

using Opc.Ua;

using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Configuration;
using SampleClient.StateMachine;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using static Opc.Ua.Utils;


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
            // Load client default (customized) configuration build with a fluent API
            // ApplicationConfigurationBuilderEx defaultConfiguration = LoadDefaultConfiguration().Result;

            // Load client default (customized) configuration build with a fluent API using a certificate password provider
            // ApplicationConfigurationBuilderEx defaultConfiguration = LoadDefaultConfiguration(new CertificatePasswordProvider("Client_Pwd")).Result;

            // Create the UaApplication object from application configuration build with a fluent API
            // UaApplication application = UaApplication.Create(defaultConfiguration).Result;

            // Create the UaApplication object from config file and certificate password provider 
            // UaApplication application = UaApplication.Create("SampleClient.Config.xml", new CertificatePasswordProvider("Client_Pwd")).Result;

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

            SerilogConfiguration serilogConfiguration = application.Configuration.ParseExtension<SerilogConfiguration>();
            if (serilogConfiguration != null)
            {
                if (serilogConfiguration.Enable)
                {
                    // setup the Serilog logging
                    SetLogger(application.Configuration, application.Configuration.ApplicationName, false, LogLevel.Error);
                }
            }

            LicensingStatus clientLicensingStatus = LicensingStatus.Ok;

            // TODO - Client binary license activation
            // Fill in your Client binary license activation keys here
            //clientLicensingStatus = application.ActivateLicense(LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
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
                process.ExecuteCommand(inputCommand).GetAwaiter().GetResult();
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
            // If this flag is set on true the CertificateValidation events for e.Error.InnerResult will be suppressed.
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
            while (ex != null)
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

        #region Customized configuration

        /// <summary>
        /// Load default configuration
        /// </summary>
        /// <returns></returns>
        private static async Task<ApplicationConfigurationBuilderEx> LoadDefaultConfiguration(ICertificatePasswordProvider certificatePasswordProvider = null)
        {

            ApplicationConfigurationBuilderEx applicationConfigurationBuilder =
                   new ApplicationConfigurationBuilderEx(ApplicationType.Client);

            await applicationConfigurationBuilder
                .Initialize("urn:localhost:Softing:UANETStandardToolkit:SampleClient",
                        "http://industrial.softing.com/OpcUaNetStandardToolkit/SampleClient")
                .SetApplicationName("Softing .NET Standard Sample Client")
                .DisableHiResClock(true)
                .SetTransportQuotas(new Opc.Ua.TransportQuotas()
                {
                    OperationTimeout = 120000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 4194304,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                })
                .AsClient()
                    .SetDefaultSessionTimeout(610000)
                    .SetMinSubscriptionLifetime(11000)
                    .AddWellKnownDiscoveryUrls("opc.tcp://{0}:4840/UADiscovery")
                .AddSecurityConfigurationExt(
                    "SoftingOpcUaSampleClient",
                    "%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/pki",
                    "%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/pki",
                    "%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/pki")
                    .SetRejectSHA1SignedCertificates(false)
                    .SetUserRoleDirectory("%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/userRoles")
                    .SetAddAppCertToTrustedStore(true)
                    .AddCertificatePasswordProvider(certificatePasswordProvider)
                .AddExtension<SampleClientConfiguration>(new XmlQualifiedName("SampleClientConfiguration"),
                    new SampleClientConfiguration()
                    {
                        ServerUrl = "opc.tcp://localhost:61510/SampleServer",
                        ServerUrlHttps = "https://localhost:61511/SampleServer",
                        ReverseConnectUrl = "opc.tcp://localhost:61512",
                        ReverseConnectServerApplicationUri = "urn:localhost:Softing:UANETStandardToolkit:SampleServer",
                        ReverseConnectServerCertificateIdentifier = new CertificateIdentifier()
                        {
                            StoreType = "Directory",
                            StorePath = "%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/pki/own",
                            SubjectName = "SoftingOpcUaSampleServer"
                        }
                    })
                .AddExtension<ClientToolkitConfiguration>(new XmlQualifiedName("ClientToolkitConfiguration"),
                    new ClientToolkitConfiguration()
                    {
                        DiscoveryOperationTimeout = 10000,
                        DecodeCustomDataTypes = true,
                        DecodeDataTypeDictionaries = true,
                        ReadNodesWithTypeNotInHierarchy = false,
                        CheckPortOnConnect = false
                    })
                 .AddExtension<SerilogConfiguration>(new XmlQualifiedName("SerilogConfiguration"),
                 new SerilogConfiguration()
                 {
                     Enable = false,
                     FilePath = "%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/logs/SampleClient.log",
                     RollingFile = true,
                     RollingTypeOption = SerilogConfiguration.RollingOptions.Size,
                     RollingFileSizeLimit = 10485760,
                     RollingFilesCountLimit = 10,
                     RollingInterval = SerilogConfiguration.RollInterval.Day,
                     MinimumLevel = LogLevel.Error
                 })
                .SetTraceMasks(1)
                .SetOutputFilePath("%CommonApplicationData%/Softing/OpcUaNetStandardToolkit/logs/SampleClient.log")
                .SetDeleteOnLoad(true)
                .Create().ConfigureAwait(false);

            return applicationConfigurationBuilder;

        }

        /// <summary>
        /// Creates, configures and sets the logger
        /// </summary>
        /// <param name="configuration">the application configuration</param>
        /// <param name="context">the context</param>
        /// <param name="logConsole">log to console or not</param>
        /// <param name="consoleLogLevel">default loglevel</param>
        private static void SetLogger(ApplicationConfiguration configuration,
            string context,
            bool logConsole,
            LogLevel consoleLogLevel)
        {
            if (!Enum.IsDefined(typeof(LogLevel), consoleLogLevel.ToString()))
            {
                Trace("Invalid 'LogLevel' parameter of the SetLogger method.");
                return;
            }

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                            //This statement is used to dynamically add and remove properties from the ambient execution context.
                            //Additions are done down below
                            .Enrich.FromLogContext();
            bool enable = false;
            SerilogConfiguration serilogConfiguration = configuration.ParseExtension<SerilogConfiguration>();
            if (serilogConfiguration != null)
            {
                enable = serilogConfiguration.Enable;
                if (enable)
                {
                    //if the loaded MinimumLevel is not a defined LogLevel the default one will be used from the method parameter
                    if (!Enum.IsDefined(typeof(LogEventLevel), (int)serilogConfiguration.MinimumLevel))
                        Trace("Invalid SerilogConfiguration {0} value: {1} set in configuration file.", nameof(serilogConfiguration.MinimumLevel), serilogConfiguration.MinimumLevel);
                    else
                        consoleLogLevel = serilogConfiguration.MinimumLevel;

                    loggerConfiguration.MinimumLevel.Is((LogEventLevel)consoleLogLevel);
                    if (string.IsNullOrEmpty(serilogConfiguration.FilePath))
                    {
                        serilogConfiguration.FilePath = configuration.TraceConfiguration.OutputFilePath;
                    }

                    if (serilogConfiguration.RollingFile == true)
                    {
                        switch (serilogConfiguration.RollingTypeOption)
                        {

                            case SerilogConfiguration.RollingOptions.Size:

                                loggerConfiguration.WriteTo.File(
                                    ReplaceSpecialFolderNames(serilogConfiguration.FilePath),
                                    rollOnFileSizeLimit: serilogConfiguration.RollingFile,
                                    fileSizeLimitBytes: serilogConfiguration.RollingFileSizeLimit,
                                    retainedFileCountLimit: serilogConfiguration.RollingFilesCountLimit);
                                break;

                            case SerilogConfiguration.RollingOptions.Time:
                                loggerConfiguration.WriteTo.File(
                                    ReplaceSpecialFolderNames(serilogConfiguration.FilePath),
                                    rollOnFileSizeLimit: serilogConfiguration.RollingFile,
                                    rollingInterval: (RollingInterval)serilogConfiguration.RollingInterval);
                                break;

                            case SerilogConfiguration.RollingOptions.TimeAndSize:
                                loggerConfiguration.WriteTo.File(
                                    ReplaceSpecialFolderNames(serilogConfiguration.FilePath),
                                    rollingInterval: (RollingInterval)serilogConfiguration.RollingInterval,
                                    rollOnFileSizeLimit: serilogConfiguration.RollingFile,
                                    fileSizeLimitBytes: serilogConfiguration.RollingFileSizeLimit,
                                    retainedFileCountLimit: serilogConfiguration.RollingFilesCountLimit);
                                break;
                        }
                    }
                    else
                    {
                        loggerConfiguration.WriteTo.File(
                            ReplaceSpecialFolderNames(serilogConfiguration.FilePath));
                    }

                    if (logConsole)
                    {
                        loggerConfiguration.WriteTo.Console(
                            restrictedToMinimumLevel: (LogEventLevel)consoleLogLevel
                            );
                    }

#if DEBUG
                    else
                    {
                        loggerConfiguration
                            .WriteTo.Debug(restrictedToMinimumLevel: (LogEventLevel)consoleLogLevel);
                    }
#endif

                    // create the serilog logger
                    Serilog.Core.Logger serilogger = loggerConfiguration
                        .CreateLogger();

                    // create the ILogger for Opc.Ua.Core
                    Microsoft.Extensions.Logging.ILogger logger = LoggerFactory.Create(builder => builder.SetMinimumLevel(consoleLogLevel))
                        .AddSerilog(serilogger)
                        .CreateLogger(context);

                    // set logger interface, disables TraceEvent
                    Utils.SetLogger(logger);
                }
                else
                {
                    return;
                }
            }
        }

        #endregion
    }
}
