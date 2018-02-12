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

namespace SampleServer
{
    /// <summary>
    /// Stores custom configuration parameters
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaConfig)]
    public class SampleServerConfiguration
    {
        #region Constructors
        /// <summary>
        /// The default constructor
        /// </summary>
        public SampleServerConfiguration()
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
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// The delay in seconds to allow a graceful shutdown
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