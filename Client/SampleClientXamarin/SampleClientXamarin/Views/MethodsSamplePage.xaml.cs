using System;
using SampleClientXamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleClientXamarin.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MethodsSamplePage : ContentPage
	{
	    private MethodsViewModel m_viewModel;
        public MethodsSamplePage ()
		{
			InitializeComponent ();
		    BindingContext = m_viewModel = new MethodsViewModel();
		}

	    #region Overrides of Page

	    /// <summary>When overridden, allows the application developer to customize behavior as the <see cref="T:Xamarin.Forms.Page" /> disappears.</summary>
	    /// <remarks>To be added.</remarks>
	    protected override void OnDisappearing()
	    {
            //ensure the session is disconnected 
	        m_viewModel.DisconnectSession();

            base.OnDisappearing();
	    }

	    #endregion

	    private void CallMethod_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.CallMethod();
	    }

	    private void CallMethodAsync_OnClicked(object sender, EventArgs e)
	    {
	        m_viewModel.AsyncCallMethod();
	    }
	}
}