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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Java.Lang;
using Opc.Ua;
using SampleClientXamarin.Helpers;
using SampleClientXamarin.Models;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.Nodes;
using Softing.Opc.Ua.Types;
using Xamarin.Forms;
using Exception = System.Exception;

namespace SampleClientXamarin.ViewModels
{
    /// <summary>
    /// View Model for readWritePage
    /// </summary>
    class ReadWriteViewModel : BaseViewModel
    {
        #region Private fields
        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\Int32
        const string StaticInt32NodeId = "ns=7;s=Scalar_Static_Int32";
        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\Guid
        const string StaticGuidNodeId = "ns=7;s=Scalar_Static_Guid";
        //Browse path: Root\Objects\CTT\Scalar\Scalar_Static\DateTime 
        const string StaticDateTimeNodeId = "ns=7;s=Scalar_Static_DateTime";

        Random m_random = new Random();
        private const string SessionName = "BrowseClient Session";
        private ClientSession m_session;
        private string m_sessionStatusText;
        private string m_operationStatusText;
        private string m_sampleServerUrl;
        private OperationTarget m_selectedOperationTarget;

        private UInt32 m_uint32NodeValue;
        private BaseNode m_baseNode;
        private ObservableCollection<NodeValueItem> m_arrayValue;
        private ComplexValueItem m_complexValue;
        private EnumValue m_enumValue;
        private NodeValueItem m_dateTimeNodeValue;
        private NodeValueItem m_guidNodeValue;
        private NodeValueItem m_in32NodeValue;
        #endregion

        #region Constructors

        /// <summary>
        /// Create new instance of ReadWriteViewModel
        /// </summary>
        public ReadWriteViewModel()
        {
            Title = "Read and write sample";
            m_arrayValue = new ObservableCollection<NodeValueItem>();
            SampleServerUrl = "opc.tcp://192.168.150.166:61510/SampleServer";
            ThreadPool.QueueUserWorkItem(o => InitializeSession());

            OperationTargets = new List<OperationTarget>()
            {
                OperationTarget.VariableNode,
                OperationTarget.ObjectNode,
                OperationTarget.ValueForNode,
                OperationTarget.ArrayValue, 
                OperationTarget.ComplexValue,
                OperationTarget.EnumerationValue,
                OperationTarget.MultipleNodes,
                
            };

            SelectedOperationTarget = OperationTargets[0];
        }

        #endregion

        #region Properties
        /// <summary>
        /// Node id for scalar UInt32 node
        /// Browse path: Root\Objects\CTT\Scalar\Scalar_Static\UInt32
        /// </summary>
        public string UInt32NodeId
        {
            get { return "ns=7;s=Scalar_Static_UInt32"; }
        }
        /// <summary>
        /// Node id for ObjectIds.Server node
        /// </summary>
        public string ServerNodeId
        {
            get { return ObjectIds.Server.ToString(); }
        }

        /// <summary>
        /// Node id for scalar UInt32 node
        /// Browse path: Root\Objects\CTT\Scalar\Scalar_Static\Arrays\Int64
        /// </summary>
        public string Int64ArrayNodeId
        {
            get { return "ns=7;s=Scalar_Static_Arrays_Int64"; }
        }

        /// <summary>
        /// Node id for complex value node
        /// Browse path: Root\Objects\CTT\StructuredTypeVariables\DataType5Variable
        /// </summary>
        public string ComplexNodeId
        {
            get { return "ns=7;i=13"; }
        }

        /// <summary>
        /// Node id for enumeration node
        /// Browse path: Root\Objects\CTT\StructuredTypeVariables\EnumerationType1Variable
        /// </summary>
        public string EnumNodeId
        {
            get { return "ns=7;i=14"; }
        }

        /// <summary>
        /// SampleServer Url
        /// </summary>
        public string SampleServerUrl
        {
            get { return m_sampleServerUrl; }
            set
            {
                SetProperty(ref m_sampleServerUrl, value);
                //disconnect existing session
                DisconnectSession();
            }
        }

        /// <summary>
        /// Text that indicates session status
        /// </summary>
        public string SessionStatusText
        {
            get { return m_sessionStatusText; }
            set { SetProperty(ref m_sessionStatusText, value); }
        }

