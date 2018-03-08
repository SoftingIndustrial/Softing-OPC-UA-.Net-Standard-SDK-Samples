/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Configuration;
using Xamarin.Forms;
using XamarinSampleServer.Services;

namespace XamarinSampleServer.ViewModels
{
    /// <summary>
    /// View model for StartPage
    /// </summary>
    class StartPageViewModel : BaseViewModel
    {
        #region Fields
        private string m_resultsText;
        private bool m_canStartServer;
        ApplicationInstance m_application;
        #endregion

        #region Constructors
        public StartPageViewModel()
        {
            Title = "OPC UA Sample Server - Xamarin";
            IPAddress[] addresses = Dns.GetHostAddresses("localhost");
            if (addresses.Length > 0)
            {
                ServerIp = addresses[0].ToString();
            }
            CanStartServer = true;
        }
        #endregion

        #region Properties       
        /// <summary>
        /// Get/set indicator if server can be started
        /// </summary>
        public bool CanStartServer
        {
            get { return m_canStartServer && !IsBusy; }
            set
            {
                SetProperty(ref m_canStartServer, value);
                OnPropertyChanged("CanStopServer");
            }
        }

        /// <summary>
        /// Get/set indicator if server can be started
        /// </summary>
        public bool CanStopServer
        {
            get { return !m_canStartServer && !IsBusy; }
        }
        /// <summary>
        /// Get Server ip
        /// </summary>
        public string ServerIp { get; private set; }

        /// <summary>
        /// Server Url 
        /// </summary>
        public string ServerUrl
        {
            get { return string.Format(@"opc.tcp://{0}:61510/SampleServer", ServerIp); }
        }

        /// <summary>
        /// Results text hint
        /// </summary>
        public string ResultsText
        {
            get { return m_resultsText; }
            set { SetProperty(ref m_resultsText, value); }
        }

        /// <summary>
        /// Indicator is view is busy executing something
        /// </summary>
        public new bool IsBusy
        {
            get { return base.IsBusy; }
            set
            {
                base.IsBusy = value;
                OnPropertyChanged("CanStartServer");
                OnPropertyChanged("CanStopServer");
            }
        }
        #endregion

        #region Start/Stop Server Methods

        /// <summary>
        /// Starts the Server
        /// </summary>
        public async Task StartServer()
        {
            CanStartServer = false;
            IsBusy = true;

            ApplicationConfiguration config;

            if (m_application == null)
            {
                ResultsText = "Initializing application configuration...";
                m_application = new ApplicationInstance
                {
                    ApplicationType = ApplicationType.Server,
                    ConfigSectionName = "XamarinSampleServer"
                };
                if (Device.RuntimePlatform == "Android")
                {
                    string currentFolder = DependencyService.Get<IPathService>().PublicExternalFolder.ToString();
                    string filename = m_application.ConfigSectionName + ".Config.xml";
                    string content = DependencyService.Get<IAssetService>().LoadFile(filename);

                    File.WriteAllText(currentFolder + filename, content);
                    // load the application configuration.
                    config = await m_application.LoadApplicationConfiguration(currentFolder + filename, false);
                    config.ServerConfiguration.BaseAddresses[0] = ServerUrl;
                }
                else
                {
                    // load the application configuration.
                    config = await m_application.LoadApplicationConfiguration(false);
                }
            }
            try
            {
                ResultsText += "\nChecking certificates...";
                // Check the application certificate
                await m_application.CheckApplicationInstanceCertificate(false, 0);
                m_application.ApplicationConfiguration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;
                ResultsText += "\nStarting server...";
                // Start the server
                await m_application.Start(new SampleServer.SampleServer());

                ResultsText += "\n\n\nXamarin SampleServer is running:";

                for (int i = 0; i < m_application.ApplicationConfiguration.ServerConfiguration.BaseAddresses.Count; i++)
                {
                    ResultsText += "\n" + m_application.ApplicationConfiguration.ServerConfiguration.BaseAddresses[i];
                }       
            }
            catch (Exception e)
            {
                ResultsText += string.Format("\n\nError starting server url: {0}.\n{1}", ServerUrl, e.Message);
            }    

            IsBusy = false;            
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void StopServer()
        {
            ResultsText = "Stopping server...";            
            if (m_application != null)
            {
                m_application.Stop();
            }
            CanStartServer = true;
            ResultsText = "Server was stopped.";
        }

        #endregion

        #region Event Handlers
        private static void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            e.Accept = true;
        }
        #endregion
    }
}
