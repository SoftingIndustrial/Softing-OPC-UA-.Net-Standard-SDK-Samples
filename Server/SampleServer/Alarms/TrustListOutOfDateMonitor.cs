/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using Opc.Ua;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an <see cref="TrustListOutOfDateAlarmState"/> attached.
    /// </summary>
    internal class TrustListOutOfDateMonitor : OffNormalAlarmMonitor
    {

        #region Constructors
        /// <summary>
        /// Create new instance of <see cref="TrustListOutOfDateMonitor"/>
        /// </summary>
        /// <param name="alarmsNodeManager"></param>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        public TrustListOutOfDateMonitor(
            AlarmsNodeManager alarmsNodeManager,
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue)
           : base(alarmsNodeManager, context, parent, namespaceIndex, name, alarmName, initialValue)
        {

            m_alarm.SetSuppressedState(context, false);

            TrustListOutOfDateAlarmState trustListOutOfDateAlarmState = m_alarm as TrustListOutOfDateAlarmState;
            if (trustListOutOfDateAlarmState != null)
            {
                // Set trust list out of date mandatory fields
                trustListOutOfDateAlarmState.TrustListId.Value = Variables.TrustListOutOfDateAlarmType_TrustListId;
                trustListOutOfDateAlarmState.LastUpdateTime.Value = DateTime.UtcNow;
                trustListOutOfDateAlarmState.UpdateFrequency.Value = 10000;
            }

        }
        #endregion

        #region Base Class Overrides
        /// <summary>
        /// Create and return new instance of <see cref="TrustListOutOfDateAlarmState"/> to be used by this monitor
        /// </summary>
        /// <returns></returns>
        protected override OffNormalAlarmState GetInstanceOfAlarmState()
        {
            return new TrustListOutOfDateAlarmState(this);
        }
        
        #endregion                     
    }
}
