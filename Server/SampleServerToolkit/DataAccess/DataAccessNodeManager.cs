/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SampleServerToolkit.DataAccess
{
    /// <summary>
    /// Node manager for DataAccess
    /// </summary>
    public class DataAccessNodeManager : NodeManager
    {
        #region Private Members
        private BaseDataVariableState m_doorOpened;
        private BaseDataVariableState m_doorClosed;
        private BaseDataVariableState m_lightStatus;

        AnalogItemState m_motorTemperature;
        private Timer m_simulationTimer;
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of DataAccessNodeManager
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        /// <param name="namespaceUris"></param>
        public DataAccessNodeManager(IServerInternal server, ApplicationConfiguration configuration, params string[] namespaceUris) 
            : base(server, configuration, Namespaces.DataAccess)
        {
        }
        #endregion

        /// <summary>
        /// Create address space associated with this NodeManager.
        /// </summary>
        /// <param name="externalReferences"></param>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                // Create a root node and add a reference to external Server Objects Folder
                FolderState folder = CreateFolder(null, "DataAccess");
                AddReference(folder, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                CreateRefrigerator(SystemContext, folder);
                m_simulationTimer = new Timer(DoSimulation, null, 1000, 1000);
            }
        }

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
            MethodState openCloseDoorMethod = CreateMethod(refrigerator, "OpenCloseDoor", inputArguments : inputArgs);               
            openCloseDoorMethod.OnCallMethod = DoOpenCloseDoorCall;
        }

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

            if ((bool)m_doorClosed.Value == true)
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

        private double GetNewValue(double minimum, double maximum)
        {
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
    }
}
