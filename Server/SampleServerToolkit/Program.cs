using System;
using Opc.Ua;
using Opc.Ua.Configuration;
using Softing.Opc.Ua.Server;

namespace SampleServerToolkit
{
    class Program
    {
        static void Main(string[] args)
        {
            StartServer();
        }


        private static async void StartServer()
        {
            ApplicationInstance application = new ApplicationInstance();
            application.ApplicationType = ApplicationType.Server;
            string configurationFile = "SampleServer.Config.xml";

            try
            {
                // Load the application configuration
                await application.LoadApplicationConfiguration(configurationFile, false);

                // Check the application certificate
                await application.CheckApplicationInstanceCertificate(false, 0);
                application.ApplicationConfiguration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;
                // Start the server
                await application.Start(new SampleServer());

                for (int i = 0; i < application.ApplicationConfiguration.ServerConfiguration.BaseAddresses.Count; i++)
                {
                    Console.WriteLine(application.ApplicationConfiguration.ServerConfiguration.BaseAddresses[i]);
                }
                Console.WriteLine("Server started");
                Console.WriteLine("Press:\n\tx,q: shutdown the server\n\n");

                do
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.KeyChar == 'q' || key.KeyChar == 'x')
                    {
                        break;
                    }
                }
                while (true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadKey();
                Environment.Exit(-1);
            }
            finally
            {
                application.Stop();
            }
        }

        private static void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            e.Accept = true;
        }
    }
}
