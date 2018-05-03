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

                FolderState dataAccessFolder = CreateFolder(null, "DataAccess", "DataAccess");
                dataAccessFolder.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);               
               
                AddRootNotifier(dataAccessFolder);

                IList<IReference> references;               
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, dataAccessFolder.NodeId));

                // Save the node for later lookup
                AddPredefinedNode(SystemContext, dataAccessFolder);
            }
        }
    }
}
