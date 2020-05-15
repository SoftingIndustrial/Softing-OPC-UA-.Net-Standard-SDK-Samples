/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Samples
{
    /// <summary>
    /// Class providing support for history read
    /// </summary>
    class HistoryClient
    {
        #region Private Fields

        private const string SessionName = "HistoryClient Session";
        private readonly UaApplication m_application;
        private ClientSession m_session;
        private readonly NodeId m_historianNodeId = new NodeId("ns=4;s=StaticHistoricalDataItem_Historian2");

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of HistoryClient
        /// </summary>
        /// <param name="application"></param>
        public HistoryClient(UaApplication application)
        {
            m_application = application;
        }

        #endregion

        #region Read History

        /// <summary>
        /// Read history Raw
        /// </summary>
        public void HistoryReadRaw()
        {
            if (m_session == null)
            {
                Console.WriteLine("HistoryReadRaw: The session is not initialized!");
                return;
            }
            if (m_session.CurrentState != State.Active)
            {
                Console.WriteLine("HistoryReadRaw: The session is not connected!");
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

            List<DataValueEx> results = null;
            try
            {
                results = m_session.HistoryReadRaw(m_historianNodeId, argument, timestampsToReturn);
            }
            catch (Exception ex)
            {
                Program.PrintException("HistoryReadRaw", ex);
            }

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No results");
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                string value = results[i].Value == null ? "NULL" : results[i].Value.ToString();
                Console.WriteLine("[{0}] Value: {1} ServerTimestamp: {2} SourceTimestamp: {3} \n\r\t\tStatusCode: {4} HistoryInfo: {5}",
                    i, value, results[i].ServerTimestamp, results[i].SourceTimestamp, results[i].StatusCode,
                    results[i].StatusCode.AggregateBits);
            }
        }

        /// <summary>
        /// Read history in interval
        /// </summary>
        public void HistoryReadAtTime()
        {
            if (m_session == null)
            {
                Console.WriteLine("HistoryReadAtTime: The session is not initialized!");
                return;
            }
            if (m_session.CurrentState != State.Active)
            {
                Console.WriteLine("HistoryReadAtTime: The session is not connected!");
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

            List<DataValueEx> results = null;
            try
            {
                results = m_session.HistoryReadAtTime(m_historianNodeId, argument, timestampsToReturn);
            }
            catch (Exception ex)
            {
                Program.PrintException("HistoryReadAtTime", ex);
            }            

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No results");
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                string value = results[i].Value == null ? "NULL" : results[i].Value.ToString();
                Console.WriteLine("[{0}] Value: {1} ServerTimestamp: {2} SourceTimestamp: {3} \n\r\t\tStatusCode: {4} HistoryInfo: {5}",
                    i, value, results[i].ServerTimestamp, results[i].SourceTimestamp, results[i].StatusCode,
                    results[i].StatusCode.AggregateBits);
            }
        }

        /// <summary>
        /// Read history aggregates
        /// </summary>
        public void HistoryReadProcessed()
        {
            if (m_session == null)
            {
                Console.WriteLine("HistoryReadProcessed: The session is not initialized!");
                return;
            }
            if (m_session.CurrentState != State.Active)
            {
                Console.WriteLine("HistoryReadProcessed: The session is not connected!");
                return;
            }
            NodeIdCollection aggregateTypes = new NodeIdCollection();
            aggregateTypes.Add(ObjectIds.AggregateFunction_Average); //aggregate function average           

            ReadProcessedDetails argument = new ReadProcessedDetails()
            {
                StartTime = new DateTime(2011, 1, 1, 12, 0, 0),
                EndTime = new DateTime(2011, 1, 1, 12, 1, 40),
                ProcessingInterval = 10000,
                AggregateType = aggregateTypes
            };
            TimestampsToReturn timestampsToReturn = TimestampsToReturn.Both;

            List<DataValueEx> results = null;
            try
            {
                results = m_session.HistoryReadProcessed(m_historianNodeId, argument, timestampsToReturn);
            }
            catch (Exception ex)
            {
                Program.PrintException("HistoryReadProcessed", ex);
            }

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No results");
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                string value = results[i].Value == null ? "NULL" : results[i].Value.ToString();
                Console.WriteLine("[{0}] Value: {1} ServerTimestamp: {2} SourceTimestamp: {3} \n\r\t\tStatusCode: {4} HistoryInfo: {5}",
                    i, value, results[i].ServerTimestamp, results[i].SourceTimestamp, results[i].StatusCode,
                    results[i].StatusCode.AggregateBits);
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
    }
}
