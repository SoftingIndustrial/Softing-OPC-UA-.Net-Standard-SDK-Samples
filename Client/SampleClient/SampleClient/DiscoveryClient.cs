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
using Opc.Ua;
using Softing.Opc.Ua;

namespace SampleClient
{

    /// <summary>
    /// Sample Cleint class that provides discovery functionality
    /// </summary>
    public class DiscoveryClient
    {
        private readonly ApplicationConfigurationEx m_applicationConfiguration;
        

        /// <summary>
        /// Create new instance of DiscoveryClient
        /// </summary>
        public DiscoveryClient(ApplicationConfigurationEx applicationConfiguration)
        {
            //keep reference to ApplicationConfigurationEx object
            m_applicationConfiguration = applicationConfiguration;
        }


        /// <summary>
        /// Displays all registered server applications and their available endpoints.
        /// </summary>
        /// <param name="discoveryUrl">url of discovery</param>
        public void DiscoverServers(string discoveryUrl)
        {
            try
            {
                Console.WriteLine(String.Format("\nDiscovering all available servers and thier endpoints from {0}...", discoveryUrl));

                //initialize the discovery service
                DiscoveryService discoveryService = new DiscoveryService(m_applicationConfiguration);

                // The method will return all the available server applications from the specified machine
                var servers = discoveryService.DiscoverServers(discoveryUrl);

                Console.WriteLine("DiscoverServers returned {0} results:\n", servers.Count);

                foreach (var serverApplicationDescription in servers)
                {
                    // retrieve endpoints for each running server and display their information
                    var endpoins = discoveryService.GetEndpoints(serverApplicationDescription);

                    Console.WriteLine("\nServer: {0} has {1} endpoints:\n", serverApplicationDescription.ApplicationUri, endpoins.Count);

                    foreach (var endpointDescription in endpoins)
                    {
                        Console.WriteLine(String.Format("       {0} - {1} - {2}", 
                            endpointDescription.EndpointUrl, 
                            endpointDescription.SecurityMode,
                            endpointDescription.SecurityPolicy));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("DiscoverServers Error : {0}.", e.Message));
            }
        }

        /// <summary>
        /// Displays all the available endpoints by invoking GetEndpoints at the specified serverUrl.
        /// </summary>
        /// <param name="serverUrl"></param>
        public void GetEndpoints(string serverUrl)
        {
            try
            {
                Console.WriteLine(String.Format("\nDiscovering available endpoints from {0}...", serverUrl));

                //initialize the discovery service
                DiscoveryService discoveryService = new DiscoveryService(m_applicationConfiguration);

                // This method will return all the server endpoints by invoking the GetEndpoints service.
                var endpoints = discoveryService.GetEndpoints(serverUrl);

                Console.WriteLine(String.Format("GetEndpoints returned {0} endpoints:\n", endpoints.Count));

                foreach (var endpointDescription in endpoints)
                {
                    Console.WriteLine(String.Format(" {0} - {1} - {2}",
                        endpointDescription.EndpointUrl,
                        endpointDescription.SecurityMode,
                        endpointDescription.SecurityPolicy));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("GetEndpoints Error : {0}.", e.Message));
            }
        }
    }
}
