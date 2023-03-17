/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SampleClient.Samples;
using Softing.Opc.Ua.Client;

namespace SampleClient.StateMachine
{
    /// <summary>
    /// Process is the class that maintains sample application's state machine and allows use to execute code from provided samples
    /// It instantiates sample client classes and calls their methods
    /// </summary>
    public class Process
    {
        #region Private Fields

        private readonly Dictionary<StateTransition, State> m_transitions;
        private readonly Dictionary<State, IList<CommandDescriptor>> m_processStateCommands;
        private readonly UaApplication m_application;
        private BrowseClient m_browseClient;
        private EventsClient m_eventsClient;
        private HistoryClient m_historyClient;
        private ReadWriteClient m_readWriteClient;
        private MonitoredItemClient m_monitoredItemClient;
        private AlarmsClient m_alarmsClient;
        private AccessRightsClient m_accessRightsClient;
        private FileTransferClient m_fileTransferClient;
        private GdsClient m_gdsClient;
        private PubSubClient m_pubSubClient;

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of Process
        /// </summary>
        public Process(UaApplication application)
        {
            m_application = application;
            CurrentState = State.Main;

            m_transitions = new Dictionary<StateTransition, State>();

            //add discovery & connect menu item - 1
            InitializeDiscoveryConnectTransitions();
            //add browse menu item - 2
            InitializeBrowseTransitions();
            //read-write - 3
            InitializeReadWriteTransitions();
            //add monitored item / events menu - 4
            InitializeMonitoredItemEventsTransitions();
            //add alarms menu - 5
            InitializeAccessRightsTransitions();
            //add call methods menu - 6
            StateTransition callMethods = new StateTransition(State.Main, Command.CallMethods, "6", "Call Methods on Server");
            callMethods.ExecuteCommand += CallMethods_ExecuteCommand;
            m_transitions.Add(callMethods, State.Main);
            //add history menu - 7
            InitializeHistoryTransitions();
            // add file transfer menu - 8
            InitializeFileTransferTransitions();
            // add PubSub menu - 9
            InitializePubSubTransitions();

            //add all exit commands
            StateTransition exit = new StateTransition(State.Main, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);
            exit = new StateTransition(State.Browse, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);
            exit = new StateTransition(State.DiscoveryConnectGds, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);           
            exit = new StateTransition(State.History, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);
            exit = new StateTransition(State.MonitoredEventsAlarms, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);
            exit = new StateTransition(State.Alarms, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);
            exit = new StateTransition(State.ReadWriteRegisterNodes, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);
            exit = new StateTransition(State.FileTransfer, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);


            m_processStateCommands = new Dictionary<State, IList<CommandDescriptor>>();
            foreach (var stateStansition in m_transitions.Keys)
            {
                if (!m_processStateCommands.ContainsKey(stateStansition.CurrentState))
                {
                    m_processStateCommands.Add(stateStansition.CurrentState, new List<CommandDescriptor>());
                }
                m_processStateCommands[stateStansition.CurrentState].Add(stateStansition.CommandDescriptor);
            }

            DisplayListOfCommands();
        }

      
        #endregion

        #region Properties

        /// <summary>
        /// Get current state of process
        /// </summary>
        public State CurrentState { get; private set; }
      

        #endregion

        #region Public Method - ExecuteCommand

        /// <summary>
        /// Execute provided command keyword and move to next state
        /// </summary>
        /// <param name="commandKeyword"></param>
        /// <returns>true if command was executed</returns>
        public async Task ExecuteCommand(string commandKeyword)
        {
            IList<CommandDescriptor> possibleCommands = GetPossibleCommands();
            foreach (var commandDescriptor in possibleCommands)
            {
                if (commandDescriptor.Keyword == commandKeyword)
                {
                    Command commandToExecute = commandDescriptor.Command;
                    StateTransition stateTransitionToExecute = GetStateTransitionForCommand(commandToExecute);
                    if (stateTransitionToExecute != null)
                    {
                        Console.WriteLine("\r\nExecuting command '{0}'...", commandDescriptor.Description);
                        //change current state before execution to have the right current state at execution time
                        CurrentState = m_transitions[stateTransitionToExecute];
                        await stateTransitionToExecute.OnExecuteCommand().ConfigureAwait(false);

                        Console.WriteLine("'{0}' execution ended.", commandDescriptor.Description);
                        DisplayListOfCommands();
                        return;
                    }
                    return;
                }
            }
            Console.WriteLine("\r\nCannot find command '{0}'. Please choose from the list below:\r\n", commandKeyword);
            DisplayListOfCommands();
            return;
        }

        #endregion

