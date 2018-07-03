/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using Opc.Ua;
using XamarinSampleClient.Helpers;
using Softing.Opc.Ua.Client;
using System.Security.Cryptography.X509Certificates;

namespace XamarinSampleClient.ViewModels
{
    /// <summary>
    /// View model for ConnectPage
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class ConnectViewModel : BaseViewModel
    {
        #region Fields
        private string m_result;
        private string m_serverUrl;
        private MessageSecurityMode m_selectedMessageSecurityMode;
        private SecurityPolicy m_selectedSecurityPolicy;
        private MessageEncoding m_selectedMessageEncoding;
        private UserTokenType m_selectedUserTokenType;
        private bool m_isEditUserCredentials;
        private bool m_isEditUserCertificate;
        private string m_userName;
        private string m_userCertificate;
        private string m_password;
        private bool m_canConnect;
        private ClientSession m_session;
        #endregion

        #region Constructors

        /// <summary>
        /// Create new instance of ConnectViewModel
        /// </summary>
        public ConnectViewModel()
        {
            Title = "Connect sample";
            ServerUrl = App.DefaultSampleServerUrl;
            MessageSecurityModes = new List<MessageSecurityMode>() { MessageSecurityMode.None, MessageSecurityMode.Sign, MessageSecurityMode.SignAndEncrypt };
            SelectedMessageSecurityMode = MessageSecurityModes[0];

            SecurityPolicies = new List<SecurityPolicy>() { SecurityPolicy.None, SecurityPolicy.Basic256, SecurityPolicy.Basic128Rsa15, SecurityPolicy.Basic256Sha256 };
            SelectedSecurityPolicy = SecurityPolicies[0];

            MessageEncodings = new List<MessageEncoding>() { MessageEncoding.Binary };
            SelectedMessageEncoding = MessageEncodings[0];

            UserName = "usr";
            Password = "pwd";
            UserCertificate = "";
            UserTokenTypes = new List<UserTokenType>() { UserTokenType.Anonymous, UserTokenType.UserName, UserTokenType.Certificate };
            SelectedUserTokenType = UserTokenTypes[0];
            CanConnect = true;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Sample Result message 
        /// </summary>
        public string Result
        {
            get { return m_result; }
            set { SetProperty(ref m_result, value); }
        }
        /// <summary>
        /// Server Url
        /// </summary>
        public string ServerUrl
        {
            get { return m_serverUrl; }
            set
            {
                SetProperty(ref m_serverUrl, value);
                Result = "";
                App.DefaultSampleServerUrl = value;
            }
        }

        /// <summary>
        /// Selected MessageSecurityMode
        /// </summary>
        public MessageSecurityMode SelectedMessageSecurityMode
        {
            get { return m_selectedMessageSecurityMode; }
            set
            {
                SetProperty(ref m_selectedMessageSecurityMode, value);
                Result = "";
            }
        }

        /// <summary>
        /// List of available MessageSecurityModes
        /// </summary>
        public List<MessageSecurityMode> MessageSecurityModes { get; }

        /// <summary>
        /// List of available SecurityPolicies
        /// </summary>
        public List<SecurityPolicy> SecurityPolicies { get; }


        /// <summary>
        /// Selected SecurityPolicy
        /// </summary>
        public SecurityPolicy SelectedSecurityPolicy
        {
            get { return m_selectedSecurityPolicy; }
            set
            {
                SetProperty(ref m_selectedSecurityPolicy, value);
                Result = "";
            }
        }

        /// <summary>
        /// List of available MessageEncodings
        /// </summary>
        public List<MessageEncoding> MessageEncodings { get; }


        /// <summary>
        /// Selected MessageEncoding
        /// </summary>
        public MessageEncoding SelectedMessageEncoding
        {
            get { return m_selectedMessageEncoding; }
            set
            {
                SetProperty(ref m_selectedMessageEncoding, value);
                Result = "";
            }
        }

        /// <summary>
        /// List of available UserIdentity
        /// </summary>
        public List<UserTokenType> UserTokenTypes { get; }

        /// <summary>
        /// Selected UserIdentity
        /// </summary>
        public UserTokenType SelectedUserTokenType
        {
            get { return m_selectedUserTokenType; }
            set
            {
                SetProperty(ref m_selectedUserTokenType, value);
                Result = "";
                IsEditUserCertificate = (value == UserTokenType.Certificate);
                IsEditUserCredentials = (value == UserTokenType.UserName);
            }
        }
        /// <summary>
        /// Indicator if username and password are editable
        /// </summary>
        public bool IsEditUserCredentials
        {
            get { return m_isEditUserCredentials && CanConnect; }
            set { SetProperty(ref m_isEditUserCredentials, value); }
        }

        /// <summary>
        /// Indicator if user certificate is editable
        /// </summary>
        public bool IsEditUserCertificate
        {
            get { return m_isEditUserCertificate && CanConnect; }
            set { SetProperty(ref m_isEditUserCertificate, value); }
        }


        /// <summary>
        /// Public property to set and get indicator if item is busy
        /// </summary>
        public new bool IsBusy
        {
            get { return base.IsBusy; }
            set
            {
                base.IsBusy = value;
                OnPropertyChanged("IsEditUserCredentials");
                OnPropertyChanged("CanConnect");
                OnPropertyChanged("CanDisconnect");
            }
        }
        /// <summary>
        /// User certificate path
        /// </summary>
        public string UserCertificate
        {
            get { return m_userCertificate; }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                SetProperty(ref m_userCertificate, value);
            }
        }
        /// <summary>
        /// Username
        /// </summary>
        public string UserName
        {
            get { return m_userName; }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                SetProperty(ref m_userName, value);
            }
        }

