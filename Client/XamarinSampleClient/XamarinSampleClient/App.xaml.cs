using XamarinSampleClient.Views;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace XamarinSampleClient
{
	public partial class App : Application
	{
        public static string DefaultSampleServerUrl = "opc.tcp://192.168.43.37:61510/SampleServer";
        public App()
		{
			InitializeComponent();

			SetMainPage();
		}

		public static void SetMainPage()
		{
		    Current.MainPage = new NavigationPage(new StartPage())
		    {
		        Title = "OPC UA Sample Client - Xamarin"
		    };
        }
	}
}
