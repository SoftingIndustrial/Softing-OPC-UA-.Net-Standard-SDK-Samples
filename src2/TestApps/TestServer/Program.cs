using Opc.Ua;
using Opc.Ua.Configuration;
using System;

namespace TestServer
{
	class Program
	{
		static int Main()
		{
		    StartServer();
			return 0;
		}

	    private static async void StartServer()
	    {
	        ApplicationInstance application = new ApplicationInstance();
	        application.ApplicationType = ApplicationType.Server;
	        application.ConfigSectionName = "TestServer";

            try
	        {
	            // load the application configuration.
	            await application.LoadApplicationConfiguration(true);

	            // check the application certificate.
	            await application.CheckApplicationInstanceCertificate(false, 0);

	            // start the server.
	            await application.Start(new TestServer());

	            int count = application.ApplicationConfiguration.ServerConfiguration.BaseAddresses.Count;
	            for (int i = 0; i < count; i++)
	            {
	                Console.WriteLine(application.ApplicationConfiguration.ServerConfiguration.BaseAddresses[i]);
	            }
	            Console.WriteLine("Server started");
	            Console.WriteLine("Press:\n\tx,q: shutdown the server\n\n");

	            do
	            {
	                ConsoleKeyInfo ki = Console.ReadKey();
	                if (ki.KeyChar == 'q' || ki.KeyChar == 'x')
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
	}
}