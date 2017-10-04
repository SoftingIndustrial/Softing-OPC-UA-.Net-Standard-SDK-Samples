using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Browse handler helper class provides helper methods for browsing the server's address space.
    /// </summary>
    /// <remarks>
    /// The helper methods use the internal cache mechanism of the session to store the information already retrieved from the server.
    /// Therefore, multiple calls of these methods on the same session is optimized.
    /// The cache of a session cannot be cleared. Therefore, in order to retrieve again the information from the server a new session must be created.
    /// </remarks>
    public class BrowseHandler
    {
        #region Fields

        private Opc.Ua.Client.Session m_session;
        private readonly object m_lock = new object();

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowseHandler"></see> class.
        /// </summary>
        /// <param name="session">A <see cref="Opc.Ua.Client.Session"/> associated with this BrowseHandler.</param>
        internal BrowseHandler(Opc.Ua.Client.Session session)
        {
            m_session = session;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Gets the reference types starting from the specified reference type node id. The method browses one level following the HasSubtype reference type.<br/>
        /// Internal caching from Softing.Opc.Ua.Sdk Toolkit is used.
        /// </summary>
        /// <param name="parentReferenceTypeId">A <see cref="NodeId"/> type representing the start node id for browsing.</param>
        /// <returns>A a list of <see cref="ReferenceTypeNode"/> with all reference types found. An empty list is returned if no reference type was found.</returns>
        public IList<ReferenceTypeNode> GetSubTypes(NodeId parentReferenceTypeId)
        {
            if (parentReferenceTypeId == null)
            {
                throw new System.ArgumentNullException("parentReferenceTypeId");
            }

            lock (m_lock)
            {
                List<ReferenceTypeNode> resultReferenceTypes = new List<ReferenceTypeNode>();

                if (m_session == null)
                {
                    return resultReferenceTypes;
                }

                IList<Opc.Ua.INode> subtypes = m_session.NodeCache.FindReferences(parentReferenceTypeId, Opc.Ua.ReferenceTypeIds.HasSubtype, false, true);
                Opc.Ua.ReferenceTypeNode node = null;

                foreach (Opc.Ua.INode subtype in subtypes)
                {
                    node = m_session.NodeCache.Find(subtype.NodeId) as Opc.Ua.ReferenceTypeNode;

                    if (node == null)
                    {
                        continue;
                    }

                    resultReferenceTypes.Add(new ReferenceTypeNode(node));
                }

                return resultReferenceTypes;
            }
        }

        /// <summary>
        /// Gets the events types and properties of events starting from the specified reference type node id. The method browses one level following the HasSubtype and HasProperty reference types.
        /// Internal caching from Softing.Opc.Ua.Sdk Toolkit is used.
        /// </summary>
        /// <param name="parentEventTypeId"> A <see cref="NodeId"/> type representing the start node id for browsing.</param>
        /// <param name="includeProperties"> A <see cref="bool"/> type representing if the properties of the event types will be retrieved . </param>
        /// <returns>A list of <see cref="ReferenceDescription"/> with all references found. Empty list is returned if none was found.</returns>
        public IList<ReferenceDescription> GetEventTypes(NodeId parentEventTypeId, bool includeProperties)
        {
            if (parentEventTypeId == null)
            {
                throw new System.ArgumentNullException("parentEventTypeId");
            }

            lock (m_lock)
            {
                List<ReferenceDescription> result = new List<ReferenceDescription>();

                if (m_session == null)
                {
                    return result;
                }

                if (includeProperties)
                {
                    result.AddRange(CreateReferenceDescriptionList(parentEventTypeId, Opc.Ua.ReferenceTypeIds.HasProperty, true));

                    IList<ReferenceDescription> components = CreateReferenceDescriptionList(parentEventTypeId, Opc.Ua.ReferenceTypeIds.HasComponent, true);

                    foreach (ReferenceDescription component in components)
                    {
                        if (component.NodeClass == NodeClass.Variable)
                        {
                            result.Add(component);
                        }
                    }
                }

                result.AddRange(CreateReferenceDescriptionList(parentEventTypeId, Opc.Ua.ReferenceTypeIds.HasSubtype, false));
                return result;
            }
        }

        /// <summary>
        /// Retrieves the display name of the node identified by the given node id from the address space.
        /// Internal caching from Softing.Opc.Ua.Sdk Toolkit is used.
        /// </summary>
        /// <param name="nodeId">The node id in a string format.</param>
        /// <returns>The name of the node as a string.</returns>
        public string GetNodeName(string nodeId)
        {
            return m_session.NodeCache.GetDisplayText(new Opc.Ua.NodeId(nodeId));
        }

        /// <summary>
        /// Returns whether a type is a subtype of another type.
        /// </summary>
        /// <param name="subTypeId"> A <see cref="ExpandedNodeId"/> representing the sub type id.</param>
        /// <param name="superTypeId">A <see cref="ExpandedNodeId"/> representing the super type id.</param>
        /// <returns>
        /// A <see cref="bool"/> type representing if subTypeId parameter is a subtype of  superTypeId parameter
        /// </returns>
        /// <remarks>The nodes can be specified as uint values; an explicit conversion is performed.</remarks>
        public bool IsTypeOf(ExpandedNodeId subTypeId, ExpandedNodeId superTypeId)
        {
            if (subTypeId == null || superTypeId == null || m_session == null)
            {
                return false;
            }

            try
            {
                return m_session.TypeTree.IsTypeOf(subTypeId, superTypeId);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the XML encoded representation of the provided value.
        /// </summary>
        /// <param name="value">The value to be returned as XML.</param>
        /// <returns>The XML encoded representation of the provided value.</returns>
        public System.Xml.XmlElement GetXmlEncodedValue(DataValue value)
        {
            if (value == null)
            {
                return null;
            }

            Opc.Ua.XmlEncoder encoder = CreateEncoder(m_session.SystemContext);

            encoder.WriteVariantContents(value.WrappedValue.Value, value.WrappedValue.TypeInfo);

            System.Xml.XmlDocument document = new System.Xml.XmlDocument();
            document.InnerXml = encoder.Close();

            return document.DocumentElement;
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Associates a <see cref="Opc.Ua.Client.Session"/> with this BrowseHandler
        /// </summary>
        /// <param name="session">A <see cref="Opc.Ua.Client.Session"/> associated with this BrowseHandler.</param>
        internal void SetSession(Opc.Ua.Client.Session session)
        {
            m_session = session;
        }

        #endregion Internal Methods

        #region Private Methods

        private IList<ReferenceDescription> CreateReferenceDescriptionList(Opc.Ua.NodeId nodeRef, Opc.Ua.NodeId type, bool isProperty)
        {
            List<ReferenceDescription> result = new List<ReferenceDescription>();
            IList<Opc.Ua.INode> browseResult = m_session.NodeCache.FindReferences(nodeRef, type, false, true);
            Opc.Ua.INode node = null;

            foreach (Opc.Ua.INode subtype in browseResult)
            {
                node = m_session.NodeCache.Find(subtype.NodeId);

                if (node == null)
                {
                    continue;
                }

                ReferenceDescription convertedNode = new ReferenceDescription();
                convertedNode.NodeId = new ExpandedNodeId(node.NodeId);
                convertedNode.DisplayName = new LocalizedText(node.DisplayName);
                convertedNode.BrowseName = new QualifiedName(node.BrowseName);
                convertedNode.NodeClass = (NodeClass)node.NodeClass;
                //todo investigate why ReferenceTypeName
                //convertedNode.ReferenceTypeName = isProperty.ToString();
                result.Add(convertedNode);
            }

            return result;
        }

        private Opc.Ua.XmlEncoder CreateEncoder(Opc.Ua.ISystemContext context)
        {
            Opc.Ua.ServiceMessageContext messageContext = new Opc.Ua.ServiceMessageContext();
            messageContext.NamespaceUris = context.NamespaceUris;
            messageContext.ServerUris = context.ServerUris;
            messageContext.Factory = context.EncodeableFactory;

            Opc.Ua.XmlEncoder encoder = new Opc.Ua.XmlEncoder(messageContext);

            Opc.Ua.NamespaceTable namespaceUris = new Opc.Ua.NamespaceTable();

            Opc.Ua.StringTable serverUris = new Opc.Ua.StringTable();
            serverUris.Append(context.ServerUris.GetString(0));

            encoder.SetMappingTables(namespaceUris, serverUris);

            return encoder;
        }

        #endregion Private Methods
    }
}
