/* ========================================================================
 * Copyright © 2011-2024 Softing Industrial Automation GmbH. 
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
using System.Threading;
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
            InitializeCallMethodsTransitions();
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
            exit = new StateTransition(State.MonitoredTransferEventsAlarms, Command.Exit, "x", "Exit Client Application");
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
        private void InitializeDiscoveryConnectTransitions()
        {
            //commAands for browse
            StateTransition startDCClient = new StateTransition(State.Main, Command.DiscoveryConnect, "1", "Enter Connect/Reverse Connect/Discovery/GDS Menu");
            m_transitions.Add(startDCClient, State.DiscoveryConnectGds);

            //add Connect menu item
            StateTransition connectMenuSample = new StateTransition(State.DiscoveryConnectGds, Command.ConnectSample, "1", "Enter Connect Sample Menu");
            m_transitions.Add(connectMenuSample, State.Connects);

            //add connect menu item
            StateTransition connectSample = new StateTransition(State.Connects, Command.ConnectSampleRSA, "1", "Execute Connect Sample using RSA");
            connectSample.ExecuteCommand += ConnectSample_ExecuteCommand;
            m_transitions.Add(connectSample, State.Connects);

            //add ECC NistP256 connect menu item
            StateTransition connectSampleEccNistP256 = new StateTransition(State.Connects, Command.ConnectSampleECCNistP256, "2", "Execute Connect Sample using ECC NistP256");
            connectSampleEccNistP256.ExecuteCommand += ConnectSample_ECC_NistP256_ExecuteCommand;
            m_transitions.Add(connectSampleEccNistP256, State.Connects);

            //add ECC NistP384 connect menu item
            StateTransition connectSampleEccNistP384 = new StateTransition(State.Connects, Command.ConnectSampleECCNistP384, "3", "Execute Connect Sample using ECC NistP384");
            connectSampleEccNistP384.ExecuteCommand += ConnectSample_ECC_NistP384_ExecuteCommand;
            m_transitions.Add(connectSampleEccNistP384, State.Connects);

            //add ECC BrainpoolP256r1 connect menu item
            StateTransition connectSampleEccBrainpoolP256r1 = new StateTransition(State.Connects, Command.ConnectSampleECCBrainpoolP256r1, "4", "Execute Connect Sample using ECC BrainpoolP256r1");
            connectSampleEccBrainpoolP256r1.ExecuteCommand += ConnectSample_ECC_BrainpoolP256r1_ExecuteCommand;
            m_transitions.Add(connectSampleEccBrainpoolP256r1, State.Connects);

            //add ECC BrainpoolP384r1 connect menu item
            StateTransition connectSampleEccBrainpoolP384r1 = new StateTransition(State.Connects, Command.ConnectSampleECCBrainpoolP384r1, "5", "Execute Connect Sample using ECC BrainpoolP384r1");
            connectSampleEccBrainpoolP384r1.ExecuteCommand += ConnectSample_ECC_BrainpoolP384r1_ExecuteCommand;
            m_transitions.Add(connectSampleEccBrainpoolP384r1, State.Connects);

            StateTransition endConnectSample = new StateTransition(State.Connects, Command.EndConnectSample, "0", "Back to Discovery/Connect/GDS Menu");
            m_transitions.Add(endConnectSample, State.DiscoveryConnectGds);

            //add reverse connect menu item
            StateTransition reverseConnectSample = new StateTransition(State.DiscoveryConnectGds, Command.ReverseConnectSample, "2", "Execute Reverse Connect Sample");
            reverseConnectSample.ExecuteCommand += ReverseConnectSample_ExecuteCommand;
            m_transitions.Add(reverseConnectSample, State.DiscoveryConnectGds);

            //add reverse connect menu item with timeout
            StateTransition reverseConnectSampleTimeout = new StateTransition(State.DiscoveryConnectGds, Command.ReverseConnectSampleTimeout, "3", "Execute Reverse Connect Sample into a specified time interval");
            reverseConnectSampleTimeout.ExecuteCommand += ReverseConnectSampleTimeoutInterval_ExecuteCommand;
            m_transitions.Add(reverseConnectSampleTimeout, State.DiscoveryConnectGds);

            //add async reverse connect menu item
            StateTransition reverseConnectSampleAsync = new StateTransition(State.DiscoveryConnectGds, Command.ReverseConnectSampleAsync, "4", "Execute Reverse Connect Async Sample");
            reverseConnectSampleAsync.ExecuteCommand += ReverseConnectSampleAsync_ExecuteCommand;
            m_transitions.Add(reverseConnectSampleAsync, State.DiscoveryConnectGds);

            //add discovery menu item
            StateTransition discoveryMenu = new StateTransition(State.DiscoveryConnectGds, Command.StartDiscoverySample, "5", "Enter Discovery Menu");
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
            StateTransition gdsSample = new StateTransition(State.DiscoveryConnectGds, Command.StartGDSSample, "6", "Enter GDS Sample Menu");
            m_transitions.Add(gdsSample, State.GDS);

            StateTransition connectAndReconnectSample = new StateTransition(State.DiscoveryConnectGds, Command.ConnectAndReconnectSample, "7", "Execute Connect and Reconnect using SessionReconnectHandler");
            connectAndReconnectSample.ExecuteCommand += ConnectAndReconnectUsingSessionReconnectHandlerSample_ExecuteCommand;
            m_transitions.Add(connectAndReconnectSample, State.DiscoveryConnectGds);

            StateTransition reverseConnectAndReconnectSample = new StateTransition(State.DiscoveryConnectGds, Command.ReverseConnectAndReconnectSample, "8", "Execute Reverse Connect and Reconnect using SessionReconnectHandler");
            reverseConnectAndReconnectSample.ExecuteCommand += ReverseConnectReconnectUsingSessionReconnectHandler_ExecuteCommand;
            m_transitions.Add(reverseConnectAndReconnectSample, State.DiscoveryConnectGds);

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

            StateTransition browseServerAsync = new StateTransition(State.Browse, Command.BrowseServerAsync, "4", "Browse server Asynchronously");
            browseServerAsync.ExecuteCommand += BrowseServerAsync_ExecuteCommand;
            m_transitions.Add(browseServerAsync, State.Browse);

            StateTransition browseServerWithOptionsAsync = new StateTransition(State.Browse, Command.BrowseServerWithOptionsAsync, "5", "Browse server with options Asynchronously");
            browseServerWithOptionsAsync.ExecuteCommand += BrowseServerWithOptionsAsync_ExecuteCommand;
            m_transitions.Add(browseServerWithOptionsAsync, State.Browse);

            StateTransition translateAsync = new StateTransition(State.Browse, Command.TranslateAsync, "6", "Translate BrowsePaths to NodeIds Asynchronously");
            translateAsync.ExecuteCommand += TranslateAsync_ExecuteCommand;
            m_transitions.Add(translateAsync, State.Browse);

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

            StateTransition registerNodesAsync = new StateTransition(State.ReadWriteRegisterNodes, Command.RegisterNodesAsync, "4", "Register Nodes Asynchronously");
            registerNodesAsync.ExecuteCommand += RegisterNodesAsync_ExecuteCommand;
            m_transitions.Add(registerNodesAsync, State.ReadWriteRegisterNodes);

            StateTransition readAsync = new StateTransition(State.ReadWriteRegisterNodes, Command.ReadAsync, "5", "Read Nodes Asynchronously");
            readAsync.ExecuteCommand += ReadAsync_ExecuteCommand;
            m_transitions.Add(readAsync, State.ReadWriteRegisterNodes);

            StateTransition writeAsync = new StateTransition(State.ReadWriteRegisterNodes, Command.WriteAsync, "6", "Write Nodes Asynchronously");
            writeAsync.ExecuteCommand += WriteAsync_ExecuteCommand;
            m_transitions.Add(writeAsync, State.ReadWriteRegisterNodes);

            StateTransition endReadWrite = new StateTransition(State.ReadWriteRegisterNodes, Command.EndReadWriteRegister, "0", "Back to Main Menu");
            endReadWrite.ExecuteCommand += EndReadWrite_ExecuteCommand;
            m_transitions.Add(endReadWrite, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for MonitoredItem/ Events (4)
        /// </summary>
        private void InitializeMonitoredItemEventsTransitions()
        {
            //commands for monitored item - 4
            StateTransition start = new StateTransition(State.Main, Command.StartMonitoredTransferEventsAlarms, "4", "Enter MonitoredItem/TransferSubscriptions/Events/Alarms Menu");
            m_transitions.Add(start, State.MonitoredTransferEventsAlarms);

            MonitorMenu();

            EventsMenu();

            AlarmsMenu();

            TransferSubscriptionsMenu();

            StateTransition end = new StateTransition(State.MonitoredTransferEventsAlarms, Command.EndMonitoredTransferEventsAlarms, "0", "Back to Main Menu");
            m_transitions.Add(end, State.Main);
        }

        private void MonitorMenu()
        {
            //commands for monitored items
            StateTransition startMonitoredItems = new StateTransition(State.MonitoredTransferEventsAlarms, Command.StartMonitoredItems, "1", "Enter MonitoredItem Menu");
            m_transitions.Add(startMonitoredItems, State.MonitoredItems);

            StateTransition startMonitoredItemsWithConnect = new StateTransition(State.MonitoredItems, Command.StartMonitoredItem, "1", "Enter MonitoredItem with implicit Subscription connect Menu (Legacy behavior)");
            startMonitoredItemsWithConnect.ExecuteCommand += StartMonitoredItem_ExecuteCommand;
            m_transitions.Add(startMonitoredItemsWithConnect, State.MonitoredItem);

            StateTransition startMonitoredItemsWithoutConnect = new StateTransition(State.MonitoredItems, Command.StartMonitoredItemsWithoutConnect, "2", "Enter MonitoredItem before Subscription connect Menu");
            startMonitoredItemsWithoutConnect.ExecuteCommand += StartMonitoredItemsWithoutConnect_ExecuteCommand;
            m_transitions.Add(startMonitoredItemsWithoutConnect, State.MonitoredItemWithoutConnect);

            StateTransition startAddNewMonitoredItemsAfterConnect = new StateTransition(State.MonitoredItems, Command.StartMonitoredItemsAddNewAfterConnect, "3", "Enter add new MonitoredItem while Subscription is connected Menu");
            startAddNewMonitoredItemsAfterConnect.ExecuteCommand += StartAddNewMonitoredItemsAfterConnect_ExecuteCommand;
            m_transitions.Add(startAddNewMonitoredItemsAfterConnect, State.MonitoredItemAddNewAfterConnect);

            #region Monitored Items with connect
            StateTransition createMonitoredItem = new StateTransition(State.MonitoredItem, Command.CreateMonitoredItem, "1", "Create data change Monitored Items");
            createMonitoredItem.ExecuteCommand += CreateMonitoredItem_ExecuteCommand;
            m_transitions.Add(createMonitoredItem, State.MonitoredItem);

            StateTransition deleteMonitoredItem = new StateTransition(State.MonitoredItem, Command.DeleteMonitoredItem, "2", "Delete data change Monitored Items");
            deleteMonitoredItem.ExecuteCommand += DeleteMonitoredItems_ExecuteCommand;
            m_transitions.Add(deleteMonitoredItem, State.MonitoredItem);

            StateTransition endMonitoredItem = new StateTransition(State.MonitoredItem, Command.EndMonitoredItem, "0", "Back to MonitoredItem Menu");
            endMonitoredItem.ExecuteCommand += EndMonitoredItem_ExecuteCommand;
            m_transitions.Add(endMonitoredItem, State.MonitoredItems);
            #endregion Monitored Items with connect

            #region Monitored Items without connect
            StateTransition createMonitoredItemWithoutConnect = new StateTransition(State.MonitoredItemWithoutConnect, Command.CreateMonitoredItemsWithoutConnect, "1", "Create data change Monitored Items before Subscription is connected");
            createMonitoredItemWithoutConnect.ExecuteCommand += CreateMonitoredItemWithoutConnect_ExecuteCommand;
            m_transitions.Add(createMonitoredItemWithoutConnect, State.MonitoredItemWithoutConnect);

            StateTransition deleteMonitoredItemWithoutConnect = new StateTransition(State.MonitoredItemWithoutConnect, Command.DeleteMonitoredItemsWithoutConnect, "2", "Delete data change Monitored Items before Subscription is connected");
            deleteMonitoredItemWithoutConnect.ExecuteCommand += DeleteMonitoredItemsWithoutConnect_ExecuteCommand;
            m_transitions.Add(deleteMonitoredItemWithoutConnect, State.MonitoredItemWithoutConnect);

            StateTransition endMonitoredItemWithoutConnect = new StateTransition(State.MonitoredItemWithoutConnect, Command.EndMonitoredItemsWithoutConnect, "0", "Back to MonitoredItem Menu");
            endMonitoredItemWithoutConnect.ExecuteCommand += EndMonitoredItemsWithoutConnect_ExecuteCommand;
            m_transitions.Add(endMonitoredItemWithoutConnect, State.MonitoredItems);
            #endregion Monitored Items without connect

            #region Add Monitored Items after connect
            StateTransition createAddNewMonitoredItemAfterConnect = new StateTransition(State.MonitoredItemAddNewAfterConnect, Command.CreateMonitoredItemsAddNewAfterConnect, "1", "Create new data change Monitored Items while Subscription is connected");
            createAddNewMonitoredItemAfterConnect.ExecuteCommand += CreateAddNewMonitoredItemsAfterConnect_ExecuteCommand;
            m_transitions.Add(createAddNewMonitoredItemAfterConnect, State.MonitoredItemAddNewAfterConnect);

            StateTransition deleteAddNewMonitoredItemAfterConnect = new StateTransition(State.MonitoredItemAddNewAfterConnect, Command.DeleteMonitoredItemsAddNewAfterConnect, "2", "Delete new data change Monitored Items while Subscription is connected");
            deleteAddNewMonitoredItemAfterConnect.ExecuteCommand += DeleteAddNewMonitoredItemsAfterConnect_ExecuteCommand;
            m_transitions.Add(deleteAddNewMonitoredItemAfterConnect, State.MonitoredItemAddNewAfterConnect);

            StateTransition endAddNewMonitoredItemAfterConnect = new StateTransition(State.MonitoredItemAddNewAfterConnect, Command.EndMonitoredItemsAddNewAfterConnect, "0", "Back to MonitoredItems Menu");
            endAddNewMonitoredItemAfterConnect.ExecuteCommand += EndAddNewMonitoredItemsAfterConnect_ExecuteCommand;
            m_transitions.Add(endAddNewMonitoredItemAfterConnect, State.MonitoredItems);
            #endregion Add Monitored Items after connect

            StateTransition endMonitoredItems = new StateTransition(State.MonitoredItems, Command.EndMonitoredItems, "0", "Back to MonitoredItem/TransferSubscriptions/Events/Alarms Menu");
            m_transitions.Add(endMonitoredItems, State.MonitoredTransferEventsAlarms);
        }

        private void EventsMenu()
        {
            //commands for events
            StateTransition startMonitoredItems = new StateTransition(State.MonitoredTransferEventsAlarms, Command.StartMonitoredEvents, "2", "Enter Events Menu");
            m_transitions.Add(startMonitoredItems, State.MonitoredEvents);

            StateTransition startMonitoredItemsWithConnect = new StateTransition(State.MonitoredEvents, Command.StartEvents, "1", "Enter Event Monitored with implicit Subscription connect Menu (Legacy behavior)");
            startMonitoredItemsWithConnect.ExecuteCommand += StartEventsClient_ExecuteCommand;
            m_transitions.Add(startMonitoredItemsWithConnect, State.Events);

            StateTransition startMonitoredItemsWithoutConnect = new StateTransition(State.MonitoredEvents, Command.StartEventsWithoutConnect, "2", "Enter Event Monitored before Subscription connect Menu");
            startMonitoredItemsWithoutConnect.ExecuteCommand += StartEventsClientWithoutConnect_ExecuteCommand;
            m_transitions.Add(startMonitoredItemsWithoutConnect, State.EventsWithoutConnect);

            StateTransition startAddNewMonitoredItemsAfterConnect = new StateTransition(State.MonitoredEvents, Command.StartEventsAddNewAfterConnect, "3", "Enter add new Event MonitoredItem while Subscription is connected Menu");
            startAddNewMonitoredItemsAfterConnect.ExecuteCommand += StartAddNewEventsClientAfterConnect_ExecuteCommand;
            m_transitions.Add(startAddNewMonitoredItemsAfterConnect, State.EventsAddNewAfterConnect);

            StateTransition startDoubleFilteringMonitoredItemsWithConnect = new StateTransition(State.MonitoredEvents, Command.StartDoubleFilteringEvents, "4", "Enter Double Filtering Event Monitored with implicit Subscription connect Menu");
            startDoubleFilteringMonitoredItemsWithConnect.ExecuteCommand += StartDoubleFilteringEventsClient_ExecuteCommand;
            m_transitions.Add(startDoubleFilteringMonitoredItemsWithConnect, State.EventsDoubleFilteringWithConnect);

            #region Event Monitored Items with connect
            StateTransition createEventMonitorItem = new StateTransition(State.Events, Command.CreateEventMonitorItem, "1", "Create Event Monitored Item");
            createEventMonitorItem.ExecuteCommand += CreateEventMonitoredItem_ExecuteCommand;
            m_transitions.Add(createEventMonitorItem, State.Events);

            StateTransition deleteEventMonitorItem = new StateTransition(State.Events, Command.DeleteEventMonitorItem, "2", "Delete Event Monitored Item");
            deleteEventMonitorItem.ExecuteCommand += DeleteEventMonitoredItem_ExecuteCommand;
            m_transitions.Add(deleteEventMonitorItem, State.Events);

            StateTransition endEvents = new StateTransition(State.Events, Command.EndEvents, "0", "Back to Monitored Events Menu");
            endEvents.ExecuteCommand += EndEvents_ExecuteCommand;
            m_transitions.Add(endEvents, State.MonitoredEvents);
            #endregion Event Monitored Items with connect

            #region Event Monitored Items before Subscription connect
            StateTransition createEventMonitorItemWithoutConnect = new StateTransition(State.EventsWithoutConnect, Command.CreateEventMonitorItemWithoutConnect, "1", "Create Event Monitored Item before Subscription connect");
            createEventMonitorItemWithoutConnect.ExecuteCommand += CreateEventMonitoredItemWithoutConnect_ExecuteCommand;
            m_transitions.Add(createEventMonitorItemWithoutConnect, State.EventsWithoutConnect);

            StateTransition deleteEventMonitorItemWithoutConnect = new StateTransition(State.EventsWithoutConnect, Command.DeleteEventMonitorItemWithoutConnect, "2", "Delete Event Monitored Item before Subscription connect");
            deleteEventMonitorItemWithoutConnect.ExecuteCommand += DeleteEventMonitoredItemWithoutConnect_ExecuteCommand;
            m_transitions.Add(deleteEventMonitorItemWithoutConnect, State.EventsWithoutConnect);

            StateTransition endEventsWithoutConnect = new StateTransition(State.EventsWithoutConnect, Command.EndEvents, "0", "Back to Monitored Events Menu");
            endEventsWithoutConnect.ExecuteCommand += EndEventsWithoutConnect_ExecuteCommand;
            m_transitions.Add(endEventsWithoutConnect, State.MonitoredEvents);
            #endregion Event Monitored Items without connect

            #region Event Add New Monitored Items while Subscription is connected
            StateTransition createAddNewEventMonitorItemAfterConnect = new StateTransition(State.EventsAddNewAfterConnect, Command.CreateEventMonitorItemAddNewAfterConnect, "1", "Create add new Event Monitored Item while Subscription is connected");
            createAddNewEventMonitorItemAfterConnect.ExecuteCommand += CreateAddNewEventMonitoredItemAfterConnect_ExecuteCommand;
            m_transitions.Add(createAddNewEventMonitorItemAfterConnect, State.EventsAddNewAfterConnect);

            StateTransition deleteAddNewEventMonitorItemAfterConnect = new StateTransition(State.EventsAddNewAfterConnect, Command.DeleteEventMonitorItemAddNewAfterConnect, "2", "Delete add new Event Monitored Item while Subscription is connected");
            deleteAddNewEventMonitorItemAfterConnect.ExecuteCommand += DeleteAddNewEventMonitoredItemAfterConnect_ExecuteCommand;
            m_transitions.Add(deleteAddNewEventMonitorItemAfterConnect, State.EventsAddNewAfterConnect);

            StateTransition endAddNewEventsAfterConnect = new StateTransition(State.EventsAddNewAfterConnect, Command.EndEvents, "0", "Back to Monitored Events Menu");
            endAddNewEventsAfterConnect.ExecuteCommand += EndAddNewEventsAfterConnect_ExecuteCommand;
            m_transitions.Add(endAddNewEventsAfterConnect, State.MonitoredEvents);
            #endregion Event Add New Monitored Items after connect

            #region Double Filtering Event Monitored Items with connect
            StateTransition createDoubleFilteringEventMonitorItem = new StateTransition(State.EventsDoubleFilteringWithConnect, Command.CreateDoubleFilteringEventMonitorItem, "1", "Create Double Filtering Event Monitored Item");
            createDoubleFilteringEventMonitorItem.ExecuteCommand += CreateDoubleFilteringEventMonitoredItem_ExecuteCommand;
            m_transitions.Add(createDoubleFilteringEventMonitorItem, State.EventsDoubleFilteringWithConnect);

            StateTransition deleteDoubleFilteringEventMonitorItem = new StateTransition(State.EventsDoubleFilteringWithConnect, Command.DeleteDoubleFilteringEventMonitorItem, "2", "Delete Double Filtering Event Monitored Item");
            deleteDoubleFilteringEventMonitorItem.ExecuteCommand += DeleteDoubleFilteringEventMonitoredItem_ExecuteCommand;
            m_transitions.Add(deleteDoubleFilteringEventMonitorItem, State.EventsDoubleFilteringWithConnect);

            StateTransition endDoubleFilteringEvents = new StateTransition(State.EventsDoubleFilteringWithConnect, Command.EndEvents, "0", "Back to Monitored Events Menu");
            endDoubleFilteringEvents.ExecuteCommand += EndDoubleFilteringEvents_ExecuteCommand;
            m_transitions.Add(endDoubleFilteringEvents, State.MonitoredEvents);
            #endregion Double Filtering Event Monitored Items with connect

            StateTransition endMonitoredItems = new StateTransition(State.MonitoredEvents, Command.EndMonitoredItems, "0", "Back to MonitoredItem/TransferSubscriptions/Events/Alarms Menu");
            m_transitions.Add(endMonitoredItems, State.MonitoredTransferEventsAlarms);
        }

        private void AlarmsMenu()
        {
            //commands for alarms
            StateTransition startAlarms = new StateTransition(State.MonitoredTransferEventsAlarms, Command.StartAlarms, "3", "Enter Alarms Menu");
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

            StateTransition endAlarms = new StateTransition(State.Alarms, Command.EndAlarms, "0", "Back to MonitoredItem/TransferSubscriptions/Events/Alarms Menu");
            endAlarms.ExecuteCommand += EndAlarms_ExecuteCommand;
            m_transitions.Add(endAlarms, State.MonitoredTransferEventsAlarms);
        }

        private void TransferSubscriptionsMenu()
        {
            StateTransition startMonitoredItems = new StateTransition(State.MonitoredTransferEventsAlarms, Command.StartTransferSubscriptions, "4", "Enter Transfer Subscriptions Menu");
            startMonitoredItems.ExecuteCommand += StartMonitoredItem_ExecuteCommand;
            m_transitions.Add(startMonitoredItems, State.TransferSubscriptions);

            StateTransition createMonitoredItem = new StateTransition(State.TransferSubscriptions, Command.CreateMonitoredItem, "1", "Create data change Monitored Items");
            createMonitoredItem.ExecuteCommand += CreateMonitoredItem_ExecuteCommand;
            m_transitions.Add(createMonitoredItem, State.TransferSubscriptions);

            StateTransition deleteMonitoredItem = new StateTransition(State.TransferSubscriptions, Command.DeleteMonitoredItem, "2", "Delete data change Monitored Items");
            deleteMonitoredItem.ExecuteCommand += DeleteMonitoredItems_ExecuteCommand;
            m_transitions.Add(deleteMonitoredItem, State.TransferSubscriptions);

            StateTransition transferSubscriptions = new StateTransition(State.TransferSubscriptions, Command.TransferSubscriptions, "3", "Transfer subscriptions");
            transferSubscriptions.ExecuteCommand += TransferSubscriptions_ExecuteCommand;
            m_transitions.Add(transferSubscriptions, State.TransferSubscriptions);

            StateTransition transferSubscriptionsAsync = new StateTransition(State.TransferSubscriptions, Command.TransferSubscriptionsAsync, "4", "Transfer subscriptions async");
            transferSubscriptionsAsync.ExecuteCommand += TransferSubscriptionsAsync_ExecuteCommand;
            m_transitions.Add(transferSubscriptionsAsync, State.TransferSubscriptions);

            StateTransition transferSubscriptionSessionClosed = new StateTransition(State.TransferSubscriptions, Command.TransferSubscriptionsSessionClosed, "5", "Transfer subscriptions session closed");
            transferSubscriptionSessionClosed.ExecuteCommand += TransferSubscriptionSessionClosed_ExecuteCommand;
            m_transitions.Add(transferSubscriptionSessionClosed, State.TransferSubscriptions);

            StateTransition saveSubscriptions = new StateTransition(State.TransferSubscriptions, Command.SaveSubscriptions, "6", "Save Subscriptions for transfer");
            saveSubscriptions.ExecuteCommand += SaveSubscriptionsForTransfer_ExecuteCommand;
            m_transitions.Add(saveSubscriptions, State.TransferSubscriptions);

            StateTransition loadSubscriptions = new StateTransition(State.TransferSubscriptions, Command.LoadSubscriptions, "7", "Load Subscriptions for transfer");
            loadSubscriptions.ExecuteCommand += LoadSubscriptionsForTransfer_ExecuteCommand;
            m_transitions.Add(loadSubscriptions, State.TransferSubscriptions);

            TransferSubscriptionsDifferentConnectionsMenu();

            StateTransition endTransferSubscriptions = new StateTransition(State.TransferSubscriptions, Command.EndTransferSubscriptions, "0", "Back to MonitoredItem/TransferSubscriptions/Events/Alarms Menu");
            endTransferSubscriptions.ExecuteCommand += EndMonitoredItem_ExecuteCommand;
            m_transitions.Add(endTransferSubscriptions, State.MonitoredTransferEventsAlarms);
        }

        private void TransferSubscriptionsDifferentConnectionsMenu()
        {
            StateTransition startMonitoredItems = new StateTransition(State.TransferSubscriptions, Command.StartTransferSubscriptionsConnectionType, "8", "Transfer Subscriptions with different connections");
            m_transitions.Add(startMonitoredItems, State.TransferSubscriptionsConnectionType);

            StateTransition createMonitoredItem = new StateTransition(State.TransferSubscriptionsConnectionType, Command.CreateMonitoredItem, "1", "Create data change Monitored Items");
            createMonitoredItem.ExecuteCommand += CreateMonitoredItem_ExecuteCommand;
            m_transitions.Add(createMonitoredItem, State.TransferSubscriptionsConnectionType);

            StateTransition deleteMonitoredItem = new StateTransition(State.TransferSubscriptionsConnectionType, Command.DeleteMonitoredItem, "2", "Delete data change Monitored Items");
            deleteMonitoredItem.ExecuteCommand += DeleteMonitoredItems_ExecuteCommand;
            m_transitions.Add(deleteMonitoredItem, State.TransferSubscriptionsConnectionType);

            StateTransition transferSubscriptionsSessionSecrets = new StateTransition(State.TransferSubscriptionsConnectionType, Command.TransferSubscriptionsUserIdentity, "3", "Transfer subscriptions - Session with user identity");
            transferSubscriptionsSessionSecrets.ExecuteCommand += TransferSubscriptionsWithUserId_ExecuteCommand;
            m_transitions.Add(transferSubscriptionsSessionSecrets, State.TransferSubscriptionsConnectionType);

            StateTransition transferSubscriptionsCertificate = new StateTransition(State.TransferSubscriptionsConnectionType, Command.TransferSubscriptionsCertificate, "4", "Transfer subscriptions - Session with certificate");
            transferSubscriptionsCertificate.ExecuteCommand += TransferSubscriptionsWithCertificate_ExecuteCommand;
            m_transitions.Add(transferSubscriptionsCertificate, State.TransferSubscriptionsConnectionType);

            StateTransition transferSubscriptionsCertificatePassword = new StateTransition(State.TransferSubscriptionsConnectionType, Command.TransferSubscriptionsCertificatePassword, "5", "Transfer subscriptions - Session with certificate password");
            transferSubscriptionsCertificatePassword.ExecuteCommand += TransferSubscriptionsWithCertificatePassword_ExecuteCommand;
            m_transitions.Add(transferSubscriptionsCertificatePassword, State.TransferSubscriptionsConnectionType);

            StateTransition transferSubscriptionsWithSecurity = new StateTransition(State.TransferSubscriptionsConnectionType, Command.TransferSubscriptionsSecurity, "6", "Transfer subscriptions - Session with security");
            transferSubscriptionsWithSecurity.ExecuteCommand += TransferSubscriptionsWithSecurity_ExecuteCommand;
            m_transitions.Add(transferSubscriptionsWithSecurity, State.TransferSubscriptionsConnectionType);

            StateTransition endTransferSubscriptions = new StateTransition(State.TransferSubscriptionsConnectionType, Command.EndTransferSubscriptionsConnectionType, "0", "Back to TransferSubscriptions Menu");
            m_transitions.Add(endTransferSubscriptions, State.TransferSubscriptions);
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
        /// Initializes all sub menu transitions for CallMethod (6)
        /// </summary>
        private void InitializeCallMethodsTransitions()
        {
            //add call methods menu - 6
            StateTransition startCallMethods = new StateTransition(State.Main, Command.StartCallMethods, "6", "Enter Call Methods Menu");
            m_transitions.Add(startCallMethods, State.CallMethods);

            StateTransition callMethods = new StateTransition(State.CallMethods, Command.CallMethods, "1", "Call Methods On Server");
            callMethods.ExecuteCommand += CallMethods_ExecuteCommand;
            m_transitions.Add(callMethods, State.CallMethods);

            StateTransition callMethodsAsync = new StateTransition(State.CallMethods, Command.CallMethodsAsync, "2", "Call Methods on Server Asynchronously");
            callMethodsAsync.ExecuteCommand += CallMethodsAsync_ExecuteCommand;
            m_transitions.Add(callMethodsAsync, State.CallMethods);

            StateTransition endCallMethods = new StateTransition(State.CallMethods, Command.EndCallMethods, "0", "Back to Main Menu");
            m_transitions.Add(endCallMethods, State.Main);
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

            StateTransition historyReadAsyncRaw = new StateTransition(State.History, Command.HistoryReadRawAsync, "4", "History read raw Asynchronously");
            historyReadAsyncRaw.ExecuteCommand += HistoryReadAsyncRaw_ExecuteCommand;
            m_transitions.Add(historyReadAsyncRaw, State.History);

            StateTransition historyReadAsyncAtTime = new StateTransition(State.History, Command.HistoryReadAtTimeAsync, "5", "History read async at time Asynchronously");
            historyReadAsyncAtTime.ExecuteCommand += HistoryReadAsyncAtTime_ExecuteCommand;
            m_transitions.Add(historyReadAsyncAtTime, State.History);

            StateTransition historyReadAsyncProcessed = new StateTransition(State.History, Command.HistoryReadProcessedAsync, "6", "History read async processed Asynchronously");
            historyReadAsyncProcessed.ExecuteCommand += HistoryReadAsyncProcessed_ExecuteCommand;
            m_transitions.Add(historyReadAsyncProcessed, State.History);

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

        private async Task TranslateAsync_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                //call translate single path
                await m_browseClient.TranslateBrowsePathToNodeIdsAsync().ConfigureAwait(false);
                //call translate multiple paths
                await m_browseClient.TranslateBrowsePathsToNodeIdsAsync().ConfigureAwait(false);
            }
        }

        private Task BrowseServerWithOptions_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                m_browseClient.BrowseWithOptions();
            }
            return Task.CompletedTask;
        }

        private async Task BrowseServerWithOptionsAsync_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                await m_browseClient.BrowseWithOptionsAsync().ConfigureAwait(false);
            }
        }

        private Task BrowseServer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                m_browseClient.BrowseTheServer();
            }
            return Task.CompletedTask;
        }

        private async Task BrowseServerAsync_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                await m_browseClient.BrowseTheServerAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region ExecuteCommand Handlers for Connect

        private async Task ConnectSample_ExecuteCommand(object sender, EventArgs e)
        {
            // ConnectClient sample does not need to load data type dictionaries or to decode custom data types
            bool rememberDecodeCustomDataTypes = m_application.ClientToolkitConfiguration.DecodeCustomDataTypes;
            bool rememberDecodeDataTypeDictionaries = m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries;
            bool readNodesWithTypeNotInHierarchy = m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy;

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = false;

            ConnectClient connectClient = new ConnectClient(m_application);

            await connectClient.CreateSessionAndConnectToEndpointWithDiscovery().ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithNoSecurity().ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.Sign, SecurityPolicy.Basic256Sha256).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.Sign, SecurityPolicy.Aes128_Sha256_RsaOaep).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Aes128_Sha256_RsaOaep).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.Sign, SecurityPolicy.Aes256_Sha256_RsaPss).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Aes256_Sha256_RsaPss).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithUserId().ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithCertificate().ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithCertificatePassword().ConfigureAwait(false);

            // await connectClient.CreateHttpsSessionWithAnonymousUserId().ConfigureAwait(false);
            // await connectClient.CreateHttpsSessionWithUserId().ConfigureAwait(false);

            // await connectClient.CreateSessionUsingDiscovery().ConfigureAwait(false);

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = rememberDecodeCustomDataTypes;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = rememberDecodeDataTypeDictionaries;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = readNodesWithTypeNotInHierarchy;
        }

        private async Task ConnectAndReconnectUsingSessionReconnectHandlerSample_ExecuteCommand(object sender, EventArgs e)
        {
            // ConnectClient sample does not need to load data type dictionaries or to decode custom data types
            bool rememberDecodeCustomDataTypes = m_application.ClientToolkitConfiguration.DecodeCustomDataTypes;
            bool rememberDecodeDataTypeDictionaries = m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries;
            bool readNodesWithTypeNotInHierarchy = m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy;

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = false;

            ConnectClient connectClient = new ConnectClient(m_application);

            await connectClient.ConnectAndReconnectUsingSessionReconnectHandler().ConfigureAwait(false);

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = rememberDecodeCustomDataTypes;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = rememberDecodeDataTypeDictionaries;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = readNodesWithTypeNotInHierarchy;
        }

        private async Task ReverseConnectSample_ExecuteCommand(object sender, EventArgs e)
        {
            //ConnectClient sample does not need to load data type dictionaries or to decode custom data types
            bool rememberDecodeCustomDataTypes = m_application.ClientToolkitConfiguration.DecodeCustomDataTypes;
            bool rememberDecodeDataTypeDictionaries = m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries;
            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = false;

            ReverseConnectClient reverseConnectClient = new ReverseConnectClient(m_application);
            //await reverseConnectClient.CreateOpcTcpSessionWithNoSecurity().ConfigureAwait(false);

            // get all endpoints and create sessions that will be connected asynchronously
            await reverseConnectClient.GetEndpointsAndReverseConnect(false).ConfigureAwait(false);

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = rememberDecodeCustomDataTypes;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = rememberDecodeDataTypeDictionaries;
        }

        private async Task SampleConnectECC(SecurityPolicy securityPolicy)
        {
            //ConnectClient sample does not need to load data type dictionaries or to decode custom data types
            bool rememberDecodeCustomDataTypes = m_application.ClientToolkitConfiguration.DecodeCustomDataTypes;
            bool rememberDecodeDataTypeDictionaries = m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries;
            bool readNodesWithTypeNotInHierarchy = m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy;

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = false;

            ConnectClient connectClient = new ConnectClient(m_application);

            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.Sign, securityPolicy).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, securityPolicy).ConfigureAwait(false);

            await connectClient.CreateOpcTcpSessionWithUserIdUsingECC(securityPolicy).ConfigureAwait(false);
            await connectClient.CreateOpcTcpSessionWithCertificateUsingECC(securityPolicy).ConfigureAwait(false);

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = rememberDecodeCustomDataTypes;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = rememberDecodeDataTypeDictionaries;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = readNodesWithTypeNotInHierarchy;
        }

        private async Task ConnectSample_ECC_NistP256_ExecuteCommand(object sender, EventArgs e)
        {
            await SampleConnectECC(SecurityPolicy.ECC_nistP256);
        }

        private async Task ConnectSample_ECC_NistP384_ExecuteCommand(object sender, EventArgs e)
        {
            await SampleConnectECC(SecurityPolicy.ECC_nistP384);
        }

        private async Task ConnectSample_ECC_BrainpoolP256r1_ExecuteCommand(object sender, EventArgs e)
        {
            await SampleConnectECC(SecurityPolicy.ECC_brainpoolP256r1);
        }

        private async Task ConnectSample_ECC_BrainpoolP384r1_ExecuteCommand(object sender, EventArgs e)
        {
            await SampleConnectECC(SecurityPolicy.ECC_brainpoolP384r1);
        }

        private async Task ReverseConnectReconnectUsingSessionReconnectHandler_ExecuteCommand(object sender, EventArgs e)
        {
            //ConnectClient sample does not need to load data type dictionaries or to decode custom data types
            bool rememberDecodeCustomDataTypes = m_application.ClientToolkitConfiguration.DecodeCustomDataTypes;
            bool rememberDecodeDataTypeDictionaries = m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries;
            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;
            m_application.ClientToolkitConfiguration.ReadNodesWithTypeNotInHierarchy = false;

            ReverseConnectClient reverseConnectClient = new ReverseConnectClient(m_application);

            await reverseConnectClient.ReverseConnectReconnectUsingSessionReconnectHandler().ConfigureAwait(false);

            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = rememberDecodeCustomDataTypes;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = rememberDecodeDataTypeDictionaries;
        }

        private async Task ReverseConnectSampleTimeoutInterval_ExecuteCommand(object sender, EventArgs e)
        {
            //ConnectClient sample does not need to lpad data type dictionaries or to decode custom data types
            bool rememberDecodeCustomDataTypes = m_application.ClientToolkitConfiguration.DecodeCustomDataTypes;
            bool rememberDecodeDataTypeDictionaries = m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries;
            m_application.ClientToolkitConfiguration.DecodeCustomDataTypes = false;
            m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries = false;

            ReverseConnectClient reverseConnectClient = new ReverseConnectClient(m_application);


            // get all endpoints and create sessions into a specified time interval
            await reverseConnectClient.GetEndpointsAndReverseConnectTimeoutInterval(false).ConfigureAwait(false);

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

        private Task DeleteEventMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.DeleteEventMonitoredItem();
            }
            return Task.CompletedTask;
        }

        private Task CreateEventMonitoredItem_ExecuteCommand(object sender, EventArgs e)
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

        #region  ExecuteCommand Handlers for Events without connect

        private async Task StartEventsClientWithoutConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient == null)
            {
                m_eventsClient = new EventsClient(m_application);
                await m_eventsClient.Initialize().ConfigureAwait(false);
            }
        }

        private Task CreateEventMonitoredItemWithoutConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.CreateEventMonitoredItemBeforeSubscriptionConnect();
            }
            return Task.CompletedTask;
        }

        private Task DeleteEventMonitoredItemWithoutConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.DeleteEventMonitoredItemCreatedBeforeSubscriptionConnect();
            }
            return Task.CompletedTask;
        }

        private async Task EndEventsWithoutConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                await m_eventsClient.Disconnect().ConfigureAwait(false);
                m_eventsClient = null;
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for Events without connect

        private async Task StartAddNewEventsClientAfterConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient == null)
            {
                m_eventsClient = new EventsClient(m_application);
                await m_eventsClient.Initialize().ConfigureAwait(false);
            }
        }

        private Task CreateAddNewEventMonitoredItemAfterConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.CreateEventMonitoredItemBeforeSubscriptionConnect();
                m_eventsClient.CreateNewEventMonitoredItemAfterSubscriptionConnect();
            }
            return Task.CompletedTask;
        }

        private Task DeleteAddNewEventMonitoredItemAfterConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.DeleteNewEventMonitoredItemCreatedAfterSubscriptionConnect();
                m_eventsClient.DeleteEventMonitoredItemCreatedBeforeSubscriptionConnect();
            }
            return Task.CompletedTask;
        }

        private async Task EndAddNewEventsAfterConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                await m_eventsClient.Disconnect().ConfigureAwait(false);
                m_eventsClient = null;
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for Double Filtering Events with connect

        private async Task StartDoubleFilteringEventsClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient == null)
            {
                m_eventsClient = new EventsClient(m_application);
                await m_eventsClient.Initialize().ConfigureAwait(false);
            }
        }

        private Task CreateDoubleFilteringEventMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.CreateDoubleFilteringEventMonitoredItem();
            }
            return Task.CompletedTask;
        }

        private Task DeleteDoubleFilteringEventMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.DeleteDoubleFilteringEventMonitoredItem();
            }
            return Task.CompletedTask;
        }

        private async Task EndDoubleFilteringEvents_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                await m_eventsClient.Disconnect().ConfigureAwait(false);
                m_eventsClient = null;
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

        private async Task HistoryReadAsyncProcessed_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                await m_historyClient.HistoryReadProcessedAsync().ConfigureAwait(false);
            }
        }

        private async Task HistoryReadAsyncAtTime_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                await m_historyClient.HistoryReadAtTimeAsync().ConfigureAwait(false);
            }
        }

        private async Task HistoryReadAsyncRaw_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                await m_historyClient.HistoryReadRawAsync().ConfigureAwait(false);
            }
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

        #region  ExecuteCommand Handlers for MonitoredItem with connect

        private async Task StartMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient == null)
            {
                m_monitoredItemClient = new MonitoredItemClient(m_application);
                await m_monitoredItemClient.Initialize().ConfigureAwait(false);
            }
        }

        private async Task CreateMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient == null)
            {
                m_monitoredItemClient = new MonitoredItemClient(m_application);
                await m_monitoredItemClient.Initialize().ConfigureAwait(false);
            }

            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.CreateMonitoredItems();
            }
        }

        private Task DeleteMonitoredItems_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.DeleteMonitoredItems();
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

        #region  ExecuteCommand TransferSubscriptions
        private Task TransferSubscriptions_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.TransferSubscription();
            }
            return Task.CompletedTask;

        }

        private async Task TransferSubscriptionsAsync_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                await m_monitoredItemClient.TransferSubscriptionAsync().ConfigureAwait(false);
            }
        }

        private async Task TransferSubscriptionSessionClosed_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                await m_monitoredItemClient.TransferSubscriptionSessionClosed().ConfigureAwait(false);
            }
        }

        private Task SaveSubscriptionsForTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.SaveSubscriptionsForTransfer();
            }
            return Task.CompletedTask;
        }

        private async Task LoadSubscriptionsForTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient == null)
            {
                m_monitoredItemClient = new MonitoredItemClient(m_application);
                await m_monitoredItemClient.Initialize(withSubscription: false).ConfigureAwait(false);
            }
            await m_monitoredItemClient.LoadSubscriptionsForTransfer().ConfigureAwait(false);
        }

        private async Task TransferSubscriptionsWithUserId_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                await m_monitoredItemClient.TransferSubscriptionsWithUserId().ConfigureAwait(false);
            }
        }

        private async Task TransferSubscriptionsWithCertificate_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                await m_monitoredItemClient.TransferSubscriptionsWithCertificate().ConfigureAwait(false);
            }
        }

        private async Task TransferSubscriptionsWithCertificatePassword_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                await m_monitoredItemClient.TransferSubscriptionsWithCertificatePassword().ConfigureAwait(false);
            }
        }

        private async Task TransferSubscriptionsWithSecurity_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                await m_monitoredItemClient.TransferSubscriptionsWithSecurity().ConfigureAwait(false);
            }
        }
        #endregion

        #region  ExecuteCommand Handlers for MonitoredItem without connect

        private async Task StartMonitoredItemsWithoutConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient == null)
            {
                m_monitoredItemClient = new MonitoredItemClient(m_application);
                await m_monitoredItemClient.Initialize().ConfigureAwait(false);
            }
        }

        private Task CreateMonitoredItemWithoutConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.CreateMonitoredItemsBeforeSubscriptionConnect();
            }
            return Task.CompletedTask;
        }

        private Task DeleteMonitoredItemsWithoutConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.DeleteMonitoredItemsCreatedBeforeSubscriptionConnect();
            }
            return Task.CompletedTask;
        }

        private async Task EndMonitoredItemsWithoutConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                await m_monitoredItemClient.Disconnect().ConfigureAwait(false);
                m_monitoredItemClient = null;
            }
        }
        #endregion

        #region  ExecuteCommand Handlers for MonitoredItem add new after connect

        private async Task StartAddNewMonitoredItemsAfterConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient == null)
            {
                m_monitoredItemClient = new MonitoredItemClient(m_application);
                await m_monitoredItemClient.Initialize().ConfigureAwait(false);
            }
        }

        private Task CreateAddNewMonitoredItemsAfterConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.CreateMonitoredItemsBeforeSubscriptionConnect();
                m_monitoredItemClient.CreateNewMonitoredItemsAfterSubscriptionConnect();
            }
            return Task.CompletedTask;
        }

        private Task DeleteAddNewMonitoredItemsAfterConnect_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.DeleteNewMonitoredItemsCreatedAfterSubscriptionConnect();
                m_monitoredItemClient.DeleteMonitoredItemsCreatedBeforeSubscriptionConnect();

            }
            return Task.CompletedTask;
        }

        private async Task EndAddNewMonitoredItemsAfterConnect_ExecuteCommand(object sender, EventArgs e)
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

            methodCallClient.Call();

            //wait and close session
            Task.Delay(1000).Wait();

            await methodCallClient.DisconnectSession().ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronous call methods on server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task CallMethodsAsync_ExecuteCommand(object sender, EventArgs e)
        {
            //initialize method call sample
            MethodCallClient methodCallClient = new MethodCallClient(m_application);

            //initialize session
            await methodCallClient.InitializeSession().ConfigureAwait(false);

            await methodCallClient.CallAsync().ConfigureAwait(false);

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

        private async Task ReadAsync_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                await m_readWriteClient.ReadAsync().ConfigureAwait(false);
            }
        }

        private Task RegisterNodes_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                m_readWriteClient.RegisterNodesSample();
            }
            return Task.CompletedTask;
        }

        private async Task RegisterNodesAsync_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                await m_readWriteClient.RegisterNodesSampleAsync().ConfigureAwait(false);
            }
        }

        private async Task StartReadWrite_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient == null)
            {
                m_readWriteClient = new ReadWriteClient(m_application);
                await m_readWriteClient.InitializeSession().ConfigureAwait(false);
            }
        }

        private async Task WriteAsync_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                await m_readWriteClient.WriteAsync().ConfigureAwait(false);
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
