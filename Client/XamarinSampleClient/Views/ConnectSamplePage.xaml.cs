using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XamarinSampleClient.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Plugin.FilePicker;
using System.IO;
using XamarinSampleClient.Services;

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

	            m_viewModel.CreateAndConnectSession();

	            Device.BeginInvokeOnMainThread(() =>
	            {
	                m_viewModel.IsBusy = false;
	            });
	        });
        }

        private void DisconnectButton_OnClicked(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = true;
                });

                m_viewModel.DisconnectSession();

                Device.BeginInvokeOnMainThread(() =>
                {
                    m_viewModel.IsBusy = false;
                });
            });
        }

        private async void FindCertificateFile_OnClicked(object sender, EventArgs e)
        {
            string currentFolder = @"/storage/emulated/0/Softing/certificates/";
            string filename = "opcuser.pfx";
            
            if (!File.Exists(currentFolder + filename))
            {
                if (await DisplayAlert("Confirm", $"Do you want to copy the certificate file to internal storage folder {currentFolder}?", "Yes", "No"))
                {
                    Directory.CreateDirectory(currentFolder);
                    DependencyService.Get<IAssetService>().SaveFile(filename, currentFolder + filename);
                    m_viewModel.UserCertificate = currentFolder + filename;
                    return;
                }              
               
            }

            if (await DisplayAlert("Confirm", $"Do you want to use default certificate: {currentFolder + filename}?", "Yes", "No"))
            {
                m_viewModel.UserCertificate = currentFolder + filename;
                return;
            }            

            var pickedFile = await CrossFilePicker.Current.PickFile();

            if (pickedFile != null)
            {
                m_viewModel.UserCertificate = pickedFile.FilePath;
            }
        }        
    }
}