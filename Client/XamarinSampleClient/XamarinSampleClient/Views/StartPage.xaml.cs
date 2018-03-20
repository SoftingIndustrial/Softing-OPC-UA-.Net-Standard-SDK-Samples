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
        public ObservableCollection<string> Items { get; set; }

        public StartPage()
        {
            InitializeComponent();

            BindingContext = m_viewModel = new StartPageViewModel();
            
        }
        protected override void OnAppearing()
        {           
            if (m_currentContentPage != null)
            {
                //close current child view model if possible
                BaseViewModel viewModel = m_currentContentPage.BindingContext as BaseViewModel;
                if (viewModel != null)
                {
                    viewModel.Close();
                }
                m_currentContentPage = null;
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
                await Navigation.PushAsync(m_currentContentPage);
            }
            //remove selection
            ((ListView)sender).SelectedItem = null;
        }        
    }
}