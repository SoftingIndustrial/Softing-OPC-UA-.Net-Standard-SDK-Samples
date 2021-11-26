/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Helpers
{
    /// <summary>
    /// File state mode
    /// </summary>
    public enum FileStateMode
    {
        Read = 1,           
        Write = 2,          
        EraseExisting = 4,  
        Append = 8
        /* options for bits position 4-7 are reserved for future version */
    }

    /// <summary>
    /// FileState helper class
    /// </summary>
    public class FileStateHelper
    {
        #region Private Members

        private UInt32 m_fileHandle;
        private ClientSession m_session;

        #endregion

        #region Constructor
        public FileStateHelper(ClientSession session, string filename, NodeId nodeId)
        {
            m_session = session;
            Filename = filename;
            NodeID = nodeId;
            
            TranslateBrowsePathToNodeIds();
        }
        #endregion

        #region Properties

        #region FileState node attributes
        public string Filename { get; private set; }

        public NodeId NodeID { get; private set; }

        public NodeId OpenNodeID { get; private set; }

        public NodeId ReadNodeID { get; private set; }

        public NodeId WriteNodeID { get; private set; }

        public NodeId CloseNodeID { get; private set; }

        public NodeId SizeNodeID { get; private set; }

        public NodeId WritableNodeID { get; private set; }

        public NodeId GetPositionNodeID { get; private set; }

        public NodeId SetPositionNodeID { get; private set; }

        public UInt64 Size
        {
            get
            {
                if (SizeNodeID==null)
                {
                    throw new Exception("Size nodeId is null.");
                }

                ReadValueId valueToRead = new ReadValueId();
                valueToRead.NodeId = SizeNodeID;
                valueToRead.AttributeId = Attributes.Value;
                DataValueEx value = m_session.Read(valueToRead);

                if (value != null)
                    return (UInt64)value.Value;

                return 0;
            }
        }

        public bool Writable
        {
            get
            {
                if (WritableNodeID == null)
                {
                    throw new Exception("Writable nodeId is null.");
                }

                ReadValueId valueToRead = new ReadValueId();
                valueToRead.NodeId = WritableNodeID;
                valueToRead.AttributeId = Attributes.Value;
                DataValueEx value = m_session.Read(valueToRead);

                if (value != null)
                    return (bool)value.Value;

                return false;
            }
        }
        #endregion FileState node attributes

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Opens the file into the specified mode.
        /// </summary>
        /// <param name="mode">The file open mode. It is bit-mask of the following possible values
        /// Read - 0
        /// Write - 1
        /// EraseExisting - 2
        /// Append - 3
        /// </param>
        public StatusCode Open(FileStateMode mode)
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] {(byte) mode};

                IList<object> outArgs = null;
                if (m_session.CurrentState == State.Active)
                {
                    statusCode = m_session.Call(NodeID, OpenNodeID, args, out outArgs);
                    m_fileHandle = (uint) outArgs[0];
                }
                else
                {
                    statusCode = StatusCodes.BadSessionClosed;
                    throw new Exception();
                }
            }
            catch
            {
                throw new Exception(string.Format("'Open' file state error: {0}", statusCode));
            }

            return statusCode;
        }

        /// <summary>
        /// Close the related file
        /// </summary>
        /// <returns></returns>
        public StatusCode Close()
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] {m_fileHandle};

                IList<object> outArgs = null;
                if (m_session.CurrentState == State.Active)
                {
                    statusCode = m_session.Call(NodeID, CloseNodeID, args, out outArgs);
                    m_fileHandle = 0;
                }
                else
                {
                    m_fileHandle = 0;
                    statusCode = StatusCodes.BadSessionClosed;
                    throw new Exception();
                }
            }
            catch
            {
                throw new Exception(string.Format("'Close' file state error: {0}", statusCode));
            }

            return statusCode;
        }

        /// <summary>
        /// Read the file content
        /// </summary>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public StatusCode Read(int length, out byte[] data)
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] { m_fileHandle, length };
                IList<object> outArgs = null;

                if (m_session.CurrentState == State.Active)
                {
                    statusCode = m_session.Call(NodeID, ReadNodeID, args, out outArgs);
                    data = outArgs[0] as byte[];
                }
                else
                {
                    data = new byte[0];
                    statusCode = StatusCodes.BadSessionClosed;
                    throw new Exception();
                }
            }
            catch
            {
                throw new Exception(string.Format("'Read' file state error: {0}", statusCode));
            }

            return statusCode;
        }

        /// <summary>
        /// Write file data content
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public StatusCode Write(byte[] data)
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] { m_fileHandle, data };

                IList<object> outArgs = null;
                if (m_session.CurrentState == State.Active)
                {
                    statusCode = m_session.Call(NodeID, WriteNodeID, args, out outArgs);
                }
                else
                {
                    statusCode = StatusCodes.BadSessionClosed;
                    throw new Exception();
                }
            }
            catch
            {
                throw new Exception(string.Format("'Write' file state error: {0}", statusCode));
            }
            return statusCode;
        }

        /// <summary>
        /// Get related file position
        /// </summary>
        /// <returns></returns>
        public UInt64 GetPosition()
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] { m_fileHandle };
                IList<object> outArgs = null;

                if (m_session.CurrentState == State.Active)
                {
                    statusCode = m_session.Call(NodeID, GetPositionNodeID, args, out outArgs);
                }
                else
                {
                    statusCode = StatusCodes.BadSessionClosed;
                    throw new Exception();
                }

                return (UInt64)outArgs[0];
            }
            catch
            {
                throw new Exception(string.Format("'GetPosition' file state error: {0}", statusCode));
            }
        }

        /// <summary>
        /// Set related file position
        /// </summary>
        /// <param name="position"></param>
        public void SetPosition(UInt64 position)
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] {m_fileHandle, position};
                IList<object> outArgs = null;

                if (m_session.CurrentState == State.Active)
                {
                    statusCode = m_session.Call(NodeID, SetPositionNodeID, args, out outArgs);
                }
                else
                {
                    statusCode = StatusCodes.BadSessionClosed;
                    throw new Exception();
                }
            }
            catch
            {
                throw new Exception(string.Format("'SetPosition' file state error: {0}", statusCode));
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Translates the specified browse path to its corresponding NodeId.
        /// </summary>
        private void TranslateBrowsePathToNodeIds()
        {
            try
            {
                // define the list of requests.
                List<BrowsePathEx> browsePaths = new List<BrowsePathEx>();

                AddBrowsePath(browsePaths, "Size");
                AddBrowsePath(browsePaths, "Writable");
                AddBrowsePath(browsePaths, "Open");
                AddBrowsePath(browsePaths, "Close");
                AddBrowsePath(browsePaths, "Read");
                AddBrowsePath(browsePaths, "Write");
                AddBrowsePath(browsePaths, "GetPosition");
                AddBrowsePath(browsePaths, "SetPosition");

                if (m_session.CurrentState == State.Active)
                {
                    // invoke the TranslateBrowsePath service.
                    IList<BrowsePathResultEx> translateResults = m_session.TranslateBrowsePathsToNodeIds(browsePaths);

                    SizeNodeID = GetTargetId(translateResults[0]);
                    WritableNodeID = GetTargetId(translateResults[1]);
                    OpenNodeID = GetTargetId(translateResults[2]);
                    CloseNodeID = GetTargetId(translateResults[3]);
                    ReadNodeID = GetTargetId(translateResults[4]);
                    WriteNodeID = GetTargetId(translateResults[5]);
                    GetPositionNodeID = GetTargetId(translateResults[6]);
                    SetPositionNodeID = GetTargetId(translateResults[7]);
                }
                else
                {
                    throw new ServiceResultException(StatusCodes.BadSessionClosed);
                }
            }
            catch(Exception e)
            {
                throw new Exception(string.Format("TranslateBrowsePathToNodeIds error: {0}", e.Message));
            }
        }

        /// <summary>
        /// Add browse path
        /// </summary>
        /// <param name="browsePaths"></param>
        /// <param name="name"></param>
        private void AddBrowsePath(List<BrowsePathEx> browsePaths, string name)
        {
            // define the starting node as the "Objects" node.
            BrowsePathEx browsePath = new BrowsePathEx();
            browsePath.StartingNode = NodeID;
            browsePath.RelativePath = new List<QualifiedName>() { new QualifiedName(name) };//new RelativePath(new QualifiedName(name));
            browsePaths.Add(browsePath);
        }

        /// <summary>
        /// Get target id
        /// </summary>
        /// <param name="browseResult"></param>
        /// <returns></returns>
        private NodeId GetTargetId(BrowsePathResultEx browseResult)
        {
            NodeId nodeId = null;

            if (StatusCode.IsGood(browseResult.StatusCode))
            {
                nodeId = browseResult.TargetIds[0]; 
            }

            return nodeId;
        }
        #endregion
    }
}
