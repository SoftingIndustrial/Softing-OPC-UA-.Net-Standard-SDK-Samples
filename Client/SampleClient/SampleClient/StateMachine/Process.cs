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
using SampleClient.Samples;
using Softing.Opc.Ua;

namespace SampleClient.StateMachine
{
    public class Process
    {
        private const string SessionNameBrowse = "BrowseClientSample Session";

        private readonly Dictionary<StateTransition, State> m_transitions;
        private readonly Dictionary<State, IList<CommandDescriptor>> m_processStateCommands;
        public State CurrentState { get; private set; }


        private readonly UaApplication m_application;
        private DiscoveryClient m_discoveryClientSample;
        private ConnectClient m_connectClientSample;
        private BrowseClient m_browseClientSample;
        private EventsClient m_eventsClient;
        private HistoryClient m_historyClient;
        /// <summary>
        /// create new instance of Process
        /// </summary>
        public Process(UaApplication application)
        {
            m_application = application;

            CurrentState = State.Main;
           
            m_transitions = new Dictionary<StateTransition, State>();

            InitializeDiscoveryTransitions();
            InitializeConnectTransitions();
            InitializeBrowseTransitions();
            InitializeEventsTransitions();

            //commands for history
            StateTransition startHistory = new StateTransition(State.Main, Command.StartHistory, "h", "Enter Read History Menu");
            startHistory.ExecuteCommand += StartHistory_ExecuteCommand;
            m_transitions.Add(startHistory, State.History);
            StateTransition historyReadRaw = new StateTransition(State.History, Command.HistoryReadRaw, "r", "History read raw");
            historyReadRaw.ExecuteCommand += HistoryReadRaw_ExecuteCommand;
            m_transitions.Add(historyReadRaw, State.History);
            StateTransition historyReadAtTime = new StateTransition(State.History, Command.HistoryReadAtTime, "t", "History read at time");
            historyReadAtTime.ExecuteCommand += HistoryReadAtTime_ExecuteCommand;
            m_transitions.Add(historyReadAtTime, State.History);
            StateTransition historyReadProcessed = new StateTransition(State.History, Command.HistoryReadProcessed, "p", "History read processed");
            historyReadProcessed.ExecuteCommand += HistoryReadProcessed_ExecuteCommand;
            m_transitions.Add(historyReadProcessed, State.History);
            StateTransition endHistory = new StateTransition(State.History, Command.EndHistory, "a", "Abandon Read History Menu");
            endHistory.ExecuteCommand += EndHistory_ExecuteCommand;
            m_transitions.Add(endHistory, State.Main);

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

        private void EndHistory_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_historyClient != null)
            {
                m_historyClient.DisconnectSession();
                DisplayListOfCommands();
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
                DisplayListOfCommands();
            }
        }

        private void InitializeEventsTransitions()
        {
            //commaands for events
            StateTransition startEventsClient = new StateTransition(State.Main, Command.StartEvents, "e", "Enter Events Menu");
            startEventsClient.ExecuteCommand += StartEventsClient_ExecuteCommand;
            m_transitions.Add(startEventsClient, State.Events);
            StateTransition createEventMonitorItem =
                new StateTransition(State.Events, Command.CreateEventMonitorItem, "e", "Create event monitored item");
            createEventMonitorItem.ExecuteCommand += CreateEventMonitorItem_ExecuteCommand;
            m_transitions.Add(createEventMonitorItem, State.Events);
            StateTransition createEventMonitorItemWithFilter =
                new StateTransition(State.Events, Command.CreateEventMonitorItemWithFilter, "f",
                    "Create and set event monitored item filter");
            createEventMonitorItemWithFilter.ExecuteCommand += CreateEventMonitorItemWithFilter_ExecuteCommand;
            m_transitions.Add(createEventMonitorItemWithFilter, State.Events);
            StateTransition endEvents = new StateTransition(State.Events, Command.EndEvents, "a", "Abandon Events Menu");
            endEvents.ExecuteCommand += EndEvents_ExecuteCommand;
            m_transitions.Add(endEvents, State.Main);
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
            StateTransition getEndpoints = new StateTransition(State.Discovery, Command.GetEndpoints, "e", "Find Endpooints for " + Constants.SampleServerUrlOpcTcp);
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

        #region  ExecuteCommand Handlers for Events
        private void EndEvents_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.DeleteEventMonitoredItem();
            }
        }

        private void CreateEventMonitorItemWithFilter_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_eventsClient != null)
            {
                m_eventsClient.ApplyEventMonitoredItemFilter();
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
                DisplayListOfCommands();
            }
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
                DisplayListOfCommands();
            }
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

        private void HttpsWithUserIdentity_ExecuteCommand(object sender, EventArgs e)
        {
           m_connectClientSample.CreateHttpsSessionWithUserId();
        }

        private void HttpsWithoutUserIdentity_ExecuteCommand(object sender, EventArgs e)
        {
            m_connectClientSample.CreateHttpsSessionWithAnomymousUserId();
        }

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

        /// <summary>
        /// Compute next state after the command is applied to current state
        /// </summary>
        /// <param name="command"></param>
        /// <param name="nextState"></param>
        /// <returns></returns>
        public bool TryGetNextState(Command command, out State nextState)
        {
            StateTransition transition = new StateTransition(CurrentState, command);
            if (!m_transitions.TryGetValue(transition, out nextState))
            {
                return false;
            }
            return true;
        }

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
        /// Execute provided command keywork and move to nexr state
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
                        Console.WriteLine("Executing command '{0}'...\r\n", commandDescriptor.Description);
                        //change current state before execution to have the right current state at execution time
                        CurrentState = m_transitions[stateTransitionToExecute];
                        stateTransitionToExecute.OnExecuteCommand();
                        return true;
                    }
                    return false;
                }
            }
            Console.WriteLine("Cannot find command '{0}'. Please choose from the list below:\r\n", commandKeyword);
            DisplayListOfCommands();
            return false;
        }

        /// <summary>
        /// Get the list of possible command descriptors for current state
        /// </summary>
        /// <returns></returns>
        public IList<CommandDescriptor> GetPossibleCommands()
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
                commandListText.AppendFormat("{0:2} - {1}\r\n", commandDescriptor.Keyword, commandDescriptor.Description);
            }
            Console.WriteLine(commandListText);
        }
    }
}
