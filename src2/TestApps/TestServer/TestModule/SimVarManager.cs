using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TestServer.SimulationModule;
using TestServer.TestModule;

namespace TestServer
{
	class SimVarManager
	{
		/// <summary>
		/// The UID assigned to each set of allocated nodeId
		/// </summary>
		uint m_nextSimVarId;
		/// <summary>
		/// List with all namespace indexes allocated for the simulation variables.
		/// </summary>
		LinkedList<ushort> m_allocatedNamespaceIndexes, m_freeNamespaceIndexes;
		/// <summary>
		/// A constant for start the namespace index allocation
		/// </summary>
		const ushort START_NAMESPACE_INDEX = 10;
		/// <summary>
		/// A constant for the upper value of the namespace indexes to be allocated.
		/// </summary>
		const ushort MAX_NAMESPACE_INDEX = 1000;
		const string namePattern = "TV_%d";
		/// <summary>
		/// All the info related to simulation variables
		/// </summary>
		Dictionary<uint, SimVarRecord> m_simVarInfo;
		/// <summary>
		/// Stores reference to all sets of sim vars created
		/// </summary>
		Dictionary<uint, SimVarSet> m_simulationVariables;

		/// <summary>
		/// Used to lock acces to the root object
		/// </summary>
		object m_RootLock;

		public SimVarManager()
		{
			m_freeNamespaceIndexes = new LinkedList<ushort>();
			//m_freeNamespaceIndexes.AddLast(2);
			for(ushort i = START_NAMESPACE_INDEX; i < MAX_NAMESPACE_INDEX; i++)
				m_freeNamespaceIndexes.AddLast(i);

			m_allocatedNamespaceIndexes = new LinkedList<ushort>();

			m_nextSimVarId = 0;

			m_simVarInfo = new Dictionary<uint, SimVarRecord>();
			m_simulationVariables = new Dictionary<uint, SimVarSet>();

			m_RootLock = new object();
		}

		StatusCode RangeToSimVarRec(IdType nodeIdType, UInt32 count,  uint nodeDataType, int nodeValueRank, int nodeArrayLength, UInt16 namespaceIndex, SimVarRecord pRec)
		{
			if (nodeIdType != IdType.Numeric &&	nodeIdType != IdType.String && nodeIdType != IdType.Guid && nodeIdType !=  IdType.Opaque)
			{
				Debug.Fail("Invalid nodeIdType!");
				return StatusCodes.BadInvalidArgument;
			}

			Debug.Assert(count > 0);

			List<NodeId> nodeIds = new List<NodeId>((int) count);

			switch(nodeIdType)
			{
				case IdType.Numeric:
					for(uint i = 1; i <= count; i++)
					{
						nodeIds.Add(new NodeId(i, namespaceIndex));
					}
					break;

				case IdType.String:
					for(uint i = 1; i <= count; i++)
					{
						nodeIds.Add(new NodeId(String.Format("TV_{0:D8}", i), namespaceIndex));
					}
					break;
				case IdType.Guid:
					for(uint i = 1; i <= count; i++)
					{
						nodeIds.Add(new NodeId(Guid.NewGuid(), namespaceIndex));
					}
					break;
				case IdType.Opaque:
					for(uint i = 1; i <= count; i++)
					{
						nodeIds.Add(new NodeId(BitConverter.GetBytes(i), namespaceIndex));
					}
					break;
			}

			for(uint i = 1; i <= count; i++)
			{
                Variant val = new Variant();

				if (nodeValueRank == ValueRanks.Scalar)
				{
					switch(nodeDataType)
					{
                        case Opc.Ua.DataTypes.UInt32:
							val = new Variant((uint) 0);
							break;
						default:
							val = new Variant();
							break;
					}
				}
				else if (nodeValueRank == ValueRanks.OneDimension)
				{
					switch(nodeDataType)
					{
                        case Opc.Ua.DataTypes.Byte:
							val = new Variant(new byte[nodeArrayLength]);
							break;
						default:
							val = new Variant();
							break;
					}
				}

				pRec.InitialValues.Add(val);
			}

			pRec.m_nodeIds = nodeIds;

			return StatusCodes.Good;
		}

		internal StatusCode CreateVariables(sbyte nodeIdType, uint count, ushort namespaceIndex, SimVarRecord pRec, BaseObjectState root, out SimVarSet simVarSet)
		{
			return CreateVariables((IdType) nodeIdType, count, DataTypeIds.UInt32, ValueRanks.Scalar, 0, namespaceIndex, pRec, root, out simVarSet);
		}

        internal StatusCode CreateVariables(IdType nodeIdType, uint count, NodeId nodeDataType, int nodeValueRank, int nodeArrayLength, ushort namespaceIndex, SimVarRecord pRec, BaseObjectState root, out SimVarSet simVarSet)
        {
            return CreateVariables(nodeIdType, count, nodeDataType, nodeValueRank, nodeArrayLength, namespaceIndex, pRec, root, out simVarSet, false);
        }

