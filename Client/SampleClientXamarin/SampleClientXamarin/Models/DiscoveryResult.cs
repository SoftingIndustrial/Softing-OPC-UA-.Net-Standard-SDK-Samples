using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;
using Softing.Opc.Ua;

namespace SampleClientXamarin.Models
{
    /// <summary>
    /// Data holder for discovery result
    /// </summary>
    class DiscoveryResult
    {
        /// <summary>
        /// Create new instance of DiscoveryResult
        /// </summary>
        /// <param name="applicationDescription"></param>
        /// <param name="endpoints"></param>
        public DiscoveryResult(ApplicationDescription applicationDescription, IList<EndpointDescriptionEx> endpoints)
        {
            ApplicationDescription = applicationDescription;
            Endpoints = endpoints;
        }

        /// <summary>
        /// Get ApplicationDescription
        /// </summary>
        public ApplicationDescription ApplicationDescription { get; private set; }

        /// <summary>
        /// Get list of endpoints
        /// </summary>
        public IList<EndpointDescriptionEx> Endpoints { get; }
    }
}
