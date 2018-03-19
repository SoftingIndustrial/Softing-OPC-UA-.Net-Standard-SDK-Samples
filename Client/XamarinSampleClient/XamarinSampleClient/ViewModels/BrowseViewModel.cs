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
using System.Collections.ObjectModel;
using System.Threading;
using Opc.Ua;
using XamarinSampleClient.Helpers;
using XamarinSampleClient.Models;
using Softing.Opc.Ua.Client;

namespace XamarinSampleClient.ViewModels
{
    /// <summary>
    /// View model for BrowsePage
    /// </summary>
    class BrowseViewModel : BaseViewModel
    {
        #region Private fields
        private const string SessionName = "BrowseClient Session";
        private ClientSession m_session;
        private string m_sessionStatusText;
        private string m_sampleServerUrl;
        public ObservableCollection<BrowseResultNode> m_results;
        private uint m_maxReferencesReturned;
        #endregion

        #region Constructors
        
        /// <summary>
        /// Create new instance of BrowseViewModel
        /// </summary>
        public BrowseViewModel()
        {
            Title = "Browse sample";
            m_results = new ObservableCollection<BrowseResultNode>();
            SampleServerUrl = App.DefaultSampleServerUrl;
            MaxReferencesReturned = 3;
        }

        #endregion

        #region Properties

        /// <summary>
        /// SampleServer Url
        /// </summary>
        public string SampleServerUrl
        {
            get { return m_sampleServerUrl; }
            set
            {
                if (value != m_sampleServerUrl)
                {
                    //disconnect existing session
                    DisconnectSession();
                }
                SetProperty(ref m_sampleServerUrl, value);
            }
        }

        /// <summary>
        /// Text that indicates session status
        /// </summary>
        public string SessionStatusText
        {
            get { return m_sessionStatusText; }
            set { SetProperty(ref m_sessionStatusText, value); }
        }

        /// <summary>
        /// Results list
        /// </summary>
        public ObservableCollection<BrowseResultNode> Results
        {
            get { return m_results; }
        }

        /// <summary>
        /// Maximum number of references returned by browse with options
        /// </summary>
        public uint MaxReferencesReturned
        {
            get { return m_maxReferencesReturned; }
            set { SetProperty(ref m_maxReferencesReturned, value); }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// The BrowseTheServer method uses the Browse method with one parameter from ClientSession, in this case the browse options will be taken from the Session object.
        /// If there are no browse options on the Session object the browse will be done with the default options.
        /// </summary>
        public void BrowseTheServer()
        {
            Results.Clear();
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    return;
                }
            }
            try
            {
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = m_session.Browse(null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Results.Add(new BrowseResultNode()
                        {
                            Text = rootReferenceDescription.DisplayName.Text
                        });
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            NodeId nodeId = new NodeId(rootReferenceDescription.NodeId.Identifier, rootReferenceDescription.NodeId.NamespaceIndex);
                            var objectReferenceDescriptions = m_session.Browse(nodeId);
                            foreach (var objectRefDescription in objectReferenceDescriptions)
                            {
                                Results.Add(new BrowseResultNode()
                                {
                                    Text = "----- " + objectRefDescription.DisplayName
                                });
                                if (objectRefDescription.BrowseName.Name == "Server")
                                {
                                    nodeId = new NodeId(objectRefDescription.NodeId.Identifier, objectRefDescription.NodeId.NamespaceIndex);
                                    var serverReferenceDescriptions = m_session.Browse(nodeId);
                                    foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                    {
                                        Results.Add(new BrowseResultNode()
                                        {
                                            Text = "---------- " + serverReferenceDescription.DisplayName
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Results.Add(new BrowseResultNode()
                {
                    Text = "Browse Error: " + ex.Message
                });
            }
        }

        /// <summary>
        /// The BrowseWithOptions method uses the Browse method with two parameters, in this case the browse options will be given as a parameter.
        /// A BrowseDescription object is created first, on which browse options can be set, and given as parameter to the Browse method.
        /// In this case any browse options on the Session object will be ignored.
        /// </summary>
        public void BrowseWithOptions()
        {
            Results.Clear();
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    return;
                }
            }
            BrowseDescriptionEx options = new BrowseDescriptionEx();
            options.MaxReferencesReturned = MaxReferencesReturned;
            try
            {
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = m_session.Browse(null, options);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Results.Add(new BrowseResultNode()
                        {
                            Text = rootReferenceDescription.DisplayName.Text, 
                            Info = rootReferenceDescription.ReferenceTypeName
                        });
                        
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            NodeId nodeId = new NodeId(rootReferenceDescription.NodeId.Identifier, rootReferenceDescription.NodeId.NamespaceIndex);
                            IList<ReferenceDescriptionEx> objectReferenceDescriptions = m_session.Browse(nodeId, options);
                            foreach (var objectReferenceDescription in objectReferenceDescriptions)
                            {
                                Results.Add(new BrowseResultNode()
                                {
                                    Text = "----- " + objectReferenceDescription.DisplayName,
                                    Info = objectReferenceDescription.ReferenceTypeName
                                });
                                if (objectReferenceDescription.BrowseName.Name == "Server")
                                {
                                    nodeId = new NodeId(objectReferenceDescription.NodeId.Identifier, objectReferenceDescription.NodeId.NamespaceIndex);
                                    IList<ReferenceDescriptionEx> serverReferenceDescriptions = m_session.Browse(nodeId, options);
                                    foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                    {
                                        Results.Add(new BrowseResultNode()
                                        {
                                            Text = "---------- " + serverReferenceDescription.DisplayName,
                                            Info = serverReferenceDescription.ReferenceTypeName
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Results.Add(new BrowseResultNode()
                {
                    Text = "Browse Error: " + ex.Message
                });
            }
        }

        #endregion

        #region Initialize & DisconnectSession

        /// <summary>
        /// Initialize session object
        /// </summary>
        public void InitializeSession()
        {
            IsBusy = true;
            if (m_session == null)
            {
                try
                {
                    // create the session object with no security and anonymous login    
                    m_session = SampleApplication.UaApplication.CreateSession(SampleServerUrl);
                    m_session.SessionName = SessionName;

                    m_session.Connect(false, true);
                    
                    SessionStatusText = "Connected";
                }
                catch (Exception ex)
                {
                    SessionStatusText = "Not connected - CreateSession Error: " + ex.Message;

                    if (m_session != null)
                    {
                        m_session.Dispose();
                        m_session = null;
                    }
                }
            }
            IsBusy = false;
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public void DisconnectSession()
        {
            SessionStatusText = "";
            if (m_session == null)
            {
                SessionStatusText = "The Session was not created.";
                return;
            }

            try
            {
                m_session.Disconnect(true);
                m_session.Dispose();
                m_session = null;

                SessionStatusText = "Disconnected";
            }
            catch (Exception ex)
            {
                SessionStatusText = "DisconnectSession Error: " + ex.Message;
            }
        }

        #endregion
    }
}
