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
        Connect,
        Browse,
        Events,
        History,
        MonitoredItem,
        Alarms,
        ReadWrite,
        Exit
    }

    public enum Command
    {
        DiscoverySample,
        ConnectSample,

        StartBrowse,
        BrowseServer,
        BrowseServerWithOptions,
        Translate,
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
        EndMonitoredItem,

        StartAlarms,
        RefreshAlarms,
        AcknowledgeAlarms,
        AddCommentAlarms,
        EndAlarms,

        StartReadWrite,
        Read,
        Write,
        EndReadWrite,

        CallMethods,
        Exit
    }


}
