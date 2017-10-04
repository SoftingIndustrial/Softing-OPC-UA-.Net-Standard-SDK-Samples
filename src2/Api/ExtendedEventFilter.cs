using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// The event filter class is used for the filtering and content selection of the event notifications.
    /// </summary>
    /// <remarks>
    /// If an Event Notification conforms to the where clause property of the event filter, then
    /// the notification is sent to the Client.
    /// Each Event Notification shall include the fields defined by the select clause property of the EventFilter. 
    /// The selectClause property is specified using the <see cref="SelectOperand"/>. The select operand uses the NodeId of an EventType and a path to an InstanceDeclaration. 
    /// An InstanceDeclaration is a Node which can be found by following forward hierarchical references from the fully inherited EventType.
    /// The where clause property is specified through the EventTypeIdFilter property. This represents the NodeId of an EventType supported by the Server.
    /// </remarks>
    public class ExtendedEventFilter : EventFilter
    {
        #region Fields       
        private List<SelectOperand> m_selectOperandList;
        private ReadOnlyCollection<SelectOperand> m_readOnlyOperandCollection;
        private NodeId m_eventTypeId;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="EventFilter"/> class.
        /// </summary>
        public ExtendedEventFilter(): this ( new EventFilter())
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="EventFilter"/> class.
        /// </summary>
        public ExtendedEventFilter(EventFilter eventFilter)
        {
            SelectClauses = eventFilter.SelectClauses;
            WhereClause = eventFilter.WhereClause;

            m_selectOperandList = new List<SelectOperand>();
            m_readOnlyOperandCollection = new ReadOnlyCollection<SelectOperand>(m_selectOperandList);
            foreach (var operand in eventFilter.SelectClauses)
            {
                m_selectOperandList.Add(new SelectOperand(operand));
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the event type filter. Adds a where clause to the filter for this event type id.
        /// The changes remain on the client side, until the  <see cref="Client.MonitoredItem.ApplyFilter"/> method is called, or the monitored item is connected (again). 
        /// </summary>
        /// <remarks>
        /// If an Event Notification conforms to the filter defined by the where parameter of the EventFilter, then
        /// the Notification is sent to the Client.
        /// </remarks>
        public NodeId EventTypeIdFilter
        {
            get
            {
                return m_eventTypeId;
            }
            set
            {
                m_eventTypeId = value;
                if (m_eventTypeId != null)
                {
                    ContentFilter whereClause = new ContentFilter();
                    whereClause.Push(FilterOperator.OfType, m_eventTypeId);
                    WhereClause = whereClause;
                }
                else
                {
                    WhereClause = null;
                }
            }
        }

        /// <summary>
        /// Gets the select clause as a list of operands. This collection is readonly.
        /// </summary>
        /// <value>
        /// List of the values to return with each Event in a Notification. At least one valid
        /// SelectOperand shall be specified.
        /// </value>
        public ReadOnlyCollection<SelectOperand> SelectOperandList
        {
            get
            {
                return m_readOnlyOperandCollection;
            }
        }        
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new field (named also property) to the select clause.
        /// The changes remain on the client side, until the  <see cref="Client.MonitoredItem.ApplyFilter"/> method is called, or the monitored item is connected (again). 
        /// </summary>
        /// <param name="eventTypeId">The identifier for the event type that owns the property.</param>
        /// <param name="propertyName">Name of the property.</param>
        public new void AddSelectClause(NodeId eventTypeId, QualifiedName propertyName)
        {
            if (eventTypeId == null)
            {
                throw new System.ArgumentNullException("eventTypeId");
            }
            if (propertyName == null)
            {
                throw new System.ArgumentNullException("propertyName");
            }
            base.AddSelectClause(eventTypeId, propertyName);
            m_selectOperandList.Add(new SelectOperand(SelectClauses[SelectClauses.Count - 1]));
        }

        /// <summary>
        /// Adds the specified browse path to the event filter.
        /// </summary>
        /// <param name="eventTypeId">The identifier for the event type that owns the property</param>
        /// <param name="browsePath">The relative path to the node. </param>
        /// <param name="attributeId">Monitored attribute id</param>
        public new void AddSelectClause(NodeId eventTypeId, string browsePath, uint attributeId)
        {
            if (eventTypeId == null)
            {
                throw new System.ArgumentNullException("eventTypeId");
            }
            if (browsePath == null)
            {
                throw new System.ArgumentNullException("browsePath");
            }
            base.AddSelectClause(eventTypeId, browsePath, attributeId);
            m_selectOperandList.Add(new SelectOperand(SelectClauses[SelectClauses.Count - 1]));
        }
        /// <summary>
        /// Adds the specified browse path to the event filter.
        /// The changes remain on the client side, until the  <see cref="Client.MonitoredItem.ApplyFilter"/> method is called, or the monitored item is connected (again).
        /// </summary>
        /// <param name="eventTypeId">The identifier for the event type that owns the property.</param>
        /// <param name="browsePath">The relative path to the node.</param>
        public void AddSelectClause(NodeId eventTypeId, string browsePath)
        {
            if (eventTypeId == null)
            {
                throw new System.ArgumentNullException("eventTypeId");
            }

            if (browsePath == null)
            {
                throw new System.ArgumentNullException("browsePath");
            }

            base.AddSelectClause(eventTypeId, browsePath, Attributes.Value);
            m_selectOperandList.Add(new SelectOperand(SelectClauses[SelectClauses.Count - 1]));
        }

        /// <summary>
        /// Removes the specified field (named also property) from select clause.
        /// The changes remain on the client side, until the  <see cref="Client.MonitoredItem.ApplyFilter"/> method is called, or the monitored item is connected (again). 
        /// </summary>
        /// <param name="eventTypeId">The identifier for the event type that owns the property.</param>
        /// <param name="propertyName">Name of the property.</param>
        public void RemoveSelectClause(NodeId eventTypeId, QualifiedName propertyName)
        {
            if (eventTypeId == null)
            {
                throw new System.ArgumentNullException("eventTypeId");
            }
            if (propertyName == null)
            {
                throw new System.ArgumentNullException("propertyName");
            }
            SimpleAttributeOperand operandToRemove = null;
            foreach (var operand in SelectClauses)
            {
                if (operand.TypeDefinitionId == eventTypeId && operand.BrowsePath.Count > 0 && operand.BrowsePath[0] == propertyName)
                {
                    operandToRemove = operand;
                    break;
                }
            }

            if (operandToRemove != null)
            {
                SelectClauses.Remove(operandToRemove);
            }

            m_selectOperandList.Clear();
            foreach (var operand in SelectClauses)
            {
                m_selectOperandList.Add(new SelectOperand(operand));
            }
        }

        #endregion

    }
}
