/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 *  
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Gds;
using Softing.Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using static Opc.Ua.Utils;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class providing support for GDS pull and push
    /// </summary>
    public class GdsClient
    {
        #region Private Fields
        private const string SessionNamePush = "GdsClient Session - Push";
        private const string SessionNamePull = "GdsClient Session - Pull";
        private readonly UaApplication m_application;


        private const string GdsAdminUser = "appadmin";
        private const string GdsAdminPassword = "demo";

        private const MessageSecurityMode ConnectioSecurityMode = MessageSecurityMode.SignAndEncrypt;
        private const SecurityPolicy ConnectionSecurityPolicy = SecurityPolicy.Basic256Sha256;
        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of GdsClient
        /// </summary>
        /// <param name="application"></param>
        public GdsClient(UaApplication application)
        {
            m_application = application;
        }

        #endregion
       
        /// <summary>
        /// Executes sample code for GDS - Pull Register And Sign Certificate Scenario
        /// </summary>
        public void ExecutePullRegisterAndSignSample()
        {

            Console.WriteLine($"Connecting to configured GDS: '{m_application.GdsConnectionConfiguration.GdsUrl}'");
            Console.WriteLine("\nPlease provide GDS credentials:");
            UserNameIdentityToken gdsUserToken = new UserNameIdentityToken();
            Console.Write("Username:");
            gdsUserToken.UserName = GdsAdminUser;//Console.ReadLine();
            Console.Write("Password:");
            gdsUserToken.DecryptedPassword = GdsAdminPassword;//Console.ReadLine();
            UserIdentity gdsUserIdentity = new UserIdentity(gdsUserToken);

            try
            {
                m_application.GdsRegisterAndSignCertificate(gdsUserIdentity);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }

        /// <summary>
        /// Executes sample code for GDS - Pull Get Trust List Scenario
        /// </summary>
        public void ExecutePullGetTrustListSample()
        {

            Console.WriteLine($"Connecting to configured GDS: '{m_application.GdsConnectionConfiguration.GdsUrl}'");
            Console.WriteLine("\nPlease provide GDS credentials:");
            UserNameIdentityToken gdsUserToken = new UserNameIdentityToken();
            Console.Write("Username:");
            gdsUserToken.UserName = GdsAdminUser;//Console.ReadLine();
            Console.Write("Password:");
            gdsUserToken.DecryptedPassword = GdsAdminPassword;//Console.ReadLine();
            UserIdentity gdsUserIdentity = new UserIdentity(gdsUserToken);

            try
            {
                TrustListDataType[] tr = m_application.GdsGetTrustList(gdsUserIdentity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }

        /// <summary>
        /// Executes sample code for GDS - Push Scenario
        /// </summary>
        public void ExecutePushSample()
        {
            ClientSession gdsSession = null, uaServerSession = null;
            try
            {
                Console.WriteLine($"Connecting to configured GDS: '{m_application.GdsConnectionConfiguration.GdsUrl}', " +
                        $"SecurityMode={ConnectioSecurityMode}, SecurityPolicy={ConnectionSecurityPolicy}");
                Console.WriteLine("\nPlease provide GDS credentials:");  
                UserNameIdentityToken gdsUserToken = new UserNameIdentityToken();
                Console.Write("Username:");
                gdsUserToken.UserName = GdsAdminUser;//Console.ReadLine();
                Console.Write("Password:");
                gdsUserToken.DecryptedPassword = GdsAdminPassword;//Console.ReadLine();
                UserIdentity gdsUserIdentity = new UserIdentity(gdsUserToken);

                // create connection to GDS 
                gdsSession = m_application.CreateSession(m_application.GdsConnectionConfiguration.GdsUrl, ConnectioSecurityMode, ConnectionSecurityPolicy, MessageEncoding.Binary, gdsUserIdentity);
                gdsSession.SessionName = SessionNamePush;
                gdsSession.Connect(true, true);   
                Console.WriteLine($"Connection to GDS is established.");

                Console.WriteLine($"\n\nConnecting to configured OPC UA Server for GDS Push: '{Program.ServerUrl}', " +
                        $"SecurityMode={ConnectioSecurityMode}, SecurityPolicy={ConnectionSecurityPolicy}");
                Console.WriteLine("\nPlease provide GDS credentials:");

                // create user identity that has SystemConfigurationIdentity credentials
                UserNameIdentityToken pushUserToken = new UserNameIdentityToken();
                Console.Write("Username:");
                pushUserToken.UserName = "admin";// Console.ReadLine();
                Console.Write("Password:");
                pushUserToken.DecryptedPassword = "admin";//Console.ReadLine();
                UserIdentity pushUserIdentity = new UserIdentity(pushUserToken);

                // create connection to Opc Ua Server being pushed the certificate
                uaServerSession = m_application.CreateSession(Program.ServerUrl, ConnectioSecurityMode, ConnectionSecurityPolicy, MessageEncoding.Binary, pushUserIdentity);
                uaServerSession.SessionName = SessionNamePush;
                uaServerSession.Connect(true, true);
                Console.WriteLine($"Connection to '{Program.ServerUrl}' established.");

                Console.WriteLine($"\n\nCreate a SigningRequest to '{Program.ServerUrl}'.");
                NodeId certificateTypeId = Opc.Ua.ObjectTypeIds.RsaSha256ApplicationCertificateType;
                byte[] certificateRequest = CreateSigningRequest(uaServerSession,
                    NodeId.Null, //certificateGroupId,
                    certificateTypeId,
                    null,       // subjectName  -> not supported yet
                    false,      // regeneratePrivateKey -> not supported yet
                    null        // nonce -> not supported yet
                    );              
                
                if (certificateRequest != null)
                {
                    Console.WriteLine($"SigningRequest was successfully created.");

                    Console.WriteLine($"\n\nGet ApplicationID for '{Program.ServerUrl}'.");
                    // get application id for OPC UA Server
                    NodeId applicationId = GetApplicationId(gdsSession, uaServerSession.CoreSession.Endpoint.Server);

                    if (applicationId != null)
                    {
                        Console.WriteLine($"ApplicationID for '{Program.ServerUrl}' is {applicationId}.");

                        // call StartSigningRequest on GSD 
                        Console.WriteLine($"\n\nCall StartSigningRequest for ApplicationID:{applicationId}.");
                        NodeId requestId = StartSigningRequest(gdsSession, applicationId, null, null, certificateRequest);

                        if (requestId != null)
                        {
                            Console.WriteLine($"StartSigningRequest for ApplicationID:{applicationId} returned RequestId:{requestId}.");

                            Console.WriteLine($"\n\nCall FinishRequest for ApplicationID:{applicationId} and RequestId:{requestId}.");
                            while (true)
                            {
                                byte[] certificate = null;
                                byte[] privateKey = null;
                                byte[][] issuerCertificates = null;

                                FinishRequest(gdsSession, applicationId, requestId, out certificate, out privateKey, out issuerCertificates);

                                if (certificate == null)
                                {
                                    // request not done yet, try again in a few seconds
                                    Console.WriteLine($"Request {requestId} not done yet. Try again in 1 second.");
                                    Thread.Sleep(1000);
                                    continue;
                                }
                                else
                                {
                                    Console.WriteLine($"Request {requestId} is finished.");

                                    Console.WriteLine($"\n\nCall UpdateCertificate on {Program.ServerUrl}.");

                                    if (privateKey != null && privateKey.Length > 0)
                                    {
                                        var x509 = new X509Certificate2(privateKey, (string)null, X509KeyStorageFlags.Exportable);
                                        privateKey = x509.Export(X509ContentType.Pfx);
                                    }                                   

                                    bool updateCertificateResult = UpdateCertificate(uaServerSession,
                                        null, // certificateGroupId                                        
                                        certificateTypeId,
                                        certificate,
                                        issuerCertificates,
                                        (privateKey != null) ? "PFX" : null,
                                        privateKey);
                                    Console.WriteLine($"UpdateCertificate method returned {updateCertificateResult}.");

                                    if (updateCertificateResult)
                                    {
                                        Console.WriteLine($"\n\nCall ApplyChanges on {Program.ServerUrl}.");
                                        ApplyChanges(uaServerSession);
                                        Console.WriteLine($"ApplyChanges was called on {Program.ServerUrl}.");
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"StartSigningRequest for ApplicationID:{applicationId} returned RequestId: NULL.");
                        }

                        Console.WriteLine($"UnregisterApplication  for ApplicationID:{applicationId}.");
                        UnregisterApplication(gdsSession, applicationId);
                    }
                    else
                    {
                        Console.WriteLine($"ApplicationID for '{Program.ServerUrl}' is NULL.");
                    }
                }
                else
                {
                    Console.WriteLine($"SigningRequest encountered a problem.");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
            finally
            {
                if (gdsSession != null)
                {
                    gdsSession.Disconnect(true);
                    gdsSession.Dispose();
                }
                if (uaServerSession != null)
                {
                    uaServerSession.Disconnect(true);
                    uaServerSession.Dispose();
                }
            }
        }

        /// <summary>
        /// Create a signing request for the opc ua Push server
        /// </summary>
        /// <param name="uaServerSession"></param>
        /// <param name="certificateGroupId"></param>
        /// <param name="certificateTypeId"></param>
        /// <param name="subjectName"></param>
        /// <param name="regeneratePrivateKey"></param>
        /// <param name="nonce"></param>
        /// <returns></returns>
        private byte[] CreateSigningRequest(ClientSession uaServerSession, NodeId certificateGroupId, NodeId certificateTypeId, string subjectName, bool regeneratePrivateKey, byte[] nonce)
        {
            if (uaServerSession == null || uaServerSession.CurrentState != State.Active)
            {
                return null;
            }
            List<object> inputArgumentsCreateSigningRequest = new List<object>()
                {
                  certificateGroupId,
                   certificateTypeId,
                   subjectName,
                   regeneratePrivateKey,
                   nonce
                };
           
            IList<object> outputArgumentsCreateSigningRequest = new List<object>();

            // create certificate request by calling CreateSigningRequest method on OPC UA Push Server
            var result = uaServerSession.Call(
               Opc.Ua.ObjectIds.ServerConfiguration,
               Opc.Ua.MethodIds.ServerConfiguration_CreateSigningRequest,
               inputArgumentsCreateSigningRequest,
               out outputArgumentsCreateSigningRequest);

            if (outputArgumentsCreateSigningRequest.Count > 0)
            {
                return (byte[])outputArgumentsCreateSigningRequest[0];
            }
            return null;
        }

        /// <summary>
        /// Get the ApplicationId for specified <see cref="ApplicationDescription"/>
        /// </summary>
        /// <param name="gdsSession"></param>
        /// <param name="applicationDescription"></param>
        /// <returns></returns>
        private NodeId GetApplicationId(ClientSession gdsSession, ApplicationDescription applicationDescription)
        {
            if (gdsSession == null || gdsSession.CurrentState != State.Active)
            {
                return null;
            }
            //search for registered application
            List<object> inputArgumentsFindApplications = new List<object>() { applicationDescription.ApplicationUri };
            IList<object> outputArgumentsFindApplications;
            var result = gdsSession.Call(
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.ObjectIds.Directory, gdsSession.CoreSession.NamespaceUris),
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.MethodIds.Directory_FindApplications, gdsSession.CoreSession.NamespaceUris), 
                inputArgumentsFindApplications, out outputArgumentsFindApplications);

            if (outputArgumentsFindApplications != null && outputArgumentsFindApplications.Count > 0)
            {
                var applications = (ApplicationRecordDataType[])ExtensionObject.ToArray(outputArgumentsFindApplications[0] as ExtensionObject[], typeof(ApplicationRecordDataType));
                if (applications.Length > 0)
                {
                    NodeId applicationId = ((ApplicationRecordDataType)applications.GetValue(0)).ApplicationId;
                    Utils.Trace(TraceMasks.Information, $"ApplicationUri: {applicationDescription.ApplicationUri} was registered with id: {applicationId}.");
                    return applicationId;
                }
            }

            Utils.Trace(TraceMasks.Information, $"Register ApplicationUri: {applicationDescription.ApplicationUri}.");
            // register application because it was not registered yet
            List<object> inputArgumentsRegisterApplication = new List<object>() {
                new ApplicationRecordDataType
                {
                    ApplicationNames = new LocalizedTextCollection { applicationDescription.ApplicationName },
                    ApplicationUri =  applicationDescription.ApplicationUri,
                    ApplicationType = ApplicationType.Server,
                    ProductUri = applicationDescription.ProductUri,
                    DiscoveryUrls = applicationDescription.DiscoveryUrls,
                    ServerCapabilities = new StringCollection { "NA" },
                }
            };
            IList<object> outputArgumentsRegisterApplication;
            result = gdsSession.Call(
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.ObjectIds.Directory, gdsSession.CoreSession.NamespaceUris),
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.MethodIds.Directory_RegisterApplication, gdsSession.CoreSession.NamespaceUris),
                inputArgumentsRegisterApplication, out outputArgumentsRegisterApplication);

            if (outputArgumentsRegisterApplication != null && outputArgumentsRegisterApplication.Count > 0)
            {
                NodeId applicationId = outputArgumentsRegisterApplication[0] as NodeId;
                Utils.Trace(TraceMasks.Information, $"ApplicationUri: {applicationDescription.ApplicationUri} was registered with id: {applicationId}.");
                return applicationId;
            }

            return null;
        }

        /// <summary>
        /// Call UnregisterApplication for the specified applicationId 
        /// </summary>
        /// <param name="gdsSession"></param>
        /// <param name="applicationId"></param>
        private void UnregisterApplication(ClientSession gdsSession, NodeId applicationId)
        {
            if (gdsSession == null || gdsSession.CurrentState != State.Active)
            {
                return;
            }
            //search for registered application
            List<object> inputArgumentsUnregisterApplication = new List<object>() {applicationId };
            IList<object> outputArgumentsUnregisterApplication;
            var result = gdsSession.Call(
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.ObjectIds.Directory, gdsSession.CoreSession.NamespaceUris),
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.MethodIds.Directory_UnregisterApplication, gdsSession.CoreSession.NamespaceUris),
                inputArgumentsUnregisterApplication, out outputArgumentsUnregisterApplication);

        }
        
        /// <summary>
        /// Starts a signing request to the GDS
        /// </summary>
        /// <param name="gdsSession"></param>
        /// <param name="applicationId"></param>
        /// <param name="certificateGroupId"></param>
        /// <param name="certificateTypeId"></param>
        /// <param name="certificateRequest"></param>
        /// <returns></returns>
        private NodeId StartSigningRequest(ClientSession gdsSession, NodeId applicationId, NodeId certificateGroupId, NodeId certificateTypeId, byte[] certificateRequest)
        {
            if (gdsSession == null || gdsSession.CurrentState != State.Active)
            {
                return null;
            }
            //start signing request on GDS
            List<object> inputArgumentsStartSigningRequest = new List<object>() { applicationId, certificateGroupId, certificateTypeId, certificateRequest };
            IList<object> outputArgumentsStartSigningRequest;
            var result = gdsSession.Call(
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.ObjectIds.Directory, gdsSession.CoreSession.NamespaceUris),
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.MethodIds.Directory_StartSigningRequest, gdsSession.CoreSession.NamespaceUris),
                inputArgumentsStartSigningRequest, out outputArgumentsStartSigningRequest);

            if (outputArgumentsStartSigningRequest != null && outputArgumentsStartSigningRequest.Count > 0)
            {
                return outputArgumentsStartSigningRequest[0] as NodeId;
            }
            return null;
        }

        /// <summary>
        ///  Call FinishRequest on GDS
        /// </summary>
        /// <param name="gdsSession"></param>
        /// <param name="applicationId"></param>
        /// <param name="requestId"></param>
        /// <param name="certificate"></param>
        /// <param name="privateKey"></param>
        /// <param name="issuerCertificates"></param>
        private void FinishRequest(ClientSession gdsSession, NodeId applicationId, NodeId requestId, out byte[] certificate, out byte[] privateKey, out byte[][] issuerCertificates)
        {
            certificate = null;
            privateKey = null;
            issuerCertificates = null;
            if (gdsSession == null || gdsSession.CurrentState != State.Active)
            {
                return;
            }

            //call finish request on GDS
            List<object> inputArgumentsFinishRequest = new List<object>() { applicationId, requestId };
            IList<object> outputArgumentsFinishRequest;
            var result = gdsSession.Call(
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.ObjectIds.Directory, gdsSession.CoreSession.NamespaceUris),
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.MethodIds.Directory_FinishRequest, gdsSession.CoreSession.NamespaceUris),
                inputArgumentsFinishRequest, out outputArgumentsFinishRequest);
            
            if (outputArgumentsFinishRequest != null && outputArgumentsFinishRequest.Count >= 1)
            {
                certificate = outputArgumentsFinishRequest[0] as byte[];
            }
            if (outputArgumentsFinishRequest != null && outputArgumentsFinishRequest.Count >= 2)
            {
                privateKey = outputArgumentsFinishRequest[1] as byte[];
            }
            if (outputArgumentsFinishRequest != null && outputArgumentsFinishRequest.Count >= 3)
            {
                issuerCertificates = outputArgumentsFinishRequest[2] as byte[][];
            }
        }

        /// <summary>
        /// Calls UpdateCertificate on target OPC UA Push Server and returns a flag that indicates if the operation was successful
        /// </summary>
        /// <param name="uaServerSession"></param>
        /// <param name="certificateGroupId"></param>
        /// <param name="certificateTypeId"></param>
        /// <param name="certificate"></param>
        /// <param name="privateKeyFormat"></param>
        /// <param name="privateKey"></param>
        /// <param name="issuerCertificates"></param>
        /// <returns></returns>
        private bool UpdateCertificate(ClientSession uaServerSession,
            NodeId certificateGroupId,
            NodeId certificateTypeId,
            byte[] certificate,
            byte[][] issuerCertificates,
            string privateKeyFormat,
            byte[] privateKey)
        {
            if (uaServerSession == null || uaServerSession.CurrentState != State.Active)
            {
                return false;
            }
            List<object> inputArgumentsUpdateCertificate = new List<object>()
                {
                    certificateGroupId,
                    certificateTypeId,
                    certificate,
                    issuerCertificates,
                    privateKeyFormat,
                    privateKey
                };            
            IList<object> outputArgumentsUpdateCertificate = new List<object>();

            var result = uaServerSession.Call(
               Opc.Ua.ObjectIds.ServerConfiguration,
               Opc.Ua.MethodIds.ServerConfiguration_UpdateCertificate,
               inputArgumentsUpdateCertificate,
               out outputArgumentsUpdateCertificate);

            if (outputArgumentsUpdateCertificate.Count > 0)
            {
                return (bool)outputArgumentsUpdateCertificate[0];
            };
            return false;
        }

        /// <summary>
        /// Calls ApplyChanges on target OPC UA Push Server and returns a flag that indicates if the operation was successful
        /// </summary>
        /// <param name="uaServerSession"></param>
        private void ApplyChanges(ClientSession uaServerSession)
        {
            if (uaServerSession == null || uaServerSession.CurrentState != State.Active)
            {
                return;
            }
            IList<object> outputArgumentsApplyChanges = new List<object>();
            var result = uaServerSession.Call(
               Opc.Ua.ObjectIds.ServerConfiguration,
               Opc.Ua.MethodIds.ServerConfiguration_ApplyChanges, new List<object>(), out outputArgumentsApplyChanges);
        }
        
    }
}
