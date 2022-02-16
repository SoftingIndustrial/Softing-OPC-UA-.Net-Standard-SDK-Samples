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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua;

namespace SampleServer.Alarms
{
    internal abstract class BaseAlarmMonitor : BaseDataVariableState<double>
    {
        #region Private Members
        protected List<ConditionState> m_conditions;
        #endregion

        #region Properties
        public List<ConditionState> ConditionStates
        {
            get { return m_conditions; }
        }
        #endregion

        public BaseAlarmMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            double initialValue) : base(parent)
        {
            Create(context, null, new QualifiedName(name, namespaceIndex), null, true);

            ReferenceTypeId = ReferenceTypeIds.HasComponent;
            Value = initialValue;
            StatusCode = StatusCodes.Good;
            Timestamp = DateTime.UtcNow;

            if (parent != null)
            {
                parent.AddChild(this);

                // Define event source.
                parent.AddNotifier(context, ReferenceTypeIds.HasEventSource, false, this);
                AddNotifier(context, ReferenceTypeIds.HasEventSource, true, parent);
            }
        }

        protected void AddCondition(ConditionState condition)
        {
            if (condition == null)
            {
                return;
            }

            if (m_conditions == null)
            {
                m_conditions = new List<ConditionState>();
            }

            bool foundCondition = false;
            for (int i = 0; i < m_conditions.Count; i++)
            {
                if (m_conditions[i].NodeId == condition.NodeId)
                {
                    foundCondition = true;
                    break;
                }
            }

            if (condition.SourceNode != null)
            {
                condition.SourceNode.Value = NodeId;
            }

            if (DisplayName != null && condition.SourceName != null)
            {
                condition.SourceName.Value = DisplayName.Text;
            }

            if (!foundCondition)
            {
                m_conditions.Add(condition);

                AddReference(ReferenceTypeIds.HasCondition, false, condition.NodeId);
                condition.AddReference(ReferenceTypeIds.HasCondition, true, NodeId);
            }
        }

        //protected virtual void InitializeAlarmMonitor(
        //    ISystemContext context,
        //    ushort namespaceIndex,
        //    string alarmName,
        //    double initialValue)
        //{
        //}

        //protected virtual void ProcessVariableChanged(ISystemContext context, object value)
        //{
        //}

    }
}
