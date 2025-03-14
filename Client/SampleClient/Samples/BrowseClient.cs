/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using Opc.Ua;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task InitializeSession()
        {
            try
            {
                // create the session object with no security and anonymous login    
                m_session = m_application.CreateSession(Program.ServerUrl);
                m_session.SessionName = SessionName;

                await m_session.ConnectAsync(false, true).ConfigureAwait(false);
                Console.WriteLine("Session is connected.");
            }
            catch (Exception ex)
            {
                Program.PrintException("BrowseClient.InitializeSession", ex);

                if (m_session != null)
                {
                    m_session.Dispose();
                    m_session = null;
                }
            }
        }

        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public async Task DisconnectSession()
        {
            if (m_session == null)
            {
                return;
            }

            try
            {
                await m_session.DisconnectAsync(true).ConfigureAwait(false);
                m_session.Dispose();
                m_session = null;
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Program.PrintException("BrowseClient.DisconnectSession", ex);
            }
        }

        #endregion

        #region Browse Methods
        /// <summary>
        /// The BrowseTheServer method uses the Browse method with one parameter, in this case the browse options will be taken from the Session object.
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
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = m_session.Browse(null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            try
                            {
                                // Browse Objects node
                                NodeId nodeId = new NodeId(rootReferenceDescription.NodeId.Identifier, rootReferenceDescription.NodeId.NamespaceIndex);
                                var objectReferenceDescriptions = m_session.Browse(nodeId);
                                foreach (var objectRefDescription in objectReferenceDescriptions)
                                {
                                    Console.WriteLine("     -" + objectRefDescription.DisplayName);
                                    if (objectRefDescription.BrowseName.Name == "Server")
                                    {
                                        try
                                        {
                                            // Browse Server node
                                            nodeId = new NodeId(objectRefDescription.NodeId.Identifier, objectRefDescription.NodeId.NamespaceIndex);
                                            var serverReferenceDescriptions = m_session.Browse(nodeId);
                                            foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                            {
                                                Console.WriteLine("        -" + serverReferenceDescription.DisplayName);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Program.PrintException("Browse 'Server'", ex);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Program.PrintException("Browse 'Objects'", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("Browse", ex);
            }
        }

        /// <summary>
        /// The BrowseWithOptions method uses the Browse method with two parameters, in this case the browse options will be given as a parameter.
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
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = m_session.Browse(null, options);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -{0} - [{1}]", rootReferenceDescription.DisplayName, rootReferenceDescription.ReferenceTypeName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            try
                            {
                                // Browse Objects node
                                NodeId nodeId = new NodeId(rootReferenceDescription.NodeId.Identifier, rootReferenceDescription.NodeId.NamespaceIndex);
                                IList<ReferenceDescriptionEx> objectReferenceDescriptions = m_session.Browse(nodeId, options);
                                foreach (var objectReferenceDescription in objectReferenceDescriptions)
                                {
                                    Console.WriteLine("    -{0} - [{1}]",
                                        objectReferenceDescription.DisplayName,
                                        objectReferenceDescription.ReferenceTypeName);

                                    if (objectReferenceDescription.BrowseName.Name == "Server")
                                    {
                                        try
                                        {
                                            // Browse Server node
                                            nodeId = new NodeId(objectReferenceDescription.NodeId.Identifier, objectReferenceDescription.NodeId.NamespaceIndex);
                                            IList<ReferenceDescriptionEx> serverReferenceDescriptions = m_session.Browse(nodeId, options);
                                            foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                            {
                                                Console.WriteLine("      -{0} - [{1}]",
                                                    serverReferenceDescription.DisplayName,
                                                    serverReferenceDescription.ReferenceTypeName);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Program.PrintException("Browse 'Server'", ex);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Program.PrintException("Browse 'Objects'", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("Browse", ex);
            }
        }

        #endregion

        #region BrowseAsync Methods

        /// <summary>
        /// The method uses the BrowseAsync method with one parameter, 
        /// in this case the browse options will be taken from the Session object.
        /// If there are no browse options on the Session object the browse will be done with the default options.
        /// </summary>
        public async Task BrowseTheServerAsync()
        {
            if (m_session == null)
            {
                Console.WriteLine("BrowseTheServerAsync: The session is not initialized!");
                return;
            }
            try
            {
                Console.WriteLine("This is the address space of server: {0}", m_session.Url);
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = await m_session.BrowseAsync(null).ConfigureAwait(false);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            try
                            {
                                // Browse Objects node
                                NodeId nodeId = new NodeId(rootReferenceDescription.NodeId.Identifier, rootReferenceDescription.NodeId.NamespaceIndex);
                                var objectReferenceDescriptions = await m_session.BrowseAsync(nodeId);
                                foreach (var objectRefDescription in objectReferenceDescriptions)
                                {
                                    Console.WriteLine("     -" + objectRefDescription.DisplayName);
                                    if (objectRefDescription.BrowseName.Name == "Server")
                                    {
                                        try
                                        {
                                            // Browse Server node
                                            nodeId = new NodeId(objectRefDescription.NodeId.Identifier, objectRefDescription.NodeId.NamespaceIndex);
                                            var serverReferenceDescriptions = await m_session.BrowseAsync(nodeId);
                                            foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                            {
                                                Console.WriteLine("        -" + serverReferenceDescription.DisplayName);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Program.PrintException("BrowseAsync 'Server'", ex);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Program.PrintException("BrowseTheServerAsync 'Objects'", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("BrowseTheServerAsync", ex);
            }
        }

        /// <summary>
        /// The method uses the BrowseAsync method with two parameters, 
        /// in this case the browse options will be given as a parameter.
        /// A BrowseDescription object is created first, on which browse options can be set, and given as parameter to the BrowseAsync method.
        /// In this case any browse options on the Session object will be ignored.
        /// </summary>
        public async Task BrowseWithOptionsAsync()
        {
            if (m_session == null)
            {
                Console.WriteLine("BrowseWithOptionsAsync: The session is not initialized!");
                return;
            }
            BrowseDescriptionEx options = new BrowseDescriptionEx();
            options.ResultMask = (uint)BrowseResultMask.All;
            options.MaxReferencesReturned = 3;
            try
            {
                Console.WriteLine("Browse server: {0}, with options: MaxReferencesReturned = {1}", m_session.Url, options.MaxReferencesReturned);
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescriptionEx> rootReferenceDescriptions = await m_session.BrowseAsync(null, options).ConfigureAwait(false);
                if (rootReferenceDescriptions != null)
                {
                    foreach (var rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -{0} - [{1}]", rootReferenceDescription.DisplayName, rootReferenceDescription.ReferenceTypeName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            try
                            {
                                // Browse Objects node
                                NodeId nodeId = new NodeId(rootReferenceDescription.NodeId.Identifier, rootReferenceDescription.NodeId.NamespaceIndex);
                                IList<ReferenceDescriptionEx> objectReferenceDescriptions = await m_session.BrowseAsync(nodeId, options);
                                foreach (var objectReferenceDescription in objectReferenceDescriptions)
                                {
                                    Console.WriteLine("    -{0} - [{1}]",
                                        objectReferenceDescription.DisplayName,
                                        objectReferenceDescription.ReferenceTypeName);

                                    if (objectReferenceDescription.BrowseName.Name == "Server")
                                    {
                                        try
                                        {
                                            // Browse Server node
                                            nodeId = new NodeId(objectReferenceDescription.NodeId.Identifier, objectReferenceDescription.NodeId.NamespaceIndex);
                                            IList<ReferenceDescriptionEx> serverReferenceDescriptions = await m_session.BrowseAsync(nodeId, options);
                                            foreach (var serverReferenceDescription in serverReferenceDescriptions)
                                            {
                                                Console.WriteLine("      -{0} - [{1}]",
                                                    serverReferenceDescription.DisplayName,
                                                    serverReferenceDescription.ReferenceTypeName);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Program.PrintException("BrowseWithOptionsAsync 'Server'", ex);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Program.PrintException("BrowseWithOptionsAsync 'Objects'", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("BrowseWithOptionsAsync", ex);
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
                    //for display reasons create BrowsePathEx
                    BrowsePathEx sourceBrowsePath = new BrowsePathEx(startingNode, browsePath);
                    Console.Write("   {0}\n\r           Target Nodes = ", sourceBrowsePath);
                    foreach (NodeId result in translateResults)
                    {
                        Console.WriteLine("{0}; ", result);
                    }
                }
                else
                {
                    Console.WriteLine("TranslateBrowsePath returned null value");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TranslateBrowsePath", ex);
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
                        Console.Write("{0}; ", targetNode);
                    }

                    Console.WriteLine("\b \b");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TranslateBrowsePaths", ex);
            }
        }

        #endregion

        #region TranslateAsync Methods

        /// <summary>
        /// Asynchronously translates the specified browse path to its corresponding NodeId.
        /// </summary>
        public async Task TranslateBrowsePathToNodeIdsAsync()
        {
            if (m_session == null)
            {
                Console.WriteLine("TranslateBrowsePathToNodeIdsAsync: The session is not initialized!");
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
                IList<NodeId> translateResults = await m_session.TranslateBrowsePathToNodeIdsAsync(startingNode, browsePath).ConfigureAwait(false);

                if (translateResults != null)
                {

                    Console.WriteLine("\nTranslateBrowsePathAsync returned {0} result(s):", translateResults.Count);
                    //for display reasons create BrowsePathEx
                    BrowsePathEx sourceBrowsePath = new BrowsePathEx(startingNode, browsePath);
                    Console.Write("   {0}\n\r           Target Nodes = ", sourceBrowsePath);
                    foreach (NodeId result in translateResults)
                    {
                        Console.WriteLine("{0}; ", result);
                    }
                }
                else
                {
                    Console.WriteLine("TranslateBrowsePathAsync returned null value");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TranslateBrowsePathAsync", ex);
            }
        }

        /// <summary>
        /// Asynchronously translates the specified list of browse paths to corresponding NodeIds.
        /// </summary>
        public async Task TranslateBrowsePathsToNodeIdsAsync()
        {
            if (m_session == null)
            {
                Console.WriteLine("TranslateBrowsePathsToNodeIdsAsync: The session is not initialized!");
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
                IList<BrowsePathResultEx> translateResults = await m_session.TranslateBrowsePathsToNodeIdsAsync(browsePaths).ConfigureAwait(false);

                // display the results.
                Console.WriteLine("\nTranslateBrowsePathsAsync returned {0} result(s):", translateResults.Count);
                int i = 0;
                foreach (BrowsePathResultEx browsePathResult in translateResults)
                {
                    Console.Write("   {0}\n\r           StatusCode = {1}; Target Nodes = ", browsePaths[i++], browsePathResult.StatusCode);

                    foreach (NodeId targetNode in browsePathResult.TargetIds)
                    {
                        Console.Write("{0}; ", targetNode);
                    }

                    Console.WriteLine("\b \b");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("TranslateBrowsePathsAsync", ex);
            }
        }

        #endregion
    }
}
