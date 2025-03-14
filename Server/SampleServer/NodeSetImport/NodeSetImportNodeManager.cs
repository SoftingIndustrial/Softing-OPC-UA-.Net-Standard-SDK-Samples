/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;
using Softing.Opc.Ua.Server.Types;

namespace SampleServer.NodeSetImport
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class NodeSetImportNodeManager : NodeManager
    {
        private uint m_nodeIdIndex = 80000;
        private bool m_isExecutingImport;

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
                ImportNodeSetFromResource(ResourceNames.NodeSetImportInitialModel);

                try
                {
                    // Find the "NodeSetImport" node
                    NodeState nodeSetImportNode = PredefinedNodes.Values.FirstOrDefault(x => x.BrowseName.Name == "NodeSetImport");
                   
                    if (nodeSetImportNode != null)
                    {
                        // Add a method for creating a secondary refrigerator from file
                        MethodState addDeviceMethod = CreateMethod(nodeSetImportNode, "AddSecondaryRefrigerator", null, null, OnAddRefrigerator);                        
                   
                        // Add a method for importing a NodeSet
                        Argument[] inputArguments = new Argument[]
                        {
                             new Argument() { Name = "FilePathNodeSet2.Xml", Description = "File path for NodeSet.xml file that will be imported.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar },
                            new Argument() { Name = "DuplicateNodeHandling", Description = "0 - ReportError and stop import, 1 - UseExistingNode, 2 - OverwriteNode.",  DataType = DataTypeIds.Int16, ValueRank = ValueRanks.Scalar },
                        };
                        Argument[] outputArguments = new Argument[]
                        {
                             new Argument() { Name = "NewNamespaceUris", Description = "The list of namespace URIs added from the imported file.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.OneDimension },
                        };
                        MethodState importMethod = CreateMethod(nodeSetImportNode, "ImportNodeSet", inputArguments, outputArguments, OnImportNodeSet);
                        inputArguments = new Argument[]
                        {
                            new Argument() { Name = "FilePathNodeSet2.Xml", Description = "File path for exported NodeSet.xml file.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar },
                        };
                        MethodState exportNodeSetMethod = CreateMethod(nodeSetImportNode, "ExportNodeSet", inputArguments, null, OnExportNodeSet);

                        inputArguments = new Argument[]
                        {
                            new Argument() { Name = "Parent NodeId", Description = "NodeId for parent node.",  DataType = DataTypeIds.NodeId, ValueRank = ValueRanks.Scalar },
                            new Argument() { Name = "Type NodeId", Description = "NodeId for the type of the new instance node.",  DataType = DataTypeIds.NodeId, ValueRank = ValueRanks.Scalar },
                            new Argument() { Name = "CreateOptionalProperties", Description = "Flag that indicates if optional properties defined in type shall be instantiated.",  DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar, Value = true },
                            new Argument() { Name = "Name", Description = "Name of the new instance node.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar },
                        };
                        MethodState createinstanceMethod = CreateMethod(nodeSetImportNode, "CreateInstance", inputArguments, null, OnCreateInstance);
  
                        //create instance of variable node with data type from imported nodeset dictionary
                        FolderState referenceServerVariables = CreateFolder(nodeSetImportNode, "Imported Types Variables");

                        var dataTypeRefrigeratorStatus = PredefinedNodes.Values.FirstOrDefault(x => x.BrowseName.Name == "RefrigeratorStatusDataType" && x.NodeClass == NodeClass.DataType);
                       
                        StructuredValue refrigeratorStatusValue = GetDefaultValueForDatatype(dataTypeRefrigeratorStatus.NodeId) as StructuredValue;
                        refrigeratorStatusValue["MotorTemperature"] = 5.6;

                        var refrigeratorStatusVariable = CreateVariable(referenceServerVariables, "RefrigeratorStatusVariable", dataTypeRefrigeratorStatus.NodeId);
                        refrigeratorStatusVariable.Value = refrigeratorStatusValue;

                        var enumerationRefrigeratorState = PredefinedNodes.Values.FirstOrDefault(x => x.BrowseName.Name == "RefrigeratorState");
                        var enumerationRefrigeratorStateValue = GetDefaultValueForDatatype(enumerationRefrigeratorState.NodeId);
                        
                        var refrigeratorStateVariable = CreateVariable(referenceServerVariables, "RefrigeratorStateVariable", enumerationRefrigeratorState.NodeId);
                        refrigeratorStateVariable.Value = enumerationRefrigeratorStateValue;
                    
                    }
                }
                catch (Exception ex)
                {
                    Utils.Trace(ex, "NodeSetImportNodeManager.CreateAddressSpace: Error adding methods:");
                }
            }
        }

        /// <summary>
        /// Add behavior to predefined node
        /// </summary>
        /// <param name="context"></param>
        /// <param name="predefinedNode"></param>
        /// <returns></returns>
        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            // This override will receive a callback every time a new node is added          

            if (m_isExecutingImport)
            {
                StringBuilder browsePath = new StringBuilder();
                NodeState currentNode = predefinedNode;
                while (currentNode != null)
                {
                    browsePath.Insert(0, currentNode.BrowseName);
                    browsePath.Insert(0, "\\");
                    currentNode = (currentNode as BaseInstanceState)?.Parent;
                }
                if (browsePath.Length > 0)
                {
                    Utils.TraceDebug($"Node imported: NodeId{predefinedNode.NodeId}, Path: {browsePath}");
                }
            }

            // The extension data can be received in predefinedNode.Extensions
            return predefinedNode;
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {           
            return new NodeId(m_nodeIdIndex++, NamespaceIndex);
        }
        #endregion

        #region Private Methods  
        /// <summary>
        /// Imports into the address space an xml file stream containing the model structure. The Xml Stream is taken from resources
        /// </summary>
        /// <param name="resourceName">resource name</param>
        /// <returns></returns>
        private ServiceResult ImportNodeSetFromResource(string resourceName)
        {
            try
            {
                if (resourceName == null) throw new ArgumentNullException(nameof(resourceName));

                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadDecodingError, "Could not load nodes from resource: {0}", resourceName);
                    }

                    ImportNodeSet(SystemContext, stream, DuplicateNodeHandling.OverwriteNode);
                }                
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "NodeSetImportNodeManager.Import: Error loading node set");
                throw new ServiceResultException(ex, StatusCodes.Bad);
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Handles the AddSecondaryRefrigerator method call
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult OnAddRefrigerator(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            var result = ImportNodeSetFromResource(ResourceNames.NodeSetImportSecondaryModel);

            // report update method audit event                        
            Server.ReportAuditUpdateMethodEvent(context, method.Parent?.NodeId, method.NodeId, inputArguments?.ToArray(),
                "Execute AddSecondaryRefrigerator method.", result.StatusCode);

            return result;

        }

        /// <summary>
        /// Handles the ImportNodeSet method call
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult OnImportNodeSet(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            StatusCode result = StatusCodes.Good;
            try
            {
                m_isExecutingImport = true;
                List<string> newNamespaceUris;
                // Import the specified model with specified duplicate node handling
                var extensions = ImportNodeSet(context, inputArguments[0] as string, (DuplicateNodeHandling)(short)inputArguments[1], out newNamespaceUris);

                // save newNamespaceUris to out parameter
                outputArguments[0] = newNamespaceUris;

                result = StatusCodes.Good;
                return result;

            }
            catch (ServiceResultException ex)
            {
                result = StatusCodes.Bad;
                Console.WriteLine("Error loading node set: {0}", ex.Message);
                throw;
            }
            catch (Exception e)
            {
                result = StatusCodes.Bad;
                Console.WriteLine("Error loading node set: {0}", e.Message);
                throw new ServiceResultException(StatusCodes.Bad, "ImportNodeSet error:" + e.Message);
            }
            finally
            {
                // report update method audit event                        
                Server.ReportAuditUpdateMethodEvent(context, method.Parent?.NodeId, method.NodeId, inputArguments?.ToArray(),
                    "Execute ImportNodeSet method.", result);

                m_isExecutingImport = false;
            }
        }

        /// <summary>
        /// Handles the ExportNodeSet method call.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult OnExportNodeSet(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            try
            {
                
                NodeStateCollection nodesToExport = new NodeStateCollection();

                // Add all nodes of the NodeManager to the list
                foreach (NodeState node in PredefinedNodes.Values)
                {
                    nodesToExport.Add(node);
                }

                // Export nodeset to file path from arguments[0].
                ExportNodeSet(inputArguments[0] as string, nodesToExport);
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex)
            {
                Console.WriteLine("Error exporting node set: {0}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error exporting node set: {0}", ex.Message);
                throw new ServiceResultException(StatusCodes.Bad, "ExportNodeSet error:" + ex.Message);
            }            
        }
        
        /// <summary>
        /// Execute handle for CreateInstance method
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult OnCreateInstance(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            //get reference to parent node
            NodeState parentNode = FindNodeInAddressSpace(inputArguments[0] as NodeId);
            if (parentNode == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Specified parent NodeId is unknown");
            }

            NodeState typeNode = FindNodeInAddressSpace(inputArguments[1] as NodeId);
            if (typeNode == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, string.Format("Specified type NodeId ({0}) is unknown", inputArguments[1]));
            }
           
            bool createOptionalFields = (bool)inputArguments[2];

            string name = inputArguments[3] as string;
            if (string.IsNullOrEmpty(name))
            {
                name = typeNode.BrowseName.Name + "_instance";
            }
            try
            {
                //create object of specified type and generate also properties that have modelling rule set on optional
                var newNode = CreateInstanceFromType(parentNode, name, inputArguments[1] as NodeId, ReferenceTypeIds.Organizes, createOptionalFields);
                if (newNode != null)
                {
                    parentNode.AddChild(newNode);
                    AddPredefinedNode(SystemContext, newNode);

                    // report update method audit event                        
                    Server.ReportAuditUpdateMethodEvent(context, method.Parent.NodeId, method.NodeId, inputArguments?.ToArray(),
                        "Execute CreateInstance method.", StatusCodes.Good);

                    return ServiceResult.Good;
                }
            }
            catch (Exception ex)
            {
                // report update method audit event                        
                Server.ReportAuditUpdateMethodEvent(context, method.Parent.NodeId, method.NodeId, inputArguments?.ToArray(),
                    "Execute CreateInstance method exception: " + ex.Message, StatusCodes.BadInternalError);

                throw new ServiceResultException(StatusCodes.BadInternalError, "OnCreateInstance:" + ex.Message);
            }

            // report update method audit event                        
            Server.ReportAuditUpdateMethodEvent(context, method.Parent.NodeId, method.NodeId, inputArguments?.ToArray(),
               "Cannot create instance of type id:" + inputArguments[1], StatusCodes.BadInvalidArgument);
    
            throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Cannot create instance of type id:" + inputArguments[1]);
        }
       
        #endregion

    }
}
