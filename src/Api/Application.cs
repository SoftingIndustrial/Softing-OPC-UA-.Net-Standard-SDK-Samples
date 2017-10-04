using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua.Toolkit.Client;
using Opc.Ua.Toolkit.Trace;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ToolkitSession = Opc.Ua.Toolkit.Client.Session;

namespace Opc.Ua.Toolkit
{

    /// <summary>
    /// A class that runs a UA application.
    /// </summary>
    /// <include file='Doc\Application.xml' path='class[@name="Application"]/*'/>    
    public class Application
    {
        #region Fields

        private ExtendedApplicationConfiguration m_Configuration;
        private List<ToolkitSession> m_sessions;
        private ReadOnlyCollection<ToolkitSession> m_readOnlySessions;
        private ApplicationInstance m_applicationInstance;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes static members of the <see cref="Application"/> class.
        /// </summary>
        /// <include file='Doc\Application.xml' path='class[@name="Application"]/constructor[@name="Application"]/*'/>
        private Application()
        {

            m_sessions = new List<ToolkitSession>();
            m_readOnlySessions = new ReadOnlyCollection<ToolkitSession>(m_sessions);
        }

        #endregion

        #region Properties       

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <include file='Doc\Application.xml' path='class[@name="Application"]/property[@name="Configuration"]/*'/>
        public ExtendedApplicationConfiguration Configuration
        {
            get
            {
                return m_Configuration;
            }
        }

        /// <summary>
        /// Gets the sessions created by this application. This is a readOnly collection.
        /// If a session is disposed it is removed from this collection.
        /// If a new session is created, it is added to this collection.
        /// </summary>
        /// <value>
        /// Readonly collection that contains the sessions of the application.
        /// </value>
        public ReadOnlyCollection<ToolkitSession> CurrentSessions
        {
            get
            {
                return m_readOnlySessions;
            }
        }

        /// <summary>
        /// Get reference to current ApplicationInstance
        /// </summary>
        public ApplicationInstance ApplicationInstance
        {
            get
            {
                return m_applicationInstance;
            }
        }
        #endregion


        #region Public Events
        /// <summary>
        /// Occurs when certificate validation is required.
        /// </summary>
        public event EventHandler<CertificateValidationEventArgs> CertificateValidation;

        #endregion

        #region Public Methods    
        public static async Task<Application> CreateConfiguredApplication(string configFileSectionName = null, string configFileName = null)
        {
            Application application = new Application();    
            
            ApplicationInstance applicationInstance = new ApplicationInstance();            
            applicationInstance.ApplicationType = ApplicationType.Client;
            applicationInstance.ConfigurationType = typeof(ExtendedApplicationConfiguration);

            if (configFileSectionName != null)
            {
                applicationInstance.ConfigSectionName = configFileSectionName;

                await applicationInstance.LoadApplicationConfiguration(true);
            }
            else if (configFileName != null)
            {
                await applicationInstance.LoadApplicationConfiguration(configFileName, true);
            }
            if (applicationInstance.ApplicationConfiguration != null)
            {
                await applicationInstance.CheckApplicationInstanceCertificate(false, 0);
            }
            else
            {
                applicationInstance.ApplicationConfiguration = new ExtendedApplicationConfiguration();
            }

            application.m_applicationInstance = applicationInstance;
            application.m_Configuration = applicationInstance.ApplicationConfiguration as ExtendedApplicationConfiguration;           

            return application;
        }


