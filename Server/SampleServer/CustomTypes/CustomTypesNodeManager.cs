﻿/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/


using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;
using Softing.Opc.Ua.Server.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleServer.ComplexTypes
{
    /// <summary>
    /// A node manager for a server that manages the custom types
    /// </summary>
    public class CustomTypesNodeManager : NodeManager
    {
        private uint m_nodeIdIndex = 1;
        private FolderState m_rootCustomTypesFolder;
       private  FolderState m_arraysFolder;
        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public CustomTypesNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.CustomTypes)
        {
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
                // Execute base class CreateAddressSpace
                base.CreateAddressSpace(externalReferences);

                // Create a root node and add a reference to external Server Objects Folder
                m_rootCustomTypesFolder = CreateObjectFromType(null, "CustomTypes", ObjectTypeIds.FolderType, ReferenceTypeIds.Organizes) as FolderState;
                AddReference(m_rootCustomTypesFolder, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                m_arraysFolder = CreateObjectFromType(m_rootCustomTypesFolder, "Arrays", ObjectTypeIds.FolderType, ReferenceTypeIds.Organizes) as FolderState;
                AddReference(m_arraysFolder, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                CreateCustomComplexTypesAndInstances();

                CreateCustomVariableTypesAndInstances();
            }
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            return new NodeId(m_nodeIdIndex++, NamespaceIndex);
        }
        #endregion


        /// <summary>
        /// Creates a set of custom complex types and instances 
        /// </summary>
        private void CreateCustomComplexTypesAndInstances()
        {
            // define enum with EnumStrings
            EnumDefinition engineStateEnum = new EnumDefinition();
            engineStateEnum.Fields = new EnumFieldCollection()
            {
                new EnumField() { Name = "Stopped", Value = 0},
                new EnumField() { Name = "Running", Value = 1}
            };
            DataTypeState engineStateType = CreateComplexDataType(DataTypeIds.Enumeration, "EngineStateType", engineStateEnum);

            // define option set enum
            EnumDefinition displayWarningEnum = new EnumDefinition();
            displayWarningEnum.Fields = new EnumFieldCollection()
            {
                new EnumField() { Name = "ABS", Value = 1},
                new EnumField() { Name = "ESP", Value = 2},
                new EnumField() { Name = "TirePressure", Value = 4},
                new EnumField() { Name = "CheckEngine", Value = 8},
                new EnumField() { Name = "OpenDoor", Value = 16},
            };
            DataTypeState displayWarningType = CreateComplexDataType(DataTypeIds.UInt16, "DisplayWarningType", displayWarningEnum);

            // define option set type
            EnumDefinition featuresEnum = new EnumDefinition();
            featuresEnum.Fields = new EnumFieldCollection()
            {
                new EnumField() { Name = "ABS", Value = 1},
                new EnumField() { Name = "ESP", Value = 2},
                new EnumField() { Name = "AirbagPassenger", Value = 4},
                new EnumField() { Name = "AirbagSides", Value = 8},
            };
            DataTypeState featuresOptionSetType = CreateComplexDataType(DataTypeIds.OptionSet, "FeaturesOptionSetType", featuresEnum);

            // define structure with optional fields
            StructureDefinition ownerStructure = new StructureDefinition();
            ownerStructure.StructureType = StructureType.StructureWithOptionalFields;
            ownerStructure.Fields = new StructureFieldCollection()
            {
                new StructureField(){Name = "Name", DataType = DataTypeIds.String, IsOptional = false, ValueRank = ValueRanks.Scalar},
                new StructureField(){Name = "Age", DataType = DataTypeIds.Byte, IsOptional = true, ValueRank = ValueRanks.Scalar},
                new StructureField(){Name = "Details", DataType = DataTypeIds.String, IsOptional = true, ValueRank = ValueRanks.Scalar},
            };
            DataTypeState ownerType = CreateComplexDataType(DataTypeIds.Structure, "OwnerDetailsType", ownerStructure);

            // define union structure
            StructureDefinition fuelLevelDetailsUnion = new StructureDefinition();
            fuelLevelDetailsUnion.StructureType = StructureType.Union;
            fuelLevelDetailsUnion.Fields = new StructureFieldCollection()
            {
                new StructureField(){Name = "IsEmpty", DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar},
                new StructureField(){Name = "IsFull", DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar},
                new StructureField(){Name = "Liters", DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar},
            };
            DataTypeState fuelLevelDetailsType = CreateComplexDataType(DataTypeIds.Union, "FuelLevelDetailsType", fuelLevelDetailsUnion);

            StructureDefinition vehicleStructure = new StructureDefinition();
            vehicleStructure.StructureType = StructureType.Structure;
            vehicleStructure.Fields = new StructureFieldCollection()
            {
                new StructureField(){Name = "Name", DataType = DataTypeIds.String, IsOptional = false, ValueRank = ValueRanks.Scalar},
                new StructureField(){Name = "Owner", DataType = ownerType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
                new StructureField(){Name = "Features", DataType = featuresOptionSetType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
                new StructureField(){Name = "FuelLevel", DataType = fuelLevelDetailsType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
                new StructureField(){Name = "DisplayWarning", DataType = displayWarningType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
                new StructureField(){Name = "State", DataType = engineStateType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
            };

            DataTypeState vehicleType = CreateComplexDataType(DataTypeIds.Structure, "VehicleType", vehicleStructure);

            // add variables of custom type     
            var engineStateVariable = CreateVariable(m_rootCustomTypesFolder, "EngineState", engineStateType.NodeId);
            var displayWarningVariable = CreateVariable(m_rootCustomTypesFolder, "DisplayWarning", displayWarningType.NodeId);
            var featuresOptionSetVariable = CreateVariable(m_rootCustomTypesFolder, "FeaturesOptionSet", featuresOptionSetType.NodeId);            
            var ownerVariable = CreateVariable(m_rootCustomTypesFolder, "Owner", ownerType.NodeId);
            var fuelLevelVariable = CreateVariable(m_rootCustomTypesFolder, "FuelLevel", fuelLevelDetailsType.NodeId);
            var vehicle1Variable = CreateVariable(m_rootCustomTypesFolder, "Vehicle", vehicleType.NodeId);
            StructuredValue vehicle = vehicle1Variable.Value as StructuredValue;
            if (vehicle != null)
            {
                vehicle["Name"] = "BMW";
            }

            // add array variables
            var engineStateArrayVariable = CreateVariable(m_arraysFolder, "EngineStates", engineStateType.NodeId, ValueRanks.OneDimension);
            var displayWarningArrayVariable = CreateVariable(m_arraysFolder, "DisplayWarnings", displayWarningType.NodeId, ValueRanks.OneDimension);
            var featuresOptionSetArrayVariable = CreateVariable(m_arraysFolder, "FeaturesOptionSets", featuresOptionSetType.NodeId, ValueRanks.OneDimension);
            var ownerArrayVariable = CreateVariable(m_arraysFolder, "Owners", ownerType.NodeId, ValueRanks.OneDimension);
            var fuelLevelArrayVariable = CreateVariable(m_arraysFolder, "FuelLevels", fuelLevelDetailsType.NodeId, ValueRanks.OneDimension);
            var vehicleArrayVariable = CreateVariable(m_arraysFolder, "Vehicles", vehicleType.NodeId, ValueRanks.OneDimension);
        }

        /// <summary>
        /// Creates a set of custom VariableTypes and instances for them
        /// </summary>
        private void CreateCustomVariableTypesAndInstances()
        {
            BaseVariableTypeState customVariableType = CreateVariableType(VariableTypeIds.BaseDataVariableType, "CustomVariableType", DataTypeIds.UInt32);

            BaseVariableTypeState customVariable3PropertiesType = CreateVariableType(customVariableType.NodeId, "CustomVariable3PropertiesType", DataTypeIds.Byte);
            PropertyState propertyState1 = CreateProperty(customVariable3PropertiesType, "MandatoryBoolProperty", DataTypeIds.Boolean);
            propertyState1.ModellingRuleId = Objects.ModellingRule_Mandatory;

            PropertyState propertyState2 = CreateProperty(customVariable3PropertiesType, "OptionalBoolProperty", DataTypeIds.Boolean);
            propertyState2.ModellingRuleId = Objects.ModellingRule_Optional;

            PropertyState propertyState3 = CreateProperty(customVariable3PropertiesType, "MandatoryInt32Property", DataTypeIds.Int32);
            propertyState3.ModellingRuleId = Objects.ModellingRule_Mandatory;


            // create instance form new variable type
            var variable = CreateVariableFromType(m_rootCustomTypesFolder, "CustomVariableInstance", customVariableType.NodeId, ReferenceTypeIds.Organizes);
            var variable3Props = CreateVariableFromType(m_rootCustomTypesFolder, "CustomVariable3PropertiesInstance", customVariable3PropertiesType.NodeId, ReferenceTypeIds.HasComponent);
        }
    }
}
