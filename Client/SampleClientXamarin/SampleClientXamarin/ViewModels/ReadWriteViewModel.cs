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
using Java.Lang;
using Opc.Ua;
using SampleClientXamarin.Helpers;
using SampleClientXamarin.Models;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.Nodes;
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

       
        //Browse path: Root\Objects\CTT\StructuredTypeVariables\EnumerationType1Variable
        const string StaticEnumNodeId = "ns=7;i=14";
        //Browse path: Root\Objects\CTT\StructuredTypeVariables\DataType5Variable
        const string StaticComplexNodeId = "ns=7;i=13";

        private const string SessionName = "BrowseClient Session";
        private ClientSession m_session;
        private string m_sessionStatusText;
        private string m_operationStatusText;
        private string m_sampleServerUrl;
        private OperationTarget m_selectedOperationTarget;

        private UInt32 m_uint32NodeValue;
        private BaseNode m_baseNode;
        private ObservableCollection<Int64Item> m_arrayValue;
        #endregion

        #region Constructors

        /// <summary>
        /// Create new instance of ReadWriteViewModel
        /// </summary>
        public ReadWriteViewModel()
        {
            Title = "Read and write sample";
            m_arrayValue = new ObservableCollection<Int64Item>();
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
        public ObservableCollection<Int64Item> ArrayValue
        {
            get { return m_arrayValue; }
            set{SetProperty(ref m_arrayValue, value);}
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
                        return true;
                    default:
                        return false;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Reads object from server
        /// </summary>
        public void Read()
        {
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
            }
        }

        /// <summary>
        /// Writes object to server
        /// </summary>
        public void Write()
        {
            switch (SelectedOperationTarget)
            {
                case OperationTarget.ValueForNode:
                    WriteValueForNode();
                    break;
                case OperationTarget.ArrayValue:
                    WriteArrayValueForNode();
                    break;
            }
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
            IsBusy = true;
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
                    OperationStatusText = "ReadVariableNode status code: Good";
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadVariableNode error:" + e.Message;
            }
            IsBusy = false;
        }

        /// <summary>
        /// Reads an object node with all its attributes.
        /// </summary>
        public void ReadObjectNode()
        {
            IsBusy = true;
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
                    OperationStatusText = "ReadVariableNode status code: Good";
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadObjectNode error:" + e.Message;
            }
            IsBusy = false;
        }

        /// <summary>
        /// Reads value for an uint node providing the NodeID and without read the whole node information.
        /// </summary>
        public void ReadValueForNode()
        {
            IsBusy = true;
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
                OperationStatusText = "ReadValueForNode status code: " + dataValue.StatusCode;
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadValueForNode error:" +  e.Message;
            }
            IsBusy = false;
        }

        /// <summary>
        ///  Reads value for an array node providing the NodeID and without read the whole node information.
        /// </summary>
        public void ReadArrayValue()
        {
            IsBusy = true;
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
            readValueId.NodeId = new NodeId(Int64ArrayNodeId);
            readValueId.AttributeId = Attributes.Value;
            
            try
            {
                DataValueEx dataValue = m_session.Read(readValueId);
                Array array = dataValue.Value as Array;
                ArrayValue.Clear();
                foreach (long item in array)
                {
                    ArrayValue.Add(new Int64Item(){Value = item});
                }
                
                if (ArrayValue == null)
                {
                    OperationStatusText = string.Format("The NodeId:{0} does not exist in the Address Space", Int64ArrayNodeId);
                }
                else
                {
                    OperationStatusText = "ReadArrayValue status code: " + dataValue.StatusCode;
                }
            }
            catch (Exception e)
            {
                OperationStatusText = "ReadArrayValue error:" + e.Message;
            }
            IsBusy = false;
        }
        #endregion

        #region Private Methods - Write
        /// <summary>
        /// Writes a value for an uint node providing the NodeID. The written value is provided by the user.
        /// </summary>
        private void WriteValueForNode()
        {
            IsBusy = true;
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
                OperationStatusText = "WriteValueForNode status code: " + statusCode;
            }
            catch (Exception e)
            {
                OperationStatusText = "WriteValueForNode error:" + e.Message;
            }
            IsBusy = false;
        }

        /// <summary>
        /// Writes a value for an array node providing the NodeID. The written values in array are random generated for a nice output.
        /// </summary>
        public void WriteArrayValueForNode()
        {
            IsBusy = true;
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
            foreach (Int64Item item in ArrayValue)
            {
                values.Add(item.Value);
            }

            valueToWrite.Value = values.ToArray();

            writeValue.Value = valueToWrite;
            try
            {
                StatusCode statusCode = m_session.Write(writeValue);
                OperationStatusText = "WriteArrayValueForNode status code: " + statusCode;
            }
            catch (Exception e)
            {
                OperationStatusText = "WriteArrayValueForNode error:" + e.Message;
            }
            IsBusy = false;
        }
        #endregion
    }
}
