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
	public partial class MonitoredItemSamplePage : ContentPage
	{
	    private MonitoredItemViewModel m_viewModel;
        public MonitoredItemSamplePage ()
		{
			InitializeComponent ();

		    BindingContext = m_viewModel = new MonitoredItemViewModel();
            
		}   

	    private void CreateMi_OnClicked(object sender, EventArgs e)
	    {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = true;
                });

                m_viewModel.CreateMonitoredItem();

                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = false;
                });
            });
        }

	    private void DeleteMi_OnClicked(object sender, EventArgs e)
	    {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = true;
                });

                m_viewModel.DeleteMonitoredItem();

                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = false;
                });
            });
        }
	}
}