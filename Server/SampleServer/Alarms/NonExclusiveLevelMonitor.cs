using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua;

namespace SampleServer.Alarms
{
    internal class NonExclusiveLevelMonitor : BaseAlarmMonitor
    {
        public NonExclusiveLevelMonitor(
            ISystemContext context,
            NodeState parent,
            ushort namespaceIndex,
            string name,
            string alarmName,
            double initialValue) 
            : base(context, parent, namespaceIndex, name, initialValue)
        {
            StateChanged += AlarmMonitor_StateChanged;
        }
    }
}
