/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System.Runtime.Serialization;

namespace SampleServer.ReferenceServer
{
    /// <summary>
    /// Stores the configuration the data access node manager.
    /// </summary>
    [DataContract(Namespace = Namespaces.ReferenceApplications)]
    public class ReferenceServerConfiguration
    {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public ReferenceServerConfiguration()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the object during deserialization.
        /// </summary>
        [OnDeserializing()]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// The delay in seconds to allow a graceful shutdown.
        /// </summary>
        [DataMember(Order = 1)]
        public uint ShutdownDelay
        {
            get { return m_shutdownDelay; }
            set { m_shutdownDelay = value; }
        }
        #endregion

        #region Private Members
        private uint m_shutdownDelay;
        #endregion
    }
}