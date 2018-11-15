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

        #region Properties

        #region TemporaryFileTransferState node attributes

        public string Filename { get; private set; }

        public NodeId NodeID { get; private set; }

        public NodeId FileNodeID
        {
            get
            {
                return m_fileNodeId;
            }
        }

        public UInt32 FileHandle
        {
            get
            {
                return m_fileHandle;
            }
        }
        public NodeId ClientProcessingTimeoutNodeID { get; private set; }

        public NodeId GenerateFileForReadNodeID { get; private set; }

        public NodeId GenerateFileForWriteNodeID { get; private set; }

        public NodeId CloseAndCommitNodeID { get; private set; }

        #endregion Properties

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

        public StatusCode CloseAndCommit()
        {
            StatusCode statusCode = new StatusCode();

            try
            {
                object[] args = new object[] { m_fileHandle };

                IList<object> outArgs = null;
                statusCode = m_session.Call(NodeID, CloseAndCommitNodeID, args, out outArgs);
                m_fileHandle = 0;

                return statusCode;
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