        #region Initialize Transitions Methods
        /// <summary>
        /// Initializes all sub menu transitions for BrowseClient (1)
        /// </summary>
        private void  InitializeDiscoveryConnectTransitions()
        {
            //commAands for browse
            StateTransition startDCClient = new StateTransition(State.Main, Command.DiscoveryConnect, "1", "Enter Connect/Reverse Connect/Discovery/GDS Menu");            
            m_transitions.Add(startDCClient, State.DiscoveryConnectGds);

            //add connect menu item
            StateTransition connectSample = new StateTransition(State.DiscoveryConnectGds, Command.ConnectSample, "1", "Execute Connect Sample");
            connectSample.ExecuteCommand += ConnectSample_ExecuteCommand;
            m_transitions.Add(connectSample, State.DiscoveryConnectGds);

            //add reverse connect menu item
            StateTransition reverseConnectSample = new StateTransition(State.DiscoveryConnectGds, Command.ReverseConnectSample, "2", "Execute Reverse Connect Sample");
            reverseConnectSample.ExecuteCommand += ReverseConnectSample_ExecuteCommand;
            m_transitions.Add(reverseConnectSample, State.DiscoveryConnectGds);

            //add async reverse connect menu item
            StateTransition reverseConnectSampleAsync = new StateTransition(State.DiscoveryConnectGds, Command.ReverseConnectSampleAsync, "3", "Execute Reverse Connect Async Sample");
            reverseConnectSampleAsync.ExecuteCommand += ReverseConnectSampleAsync_ExecuteCommand;
            m_transitions.Add(reverseConnectSampleAsync, State.DiscoveryConnectGds);

            //add discovery menu item
            StateTransition discoveryMenu = new StateTransition(State.DiscoveryConnectGds, Command.StartDiscoverySample, "4", "Enter Discovery Menu");           
            m_transitions.Add(discoveryMenu, State.Discovery);

            //add discovery menu item
            StateTransition discoverServersSample = new StateTransition(State.Discovery, Command.DiscoverServersSample, "1", "Execute DiscoverServers Sample");
            discoverServersSample.ExecuteCommand += DiscoverServersSample_ExecuteCommand;
            m_transitions.Add(discoverServersSample, State.Discovery);

            StateTransition discoverServersOnNetworkSample = new StateTransition(State.Discovery, Command.DiscoverServersOnNetworkSample, "2", "Execute DiscoverServersOnNetwork Sample");
            discoverServersOnNetworkSample.ExecuteCommand += DiscoverServersOnNetworkSample_ExecuteCommand;
            m_transitions.Add(discoverServersOnNetworkSample, State.Discovery);          

            //add discovery menu item
            StateTransition discoverServersSampleAsync = new StateTransition(State.Discovery, Command.DiscoverServersSampleAsync, "3", "Execute DiscoverServers Async Sample");
            discoverServersSampleAsync.ExecuteCommand += DiscoverServersSampleAsync_ExecuteCommand;
            m_transitions.Add(discoverServersSampleAsync, State.Discovery);

            //add discovery menu item
            StateTransition discoverServersOnNetworkSampleAsync = new StateTransition(State.Discovery, Command.DiscoverServersOnNetworkSampleAsync, "4", "Execute DiscoverServersOnNetwork Async Sample");
            discoverServersOnNetworkSampleAsync.ExecuteCommand += DiscoverServersOnNetworkSampleAsync_ExecuteCommand;
            m_transitions.Add(discoverServersOnNetworkSampleAsync, State.Discovery);

            StateTransition endDiscoveryMenu = new StateTransition(State.Discovery, Command.EndDiscoverySample, "0", "Back to Discovery/Connect Menu");
            m_transitions.Add(endDiscoveryMenu, State.DiscoveryConnectGds);

            //add GDS menu item
            StateTransition gdsSample = new StateTransition(State.DiscoveryConnectGds, Command.StartGDSSample, "5", "Enter GDS Sample Menu");            
            m_transitions.Add(gdsSample, State.GDS);

            //commands for GDS Pull Get Trust List
            StateTransition startGDSGetTrustListSample = new StateTransition(State.GDS, Command.StartGDSPullGetTrustListSample, "1", "Execute GDS Pull Get Trust List Sample");
            startGDSGetTrustListSample.ExecuteCommand += GdsPullTrustListSample_ExecuteCommand;
            m_transitions.Add(startGDSGetTrustListSample, State.GDS);

            //commands for GDS Pull Register And Sign Certificate
            StateTransition startGDSPullRegSignSample = new StateTransition(State.GDS, Command.StartGDSPullRegSignSample, "2", "Execute GDS Pull Register And Sign Certificate Sample");
            startGDSPullRegSignSample.ExecuteCommand += GdsPullRegisterAndSignCertificateSample_ExecuteCommand;
            m_transitions.Add(startGDSPullRegSignSample, State.GDS);

            //commands for GDS Push Application Certificate
            StateTransition startGDSPushTrustListSample = new StateTransition(State.GDS, Command.StartGDSPushTrustListSample, "3", "Execute GDS Push Trust List Sample");
            startGDSPushTrustListSample.ExecuteCommand += GDSPushTrustListSample_ExecuteCommand;
            m_transitions.Add(startGDSPushTrustListSample, State.GDS);

            //commands for GDS Push Application Certificate
            StateTransition startGDSPushCertificateSample = new StateTransition(State.GDS, Command.StartGDSPushCertificateSample, "4", "Execute GDS Push Application Certificate Sample");
            startGDSPushCertificateSample.ExecuteCommand += GDSPushCertificateSample_ExecuteCommand;
            m_transitions.Add(startGDSPushCertificateSample, State.GDS);

            StateTransition endGDSSample = new StateTransition(State.GDS, Command.EndGDSSample, "0", "Back to Discovery/Connect Menu");           
            m_transitions.Add(endGDSSample, State.DiscoveryConnectGds);

            StateTransition endDiscoveryConnect = new StateTransition(State.DiscoveryConnectGds, Command.EndDiscoveryConnect, "0", "Back to Main Menu");            
            m_transitions.Add(endDiscoveryConnect, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for BrowseClient (2)
        /// </summary>
        private void InitializeBrowseTransitions()
        {
            //commAands for browse
            StateTransition startBrowseClient = new StateTransition(State.Main, Command.StartBrowse, "2", "Enter Browse Menu");
            startBrowseClient.ExecuteCommand += StartBrowseClient_ExecuteCommand;
            m_transitions.Add(startBrowseClient, State.Browse);
            StateTransition browseServer = new StateTransition(State.Browse, Command.BrowseServer, "1", "Browse server");
            browseServer.ExecuteCommand += BrowseServer_ExecuteCommand;
            m_transitions.Add(browseServer, State.Browse);
            StateTransition browseServerWithOptions = new StateTransition(State.Browse, Command.BrowseServerWithOptions, "2", "Browse server with options");
            browseServerWithOptions.ExecuteCommand += BrowseServerWithOptions_ExecuteCommand;
            m_transitions.Add(browseServerWithOptions, State.Browse);
            StateTransition translate = new StateTransition(State.Browse, Command.Translate, "3", "Translate BrowsePaths to NodeIds");
            translate.ExecuteCommand += Translate_ExecuteCommand;
            m_transitions.Add(translate, State.Browse);
            StateTransition endBrowseClient = new StateTransition(State.Browse, Command.EndBrowse, "0", "Back to Main Menu");
            endBrowseClient.ExecuteCommand += EndBrowseClient_ExecuteCommand;
            m_transitions.Add(endBrowseClient, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for ReadWriteClient (3)
        /// </summary>
        private void InitializeReadWriteTransitions()
        {
            //commAands for readWrite
            StateTransition startReadWrite = new StateTransition(State.Main, Command.StartReadWriteRegister, "3", "Enter Read/Write/RegisterNodes Menu");
            startReadWrite.ExecuteCommand += StartReadWrite_ExecuteCommand;
            m_transitions.Add(startReadWrite, State.ReadWriteRegisterNodes);
            StateTransition read = new StateTransition(State.ReadWriteRegisterNodes, Command.Read, "1", "Read Nodes");
            read.ExecuteCommand += Read_ExecuteCommand;
            m_transitions.Add(read, State.ReadWriteRegisterNodes);
            StateTransition write = new StateTransition(State.ReadWriteRegisterNodes, Command.Write, "2", "Write Nodes");
            write.ExecuteCommand += Write_ExecuteCommand;
            m_transitions.Add(write, State.ReadWriteRegisterNodes);
            StateTransition registerNodes = new StateTransition(State.ReadWriteRegisterNodes, Command.RegisterNodes, "3", "Register Nodes");
            registerNodes.ExecuteCommand += RegisterNodes_ExecuteCommand;
            m_transitions.Add(registerNodes, State.ReadWriteRegisterNodes);

            StateTransition endReadWrite = new StateTransition(State.ReadWriteRegisterNodes, Command.EndReadWriteRegister, "0", "Back to Main Menu");
            endReadWrite.ExecuteCommand += EndReadWrite_ExecuteCommand;
            m_transitions.Add(endReadWrite, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for MonitoredItem/ Events (4)
        /// </summary>
        private void InitializeMonitoredItemEventsTransitions()
        {
            //commands for monitored item
            StateTransition start = new StateTransition(State.Main, Command.StartMonitoredEventsAlarms, "4", "Enter MonitoredItem/Events Menu/Alarms Menu");
            m_transitions.Add(start, State.MonitoredEventsAlarms);

            //commands for monitored item
            StateTransition startMonitoredItem = new StateTransition(State.MonitoredEventsAlarms, Command.StartMonitoredItem, "1", "Enter MonitoredItem Menu");
            startMonitoredItem.ExecuteCommand += StartMonitoredItem_ExecuteCommand;
            m_transitions.Add(startMonitoredItem, State.MonitoredItem);
            StateTransition createMonitoredItem = new StateTransition(State.MonitoredItem, Command.CreateMonitoredItem, "1", "Create data change Monitored Items");
            createMonitoredItem.ExecuteCommand += CreateMonitoredItem_ExecuteCommand;
            m_transitions.Add(createMonitoredItem, State.MonitoredItem);
            StateTransition deleteMonitoredItem = new StateTransition(State.MonitoredItem, Command.DeleteMonitoredItem, "2", "Delete data change Monitored Items");
            deleteMonitoredItem.ExecuteCommand += DeleteMonitoredItem_ExecuteCommand;
            m_transitions.Add(deleteMonitoredItem, State.MonitoredItem);
            StateTransition endMonitoredItem = new StateTransition(State.MonitoredItem, Command.EndMonitoredItem, "0", "Back to MonitoredItem/Events Menu/Alarms Menu");
            endMonitoredItem.ExecuteCommand += EndMonitoredItem_ExecuteCommand;
            m_transitions.Add(endMonitoredItem, State.MonitoredEventsAlarms);

            //commands for events
            StateTransition startEventsClient = new StateTransition(State.MonitoredEventsAlarms, Command.StartEvents, "2", "Enter Events Menu");
            startEventsClient.ExecuteCommand += StartEventsClient_ExecuteCommand;
            m_transitions.Add(startEventsClient, State.Events);
            StateTransition createEventMonitorItem = new StateTransition(State.Events, Command.CreateEventMonitorItem, "1", "Create event Monitored Item");
            createEventMonitorItem.ExecuteCommand += CreateEventMonitorItem_ExecuteCommand;
            m_transitions.Add(createEventMonitorItem, State.Events);
            StateTransition deleteEventMonitorItem = new StateTransition(State.Events, Command.DeleteEventMonitorItem, "2", "Delete event Monitored Item");
            deleteEventMonitorItem.ExecuteCommand += DeleteEventMonitorItem_ExecuteCommand;
            m_transitions.Add(deleteEventMonitorItem, State.Events);
            StateTransition endEvents = new StateTransition(State.Events, Command.EndEvents, "0", "Back to MonitoredItem/Events Menu/Alarms Menu");
            endEvents.ExecuteCommand += EndEvents_ExecuteCommand;
            m_transitions.Add(endEvents, State.MonitoredEventsAlarms);

            //commands for alarms
            StateTransition startAlarms = new StateTransition(State.MonitoredEventsAlarms, Command.StartAlarms, "3", "Enter Alarms Menu");
            startAlarms.ExecuteCommand += StartAlarms_ExecuteCommand;
            m_transitions.Add(startAlarms, State.Alarms);
            StateTransition refreshAlarms = new StateTransition(State.Alarms, Command.RefreshAlarms, "1", "Refresh active alarms");
            refreshAlarms.ExecuteCommand += RefreshAlarms_ExecuteCommand;
            m_transitions.Add(refreshAlarms, State.Alarms);
            StateTransition acknowledgeAlarms = new StateTransition(State.Alarms, Command.AcknowledgeAlarms, "2", "Acknowledge alarm");
            acknowledgeAlarms.ExecuteCommand += AcknowledgeAlarms_ExecuteCommand;
            m_transitions.Add(acknowledgeAlarms, State.Alarms);
            StateTransition addCommentAllarms = new StateTransition(State.Alarms, Command.AddCommentAlarms, "3", "Add comment to alarm");
            addCommentAllarms.ExecuteCommand += AddCommentAlarms_ExecuteCommand;
            m_transitions.Add(addCommentAllarms, State.Alarms);

            //commands for start/stop triggering alarms
            StateTransition enableTriggerAlarms = new StateTransition(State.Alarms, Command.EnableTriggerAlarms, "4", "Enable Trigger Alarms (refresh timeout = 15 seconds)");
            enableTriggerAlarms.ExecuteCommand += EnableTriggerAlarms_ExecuteCommand;
            m_transitions.Add(enableTriggerAlarms, State.Alarms);
            StateTransition disableTriggerAlarms = new StateTransition(State.Alarms, Command.DisableTriggerAlarms, "5", "Disable Trigger Alarms");
            disableTriggerAlarms.ExecuteCommand += DisableTriggerAlarms_ExecuteCommand;
            m_transitions.Add(disableTriggerAlarms, State.Alarms);

            StateTransition endAlarms = new StateTransition(State.Alarms, Command.EndAlarms, "0", "Back to MonitoredItem/Events Menu/Alarms Menu");
            endAlarms.ExecuteCommand += EndAlarms_ExecuteCommand;
            m_transitions.Add(endAlarms, State.MonitoredEventsAlarms);

            StateTransition end = new StateTransition(State.MonitoredEventsAlarms, Command.EndMonitoredEventsAlarms, "0", "Back to Main Menu");
            m_transitions.Add(end, State.Main);
        }
        

        /// <summary>
        /// Initializes all sub menu transitions for Alarms (5)
        /// </summary>
        private void InitializeAccessRightsTransitions()
        {
            //commands for access rights
            StateTransition startAccessRights = new StateTransition(State.Main, Command.StartAccessRights, "5", "Enter Access Rights Menu");
            startAccessRights.ExecuteCommand += StartAccessRights_ExecuteCommand;
            m_transitions.Add(startAccessRights, State.AccessRights);
            StateTransition accessRestrictions = new StateTransition(State.AccessRights, Command.AccessRestrictions, "1", "Sample AccessRestrictions");
            accessRestrictions.ExecuteCommand += AccessRestrictions_ExecuteCommand;
            m_transitions.Add(accessRestrictions, State.AccessRights);
            StateTransition rolePermissions = new StateTransition(State.AccessRights, Command.RolePermissions, "2", "Sample RolePermissions");
            rolePermissions.ExecuteCommand += RolePermissions_ExecuteCommand;
            m_transitions.Add(rolePermissions, State.AccessRights);
            StateTransition usrRolePermissions = new StateTransition(State.AccessRights, Command.UserRolePermissions, "3", "Sample UserRolePermissions");
            usrRolePermissions.ExecuteCommand += UserRolePermissions_ExecuteCommand;
            m_transitions.Add(usrRolePermissions, State.AccessRights);
            StateTransition endAccessRights = new StateTransition(State.AccessRights, Command.EndAccessRights, "0", "Back to Main Menu");
            m_transitions.Add(endAccessRights, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for History (7)
        /// </summary>
        private void InitializeHistoryTransitions()
        {
            //commands for history
            StateTransition startHistory = new StateTransition(State.Main, Command.StartHistory, "7", "Enter Read History Menu");
            startHistory.ExecuteCommand += StartHistory_ExecuteCommand;
            m_transitions.Add(startHistory, State.History);
            StateTransition historyReadRaw = new StateTransition(State.History, Command.HistoryReadRaw, "1", "History read raw");
            historyReadRaw.ExecuteCommand += HistoryReadRaw_ExecuteCommand;
            m_transitions.Add(historyReadRaw, State.History);
            StateTransition historyReadAtTime = new StateTransition(State.History, Command.HistoryReadAtTime, "2", "History read at time");
            historyReadAtTime.ExecuteCommand += HistoryReadAtTime_ExecuteCommand;
            m_transitions.Add(historyReadAtTime, State.History);
            StateTransition historyReadProcessed = new StateTransition(State.History, Command.HistoryReadProcessed, "3", "History read processed");
            historyReadProcessed.ExecuteCommand += HistoryReadProcessed_ExecuteCommand;
            m_transitions.Add(historyReadProcessed, State.History);
            StateTransition endHistory = new StateTransition(State.History, Command.EndHistory, "0", "Back to Main Menu");
            endHistory.ExecuteCommand += EndHistory_ExecuteCommand;
            m_transitions.Add(endHistory, State.Main);
        }


        /// <summary>
        /// Initializes all sub menu transitions for file transfer (8)
        /// </summary>
        private void InitializeFileTransferTransitions()
        {
            //commands for file transfer
            StateTransition startFileTransfer = new StateTransition(State.Main, Command.StartFileTransfer, "8", "Enter File Transfer Menu");
            startFileTransfer.ExecuteCommand += StartFileTransfer_ExecuteCommand;
            m_transitions.Add(startFileTransfer, State.FileTransfer);

            StateTransition uploadFileTransfer = new StateTransition(State.FileTransfer, Command.UploadFileTransfer, "1", "Upload file");
            uploadFileTransfer.ExecuteCommand += UploadFileTransfer_ExecuteCommand;
            m_transitions.Add(uploadFileTransfer, State.FileTransfer);

            StateTransition downloadFileTransfer = new StateTransition(State.FileTransfer, Command.DownloadFileTransfer, "2", "Download file");
            downloadFileTransfer.ExecuteCommand += DownloadFileTransfer_ExecuteCommand;
            m_transitions.Add(downloadFileTransfer, State.FileTransfer);

            StateTransition readByteArrayFileTransfer = new StateTransition(State.FileTransfer, Command.ReadByteArrayFileTransfer, "3", "Read Byte Array");
            readByteArrayFileTransfer.ExecuteCommand += ReadByteArrayFileTransfer_ExecuteCommand;
            m_transitions.Add(readByteArrayFileTransfer, State.FileTransfer);
            
            StateTransition uploadTemporaryFileTransfer = new StateTransition(State.FileTransfer, Command.UploadTemporaryFileTransfer, "4", "Upload Temporary File");
            uploadTemporaryFileTransfer.ExecuteCommand += WriteTemporaryFileTransfer_ExecuteCommand;
            m_transitions.Add(uploadTemporaryFileTransfer, State.FileTransfer);

            StateTransition downloadTemporaryFileTransfer = new StateTransition(State.FileTransfer, Command.DownloadTemporaryFileTransfer, "5", "Download Temporary File");
            downloadTemporaryFileTransfer.ExecuteCommand += ReadTemporaryFileTransfer_ExecuteCommand;
            m_transitions.Add(downloadTemporaryFileTransfer, State.FileTransfer);

            StateTransition endFileTransfer = new StateTransition(State.FileTransfer, Command.EndFileTransfer, "0", "Back to Main Menu");
            endFileTransfer.ExecuteCommand += EndFileTransfer_ExecuteCommand;
            m_transitions.Add(endFileTransfer, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for PubSub (9)
        /// </summary>
        private void InitializePubSubTransitions()
        {
            //commands for reading PubSubStateMachine
            StateTransition startPubSubMenu = new StateTransition(State.Main, Command.PubSubConfigMenu, "9", "Enter PubSub Menu");
            startPubSubMenu.ExecuteCommand += StartPubSubCfgMenu_ExecuteCommand;
            m_transitions.Add(startPubSubMenu, State.PubSub);//commands for PubSub
            StateTransition startPubSubReadCfg = new StateTransition(State.PubSub, Command.PubSubReadConfig, "1", "Read PubSubConfiguration");
            startPubSubReadCfg.ExecuteCommand += StartPubSubReadCfg_ExecuteCommand;
            m_transitions.Add(startPubSubReadCfg, State.PubSub);

            StateTransition exitPubSubSession = new StateTransition(State.PubSub, Command.EndFileTransfer, "0", "Back to Main Menu");
            exitPubSubSession.ExecuteCommand += ExitPubSubCfgMenu_ExecuteCommand;
            m_transitions.Add(exitPubSubSession, State.Main);
        }

        #endregion

        #region ExecuteCommand Handler for Alarms

        private async Task EndAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                // disable alarms
                m_alarmsClient.CallTriggerAlarms(TriggerAlarmsOption.Disable);
                
                // disconnect
                await m_alarmsClient.Disconnect().ConfigureAwait(false);
                m_alarmsClient = null;
            }
        }

        private Task AddCommentAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.AddCommentToAlarm();
            }
            return Task.CompletedTask;
        }

        private async Task StartTriggerAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient == null)
            {
                m_alarmsClient = new AlarmsClient(m_application);
                await m_alarmsClient.Initialize().ConfigureAwait(false);
            }
        }

        private Task EnableTriggerAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.CallTriggerAlarms(TriggerAlarmsOption.Enable);
                Console.WriteLine("Please wait ...");
            }
            return Task.CompletedTask;
        }

        private Task DisableTriggerAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.CallTriggerAlarms(TriggerAlarmsOption.Disable);
            }
            return Task.CompletedTask;
        }

