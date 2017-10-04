using System;
using System.Collections.Generic;
using Opc.Ua;
using Opc.Ua.Server;

namespace TestServer.SimulationModule
{
    public class SimulationModule : Module
    {
        #region Private Members

        private SimulationNodeManager m_nodeManager;    
        private List<SimulationVariableList> m_simulationVariables = new List<SimulationVariableList>();
        private SimulationVariableList m_simulationVariablesOld = new SimulationVariableList();
        ushort m_nextNameSpaceIndex = 1;

        #endregion

        public override INodeManager GetNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            if (m_nodeManager == null)
            {
                m_nodeManager = new SimulationNodeManager(server, configuration);
                m_nodeManager.Init(this);
            }

            return m_nodeManager;
        }

        #region Simulation
        internal void StartSimulation(NodeIdInfo nodeIdInfo)
        {
            if (!TestAlreadyCreated(nodeIdInfo))
            {
                TestUtils.Trace("test allready created.");
                return;
            }

            if (TestLaunchedIntersect(nodeIdInfo))
            {
                TestUtils.Trace("test launched intersect.");
                return;
            }

            if (TestAlreadyLaunched(nodeIdInfo))
            {
                TestUtils.Trace("test allready launched.");
                return;
            }
            SimulationVariableList pSv = GetTest(nodeIdInfo);
            if (nodeIdInfo.VariableDataType == VarType.Unknown)
            {
                TestUtils.Trace("Input simulation can not be started, unknown data type.");
                return;
            }
            //since we pass double for decimals also, the value should be checked before casting.
            if (!VerifyIncrement(nodeIdInfo.VariableDataType, nodeIdInfo.Increment))
            {
                TestUtils.Trace("Input simulation can not be started, increment incompatible with variable type.");
                return;
            }
            pSv.IntervalMilliSecs = nodeIdInfo.Interval;
            pSv.SimulationRepeatCount = nodeIdInfo.RepeatCount;
            pSv.Increment = nodeIdInfo.Increment;

            //todo
            //pSv->m_simAction.setCyclic(true);
            //pSv->m_simAction.setTimeout(pSv->m_intervalMilliSecs);

            pSv.SimulationIsRunning = true;
            //todo
            //getApplicationModule()->addAction(&pSv->m_simAction);
        }

        internal void StopSimulation()
        {
            SimulationVariableList pSv;
	        for(int i = 0; i < m_simulationVariables.Count; i++)
	        {
		        //first stop the scheduled actions.
		        pSv = m_simulationVariables[i];
		        if (pSv.SimulationIsRunning)
		        {
                    //todo
			        //getApplicationModule()->removeAction(&pSv->m_simAction);
			        pSv.SimulationIsRunning = false;
		        }
		        //remove the variables from the address space
		        SimulationVariable simVarPtr;
		        for(int j = 0; j < pSv.Count; j++)
		        {
			        simVarPtr = pSv[j];
			        simVarPtr.Delete(NodeManager.SystemContext);
		        }
		        //delete the variables from the array
		        pSv.Clear();
	        }
	        //delete all the array 
	        m_simulationVariables.Clear();
        }

        internal void StopSimulation(NodeIdInfo nodeIdInfo)
        {
            SimulationVariableList pSv;
	        for(int i = 0; i < m_simulationVariables.Count; i++)
	        {
		        pSv = m_simulationVariables[i];
		        if (pSv.NodeIdInfo.AreEqual(nodeIdInfo))
		        {
			        if (pSv.SimulationIsRunning)
			        {
                        //todo
				        //getApplicationModule()->removeAction(&pSv->m_simAction);
				        pSv.SimulationIsRunning = false;
			        }
			        //remove the variables from the address space
			        SimulationVariable simVarPtr;
			        for(int j = 0; j < pSv.Count; j++)
			        {
				        simVarPtr = pSv[j];
                        simVarPtr.Delete(NodeManager.SystemContext);
			        }
			        //delete the variables from the array
			        pSv.Clear();
			        //delete the set of variables from the simulation module
			        m_simulationVariables.Clear();
			        break;
		        }
		        else
		        {
			        i++;
		        }
	        }
        }

        internal void CreateAllTestVariables(NodeIdInfo nodeIdInfo)
        {
            if (!nodeIdInfo.isValid())
            {
                TestUtils.Trace(string.Format("Invalid parameters for create nodes: {0} \n", nodeIdInfo.ToString()));
            }
            if (TestAlreadyCreated(nodeIdInfo))
                return;
            if (TestIntersect(nodeIdInfo))
                return;

            CreateTestVariables(nodeIdInfo);
        }

