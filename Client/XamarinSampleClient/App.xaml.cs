using XamarinSampleClient.Views;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace XamarinSampleClient
{
	public partial class App : Application
	{
        public static string DefaultSampleServerUrl = "opc.tcp://localhost:61510/SampleServer";
        private static StartPage m_startPage;
        public App()
		{
			InitializeComponent();

			SetMainPage();
		}

		public static void SetMainPage()
		{
            if (m_startPage == null)
            {
                m_startPage = new StartPage();		   
            }
		    Current.MainPage = new NavigationPage(m_startPage)
		    {
		        Title = "OPC UA Sample Client - Xamarin"
		    };
        }
	}
}
