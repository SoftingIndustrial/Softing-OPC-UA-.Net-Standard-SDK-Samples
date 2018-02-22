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
using SampleClientXamarin.Helpers;
using SampleClientXamarin.Models;
using Xamarin.Forms;

namespace SampleClientXamarin.ViewModels
{
   /// <summary>
   /// View model for StartPage
   /// </summary>
    class StartPageViewMode : BaseViewModel
    {
        #region Constructor
        /// <summary>
        /// Create new instance of StartPageViewMode
        /// </summary>
        public StartPageViewMode()
        {
            ThreadPool.QueueUserWorkItem(o => InitializeUaApplication());

            Title = "OPC UA sample client";
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
        /// List of available samples
        /// </summary>
        public IList<SampleItem> Samples { get; set; }
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
          
            await SampleApplication.InitializeUaApplication();
           
            Device.BeginInvokeOnMainThread(() =>
            {
                IsBusy = false;
            });
        }
        #endregion
    }
}
