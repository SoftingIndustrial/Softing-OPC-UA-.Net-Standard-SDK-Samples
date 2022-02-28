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
    class CertificateExpirationMonitor : BaseAlarmMonitor
    {
        #region Private Members

        private CertificateExpirationAlarmState m_alarm;

        #endregion

        #region Constructors
        public CertificateExpirationMonitor(
            AlarmsNodeManager alarmsNodeManager, 
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue)
           : base(context, parent, namespaceIndex, name, initialValue, alarmsNodeManager)
        {
            BaseDataVariableState normalValueVariable = alarmsNodeManager.CreateVariable<double>(this, "NormalValueVariable");
            normalValueVariable.Value = initialValue;

            // Attach the alarm monitor.
            InitializeAlarmMonitor(
                context,
                parent,
                namespaceIndex,
                alarmName, 
                normalValueVariable);

            m_alarm.OnAcknowledge += AlarmMonitor_OnAcknowledge;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the alarm monitor 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="alarmName"></param>
        /// <param name="normalValueVariable"></param>
        private void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName,
            BaseDataVariableState normalValueVariable)
        {
            // Create the alarm object
            m_alarm = new CertificateExpirationAlarmState(this);

            InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName, m_alarm);

            // Set input node
            m_alarm.InputNode.Value = NodeId;

            // Setup the NormalState
            AddChild(normalValueVariable);
            m_alarm.NormalState.Value = normalValueVariable.NodeId;

            // set acknowledge state
            m_alarm.SetAcknowledgedState(context, false);
            m_alarm.AckedState.Value = new LocalizedText("en-US", ConditionStateNames.Unacknowledged);

            // Set state values
            m_alarm.SetSuppressedState(context, false);
            m_alarm.SetActiveState(context, false);

            // Set certificate expiration mandatory fields
            
            m_alarm.CertificateType.Value = Variables.CertificateExpirationAlarmType_CertificateType;
            m_alarm.Certificate.Value = GetCertificate().RawData;

            // optional
            //m_alarm.ExpirationLimit.Value = 120000; // 2 seconds

            // Disable this property 
            m_alarm.LatchedState = null;
        }

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

        #region Prrotected Methods
        protected override void ProcessVariableChanged(ISystemContext context, object value)
        {
            BaseVariableState normalValVar = (BaseVariableState)AlarmsNodeManager.FindNodeInAddressSpace(m_alarm.NormalState.Value);
            OffNormalAlarmMonitor.ProcessVariableChanged(context, value, m_alarm, normalValVar.Value);
        }

        #endregion
    }
}
