using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// A request to translate a path into a node id.
    /// </summary>
    public class SimpleBrowsePath
    {
        #region Fields
        private NodeId m_startingNode;
        private List<QualifiedName> m_relativePath;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBrowsePath"/> class with the default values.
        /// </summary>
        public SimpleBrowsePath()
        {
            m_startingNode = null;
            m_relativePath = new List<QualifiedName>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBrowsePath"/> class.
        /// </summary>
        public SimpleBrowsePath(NodeId startingNode, List<QualifiedName> relativePath)
        {
            m_startingNode = startingNode;
            m_relativePath = relativePath;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the starting node for the browse path.
        /// </summary>
        public NodeId StartingNode
        {
            get
            {
                return m_startingNode;
            }
            set
            {
                m_startingNode = value;
            }
        }

        /// <summary>
        /// Gets or sets the path to follow from the starting node.
        /// </summary>
        public List<QualifiedName> RelativePath
        {
            get
            {
                return m_relativePath;
            }
            set
            {
                m_relativePath = value;

                if (value == null)
                {
                    m_relativePath = new List<QualifiedName>();
                }
            }
        }
        #endregion
    }
}
