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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Helpers
{    
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
        public FileStateHelper(ClientSession session, string filename, NodeId nodeId, UInt32 fileHandle = 0)
        {
            m_session = session;
            Filename = filename;
            NodeID = nodeId;
            m_fileHandle = fileHandle;

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
        /// <param name="mode">The file open mode. It is bitmask of the following possible values
        /// Read - 1
        /// Write - 2
        /// ReadWrite - 3
        /// EraseExisting - 4
        /// Append - 8
        /// </param>
        public StatusCode Open(byte mode)
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] { mode };

                IList<object> outArgs = null;
                statusCode = m_session.Call(NodeID, OpenNodeID, args, out outArgs);
                m_fileHandle = (uint)outArgs[0];

                return statusCode;
            }
            catch(Exception ex)
            {
                string errorText =  StatusCode.IsGood(statusCode) ? string.Format("\nStatus Code is: {0}", statusCode) : string.Format("File cannot be opend: [0}", ex.Message);
                throw new Exception(errorText);
            }
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
                object[] args = new object[] { m_fileHandle };

                IList<object> outArgs = null;
                statusCode = m_session.Call(NodeID, CloseNodeID, args, out outArgs);
                m_fileHandle = 0;

                return statusCode;
            }
            catch
            {
                throw new Exception(string.Format("\nStatus Code is: {0}", statusCode));
            }
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
                statusCode = m_session.Call(NodeID, ReadNodeID, args, out outArgs);
                data = outArgs[0] as byte[];

                return statusCode;
            }
            catch
            {
                throw new Exception(string.Format("\nStatus Code is: {0}", statusCode));
            }
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
                statusCode = m_session.Call(NodeID, WriteNodeID, args, out outArgs);

                return statusCode;
            }
            catch
            {
                throw new Exception(string.Format("\nStatus Code is: {0}", statusCode));
            }
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

                statusCode = m_session.Call(NodeID, GetPositionNodeID, args, out outArgs);

                return (UInt64)outArgs[0];
            }
            catch
            {
                throw new Exception(string.Format("\nStatus Code is: {0}", statusCode));
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
                object[] args = new object[] { m_fileHandle, position };
                IList<object> outArgs = null;

                statusCode = m_session.Call(NodeID, SetPositionNodeID, args, out outArgs);
            }
            catch
            {
                throw new Exception(string.Format("\nStatus Code is: {0}", statusCode));
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
            catch (Exception ex)
            {
                throw new Exception("TranslateBrowsePathToNodeIds error: " + ex.Message);
            }
        }

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
