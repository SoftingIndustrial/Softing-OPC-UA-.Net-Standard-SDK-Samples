using SampleClientXamarin.Views;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace SampleClientXamarin
{
	public partial class App : Application
	{
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
