/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System.Runtime.Serialization;
using Opc.Ua;

namespace SampleClient
{
    /// <summary>
    /// Stores the configuration of the Sample Client
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaConfig)]
    public class SampleClientConfiguration
    {
        #region Private Members
        private string m_serverUrl = "opc.tcp://localhost:61510/SampleServer";
        private string m_serverUrlHttps = "https://localhost:61511/SampleServer";
        private string m_reverseConnectUrl = "opc.tcp://localhost:65300";
        private string m_reverseConnectServerApplicationUri = "urn:localhost:Softing:UANETStandardToolkit:SampleServer";
        private CertificateIdentifier m_reverseConnectServerCertificateIdentifier;
        #endregion

        #region Constructors
        /// <summary>
        /// The default constructor
        /// </summary>
        public SampleClientConfiguration()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the object during de-serialization
        /// </summary>
        [OnDeserializing()]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values
        /// </summary>
        private void Initialize()
        {
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the server endpoint URL where the client will connect on opc.tcp protocol.
        /// </summary>
        [DataMember(IsRequired = false, Order = 1)]
        public string ServerUrl
        {
            get { return m_serverUrl; }
            set { m_serverUrl = value; }
        }
        
        /// <summary>
        /// Gets or sets the server endpoint URL where the client will connect on https protocol.
        /// </summary>
        [DataMember(IsRequired = false, Order = 2)]
        public string ServerUrlHttps
        {
            get { return m_serverUrlHttps; }
            set { m_serverUrlHttps = value; }
        }

        /// <summary>
        /// Gets or sets the reverse connect endpoint URL where the client will wait for reverse connect messages.
        /// </summary>
        [DataMember(IsRequired = false, Order = 3)]
        public string ReverseConnectUrl
        {
            get { return m_reverseConnectUrl; }
            set { m_reverseConnectUrl = value; }
        }

        /// <summary>
        /// Gets or sets the server application URI that will be allowed to reverse connect to this client.
        /// </summary>
        [DataMember(IsRequired = false, Order = 4)]
        public string ReverseConnectServerApplicationUri
        {
            get { return m_reverseConnectServerApplicationUri; }
            set
            {
                m_reverseConnectServerApplicationUri = Utils.ReplaceLocalhost(value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CertificateIdentifier"/> for the server that will be used to create reverse connect sessions.
        /// </summary>
        [DataMember(IsRequired = false, Order = 5)]
        public CertificateIdentifier ReverseConnectServerCertificateIdentifier
        {
            get { return m_reverseConnectServerCertificateIdentifier; }
            set
            {
                m_reverseConnectServerCertificateIdentifier = value;
            }
        }


        #endregion
    }
}
