/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
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
        private FileTransferClient m_fileTransferClient;

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
            //readwrite - 3
            InitializeReadWriteTransitions();
            //add monitored item / events menu - 4
            InitializeMonitoredItemEventsTransitions();
            //add alarms menu - 5
            InitializeAlarmsTransitions();
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
            exit = new StateTransition(State.DiscoveryConnect, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);           
            exit = new StateTransition(State.History, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);
            exit = new StateTransition(State.MonitoredItemEvents, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);
            exit = new StateTransition(State.Alarms, Command.Exit, "x", "Exit Client Application");
            exit.ExecuteCommand += Exit_ExecuteCommand;
            m_transitions.Add(exit, State.Exit);
            exit = new StateTransition(State.ReadWrite, Command.Exit, "x", "Exit Client Application");
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
        public void ExecuteCommand(string commandKeyword)
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
                        stateTransitionToExecute.OnExecuteCommand();

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
            StateTransition startDCClient = new StateTransition(State.Main, Command.DiscoveryConnect, "1", "Enter Discovery/Connect Menu");            
            m_transitions.Add(startDCClient, State.DiscoveryConnect);

            //add discovery menu item
            StateTransition discoverySample = new StateTransition(State.DiscoveryConnect, Command.DiscoverySample, "1", "Execute Discovery Sample");
            discoverySample.ExecuteCommand += DiscoverySample_ExecuteCommand;
            m_transitions.Add(discoverySample, State.DiscoveryConnect);
            //add connect menu item
            StateTransition connectSample = new StateTransition(State.DiscoveryConnect, Command.ConnectSample, "2", "Execute Connect Sample");
            connectSample.ExecuteCommand += ConnectSample_ExecuteCommand;
            m_transitions.Add(connectSample, State.DiscoveryConnect);

            StateTransition endDiscoveryConnect = new StateTransition(State.DiscoveryConnect, Command.EndDiscoveryConnect, "0", "Back to Main Menu");            
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
            StateTransition startReadWrite = new StateTransition(State.Main, Command.StartReadWrite, "3", "Enter Read/Write Menu");
            startReadWrite.ExecuteCommand += StartReadWrite_ExecuteCommand;
            m_transitions.Add(startReadWrite, State.ReadWrite);
            StateTransition read = new StateTransition(State.ReadWrite, Command.Read, "1", "Read Nodes");
            read.ExecuteCommand += Read_ExecuteCommand;
            m_transitions.Add(read, State.ReadWrite);
            StateTransition write = new StateTransition(State.ReadWrite, Command.Write, "2", "Write Nodes");
            write.ExecuteCommand += Write_ExecuteCommand;
            m_transitions.Add(write, State.ReadWrite);
            StateTransition endReadWrite = new StateTransition(State.ReadWrite, Command.EndReadWrite, "0", "Back to Main Menu");
            endReadWrite.ExecuteCommand += EndReadWrite_ExecuteCommand;
            m_transitions.Add(endReadWrite, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for MonitoredItem/ Events (4)
        /// </summary>
        private void InitializeMonitoredItemEventsTransitions()
        {
            //commands for monitored item
            StateTransition start = new StateTransition(State.Main, Command.StartMonitoredItemEvents, "4", "Enter MonitoredItem/Events Menu");
            m_transitions.Add(start, State.MonitoredItemEvents);

            //commands for monitored item
            StateTransition startMonitoredItem = new StateTransition(State.MonitoredItemEvents, Command.StartMonitoredItem, "1", "Enter MonitoredItem Menu");
            startMonitoredItem.ExecuteCommand += StartMonitoredItem_ExecuteCommand;
            m_transitions.Add(startMonitoredItem, State.MonitoredItem);
            StateTransition createMonitoredItem = new StateTransition(State.MonitoredItem, Command.CreateMonitoredItem, "1", "Create data change Monitored Items");
            createMonitoredItem.ExecuteCommand += CreateMonitoredItem_ExecuteCommand;
            m_transitions.Add(createMonitoredItem, State.MonitoredItem);
            StateTransition deleteMonitoredItem = new StateTransition(State.MonitoredItem, Command.DeleteMonitoredItem, "2", "Delete data change Monitored Items");
            deleteMonitoredItem.ExecuteCommand += DeleteMonitoredItem_ExecuteCommand;
            m_transitions.Add(deleteMonitoredItem, State.MonitoredItem);
            StateTransition endMonitoredItem = new StateTransition(State.MonitoredItem, Command.EndMonitoredItem, "0", "Back to MonitoredItem/Events Menu");
            endMonitoredItem.ExecuteCommand += EndMonitoredItem_ExecuteCommand;
            m_transitions.Add(endMonitoredItem, State.MonitoredItemEvents);

            //commands for events
            StateTransition startEventsClient = new StateTransition(State.MonitoredItemEvents, Command.StartEvents, "2", "Enter Events Menu");
            startEventsClient.ExecuteCommand += StartEventsClient_ExecuteCommand;
            m_transitions.Add(startEventsClient, State.Events);
            StateTransition createEventMonitorItem = new StateTransition(State.Events, Command.CreateEventMonitorItem, "1", "Create event Monitored Item");
            createEventMonitorItem.ExecuteCommand += CreateEventMonitorItem_ExecuteCommand;
            m_transitions.Add(createEventMonitorItem, State.Events);
            StateTransition deleteEventMonitorItem = new StateTransition(State.Events, Command.DeleteEventMonitorItem,
                "2",
                "Delete event Monitored Item");
            deleteEventMonitorItem.ExecuteCommand += DeleteEventMonitorItem_ExecuteCommand;
            m_transitions.Add(deleteEventMonitorItem, State.Events);
            StateTransition endEvents = new StateTransition(State.Events, Command.EndEvents, "0", "Back to MonitoredItem/Events Menu");
            endEvents.ExecuteCommand += EndEvents_ExecuteCommand;
            m_transitions.Add(endEvents, State.MonitoredItemEvents);

            StateTransition end = new StateTransition(State.MonitoredItemEvents, Command.EndMonitoredItemEvents, "0", "Back to Main Menu");
            m_transitions.Add(end, State.Main);
        }
        

        /// <summary>
        /// Initializes all sub menu transitions for Alarms (5)
        /// </summary>
        private void InitializeAlarmsTransitions()
        {
            //commands for alarms
            StateTransition startAlarms = new StateTransition(State.Main, Command.StartAlarms, "5", "Enter Alarms Menu");
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
            StateTransition endAlarms = new StateTransition(State.Alarms, Command.EndAlarms, "0", "Back to Main Menu");
            endAlarms.ExecuteCommand += EndAlarms_ExecuteCommand;
            m_transitions.Add(endAlarms, State.Main);
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
            StateTransition startPubSubMenu = new StateTransition(State.Main, Command.PubSubConfigMenu, "9", "Enter PubSub menu");
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

        private void EndAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.Disconnect();
                m_alarmsClient = null;
            }
        }

        private void AddCommentAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.AddCommentToAlarm();
            }
        }

        private void AcknowledgeAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.AcknowledgeAlarm();
            }
        }

        private void RefreshAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.ConditionRefresh();
            }
        }

        private void StartAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient == null)
            {
                m_alarmsClient = new AlarmsClient(m_application);
                m_alarmsClient.Initialize();
            }

        }

        #endregion

        #region  ExecuteCommand Handlers for Browse & Translate

        private void StartBrowseClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient == null)
            {
                m_browseClient = new BrowseClient(m_application);
                m_browseClient.InitializeSession();
            }
        }

        private void EndBrowseClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                m_browseClient.DisconnectSession();
                m_browseClient = null;
            }
        }

        private void Translate_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                //call translate single path
                m_browseClient.TranslateBrowsePathToNodeIds();
                //call translate multiple paths
                m_browseClient.TranslateBrowsePathsToNodeIds();
            }
        }

        private void BrowseServerWithOptions_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                m_browseClient.BrowseWithOptions();
            }
        }

        private void BrowseServer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                m_browseClient.BrowseTheServer();
            }
        }

        #endregion

        #region ExecuteCommand Handlers for Connect

        private void ConnectSample_ExecuteCommand(object sender, EventArgs e)
        {
            //ConnectClient sample creates its own UAApplication object with an ApplicationConfiguration object created 
            //programmatically and not loaded from config file
            ConnectClient connectClient = new ConnectClient();

            connectClient.CreateOpcTcpSessionWithNoSecurity();
            connectClient.CreateOpcTcpSessionWithSecurity( Opc.Ua.MessageSecurityMode.Sign, SecurityPolicy.Basic256Sha256);
            connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Basic256Sha256);
            connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.Sign, SecurityPolicy.Aes128_Sha256_RsaOaep);
            connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Aes128_Sha256_RsaOaep);
            connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.Sign, SecurityPolicy.Aes256_Sha256_RsaPss);
            connectClient.CreateOpcTcpSessionWithSecurity(Opc.Ua.MessageSecurityMode.SignAndEncrypt, SecurityPolicy.Aes256_Sha256_RsaPss);
            connectClient.CreateOpcTcpSessionWithUserId();
            //connectClient.CreateHttpsSessionWithAnomymousUserId();
            //connectClient.CreateHttpsSessionWithUserId();
            connectClient.CreateSessionUsingDiscovery();
        }

        #endregion

        #region ExecuteCommand Handlers for Discovery

        /// <summary>
        /// ExeuteCommand handler for Discovery - FindServers command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiscoverySample_ExecuteCommand(object sender, EventArgs e)
        {
            //initialize discovery sample
            DiscoveryClient discoveryClientSample = new DiscoveryClient(m_application);
            //call sample discovery methods
            discoveryClientSample.DiscoverServers();
            discoveryClientSample.DiscoverServersOnNetwork();
        }

        #endregion

        #region  ExecuteCommand Handlers for Events

        private void EndEvents_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.Disconnect();
                m_eventsClient = null;
            }
        }

        private void DeleteEventMonitorItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.DeleteEventMonitoredItem();
            }
        }

        private void CreateEventMonitorItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.CreateEventMonitoredItem();
            }
        }

        private void StartEventsClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient == null)
            {
                m_eventsClient = new EventsClient(m_application);
                m_eventsClient.Initialize();
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for History

        private void EndHistory_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                m_historyClient.DisconnectSession();
                m_historyClient = null;
            }
        }

        private void HistoryReadProcessed_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                m_historyClient.HistoryReadProcessed();
            }
        }

        private void HistoryReadAtTime_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                m_historyClient.HistoryReadAtTime();
            }
        }

        private void HistoryReadRaw_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                m_historyClient.HistoryReadRaw();
            }
        }

        private void StartHistory_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient == null)
            {
                m_historyClient = new HistoryClient(m_application);
                m_historyClient.InitializeSession();
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for MonitoredItem

        private void StartMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient == null)
            {
                m_monitoredItemClient = new MonitoredItemClient(m_application);
                m_monitoredItemClient.Initialize();
            }
        }

        private void CreateMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.CreateMonitoredItem();
            }
        }

        private void DeleteMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.DeleteMonitoredItem();
            }
        }

        private void EndMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.Disconnect();
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
        private void CallMethods_ExecuteCommand(object sender, EventArgs e)
        {
            //initialize method call sample
            MethodCallClient methodCallClient = new MethodCallClient(m_application);

            //initialize session
            methodCallClient.InitializeSession();
            //call method 
            methodCallClient.CallMethod();
            //call async method 
            methodCallClient.AsyncCallMethod();

            //wait and close session
            Task.Delay(1000).Wait();
            methodCallClient.DisconnectSession();
        }

        #endregion

        #region ExecuteCommand Handler for Read Write 

        private void EndReadWrite_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                m_readWriteClient.DisconnectSession();
                m_readWriteClient = null;
            }
        }

        private void Write_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient != null)
            {
                m_readWriteClient.WriteValueForNode();
                m_readWriteClient.WriteArrayValueForNode();
                m_readWriteClient.WriteComplexValueForNode();
                m_readWriteClient.WriteEnumValueForNode();
                m_readWriteClient.WriteMultipleNodesValues();
            }
        }

        private void Read_ExecuteCommand(object sender, EventArgs e)
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
            }
        }

        private void StartReadWrite_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_readWriteClient == null)
            {
                m_readWriteClient = new ReadWriteClient(m_application);
                m_readWriteClient.InitializeSession();
            }
        }

        #endregion

        #region  ExecuteCommand Handlers for FileTransfer

        private void StartFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient == null)
            {
                m_fileTransferClient = new FileTransferClient(m_application);
                m_fileTransferClient.Initialize();
            }
        }

        private void UploadFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.UploadFile();
            }
        }

        private void DownloadFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.DownloadFile();
            }
        }

        private void ReadByteArrayFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.ReadByteString();
            }
        }

        private void ReadTemporaryFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.DownloadTemporaryFile();
            }
        }

        private void WriteTemporaryFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.UploadTemporaryFile();
            }
        }

        private void EndFileTransfer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_fileTransferClient != null)
            {
                m_fileTransferClient.Disconnect();
                m_fileTransferClient = null;
            }
        }

        private void StartPubSubCfgMenu_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_pubSubClient == null)
            {
                m_pubSubClient = new PubSubClient(m_application);
                m_pubSubClient.Initialize();
            }
        }

        private void StartPubSubReadCfg_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_pubSubClient != null)
            {
                m_pubSubClient.PubSubReadCfg();
            }
        }

        private void ExitPubSubCfgMenu_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_pubSubClient != null)
            {
                m_pubSubClient.Disconnect();
                m_pubSubClient = null;
            }
        }
        

        #endregion

        #region ExecuteCommand Handler for Exit

        private void Exit_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClient != null)
            {
                m_browseClient.DisconnectSession();
            }
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.Disconnect();
            }
            if (m_alarmsClient != null)
            {
                m_alarmsClient.Disconnect();
            }
            if (m_eventsClient != null)
            {
                m_eventsClient.Disconnect();
            }
            if (m_historyClient != null)
            {
                m_historyClient.DisconnectSession();
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
