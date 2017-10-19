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

namespace SampleClient.StateMachine
{
    /// <summary>
    /// Describes the command, provides a keyworkd and the description of current command
    /// </summary>
    public class CommandDescriptor
    {
        public readonly Command Command;
        public string Keyword;
        public string Description;

        /// <summary>
        /// Create new instance of CommandDescriptor
        /// </summary>
        /// <param name="command"></param>
        /// <param name="keyword"></param>
        /// <param name="description"></param>
        public CommandDescriptor(Command command, string keyword, string description)
        {
            Command = command;
            Keyword = keyword;
            Description = description;
        }
    }
}
