using System;
using System.Threading;
using XamarinSampleClient.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinSampleClient.Views
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
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = true;
                });

                m_viewModel.CallMethod();

                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = false;
                });
            });
        }

	    private void CallMethodAsync_OnClicked(object sender, EventArgs e)
	    {	        
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = true;
                });

                m_viewModel.AsyncCallMethod();

                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = false;
                });
            });
        }
	}
}