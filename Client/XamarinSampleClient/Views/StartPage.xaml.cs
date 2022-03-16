using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XamarinSampleClient.Helpers;
using XamarinSampleClient.Models;
using XamarinSampleClient.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinSampleClient.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StartPage : ContentPage
    {
        private StartPageViewModel m_viewModel;
        private ContentPage m_currentContentPage;

        public StartPage()
        {
            BindingContext = m_viewModel = new StartPageViewModel();

            InitializeComponent();

            
            
        }
        protected override void OnAppearing()
        {
          //  Samples.ItemsSource
            if (m_viewModel.CurrentSampleViewModel != null)
            {
                //close current child view model if possible
                m_viewModel.CurrentSampleViewModel.Close();
                m_viewModel.CurrentSampleViewModel = null;
            }
        }

        async void Samples_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (m_viewModel.IsBusy)
            {
                ((ListView)sender).SelectedItem = null;
                return;
            }
            //get selected item
            SampleItem tappedItem = e.Item as SampleItem;
            if (tappedItem == null)
            {
                return;
            }            
            
            //open desired page
            switch (tappedItem.Command)
            {
                case SampleCommand.DiscoverySample:
                    m_currentContentPage = new DiscoverySamplePage();                   
                    break;
                case SampleCommand.BrowseSample:
                    m_currentContentPage = new BrowseSamplePage();
                    break;
                case SampleCommand.CallMethodsSample:
                    m_currentContentPage = new MethodsSamplePage();
                    break;
                case SampleCommand.ConnectSample:
                    m_currentContentPage = new ConnectSamplePage();
                    break;
                case SampleCommand.EventsSample:
                    m_currentContentPage = new EventsSamplePage();
                    break;
                case SampleCommand.MonitoredItemSample:
                    m_currentContentPage = new MonitoredItemSamplePage();
                    break;
                case SampleCommand.ReadWriteSample:
                    m_currentContentPage = new ReadWriteSamplePage();
                    break;
                default:
                        break;
            }
            if (m_currentContentPage != null)
            {
                m_viewModel.CurrentSampleViewModel = m_currentContentPage.BindingContext as BaseViewModel;
                await Navigation.PushAsync(m_currentContentPage);
            }
            //remove selection
            ((ListView)sender).SelectedItem = null;
        }        
    }
}