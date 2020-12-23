/* ========================================================================
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

        private NodeIdDictionary<NodeState> m_registeredNodes = new NodeIdDictionary<NodeState>();
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
            // create the NamespaceMetadata ReferenceServer Namespace
            ConfigurationNodeManager configurationNodeManager = server.DiagnosticsNodeManager as ConfigurationNodeManager;
            var metadaata = configurationNodeManager?.CreateNamespaceMetadataState(Namespaces.ReferenceApplications);

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

        #region UserAuthentication Custom Implementation

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

        #region RoleManagement Custom Implementation
        /// <summary>
        /// Custom implementation of RoleSet. Define custom role set 
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="roleStateHelper">The helper class that implements roleSet methods</param>
        public override void OnRoleSetInitialized(IServerInternal server, IRoleStateHelper roleStateHelper)
        {
            // add username identity mapping to enigineer role
            roleStateHelper.AddIdentityToRoleState(ObjectIds.WellKnownRole_Engineer,
               new IdentityMappingRuleType {
                   CriteriaType = IdentityCriteriaType.UserName,
                   Criteria = EngineerUser
               });

            // add username identity mapping to operator role
            roleStateHelper.AddIdentityToRoleState(ObjectIds.WellKnownRole_Operator,
                 new IdentityMappingRuleType {
                     CriteriaType = IdentityCriteriaType.UserName,
                     Criteria = OperatorUser1
                 });
            // add username identity mapping to operator role
            roleStateHelper.AddIdentityToRoleState(ObjectIds.WellKnownRole_Operator,
                new IdentityMappingRuleType {
                    CriteriaType = IdentityCriteriaType.UserName,
                    Criteria = OperatorUser2
                });

            // configure operator role to include all applicationUris 
            roleStateHelper.AddApplicationToRoleState(ObjectIds.WellKnownRole_Operator, "");
            roleStateHelper.SetExcludeApplications(ObjectIds.WellKnownRole_Operator, true);

            base.OnRoleSetInitialized(server, roleStateHelper);
        }

        /// <summary>
        /// Validates if the provided userName is appropriate for the provided roleId.
        /// It checks that the Username is a name of a user known to the Server and can be associated with the specified Role.
        /// </summary>
        /// <param name="roleId">The RoleState node Id</param>
        /// <param name="userName">The string representing a user name</param>
        /// <returns>Good if input criteria passes the validation or a bad status code otherwise</returns>
        protected override ServiceResult ValidateRoleUserNameCriteria(NodeId roleId, string userName)
        {
            if (!string.IsNullOrEmpty(userName) && m_userNameIdentities.ContainsKey(userName))
            {
                // Accept the username.
                return ServiceResult.Good;
            }

            // Reject the username.
            return ServiceResult.Create(StatusCodes.BadInvalidArgument,
                "ValidateUserNameCriteria failed: username {0} is not known to Server", userName);
        }

        /// <summary>
        /// Validates if the Thumbprint criteria is appropriate for the provided RoleId.
        /// It checks that the criteria is a thumbprint of a Certificate of a user or CA which is trusted by the Server.
        /// </summary>
        /// <param name="roleId">The RoleState node Id</param>
        /// <param name="thumbprint">The string representing a certificate thumbprint.</param>
        /// <returns>Good if input criteria passes the validation or a bad status code otherwise.</returns>
        protected override ServiceResult ValidateRoleThumbprintCriteria(NodeId roleId, string thumbprint)
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
        /// Validates if anonymous user is accepted by the role that has the specified roleId
        /// </summary>
        /// <param name="roleId">The RoleState node Id</param>
        /// <returns>Good if input criteria passes the validation or a bad status code otherwise</returns>
        protected override ServiceResult ValidateRoleAnonymousCriteria(NodeId roleId)
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
        #endregion

        #region Register/UnregisterNodes Implementation
        /// <summary>
        /// Invokes the RegisterNodes service.
        /// </summary>
        /// <param name="requestHeader">The request header.</param>
        /// <param name="nodesToRegister">The list of NodeIds to register.</param>
        /// <param name="registeredNodeIds">The list of NodeIds identifying the registered nodes. </param>
        /// <returns>
        /// Returns a <see cref="ResponseHeader"/> object
        /// </returns>
        public override ResponseHeader RegisterNodes(RequestHeader requestHeader, NodeIdCollection nodesToRegister, out NodeIdCollection registeredNodeIds)
        {

            OperationContext context = ValidateRequest(requestHeader, RequestType.RegisterNodes);

            try
            {
                if (nodesToRegister == null || nodesToRegister.Count == 0)
                {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }
                // return the node id provided.
                registeredNodeIds = new NodeIdCollection();

                foreach (NodeId nodeId in nodesToRegister)
                {
                    // initialize the newId with the old id - RegisterNodes does not validate the NodeIds from the request. 
                    // Servers will simply copy unknown NodeIds in the response. 
                    NodeId newId = nodeId;
                    var nodeState = ServerInternal.DiagnosticsNodeManager.FindNodeInAddressSpace(nodeId);

                    if (nodeState != null)
                    {
                        // generate a new id
                        newId = new NodeId(Guid.NewGuid(), nodeId.NamespaceIndex);

                        lock (Lock)
                        {
                            //remember new id
                            m_registeredNodes[newId] = nodeState;
                        }                        
                    }     

                    // add node id to return list
                    registeredNodeIds.Add(newId);
                }

                Utils.Trace((int)Utils.TraceMasks.ServiceDetail,
                    "SampleServer.RegisterNodes - Count={0}",
                    nodesToRegister.Count);

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e)
            {
                lock (ServerInternal.DiagnosticsWriteLock)
                {
                    ServerInternal.ServerDiagnostics.RejectedRequestsCount++;

                    if (IsSecurityError(e.StatusCode))
                    {
                        ServerInternal.ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw TranslateException(context, e);
            }
            finally
            {
                OnRequestComplete(context);
            }
        }

        /// <summary>
        /// Invokes the UnregisterNodes service.
        /// </summary>
        /// <param name="requestHeader">The request header.</param>
        /// <param name="nodesToUnregister">The list of NodeIds to unregister</param>
        /// <returns>
        /// Returns a <see cref="ResponseHeader"/> object
        /// </returns>
        public override ResponseHeader UnregisterNodes(RequestHeader requestHeader, NodeIdCollection nodesToUnregister)
        {
            OperationContext context = ValidateRequest(requestHeader, RequestType.UnregisterNodes);

            try
            {
                if (nodesToUnregister == null || nodesToUnregister.Count == 0)
                {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                foreach (NodeId nodeId in nodesToUnregister)
                {
                    lock (Lock)
                    {
                        if (!m_registeredNodes.ContainsKey(nodeId))
                        {
                            //remove node id
                            m_registeredNodes.Remove(nodeId);
                        }
                    }
                }

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e)
            {
                lock (ServerInternal.DiagnosticsWriteLock)
                {
                    ServerInternal.ServerDiagnostics.RejectedRequestsCount++;

                    if (IsSecurityError(e.StatusCode))
                    {
                        ServerInternal.ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw TranslateException(context, e);
            }
            finally
            {
                OnRequestComplete(context);
            }
        }

        /// <summary>
        /// Invokes the Read service taking into account the registered nodes
        /// </summary>
        /// <param name="requestHeader">The request header.</param>
        /// <param name="maxAge">The Maximum age of the value to be read in milliseconds.</param>
        /// <param name="timestampsToReturn">The type of timestamps to be returned for the requested Variables.</param>
        /// <param name="nodesToRead">The list of Nodes and their Attributes to read.</param>
        /// <param name="results">The list of returned Attribute values</param>
        /// <param name="diagnosticInfos">The diagnostic information for the results.</param>
        /// <returns>
        /// Returns a <see cref="ResponseHeader"/> object
        /// </returns>
        public override ResponseHeader Read(RequestHeader requestHeader, double maxAge, TimestampsToReturn timestampsToReturn, ReadValueIdCollection nodesToRead, out DataValueCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            if (nodesToRead == null || nodesToRead.Count == 0)
            {
                throw new ServiceResultException(StatusCodes.BadNothingToDo);
            }

            // check if there is any registered node id to be read
            int registeredIdsCount = 0;
            for (int i = 0; i < nodesToRead.Count; i++)
            {
                NodeId nodeId = nodesToRead[i].NodeId;
                if (m_registeredNodes.ContainsKey(nodeId) && m_registeredNodes[nodeId] != null)
                {
                    registeredIdsCount++;
                }
            }

            if (registeredIdsCount == 0)
            {
                // there is no registered node id, execute base class routine
                return base.Read(requestHeader, maxAge, timestampsToReturn, nodesToRead, out results, out diagnosticInfos);
            }

            // initialize output arguments
            diagnosticInfos = new DiagnosticInfoCollection(nodesToRead.Count);
            results = new DataValueCollection(nodesToRead.Count);
            OperationContext context = ValidateRequest(requestHeader, RequestType.Read);

            // values collection will keep locally the values read from registered nodes
            DataValueCollection values = new DataValueCollection(nodesToRead.Count);            

            try
            {            
                // read values for registered nodes
                for (int i = 0; i < nodesToRead.Count; i++)
                {
                    NodeId nodeId = nodesToRead[i].NodeId;
                    values.Add(null);
                    diagnosticInfos.Add(null);

                    if (m_registeredNodes.ContainsKey(nodeId) && m_registeredNodes[nodeId] != null)
                    {
                        // create an initial value.
                        DataValue value = values[i] = new DataValue();
                        
                        // read the requested attribute directly from NodeState
                        m_registeredNodes[nodeId].ReadAttribute(
                                                ServerInternal.DefaultSystemContext,
                                                nodesToRead[i].AttributeId,
                                                nodesToRead[i].ParsedIndexRange,
                                                nodesToRead[i].DataEncoding,
                                                value);
                        value.ServerTimestamp = DateTime.UtcNow;
                        value.SourceTimestamp = DateTime.MinValue;
                        value.StatusCode = StatusCodes.Good;

                        // mark node as processed
                        nodesToRead[i].Processed = true;
                    }
                }

                if (registeredIdsCount < nodesToRead.Count)
                {
                    // there not on;y regostered nodes, it is needed to read other nodes
                    ResponseHeader responseHeader = base.Read(requestHeader, maxAge, timestampsToReturn, nodesToRead, out results, out diagnosticInfos);
                    for (int i = 0; i < nodesToRead.Count; i++)
                    {
                        if (values[i] != null)
                        {
                            results[i] = values[i];
                        }
                    }
                    return responseHeader;
                }
                results = values;

                // return response                
                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e)
            {
                lock (ServerInternal.DiagnosticsWriteLock)
                {
                    ServerInternal.ServerDiagnostics.RejectedRequestsCount++;

                    if (IsSecurityError(e.StatusCode))
                    {
                        ServerInternal.ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw TranslateException(context, e);
            }
            finally
            {
                OnRequestComplete(context);
            }
        }
        #endregion
    }
}
