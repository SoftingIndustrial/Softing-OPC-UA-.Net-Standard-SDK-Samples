using System;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinSampleServer.ViewModels;

namespace XamarinSampleServer.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class StartPage : ContentPage
	{
	    private StartPageViewModel m_viewModel;
        public StartPage()
		{
            Title = "Sample server";
			InitializeComponent ();
		    BindingContext = m_viewModel = new StartPageViewModel();
        }

	    private async void StartServer_OnClicked(object sender, EventArgs e)
        {            
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = true;
                });

                m_viewModel.StartServer().Wait();

                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = false;
                });
            });
        }

	    private async void StopServer_OnClicked(object sender, EventArgs e)
	    {	        
            ThreadPool.QueueUserWorkItem(o =>
	        {
	            Device.BeginInvokeOnMainThread(() =>
	            {
	                m_viewModel.IsBusy = true;
                });

                m_viewModel.StopServer();

	            Device.BeginInvokeOnMainThread(() =>
	            {
	                m_viewModel.IsBusy = false;
	            });
            });
           
	    }
    }
}