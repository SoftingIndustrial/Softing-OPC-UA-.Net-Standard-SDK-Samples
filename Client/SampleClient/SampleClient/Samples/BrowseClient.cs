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
using System.ComponentModel;
using Opc.Ua;
using Softing.Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class tat conainbs sample code for browse & translate path functionality
    /// </summary>
    public class BrowseClient
    {
        #region Private Fields

        private const string SessionName = "BrowseClient Session";
        private readonly UaApplication m_application;
        private ClientSession m_session;
        private NamespaceTable m_namespaceUris; 
        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of BrowseClientSample
        /// </summary>
        /// <param name="application"></param>
        public BrowseClient(UaApplication application) 
        {
            m_application = application;
        }
        #endregion

        #region Initialize & DisconnectSession

        /// <summary>
        /// Initialize session object
        /// </summary>
        public void InitializeSession()
        {
            UserIdentity userIdentity = new UserIdentity();
            // create the session object.            
            m_session = m_application.CreateSession(Constants.SampleServerUrlOpcTcp,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, userIdentity, null);
            m_session.SessionName = SessionName;

            try
            {
                m_session.Connect(false, true);
                Console.WriteLine("Session is connected.");

                m_namespaceUris = new NamespaceTable(m_session.NamespaceUris);
                m_session.ContinuationPointReached += Session_ContinuationPointReached;
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateSession Error: {0}", ex);
            }
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public virtual void DisconnectSession()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                m_session.ContinuationPointReached -= Session_ContinuationPointReached;

                m_session.Disconnect(true);
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DisconnectSession Error: {0}", ex.Message);
            }

            m_session.Dispose();
            m_session = null;
        }
        #endregion

        #region Browse Methods

        /// <summary>
        /// The BrowseTheServer method uses the Browse method with two parameters, in this case the browse options will be taken from the Session object.
        /// If there are no browse options on the Session object the browse will be done with the default options.
        /// </summary>
        public void BrowseTheServer()
        {
            try
            {
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = Browse(null, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            NodeId nodeId = ExpandedNodeId.ToNodeId(rootReferenceDescription.NodeId, m_namespaceUris);
                            var objectReferenceDescriptions = Browse(nodeId, null);
                            foreach (var objectRefDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("     -" + objectRefDescription.DisplayName);
                                if (objectRefDescription.BrowseName.Name == "Server")
                                {
                                    nodeId = ExpandedNodeId.ToNodeId(objectRefDescription.NodeId, m_namespaceUris);
                                    var serverReferenceDescriptions = Browse(nodeId, null);
                                    foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                    {
                                        Console.WriteLine("        -" + serverReferenceDescription.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse Error: " + ex.Message);
            }
        }

        /// <summary>
        /// The BrowseWithOptions method uses the Browse method with three parameters, in this case the browse options will be given as a parameer.
        /// A BrowseDescription object is created first, on which browse options can be set, and given as parameter to the Browse method.
        /// In this case any browse options on the Session object will be ignored.
        /// </summary>
        public void BrowseWithOptions()
        {
            BrowseDescriptionEx options = new BrowseDescriptionEx();
            options.MaxReferencesReturned = 3;
            try
            {
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = BrowseOptions(null, null, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -{0} - [{1}]", rootReferenceDescription.DisplayName,
                            rootReferenceDescription.ReferenceTypeName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            IList<ReferenceDescriptionEx> objectReferenceDescriptions =
                                new List<ReferenceDescriptionEx>();
                            objectReferenceDescriptions =
                                BrowseOptions(ExpandedNodeId.ToNodeId(rootReferenceDescription.NodeId, m_namespaceUris),
                                    options, rootReferenceDescription);
                            foreach (var objectReferenceDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("    -{0} - [{1}]", objectReferenceDescription.DisplayName,
                                    objectReferenceDescription.ReferenceTypeName);
                                if (objectReferenceDescription.BrowseName.Name == "Server")
                                {
                                    IList<ReferenceDescriptionEx> serverReferenceDescriptions =
                                        new List<ReferenceDescriptionEx>();
                                    serverReferenceDescriptions = BrowseOptions(
                                        ExpandedNodeId.ToNodeId(objectReferenceDescription.NodeId, m_namespaceUris),
                                        options, objectReferenceDescription);
                                    foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                    {
                                        Console.WriteLine("      -{0} - [{1}]", serverReferenceDescription.DisplayName,
                                            serverReferenceDescription.ReferenceTypeName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Browses the specified node id and returns its list of references.
        /// This method uses browse options set on the Session object
        /// </summary>
        private IList<ReferenceDescriptionEx> Browse(NodeId nodeId, object sender)
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return new List<ReferenceDescriptionEx>();
            }
            IList<ReferenceDescriptionEx> results = null;

            try
            {
                results = m_session.Browse(nodeId, sender);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse error: " + ex.Message);
            }
            return results;
        }

        /// <summary>
        /// Browses the specified node id and returns its list of references.
        /// This method uses browse options as an input parameter.
        /// </summary>
        private IList<ReferenceDescriptionEx> BrowseOptions(NodeId nodeId, BrowseDescriptionEx browseOptions,
            object sender)
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return new List<ReferenceDescriptionEx>();
            }
            IList<ReferenceDescriptionEx> results = null;

            try
            {
                results = m_session.Browse(nodeId, browseOptions, sender);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse error: " + ex.Message);
            }
            return results;
        }
        #endregion

        #region TranslateBrowsePathToNodeIds

        /// <summary>
        /// Translates the specified browse path to its corresponding NodeId.
        /// </summary>
        public void TranslateBrowsePathToNodeIds()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                // define the starting node as the "Objects\Data" node.
                NodeId startingNode = new NodeId("ns=3;i=10157");

                // define the BrowsePath to the "Static\Scalar\Int32Value" node.
                List<QualifiedName> browsePath = new List<QualifiedName>();
                browsePath.Add(new QualifiedName("Static", 3));
                browsePath.Add(new QualifiedName("Scalar", 3));
                browsePath.Add(new QualifiedName("Int32Value", 3));

                // invoke the TranslateBrowsePath service.
                IList<NodeId> translateResults = m_session.TranslateBrowsePathToNodeIds(startingNode, browsePath);

                if (translateResults != null)
                {
                    Console.WriteLine("TranslateBrowsePath returned {0} result(s):", translateResults.Count);

                    foreach (NodeId result in translateResults)
                    {
                        Console.WriteLine("    {0}", result);
                    }
                }
                else
                {
                    Console.WriteLine("TranslateBrowsePath returned null value");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TranslateBrowsePath error: " + ex.Message);
            }
        }

        /// <summary>
        /// Translates the specified list of browse paths to corresponding NodeIds.
        /// </summary>
        public void TranslateBrowsePathsToNodeIds()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                // define the list of requests.
                List<BrowsePathEx> browsePaths = new List<BrowsePathEx>();

                // define the starting node as the "Objects" node.
                BrowsePathEx browsePath = new BrowsePathEx();
                browsePath.StartingNode = new NodeId("ns=0;i=85");

                // define the relative browse path to the "Data\Static\Scalar\Int32Value" node.
                browsePath.RelativePath.Add(new QualifiedName("Data", 3));
                browsePath.RelativePath.Add(new QualifiedName("Static", 3));
                browsePath.RelativePath.Add(new QualifiedName("Scalar", 3));
                browsePath.RelativePath.Add(new QualifiedName("Int32Value", 3));
                browsePaths.Add(browsePath);

                // define the starting node as the "Objects" node.
                browsePath = new BrowsePathEx();
                browsePath.StartingNode = new NodeId("ns=0;i=85");

                // define the relative browse path to the "Data\Static\Array\UInt32Value" node.
                browsePath.RelativePath.Add(new QualifiedName("Data", 3));
                browsePath.RelativePath.Add(new QualifiedName("Static", 3));
                browsePath.RelativePath.Add(new QualifiedName("Array", 3));
                browsePath.RelativePath.Add(new QualifiedName("UInt32Value", 3));
                browsePaths.Add(browsePath);

                // invoke the TranslateBrowsePathsToNodeIds service.
                IList<BrowsePathResultEx> translateResults = m_session.TranslateBrowsePathsToNodeIds(browsePaths);

                // display the results.
                Console.WriteLine("TranslateBrowsePaths returned {0} result(s):", translateResults.Count);

                foreach (BrowsePathResultEx browsePathResult in translateResults)
                {
                    Console.Write("    StatusCode = {0} ; Target Nodes = ", browsePathResult.StatusCode);

                    foreach (NodeId targetNode in browsePathResult.TargetIds)
                    {
                        Console.Write("{0} ;", targetNode);
                    }

                    Console.WriteLine("\b \b");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TranslateBrowsePaths error: " + ex.Message);
            }
        }
        #endregion

        #region Event Hanlders
        /// <summary>
        /// Handle the ContinuationPointReached event.
        /// This event is raised when a continuation point is reached.
        /// For example if from Browse Options the MaxReferencesReturned is set to x, then when browsing every x references returned this event will be thrown.
        /// </summary>
        private void Session_ContinuationPointReached(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
        #endregion
    }
}
