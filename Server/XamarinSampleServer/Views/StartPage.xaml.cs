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
            BindingContext = m_viewModel = StartPageViewModel.Instance;           
        }

        
        private void ServerIpsList_Changed(object sender, EventArgs e)
        {
            StackLayout stackLayout = sender as StackLayout;
            if (stackLayout != null)
            {
                stackLayout.Children.Clear();
                foreach (var serverIp in m_viewModel.ServerIps)
                {
                    Label label = new Label()
                    {
                        Text = String.Format("opc.tcp://{0}:61510/SampleServer", serverIp),
                        Margin = 1
                    };
                    stackLayout.Children.Add(label);
                }
            }
        }

        private void StartServer_OnClicked(object sender, EventArgs e)
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

	    private void StopServer_OnClicked(object sender, EventArgs e)
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