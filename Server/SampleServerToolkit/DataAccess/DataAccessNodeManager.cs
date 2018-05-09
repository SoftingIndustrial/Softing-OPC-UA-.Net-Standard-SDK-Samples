﻿/* ========================================================================
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

                BaseObjectState root = CreateObject(null, "DataAccess");
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);

                IList<IReference> references;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));

                // Create variable nodes.
                BaseDataVariableState byteVariable = CreateVariable(root, "ByteVariable", DataTypeIds.Byte, ValueRanks.Scalar);
                BaseDataVariableState stringVariable = CreateVariable(root, "StringVariable", DataTypeIds.String, ValueRanks.Scalar);
                BaseDataVariableState intArrayVariable = CreateVariable(root, "Int32Array", DataTypeIds.Int32, ValueRanks.OneDimension);
                AnalogItemState analogVariable = CreateAnalogVariable(root, "AnalogVariable", DataTypeIds.Float, ValueRanks.Scalar, new Range(100, 0), null);

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
                            BaseObjectState parentNode = new BaseObjectState(null);

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
            }
        }
    }
}
