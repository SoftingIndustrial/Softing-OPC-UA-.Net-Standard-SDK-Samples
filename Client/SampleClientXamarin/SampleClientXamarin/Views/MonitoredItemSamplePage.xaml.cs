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
	public partial class MonitoredItemSamplePage : ContentPage
	{
	    private MonitoredItemViewModel m_viewModel;
        public MonitoredItemSamplePage ()
		{
			InitializeComponent ();

		    BindingContext = m_viewModel = new MonitoredItemViewModel();
		}

	    /// <summary>When overridden, allows the application developer to customize behavior as the <see cref="T:Xamarin.Forms.Page" /> disappears.</summary>
	    /// <remarks>To be added.</remarks>
	    protected override void OnDisappearing()
	    {
	        //ensure the session is disconnected 
	        m_viewModel.DisconnectSession();

	        base.OnDisappearing();
	    }

	    private void CreateMi_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.CreateMonitoredItem();
	    }

	    private void DeleteMi_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.DeleteMonitoredItem();
	    }
	}
}