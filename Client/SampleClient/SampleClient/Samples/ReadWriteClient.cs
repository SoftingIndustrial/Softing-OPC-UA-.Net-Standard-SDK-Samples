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
using Opc.Ua;
using Softing.Opc.Ua;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.Nodes;

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

        //Browse path: Root\Objects\Data\Static\Scalar\UInt32Value
        const string StaticValueNodeIDVar = "ns=3;i=10222";
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

       
        #region Public Methods
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
            NodeId nodeID = new NodeId(StaticValueNodeIDVar);
            try
            {
                BaseNode baseNode = m_session.ReadNode(nodeID);
                if (baseNode == null)
                {
                    Console.WriteLine("\n The NodeId:{0} does not exist in the Address Space", StaticValueNodeIDVar);
                    return;
                }
                Console.WriteLine("\n Read node with NodeId({0}):", StaticValueNodeIDVar);
                Console.WriteLine("  DisplayName Name is '{0}'", baseNode.DisplayName.Text);
                Console.WriteLine("  Browse Name is '{0}'", baseNode.BrowseName.Name);
                Console.WriteLine("  Description is '{0}'", baseNode.Description.Text);
                Console.WriteLine("  NodeClass is '{0}'", baseNode.NodeClass.ToString());
                if (baseNode.NodeClass == NodeClass.Variable)
                {
                    VariableNodeEx variableNode = baseNode as VariableNodeEx;
                    Console.WriteLine("  DataType is  {0}", variableNode.DataType);
                    Console.WriteLine("  Value Rank is  {0}", variableNode.ValueRank.ToString());
                    DisplayInformationForDataValue(variableNode.Value);
                    Console.WriteLine("  Value is  {0}", variableNode.UserAccessLevelText);
                    Console.WriteLine("  Value is Historizing: {0}", variableNode.Historizing);
                    Console.WriteLine("  Value sampling interval: {0}", variableNode.MinimumSamplingIntervalText);
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
           //Browse path: Root\Objects\Server
           NodeId nodeId = ObjectIds.Server;
           try
           {
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
               Console.WriteLine("  NodeClass is '{0}'", baseNode.NodeClass.ToString());
               if (baseNode.NodeClass == NodeClass.Object)
               {
                   ObjectNodeEx variableNode = baseNode as ObjectNodeEx;
                   Console.WriteLine("  EventNotifier :  {0}", variableNode.EventNotifierText);
               }
           }
           catch (Exception e)
           {
               Console.WriteLine(e.Message);
           }
       }
        /*
      /// <summary>
      /// Reads value for an uint node providing the NodeID and without read the whole node information.
      /// </summary>
      /// <param name="session">The session connected to a server which provides the Address Space where NodeId resides. </param>
      public void ReadSimpleNodeValue(Session session)
      {
          ReadValueId readValueId = new ReadValueId();
          readValueId.NodeId = new NodeId(StaticValueNodeIDVar);
          readValueId.AttributeId = AttributeId.Value;

          Console.WriteLine("\n Read value for NodeId:{0}", StaticValueNodeIDVar);
          try
          {
              DataValue dataValue = session.Read(readValueId);
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
      /// <param name="session">The session connected to a server which provides the Address Space where NodeId resides. </param>
      public void ReadArrayValue(Session session)
      {
          //Browse path: Root\Objects\Server\Data\Static\Array\Int64Value
          const string StaticValueNodeIDArray = "ns=3;i=10307";

          ReadValueId readValueId = new ReadValueId();
          readValueId.NodeId = new NodeId(StaticValueNodeIDArray);
          readValueId.AttributeId = AttributeId.Value;

          Console.WriteLine("\n Read array value for NodeId:{0}", StaticValueNodeIDArray);
          try
          {
              DataValue dataValue = session.Read(readValueId);

              //display read information
              Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode.ToString());
              if (dataValue.Value is Array)
              {
                  Console.WriteLine("  Value is an array with values:");
                  Array array = dataValue.Value as Array;
                  foreach (object obj in array)
                  {
                      Console.WriteLine("   {0}", obj.ToString());
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
      /// <param name="session">The session connected to a server which provides the Address Space where NodeId resides. </param>
      public void ReadComplexValue(Session session)
      {
          //Browse path: Root\Objects\Refrigerators\Refrigerator #1\RefrigeratorStatus
          const string StaticValueNodeIDComplex = "ns=10;i=13";

          ReadValueId readValueId = new ReadValueId();
          readValueId.NodeId = new NodeId(StaticValueNodeIDComplex);
          readValueId.AttributeId = AttributeId.Value;

          Console.WriteLine("\n Read complex value for NodeId:{0}", StaticValueNodeIDComplex);
          try
          {
              DataValue dataValue = session.Read(readValueId);

              //display information for read value
              Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode.ToString());

              if (dataValue.Value is StructuredValue)
              {
                  StructuredValue complexData = dataValue.Value as StructuredValue;
                  Console.WriteLine("  Value is 'Structured Value' with fields: ");
                  foreach (StructuredField field in complexData.Fields)
                  {
                      Console.WriteLine("   Field: {0} Value:{1} Type:{2} ", field.Name, complexData[field.Name], complexData[field.Name].GetType().Name);
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
      /// <param name="session">The session connected to a server which provides the Address Space where NodeId resides. </param>
      public void ReadEnumValue(Session session)
      {
          //Browse path: Root\Objects\Refrigerators\Refrigerator #1\State
          const string HardcodedValueNodeID = "ns=10;i=16";

          NodeId nodeID = new NodeId(HardcodedValueNodeID);
          try
          {
              BaseNode baseNode = session.ReadNode(nodeID);

              if (baseNode.NodeClass == NodeClass.Variable)
              {
                  VariableNode variableNode = baseNode as VariableNode;

                  ReadValueId readValueId = new ReadValueId();
                  readValueId.NodeId = variableNode.NodeId;
                  readValueId.AttributeId = AttributeId.Value;

                  Console.WriteLine("\n Read enum value for Node: {0} (NodeId:{1})", variableNode.DisplayName, HardcodedValueNodeID);
                  DataValue dataValue = session.Read(readValueId);
                  dataValue.TryConvertToEnumValue(variableNode.DataTypeId, variableNode.ValueRank, session);

                  //display information for read value
                  Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode.ToString());
                  if (dataValue.Value is EnumValue)
                  {
                      EnumValue enumValue = dataValue.Value as EnumValue;
                      Console.WriteLine("  Value is an enum with value: {0}", enumValue.ValueString);
                      Console.WriteLine("  All possible values for this Enum are :");
                      List<string> allPossibleVals = new List<string>(enumValue.ValueStrings);
                      for (int i = 0; i < allPossibleVals.Count; i++)
                      {
                          Console.WriteLine("   {0}   ", allPossibleVals[i]);
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
      /// <param name="session">The session connected to a server which provides the Address Space where NodeId resides. </param>
      public void ReadMultipleNodesValues(Session session)
      {
          //Browse path: Root\Objects\Server\Data\Static\Scalar\Int32Value
          const string StaticValueNodeID = "ns=3;i=10221";
          //Browse path: Root\Objects\Server\Data\Static\Scalar\GuidValue
          const string StaticValueNodeID1 = "ns=3;i=10229";
          //Browse path: Root\Objects\Server\Data\Static\Scalar\DateTimeValue
          const string StaticValueNodeID2 = "ns=3;i=10228";

          List<ReadValueId> listOfNodes = new List<ReadValueId>()
          {
              new ReadValueId()
              {
                  NodeId = new NodeId(StaticValueNodeID),
                  AttributeId = AttributeId.Value
              },
              new ReadValueId()
              {
                  NodeId = new NodeId(StaticValueNodeID1),
                  AttributeId = AttributeId.Value
              },
              new ReadValueId()
              {
                  NodeId = new NodeId(StaticValueNodeID2),
                  AttributeId = AttributeId.Value
              }
          };

          Console.WriteLine("\n Read value for multiple nodes: ");
          try
          {
              IList<DataValue> dataValues = session.Read(listOfNodes, 0, TimestampsToReturn.Both);
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

      /// <summary>
      /// Displays information at console for a read DataValue.
      /// </summary>
      /// <param name="dataValue">Value that is provided for displaying information </param>
      private void DisplayInformationForDataValue(DataValue dataValue)
      {
          Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode.ToString());
          Console.WriteLine("  Data Value is {0}.", dataValue.Value);
      }*/
        #endregion

        #region InitializeSession & DisconnectSession
        /// <summary>
        /// Initialize session object
        /// </summary>
        public void InitializeSession()
        {
            UserIdentity userIdentity = new UserIdentity();
            // create the session object.            
            m_session = m_application.CreateSession(Constants.SampleServerUrlOpcTcp,
                MessageSecurityMode.None, SecurityPolicy.None, MessageEncoding.Binary, userIdentity, null);
            m_session.SessionName = SessionName;

            try
            {
                //connect session
                m_session.Connect(false, true);

                Console.WriteLine("Session is connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateSession Error: {0}", ex.Message);
                m_session.Dispose();
                m_session = null;
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

        /// <summary>
        /// Displays information at console for a read DataValue.
        /// </summary>
        /// <param name="dataValue">Value that is provided for displaying information </param>
        private void DisplayInformationForDataValue(DataValue dataValue)
        {
            Console.WriteLine("  Status Code is {0}.", dataValue.StatusCode.ToString());
            Console.WriteLine("  Data Value is {0}.", dataValue.Value);
        }
    }
}
