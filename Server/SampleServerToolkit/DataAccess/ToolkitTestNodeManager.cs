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
using System.Threading;

namespace SampleServerToolkit.ToolkitTest
{
    /// <summary>
    /// Node manager for Testing Toolkit Server API
    /// </summary>
    public class ToolkitTestNodeManager : NodeManager
    {
        #region Private Members
        private Timer m_pollingTimer;
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of ToolkitTestNodeManager
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        /// <param name="namespaceUris"></param>
        public ToolkitTestNodeManager(IServerInternal server, ApplicationConfiguration configuration, params string[] namespaceUris) 
            : base(server, configuration, Namespaces.ToolkitTest)
        {
        }
        #endregion

        /// <summary>
        /// Create address space associated with this NodeManager.
        /// </summary>
        /// <param name="externalReferences"></param>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                // Create a root node and add a reference to external Server Objects Folder
                FolderState folder = CreateFolder(null, "Toolkit Test");
                AddReference(folder, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                // Create test variable nodes.
                FolderState testVariables = CreateFolder(folder, "TestVariables");

                PropertyState property = CreateProperty(testVariables, "Property", DataTypeIds.Int32, ValueRanks.Scalar);
                PropertyState<bool[]> typedProperty = CreateProperty<bool[]>(testVariables, "TypedProperty");
                BaseDataVariableState byteVariable = CreateVariable(testVariables, "ByteVariable", DataTypeIds.Byte, ValueRanks.Scalar);
                BaseDataVariableState stringVariable = CreateVariable(testVariables, "StringVariable", DataTypeIds.String, ValueRanks.Scalar);
                BaseDataVariableState intArrayVariable = CreateVariable(testVariables, "Int32Array", DataTypeIds.Int32, ValueRanks.OneDimension);
                AnalogItemState analogVariable = CreateAnalogVariable(testVariables, "AnalogVariable", DataTypeIds.Float, ValueRanks.Scalar, new Range(100, 0), null);
                TwoStateDiscreteState twoStateVariable = CreateTwoStateDiscreteVariable(testVariables, "TwoStateDiscreteVariable","Enabled", "Disabled");
                MultiStateDiscreteState multiStateVariable = CreateMultiStateDiscreteVariable(testVariables, "MultiStateDiscreteVariable","Green", "Yellow", "Red");

                // Add reference from stringVariable to Root.
                AddReference(stringVariable, ReferenceTypeIds.Aggregates, false, folder.NodeId, true);

                // Create methods.
                MethodState method = CreateMethod(folder, "Method1");

                // Create Object type instances for all known types.
                FolderState objectInstances = CreateFolder(folder, "ObjectInstances");

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
                FolderState variableInstances = CreateFolder(folder, "VariableInstances");

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

                m_pollingTimer = new Timer(ReadDynamicValues, null, 1000, 1000);
            }
        }

        private void ReadDynamicValues(object state)
        {
            try
            {
                lock (Lock)
                {
                    //Update dynamic values.
                }
            }
            catch (Exception e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "ToolkitTest.ReadDynamicValues", "Unexpected error doing simulation.", e);
            }
        }
    }
}
