/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.DataAccess
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class DataAccessNodeManager : NodeManager
    {
        #region Private Members
        private BaseDataVariableState m_doorOpened;
        private BaseDataVariableState m_doorClosed;
        private BaseDataVariableState m_lightStatus;
        private AnalogItemState m_motorTemperature;
        private Timer m_simulationTimer;

        private uint m_timerInterval = 1000;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public DataAccessNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.DataAccess)
        {
            //parse custom configuration extension 
            SampleServerConfiguration sampleServerConfiguration = configuration.ParseExtension<SampleServerConfiguration>();
            if (sampleServerConfiguration != null)
            {
                m_timerInterval = sampleServerConfiguration.TimerInterval;
            }
        }
        #endregion

        #region INodeManager Members
        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                // Create a root node and add a reference to external Server Objects Folder
                FolderState folder = CreateFolder(null, "DataAccess");
                AddReference(folder, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                CreateRefrigerator(SystemContext, folder);
                

                m_simulationTimer = new Timer(DoSimulation, null, m_timerInterval, m_timerInterval);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates the refrigerator instance with all its components
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="parent">The folder</param>
        private void CreateRefrigerator(ServerSystemContext context, FolderState parent)
        {
            BaseObjectState refrigerator = CreateObject(parent, "Refrigerator");

            parent.AddReference(ReferenceTypeIds.Organizes, false, refrigerator.NodeId);
            refrigerator.AddReference(ReferenceTypeIds.Organizes, true, parent.NodeId);

            // Create CoolingMotorRunning variable
            BaseDataVariableState coolingMotorRunning = CreateVariable(refrigerator, "CoolingMotorRunning", DataTypeIds.Boolean, ValueRanks.Scalar);
            coolingMotorRunning.Value = true;

            // Create DoorMotor variable
            BaseDataVariableState doorMotor = CreateVariable(refrigerator, "DoorMotor", DataTypeIds.Double, ValueRanks.Scalar);
            doorMotor.Value = 11.2;

            // Create LightStatus variable
            m_lightStatus = CreateVariable(refrigerator, "LightStatus", DataTypeIds.Boolean, ValueRanks.Scalar);
            m_lightStatus.Value = true;

            // Create DoorClosed variable
            m_doorClosed = CreateVariable(refrigerator, "DoorClosed", DataTypeIds.Boolean, ValueRanks.Scalar);

            // Create DoorOpened variable
            m_doorOpened = CreateVariable(refrigerator, "DoorOpened", DataTypeIds.Boolean, ValueRanks.Scalar);
            m_doorOpened.Value = true;

            // Create ActualTemperature variable
            AnalogItemState actualTemperature = CreateAnalogVariable(refrigerator, "ActualTemperature", DataTypeIds.Double, ValueRanks.Scalar, new Range(90, 10), null);
            actualTemperature.InstrumentRange.Value = new Range(100, 0);
            actualTemperature.Value = 2.7;

            // Create MotorTemperature variable
            m_motorTemperature = CreateAnalogVariable(refrigerator, "MotorTemperature", DataTypeIds.Double, ValueRanks.Scalar, new Range(90, 10), null);
            m_motorTemperature.InstrumentRange.Value = new Range(100, 0);
            m_motorTemperature.Value = 47.6;

            // Create SetpointOfTheTemperature variable
            AnalogItemState setpointOfTheTemperature = CreateAnalogVariable(refrigerator, "SetpointOfTheTemperature", DataTypeIds.Double, ValueRanks.Scalar, new Range(90, 10), null);
            setpointOfTheTemperature.Value = 3.2;
            setpointOfTheTemperature.InstrumentRange.Value = new Range(100, 0);

            // Create OpenCloseDoor method
            Argument[] inputArgs = new Argument[]
                 {
                    new Argument { Name = "OpenCloseDoor", Description = "Opens/closes the door.",  DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar }
                };
            MethodState openCloseDoorMethod = CreateMethod(refrigerator, "OpenCloseDoor", inputArguments: inputArgs);
            openCloseDoorMethod.OnCallMethod = DoOpenCloseDoorCall;
        }

        /// <summary>
        /// Handles the method call of the OpenCloseDoor method
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="method">The method state.</param>
        /// <param name="inputArguments">The input arguments.</param>
        /// <param name="outputArguments">The output arguments.</param>
        /// <returns></returns>
        private ServiceResult DoOpenCloseDoorCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments == null)
            {
                return new ServiceResult(StatusCodes.BadArgumentsMissing);
            }

            if (inputArguments.Count == 0)
            {
                return new ServiceResult(StatusCodes.BadArgumentsMissing);
            }

            bool? input = inputArguments[0] as bool?;
            if (!input.HasValue)
            {
                return new ServiceResult(StatusCodes.BadArgumentsMissing);
            }

            m_doorOpened.Value = input.Value;
            m_doorOpened.ClearChangeMasks(context, false);
            m_doorClosed.Value = !input.Value;
            m_doorClosed.ClearChangeMasks(context, false);

            if ((bool)m_doorClosed.Value)
            {
                m_lightStatus.Value = false;
            }
            else
            {
                m_lightStatus.Value = true;
            }

            m_lightStatus.ClearChangeMasks(context, false);

            return new ServiceResult(StatusCodes.Good);
        }

        /// <summary>
        /// Simulate changes in variable nodes
        /// </summary>
        /// <param name="state"></param>
        private void DoSimulation(object state)
        {
            try
            {
                lock (Lock)
                {
                    m_motorTemperature.Value = GetNewValue(0, 100);
                    m_motorTemperature.Timestamp = DateTime.UtcNow;
                    m_motorTemperature.ClearChangeMasks(SystemContext, false);
                }

                // Report an event at Server level

                string eventMessage = String.Format("Motor temperature changed to {0}", m_motorTemperature.Value);
                BaseEventState temperatureChangeEvent = new BaseEventState(m_motorTemperature);

                temperatureChangeEvent.Initialize(SystemContext, m_motorTemperature, EventSeverity.Medium,
                    new LocalizedText(eventMessage));

                Server.ReportEvent(temperatureChangeEvent);
            }
            catch (Exception e)
            {
                Utils.Trace(Utils.TraceMasks.Error, "DataAccess.DataAccessNodeManager.DoSimulation", "Unexpected error doing simulation.", e);
            }
        }

        /// <summary>
        /// Generate new value in provided bounds
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <returns></returns>
        private double GetNewValue(double minimum, double maximum)
        {
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
        #endregion
    }
}