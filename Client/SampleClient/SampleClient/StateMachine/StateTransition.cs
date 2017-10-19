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

namespace SampleClient.StateMachine
{
    /// <summary>
    /// StateTransition is a pair of a ProcessState and a possible command for it
    /// </summary>
    class StateTransition
    {
        public readonly State CurrentState;
        public readonly CommandDescriptor CommandDescriptor;
        public event EventHandler ExecuteCommand;

        /// <summary>
        /// Create new instance of StateTransition
        /// </summary>
        /// <param name="currentState"></param>
        /// <param name="command"></param>
        /// <param name="keyword"></param>
        /// <param name="description"></param>
        public StateTransition(State currentState, Command command, string keyword = "", string description = "")
        {
            CurrentState = currentState;
            CommandDescriptor = new CommandDescriptor(command, keyword, description);
        }


        /// <summary>
        /// Execute the command
        /// </summary>
        public virtual void OnExecuteCommand()
        {
            ExecuteCommand?.Invoke(this, EventArgs.Empty);
        }


        #region Equals & GetHashCode
        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return 17 + 31 * CurrentState.GetHashCode() + 31 * CommandDescriptor.Command.GetHashCode();
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            StateTransition other = obj as StateTransition;
            return other != null && CurrentState == other.CurrentState && CommandDescriptor.Command == other.CommandDescriptor.Command;
        } 
        #endregion
    }
}
