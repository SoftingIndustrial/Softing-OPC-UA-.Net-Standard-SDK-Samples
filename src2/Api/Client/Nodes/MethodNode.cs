/* ========================================================================
 * Copyright © 2011-2017 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Opc.Ua.Toolkit.Client.Nodes
{
    /// <summary>
    /// Specifies the attributes which belong to method nodes.
    /// </summary>
    /// <remarks>
    /// Methods are functions, similar to the methods of a class in object-oriented programming. 
    /// Methods are invoked by a client, proceed to completion on the Server and return the result to the client
    /// </remarks>
    public class MethodNode : BaseNode
    {
        #region Private
        private List<Argument> m_inputArguments, m_outputArguments;
        private ReadOnlyCollection<Argument> m_readonlyInputArguments, m_readonlyOutputArguments;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodNode"/> class.
        /// </summary>
        internal MethodNode()
        {
            AttributeStatusCodes.Add(AttributeId.Executable, new StatusCode(StatusCodes.GoodNoData));
            AttributeStatusCodes.Add(AttributeId.UserExecutable, new StatusCode(StatusCodes.GoodNoData));
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether this <see cref="MethodNode"/> is currently executable.
        /// </summary>
        /// <value>
        /// <c>False</c> means not executable, <c>True</c> means executable 
        /// </value>
        /// <remarks>
        /// The executable attribute does not take any user access rights into account,
        /// i.e. although the Method is executable this may be restricted to a certain user / user group
        /// </remarks>
        public bool Executable
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="MethodNode"/> is executable taking user access rights into account.
        /// </summary>
        /// <value>
        /// <c>False</c> means not executable, <c>True</c> means executable 
        /// </value>
        public bool UserExecutable
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the arguments that shall be used by a client when calling the Method.
        /// </summary>
        /// <remarks>
        /// A method can have a varying number of input arguments.
        /// </remarks>
        public IList<Argument> InputArguments
        {
            get
            {
                return m_readonlyInputArguments;
            }
        }

        /// <summary>
        /// Gets the result returned from the Method call.
        /// </summary>
        /// <remarks>
        /// A method can have a varying number of output arguments.
        /// </remarks>
        public IList<Argument> OutputArguments
        {
            get
            {
                return m_readonlyOutputArguments;
            }
        }

        /// <summary>
        /// Gets or sets the node identifier of the parent node that contains this method.
        /// </summary>
        /// <remarks>
        /// The NodeId shall be that of the Object or ObjectType that is the source of a HasComponent Reference 
        /// (or subtype of HasComponent Reference) to the Method specified in BaseNode.<see cref="BaseNode.NodeId"/>.
        /// </remarks>
        public NodeId ObjectId
        {
            get;
            set;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Reads the input and output arguments.
        /// </summary>
        /// <param name="session">A <see cref="Session"/></param>
        internal void ReadArguments(Session session)
        {
            session.GetMethodArguments(NodeId, out m_inputArguments, out m_outputArguments);
            m_readonlyInputArguments = m_inputArguments != null ? new ReadOnlyCollection<Argument>(m_inputArguments) : null;
            m_readonlyOutputArguments = m_outputArguments != null ? new ReadOnlyCollection<Argument>(m_outputArguments) : null;
        }
        #endregion
    }
}
