/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System.Collections.Generic;
using System.Threading;
using XamarinSampleClient.Helpers;
using XamarinSampleClient.Models;
using Xamarin.Forms;
using System;
using Softing.Opc.Ua.Client.Private;

namespace XamarinSampleClient.ViewModels
{
    /// <summary>
    /// View model for StartPage
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class StartPageViewModel : BaseViewModel
    {
        #region Constructor
        /// <summary>
        /// Create new instance of StartPageViewMode
        /// </summary>
        public StartPageViewModel()
        {
            ThreadPool.QueueUserWorkItem(o => InitializeUaApplication());

            Title = "OPC UA sample client";
           
            WelcomeMessage = "Please select a sample from the list below:";

            Samples = new List<SampleItem>();
            Samples.Add(new SampleItem() { SampleName = "Discover",
                Description = "Sample code for discovering endpoints of a server or discovering the servers on network.",
                Command = SampleCommand.DiscoverySample});
            Samples.Add(new SampleItem() { SampleName = "Connect",
                Description = "Sample code for connecting to a server using various security modes, security policies, encodings or user identities.",
                Command = SampleCommand.ConnectSample});
            Samples.Add(new SampleItem() { SampleName = "Browse",
                Description = "Sample code for browse methods on an OPC UA server.",
                Command = SampleCommand.BrowseSample });
            Samples.Add(new SampleItem() { SampleName = "Read/Write",
                Description = "Sample code for read and write nodes, node attributes, various node values (array, complex or enum values).",
                Command = SampleCommand.ReadWriteSample });
            Samples.Add(new SampleItem() { SampleName = "Monitored item",
                Description = "Sample code for creating and consuming a monitored item on an OPC UA server.",
                Command = SampleCommand.MonitoredItemSample });
            Samples.Add(new SampleItem() { SampleName = "Events",
                Description = "Sample code for creating and consuming an event monitored item on an OPC UA server.",
                Command = SampleCommand.EventsSample });
            Samples.Add(new SampleItem() { SampleName = "Call methods",
                Description = "Sample code for calling methods synchronously and asynchronously on an OPC UA server.",
                Command = SampleCommand.CallMethodsSample });
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get/set welcome message
        /// </summary>
        public string WelcomeMessage { get;set;}

        /// <summary>
        /// List of available samples
        /// </summary>
        public IList<SampleItem> Samples { get; set; }

        /// <summary>
        /// Get/set rference to CurrentSampleViewModel
        /// </summary>
        public BaseViewModel CurrentSampleViewModel { get; set; }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initializes the Application
        /// </summary>
        private async void InitializeUaApplication()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                IsBusy = true;
            });

            try
            {              
                await SampleApplication.InitializeUaApplication();

                LicensingStatus result = LicensingStatus.Ok;

                // TODO - design time license activation
                // Fill in your design time license activation keys here
                // result = SampleApplication.UaApplication.ActivateLicense(LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
                
                if (result == LicensingStatus.Expired)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        WelcomeMessage = "License period expired!";
                        Samples = new List<SampleItem>();
                        OnPropertyChanged("WelcomeMessage");
                        OnPropertyChanged("Samples");
                    });

                }
                if (result == LicensingStatus.Invalid)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        WelcomeMessage = "Invalid License key!";
                        Samples = new List<SampleItem>();
                        OnPropertyChanged("WelcomeMessage");
                        OnPropertyChanged("Samples");
                    });
                }
            }
            catch(Exception ex)
            {
                WelcomeMessage = "An exception occured while initializing UaApplication." + ex.Message;
                Samples.Clear();
                Console.WriteLine(ex);
            }
           
            Device.BeginInvokeOnMainThread(() =>
            {
                IsBusy = false;
            });
        }
        #endregion
    }
}
