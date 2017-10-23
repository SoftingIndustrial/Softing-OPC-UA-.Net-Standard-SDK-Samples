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
        private readonly ApplicationConfigurationEx m_applicationConfiguration; 
        #endregion
        
        #region Constructor
        /// <summary>
        /// Create new instance of DiscoveryClient
        /// </summary>
        public DiscoveryClient(ApplicationConfigurationEx applicationConfiguration)
        {
            //keep reference to ApplicationConfigurationEx object
            m_applicationConfiguration = applicationConfiguration;
        }

        #endregion

        #region Discover & GetEndpoints Methods
        /// <summary>
        /// Displays all registered server applications and their available endpoints.
        /// </summary>
        /// <param name="discoveryUrl">url of discovery</param>
        public void DiscoverServers(string discoveryUrl)
        {
            try
            {
                Console.WriteLine("Discovering all available servers and thier endpoints from {0}...\r\n", discoveryUrl);

                //initialize the discovery service
                DiscoveryService discoveryService = new DiscoveryService(m_applicationConfiguration);

                // The method will return all the available server applications from the specified machine
                var servers = discoveryService.DiscoverServers(discoveryUrl);

                Console.WriteLine("DiscoverServers returned {0} results:", servers.Count);

                foreach (var serverApplicationDescription in servers)
                {
                    // retrieve endpoints for each running server and display their information
                    var endpoins = discoveryService.GetEndpoints(serverApplicationDescription);

                    Console.WriteLine("Server: {0} has {1} endpoints:", serverApplicationDescription.ApplicationUri, endpoins.Count);

                    foreach (var endpointDescription in endpoins)
                    {
                        Console.WriteLine("       {0} - {1} - {2}",
                            endpointDescription.EndpointUrl,
                            endpointDescription.SecurityMode,
                            endpointDescription.SecurityPolicy);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("DiscoverServers Error : {0}.", e.Message);
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
                Console.WriteLine("Discovering available endpoints from {0}...\r\n", serverUrl);

                //initialize the discovery service
                DiscoveryService discoveryService = new DiscoveryService(m_applicationConfiguration);

                // This method will return all the server endpoints by invoking the GetEndpoints service.
                var endpoints = discoveryService.GetEndpoints(serverUrl);

                Console.WriteLine("GetEndpoints returned {0} endpoints:", endpoints.Count);

                foreach (var endpointDescription in endpoints)
                {
                    Console.WriteLine(" {0} - {1} - {2}",
                        endpointDescription.EndpointUrl,
                        endpointDescription.SecurityMode,
                        endpointDescription.SecurityPolicy);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("GetEndpoints Error : {0}.", e.Message);
            }
        } 
        #endregion
    }
}
