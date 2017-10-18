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
using Opc.Ua;
using Softing.Opc.Ua;

namespace SampleClient
{

    /// <summary>
    /// Sample Cleint class that provides discovery functionality
    /// </summary>
    public class DiscoveryClient
    {
        private const string ServerDiscoveryUrl = "opc.tcp://localhost:4840";
        private readonly DiscoveryService m_discoveryService;

        /// <summary>
        /// Create new instance of DiscoveryClient
        /// </summary>
        public DiscoveryClient(ApplicationConfigurationEx applicationConfiguration)
        {
            m_discoveryService = new DiscoveryService(applicationConfiguration);
        }


        /// <summary>
        /// Displays all registered server applications and their available endpoints.
        /// </summary>
        public void DiscoverServers()
        {
            try
            {
                Console.WriteLine("\nDiscovering all available endpoints...");

                // The method will return all the available servers from the specified machine as ApplicationDescription objects
                // by invoking FindServers to retrieve the available server applications and GetEndpoints for each running server.
                var servers = m_discoveryService.DiscoverServers(ServerDiscoveryUrl);

                Console.WriteLine("DiscoverServers returned {0} results:\n", servers.Count);

                foreach (ApplicationDescription serverApplicationDescription in servers)
                {
                    Console.WriteLine("\nServer: {0}:\n", serverApplicationDescription.DiscoveryUrls[0]);
                  
                    var endpoins = m_discoveryService.GetEndpoints(serverApplicationDescription);
                    foreach (EndpointDescriptionEx endpointDescription in endpoins)
                    {
                        //Console.WriteLine(String.Format("       {0} - {1} - {2}", endpointDescription.EndpointUrl, endpointDescription.SecurityMode,
                        //    endpointDescription.SecurityPolicy));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("DiscoverServers Error : {0}.", e.Message));
            }

        }
    }
}
