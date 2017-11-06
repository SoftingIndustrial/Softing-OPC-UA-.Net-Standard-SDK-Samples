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
using Softing.Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that contains sample code for browse & translate path functionality
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
        /// Create new instance of BrowseClient
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
            m_session = m_application.CreateSession(Constants.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, userIdentity, null);
            m_session.SessionName = SessionName;

            try
            {
                m_session.Connect(false, true);
                Console.WriteLine("Session is connected.");

                m_namespaceUris = new NamespaceTable(m_session.NamespaceUris);
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateSession Error: {0}", ex.Message);
                m_session.Dispose();
                m_session = null;
            }
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public void DisconnectSession()
        {
            if (m_session == null)
            {
                return;
            }

            try
            {
                m_session.Disconnect(true);
                m_session.Dispose();
                m_session = null;
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DisconnectSession Error: {0}", ex.Message);
            }
        }
        #endregion

        #region Browse Methods
        /// <summary>
        /// The BrowseTheServer method uses the Browse method with two parameters, in this case the browse options will be taken from the Session object.
        /// If there are no browse options on the Session object the browse will be done with the default options.
        /// </summary>
        public void BrowseTheServer()
        {
            if (m_session == null)
            {
                Console.WriteLine("BrowseTheServer: The session is not initialized!");
                return;
            }
            try
            {
                Console.WriteLine("This is the address space of server: {0}", m_session.Url);
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = m_session.Browse(null, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            NodeId nodeId = ExpandedNodeId.ToNodeId(rootReferenceDescription.NodeId, m_namespaceUris);
                            var objectReferenceDescriptions = m_session.Browse(nodeId, null);
                            foreach (var objectRefDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("     -" + objectRefDescription.DisplayName);
                                if (objectRefDescription.BrowseName.Name == "Server")
                                {
                                    nodeId = ExpandedNodeId.ToNodeId(objectRefDescription.NodeId, m_namespaceUris);
                                    var serverReferenceDescriptions = m_session.Browse(nodeId, null);
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
        /// The BrowseWithOptions method uses the Browse method with three parameters, in this case the browse options will be given as a parameter.
        /// A BrowseDescription object is created first, on which browse options can be set, and given as parameter to the Browse method.
        /// In this case any browse options on the Session object will be ignored.
        /// </summary>
        public void BrowseWithOptions()
        {
            if (m_session == null)
            {
                Console.WriteLine("BrowseWithOptions: The session is not initialized!");
                return;
            }
            BrowseDescriptionEx options = new BrowseDescriptionEx();
            options.MaxReferencesReturned = 3;
            try
            {
                Console.WriteLine("Browse server: {0}, with options: MaxReferencesReturned = {1}", m_session.Url, options.MaxReferencesReturned);
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = m_session.Browse(null, options, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -{0} - [{1}]", rootReferenceDescription.DisplayName, rootReferenceDescription.ReferenceTypeName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            NodeId nodeId = ExpandedNodeId.ToNodeId(rootReferenceDescription.NodeId, m_namespaceUris);
                            IList<ReferenceDescriptionEx> objectReferenceDescriptions = m_session.Browse(nodeId, options, rootReferenceDescription);
                            foreach (var objectReferenceDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("    -{0} - [{1}]", 
                                    objectReferenceDescription.DisplayName,
                                    objectReferenceDescription.ReferenceTypeName);

                                if (objectReferenceDescription.BrowseName.Name == "Server")
                                {
                                    nodeId = ExpandedNodeId.ToNodeId(objectReferenceDescription.NodeId, m_namespaceUris);
                                    IList<ReferenceDescriptionEx> serverReferenceDescriptions = m_session.Browse(nodeId, options, objectReferenceDescription);
                                    foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                    {
                                        Console.WriteLine("      -{0} - [{1}]", 
                                            serverReferenceDescription.DisplayName,
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
        #endregion

        #region Translate Methods
        /// <summary>
        /// Translates the specified browse path to its corresponding NodeId.
        /// </summary>
        public void TranslateBrowsePathToNodeIds()
        {
            if (m_session == null)
            {
                Console.WriteLine("TranslateBrowsePathToNodeIds: The session is not initialized!");
                return;
            }
            try
            {
                // define the starting node as the "Objects" node.
                NodeId startingNode = ObjectIds.ObjectsFolder;
           
                // define the BrowsePath to the "Static\Scalar\Int32Value" node.
                List<QualifiedName> browsePath = new List<QualifiedName>();
                browsePath.Add(new QualifiedName("DataAccess", 3));
                browsePath.Add(new QualifiedName("Refrigerator", 3));
                browsePath.Add(new QualifiedName("DoorMotor", 3));

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
                Console.WriteLine("TranslateBrowsePathsToNodeIds: The session is not initialized!");
                return;
            }
            try
            {
                // define the list of requests.
                List<BrowsePathEx> browsePaths = new List<BrowsePathEx>();

                // define the starting node as the "Objects" node.
                BrowsePathEx browsePath = new BrowsePathEx();
                browsePath.StartingNode = ObjectIds.ObjectsFolder;

                // define the relative browse path to the "DataAccess\Refrigerator\DoorMotor" node.
                browsePath.RelativePath.Add(new QualifiedName("DataAccess", 3));
                browsePath.RelativePath.Add(new QualifiedName("Refrigerator", 3));
                browsePath.RelativePath.Add(new QualifiedName("DoorMotor", 3));
                browsePaths.Add(browsePath);

                // define the starting node as the "Objects" node.
                browsePath = new BrowsePathEx();
                browsePath.StartingNode = ObjectIds.ObjectsFolder;

                // define the relative browse path to the "DataAccess\Refrigerator\LightStatus" node.
                browsePath.RelativePath.Add(new QualifiedName("DataAccess", 3));
                browsePath.RelativePath.Add(new QualifiedName("Refrigerator", 3));
                browsePath.RelativePath.Add(new QualifiedName("LightStatus", 3));
                browsePaths.Add(browsePath);

                // invoke the TranslateBrowsePathsToNodeIds service.
                IList<BrowsePathResultEx> translateResults = m_session.TranslateBrowsePathsToNodeIds(browsePaths);

                // display the results.
                Console.WriteLine("TranslateBrowsePaths returned {0} result(s):", translateResults.Count);
                int i = 0;
                foreach (BrowsePathResultEx browsePathResult in translateResults)
                {
                    Console.Write("   {0}\n\r           StatusCode = {1}; Target Nodes = ", browsePaths[i++], browsePathResult.StatusCode);

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
    }
}
