using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// The masks used to filter trace messages.
    /// </summary>
    public enum TraceMasks
    {
        /// <summary>
        /// Do not output any messages.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Output error messages.
        /// </summary>
        Error = 0x1,

        /// <summary>
        /// Output informational messages.
        /// </summary>
        Information = 0x2,

        /// <summary>
        /// Output stack traces.
        /// </summary>
         StackTrace = 0x4,

        /// <summary>
        /// Output basic messages for service calls.
        /// </summary>
        Service = 0x8,

        /// <summary>
        /// Output detailed messages for service calls.
        /// </summary>
       ServiceDetail = 0x10,

        /// <summary>
        /// Output basic messages for each operation.
        /// </summary>
        Operation = 0x20,

        /// <summary>
        /// Output detailed messages for each operation.
        /// </summary>
         OperationDetail = 0x40,

        /// <summary>
        /// Output messages related to application initialization or shutdown
        /// </summary>
         StartStop = 0x80,

        /// <summary>
        /// Output messages related to a call to an external system.
        /// </summary>
         ExternalSystem = 0x100,

        /// <summary>
        /// Output messages related to security
        /// </summary>
        Security = 0x200,

        /// <summary>
        /// Output all messages.
        /// </summary>
         All = 0x7FFFFFFF
    }

    /// <summary>
    /// Class that holds the constants representing the tracing masks
    /// </summary>
    [System.Flags]
    public enum TraceSources : uint
    {
        /// <summary>
        /// Output traces from the configuration library
        /// </summary>
        ConfigurationSDK = 0x00000001, // BIT 0
        /// <summary>
        /// Output traces from the client library.
        /// </summary>
        ClientSDK = 0x00000002, // BIT 1
        /// <summary>
        /// Output traces from the server library.
        /// </summary>
        ServerSDK = 0x00000004, // BIT 2
        /// <summary>
        /// Output traces from the core library.
        /// </summary>
        Core = 0x00000008, // BIT 3
        /// <summary>
        /// Output traces from the exception's stack traces
        /// </summary>
        StackTrace = 0x00000010, // BIT 4
        /// <summary>
        /// Output traces from the old (obsolete) tracing mechanism
        /// </summary>
        OldTrace = 0x00000020, // BIT 5
        /// <summary>
        /// Output traces for SimpleAPI
        /// </summary>
        ClientAPI = 0x00010000,  // BIT 16
        /// <summary>
        /// Output traces for DemoClient
        /// </summary>
        ServerAPI = 0x00020000,  // BIT 17
        /// <summary>
        /// Output traces for user3 application; extension of the Toolkit trace masks
        /// </summary>
        User3 = 0x00040000,  // BIT 18
        /// <summary>
        /// Output traces for user4 application; extension of the Toolkit trace masks
        /// </summary>
        User4 = 0x00080000,  // BIT 19
        /// <summary>
        /// Output traces for user5 application; extension of the Toolkit trace masks
        /// </summary>
        User5 = 0x00100000,  // BIT 20
        /// <summary>
        /// Output traces for user6 application; extension of the Toolkit trace masks
        /// </summary>
        User6 = 0x00200000,  // BIT 21
        /// <summary>
        /// Output traces for user7 application; extension of the Toolkit trace masks
        /// </summary>
        User7 = 0x00400000,  // BIT 22
        /// <summary>
        /// Output traces for user8 application; extension of the Toolkit trace masks
        /// </summary>
        User8 = 0x00800000  // BIT 23
    }

    /// <summary>
    /// The transport protocols.
    /// </summary>
    public enum TransportProtocols
    {
        /// <summary>
        /// Opc tcp native transport protocol.
        /// </summary>
        OpcTcp,
        /// <summary>
        /// Http transport protocol.
        /// </summary>
        Http,
        /// <summary>
        /// Https transport protocol.
        /// </summary>
        Https
    }

    /// <summary>
    /// License feature selection
    /// </summary>
    public enum LicenseFeature
    {
        /// <summary>
        /// Client Toolkit Functionality
        /// </summary>
        Client = 0,

        /// <summary>
        /// Server Toolkit Functionality
        /// </summary>
        Server = 1
    }

    /// <summary>
    /// Certificate validation options.
    /// Depending on the option selected the certificate will be kept in a permanent store , a rejected store or accepted temporarily.
    /// </summary>
    public enum CertificateValidationOption
    {
        /// <summary>
        /// Reject the certificate
        /// </summary>
        Reject,
        /// <summary>
        /// Accept the certificate temporarily
        /// </summary>
        AcceptOnce,
        /// <summary>
        /// Accept the certificate permanently
        /// </summary>
        AcceptAlways
    }

    /// <summary>
    /// Specifies the possible security mechanisms.
    /// </summary>
    public enum SecurityPolicy
    {
        /// <summary>
        /// None security is applied.
        /// </summary>
        None,
        /// <summary>
        /// Basic256 algorithm is used for signing and encryption.
        /// </summary>
        Basic256,
        /// <summary>
        /// Basic128Rsa15 algorithm is used for signing and encryption.
        /// </summary>
        Basic128Rsa15,
        /// <summary>
        /// Basic256Sha256 algorithm is used for signing and encryption.
        /// </summary>
        Basic256Sha256
    }

}