        /// <summary>
        /// Creates and initializes a new instance of the <see cref="Session"/> class.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="securityMode"></param>
        /// <param name="securityPolicy"></param>
        /// <param name="encoding"></param>
        /// <param name="user"></param>
        /// <param name="locales"></param>
        /// <returns></returns>
        public ToolkitSession CreateSession(string url, MessageSecurityMode securityMode, SecurityPolicy securityPolicy, MessageEncoding encoding, UserIdentity user, string[] locales)
        {    
            ToolkitSession newSession = new ToolkitSession(url, securityMode, securityPolicy.ToString(), encoding, user, locales);

            //set configured values
            if (Configuration.DefaultKeepAliveInterval > 0)
            {
                newSession.KeepAliveInterval = Configuration.DefaultKeepAliveInterval;
            }
            newSession.Timeout = Configuration.DefaultSessionTimeout;
            newSession.ReconnectTimerDelay = Configuration.SessionReconnectDelay;
            newSession.KeepAliveTimeout = Configuration.DefaultKeepAliveTimeout;
            newSession.ApplicationConfiguration = this.Configuration;
            //register session
            RegisterSession(newSession);

            newSession.Disposing += Session_Disposing;
            newSession.SessionNameChanging += Session_SessionNameChanging;
            return newSession;
        }       

        /// <summary>
        /// Activates the license for the specified feature.
        /// </summary>
        /// <param name="licenseFeature">The license feature.</param>
        /// <param name="licenseKey">The license key.</param>
        /// <returns>True if License is OK</returns>
        //TODO 2 - licensing
        public bool ActivateLicense(LicenseFeature licenseFeature, string licenseKey)
        {
            return true;// ApplicationInstance.ActivateLicense((LicenseFeature)licenseFeature, licenseKey);
        }
        

        /// <summary>
        ///  Uses the UA validation logic for HTTPS certificates.
        /// </summary>
        public void UseUaValidationForHttps()
        {
            //todo 3 - see what is about https validation
            //ApplicationInstance.SetUaValidationForHttps(ApplicationInstance.ApplicationConfiguration.CertificateValidator);
        }

        #endregion

        #region Internal Methods
        private void RegisterSession(ToolkitSession session)
        {
            lock (m_sessions)
            {
                m_sessions.Add(session);
            }
        }

        internal void UnregisterSession(ToolkitSession session)
        {
            lock (m_sessions)
            {
                m_sessions.Remove(session);
            }
        }

        internal void OnConfigurationValidated()
        {
            m_applicationInstance.ApplicationConfiguration.CertificateValidator.CertificateValidation -= CertificateValidator_CertificateValidation;
            m_applicationInstance.ApplicationConfiguration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;
        }

        /// <summary>
        /// Handle 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Session_Disposing(object sender, EventArgs e)
        {
            ToolkitSession session = sender as ToolkitSession;
            if (session != null)
            {
                session.Disposing -= Session_Disposing;
                session.SessionNameChanging -= Session_SessionNameChanging;

                UnregisterSession(session);
            }
        }

        /// <summary>
        /// Handler for Session.SessionNameChanging
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Session_SessionNameChanging(object sender, PropertyChangingEventArgs e)
        {
            ToolkitSession session = sender as ToolkitSession;
            string newSessionName = e.NewValue as string;
            if (session == null || newSessionName == null)
            {
                e.Cancel = true;
                return;
            }
            ToolkitSession sessionDuplicate = m_sessions.FirstOrDefault(t => t.SessionName != null && t.SessionName.Equals(newSessionName) && t != session);

            if (sessionDuplicate != null)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.SessionName", "Duplicate Session Name for Session: {0}.", newSessionName);
            }
        }
        #endregion

        #region Private Methods
        private void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            if (CertificateValidation != null)
            {
                if (e.Error != null && e.Error.StatusCode != StatusCodes.BadCertificateUntrusted)
                {
                    string message = e.Error.LocalizedText != null ? e.Error.LocalizedText.Text : string.Empty;
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Application.CertificateValidator_CertificateValidation", "{0} {1}", e.Error.StatusCode , message);
                }

                CertificateValidation(sender, e);

                if (e.Accept)
                {

                    ToolkitUtils.AddCertificateToStore(Configuration.SecurityConfiguration.TrustedPeerCertificates, e.Certificate);
                }
                else
                {
                    ToolkitUtils.AddCertificateToStore(Configuration.SecurityConfiguration.RejectedCertificateStore, (e.Certificate));
                }
            }
        }  
        #endregion
    }
    
}
