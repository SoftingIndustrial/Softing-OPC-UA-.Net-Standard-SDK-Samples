using System.Collections.Generic;

namespace TestServer
{
	class DynamicVariableChange : ScheduledAction
	{
		List<DynamicTestVariable> m_variables;

		public DynamicVariableChange() : base(1000, true)
		{
			m_variables = new List<DynamicTestVariable>();
		}

		public void Add(DynamicTestVariable variable)
		{
			m_variables.Add(variable);
		}

		public override void Execute()
		{
			foreach (DynamicTestVariable var in m_variables)
			{
				var.ChangeValue();
			}
		}
	}
}