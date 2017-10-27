/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/
 

namespace SampleClient.StateMachine
{
    public enum State
    {
        Main,
        Discovery,
        Connect,
        Browse,
        Events,
        History,
        MonitoredItem,
        Alarms,
        Terminated
    }

    public enum Command
    {
        StartDiscoveryClient,
        GetEndpoints,
        FindServers,
        EndDiscoveryClient,

        StartConnectClient,
        OpcTcpWithoutSecurity,
        OpcTcpUserIdentity,
        OpcTcpUserIdentityAndSecurity,
        HttpsWithoutUserIdentity,
        HttpsWithUserIdentity,
        SessionWithDiscovery,
        EndConnectClient,

        StartBrowse,
        BrowseServer,
        BrowseServerWithOptions,
        Translate,
        TranslateMultiple,
        EndBrowse,

        StartEvents,
        CreateEventMonitorItem,
        DeleteEventMonitorItem,
        EndEvents,

        StartHistory, 
        HistoryReadRaw,
        HistoryReadAtTime,
        HistoryReadProcessed,
        EndHistory,

        StartMonitoredItem,
        CreateMonitoredItem,
        DeleteMonitoredItem,
        ReadMonitoredItem,
        WriteMonitoredItem,
        EndMonitoredItem,

        StartAlarms,
        RefreshAlarms,
        AcknowledgeAlarms,
        AddCommentAlarms,
        EndAlarms,

        CallMethods,
        Exit
    }


}
