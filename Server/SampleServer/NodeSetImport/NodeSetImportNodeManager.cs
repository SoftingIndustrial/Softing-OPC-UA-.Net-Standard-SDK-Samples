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
                        MethodState importMethod = CreateMethod(nodeSetImportNode, "ImportNodeSet", inputArguments, null, OnImportNodeSet);
                        inputArguments = new Argument[]
                        {
                            new Argument() { Name = "FilePathNodeSet2.Xml", Description = "File path for exported NodeSet.xml file.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar },
                        };
                        MethodState exportNodeSetMethod = CreateMethod(nodeSetImportNode, "ExportNodeSet", inputArguments, null, OnExportNodeSet);

                        inputArguments = new Argument[]
                        {
                            new Argument() { Name = "Parent NodeId", Description = "NodeId for parent node.",  DataType = DataTypeIds.NodeId, ValueRank = ValueRanks.Scalar },
                        new Argument() { Name = "Type NodeId", Description = "NodeId for the type of the new instance node.",  DataType = DataTypeIds.NodeId, ValueRank = ValueRanks.Scalar },
                        new Argument() { Name = "Name", Description = "Name of the new instance node.",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar },
                };
                        MethodState createinstanceMethod = CreateMethod(nodeSetImportNode, "CreateInstance", inputArguments, null, OnCreateInstance);
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

                    ImportNodeSet(SystemContext, stream);
                }                
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
            return ImportNodeSetFromResource(ResourceNames.NodeSetImportInitialSecondaryModel);
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
            try
            {
                // Import the specified model with specified duplicate node handling
                ImportNodeSet(context, inputArguments[0] as string, (DuplicateNodeHandling)(short)inputArguments[1]);
                return ServiceResult.Good;
            }
            catch (ServiceResultException ex)
            {
                Console.WriteLine("Error loading node set: {0}", ex.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error loading node set: {0}", e.Message);
                throw new ServiceResultException(StatusCodes.Bad, "ImportNodeSet error:" + e.Message);
            }
        }
        /// <summary>
        /// Handles the ExportNodeSet.
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
            NodeState parentNode = GetNodeState(inputArguments[0] as NodeId);
            if (parentNode == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Specified parent NodeId is unknown");
            }

            NodeState typeNode = GetNodeState(inputArguments[1] as NodeId);
            if (typeNode == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, string.Format("Specified type NodeId ({0}) is unknown", inputArguments[1]));
            }
            string name = inputArguments[2] as string;
            if (string.IsNullOrEmpty(name))
            {
                name = typeNode.BrowseName.Name + "_instance";
            }
            try
            {
                //create object of specified type
                var newNode = CreateInstanceFromType(parentNode, name, NamespaceIndex, inputArguments[1] as NodeId);
                if (newNode != null)
                {
                    parentNode.AddChild(newNode);
                    AddPredefinedNode(SystemContext, newNode);
                    return ServiceResult.Good;
                }
            }
            catch (Exception ex)
            {
                throw new ServiceResultException(StatusCodes.BadInternalError, "OnCreateInstance:" + ex.Message);
            }
            throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Cannot create instance of type id:" + inputArguments[1]);
        }
        #endregion

        #region Create Instance From Type Methods
        /// <summary>
        /// Get node from address space based on ints nodeId
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private NodeState GetNodeState(NodeId nodeId)
        {
            if (nodeId == null)
            {
                return null;
            }

            INodeManager typeDefinitionNodeManager = null;
            Server.NodeManager.GetManagerHandle(nodeId, out typeDefinitionNodeManager);

            if (typeDefinitionNodeManager is CustomNodeManager2)
            {
                return ((CustomNodeManager2)typeDefinitionNodeManager).FindPredefinedNode(nodeId, typeof(object));
            }
            return null;
        }

        /// <summary>
        /// Creates a new object node in the address space according to the specified type definition.
        /// </summary>
        /// <param name="parent">The parent node. If null no parent is set.</param>
        /// <param name="name">Name for new instance.</param>
        /// <param name="typeDefinition">The TypeDefinition for the object node to be created.</param>
        /// <param name="namespaceIndex">The namespace index for the node to be created.</param>
        /// <returns>An object instance of a type inferred from typeDefinition parameter.</returns>
        private BaseInstanceState CreateInstanceFromType(NodeState parent, string name, ushort namespaceIndex, NodeId typeDefinition)
        {

            BaseInstanceState instance = SystemContext.NodeStateFactory.CreateInstance(SystemContext, parent,
                NodeClass.ReferenceType, null, ReferenceTypeIds.Organizes, typeDefinition) as BaseInstanceState;
            if (instance != null)
            {
                //the instance was created via type definition  
                instance.Create(SystemContext, null, new QualifiedName(name, NamespaceIndex), name, true);
                instance.TypeDefinitionId = typeDefinition;
            }
            else
            {
                NodeState typeNode = GetNodeState(typeDefinition);
                if (typeNode != null)
                {
                    switch (typeNode.NodeClass)
                    {
                        case NodeClass.VariableType:
                            {
                                instance = new BaseDataVariableState(parent);
                                break;
                            }

                        case NodeClass.ObjectType:
                            {
                                instance = new BaseObjectState(parent);
                                break;
                            }

                        case NodeClass.Method:
                            {
                                instance = new MethodState(parent);
                                MethodState methodType = typeNode as MethodState;
                                if (methodType != null)
                                {
                                    ((MethodState)instance).InputArguments = CreateCopyFromNodeState(instance, methodType.InputArguments) as PropertyState<Argument[]>;
                                    ((MethodState)instance).OutputArguments = CreateCopyFromNodeState(instance, methodType.OutputArguments) as PropertyState<Argument[]>;
                                }

                                break;
                            }
                    }

                    if (instance != null)
                    {
                        //handle children
                        IList<BaseInstanceState> children = new List<BaseInstanceState>();
                        typeNode.GetChildren(SystemContext, children);
                        foreach (var child in children)
                        {
                            if (child.ModellingRuleId == Objects.ModellingRule_Mandatory
                                || child.ModellingRuleId == Objects.ModellingRule_Optional)
                            {
                                BaseInstanceState childObject = CreateCopyFromNodeState(instance, child);
                                if (childObject != null)
                                {
                                    instance.AddChild(childObject);
                                }
                            }
                        }
                        // Create the object and the defined structure of nodes.                
                        instance.Create(SystemContext, null, new QualifiedName(name, namespaceIndex), name, true);
                        instance.TypeDefinitionId = typeDefinition;

                        CopyReferences(typeNode, instance);
                    }
                }
            }
            return instance;
        }

        /// <summary>
        /// Creates a copy of BaseInstanceState object
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="orriginalNode"></param>
        /// <returns></returns>
        private BaseInstanceState CreateCopyFromNodeState(NodeState parent, BaseInstanceState orriginalNode)
        {
            if (orriginalNode == null) return null;
            string name = orriginalNode.BrowseName.Name;
            ushort namespaceIndex = orriginalNode.BrowseName.NamespaceIndex;

            BaseInstanceState newNode = null;
            switch (orriginalNode.NodeClass)
            {
                case NodeClass.Object:
                    if (((BaseObjectState)orriginalNode).TypeDefinitionId != ObjectTypeIds.BaseObjectType)
                    {
                        newNode = CreateInstanceFromType(parent, name, namespaceIndex, ((BaseObjectState)orriginalNode).TypeDefinitionId);
                    }
                    else
                    {
                        BaseObjectState newObject = new BaseObjectState(parent);
                        newObject.Create(SystemContext, null, new QualifiedName(name, namespaceIndex), name, true);
                        newObject.TypeDefinitionId = ((BaseObjectState)orriginalNode).TypeDefinitionId;

                        CopyReferences(orriginalNode, newObject);
                        newNode = newObject;
                    }                    
                    break;
                case NodeClass.Method:
                    NodeId methodTypeId = orriginalNode.TypeDefinitionId;
                    if (methodTypeId == null)
                    {
                        methodTypeId = orriginalNode.NodeId;
                    }
                    NodeState typeNode = GetNodeState(methodTypeId);
                    if (typeNode != null)
                    {
                        newNode = CreateInstanceFromType(parent, name, namespaceIndex, methodTypeId);

                        ((MethodState)newNode).Executable = ((MethodState)orriginalNode).Executable;
                        ((MethodState)newNode).UserExecutable = ((MethodState)orriginalNode).UserExecutable;
                    }
                    break;
                case NodeClass.Variable:
                    BaseVariableState newVariable = Activator.CreateInstance(orriginalNode.GetType(), parent) as BaseVariableState;

                    newVariable.Create(SystemContext, null, new QualifiedName(name, namespaceIndex), name, true);
                    newVariable.DataType = ((BaseVariableState)orriginalNode).DataType;
                    newVariable.ValueRank = ((BaseVariableState)orriginalNode).ValueRank;
                    newVariable.ArrayDimensions = ((BaseVariableState)orriginalNode).ArrayDimensions;
                    newVariable.AccessLevel = ((BaseVariableState)orriginalNode).AccessLevel;
                    newVariable.UserAccessLevel = ((BaseVariableState)orriginalNode).UserAccessLevel;
                    newVariable.MinimumSamplingInterval = ((BaseVariableState)orriginalNode).MinimumSamplingInterval;
                    newVariable.Historizing = ((BaseVariableState)orriginalNode).Historizing;
                    newVariable.Value = ((BaseVariableState)orriginalNode).Value;

                    CopyReferences(orriginalNode, newVariable);

                    newNode = newVariable;
                    break;
            }
            if (newNode != null)
            {
                newNode.DisplayName = orriginalNode.DisplayName;
                newNode.Description = orriginalNode.Description;
                newNode.WriteMask = orriginalNode.WriteMask;
                newNode.UserWriteMask = orriginalNode.UserWriteMask;

                newNode.ReferenceTypeId = orriginalNode.ReferenceTypeId;
                AddPredefinedNode(SystemContext, newNode);
            }

            return newNode;
        }

        /// <summary>
        /// Copy HasComponent and HasProperty references from source node to destination node
        /// </summary>
        /// <param name="sourceNode"></param>
        /// <param name="destinationNode"></param>
        private void CopyReferences(NodeState sourceNode, NodeState destinationNode)
        {
            //handle children specified as references
            IList<IReference> references = new List<IReference>();
            sourceNode.GetReferences(SystemContext, references);
            foreach (var reference in references)
            {
                if (reference.IsInverse
                    || (reference.ReferenceTypeId != ReferenceTypeIds.HasComponent && reference.ReferenceTypeId != ReferenceTypeIds.HasProperty))
                {
                    continue;
                }
                NodeId targetNodeId = ExpandedNodeId.ToNodeId(reference.TargetId, SystemContext.NamespaceUris);
                BaseInstanceState targetNode = GetNodeState(targetNodeId) as BaseInstanceState;
                if (targetNode != null)
                {
                    BaseInstanceState copyNode = CreateCopyFromNodeState(destinationNode, targetNode);
                    if (copyNode != null)
                    {
                        //create copy references
                        destinationNode.AddReference(reference.ReferenceTypeId, reference.IsInverse, copyNode.NodeId);
                        copyNode.AddReference(reference.ReferenceTypeId, !reference.IsInverse, destinationNode.NodeId);
                    }
                }
            }
        }
        #endregion    
    }
}