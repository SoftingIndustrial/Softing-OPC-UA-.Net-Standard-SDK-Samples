/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
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

namespace SampleServer.CustomTypes
{
    /// <summary>
    /// A node manager for a server that manages the custom types
    /// </summary>
    public class CustomTypesNodeManager : NodeManager
    {
        #region Private Fields
        private uint m_nodeIdIndex = 1;
        private FolderState m_rootCustomTypesFolder;
        private FolderState m_arraysFolder;

        private NodeId m_vehicleDataTypeNodeId;
        #endregion

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

                #region  Create custom complex types and instances
                // define enum with EnumStrings
                EnumDefinition engineStateEnum = new EnumDefinition();
                engineStateEnum.Fields = new EnumFieldCollection()
                {
                    new EnumField() { Name = "Stopped", Value = 0},
                    new EnumField() { Name = "Running", Value = 1}
                };
                DataTypeState engineStateType = CreateComplexDataType(DataTypeIds.Enumeration, "EngineStateType", engineStateEnum);

                // define option set enumeration. All EnumField values must be a power of 2
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

                // define option set type. All EnumField values must be a power of 2
                EnumDefinition featuresEnum = new EnumDefinition();
                featuresEnum.Fields = new EnumFieldCollection()
                {
                    new EnumField() { Name = "ABS", Value = 1},
                    new EnumField() { Name = "ESP", Value = 2},
                    new EnumField() { Name = "AirbagPassenger", Value = 4},
                    new EnumField() { Name = "AirbagSides", Value = 8},
                };
                DataTypeState featuresOptionSetType = CreateComplexDataType(DataTypeIds.OptionSet, "FeaturesOptionSetType", featuresEnum);

                // create a StructureDefinition object and specify which fields are optional 
                StructureDefinition ownerStructure = new StructureDefinition();
                // set the StructureType property to StructureWithOptionalFields
                ownerStructure.StructureType = StructureType.StructureWithOptionalFields;
                ownerStructure.Fields = new StructureFieldCollection()
                {
                    new StructureField(){Name = "Name", DataType = DataTypeIds.String, IsOptional = false, ValueRank = ValueRanks.Scalar},
                    new StructureField(){Name = "Age", DataType = DataTypeIds.Byte, IsOptional = true, ValueRank = ValueRanks.Scalar},
                    new StructureField(){Name = "Details", DataType = DataTypeIds.String, IsOptional = true, ValueRank = ValueRanks.Scalar},
                };
                DataTypeState ownerType = CreateComplexDataType(DataTypeIds.Structure, "OwnerDetailsType", ownerStructure);

                // create a StructureDefinition object that describes a union data type 
                StructureDefinition fuelLevelDetailsUnion = new StructureDefinition();
                // set the StructureType property to Union
                fuelLevelDetailsUnion.StructureType = StructureType.Union;
                fuelLevelDetailsUnion.Fields = new StructureFieldCollection()
                {
                    new StructureField(){Name = "IsEmpty", DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar},
                    new StructureField(){Name = "IsFull", DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar},
                    new StructureField(){Name = "Liters", DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar},
                };
                DataTypeState fuelLevelDetailsType = CreateComplexDataType(DataTypeIds.Union, "FuelLevelDetailsType", fuelLevelDetailsUnion);

                // create a StructureDefinition object, specify IsOptional = false for all fields 
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
                // call CreateComplexDataType with baseDataTypeId = DataTypeIds.Structure 
                DataTypeState vehicleType = CreateComplexDataType(DataTypeIds.Structure, "VehicleType", vehicleStructure);
                m_vehicleDataTypeNodeId = vehicleType.NodeId;

                // add variables of custom type     
                var engineStateVariable = CreateVariable(m_rootCustomTypesFolder, "EngineState", engineStateType.NodeId);
                engineStateVariable.Description = "Variable with data type defined as custom Enumeration";
                var displayWarningVariable = CreateVariable(m_rootCustomTypesFolder, "DisplayWarning", displayWarningType.NodeId);
                displayWarningVariable.Description = "Variable with data type defined as custom OptionSet Enumeration";
                var featuresOptionSetVariable = CreateVariable(m_rootCustomTypesFolder, "FeaturesOptionSet", featuresOptionSetType.NodeId);
                featuresOptionSetVariable.Description = "Variable with data type defined as custom OptionSet";
                var ownerVariable = CreateVariable(m_rootCustomTypesFolder, "Owner", ownerType.NodeId);
                ownerVariable.Description = "Variable with data type defined as StructuredValue with optional fields";
                var fuelLevelVariable = CreateVariable(m_rootCustomTypesFolder, "FuelLevel", fuelLevelDetailsType.NodeId);
                fuelLevelVariable.Description = "Variable with data type defined as Union";
var vehicle1Variable = CreateVariable(m_rootCustomTypesFolder, "Vehicle", vehicleType.NodeId);
vehicle1Variable.Description = "Variable with data type defined as StructuredValue";
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
                #endregion

                #region  create custom Variable types and instances
                BaseVariableTypeState customVariableType = CreateVariableType(VariableTypeIds.BaseDataVariableType, "CustomVariableType", DataTypeIds.UInt32);

                // Create a variable type that has data type = complex data vehicle type 
                BaseVariableTypeState vehicleVariableType = CreateVariableType(customVariableType.NodeId, "VehicleVariableType", vehicleType.NodeId, ValueRanks.Scalar);
                vehicleVariableType.Description = "Custom Variable type with DataType=VehicleType";
                // set variable type default value
                StructuredValue vehicleDefault = GetDefaultValueForDatatype(vehicleType.NodeId) as StructuredValue;
                vehicleVariableType.Value = vehicleDefault;

