using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// The operand used in the select clause of the event filter represents a property. The property can be accessed for any event notification to obtain information about it.
    /// </summary>
    public class SelectOperand
    {
        #region Fields
        private SimpleAttributeOperand m_wrapped;
        private NodeId m_eventTypeId;
        private QualifiedName m_propertyName;
        private List<QualifiedName> m_browsePath;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectOperand"/> class.
        /// </summary>
        /// <param name="attributeOperand"> The sdk operand.</param>
        internal SelectOperand(SimpleAttributeOperand attributeOperand)
        {
            m_wrapped = attributeOperand;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the value representing the <see cref="NodeId"/> of a TypeDefinitionNode. This parameter restricts the operand to instances of the TypeDefinitionNode or
        /// one of its subtypes.
        /// </summary>
        public NodeId EventTypeId
        {
            get
            {
                if (m_eventTypeId == null)
                {
                    m_eventTypeId = m_wrapped.TypeDefinitionId;
                }

                return m_eventTypeId;
            }
        }

        /// <summary>
        /// Gets the name of a property in the select clause.
        /// </summary>
        /// <value>
        /// Specifies a relative path using a list of BrowseNames.  
        /// The list of BrowseNames is equivalent to a RelativePath that specifies forward references
        /// which are subtypes of the HierarchicalReferences ReferenceType.
        /// All Nodes followed by the browsePath shall be of the NodeClass Object or Variable.
        /// </value>
        public QualifiedName PropertyName
        {
            get
            {
                if (m_propertyName == null)
                {
                    if (m_wrapped.BrowsePath.Count > 0)
                    {
                        m_propertyName = m_wrapped.BrowsePath[m_wrapped.BrowsePath.Count - 1];
                    }
                    else
                    {
                        m_propertyName = QualifiedName.ToQualifiedName(null);
                    }
                }

                return m_propertyName;
            }
        }

        /// <summary>
        /// Gets the relative browse path from the select clause of the filter.
        /// </summary>
        public List<QualifiedName> BrowsePath
        {
            get
            {
                return m_wrapped.BrowsePath;
            }
        }
        #endregion
    }
}
