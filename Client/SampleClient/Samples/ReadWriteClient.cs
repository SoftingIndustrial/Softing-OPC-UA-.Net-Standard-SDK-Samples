/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Opc.Ua;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.Nodes;
using Softing.Opc.Ua.Client.Types;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class that contains sample code for read/write functionality
    /// </summary>
    class ReadWriteClient
    {
        #region Private Fields
        private const string SessionName = "ReadWriteClient Session";
        private readonly UaApplication m_application;
        private ClientSession m_session;
        private readonly Random m_random = new Random();

        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\Int32
        const string StaticInt32NodeId = "ns=7;s=CTT_Scalar_Scalar_Static_Int32";
        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\UInt32
        const string StaticUInt32NodeId = "ns=7;s=CTT_Scalar_Scalar_Static_UInt32";
        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\Guid
        const string StaticGuidNodeId = "ns=7;s=CTT_Scalar_Scalar_Static_Guid";
        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\DateTime 
        const string StaticDateTimeNodeId = "ns=7;s=CTT_Scalar_Scalar_Static_DateTime";

        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\Arrays\Int64
        const string StaticInt64ArrayNodeId = "ns=7;s=CTT_Scalar_Scalar_Static_Arrays_Int64";
        //Browse path: Root\Objects\CTT\StructuredTypeVariables\EnumerationType1Variable
        const string StaticEnumNodeId = "ns=7;i=15014";
        //Browse path: Root\Objects\CTT\StructuredTypeVariables\DataType5Variable
         const string StaticComplexNodeId = "ns=7;i=15013";

        //Browse path: Root\Objects\CustomTypes\EngineState
        const string StaticCustomEnumerationNodeId = "ns=11;i=27";
        //Browse path: Root\Objects\CustomTypes\Arrays\EngineStates
        const string StaticCustomEnumerationArrayNodeId = "ns=11;i=35";

        //Browse path: Root\Objects\CustomTypes\DisplayWarning
        const string StaticCustomOptionSetEnumerationNodeId = "ns=11;i=28";
        //Browse path: Root\Objects\CustomTypes\Arrays\DisplayWarnings
        const string StaticCustomOptionSetEnumerationArrayNodeId = "ns=11;i=36";

        //Browse path: Root\Objects\CustomTypes\FeaturesOptionSet
        const string StaticCustomOptionSetNodeId = "ns=11;i=29";
        //Browse path: Root\Objects\CustomTypes\Arrays\FeaturesOptionSets
        const string StaticCustomOptionSetArrayNodeId = "ns=11;i=37";

        //Browse path: Root\Objects\CustomTypes\Owner
        const string StaticCustomStructureWithOptionalFieldsNodeId = "ns=11;i=30";
        //Browse path: Root\Objects\CustomTypes\Arrays\Owners
        const string StaticCustomStructureWithOptionalFieldsArrayNodeId = "ns=11;i=38";

        //Browse path: Root\Objects\CustomTypes\FuelLevel
        const string StaticCustomUnionNodeId = "ns=11;i=31";
        //Browse path: Root\Objects\CustomTypes\Arrays\FuelLevels
        const string StaticCustomUnionArrayNodeId = "ns=11;i=39";

        //Browse path: Root\Objects\CustomTypes\Vehicle
        const string StaticCustomStructuredValueNodeId = "ns=11;i=32";
        //Browse path: Root\Objects\CustomTypes\Arrays\Vehicles
        const string StaticCustomStructuredValueArrayNodeId = "ns=11;i=40";

        //Browse path: Root\Objects\DataAccess\NodesForRegister\Node0
        const string RegisterNodeId0 = "ns=3;i=49";
        //Browse path: Root\Objects\DataAccess\NodesForRegister\Node1
        const string RegisterNodeId1 = "ns=3;i=50";
        //Browse path: Root\Objects\DataAccess\NodesForRegister\Node2
        const string RegisterNodeId2 = "ns=3;i=51";
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of ReadWriteClient
        /// </summary>
        /// <param name="application"></param>
        public ReadWriteClient(UaApplication application)
        {
            m_application = application;
        }
        #endregion

        #region Public Methods - Read
        /// <summary>
        /// Reads a variable node with all its attributes.
        /// </summary>
        public void ReadVariableNode()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadVariableNode: The session is not initialized!");
                return;
            }
           
            try
            {
                NodeId nodeId = new NodeId(StaticUInt32NodeId);
                BaseNode baseNode = m_session.ReadNode(nodeId);
                if (baseNode == null)
                {
                    Console.WriteLine("\n The NodeId:{0} does not exist in the Address Space", StaticUInt32NodeId);
                    return;
                }
                Console.WriteLine("\n Read node with NodeId({0}):", StaticUInt32NodeId);
                Console.WriteLine("  DisplayName Name is '{0}'", baseNode.DisplayName.Text);
                Console.WriteLine("  Browse Name is '{0}'", baseNode.BrowseName.Name);
                Console.WriteLine("  Description is '{0}'", baseNode.Description.Text);
                Console.WriteLine("  NodeClass is '{0}'", baseNode.NodeClass);
                if (baseNode.NodeClass == NodeClass.Variable)
                {
                    VariableNodeEx variableNode = baseNode as VariableNodeEx;
                    if (variableNode != null)
                    {
                        Console.WriteLine("  DataType is  {0}", variableNode.DataType);
                        Console.WriteLine("  Value Rank is  {0}", variableNode.ValueRank);
                        DisplayInformationForDataValue(variableNode.Value);
                        Console.WriteLine("  Value is  {0}", variableNode.UserAccessLevelText);
                        Console.WriteLine("  Value is Historizing: {0}", variableNode.Historizing);
                        Console.WriteLine("  Value sampling interval: {0}", variableNode.MinimumSamplingIntervalText);
                    }
                }
            }            
            catch (Exception ex)
            {
                Program.PrintException("ReadVariableNode", ex);
            }
        }

        /// <summary>
        /// Reads an object node with all its attributes.
        /// </summary>
        public void ReadObjectNode()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadObjectNode: The session is not initialized!");
                return;
            }

            try
            {
                //Browse path: Root\Objects\Server
                NodeId nodeId = ObjectIds.Server;
                BaseNode baseNode = m_session.ReadNode(nodeId);
                if (baseNode == null)
                {
                    Console.WriteLine("\n The NodeId:{0} does not exist in the Address Space", nodeId);
                    return;
                }
                Console.WriteLine("\n Read node with NodeId({0}):", nodeId);
                Console.WriteLine("  DisplayName Name is '{0}'", baseNode.DisplayName.Text);
                Console.WriteLine("  Browse Name is '{0}'", baseNode.BrowseName.Name);
                Console.WriteLine("  Description is '{0}'", baseNode.Description.Text);
                Console.WriteLine("  NodeClass is '{0}'", baseNode.NodeClass);
                if (baseNode.NodeClass == NodeClass.Object)
                {
                    ObjectNodeEx variableNode = baseNode as ObjectNodeEx;
                    if (variableNode != null)
                    {
                        Console.WriteLine("  EventNotifier :  {0}", variableNode.EventNotifierText);
                    }
                }
            }            
            catch (Exception ex)
            {
                Program.PrintException("ReadObjectNode", ex);
            }
        }

        /// <summary>
        /// Reads value for an uint node providing the NodeID without reading the whole node information.
        /// </summary>
        public void ReadValueForNode ()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadSimpleNodeValue: The session is not initialized!");
                return;
            }

            ReadValueId readValueId = new ReadValueId();
            readValueId.NodeId = new NodeId(StaticUInt32NodeId);
            readValueId.AttributeId = Attributes.Value;

            Console.WriteLine("\n Read value for NodeId:{0}", StaticUInt32NodeId);
            try
            {
                DataValueEx dataValue = m_session.Read(readValueId);
                DisplayInformationForDataValue(dataValue);
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadValueForNode", ex);
            }
        }

        /// <summary>
        ///  Reads value for an array node providing the NodeID without reading the whole node information.
        /// </summary>
        public void ReadArrayValue()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadArrayValue: The session is not initialized!");
                return;
            }

            ReadValueId readValueId = new ReadValueId();
            readValueId.NodeId = new NodeId(StaticInt64ArrayNodeId);
            readValueId.AttributeId = Attributes.Value;

            Console.WriteLine("\n Read array value for NodeId:{0}", StaticInt64ArrayNodeId);
            try
            {
                DataValueEx dataValue = m_session.Read(readValueId);

                //display read information
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                Array array = dataValue.Value as Array;
                if (array != null)
                {
                    Console.WriteLine("  Value is an array with values:");
                    foreach (object obj in array)
                    {
                        Console.WriteLine("   {0}", obj);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadArrayValue", ex);
            }
        }

        /// <summary>
        ///  Reads value for a complex node providing the NodeID and without read the whole node information.
        /// </summary>
        public void ReadComplexValue()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadComplexValue: The session is not initialized!");
                return;
            }
            ReadValueId readValueId = new ReadValueId();
            readValueId.NodeId = new NodeId(StaticComplexNodeId); 
            readValueId.AttributeId = Attributes.Value;

            Console.WriteLine("\n Read complex value for NodeId:{0}", StaticComplexNodeId);
            try
            {
                DataValueEx dataValue = m_session.Read(readValueId);

                //display information for read value
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                if (dataValue.ProcessedValue == null)
                {
                    Console.WriteLine(" 'Structured Value' is null ");
                }
                else
                {
                    StructuredValue complexData = dataValue.ProcessedValue as StructuredValue;
                    if (complexData != null)
                    {
                        Console.WriteLine("  Value is 'Structured Value' with fields: ");
                        foreach (StructuredField field in complexData.Fields)
                        {
                            Console.WriteLine("   Field: {0} Value:{1} Type:{2} ", field.Name, complexData[field.Name] == null ? "<null>" : complexData[field.Name],
                                complexData[field.Name] == null ? "N/A" : complexData[field.Name].GetType().Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadComplexValue", ex);
            }
        }

        /// <summary>
        ///  Reads value for an enum node providing the NodeID and without read the whole node information.
        /// </summary>
        public void ReadEnumValue()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadEnumValue: The session is not initialized!");
                return;
            }
            Console.WriteLine("\n Read enum value for NodeId:{0}", StaticEnumNodeId);
            NodeId nodeId =  new NodeId(StaticEnumNodeId);
            try
            {
                // we need the data type id and this is why we read node
                BaseNode baseNode = m_session.ReadNode(nodeId);
                
                if (baseNode.NodeClass == NodeClass.Variable)
                {
                    VariableNodeEx variableNode = baseNode as VariableNodeEx;
                    ReadValueId readValueId = new ReadValueId();
                    if (variableNode != null)
                    {
                        readValueId.NodeId = variableNode.NodeId;
                        readValueId.AttributeId = Attributes.Value;

                        Console.WriteLine("\n Read enum value for Node: {0} (NodeId:{1})", variableNode.DisplayName, StaticEnumNodeId);

                        DataValueEx dataValue = m_session.Read(readValueId);
                        // attempt to convert the integer value read from node to an EnumValue instance
                        dataValue.TryConvertToEnumValue(m_session, variableNode.DataTypeId, variableNode.ValueRank);

                        //display information for read value
                        Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                        EnumValue enumValue = dataValue.ProcessedValue as EnumValue;
                        if (enumValue != null)
                        {
                            Console.WriteLine("  Value is an enum with value: {0}", enumValue.ValueString);
                            Console.WriteLine("  All possible values for this Enum are:");
                            List<string> allPossibleVals = new List<string>(enumValue.ValueStrings);
                            for (int i = 0; i < allPossibleVals.Count; i++)
                            {
                                Console.WriteLine("   {0}   ", allPossibleVals[i]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadEnumValue", ex);
            }
        }
       
        /// <summary>
        /// Reads a list of values for a list of nodes providing the NodeIDs and without read the whole node information.
        /// The list of values contains values for an uint node, a GUID node and a datetime node.
        /// </summary>
        public void ReadMultipleNodesValues()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadMultipleNodesValues: The session is not initialized!");
                return;
            }

            List<ReadValueId> listOfNodes = new List<ReadValueId>()
            {
                new ReadValueId()
                {
                    NodeId = new NodeId(StaticInt32NodeId),
                    AttributeId = Attributes.Value
                },
                new ReadValueId()
                {
                    NodeId = new NodeId(StaticGuidNodeId),
                    AttributeId = Attributes.Value
                },
                new ReadValueId()
                {
                    NodeId = new NodeId(StaticDateTimeNodeId),
                    AttributeId = Attributes.Value
                }
            };

            Console.WriteLine("\n Read value for multiple nodes: ");
            try
            {
                IList<DataValueEx> dataValues = m_session.Read(listOfNodes, 0, TimestampsToReturn.Both);
                for (int i = 0; i < listOfNodes.Count; i++)
                {
                    Console.WriteLine(" \n {0}. Read value for node {1}.", i, listOfNodes[i].NodeId);
                    DisplayInformationForDataValue(dataValues[i]);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadMultipleNodesValues", ex);
            }
        }

        /// <summary>
        /// Call all simple ReadValuesForCustom* methods to read values from variable nodes defined using custom complex data types
        /// </summary>
        public void ReadValuesForCustomDataTypes()
        {
            ReadValuesForCustomEnumerationDataType();
            ReadValuesForCustomOptionSetEnumerationDataType();

            ReadValuesForCustomStructuredValueDataType();
            ReadValuesForCustomStructureWithOptionalFieldsDataType();
            ReadValuesForCustomUnionDataType();

            ReadValuesForCustomOptionSetDataType();
        }

        /// <summary>
        /// Read value of variable nodes created with custom Enumeration data types  
        /// </summary>
        public void ReadValuesForCustomEnumerationDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadValuesForCustomEnumerationDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read DataType attribute for node StaticCustomEnumerationNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(StaticCustomEnumerationNodeId);
                readValueId.AttributeId = Attributes.DataType;
                Console.WriteLine("\n Read values for custom Enumeration data type");
                Console.WriteLine(" Read DataType Id for NodeId:{0}", StaticCustomEnumerationNodeId);

                DataValueEx dataValueTypeId = m_session.Read(readValueId);
                NodeId dataValueTypeNodeId = dataValueTypeId.Value as NodeId;

                Console.WriteLine("  Status Code is {0}.", dataValueTypeId.StatusCode);

                // try to get the complex type info for the specified data type Id
                BaseComplexTypeInfo baseComplexTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId);
                if (baseComplexTypeInfo == null)
                {
                    Console.WriteLine("  Current session does not know DataType: {0} for NodeId: {1}. Please make sure that DataTypeDefinitions are loaded from DataTypeDefinition attribute or from data types dictionary.",
                        dataValueTypeNodeId, StaticCustomEnumerationNodeId);
                    return;
                }

                if (dataValueTypeNodeId != null)
                {
                    readValueId.NodeId = StaticCustomEnumerationNodeId;
                    readValueId.AttributeId = Attributes.Value;
                    Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomEnumerationNodeId);

                    DataValueEx dataValue = m_session.Read(readValueId);
                    // attempt to convert the integer value read from node to an EnumValue instance
                    dataValue.TryConvertToEnumValue(m_session, dataValueTypeNodeId);

                    //display information for read value
                    Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                    EnumValue enumValue = dataValue.ProcessedValue as EnumValue;
                    if (enumValue != null)
                    {
                        Console.WriteLine("  All possible values for {0} Enumeration are:", enumValue.TypeName.Name);
                        for (int i = 0; i < enumValue.ValueStrings.Count; i++)
                        {
                            Console.WriteLine("   {0}   ", enumValue.ValueStrings[i]);
                        }

                        Console.WriteLine("  The Value of NodeId {0} is an instance of {1}: {2}({3})", StaticCustomEnumerationNodeId, enumValue.TypeName.Name, enumValue.ValueString, enumValue.Value);                        
                    }

                    // read array value 
                    readValueId.NodeId = StaticCustomEnumerationArrayNodeId;
                    Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomEnumerationArrayNodeId);
                    dataValue = m_session.Read(readValueId);
                    Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);

                    // attempt to convert the integer value read from node to an EnumValue instance
                    dataValue.TryConvertToEnumValue(m_session, dataValueTypeNodeId, ValueRanks.OneDimension);
                    EnumValue[] enumValues = dataValue.ProcessedValue as EnumValue[];
                    if (enumValues != null)
                    {
                        Console.Write("  The Value of NodeId {0} is an instance of {1}[{2}]:", StaticCustomEnumerationArrayNodeId, enumValue.TypeName.Name, enumValues.Length);
                        for (int i = 0; i < enumValues.Length; i++)
                        {
                            Console.Write("{0}({1}), ", enumValues[i].ValueString, enumValues[i].Value);
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("  The Value of NodeId {0} is null.", StaticCustomEnumerationArrayNodeId);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadValuesForCustomEnumerationDataType", ex);
            }
        }

        /// <summary>
        /// Read value of variable nodes created with custom OptionSet Enumeration data types  
        /// </summary>
        public void ReadValuesForCustomOptionSetEnumerationDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadValuesForCustomOptionSetEnumerationDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read  DataType attribute for node StaticCustomOptionSetEnumerationNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(StaticCustomOptionSetEnumerationNodeId);
                readValueId.AttributeId = Attributes.DataType;
                Console.WriteLine("\n Read values for custom OptionSet Enumeration data type");
                Console.WriteLine(" Read DataType Id for NodeId:{0}", StaticCustomOptionSetEnumerationNodeId);

                DataValueEx dataValueTypeId = m_session.Read(readValueId);
                NodeId dataValueTypeNodeId = dataValueTypeId.Value as NodeId;

                Console.WriteLine("  Status Code is {0}.", dataValueTypeId.StatusCode);

                // try to get the complex type info for the specified data type Id
                BaseComplexTypeInfo baseComplexTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId);
                if (baseComplexTypeInfo == null)
                {
                    Console.WriteLine("  Current session does not know DataType: {0} for NodeId: {1}. Please make sure that DataTypeDefinitions are loaded from DataTypeDefinition attribute or from data types dictionary.", 
                        dataValueTypeNodeId, StaticCustomOptionSetEnumerationNodeId);
                    return;
                }
                if (dataValueTypeNodeId != null)
                {
                    readValueId.NodeId = StaticCustomOptionSetEnumerationNodeId;
                    readValueId.AttributeId = Attributes.Value;
                    Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomOptionSetEnumerationNodeId);

                    DataValueEx dataValue = m_session.Read(readValueId);
                    // attempt to convert the integer value read from node to an EnumValue instance
                    dataValue.TryConvertToEnumValue(m_session, dataValueTypeNodeId);

                    //display information for read value
                    Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                    EnumValue enumValue = dataValue.ProcessedValue as EnumValue;
                    if (enumValue != null)
                    {
                        Console.WriteLine("  All possible values for {0} OptionSet Enumeration are:", enumValue.TypeName.Name);
                        for (int i = 0; i < enumValue.ValueStrings.Count; i++)
                        {
                            Console.WriteLine("   {0}   ", enumValue.ValueStrings[i]);
                        }

                        Console.WriteLine("  The Value of NodeId {0} is an instance of {1}: {2} ({3})", StaticCustomOptionSetEnumerationNodeId, enumValue.TypeName.Name, enumValue.ValueString, enumValue.Value);
                    }

                    // read array value 
                    readValueId.NodeId = StaticCustomOptionSetEnumerationArrayNodeId;
                    Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomOptionSetEnumerationArrayNodeId);
                    dataValue = m_session.Read(readValueId);
                    Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);

                    // attempt to convert the integer value read from node to an EnumValue instance
                    dataValue.TryConvertToEnumValue(m_session, dataValueTypeNodeId, ValueRanks.OneDimension);
                    EnumValue[] enumValues = dataValue.ProcessedValue as EnumValue[];
                    if (enumValues != null)
                    {
                        Console.Write("  The Value of NodeId {0} is an instance of {1}[{2}]:", StaticCustomOptionSetEnumerationArrayNodeId, enumValue.TypeName.Name, enumValues.Length);
                        for (int i = 0; i < enumValues.Length; i++)
                        {
                            Console.Write("{0}({1}), ", enumValues[i].ValueString, enumValues[i].Value);
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("  The Value of NodeId {0} is null.", StaticCustomOptionSetEnumerationArrayNodeId);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadValuesForCustomoptionSetEnumerationDataType", ex);
            }
        }

        /// <summary>
        /// Read value of variable nodes created with custom OptionSet data types  
        /// </summary>
        public void ReadValuesForCustomOptionSetDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadValuesForCustomOptionSetDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read value for node StaticCustomOptionSetNodeId
                ReadValueId readValueId = new ReadValueId();
                Console.WriteLine("\n Read values for custom OptionSet data type");
                readValueId.NodeId = StaticCustomOptionSetNodeId;
                readValueId.AttributeId = Attributes.Value;
                Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomOptionSetNodeId);

                DataValueEx dataValue = m_session.Read(readValueId);

                //display information for read value
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                OptionSetValue optionSetValue = dataValue.ProcessedValue as OptionSetValue;
                if (optionSetValue != null)
                {
                    Console.WriteLine("  The Value of NodeId {0} is an instance of {1}: {2}", StaticCustomOptionSetNodeId, optionSetValue.TypeName.Name, optionSetValue);
                }
                else
                {
                    Console.WriteLine("  The Value of NodeId {0} cannot be decoded as an OptionSetValue instance ", StaticCustomOptionSetNodeId);
                }

                // read value for array node
                readValueId.NodeId = StaticCustomOptionSetArrayNodeId;
                readValueId.AttributeId = Attributes.Value;
                Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomOptionSetArrayNodeId);

                dataValue = m_session.Read(readValueId);

                //display information for read value
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                ExtensionObject[] extensionObjectValueArray = dataValue.Value as ExtensionObject[];
                if (extensionObjectValueArray != null)
                {
                    Console.Write("  The Value of NodeId {0} is an OptionSetValue[{1}]:", StaticCustomOptionSetArrayNodeId, extensionObjectValueArray.Length);
                    foreach (var extensionObject in extensionObjectValueArray)
                    {
                        optionSetValue = extensionObject.Body as OptionSetValue;
                        Console.Write("{0}, ", optionSetValue);
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("  The Value of NodeId {0} is null", StaticCustomOptionSetArrayNodeId);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadValuesForCustomOptionSetDataType", ex);
            }
        }

        /// <summary>
        /// Read value of variable nodes created with custom Structure with Optional fields data types  
        /// </summary>
        public void ReadValuesForCustomStructureWithOptionalFieldsDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadValuesForCustomStructureWithOptionalFieldsDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read value for node StaticCustomStructureWithOptionalFieldsNodeId
                ReadValueId readValueId = new ReadValueId();
                Console.WriteLine("\n Read values for custom OptionalFieldsStructuredValue data type");
                readValueId.NodeId = StaticCustomStructureWithOptionalFieldsNodeId;
                readValueId.AttributeId = Attributes.Value;
                Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomStructureWithOptionalFieldsNodeId);

                DataValueEx dataValue = m_session.Read(readValueId);

                //display information for read value
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                OptionalFieldsStructuredValue optionalFieldsStructuredValue = dataValue.ProcessedValue as OptionalFieldsStructuredValue;
                if (optionalFieldsStructuredValue != null)
                {
                    Console.WriteLine("  The Value of NodeId {0} is an instance of {1}: {2}", StaticCustomStructureWithOptionalFieldsNodeId, optionalFieldsStructuredValue.TypeName.Name, optionalFieldsStructuredValue);
                }
                else
                {
                    Console.WriteLine("  The Value of NodeId {0} cannot be decoded as an OptionalFieldsStructuredValue instance ", StaticCustomStructureWithOptionalFieldsNodeId);
                }                

                // read value for array node
                readValueId.NodeId = StaticCustomStructureWithOptionalFieldsArrayNodeId;
                readValueId.AttributeId = Attributes.Value;
                Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomStructureWithOptionalFieldsNodeId);

                dataValue = m_session.Read(readValueId);

                //display information for read value
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                ExtensionObject[] extensionObjectValueArray = dataValue.Value as ExtensionObject[];
                if (extensionObjectValueArray != null)
                {
                    Console.Write("  The Value of NodeId {0} is an OptionalFieldsStructuredValue[{1}]:", StaticCustomStructureWithOptionalFieldsNodeId, extensionObjectValueArray.Length);
                    foreach (var extensionObject in extensionObjectValueArray)
                    {
                        optionalFieldsStructuredValue = extensionObject.Body as OptionalFieldsStructuredValue;
                        Console.Write("{0}, ", optionalFieldsStructuredValue);
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("  The Value of NodeId {0} is null", StaticCustomStructureWithOptionalFieldsNodeId);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadValuesForCustomStructureWithOptionalFieldsDataType", ex);
            }
        }

        /// <summary>
        /// Read value of variable nodes created with custom Union data types  
        /// </summary>
        public void ReadValuesForCustomUnionDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadValuesForCustomUnionDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read value for node StaticCustomUnionNodeId
                ReadValueId readValueId = new ReadValueId();
                Console.WriteLine("\n Read values for custom Union data type");
                readValueId.NodeId = StaticCustomUnionNodeId;
                readValueId.AttributeId = Attributes.Value;
                Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomUnionNodeId);

                DataValueEx dataValue = m_session.Read(readValueId);

                //display information for read value
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                UnionStructuredValue unionStructuredValue = dataValue.ProcessedValue as UnionStructuredValue;
                if (unionStructuredValue != null)
                {
                    Console.WriteLine("  The Value of NodeId {0} is an instance of {1}: {2}", StaticCustomUnionNodeId, unionStructuredValue.TypeName.Name, unionStructuredValue);
                }
                else
                {
                    Console.WriteLine("  The Value of NodeId {0} cannot be decoded as a UnionStructuredValue instance ", StaticCustomUnionNodeId);
                }

                // read value for array node
                readValueId.NodeId = StaticCustomUnionArrayNodeId;
                readValueId.AttributeId = Attributes.Value;
                Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomUnionArrayNodeId);

                dataValue = m_session.Read(readValueId);

                //display information for read value
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                ExtensionObject[] extensionObjectValueArray = dataValue.Value as ExtensionObject[];
                if (extensionObjectValueArray != null)
                {
                    Console.Write("  The Value of NodeId {0} is a UnionStructuredValue[{1}]:", StaticCustomUnionArrayNodeId, extensionObjectValueArray.Length);
                    foreach (var extensionObject in extensionObjectValueArray)
                    {
                        unionStructuredValue = extensionObject.Body as UnionStructuredValue;
                        if (unionStructuredValue != null)
                        {
                            Console.Write("{0}, ", unionStructuredValue);
                        }                        
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("  The Value of NodeId {0} is null", StaticCustomUnionArrayNodeId);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadValuesForCustomUnionDataType", ex);
            }
        }

        /// <summary>
        /// Read value of variable nodes created with custom StructuredValue data types  
        /// </summary>
        public void ReadValuesForCustomStructuredValueDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("ReadValuesForCustomStructuredValueDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read value for node StaticCustomStructuredValueNodeId
                ReadValueId readValueId = new ReadValueId();
                Console.WriteLine("\n Read values for custom StructuredValue data type");
                readValueId.NodeId = StaticCustomStructuredValueNodeId;
                readValueId.AttributeId = Attributes.Value;
                Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomStructuredValueNodeId);

                DataValueEx dataValue = m_session.Read(readValueId);

                //display information for read value
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                StructuredValue structuredValue = dataValue.ProcessedValue as StructuredValue;
                if (structuredValue != null)
                {
                    Console.WriteLine("  The Value of NodeId {0} is an instance of {1}: {2}", StaticCustomStructuredValueNodeId, structuredValue.TypeName.Name, structuredValue);
                }
                else
                {
                    Console.WriteLine("  The Value of NodeId {0} is not an instance of StructuredValue.", StaticCustomStructuredValueNodeId);
                }

                // read value for array node
                readValueId.NodeId = StaticCustomStructuredValueArrayNodeId;
                readValueId.AttributeId = Attributes.Value;
                Console.WriteLine("\n Read value for NodeId: {0} ", StaticCustomStructuredValueArrayNodeId);

                dataValue = m_session.Read(readValueId);

                //display information for read value
                Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
                ExtensionObject[] extensionObjectValueArray = dataValue.Value as ExtensionObject[];
                if (extensionObjectValueArray != null)
                {
                    Console.WriteLine("  The Value of NodeId {0} is a StructuredValue[{1}]:", StaticCustomStructuredValueArrayNodeId, extensionObjectValueArray.Length);
                    foreach (var extensionObject in extensionObjectValueArray)
                    {
                        structuredValue = extensionObject.Body as StructuredValue;
                        Console.WriteLine("      {0}, ", structuredValue);
                    }
                }
                else
                {
                    Console.WriteLine("  The Value of NodeId {0} is null", StaticCustomStructuredValueArrayNodeId);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadValuesForCustomStructuredValueDataType", ex);
            }
        }
        #endregion

        #region Public Methods - Write
        /// <summary>
        /// Writes a value for an uint node providing the NodeID. The written value is random generated for a nice output.
        /// </summary>
        public void WriteValueForNode()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteValueForNode: The session is not initialized!");
                return;
            }

            //first value to write
            WriteValue writeValue = new WriteValue();
            writeValue.AttributeId = Attributes.Value;
            writeValue.NodeId = new NodeId(StaticUInt32NodeId);

            DataValue valueToWrite = new DataValue();
            valueToWrite.Value = (uint)m_random.Next(1, 1975109192);

            writeValue.Value = valueToWrite;

            try
            {
                StatusCode statusCode = m_session.Write(writeValue);
                Console.WriteLine("\n The NodeId:{0} was written with the value {1} ", StaticUInt32NodeId, writeValue.Value.Value);
                Console.WriteLine(" Status code is {0}", statusCode);
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteValueForNode", ex);
            }
        }

        /// <summary>
        /// Writes a value for an array node providing the NodeID. The written values in array are random generated for a nice output.
        /// </summary>
        public void WriteArrayValueForNode()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteArrayValueForNode: The session is not initialized!");
                return;
            }

            WriteValue writeValue = new WriteValue();
            writeValue.AttributeId = Attributes.Value;
            writeValue.NodeId = new NodeId(StaticInt64ArrayNodeId);

            DataValue valueToWrite = new DataValue();
            long[] value = new long[2];
            value[0] = m_random.Next(1, 100000);
            value[1] = m_random.Next(1, 200000);
            valueToWrite.Value = value;

            writeValue.Value = valueToWrite;
            try
            {
                StatusCode statusCode = m_session.Write(writeValue);
                Console.WriteLine("\n The NodeId:{0} was written with the array value {1} ", StaticInt64ArrayNodeId, writeValue.Value.ToString());
                Console.WriteLine(" Status code is {0}", statusCode);
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteArrayValueForNode", ex);
            }
        }

        /// <summary>
        /// Writes a value for a complex node providing the NodeID. Some written values are random generated for a nice output.
        /// </summary>
        public void WriteComplexValueForNode()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteComplexValueForNode: The session is not initialized!");
                return;
            }
           
            try
            {
                //read data type id for node StaticComplexNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(StaticComplexNodeId);
                readValueId.AttributeId = Attributes.DataType;

                Console.WriteLine("\n Read DataType Id for NodeId:{0}", StaticComplexNodeId);
               
                DataValueEx dataValuetypeId = m_session.Read(readValueId);

                //Get Default value for data type
                StructuredValue defaultValue = m_session.GetDefaultValueForDatatype(dataValuetypeId.Value as NodeId) as StructuredValue;

                if (defaultValue != null)
                {
                    //change some fields for default object
                    //for this you need to know the exact name and type of fields from type information
                    defaultValue["Int32Field"] = 100;
                    defaultValue["FloatField"] = 100f;
                    defaultValue["StringField"] = "dummy string value";
                    ((StructuredValue)defaultValue["DataType2Field"])["Int32Field"] = 10;
                    ((StructuredValue)defaultValue["DataType2Field"])["FloatField"] = 10f;
                    ((StructuredValue)defaultValue["DataType2Field"])["StringField"] = "another dummy value";
                    ((EnumValue)(defaultValue["EnumerationType1Field"])).Value = 2;
                    //write new value to node StaticComplexNodeId
                    DataValue valueToWrite = new DataValue();
                    valueToWrite.Value = defaultValue;

                    //create WriteValue that will be sent to the ClientSession instance 
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticComplexNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticComplexNodeId, defaultValue);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteComplexValueForNode", ex);
            }
        }

        /// <summary>
        /// Writes a value for an enum node providing the NodeID. Written value is random generated for a nice output.
        /// </summary>
        public void WriteEnumValueForNode()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteEnumValueForNode: The session is not initialized!");
                return;
            }
            
            WriteValue writeValue = new WriteValue();
            writeValue.AttributeId = Attributes.Value;
            writeValue.NodeId = new NodeId(StaticEnumNodeId);

            DataValue valueToWrite = new DataValue();
            valueToWrite.Value = m_random.Next(0, 3);

            writeValue.Value = valueToWrite;
            try
            {
                StatusCode statusCode = m_session.Write(writeValue);
                Console.WriteLine("\n The NodeId:{0} was written with the enum value {1} ", StaticEnumNodeId, writeValue.Value.ToString());
                Console.WriteLine(" Status code is {0}", statusCode);
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteEnumValueForNode", ex);
            }
        }

        /// <summary>
        /// Writes values for a list of nodes providing the NodeIDs.The list of nodes contains a uint, an GUID and a datetime node. Written values are random generated for a nice output.
        /// </summary>
        public void WriteMultipleNodesValues()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteMultipleNodesValues: The session is not initialized!");
                return;
            }

            List<WriteValue> listOfNodes = new List<WriteValue>()
            {
                new WriteValue()
                {
                    NodeId = new NodeId(StaticInt32NodeId),
                    AttributeId = Attributes.Value
                },
                new WriteValue()
                {
                    NodeId = new NodeId(StaticGuidNodeId),
                    AttributeId = Attributes.Value
                },
                new WriteValue()
                {
                    NodeId = new NodeId(StaticDateTimeNodeId),
                    AttributeId = Attributes.Value
                }
            };

            DataValue valueToWrite = new DataValue();
            valueToWrite.Value = m_random.Next(1, 1975109192);
            listOfNodes[0].Value = valueToWrite;

            DataValue valueToWrite1 = new DataValue();
            valueToWrite1.Value = Guid.NewGuid();
            listOfNodes[1].Value = valueToWrite1;

            DataValue valueToWrite2 = new DataValue();
            valueToWrite2.Value = DateTime.Now;
            listOfNodes[2].Value = valueToWrite2;

            Console.WriteLine("\n Write value for multiple nodes: ");
            try
            {
                IList<StatusCode> statusCodes = m_session.Write(listOfNodes);
                for (int i = 0; i < listOfNodes.Count; i++)
                {
                    Console.WriteLine(" \n {0}. Write value for node {1}.", i, listOfNodes[i].NodeId);
                    Console.WriteLine(" Written value is {0} ", listOfNodes[i].Value.ToString());
                    Console.WriteLine(" Status code is {0}", statusCodes[i]);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteMultipleNodesValues", ex);
            }
        }

        /// <summary>
        /// Call all WriteValuesForCustom* methods to write values in variable nodes defined using custom complex data types
        /// </summary>
        public void WriteValuesForCustomDataTypes()
        {
            WriteValuesForCustomEnumerationDataType();
            WriteValuesForCustomOptionSetEnumerationDataType();

            WriteValuesForCustomStructuredValueDataType();            
            WriteValuesForCustomStructureWithOptionalFieldsDataType();
            WriteValuesForCustomUnionDataType();

            WriteValuesForCustomOptionSetDataType();
        }

        /// <summary>
        /// Writes values in variable nodes created with custom Enumeration data types  
        /// </summary>
        public void WriteValuesForCustomEnumerationDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteComplexValuesForCustomEnumerationDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read data type id for node StaticCustomEnumerationNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(StaticCustomEnumerationNodeId);
                readValueId.AttributeId = Attributes.DataType;

                Console.WriteLine("\n Read DataType Id for NodeId:{0}", StaticCustomEnumerationNodeId);

                DataValueEx dataValueTypeId = m_session.Read(readValueId);
                NodeId dataValueTypeNodeId = dataValueTypeId.Value as NodeId;

                Console.WriteLine("  Status Code is {0}.", dataValueTypeId.StatusCode);

                // try to get the complex type info for the specified node
                BaseComplexTypeInfo baseComplexTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId);
                if (baseComplexTypeInfo == null)
                {
                    Console.WriteLine("  Current session does not know DataType: {0} for NodeId: {1}. Please make sure that DataTypeDefinitions are loaded from DataTypeDefinition attribute or from data types dictionary.", 
                        dataValueTypeNodeId, StaticCustomEnumerationNodeId);
                    return;
                }

                //Get Default value for data type
                EnumValue defaultValue = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId) as EnumValue;

                if (defaultValue != null)
                {
                    //change some fields for default object
                    defaultValue.Value = 1;
                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    valueToWrite.Value = defaultValue.GetValueToEncode();

                    //create WriteValue that will be sent to the ClientSession instance 
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomEnumerationNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomEnumerationNodeId, defaultValue);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }

                EnumValue[] defaultValueArray = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId, ValueRanks.OneDimension, 3) as EnumValue[];
                // write value into array variable node
                if (defaultValueArray != null)
                {
                    //change some fields for default object
                    defaultValueArray[0].Value = 1;
                    defaultValueArray[1].Value = 1;
                    defaultValueArray[2].Value = 0;

                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    // get the EnumTypeInfo that describes this particular Enumeration data type 
                    EnumTypeInfo enumTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId) as EnumTypeInfo;
                    // get the actual values as an array of values of the type the server expects
                    valueToWrite.Value = EnumValue.GetValueToEncode(enumTypeInfo, defaultValueArray);

                    //create WriteValue that will be sent to the ClientSession instance
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomEnumerationArrayNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomEnumerationArrayNodeId, defaultValueArray);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteValuesForCustomEnumerationDataType", ex);
            }
        }

        /// <summary>
        /// Writes values in variable nodes created with custom OptionSetEnumeration data types  
        /// </summary>
        public void WriteValuesForCustomOptionSetEnumerationDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteComplexValuesForCustomOptionSetEnumerationDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read data type id for node StaticCustomOptionSetEnumerationNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(StaticCustomOptionSetEnumerationNodeId);
                readValueId.AttributeId = Attributes.DataType;

                Console.WriteLine("\n Read DataType Id for NodeId:{0}", StaticCustomOptionSetEnumerationNodeId);

                DataValueEx dataValueTypeId = m_session.Read(readValueId);
                NodeId dataValueTypeNodeId = dataValueTypeId.Value as NodeId;

                Console.WriteLine("  Status Code is {0}.", dataValueTypeId.StatusCode);

                // try to get the complex type info for the specified node
                BaseComplexTypeInfo baseComplexTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId);
                if (baseComplexTypeInfo == null)
                {
                    Console.WriteLine("  Current session does not know DataType: {0} for NodeId: {1}. Please make sure that DataTypeDefinitions are loaded from DataTypeDefinition attribute or from data types dictionary.",
                        dataValueTypeNodeId, StaticCustomOptionSetEnumerationNodeId);
                    return;
                }
                //Get Default value for data type
                EnumValue defaultValue = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId) as EnumValue;

                if (defaultValue != null)
                {
                    //change some fields for default object
                    defaultValue.ValueString = "ABS|ESP";
                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    valueToWrite.Value = defaultValue.GetValueToEncode();

                    //create WriteValue that will be sent to the ClientSession instance 
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomOptionSetEnumerationNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomOptionSetEnumerationNodeId, defaultValue);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }

                EnumValue[] defaultValueArray = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId, ValueRanks.OneDimension, 3) as EnumValue[];
                // write value into array variable node
                if (defaultValueArray != null)
                {
                    //change some fields for default object
                    defaultValueArray[0].Value = 1;
                    defaultValueArray[1].Value = 5;
                    defaultValueArray[2].Value = 15;

                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    // get the EnumTypeInfo that describes this particular Enumeration data type 
                    EnumTypeInfo enumTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId) as EnumTypeInfo;
                    // get the actual values as an array of values of the type the server expects
                    valueToWrite.Value = EnumValue.GetValueToEncode(enumTypeInfo, defaultValueArray);

                    //create WriteValue that will be sent to the ClientSession instance
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomOptionSetEnumerationArrayNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomOptionSetEnumerationArrayNodeId, defaultValueArray);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteValuesForCustomoptionSetEnumerationDataType", ex);
            }
        }

        /// <summary>
        /// Writes values in variable nodes created with custom OptionSet data types  
        /// </summary>
        public void WriteValuesForCustomOptionSetDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteComplexValuesForCustomOptionSetDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read data type id for node StaticCustomOptionSetNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(StaticCustomOptionSetNodeId);
                readValueId.AttributeId = Attributes.DataType;

                Console.WriteLine("\n Read DataType Id for NodeId:{0}", StaticCustomOptionSetNodeId);

                DataValueEx dataValueTypeId = m_session.Read(readValueId);
                NodeId dataValueTypeNodeId = dataValueTypeId.Value as NodeId;

                Console.WriteLine("  Status Code is {0}.", dataValueTypeId.StatusCode);

                // try to get the complex type info for the specified node
                BaseComplexTypeInfo baseComplexTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId);
                if (baseComplexTypeInfo == null)
                {
                    Console.WriteLine(" Current session does not know DataType: {0} for NodeId: {0}.  Please make sure that DataTypeDefinitions are loaded from DataTypeDefinition attribute or from data types dictionary.", 
                        dataValueTypeNodeId, StaticCustomOptionSetNodeId);
                    return;
                }
                //Get Default value for data type
                OptionSetValue defaultValue = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId) as OptionSetValue;

                if (defaultValue != null)
                {
                    //change some fields for default object
                    //for this you need to know the exact name and type of fields from type information
                    defaultValue["ABS"] = true;
                    defaultValue["AirbagSides"] = true;
                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    valueToWrite.Value = defaultValue;

                    //create WriteValue that will be sent to the ClientSession instance 
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomOptionSetNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomOptionSetNodeId, defaultValue);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
                // For this data type the default value is of type OptionSetValue
                OptionSetValue[] defaultValueArray = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId, ValueRanks.OneDimension, 3) as OptionSetValue[];
                // write value into array variable node
                if (defaultValueArray != null)
                {
                    //change some fields for default object
                    defaultValueArray[0]["ABS"] = true;
                    defaultValueArray[1]["ABS"] = true;
                    defaultValueArray[2]["ABS"] = true;

                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    // get the actual values as an array of values of the type the server expects
                    valueToWrite.Value = defaultValueArray;

                    //create WriteValue that will be sent to the ClientSession instance
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomOptionSetArrayNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomOptionSetArrayNodeId, defaultValueArray);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteValuesForCustomOptionSetDataType", ex);
            }
        }

        /// <summary>
        /// Writes values in variable nodes created with custom Structure with optional fields data types  
        /// </summary>
        public void WriteValuesForCustomStructureWithOptionalFieldsDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteComplexValuesForStructureWithOptionalFieldsDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read data type id for node StaticCustomStructureWithOptionalFieldsNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(StaticCustomStructureWithOptionalFieldsNodeId);
                readValueId.AttributeId = Attributes.DataType;

                Console.WriteLine("\n Read DataType Id for NodeId:{0}", StaticCustomStructureWithOptionalFieldsNodeId);

                DataValueEx dataValueTypeId = m_session.Read(readValueId);
                NodeId dataValueTypeNodeId = dataValueTypeId.Value as NodeId;

                Console.WriteLine("  Status Code is {0}.", dataValueTypeId.StatusCode);

                // try to get the complex type info for the specified node
                BaseComplexTypeInfo baseComplexTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId); 
                if (baseComplexTypeInfo == null)
                {
                    Console.WriteLine("  Current session does not know DataType: {0} for NodeId: {1}. Please make sure that DataTypeDefinitions are loaded from DataTypeDefinition attribute or from data types dictionary.",
                        dataValueTypeNodeId, StaticCustomStructureWithOptionalFieldsNodeId);
                    return;
                }
                //Get Default value for data type. It will be an instance of OptionalFieldsStructuredValue
                OptionalFieldsStructuredValue defaultValue = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId) as OptionalFieldsStructuredValue;

                if (defaultValue != null)
                {
                    //change some fields for default object
                    //for this you need to know the exact name and type of fields from type information
                    defaultValue["Name"] = "John Smith";
                    defaultValue["Age"] = null;
                    defaultValue["Details"] = "bla bla";
                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    valueToWrite.Value = defaultValue;

                    //create WriteValue that will be sent to the ClientSession instance 
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomStructureWithOptionalFieldsNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomStructureWithOptionalFieldsNodeId, defaultValue);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
                // For this data type the default value is of type OptionalFieldsStructuredValue
                OptionalFieldsStructuredValue[] defaultValueArray = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId, ValueRanks.OneDimension, 2) as OptionalFieldsStructuredValue[];
                // write value into array variable node
                if (defaultValueArray != null)
                {
                    //change some fields for default object
                    defaultValueArray[0]["Name"] = "John Smith";
                    defaultValueArray[0]["Age"] = (byte)30;
                    
                    defaultValueArray[1]["Name"] = "John Smith";
                    defaultValueArray[1]["Details"] = "bla bla";
                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    // get the actual values as an array of values of the type the server expects
                    valueToWrite.Value = defaultValueArray;

                    //create WriteValue that will be sent to the ClientSession instance
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomStructureWithOptionalFieldsArrayNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomStructureWithOptionalFieldsArrayNodeId, defaultValueArray);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteValuesForCustomStructureWithOptionalFieldsDataType", ex);
            }
        }

        /// <summary>
        /// Writes values in variable nodes created with custom Union Structure data types  
        /// </summary>
        public void WriteValuesForCustomUnionDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteComplexValuesForUnionDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read data type id for node StaticCustomUnionNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(StaticCustomUnionNodeId);
                readValueId.AttributeId = Attributes.DataType;

                Console.WriteLine("\n Read DataType Id for NodeId:{0}", StaticCustomUnionNodeId);

                DataValueEx dataValueTypeId = m_session.Read(readValueId);
                NodeId dataValueTypeNodeId = dataValueTypeId.Value as NodeId;

                Console.WriteLine("  Status Code is {0}.", dataValueTypeId.StatusCode);

                // try to get the complex type info for the specified node
                BaseComplexTypeInfo baseComplexTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId);
                if (baseComplexTypeInfo == null)
                {
                    Console.WriteLine("  Current session does not know DataType: {0} for NodeId: {1}. Please make sure that DataTypeDefinitions are loaded from DataTypeDefinition attribute or from data types dictionary.",
                        dataValueTypeNodeId, StaticCustomUnionNodeId);
                    return;
                }
                //Get Default value for data type. It will be an instance of UnionStructuredValue
                UnionStructuredValue defaultValue = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId) as UnionStructuredValue;

                if (defaultValue != null)
                {
                    //change some fields for default object
                    defaultValue["IsEmpty"] = true;
                    defaultValue.SwitchFieldPosition = 1; // use first field as Union Value
                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    valueToWrite.Value = defaultValue;

                    //create WriteValue that will be sent to the ClientSession instance 
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomUnionNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomUnionNodeId, defaultValue);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
                // For this data type the default value is of type UnionStructuredValue
                UnionStructuredValue[] defaultValueArray = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId, ValueRanks.OneDimension, 3) as UnionStructuredValue[];
                // write value into array variable node
                if (defaultValueArray != null)
                {
                    //change some fields for default object
                    defaultValueArray[0]["IsEmpty"] = false;
                    defaultValueArray[0].SwitchFieldPosition = 1; // use first field as Union Value

                    defaultValueArray[1]["IsFull"] = true;
                    defaultValueArray[1].SwitchFieldPosition = 2; // use second field as Union Value

                    defaultValueArray[2]["Liters"] = (float)50;
                    defaultValueArray[2].SwitchFieldPosition = 3; // use third field as Union Value
                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    // get the actual values as an array of values of the type the server expects
                    valueToWrite.Value = defaultValueArray;

                    //create WriteValue that will be sent to the ClientSession instance
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomUnionArrayNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomUnionArrayNodeId, defaultValueArray);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteValuesForCustomUnionDataType", ex);
            }
        }

        /// <summary>
        /// Writes values in variable nodes created with custom StructuredValue data types  
        /// </summary>
        public void WriteValuesForCustomStructuredValueDataType()
        {
            if (m_session == null)
            {
                Console.WriteLine("WriteValuesForCustomStructuredValueDataType: The session is not initialized!");
                return;
            }

            try
            {
                //read data type id for node StaticCustomStructuredValueNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(StaticCustomStructuredValueNodeId);
                readValueId.AttributeId = Attributes.DataType;

                Console.WriteLine("\n Read DataType Id for NodeId:{0}", StaticCustomStructuredValueNodeId);

                DataValueEx dataValueTypeId = m_session.Read(readValueId);
                NodeId dataValueTypeNodeId = dataValueTypeId.Value as NodeId;

                Console.WriteLine("  Status Code is {0}.", dataValueTypeId.StatusCode);

                // try to get the complex type info for the specified node
                BaseComplexTypeInfo baseComplexTypeInfo = m_session.GetComplexTypeInfo(dataValueTypeNodeId);
                if (baseComplexTypeInfo == null)
                {
                    Console.WriteLine("  Current session does not know DataType: {0} for NodeId: {1}. Please make sure that DataTypeDefinitions are loaded from DataTypeDefinition attribute or from data types dictionary.",
                        dataValueTypeNodeId, StaticCustomStructuredValueNodeId);
                    return;
                }
                //Get Default value for data type. It will be an instance of StructuredValue
                StructuredValue defaultValue = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId) as StructuredValue;

                if (defaultValue != null)
                {
                    //change some fields for default object
                    //for this you need to know the exact name and type of fields from type information
                    defaultValue["Name"] = "Mazda";
                    OptionalFieldsStructuredValue owner = defaultValue["Owner"] as OptionalFieldsStructuredValue;
                    if (owner != null)
                    {
                        owner["Name"] = "John Doe";
                        owner["Age"] = (byte)50;
                    }
                    OptionSetValue features =  defaultValue["Features"] as OptionSetValue;
                    if (features != null)
                    {
                        features["ABS"] = true;
                        features["ESP"] = true;
                        features["AirbagSides"] = true;
                    }
                    UnionStructuredValue fuelLevel = defaultValue["FuelLevel"] as UnionStructuredValue;
                    if (features != null)
                    {
                        fuelLevel["Liters"] = (float)34;
                        fuelLevel.SwitchFieldPosition = 3; // use third field as value for Union
                    }
                    EnumValue displayWarning = defaultValue["DisplayWarning"] as EnumValue;
                    if (displayWarning != null)
                    {
                        // this is an option set enum instance
                        displayWarning.Value = 7;
                    }
                    EnumValue state = defaultValue["State"] as EnumValue;
                    if (state != null)
                    {
                        // this a simple enum instance
                        state.ValueString = state.ValueStrings[1];
                    }
                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    valueToWrite.Value = defaultValue;

                    //create WriteValue that will be sent to the ClientSession instance 
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomStructuredValueNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomStructuredValueNodeId, defaultValue);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
                // For this data type the default value is of type StructuredValue
               StructuredValue[] defaultValueArray = m_session.GetDefaultValueForDatatype(dataValueTypeNodeId, ValueRanks.OneDimension, 3) as StructuredValue[];
                // write value into array variable node
                if (defaultValueArray != null)
                {
                    //change some fields for objects in the array
                    OptionalFieldsStructuredValue owner = defaultValueArray[0]["Owner"] as OptionalFieldsStructuredValue;
                    if (owner != null)
                    {
                        owner["Name"] = "John Doe";
                        owner["Age"] = (byte)50;
                    }
                    OptionSetValue features = defaultValueArray[1]["Features"] as OptionSetValue;
                    if (features != null)
                    {
                        features["ABS"] = true;
                        features["ESP"] = true;
                        features["AirbagSides"] = true;
                    }
                    UnionStructuredValue fuelLevel = defaultValueArray[2]["FuelLevel"] as UnionStructuredValue;
                    if (features != null)
                    {
                        fuelLevel["Liters"] = (float)34;
                        fuelLevel.SwitchFieldPosition = 3; // use third field as value for Union
                    }
                    //write new value to node 
                    DataValue valueToWrite = new DataValue();
                    // get the actual values as an array of values of the type the server expects
                    valueToWrite.Value = defaultValueArray;

                    //create WriteValue that will be sent to the ClientSession instance
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticCustomStructuredValueArrayNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticCustomStructuredValueArrayNodeId, defaultValueArray);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("WriteValuesForCustomStructuredValueDataType", ex);
            }
        }
        #endregion

        #region Public Methods - Register/UnregisterNodes
        /// <summary>
        /// Sample for Register/Unregister nodes 
        /// </summary>
        public void RegisterNodesSample()
        {
            if (m_session == null)
            {
                Console.WriteLine("RegisterNodesSample: The session is not initialized!");
                return;
            }

            try
            {
                // Register node RegisterNodeId0
                NodeId registeredNodeId = m_session.RegisterNode(RegisterNodeId0);
                Console.WriteLine("RegisterNode(\"{0}\") returned the assigned NodeId=\"{1}\".", RegisterNodeId0, registeredNodeId);

                // read the value from RegisterNodeId0 using the returned registered node id
                var value = m_session.Read(new ReadValueId() { NodeId = registeredNodeId, AttributeId = Attributes.Value });
                Console.WriteLine("\tRead(\"{0}\"), Value= {1}, StatusCode:{2}.", registeredNodeId, value.Value, value.StatusCode);

                // unregister the RegisterNodeId0 using the registered node id
                m_session.UnregisterNode(registeredNodeId);
                Console.WriteLine("\tUnregisterNode(\"{0}\") returned.", registeredNodeId);

                // Register nodes RegisterNodeId0, RegisterNodeId1, RegisterNodeId2
                NodeIdCollection nodesToRegister = new NodeIdCollection() { RegisterNodeId0, RegisterNodeId1, RegisterNodeId2 };
                var registeredNodeIds = m_session.RegisterNodes(nodesToRegister);

                Console.WriteLine("\n\nRegisterNodes returned:");

                for (int i = 0; i < nodesToRegister.Count; i++)
                {
                    if (registeredNodeIds.Count > i)
                    {
                        Console.WriteLine("\tNodeId:\"{0}\" was assigned NodeId=\"{1}\".", nodesToRegister[i], registeredNodeIds[i]);

                        // read the value from  registeredNodeIds[i] using the returned registered node id
                        value = m_session.Read(new ReadValueId() { NodeId = registeredNodeIds[i], AttributeId = Attributes.Value });
                        Console.WriteLine("\tRead(\"{0}\"), Value= {1}, StatusCode:{2}.", registeredNodeIds[i], value.Value, value.StatusCode);
                    }
                }

                // unregister the nodes using the registered node id
                m_session.UnregisterNodes(registeredNodeIds);
                Console.WriteLine("\tUnregisterNodes returned.");

            }
            catch (Exception ex)
            {
                Program.PrintException("RegisterNodesSample", ex);
            }
        }
        #endregion

        #region InitializeSession & DisconnectSession

        /// <summary>
        /// Initialize session object
        /// </summary>
        public void InitializeSession()
        {
            try
            {
                // create the session object with no security and anonymous login    
                m_session = m_application.CreateSession(Program.ServerUrl);
                m_session.SessionName = SessionName;

                //connect session
                m_session.Connect(false, true);

                Console.WriteLine("Session is connected.");

                //wait until custom data types are loaded
                if (m_application.ClientToolkitConfiguration.DecodeCustomDataTypes)
                {
                    //wait until all data type definitions are loaded
                    while (!m_session.DataTypeDefinitionsLoaded)
                    {
                        Task.Delay(500).Wait();
                    }
                }
                if (m_application.ClientToolkitConfiguration.DecodeDataTypeDictionaries)
                {
                    //wait until all data types dictionaries are loaded 
                    while (!m_session.TypeDictionariesLoaded)
                    {
                        Task.Delay(500).Wait();
                    }
                }
                Console.WriteLine("Session - Custom Data Types information is loaded.");
            }
            catch (Exception ex)
            {
                Program.PrintException("CreateSession", ex);

                if (m_session != null)
                {
                    m_session.Dispose();
                    m_session = null;
                }
            }
        }

        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public void DisconnectSession()
        {
            if (m_session == null)
            {
                return;
            }

            try
            {
                m_session.Disconnect(true);
                m_session.Dispose();
                m_session = null;
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Program.PrintException("DisconnectSession", ex);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Displays information at console for a read DataValue.
        /// </summary>
        /// <param name="dataValue">Value that is provided for displaying information </param>
        private void DisplayInformationForDataValue(DataValueEx dataValue)
        {
            Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode);
            Console.WriteLine("  Data Value is {0}.", dataValue.Value);
        } 
        #endregion
    }
}
