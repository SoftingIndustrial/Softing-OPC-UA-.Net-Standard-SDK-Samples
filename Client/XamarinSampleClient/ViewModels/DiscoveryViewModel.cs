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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Opc.Ua;
using XamarinSampleClient.Helpers;
using XamarinSampleClient.Models;
using Softing.Opc.Ua.Client;

namespace XamarinSampleClient.ViewModels
{
    /// <summary>
    /// View model for DiscoveryPage
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class DiscoveryViewModel : BaseViewModel
    {
        #region Fields
        private string m_resultsText;
        private string m_serverUrlEndpoints;
        private string m_serverUrlNetwork;
        public ObservableCollection<string> m_results;
        #endregion

        #region Constructors
        public DiscoveryViewModel()
        {
            Title = "Discovery sample";
            m_results = new ObservableCollection<string>();
            ServerUrlEndpoints = App.DefaultSampleServerUrl;
            ServerUrlNetwork = "opc.tcp://0.0.0.0:4840";
        }
        #endregion

        #region Properties

        /// <summary>
        /// Results list
        /// </summary>
        public ObservableCollection<string> Results
        {
            get { return m_results; }
        }

        /// <summary>
        /// Server Url for Endpoints
        /// </summary>
        public string ServerUrlEndpoints
        {
            get { return m_serverUrlEndpoints; }
            set
            {
                SetProperty(ref m_serverUrlEndpoints, value);
                m_results.Clear();
                ResultsText = "";
                App.DefaultSampleServerUrl = value;
            }
        }

        /// <summary>
        /// Server Url for Endpoints
        /// </summary>
        public string ServerUrlNetwork
        {
            get { return m_serverUrlNetwork; }
            set
            {
                SetProperty(ref m_serverUrlNetwork, value);
                m_results.Clear();
                ResultsText = "";
            }
        }
        
        /// <summary>
        /// Results text hint
        /// </summary>
        public string ResultsText
        {
            get { return m_resultsText; }
            set{SetProperty(ref m_resultsText, value);}
        }
        #endregion

        #region Discovery Methods

        /// <summary>
        /// Displays all registered server applications and their available endpoints.
        /// </summary>
        public void DiscoverEndpoints()
        {
            try
            {
                Results.Clear();
                ResultsText = string.Format("Endpoint results for '{0}':", ServerUrlEndpoints);

                // the method will return all the registered server applications from the specified machine.
                // if the "discoveryUrl" parameter is null or empty, DiscoverServers() will return the servers from the local machine.
                // use the default discovery url of the local machine
                var servers = SampleApplication.UaApplication.DiscoverServers(ServerUrlEndpoints);
                foreach (var applicationDescription in servers)
                {
                    try
                    {
                        string serverDiscoveryUrl;
                        if (applicationDescription.DiscoveryUrls == null || applicationDescription.DiscoveryUrls.Count == 0)
                        {
                            serverDiscoveryUrl = ServerUrlEndpoints;
                        }
                        else
                        {
                            // retrieve available endpoints for each registered server and display their information.
                            serverDiscoveryUrl = applicationDescription.DiscoveryUrls[0];
                        }
                        
                        Results.Add(serverDiscoveryUrl);
                        IList<EndpointDescriptionEx> endpoins = SampleApplication.UaApplication.GetEndpoints(serverDiscoveryUrl);

                        foreach (EndpointDescriptionEx endpointDescription in endpoins)
                        {
                            Results.Add(string.Format("----- SecurityMode: {0}, Policy: {1}",  endpointDescription.SecurityMode, endpointDescription.SecurityPolicy));
                        }
                    }
                    catch (Exception ex)
                    {
                        Results.Add(string.Format("----- GetEndpoints Error: {0}", ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                Results.Add(string.Format("DiscoverEndpoints Error : {0}.", ex.Message));
            }
        }

        /// <summary>
        /// Displays all registered server applications on the network.
        /// </summary>
        public void DiscoverServersOnNetwork()
        {
            try
            {
                Results.Clear();
                ResultsText = string.Format("Network results for '{0}':", ServerUrlNetwork);
                // The method will return all the registered server applications from the local network.
                // DiscoverServersOnNetwork service is supported only by LDS-ME installations.
                // If the "discoveryUrl" parameter is null or empty, DiscoverServersOnNetwork() will be invoked on the local machine.
                var serversOnNetwork = SampleApplication.UaApplication.DiscoverServersOnNetwork(ServerUrlNetwork);
                foreach (var serverOnNetwork in serversOnNetwork)
                {
                    try
                    {
                        // skip servers without DiscoveryUrl information.
                        if (String.IsNullOrEmpty(serverOnNetwork.DiscoveryUrl))
                        {
                            continue;
                        }

                        // ignore Urls with unsuported transport profiles.
                        if (!serverOnNetwork.DiscoveryUrl.StartsWith(Utils.UriSchemeOpcTcp))
                        {
                            continue;
                        }

                        // retrieve available endpoints for each registered server and display their information.
                        string serverDiscoveryUrl = serverOnNetwork.DiscoveryUrl.Replace(".local.", "");
                        Results.Add(serverDiscoveryUrl);

                        var endpoins = SampleApplication.UaApplication.GetEndpoints(serverDiscoveryUrl);
                        foreach (var endpointDescription in endpoins)
                        {
                            Results.Add(string.Format("----- SecurityMode: {0}, Policy: {1}", endpointDescription.SecurityMode, endpointDescription.SecurityPolicy));
                        }
                    }
                    catch (Exception ex)
                    {
                        Results.Add(string.Format("----- GetEndpoints Error: {0}", ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                Results.Add(string.Format("DiscoverServersOnNetwork Error : {0}.", ex.Message));
            }
        }

        #endregion
    }
}
