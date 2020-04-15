/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/
 
using System.Collections.Generic;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Server;
using SampleServer.Alarms;
using SampleServer.CustomTypes;
using SampleServer.DataAccess;
using SampleServer.FileTransfer;
using SampleServer.HistoricalDataAccess;
using SampleServer.Methods;
using SampleServer.NodeSetImport;
using SampleServer.PubSub;
using SampleServer.ReferenceServer;
using SampleServer.UserAuthentication;
using Softing.Opc.Ua.Server;

namespace SampleServer
{
    /// <summary>
    /// A sample implementation of UaServer from Softing OPC UA .Net Standard Toolkit
    /// </summary>
    public class SampleServer : UaServer
    {
        #region Private Members
        private const string OperatorUser1 = "operator1";
        private const string OperatorUser2 = "operator2";

        private Dictionary<string, string> m_userNameIdentities;
        private Timer m_certificatesTimer;
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of SampleServer
        /// </summary>
        public SampleServer()
        {
            // Initialize the list of accepted user identities.
            m_userNameIdentities = new Dictionary<string, string>();
            m_userNameIdentities.Add(OperatorUser1, "pwd");
            m_userNameIdentities.Add(OperatorUser2, "pwd");
            m_userNameIdentities.Add("usr", "pwd");
            m_userNameIdentities.Add("admin", "admin");
            ManufacturerName = "Softing";
        }

        #endregion
                
        #region OnServerStarted
        /// <summary>
        /// Called after the server has been started.
        /// </summary>
        /// <param name="server">The server.</param>
        protected override void OnServerStarted(IServerInternal server)
        {
            base.OnServerStarted(server);            
            
            uint clearCertificatesInterval = 30000;

            //parse custom configuration extension 
            SampleServerConfiguration sampleServerConfiguration = this.Configuration.ParseExtension<SampleServerConfiguration>();
            if (sampleServerConfiguration != null)
            {
                clearCertificatesInterval = sampleServerConfiguration.ClearCachedCertificatesInterval;
            }

            m_certificatesTimer = new Timer(ClearCachedCertificates, null, clearCertificatesInterval, clearCertificatesInterval);

        }

        /// <summary>
        /// Clear cached trusted certificates
        /// </summary>
        /// <param name="state"></param>
        private void ClearCachedCertificates(object state)
        {
            try
            {
                // clear list of validated certificates
                CertificateValidator.Update(Configuration).Wait();
            }
            catch { }
        }
        #endregion

        #region 
        /// <summary>
        /// Custom implementation of RoleSet 
        /// </summary>
        /// <param name="server"></param>
        public override void OnRoleSetInitialized(IServerInternal server)
        {
            RoleState operatorRole = null;// server.NodeManager.ConfigurationNodeManager.GetRoleState(ObjectIds.WellKnownRole_Operator);
            if (operatorRole != null)
            {
                operatorRole.Identities.Value = new IdentityMappingRuleType[]
                {
                    new IdentityMappingRuleType()
                    {
                        CriteriaType = IdentityCriteriaType.UserName,
                        Criteria = OperatorUser1
                    },
                    new IdentityMappingRuleType()
                    {
                        CriteriaType = IdentityCriteriaType.UserName,
                        Criteria = OperatorUser2
                    }
                };
                operatorRole.Applications.Value = new string[]
                {
                    "urn:localhost:Softing:UANETStandardToolkit:SampleClient"
                };
            }

            base.OnRoleSetInitialized(server);
        }
        #endregion

        #region Override CreateMasterNodeManager

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
            // add RolesNodeManager to support Role based permission handling in this server
            nodeManagers.Add(new RolesNodeManager(server, configuration));
            nodeManagers.Add(new AlarmsNodeManager(server, configuration));
            nodeManagers.Add(new DataAccessNodeManager(server, configuration));
            nodeManagers.Add(new SampleHDANodeManager(server, configuration));
            nodeManagers.Add(new MethodsNodeManager(server, configuration));
            nodeManagers.Add(new NodeSetImportNodeManager(server, configuration));
            nodeManagers.Add(new ReferenceNodeManager(server, configuration));
            nodeManagers.Add(new UserAuthenticationNodeManager(server, configuration));
            nodeManagers.Add(new FileTransferNodeManager(server, configuration));
            nodeManagers.Add(new PubSubNodeManager(server, configuration, true));
            nodeManagers.Add(new CustomTypesNodeManager(server, configuration));

            // Create master node manager
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }
        #endregion

        #region UserAuthentication

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

        /// <summary>
        /// Validates the user and password identity for <see cref="SystemConfigurationIdentity"/>.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns>true if the user identity is valid.</returns>
        protected override bool ValidateSystemConfigurationIdentity(string userName, string password)
        {
            //  Ensure that username "admin" will be instantiated as SystemConfigurationIdentity. The Password is validated by ValidateUserPassword method.
            return (userName == "admin");
        }
        
        #endregion
    }
}