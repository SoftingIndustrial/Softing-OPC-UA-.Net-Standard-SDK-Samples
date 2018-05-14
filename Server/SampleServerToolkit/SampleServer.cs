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
using SampleServerToolkit.ToolkitTest;
using Softing.Opc.Ua.Server;
using System.Collections.Generic;

namespace SampleServerToolkit
{
    /// <summary>
    /// Sample implementation of an Opc Ua Server using Softing OPc UA .NET Standard Toolkit.
    /// </summary>
    class SampleServer : UaServer
    {
        private Dictionary<string, string> m_userNameIdentities;

        /// <summary>
        /// Create new instance of SampleServer
        /// </summary>
        public SampleServer()
        {
            // Initialize the list of accepted user identities.
            m_userNameIdentities = new Dictionary<string, string>();
            m_userNameIdentities.Add("usr", "pwd");
            m_userNameIdentities.Add("admin", "admin");
        }

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
            nodeManagers.Add(new ToolkitTestNodeManager(server, configuration));

            // Create master node manager
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }

        /// <summary>
        /// Validates the user and password identity.
        /// </summary>
        /// <returns>true if the user identity is valid.</returns>
        protected override bool ValidateUserPassword(string userName, string password)
        {
            if (m_userNameIdentities.ContainsKey(userName) && m_userNameIdentities[userName].Equals(password))
            {
                // Accept the user identity.
                return true;
            }
            else
            {
                // Reject the user identity.
                return false;
            }
        }
    }
}
