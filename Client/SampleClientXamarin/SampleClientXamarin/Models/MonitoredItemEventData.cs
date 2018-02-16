/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

 
using SampleClientXamarin.Helpers;

namespace SampleClientXamarin.Models
{
    /// <summary>
    /// Model class for monitored item event data
    /// </summary>
    class MonitoredItemEventData : ObservableObject
    {
        #region Private Fields
        private string m_monitoredItemName;
        private uint m_sequenceNumber;
        private string m_value;
        private string m_statusCode;
        private string m_serverTimeStamp;
        private string m_sourceTimestamp;
        #endregion

        #region Public Properties
        /// <summary>
        /// Monitored item name
        /// </summary>
        public string MonitoredItemName
        {
            get { return m_monitoredItemName; }
            set { SetProperty(ref m_monitoredItemName, value); }
        }

        /// <summary>
        /// Event sequence number
        /// </summary>
        public uint SequenceNumber
        {
            get { return m_sequenceNumber; }
            set { SetProperty(ref m_sequenceNumber, value); }
        }
        /// <summary>
        /// Event sequence number
        /// </summary>
        public string Value
        {
            get { return m_value; }
            set { SetProperty(ref m_value, value); }
        }

        /// <summary>
        /// Event status code
        /// </summary>
        public string StatusCode
        {
            get { return m_statusCode; }
            set { SetProperty(ref m_statusCode, value); }
        }


        /// <summary>
        /// Event ServerTimeStamp
        /// </summary>
        public string ServerTimeStamp
        {
            get { return m_serverTimeStamp; }
            set { SetProperty(ref m_serverTimeStamp, value); }
        }

        /// <summary>
        /// Event ServerTimeStamp
        /// </summary>
        public string SourceTimestamp
        {
            get { return m_sourceTimestamp; }
            set { SetProperty(ref m_sourceTimestamp, value); }
        }

        #endregion
    }
}