        /// <summary>
        /// Text that indicates operation status
        /// </summary>
        public string OperationStatusText
        {
            get { return m_operationStatusText; }
            set { SetProperty(ref m_operationStatusText, value); }
        }

        /// <summary>
        /// List of possible operation targets
        /// </summary>
        public List<OperationTarget> OperationTargets { get; }

        /// <summary>
        /// Selected operation target
        /// </summary>
        public OperationTarget SelectedOperationTarget
        {
            get { return m_selectedOperationTarget; }
            set
            {
                SetProperty(ref m_selectedOperationTarget, value);
                OnPropertyChanged("CanWrite");
                BaseNode = null;
                m_arrayValue.Clear();
                ComplexValue = null;
                EnumValue = null;
                DateTimeNodeValue = null;
                Int32NodeValue = null;
                GuidNodeValue = null;
            }
        }

        /// <summary>
        /// Data keeper for UInt32 node value
        /// </summary>
        public UInt32 UInt32NodeValue
        {
            get { return m_uint32NodeValue; }
            set { SetProperty(ref m_uint32NodeValue, value); }
        }

        /// <summary>
        /// Node for used for reading
        /// </summary>
        public BaseNode BaseNode
        {
            get { return m_baseNode; }
            set {SetProperty(ref m_baseNode, value);}
        }

        /// <summary>
        /// Array value for read and write
        /// </summary>
        public ObservableCollection<NodeValueItem> ArrayValue
        {
            get { return m_arrayValue; }
            set{SetProperty(ref m_arrayValue, value);}
        }

        /// <summary>
        /// Complex value for read write complex values
        /// </summary>
        public ComplexValueItem ComplexValue
        {
            get { return m_complexValue; }
            set { SetProperty(ref m_complexValue, value); }
        }

        /// <summary>
        /// Enum value object read from the server
        /// </summary>
        public EnumValue EnumValue
        {
            get { return m_enumValue; }
            set
            {
                SetProperty(ref m_enumValue, value); 
                OnPropertyChanged("EnumValue.ValueString");
            }
        }
       
        /// <summary>
        /// NodeValueItem for date time node
        /// </summary>
        public NodeValueItem DateTimeNodeValue
        {
            get { return m_dateTimeNodeValue; }
            set { SetProperty(ref m_dateTimeNodeValue, value); }
        }

        /// <summary>
        /// NodeValueItem for guid node
        /// </summary>
        public NodeValueItem GuidNodeValue
        {
            get { return m_guidNodeValue; }
            set { SetProperty(ref m_guidNodeValue, value); }
        }

