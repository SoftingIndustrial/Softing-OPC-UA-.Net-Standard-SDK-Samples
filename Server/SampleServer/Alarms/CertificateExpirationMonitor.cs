/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Opc.Ua;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an <see cref="CertificateExpirationAlarmState"/> attached.
    /// </summary>
    class CertificateExpirationMonitor : OffNormalAlarmMonitor
    {
        #region Constructors
        /// <summary>
        /// Create new instance of <see cref="CertificateExpirationMonitor"/>
        /// </summary>
        /// <param name="alarmsNodeManager"></param>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        public CertificateExpirationMonitor(
            AlarmsNodeManager alarmsNodeManager, 
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue)
           : base(alarmsNodeManager, context, parent, namespaceIndex, name, alarmName, initialValue)
        {            
            CertificateExpirationAlarmState certificateExpirationAlarmState = m_alarm as CertificateExpirationAlarmState;

            if (certificateExpirationAlarmState != null)
            {
                // Set certificate expiration mandatory fields
                certificateExpirationAlarmState.CertificateType.Value = Variables.CertificateExpirationAlarmType_CertificateType;
                certificateExpirationAlarmState.Certificate.Value = GetCertificate().RawData;
            }
        }
        #endregion

        #region Base Class Overrides

        /// <summary>
        /// Create and return new instance of <see cref="CertificateExpirationAlarmState"/> to be used by the monitor
        /// </summary>
        /// <returns></returns>
        protected override OffNormalAlarmState GetInstanceOfAlarmState()
        {
            return new CertificateExpirationAlarmState(this); 
        }
        #endregion

        #region Private Methods    
        /// <summary>
        /// Get the certificate
        /// </summary>
        /// <returns></returns>
        private X509Certificate2 GetCertificate()
        {
            string certificateFilePath = Path.Combine("Alarms","Files", "opcuser.pfx");
            if (!File.Exists(certificateFilePath))
            {
                Console.WriteLine("The user certificate file is missing ('{0}').", certificateFilePath);
                return null;
            }
            // load the certificate from file
            X509Certificate2 certificate = new X509Certificate2(certificateFilePath,
                           null as string,
                           X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            return certificate;
        }

        #endregion        
    }
}
