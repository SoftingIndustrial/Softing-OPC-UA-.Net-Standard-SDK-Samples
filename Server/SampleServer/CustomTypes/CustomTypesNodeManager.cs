/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/


using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;
using Softing.Opc.Ua.Server.Types;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private BaseEventState m_customEventInstance;

        private NodeId m_vehicleDataTypeNodeId;
        private NodeId m_customEventTypeNodeId;
        private int m_customEventCounter = 0;
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

                // Add SubscribeToEvents EventNotifier
                m_rootCustomTypesFolder.EventNotifier =  EventNotifiers.SubscribeToEvents;
                AddRootNotifier(m_rootCustomTypesFolder);

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
                var engineStateVariable = CreateVariableWithValue(m_rootCustomTypesFolder, "EngineState", engineStateType.NodeId);
                engineStateVariable.Description = "Variable with data type defined as custom Enumeration";
                var displayWarningVariable = CreateVariableWithValue(m_rootCustomTypesFolder, "DisplayWarning", displayWarningType.NodeId);
                displayWarningVariable.Description = "Variable with data type defined as custom OptionSet Enumeration";
                var featuresOptionSetVariable = CreateVariableWithValue(m_rootCustomTypesFolder, "FeaturesOptionSet", featuresOptionSetType.NodeId);
                featuresOptionSetVariable.Description = "Variable with data type defined as custom OptionSet";
                var ownerVariable = CreateVariableWithValue(m_rootCustomTypesFolder, "Owner", ownerType.NodeId);
                ownerVariable.Description = "Variable with data type defined as StructuredValue with optional fields";
                var fuelLevelVariable = CreateVariableWithValue(m_rootCustomTypesFolder, "FuelLevel", fuelLevelDetailsType.NodeId);
                fuelLevelVariable.Description = "Variable with data type defined as Union";
                var vehicleVariable = CreateVariableWithValue(m_rootCustomTypesFolder, "Vehicle", vehicleType.NodeId);
                vehicleVariable.Description = "Variable with data type defined as StructuredValue";
                StructuredValue vehicle = vehicleVariable.Value as StructuredValue;
                if (vehicle != null)
                {
                    // Set the Name field of the structure
                    // For this you need to know in advance the exact name and type of the field from the type definition
                    vehicle["Name"] = "BMW";
                }
                var vehicleExtra1Variable = CreateVariableWithValue(m_rootCustomTypesFolder, "VehicleExtra1", vehicleExtra1Type.NodeId);
                vehicleExtra1Variable.Description = "Variable with data type defined as StructuredValue and derived from custom type";

                var vehicleExtra2Variable = CreateVariableWithValue(m_rootCustomTypesFolder, "VehicleExtra2", vehicleExtra2Type.NodeId);
                vehicleExtra2Variable.Description = "Variable with data type defined as StructuredValue and derived from custom type";

                // Create Array variable nodes for defined DataTypes
                var engineStateArrayVariable = CreateVariableWithValue(m_arraysFolder, "EngineStates", engineStateType.NodeId, ValueRanks.OneDimension);
                var displayWarningArrayVariable = CreateVariableWithValue(m_arraysFolder, "DisplayWarnings", displayWarningType.NodeId, ValueRanks.OneDimension);
                var featuresOptionSetArrayVariable = CreateVariableWithValue(m_arraysFolder, "FeaturesOptionSets", featuresOptionSetType.NodeId, ValueRanks.OneDimension);
                var ownerArrayVariable = CreateVariableWithValue(m_arraysFolder, "Owners", ownerType.NodeId, ValueRanks.OneDimension);
                var fuelLevelArrayVariable = CreateVariableWithValue(m_arraysFolder, "FuelLevels", fuelLevelDetailsType.NodeId, ValueRanks.OneDimension);
                var vehicleArrayVariable = CreateVariableWithValue(m_arraysFolder, "Vehicles", vehicleType.NodeId, ValueRanks.OneDimension);

                // Create Matrix variable nodes for defined DataTypes
                var engineStateMatrixVariable = CreateVariable(matrixFolder, "EngineStates", engineStateType.NodeId, ValueRanks.OneOrMoreDimensions);
                engineStateMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };  
                // create matrix value
                engineStateMatrixVariable.Value = GetDefaultValueForDatatype(engineStateType.NodeId, ValueRanks.OneOrMoreDimensions, 0, new int[] { 1, 2, 3 });
                
                var displayWarningMatrixVariable = CreateVariable(matrixFolder, "DisplayWarnings", displayWarningType.NodeId, ValueRanks.OneOrMoreDimensions);
                displayWarningMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                // create matrix value
                displayWarningMatrixVariable.Value = GetDefaultValueForDatatype(displayWarningType.NodeId, ValueRanks.OneOrMoreDimensions, 0, new int[] { 1, 2, 3 });
                
                var featuresOptionSetMatrixVariable = CreateVariable(matrixFolder, "FeaturesOptionSets", featuresOptionSetType.NodeId, ValueRanks.OneOrMoreDimensions);
                featuresOptionSetMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                // create matrix value
                featuresOptionSetMatrixVariable.Value = GetDefaultValueForDatatype(featuresOptionSetType.NodeId, ValueRanks.OneOrMoreDimensions, 0, new int[] { 1, 2, 3 });
                
                var ownerMatrixVariable = CreateVariable(matrixFolder, "Owners", ownerType.NodeId, ValueRanks.OneOrMoreDimensions);
                ownerMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                // create matrix value
                ownerMatrixVariable.Value = GetDefaultValueForDatatype(ownerType.NodeId, ValueRanks.OneOrMoreDimensions, 0, new int[] { 1, 2, 3 });
                
                var fuelLevelMatrixVariable = CreateVariable(matrixFolder, "FuelLevels", fuelLevelDetailsType.NodeId, ValueRanks.OneOrMoreDimensions);
                fuelLevelMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };
                // create matrix value
                fuelLevelMatrixVariable.Value = GetDefaultValueForDatatype(fuelLevelDetailsType.NodeId, ValueRanks.OneOrMoreDimensions, 0, new int[] { 1, 2, 3 });

                var vehicleMatrixVariable = CreateVariable(matrixFolder, "Vehicles", vehicleType.NodeId, ValueRanks.OneOrMoreDimensions);
                vehicleMatrixVariable.ArrayDimensions = new uint[] { 1, 2, 3 };               
                // create matrix value
                vehicleMatrixVariable.Value = GetDefaultValueForDatatype(vehicleType.NodeId, ValueRanks.OneOrMoreDimensions, 0, new int[] { 1, 2, 3 });

                #region create custom data type that has fileds with various ValueRanks 
                // define the StructureDefinition
                StructureDefinition simpleStructure = new StructureDefinition();
                simpleStructure.Fields = new StructureFieldCollection() {
                    new StructureField() { Name = "EngineStateScalar", DataType = engineStateType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
                    new StructureField() { Name = "EngineStateOneDimension", DataType = engineStateType.NodeId, IsOptional = false, ValueRank = ValueRanks.OneDimension,
                            ArrayDimensions = new UInt32Collection(){5 } },
                    new StructureField() { Name = "EngineState2D", DataType = engineStateType.NodeId, IsOptional = false, ValueRank = ValueRanks.TwoDimensions,
                            ArrayDimensions = new UInt32Collection(){5, 5 } },
                    new StructureField() { Name = "FeaturesOptionSetScalar", DataType = featuresOptionSetType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
                    new StructureField() { Name = "FeaturesOptionSetOneDimension", DataType = featuresOptionSetType.NodeId, IsOptional = false, ValueRank = ValueRanks.OneDimension,
                            ArrayDimensions = new UInt32Collection(){5 } },
                    new StructureField() { Name = "FeaturesOptionSet2D", DataType = featuresOptionSetType.NodeId, IsOptional = false, ValueRank = ValueRanks.TwoDimensions,
                            ArrayDimensions = new UInt32Collection(){5, 5 } },
                    new StructureField() { Name = "VehicleScalar", DataType = vehicleType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
                    new StructureField() { Name = "VehicleOneDimension", DataType = vehicleType.NodeId, IsOptional = false, ValueRank = ValueRanks.OneDimension,
                            ArrayDimensions = new UInt32Collection(){5 } },
                    new StructureField() { Name = "Vehicle2D", DataType = vehicleType.NodeId, IsOptional = false, ValueRank = ValueRanks.TwoDimensions,
                            ArrayDimensions = new UInt32Collection(){5, 5 } },                   
                    new StructureField() { Name = "OwnerDetailsScalar", DataType = ownerType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
                    new StructureField() { Name = "OwnerDetailsOneDimension", DataType = ownerType.NodeId, IsOptional = false, ValueRank = ValueRanks.OneDimension,
                            ArrayDimensions = new UInt32Collection(){5 } },
                    new StructureField() { Name = "OwnerDetails2D", DataType = ownerType.NodeId, IsOptional = false, ValueRank = ValueRanks.TwoDimensions,
                            ArrayDimensions = new UInt32Collection(){5, 5 } },
                    new StructureField() { Name = "FuelLevelScalar", DataType = fuelLevelDetailsType.NodeId, IsOptional = false, ValueRank = ValueRanks.Scalar},
                    new StructureField() { Name = "FuelLevelOneDimension", DataType = fuelLevelDetailsType.NodeId, IsOptional = false, ValueRank = ValueRanks.OneDimension,
                            ArrayDimensions = new UInt32Collection(){5 } },
                    new StructureField() { Name = "FuelLevel2D", DataType = fuelLevelDetailsType.NodeId, IsOptional = false, ValueRank = ValueRanks.TwoDimensions,
                            ArrayDimensions = new UInt32Collection(){5, 5 } },

                };
                
                // create the data type
                var structureWithValueRanksType = CreateDataType(DataTypeIds.Structure, "StructureWithValueRanksType", simpleStructure);
                
                // create an instance of new data type
                StructuredValue simpleStructureValue = GetDefaultValueForDatatype(structureWithValueRanksType.NodeId) as StructuredValue;
                if (simpleStructureValue != null)
                {               
                    EnumValue[] enumValues = (EnumValue[])simpleStructureValue["EngineStateOneDimension"];
                    for (int i = 0; i < enumValues.Length; i++)
                    {
                        int intvalue = i % enumValues[i].ValueStrings.Count;
                        enumValues[i].ValueString = enumValues[i].ValueStrings[intvalue];
                    }

                    enumValues = ((Matrix)simpleStructureValue["EngineState2D"]).Elements as EnumValue[];
                    for (int i = 0; i < enumValues.Length; i++)
                    {
                        int intvalue = i % enumValues[i].ValueStrings.Count;
                        enumValues[i].ValueString = enumValues[i].ValueStrings[intvalue];
                    }

                    // set field values for the StructuredValue object
                    ((UnionStructuredValue)simpleStructureValue["FuelLevelScalar"]).SwitchFieldPosition = 1;
                    ((UnionStructuredValue)simpleStructureValue["FuelLevelScalar"]).Fields[0].Value = false;

                    UnionStructuredValue[] unionStructuredValues = (UnionStructuredValue[])simpleStructureValue["FuelLevelOneDimension"];
                    for (int i = 0; i < unionStructuredValues.Length; i++)
                    {
                        uint position = (uint)(i % unionStructuredValues[i].Fields.Count);
                        unionStructuredValues[i].SwitchFieldPosition = position + 1;
                        unionStructuredValues[i].Fields[(int)position].Value = Convert.ChangeType(i, unionStructuredValues[i].Fields[(int)position].Value.GetType());
                    }

                    unionStructuredValues = ((Matrix)simpleStructureValue["FuelLevel2D"]).Elements as UnionStructuredValue[];
                    for (int i = 0; i < unionStructuredValues.Length; i++)
                    {
                        uint position = (uint)(i % unionStructuredValues[i].Fields.Count);
                        unionStructuredValues[i].SwitchFieldPosition = position + 1;
                        unionStructuredValues[i].Fields[(int)position].Value = Convert.ChangeType(i, unionStructuredValues[i].Fields[(int)position].Value.GetType());
                    }

                }

                // create a variable with the new data type
                BaseDataVariableState simpleStructureVariable = CreateVariable(m_rootCustomTypesFolder, "StructureWithValueRanks", structureWithValueRanksType.NodeId);

                // set the Value of the variable ty4o the StructureValue that was created and modified
                simpleStructureVariable.Value = simpleStructureValue;
                #endregion
                #endregion

                #region Create custom VariableType nodes and Variable instances
                BaseVariableTypeState customVariableType = CreateVariableType(VariableTypeIds.BaseDataVariableType, "CustomVariableType", DataTypeIds.UInt32);
                // Define the structure of the CustomVariableType definition
                PropertyState propertyStateBase1 = CreateProperty(customVariableType, "MandatoryBoolBaseProperty", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyStateBase1.ModellingRuleId = Objects.ModellingRule_Mandatory;
                propertyStateBase1.Value = true;

                PropertyState propertyStateBase2 = CreateProperty(customVariableType, "OptionalBoolBaseProperty", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyStateBase2.ModellingRuleId = Objects.ModellingRule_Optional;
                propertyStateBase2.Value = false;

                PropertyState propertyStateBase3 = CreateProperty(customVariableType, "BoolPropertyToOverride", DataTypeIds.Boolean);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyStateBase3.ModellingRuleId = Objects.ModellingRule_Optional;
                propertyStateBase3.Value = false;

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
                variable.Value = (uint)0;

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

                // create custom event type
                BaseObjectTypeState customEventType = CreateObjectType(ObjectTypeIds.BaseEventType, "CustomEventType", false);
                //remember CustomEventType id
                m_customEventTypeNodeId = customEventType.NodeId;
                customEventType.Description = "Custom EventType with some custom properties";

                propertyState = CreateProperty(customEventType, "CountProperty", DataTypeIds.Int32);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyState.ModellingRuleId = Objects.ModellingRule_Mandatory;

                propertyState = CreateProperty(customEventType, "RandomValueProperty", DataTypeIds.Int32);
                // for properties that need to be created on instances of type the modelling rule has to be specified
                propertyState.ModellingRuleId = Objects.ModellingRule_Mandatory;

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

                // create method for raising custom event of d\custom defined type
                MethodState raiseCustomEventMethodInstance = CreateMethod(m_rootCustomTypesFolder, "RaiseCustomEvent", null, null, RaiseCustomEventOnCallHandler);

                // create an instance of a custom event type
                m_customEventInstance = CreateObjectFromType(m_rootCustomTypesFolder, "CustomEventInstance", m_customEventTypeNodeId) as BaseEventState;

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
        /// Create a varable state node and set its value to defautk value dfor data type
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        private BaseVariableState CreateVariableWithValue(NodeState parent, string name, NodeId dataType, int valueRank = ValueRanks.Scalar )
        {
            var variable = CreateVariable(parent, name, dataType, valueRank);
            variable.Value = GetDefaultValueForDatatype(dataType, valueRank);

            return variable;
        }
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
            StatusCode result = StatusCodes.BadInvalidArgument;
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
                        result = StatusCodes.Good;
                    }
                }
            }

            // report update method audit event
            Server.ReportAuditUpdateMethodEvent(context, method.Parent.NodeId, method.NodeId, inputArguments?.ToArray(), "Execute AddVehicle method", result);
            return new ServiceResult(result);

        }

        /// <summary>
        /// Hander for RaiseCustomEvent method
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        /// <returns></returns>
        private ServiceResult RaiseCustomEventOnCallHandler(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (m_customEventInstance != null)
            {
                // Set custom properties
                var countProperty = m_customEventInstance.FindChildBySymbolicName(SystemContext, "CountProperty") as BaseVariableState;
                if (countProperty != null)
                {
                    countProperty.Value = ++m_customEventCounter;
                }
                var randomProperty = m_customEventInstance.FindChildBySymbolicName(SystemContext, "RandomValueProperty") as BaseVariableState;
                if (randomProperty != null)
                {
                    randomProperty.Value = new Random(100).Next();
                }

                LocalizedText eventMessage = new LocalizedText("CustomEvent" + m_customEventCounter);
                ReportEvent(m_rootCustomTypesFolder, m_customEventInstance, eventMessage, EventSeverity.Medium);

                return new ServiceResult(StatusCodes.Good);
            }

            return new ServiceResult(StatusCodes.BadNodeIdInvalid);
        }

        #endregion
    }
}
