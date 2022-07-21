﻿/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Opc.Ua;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an <see cref="SystemDiagnosticAlarmMonitor"/> attached.
    /// </summary>
    class SystemDiagnosticAlarmMonitor : OffNormalAlarmMonitor
    {
        #region Constructor
        /// <summary>
        /// Create new instance of <see cref="SystemDiagnosticAlarmMonitor"/>
        /// </summary>
        /// <param name="alarmsNodeManager"></param>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        public SystemDiagnosticAlarmMonitor(
            AlarmsNodeManager alarmsNodeManager,
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue)
             : base(alarmsNodeManager, context, parent, namespaceIndex, name, alarmName, initialValue)
        {
            
        }
        #endregion

        #region Base Class Overrides
        /// <summary>
        /// Create and return new instance of <see cref="SystemDiagnosticAlarmState"/> to be used by corrent monitor
        /// </summary>
        /// <returns></returns>
        protected override OffNormalAlarmState GetInstanceOfAlarmState()
        {
            return new SystemDiagnosticAlarmState(this);
        }
       
        #endregion        
    }
}
