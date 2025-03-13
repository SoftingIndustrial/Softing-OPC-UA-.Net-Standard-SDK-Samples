/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinSampleServer.Model;
using XamarinSampleServer.Services;
using Opc.Ua;
using Opc.Ua.Configuration;
using Softing.Opc.Ua.Server;

namespace XamarinSampleServer.ViewModels
{
    /// <summary>
    /// View model for StartPage
    /// </summary>

    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class StartPageViewModel : BaseViewModel
    {
        public static StartPageViewModel Instance;        
        
        #region Fields
        private ObservableCollection<ConnectedSession> m_connectedSessions;
        private bool m_isRefreshingSessions;
        private string m_resultsText;
        private bool m_canStartServer;
        private SampleServer.SampleServer m_sampleServer;
        LicensingStatus m_isValidLicenseKey = LicensingStatus.Ok;
        #endregion

        #region Constructors
        static StartPageViewModel()
        {
            Instance = new StartPageViewModel();
        }

        private StartPageViewModel()
        {
            Title = "OPC UA Sample Server - Xamarin";
            ServerIps = new ObservableCollection<string>();
            IPAddress[] addresses = Dns.GetHostAddresses("localhost");
            foreach(var ipAddress in addresses)
            {
                ServerIps.Add(ipAddress.ToString());
            }
           
            LoadSessionsCommand = new Command(() => ExecuteLoadSessionsCommand());
            m_connectedSessions = new ObservableCollection<ConnectedSession>();

            // TODO - Server binary license activation
            // Fill in your Server binary license activation keys here
            // m_isValidLicenseKey = Softing.Opc.Ua.Server.License.ActivateLicense(Softing.Opc.Ua.Server.LicenseFeature.Server, "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");

            if (m_isValidLicenseKey != Softing.Opc.Ua.Server.LicensingStatus.Ok)
            {
                ResultsText = string.Format("\n\nError starting server: Server license status is: {0}!", m_isValidLicenseKey);
                Console.WriteLine();
                CanStartServer = false;
                return;
            }

            CanStartServer = true;
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = true;
                });

                StartServer().Wait();

                Device.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = false;
                });
            });
        }
        #endregion

        #region Properties    
        /// <summary>
        /// Get/set command for load sessions
        /// </summary>
        public Command LoadSessionsCommand { get; private set; }

        /// <summary>
        /// Get/set indicator IsRefreshingSessions
        /// </summary>
        public bool IsRefreshingSessions
        {
            get { return m_isRefreshingSessions; }
            set { SetProperty(ref m_isRefreshingSessions, value); }
        }

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
            get { return !m_canStartServer && !IsBusy && m_isValidLicenseKey == Softing.Opc.Ua.Server.LicensingStatus.Ok; }
        }
        /// <summary>
        /// Get Server ip list
        /// </summary>
        public ObservableCollection<string> ServerIps { get; private set; }

        /// <summary>
        /// Results text hint
        /// </summary>
        public string ResultsText
        {
            get { return m_resultsText; }
            set { SetProperty(ref m_resultsText, value); }
        }

        /// <summary>
        /// Get list of connected sessions
        /// </summary>
        public ObservableCollection<ConnectedSession> ConnectedSessions
        {
            get { return m_connectedSessions; }
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
            
            string serverUrl = "";
           
            ResultsText = "Initializing application configuration...";                
            if (Device.RuntimePlatform == "Android")
            {

                string currentFolder = @"/storage/emulated/0/Softing/config/";
                string filename = "XamarinSampleServer.Config.xml";
                string content = DependencyService.Get<IAssetService>().LoadFile(filename);                

                Directory.CreateDirectory(currentFolder);
                if (!File.Exists(currentFolder + filename))
                {
                    File.WriteAllText(currentFolder + filename, content);
                }                
                try
                {
                    ApplicationConfiguration config;
                    config = await new ApplicationInstance().LoadApplicationConfiguration(currentFolder + filename, false);
                    config.ServerConfiguration.BaseAddresses.Clear();
                    config.ApplicationType = ApplicationType.Server;
                    foreach (var serverIp in ServerIps)
                    {
                        string url = $"opc.tcp://{serverIp}:61510/SampleServer";
                        config.ServerConfiguration.BaseAddresses.Add(url);
                        serverUrl += url + "\n";
                        break;
                    }

                    ResultsText += "\nStarting server...";

                    // Start the server
                    m_sampleServer = new SampleServer.SampleServer();
                    await m_sampleServer.Start(config);

                    // Check if the server addresses are available
                    List<string> activeListenersUris = m_sampleServer.GetActiveListenersUris();
                    if (activeListenersUris.Count > 0)
                    {
                        ResultsText = "Server is running.";
                    }
                    else
                    {
                        ResultsText = "Server address is not available.";
                    }
                }
                catch (Exception e)
                {
                    ResultsText += string.Format("\n\nError starting server url: {0}.\n{1}", serverUrl, e.Message);
                }
            }                

            Device.StartTimer(new TimeSpan(0, 0, 20), () => {
                LoadSessionsCommand.Execute(null);
                return CanStopServer;
            });

            IsBusy = false;
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void StopServer()
        {
            ResultsText = "Stopping server...";
            if (m_sampleServer != null)
            {
                m_sampleServer.Stop();
                m_connectedSessions.Clear();
                m_sampleServer.Dispose();
                m_sampleServer = null;
            }
            CanStartServer = true;
            ResultsText = "Server was stopped.";
        }

        #endregion

        #region Execute Command Methods

        public void ExecuteLoadSessionsCommand()
        {
            if (IsRefreshingSessions)
                return;
            IsRefreshingSessions = true;

            m_connectedSessions.Clear();
            if (m_sampleServer != null)
            {
                try
                {
                    IList<Opc.Ua.Server.Session> sessions = m_sampleServer.CurrentInstance.SessionManager.GetSessions();
                    IList<Opc.Ua.Server.Subscription> subscriptions = m_sampleServer.CurrentInstance.SubscriptionManager.GetSubscriptions();
                    foreach (var session in sessions)
                    {
                        m_connectedSessions.Add(new ConnectedSession()
                        {
                            SessionName = session.SessionDiagnostics.SessionName,
                            SubscriptionsCount = session.SessionDiagnostics.CurrentSubscriptionsCount,
                        });
                    }
                }
                catch(Exception ex)
                {
                    // ignore esception that may arrive because the server is stopped
                }

            }
            IsRefreshingSessions = false;
        }
        #endregion

        #region Event Handlers
        private static void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            // accept all certificates no matter of error codes received
            e.AcceptAll = true;
        }
        #endregion
    }
}
