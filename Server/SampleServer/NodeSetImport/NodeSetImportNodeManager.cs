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
using System.IO;
using System.Linq;
using System.Xml;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.NodeSetImport
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class NodeSetImportNodeManager : NodeManager
    {      
        #region Constructors
        /// <summary>
        /// Initializes the node manager. 
        /// </summary>
        public NodeSetImportNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.Refrigerators)
        {
        }
        #endregion

        #region INodeManager Members
        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // Execute base class CreateAddressSpace
                base.CreateAddressSpace(externalReferences);

                // Import the initial data model from a NodeSet file
                ImportNodeSet(ResourceNames.NodeSetImportInitialModel);

                try
                {
                    // Find the "NodeSetImport" node
                    NodeState nodeSetImportNode = PredefinedNodes.Values.FirstOrDefault(x => x.BrowseName.Name == "NodeSetImport");

                    // Add a method for creating a secondary refrigerator from file
                    if (nodeSetImportNode != null)
                    {
                        MethodState addDeviceMethod = CreateMethod(nodeSetImportNode, "AddSecondaryRefrigerator", null, null, OnAddRefrigerator);                        
                    }

                    // Add a method for importing a NodeSet
                    if (nodeSetImportNode != null)
                    {
                        Argument[] inputArguments = new Argument[]
                        {
                            new Argument("File path", DataTypeIds.String, ValueRanks.Scalar, null)
                        };
                        MethodState importMethod = CreateMethod(nodeSetImportNode, "Import", inputArguments, null, OnImportNodeSet);                        
                    }
                }
                catch (Exception ex)
                {
                    Utils.Trace(Utils.TraceMasks.Error, "NodeSetImportNodeManager.CreateAddressSpace", "Error adding methods:", ex.Message);
                }
            }
        }

        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            // This override will receive a callback every time a new node is added
            // e.g. The extension data can be received in predefinedNode.Extensions
            return predefinedNode;
        }
        #endregion

        #region Private Methods     
        /// <summary>
        /// Imports into the address space an xml file stream containing the model structure. The Xml Stream is taken from resources
        /// </summary>
        /// <param name="resourceName">resource name</param>
        /// <returns></returns>
        private ServiceResult ImportNodeSet(string resourceName)
        {
            try
            {
                if (resourceName == null) throw new ArgumentNullException(nameof(resourceName));

                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError, "Could not load nodes from resource: {0}", resourceName);
                }

                ImportNodeSet(SystemContext, stream);
            }
            catch (Exception ex)
            {
                Utils.Trace(Utils.TraceMasks.Error, "NodeSetImportNodeManager.Import", "Error loading node set: {0}", ex.Message);
                throw new ServiceResultException(ex, StatusCodes.Bad);
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Handles the AddDeviceMethodCall
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult OnAddRefrigerator(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            return ImportNodeSet(ResourceNames.NodeSetImportInitialSecondaryModel);
        }

        /// <summary>
        /// Handles the ImportMethodCall
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult OnImportNodeSet(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            string filePath = (string)inputArguments[0];
            try
            {
                ImportNodeSet(context, filePath);
            }
            catch
            {
                new ServiceResult(StatusCodes.Bad);
            }
            return ServiceResult.Good;
        }
        #endregion       
    }
}