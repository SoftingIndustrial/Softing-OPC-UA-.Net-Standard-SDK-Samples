using Opc.Ua.Toolkit.Trace;
using System;
using System.Collections.Generic;

namespace Opc.Ua.Toolkit
{

    /// <summary>
    /// A class that provides discovery functionality.
    /// </summary>
    /// <include file='Doc\DiscoveryService.xml' path='class[@name="DiscoveryService"]/*'/>   
    public class DiscoveryService
    {
        #region Fields
        private const string DefaultDiscoveryUrl = "{0}://{1}:4840";
        private const string OpcTcpProtocol = "opc.tcp";
        private const string HttpProtocol = "http";
        private const string HttpSecureProtocol = "https";

        private EndpointConfiguration m_endpointConfiguration;
        #endregion        

        /// <summary>
        /// Create new instance of DiscoveryService with specific ApplicationConfiguration
        /// </summary>
        /// <param name="applicationConfiguration"></param>
        public DiscoveryService(ExtendedApplicationConfiguration applicationConfiguration)
        {
            //initialize endpoint configuration for this discovery service
            m_endpointConfiguration = EndpointConfiguration.Create(applicationConfiguration);
            m_endpointConfiguration.OperationTimeout = applicationConfiguration.DiscoveryOperationTimeout;
        }

