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

                var matrixFolder = CreateObjectFromType(m_rootCustomTypesFolder, "Matrix", ObjectTypeIds.FolderType, ReferenceTypeIds.Organizes) as FolderState;

                #region  Create custom DataType nodes and Variable instances
                // Create a custom  Enumeration type with EnumStrings
                EnumDefinition engineStateEnum = new EnumDefinition();
                engineStateEnum.Fields = new EnumFieldCollection()
                {
                    new EnumField() { Name = "Stopped", Value = 0},
                    new EnumField() { Name = "Running", Value = 1}
                };
                DataTypeState engineStateType = CreateDataType(DataTypeIds.Enumeration, "EngineStateType", engineStateEnum);

                // Create a custom OptionSet enumeration type. All EnumField values must be a power of 2
                EnumDefinition displayWarningEnum = new EnumDefinition();
                displayWarningEnum.Fields = new EnumFieldCollection()
                {
                    new EnumField() { Name = "ABS", Value = 1},
                    new EnumField() { Name = "ESP", Value = 2},
                    new EnumField() { Name = "TirePressure", Value = 4},
                    new EnumField() { Name = "CheckEngine", Value = 8},
                    new EnumField() { Name = "OpenDoor", Value = 16},
                };
                DataTypeState displayWarningType = CreateDataType(DataTypeIds.UInt16, "DisplayWarningType", displayWarningEnum);

                // Create a custom OptionSet structure type. All EnumField values must be a power of 2
                EnumDefinition featuresEnum = new EnumDefinition();
                featuresEnum.Fields = new EnumFieldCollection()
                {
                    new EnumField() { Name = "ABS", Value = 1},
                    new EnumField() { Name = "ESP", Value = 2},
                    new EnumField() { Name = "AirbagPassenger", Value = 4},
                    new EnumField() { Name = "AirbagSides", Value = 8},
                };
                DataTypeState featuresOptionSetType = CreateDataType(DataTypeIds.OptionSet, "FeaturesOptionSetType", featuresEnum);

                // Create a custom StructureWithOptionalFields type
                // StructureType property should be set to StructureWithOptionalFields
                StructureDefinition ownerStructure = new StructureDefinition();
                ownerStructure.StructureType = StructureType.StructureWithOptionalFields;
                ownerStructure.Fields = new StructureFieldCollection()
                {
                    new StructureField(){Name = "Name", DataType = DataTypeIds.String, IsOptional = false, ValueRank = ValueRanks.Scalar},
                    new StructureField(){Name = "Age", DataType = DataTypeIds.Byte, IsOptional = true, ValueRank = ValueRanks.Scalar},
                    new StructureField(){Name = "Details", DataType = DataTypeIds.String, IsOptional = true, ValueRank = ValueRanks.Scalar},
                };
                DataTypeState ownerType = CreateDataType(DataTypeIds.Structure, "OwnerDetailsType", ownerStructure);

                // Create a custom Union type
                // StructureType property should be set to Union
                StructureDefinition fuelLevelDetailsUnion = new StructureDefinition();
                fuelLevelDetailsUnion.StructureType = StructureType.Union;
                fuelLevelDetailsUnion.Fields = new StructureFieldCollection()
                {
                    new StructureField(){Name = "IsEmpty", DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar, IsOptional = false},
                    new StructureField(){Name = "IsFull", DataType = DataTypeIds.Boolean, ValueRank = ValueRanks.Scalar, IsOptional = false},
                    new StructureField(){Name = "Liters", DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar, IsOptional = false},
                };
                DataTypeState fuelLevelDetailsType = CreateDataType(DataTypeIds.Union, "FuelLevelDetailsType", fuelLevelDetailsUnion);

                // Create a custom Structure type
                // Make sure to set StructureField.IsOptional = false for all fields because default value is true
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
                // Set baseDataTypeId = DataTypeIds.Structure to define the type as subtype of Structure
                DataTypeState vehicleType = CreateDataType(DataTypeIds.Structure, "VehicleType", vehicleStructure);
                m_vehicleDataTypeNodeId = vehicleType.NodeId;

                // create custom structured value type derived from a custom type
                StructureDefinition vehicleWithExtraStructure = new StructureDefinition();
                vehicleWithExtraStructure.StructureType = StructureType.Structure;
                vehicleWithExtraStructure.Fields = new StructureFieldCollection()
                {
                    new StructureField(){Name = "Extra1", DataType = DataTypeIds.BaseDataType, IsOptional = false, ValueRank = ValueRanks.Scalar},
                };
                DataTypeState vehicleExtra1Type = CreateDataType(vehicleType.NodeId, "VehicleWithExtra1Type", vehicleWithExtraStructure);

                // create custom structured value type derived from a custom type with a structure definition that already contains base type fields
                vehicleStructure.Fields.AddRange(new StructureFieldCollection()
                {
                    new StructureField() { Name = "Extra2", DataType = DataTypeIds.BaseDataType, IsOptional = false, ValueRank = ValueRanks.Scalar },
                });
                DataTypeState vehicleExtra2Type = CreateDataType(vehicleType.NodeId, "VehicleWithExtra2Type", vehicleStructure);

                // Create Variable node instances for defined DataTypes
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
                var vehicleVariable = CreateVariable(m_rootCustomTypesFolder, "Vehicle", vehicleType.NodeId);
                vehicleVariable.Description = "Variable with data type defined as StructuredValue";
                StructuredValue vehicle = vehicleVariable.Value as StructuredValue;
                if (vehicle != null)
                {
                    // Set the Name field of the structure
                    // For this you need to know in advance the exact name and type of the field from the type definition
                    vehicle["Name"] = "BMW";
                }
                var vehicleExtra1Variable = CreateVariable(m_rootCustomTypesFolder, "VehicleExtra1", vehicleExtra1Type.NodeId);
                vehicleExtra1Variable.Description = "Variable with data type defined as StructuredValue and derived from custom type";

                var vehicleExtra2Variable = CreateVariable(m_rootCustomTypesFolder, "VehicleExtra2", vehicleExtra2Type.NodeId);
                vehicleExtra2Variable.Description = "Variable with data type defined as StructuredValue and derived from custom type";

                // Create Array variable nodes for defined DataTypes
                var engineStateArrayVariable = CreateVariable(m_arraysFolder, "EngineStates", engineStateType.NodeId, ValueRanks.OneDimension);
                var displayWarningArrayVariable = CreateVariable(m_arraysFolder, "DisplayWarnings", displayWarningType.NodeId, ValueRanks.OneDimension);
                var featuresOptionSetArrayVariable = CreateVariable(m_arraysFolder, "FeaturesOptionSets", featuresOptionSetType.NodeId, ValueRanks.OneDimension);
                var ownerArrayVariable = CreateVariable(m_arraysFolder, "Owners", ownerType.NodeId, ValueRanks.OneDimension);
                var fuelLevelArrayVariable = CreateVariable(m_arraysFolder, "FuelLevels", fuelLevelDetailsType.NodeId, ValueRanks.OneDimension);
                var vehicleArrayVariable = CreateVariable(m_arraysFolder, "Vehicles", vehicleType.NodeId, ValueRanks.OneDimension);

                // Create Matrix variable nodes for defined DataTypes
                var engineStateMatrixVariable = CreateVariable(matrixFolder, "EngineStates", engineStateType.NodeId, ValueRanks.OneOrMoreDimensions);
                engineStateMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                Array elements = GetDefaultValueForDatatype(engineStateType.NodeId, ValueRanks.OneDimension, 6) as Array;
                TypeInfo sanityCheck = TypeInfo.Construct(elements);
                engineStateMatrixVariable.Value = new Matrix(elements, sanityCheck.BuiltInType, 1, 2, 3 );
                var displayWarningMatrixVariable = CreateVariable(matrixFolder, "DisplayWarnings", displayWarningType.NodeId, ValueRanks.OneOrMoreDimensions);
                displayWarningMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                elements = GetDefaultValueForDatatype(displayWarningType.NodeId, ValueRanks.OneDimension, 6) as Array;
                sanityCheck = TypeInfo.Construct(elements);
                displayWarningMatrixVariable.Value = new Matrix(elements, sanityCheck.BuiltInType, 1, 2, 3);
                var featuresOptionSetMatrixVariable = CreateVariable(matrixFolder, "FeaturesOptionSets", featuresOptionSetType.NodeId, ValueRanks.OneOrMoreDimensions);
                featuresOptionSetMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                elements = GetDefaultValueForDatatype(featuresOptionSetType.NodeId, ValueRanks.OneDimension, 6) as Array;
                featuresOptionSetMatrixVariable.Value = new Matrix(elements, BuiltInType.Variant, 1, 2, 3);
                var ownerMatrixVariable = CreateVariable(matrixFolder, "Owners", ownerType.NodeId, ValueRanks.OneOrMoreDimensions);
                ownerMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                elements = GetDefaultValueForDatatype(ownerType.NodeId, ValueRanks.OneDimension, 6) as Array;
                ownerMatrixVariable.Value = new Matrix(elements, BuiltInType.Variant, 1, 2, 3);
                var fuelLevelMatrixVariable = CreateVariable(matrixFolder, "FuelLevels", fuelLevelDetailsType.NodeId, ValueRanks.OneOrMoreDimensions);
                fuelLevelMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                elements = GetDefaultValueForDatatype(fuelLevelDetailsType.NodeId, ValueRanks.OneDimension, 6) as Array;
                fuelLevelMatrixVariable.Value = new Matrix(elements, BuiltInType.Variant, 1, 2, 3);
                var vehicleMatrixVariable = CreateVariable(matrixFolder, "Vehicles", vehicleType.NodeId, ValueRanks.OneOrMoreDimensions);
                vehicleMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                elements = GetDefaultValueForDatatype(vehicleType.NodeId, ValueRanks.OneDimension, 6) as Array;
                vehicleMatrixVariable.Value = new Matrix(elements, BuiltInType.Variant, 1, 2, 3);

                #endregion

                #region Create custom VariableType nodes and Variable instances
                BaseVariableTypeState customVariableType = CreateVariableType(VariableTypeIds.BaseDataVariableType, "CustomVariableType", DataTypeIds.UInt32);
                // Define the structure of the CustomVariableType definition
                PropertyState propertyStateBase1 = CreateProperty(customVariableType, "MandatoryBoolBaseProperty", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyStateBase1.ModellingRuleId = Objects.ModellingRule_Mandatory;

                PropertyState propertyStateBase2 = CreateProperty(customVariableType, "OptionalBoolBaseProperty", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyStateBase2.ModellingRuleId = Objects.ModellingRule_Optional;

                PropertyState propertyStateBase3 = CreateProperty(customVariableType, "BoolPropertyToOverride", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyStateBase3.ModellingRuleId = Objects.ModellingRule_Optional;

                // Create a VariableType node that has DataType = VehicleType complex type
                BaseVariableTypeState vehicleVariableType = CreateVariableType(customVariableType.NodeId, "VehicleVariableType", vehicleType.NodeId, ValueRanks.Scalar);
                vehicleVariableType.Description = "Custom Variable type with DataType=VehicleType";
                // Set variable type default value
                StructuredValue vehicleDefault = GetDefaultValueForDatatype(vehicleType.NodeId) as StructuredValue;
                vehicleVariableType.Value = vehicleDefault;

                // override a property from base type
                PropertyState propertyState0 = CreateProperty(vehicleVariableType, "BoolPropertyToOverride", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyState0.ModellingRuleId = Objects.ModellingRule_Mandatory;

                // Define the structure of the VariableType definition
                PropertyState propertyState1 = CreateProperty(vehicleVariableType, "MandatoryBoolProperty", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyState1.ModellingRuleId = Objects.ModellingRule_Mandatory;

                PropertyState propertyState2 = CreateProperty(vehicleVariableType, "OptionalBoolProperty", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyState2.ModellingRuleId = Objects.ModellingRule_Optional;
                // this property will not be created for any instance of the variable type because it has no moddeling rule specified
                // it can be used to expose some type definition specific information
                PropertyState propertyState3 = CreateProperty(vehicleVariableType, "Int32PropertyWithoutModellingRule", DataTypeIds.Int32);

                // Create Variable node instances for defined VariableTypes
                var variable = CreateVariableFromType(m_rootCustomTypesFolder, "CustomVariableInstance", customVariableType.NodeId, ReferenceTypeIds.Organizes);
                variable.Description = "Variable instance of custom VariableType: CustomVariableType ";
                var vehicleVariableInstance = CreateVariableFromType(m_rootCustomTypesFolder, "VehicleVariableInstance", vehicleVariableType.NodeId, ReferenceTypeIds.HasComponent);
                vehicleVariableInstance.Description = "Variable instance of custom VariableType: VehicleVariableType ";
                #endregion

                #region Create custom ObjectType nodes and Object instances
                // Create an abstract ObjectType derived from BaseObjectType
                BaseObjectTypeState customObjectType = CreateObjectType(ObjectTypeIds.BaseObjectType, "CustomObjectType", true);
                customObjectType.Description = "Custom abstract object Type with one mandatory property";
                PropertyState propertyState = CreateProperty(customObjectType, "MandatoryFloatProperty", DataTypeIds.Float);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyState.ModellingRuleId = Objects.ModellingRule_Mandatory;

                // Create an ObjectType derived from CustomObjectType
                BaseObjectTypeState parkingObjectType = CreateObjectType(customObjectType.NodeId, "ParkingObjectType", false);
                vehicleVariableType.Description = "Custom Object type with DataType=VehicleType";

                // Define the structure of the ObjectType definition
                var folderVariable = CreateFolder(parkingObjectType, "Vehicles");
                // for properties that need to be created on instances of type the modelling rule has to be specified
                folderVariable.ModellingRuleId = Objects.ModellingRule_Mandatory;
                folderVariable.Description = "Folder that will contain all Vehicles associated with this parking lot";

                propertyState = CreateProperty(folderVariable, "<Vehicle>", vehicleType.NodeId, ValueRanks.Scalar);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyState.ModellingRuleId = Objects.ModellingRule_OptionalPlaceholder;

                propertyState = CreateProperty(parkingObjectType, "Address", DataTypeIds.String);
                // for properties that need to be created on instances of type the modelling rule has to be specified
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

                // Create Object node instance for defined ObjectType
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
                        // create new Vehicle variable instance inside Vehicles folders
                        var vehicleVariable = CreateVariable(vehiclesFolder, vehicle["Name"] as String, m_vehicleDataTypeNodeId);
                        vehicleVariable.Description = "Variable instance added by AddVehicleMethod";
                        vehicleVariable.Value = vehicle;

                        // set output arguments as the NodeId of the created Vehicle variable
                        outputArguments[0] = vehicleVariable.NodeId;
                        return ServiceResult.Good;
                    }
                }
            }

            return new ServiceResult(StatusCodes.BadInvalidArgument);
        }
        #endregion
    }
}
