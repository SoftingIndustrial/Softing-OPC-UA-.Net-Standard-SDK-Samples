using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Opc.Ua;
using Softing.Opc.Ua.Client;

namespace SampleClient.Helpers
{
    /// <summary>
    /// TemporaryFileTransferState helper class
    /// </summary>
    public class TemporaryFileTransferStateHelper
    {
        #region Private Members

        private NodeId m_fileNodeId;
        private UInt32 m_fileHandle;
        private ClientSession m_session;

        #endregion

        #region Constructor
        public TemporaryFileTransferStateHelper(ClientSession session, string filename, NodeId nodeId)
        {
            m_session = session;
            Filename = filename;
            NodeID = nodeId;

            TranslateBrowsePathToNodeIds();
        }
        #endregion

        #region Public Properties

        public string Filename { get; private set; }

        #region TemporaryFileTransferState node attributes

        public NodeId NodeID { get; private set; }

        public NodeId FileNodeID
        {
            get { return m_fileNodeId; }
        }

        public NodeId ClientProcessingTimeoutNodeID { get; private set; }

        public NodeId GenerateFileForReadNodeID { get; private set; }

        public NodeId GenerateFileForWriteNodeID { get; private set; }

        public NodeId CloseAndCommitNodeID { get; private set; }

        #endregion Public Properties

        public double ClientProcessingTimeout
        {
            get
            {
                if (ClientProcessingTimeoutNodeID == null)
                {
                    throw new Exception("ClientProcessingTimeout nodeId is null.");
                }

                ReadValueId valueToRead = new ReadValueId();
                valueToRead.NodeId = ClientProcessingTimeoutNodeID;
                valueToRead.AttributeId = Attributes.Value;
                DataValueEx value = m_session.Read(valueToRead);

                if (value != null)
                    return (double)value.Value;

                return 0;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Generate and open a file state for read
        /// </summary>
        /// <param name="generateOptions"></param>
        /// <returns></returns>
        public StatusCode GenerateFileForRead(object generateOptions)
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] { generateOptions };

                IList<object> outArgs = null;
                statusCode = m_session.Call(NodeID, GenerateFileForReadNodeID, args, out outArgs);
                if (outArgs != null && outArgs.Count == 3)
                {
                    m_fileNodeId = (NodeId)outArgs[0];
                    m_fileHandle = (uint)outArgs[1];
                    NodeId completionStateMachine = (NodeId)outArgs[2];
                }
                else
                {
                    throw new Exception("Invalid number of output arguments received.");
                }
            }
            catch (Exception ex)
            {
                string errorText = StatusCode.IsGood(statusCode) ? string.Format("\nStatus Code is: {0}", statusCode) : string.Format("File cannot be opend: [0}", ex.Message);
                throw new Exception(errorText);
            }

            return statusCode;
        }

        /// <summary>
        /// Generate and open a file state for write
        /// </summary>
        /// <param name="generateOptions"></param>
        /// <returns></returns>
        public StatusCode GenerateFileForWrite(object generateOptions)
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] { generateOptions };

                IList<object> outArgs = null;
                statusCode = m_session.Call(NodeID, GenerateFileForWriteNodeID, args, out outArgs);
                if (outArgs != null && outArgs.Count == 2)
                {
                    m_fileNodeId = (NodeId) outArgs[0];
                    m_fileHandle = (uint) outArgs[1];
                }
                else
                {
                    throw new Exception("Invalid number of output arguments received.");
                }
            }
            catch (Exception ex)
            {
                string errorText = StatusCode.IsGood(statusCode) ? string.Format("\nStatus Code is: {0}", statusCode) : string.Format("File cannot be opend: [0}", ex.Message);
                throw new Exception(errorText);
            }

            return statusCode;
        }

        /// <summary>
        /// Apply(commit) changes that close the file state 
        /// </summary>
        /// <returns></returns>
        public StatusCode CloseAndCommit()
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] { m_fileHandle };

                IList<object> outArgs = null;
                statusCode = m_session.Call(NodeID, CloseAndCommitNodeID, args, out outArgs);
                m_fileHandle = 0;
            }
            catch
            {
                throw new Exception(string.Format("\nStatus Code is: {0}", statusCode));
            }

            return statusCode;
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

                AddBrowsePath(browsePaths, "ClientProcessingTimeout");
                AddBrowsePath(browsePaths, "GenerateFileForRead");
                AddBrowsePath(browsePaths, "GenerateFileForWrite");
                AddBrowsePath(browsePaths, "CloseAndCommit");
                
                // invoke the TranslateBrowsePath service.
                IList<BrowsePathResultEx> translateResults = m_session.TranslateBrowsePathsToNodeIds(browsePaths);

                ClientProcessingTimeoutNodeID = GetTargetId(translateResults[0]);
                GenerateFileForReadNodeID = GetTargetId(translateResults[1]);
                GenerateFileForWriteNodeID = GetTargetId(translateResults[2]);
                CloseAndCommitNodeID = GetTargetId(translateResults[3]);
            }
            catch (Exception ex)
            {
                throw new Exception("TranslateBrowsePathToNodeIds error: " + ex.Message);
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
            browsePath.RelativePath = new List<QualifiedName>() { new QualifiedName(name) };
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
