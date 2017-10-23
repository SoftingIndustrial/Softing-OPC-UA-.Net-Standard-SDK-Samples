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
using System.Text;

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
        CreateEventMonitorItemWithFilter,
        EndEvents,

        StartHistory, 
        HistoryReadRaw,
        HistoryReadAtTime,
        HistoryReadProcessed,
        EndHistory,

        Exit
    }


}
