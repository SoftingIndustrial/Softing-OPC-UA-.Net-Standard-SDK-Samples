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
using System.Threading.Tasks;
using Opc.Ua;
using Softing.Opc.Ua;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.Nodes;
using Softing.Opc.Ua.Types;

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
        const string StaticInt32NodeId = "ns=7;s=Scalar_Static_Int32";
        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\UInt32
        const string StaticUInt32NodeId = "ns=7;s=Scalar_Static_UInt32";
        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\Guid
        const string StaticGuidNodeId = "ns=7;s=Scalar_Static_Guid";
        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\DateTime 
        const string StaticDateTimeNodeId = "ns=7;s=Scalar_Static_DateTime";

        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\Arrays\Int64
        const string StaticInt64ArrayNodeId = "ns=7;s=Scalar_Static_Arrays_Int64";
        //Browse path: Root\Objects\CTT\StructuredTypeVariables\EnumerationType1Variable
        const string StaticEnumNodeId = "ns=7;i=14";
        //Browse path: Root\Objects\CTT\StructuredTypeVariables\DataType5Variable
        const string StaticComplexNodeId = "ns=7;i=13";
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Reads value for an uint node providing the NodeID and without read the whole node information.
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        ///  Reads value for an array node providing the NodeID and without read the whole node information.
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
                if (dataValue.Value is Array)
                {
                    Console.WriteLine("  Value is an array with values:");
                    Array array = dataValue.Value as Array;
                    foreach (object obj in array)
                    {
                        Console.WriteLine("   {0}", obj);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
                            Console.WriteLine("   Field: {0} Value:{1} Type:{2} ", field.Name, complexData[field.Name],
                                complexData[field.Name].GetType().Name);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
                BaseNode baseNode = m_session.ReadNode(nodeId);
                
                if (baseNode.NodeClass == NodeClass.Variable)
                {
                    VariableNodeEx variableNode = baseNode as VariableNodeEx;
                    ReadValueId readValueId = new ReadValueId();
                    if (variableNode != null)
                    {
                        readValueId.NodeId = variableNode.NodeId;
                        readValueId.AttributeId = Attributes.Value;

                        Console.WriteLine("\n Read enum value for Node: {0} (NodeId:{1})", variableNode.DisplayName,
                            StaticEnumNodeId);

                        DataValueEx dataValue = m_session.Read(readValueId);
                        //convert int32 value read from node to a well known enumeration type
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
                StructuredValue defaultValue = m_session.GetDefaultValueForDatatype(dataValuetypeId.Value as NodeId, ValueRanks.Scalar) as StructuredValue;

                if (defaultValue != null)
                {
                    //change some fields for default object
                    foreach (var field in defaultValue.Fields)
                    {
                        if (field.Value is string)
                        {
                            field.Value = "new string value";
                        }
                        else if (field.Value is int)
                        {
                            field.Value = 100;
                        }
                    }
                    //write new value to node StaticComplexNodeId
                    DataValue valueToWrite = new DataValue();
                    valueToWrite.Value = defaultValue;

                    //create WriteValue that will be sent to the ClientSession instance 
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(StaticComplexNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);
                    Console.WriteLine("\n The NodeId:{0} was written with the complex value {1} ", StaticComplexNodeId,
                        defaultValue);
                    Console.WriteLine(" Status code is {0}", statusCode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
                UserIdentity userIdentity = new UserIdentity();
                // create the session object.            
                m_session = m_application.CreateSession(Constants.ServerUrl,
                    MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, userIdentity, null);
                m_session.SessionName = SessionName;

                //connect session
                m_session.Connect(false, true);

                Console.WriteLine("Session is connected.");

                //wait until type dictionaries are loaded
                if (m_application.Configuration.DecodeCustomDataTypes)
                {
                    while (!m_session.TypeDictionariesLoaded)
                    {
                        Task.Delay(500).Wait();
                    }
                }
                Console.WriteLine("Session - TypeDictionariesLoaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateSession Error: {0}", ex.Message);
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
        public virtual void DisconnectSession()
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
                Console.WriteLine("DisconnectSession Error: {0}", ex.Message);
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
