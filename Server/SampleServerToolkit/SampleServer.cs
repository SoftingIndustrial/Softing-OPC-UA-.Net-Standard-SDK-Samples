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
using SampleServerToolkit.DataAccess;
using Softing.Opc.Ua.Server;
using System.Collections.Generic;

namespace SampleServerToolkit
{
    /// <summary>
    /// Sample implementation of an Opc Ua Server using Softing opc ua .net standard tollkit
    /// </summary>
    class SampleServer : UaServer
    {
        /// <summary>
        /// Creates the node managers for the server.
        /// </summary>
        /// <remarks>
        /// This method allows the sub-class create any additional node managers which it uses. The SDK
        /// always creates a CoreNodeManager which handles the built-in nodes defined by the specification.
        /// Any additional NodeManagers are expected to handle application specific nodes.
        /// </remarks>
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Utils.Trace(Utils.TraceMasks.Information, "SampleServer.CreateMasterNodeManager", "Creating the Node Managers.");
                      
            List<INodeManager> nodeManagers = new List<INodeManager>();
            // Add all node managers to the list
            nodeManagers.Add(new DataAccessNodeManager(server, configuration));           

            // Create master node manager
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }
    }
}
