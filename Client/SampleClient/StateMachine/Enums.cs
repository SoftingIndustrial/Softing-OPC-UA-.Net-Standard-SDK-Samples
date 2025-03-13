/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
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
        Connects,
        GDS,
        Browse,
        MonitoredEventsAlarms,
        MonitoredItems,
        MonitoredItem,
        MonitoredItemWithoutConnect,
        MonitoredItemAddNewAfterConnect,
        MonitoredTransferEventsAlarms,
        MonitoredEvents,
        TransferSubscriptions,
        TransferSubscriptionsConnectionType,
        Events,
        EventsWithoutConnect,
        EventsAddNewAfterConnect,
        EventsDoubleFilteringWithConnect,
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
        ConnectSampleRSA,
        ConnectSampleECCNistP256,
        ConnectSampleECCNistP384,
        ConnectSampleECCBrainpoolP256r1,
        ConnectSampleECCBrainpoolP384r1,
        EndConnectSample,
        ConnectAndReconnectSample,
        ReverseConnectSample,
        ReverseConnectAndReconnectSample,
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

        StartMonitoredEvents,
        EndMonitoredEvents,

        StartEvents,
        CreateEventMonitorItem,
        DeleteEventMonitorItem,
        EndEvents,

        StartEventsWithoutConnect,
        CreateEventMonitorItemWithoutConnect,
        DeleteEventMonitorItemWithoutConnect,
        EndEventsWithoutConnect,

        StartEventsAddNewAfterConnect,
        CreateEventMonitorItemAddNewAfterConnect,
        DeleteEventMonitorItemAddNewAfterConnect,
        EndEventsAddNewAfterConnect,

        StartDoubleFilteringEvents,
        CreateDoubleFilteringEventMonitorItem,
        DeleteDoubleFilteringEventMonitorItem,
        EndDoubleFilteringEvents,

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

        StartMonitoredTransferEventsAlarms,
        EndMonitoredTransferEventsAlarms,

        StartMonitoredItems,
        EndMonitoredItems,

        StartTransferSubscriptions,
        EndTransferSubscriptions,

        StartTransferSubscriptionsConnectionType,
        EndTransferSubscriptionsConnectionType,

        StartMonitoredItem,
        CreateMonitoredItem,
        DeleteMonitoredItem,
        EndMonitoredItem,
        TransferSubscriptions,
        TransferSubscriptionsAsync,
        TransferSubscriptionsSessionClosed,
        SaveSubscriptions,
        LoadSubscriptions,
        TransferSubscriptionsDetached,
        TransferSubscriptionsUserIdentity,
        TransferSubscriptionsCertificate,
        TransferSubscriptionsCertificatePassword,
        TransferSubscriptionsSecurity,

        StartMonitoredItemsWithoutConnect,
        CreateMonitoredItemsWithoutConnect,
        DeleteMonitoredItemsWithoutConnect,
        EndMonitoredItemsWithoutConnect,

        StartMonitoredItemsAddNewAfterConnect,
        CreateMonitoredItemsAddNewAfterConnect,
        DeleteMonitoredItemsAddNewAfterConnect,
        EndMonitoredItemsAddNewAfterConnect,

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
