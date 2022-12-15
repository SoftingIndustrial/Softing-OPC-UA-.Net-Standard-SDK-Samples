/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using Opc.Ua;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an <see cref="AcknowledgeableConditionState"/> attached.
    /// </summary>
    class AcknowledgeableConditionMonitor : BaseAlarmMonitor<AcknowledgeableConditionState>
    {
        #region Private Members

        private double? m_value = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of <see cref="AcknowledgeableConditionMonitor"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="alarmsNodeManager"></param>        
        public AcknowledgeableConditionMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue,
            AlarmsNodeManager alarmsNodeManager)
             : base(context, parent, namespaceIndex, name, initialValue, alarmsNodeManager)
        {
            // Attach the alarm monitor.
            InitializeAlarmMonitor(
                context,
                parent,
                namespaceIndex,
                alarmName);

            // Create ResetAcknowledge method
            MethodState resetAcknowledgeMethod = alarmsNodeManager.CreateMethod(this, "ResetAcknowledge");
            resetAcknowledgeMethod.OnCallMethod = ResetAcknowledgeCall;

            // Create ResetConfirm method
            MethodState resetConfirmMethod = alarmsNodeManager.CreateMethod(this, "ResetConfirm");
            resetConfirmMethod.OnCallMethod = ResetConfirmCall;
        }

        #endregion

        #region Base Class Overrides
        /// <summary>
        /// Hendle the Variable value change
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        protected override void ProcessVariableChanged(ISystemContext context, object value)
        {
            try
            {
                string currentUserId = string.Empty;
                IOperationContext operationContext = context as IOperationContext;

                if (operationContext != null && operationContext.UserIdentity != null)
                {
                    currentUserId = operationContext.UserIdentity.DisplayName;
                }

                double? newValue = Convert.ToDouble(value);

                bool updateRequired = false;

                if (m_value != newValue)
                {
                    m_value = newValue;
                    updateRequired = true;
                }

                if (updateRequired)
                {
                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm AckedState = {0}, ConfirmedState = {1}",
                        m_alarm.AckedState?.Value, m_alarm.ConfirmedState?.Value));
                    m_alarm.SetSeverity(context, EventSeverity.Medium);

                    base.ProcessVariableChanged(context, value);

                }
            }
            catch (Exception exception)
            {
                Utils.Trace(exception, "Alarms.AcknowledgeableConditionMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
            }
        }

        #endregion 

        #region Private Methods
        /// <summary>
        /// Initialize the alarm monitor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="alarmName"></param>
        protected override void InitializeAlarmMonitor(
           ISystemContext context,
           NodeState parent,
           ushort namespaceIndex,
           string alarmName)
        {
            // Create the alarm object
            m_alarm = new AcknowledgeableConditionState(this);

            base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName);

            // Mandatory fields
            m_alarm.SetAcknowledgedState(context, true);

            // Optional fields
            m_alarm.SetConfirmedState(context, true);

            m_alarm.Retain.Value = false;

            #region disable unused properties

            #endregion
        }

        #endregion

        #region Methods Callbacks

        /// <summary>
        /// Reset alarm Acknowledge flag
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult ResetAcknowledgeCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_alarm.SetAcknowledgedState(context, false);

            return new ServiceResult(StatusCodes.Good);
        }

        /// <summary>
        /// Reset alarm Confirm flag
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult ResetConfirmCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_alarm.SetConfirmedState(context, false);

            return new ServiceResult(StatusCodes.Good);
        }

        #endregion

    }
}
