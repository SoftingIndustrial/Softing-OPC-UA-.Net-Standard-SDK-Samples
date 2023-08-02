/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using Opc.Ua;
using Softing.Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that contains sample code for AccessRestrictions, RolePermissions and UserRolePermissions
    /// </summary>
    public class AccessRightsClient
    {
        #region Private Fields

        private const string SessionNameNoSecurity = "No Security Session";
        private const string SessionNameSign = "Signed Session";
        private const string SessionNameEncrypted = "Encrypted Session";

        private const string NodeIdAccessRestrictionsNone = "ns=7;s=CTT_AccessRights_AccessRestrictions_None";
        private const string NodeIdAccessRestrictionsSigningRequired = "ns=7;s=CTT_AccessRights_AccessRestrictions_SigningRequired";
        private const string NodeIdAccessRestrictionsEncryptionRequired = "ns=7;s=CTT_AccessRights_AccessRestrictions_EncryptionRequired";
        private const string NodeIdAccessRestrictionsSessionRequired = "ns=7;s=CTT_AccessRights_AccessRestrictions_SessionRequired";

        private const string SessionNameAnonymous = "Anonymous User Session";
        private const string SessionNameAuthenticated = "Authenticated User Session";
        private const string SessionNameOperator = "Operator User Session";
        private const string SessionNameEngineer = "Engineer User Session";

        private const string NodeIdRolePermissionsAnonymous = "ns=7;s=CTT_AccessRights_RolePermissions_AnonymousAccess";
        private const string NodeIdRolePermissionsAuthenticated = "ns=7;s=CTT_AccessRights_RolePermissions_AuthenticatedAccess";
        private const string NodeIdRolePermissionsOperator = "ns=7;s=CTT_AccessRights_RolePermissions_OperatorAccess";

        private const string NodeIdUserRolePermissionsAllForOperator = "ns=7;s=CTT_AccessRights_RolePermissions_UserRolePermissionsForOperator";
        private const string NodeIdUserRolePermissionsForEngineer = "ns=7;s=CTT_AccessRights_RolePermissions_UserRolePermissionsForEngineeer";


        private const string AuthenticatedUser = "usr";
        private const string Password = "pwd";
        private const string OperatorUser = "operator1";
        private const string EngineerUser = "engineer";
        private readonly UaApplication m_application;
        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of AccessRightsClient
        /// </summary>
        public AccessRightsClient()
        {
            // initialize the UaApplication with config file
            m_application = UaApplication.Create("SampleClient.Config.xml").Result;
            m_application.Configuration.CertificateValidator.CertificateValidation += Program.CertificateValidator_CertificateValidation;
            // set the flags to avoid loading custom data types 
            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = false;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Read nodes from Objects\CTT\AccessRights\AccessRestrictions with sessions created with various security settings
        /// </summary>
        public async Task SampleAccessRestrictions()
        {
            Console.WriteLine(@"Read the nodes from SampleServer Objects\CTT\AccessRights\AccessRestrictions folder with various sessions security options.");
            // create a session with no security 
            using (ClientSession noSecuritySession = CreateSession(SessionNameNoSecurity, Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity()))
            {
                try
                {
                    await noSecuritySession.ConnectAsync(true, true).ConfigureAwait(false);

                    // read value attribute for all variables under Objects\CTT\AccessRights\AccessRestrictions folder
                    ReadVariableAttribute(noSecuritySession, new NodeId(NodeIdAccessRestrictionsNone), (uint)Attributes.Value);
                    ReadVariableAttribute(noSecuritySession, new NodeId(NodeIdAccessRestrictionsSigningRequired), (uint)Attributes.Value);
                    ReadVariableAttribute(noSecuritySession, new NodeId(NodeIdAccessRestrictionsEncryptionRequired), (uint)Attributes.Value);
                    ReadVariableAttribute(noSecuritySession, new NodeId(NodeIdAccessRestrictionsSessionRequired), (uint)Attributes.Value);

                    await noSecuritySession.DisconnectAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Program.PrintException("AccessRightsClient.SampleAccessRestrictions", ex);
                }
            }

            // create a session with signing but no encyption
            using (ClientSession signSession = CreateSession(SessionNameSign, Program.ServerUrl,
                MessageSecurityMode.Sign, SecurityPolicy.Basic256Sha256, MessageEncoding.Binary, new UserIdentity()))
            {
                try
                {
                    await signSession.ConnectAsync(true, true).ConfigureAwait(false);
                    // read value attribute for all variables under Objects\CTT\AccessRights\AccessRestrictions folder
                    ReadVariableAttribute(signSession, new NodeId(NodeIdAccessRestrictionsNone), (uint)Attributes.Value);
                    ReadVariableAttribute(signSession, new NodeId(NodeIdAccessRestrictionsSigningRequired), (uint)Attributes.Value);
                    ReadVariableAttribute(signSession, new NodeId(NodeIdAccessRestrictionsEncryptionRequired), (uint)Attributes.Value);
                    ReadVariableAttribute(signSession, new NodeId(NodeIdAccessRestrictionsSessionRequired), (uint)Attributes.Value);

                    await signSession.DisconnectAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Program.PrintException("AccessRightsClient.SampleAccessRestrictions", ex);
                }
            }

            // create a session with encyption
            using (ClientSession encryptionSession = CreateSession(SessionNameEncrypted, Program.ServerUrl,
                MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256, MessageEncoding.Binary, new UserIdentity()))
            {
                try
                {
                    await encryptionSession.ConnectAsync(true, true).ConfigureAwait(false);
                    // read value attribute for all variables under Objects\CTT\AccessRights\AccessRestrictions folder
                    ReadVariableAttribute(encryptionSession, new NodeId(NodeIdAccessRestrictionsNone), (uint)Attributes.Value);
                    ReadVariableAttribute(encryptionSession, new NodeId(NodeIdAccessRestrictionsSigningRequired), (uint)Attributes.Value);
                    ReadVariableAttribute(encryptionSession, new NodeId(NodeIdAccessRestrictionsEncryptionRequired), (uint)Attributes.Value);
                    ReadVariableAttribute(encryptionSession, new NodeId(NodeIdAccessRestrictionsSessionRequired), (uint)Attributes.Value);

                    await encryptionSession.DisconnectAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Program.PrintException("AccessRightsClient.SampleAccessRestrictions", ex);
                }
            }
        }

        /// <summary>
        /// Access nodes from  Objects\CTT\AccessRights\RolePermissions with sessions created with various users that have specific user roles in SampleServer
        /// </summary>
        public async Task SampleRolePermissions()
        {
            Console.WriteLine(@"Read the nodes from SampleServer Objects\CTT\AccessRights\RolePermissions folder with various user identities.");
            // create a session for anonymous user
            using (ClientSession anonymousSession = CreateSession(SessionNameNoSecurity, Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity()))
            {
                try
                {
                    await anonymousSession.ConnectAsync(true, true).ConfigureAwait(false);
                    // read value attribute for some variables under Objects\CTT\AccessRights\RolePermissions folder
                    ReadVariableAttribute(anonymousSession, new NodeId(NodeIdRolePermissionsAnonymous), (uint)Attributes.Value);
                    ReadVariableAttribute(anonymousSession, new NodeId(NodeIdRolePermissionsAuthenticated), (uint)Attributes.Value);
                    ReadVariableAttribute(anonymousSession, new NodeId(NodeIdRolePermissionsOperator), (uint)Attributes.Value);

                    await anonymousSession.DisconnectAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Program.PrintException("AccessRightsClient.SampleRolePermissions", ex);
                }
            }

            // create a session for authenticated user
            using (ClientSession authenticatedSession = CreateSession(SessionNameAuthenticated, Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity(AuthenticatedUser, Password)))
            {
                try
                {
                    await authenticatedSession.ConnectAsync(true, true).ConfigureAwait(false);
                    // read value attribute for some variables under Objects\CTT\AccessRights\RolePermissions folder
                    ReadVariableAttribute(authenticatedSession, new NodeId(NodeIdRolePermissionsAnonymous), (uint)Attributes.Value);
                    ReadVariableAttribute(authenticatedSession, new NodeId(NodeIdRolePermissionsAuthenticated), (uint)Attributes.Value);
                    ReadVariableAttribute(authenticatedSession, new NodeId(NodeIdRolePermissionsOperator), (uint)Attributes.Value);

                    await authenticatedSession.DisconnectAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Program.PrintException("AccessRightsClient.SampleRolePermissions", ex);
                }
            }

            // create a session for operator user
            using (ClientSession operatorSession = CreateSession(SessionNameOperator, Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity(OperatorUser, Password)))
            {
                try
                {
                    await operatorSession.ConnectAsync(true, true).ConfigureAwait(false);
                    // read value attribute for some variables under Objects\CTT\AccessRights\RolePermissions folder
                    ReadVariableAttribute(operatorSession, new NodeId(NodeIdRolePermissionsAnonymous), (uint)Attributes.Value);
                    ReadVariableAttribute(operatorSession, new NodeId(NodeIdRolePermissionsAuthenticated), (uint)Attributes.Value);
                    ReadVariableAttribute(operatorSession, new NodeId(NodeIdRolePermissionsOperator), (uint)Attributes.Value);

                    await operatorSession.DisconnectAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Program.PrintException("AccessRightsClient.SampleRolePermissions", ex);
                }
            }
        }

        /// <summary>
        /// Access nodes from  Objects\CTT\AccessRights\UserRolePermissions with sessions created with various users that have specific user roles in SampleServer
        /// </summary>
        public async Task SampleUserRolePermissions()
        {
            // create a session for operator user
            using (ClientSession anonymousSession = CreateSession(SessionNameAnonymous, Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity()))
            {
                try
                {
                    await anonymousSession.ConnectAsync(true, true).ConfigureAwait(false);
                    // read value attribute for some variables under Objects\CTT\AccessRights\RolePermissions folder that have user roles handled
                    ReadVariableAttribute(anonymousSession, new NodeId(NodeIdUserRolePermissionsAllForOperator), (uint)Attributes.Value);
                    ReadVariableAttribute(anonymousSession, new NodeId(NodeIdUserRolePermissionsForEngineer), (uint)Attributes.Value);

                    await anonymousSession.DisconnectAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Program.PrintException("AccessRightsClient.SampleUserRolePermissions", ex);
                }
            }
            // create a session for operator user
            using (ClientSession operatorSession = CreateSession(SessionNameOperator, Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity(OperatorUser, Password)))
            {
                try
                {
                    await operatorSession.ConnectAsync(true, true).ConfigureAwait(false);
                    // read value attribute for some variables under Objects\CTT\AccessRights\RolePermissions folder that have user roles handled
                    ReadVariableAttribute(operatorSession, new NodeId(NodeIdUserRolePermissionsAllForOperator), (uint)Attributes.Value);
                    ReadVariableAttribute(operatorSession, new NodeId(NodeIdUserRolePermissionsForEngineer), (uint)Attributes.Value);

                    await operatorSession.DisconnectAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Program.PrintException("AccessRightsClient.SampleUserRolePermissions", ex);
                }
            }

            // create a session for engineer user
            using (ClientSession engineerSession = CreateSession(SessionNameEngineer, Program.ServerUrl,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, new UserIdentity(EngineerUser, Password)))
            {
                try
                {
                    await engineerSession.ConnectAsync(true, true).ConfigureAwait(false);
                    // read value attribute for some variables under Objects\CTT\AccessRights\RolePermissions folder that have user roles handled
                    ReadVariableAttribute(engineerSession, new NodeId(NodeIdUserRolePermissionsAllForOperator), (uint)Attributes.Value);
                    ReadVariableAttribute(engineerSession, new NodeId(NodeIdUserRolePermissionsForEngineer), (uint)Attributes.Value);

                    await engineerSession.DisconnectAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Program.PrintException("AccessRightsClient.SampleUserRolePermissions", ex);
                }
            }
        }

        /// <summary>
        /// Cleanup resources used by this instance
        /// </summary>
        public void Dispose()
        {
            // detach static event handler
            m_application.Configuration.CertificateValidator.CertificateValidation -= Program.CertificateValidator_CertificateValidation;
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Creates and connects an new session with the specified parameters.
        /// </summary>        
        private ClientSession CreateSession(string sessionName, string serverUrl, MessageSecurityMode securityMode,
            SecurityPolicy securityPolicy, MessageEncoding messageEncoding, UserIdentity userId)
        {
            try
            {
                Console.WriteLine("\n Creating the session '{0}' (SecurityMode = {1}, SecurityPolicy = {2}, UserIdentity = {3})...",
                    sessionName, securityMode, securityPolicy, userId.GetIdentityToken());
                // Create the Session object.
                ClientSession session = m_application.CreateSession(serverUrl, securityMode, securityPolicy, messageEncoding, userId);

                session.SessionName = sessionName;
                return session;
            }
            catch (Exception ex)
            {
                Program.PrintException("AccessRightsClient.CreateSession", ex);
                return null;
            }
        }
        
        /// <summary>
        /// Read attribute for specified node id from specified session
        /// </summary>
        /// <param name="clientSession"></param>
        /// <param name="nodeId"></param>
        /// <param name="attribute"></param>
        private void ReadVariableAttribute(ClientSession clientSession, NodeId nodeId, uint attribute)
        {
            ReadValueId readValueId = new ReadValueId()
            {
                NodeId = nodeId,
                AttributeId = attribute
            };

            try
            {
                DataValueEx dataValueEx = clientSession.Read(readValueId);
                Console.WriteLine("\tRead '{0}' for '{1}' has StatusCode:{2}", Attributes.GetBrowseName(attribute), nodeId, dataValueEx.StatusCode);
                
            }
            catch(Exception ex)
            {
                Console.WriteLine("tRead '{0}' for '{1}' throws Exception:{2}", Attributes.GetBrowseName(attribute), nodeId, ex.Message);
            }
        }       
        
        #endregion
    }
}
