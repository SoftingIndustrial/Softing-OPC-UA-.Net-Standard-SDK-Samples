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
using Softing.Opc.Ua;

namespace SampleClient.Samples
{
    /// <summary>
    /// Sample Client class that provides discovery functionality
    /// </summary>
    public class DiscoveryClient
    {
        #region Private Fields
        private readonly UaApplication m_application;
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of DiscoveryClient
        /// </summary>
        public DiscoveryClient(UaApplication application)
        {
            m_application = application;
        }
        #endregion

        #region Discovery Methods
        /// <summary>
        /// Displays all registered server applications and their available endpoints.
        /// </summary>
        public void DiscoverServers()
        {
            try
            {
                Console.WriteLine("Discovering all available servers and their endpoints from local host...");

                // the method will return all the registered server applications from the specified machine.
                // if the "discoveryUrl" parameter is null or empty, DiscoverServers() will return the servers from the local machine.
                // use the default discovery url of the local machine
                string hostname = System.Net.Dns.GetHostName();
                string discoveryUrl = Opc.Ua.Utils.Format(Opc.Ua.Utils.DiscoveryUrls[0], hostname);

                var servers = m_application.DiscoverServers(discoveryUrl);
                Console.WriteLine("DiscoverServers returned {0} results:", servers.Count);

                foreach (var serverDescription in servers)
                {
                    try
                    {
                        if (serverDescription.DiscoveryUrls == null || serverDescription.DiscoveryUrls.Count == 0)
                        {
                            // skip servers without DiscoveryUrl information.
                            continue;
                        }

                        // retrieve available endpoints for each registered server and display their information.
                        Console.WriteLine("\r\nCall GetEndpoints for server: {0} ...", serverDescription.ApplicationUri);
                        string serverDiscoveryUrl = serverDescription.DiscoveryUrls[0];
                        var endpoins = m_application.GetEndpoints(serverDiscoveryUrl);

                        Console.WriteLine("-Server: {0} has {1} available endpoints:", serverDiscoveryUrl, endpoins.Count);

                        foreach (var endpointDescription in endpoins)
                        {
                            Console.WriteLine("       {0} - {1} - {2}",
                                endpointDescription.EndpointUrl,
                                endpointDescription.SecurityMode,
                                endpointDescription.SecurityPolicy);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("-Server: {0} GetEndpoints Error: {1}", serverDescription.ApplicationUri, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DiscoverServers Error : {0}.", ex.Message);
            }
        }

        /// <summary>
        /// Displays all registered server applications on the network.
        /// </summary>
        public void DiscoverServersOnNetwork()
        {
            try
            {
                Console.WriteLine("Discovering all available servers and their endpoints from local network...");

                // The method will return all the registered server applications from the local network.
                // DiscoverServersOnNetwork service is supported only by LDS-ME installations.
                // If the "discoveryUrl" parameter is null or empty, DiscoverServersOnNetwork() will be invoked on the local machine.
                var serversOnNetwork = m_application.DiscoverServersOnNetwork(null);

                Console.WriteLine("DiscoverServersOnNetwork returned {0} results:", serversOnNetwork.Count);

                foreach (var serverOnNetwork in serversOnNetwork)
                {
                    try
                    {
                        if (String.IsNullOrEmpty( serverOnNetwork.DiscoveryUrl))
                        {
                            // skip servers without DiscoveryUrl information.
                            continue;
                        }

                        // retrieve available endpoints for each registered server and display their information.
                        Console.WriteLine("\r\nCall GetEndpoints for server: {0} ...", serverOnNetwork.ServerName);
                        string serverDiscoveryUrl = serverOnNetwork.DiscoveryUrl;
                        var endpoins = m_application.GetEndpoints(serverDiscoveryUrl);

                        Console.WriteLine("-Server: {0} has {1} available endpoints:", serverDiscoveryUrl, endpoins.Count);

                        foreach (var endpointDescription in endpoins)
                        {
                            Console.WriteLine("       {0} - {1} - {2}",
                                endpointDescription.EndpointUrl,
                                endpointDescription.SecurityMode,
                                endpointDescription.SecurityPolicy);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("-Server: {0} GetEndpoints Error: {1}", serverOnNetwork.DiscoveryUrl, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DiscoverServersOnNetwork Error : {0}.", ex.Message);
            }
        }
        #endregion
    }
}