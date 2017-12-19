/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Server;
using SampleServer.Alarms;
using SampleServer.DataAccess;
using SampleServer.HistoricalDataAccess;
using SampleServer.Methods;
using SampleServer.NodeManagement;
using SampleServer.NodeSetImport;
using SampleServer.ReferenceServer;
using SampleServer.UserAuthentication;

namespace SampleServer
{
    public class SampleServer : StandardServer
    {
        #region Private Members
        private uint m_shutdownDelay = 5;
        private NodeManagementNodeManager m_nodeManagementManager; // The sample node manager able to handle UA Node management services
        #endregion

        #region Overridden Methods

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

            // Get the ShutdownDelay configuration parameter
            ReferenceServerConfiguration referenceServerConfiguration = configuration.ParseExtension<ReferenceServerConfiguration>();
            if (referenceServerConfiguration != null)
            {
                m_shutdownDelay = referenceServerConfiguration.ShutdownDelay;
            }

            List<INodeManager> nodeManagers = new List<INodeManager>();

            nodeManagers.Add(new AlarmsNodeManager(server, configuration));
            nodeManagers.Add(new DataAccessNodeManager(server, configuration));
            nodeManagers.Add(new SampleHDANodeManager(server, configuration));
            nodeManagers.Add(new MethodsNodeManager(server, configuration));
            // Uncomment this when there is implementation in the client
            // NodeManagement support missing from ClientAPI
            // nodeManagers.Add(m_nodeManagementManager = new DynamicASNodeManager(server, configuration)); 
            nodeManagers.Add(new NodeSetImportNodeManager(server, configuration));
            nodeManagers.Add(new ReferenceNodeManager(server, configuration));
            nodeManagers.Add(new UserAuthenticationNodeManager(server, configuration));

            // Create master node manager
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }
        
        /// <summary>
        /// Loads the non-configurable properties for the application
        /// </summary>
        /// <remarks>
        /// These properties are exposed by the server but cannot be changed by administrators
        /// </remarks>
        protected override ServerProperties LoadServerProperties()
        {
            ServerProperties properties = new ServerProperties();

            properties.ManufacturerName = "Softing";
            properties.ProductName = "Sample Server";
            properties.ProductUri = "http://industrial.softing.com/OpcUaNetStandardToolkit/SampleServer";
            properties.SoftwareVersion = Utils.GetAssemblySoftwareVersion();
            properties.BuildNumber = Utils.GetAssemblyBuildNumber();
            properties.BuildDate = Utils.GetAssemblyTimestamp();

            return properties;
        }

        /// <summary>
        /// Called after the server has been started
        /// </summary>
        protected override void OnServerStarted(IServerInternal server)
        {
            base.OnServerStarted(server);

            #region NodeManagement
            RegisterKnownVariableTypes();
            RegisterKnownObjectTypes();
            #endregion

            #region UserAuthentication
            // Request notifications when the user identity is changed. All valid users are accepted by default.
            server.SessionManager.ImpersonateUser += SessionManager_ImpersonateUser;
            #endregion
        }

        /// <summary>
        /// Cleans up before the server shuts down.
        /// </summary>
        /// <remarks>
        /// This method is called before any shutdown processing occurs.
        /// </remarks>
        protected override void OnServerStopping()
        {
            try
            {
                // Check for connected clients
                IList<Session> currentessions = ServerInternal.SessionManager.GetSessions();

                if (currentessions.Count > 0)
                {
                    // Provide some time for the connected clients to detect the shutdown state
                    ServerInternal.Status.Value.ShutdownReason = new LocalizedText("en-US", "Application closed.");
                    ServerInternal.Status.Variable.ShutdownReason.Value = new LocalizedText("en-US", "Application closed.");
                    ServerInternal.Status.Value.State = ServerState.Shutdown;
                    ServerInternal.Status.Variable.State.Value = ServerState.Shutdown;
                    ServerInternal.Status.Variable.ClearChangeMasks(ServerInternal.DefaultSystemContext, true);

                    for (uint timeTillShutdown = m_shutdownDelay; timeTillShutdown > 0; timeTillShutdown--)
                    {
                        ServerInternal.Status.Value.SecondsTillShutdown = timeTillShutdown;
                        ServerInternal.Status.Variable.SecondsTillShutdown.Value = timeTillShutdown;
                        ServerInternal.Status.Variable.ClearChangeMasks(ServerInternal.DefaultSystemContext, true);

                        Thread.Sleep(1000);
                    }
                }
            }
            catch
            {
                // Ignore error during shutdown procedure
            }

            base.OnServerStopping();
        }
        #endregion

