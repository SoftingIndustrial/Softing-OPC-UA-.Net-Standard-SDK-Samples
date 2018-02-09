using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SampleClientXamarin.ViewModels;
using Softing.Opc.Ua;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleClientXamarin.Views
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

	    private void DiscoverEndpoints_OnClicked(object sender, EventArgs e)
	    {
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

	    private void DiscoverServers_OnClicked(object sender, EventArgs e)
	    {
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