        /// <summary>
        /// Password
        /// </summary>
        public string Password
        {
            get { return m_password; }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                SetProperty(ref m_password, value);
            }
        }

        /// <summary>
        /// Get/set indicator if session can be connected
        /// </summary>
        public bool CanConnect
        {
            get { return m_canConnect && !IsBusy; }
            set
            {
                SetProperty(ref m_canConnect, value);
                OnPropertyChanged("CanDisconnect");
            }
        }
     
        /// <summary>
        /// Get/set indicator if session can be disconencted
        /// </summary>
        public bool CanDisconnect
        {
            get { return !m_canConnect && !IsBusy; }
        }
        #endregion
        
        #region Methods

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and anonymous user identity.
        /// </summary>
        public void CreateAndConnectSession()
        {
            // create the session object.
            try
            {
                //create user identity used to connect
                UserIdentity userIdentity = null;
                switch (SelectedUserTokenType)
                {
                    case UserTokenType.Anonymous:
                        userIdentity = new UserIdentity();
                        break;
                    case UserTokenType.UserName:                
                        userIdentity = new UserIdentity(UserName, Password);
                        break;
                    case UserTokenType.Certificate:
                        userIdentity = new UserIdentity(new X509Certificate2(UserCertificate));
                        break;
                }

                // Create the Session object.
                m_session = SampleApplication.UaApplication.CreateSession(ServerUrl,
                    SelectedMessageSecurityMode,
                    SelectedSecurityPolicy,
                    SelectedMessageEncoding,
                    userIdentity);


                Result = "Session created...";
                m_session.SessionName = "Connect sample";
                m_session.Connect(false, true);
                Result += " Session connected...";
                CanConnect = false;
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    Result += string.Format("Error: {0}", e.InnerException.Message);
                }
                else
                {
                    Result += string.Format("Error: {0}", e.Message);
                }
            }
        }

        /// <summary>
        /// Creates and connects a session on opc.tcp protocol with no security and anonymous user identity.
        /// </summary>
        public void DisconnectSession()
        {
            // create the session object.
            try
            {
                if (m_session != null)
                {
                    // Disconnect the session.
                    m_session.Disconnect(true);
                    Result += "Session disconnected.";

                    m_session = null;
                    CanConnect = true;
                }                
            }
            catch (Exception e)
            {
                Result += string.Format("Error: {0}", e.Message);
            }
        }

        #endregion

        #region Public Override Methods
        /// <summary>
        /// Perform operations required when closing a view
        /// </summary>
        public override void Close()
        {
            DisconnectSession();
            base.Close();
        }
        #endregion
    }
}
