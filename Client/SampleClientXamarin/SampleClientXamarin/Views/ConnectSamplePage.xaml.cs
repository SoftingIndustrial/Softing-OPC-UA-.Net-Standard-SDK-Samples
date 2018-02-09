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
	public partial class ConnectSamplePage : ContentPage
	{
	    private ConnectViewModel m_viewModel;
        public ConnectSamplePage ()
		{
			InitializeComponent ();

		    BindingContext = m_viewModel = new ConnectViewModel();
		}

	    private void Button_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.CreateAndTestSession();
        }
	}
}