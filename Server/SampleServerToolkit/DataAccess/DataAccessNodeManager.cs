/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SampleServerToolkit.DataAccess
{
    /// <summary>
    /// Node manager for DataAccess
    /// </summary>
    public class DataAccessNodeManager :NodeManager
    {        
        #region Constructor
        /// <summary>
        /// Create new instance of DataAccessNodeManager
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        /// <param name="namespaceUris"></param>
        public DataAccessNodeManager(IServerInternal server, ApplicationConfiguration configuration, params string[] namespaceUris) : base(server, configuration, "DataAccessNsUri")
        {
        }
        #endregion

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                // Create a root node and add a reference to external Server Objects Folder
                BaseObjectState root = CreateObject(null, "DataAccess");
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                // Create test variable nodes.
                FolderState testVariables = CreateFolder(root, "TestVariables");

                PropertyState property = CreateProperty(testVariables, "Property", DataTypeIds.Int32, ValueRanks.Scalar);
                BaseDataVariableState byteVariable = CreateVariable(testVariables, "ByteVariable", DataTypeIds.Byte, ValueRanks.Scalar);
                BaseDataVariableState stringVariable = CreateVariable(testVariables, "StringVariable", DataTypeIds.String, ValueRanks.Scalar);
                BaseDataVariableState intArrayVariable = CreateVariable(testVariables, "Int32Array", DataTypeIds.Int32, ValueRanks.OneDimension);
                AnalogItemState analogVariable = CreateAnalogVariable(testVariables, "AnalogVariable", DataTypeIds.Float, ValueRanks.Scalar, new Range(100, 0), null);
                TwoStateDiscreteState twoStateVariable = CreateTwoStateDiscreteVariable(testVariables, "TwoStateDiscreteVariable","Enabled", "Disabled");
                MultiStateDiscreteState multiStateVariable = CreateMultiStateDiscreteVariable(testVariables, "MultiStateDiscreteVariable","Green", "Yellow", "Red");

                // Add reference from stringVariable to Root.
                AddReference(stringVariable, ReferenceTypeIds.Aggregates, false, root.NodeId, true);

                // Create methods.
                MethodState method = CreateMethod(root, "Method1");

                // Create Object type instances for all known types.
                FolderState objectInstances = CreateFolder(root, "ObjectInstances");

                try
                {
                    Type baseObjectStateType = typeof(BaseObjectState);
                    Assembly coreLibrary = baseObjectStateType.GetTypeInfo().Assembly;

                    foreach (Type objectStateType in coreLibrary.GetTypes().Where(t => baseObjectStateType.IsAssignableFrom(t)))
                    {
                        if (!objectStateType.GetTypeInfo().IsAbstract && !objectStateType.GetTypeInfo().ContainsGenericParameters)
                        {
                            BaseObjectState objectNode = Activator.CreateInstance(objectStateType, new BaseObjectState(null)) as BaseObjectState;

                            if (objectNode != null)
                            {
                                NodeId typeDefinitionId = objectNode.GetDefaultTypeDefinitionId(SystemContext);

                                if (!typeDefinitionId.IsNullNodeId)
                                {
                                    CreateObject(objectInstances, objectStateType.Name+"Instance", typeDefinitionId, ReferenceTypeIds.HasComponent);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                }

                // Create Object type instances for all known types.
                FolderState variableInstances = CreateFolder(root, "VariableInstances");

                try
                {
                    Type baseVariableType = typeof(BaseVariableState);
                    Assembly coreLibrary = baseVariableType.GetTypeInfo().Assembly;

                    foreach (Type variableType in coreLibrary.GetTypes().Where(t => baseVariableType.IsAssignableFrom(t)))
                    {
                        if (!variableType.GetTypeInfo().IsAbstract && !variableType.GetTypeInfo().ContainsGenericParameters)
                        {
                            BaseVariableState variableNode = Activator.CreateInstance(variableType, new BaseObjectState(null)) as BaseVariableState;

                            if (variableNode != null)
                            {
                                NodeId typeDefinitionId = variableNode.GetDefaultTypeDefinitionId(SystemContext);

                                if (!typeDefinitionId.IsNullNodeId)
                                {
                                    if (typeDefinitionId == VariableTypes.PropertyType)
                                    {
                                        CreateVariable(variableInstances, variableType.Name + "Instance", typeDefinitionId, ReferenceTypeIds.HasProperty);
                                    }
                                    else
                                    {
                                        CreateVariable(variableInstances, variableType.Name + "Instance", typeDefinitionId, ReferenceTypeIds.HasChild);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}
