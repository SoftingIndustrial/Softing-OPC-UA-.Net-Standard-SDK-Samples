using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// A result for a translate browse path request.
    /// </summary>
    public class SimpoleBrowsePathResult
    {
        #region Fields
        private StatusCode m_statusCode;
        private List<NodeId> m_targetIds;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BrowsePath"/> class with the default values.
        /// </summary>
        public SimpoleBrowsePathResult()
        {
            m_statusCode = new StatusCode();
            m_targetIds = new List<NodeId>();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the status code associated with the translate browse path request.
        /// </summary>
        public StatusCode StatusCode
        {
            get
            {
                return m_statusCode;
            }
            set
            {
                m_statusCode = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of target nodes of the browse path.
        /// </summary>
        public List<NodeId> TargetIds
        {
            get
            {
                return m_targetIds;
            }
            set
            {
                m_targetIds = value;

                if (value == null)
                {
                    m_targetIds = new List<NodeId>();
                }
            }
        }
        #endregion
    }
}