        #region Public Methods
        /// <summary>
        /// Gets the endpoints of the specified server. This service returns the Endpoints supported by a Server and all of the configuration information required to establish a SecureChannel and a Session. 
        /// This service does not require any message security.
        /// </summary>
        /// <param name="serverUrl">The server URL.</param>
        /// <returns>A list of Endpoint descriptions</returns>
        public IList<EndpointDescription> GetEndpoints(string serverUrl)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                throw new ArgumentNullException("serverUrl");
            }
            return Discover(null, serverUrl);
        }

        /// <summary>
        /// This method returns the Servers known to the specified Server or Discovery Server. This method uses both services FindServer and GetEndpoints. 
        /// For each server found, its endpoints is requested. A list of all endpoints for all servers is returned.
        /// </summary>
        /// <param name="discoverUrl">The discover URL.</param>
        /// <returns>A list of Endpoint descriptions</returns>
        public IList<EndpointDescription> DiscoverServers(string discoverUrl)
        {
            if (string.IsNullOrEmpty(discoverUrl))
            {
                throw new ArgumentNullException("discoverUrl");
            }
            return Discover(discoverUrl, null);
        }

        /// <summary>
        /// Gets the default discovery url for the specified host name and transport protocol
        /// </summary>
        /// <param name="hostName">The machine host name.</param>
        /// <param name="transportProtocol">The transport protocol.</param>
        /// <returns>The discovery URL</returns>
        public static string GetDefaultDiscoveryUrl(string hostName, TransportProtocols transportProtocol)
        {
            string discoveryUrl = string.Empty;
            string protocol = string.Empty;

            switch (transportProtocol)
            {
                case TransportProtocols.OpcTcp:
                    protocol = OpcTcpProtocol;
                    break;
                case TransportProtocols.Http:
                    protocol = HttpProtocol;
                    break;
                case TransportProtocols.Https:
                    protocol = HttpSecureProtocol;
                    break;
                default:
                    protocol = OpcTcpProtocol;
                    break;
            }

            discoveryUrl = string.Format(DefaultDiscoveryUrl, protocol, hostName);
            return discoveryUrl;
        }

        #endregion       

        #region Private Methods 

        /// <summary>
        /// Discovers the specified host name.
        /// </summary>
        /// <param name="discoveryUrl">The discovery url.</param>
        /// <param name="serverUrl">The server URL.</param>
        /// <returns>A list of Endpoint descriptions</returns>
        /// <include file='Doc\Application.xml' path='class[@name="Application"]/method[@name="Discover"]/*'/>
        private IList<EndpointDescription> Discover(string discoveryUrl, string serverUrl)
        {
            // parameters pre-validation
            if ((string.IsNullOrEmpty(discoveryUrl) && string.IsNullOrEmpty(serverUrl))
                || (!string.IsNullOrEmpty(discoveryUrl) && !string.IsNullOrEmpty(serverUrl)))
            {
                throw new ArgumentException("The specified arguments are invalid");
            }

            List<EndpointDescription> results = new List<EndpointDescription>();
            List<ApplicationDescription> applicationDescriptions = new List<ApplicationDescription>();

            if (!string.IsNullOrEmpty(discoveryUrl))
            {
                Uri hostNameUri = new Uri(discoveryUrl);            

                using (DiscoveryClient client = DiscoveryClient.Create(hostNameUri, m_endpointConfiguration))
                {
                    TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Application.Discover", "Finding available servers on {0}.", hostNameUri);

                    client.OperationTimeout = m_endpointConfiguration.OperationTimeout;

                    try
                    {
                        ApplicationDescriptionCollection applications = client.FindServers(null);

                        foreach (ApplicationDescription application in applications)
                        {
                            if (ValidateServer(application))
                            {
                                applicationDescriptions.Add(application);
                            }
                        }
                        
                        TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Application.Discover", "FindServers() returned {0} server(s) from {1}.", applicationDescriptions.Count, hostNameUri);
                    }                   
                    catch (Exception ex)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Application.Discover", ex);

                        throw new BaseException("Discover error", ex);
                    }
                }
            }
            else
            {
                ApplicationDescription application = new ApplicationDescription();
                application.DiscoveryUrls.Add(serverUrl);
                applicationDescriptions.Add(application);
            }

            foreach (ApplicationDescription application in applicationDescriptions)
            {
                string serverDiscoveryUrl = application.DiscoveryUrls[0];

                // needs to add the '/discovery' back onto non-UA TCP URLs.
                if (!serverDiscoveryUrl.StartsWith(Utils.UriSchemeOpcTcp))
                {
                    if (!serverDiscoveryUrl.EndsWith("/discovery"))
                    {
                        serverDiscoveryUrl += "/discovery";
                    }
                }

                try
                {
                    // parse the selected URL.
                    Uri uri = new Uri(serverDiscoveryUrl);

                    // Connect to the server's discovery endpoint and find the available configuration.
                    using (DiscoveryClient client = DiscoveryClient.Create(uri, m_endpointConfiguration))
                    {
                        TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Application.Discover", "Finding available endpoints on {0}.", serverDiscoveryUrl);

                        client.OperationTimeout = m_endpointConfiguration.OperationTimeout;

                        EndpointDescriptionCollection endpoints = client.GetEndpoints(null);
                        
                        TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Application.Discover", 
                            "GetEndpoints() returned {0} endpoint(s) from {1}", endpoints.Count, serverDiscoveryUrl);

                        foreach (EndpointDescription ed in endpoints)
                        {
                            results.Add(ed);

                            try
                            {
                                if (!string.IsNullOrEmpty(ed.EndpointUrl))
                                {
                                    Uri serverUri = new Uri(ed.EndpointUrl);

                                    if (string.Compare(serverUri.Host, uri.Host, true) != 0)
                                    {
                                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Application.Discover", 
                                            "Returned EndpointUrl is of a different hostname \"{0}\" then the one requested \"{1}\"", serverUri.Host, uri.Host);
                                    }
                                }
                            }
                            catch
                            {
                                //uri creation can throw exception but the same exception is thrown when create EndpointDescription see GetDiscoveryUrl method there
                            }
                        }
                    }
                }               
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Application.Discover", ex);

                    if (string.IsNullOrEmpty(discoveryUrl))
                    {
                        throw new BaseException("Discover error", ex);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Validates the Server application Description (description returned at FindServers) and logs incorrect values for its properties.
        /// </summary>
        /// <param name="application">The application description that will be analysed.</param>
        /// <returns>If the server description is valid and it can be further parsed by Discovery method.</returns>
        private static bool ValidateServer(ApplicationDescription application)
        {
            if (application.ApplicationType != ApplicationType.Server && application.ApplicationType != ApplicationType.ClientAndServer)
            {
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Application.ValidateServer", "FindServers returned a description with ApplicationType = '{0}'", application.ApplicationType);
                return false;
            }
            if (application.DiscoveryUrls == null || application.DiscoveryUrls.Count == 0)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Application.ValidateServer", "FindServers returned a description with DiscoveryUrls empty.");
                return false;
            }
            if (string.IsNullOrEmpty(application.ProductUri))
            {
                TraceService.Log(TraceMasks.Information, TraceSources.ClientAPI, "Application.ValidateServer", "FindServers returned a description with ProductUri null or empty.");
            }
            if (application.ApplicationName == null || string.IsNullOrEmpty(application.ApplicationName.Text))
            {
                TraceService.Log(TraceMasks.Information, TraceSources.ClientAPI, "Application.ValidateServer", "FindServers returned a description with ApplicationName null or empty.");
            }
            return true;
        }

        #endregion
    }
    
}
