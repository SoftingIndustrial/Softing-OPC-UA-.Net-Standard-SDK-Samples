using Opc.Ua;
using Opc.Ua.Toolkit;
using Opc.Ua.Toolkit.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleClient
{
    class Client
    {
        #region Private Members
        private const string m_demoServerUrl = "opc.tcp://localhost:51510/UA/DemoServer";


        private Session m_session = null;
        private NamespaceTable m_namespaceUris;
        private Subscription m_subscription = null;
        private Application m_application;
        private List<MonitoredItem> m_monitoredItems = new List<MonitoredItem>();
        //Browse path: Root\Objects\Server
        private static NodeId m_eventSourceNodeId = new NodeId("ns=0;i=2253");

        //Browse path: Root\Types\ObjectTypes\BaseObjectType\BaseEventType
        private static NodeId m_selectEventTypeId = new NodeId("ns=0;i=2041");

        //Browse path: Root\Types\ObjectTypes\BaseObjectType\BaseEventType
        private static NodeId m_eventTypeId = new NodeId("ns=5;i=265");

        private NodeId m_historianNodeId = new NodeId("ns=2;s=StaticHistoricalDataItem_Historian1");
        private static QualifiedName m_eventPropertyName = new QualifiedName("FluidLevel", 5);
        
        private MonitoredItem m_eventMonitoredItem;
        private MonitoredItem m_readWriteMonitoredItem = null;
        private Random m_randomGenerator = new Random();
        private int m_callIdentifier;

        //Browse path: Root\Objects\Data\Static\Scalar\Int16Value
        private const string m_readWriteNodeId = "ns=3;i=10219";

        //Browse path: Root\Objects\Data\Dynamic\Scalar\ByteValue
        private const string m_monitoredItemNodeId = "ns=3;i=10846";

        #endregion

        public Client(Application application)
        {
            m_application = application;
        }

        #region Session
        /// <summary>
        /// Creates a new session and connects it to the server.
        /// </summary>
        internal void CreateSession()
        {
            if (m_session != null)
            {
                Console.WriteLine("Session already created.");
                return;
            }

           UserNameIdentityToken userToken = new UserNameIdentityToken();
            userToken.UserName = "usr";
            userToken.DecryptedPassword = "pwd";


            UserIdentity userIdentity = new UserIdentity();
            // create the session object.            
            m_session = m_application.CreateSession(
               m_demoServerUrl,
               MessageSecurityMode.None,
               SecurityPolicy.None,
               MessageEncoding.Binary,
               userIdentity,
               null);
            m_session.CallCompleted += Session_CallCompleted;
            m_session.ContinuationPointReached += Session_ContinuationPointReached;
            m_session.SessionName = "Softing Browse Sample Client";
           
            try
            {
                m_session.Connect(false, true);
                Console.WriteLine("Session is connected.");

                m_namespaceUris = new NamespaceTable(m_session.NamespaceUris);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("CreateSession Error: {0}", ex.StackTrace));
            }

           // m_session.ContinuationPointReached += Session_ContinuationPointReached;
        }


        /// <summary>
        /// Disconnects the current session.
        /// </summary>
        internal void DisconnectSession()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                m_subscription = null;
                m_monitoredItems.Clear();
                m_readWriteMonitoredItem = null;
                m_eventMonitoredItem = null;

                m_session.CallCompleted -= Session_CallCompleted;
                m_session.ContinuationPointReached -= Session_ContinuationPointReached;
                m_session.Disconnect(false);
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("DisconnectSession Error: {0}", ex.Message));
            }

            m_session.Dispose();
            m_session = null;
        }

        #endregion

        #region Subscription
        /// <summary>
        /// Creates a subscription. The subscription is activated in the constructor if the session is active as well.
        /// </summary>
        internal void CreateSubscription()
        {
            if (m_session != null)
            {
                if (m_subscription == null)
                {
                    try
                    {
                        m_subscription = new Subscription(m_session, "SampleSubscription");

                        // set the Publishing interval for this subscription
                        m_subscription.PublishingInterval = 500;
                        m_subscription.DataChangesReceived += Subscription_DataChangesReceived;
                        Console.WriteLine("Subscription created");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Subscription already created");
                }
            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
            }
        }        

        /// <summary>
        /// Deletes the current subscription.
        /// </summary>
        internal void DeleteSubscription()
        {
            if (m_session != null)
            {
                if (m_subscription != null)
                {
                    m_subscription = null;
                    m_monitoredItems.Clear();
                    m_readWriteMonitoredItem = null;
                    m_eventMonitoredItem = null;

                    m_subscription.DataChangesReceived -= Subscription_DataChangesReceived;
                    m_session.DeleteSubscription(m_subscription);
                    m_subscription = null;
                    Console.WriteLine("Subscription deleted");
                }
                else
                {
                    Console.WriteLine("Subscription is not created, please use \"s\" command");
                }

            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
            }
        }


        private void Subscription_DataChangesReceived(object sender, DataChangesNotificationEventArgs e)
        {
            foreach (var dataChangeNotification in e.DataChangeNotifications)
            {
                Console.WriteLine(" {0} Received data value change for monitored item: '{1}' in subscription '{2}':", dataChangeNotification.SequenceNo, dataChangeNotification.MonitoredItem.DisplayName, ((Subscription)sender).DisplayName);
                Console.WriteLine("    Value : {0} ", dataChangeNotification.Value);
                Console.WriteLine("    StatusCode : {0} ", dataChangeNotification.Value.StatusCode);
                Console.WriteLine("    ServerTimestamp : {0}", dataChangeNotification.Value.ServerTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt"));
                Console.WriteLine("    SourceTimestamp : {0}", dataChangeNotification.Value.SourceTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt"));
            }
        }
        #endregion

        #region MonitoredItem
        /// <summary>
        /// Creates a monitoredItem. The monitored item is activated in the constructor if the subscription is active as well.
        /// </summary>
        internal void CreateMonitoredItem()
        {
            if (m_session != null)
            {
                if (m_subscription != null)
                {
                    try
                    {
                        NodeId node = new NodeId(m_monitoredItemNodeId);
                        MonitoredItem monitoredItem = new MonitoredItem(m_subscription, node, AttributeId.Value, null, "Sample Monitored Item" + m_monitoredItems.Count);
                        monitoredItem.DataChangesReceived += Monitoreditem_DataChangesReceived;
                        m_monitoredItems.Add(monitoredItem);

                        Console.WriteLine("Monitored item created. Data value changes are shown:");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Subscription is not created, please use \"s\" command");
                }
            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
            }
        }

        /// <summary>
        /// Handles the Notification event of the Monitoreditem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Softing.Opc.Ua.Toolkit.Client.MonitoredItemNotificationEventArgs"/> instance containing the event data.</param>
        private void Monitoreditem_DataChangesReceived(object sender, DataChangesNotificationEventArgs e)
        {
            return;
            foreach (var dataChangeNotification in e.DataChangeNotifications)
            {
                Console.WriteLine(" {0} Received data value change for monitored item:", dataChangeNotification.SequenceNo);
                Console.WriteLine("    Value : {0} ", dataChangeNotification.Value);
                Console.WriteLine("    StatusCode : {0} ", dataChangeNotification.Value.StatusCode);
                Console.WriteLine("    ServerTimestamp : {0}", dataChangeNotification.Value.ServerTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt"));
                Console.WriteLine("    SourceTimestamp : {0}", dataChangeNotification.Value.SourceTimestamp.ToLocalTime().ToString("hh:mm:ss.fff tt"));
            }
        }

        /// <summary>
        /// Reads the value of a node, using a monitored item.
        /// </summary>
        /// <returns>The datavalue for the read monitored item</returns>
        public DataValue ReadMonitoredItem()
        {
            if (m_session != null)
            {
                if (m_subscription != null)
                {
                    if (!m_subscription.MonitoredItems.Contains(m_readWriteMonitoredItem))
                    {
                        NodeId readWriteNodeId = new NodeId(m_readWriteNodeId);
                        try
                        {
                            m_readWriteMonitoredItem = new MonitoredItem(m_subscription, readWriteNodeId, AttributeId.Value, null, "SampleReadWriteMI");
                            Console.WriteLine("Created Monitored Item for read/write.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Monitored item could not created: " + ex.Message);
                        }
                    }
                    try
                    {
                        return m_readWriteMonitoredItem.Read();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("Subscription is not created, please use \"s\" command");
                    return null;
                }

            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return null;
            }
        }

        /// <summary>
        /// Writes a value to a node, using a monitored item.
        /// </summary>
        /// <returns>The status code of the write operation.</returns>
        public StatusCode WriteMonitoredItem()
        {
            if (m_session != null)
            {
                if (m_subscription != null)
                {
                    if (!m_subscription.MonitoredItems.Contains(m_readWriteMonitoredItem))
                    {
                        NodeId readWriteNodeId = new NodeId(m_readWriteNodeId);
                        try
                        {
                            m_readWriteMonitoredItem = new MonitoredItem(m_subscription, readWriteNodeId, AttributeId.Value, null, "SampleReadWriteMI");
                            Console.WriteLine("Created Monitored Item for read/write.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Monitored item could not created: " + ex.Message);
                        }
                    }
                    DataValue m_dataValue = new DataValue();
                    m_dataValue.Value = (Int16)m_randomGenerator.Next(1, 755);
                  //  m_dataValue.ValueRank = ValueRanks.Scalar;
                    m_dataValue.StatusCode = new StatusCode();
                    Console.WriteLine("Generated value: {0} to write.", m_dataValue.Value);
                    try
                    {
                        return m_readWriteMonitoredItem.Write(m_dataValue);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return StatusCodes.Bad;
                    }
                }
                else
                {
                    Console.WriteLine("Subscription is not created, please use \"s\" command");
                    return StatusCodes.Bad;
                }

            }
            else
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return StatusCodes.Bad;
            }
        }

        /// <summary>
        /// Deletes the current MonitoredItem.
        /// </summary>
        internal void DeleteMonitoredItem()
        {
            if (m_monitoredItems.Count == 0)
            {
                Console.WriteLine("Monitored item is not created, please use \"m\" command");
                return;
            }
            try
            {
                MonitoredItem monitoredItem = m_monitoredItems[m_monitoredItems.Count - 1];
                monitoredItem.DataChangesReceived -= Monitoreditem_DataChangesReceived;
                Console.WriteLine("Monitored item unsubscribed from receiving data change notifications.");
                monitoredItem.Delete();
                m_monitoredItems.Remove(monitoredItem);
                Console.WriteLine("Monitored item deleted. ");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Monitored deleted error: " + ex.Message);
            }
        }
        #endregion

        #region Event Monitored Items
        /// <summary>
        /// Creates the event monitored item.
        /// </summary>
        public void CreateEventMonitoredItem()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created.");
                return;
            }
            if (m_subscription == null)
            {
                Console.WriteLine("Subscription is not created.");
                return;
            }
            if (m_eventMonitoredItem != null)
            {
                Console.WriteLine("EventMonitoredItem is already created.");
                return;
            }

            try
            {
                m_eventMonitoredItem = new MonitoredItem(m_subscription, m_eventSourceNodeId, "Sample Event Monitored Item", null);
                m_eventMonitoredItem.EventsReceived += m_eventMonitoredItem_EventsReceived;
                Console.WriteLine("Event Monitored Item is created and connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Creates the event monitored item filter.
        /// </summary>
        public void ApplyEventMonitoredItemFilter()
        {
            if (m_eventMonitoredItem == null)
            {
                Console.WriteLine("EventMonitoredItem is not created.");
                return;
            }

            ExtendedEventFilter filter = (ExtendedEventFilter)m_eventMonitoredItem.Filter;
            if (filter != null)
            {
                foreach (var selectOperand in filter.SelectOperandList)
                {
                    if (selectOperand.EventTypeId == m_selectEventTypeId &&
                        selectOperand.PropertyName.Equals(m_eventPropertyName))
                    {
                        Console.WriteLine("Filter is already applied.");
                        return;
                    }
                }

                filter.AddSelectClause(m_selectEventTypeId, m_eventPropertyName);
                filter.EventTypeIdFilter = m_eventTypeId;
                try
                {
                    m_eventMonitoredItem.ApplyFilter();
                    Console.WriteLine("Filter is applied on Event Monitored Item.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Filter could not be applied.Error: " + ex.Message);
                }
            }
            
        }

        /// <summary>
        /// Deletes the event monitored item.
        /// </summary>
        public void DeleteEventMonitoredItem()
        {
            if (m_eventMonitoredItem == null)
            {
                Console.WriteLine("EventMonitoredItem is not created.");
                return;
            }
            try
            {
                m_eventMonitoredItem.Delete();
                m_eventMonitoredItem.EventsReceived -= m_eventMonitoredItem_EventsReceived;
                m_eventMonitoredItem = null;
                Console.WriteLine("Event Monitored Item was disconnected and deleted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Handles the Notification event of the eventMonitoredItem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Softing.Opc.Ua.Toolkit.Client.MonitoredItemNotificationEventArgs"/> instance containing the event data.</param>
        private void m_eventMonitoredItem_EventsReceived(object sender, EventsNotificationEventArgs e)
        {
            foreach (var eventNotification in e.EventNotifications)
            {
                Console.WriteLine("Event notification received for {0}.\n", eventNotification.MonitoredItem.DisplayName);
                string displayNotification = string.Empty;
                IList<SelectOperand> listOfOperands = ((ExtendedEventFilter)m_eventMonitoredItem.Filter).SelectOperandList;
                for (int i = 0; i < listOfOperands.Count; i++)
                {
                    displayNotification += listOfOperands[i].PropertyName.NamespaceIndex + ":" + listOfOperands[i].PropertyName.Name 
                        + " : " + eventNotification.EventFields[i].ToString() + "\n";
                }

                Console.WriteLine(displayNotification);
            }
        }
        #endregion

        #region Method Call

        /// <summary>
        /// Call the method.
        /// </summary>
        /// <returns>The list of output arguments returned by the method call.</returns>
        internal void CallMethod()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            /*Select the method from the address space*/
            //Browse Path: Root\Objects\Server\Data\Static\MethodTest
            NodeId parentObjectId = new NodeId(10755, 3);
            //Browse Path: Root\Objects\Server\Data\Static\MethodTest\ScalarMethod1
            NodeId methodId = new NodeId(10756, 3);

            /*initialize input arguments*/
            bool Arg1 = true;
            sbyte Arg2 = -100;
            byte Arg3 = 200;
            Int16 Arg4 = -1200;
            UInt16 Arg5 = 1200;
            Int32 Arg6 = -56000;
            UInt32 Arg7 = 125066;
            Int64 Arg8 = -25000666;
            UInt64 Arg9 = 40444000;
            float Arg10 = 3455.67f;
            Double Arg11 = -1.7976;

            List<object> InputArguments = new List<object> { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11 };
            Console.WriteLine("\nMethod is called with the following arguments:\n");
            for (int i = 0; i < InputArguments.Count; i++)
            {
                Console.WriteLine("InArg[" + i + "]= " + InputArguments[i]);
            }

            IList<object> output = new List<object>();
            StatusCode statusCode = new StatusCode();
            try
            {
                statusCode = m_session.Call(parentObjectId, methodId, InputArguments, out output);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Method call exception: " + ex.Message);
            }
            Console.WriteLine("\nOutput arguments are:\n");
            for (int i = 0; i < output.Count; i++)
            {
                Console.WriteLine("OutArg[" + i + "]= " + output[i]);
            }
            Console.WriteLine(string.Format("\nStatus Code is: {0}\n", statusCode));
        }

        internal void AsyncCallMethod()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            /*Select the method from the address space*/
            //Browse Path: Root\Objects\Server\Data\Static\MethodTest
            NodeId parentObjectId = new NodeId(10755, 3);
            //Browse Path: Root\Objects\Server\Data\Static\MethodTest\ScalarMethod1
            NodeId methodId = new NodeId(10756, 3);

            /*initialize input arguments*/
            bool Arg1 = true;
            sbyte Arg2 = -100;
            byte Arg3 = 200;
            Int16 Arg4 = -1200;
            UInt16 Arg5 = 1200;
            Int32 Arg6 = -56000;
            UInt32 Arg7 = 125066;
            Int64 Arg8 = -25000666;
            UInt64 Arg9 = 40444000;
            float Arg10 = 3455.67f;
            Double Arg11 = -1.7976;

            List<object> InputArguments = new List<object> { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11 };
            Console.WriteLine("\nMethod is called with the following arguments:\n");
            for (int i = 0; i < InputArguments.Count; i++)
            {
                Console.WriteLine("InArg[" + i + "]= " + InputArguments[i]);
            }

            try
            {
                m_callIdentifier++;
                m_session.CallAsync(parentObjectId, methodId, InputArguments, m_callIdentifier);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Asynchronous method call exception: " + ex.Message);
            }
        }       

        void Session_CallCompleted(object sender, MethodExecutionArgs e)
        {
            Console.WriteLine(string.Format("\nCall returned for: {0}", e.Cookie));
            Console.WriteLine("Output arguments are:");
            for (int i = 0; i < e.OutputParameters.Count; i++)
            {
                Console.WriteLine("OutArg[" + i + "]= " + e.OutputParameters[i]);
            }
            Console.WriteLine(string.Format("\nStatus Code is: {0}\n", e.Result));
        }
        #endregion

        #region History Read
        public void HistoryReadRaw()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created.");
                return;
            }
            
            ReadRawModifiedDetails argument = new ReadRawModifiedDetails()
            {
                IsReadModified = false,
                StartTime = new DateTime(2011, 1, 1, 12, 0, 0),
                EndTime = new DateTime(2011, 1, 1, 12, 1, 40),               
                NumValuesPerNode = 3,
                ReturnBounds = false
            };

            TimestampsToReturn timestampsToReturn = TimestampsToReturn.Both;           

            List<DataValue> results = null;
            try
            {
                results = m_session.HistoryReadRaw(m_historianNodeId, argument, timestampsToReturn, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No results");
                return;
            }

            string value = null;
            for (int i = 0; i < results.Count; i++)
            {
                value = results[i].Value == null ? "NULL" : results[i].Value.ToString();
                Console.WriteLine("[" + i + "]"
                    + " " + "Value: " + value
                    + " " + "ServerTimestamp: " + results[i].ServerTimestamp
                    + " " + "SourceTimestamp: " + results[i].SourceTimestamp
                    + " " + "StatusCode: " + results[i].StatusCode
                    + " " + "HistoryInfo:" + results[i].StatusCode.AggregateBits);
            }
        }

        public void HistoryReadAtTime()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created.");
                return;
            }

            DateTimeCollection requiredTimes = new DateTimeCollection();
            requiredTimes.Add(new DateTime(2011, 1, 1, 12, 0, 0));
            requiredTimes.Add(new DateTime(2011, 7, 1, 12, 1, 0));   
            ReadAtTimeDetails argument = new ReadAtTimeDetails()
            {
                ReqTimes = requiredTimes,
                UseSimpleBounds = true
            };

            TimestampsToReturn timestampsToReturn = TimestampsToReturn.Both;

            List<DataValue> results = null;
            try
            {
                results = m_session.HistoryReadAtTime(m_historianNodeId, argument, timestampsToReturn, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No results");
                return;
            }

            string value = null;
            for (int i = 0; i < results.Count; i++)
            {
                value = results[i].Value == null ? "NULL" : results[i].Value.ToString();
                Console.WriteLine("[" + i + "]"
                    + " " + "Value: " + value
                    + " " + "ServerTimestamp: " + results[i].ServerTimestamp
                    + " " + "SourceTimestamp: " + results[i].SourceTimestamp
                    + " " + "StatusCode: " + results[i].StatusCode
                    + " " + "HistoryInfo:" + results[i].StatusCode.AggregateBits);
            }
        }

        public void HistoryReadProcessed()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created.");
                return;
            }
            
            NodeIdCollection aggregateTypes = new NodeIdCollection();
            aggregateTypes.Add(new NodeId(2342)); //aggregate function average           

            ReadProcessedDetails argument = new ReadProcessedDetails()
            {
                StartTime = new DateTime(2011, 1, 1, 12, 0, 0),
                EndTime = new DateTime(2011, 1, 1, 12, 1, 40),
                ProcessingInterval = 10000,
                AggregateType = aggregateTypes
            };
            TimestampsToReturn timestampsToReturn = TimestampsToReturn.Both;

            List<DataValue> results = null;
            try
            {
                results = m_session.HistoryReadProcessed(m_historianNodeId, argument, timestampsToReturn, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No results");
                return;
            }

            string value = null;
            for (int i = 0; i < results.Count; i++)
            {
                value = results[i].Value == null ? "NULL" : results[i].Value.ToString();
                Console.WriteLine("[" + i + "]"
                    + " " + "Value: " + value
                    + " " + "ServerTimestamp: " + results[i].ServerTimestamp
                    + " " + "SourceTimestamp: " + results[i].SourceTimestamp
                    + " " + "StatusCode: " + results[i].StatusCode
                    + " " + "HistoryInfo:" + results[i].StatusCode.AggregateBits);
            }
        }

        void HistoryContinuationPointReached(object sender, HistoryReadContinuationEventArgs e)
        {
            Console.WriteLine("Continuation point reached.");

            //one can cancel here the history read for new continuation points
            //e.Cancel = true;

            //one can identify the history read call that raised the continuation point by checking the cookie passed to the method
            //e.Cookie
        }
        #endregion

        #region Browse
        /// <summary>
        /// The BrowseTheServer method uses the Browse method with two parameters, in this case the browse options will be taken from the Session object.
        /// If there are no browse options on the Session object the browse will be done with the default options.
        /// </summary>
        internal void BrowseTheServer()
        {
            try
            {                
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescription> rootReferenceDescriptions = Browse(null, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (ReferenceDescription rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            IList<ReferenceDescription> objectReferenceDescriptions = new List<ReferenceDescription>();
                            objectReferenceDescriptions = Browse(ExpandedNodeId.ToNodeId(rootReferenceDescription.NodeId, m_namespaceUris), null);
                            foreach (ReferenceDescription objectReferenceDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("     -" + objectReferenceDescription.DisplayName);
                                if (objectReferenceDescription.BrowseName.Name == "Server")
                                {
                                    IList<ReferenceDescription> serverReferenceDescriptions = new List<ReferenceDescription>();
                                    serverReferenceDescriptions = Browse(ExpandedNodeId.ToNodeId(objectReferenceDescription.NodeId, m_namespaceUris), null);
                                    foreach (ReferenceDescription serverReferenceDescription in serverReferenceDescriptions)
                                    {
                                        Console.WriteLine("        -" + serverReferenceDescription.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse Error: " + ex.Message);
            }
        }

        /// <summary>
        /// The BrowseWithOptions method uses the Browse method with three parameters, in this case the browse options will be given as a parameer.
        /// A BrowseOptions object is created first, on which browse options can be set, and given as parameter to the Browse method.
        /// In this case any browse options on the Session object will be ignored.
        /// </summary>
        internal void BrowseWithOptions()
        {
            BrowseOptions options = new BrowseOptions();
            options.MaxReferencesReturned = 3;
            try
            {
                //Using the Browse method with null parameters will return the browse result for the root node.
                IList<ReferenceDescription> rootReferenceDescriptions = BrowseOptions(null, null, null);
                if (rootReferenceDescriptions != null)
                {
                    foreach (ReferenceDescription rootReferenceDescription in rootReferenceDescriptions)
                    {
                        Console.WriteLine("  -" + rootReferenceDescription.DisplayName);
                        if (rootReferenceDescription.BrowseName.Name == "Objects")
                        {
                            IList<ReferenceDescription> objectReferenceDescriptions = new List<ReferenceDescription>();
                            objectReferenceDescriptions = BrowseOptions(ExpandedNodeId.ToNodeId(rootReferenceDescription.NodeId, m_namespaceUris), options, rootReferenceDescription);
                            foreach (ReferenceDescription objectReferenceDescription in objectReferenceDescriptions)
                            {
                                Console.WriteLine("     -" + objectReferenceDescription.DisplayName);
                                if (objectReferenceDescription.BrowseName.Name == "Server")
                                {
                                    IList<ReferenceDescription> serverReferenceDescriptions = new List<ReferenceDescription>();
                                    serverReferenceDescriptions = BrowseOptions(ExpandedNodeId.ToNodeId(objectReferenceDescription.NodeId, m_namespaceUris), options, objectReferenceDescription);
                                    foreach (ReferenceDescription serverReferenceDescription in serverReferenceDescriptions)
                                    {
                                        Console.WriteLine("        -" + serverReferenceDescription.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse Error: " + ex.Message);
            }
        }
        /// <summary>
        /// Browses the specified node id and returns its list of references.
        /// This method uses browse options set on the Session object
        /// </summary>
        internal IList<ReferenceDescription> Browse(NodeId nodeId, object sender)
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return new List<ReferenceDescription>();
            }
            IList<ReferenceDescription> results = null;

            try
            {
                results = m_session.Browse(nodeId, sender);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse error: " + ex.Message);
            }
            return results;
        }

        /// <summary>
        /// Browses the specified node id and returns its list of references.
        /// This method uses browse options as an input parameter.
        /// </summary>
        internal IList<ReferenceDescription> BrowseOptions(NodeId nodeId, BrowseOptions browseOptions, object sender)
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return new List<ReferenceDescription>();
            }
            IList<ReferenceDescription> results = null;

            try
            {
                results = m_session.Browse(nodeId, browseOptions, sender);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse error: " + ex.Message);
            }
            return results;
        }

        /// <summary>
        /// Handle the ContinuationPointReached event.
        /// This event is raised when a continuation point is reached.
        /// For example if from Browse Options the MaxReferencesReturned is set to x, then when browsing every x references returned this event will be thrown.
        /// </summary>
        private void Session_ContinuationPointReached(object sender, BrowseEventArgs e)
        {
            e.Cancel = true;
        }
        #endregion


        #region TranslateBrowsePathToNodeIds

        /// <summary>
        /// Translates the specified browse path to its corresponding NodeId.
        /// </summary>
        internal void TranslateBrowsePathToNodeIds()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                // define the starting node as the "Objects\Data" node.
                NodeId startingNode = new NodeId("ns=3;i=10157");

                // define the BrowsePath to the "Static\Scalar\Int32Value" node.
                List<QualifiedName> browsePath = new List<QualifiedName>();
                browsePath.Add(new QualifiedName("Static", 3));
                browsePath.Add(new QualifiedName("Scalar", 3));
                browsePath.Add(new QualifiedName("Int32Value", 3));

                // invoke the TranslateBrowsePath service.
                IList<NodeId> translateResults = m_session.TranslateBrowsePathToNodeIds(startingNode, browsePath);

                if (translateResults != null)
                {
                    Console.WriteLine("TranslateBrowsePath returned {0} result(s):", translateResults.Count);

                    foreach (NodeId result in translateResults)
                    {
                        Console.WriteLine("    {0}", result);
                    }
                }
                else
                {
                    Console.WriteLine("TranslateBrowsePath returned null value");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TranslateBrowsePath error: " + ex.Message);
            }
        }

        /// <summary>
        /// Translates the specified list of browse paths to corresponding NodeIds.
        /// </summary>
        internal void TranslateBrowsePathsToNodeIds()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"c\" command");
                return;
            }

            try
            {
                // define the list of requests.
                List<SimpleBrowsePath> browsePaths = new List<SimpleBrowsePath>();

                // define the starting node as the "Objects" node.
                SimpleBrowsePath browsePath = new SimpleBrowsePath();
                browsePath.StartingNode = new NodeId("ns=0;i=85");

                // define the relative browse path to the "Data\Static\Scalar\Int32Value" node.
                browsePath.RelativePath.Add(new QualifiedName("Data", 3));
                browsePath.RelativePath.Add(new QualifiedName("Static", 3));
                browsePath.RelativePath.Add(new QualifiedName("Scalar", 3));
                browsePath.RelativePath.Add(new QualifiedName("Int32Value", 3));
                browsePaths.Add(browsePath);

                // define the starting node as the "Objects" node.
                browsePath = new SimpleBrowsePath();
                browsePath.StartingNode = new NodeId("ns=0;i=85");

                // define the relative browse path to the "Data\Static\Array\UInt32Value" node.
                browsePath.RelativePath.Add(new QualifiedName("Data", 3));
                browsePath.RelativePath.Add(new QualifiedName("Static", 3));
                browsePath.RelativePath.Add(new QualifiedName("Array", 3));
                browsePath.RelativePath.Add(new QualifiedName("UInt32Value", 3));
                browsePaths.Add(browsePath);

                // invoke the TranslateBrowsePathsToNodeIds service.
                IList<SimpoleBrowsePathResult> translateResults = m_session.TranslateBrowsePathsToNodeIds(browsePaths);

                // display the results.
                Console.WriteLine("TranslateBrowsePaths returned {0} result(s):", translateResults.Count);

                foreach (SimpoleBrowsePathResult browsePathResult in translateResults)
                {
                    Console.Write("    StatusCode = {0} ; Target Nodes = ", browsePathResult.StatusCode);

                    foreach (NodeId targetNode in browsePathResult.TargetIds)
                    {
                        Console.Write("{0} ;", targetNode);
                    }

                    Console.WriteLine("\b \b");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TranslateBrowsePaths error: " + ex.Message);
            }
        }
        #endregion
    }
}