        /// <summary>
        /// NodeValueItem for int32 node
        /// </summary>
        public NodeValueItem Int32NodeValue
        {
            get { return m_in32NodeValue; }
            set { SetProperty(ref m_in32NodeValue, value); }
        }
        /// <summary>
        /// Decide if write operation is available
        /// </summary>
        public bool CanWrite
        {
            get
            {
                switch (SelectedOperationTarget)
                {
                    case OperationTarget.ValueForNode:
                    case OperationTarget.ArrayValue:
                    case OperationTarget.ComplexValue:
                    case OperationTarget.EnumerationValue:
                    case OperationTarget.MultipleNodes:
                        return IsBusy;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Flag that indicates if view is busy
        /// </summary>
        public new bool IsBusy
        {
            get { return base.IsBusy; }
            set
            {
                base.IsBusy = value;
                OnPropertyChanged("CanWrite");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Reads object from server
        /// </summary>
        public void Read()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = true;
                });
                switch (SelectedOperationTarget)
                {
                    case OperationTarget.VariableNode:
                        ReadVariableNode();
                        break;
                    case OperationTarget.ObjectNode:
                        ReadObjectNode();
                        break;
                    case OperationTarget.ValueForNode:
                        ReadValueForNode();
                        break;
                    case OperationTarget.ArrayValue:
                        ReadArrayValue();
                        break;
                    case OperationTarget.ComplexValue:
                        ReadComplexValue();
                        break;
                    case OperationTarget.EnumerationValue:
                        ReadEnumValue();
                        break;
                    case OperationTarget.MultipleNodes:
                        ReadMultipleNodesValues();
                        break;
                }
                Device.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = false;
                });
            });
        }

        /// <summary>
        /// Writes object to server
        /// </summary>
        public void Write()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = true;
                });
                switch (SelectedOperationTarget)
                {
                    case OperationTarget.ValueForNode:
                        WriteValueForNode();
                        break;
                    case OperationTarget.ArrayValue:
                        WriteArrayValueForNode();
                        break;
                    case OperationTarget.ComplexValue:
                        WriteComplexValueForNode();
                        break;
                    case OperationTarget.EnumerationValue:
                        WriteEnumValueForNode();
                        break;
                    case OperationTarget.MultipleNodes:
                        WriteMultipleNodesValues();
                        break;
                }
                Device.BeginInvokeOnMainThread(() =>
                {
                    IsBusy = false;
                });
            });
        }
        #endregion

        #region Initialize & DisconnectSession

        /// <summary>
        /// Initialize session object
        /// </summary>
        public void InitializeSession()
        {
            IsBusy = true;
            if (m_session == null)
            {
                try
                {
                    // create the session object with no security and anonymous login    
                    m_session = SampleApplication.UaApplication.CreateSession(SampleServerUrl);
                    m_session.SessionName = SessionName;

                    m_session.Connect(false, true);

                    SessionStatusText = "Connected";
                }
                catch (Exception ex)
                {
                    SessionStatusText = "Not connected - CreateSession Error: " + ex.Message;

                    if (m_session != null)
                    {
                        m_session.Dispose();
                        m_session = null;
                    }
                }
            }
            IsBusy = false;
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        public void DisconnectSession()
        {
            SessionStatusText = "";
            if (m_session == null)
            {
                SessionStatusText = "The Session was not created.";
                return;
            }

            try
            {
                m_session.Disconnect(true);
                m_session.Dispose();
                m_session = null;

                SessionStatusText = "Disconnected";
            }
            catch (Exception ex)
            {
                SessionStatusText = "DisconnectSession Error: " + ex.Message;
            }
        }

        #endregion

        #region Private Methods - Read
        /// <summary>
        /// Reads a variable node with all its attributes.
        /// </summary>
        public void ReadVariableNode()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "ReadVariableNode no session available.";
                    return;
                }
            }

            try
            {
                NodeId nodeId = new NodeId(UInt32NodeId);
                BaseNode = m_session.ReadNode(nodeId);
                if (BaseNode == null)
                {
                    OperationStatusText = string.Format("The NodeId:{0} does not exist in the Address Space", UInt32NodeId);
                }
                else
                {
                    OperationStatusText = "Good";
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadVariableNode error:" + e.Message;
            }
        }

        /// <summary>
        /// Reads an object node with all its attributes.
        /// </summary>
        public void ReadObjectNode()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "ReadObjectNode no session available.";
                    return;
                }
            }

            try
            {
                //Browse path: Root\Objects\Server
                BaseNode = m_session.ReadNode( new NodeId(ServerNodeId));
                if (BaseNode == null)
                {
                    OperationStatusText = string.Format("The NodeId:{0} does not exist in the Address Space", ServerNodeId);
                }
                else
                {
                    OperationStatusText = "Good";
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadObjectNode error:" + e.Message;
            }
        }

        /// <summary>
        /// Reads value for an uint node providing the NodeID and without read the whole node information.
        /// </summary>
        public void ReadValueForNode()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "ReadValueForNode no session available.";
                    return;
                }
            }

            ReadValueId readValueId = new ReadValueId();
            readValueId.NodeId = new NodeId(UInt32NodeId);
            readValueId.AttributeId = Attributes.Value;
            
            try
            {
                DataValueEx dataValue = m_session.Read(readValueId);
                UInt32NodeValue = (uint) dataValue.Value;
                OperationStatusText = dataValue.StatusCode.ToString();
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadValueForNode error:" +  e.Message;
            }
        }

        /// <summary>
        ///  Reads value for an array node providing the NodeID and without read the whole node information.
        /// </summary>
        public void ReadArrayValue()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "ReadArrayValue no session available.";
                    return;
                }
            }
            ReadValueId readValueId = new ReadValueId();
            readValueId.NodeId = new NodeId(Int64ArrayNodeId);
            readValueId.AttributeId = Attributes.Value;
            
            try
            {
                DataValueEx dataValue = m_session.Read(readValueId);
                Array array = dataValue.Value as Array;
                ArrayValue.Clear();
                foreach (long item in array)
                {
                    ArrayValue.Add(new NodeValueItem(){Value = item});
                }
                
                if (ArrayValue == null)
                {
                    OperationStatusText = string.Format("The NodeId:{0} does not exist in the Address Space", Int64ArrayNodeId);
                }
                else
                {
                    OperationStatusText = dataValue.StatusCode.ToString();
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadArrayValue error:" + e.Message;
            }
        }

        /// <summary>
        ///  Reads value for a complex node providing the NodeID and without read the whole node information.
        /// </summary>
        public void ReadComplexValue()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "ReadComplexValue no session available.";
                    return;
                }
            }
            //ensure type dictionaries are loaded
            if (SampleApplication.UaApplication.Configuration.DecodeCustomDataTypes)
            {
                while (!m_session.TypeDictionariesLoaded)
                {
                    Task.Delay(500).Wait();
                }
            }
            ReadValueId readValueId = new ReadValueId();
            readValueId.NodeId = new NodeId(ComplexNodeId);
            readValueId.AttributeId = Attributes.Value;
            
            try
            {
                DataValueEx dataValue = m_session.Read(readValueId);

                //display information for read value
                OperationStatusText = dataValue.StatusCode.ToString();

                StructuredValue structuredValue = dataValue.ProcessedValue as StructuredValue;
                if (structuredValue == null)
                {
                    ComplexValue = null;
                }
                else
                {
                    ComplexValue = new ComplexValueItem();
                    foreach (StructuredField field in structuredValue.Fields)
                    {
                        ComplexValueFieldItem item = null;
                        object value = structuredValue[field.Name];
                        if ((value is EnumValue) || (value is StructuredValue))
                        {
                            item = new ComplexValueFieldItem(field.Name,
                                structuredValue[field.Name].GetType().Name,
                                value.ToString());
                        }
                        else
                        {
                            item = new ComplexValueFieldItem(field.Name,
                                structuredValue[field.Name].GetType().Name,
                                value);
                            item.IsEditable = true;
                        }
                        ComplexValue.Fields.Add(item);
                    }
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadComplexValue error:" + e.Message;
            }
        }

        /// <summary>
        ///  Reads value for an enum node providing the NodeID and without read the whole node information.
        /// </summary>
        public void ReadEnumValue()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "ReadEnumValue no session available.";
                    return;
                }
            }
            //ensure type dictionaries are loaded
            if (SampleApplication.UaApplication.Configuration.DecodeCustomDataTypes)
            {
                while (!m_session.TypeDictionariesLoaded)
                {
                    Task.Delay(500).Wait();
                }
            }

            NodeId nodeId = new NodeId(EnumNodeId);
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

                        DataValueEx dataValue = m_session.Read(readValueId);
                        //convert int32 value read from node to a well known enumeration type
                        dataValue.TryConvertToEnumValue(m_session, variableNode.DataTypeId, variableNode.ValueRank);
                        
                        EnumValue = dataValue.ProcessedValue as EnumValue;
                        
                        //display information for read value
                        OperationStatusText = dataValue.StatusCode.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadEnumValue error:" + e.Message;
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
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "ReadMultipleNodesValues no session available.";
                    return;
                }
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
            
            try
            {
                OperationStatusText = "";
                IList<DataValueEx> dataValues = m_session.Read(listOfNodes, 0, TimestampsToReturn.Both);
                for (int i = 0; i < listOfNodes.Count; i++)
                {
                    NodeValueItem nodeValueItem = new NodeValueItem();
                    nodeValueItem.NodeId = listOfNodes[i].NodeId.ToString();
                    nodeValueItem.Value = dataValues[i].Value;
                    nodeValueItem.TypeName = dataValues[i].Value.GetType().Name;
                    
                    switch (listOfNodes[i].NodeId.ToString())
                    {
                        case StaticDateTimeNodeId:
                            DateTimeNodeValue = nodeValueItem;
                            break;
                        case StaticGuidNodeId:
                            GuidNodeValue = nodeValueItem;
                            GuidNodeValue.Value = dataValues[i].Value.ToString();
                            break;
                        case StaticInt32NodeId:
                            Int32NodeValue = nodeValueItem;
                            break;
                    }
                    
                    OperationStatusText += dataValues[i].StatusCode + ",";
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadMultipleNodesValues error:" + e.Message;
            }
        }
        #endregion

        #region Private Methods - Write
        /// <summary>
        /// Writes a value for an uint node providing the NodeID. The written value is provided by the user.
        /// </summary>
        private void WriteValueForNode()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "WriteValueForNode no session available.";
                    return;
                }
            }

            //first value to write
            WriteValue writeValue = new WriteValue();
            writeValue.AttributeId = Attributes.Value;
            writeValue.NodeId = new NodeId(UInt32NodeId);

            DataValue valueToWrite = new DataValue();
            valueToWrite.Value = UInt32NodeValue;

            writeValue.Value = valueToWrite;

            try
            {
                StatusCode statusCode = m_session.Write(writeValue);
                OperationStatusText = statusCode.ToString();
            }
            catch (Exception e)
            {
                OperationStatusText = "WriteValueForNode error:" + e.Message;
            }
        }

        /// <summary>
        /// Writes a value for an array node providing the NodeID. The written values in array are random generated for a nice output.
        /// </summary>
        public void WriteArrayValueForNode()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "WriteValueForNode no session available.";
                    return;
                }
            }

            WriteValue writeValue = new WriteValue();
            writeValue.AttributeId = Attributes.Value;
            writeValue.NodeId = new NodeId(Int64ArrayNodeId);

            DataValue valueToWrite = new DataValue();
            List<long> values = new List<long>();
            foreach (NodeValueItem item in ArrayValue)
            {
                long value = 0;
                long.TryParse(item.Value.ToString(), out value);
                values.Add(value);
            }

            valueToWrite.Value = values.ToArray();

            writeValue.Value = valueToWrite;
            try
            {
                StatusCode statusCode = m_session.Write(writeValue);
                OperationStatusText = statusCode.ToString();
            }
            catch (Exception e)
            {
                OperationStatusText = "WriteArrayValueForNode error:" + e.Message;
            }
        }

        /// <summary>
        /// Writes a value for a complex node providing the NodeID. Some written values are random generated for a nice output.
        /// </summary>
        public void WriteComplexValueForNode()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "WriteComplexValueForNode no session available.";
                    return;
                }
            }
            //ensure type dictionaries are loaded
            if (SampleApplication.UaApplication.Configuration.DecodeCustomDataTypes)
            {
                while (!m_session.TypeDictionariesLoaded)
                {
                    Task.Delay(500).Wait();
                }
            }
            try
            {
                //read data type id for node StaticComplexNodeId
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(ComplexNodeId);
                readValueId.AttributeId = Attributes.DataType;

                DataValueEx dataValuetypeId = m_session.Read(readValueId);

                //Get Default value for data type
                StructuredValue defaultValue = m_session.GetDefaultValueForDatatype(dataValuetypeId.Value as NodeId, ValueRanks.Scalar) as StructuredValue;

                if (defaultValue != null)
                {
                    if (ComplexValue == null)
                    {
                        //change some fields for default object
                        foreach (var field in defaultValue.Fields)
                        {
                            if (field.Value is string)
                            {
                                field.Value = "dummy string value";
                            }
                        }
                    }
                    else
                    {
                        //copy edited values to object to be written
                        foreach (var field in ComplexValue.Fields)
                        {
                            if (field.IsEditable)
                            {
                                switch (field.TypeName)
                                {
                                    case "String":
                                    defaultValue[field.FieldName] = field.Value;
                                        break;
                                    case "Int32":
                                        int intValue = 0;
                                        int.TryParse(field.Value.ToString(), out intValue);
                                        defaultValue[field.FieldName] = intValue;
                                        break;
                                    case "Single":
                                        Single singleValue = 0;
                                        Single.TryParse(field.Value.ToString(), out singleValue);
                                        defaultValue[field.FieldName] = singleValue;
                                        break;
                                }
                               
                            }
                        }
                    }
                    //write new value to node StaticComplexNodeId
                    DataValue valueToWrite = new DataValue();
                    valueToWrite.Value = defaultValue;

                    //create WriteValue that will be sent to the ClientSession instance 
                    WriteValue writeValue = new WriteValue();
                    writeValue.AttributeId = Attributes.Value;
                    writeValue.NodeId = new NodeId(ComplexNodeId);
                    writeValue.Value = valueToWrite;

                    StatusCode statusCode = m_session.Write(writeValue);

                    OperationStatusText = statusCode.ToString();
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "WriteComplexValueForNode error:" + e.Message;
            }
        }

        /// <summary>
        /// Writes a value for an enum node providing the NodeID. If no value available the written value is random generated for a nice output.
        /// </summary>
        public void WriteEnumValueForNode()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "WriteComplexValueForNode no session available.";
                    return;
                }
            }
            //ensure type dictionaries are loaded
            if (SampleApplication.UaApplication.Configuration.DecodeCustomDataTypes)
            {
                while (!m_session.TypeDictionariesLoaded)
                {
                    Task.Delay(500).Wait();
                }
            }

            WriteValue writeValue = new WriteValue();
            writeValue.AttributeId = Attributes.Value;
            writeValue.NodeId = new NodeId(EnumNodeId);

            DataValue valueToWrite = new DataValue();
            if (EnumValue != null)
            {
                valueToWrite.Value = EnumValue.Value;
            }
            else
            {
                valueToWrite.Value = m_random.Next(0, 3);
            }

            writeValue.Value = valueToWrite;
            try
            {
                StatusCode statusCode = m_session.Write(writeValue);

                OperationStatusText = statusCode.ToString();
            }
            catch (Exception e)
            {
                OperationStatusText = "WriteEnumValueForNode error:" + e.Message;
            }
        }


        /// <summary>
        /// Writes values for a list of nodes providing the NodeIDs.The list of nodes contains a uint, an GUID and a datetime node. 
        /// If no values available random values are generated 
        /// </summary>
        public void WriteMultipleNodesValues()
        {
            if (m_session == null)
            {
                //try to initialize session
                InitializeSession();
                if (m_session == null)
                {
                    OperationStatusText = "WriteMultipleNodesValues no session available.";
                    return;
                }
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
            if (Int32NodeValue == null)
            {
                valueToWrite.Value = m_random.Next(1, 1975109192);
            }
            else
            {
                int value = 0;
                int.TryParse(Int32NodeValue.Value.ToString(), out value);
                valueToWrite.Value = value;
            }
            listOfNodes[0].Value = valueToWrite;

            DataValue valueToWrite1 = new DataValue();
            if (GuidNodeValue == null)
            {
                valueToWrite1.Value = Guid.NewGuid();
            }
            else
            {
                Guid guid = Guid.NewGuid();
                Guid.TryParse(GuidNodeValue.Value.ToString(), out guid);
                valueToWrite1.Value = guid;
            }
            listOfNodes[1].Value = valueToWrite1;

            DataValue valueToWrite2 = new DataValue();
            if (DateTimeNodeValue == null)
            {
                valueToWrite2.Value = DateTime.Now;
            }
            else
            {
                DateTime dateTime = DateTime.Now;
                DateTime.TryParse(DateTimeNodeValue.Value.ToString(), out dateTime);
                valueToWrite2.Value = dateTime;
            }

            listOfNodes[2].Value = valueToWrite2;
            
            try
            {
                IList<StatusCode> statusCodes = m_session.Write(listOfNodes);
                OperationStatusText = "";
                for (int i = 0; i < listOfNodes.Count; i++)
                {
                     OperationStatusText += statusCodes[i] + ",";
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "WriteMultipleNodesValues error:" + e.Message;
            }
        }
        #endregion
    }
}
