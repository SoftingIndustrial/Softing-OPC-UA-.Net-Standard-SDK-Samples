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
	public partial class EventsSamplePage : ContentPage
	{
	    private EventsViewModel m_viewModel;
	    public EventsSamplePage()
	    {
	        InitializeComponent();

	        BindingContext = m_viewModel = new EventsViewModel();
	    }


        /// <summary>When overridden, allows the application developer to customize behavior as the <see cref="T:Xamarin.Forms.Page" /> disappears.</summary>
        /// <remarks>To be added.</remarks>
        protected override void OnDisappearing()
	    {
	        //ensure the session is disconnected 
	        m_viewModel.DisconnectSession();

	        base.OnDisappearing();
	    }

	    private void CreateEventMi_OnClicked(object sender, EventArgs e)
	    {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = true;
                });

                m_viewModel.CreateEventMonitoredItem();

                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = false;
                });
            });
        }

	    private void DeleteEventMi_OnClicked(object sender, EventArgs e)
	    {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = true;
                });

                m_viewModel.DeleteEventMonitoredItem();

                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = false;
                });
            });
        }
	}
}