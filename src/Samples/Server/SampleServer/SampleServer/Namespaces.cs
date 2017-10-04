/* ========================================================================
 * Copyright © 2011-2017 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * The Software is based on the OPC Foundation, Inc.’s software. This 
 * original OPC Foundation’s software can be found here:
 * http://www.opcfoundation.org
 * 
 * The original OPC Foundation’s software is subject to the OPC Foundation
 * MIT License 1.00, which can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * 
 * ======================================================================*/

namespace SampleServer
{
    /// <summary>
    /// The namespaces for the nodes provided by the server
    /// </summary>
    public static class Namespaces
    {
        public const string OpcUa = "http://opcfoundation.org/UA/";

        public const string OpcUaXsd = "http://opcfoundation.org/UA/2008/02/Types.xsd";

        public const string Alarms = "http://softing.com/Softing.Opc.Ua.Toolkit.Samples/AlarmsServer";
        
        public const string DataAccess = "http://softing.com/Softing.Opc.Ua.Toolkit.Samples/DataAccessServer";

        public const string FileSystem = "http://softing.com/Softing.Opc.Ua.Toolkit.Samples/FileSystemServer";

        public const string HistoricalDataAccess = "http://opcfoundation.org/Quickstarts/HistoricalDataAccess";

        public const string Methods = "http://softing.com/Softing.Opc.Ua.Toolkit.Samples/MethodsServer";
        
        public const string NodeManagement = "http://softing.com/Softing.Opc.Ua.Toolkit.Samples/NodeManagementServer";

        public const string Refrigerators = "http://industrial.softing.com/UA/Refrigerators";

        public const string UserAuthentication = "http://softing.com/Softing.Opc.Ua.Toolkit.Samples/UserAuthentication";
    }
}