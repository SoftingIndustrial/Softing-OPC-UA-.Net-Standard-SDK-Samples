using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Opc.Ua.Toolkit
{
    public class SecurityConfiguration
    {
        #region Fields
        private Opc.Ua.SecurityConfiguration m_securityConfiguration;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityConfiguration"/> class.
        /// </summary>
        /// <param name="applicationConfiguration">The application configuration.</param>
        public SecurityConfiguration(Opc.Ua.ApplicationConfiguration applicationConfiguration)
        {
            m_securityConfiguration = applicationConfiguration.SecurityConfiguration;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether [auto accept untrusted certificates].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [auto accept untrusted certificates]; otherwise, <c>false</c>.
        /// </value>
        public bool AutoAcceptUntrustedCertificates
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the application's own certificate store, directory path.
        /// </summary>
        /// <value>
        /// The application certificate store.
        /// </value>
        public string ApplicationCertificateStore
        {
            get
            {
                return m_securityConfiguration.ApplicationCertificate.StorePath;
            }
            set
            {
                if (m_securityConfiguration.ApplicationCertificate == null)
                {
                    m_securityConfiguration.ApplicationCertificate = new Opc.Ua.CertificateIdentifier();
                }

                m_securityConfiguration.ApplicationCertificate.StorePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the application certificate subject, the entity beeing secured.
        /// </summary>
        /// <value>
        /// The application certificate subject.
        /// </value>
        public string ApplicationCertificateSubject
        {
            get
            {
                return m_securityConfiguration.ApplicationCertificate.SubjectName;
            }
            set
            {
                if (m_securityConfiguration.ApplicationCertificate == null)
                {
                    m_securityConfiguration.ApplicationCertificate = new Opc.Ua.CertificateIdentifier();
                }

                m_securityConfiguration.ApplicationCertificate.SubjectName = value;
            }
        }

        /// <summary>
        /// Gets the application certificate.
        /// </summary>
        public X509Certificate2 ApplicationCertificate
        {
            get
            {
                return m_securityConfiguration.ApplicationCertificate.Certificate;
            }
        }

        /// <summary>
        /// Gets or sets the trusted certificate store, directory path.
        /// </summary>
        /// <value>
        /// The trusted certificate store.
        /// </value>
        public string TrustedCertificateStore
        {
            get
            {
                return m_securityConfiguration.TrustedPeerCertificates.StorePath;
            }

            set
            {
                m_securityConfiguration.TrustedPeerCertificates.StorePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the rejected certificate store, directory path.
        /// </summary>
        /// <value>
        /// The rejected certificate store.
        /// </value>
        public string RejectedCertificateStore
        {
            get
            {
                return m_securityConfiguration.RejectedCertificateStore.StorePath;
            }

            set
            {
                if (m_securityConfiguration.RejectedCertificateStore == null)
                {
                    m_securityConfiguration.RejectedCertificateStore = new Opc.Ua.CertificateStoreIdentifier();
                }

                m_securityConfiguration.RejectedCertificateStore.StorePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the trusted issuer certificate store, directory path.
        /// </summary>
        /// <value>
        /// The trusted issuer certificate store.
        /// </value>
        public string TrustedIssuerCertificateStore
        {
            get
            {
                return m_securityConfiguration.TrustedIssuerCertificates.StorePath;
            }

            set
            {
                m_securityConfiguration.TrustedIssuerCertificates.StorePath = value;
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Imports the application's own certificate to the certificate store.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        public void ImportOwnCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new System.ArgumentNullException("certificate");
            }

            if (m_securityConfiguration.ApplicationCertificate != null)
            {
                try
                {
                    Opc.Ua.ICertificateStore certificateStore = m_securityConfiguration.ApplicationCertificate.OpenStore();
                    if (certificateStore != null)
                    {
                        var certInStore = certificateStore.FindByThumbprint(certificate.Thumbprint);
                        if (certInStore != null)
                        {
                            if (!certInStore.HasPrivateKey)
                            {
                                certificateStore.Delete(certInStore.Thumbprint);
                                certificateStore.Add(certificate);
                            }
                        }
                        else
                        {
                            certificateStore.Add(certificate);
                        }
                    }

                    m_securityConfiguration.ApplicationCertificate.Certificate = certificate;

                    string logMessage = string.Format("Certificate with SubjectName \"{0}\" set as the Application Instance certificate", certificate.Subject);
                    Application.Log(TraceLevels.Information, TraceMasks.ClientAPI, "SecurityConfiguration.ImportOwnCertificate", logMessage, null);
                }
                catch (Exception ex)
                {
                    Application.Log(TraceLevels.Warning, TraceMasks.ClientAPI, "SecurityConfiguration.ImportOwnCertificate", ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// When connecting to a server, this method adds that servers certificate to the application's trusted store.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        public void AddCertificateToTrustStore(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new System.ArgumentNullException("certificate");
            }

            if (m_securityConfiguration.TrustedPeerCertificates != null)
            {
                try
                {
                    Opc.Ua.ICertificateStore certificateStore = m_securityConfiguration.TrustedPeerCertificates.OpenStore();
                    if (certificateStore != null)
                    {
                        certificateStore.Add(certificate);
                    }

                    string logMessage = string.Format("Certificate with SubjectName \"{0}\" added to Trusted Store", certificate.Subject);
                    Application.Log(TraceLevels.Information, TraceMasks.ClientAPI, "SecurityConfiguration.AddCertificateToTrustStore", logMessage, null);
                }
                catch (Exception ex)
                {
                    Application.Log(TraceLevels.Warning, TraceMasks.ClientAPI, "SecurityConfiguration.AddCertificateToTrustStore", ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// This method removes a certificate from the application's trusted store.
        /// </summary>
        /// <param name="thumbprint">The certificate's thumbprint.</param>
        /// <returns>True if certificate is found</returns>
        public bool RemoveCertificateFromTrustStore(string thumbprint)
        {
            if (m_securityConfiguration.TrustedPeerCertificates != null)
            {
                try
                {
                    bool found = false;
                    Opc.Ua.ICertificateStore certificateStore = m_securityConfiguration.TrustedPeerCertificates.OpenStore();

                    if (certificateStore != null)
                    {
                        found = certificateStore.Delete(thumbprint);
                    }

                    string logMessage = string.Format("Certificate with thumbprint \"{0}\" removed from Trusted Store", thumbprint);
                    Application.Log(TraceLevels.Information, TraceMasks.ClientAPI, "SecurityConfiguration.RemoveCertificateFromTrustStore", logMessage, null);

                    Application.Configuration.InternalApplicationConfiguration.CertificateValidator.Update(m_securityConfiguration);

                    return found;
                }
                catch (Exception ex)
                {
                    Application.Log(TraceLevels.Warning, TraceMasks.ClientAPI, "SecurityConfiguration.RemoveCertificateFromTrustStore", ex.Message, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// This method adds a certificate to the applications rejected store. This means that the application will not trust that certificate and its subject.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        public void AddCertificateToRejectedStore(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new System.ArgumentNullException("certificate");
            }

            if (m_securityConfiguration.RejectedCertificateStore != null)
            {
                try
                {
                    Opc.Ua.ICertificateStore certificateStore = m_securityConfiguration.RejectedCertificateStore.OpenStore();
                    if (certificateStore != null)
                    {
                        certificateStore.Add(certificate);
                    }

                    string logMessage = string.Format("Certificate with SubjectName \"{0}\" added to Rejected Store", certificate.Subject);
                    Application.Log(TraceLevels.Information, TraceMasks.ClientAPI, "SecurityConfiguration.AddCertificateToRejectedStore", logMessage, null);
                }
                catch (Exception ex)
                {
                    Application.Log(TraceLevels.Warning, TraceMasks.ClientAPI, "SecurityConfiguration.AddCertificateToRejectedStore", ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// This method removes a certificate from the application's rejected store.
        /// </summary>
        /// <param name="thumbprint">The certificate's thumbprint.</param>
        /// <returns>True if certificate is found</returns>
        public bool RemoveCertificateFromRejectedStore(string thumbprint)
        {
            if (m_securityConfiguration.RejectedCertificateStore != null)
            {
                try
                {
                    Opc.Ua.ICertificateStore certificateStore = m_securityConfiguration.RejectedCertificateStore.OpenStore();
                    bool found = false;

                    if (certificateStore != null)
                    {
                        found = certificateStore.Delete(thumbprint);
                    }

                    string logMessage = string.Format("Certificate with thumbprint \"{0}\" removed from Rejected Store", thumbprint);
                    Application.Log(TraceLevels.Information, TraceMasks.ClientAPI, "SecurityConfiguration.RemoveCertificateFromRejectedStore", logMessage, null);

                    return found;
                }
                catch (Exception ex)
                {
                    Application.Log(TraceLevels.Warning, TraceMasks.ClientAPI, "SecurityConfiguration.RemoveCertificateFromRejectedStore", ex.Message, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Returns all the certificates that are currently in the application's trust store.
        /// </summary>
        /// <returns>A collection of certificates</returns>
        public X509Certificate2Collection EnumerateCertificatesFromTrustStore()
        {
            if (m_securityConfiguration.TrustedPeerCertificates != null)
            {
                try
                {
                    Opc.Ua.ICertificateStore certificateStore = m_securityConfiguration.TrustedPeerCertificates.OpenStore();
                    if (certificateStore != null)
                    {
                        return certificateStore.Enumerate();
                    }
                }
                catch (Exception ex)
                {
                    Application.Log(TraceLevels.Warning, TraceMasks.ClientAPI, "SecurityConfiguration.EnumerateCertificatesFromTrustStore", ex.Message, ex);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns all the certificates that are currently in the application's rejected store.
        /// </summary>
        /// <returns>A collection of certificates</returns>
        public X509Certificate2Collection EnumerateCertificatesFromRejectedStore()
        {
            if (m_securityConfiguration.TrustedPeerCertificates != null)
            {
                try
                {
                    Opc.Ua.ICertificateStore certificateStore = m_securityConfiguration.RejectedCertificateStore.OpenStore();
                    if (certificateStore != null)
                    {
                        return certificateStore.Enumerate();
                    }
                }
                catch (Exception ex)
                {
                    Application.Log(TraceLevels.Warning, TraceMasks.ClientAPI, "SecurityConfiguration.EnumerateCertificatesFromRejectedStore", ex.Message, ex);
                }
            }

            return null;
        }
        #endregion
    }
}
