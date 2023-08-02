/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
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
        HistoryAsync,
        Alarms,
        AccessRights,
        RolePermissions,
        ReadWriteRegisterNodes,
        CallMethods,
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
        ReverseConnectSampleTimeout,
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
        BrowseServerAsync,
        BrowseServerWithOptions,
        BrowseServerWithOptionsAsync,
        Translate,
        TranslateAsync,
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
        
        HistoryReadRawAsync,
        HistoryReadAtTimeAsync,
        HistoryReadProcessedAsync,

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

        StartTriggerAlarms,
        EnableTriggerAlarms,
        DisableTriggerAlarms,
        EndTriggerAlarms,

        StartAccessRights,
        AccessRestrictions,
        RolePermissions,
        UserRolePermissions,
        EndAccessRights,

        StartReadWriteRegister,
        Read,
        ReadAsync,
        Write,
        WriteAsync,
        RegisterNodes,
        RegisterNodesAsync,
        EndReadWriteRegister,

        StartCallMethods,
        CallMethods,
        CallMethodsAsync,
        EndCallMethods,

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