		internal StatusCode CreateVariables(IdType nodeIdType, uint count, NodeId nodeDataType, int nodeValueRank, int nodeArrayLength, ushort namespaceIndex, SimVarRecord pRec, BaseObjectState root, out SimVarSet simVarSet, bool disableStatistics)
		{
			StatusCode retCode;

			if ((retCode = RangeToSimVarRec(nodeIdType, count, (UInt32) nodeDataType.Identifier, nodeValueRank, nodeArrayLength, namespaceIndex, pRec)) != StatusCodes.Good)
			{
				simVarSet = null;
				return retCode;
			}

			// Create the sim vars
			simVarSet = new SimVarSet();

			for(int i = 0; i < count; i++)
			{
				SimulationVariable simVariable = new SimulationVariable(root, disableStatistics);
				simVariable.NodeId = pRec.m_nodeIds[i];
				simVariable.BrowseName = new QualifiedName(String.Format("TV_{0}", i), namespaceIndex);
				simVariable.DisplayName = simVariable.BrowseName.Name;
				simVariable.DataType = nodeDataType;
				simVariable.ValueRank = nodeValueRank;
				simVariable.TypeDefinitionId = VariableTypeIds.DataItemType;
				simVariable.AccessLevel = AccessLevels.CurrentReadOrWrite;
                simVariable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
				simVariable.Value = pRec.InitialValues[i];
                simVariable.OnReadValue = NodeValueEventHandler;

				simVariable.DataTypeId = (UInt32) nodeDataType.Identifier;

				lock(m_RootLock)
				{
					root.AddChild(simVariable);
				}

				simVarSet.Add(simVariable);
			}
			
			lock(m_simulationVariables)
			{
				m_nextSimVarId++;

				pRec.simVarSetId = m_nextSimVarId;
				m_simVarInfo.Add(m_nextSimVarId, pRec);
			
				m_simulationVariables.Add(m_nextSimVarId, simVarSet);
			}
			
			return StatusCodes.Good;
		}

        public ServiceResult NodeValueEventHandler(
        ISystemContext context,
        NodeState node,
        NumericRange indexRange,
        QualifiedName dataEncoding,
        ref object value,
        ref StatusCode statusCode,
        ref DateTime timestamp) 
        {
            timestamp = DateTime.UtcNow;
            return ServiceResult.Good;
        }

		public bool GetNextNamespaceIndex(ref ushort pNextIndex)
		{
			lock(m_allocatedNamespaceIndexes)
			{
				if (m_freeNamespaceIndexes.First != null)
				{
					pNextIndex = m_freeNamespaceIndexes.First.Value;
					LinkedListNode<ushort> node = m_freeNamespaceIndexes.First;
					m_freeNamespaceIndexes.RemoveFirst();
					m_allocatedNamespaceIndexes.AddLast(node);
					return true;
				}
			}

			return false;
		}

		private bool DeleteNamespaceIndex(ushort index)
		{
			lock(m_allocatedNamespaceIndexes)
			{
				LinkedListNode<ushort> node = m_allocatedNamespaceIndexes.Find(index);

				if (node == null)
					return false;

				m_allocatedNamespaceIndexes.Remove(node);
				m_freeNamespaceIndexes.AddLast(node);
			}

			return true;
		}

		internal StatusCode DeleteVariables(uint simVarSetId, BaseObjectState root, out SimVarSet pSimVarSet)
		{
			SimVarRecord rec;

			lock(m_simulationVariables)
			{
				if (m_simVarInfo.TryGetValue(simVarSetId, out rec))
				{
					DeleteNamespaceIndex(rec.NameIndex);

					pSimVarSet = m_simulationVariables[simVarSetId];

					lock(m_RootLock)
					{
						foreach(SimulationVariable var in pSimVarSet)
						{
							root.RemoveChild(var);
						}
					}

					m_simVarInfo.Remove(simVarSetId);
					m_simulationVariables.Remove(simVarSetId);

					return StatusCodes.Good;
				}
				else
				{
					pSimVarSet = null;
					return StatusCodes.Bad;
				}
			}
		}

		public bool IsNodeIdInNamespace(NodeId nodeId)
		{
			lock(m_allocatedNamespaceIndexes)
			{
				if (m_allocatedNamespaceIndexes.Contains(nodeId.NamespaceIndex))
					return true;
			}

			return false;
		}

		internal StatusCode StartSimulation(uint simVarSetId, uint changeInterval, uint repeatCount, double increment, uint changeCount)
		{
			SimVarRecord rec;
			SimVarSet varSet;

			lock(m_simulationVariables)
			{
				if (!m_simVarInfo.TryGetValue(simVarSetId, out rec))
					return StatusCodes.Bad;

				if (rec.simulationStarted)
					return StatusCodes.Good;

				varSet = m_simulationVariables[simVarSetId];
			}

			rec.m_simAction = new SimVarChangeAction(varSet, changeInterval, repeatCount, increment, changeCount);
			rec.simulationStarted = true;
			ApplicationModule.Instance.TimerThread.AddAction(rec.m_simAction);

			return StatusCodes.Good;
		}

		internal StatusCode StopSimulation(uint simVarSetId)
		{
			SimVarRecord rec;
			lock(m_simulationVariables)
			{
				if (!m_simVarInfo.TryGetValue(simVarSetId, out rec))
					return StatusCodes.Bad;

				if (!rec.simulationStarted)
					return StatusCodes.Good;

				SimVarSet varSet = m_simulationVariables[simVarSetId];
			}

			rec.simulationStarted = false;
			ApplicationModule.Instance.TimerThread.RemoveAction(rec.m_simAction);

			Console.WriteLine("StopSimulation after {0} changes", rec.m_simAction.Total);

			return StatusCodes.Good;
		}
	}

	class SimVarSet : List<SimulationVariable> { };
}
