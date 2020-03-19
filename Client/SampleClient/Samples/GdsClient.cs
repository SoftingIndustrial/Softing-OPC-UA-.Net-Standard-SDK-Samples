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
using System.IO;
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

        private const string SampleServerAdminUser = "admin";
        private const string SampleServerAdminPassword = "admin";

        private const MessageSecurityMode ConnectionSecurityMode = MessageSecurityMode.SignAndEncrypt;
        private const SecurityPolicy ConnectionSecurityPolicy = SecurityPolicy.Basic256Sha256;
        #endregion

        #region Properties
        GdsConnectionConfiguration GdsConnectionConfiguration { get; }
        #endregion Properties

        #region Constructor

        /// <summary>
        /// Create new instance of GdsClient
        /// </summary>
        /// <param name="application"></param>
        public GdsClient(UaApplication application)
        {
            m_application = application;
            GdsConnectionConfiguration = m_application.Configuration.ParseExtension<GdsConnectionConfiguration>();
        }

        #endregion
       
        /// <summary>
        /// Executes sample code for GDS - Pull Register And Sign Certificate Scenario
        /// </summary>
        public void ExecutePullRegisterAndSignSample()
        {

            Console.WriteLine("Connecting to configured GDS: '{0}'", GdsConnectionConfiguration.GdsUrl);
            UserNameIdentityToken gdsUserToken = new UserNameIdentityToken();
            gdsUserToken.UserName = GdsAdminUser;
            gdsUserToken.DecryptedPassword = GdsAdminPassword;
            UserIdentity gdsUserIdentity = new UserIdentity(gdsUserToken);

            try
            {
                //get GdsConnectionConfiguration for this  UaApplication
                m_application.GdsRegisterAndSignCertificate(GdsConnectionConfiguration, gdsUserIdentity);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Executes sample code for GDS - Pull Get Trust List Scenario
        /// </summary>
        public void ExecutePullGetTrustListSample()
        {

            Console.WriteLine("Connecting to configured GDS: '{0}'", GdsConnectionConfiguration.GdsUrl);
            UserNameIdentityToken gdsUserToken = new UserNameIdentityToken();
            gdsUserToken.UserName = GdsAdminUser;
            gdsUserToken.DecryptedPassword = GdsAdminPassword;
            UserIdentity gdsUserIdentity = new UserIdentity(gdsUserToken);

            try
            {
                GdsConnectionConfiguration gdsConnectionConfiguration = m_application.Configuration.ParseExtension<GdsConnectionConfiguration>();
                TrustListDataType[] tr = m_application.GdsGetTrustList(gdsConnectionConfiguration, gdsUserIdentity);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Executes sample code for GDS - Push Application certificate Scenario
        /// </summary>
        public void ExecutePushCertificateSample()
        {
            ClientSession gdsSession = null, uaServerSession = null;
            try
            {
                
 				// create connection to GDS 
                gdsSession = GetGdsClientSession();

                // create connection to OPC UA Server 
                uaServerSession = GetPushServerClientSession();
                Console.WriteLine("\n\nCreate a SigningRequest to '{0}'.", Program.ServerUrl);
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
                    Console.WriteLine("SigningRequest was successfully created.");

                    Console.WriteLine("\n\nGet ApplicationID for '{0}'.", Program.ServerUrl);
                    // get application id for OPC UA Server
                    NodeId applicationId = GetApplicationId(gdsSession, uaServerSession.CoreSession.Endpoint.Server);

                    if (applicationId != null)
                    {
                        Console.WriteLine("ApplicationID for '{0}' is {1}.", Program.ServerUrl, applicationId);

                        // call StartSigningRequest on GDS 
                        Console.WriteLine("\n\nCall StartSigningRequest for ApplicationID:{0}.", applicationId);
                        NodeId requestId = StartSigningRequest(gdsSession, applicationId, null, null, certificateRequest);

                        if (requestId != null)
                        {
                            Console.WriteLine("StartSigningRequest for ApplicationID:{0} returned RequestId:{1}.", applicationId, requestId);

                            Console.WriteLine("\n\nCall FinishRequest for ApplicationID:{0} and RequestId:{1}.", applicationId, requestId);
                            while (true)
                            {
                                byte[] certificate = null;
                                byte[] privateKey = null;
                                byte[][] issuerCertificates = null;

                                FinishRequest(gdsSession, applicationId, requestId, out certificate, out privateKey, out issuerCertificates);

                                if (certificate == null)
                                {
                                    // request not done yet, try again in a few seconds
                                    Console.WriteLine("Request {0} not done yet. Try again in 1 second.", requestId);
                                    Thread.Sleep(1000);
                                    continue;
                                }
                                else
                                {
                                    Console.WriteLine("Request {0} is finished.", requestId);

                                    Console.WriteLine("\n\nCall UpdateCertificate on {0}.", Program.ServerUrl);

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
                                    Console.WriteLine("UpdateCertificate method returned {0}.", updateCertificateResult);

                                    if (updateCertificateResult)
                                    {
                                        Console.WriteLine("\n\nCall ApplyChanges on {0}.", Program.ServerUrl);
                                        ApplyChanges(uaServerSession);
                                        Console.WriteLine("ApplyChanges was called on {0}.", Program.ServerUrl);                                        
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("StartSigningRequest for ApplicationID:{0} returned RequestId: NULL.", applicationId);
                        }

                        Console.WriteLine("UnregisterApplication  for ApplicationID:{0}.", applicationId);
                        UnregisterApplication(gdsSession, applicationId);
                    }
                    else
                    {
                        Console.WriteLine("ApplicationID for '{0}' is NULL.", Program.ServerUrl);
                    }
                }
                else
                {
                    Console.WriteLine("SigningRequest encountered a problem.");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("ExecutePushCertificateSample Error: {0}", ex.Message);
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
        /// Creates the connection to configured GDS 
        /// </summary>
        /// <returns></returns>
        private ClientSession GetGdsClientSession()
        {
            Console.WriteLine("Connecting to configured GDS: '{0}', SecurityMode={1}, SecurityPolicy={2}",
                       GdsConnectionConfiguration.GdsUrl,
                       GdsConnectionConfiguration.MessageSecurityMode,
                       GdsConnectionConfiguration.SecurityPolicy);
            UserNameIdentityToken gdsUserToken = new UserNameIdentityToken();           
            gdsUserToken.UserName = GdsAdminUser;
            gdsUserToken.DecryptedPassword = GdsAdminPassword;
            UserIdentity gdsUserIdentity = new UserIdentity(gdsUserToken);

            // create connection to GDS 
            ClientSession gdsSession = m_application.CreateSession(GdsConnectionConfiguration.GdsUrl,
                        GdsConnectionConfiguration.MessageSecurityMode,
                        GdsConnectionConfiguration.SecurityPolicy,
                        GdsConnectionConfiguration.MessageEncoding,
                        gdsUserIdentity);
            gdsSession.SessionName = SessionNamePush;
            gdsSession.Connect(true, true);
            Console.WriteLine("Connection to '{0}' established.", GdsConnectionConfiguration.GdsUrl);

            return gdsSession;
        }

        /// <summary>
        /// Creates the connection to Push Server
        /// </summary>
        /// <returns></returns>
        private ClientSession GetPushServerClientSession()
        {
            Console.WriteLine("\n\nConnecting to configured OPC UA Server for GDS Push: '{0}', SecurityMode={1}, SecurityPolicy={2}",
                         Program.ServerUrl, ConnectionSecurityMode, ConnectionSecurityPolicy);

            // create user identity that has SystemConfigurationIdentity credentials on PushServer
            UserNameIdentityToken pushUserToken = new UserNameIdentityToken();
            pushUserToken.UserName = SampleServerAdminUser;
            pushUserToken.DecryptedPassword = SampleServerAdminPassword;
            UserIdentity pushUserIdentity = new UserIdentity(pushUserToken);

            // create connection to Opc Ua Server being pushed the certificate
            ClientSession uaServerSession = m_application.CreateSession(Program.ServerUrl,
                ConnectionSecurityMode,
                ConnectionSecurityPolicy,
                MessageEncoding.Binary,
                pushUserIdentity);
            uaServerSession.SessionName = SessionNamePush;
            uaServerSession.Connect(true, true);
            Console.WriteLine("Connection to '{0}' established.", Program.ServerUrl);

            return uaServerSession;
        }

        /// <summary>
        /// Execute sample code for GDS - Push Trust list Scenario
        /// </summary>
        public void ExecutePushTrustListSample()
        {
            ClientSession gdsSession = null, uaServerSession = null;
            
            try
            {                        
                // create sesssion to GDS
                gdsSession = GetGdsClientSession();
                // creste session to pish server
                uaServerSession = GetPushServerClientSession();

                Console.WriteLine("\n\nGet ApplicationID for '{0}'.", Program.ServerUrl);
                // get application id for OPC UA Server
                NodeId applicationId = GetApplicationId(gdsSession, uaServerSession.CoreSession.Endpoint.Server);

                if (applicationId != null)
                {
                    Console.WriteLine("ApplicationID for '{0}' is {1}.", Program.ServerUrl, applicationId);

                    // Get Trust list from GDS for Application ID
                    Console.WriteLine("\nGet Trust List From GDS for Application ID: {0}.", applicationId);
                    TrustListDataType gdsTrustList = GetTrustListFromGds(gdsSession, applicationId);

                    // specify the trust lists to be updated
                    gdsTrustList.SpecifiedLists = (uint)TrustListMasks.All;
                    // request user imput to wheter overwrite Trust list or merge it with the existing one
                    Console.WriteLine("\nPlease select the Update Trust List Mode:");
                    Console.WriteLine("\t 1- Merge Server Trust List with GDS Trust List");
                    Console.WriteLine("\t 2- Replace Trust List with GDS Trust List");
                    int selectedIndex = Convert.ToInt32(Console.ReadKey().KeyChar.ToString());
                    if (selectedIndex == 1)
                    {
                        // retrieve push server trust list 
                        Console.WriteLine("\nGet Trust List for {0}", Program.ServerUrl);
                        TrustListDataType serverTrustList = ReadTrustList(uaServerSession, Opc.Ua.ObjectIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList);

                        // get GDS issuer certificates as strings
                        List<string> certificatesAsStrings = gdsTrustList.IssuerCertificates.ConvertAll(b => ConvertCertificateBytesToString(b));
                        // merge IssuerCertificates                        
                        foreach (var issuer in serverTrustList.IssuerCertificates)
                        {
                            // check if the exact certificate is already added to collection
                            if (!certificatesAsStrings.Contains(ConvertCertificateBytesToString(issuer)))
                            {
                                gdsTrustList.IssuerCertificates.Add(issuer);
                            }
                        }
                        // get GDS IssuerCrls as strings
                        certificatesAsStrings = gdsTrustList.IssuerCrls.ConvertAll(b => ConvertCertificateBytesToString(b));
                        // merge IssuerCrls
                        foreach (var issuerCrl in serverTrustList.IssuerCrls)
                        {
                            // check if the exact certificate is already added to collection
                            if (!certificatesAsStrings.Contains(ConvertCertificateBytesToString(issuerCrl)))
                            {
                                gdsTrustList.IssuerCrls.Add(issuerCrl);
                            }                            
                        }
                        // get GDS issuer certificates as strings
                        certificatesAsStrings = gdsTrustList.TrustedCertificates.ConvertAll(b => ConvertCertificateBytesToString(b));
                        // merge TrustedCertificates
                        foreach (var trusted in serverTrustList.TrustedCertificates)
                        {
                            // check if the exact certificate is already added to collection
                            if (!certificatesAsStrings.Contains(ConvertCertificateBytesToString(trusted)))
                            {
                                gdsTrustList.TrustedCertificates.Add(trusted);
                            }
                        }
                        // get GDS TrustedCrls as strings
                        certificatesAsStrings = gdsTrustList.TrustedCrls.ConvertAll(b => ConvertCertificateBytesToString(b));                        
                        // merge TrustedCrls
                        foreach (var trustedCrl in serverTrustList.TrustedCrls)
                        {
                            // check if the exact certificate is already added to collection
                            if (!certificatesAsStrings.Contains(ConvertCertificateBytesToString(trustedCrl)))
                            {
                                gdsTrustList.TrustedCrls.Add(trustedCrl);
                            }                            
                        }
                    }
                    Console.WriteLine("\nUpdate TrustList for DefaultapplicationGroup on {0}", Program.ServerUrl);
                    bool needsApplyChanges = UpdateDefaultApplicationGroupTrustList(uaServerSession, gdsTrustList);
                    Console.WriteLine("\nTrustList for DefaultapplicationGroup on {0} was successfully updated.", Program.ServerUrl);

                    if (needsApplyChanges)
                    {
                        Console.WriteLine("\n\nCall ApplyChanges on {0}.", Program.ServerUrl);
                        ApplyChanges(uaServerSession);
                        Console.WriteLine("ApplyChanges was called on {0}.", Program.ServerUrl);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("ExecutePushTrustListSample Error: {0}", ex.Message);
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
        /// Converts bytes array to string
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        private string ConvertCertificateBytesToString(byte[] certificate)
        {
            return Convert.ToBase64String(certificate, Base64FormattingOptions.None);
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
                    Utils.Trace(TraceMasks.Information, String.Format("ApplicationUri: {0} was registered with id: {1}.", applicationDescription.ApplicationUri, applicationId));
                    return applicationId;
                }
            }

            Utils.Trace(TraceMasks.Information, String.Format("Register ApplicationUri: {0}.", applicationDescription.ApplicationUri));
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
                Utils.Trace(TraceMasks.Information, String.Format("ApplicationUri: {0} was registered with id: {1}.", applicationDescription.ApplicationUri, applicationId));
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

        /// <summary>
        /// Calls GetTrustList and then ReadTrustList for an application id
        /// </summary>
        /// <param name="gdsSession"></param>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        private TrustListDataType GetTrustListFromGds(ClientSession gdsSession, NodeId applicationId)
        {           
            List<object> inputArgumentsGetTrustList = new List<object>()
            {
                applicationId,
                null
            };
            IList<object> outputArgumentsGetTrustList = new List<object>();
            // Call Directory_GetTrustList to obtain the node id of the trust list
            var result = gdsSession.Call(
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.ObjectIds.Directory, gdsSession.CoreSession.NamespaceUris),
                ExpandedNodeId.ToNodeId(Opc.Ua.Gds.MethodIds.Directory_GetTrustList, gdsSession.CoreSession.NamespaceUris),
               inputArgumentsGetTrustList,
               out outputArgumentsGetTrustList);

            if (outputArgumentsGetTrustList.Count > 0)
            {
                NodeId trustListId = outputArgumentsGetTrustList[0] as NodeId;
                return ReadTrustList(gdsSession, trustListId);                
            };
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientSession"></param>
        /// <param name="trustListId"></param>
        /// <returns></returns>
        private TrustListDataType ReadTrustList(ClientSession clientSession, NodeId trustListId)
        {
            if (trustListId != null)
            {
                // open and read the trust list with id trustListId
                List<object> inputArgumentsOpenTrustList = new List<object>()
                        {
                            (byte)OpenFileMode.Read
                        };
                IList<object> outputArgumentsOpenTrustList = new List<object>();

                clientSession.Call(trustListId,
                    Opc.Ua.MethodIds.FileType_Open,
                    inputArgumentsOpenTrustList,
                    out outputArgumentsOpenTrustList);

                uint fileHandle = (uint)outputArgumentsOpenTrustList[0];
                MemoryStream ostrm = new MemoryStream();

                try
                {
                    while (true)
                    {
                        int length = 4096;
                        List<object> inputArgumentsReadTrustList = new List<object>()
                                {
                                    fileHandle, length
                                };
                        IList<object> outputArgumentsReadTrustList = new List<object>();
                        clientSession.Call(trustListId,
                            Opc.Ua.MethodIds.FileType_Read,
                            inputArgumentsReadTrustList,
                            out outputArgumentsReadTrustList);

                        byte[] bytes = (byte[])outputArgumentsReadTrustList[0];
                        ostrm.Write(bytes, 0, bytes.Length);

                        if (length != bytes.Length)
                        {
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    // close the trust list 
                    List<object> inputArgumentsCloseTrustList = new List<object>()
                                {
                                    fileHandle
                                };
                    IList<object> outputArgumentsCloseTrustList = new List<object>();
                    clientSession.Call(trustListId,
                            Opc.Ua.MethodIds.FileType_Close,
                            outputArgumentsCloseTrustList,
                            out outputArgumentsCloseTrustList);
                }

                ostrm.Position = 0;

                BinaryDecoder decoder = new BinaryDecoder(ostrm, clientSession.CoreSession.MessageContext);
                TrustListDataType trustList = new TrustListDataType();
                trustList.Decode(decoder);
                decoder.Close();
                ostrm.Close();

                return trustList;
            }
            return null;
        }

        /// <summary>
        /// Update the trust list of DegfaultApplicationGroup on PushServer with what is stored in parameter trustList. Beware that all server certificates are updated!
        /// </summary>
        /// <param name="uaServerSession"></param>
        /// <param name="trustList"></param>
        /// <returns></returns>
        private bool UpdateDefaultApplicationGroupTrustList(ClientSession uaServerSession, TrustListDataType trustList)
        {
            if (uaServerSession == null || uaServerSession.CurrentState != State.Active)
            {
                return false;
            }

            // get bytes from trust list
            MemoryStream strm = new MemoryStream();
            BinaryEncoder encoder = new BinaryEncoder(strm, uaServerSession.CoreSession.MessageContext);
            encoder.WriteEncodeable(null, trustList, null);
            strm.Position = 0;

            List<object> inputArgumentsOpenTrustList = new List<object>()
                {
                    (byte)(OpenFileMode.Write | OpenFileMode.EraseExisting)
                };
            IList<object> outputArgumentsOpenTrustList = new List<object>();
            // open trust list on push server
            uaServerSession.Call(
               Opc.Ua.ObjectIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList,
               Opc.Ua.MethodIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList_Open,
               inputArgumentsOpenTrustList,
               out outputArgumentsOpenTrustList);
            uint fileHandle = (uint)outputArgumentsOpenTrustList[0];

            try
            {
                bool writing = true;
                byte[] buffer = new byte[256];

                while (writing)
                {
                    int bytesWritten = strm.Read(buffer, 0, buffer.Length);

                    if (bytesWritten != buffer.Length)
                    {
                        byte[] copy = new byte[bytesWritten];
                        Array.Copy(buffer, copy, bytesWritten);
                        buffer = copy;
                        writing = false;
                    }

                    List<object> inputArgumentsWriteTrustList = new List<object>()
                        {
                            fileHandle,  buffer
                        };
                    IList<object> outputArgumentsWriteTrustList = new List<object>();
                    // write chuncks of trust list until ready
                    uaServerSession.Call(
                        Opc.Ua.ObjectIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList,
                        Opc.Ua.MethodIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList_Write,
                        inputArgumentsWriteTrustList,
                        out outputArgumentsWriteTrustList);
                }

                List<object> inputArgumentsCloseAndUpdateTrustList = new List<object>()
                        {
                            fileHandle
                        };
                IList<object> outputArgumentsCloseAndUpdateTrustList = new List<object>();
                // Call CloseAndUpdate
               var status = uaServerSession.Call(
                    Opc.Ua.ObjectIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList,
                    Opc.Ua.MethodIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList_CloseAndUpdate,
                    inputArgumentsCloseAndUpdateTrustList, 
                    out outputArgumentsCloseAndUpdateTrustList);
                if (StatusCode.IsGood(status))
                {
                    return (bool)outputArgumentsCloseAndUpdateTrustList[0];
                }
                else
                {
                    throw new ServiceResultException(status.Code, "CloseAndUpdate returned status code:" + status);
                }
            }
            catch (Exception ex)
            {
                // close the trust list 
                List<object> inputArgumentsCloseTrustList = new List<object>()
                                {
                                    fileHandle
                                };
                IList<object> outputArgumentsCloseTrustList = new List<object>();
                uaServerSession.Call(Opc.Ua.ObjectIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList,
                        Opc.Ua.MethodIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList_Close,
                        outputArgumentsCloseTrustList,
                        out outputArgumentsCloseTrustList);

                throw;
            }
        }
    }
}
