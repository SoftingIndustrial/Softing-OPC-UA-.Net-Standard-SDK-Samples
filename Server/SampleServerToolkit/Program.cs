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
            string configurationFile = "SampleServer.Config.xml";
            SampleServer sampleServer = new SampleServer();
            try
            {
                //await sampleServer.Start(configurationFile);                
                await sampleServer.Start(12345);
                for (int i = 0; i < sampleServer.ApplicationConfiguration.ServerConfiguration.BaseAddresses.Count; i++)
                {
                    Console.WriteLine(sampleServer.ApplicationConfiguration.ServerConfiguration.BaseAddresses[i]);
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
                sampleServer.Stop();
            }
        }

        private static void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            e.Accept = true;
        }
    }
}