        internal void CreateTestVariablesOld(uint countVariable, bool stringIdsVariable)
        {
            m_simulationVariablesOld.Clear();
            for (int i = 0; i < countVariable; i++)
            {
                SimulationVariable simVariable = new SimulationVariable(NodeManager.TestVariablesNode);
                string testVariableName = "TV_" + i;
                LocalizedText parameterVariableDisplayName = new LocalizedText("en", testVariableName);
                // instantiate based on the type model. assigns ids automatically using SystemContext.NodeIdFactory
                simVariable.Create(
                    NodeManager.SystemContext,
                    GenerateNodeId(m_nextNameSpaceIndex, stringIdsVariable, testVariableName, (uint)i),
                    new QualifiedName(testVariableName, m_nextNameSpaceIndex),
                    parameterVariableDisplayName,
                    true);
                simVariable.DataType = DataTypeIds.UInt32;
                simVariable.ValueRank = ValueRanks.Scalar;
                simVariable.AccessLevel = AccessLevels.CurrentReadOrWrite;
                simVariable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                simVariable.Description = string.Empty;
                simVariable.StatusCode = StatusCodes.Good;
                TestUtils.SetDefaultValue(simVariable);
                NodeManager.TestVariablesNode.AddChild(simVariable);

                NodeManager.AddPredefinedNode(simVariable);
            }
        }

        internal void DeleteNode(NodeId nodeId)
        {
            if (m_nodeManager == null)
            {
                TestUtils.Trace("Node manager is NULL!");
                throw new Exception("Node manager is NULL!");
            }
            m_nodeManager.DeleteNode(nodeId);
        }
        #endregion

        #region Properties
        public SimulationNodeManager NodeManager
        {
            get { return m_nodeManager; }
        }

        #endregion

        #region Private Methods

