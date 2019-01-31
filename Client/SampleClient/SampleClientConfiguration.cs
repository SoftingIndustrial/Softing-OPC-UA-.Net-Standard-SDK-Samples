/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System.Runtime.Serialization;
using Opc.Ua;

namespace SampleClient
{
    /// <summary>
    /// Stores the configuration the Sample Client
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaConfig)]
    public class SampleClientConfiguration
    {
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
            get { return m_ServerUrl; }
            set { m_ServerUrl = value; }
        }
        
        /// <summary>
        /// Gets or sets the server endpoint URL where the client will connect on https protocol.
        /// </summary>
        [DataMember(IsRequired = false, Order = 2)]
        public string ServerUrlHttps
        {
            get { return m_ServerUrlHttps; }
            set { m_ServerUrlHttps = value; }
        }
        #endregion

        #region Private Members
        private string m_ServerUrl= "opc.tcp://localhost:61510/SampleServer";
        private string m_ServerUrlHttps= "https://localhost:61511/SampleServer";
        #endregion
    }
}