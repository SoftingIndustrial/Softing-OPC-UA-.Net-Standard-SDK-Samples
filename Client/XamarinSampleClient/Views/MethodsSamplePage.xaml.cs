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

        protected override bool OnBackButtonPressed()
        {
            m_viewModel.Close();
            return base.OnBackButtonPressed();
        }

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