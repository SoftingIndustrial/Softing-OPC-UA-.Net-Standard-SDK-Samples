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
using Opc.Ua;

namespace SampleServer.Alarms
{
    /// <summary>
    /// A monitored variable with an <see cref="DialogConditionState"/> attached.
    /// </summary>
    class DialogConditionMonitor : BaseAlarmMonitor<DialogConditionState>
    {
        #region Private Members

        double? m_value = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Create instance of <see cref="DialogConditionMonitor"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="name"></param>
        /// <param name="alarmName"></param>
        /// <param name="initialValue"></param>
        /// <param name="alarmsNodeManager"></param>
        public DialogConditionMonitor(
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

                    bool dialogState = newValue % 2 == 0;
                    m_alarm.DialogState.Value = new LocalizedText("en", dialogState ? ConditionStateNames.Active : ConditionStateNames.Inactive);
                    m_alarm.DialogState.TransitionTime.Value = DateTime.UtcNow;

                    // Not interested in disabled or inactive alarms
                    if (!m_alarm.EnabledState.Id.Value || !m_alarm.DialogState.Id.Value)
                    {
                        m_alarm.Retain.Value = false;
                    }
                    else
                    {
                        m_alarm.Retain.Value = true;
                    }

                    int selectedResponse = dialogState ? 0 : 1;
                    m_alarm.DefaultResponse.Value = selectedResponse;
                    m_alarm.LastResponse.Value = selectedResponse;

                    LocalizedText[] responseOptions = m_alarm.ResponseOptionSet.Value;

                    m_alarm.Message.Value = new LocalizedText("en-US", String.Format("Alarm DialogState = {0} - Response answer as {1}", 
                        m_alarm.DialogState?.Value, responseOptions[selectedResponse].Text));
                    m_alarm.SetSeverity(context, EventSeverity.Low);

                    base.ProcessVariableChanged(context, value);

                    //send dialog response - the DialogState is reset to Inactive
                    m_alarm.SetResponse(context, selectedResponse);
                }
            }
            catch (Exception exception)
            {
                Utils.Trace(exception, "Alarms.DialogConditionMonitor.ProcessVariableChanged: Unexpected error processing value changed notification.");
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
        /// <param name="initialValue"></param>
        protected override void InitializeAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string alarmName)
        {
            // Create the alarm object
            m_alarm = new DialogConditionState(this);

            base.InitializeAlarmMonitor(context, parent, namespaceIndex, alarmName);

            // Manadatory fields initialization
            m_alarm.DialogState.Value = new LocalizedText("en-US", ConditionStateNames.Inactive);

            // Manadatory properties initialization
            m_alarm.Prompt.Value = new LocalizedText("en-US", "Select option: Ok|Cancel");
            m_alarm.ResponseOptionSet.Value = new LocalizedText[] { new LocalizedText("en-US", "Ok"), new LocalizedText("en-US", "Cancel") };
            int response = 0;
            m_alarm.DefaultResponse.Value = response;
            m_alarm.LastResponse.Value = response;
            m_alarm.OkResponse.Value = response;
            m_alarm.CancelResponse.Value = 1;
            m_alarm.SetResponse(context, response);

            m_alarm.Retain.Value = false;

            #region disable unused properties

            #endregion

        }

        #endregion
    }
}