        #region NodeManagement
        /// <summary>
        /// Registers all known variable types in NodeStateFactory.
        /// </summary>
        private void RegisterKnownVariableTypes()
        {
            try
            {
                // Load known TypeDefinitionIds dictionary
                Type baseVariableType = typeof(BaseDataVariableState);
                Assembly coreLibrary = Assembly.GetAssembly(baseVariableType);

                foreach (Type variableType in coreLibrary.GetTypes().Where(t => baseVariableType.IsAssignableFrom(t)))
                {
                    if (!variableType.IsAbstract && !variableType.ContainsGenericParameters)
                    {
                        BaseObjectState parentNode = new BaseObjectState(null);

                        BaseDataVariableState variableNode = Activator.CreateInstance(variableType, parentNode) as BaseDataVariableState;

                        NodeId typeDefinitionId = variableNode.GetDefaultTypeDefinitionId(ServerInternal.DefaultSystemContext);

                        if (!typeDefinitionId.IsNullNodeId)
                        {
                            // Register known variable type
                            ServerInternal.DefaultSystemContext.NodeStateFactory.RegisterType(typeDefinitionId, variableType);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "SampleServer.RegisterKnownVariableTypes", string.Format("Unexpected error RegisterKnownVariableTypes : {0}", e.Message));
            }
        }

        /// <summary>
        /// Registers all known object types in NodeStateFactory
        /// </summary>
        private void RegisterKnownObjectTypes()
        {
            try
            {
                // Load known TypeDefinitionIds dictionary
                Type baseObjectType = typeof(BaseObjectState);
                Assembly coreLibrary = Assembly.GetAssembly(baseObjectType);

                foreach (Type objectType in coreLibrary.GetTypes().Where(t => baseObjectType.IsAssignableFrom(t)))
                {
                    if (!objectType.IsAbstract && !objectType.ContainsGenericParameters)
                    {
                        BaseObjectState parentNode = new BaseObjectState(null);

                        BaseObjectState objectNode = Activator.CreateInstance(objectType, parentNode) as BaseObjectState;

                        NodeId typeDefinitionId = objectNode.GetDefaultTypeDefinitionId(ServerInternal.DefaultSystemContext);

                        if (!typeDefinitionId.IsNullNodeId)
                        {
                            // Register known object type
                            ServerInternal.DefaultSystemContext.NodeStateFactory.RegisterType(typeDefinitionId, objectType);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "SampleServer.RegisterKnownObjectTypes", string.Format("Unexpected error RegisterKnownObjectTypes : {0}", e.Message));
            }
        }

        /// <summary>
        /// Overrides the AddNode UA service implementation handler.
        /// NOTE: implementation of this method will become part of the Toolkit implementation
        /// and will be extended to handle all node managers defined by the server.
        /// </summary>
        /// <param name="requestHeader">The requestHeader parameter.</param>
        /// <param name="nodesToAdd">The nodesToAdd parameter.</param>
        /// <param name="results">The results parameter.</param>
        /// <param name="diagnosticInfos">The diagnosticInfos parameter.</param>
        public override ResponseHeader AddNodes(
            RequestHeader requestHeader,
            AddNodesItemCollection nodesToAdd,
            out AddNodesResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            if (m_nodeManagementManager == null)
            {
                throw new ServiceResultException(StatusCodes.BadServiceUnsupported);
            }

            OperationContext context = ValidateRequest(requestHeader, RequestType.AddNodes);

            try
            {
                if (nodesToAdd == null || nodesToAdd.Count == 0)
                {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                m_nodeManagementManager.AddNodes(context, nodesToAdd, out results, out diagnosticInfos);

                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.AddNodes", string.Format("Finished AddNodes : request handle= {0}, nodes= {1} ", requestHeader.RequestHandle, nodesToAdd));

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "SampleServer.AddNodes", string.Format("Unexpected error AddNodes : request handle= {0}, nodes= {1} ", requestHeader.RequestHandle, nodesToAdd));

                lock (ServerInternal.DiagnosticsLock)
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
        /// Overrides the DeleteNodes UA service implementation handler.
        /// NOTE: implementation of this method will become part of the Toolkit implementation
        /// and will be extended to handle all node managers defined by the server.
        /// </summary>
        /// <param name="requestHeader">The requestHeader parameter.</param>
        /// <param name="nodesToDelete">The nodesToDelete parameter.</param>
        /// <param name="results">The results parameter.</param>
        /// <param name="diagnosticInfos">The diagnosticInfos parameter.</param>
        public override ResponseHeader DeleteNodes(
            RequestHeader requestHeader,
            DeleteNodesItemCollection nodesToDelete,
            out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            if (m_nodeManagementManager == null)
            {
                throw new ServiceResultException(StatusCodes.BadServiceUnsupported);
            }

            results = null;
            diagnosticInfos = null;

            OperationContext context = ValidateRequest(requestHeader, RequestType.DeleteNodes);

            try
            {
                if (nodesToDelete == null || nodesToDelete.Count == 0)
                {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                m_nodeManagementManager.DeleteNodes(
                    context,
                    nodesToDelete,
                    out results,
                    out diagnosticInfos);

                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.DeleteNodes", string.Format("Finished DeleteNodes : request handle= {0}, nodes= {1} ", requestHeader.RequestHandle, nodesToDelete));

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "SampleServer.DeleteNodes", string.Format("Unexpected error DeleteNodes : request handle= {0}, nodes= {1} ", requestHeader.RequestHandle, nodesToDelete));

                lock (ServerInternal.DiagnosticsLock)
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
        /// Overrides the AdReference UA service implementation handler.
        /// NOTE: implementation of this method will become part of the Toolkit implementation
        /// and will be extended to handle all node managers defined by the server.
        /// </summary>
        /// <param name="requestHeader">The requestHeader parameter.</param>
        /// <param name="referencesToAdd">The referencesToAdd parameter.</param>
        /// <param name="results">The results parameter.</param>
        /// <param name="diagnosticInfos">The diagnosticInfos parameter.</param>
        public override ResponseHeader AddReferences(
            RequestHeader requestHeader,
            AddReferencesItemCollection referencesToAdd,
            out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            if (m_nodeManagementManager == null)
            {
                throw new ServiceResultException(StatusCodes.BadServiceUnsupported);
            }

            OperationContext context = ValidateRequest(requestHeader, RequestType.AddReferences);

            try
            {
                if (referencesToAdd == null || referencesToAdd.Count == 0)
                {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                m_nodeManagementManager.AddReferences(
                    context,
                    referencesToAdd,
                    out results,
                    out diagnosticInfos);

                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.AddReferences", string.Format("Finished AddReferences : request handle= {0}, references= {1} ", requestHeader.RequestHandle, referencesToAdd));

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "SampleServer.AddReferences", string.Format("Unexpected error AddReferences : request handle= {0}, references= {1} ", requestHeader.RequestHandle, referencesToAdd));

                lock (ServerInternal.DiagnosticsLock)
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
        /// Overrides the DeleteReferences UA service implementation handler. 
        /// NOTE: implementation of this method will become part of the Toolkit implementation
        /// and will be extended to handle all node managers defined by the server.
        /// </summary>
        /// <param name="requestHeader">The requestHeader parameter.</param>
        /// <param name="referencesToDelete">The referencesToDelete parameter.</param>
        /// <param name="results">The results parameter.</param>
        /// <param name="diagnosticInfos">The diagnosticInfos parameter.</param>
        public override ResponseHeader DeleteReferences(
            RequestHeader requestHeader,
            DeleteReferencesItemCollection referencesToDelete,
            out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            if (m_nodeManagementManager == null)
            {
                throw new ServiceResultException(StatusCodes.BadServiceUnsupported);
            }

            OperationContext context = ValidateRequest(requestHeader, RequestType.DeleteReferences);

            try
            {
                if (referencesToDelete == null || referencesToDelete.Count == 0)
                {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                m_nodeManagementManager.DeleteReferences(
                    context,
                    referencesToDelete,
                    out results,
                    out diagnosticInfos);

                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.DeleteReferences", string.Format("Finished DeleteReferences : request handle= {0}, references= {1} ", requestHeader.RequestHandle, referencesToDelete));

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "SampleServer.DeleteReferences", string.Format("Unexpected error DeleteReferences : request handle= {0}, references= {1} ", requestHeader.RequestHandle, referencesToDelete));

                lock (ServerInternal.DiagnosticsLock)
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

        #region UserAuthentication
        /// <summary>
        /// Called when a client tries to change its user identity
        /// </summary>
        private void SessionManager_ImpersonateUser(Session session, ImpersonateEventArgs args)
        {
            // Check for a user name token
            UserNameIdentityToken userNameToken = args.NewIdentity as UserNameIdentityToken;

            if (userNameToken != null)
            {
                VerifyPassword(userNameToken.UserName, userNameToken.DecryptedPassword);
                args.Identity = new UserIdentity(userNameToken);
                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.SessionManager_ImpersonateUser", string.Format("UserName Token Accepted: {0}", args.Identity.DisplayName));
                return;
            }

            // Check for x509 user token
            X509IdentityToken x509Token = args.NewIdentity as X509IdentityToken;

            if (x509Token != null)
            {
                VerifyCertificate(x509Token.Certificate);
                args.Identity = new UserIdentity(x509Token);
                Utils.Trace(Utils.TraceMasks.Information, "SampleServer.SessionManager_ImpersonateUser", string.Format("X509 Token Accepted: {0}", args.Identity.DisplayName));
            }
        }

        /// <summary>
        /// Validates the password for a username token
        /// </summary>
        private void VerifyPassword(string userName, string password)
        {
            if (String.IsNullOrEmpty(userName))
            {
                // An empty username is not accepted
                throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid, "Security token is not a valid username token. An empty username is not accepted.");
            }

            if (String.IsNullOrEmpty(password))
            {
                // An empty password is not accepted
                throw ServiceResultException.Create(StatusCodes.BadIdentityTokenRejected, "Security token is not a valid username token. An empty password is not accepted.");
            }

            if (userName != "usr" || password != "pwd")
            {
                // Construct translation object with default text
                TranslationInfo info = new TranslationInfo("InvalidPassword", "en-US", "Invalid username or Password.", userName);

                // Create an exception with a vendor defined sub-code
                throw new ServiceResultException(new ServiceResult(StatusCodes.BadUserAccessDenied, "InvalidPassword", "http://opcfoundation.org/UA/Sample/", new LocalizedText(info)));
            }
        }

        /// <summary>
        /// Verifies that a certificate user token is trusted
        /// </summary>
        private void VerifyCertificate(X509Certificate2 certificate)
        {
            try
            {
                CertificateValidator.Validate(certificate);

                // determine if self-signed.
                bool isSelfSigned = Utils.CompareDistinguishedName(certificate.Subject, certificate.Issuer);

                // do not allow self signed application certs as user token
                if (isSelfSigned && Utils.HasApplicationURN(certificate))
                {
                    throw new ServiceResultException(StatusCodes.BadCertificateUseNotAllowed);
                }
            }
            catch (Exception e)
            {
                TranslationInfo info;
                StatusCode result = StatusCodes.BadIdentityTokenRejected;
                ServiceResultException se = e as ServiceResultException;

                if (se != null && se.StatusCode == StatusCodes.BadCertificateUseNotAllowed)
                {
                    info = new TranslationInfo(
                        "InvalidCertificate",
                        "en-US",
                        "'{0}' is an invalid user certificate.",
                        certificate.Subject);

                    result = StatusCodes.BadIdentityTokenInvalid;
                }
                else
                {
                    // construct translation object with default text.
                    info = new TranslationInfo(
                        "UntrustedCertificate",
                        "en-US",
                        "'{0}' is not a trusted user certificate.",
                        certificate.Subject);
                }

                // create an exception with a vendor defined sub-code.
                throw new ServiceResultException(new ServiceResult(
                    result,
                    info.Key,
                    LoadServerProperties().ProductUri,
                    new LocalizedText(info)));
            }
        }
        #endregion

    }
}