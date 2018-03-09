using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XamarinSampleClient.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinSampleClient.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ConnectSamplePage : ContentPage
	{
	    private ConnectViewModel m_viewModel;
        public ConnectSamplePage ()
		{
			InitializeComponent ();

		    BindingContext = m_viewModel = new ConnectViewModel();
		}

	    private void ConnectButton_OnClicked(object sender, EventArgs e)
	    {
	        ThreadPool.QueueUserWorkItem(o =>
	        {
	            Device.BeginInvokeOnMainThread(() =>
	            {
	                m_viewModel.IsBusy = true;
	            });

	            m_viewModel.CreateAndTestSession();

	            Device.BeginInvokeOnMainThread(() =>
	            {
	                m_viewModel.IsBusy = false;
	            });
	        });
        }
	}
}