                PropertyState propertyState1 = CreateProperty(vehicleVariableType, "MandatoryBoolProperty", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modeling rule has to be specified
                propertyState1.ModellingRuleId = Objects.ModellingRule_Mandatory;

                PropertyState propertyState2 = CreateProperty(vehicleVariableType, "OptionalBoolProperty", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modeling rule has to be specified
                propertyState2.ModellingRuleId = Objects.ModellingRule_Optional;
                // this property will not be created for any instance of the variable type
                PropertyState propertyState3 = CreateProperty(vehicleVariableType, "Int32PropertyWithoutModellingRule", DataTypeIds.Int32);

                // create instance form new variable type
                var variable = CreateVariableFromType(m_rootCustomTypesFolder, "CustomVariableInstance", customVariableType.NodeId, ReferenceTypeIds.Organizes);
                variable.Description = "Variable instance of custom VariableType: CustomVariableType ";
                var vehicleVariable = CreateVariableFromType(m_rootCustomTypesFolder, "VehicleVariableInstance", vehicleVariableType.NodeId, ReferenceTypeIds.HasComponent);
                vehicleVariable.Description = "Variable instance of custom VariableType: VehicleVariableType ";
                #endregion

                #region  create custom Object types and instances
                BaseObjectTypeState customObjectType = CreateObjectType(ObjectTypeIds.BaseObjectType, "CustomObjectType", true);
                customObjectType.Description = "Custom abstract object Type with one mandatory property";
                PropertyState propertyState = CreateProperty(customObjectType, "MandatoryFloatProperty", DataTypeIds.Float);
                // for properties that need to be created on instances of type the modeling rule has to be specified
                propertyState.ModellingRuleId = Objects.ModellingRule_Mandatory;


                // Create a derived object type 
                BaseObjectTypeState parkingObjectType = CreateObjectType(customObjectType.NodeId, "ParkingObjectType", false);
                vehicleVariableType.Description = "Custom Object type with DataType=VehicleType";

                var folderVariable = CreateFolder(parkingObjectType, "Vehicles");
                // for properties that need to be created on instances of type the modeling rule has to be specified
                folderVariable.ModellingRuleId = Objects.ModellingRule_Mandatory;
                folderVariable.Description = "Folder that will contain all Vehicles associated with this parking lot";

                propertyState = CreateProperty(folderVariable, "<Vehicle>", vehicleType.NodeId, ValueRanks.Scalar);
                // for properties that need to be created on instances of type the modeling rule has to be specified
                propertyState.ModellingRuleId = Objects.ModellingRule_OptionalPlaceholder;

                propertyState = CreateProperty(parkingObjectType, "Address", DataTypeIds.String);
                // for properties that need to be created on instances of type the modeling rule has to be specified
                propertyState.ModellingRuleId = Objects.ModellingRule_Optional;

                // create a method and associate it with the object type
                Argument[] addVehicleInputArguments = new Argument[] 
                {
                    new Argument() { Name = "NewVehicle", DataType = vehicleType.NodeId, ValueRank = ValueRanks.Scalar, Description = "Vehicle data type instance to be added to Vehicles folder" },
                };
                Argument[] addVehicleOutputArguments = new Argument[] 
                {
                    new Argument() { Name = "NodeId", DataType = DataTypeIds.NodeId, ValueRank = ValueRanks.Scalar, Description ="New Vehicle NodeId" }
                };
                var addVehicleMethod = CreateMethod(parkingObjectType, "AddVehicle", addVehicleInputArguments, addVehicleOutputArguments);
                addVehicleMethod.ModellingRuleId = Objects.ModellingRule_Mandatory;

                // create instance form new object type
                var parkingLotInstance = CreateObjectFromType(m_rootCustomTypesFolder, "ParkingLotInstance", parkingObjectType.NodeId, ReferenceTypeIds.Organizes);
                parkingLotInstance.Description = "Object instance of custom ObjectType: ParkingObjectType ";
                MethodState addVehicleMethodInstance = parkingLotInstance.FindChild(SystemContext, addVehicleMethod.BrowseName) as MethodState;
                if (addVehicleMethodInstance != null)
                {
                    // Add event handler for object instance method call
                    addVehicleMethodInstance.OnCallMethod = ParkingLotAddVehicleOnCallHandler;
                }

                #endregion
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

        #region Private implementation
        /// <summary>
        /// Handler for AddVehicle method
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult ParkingLotAddVehicleOnCallHandler(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments != null && inputArguments.Count == 1)
            {
                ExtensionObject extensionObject = inputArguments[0] as ExtensionObject;
                if (extensionObject != null)
                {
                    StructuredValue vehicle = extensionObject.Body as StructuredValue;
                    if (method.Parent != null)
                    {
                        FolderState vehiclesFolder = method.Parent.FindChild(SystemContext, new QualifiedName("Vehicles", NamespaceIndex)) as FolderState;
                        // create new vehicle variable instance 
                        var vehicleVariable = CreateVariable(vehiclesFolder, vehicle["Name"] as String, m_vehicleDataTypeNodeId);
                        vehicleVariable.Description = "Variable instance added by AddVehicleMethod";
                        vehicleVariable.Value = vehicle;

                        // set output arguments
                        outputArguments[0] = vehicleVariable.NodeId;
                        return ServiceResult.Good;
                    }
                }
            }

            return new ServiceResult(StatusCodes.Bad);
        }
        #endregion
    }
}
