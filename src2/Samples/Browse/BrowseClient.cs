/* ========================================================================
 * Copyright © 2011-2017 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/
using Opc.Ua;
using Opc.Ua.Toolkit;
using Opc.Ua.Toolkit.Client;
using System;
using System.Collections.Generic;

namespace Softing.Opc.Ua.Toolkit.Client.Samples.BrowseClient
{
    /// <summary>
    /// Represents the Browse sample class
    /// </summary>
    class BrowseClient
    {
        #region Private Members
        private const string m_demoServerUrl = "opc.tcp://localhost:51510/UA/DemoServer";
        private Session m_session = null;

        #endregion
        
        #region Browse
        /// <summary>
        /// Browses the specified node id and returns its list of references.
        /// This method uses browse options set on the Session object
        /// </summary>
        internal IList<ReferenceDescription> Browse(NodeId nodeId, object sender)
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return new List<ReferenceDescription>();
            }
            IList<ReferenceDescription> results = null;

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
        internal IList<ReferenceDescription> BrowseOptions(NodeId nodeId, BrowseOptions browseOptions, object sender)
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return new List<ReferenceDescription>();
            }
            IList<ReferenceDescription> results = null;

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

        /// <summary>
        /// Handle the ContinuationPointReached event.
        /// This event is raised when a continuation point is reached.
        /// For example if from Browse Options the MaxReferencesReturned is set to x, then when browsing every x references returned this event will be thrown.
        /// </summary>
        private void Session_ContinuationPointReached(object sender, BrowseEventArgs e)
        {
            e.Cancel = true;
        }
        #endregion

        #region TranslateBrowsePathToNodeIds

        /// <summary>
        /// Translates the specified browse path to its corresponding NodeId.
        /// </summary>
        internal void TranslateBrowsePathToNodeIds()
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
        internal void TranslateBrowsePathsToNodeIds()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                //// define the list of requests.
                //List<BrowsePath> browsePaths = new List<BrowsePath>();

                //// define the starting node as the "Objects" node.
                //BrowsePath browsePath = new BrowsePath();
                //browsePath.StartingNode = new NodeId("ns=0;i=85");

                //// define the relative browse path to the "Data\Static\Scalar\Int32Value" node.
                //browsePath.RelativePath.Add(new QualifiedName("Data", 3));
                //browsePath.RelativePath.Add(new QualifiedName("Static", 3));
                //browsePath.RelativePath.Add(new QualifiedName("Scalar", 3));
                //browsePath.RelativePath.Add(new QualifiedName("Int32Value", 3));
                //browsePaths.Add(browsePath);

                //// define the starting node as the "Objects" node.
                //browsePath = new BrowsePath();
                //browsePath.StartingNode = new NodeId("ns=0;i=85");

                //// define the relative browse path to the "Data\Static\Array\UInt32Value" node.
                //browsePath.RelativePath.Add(new QualifiedName("Data", 3));
                //browsePath.RelativePath.Add(new QualifiedName("Static", 3));
                //browsePath.RelativePath.Add(new QualifiedName("Array", 3));
                //browsePath.RelativePath.Add(new QualifiedName("UInt32Value", 3));
                //browsePaths.Add(browsePath);

                //// invoke the TranslateBrowsePathsToNodeIds service.
                //IList<BrowsePathResult> translateResults = m_session.TranslateBrowsePathsToNodeIds(browsePaths);

                //// display the results.
                //Console.WriteLine("TranslateBrowsePaths returned {0} result(s):", translateResults.Count);

                //foreach (BrowsePathResult browsePathResult in translateResults)
                //{
                //    Console.Write("    StatusCode = {0} ; Target Nodes = ", browsePathResult.StatusCode);

                //    foreach (NodeId targetNode in browsePathResult.TargetIds)
                //    {
                //        Console.Write("{0} ;", targetNode);
                //    }

                //    Console.WriteLine("\b \b");
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("TranslateBrowsePaths error: " + ex.Message);
            }
        }
        #endregion

        #region Session
        /// <summary>
        /// Creates a new session and connects it to the server.
        /// </summary>
        internal void CreateSession()
        {
            if (m_session != null)
            {
                Console.WriteLine("Session already created.");
                return;
            }

            // create the session object.
            m_session = new Session(
                m_demoServerUrl,
                MessageSecurityMode.None,
                SecurityPolicy.None.ToString(),
                MessageEncoding.Binary,
                new AnonymousUserIdentity(),
                null);
            m_session.SessionName = "Softing Browse Sample Client";

            try
            {
                m_session.Connect(false, true);
                Console.WriteLine("Session is connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("CreateSession Error: {0}", ex.Message));
            }
            
            m_session.ContinuationPointReached += Session_ContinuationPointReached;
        }

        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        internal void DisconnectSession()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                m_session.Disconnect(false);
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("DisconnectSession Error: {0}", ex.Message));
            }

            m_session.Dispose();
            m_session = null;
        }

        /// <summary>
        /// The BrowseTheServer method uses the Browse method with two parameters, in this case the browse options will be taken from the Session object.
        /// If there are no browse options on the Session object the browse will be done with the default options.
        /// </summary>
        internal void BrowseTheServer()
        {
            try
            {
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescription> rootReferenceDescriptions = Browse(null, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (ReferenceDescription rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            IList<ReferenceDescription> objectReferenceDescriptions = new List<ReferenceDescription>();
                            objectReferenceDescriptions = Browse(new NodeId(rootReferenceDescription.NodeId), null);
                            foreach (ReferenceDescription objectReferenceDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("     -" + objectReferenceDescription.DisplayName);
                                if (objectReferenceDescription.BrowseName.Name == "Server")
                                {
                                    IList<ReferenceDescription> serverReferenceDescriptions = new List<ReferenceDescription>();
                                    serverReferenceDescriptions = Browse(new NodeId(objectReferenceDescription.NodeId), null);
                                    foreach (ReferenceDescription serverReferenceDescription in serverReferenceDescriptions)
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
        /// A BrowseOptions object is created first, on which browse options can be set, and given as parameter to the Browse method.
        /// In this case any browse options on the Session object will be ignored.
        /// </summary>
        internal void BrowseWithOptions()
        {
            BrowseOptions options = new BrowseOptions();
            options.MaxReferencesReturned = 3;
            try
            {
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescription> rootReferenceDescriptions = BrowseOptions(null, null, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (ReferenceDescription rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            IList<ReferenceDescription> objectReferenceDescriptions = new List<ReferenceDescription>();
                            objectReferenceDescriptions = BrowseOptions(new NodeId(rootReferenceDescription.NodeId), options, rootReferenceDescription);
                            foreach (ReferenceDescription objectReferenceDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("     -" + objectReferenceDescription.DisplayName);
                                if (objectReferenceDescription.BrowseName.Name == "Server")
                                {
                                    IList<ReferenceDescription> serverReferenceDescriptions = new List<ReferenceDescription>();
                                    serverReferenceDescriptions = BrowseOptions(new NodeId(objectReferenceDescription.NodeId), options, objectReferenceDescription);
                                    foreach (ReferenceDescription serverReferenceDescription in serverReferenceDescriptions)
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
        #endregion
    }
}