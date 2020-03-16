/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 *  
 * ======================================================================*/
 

namespace SampleClient.StateMachine
{
    /// <summary>
    /// State of the <see cref="Process"/> state machine
    /// </summary>
    public enum State
    {
        Main,
        DiscoveryConnect,
        GDS,
        Browse,
        MonitoredItemEvents,
        MonitoredItem,
        Events,
        History,       
        Alarms,
        ReadWrite,
        FileTransfer,
        PubSub,
        Exit
    }

    /// <summary>
    /// Enumeration for Command that applies to <see cref="Process"/> state machine
    /// </summary>
    public enum Command
    {
        DiscoveryConnect,
        DiscoverySample,
        ConnectSample,
        StartGDSSample,
        EndGDSSample,
        EndDiscoveryConnect,

        StartGDSPullRegSignSample,
        StartGDSPullGetTrustListSample,
        StartGDSPushCertificateSample,
        StartGDSPushTrustListSample,

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

        StartMonitoredItemEvents,
        EndMonitoredItemEvents,

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

        StartFileTransfer,
        UploadFileTransfer,
        DownloadFileTransfer,
        ReadByteArrayFileTransfer,
        DownloadTemporaryFileTransfer,
        UploadTemporaryFileTransfer,
        EndFileTransfer,

        PubSubConfigMenu,
        PubSubReadConfig,

        Exit
    }
}
