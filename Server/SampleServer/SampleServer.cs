﻿/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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

using X509Certificate2Collection = System.Security.Cryptography.X509Certificates.X509Certificate2Collection;

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
        private const string EngineerUser = "engineer";

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
            m_userNameIdentities.Add(EngineerUser, "pwd");
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

            // TODO: check if this can be done in a better place!
            // create the NamespaceMetadata nodes for all Namespaces
            ConfigurationNodeManager configurationNodeManager = server.DiagnosticsNodeManager as ConfigurationNodeManager;

            foreach (string namespaceUri in server.NamespaceUris.ToArray())
            {
                var metadaata = configurationNodeManager?.CreateNamespaceMetadataState(namespaceUri);
            }

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

        #region RoleSet Handling

        /// <summary>
        /// Validates GroupId criteria type of a IdentityMappingRuleType.
        /// It checks that the criteria is a generic text identifier for a user group specific to the Authorization Service.
        /// </summary>
        /// <param name="groupId">A generic text identifier for a user group specific to the Authorization Service</param>
        /// <param name="roleId">The RoleState node</param>
        /// <returns>Good if input criteria passes the validation or a bad status code otherwise</returns>
        protected override ServiceResult ValidateGroupIdCriteria(string groupId, NodeId roleId)
        {
            // let all groupIds pass
            return ServiceResult.Good;
        }

        /// <summary>
        /// Validates Role criteria type of a IdentityMappingRuleType.
        /// It checks that the criteria is a name of a restriction found in the Access Token.
        /// </summary>
        /// <param name="restrictionName">The string representing a name of a restriction found in the Access Token</param>
        /// <param name="roleId">The RoleState node</param>
        /// <returns>Good if input criteria passes the validation or a bad status code otherwise</returns>
        protected override ServiceResult ValidateRoleCriteria(string restrictionName, NodeId roleId)
        {
            // let all restriction names pass
            return StatusCodes.Good;
        }

        /// <summary>
        /// Validates Thumbprint criteria type of a IdentityMappingRuleType.
        /// It checks that the criteria is a thumbprint of a Certificate of a user or CA which is trusted by the Server.
        /// </summary>
        /// <param name="thumbprint">The string representing a user name</param>
        /// <param name="roleId">The RoleState node</param>
        /// <returns>Good if input criteria passes the validation or a bad status code otherwise</returns>
        protected override ServiceResult ValidateThumbprintCriteria(string thumbprint, NodeId roleId)
        {
            X509Certificate2Collection trustedCertificates = new X509Certificate2Collection();
            trustedCertificates.AddRange(Configuration.SecurityConfiguration.TrustedUserCertificates.GetCertificates().Result);
            trustedCertificates.AddRange(Configuration.SecurityConfiguration.TrustedPeerCertificates.GetCertificates().Result);
            trustedCertificates.AddRange(Configuration.SecurityConfiguration.TrustedIssuerCertificates.GetCertificates().Result);

            // If there is any trusted certificate containing the given thumbprint
            foreach (X509Certificate2 trustedCertificate in trustedCertificates)
            {
                bool? found = trustedCertificate?.Thumbprint?.Equals(thumbprint);
                if (found ?? false)
                {
                    return ServiceResult.Good;
                }
            }
            return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                "ValidateThumbprintCriteria failed: thumbprint {0} not found amongst trusted certificates", thumbprint);
        }

        /// <summary>
        /// Validates UserName criteria type of a IdentityMappingRuleType.
        /// It checks that the Username is a name of a user known to the Server.
        /// </summary>
        /// <param name="username">The string representing a user name</param>
        /// <param name="roleId">The RoleState node</param>
        /// <returns>Good if input criteria passes the validation or a bad status code otherwise</returns>
        protected override ServiceResult ValidateUserNameCriteria(string username, NodeId roleId)
        {
            if (!string.IsNullOrEmpty(username) && m_userNameIdentities.ContainsKey(username))
            {
                // Accept the username.
                return ServiceResult.Good;
            }

            // Reject the username.
            return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                "ValidateUserNameCriteria failed: username {0} is not known to Server", username);
        }

        /// <summary>
        /// Validates Anonymous criteria type of a IdentityMappingRuleType.
        /// In case of Anonymous it checks that the criteria is a null string which indicates that no user credentials have been provided.
        /// </summary>
        /// <param name="roleId">The RoleState node</param>
        /// <returns>Good if input criteria passes the validation or a bad status code otherwise</returns>
        protected override ServiceResult ValidateAnonymousUserCriteria(NodeId roleId)
        {
            // Anonymous users can't be added to administrator roles
            if (roleId == ObjectIds.WellKnownRole_Supervisor ||
                roleId == ObjectIds.WellKnownRole_SecurityAdmin ||
                roleId == ObjectIds.WellKnownRole_ConfigureAdmin)
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                    "ANONYMOUS_5 mapping rule cannot be added to Roles with administrator privileges");
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Validates AuthenticatedUser criteria type of a IdentityMappingRuleType.
        /// In case of AuthenticatedUser it checks that the criteria is a null string which indicates any valid user credentials have been provided.
        /// </summary>
        /// <param name="roleId">The RoleState node</param>
        /// <returns>Good if input criteria passes the validation or a bad status code otherwise</returns>
        protected override ServiceResult ValidateAuthenticatedUserCriteria(NodeId roleId)
        {
            // let all authenticated user criteria pass
            return ServiceResult.Good;
        }

        /// <summary>
        /// Custom implementation of RoleSet 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="rolesNodeManager"></param>
        public override void OnRoleSetInitialized(IServerInternal server, RolesNodeManager rolesNodeManager)
        {
            // add username identity mapping to enigineer role
            ServiceResult serviceResult = rolesNodeManager.RoleStateHelper.AddIdentityToRoleState(ObjectIds.WellKnownRole_Engineer,
               new IdentityMappingRuleType
               {
                   CriteriaType = IdentityCriteriaType.UserName,
                   Criteria = EngineerUser
               });
            if (ServiceResult.IsBad(serviceResult))
            {
                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.OnRoleSetInitializedserviceResult failed: ", serviceResult.LocalizedText);
                Console.WriteLine(String.Format("SampleServer.OnRoleSetInitializedserviceResult failed: {0}",
                    serviceResult.LocalizedText));
            }
            // add username identity mapping to operator role
            serviceResult = rolesNodeManager.RoleStateHelper.AddIdentityToRoleState(ObjectIds.WellKnownRole_Operator,
                new IdentityMappingRuleType
                {
                    CriteriaType = IdentityCriteriaType.UserName,
                    Criteria = OperatorUser1
                });
            if (ServiceResult.IsBad(serviceResult))
            {
                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.OnRoleSetInitializedserviceResult failed: ", serviceResult.LocalizedText);
                Console.WriteLine(String.Format("SampleServer.OnRoleSetInitializedserviceResult failed: {0}",
                    serviceResult.LocalizedText));
            }
            // add username identity mapping to operator role
            serviceResult = rolesNodeManager.RoleStateHelper.AddIdentityToRoleState(ObjectIds.WellKnownRole_Operator,
                new IdentityMappingRuleType
                {
                    CriteriaType = IdentityCriteriaType.UserName,
                    Criteria = OperatorUser2
                });
            if (ServiceResult.IsBad(serviceResult))
            {
                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.OnRoleSetInitializedserviceResult failed: ", serviceResult.LocalizedText);
                Console.WriteLine(String.Format("SampleServer.OnRoleSetInitializedserviceResult failed: {0}",
                    serviceResult.LocalizedText));
            }

            serviceResult = rolesNodeManager.RoleStateHelper.AddApplicationToRoleState(ObjectIds.WellKnownRole_Operator,
                "urn:localhost:Softing:UANETStandardToolkit:SampleClient");
            if (ServiceResult.IsBad(serviceResult))
            {
                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.OnRoleSetInitializedserviceResult failed: ", serviceResult.LocalizedText);
                Console.WriteLine(String.Format("SampleServer.OnRoleSetInitializedserviceResult failed: {0}",
                    serviceResult.LocalizedText));
            }

            serviceResult = rolesNodeManager.RoleStateHelper.AddEndpointToRoleState(ObjectIds.WellKnownRole_Operator,
                new EndpointType()
                {
                    EndpointUrl = "opc.tcp://localhost",
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    SecurityPolicyUri = "None",
                    TransportProfileUri = "None"
                });
            if (ServiceResult.IsBad(serviceResult))
            {
                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.OnRoleSetInitializedserviceResult failed: ", serviceResult.LocalizedText);
                Console.WriteLine(String.Format("SampleServer.OnRoleSetInitializedserviceResult failed: {0}",
                    serviceResult.LocalizedText));
            }

            // Will accept only the endpoints added with AddEndpointToRoleState
            serviceResult = rolesNodeManager.RoleStateHelper.ExcludeEndpoints(ObjectIds.WellKnownRole_Operator, false);
            if (ServiceResult.IsBad(serviceResult))
            {
                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.OnRoleSetInitializedserviceResult failed: ", serviceResult.LocalizedText);
                Console.WriteLine(String.Format("SampleServer.OnRoleSetInitializedserviceResult failed: {0}",
                    serviceResult.LocalizedText));
            }

            // Will accept only the SampleClient added with AddApplicationToRoleState
            serviceResult = rolesNodeManager.RoleStateHelper.ExcludeApplications(ObjectIds.WellKnownRole_Operator, false);
            if (ServiceResult.IsBad(serviceResult))
            {
                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.OnRoleSetInitializedserviceResult failed: ", serviceResult.LocalizedText);
                Console.WriteLine(String.Format("SampleServer.OnRoleSetInitializedserviceResult failed: {0}",
                    serviceResult.LocalizedText));
            }

            base.OnRoleSetInitialized(server, rolesNodeManager);
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