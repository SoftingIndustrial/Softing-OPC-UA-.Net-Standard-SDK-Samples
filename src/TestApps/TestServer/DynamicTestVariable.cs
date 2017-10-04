using System;
using System.Collections;
using Opc.Ua;
using Opc.Ua.Server;

namespace TestServer
{
	class DynamicTestVariable
	{
		BaseVariableState m_variable;
		ServerSystemContext m_context = null;
		IList m_values;
		int m_idx;

		public DynamicTestVariable(IList values, BaseVariableState variable, ServerSystemContext context)
		{
			m_values = values;
			m_variable = variable;
			m_context = context;
			m_idx = 0;
		}

		public void ChangeValue()
		{
			if (m_values.Count > 0)
			{
				m_idx++;
                if (m_idx >= m_values.Count)
                {
                    m_idx = 0;
                }
				//int idx = rand() % m_dataValues.size();
				
				m_variable.Value = m_values[m_idx];
				m_variable.Timestamp = DateTime.UtcNow;

				m_variable.ClearChangeMasks(m_context, true);
			}
		}
	}
}