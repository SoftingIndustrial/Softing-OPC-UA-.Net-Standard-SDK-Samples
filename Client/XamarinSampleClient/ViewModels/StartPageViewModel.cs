/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System.Collections.Generic;
using System.Threading;
using XamarinSampleClient.Helpers;
using XamarinSampleClient.Models;
using Xamarin.Forms;
using System;
using Softing.Opc.Ua.Client;

namespace XamarinSampleClient.ViewModels
{
    /// <summary>
    /// View model for StartPage
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class StartPageViewModel : BaseViewModel
    {
        private IList<SampleItem> m_samples;
        private String m_welcomeMessage;

        #region Constructor
        /// <summary>
        /// Create new instance of StartPageViewMode
        /// </summary>
        public StartPageViewModel()
        {
            ThreadPool.QueueUserWorkItem(o => InitializeUaApplication());

            Title = "OPC UA sample client";
           
            WelcomeMessage = "Please select a sample from the list below:";

            List<SampleItem> samples = new List<SampleItem>();
            samples.Add(new SampleItem() { SampleName = "Discover",
                Description = "Sample code for discovering endpoints of a server or discovering the servers on network.",
                Command = SampleCommand.DiscoverySample});
            samples.Add(new SampleItem() { SampleName = "Connect",
                Description = "Sample code for connecting to a server using various security modes, security policies, encodings or user identities.",
                Command = SampleCommand.ConnectSample});
            samples.Add(new SampleItem() { SampleName = "Browse",
                Description = "Sample code for browse methods on an OPC UA server.",
                Command = SampleCommand.BrowseSample });
            samples.Add(new SampleItem() { SampleName = "Read/Write",
                Description = "Sample code for read and write nodes, node attributes, various node values (array, complex or enum values).",
                Command = SampleCommand.ReadWriteSample });
            samples.Add(new SampleItem() { SampleName = "Monitored item",
                Description = "Sample code for creating and consuming a monitored item on an OPC UA server.",
                Command = SampleCommand.MonitoredItemSample });
            samples.Add(new SampleItem() { SampleName = "Events",
                Description = "Sample code for creating and consuming an event monitored item on an OPC UA server.",
                Command = SampleCommand.EventsSample });
            samples.Add(new SampleItem() { SampleName = "Call methods",
                Description = "Sample code for calling methods synchronously and asynchronously on an OPC UA server.",
                Command = SampleCommand.CallMethodsSample });
            Samples = samples;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get/set welcome message
        /// </summary>
        public string WelcomeMessage
        {
            get { return m_welcomeMessage; }
            set { SetProperty(ref m_welcomeMessage, value); }
        }

        /// <summary>
        /// List of available samples
        /// </summary>
        public IList<SampleItem> Samples
        {
            get { return m_samples; }
            set { SetProperty(ref m_samples, value); }
        }
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

                // TODO - binary license activation
                // Fill in your Client binary license activation keys here
                // result = SampleApplication.UaApplication.ActivateLicense(LicenseFeature.Client, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");

                if (result != LicensingStatus.Ok)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        WelcomeMessage = string.Format("License key status is: {0}!", result);
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
