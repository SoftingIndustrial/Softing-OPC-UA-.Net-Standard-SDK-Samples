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
using System.Threading.Tasks;
using Opc.Ua;
using Softing.Opc.Ua.Client;

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
                Console.WriteLine("\r\nDiscovering all available servers and their endpoints from local host...");
                
                string hostname = System.Net.Dns.GetHostName();
                string discoveryUrl = Utils.Format(Utils.DiscoveryUrls[0], hostname);

                // the method will return all the registered server applications from the specified machine.
                // if the "discoveryUrl" parameter is null or empty, DiscoverServers() will return the servers from the local machine.
                // use the default discovery url of the local machine
                IList<ApplicationDescription> appDescriptions = m_application.DiscoverServers(discoveryUrl);
                Console.WriteLine("DiscoverServers returned {0} results:", appDescriptions.Count);

                foreach (var applicationDescription in appDescriptions)
                {
                    try
                    {
                        if (applicationDescription.DiscoveryUrls == null || applicationDescription.DiscoveryUrls.Count == 0)
                        {
                            // skip servers without DiscoveryUrl information.
                            continue;
                        }

                        // retrieve available endpoints for each registered server and display their information.
                        Console.WriteLine("\r\nGet available endpoints for server: {0} ...", applicationDescription.ApplicationUri);
                        string serverDiscoveryUrl = applicationDescription.DiscoveryUrls[0];
                        IList<EndpointDescriptionEx> endpoins = m_application.GetEndpoints(serverDiscoveryUrl);

                        Console.WriteLine("Server: {0} returned {1} available endpoints:", serverDiscoveryUrl, endpoins.Count);

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
                        Program.PrintException(string.Format("Server: {0} GetEndpoints", applicationDescription.ApplicationUri), ex);
                    }                    
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DiscoverServers", ex);
            }
        }


        /// <summary>
        /// Asynchronously Gets and Displays all registered server applications and their available endpoints.
        /// The method calls <see cref="UaApplication.DiscoverServersAsync(string)"/> method.
        /// </summary>
        public async Task DiscoverServersAsync()
        {
            try
            {
                Console.WriteLine("\r\nDiscovering asynchronously all available servers and their endpoints from local host...");

                string hostname = System.Net.Dns.GetHostName();
                string discoveryUrl = Utils.Format(Utils.DiscoveryUrls[0], hostname);

                // the method will return all the registered server applications from the specified machine.
                // if the "discoveryUrl" parameter is null or empty, DiscoverServersAsync() will return the servers from the local machine.
                // use the default discovery url of the local machine
                IList<ApplicationDescription> appDescriptions = await m_application.DiscoverServersAsync(discoveryUrl).ConfigureAwait(false);
                Console.WriteLine("DiscoverServersAsync returned {0} results:", appDescriptions.Count);

                foreach (var applicationDescription in appDescriptions)
                {
                    try
                    {
                        if (applicationDescription.DiscoveryUrls == null || applicationDescription.DiscoveryUrls.Count == 0)
                        {
                            // skip servers without DiscoveryUrl information.
                            continue;
                        }

                        // retrieve available endpoints for each registered server and display their information.
                        Console.WriteLine("\r\nGet available endpoints for server: {0} ...", applicationDescription.ApplicationUri);
                        string serverDiscoveryUrl = applicationDescription.DiscoveryUrls[0];
                        IList<EndpointDescriptionEx> endpoins = await m_application.GetEndpointsAsync(serverDiscoveryUrl).ConfigureAwait(false);

                        Console.WriteLine("Server: {0} returned {1} available endpoints:", serverDiscoveryUrl, endpoins.Count);

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
                        Program.PrintException(string.Format("Server: {0} GetEndpoints", applicationDescription.ApplicationUri), ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DiscoverServers", ex);
            }
        }

        /// <summary>
        /// Displays all registered server applications on the network.
        /// </summary>
        public void DiscoverServersOnNetwork()
        {
            try
            {
                Console.WriteLine("\r\nDiscovering all available servers and their endpoints from local network...");

                // The method will return all the registered server applications from the local network.
                // DiscoverServersOnNetwork service is supported only by LDS-ME installations.
                // If the "discoveryUrl" parameter is null or empty, DiscoverServersOnNetwork() will be invoked on the local machine.
                var serversOnNetwork = m_application.DiscoverServersOnNetwork(null);

                Console.WriteLine("DiscoverServersOnNetwork returned {0} results:", serversOnNetwork.Count);

                foreach (var serverOnNetwork in serversOnNetwork)
                {
                    try
                    {
                        // skip servers without DiscoveryUrl information.
                        if (String.IsNullOrEmpty(serverOnNetwork.DiscoveryUrl))
                        {
                            continue;
                        }

                        // ignore Urls with unsupported transport profiles.
                        if (!serverOnNetwork.DiscoveryUrl.StartsWith(Utils.UriSchemeOpcTcp))
                        {
                            continue;
                        }

                        // retrieve available endpoints for each registered server and display their information.
                        string serverDiscoveryUrl = serverOnNetwork.DiscoveryUrl;
                        Console.WriteLine("\r\nGet available endpoints for : {0} ...", serverDiscoveryUrl);
                        var endpoins = m_application.GetEndpoints(serverDiscoveryUrl);

                        Console.WriteLine("Server: {0} returned {1} available endpoints:", serverOnNetwork.ServerName,
                            endpoins.Count);

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
                        Program.PrintException(string.Format("Server: {0} GetEndpoints", serverOnNetwork.DiscoveryUrl), ex);
                    }                    
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DiscoverServersOnNetwork", ex);
            }           
        }


        /// <summary>
        /// Asynchronously Gets and Displays all registered server applications on the network.
        /// The method calls <see cref="UaApplication.DiscoverServersOnNetworkAsync(string)"/> method.
        /// </summary>
        public async Task DiscoverServersOnNetworkAsync()
        {
            try
            {
                Console.WriteLine("\r\nDiscovering all available servers and their endpoints from local network...");

                // The method will return all the registered server applications from the local network.
                // DiscoverServersOnNetworkAsync service is supported only by LDS-ME installations.
                // If the "discoveryUrl" parameter is null or empty, DiscoverServersOnNetworkAsync() will be invoked on the local machine.
                var serversOnNetwork = await m_application.DiscoverServersOnNetworkAsync(null).ConfigureAwait(false);

                Console.WriteLine("DiscoverServersOnNetwork returned {0} results:", serversOnNetwork.Count);

                foreach (var serverOnNetwork in serversOnNetwork)
                {
                    try
                    {
                        // skip servers without DiscoveryUrl information.
                        if (String.IsNullOrEmpty(serverOnNetwork.DiscoveryUrl))
                        {
                            continue;
                        }

                        // ignore Urls with unsupported transport profiles.
                        if (!serverOnNetwork.DiscoveryUrl.StartsWith(Utils.UriSchemeOpcTcp))
                        {
                            continue;
                        }

                        // retrieve available endpoints for each registered server and display their information.
                        string serverDiscoveryUrl = serverOnNetwork.DiscoveryUrl;
                        Console.WriteLine("\r\nGet available endpoints for : {0} ...", serverDiscoveryUrl);
                        var endpoins = await m_application.GetEndpointsAsync(serverDiscoveryUrl).ConfigureAwait(false);

                        Console.WriteLine("Server: {0} returned {1} available endpoints:", serverOnNetwork.ServerName,
                            endpoins.Count);

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
                        Program.PrintException(string.Format("Server: {0} GetEndpoints", serverOnNetwork.DiscoveryUrl), ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DiscoverServersOnNetwork", ex);
            }
        }
        #endregion
    }
}
