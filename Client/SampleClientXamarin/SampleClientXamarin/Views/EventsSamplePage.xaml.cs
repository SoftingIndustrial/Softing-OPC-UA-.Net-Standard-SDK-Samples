using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SampleClientXamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleClientXamarin.Views
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
	        m_viewModel.CreateEventMonitoredItem();
	    }

	    private void DeleteEventMi_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.DeleteEventMonitoredItem();
	    }
	}
}