        private bool TestAlreadyCreated(NodeIdInfo nodeIdInfo)
        {
            SimulationVariableList variableList;
            bool result = false;
            for (int i = 0; i < m_simulationVariables.Count; i++)
            {
                variableList = m_simulationVariables[i];
                if (variableList.NodeIdInfo.AreEqual(nodeIdInfo))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        private bool TestIntersect(NodeIdInfo nodeIdInfo)
        {
            SimulationVariableList pSv;
            bool result = false;
            for (int i = 0; i < m_simulationVariables.Count; i++)
            {
                pSv = m_simulationVariables[i];
                if (pSv.NodeIdInfo.AreIntersecting(nodeIdInfo))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        private void CreateTestVariables(NodeIdInfo nodeIdInfo)
        {
            SimulationVariableList pSv = new SimulationVariableList();
            pSv.NodeIdInfo = nodeIdInfo;
            m_simulationVariables.Add(pSv);

            string testVariableNamePrefix = string.Empty;
            if (nodeIdInfo.IsString)
            {
                testVariableNamePrefix += nodeIdInfo.Pattern;
            }
            else
            {
                testVariableNamePrefix += "TV_";
            }

            //set the data type out of the loop, same for all variables
            NodeId dataType = null;
            switch (nodeIdInfo.VariableDataType)
            {
                case VarType.Uint8:
                    dataType = DataTypeIds.SByte;
                    break;
                case VarType.Int8:
                    dataType = DataTypeIds.Byte;
                    break;
                case VarType.Uint16:
                    dataType = DataTypeIds.UInt16;
                    break;
                case VarType.Int16:
                    dataType = DataTypeIds.Int16;
                    break;
                case VarType.Uint32:
                    dataType = DataTypeIds.UInt32;
                    break;
                case VarType.Int32:
                    dataType = DataTypeIds.Int32;
                    break;
                case VarType.Uint64:
                    dataType = DataTypeIds.UInt64;
                    break;
                case VarType.Int64:
                    dataType = DataTypeIds.Int64;
                    break;
                case VarType.Float:
                    dataType = DataTypeIds.Float;
                    break;
                case VarType.Double:
                    dataType = DataTypeIds.Double;
                    break;
                default:
                    TestUtils.Trace(string.Format("Invalid data type for create nodes: {0} \n", nodeIdInfo.VariableDataType));
                    break;
            }

            ++m_nextNameSpaceIndex;

            for (ulong i = nodeIdInfo.StartIndex; i < nodeIdInfo.EndIndex; i++)
            {
                SimulationVariable simVariable = new SimulationVariable(NodeManager.TestVariablesNode);
                string testVariableName = testVariableNamePrefix + i;


                LocalizedText parameterVariableDisplayName = new LocalizedText("en", testVariableName);
                // instantiate based on the type model. assigns ids automatically using SystemContext.NodeIdFactory
                simVariable.Create(
                    NodeManager.SystemContext,
                    GenerateNodeId(m_nextNameSpaceIndex, nodeIdInfo.IsString, testVariableName, (uint)i),
                    new QualifiedName(testVariableName, m_nextNameSpaceIndex),
                    parameterVariableDisplayName,
                    true);
                simVariable.DataType = dataType;
                simVariable.ValueRank = ValueRanks.Scalar;
                simVariable.AccessLevel = AccessLevels.CurrentReadOrWrite;
                simVariable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                simVariable.Description = string.Empty;
                simVariable.StatusCode = StatusCodes.Good;
                TestUtils.SetDefaultValue(simVariable);
                NodeManager.TestVariablesNode.AddChild(simVariable);

                NodeManager.AddPredefinedNode(simVariable);
                pSv.Add(simVariable);
            }
        }

        private NodeId GenerateNodeId(ushort nextNameSpaceIndex, bool isString, string testVariableName, uint index)
        {
            NodeId nodeId;
            if (isString)
            {
                nodeId = new NodeId(testVariableName, nextNameSpaceIndex);
            }
            else
            {
                nodeId = new NodeId(index, nextNameSpaceIndex);
            }

            return nodeId;
        }

        private bool TestLaunchedIntersect(NodeIdInfo nodeIdInfo)
        {
            SimulationVariableList pSv;
            bool result = false;
            for (int i = 0; i < m_simulationVariables.Count; i++)
            {
                pSv = m_simulationVariables[i];
                if (pSv.SimulationIsRunning)
                {
                    if (pSv.NodeIdInfo.AreIntersecting(nodeIdInfo))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        private bool TestAlreadyLaunched(NodeIdInfo nodeIdInfo)
        {
            SimulationVariableList pSv;
            bool result = false;
            for (int i = 0; i < m_simulationVariables.Count; i++)
            {
                pSv = m_simulationVariables[i];
                if (pSv.NodeIdInfo.AreEqual(nodeIdInfo))
                {
                    result = pSv.SimulationIsRunning;
                    break;
                }
            }

            return result;
        }

        private SimulationVariableList GetTest(NodeIdInfo nodeIdInfo)
        {
            SimulationVariableList pSv = null;
            for (int i = 0; i < m_simulationVariables.Count; i++)
            {
                if (m_simulationVariables[i].NodeIdInfo.AreEqual(nodeIdInfo))
                {
                    pSv = m_simulationVariables[i];
                    //find out the type of the variable
                    if (pSv.Count > 0)
                    {
                        SimulationVariable pV = pSv[0];
                        if (pV != null)
                        {
                            nodeIdInfo.VariableDataType = TestUtils.DataTypeToVarType(pV.DataType);
                        }
                    }
                    break;
                }
            }

            return pSv;
        }

        private bool VerifyIncrement(VarType varType, double inc)
        {
            if (inc <= 0)
            {
                TestUtils.Trace(string.Format("Increment value for input simulation can not be <= 0, value={0}.", inc));
                return false;
            }
            bool result = true;
            switch (varType)
            {
                case VarType.Uint8:
                    if (inc < byte.MinValue ||
                        inc > byte.MaxValue)
                        result = false;
                    break;
                case VarType.Int8:
                    if (inc < byte.MinValue ||
                        inc > byte.MaxValue)
                        result = false;
                    break;
                case VarType.Uint16:
                    if (inc < UInt16.MinValue ||
                        inc > UInt16.MaxValue)
                        result = false;
                    break;
                case VarType.Int16:
                    if (inc < Int16.MinValue ||
                        inc > Int16.MaxValue)
                        result = false;
                    break;
                case VarType.Uint32:
                    if (inc < UInt32.MinValue ||
                        inc > UInt32.MaxValue)
                        result = false;
                    break;
                case VarType.Int32:
                    if (inc < Int32.MinValue ||
                        inc > Int32.MaxValue)
                        result = false;
                    break;
                case VarType.Uint64:
                    if (inc < UInt64.MinValue ||
                        inc > UInt64.MaxValue)
                        result = false;
                    break;
                case VarType.Int64:
                    if (inc < Int64.MinValue ||
                        inc > Int64.MaxValue)
                        result = false;
                    break;
                case VarType.Float:
                    if (inc < float.MinValue ||
                        inc > float.MaxValue)
                        result = false;
                    break;
                case VarType.Double:
                    break;
                default:
                    result = false;
                    TestUtils.Trace("Unknown variable type");
                    throw new Exception("Unknown variable type");
            }

            return result;
        }

        #endregion
    }
}