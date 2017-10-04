using System;
using Opc.Ua;

namespace TestServer.EventingModule
{
	class ScheduledEventAction<T> : ScheduledAction where T : BaseEventState
	{
		BaseObjectState m_object;
		SystemContext m_context;
        BaseEventState m_eventToRaise;

		public ScheduledEventAction(BaseObjectState obj, SystemContext context) : base(1000, true)
		{
			m_object = obj;
			m_context = context;
		}

        public ScheduledEventAction(BaseObjectState obj, SystemContext context, BaseEventState eventToRaise) : base(1000, true)
        {
            m_object = obj;
            m_context = context;
            m_eventToRaise = eventToRaise;
        }

		public override void Execute()
		{
            if (m_eventToRaise == null)
            {
                m_eventToRaise = (T)Activator.CreateInstance(typeof(T), new object[] { (NodeState)null });
                m_eventToRaise.Initialize(m_context, null, EventSeverity.Medium, new LocalizedText("en", "Ana is going to pick some apples."));
            }

            m_object.ReportEvent(m_context, m_eventToRaise);
		}
	}
}