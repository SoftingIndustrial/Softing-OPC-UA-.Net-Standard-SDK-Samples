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
using System.Collections.Generic;

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

                FolderState rootFolder = CreateFolder(null, "DataAccess");
                rootFolder.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);

                IList<IReference> references;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, rootFolder.NodeId));

                // Create variable nodes
                BaseDataVariableState byteVariable = CreateVariable(rootFolder, "ByteVariable", DataTypeIds.Byte, ValueRanks.Scalar);
                BaseDataVariableState stringVariable = CreateVariable(rootFolder, "StringVariable", DataTypeIds.String, ValueRanks.Scalar);
                BaseDataVariableState intArrayVariable = CreateVariable(rootFolder, "Int32Array", DataTypeIds.Int32, ValueRanks.OneDimension);

                AnalogItemState analogVariable = CreateAnalogVariable(rootFolder, "AnalogVariable", DataTypeIds.Float, ValueRanks.Scalar, new Range(100, 0), null);

                // Create methods
                MethodState method = CreateMethod(rootFolder, "Method1");
            }
        }
    }
}
