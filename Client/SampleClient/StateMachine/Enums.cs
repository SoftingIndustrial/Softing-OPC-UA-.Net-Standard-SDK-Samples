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
        DiscoveryConnectGds,
        Discovery,
        GDS,
        Browse,
        MonitoredEventsAlarms,
        MonitoredItem,
        Events,
        History,       
        Alarms,
        AccessRights,
        RolePermissions,
        ReadWriteRegisterNodes,
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
        DiscoveryAsyncSample,
        DiscoverServersSample,
        DiscoverServersOnNetworkSample,
        DiscoverServersSampleAsync,
        DiscoverServersOnNetworkSampleAsync,
        ConnectSample,
        ReverseConnectSample,
        ReverseConnectSampleAsync,
        StartDiscoverySample,
        EndDiscoverySample,
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

        StartMonitoredEventsAlarms,
        EndMonitoredEventsAlarms,

        StartMonitoredItem, 
        CreateMonitoredItem,
        DeleteMonitoredItem,
        EndMonitoredItem,

        StartAlarms,
        RefreshAlarms,
        AcknowledgeAlarms,
        AddCommentAlarms,
        EndAlarms,

        StartAccessRights,
        AccessRestrictions,
        RolePermissions,
        UserRolePermissions,
        EndAccessRights,

        StartReadWriteRegister,
        Read,
        Write,
        RegisterNodes,
        EndReadWriteRegister,

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
