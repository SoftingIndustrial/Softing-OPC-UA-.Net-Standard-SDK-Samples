using System.Collections.Generic;
using Opc.Ua;

namespace TestServer.TestModule
{
	class SimVarRecord
	{
		/// <summary>
		/// allocated nodeIds
		/// </summary>
		public List<NodeId> m_nodeIds;
		/// <summary>
		/// Initial values of the value attribute of the nodes to be created
		/// </summary>
		List<Variant> m_initialValues;
		//public QualifiedName BrowseName { get; set; }
		//public LocalizedText DisplayName { get; set; }
		/// <summary>
		/// The UID allocated for this record by the server
		/// </summary>
		public uint simVarSetId;

		public bool simulationStarted;

		public SimVarChangeAction m_simAction;

		public SimVarRecord()
		{
			m_initialValues = new List<Variant>();
		}

		public List<Variant> InitialValues
		{
			get { return m_initialValues; }
		}

		public ushort NamespaceIndex
		{
			get;
			set;
		}

		public ushort NameIndex
		{
			get;
			set;
		}
	}
}