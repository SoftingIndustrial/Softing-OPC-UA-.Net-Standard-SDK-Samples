using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SampleClientXamarin.Helpers;
using SampleClientXamarin.Models;
using SampleClientXamarin.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleClientXamarin.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StartPage : ContentPage
    {
        private StartPageViewMode m_viewModel;
        public ObservableCollection<string> Items { get; set; }

        public StartPage()
        {
            InitializeComponent();

            BindingContext = m_viewModel = new StartPageViewMode();
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
                    await Navigation.PushAsync(new NavigationPage(new DiscoverySamplePage()));
                    break;
                case SampleCommand.BrowseSample:
                    await Navigation.PushAsync(new NavigationPage(new BrowseSamplePage()));
                    break;
                case SampleCommand.CallMethodsSample:
                    await Navigation.PushAsync(new NavigationPage(new MethodsSamplePage()));
                    break;
                case SampleCommand.ConnectSample:
                    await Navigation.PushAsync(new NavigationPage(new ConnectSamplePage()));
                    break;
                case SampleCommand.EventsSample:
                    await Navigation.PushAsync(new NavigationPage(new EventsSamplePage()));
                    break;
                case SampleCommand.MonitoredItemSample:
                    await Navigation.PushAsync(new NavigationPage(new MonitoredItemSamplePage()));
                    break;
                case SampleCommand.ReadWriteSample:
                    await Navigation.PushAsync(new NavigationPage(new ReadWriteSamplePage()));
                    break;
                default:
                        break;
            }
            //remove selection
            ((ListView)sender).SelectedItem = null;
        }


        
    }
}