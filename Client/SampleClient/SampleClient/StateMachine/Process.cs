﻿/* ========================================================================
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
using System.Threading.Tasks;
using SampleClient.Samples;
using Softing.Opc.Ua;

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
        public State CurrentState { get; private set; }


        private readonly UaApplication m_application;
        private DiscoveryClient m_discoveryClientSample;
        private ConnectClient m_connectClientSample;
        private BrowseClient m_browseClientSample;
        private EventsClient m_eventsClient;
        private HistoryClient m_historyClient;
        private MethodCallClient m_methodCallClient;
        private MonitoredItemClient m_monitoredItemClient;
        private AlarmsClient m_alarmsClient;
        #endregion

        #region Constructor

        /// <summary>
        /// create new instance of Process
        /// </summary>
        public Process(UaApplication application)
        {
            m_application = application;
            CurrentState = State.Main;

            m_transitions = new Dictionary<StateTransition, State>();

            InitializeAlarmsTransitions();
            InitializeBrowseTransitions();
            InitializeConnectTransitions();
            InitializeDiscoveryTransitions();
            InitializeEventsTransitions();
            InitializeHistoryTransitions();
            InitializeMonitoredItemTransitions();

            StateTransition callMethods = new StateTransition(State.Main, Command.CallMethods, "m", "Call Methods on Server");
            callMethods.ExecuteCommand += CallMethods_ExecuteCommand;
            m_transitions.Add(callMethods, State.Main);

            //add here all exit commands
            StateTransition exit = new StateTransition(State.Main, Command.Exit, "x", "Exit Client Application");
            m_transitions.Add(exit, State.Terminated);
            exit = new StateTransition(State.Browse, Command.Exit, "x", "Exit Client Application");
            m_transitions.Add(exit, State.Terminated);
            exit = new StateTransition(State.Discovery, Command.Exit, "x", "Exit Client Application");
            m_transitions.Add(exit, State.Terminated);
            exit = new StateTransition(State.Connect, Command.Exit, "x", "Exit Client Application");
            m_transitions.Add(exit, State.Terminated);
            exit = new StateTransition(State.Events, Command.Exit, "x", "Exit Client Application");
            m_transitions.Add(exit, State.Terminated);
            exit = new StateTransition(State.History, Command.Exit, "x", "Exit Client Application");
            m_transitions.Add(exit, State.Terminated);
            exit = new StateTransition(State.MonitoredItem, Command.Exit, "x", "Exit Client Application");
            m_transitions.Add(exit, State.Terminated);
            exit = new StateTransition(State.Alarms, Command.Exit, "x", "Exit Client Application");
            m_transitions.Add(exit, State.Terminated);

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

        #region Public Method - ExecuteCommand
        /// <summary>
        /// Execute provided command keyword and move to next state
        /// </summary>
        /// <param name="commandKeyword"></param>
        /// <returns>true if command was executed</returns>
        public bool ExecuteCommand(string commandKeyword)
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
                        Console.WriteLine("\r\nExecuting command '{0}'...\r\n", commandDescriptor.Description);
                        //change current state before execution to have the right current state at execution time
                        CurrentState = m_transitions[stateTransitionToExecute];
                        stateTransitionToExecute.OnExecuteCommand();
                        return true;
                    }
                    return false;
                }
            }
            Console.WriteLine("\r\nCannot find command '{0}'. Please choose from the list below:\r\n", commandKeyword);
            DisplayListOfCommands();
            return false;
        }
        #endregion

        #region Initialize Transitions Methods
        /// <summary>
        /// Initializes all sub menu transitions for Alarms
        /// </summary>
        private void InitializeAlarmsTransitions()
        {
            //commands for alarms
            StateTransition startAlarms = new StateTransition(State.Main, Command.StartAlarms, "a", "Enter Alarms Menu");
            startAlarms.ExecuteCommand += StartAlarms_ExecuteCommand;
            m_transitions.Add(startAlarms, State.Alarms);
            StateTransition refreshAlarms =
                new StateTransition(State.Alarms, Command.RefreshAlarms, "r", "Refresh active alarms");
            refreshAlarms.ExecuteCommand += RefreshAlarms_ExecuteCommand;
            m_transitions.Add(refreshAlarms, State.Alarms);
            StateTransition acknowledgeAlarms =
                new StateTransition(State.Alarms, Command.AcknowledgeAlarms, "a", "Acknowledge alarm");
            acknowledgeAlarms.ExecuteCommand += AcknowledgeAlarms_ExecuteCommand;
            m_transitions.Add(acknowledgeAlarms, State.Alarms);
            StateTransition addCommentAllarms =
                new StateTransition(State.Alarms, Command.AddCommentAlarms, "c", "Add comment to alarm");
            addCommentAllarms.ExecuteCommand += AddCommentAlarms_ExecuteCommand;
            m_transitions.Add(addCommentAllarms, State.Alarms);
            StateTransition endAlarms = new StateTransition(State.Alarms, Command.EndAlarms, "b", "Back to Main Menu");
            endAlarms.ExecuteCommand += EndAlarms_ExecuteCommand;
            m_transitions.Add(endAlarms, State.Main);
        }
        
        /// <summary>
        /// Initializes all sub menu transitions for BrowseClient
        /// </summary>
        private void InitializeBrowseTransitions()
        {
            //commAands for browse
            StateTransition startBrowseClient = new StateTransition(State.Main, Command.StartBrowse, "b", "Enter Browse Menu");
            startBrowseClient.ExecuteCommand += StartBrowseClient_ExecuteCommand;
            m_transitions.Add(startBrowseClient, State.Browse);
            StateTransition browseServer = new StateTransition(State.Browse, Command.BrowseServer, "s", "Browse server");
            browseServer.ExecuteCommand += BrowseServer_ExecuteCommand;
            m_transitions.Add(browseServer, State.Browse);
            StateTransition browseServerWithOptions =
                new StateTransition(State.Browse, Command.BrowseServerWithOptions, "o", "Browse server with options");
            browseServerWithOptions.ExecuteCommand += BrowseServerWithOptions_ExecuteCommand;
            m_transitions.Add(browseServerWithOptions, State.Browse);
            StateTransition translate =
                new StateTransition(State.Browse, Command.Translate, "t", "Translate BrowsePaths to NodeIds");
            translate.ExecuteCommand += Translate_ExecuteCommand;
            m_transitions.Add(translate, State.Browse);
            StateTransition translateMultiple =
                new StateTransition(State.Browse, Command.TranslateMultiple, "m", "Translate multiple Browse Paths");
            translateMultiple.ExecuteCommand += TranslateMultiple_ExecuteCommand;
            m_transitions.Add(translateMultiple, State.Browse);
            StateTransition endBrowseClient = new StateTransition(State.Browse, Command.EndBrowse, "b", "Back to Main Menu");
            endBrowseClient.ExecuteCommand += EndBrowseClient_ExecuteCommand;
            m_transitions.Add(endBrowseClient, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for ConnectClient
        /// </summary>
        private void InitializeConnectTransitions()
        {
            //commands for connect
            StateTransition startConnectClient =
                new StateTransition(State.Main, Command.StartConnectClient, "c", "Enter Connect Menu");
            startConnectClient.ExecuteCommand += StartConnectClient_ExecuteCommand;
            m_transitions.Add(startConnectClient, State.Connect);
            StateTransition opcTcpWithoutSecurity = new StateTransition(State.Connect, Command.OpcTcpWithoutSecurity, "1",
                "Create opc.tcp session without security");
            opcTcpWithoutSecurity.ExecuteCommand += OpcTcpWithoutSecurity_ExecuteCommand;
            m_transitions.Add(opcTcpWithoutSecurity, State.Connect);
            StateTransition opcTcpUserIdentity = new StateTransition(State.Connect, Command.OpcTcpUserIdentity, "2",
                "Create opc.tcp session with user identity");
            opcTcpUserIdentity.ExecuteCommand += OpcTcpUserIdentity_ExecuteCommand;
            m_transitions.Add(opcTcpUserIdentity, State.Connect);
            StateTransition opcTcpUserIdentityAndSecurity = new StateTransition(State.Connect,
                Command.OpcTcpUserIdentityAndSecurity, "3", "Create opc.tcp session with security and user identity");
            opcTcpUserIdentityAndSecurity.ExecuteCommand += OpcTcpUserIdentityAndSecurity_ExecuteCommand;
            m_transitions.Add(opcTcpUserIdentityAndSecurity, State.Connect);
            //StateTransition httpsWithoutUserIdentity = new StateTransition(State.Connect, Command.HttpsWithoutUserIdentity, "4",
            //    "Create https session without user identity");
            //httpsWithoutUserIdentity.ExecuteCommand += HttpsWithoutUserIdentity_ExecuteCommand;
            //m_transitions.Add(httpsWithoutUserIdentity, State.Connect);
            //StateTransition httpsWithUserIdentity = new StateTransition(State.Connect, Command.HttpsWithUserIdentity, "5",
            //    "Create https session with user identity");
            //httpsWithUserIdentity.ExecuteCommand += HttpsWithUserIdentity_ExecuteCommand;
            //m_transitions.Add(httpsWithUserIdentity, State.Connect);
            StateTransition sessionWithDiscovery = new StateTransition(State.Connect, Command.SessionWithDiscovery, "4",
                "Create session using discovery process");
            sessionWithDiscovery.ExecuteCommand += SessionWithDiscovery_ExecuteCommand;
            m_transitions.Add(sessionWithDiscovery, State.Connect);
            StateTransition endConnectClient =
                new StateTransition(State.Connect, Command.EndConnectClient, "b", "Back to Main Menu");
            endConnectClient.ExecuteCommand += EndConnectClient_ExecuteCommand;
            m_transitions.Add(endConnectClient, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for DiscoveryClient
        /// </summary>
        private void InitializeDiscoveryTransitions()
        {
            //Commands for discovery
            StateTransition startDiscoveryClient =
                new StateTransition(State.Main, Command.StartDiscoveryClient, "d", "Enter Discovery Menu");
            startDiscoveryClient.ExecuteCommand += StartDiscoveryClient_ExecuteCommand;
            m_transitions.Add(startDiscoveryClient, State.Discovery);
            StateTransition getEndpoints = new StateTransition(State.Discovery, Command.GetEndpoints, "e", "Find Endpoints for " + Constants.SampleServerUrlOpcTcp);
            getEndpoints.ExecuteCommand += GetEndpoints_ExecuteCommand;
            m_transitions.Add(getEndpoints, State.Discovery);
            StateTransition findServers = new StateTransition(State.Discovery, Command.FindServers, "s", "Find Servers");
            findServers.ExecuteCommand += FindServers_ExecuteCommand;
            m_transitions.Add(findServers, State.Discovery);
            StateTransition endDiscoveryClient =
                new StateTransition(State.Discovery, Command.EndDiscoveryClient, "b", "Back to Main Menu");
            endDiscoveryClient.ExecuteCommand += EndDiscoveryClient_ExecuteCommand;
            m_transitions.Add(endDiscoveryClient, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for Events
        /// </summary>
        private void InitializeEventsTransitions()
        {
            //commands for events
            StateTransition startEventsClient = new StateTransition(State.Main, Command.StartEvents, "e", "Enter Events Menu");
            startEventsClient.ExecuteCommand += StartEventsClient_ExecuteCommand;
            m_transitions.Add(startEventsClient, State.Events);
            StateTransition createEventMonitorItem =
                new StateTransition(State.Events, Command.CreateEventMonitorItem, "c", "Create event monitored item");
            createEventMonitorItem.ExecuteCommand += CreateEventMonitorItem_ExecuteCommand;
            m_transitions.Add(createEventMonitorItem, State.Events);
            StateTransition deleteEventMonitorItem = new StateTransition(State.Events, Command.DeleteEventMonitorItem, "d",
                "Delete event monitored item");
            deleteEventMonitorItem.ExecuteCommand += DeleteEventMonitorItem_ExecuteCommand;
            m_transitions.Add(deleteEventMonitorItem, State.Events);
            StateTransition endEvents = new StateTransition(State.Events, Command.EndEvents, "b", "Back to Main Menu");
            endEvents.ExecuteCommand += EndEvents_ExecuteCommand;
            m_transitions.Add(endEvents, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for History
        /// </summary>
        private void InitializeHistoryTransitions()
        {
            //commands for history
            StateTransition startHistory =
                new StateTransition(State.Main, Command.StartHistory, "h", "Enter Read History Menu");
            startHistory.ExecuteCommand += StartHistory_ExecuteCommand;
            m_transitions.Add(startHistory, State.History);
            StateTransition historyReadRaw =
                new StateTransition(State.History, Command.HistoryReadRaw, "r", "History read raw");
            historyReadRaw.ExecuteCommand += HistoryReadRaw_ExecuteCommand;
            m_transitions.Add(historyReadRaw, State.History);
            StateTransition historyReadAtTime =
                new StateTransition(State.History, Command.HistoryReadAtTime, "t", "History read at time");
            historyReadAtTime.ExecuteCommand += HistoryReadAtTime_ExecuteCommand;
            m_transitions.Add(historyReadAtTime, State.History);
            StateTransition historyReadProcessed =
                new StateTransition(State.History, Command.HistoryReadProcessed, "p", "History read processed");
            historyReadProcessed.ExecuteCommand += HistoryReadProcessed_ExecuteCommand;
            m_transitions.Add(historyReadProcessed, State.History);
            StateTransition endHistory =
                new StateTransition(State.History, Command.EndHistory, "b", "Back to Main Menu");
            endHistory.ExecuteCommand += EndHistory_ExecuteCommand;
            m_transitions.Add(endHistory, State.Main);
        }

        /// <summary>
        /// Initializes all sub menu transitions for MonitoredItem
        /// </summary>
        private void InitializeMonitoredItemTransitions()
        {
            //commands for monitored item
            StateTransition startMonitoredItem =
                new StateTransition(State.Main, Command.StartMonitoredItem, "i", "Enter MonitoredItem Menu");
            startMonitoredItem.ExecuteCommand += StartMonitoredItem_ExecuteCommand;
            m_transitions.Add(startMonitoredItem, State.MonitoredItem);
            StateTransition createMonitoredItem =
                new StateTransition(State.MonitoredItem, Command.CreateMonitoredItem, "c", "Create new MonitoredItem");
            createMonitoredItem.ExecuteCommand += CreateMonitoredItem_ExecuteCommand;
            m_transitions.Add(createMonitoredItem, State.MonitoredItem);
            StateTransition deleteMonitoredItem =
                new StateTransition(State.MonitoredItem, Command.DeleteMonitoredItem, "d", "Delete last MonitoredItem");
            deleteMonitoredItem.ExecuteCommand += DeleteMonitoredItem_ExecuteCommand;
            m_transitions.Add(deleteMonitoredItem, State.MonitoredItem);
            StateTransition endMonitoredItem =
                new StateTransition(State.MonitoredItem, Command.EndMonitoredItem, "b", "Back to Main Menu");
            endMonitoredItem.ExecuteCommand += EndMonitoredItem_ExecuteCommand;
            m_transitions.Add(endMonitoredItem, State.Main);
        }
        #endregion

        #region ExecuteCommand Handler for Alarms
        private void EndAlarms_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_alarmsClient != null)
            {
                m_alarmsClient.DisconnectSession();
            }
            DisplayListOfCommands();
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
            }
            m_alarmsClient.Initialize();
            DisplayListOfCommands();
        }
        #endregion

        #region  ExecuteCommand Handlers for Browse & Translate
        private void EndBrowseClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClientSample != null)
            {
                m_browseClientSample.DisconnectSession();
            }
            DisplayListOfCommands();
        }

        private void TranslateMultiple_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClientSample != null)
            {
                m_browseClientSample.TranslateBrowsePathsToNodeIds();
            }
        }

        private void Translate_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClientSample != null)
            {
                m_browseClientSample.TranslateBrowsePathToNodeIds();
            }
        }

        private void BrowseServerWithOptions_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClientSample != null)
            {
                m_browseClientSample.BrowseWithOptions();
            }
        }

        private void BrowseServer_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClientSample != null)
            {
                m_browseClientSample.BrowseTheServer();
            }
        }

        private void StartBrowseClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_browseClientSample == null)
            {
                m_browseClientSample = new BrowseClient(m_application);
                m_browseClientSample.InitializeSession();
            }
            DisplayListOfCommands();
        }
        #endregion

        #region ExecuteCommand Handlers for Connect
        private void EndConnectClient_ExecuteCommand(object sender, EventArgs e)
        {
            DisplayListOfCommands();
        }

        private void StartConnectClient_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_connectClientSample == null)
            {
                m_connectClientSample = new ConnectClient(m_application);
            }
            DisplayListOfCommands();
        }
        private void SessionWithDiscovery_ExecuteCommand(object sender, EventArgs e)
        {
            m_connectClientSample.CreateSessionUsingDiscovery();
        }

        //private void HttpsWithUserIdentity_ExecuteCommand(object sender, EventArgs e)
        //{
        //   m_connectClientSample.CreateHttpsSessionWithUserId();
        //}

        //private void HttpsWithoutUserIdentity_ExecuteCommand(object sender, EventArgs e)
        //{
        //    m_connectClientSample.CreateHttpsSessionWithAnomymousUserId();
        //}

        private void OpcTcpUserIdentityAndSecurity_ExecuteCommand(object sender, EventArgs e)
        {
            m_connectClientSample.CreateOpcTcpSessionWithSecurity();
        }

        private void OpcTcpUserIdentity_ExecuteCommand(object sender, EventArgs e)
        {
            m_connectClientSample.CreateOpcTcpSessionWithUserId();
        }

        private void OpcTcpWithoutSecurity_ExecuteCommand(object sender, EventArgs e)
        {
            m_connectClientSample.CreateOpcTcpSessionWithNoSecurity();
        }
        #endregion

        #region ExecuteCommand Handlers for Discovery
        /// <summary>
        /// ExeuteCommand handler for StartDiscoveryClient command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void StartDiscoveryClient_ExecuteCommand(object sender, EventArgs eventArgs)
        {
            if (m_discoveryClientSample == null)
            {
                m_discoveryClientSample = new DiscoveryClient(m_application.Configuration);
            }
            DisplayListOfCommands();
        }

        /// <summary>
        /// ExeuteCommand handler for EndDiscoveryClient
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EndDiscoveryClient_ExecuteCommand(object sender, EventArgs e)
        {
            DisplayListOfCommands();
        }

        /// <summary>
        /// ExeuteCommand handler for Discovery - FindServers command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FindServers_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_discoveryClientSample != null)
            {
                m_discoveryClientSample.DiscoverServers(Constants.ServerDiscoveryUrl);
            }
        }

        /// <summary>
        /// ExeuteCommand handler for Discovery - GetEndpoints command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetEndpoints_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_discoveryClientSample != null)
            {
                m_discoveryClientSample.GetEndpoints(Constants.SampleServerUrlOpcTcp);
            }

        }
        #endregion

        #region  ExecuteCommand Handlers for Events
        private void EndEvents_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.DeleteEventMonitoredItem();
            }
            DisplayListOfCommands();
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
            }
            DisplayListOfCommands();
        }
        #endregion

        #region  ExecuteCommand Handlers for History
        private void EndHistory_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                m_historyClient.DisconnectSession();
            }
            DisplayListOfCommands();
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
            }
            DisplayListOfCommands();
        }
        #endregion

        #region  ExecuteCommand Handlers for MonitoredItem
        private void EndMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.DisconnectSession();
            }
            DisplayListOfCommands();
        }

        private void DeleteMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.DeleteMonitoredItem();
            }
        }

        private void CreateMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient != null)
            {
                m_monitoredItemClient.CreateMonitoredItem();
            }
        }

        private void StartMonitoredItem_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_monitoredItemClient == null)
            {
                m_monitoredItemClient = new MonitoredItemClient(m_application);
            }
            DisplayListOfCommands();
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
            if (m_methodCallClient == null)
            {
                m_methodCallClient = new MethodCallClient(m_application);
            }

            m_methodCallClient.InitializeSession();
            //call method 
            m_methodCallClient.CallMethod();
            //call async method 
            m_methodCallClient.AsyncCallMethod();

            //wait and close session
            Task.Delay(1000).Wait();
            m_methodCallClient.DisconnectSession();

            DisplayListOfCommands();
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
            commandListText.AppendFormat("{0} Menu:\r\n", CurrentState);

            foreach (var commandDescriptor in commandDescriptors)
            {
                commandListText.AppendFormat("{0} - {1}\r\n", commandDescriptor.Keyword, commandDescriptor.Description);
            }
            Console.WriteLine(commandListText);
        } 
        #endregion
    }
}
