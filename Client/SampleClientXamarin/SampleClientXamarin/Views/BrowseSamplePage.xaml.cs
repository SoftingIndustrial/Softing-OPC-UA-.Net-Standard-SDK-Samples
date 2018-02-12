using System;
using SampleClientXamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleClientXamarin.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BrowseSamplePage : ContentPage
	{
	    private BrowseViewModel m_viewModel;
		public BrowseSamplePage ()
		{
			InitializeComponent ();

		    BindingContext = m_viewModel = new BrowseViewModel();
		}

	    private void Browse_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.BrowseTheServer();
	    }

	    private void BrowseWithOptions_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.BrowseWithOptions();
        }
	}
}