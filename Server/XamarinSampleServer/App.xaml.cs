using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinSampleServer.Views;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace XamarinSampleServer
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
            Current.MainPage = new NavigationPage(new StartPage());
        }
	}
}
