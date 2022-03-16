using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XamarinSampleClient.ViewModels;
using Softing.Opc.Ua.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinSampleClient.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class DiscoverySamplePage : ContentPage
	{
	    private DiscoveryViewModel m_viewModel;
        public DiscoverySamplePage ()
		{
			InitializeComponent ();
		    BindingContext = m_viewModel = new DiscoveryViewModel();
        }

	    private async void DiscoverEndpoints_OnClicked(object sender, EventArgs e)
	    {
	        if (string.IsNullOrEmpty(m_viewModel.ServerUrlEndpoints))
	        {
	            await DisplayAlert("Warning", "Please enter a value for 'Server URL for endpoints discovery'", null, "OK");
	            return;
            }
	        ThreadPool.QueueUserWorkItem(o =>
	        {
	            Device.BeginInvokeOnMainThread(() =>
	            {
	                m_viewModel.IsBusy = true;
	            });

	            m_viewModel.DiscoverEndpoints();

	            Device.BeginInvokeOnMainThread(() =>
	            {
	                m_viewModel.IsBusy = false;
	            });
	        });
        }

	    private async void DiscoverServers_OnClicked(object sender, EventArgs e)
	    {
	        if (string.IsNullOrEmpty(m_viewModel.ServerUrlNetwork))
	        {
	            await DisplayAlert("Warning", "Please enter a value for 'Server URL for network discovery'", null, "OK");
	            return;
	        }
            ThreadPool.QueueUserWorkItem(o =>
	        {
	            Device.BeginInvokeOnMainThread(() =>
	            {
	                m_viewModel.IsBusy = true;
                });

                m_viewModel.DiscoverServersOnNetwork();

	            Device.BeginInvokeOnMainThread(() =>
	            {
	                m_viewModel.IsBusy = false;
	            });
            });
           
	    }
    }
}