        private Task AcknowledgeAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.AcknowledgeAlarm();
            }
            return Task.CompletedTask;
        }

        private Task RefreshAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.ConditionRefresh();
            }
            return Task.CompletedTask;
        }

        private async Task StartAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient == null)
            {
                m_alarmsClient = new AlarmsClient(m_application);
                await m_alarmsClient.Initialize().ConfigureAwait(false);
            }
        }

        #endregion

        #region ExecuteCommand Handler for Access Rights

        private Task EndAccessRights_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_accessRightsClient != null)
            {
                m_accessRightsClient.Dispose();
                m_accessRightsClient = null;
            }
            return Task.CompletedTask;
        }
        private Task StartAccessRights_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_accessRightsClient == null)
            {
                m_accessRightsClient = new AccessRightsClient();               
            }
            return Task.CompletedTask;
        }

        private async Task AccessRestrictions_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_accessRightsClient != null)
            {
                await m_accessRightsClient.SampleAccessRestrictions().ConfigureAwait(false);
            }
        }
        private async Task RolePermissions_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_accessRightsClient != null)
            {
                await m_accessRightsClient.SampleRolePermissions().ConfigureAwait(false);
            }
        }

        private async Task UserRolePermissions_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_accessRightsClient != null)
            {
                await m_accessRightsClient.SampleUserRolePermissions().ConfigureAwait(false);
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for Browse & Translate

        private async Task StartBrowseClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient == null)
            {
                m_browseClient = new BrowseClient(m_application);
                await m_browseClient.InitializeSession().ConfigureAwait(false);
            }
        }

        private async Task EndBrowseClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                await m_browseClient.DisconnectSession().ConfigureAwait(false);
                m_browseClient = null;
            }
        }

        private Task Translate_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                //call translate single path
                m_browseClient.TranslateBrowsePathToNodeIds();
                //call translate multiple paths
                m_browseClient.TranslateBrowsePathsToNodeIds();
            }
            return Task.CompletedTask;
        }

        private Task BrowseServerWithOptions_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                m_browseClient.BrowseWithOptions();
            }
            return Task.CompletedTask;
        }

        private Task BrowseServer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                m_browseClient.BrowseTheServer();
            }
            return Task.CompletedTask;
        }

        #endregion

        #region ExecuteCommand Handlers for Connect

        private async Task ConnectSample_ExecuteCommand(object sender, EventArgs e)
        {
            //ConnectClient sample does not need to lpad data type dictionaries or to decode custom data types
            bool rememberDecodeCustomDataTypes = m_application.ClientToolkitConfiguration.DecodeCustomDataTypes;
            bool rememberDecodeDataTypeDictionaries = m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries;

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;

            ConnectClient connectClient = new ConnectClient(m_application);

            await connectClient.CreateOpcTcpSessionWithNoSecurity().ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.Sign, SecurityPolicy.Basic256Sha256).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.Sign, SecurityPolicy.Aes128_Sha256_RsaOaep).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Aes128_Sha256_RsaOaep).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.Sign, SecurityPolicy.Aes256_Sha256_RsaPss).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Aes256_Sha256_RsaPss).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithUserId().ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithCertificate().ConfigureAwait(false);

            //await connectClient.CreateHttpsSessionWithAnonymousUserId().ConfigureAwait(false);
            //await connectClient.CreateHttpsSessionWithUserId().ConfigureAwait(false);

            await connectClient.CreateSessionUsingDiscovery().ConfigureAwait(false);

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = rememberDecodeCustomDataTypes;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = rememberDecodeDataTypeDictionaries;
        }

        private async Task ReverseConnectSample_ExecuteCommand(object sender, EventArgs e)
        {
            //ConnectClient sample does not need to lpad data type dictionaries or to decode custom data types
            bool rememberDecodeCustomDataTypes = m_application.ClientToolkitConfiguration.DecodeCustomDataTypes;
            bool rememberDecodeDataTypeDictionaries = m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries;
            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;

            
            ReverseConnectClient reverseConnectClient = new ReverseConnectClient(m_application);
            //await reverseConnectClient.CreateOpcTcpSessionWithNoSecurity().ConfigureAwait(false);

            // get all endpoints and create sessions that will be connected synchronously
            await reverseConnectClient.GetEndpointsAndReverseConnect(false).ConfigureAwait(false);

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = rememberDecodeCustomDataTypes;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = rememberDecodeDataTypeDictionaries;
        }

        private async Task ReverseConnectSampleAsync_ExecuteCommand(object sender, EventArgs e)
        {
            ReverseConnectClient reverseConnectClient = new ReverseConnectClient(m_application);
            
            await reverseConnectClient.GetEndpointsAndReverseConnect(true).ConfigureAwait(false);
        }

        #endregion

        #region ExecuteCommand Handlers for Discovery

        /// <summary>
        /// ExeuteCommand handler for DiscoverServers command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private Task DiscoverServersSample_ExecuteCommand(object sender, EventArgs e)
        {
            //initialize discovery sample
            DiscoveryClient discoveryClientSample = new DiscoveryClient(m_application);
            //call sample discovery methods
            discoveryClientSample.DiscoverServers();
            return Task.CompletedTask;
        }

        /// <summary>
        /// ExeuteCommand handler for DiscoverServersOnNetwork command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private Task DiscoverServersOnNetworkSample_ExecuteCommand(object sender, EventArgs e)
        {
            //initialize discovery sample
            DiscoveryClient discoveryClientSample = new DiscoveryClient(m_application);
            //call sample discovery methods
            discoveryClientSample.DiscoverServersOnNetwork();
            return Task.CompletedTask;
        }

        /// <summary>
        /// ExeuteCommand handler for DiscoverServersasync command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task DiscoverServersSampleAsync_ExecuteCommand(object sender, EventArgs e)
        {
            //initialize discovery sample
            DiscoveryClient discoveryClientSample = new DiscoveryClient(m_application);
            //call sample discovery methods
            await discoveryClientSample.DiscoverServersAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// ExeuteCommand handler for DiscoverServersOnNetworkAsync command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task DiscoverServersOnNetworkSampleAsync_ExecuteCommand(object sender, EventArgs e)
        {
            //initialize discovery sample
            DiscoveryClient discoveryClientSample = new DiscoveryClient(m_application);

            //call sample discovery methods
            await discoveryClientSample.DiscoverServersOnNetworkAsync().ConfigureAwait(false);
        }


        /// <summary>
        /// ExeuteCommand handler for GDS Pull Register and Sign Certificate command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private Task GdsPullRegisterAndSignCertificateSample_ExecuteCommand(object sender, EventArgs e)
        {
            InitializeGdsClient();

            if (m_gdsClient != null)
            {
                m_gdsClient.ExecutePullRegisterAndSignSample();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// ExeuteCommand handler for GDS Pull Get Trust List command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task GdsPullTrustListSample_ExecuteCommand(object sender, EventArgs e)
        {
            InitializeGdsClient();

            if (m_gdsClient != null)
            {
                await m_gdsClient.ExecutePullGetTrustListSample().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// ExeuteCommand handler for GDS Push command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task GDSPushCertificateSample_ExecuteCommand(object sender, EventArgs e)
        {
            InitializeGdsClient();

            if (m_gdsClient != null)
            {
                await m_gdsClient.ExecutePushCertificateSample().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// ExeuteCommand handler for GDS Push command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task GDSPushTrustListSample_ExecuteCommand(object sender, EventArgs e)
        {
            InitializeGdsClient();

            if (m_gdsClient != null)
            {
                await m_gdsClient.ExecutePushTrustListSample().ConfigureAwait(false);
            }
        }
        /// <summary>
        ///  Initialize the <see cref="GdsClient"/> instance
        /// </summary>
        private void InitializeGdsClient()
        {
            GdsConnectionConfiguration gdsConnectionConfiguration = m_application.Configuration.ParseExtension<GdsConnectionConfiguration>();
            if (gdsConnectionConfiguration == null)
            {
                Console.WriteLine("The SampleClient.Config.xml configuration file does not contain the <GdsConnectionConfiguration> section.");
                return;
            }
            if (m_gdsClient == null)
            {
                m_gdsClient = new GdsClient(m_application);                
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for Events

        private async Task EndEvents_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                await m_eventsClient.Disconnect().ConfigureAwait(false);
                m_eventsClient = null;
            }
        }

        private Task DeleteEventMonitorItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.DeleteEventMonitoredItem();
            }
            return Task.CompletedTask;
        }

        private Task CreateEventMonitorItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.CreateEventMonitoredItem();
            }
            return Task.CompletedTask;
        }

        private async Task StartEventsClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient == null)
            {
                m_eventsClient = new EventsClient(m_application);
                await m_eventsClient.Initialize().ConfigureAwait(false);
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for History

        private async Task EndHistory_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                await m_historyClient.DisconnectSession().ConfigureAwait(false);
                m_historyClient = null;
            }
        }

        private Task HistoryReadProcessed_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                m_historyClient.HistoryReadProcessed();
            }
            return Task.CompletedTask;
        }

        private Task HistoryReadAtTime_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                m_historyClient.HistoryReadAtTime();
            }
            return Task.CompletedTask;
        }

        private Task HistoryReadRaw_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                m_historyClient.HistoryReadRaw();
            }
            return Task.CompletedTask;
        }

        private async Task StartHistory_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient == null)
            {
                m_historyClient = new HistoryClient(m_application);
                await m_historyClient.InitializeSession().ConfigureAwait(false);
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for MonitoredItem

        private async Task StartMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient == null)
            {
                m_monitoredItemClient = new MonitoredItemClient(m_application);
                await m_monitoredItemClient.Initialize().ConfigureAwait(false);
            }
        }

        private Task CreateMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.CreateMonitoredItem();
            }
            return Task.CompletedTask;
        }

        private Task DeleteMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.DeleteMonitoredItem();
            }
            return Task.CompletedTask;
        }

        private async Task EndMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                await m_monitoredItemClient.Disconnect().ConfigureAwait(false);
                m_monitoredItemClient = null;
            }
        }

        #endregion

        #region ExecuteCommand Handler for Method Calls

        /// <summary>
        /// Call methods on server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task CallMethods_ExecuteCommand(object sender, EventArgs e)
        {
            //initialize method call sample
            MethodCallClient methodCallClient = new MethodCallClient(m_application);

            //initialize session
            await methodCallClient.InitializeSession().ConfigureAwait(false);
            //call method 
            methodCallClient.CallMethod();

            methodCallClient.CallCountRefrigeratorStatesMethod();
            //call async method 
            methodCallClient.AsyncCallMethod();

            //wait and close session
            Task.Delay(1000).Wait();
            await methodCallClient.DisconnectSession().ConfigureAwait(false);
        }

        #endregion

        #region ExecuteCommand Handler for Read Write 

        private async Task EndReadWrite_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                await m_readWriteClient.DisconnectSession().ConfigureAwait(false);
                m_readWriteClient = null;
            }
        }

        private Task Write_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                m_readWriteClient.WriteValueForNode();
                m_readWriteClient.WriteArrayValueForNode();
                m_readWriteClient.WriteComplexValueForNode();
                m_readWriteClient.WriteEnumValueForNode();
                m_readWriteClient.WriteMultipleNodesValues();
                m_readWriteClient.WriteValuesForCustomDataTypes();
            }
            return Task.CompletedTask;
        }

        private Task Read_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                m_readWriteClient.ReadVariableNode();
                m_readWriteClient.ReadObjectNode();
                m_readWriteClient.ReadValueForNode();
                m_readWriteClient.ReadArrayValue();
                m_readWriteClient.ReadComplexValue();
                m_readWriteClient.ReadEnumValue();
                m_readWriteClient.ReadMultipleNodesValues();
                m_readWriteClient.ReadValuesForCustomDataTypes();
            }
            return Task.CompletedTask;
        }

        private Task RegisterNodes_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                m_readWriteClient.RegisterNodesSample();
            }
            return Task.CompletedTask;
        }

        private async Task StartReadWrite_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient == null)
            {    
                m_readWriteClient = new ReadWriteClient(m_application);
                await m_readWriteClient.InitializeSession().ConfigureAwait(false);
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for FileTransfer

        private async Task StartFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient == null)
            {
                m_fileTransferClient = new FileTransferClient(m_application);
                await m_fileTransferClient.Initialize().ConfigureAwait(false);
            }
        }

        private Task UploadFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.UploadFile();
            }
            return Task.CompletedTask;
        }

        private Task DownloadFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.DownloadFile();
            }
            return Task.CompletedTask;
        }

        private Task ReadByteArrayFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.ReadByteString();
            }
            return Task.CompletedTask;
        }

        private Task ReadTemporaryFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.DownloadTemporaryFile();
            }
            return Task.CompletedTask;
        }

        private Task WriteTemporaryFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.UploadTemporaryFile();
            }
            return Task.CompletedTask;
        }

        private async Task EndFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                await m_fileTransferClient.Disconnect().ConfigureAwait(false);
                m_fileTransferClient = null;
            }
        }

        private async Task StartPubSubCfgMenu_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_pubSubClient == null)
            {
                m_pubSubClient = new PubSubClient(m_application);
                await m_pubSubClient.Initialize().ConfigureAwait(false);
            }
        }

        private Task StartPubSubReadCfg_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_pubSubClient != null)
            {
                m_pubSubClient.ReadPubSubConfiguration();
            }
            return Task.CompletedTask;
        }

        private async Task ExitPubSubCfgMenu_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_pubSubClient != null)
            {
                await m_pubSubClient.Disconnect().ConfigureAwait(false);
                m_pubSubClient = null;
            }
        }
        

        #endregion

        #region ExecuteCommand Handler for Exit

        private async Task Exit_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                await m_browseClient.DisconnectSession().ConfigureAwait(false);
            }
            if (m_monitoredItemClient != null)
            {
                await m_monitoredItemClient.Disconnect().ConfigureAwait(false);
            }
            if (m_fileTransferClient != null)
            {
                await m_fileTransferClient.Disconnect().ConfigureAwait(false);
            }
            if (m_alarmsClient != null)
            {
                await m_alarmsClient.Disconnect().ConfigureAwait(false);
            }
            if (m_eventsClient != null)
            {
                await m_eventsClient.Disconnect().ConfigureAwait(false);
            }
            if (m_historyClient != null)
            {
                await m_historyClient.DisconnectSession().ConfigureAwait(false);
            }
            if (m_readWriteClient != null)
            {
                await m_readWriteClient.DisconnectSession().ConfigureAwait(false);
            }
        }

        #endregion

        #region Process Private Methods

        /// <summary>
        /// Gets the available StateTransition object for current state and a command 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private StateTransition GetStateTransitionForCommand(Command command)
        {
            foreach (var stateTransition in m_transitions.Keys)
            {
                if (stateTransition.CurrentState == CurrentState &&
                    stateTransition.CommandDescriptor.Command == command)
                {
                    return stateTransition;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the list of possible command descriptors for current state
        /// </summary>
        /// <returns></returns>
        private IList<CommandDescriptor> GetPossibleCommands()
        {
            if (m_processStateCommands.ContainsKey(CurrentState))
            {
                return m_processStateCommands[CurrentState];
            }
            return new List<CommandDescriptor>();
        }

        /// <summary>
        /// Computes and prints to console the list of available commands 
        /// </summary>
        private void DisplayListOfCommands()
        {
            var commandDescriptors = GetPossibleCommands();

            StringBuilder commandListText = new StringBuilder();
            commandListText.AppendFormat("\r\n{0} Menu:\r\n", CurrentState);

            foreach (var commandDescriptor in commandDescriptors)
            {
                commandListText.AppendFormat("{0} - {1}\r\n", commandDescriptor.Keyword, commandDescriptor.Description);
            }
            Console.WriteLine(commandListText);
        }

        #endregion
    }
}
