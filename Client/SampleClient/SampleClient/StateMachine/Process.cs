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
using System.Runtime.CompilerServices;
using System.Text;
using Softing.Opc.Ua;

namespace SampleClient.StateMachine
{
    public class Process
    {
        private readonly Dictionary<StateTransition, State> m_transitions;
        private readonly Dictionary<State, IList<CommandDescriptor>> m_processStateCommands;
        public State CurrentState { get; private set; }


        private readonly UaApplication m_application;
        private DiscoveryClient m_discoveryClient;


        /// <summary>
        /// create new instance of Process
        /// </summary>
        public Process(UaApplication application)
        {
            m_application = application;

            CurrentState = State.Main;
            m_transitions = new Dictionary<StateTransition, State>();
            StateTransition startDiscoveryClient = new StateTransition(State.Main, Command.StartDiscoveryClient, "d", "Enter Discovery Mode");
            startDiscoveryClient.ExecuteCommand += StartDiscoveryClient_ExecuteCommand;
            m_transitions.Add(startDiscoveryClient, State.Discovery);
            StateTransition getEndpoints = new StateTransition(State.Discovery, Command.GetEndpoints, "e", "Get Endpooints");
            getEndpoints.ExecuteCommand += GetEndpoints_ExecuteCommand;
            m_transitions.Add(getEndpoints, State.Discovery);
            StateTransition findServers = new StateTransition(State.Discovery, Command.FindServers, "f", "Find Servers");
            findServers.ExecuteCommand += FindServers_ExecuteCommand;
            m_transitions.Add(findServers, State.Discovery);
            StateTransition endDiscoveryClient = new StateTransition(State.Discovery, Command.EndDiscoveryClient, "a", "Abandon Discovery Mode");
            endDiscoveryClient.ExecuteCommand += EndDiscoveryClient_ExecuteCommand;
            m_transitions.Add(endDiscoveryClient, State.Main);


            //add here all exit commands
            StateTransition exit = new StateTransition(State.Discovery, Command.Exit, "x", "Exit Client Application");
            m_transitions.Add(exit, State.Terminated);
            exit = new StateTransition(State.Main, Command.Exit, "x", "Exit Client Application");
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
        }

        private void EndDiscoveryClient_ExecuteCommand(object sender, EventArgs e)
        {
            DisplayListOfCommands();
        }

        private void FindServers_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_discoveryClient != null)
            {
                m_discoveryClient.GetEndpoints(Constants.ServerDiscoveryUrl);
            }
        }

        /// <summary>
        /// ExeuteCommand handler for GetEndpoints command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetEndpoints_ExecuteCommand(object sender, EventArgs e)
        {
            if (m_discoveryClient != null)
            {
                m_discoveryClient.GetEndpoints(Constants.SampleServerUrl);
            }
           
        }

        /// <summary>
        /// ExeuteCommand handler for StartDiscoveryClient command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void StartDiscoveryClient_ExecuteCommand(object sender, EventArgs eventArgs)
        {
            if (m_discoveryClient == null)
            {
                m_discoveryClient = new DiscoveryClient(m_application.Configuration);
            }
            DisplayListOfCommands();
        }

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
        public void DisplayListOfCommands()
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
