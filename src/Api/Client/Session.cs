using Opc.Ua.Client;
using Opc.Ua.Toolkit.Client.Nodes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using ToolkitVariableNode = Opc.Ua.Toolkit.Client.Nodes.VariableNode;
using ToolkitObjectNode = Opc.Ua.Toolkit.Client.Nodes.ObjectNode;
using ToolkitObjectTypeNode = Opc.Ua.Toolkit.Client.Nodes.ObjectTypeNode;
using ToolkitVariableTypeNode = Opc.Ua.Toolkit.Client.Nodes.VariableTypeNode;
using ToolkitMethodNode = Opc.Ua.Toolkit.Client.Nodes.MethodNode;
using ToolkitDataTypeNode = Opc.Ua.Toolkit.Client.Nodes.DataTypeNode;
using ToolkitReferenceTypeNode = Opc.Ua.Toolkit.Client.Nodes.ReferenceTypeNode;
using ToolkitViewNode = Opc.Ua.Toolkit.Client.Nodes.ViewNode;
using Opc.Ua.Toolkit.Trace;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Manages a communication session between the OPC UA Client and the OPC UA Server.
    /// </summary>
    /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/*'/>
    public partial class Session : BaseStateManagement, IDisposable
    {
        #region Fields

        /// <summary>
        /// Advises whether the current session instance was disposed or not.<br/>
        /// Implements the IDisposable interface due to the fact that the contained SDK session implements IDisposable, too (contains a socket unmanaged resource).
        /// </summary>
        protected bool m_disposed;
        
        private string m_url;
        private string m_sessionName;
        private uint m_timeout;
        private uint m_revisedTimeout;
        private MessageSecurityMode m_securityMode;
        private UserIdentity m_userIdentity;
        private string[] m_locales;
        private string m_securityPolicy;
        private MessageEncoding m_encoding; //TODO INVESTIGATE WHY IT IS OVERWRITTEN WHEN connectiong to a new session

        private readonly Dictionary<Browser, object> m_browseRequestSenders = new Dictionary<Browser, object>();
        
        private volatile System.Threading.Timer m_reconnectTimer;
        private int m_keepAliveInterval;
        private uint m_keepAliveTimeout;

        // Reconnect objects.
        private bool m_reconnecting;
        private bool m_modified;
        private readonly object m_reconnectLock = new object();
        private Opc.Ua.Client.Session m_disconectedSession;

        private EndpointDescription m_endpointDescription;

        // Toolkit members
        private Opc.Ua.Client.Session m_session;
        private EndpointDescriptionCollection m_expectedServerEndpoints; //todo investigate why these endpoints are kept here
        private readonly List<Subscription> m_subscriptions = new List<Subscription>();
        private readonly ReadOnlyCollection<Subscription> m_readonlySubscriptions;
        private readonly List<Method> m_methods = new List<Method>();
        private readonly ReadOnlyCollection<Method> m_readonlyMethods;
        private readonly BrowseHandler m_browseHandler;
        private NodeId m_sessionId;
        private BrowseOptions m_browseOptions = new BrowseOptions();
        private bool m_checkDomain;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class.
        /// </summary>
        /// <param name="url">The server URL to connect to.</param>
        /// <param name="securityMode">The security mode to be used.</param>
        /// <param name="securityPolicy">The security policy to be enforced.</param>
        /// <param name="encoding">The encoding to be used.</param>
        /// <param name="user">The user identity to be used.</param>
        /// <param name="locales">The locales preferences to be sent to the server.</param>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/constructor[@name="Session"]/*'/>
        internal Session(string url, MessageSecurityMode securityMode, string securityPolicy, MessageEncoding encoding, UserIdentity user, string[] locales) 
            : base(null)
        {       
            m_url = url.Trim();
            m_securityMode = securityMode;
            m_securityPolicy = securityPolicy.StartsWith(SecurityPolicies.BaseUri) ? securityPolicy : SecurityPolicies.GetUri(securityPolicy);

            if (m_securityPolicy == null)
            {
                m_securityPolicy = SecurityPolicies.None;
            }

            m_encoding = encoding;
            m_userIdentity = user;
            m_locales = locales;            

            m_readonlySubscriptions = new ReadOnlyCollection<Subscription>(m_subscriptions);
            m_readonlyMethods = new ReadOnlyCollection<Method>(m_methods);
            m_browseHandler = new BrowseHandler(m_session);            
        }

        #endregion Constructors

        #region Public Events
       

        /// <summary>
        /// Event raised when a browse operation reached a continuation point.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/event[@name="ContinuationPointReached"]/*'/>
        public event EventHandler<BrowseEventArgs> ContinuationPointReached;

        /// <summary>
        /// Event raised when a history read operation reached a continuation point.
        /// </summary>
        public event EventHandler<HistoryReadContinuationEventArgs> HistoryContinuationPointReached;

        /// <summary>
        /// Event raised if a publish error occurred.
        /// </summary>
        public event EventHandler<PublishErrorEventArgs> PublishError;

        /// <summary>
        /// Event raised when a keep alive arrives from the server or an error is detected.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/event[@name="KeepAlive"]/*'/>
        public event EventHandler<KeepAliveEventArgs> KeepAlive;

        #endregion Public Events

        #region Internal Events
        /// <summary>
        /// Event raised when the session name is changind
        /// </summary>
        internal event EventHandler<PropertyChangingEventArgs> SessionNameChanging;

        /// <summary>
        /// Event raised when a Session object is disposed
        /// </summary>
        internal event EventHandler Disposing;
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the session.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="SessionName"]/*'/>
        public virtual string SessionName
        {
            get { return m_sessionName; }

            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("The SessionName property can only be changed in Disconnected state", StatusCodes.BadInvalidState);
                }

                PropertyChangingEventArgs e = new PropertyChangingEventArgs()
                {
                    OldValue = m_sessionName,
                    NewValue = value,
                    Cancel = false
                };

                RaiseSessionNameChanging(e);

                if (!e.Cancel)
                {
                    m_sessionName = value;
                }                
            }
        }

        /// <summary>
        /// Gets or sets the requested timeout.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="Timeout"]/*'/>
        public virtual uint Timeout
        {
            get { return m_timeout; }

            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("The Timeout property can only be changed in Disconnected state", StatusCodes.BadInvalidState);
                }

                m_timeout = value;
            }
        }

        /// <summary>
        /// Gets the server-revised timeout.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="RevisedTimeout"]/*'/>
        public virtual uint RevisedTimeout
        {
            get { return m_revisedTimeout; }
        }

        /// <summary>
        /// Gets or sets the session URL.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="Url"]/*'/>
        public virtual string Url
        {
            get { return m_url; }

            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("The URL property can only be changed in Disconnected state", StatusCodes.BadInvalidState);
                }

                m_url = value.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the security mode.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="SecurityMode"]/*'/>
        public virtual MessageSecurityMode SecurityMode
        {
            get { return m_securityMode; }
            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("The SecurityMode property can only be changed in Disconnected state", StatusCodes.BadInvalidState);
                }

                m_securityMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the user identity.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="UserIdentity"]/*'/>
        public virtual UserIdentity UserIdentity
        {
            get { return m_userIdentity; }

            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("The UserIdentity property can only be changed in Disconnected state", StatusCodes.BadInvalidState);
                }

                m_userIdentity = value;

                Update(m_userIdentity, m_locales);
            }
        }

        /// <summary>
        /// Gets or sets the list of locales used in the provided order by the server to return localized strings.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="Locales"]/*'/>
        public virtual string[] Locales
        {
            get { return m_locales; }

            set
            {
                m_locales = value;

                Update(m_userIdentity, m_locales);
            }
        }

        /// <summary>
        /// Gets or sets the security policy URI.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="SecurityPolicy"]/*'/>
        public virtual string SecurityPolicy
        {
            get { return m_securityPolicy; }

            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("The SecurityPolicy property can only be changed in Disconnected state", StatusCodes.BadInvalidState);
                }

                if (value == null)
                {
                    m_securityPolicy = null;
                }
                else
                {
                    m_securityPolicy = value.StartsWith(SecurityPolicies.BaseUri) ? value : SecurityPolicies.GetUri(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the message encoding (xml / binary).
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="Encoding"]/*'/>
        public virtual MessageEncoding Encoding
        {
            get { return m_encoding; }

            set
            {
                if (TargetState != State.Disconnected)
                {
                    throw new BaseException("The Encoding property can only be changed in Disconnected state", StatusCodes.BadInvalidState);
                }

                m_encoding = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to check the domain of the certificate used to create the session or not.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="CheckDomain"]/*'/>
        public virtual bool CheckDomain
        {
            get { return m_checkDomain; }
            set { m_checkDomain = value; }
        }

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="ApplicationName"]/*'/>
        public virtual string ApplicationName
        {
            get
            {
                if (m_endpointDescription != null 
                    && m_endpointDescription.Server!= null
                    && m_endpointDescription.Server.ApplicationName != null)
                {
                    return m_endpointDescription.Server.ApplicationName.Text;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the server assigned session id.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="Id"]/*'/>
        public virtual NodeId Id
        {
            get { return m_sessionId; }
        }

        /// <summary>
        /// Gets the subscriptions belonging to this session. This is a read-only collection.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="Subscriptions"]/*'/>
        public virtual ReadOnlyCollection<Subscription> Subscriptions
        {
            get { return m_readonlySubscriptions; }
        }

        /// <summary>
        /// Gets the methods belonging to this session. This is a read-only collection.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="Methods"]/*'/>
        public virtual ReadOnlyCollection<Method> Methods
        {
            get { return m_readonlyMethods; }
        }

        /// <summary>
        /// Gets or sets the options used for browsing this session.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="BrowseOptions"]/*'/>
        public virtual BrowseOptions BrowseOptions
        {
            get { return m_browseOptions; }
            set { m_browseOptions = value; }
        }

        /// <summary>
        /// Gets the session's browse handler.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="BrowseHandler"]/*'/>
        public virtual BrowseHandler BrowseHandler
        {
            get { return m_browseHandler; }
        }

        /// <summary>
        /// Gets the factory used to create encodable objects.
        /// </summary>
        public virtual EncodeableFactory Factory
        {
            get
            {
                return m_session.Factory;
            }
        }

        /// <summary>
        /// Gets the list of primary server's known namespace URIs.
        /// </summary>
        public virtual List<string> NamespaceUris
        {
            get
            {
                if (m_session != null && m_session.NamespaceUris != null)
                {
                    return new List<string>(m_session.NamespaceUris.ToArray());
                }

                return null;
            }
        }

        /// <summary>
        /// Gets or sets a value (in milliseconds) indicating how frequently the server connection is checked to see whether the communication is still working or not.
        /// </summary>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/property[@name="KeepAliveInterval"]/*'/>
        public virtual int KeepAliveInterval
        {
            get
            {
                return m_keepAliveInterval;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("The value must be non-negative.");
                }

                m_keepAliveInterval = value;

                if (m_session != null)
                {
                    m_session.KeepAliveInterval = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the waiting time (in milliseconds) for KeepAlive read requests to return from the server.
        /// </summary>
        public virtual uint KeepAliveTimeout
        {
            get
            {
                return m_keepAliveTimeout;
            }

            set
            {
                m_keepAliveTimeout = value;

                if (m_session != null)
                {
                    //todo investigate why it was removed from sdk
                   // m_session.KeepAliveTimeout = value;
                }
            }
        }

        #endregion Public Properties

        #region Internal Properties
        /// <summary>
        /// Get/Set the appliation consifguration associated with this session instance
        /// </summary>
        internal ExtendedApplicationConfiguration ApplicationConfiguration
        {
            get;set;
        }



        internal int ReconnectTimerDelay
        {
            get;set;
        }
        /// <summary>
        /// Returns the SDK Core session object.
        /// </summary>
        internal virtual Opc.Ua.Client.Session CoreSession
        {
            get
            {
                return m_session;
            }
        }

        #endregion Internal Propeties

        #region Public Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.<br/>
        /// Releases all the resources held by the current Session instance and suppresses the Garbage Collector Finalizer call.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initializes the Session with the endpoint description returned by discovery.<br/>
        /// This includes the ServerCertificate, Server Discovery URLs etc.
        /// </summary>
        /// <param name="description">The endpoint description parameter of type <see cref="EndpointDescription"/>.</param>
        public virtual void InitializeWithDiscoveryEndpointDescription(EndpointDescription description)
        {
            m_endpointDescription = description;
        }

        /// <summary>
        /// Updates the session with the specified identity and locales.
        /// </summary>
        /// <param name="identity">The user identity parameter of type <see cref="UserIdentity"/>.</param>
        /// <param name="locales">The locales to be used by the server.</param>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="Update"]/*'/>
        public virtual void Update(UserIdentity identity, string[] locales)
        {
            if (identity == null)
            {
                throw new System.ArgumentNullException("identity");
            }

            SetModified();
            
            if (m_session != null && CurrentState != State.Disconnected)
            {
                StringCollection localeCollection = locales == null ? null : new StringCollection(locales);

                try
                {
                    lock (this)
                    {
                        m_session.UpdateSession(identity, localeCollection);
                        m_userIdentity = identity;
                        m_locales = locales;
                    }
                    
                    TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Update", "Session update completed for session {0}.", this.m_sessionName);
                }
                catch (ServiceResultException sre)
                {
                    string logMessage = string.Format("Session Update error {0}.", sre.Message);

                    if (sre.StatusCode == StatusCodes.BadIdentityTokenRejected)
                    {
                        logMessage = string.Format("Session Update error. The server has rejected the specified User Identity.");
                    }

                    if (sre.StatusCode == StatusCodes.BadIdentityTokenInvalid)
                    {
                        logMessage = string.Format("Session Update error. The specified user User Identity is invalid.");
                    }

                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Update", sre, logMessage);
                    throw new BaseException(logMessage, sre);
                }
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Update", ex);
                    throw new BaseException("Session Update error", ex);
                }
            }
            else
            {
                lock (this)
                {
                    m_userIdentity = identity;
                    m_locales = locales;
                }
            }

            SetModified();
        }

        /// <summary>
        /// Disconnects and deletes a subscription from the current session.
        /// </summary>
        /// <param name="subscription">The Subscription instance to be deleted.</param>
        public virtual void DeleteSubscription(Subscription subscription)
        {
            lock (((ICollection)m_subscriptions).SyncRoot)
            {
                if (m_subscriptions.Remove(subscription) == false)
                {
                    throw new BaseException("Provided subscription was not found in current session.");
                }
            }

            try
            {
                subscription.Disconnect(true);
                subscription.Parent = null;
            }
            catch (Exception ex)
            {
                lock (((ICollection)m_subscriptions).SyncRoot)
                {
                    m_subscriptions.Add(subscription);
                }

                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.DeleteSubscription", ex);
                throw;
            }
            finally
            {
                SetModified();
            }
        }

        #region Read

        /// <summary>
        /// Reads the specified node.
        /// </summary>
        /// <param name="nodeToRead">The node to be read.</param>
        /// <returns>A <see cref="DataValue"/> that represents the value of the specified attribute.</returns>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="Read"]/*'/>
        public virtual DataValue Read(ReadValueId nodeToRead)
        {
            if (nodeToRead == null)
            {
                throw new ArgumentNullException("nodeToRead");
            }

            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Read while in the Disconnected state", StatusCodes.BadInvalidState);
            }          

            DataValueCollection dataValues;
            DiagnosticInfoCollection diagnosticInfos;
            ReadValueIdCollection readValueIds = new ReadValueIdCollection();

            readValueIds.Add(nodeToRead);

            try
            {
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Read", "Read operation started for node {0}.", nodeToRead.NodeId);

                m_session.Read(null, 0,
                               TimestampsToReturn.Both,
                               readValueIds,
                               out dataValues,
                               out diagnosticInfos);

                ClientBase.ValidateResponse(dataValues, readValueIds);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, readValueIds);
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Read", "Read operation completed for node {0}.", nodeToRead.NodeId);
            }            
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Read", ex);
                throw new BaseException("Session Read error", ex);
            }

            if (dataValues != null && dataValues.Count == 1)
            {
                return new DataValue(dataValues[0]);
            }

            return null;
        }

        /// <summary>
        /// Reads the specified nodes from the server.
        /// </summary>
        /// <param name="nodesToRead">The nodes to be read.</param>
        /// <param name="maxAge">Maximum age of the value to be read in milliseconds. The age of the value is based on the difference between the ServerTimestamp and the time when the Server starts processing the read request.</param>
        /// <param name="timestampsToReturn">An enumeration that specifies the timestamps to be returned for each requested variable value attribute.</param>
        /// <returns>A list of <see cref="DataValue"/> that represents the values of the specified attributes.</returns>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="Read1"]/*'/>
        public virtual IList<DataValue> Read(IList<ReadValueId> nodesToRead, double maxAge, TimestampsToReturn timestampsToReturn)
        {
            if (nodesToRead == null)
            {
                throw new ArgumentNullException("nodesToRead");
            }

            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Read while in the Disconnected state", StatusCodes.BadInvalidState);
            }
                        
            List<DataValue> result = new List<DataValue>();

            DataValueCollection dataValues;
            DiagnosticInfoCollection diagnosticInfos;
            ReadValueIdCollection readValueIds = new ReadValueIdCollection();

            ReadValueId itemToRead = null;
            for (int i = 0; i < nodesToRead.Count; i++)
            {
                itemToRead = nodesToRead[i];
                if (itemToRead != null)
                {
                    readValueIds.Add(itemToRead);
                }
            }

            try
            {
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Read", "Read operation started for {0} values(s).", nodesToRead.Count);

                // TODO: responseHeader is not used
                ResponseHeader responseHeader = m_session.Read(null, maxAge,
                                            timestampsToReturn,
                                            readValueIds,
                                            out dataValues,
                                            out diagnosticInfos);

                ClientBase.ValidateResponse(dataValues, readValueIds);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, readValueIds);
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Read", "Read operation completed for {0} values(s).", nodesToRead.Count);
            }            
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Read", ex);
                throw new BaseException("Session Read error", ex);
            }

            if (dataValues != null)
            {
                for (int i = 0; i < dataValues.Count; i++)
                {
                    result.Add(new DataValue(dataValues[i]));
                }
            }

            return result;
        }

        /// <summary>
        /// Reads the specified node from the server.
        /// </summary>
        /// <param name="nodeId">The node id to be read.</param>
        /// <returns>A <see cref="BaseNode"/> that represents the node with its attributes read for the specified node id.</returns>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="ReadNode"]/*'/>
        public virtual BaseNode ReadNode(NodeId nodeId)
        {
            if (nodeId == null)
            {
                throw new ArgumentNullException("nodeId");
            }

            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Read while in the Disconnected state", StatusCodes.BadInvalidState);
            }       
            
            TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.ReadNode", "Read node operation started for node {0}.", nodeId);

            BaseNode result = null;
            try
            {
                result = ReadNodeComplete(nodeId, m_session);
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.ReadNode", "Read node operation completed for node {0}.", nodeId);
            }            
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.ReadNode", ex);
                throw new BaseException("Session ReadNode error", ex);
            }

            return result;
        }

        #endregion Read

        #region Browse

        /// <summary>
        /// Browses the specified node id and returns the list of references. Uses the BrowseOptions set on the session.
        /// </summary>
        /// <param name="nodeId">The NodeId of the start node, if the Root node is the start node this parameter should be set to null.</param>
        /// <param name="cookie">The sender/cookie of the request.</param>
        /// <returns>A list of <see cref="ReferenceDescription"/> with all references found. An empty list is returned if nothing was found.</returns>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="BrowseA"]/*'/>
        public virtual IList<ReferenceDescription> Browse(NodeId nodeId, object cookie)
        {
            if (CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot Browse while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            // create a copy of the browse options for the session
            BrowseOptions browseOptions = (BrowseOptions)BrowseOptions.MemberwiseClone();

            return Browse(nodeId, browseOptions, cookie);
        }

        /// <summary>
        /// Browses the specified node id and returns the list of references. Uses the provided BrowseOptions object.
        /// </summary>
        /// <param name="nodeId">The NodeId of the node to browse.</param>
        /// <param name="browseOptions">The options to use for browsing operation.</param>
        /// <param name="cookie">The sender/cookie of the request.</param>
        /// <returns>A list of <see cref="ReferenceDescription"/> with all references found. Empty list is returned if none was found.</returns>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="BrowseB"]/*'/>
        public virtual IList<ReferenceDescription> Browse(NodeId nodeId, BrowseOptions browseOptions, object cookie)
        {
            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Browse while in the Disconnected state", StatusCodes.BadInvalidState);
            }
            
            // declare a Browser object
            Browser browser = null;
            try
            {
                // initialize Browser object
                browser = new Browser(m_session);

                // set browse options
                if (browseOptions != null)
                {
                    if (browseOptions.ReferenceTypeId != null)
                    {
                        browser.ReferenceTypeId = browseOptions.ReferenceTypeId;
                    }
                    else
                    {
                        browser.ReferenceTypeId = null;
                    }

                    browser.MaxReferencesReturned = browseOptions.MaxReferencesReturned;
                    browser.BrowseDirection = browseOptions.BrowseDirection;
                    browser.IncludeSubtypes = browseOptions.IncludeSubtypes;
                    browser.NodeClassMask = Utils.ToInt32(browseOptions.NodeClassMask);
                    browser.ContinueUntilDone = browseOptions.ContinueUntilDone;
                    browser.View = null; // View not set
                }

                browser.MoreReferences += new BrowserEventHandler(MoreReferences);

                NodeId nodeToBrowse;

                if (nodeId == null)
                {
                    nodeToBrowse = ObjectIds.RootFolder;
                }
                else
                {
                    nodeToBrowse = nodeId;
                }

                // add sender object to the list of active request senders
                if (cookie != null)
                {
                    lock (m_browseRequestSenders)
                    {
                        m_browseRequestSenders.Add(browser, cookie);
                    }
                }
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Browse", "Browse operation started for NodeId {0}.", nodeToBrowse);

                ReferenceDescriptionCollection referenceCollection = browser.Browse(nodeToBrowse);

                browser.MoreReferences -= new BrowserEventHandler(MoreReferences);

                //todo investigate why refernce type name must be provided now
                // prepare result object
                //List<ReferenceDescription> browseResults = new List<ReferenceDescription>();

                //foreach (ReferenceDescription referenceDescription in referenceCollection)
                //{
                //    ReferenceDescription browseResult = new ReferenceDescription(referenceDescription);
                //   // browseResult.ReferenceTypeName = FindReferenceTypeName(browseResult.ReferenceTypeId, browseResult.IsForward);

                //    browseResults.Add(browseResult);
                //}
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Browse", "Browse operation returned {0} results for NodeId {1}.", referenceCollection.Count, nodeToBrowse);

                return referenceCollection;
            }           
            catch (Exception exception)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Browse", exception);
                throw new BaseException("Session Browse error", exception);
            }
            finally
            {
                if (browser != null)
                {
                    lock (m_browseRequestSenders)
                    {
                        m_browseRequestSenders.Remove(browser);
                    }
                }
            }
        }

        /// <summary>
        /// Translates the specified browse path to corresponding NodeIds.
        /// </summary>
        /// <param name="startingNode">NodeId of the starting Node for the browse path.</param>
        /// <param name="browsePath">The path to follow from the startingNode.</param>
        /// <returns>The list of <see cref="NodeId"/>s identified as the target of the specified browse path.</returns>
        public virtual IList<NodeId> TranslateBrowsePathToNodeIds(NodeId startingNode, List<QualifiedName> browsePath)
        {
            if (startingNode == null)
            {
                throw new ArgumentNullException("startingNode");
            }

            if (browsePath == null || browsePath.Count == 0)
            {
                throw new ArgumentNullException("browsePath");
            }

            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Translate Browse Path while in the Disconnected state", StatusCodes.BadInvalidState);
            }            

            try
            {
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.TranslateBrowsePathToNodeIds", "TranslateBrowsePathToNodeIds operation started for startingNode {0} .", startingNode);

                // create the request.
                BrowsePathCollection pathsToTranslate = new BrowsePathCollection();

                BrowsePath pathToTranslate = new BrowsePath();
                pathToTranslate.StartingNode = startingNode;
                pathToTranslate.RelativePath = new RelativePath();

                foreach (QualifiedName pathElement in browsePath)
                {
                    RelativePathElement element = new RelativePathElement();
                    element.ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences;
                    element.IsInverse = false;
                    element.TargetName = new QualifiedName(pathElement.Name, pathElement.NamespaceIndex);

                    pathToTranslate.RelativePath.Elements.Add(element);
                }

                pathsToTranslate.Add(pathToTranslate);

                // invoke the TranslateBrowsePathsToNodeIds service.
                BrowsePathResultCollection results = null;
                DiagnosticInfoCollection diagnosticInfos = null;

                ResponseHeader responseHeader = m_session.TranslateBrowsePathsToNodeIds(
                    null,
                    pathsToTranslate,
                    out results,
                    out diagnosticInfos);

                // verify that the server returned the correct number of results.
                ClientBase.ValidateResponse(results, pathsToTranslate);

                // check for bad operation result.
                if (ServiceResult.IsBad(results[0].StatusCode))
                {
                   string  logMessage = string.Format("TranslateBrowsePaths result {0} for startingNode {1} and path {2} ",
                        results[0].StatusCode, startingNode, pathToTranslate.RelativePath.Format(this.m_session.TypeTree));

                    throw new ServiceResultException(results[0].StatusCode.Code, logMessage);
                }

                List<NodeId> translateResults = new List<NodeId>();

                foreach (BrowsePathTarget target in results[0].Targets)
                {
                    translateResults.Add(new NodeId(ExpandedNodeId.ToNodeId(target.TargetId, m_session.NamespaceUris)));
                }
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.TranslateBrowsePathToNodeIds", 
                    "TranslateBrowsePath operation returned {0} result(s) for startingNode {1} and path {2}",
                    translateResults.Count,
                    startingNode,
                    pathToTranslate.RelativePath.Format(this.m_session.TypeTree));

                return translateResults;
            }           
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.TranslateBrowsePathToNodeIds", ex);
                throw new BaseException("Translate Browse Path error ", ex);
            }
        }

        /// <summary>
        /// Translates the specified list of browse paths to corresponding NodeIds.
        /// </summary>
        /// <param name="browsePaths">The list of <see cref="BrowsePath"/> requests to be translated.</param>
        /// <returns>The list of <see cref="BrowsePathResult"/>s identified as the target of the specified browse paths.</returns>
        /// 
        //todo seriousluy test this method due to heavy refactor!!!!

        public virtual IList<BrowsePathResult> TranslateBrowsePathsToNodeIds(List<BrowsePath> browsePaths)
        {
            if (browsePaths == null)
            {
                throw new ArgumentNullException("browsePaths");
            }

            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Translate Browse Paths while in the Disconnected state", StatusCodes.BadInvalidState);
            }           

            try
            {
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.TranslateBrowsePathsToNodeIds", 
                    "TranslateBrowsePathsToNodeIds operation started for {0} browsePaths .", browsePaths.Count);

                // create the request.
                BrowsePathCollection pathsToTranslate = new BrowsePathCollection();

                foreach (BrowsePath browsePath in browsePaths)
                {
                    BrowsePath pathToTranslate = new BrowsePath();
                    pathToTranslate.StartingNode = browsePath.StartingNode;
                    pathToTranslate.RelativePath = new RelativePath();

                    foreach (RelativePathElement pathElement in browsePath.RelativePath.Elements)
                    {
                        RelativePathElement element = new RelativePathElement();
                        element.ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences;
                        element.IsInverse = false;
                        element.TargetName = pathElement.TargetName;// new QualifiedName(pathElement.Name, pathElement.NamespaceIndex);

                        pathToTranslate.RelativePath.Elements.Add(element);
                    }

                    pathsToTranslate.Add(pathToTranslate);
                }

                // invoke the TranslateBrowsePathsToNodeIds service.
                BrowsePathResultCollection results = null;
                DiagnosticInfoCollection diagnosticInfos = null;

                ResponseHeader responseHeader = m_session.TranslateBrowsePathsToNodeIds(
                    null,
                    pathsToTranslate,
                    out results,
                    out diagnosticInfos);

                // verify that the server returned the correct number of results.
                ClientBase.ValidateResponse(results, pathsToTranslate);

                //List<BrowsePathResult> translateResults = new List<BrowsePathResult>();

                //// create the list of results.
                //foreach (BrowsePathResult result in results)
                //{
                //    BrowsePathResult translateResult = new BrowsePathResult();
                //    translateResult.StatusCode = new StatusCode((uint)result.StatusCode);

                //    foreach (BrowsePathTarget target in result.Targets)
                //    {
                //        translateResult.TargetIds.Add(new NodeId(ExpandedNodeId.ToNodeId(target.TargetId, m_session.NamespaceUris)));
                //    }

                //    translateResults.Add(translateResult);
                //}
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.TranslateBrowsePathsToNodeIds", "TranslateBrowsePathsToNodeIds operation returned {0} result(s).", results.Count);
                //todo heavy test this method due to refactor!!!!
                return results;
            }           
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.TranslateBrowsePathsToNodeIds",  ex);
                throw new BaseException("Translate Browse Path error ", ex);
            }
        }
        #endregion Browse

        #region Method Call

        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="objectId">The NodeId of the object that provides the method.</param>
        /// <param name="methodId">The NodeId of the method to call.</param>
        /// <param name="inputArgs">The list of input argument values.</param>
        /// <param name="outputArgs">The list of output argument values.</param>
        /// <returns>The StatusCode returned informs if method was called with success, if failed or if the results are uncertain.</returns>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="Call"]/*'/>
        public virtual StatusCode Call(NodeId objectId, NodeId methodId, IList<object> inputArgs, out IList<object> outputArgs)
        {
            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Call method while in the Disconnected state", StatusCodes.BadInvalidState);
            }
            
            TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Call", "Call operation started for method {0}.", methodId);

            outputArgs = new List<object>();
            try
            {
                outputArgs = m_session.Call(objectId, methodId, inputArgs.ToArray());                
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Call", "Call operation completed for method {0}.", methodId);

                return new StatusCode();
            }
            catch (ServiceResultException sre)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Call", sre);                

                return sre.StatusCode;
            }
            catch (Exception exception)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Call", exception);

                throw new BaseException("Session Call error", exception);
            }
        }


        internal static object GetUnderlyingValue(object wrappedValue)
        {
            if (wrappedValue is bool)
            {
                return new Variant((bool)wrappedValue);
            }
            if (wrappedValue is sbyte)
            {
                return new Variant((sbyte)wrappedValue);
            }
            if (wrappedValue is byte)
            {
                return new Variant((byte)wrappedValue);
            }
            if (wrappedValue is short)
            {
                return new Variant((short)wrappedValue);
            }
            if (wrappedValue is ushort)
            {
                return new Variant((ushort)wrappedValue);
            }
            if (wrappedValue is int)
            {
                return new Variant((int)wrappedValue);
            }
            if (wrappedValue is uint)
            {
                return new Variant((uint)wrappedValue);
            }
            if (wrappedValue is long)
            {
                return new Variant((long)wrappedValue);
            }
            if (wrappedValue is ulong)
            {
                return new Variant((ulong)wrappedValue);
            }
            if (wrappedValue is float)
            {
                return new Variant((float)wrappedValue);
            }
            if (wrappedValue is double)
            {
                return new Variant((double)wrappedValue);
            }
            if (wrappedValue is string)
            {
                return new Variant((string)wrappedValue);
            }
            if (wrappedValue is DateTime)
            {
                return new Variant((DateTime)wrappedValue);
            }
            if (wrappedValue is Guid)
            {
                return new Variant((Guid)wrappedValue);
            }
            if (wrappedValue is Uuid)
            {
                return new Variant((Uuid)wrappedValue);
            }
            return wrappedValue;
        }

        /// <summary>
        /// Returns a list of input and output arguments for the method with the specified method id.
        /// </summary>
        /// <param name="methodId">The NodeId of the method.</param>
        /// <param name="inputArguments">Output parameter that represents the list of input arguments.</param>
        /// <param name="outputArguments">Output parameter that represents the list of output arguments.</param>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="GetMethodArguments"]/*'/>
        public virtual void GetMethodArguments(NodeId methodId, out List<Argument> inputArguments, out List<Argument> outputArguments)
        {
            if (methodId == null)
            {
                throw new ArgumentNullException("methodId");
            }

            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Call method while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            inputArguments = null;
            outputArguments = null;

            try
            {
                NodeId inputArgumentsNodeId = null;
                NodeId outputArgumentsNodeId = null;
                ReferenceDescriptionCollection references = null;

                try
                {
                    // fetch method references from server.
                    references = this.m_session.FetchReferences(methodId);
                }
                catch
                {
                }

                if (references == null)
                {
                    // Node has no references.
                    return;
                }

                foreach (ReferenceDescription reference in references)
                {
                    if (reference.ReferenceTypeId == ReferenceTypeIds.HasProperty)
                    {
                        if (reference.BrowseName == BrowseNames.InputArguments)
                        {
                            inputArgumentsNodeId = ExpandedNodeId.ToNodeId(reference.NodeId, this.m_session.NamespaceUris);
                        }

                        if (reference.BrowseName == BrowseNames.OutputArguments)
                        {
                            outputArgumentsNodeId = ExpandedNodeId.ToNodeId(reference.NodeId, this.m_session.NamespaceUris);
                        }
                    }
                }

                // read the value from the server.
                // build list of values to read.
                ReadValueIdCollection nodesToRead = new ReadValueIdCollection(2);

                if (inputArgumentsNodeId != null)
                {
                    ReadValueId valueToRead = new ReadValueId();
                    valueToRead.NodeId = inputArgumentsNodeId;
                    valueToRead.AttributeId = Attributes.Value;
                    nodesToRead.Add(valueToRead);
                }

                if (outputArgumentsNodeId != null)
                {
                    ReadValueId valueToRead = new ReadValueId();
                    valueToRead.NodeId = outputArgumentsNodeId;
                    valueToRead.AttributeId = Attributes.Value;
                    nodesToRead.Add(valueToRead);
                }

                if (inputArgumentsNodeId == null && outputArgumentsNodeId == null)
                {   // Nothing to do
                    return;
                }

                // read the values.
                DataValueCollection results = null;
                DiagnosticInfoCollection diagnosticInfos = null;

                ResponseHeader responseHeader = m_session.Read(null, int.MaxValue, TimestampsToReturn.Neither, nodesToRead, out results, out diagnosticInfos);

                if (ServiceResult.IsGood(responseHeader.ServiceResult) && ServiceResult.IsGood(results[0].StatusCode) && ServiceResult.IsGood(results[0].StatusCode))
                {
                    ExtensionObject[] inputArgumentsList = null;
                    ExtensionObject[] outputArgumentsList = null;

                    if (inputArgumentsNodeId != null)
                    {
                        inputArgumentsList = results[0].Value as ExtensionObject[];

                        if (outputArgumentsNodeId != null)
                        {
                            outputArgumentsList = results[1].Value as ExtensionObject[];
                        }
                    }
                    else
                    {
                        if (outputArgumentsNodeId != null)
                        {
                            outputArgumentsList = results[0].Value as ExtensionObject[];
                        }
                    }

                    // retrieve the results
                    if (inputArgumentsList != null)
                    {
                        inputArguments = new List<Argument>(inputArgumentsList.Length);

                        for (int ii = 0; ii < inputArgumentsList.Length; ii++)
                        {
                            Argument argument = inputArgumentsList[ii].Body as Argument;

                            if (argument != null)
                            {
                                if (argument.Value == null && argument.DataType != null && !argument.DataType.IsNullNodeId)
                                {
                                    argument.Value = ArgumentExtension.GetDefaultValueForDatatype(argument.DataType, (ValueRanks)argument.ValueRank, this);
                                }

                                inputArguments.Add(argument);
                            }
                        }
                    }

                    if (outputArgumentsList != null)
                    {
                        outputArguments = new List<Argument>(outputArgumentsList.Length);

                        for (int ii = 0; ii < outputArgumentsList.Length; ii++)
                        {
                            Argument argument = outputArgumentsList[ii].Body as Argument;

                            if (argument != null)
                            {
                                if (argument.Value == null && argument.DataType != null && !argument.DataType.IsNullNodeId)
                                {
                                    argument.Value = ArgumentExtension.GetDefaultValueForDatatype(argument.DataType, (ValueRanks)argument.ValueRank, this);
                                }

                                outputArguments.Add(argument);
                            }
                        }
                    }

                    TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.GetMethodArguments", "ReadArguments completed for method {0}.", methodId);
                }
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.GetMethodArguments", ex);
                throw new BaseException("Session GetMethodArguments error", ex);
            }
        }

        #endregion Method Call

        #region Write

        /// <summary>
        /// Makes a write call with the specified value.
        /// </summary>
        /// <param name="valueToWrite">The value to write in the specified node and its attribute.</param>
        /// <returns>The <see cref="StatusCode"/> of the write operation.</returns>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="WriteA"]/*'/>
        public virtual StatusCode Write(WriteValue valueToWrite)
        {
            if (valueToWrite == null)
            {
                throw new ArgumentNullException("valueToWrite");
            }

            if (CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot Write while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            // create the write request
            List<WriteValue> valuesToWrite = new List<WriteValue>();
            valuesToWrite.Add(valueToWrite);

            // Call the Write service
            IList<StatusCode> writeResults = Write(valuesToWrite);

            // return the result
            return writeResults[0];
        }

        /// <summary>
        /// Makes a write call with the specified parameters.
        /// </summary>
        /// <param name="valuesToWrite">The values to write in the specified nodes and their attributes.</param>
        /// <returns>The <see cref="StatusCode"/> list of the write operations.</returns>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="WriteB"]/*'/>
        public virtual IList<StatusCode> Write(List<WriteValue> valuesToWrite)
        {
            if (valuesToWrite == null)
            {
                throw new ArgumentNullException("valueToWrite");
            }

            if (CurrentState == State.Disconnected || m_session == null)
            {
                throw new BaseException("Cannot Write while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            try
            {
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Write", "Write operation started for {0} values(s).", valuesToWrite.Count);
                        
                // prepare result objects
                StatusCodeCollection results = null;
                DiagnosticInfoCollection diagnosticInfos;

                // Call the Write service
                ResponseHeader responseHeader = m_session.Write(null,
                    new WriteValueCollection(valuesToWrite),
                    out results,
                    out diagnosticInfos);

                // Validate the results
                ClientBase.ValidateResponse(results, valuesToWrite);

                // return the results of the service call
                List<StatusCode> writeResults = new List<StatusCode>();

                foreach (StatusCode result in results.ToArray())
                {
                    writeResults.Add(result.Code);
                }
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Write", "Write operation completed for {0} values(s).", valuesToWrite.Count);

                return writeResults;
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Write", ex);
                throw new BaseException("Session Write error", ex);
            }
        }

        #endregion Write

        #region History
        /// <summary>
        /// Performs the history read raw call to the server. If the session is in disconnected state, the method will raise an exception.
        /// </summary>
        /// <param name="nodeToReadId">The node to read id.</param>
        /// <param name="readRawModifiedDetails">The read raw argument.</param>
        /// <param name="cookie">The cookie used to identify the call in case of a continuation point reached event.</param>
        /// <returns>The list of data values returned by the server.</returns>
        public virtual List<DataValue> HistoryReadRaw(NodeId nodeToReadId, ReadRawModifiedDetails readRawModifiedDetails, TimestampsToReturn timestampsToReturn, object cookie)
        {
            if (readRawModifiedDetails == null)
            {
                throw new ArgumentNullException("readRawModifiedDetails");
            }

            if (nodeToReadId == null)
            {
                throw new ArgumentNullException("nodeToReadId");
            }

            if (CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot History Read Raw while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            List<DataValue> resultDataValues = new List<DataValue>();

            try
            {
                DataValueCollection dataValueCollection = HistoryRead(nodeToReadId, 
                    new ExtensionObject(readRawModifiedDetails), timestampsToReturn, cookie);

                foreach (var dataValue in dataValueCollection)
                {
                    resultDataValues.Add(new DataValue(dataValue));
                }
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.HistoryReadRaw", ex);
                throw new BaseException("Session HistoryReadRaw error", ex);
            }

            return resultDataValues;
        }

        /// <summary>
        /// Performs the history read at time call to the server. If the session is in disconnected state, the method will raise an exception.
        /// </summary>
        /// <param name="nodeToReadId">The node to read id.</param>
        /// <param name="readAtTimeDetails">The read at time argument.</param>
        /// <param name="cookie">The cookie used to identify the call in case of a continuation point reached event.</param>
        /// <returns>The list of data values returned by the server.</returns>
        public virtual List<DataValue> HistoryReadAtTime(NodeId nodeToReadId, ReadAtTimeDetails readAtTimeDetails, TimestampsToReturn timestampsToReturn, object cookie)
        {
            if (readAtTimeDetails == null)
            {
                throw new ArgumentNullException("readAtTimeDetails");
            }

            if (nodeToReadId == null)
            {
                throw new ArgumentNullException("nodeToReadId");
            }

            if (CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot History Read At Time while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            List<DataValue> resultDataValues = new List<DataValue>();

            try
            {
                DataValueCollection dataValueCollection = HistoryRead(nodeToReadId, new ExtensionObject(readAtTimeDetails), timestampsToReturn, cookie);

                foreach (var dataValue in dataValueCollection)
                {
                    resultDataValues.Add(new DataValue(dataValue));
                }
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.HistoryReadAtTime", ex);
                throw new BaseException("Session HistoryReadAtTime error", ex);
            }

            return resultDataValues;
        }

        /// <summary>
        /// Performs the history read at time call to the server. If the session is in disconnected state, the method will raise an exception.
        /// </summary>
        /// <param name="nodeToReadId">The node to read id.</param>
        /// <param name="readProcessedDetails">The read processed argument.</param>
        /// <param name="cookie">The cookie used to identify the call in case of a continuation point reached event.</param>
        /// <returns>The list of data values returned by the server.</returns>
        public virtual List<DataValue> HistoryReadProcessed(NodeId nodeToReadId, ReadProcessedDetails readProcessedDetails, TimestampsToReturn timestampsToReturn, object cookie)
        {
            if (readProcessedDetails == null)
            {
                throw new ArgumentNullException("readProcessedDetails");
            }

            if (nodeToReadId == null)
            {
                throw new ArgumentNullException("nodeToReadId");
            }

            if (CurrentState == State.Disconnected)
            {
                throw new BaseException("Cannot History Read Processed while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            List<DataValue> resultDataValues = new List<DataValue>();

            try
            {
                DataValueCollection dataValueCollection = HistoryRead(nodeToReadId, new ExtensionObject(readProcessedDetails), timestampsToReturn, cookie);

                foreach (var dataValue in dataValueCollection)
                {
                    resultDataValues.Add(new DataValue(dataValue));
                }
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.HistoryReadProcessed", ex);
                throw new BaseException("Session HistoryReadProcessed error", ex);
            }

            return resultDataValues;
        }
        #endregion History

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Reads the specified node and tries to convert the value to the specified enum type value.
        /// </summary>
        /// <param name="nodeToRead">The node to read.</param>
        /// <param name="enumTypeId">The enum type id.</param>
        /// <param name="valueRank">The value rank of the enum.</param>
        /// <returns>A <see cref="DataValue"/> that represents the value of the specified attribute.</returns>
        /// <include file='Doc\Client\Session.xml' path='class[@name="Session"]/method[@name="Read2"]/*'/>
        internal DataValue Read(ReadValueId nodeToRead, NodeId enumTypeId, ValueRanks valueRank)
        {
            DataValue dataValue = Read(nodeToRead);
            //todo refactor for enums
            //if (dataValue != null && enumTypeId != null)
            //{
            //    dataValue.TryConvertToEnumValue(enumTypeId, valueRank, this);
            //}
            return dataValue;
        }

        /// <summary>
        /// Performs operations related to creating internal objects and connecting the session.
        /// </summary>
        /// <param name="targetState">The target state to advance to.</param>
        /// <param name="reconnecting">Whether this is a reconnecting call or not.</param>
        internal override void InternalConnect(State targetState, bool reconnecting)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException("Session");
            }

            //if session is already connected or active - do nothing
            if (CurrentState != State.Disconnected)
            {
                return;
            }

            try
            {
                string logMessage = String.Empty;

                if (reconnecting)
                {
                    if (Reconnect())
                    {
                        bool releaseResources = m_modified;

                        lock (m_reconnectLock)
                        {
                            m_reconnecting = false;
                            m_modified = false;
                        }

                        // Release and dispose reconnected session if local changes were performed.
                        if (releaseResources)
                        {
                            ReleaseDisconnectedSession();
                            
                            TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.InternalConnect", "Connection resources released for session {0}.", this.SessionName);
                        }
                        else
                        {
                            // Reuse the session.
                            m_session = m_disconectedSession;
                            m_sessionId = new NodeId(m_session.SessionId);
                            m_revisedTimeout = (uint)m_session.SessionTimeout;

                            m_session.KeepAlive += OnKeepAlive;
                            m_session.PublishError += OnPublishError;
                            m_browseHandler.SetSession(m_session);

                            m_disconectedSession = null;
                            
                            TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.InternalConnect", "Connection re-established for session {0}.", this.SessionName);

                            return;
                        }
                    }
                }

                // Abandon the reconnect procedure.
                lock (m_reconnectLock)
                {
                    m_reconnecting = false;
                }

                // Release and dispose the lost session if reconnect is not active.
                ReleaseDisconnectedSession();
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.InternalConnect", "Connect procedure started for session {0}.", this.SessionName);

                Uri uri = new Uri(Url);

                EndpointDescription endpointDescription = m_endpointDescription;
                if (endpointDescription == null)
                {
                    endpointDescription = new EndpointDescription();
                    endpointDescription.EndpointUrl = uri.ToString();
                    endpointDescription.SecurityMode = (MessageSecurityMode)SecurityMode;
                    endpointDescription.SecurityPolicyUri = m_securityPolicy;
                    endpointDescription.Server.ApplicationName = uri.AbsolutePath;
                
                    /////todo investigate why this code!!!
                    //if (m_endpointDescription != null && !string.IsNullOrEmpty(m_endpointDescription.DiscoveryEndpointUrl) && m_endpointDescription.EndpointUrl == endpointDescription.EndpointUrl)
                    //{
                    //    // if the wrapped endpoint description is null, use the discovery url from the endpoint description
                    //    endpointDescription.Server.DiscoveryUrls.Add(m_endpointDescription.DiscoveryEndpointUrl);
                    //}

                    // in this case we do not use the m_endpointDescription member; because it was not set or it is different from the url in the constructor
                    if (Url.StartsWith(Utils.UriSchemeOpcTcp, StringComparison.Ordinal))
                    {
                        endpointDescription.TransportProfileUri = Profiles.UaTcpTransport;
                        endpointDescription.Server.DiscoveryUrls.Add(endpointDescription.EndpointUrl);
                    }
                    else if (Url.StartsWith(Utils.UriSchemeHttps, StringComparison.Ordinal))
                    {
                        endpointDescription.TransportProfileUri = Profiles.HttpsBinaryTransport;
                        endpointDescription.Server.DiscoveryUrls.Add(endpointDescription.EndpointUrl);
                    }
                    //else
                    //{
                    //    endpointDescription.TransportProfileUri = Profiles.WsHttpXmlOrBinaryTransport;
                    //    endpointDescription.Server.DiscoveryUrls.Add(endpointDescription.EndpointUrl + "/discovery");
                    //}
                }
                if (m_userIdentity == null)
                {
                    m_userIdentity = new UserIdentity();
                }               

                EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(ApplicationConfiguration);

                endpointConfiguration.UseBinaryEncoding = m_encoding == MessageEncoding.Binary;

                ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
                m_expectedServerEndpoints = GetServerEndpoints(endpoint);

                if (m_endpointDescription == null || m_endpointDescription == null || m_endpointDescription.EndpointUrl != endpointDescription.EndpointUrl)
                {
                    UpdateFromServer(endpoint, m_expectedServerEndpoints, m_userIdentity);

                    endpoint.EndpointUrl = uri;
                }
                //todo handle this check
                //if (endpoint.Configuration.UseBinaryEncoding != endpointConfiguration.UseBinaryEncoding && endpoint.EndpointUrl.Scheme != Uri.UriSchemeHttps)
                //{
                //    ArgumentException exception = new ArgumentException("The specified message encoding is not supported by the current endpoint configuration.");
                //    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.InternalConnect", exception);

                //    throw exception;
                //}
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.InternalConnect", "Connecting to {0}.", endpoint.EndpointUrl);

                try
                {
                    m_session = Opc.Ua.Client.Session.Create(ApplicationConfiguration,
                        endpoint,
                        false,
                        CheckDomain,
                        SessionName,
                        m_timeout,
                        m_userIdentity,
                        Locales).Result; //todo check why parameter , m_expectedServerEndpoints is not needed anymore
                }
                catch (ServiceResultException ex)
                {
                    if (ex.StatusCode == StatusCodes.BadSecurityChecksFailed)
                    {
                        // re-attempt to connect without server endpoints validation.
                        m_session = Opc.Ua.Client.Session.Create(ApplicationConfiguration,
                            endpoint,
                            false,
                            CheckDomain,
                            SessionName,
                            m_timeout,
                            m_userIdentity,
                            Locales).Result;
                    }
                    else
                    {
                        throw ex;
                    }
                }

                m_sessionId = new NodeId(m_session.SessionId);

                if (m_sessionId.IsNullNodeId)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.InternalConnect", "Session ID is null for Session:{0}" , SessionName);
                }
                //todo investigate what is it with duplicate session id
                //Session sessionDuplicate = Application.CurrentSessions.FirstOrDefault(t => t.Id != null && t.Id.Equals(Id) && t != this);

                //if (sessionDuplicate != null)
                //{
                //    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.InternalConnect", "Duplicate Session ID for Session: {0} and Session: {1}", sessionDuplicate.SessionName, SessionName);
                //}

                //m_session.ReturnDiagnostics = DiagnosticsMasks.All;

                // update the session in the browse handler helper class
                m_browseHandler.SetSession(m_session);

                m_session.KeepAliveInterval = m_keepAliveInterval;
                //todo handle this field missing
               // m_session.KeepAliveTimeout = m_keepAliveTimeout;
                m_session.KeepAlive += OnKeepAlive;
                m_session.PublishError += OnPublishError;
                m_revisedTimeout = (uint)m_session.SessionTimeout;
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.InternalConnect", "Current state set to {0} for session {1}.", targetState, this.SessionName);
            }
            catch (ServiceResultException sre)
            {
                if (sre.StatusCode == StatusCodes.BadCertificateUntrusted)
                {
                    try
                    {
                        Disconnect(false);
                    }
                    catch
                    {
                    }
                }
                else
                {
                    EnableReconnectHandler();
                }
                
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.InternalConnect", sre, "Session Connect error ");

                throw new BaseException(string.Format("Session Connect error for session \"{0}\"", SessionName), sre);
            }
            catch (Exception ex)
            {
                EnableReconnectHandler(); //todo investigate why enable reconnect handler here????
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.InternalConnect",  ex);
                throw new BaseException($"Session Connect error for session \"{SessionName}\"", ex);
            }
        }

        /// <summary>
        /// Performs operations related to disconnecting the session and releasing any acquired resources.
        /// </summary>
        /// <param name="reconnecting">Whether this is a reconnecting call or not.</param>
        internal override void InternalDisconnect(bool reconnecting)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException("Session");
            }

            try
            {
                m_session.PublishError -= OnPublishError;
                m_session.KeepAlive -= OnKeepAlive;
                StatusCode statusCode = m_session.Close();
                if (StatusCode.IsUncertain(statusCode))
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.InternalDisconnect", "A warning appeared at close session: {0}" , statusCode);
                }
                else if (StatusCode.IsBad(statusCode))
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.InternalDisconnect", "An error appeared at close session: {0}" , statusCode);
                }
                m_session = null;
                m_sessionId = null;
                m_browseHandler.SetSession(m_session);

                lock (m_reconnectLock)
                {
                    m_reconnecting = false;
                    m_modified = false;
                }
                
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.InternalDisconnect", "Current state set to {0} for session {1}.", State.Disconnected, this.SessionName);
            }
            catch (Exception ex)
            {
                EnableReconnectHandler();
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.InternalDisconnect",  ex);
            }
        }

        internal override void EnableReconnectHandler()
        {
            if (m_reconnectTimer == null)
            {
                m_reconnectTimer = new System.Threading.Timer(PerformStateTransition, null, ReconnectTimerDelay, System.Threading.Timeout.Infinite);
            }
        }

        internal override void DisableReconnectHandler()
        {
            if (m_reconnectTimer != null)
            {
                m_reconnectTimer.Dispose();
                m_reconnectTimer = null;
            }
        }

        internal override void SetModified()
        {
            lock (m_reconnectLock)
            {
                m_modified = true;
            }
        }

        /// <summary>
        /// Gets the children list.
        /// </summary>
        /// <returns>A list of Subscriptions as BaseStateManagement objects.</returns>
        internal override List<BaseStateManagement> GetChildren()
        {
            lock (((ICollection)m_subscriptions).SyncRoot)
            {
                return new List<BaseStateManagement>(m_subscriptions.ToArray());
            }
        }

        /// <summary>
        /// Adds a subscription.
        /// </summary>
        /// <param name="subscription">The subscription to be added.</param>
        internal virtual void AddSubscription(Subscription subscription)
        {
            lock (((ICollection)m_subscriptions).SyncRoot)
            {
                m_subscriptions.Add(subscription);
            }

            SetModified();
        }

        /// <summary>
        /// Removes a subscription.
        /// </summary>
        /// <param name="subscription">The subscription to be removed.</param>
        internal virtual void RemoveSubscription(Subscription subscription)
        {
            lock (((ICollection)m_subscriptions).SyncRoot)
            {
                m_subscriptions.Remove(subscription);
            }

            SetModified();
        }

        /// <summary>
        /// Adds a method.
        /// </summary>
        /// <param name="method">The method to be added.</param>
        internal virtual void AddMethod(Method method)
        {
            lock (m_methods)
            {
                m_methods.Add(method);
            }
        }

        /// <summary>
        /// Removes a method.
        /// </summary>
        /// <param name="method">The method to be removed.</param>
        internal virtual void RemoveMethod(Method method)
        {
            lock (m_methods)
            {
                m_methods.Remove(method);
            }
        }

        private void RaiseSessionNameChanging(PropertyChangingEventArgs e)
        {
            if (SessionNameChanging == null)
            {
                return;
            }
            try
            {
                SessionNameChanging(this, e);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.RaiseSessionNameChanging", ex);
            }
        }

        private void RaiseDisposing(EventArgs e)
        {
            if (Disposing == null)
            {
                return;
            }

            try
            {
                Disposing(this, e);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.RaiseDisposing", ex);
            }
        }


        internal void RaiseContinuationPointReached(BrowseEventArgs e)
        {
            if (ContinuationPointReached == null)
            {
                return;
            }

            try
            {
                ContinuationPointReached(this, e);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.RaiseContinuationPointReached", ex);
            }
        }

        internal void RaiseHistoryReadContinuationReached(HistoryReadContinuationEventArgs e)
        {
            if (HistoryContinuationPointReached == null)
            {
                return;
            }

            try
            {
                HistoryContinuationPointReached(this, e);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.RaiseHistoryReadContinuationReached", ex);
            }
        }

        internal void RaisePublishError(PublishErrorEventArgs e)
        {
            if (PublishError == null)
            {
                return;
            }

            try
            {
                PublishError(this, e);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.RaisePublishError",  ex);
            }
        }

        internal void RaiseKeepAlive(KeepAliveEventArgs e)
        {
            try
            {
                if (KeepAlive != null)
                {
                    KeepAlive(this, e);
                }
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.RaiseKeepAlive",  ex);
            }
        }

        #endregion Internal Methods

        #region Protected Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.<br/>
        /// Discharges unmanaged objects (e.g. the socket) held by the SDK Core Session object aggregated by this Session instance.
        /// </summary>
        /// <param name="disposing">Boolean parameter indicating whether the method was invoked from the IDisposable.Dispose implementation or from the finalizer. The finalizer should call this method with [false]. The resources are disposed if called with [true].</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Disconnect(true);
                    }
                    catch (Exception ex)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Dispose", ex);
                    }

                    RaiseDisposing(new EventArgs());

                    DisableReconnectHandler();

                    lock (m_browseRequestSenders)
                    {
                        m_browseRequestSenders.Clear();
                    }
                }

                m_disposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static void InitBaseAttributes(SortedDictionary<AttributeId, DataValue> attributes, int? nodeClass, StatusCode nodeClassStatusCode, BaseNode node)
        {

            node.NodeClass = (NodeClass)nodeClass.Value;
            node.SetAttibuteStatusCode(AttributeId.NodeClass, nodeClassStatusCode);

            // NodeId Attribute
            DataValue value = attributes[AttributeId.NodeId];

            if (value != null)
            {
                node.NodeId = new NodeId((NodeId)value.GetValue(typeof(NodeId)));
                node.SetAttibuteStatusCode(AttributeId.NodeId, value.StatusCode);
            }
            else
            {
                node.SetAttibuteStatusCode(AttributeId.NodeId, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // BrowseName Attribute
            value = attributes[AttributeId.BrowseName];

            if (value != null)
            {
                node.BrowseName = new QualifiedName((QualifiedName)value.GetValue(typeof(QualifiedName)));
                node.SetAttibuteStatusCode(AttributeId.BrowseName, value.StatusCode);
            }
            else
            {
                node.SetAttibuteStatusCode(AttributeId.BrowseName, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // DisplayName Attribute
            value = attributes[AttributeId.DisplayName];
            if (value != null)
            {
                node.DisplayName = new LocalizedText((LocalizedText)value.GetValue(typeof(LocalizedText)));
                node.SetAttibuteStatusCode(AttributeId.DisplayName, value.StatusCode);
            }
            else
            {
                node.SetAttibuteStatusCode(AttributeId.DisplayName, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // Description Attribute
            value = attributes[AttributeId.Description];

            if (value != null)
            {
                node.Description = new LocalizedText((LocalizedText)value.GetValue(typeof(LocalizedText)));
                node.SetAttibuteStatusCode(AttributeId.Description, value.StatusCode);
            }
            else
            {
                node.SetAttibuteStatusCode(AttributeId.Description, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // WriteMask Attribute
            value = attributes[AttributeId.WriteMask];

            if (value != null && value.Value != null)
            {
                node.WriteMask = (uint)value.GetValue(typeof(uint));
                node.SetAttibuteStatusCode(AttributeId.WriteMask, value.StatusCode);
            }

            // UserWriteMask Attribute
            value = attributes[AttributeId.UserWriteMask];

            if (value != null && value.Value != null)
            {
                node.UserWriteMask = (uint)value.GetValue(typeof(uint));
                node.SetAttibuteStatusCode(AttributeId.UserWriteMask,value.StatusCode);
            }
        }

        private void PerformStateTransition(object dummy)
        {
            try
            {
                base.PerformStateTransition(m_reconnecting);
                RaiseOnStateChanged();
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.PerformStateTransition",  ex);
            }
        }

        /// <summary>
        /// Reconnects to the server.
        /// </summary>
        private bool Reconnect()
        {
            // Try a reconnect.
            if (m_reconnecting && m_disconectedSession != null)
            {
                try
                {
                    m_disconectedSession.Reconnect();

                    // Connection re-established. Monitored items should start updating on their own.
                    return true;
                }
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.Reconnect", ex, "Could not reconnect the session {0}", SessionName);

                    // Check if the error occured due to a network interruption and reconnecting is stil an option.
                    if (IsNetworkError(ex, m_disconectedSession.ConfiguredEndpoint.EndpointUrl.Scheme))
                    {
                        // Check if timeout interval has elapsed.
                        if (m_disconectedSession.LastKeepAliveTime.AddMilliseconds(m_disconectedSession.SessionTimeout) > DateTime.UtcNow)
                        {
                            // Reconnect remains still active because the server is not reachable yet.
                            throw ex;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether the exception is a network error for the specified protocol.
        /// </summary>
        /// <returns>True if a network error, false otherwise</returns>
        private bool IsNetworkError(Exception exception, string urlScheme)
        {
            // Check for specific errors caused by unreachable servers.
            ServiceResultException serviceException = exception as ServiceResultException;

            switch (urlScheme)
            {
                case Utils.UriSchemeOpcTcp:
                    {
                        if ((serviceException != null && serviceException.StatusCode == StatusCodes.BadTcpInternalError) ||
                            exception is SocketException)
                        {
                            return true;
                        }

                        break;
                    }
                    //todo http removed
                //case Utils.UriSchemeHttp:
                //    {
                //        if ((serviceException != null && serviceException.StatusCode == StatusCodes.BadCommunicationError) ||
                //            exception is CommunicationException || exception is WebException)
                //        {
                //            return true;
                //        }

                //        break;
                //    }

                case Utils.UriSchemeHttps:
                    {
                        if ((serviceException != null && serviceException.StatusCode == StatusCodes.BadCommunicationError) ||
                            exception is EndpointNotFoundException)//todo find fix || exception is WebException)
                        {
                            return true;
                        }

                        break;
                    }

                default:
                    {
                        break;
                    }
            }

            return false;
        }

        /// <summary>
        /// Release the disconnected session.
        /// </summary>
        private void ReleaseDisconnectedSession()
        {
            if (m_disconectedSession != null)
            {
                try
                {
                    m_disconectedSession.Close();
                    m_disconectedSession.Dispose();
                    m_disconectedSession = null;
                }
                catch
                {
                    // Ignore errors.
                }
            }
        }

        private void UpdateFromServer(ConfiguredEndpoint endpoint, EndpointDescriptionCollection serverEndpoints, UserIdentity identity)
        {
            DiscoveryClient client = null;

            try
            {
                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.UpdateFromServer", "Updating endpoint description from server.");

                // check if the endpointUrl can be parsed
                if (endpoint.EndpointUrl == null)
                {
                    Uri endpointUrl = Utils.ParseUri(endpoint.Description.EndpointUrl);

                    if (endpointUrl == null)
                    {
                       throw new BaseException("The specified server endpoint is not a valid Uri", StatusCodes.BadServerUriInvalid);
                    }
                }

                // get the a discovery url.
                Uri discoveryUrl = endpoint.GetDiscoveryUrl(endpoint.EndpointUrl);
                EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(ApplicationConfiguration);
                endpointConfiguration.OperationTimeout = ApplicationConfiguration.DiscoveryOperationTimeout;

                // create the discovery client.
                client = DiscoveryClient.Create(discoveryUrl, endpointConfiguration);

                // get the endpoints.
                EndpointDescriptionCollection collection = serverEndpoints;

                if ((collection == null || collection.Count == 0) && endpoint.Description.SecurityMode != MessageSecurityMode.None)
                {
                    BaseException exception = new BaseException("The Server does not have any endpoints defined", StatusCodes.BadUnknownResponse);

                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.UpdateFromServer",  exception);

                    throw exception;
                }

                EndpointDescription match = null;

                if (collection != null)
                {
                    foreach (EndpointDescription description in collection)
                    {
                        // check for match on security policy.
                        if (endpoint.Description.SecurityPolicyUri != description.SecurityPolicyUri)
                        {
                            continue;
                        }

                        // check for match on security mode.
                        if (endpoint.Description.SecurityMode != description.SecurityMode)
                        {
                            continue;
                        }

                        // parse the endpoint url.
                        Uri sessionUrl = Utils.ParseUri(description.EndpointUrl);
                        if (sessionUrl == null)
                        {
                            continue;
                        }

                        // check for matching protocol.
                        if (sessionUrl.Scheme != endpoint.EndpointUrl.Scheme)
                        {
                            continue;
                        }

                        // check for matching port number.
                        if (sessionUrl.Port != endpoint.EndpointUrl.Port)
                        {
                            continue;
                        }

                        if (description.UserIdentityTokens != null
                            && description.UserIdentityTokens.FirstOrDefault(u => u.TokenType == identity.TokenType) == null)
                        {
                            continue;
                        }

                        match = description;
                        break;
                    }
                }

                // update the endpoint.
                if (match == null)
                {
                    if (endpoint.Description.SecurityMode != MessageSecurityMode.None)
                    {
                        BaseException exception = new BaseException("The UA Server does not support the requested endpoint description");
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.UpdateFromServer", exception);
                        throw exception;
                    }
                }
                else
                {
                    endpoint.Update(match);
                    m_endpointDescription = match;
                }
            }
            finally
            {
                if (client != null)
                {
                    client.Close();
                }
            }
        }

        private EndpointDescriptionCollection GetServerEndpoints(ConfiguredEndpoint endpoint)
        {
            DiscoveryClient client = null;

            try
            {
                // get the a discovery url.
                Uri discoveryUrl = endpoint.GetDiscoveryUrl(endpoint.EndpointUrl);
                EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(ApplicationConfiguration);
                endpointConfiguration.OperationTimeout = ApplicationConfiguration.DiscoveryOperationTimeout;

                // create the discovery client.
                client = DiscoveryClient.Create(discoveryUrl, endpointConfiguration);

                // get the endpoints.
                return client.GetEndpoints(null);
            }
            catch (Exception exception)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.GetServerEndpoints", exception);

                throw;
            }
            finally
            {
                if (client != null)
                {
                    client.Close();
                }
            }
        }

        private void MoreReferences(Browser browser, BrowserEventArgs e)
        {
            e.Cancel = false;

            try
            {
                if (browser != null)
                {
                    // try to identify the sender of the browse request and send the "MoreReferences" notification
                    object sender = null;
                    lock (m_browseRequestSenders)
                    {
                        m_browseRequestSenders.TryGetValue(browser, out sender);
                    }

                    if (sender != null)
                    {
                        TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.Browse",
                            "Browse operation retrieved {0} results so far.", (e.References != null ? e.References.Count : 0));

                        if (ContinuationPointReached != null)
                        {
                            BrowseEventArgs args = new BrowseEventArgs();
                            ContinuationPointReached(sender, args);
                            TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.MoreReferences", "Continuation Point Reached.");
                            // set the option to cancel browse operation
                            e.Cancel = args.Cancel;

                            if (e.Cancel)
                            {
                                TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.MoreReferences", "Browse operation canceled by user.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.MoreReferences", ex);
            }
        }

        /// <summary>
        /// Returns the name for a reference. This is useful when Browsing.
        /// </summary>
        /// <param name="referenceTypeId">The identifier for the node.</param>
        /// <param name="isForward">The direction when following the reference. For a direction from source to target this parameter is true. </param>
        /// <returns>The name of the specified node as a <see cref="String"/></returns>
        private string FindReferenceTypeName(NodeId referenceTypeId, bool isForward)
        {
            if (m_session == null)
            {
                throw new BaseException("Cannot find reference while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            try
            {
                ReferenceTypeNode typeNode = m_session.NodeCache.Find(referenceTypeId) as ReferenceTypeNode;

                if (typeNode != null)
                {
                    if (!isForward && typeNode.InverseName != null)
                    {
                        return typeNode.InverseName.Text;
                    }
                    else
                    {
                        return typeNode.DisplayName.Text;
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Finds the display name of the specified node.
        /// </summary>
        /// <param name="nodeId">The node id.</param>
        /// <returns>The name of the specified node as a <see cref="String"/></returns>
        private string FindNodeDisplayName(NodeId nodeId)
        {
            if (m_session == null)
            {
                throw new BaseException("Cannot find node disaplay name while in the Disconnected state", StatusCodes.BadInvalidState);
            }

            try
            {
                Node node = m_session.NodeCache.Find(nodeId) as Node;

                if (node != null)
                {
                    if (node.DisplayName != null)
                    {
                        return node.DisplayName.Text;
                    }
                }

                return nodeId.ToString();
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.FindNodeDisplayName", ex);
                return null;
            }
        }

        /// <summary>
        /// Reads the values for the node attributes and returns a node object. The value attribute is returned as well.
        /// </summary>
        /// <param name="nodeId">The node id.</param>
        /// <param name="session">Current SDK session.</param>
        /// <returns>A <see cref="BaseNode"/> with all attributes read.</returns>
        private BaseNode ReadNodeComplete(NodeId nodeId, SessionClient session)
        {
            // build list of attributes.
            SortedDictionary<AttributeId, DataValue> attributes = new SortedDictionary<AttributeId, DataValue>();

            attributes.Add(AttributeId.NodeId, null);
            attributes.Add(AttributeId.NodeClass, null);
            attributes.Add(AttributeId.BrowseName, null);
            attributes.Add(AttributeId.DisplayName, null);
            attributes.Add(AttributeId.Description, null);
            attributes.Add(AttributeId.WriteMask, null);
            attributes.Add(AttributeId.UserWriteMask, null);
            attributes.Add(AttributeId.Value, null);
            attributes.Add(AttributeId.DataType, null);
            attributes.Add(AttributeId.ValueRank, null);
            attributes.Add(AttributeId.ArrayDimensions, null);
            attributes.Add(AttributeId.AccessLevel, null);
            attributes.Add(AttributeId.UserAccessLevel, null);
            attributes.Add(AttributeId.Historizing, null);
            attributes.Add(AttributeId.MinimumSamplingInterval, null);
            attributes.Add(AttributeId.EventNotifier, null);
            attributes.Add(AttributeId.Executable, null);
            attributes.Add(AttributeId.UserExecutable, null);
            attributes.Add(AttributeId.IsAbstract, null);
            attributes.Add(AttributeId.InverseName, null);
            attributes.Add(AttributeId.Symmetric, null);
            attributes.Add(AttributeId.ContainsNoLoops, null);

            // build list of values to read.
            ReadValueIdCollection itemsToRead = new ReadValueIdCollection();

            foreach (uint attributeId in attributes.Keys)
            {
                ReadValueId itemToRead = new ReadValueId();

                itemToRead.NodeId = nodeId;
                itemToRead.AttributeId = attributeId;

                itemsToRead.Add(itemToRead);
            }

            // read from server.
            DataValueCollection values = null;
            DiagnosticInfoCollection diagnosticInfos = null;

            ResponseHeader responseHeader = session.Read(
                null,
                0,
                TimestampsToReturn.Both,
                itemsToRead,
                out values,
                out diagnosticInfos);

            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            // process results.
            int? nodeClass = null;
            StatusCode nodeClassStatusCode = new StatusCode();
            for (int ii = 0; ii < itemsToRead.Count; ii++)
            {
                uint attributeId = itemsToRead[ii].AttributeId;
                attributes[(AttributeId)attributeId] = values[ii];

                // the node probably does not exist if the node class is not found.
                if (attributeId == (uint)AttributeId.NodeClass)
                {
                    // check for good status code.
                    if (StatusCode.IsNotGood(values[ii].StatusCode))
                    {
                        throw new ServiceResultException(values[ii].StatusCode.Code);
                    }

                    // check for valid node class.
                    nodeClass = values[ii].Value as int?;
                    nodeClassStatusCode = values[ii].StatusCode;

                    if (nodeClass == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Node does not have a valid value for NodeClass {0}.", values[ii].Value);
                    }
                }
            }

            BaseNode node = null;
            DataValue value = null;

            switch ((NodeClass)nodeClass.Value)
            {
                default:
                    {
                        if (attributes[AttributeId.Value] != null && attributes[AttributeId.Value].Value != null)
                        {
                            node = ReadVariableNode(attributes, nodeClass, nodeClassStatusCode);
                        }
                        else
                        {
                            node = ReadObjectNode(attributes, nodeClass, nodeClassStatusCode);
                        }
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.ReadNodeComplete", "Node does not have a valid value for NodeClass {0}" , nodeClass.Value);
                        break;
                    }
                case NodeClass.Object:
                    {
                        node = ReadObjectNode(attributes, nodeClass, nodeClassStatusCode);
                        break;
                    }

                case NodeClass.ObjectType:
                    {
                        ToolkitObjectTypeNode objectTypeNode = new ToolkitObjectTypeNode();
                        InitBaseAttributes(attributes, nodeClass, nodeClassStatusCode, objectTypeNode);

                        value = attributes[AttributeId.IsAbstract];
                        if (value != null && value.Value != null)
                        {
                            objectTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));
                            objectTypeNode.SetAttibuteStatusCode(AttributeId.IsAbstract, value.StatusCode);
                        }
                        else
                        {
                            objectTypeNode.SetAttibuteStatusCode(AttributeId.IsAbstract, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }
                        node = objectTypeNode;
                        break;
                    }

                case NodeClass.Variable:
                    {
                        node = ReadVariableNode(attributes, nodeClass, nodeClassStatusCode);
                        break;
                    }

                case NodeClass.VariableType:
                    {
                        ToolkitVariableTypeNode variableTypeNode = new ToolkitVariableTypeNode();
                        InitBaseAttributes(attributes, nodeClass, nodeClassStatusCode, variableTypeNode);

                        // IsAbstract Attribute
                        value = attributes[AttributeId.IsAbstract];
                        if (value != null && value.Value != null)
                        {
                            variableTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));
                            variableTypeNode.SetAttibuteStatusCode(AttributeId.IsAbstract, value.StatusCode);
                        }
                        else
                        {
                            variableTypeNode.SetAttibuteStatusCode(AttributeId.IsAbstract, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }

                        // DataType Attribute
                        value = attributes[AttributeId.DataType];
                        if (value != null && value.Value != null)
                        {
                            variableTypeNode.DataTypeId = new NodeId((NodeId)value.GetValue(typeof(NodeId)));
                            variableTypeNode.SetAttibuteStatusCode(AttributeId.DataType, value.StatusCode);
                            variableTypeNode.DataType = FindNodeDisplayName(variableTypeNode.DataTypeId);
                        }
                        else
                        {
                            variableTypeNode.SetAttibuteStatusCode(AttributeId.DataType, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }

                        // ValueRank Attribute
                        value = attributes[AttributeId.ValueRank];

                        if (value != null && value.Value != null)
                        {
                            variableTypeNode.ValueRank = (ValueRanks)(int)value.GetValue(typeof(int));
                            variableTypeNode.SetAttibuteStatusCode(AttributeId.ValueRank, value.StatusCode);
                        }
                        else
                        {
                            variableTypeNode.SetAttibuteStatusCode(AttributeId.ValueRank, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }

                        value = attributes[AttributeId.Value];
                        if (value != null)
                        {
                            variableTypeNode.Value = new DataValue(value);
                            variableTypeNode.SetAttibuteStatusCode(AttributeId.Value, value.StatusCode);
                            //todo check this region - also duplicate code!!!!
                            // check to see if it's an enumerated type
                            //if (variableTypeNode.DataTypeId != null && value.WrappedValue.TypeInfo != null && value.WrappedValue.TypeInfo.BuiltInType == BuiltInType.Int32)
                            //{
                            //    if (variableTypeNode.ValueRank < 0)
                            //    {
                            //        if (value.Value is int)
                            //        {
                            //            try
                            //            {
                            //                EnumValue enumValue = Softing.Toolkit.Argument.GetDefaultValueForDatatype(variableTypeNode.DataTypeId, variableTypeNode.ValueRank, this) as EnumValue;
                            //                if (enumValue == null)
                            //                {
                            //                    Type type = Softing.Toolkit.Argument.GetSystemType(variableTypeNode.DataTypeId, Factory);
                            //                    if (type != null && type.IsEnum)
                            //                    {
                            //                        enumValue = new EnumValue(type);
                            //                    }
                            //                }
                            //                if (enumValue != null)
                            //                {
                            //                    enumValue.Value = (int)value.Value;
                            //                    variableTypeNode.Value.Value = enumValue;
                            //                }
                            //            }
                            //            catch (NotSupportedException ex)
                            //            {
                            //                TraceService.Log(TraceMasks.Warning, TraceSources.ClientAPI, "Session.ReadNode", ex.Message, ex);
                            //            }
                            //        }
                            //    }
                            //    else
                            //    {
                            //        int[] intValues = value.Value as int[];
                            //        if (intValues != null)
                            //        {
                            //            Type type = Softing.Toolkit.Argument.GetSystemType(variableTypeNode.DataTypeId, Factory);
                            //            if (type != null && type.IsEnum)
                            //            {
                            //                EnumValue[] enumValueArray = new EnumValue[intValues.Length];
                            //                for (int i = 0; i < intValues.Length; i++)
                            //                {
                            //                    enumValueArray[i] = new EnumValue(type);
                            //                    enumValueArray[i].Value = intValues[i];
                            //                }

                            //                variableTypeNode.Value.Value = enumValueArray;
                            //            }
                            //            else
                            //            {
                            //                try
                            //                {
                            //                    EnumValue enumValue = Softing.Toolkit.Argument.GetDefaultValueForDatatype(variableTypeNode.DataTypeId, ValueRanks.Scalar, this) as EnumValue;
                            //                    if (enumValue != null)
                            //                    {
                            //                        EnumValue[] enumValueArray = new EnumValue[intValues.Length];
                            //                        for (int i = 0; i < intValues.Length; i++)
                            //                        {
                            //                            enumValueArray[i] = enumValue.Clone();
                            //                            enumValueArray[i].Value = intValues[i];
                            //                        }

                            //                        variableTypeNode.Value.Value = enumValueArray;
                            //                    }
                            //                }
                            //                catch (NotSupportedException ex)
                            //                {
                            //                    TraceService.Log(TraceMasks.Warning, TraceSources.ClientAPI, "Session.ReadNode", ex.Message, ex);
                            //                }
                            //            }
                            //        }
                            //    }
                            //}

                            //variableTypeNode.Value.DataType = variableTypeNode.DataTypeId;
                            //variableTypeNode.Value.ValueRank = variableTypeNode.ValueRank;
                        }

                        // ArrayDimensions Attribute
                        value = attributes[AttributeId.ArrayDimensions];

                        if (value != null && value.Value != null)
                        {
                            variableTypeNode.ArrayDimensions = new List<uint>((uint[])value.GetValue(typeof(uint[])));
                            variableTypeNode.SetAttibuteStatusCode(AttributeId.ArrayDimensions, value.StatusCode);
                        }

                        node = variableTypeNode;
                        break;
                    }

                case NodeClass.Method:
                    {
                        ToolkitMethodNode methodNode = new ToolkitMethodNode();
                        InitBaseAttributes(attributes, nodeClass, nodeClassStatusCode, methodNode);

                        // Executable Attribute
                        value = attributes[AttributeId.Executable];

                        if (value != null && value.Value != null)
                        {
                            methodNode.Executable = (bool)value.GetValue(typeof(bool));
                            methodNode.SetAttibuteStatusCode(AttributeId.Executable,value.StatusCode);
                        }
                        else
                        {
                            methodNode.SetAttibuteStatusCode(AttributeId.Executable, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }

                        // UserExecutable Attribute
                        value = attributes[AttributeId.UserExecutable];
                        if (value != null && value.Value != null)
                        {
                            methodNode.UserExecutable = (bool)value.GetValue(typeof(bool));
                            methodNode.SetAttibuteStatusCode(AttributeId.UserExecutable, value.StatusCode);
                        }
                        else
                        {
                            methodNode.SetAttibuteStatusCode(AttributeId.UserExecutable, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }
                        // read the arguments of the method
                        methodNode.ReadArguments(this);

                        node = methodNode;
                        break;
                    }

                case NodeClass.DataType:
                    {
                        ToolkitDataTypeNode dataTypeNode = new ToolkitDataTypeNode();
                        InitBaseAttributes(attributes, nodeClass, nodeClassStatusCode, dataTypeNode);

                        // IsAbstract Attribute
                        value = attributes[AttributeId.IsAbstract];

                        if (value != null && value.Value != null)
                        {
                            dataTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));
                            dataTypeNode.SetAttibuteStatusCode(AttributeId.IsAbstract, value.StatusCode);
                        }
                        else
                        {
                            dataTypeNode.SetAttibuteStatusCode(AttributeId.IsAbstract, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }
                        node = dataTypeNode;
                        break;
                    }

                case NodeClass.ReferenceType:
                    {
                        ToolkitReferenceTypeNode referenceTypeNode = new ToolkitReferenceTypeNode();
                        InitBaseAttributes(attributes, nodeClass, nodeClassStatusCode, referenceTypeNode);

                        // IsAbstract Attribute
                        value = attributes[AttributeId.IsAbstract];

                        if (value != null && value.Value != null)
                        {
                            referenceTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));
                            referenceTypeNode.SetAttibuteStatusCode(AttributeId.IsAbstract, value.StatusCode);
                        }
                        else
                        {
                            referenceTypeNode.SetAttibuteStatusCode(AttributeId.IsAbstract, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }

                        // Symmetric Attribute
                        value = attributes[AttributeId.Symmetric];

                        if (value != null && value.Value != null)
                        {
                            referenceTypeNode.Symmetric = (bool)value.GetValue(typeof(bool));
                            referenceTypeNode.SetAttibuteStatusCode(AttributeId.Symmetric, value.StatusCode);
                        }
                        else
                        {
                            referenceTypeNode.SetAttibuteStatusCode(AttributeId.Symmetric, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }

                        // InverseName Attribute
                        value = attributes[AttributeId.InverseName];

                        if (value != null)
                        {
                            referenceTypeNode.InverseName = new LocalizedText((LocalizedText)value.GetValue(typeof(LocalizedText)));
                            referenceTypeNode.SetAttibuteStatusCode(AttributeId.InverseName, value.StatusCode);
                        }
                        else
                        {
                            referenceTypeNode.SetAttibuteStatusCode(AttributeId.InverseName, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }

                        node = referenceTypeNode;
                        break;
                    }

                case NodeClass.View:
                    {
                        ToolkitViewNode viewNode = new ToolkitViewNode();
                        InitBaseAttributes(attributes, nodeClass, nodeClassStatusCode, viewNode);

                        // EventNotifier Attribute
                        value = attributes[AttributeId.EventNotifier];

                        if (value != null && value.Value != null)
                        {
                            viewNode.EventNotifier = (byte)value.GetValue(typeof(byte));
                            viewNode.SetAttibuteStatusCode(AttributeId.EventNotifier, value.StatusCode);
                        }
                        else
                        {
                            viewNode.SetAttibuteStatusCode(AttributeId.EventNotifier, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }

                        // ContainsNoLoops Attribute
                        value = attributes[AttributeId.ContainsNoLoops];

                        if (value != null && value.Value != null)
                        {
                            viewNode.ContainsNoLoops = (bool)value.GetValue(typeof(bool));
                            viewNode.SetAttibuteStatusCode(AttributeId.ContainsNoLoops, value.StatusCode);
                        }
                        else
                        {
                            viewNode.SetAttibuteStatusCode(AttributeId.ContainsNoLoops, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
                        }

                        node = viewNode;
                        break;
                    }
            }

            return node;
        }

        private ToolkitVariableNode ReadVariableNode(SortedDictionary<AttributeId, DataValue> attributes, int? nodeClass, StatusCode nodeClassStatusCode)
        {
            ToolkitVariableNode variableNode = new ToolkitVariableNode();
            InitBaseAttributes(attributes, nodeClass, nodeClassStatusCode, variableNode);

            // DataType Attribute
            DataValue value = attributes[AttributeId.DataType];
            if (value != null && value.Value != null)
            {
                variableNode.DataTypeId = new NodeId((NodeId)value.GetValue(typeof(NodeId)));
                variableNode.SetAttibuteStatusCode(AttributeId.DataType, value.StatusCode);
                variableNode.DataType = FindNodeDisplayName(variableNode.DataTypeId);
            }
            else
            {
                variableNode.SetAttibuteStatusCode(AttributeId.DataType, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // ValueRank Attribute
            value = attributes[AttributeId.ValueRank];
            if (value != null && value.Value != null)
            {
                variableNode.ValueRank = (ValueRanks)(int)value.GetValue(typeof(int));
                variableNode.SetAttibuteStatusCode(AttributeId.ValueRank, value.StatusCode);
            }
            else
            {
                variableNode.SetAttibuteStatusCode(AttributeId.ValueRank, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // Value Attribute
            value = attributes[AttributeId.Value];
            if (value != null)
            {
                variableNode.Value = new DataValue(value);
                variableNode.SetAttibuteStatusCode(AttributeId.Value, value.StatusCode);
                //todo check wnat to do with enum types
                // check to see if it's an enumerated type
                //if (variableNode.DataTypeId != null && value.WrappedValue.TypeInfo != null && value.WrappedValue.TypeInfo.BuiltInType == BuiltInType.Int32)
                //{
                //    if (variableNode.ValueRank < 0)
                //    {
                //        if (value.Value is int)
                //        {
                //            try
                //            {
                //                EnumValue enumValue = Softing.Toolkit.Argument.GetDefaultValueForDatatype(variableNode.DataTypeId, variableNode.ValueRank, this) as EnumValue;
                //                if (enumValue == null)
                //                {
                //                    Type type = Softing.Toolkit.Argument.GetSystemType(variableNode.DataTypeId, Factory);
                //                    if (type != null && type.IsEnum)
                //                    {
                //                        enumValue = new EnumValue(type);
                //                    }
                //                }
                //                if (enumValue != null)
                //                {
                //                    enumValue.Value = (int)value.Value;
                //                    variableNode.Value.Value = enumValue;
                //                }
                //            }
                //            catch (NotSupportedException ex)
                //            {
                //                TraceService.Log(TraceMasks.Warning, TraceSources.ClientAPI, "Session.ReadNode", ex.Message, ex);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        int[] intValues = value.Value as int[];
                //        if (intValues != null)
                //        {
                //            Type type = Softing.Toolkit.Argument.GetSystemType(variableNode.DataTypeId, Factory);
                //            if (type != null && type.IsEnum)
                //            {
                //                EnumValue[] enumValueArray = new EnumValue[intValues.Length];
                //                for (int i = 0; i < intValues.Length; i++)
                //                {
                //                    enumValueArray[i] = new EnumValue(type);
                //                    enumValueArray[i].Value = intValues[i];
                //                }

                //                variableNode.Value.Value = enumValueArray;
                //            }
                //            else
                //            {
                //                try
                //                {
                //                    EnumValue enumValue = Softing.Toolkit.Argument.GetDefaultValueForDatatype(variableNode.DataTypeId, ValueRanks.Scalar, this) as EnumValue;
                //                    if (enumValue != null)
                //                    {
                //                        EnumValue[] enumValueArray = new EnumValue[intValues.Length];
                //                        for (int i = 0; i < intValues.Length; i++)
                //                        {
                //                            enumValueArray[i] = enumValue.Clone();
                //                            enumValueArray[i].Value = intValues[i];
                //                        }

                //                        variableNode.Value.Value = enumValueArray;
                //                    }
                //                }
                //                catch (NotSupportedException ex)
                //                {
                //                    TraceService.Log(TraceMasks.Warning, TraceSources.ClientAPI, "Session.ReadNode", ex.Message, ex);
                //                }
                //            }
                //        }
                //    }
                //}

                //totdo check if props are really necessary
                //variableNode.Value.DataType = variableNode.DataTypeId;
                //variableNode.Value.ValueRank = variableNode.ValueRank;
            }
            else
            {
                variableNode.SetAttibuteStatusCode(AttributeId.Value, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // ArrayDimensions Attribute
            value = attributes[AttributeId.ArrayDimensions];

            if (value != null && (value.Value == null || value.Value is uint[]))
            {
                if (value.Value == null)
                {
                    variableNode.ArrayDimensions = new List<uint>();
                }
                else
                {
                    variableNode.ArrayDimensions = new List<uint>((uint[])value.GetValue(typeof(uint[])));
                }
                variableNode.SetAttibuteStatusCode(AttributeId.ArrayDimensions, value.StatusCode);
            }
            else
            {
                variableNode.SetAttibuteStatusCode(AttributeId.ArrayDimensions, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // AccessLevel Attribute
            value = attributes[AttributeId.AccessLevel];
            if (value != null && value.Value != null)
            {
                variableNode.AccessLevel = (byte)value.GetValue(typeof(byte));
                variableNode.SetAttibuteStatusCode(AttributeId.AccessLevel, value.StatusCode);
            }
            else
            {
                variableNode.SetAttibuteStatusCode(AttributeId.AccessLevel, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // UserAccessLevel Attribute
            value = attributes[AttributeId.UserAccessLevel];
            if (value != null && value.Value != null)
            {
                variableNode.UserAccessLevel = (byte)value.GetValue(typeof(byte));
                variableNode.SetAttibuteStatusCode(AttributeId.UserAccessLevel, value.StatusCode);
            }
            else
            {
                variableNode.SetAttibuteStatusCode(AttributeId.UserAccessLevel, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // Historizing Attribute
            value = attributes[AttributeId.Historizing];
            if (value != null && value.Value != null)
            {
                variableNode.Historizing = (bool)value.GetValue(typeof(bool));
                variableNode.SetAttibuteStatusCode(AttributeId.Historizing, value.StatusCode);
            }
            else
            {
                variableNode.SetAttibuteStatusCode(AttributeId.Historizing, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            // MinimumSamplingInterval Attribute
            value = attributes[AttributeId.MinimumSamplingInterval];

            if (value != null && value.Value is double)
            {
                variableNode.MinimumSamplingInterval = Convert.ToDouble(value.Value);
                variableNode.SetAttibuteStatusCode(AttributeId.MinimumSamplingInterval, value.StatusCode);
            }
            return variableNode;
        }

        private ToolkitObjectNode ReadObjectNode(SortedDictionary<AttributeId, DataValue> attributes, int? nodeClass, StatusCode nodeClassStatusCode)
        {
            ToolkitObjectNode objectNode = new ToolkitObjectNode();
            InitBaseAttributes(attributes, nodeClass, nodeClassStatusCode, objectNode);

            DataValue value = attributes[AttributeId.EventNotifier];
            if (value != null && value.Value != null)
            {
                objectNode.EventNotifier = (byte)value.GetValue(typeof(byte));
                objectNode.SetAttibuteStatusCode(AttributeId.EventNotifier, value.StatusCode);
            }
            else
            {
                objectNode.SetAttibuteStatusCode(AttributeId.EventNotifier, new StatusCode(StatusCodes.BadNodeAttributesInvalid));
            }

            return objectNode;
        }

        #region Event Handlers

        private void OnPublishError(Opc.Ua.Client.Session session, PublishErrorEventArgs e)
        {
            if (PublishError != null)
            {
                PublishError(this, e);
            }
        }

        private void OnKeepAlive(Opc.Ua.Client.Session session, KeepAliveEventArgs e)
        {
            TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.OnKeepAlive",
                "Session KeepAlive received for session {0} ServerState = {1}, ServerTime = {2} .", 
                this.SessionName, e.CurrentState, e.CurrentTime.ToLocalTime());

            RaiseKeepAlive(e);

            if (ServiceResult.IsBad(e.Status))
            {
                bool isDisconnect = false;

                lock (StateTransitionSync)
                {
                    if (CurrentState == State.Active || CurrentState == State.Connected)
                    {
                        OnConnectionLost();

                        CurrentState = State.Disconnected;
                        PerformChildrenStateTransition(m_reconnecting);
                        RaiseOnStateChanged();

                        isDisconnect = true;
                    }
                }

                if (isDisconnect && !m_disposed)
                {
                    PerformStateTransition(null);
                }
            }
        }

        private void OnConnectionLost()
        {
            try
            {
                if (!m_reconnecting)
                {
                    lock (m_reconnectLock)
                    {
                        m_reconnecting = true;
                        m_modified = false;
                        m_disconectedSession = m_session;
                    }

                    m_session.PublishError -= OnPublishError;
                    m_session.KeepAlive -= OnKeepAlive;

                    m_session = null;
                    m_sessionId = null;
                    m_browseHandler.SetSession(m_session);
                    
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.OnConnectionLost", "Connection lost for session {0}", this.SessionName);
                }
            }
            catch (Exception ex)
            {
                EnableReconnectHandler();
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Session.OnConnectionLost", ex);
            }
        }

        #endregion Event Handlers

        private DataValueCollection HistoryRead(NodeId nodeToReadId, ExtensionObject readDetails, TimestampsToReturn timestampsToReturn, object cookie)
        {
            // Create the request parameters
            HistoryReadValueIdCollection nodesToRead = new HistoryReadValueIdCollection();
            HistoryReadValueId nodeToRead = new HistoryReadValueId();
            nodeToRead.NodeId = nodeToReadId;
            nodesToRead.Add(nodeToRead);

            HistoryReadResultCollection results = null;
            DiagnosticInfoCollection diagnosticInfos = null;

            m_session.HistoryRead(
               null,
               readDetails,
               timestampsToReturn,
               false,
               nodesToRead,
               out results,
               out diagnosticInfos);

            ClientBase.ValidateResponse(results, nodesToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

            if (StatusCode.IsBad(results[0].StatusCode))
            {
                throw new ServiceResultException(results[0].StatusCode);
            }

            HistoryData historyData = ExtensionObject.ToEncodeable(results[0].HistoryData) as HistoryData;
            DataValueCollection resultCollection = historyData.DataValues;

            byte[] continuationPoint = results[0].ContinuationPoint;
            while (continuationPoint != null)
            {
                if (HistoryContinuationPointReached != null)
                {
                    HistoryReadContinuationEventArgs args = new HistoryReadContinuationEventArgs(cookie);
                    HistoryContinuationPointReached(this, args);

                    TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.HistoryRead", "History read continuation Point Reached.");
                    if (args.Cancel)
                    {
                        TraceService.Log(TraceMasks.OperationDetail, TraceSources.ClientAPI, "Session.HistoryRead", "History read operation canceled by user.");
                        break;
                    }
                }

                nodeToRead.ContinuationPoint = continuationPoint;

                m_session.HistoryRead(
                    null,
                    readDetails,
                    timestampsToReturn,
                    false,
                    nodesToRead,
                    out results,
                    out diagnosticInfos);

                historyData = ExtensionObject.ToEncodeable(results[0].HistoryData) as HistoryData;
                if (historyData.DataValues != null)
                {
                    resultCollection.AddRange(historyData.DataValues);
                }

                continuationPoint = results[0].ContinuationPoint;
            }

            return resultCollection;
        }

        #endregion Private Methods
    }
}
