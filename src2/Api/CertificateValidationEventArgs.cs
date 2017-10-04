using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// The event arguments provided when a certificate validation error occurs.
    /// </summary>
    public class CertificateValidationEventArgs : EventArgs
    {
        #region Fields
        private Opc.Ua.CertificateValidationEventArgs m_eventArgs;
        #endregion

        #region Constructor
        internal CertificateValidationEventArgs(Opc.Ua.CertificateValidationEventArgs eventArgs)
        {
            m_eventArgs = eventArgs;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the certificate.
        /// </summary>
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate
        {
            get { return m_eventArgs.Certificate; }
        }

        /// <summary>
        /// Gets or sets the validation option, can be Accepted, Rejected or Accepted temporarily.
        /// </summary>
        /// <value>
        /// The validation option.
        /// </value>
        public CertificateValidationOption ValidationOption
        {
            get;
            set;
        }
        #endregion
    }
}
