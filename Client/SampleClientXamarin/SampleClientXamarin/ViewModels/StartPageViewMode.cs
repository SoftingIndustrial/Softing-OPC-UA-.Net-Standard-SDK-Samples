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
            Samples.Add(new SampleItem() { SampleName = "Discovery sample",
                Description = "This sample demonstrates the Discovery functionality",
                Command = SampleCommand.DiscoverySample});
            Samples.Add(new SampleItem() { SampleName = "Connect sample",
                Description = "This sample demonstrates the Connect functionality",
                Command = SampleCommand.ConnectSample});
            Samples.Add(new SampleItem() { SampleName = "Browse sample",
                Description = "This sample demonstrates the Browse functionality",
                Command = SampleCommand.BrowseSample });
            Samples.Add(new SampleItem() { SampleName = "Read/Write sample",
                Description = "This sample demonstrates the Read and Write functionality",
                Command = SampleCommand.ReadWriteSample });
            Samples.Add(new SampleItem() { SampleName = "MonitoredItem sample",
                Description = "This sample demonstrates the Monitored item functionality",
                Command = SampleCommand.MonitoredItemSample });
            Samples.Add(new SampleItem() { SampleName = "Events sample",
                Description = "This sample demonstrates the Events functionality",
                Command = SampleCommand.EventsSample });
            Samples.Add(new SampleItem() { SampleName = "Call methods sample",
                Description = "This sample demonstrates the Method call functionality",
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
