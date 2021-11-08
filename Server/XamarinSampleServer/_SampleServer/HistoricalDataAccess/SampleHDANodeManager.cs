/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA_SIA_EN
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Opc.Ua.Server;
using Opc.Ua;
using Softing.Opc.Ua.Server;
using System.Text;

namespace SampleServer.HistoricalDataAccess
{
    class SampleHDANodeManager : NodeManager
    {
        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public SampleHDANodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.HistoricalDataAccess)
        {
        }

        #endregion      
        #region INodeIdFactory Members
        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="node">The node.</param>
        /// <returns>The new NodeId.</returns>
        /// <remarks>
        /// This method is called by the NodeState.Create() method which initializes a Node from
        /// the type model. During initialization a number of child nodes are created and need to 
        /// have NodeIds assigned to them. This implementation constructs NodeIds by constructing
        /// strings. Other implementations could assign unique integers or Guids and save the new
        /// Node in a dictionary for later lookup.
        /// </remarks>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            BaseInstanceState instance = node as BaseInstanceState;

            if (instance != null && instance.Parent != null)
            {
                // Parent must have a string identifier
                string parentId = instance.Parent.NodeId.Identifier as string;

                if (parentId == null)
                {
                    return null;
                }

                StringBuilder buffer = new StringBuilder();
                buffer.Append(parentId);

                // Check if the parent is another component
                bool isAntoherComponent = parentId.IndexOf('?') == -1;
                buffer.Append(isAntoherComponent ? '?' : '/');

                buffer.Append(node.SymbolicName);

                return new NodeId(buffer.ToString(), instance.Parent.NodeId.NamespaceIndex);
            }
            if (node != null && node.BrowseName != null)
            {
                return new NodeId(node.BrowseName.Name, NamespaceIndex);
            }
            return base.New(context, node);
        }
        #endregion

        #region Overridden Methods
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            base.CreateAddressSpace(externalReferences);

            try
            {
                FolderState root = CreateFolder(null, "HistoricalDataAccess");
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                // Add Support for Event Notifiers
                // Creating notifier ensures events propagate up the hierarchy when they are produced
                AddRootNotifier(root);
                
                // Historical Access
                FolderState dynamicHistoricals = CreateFolder(root, "DynamicHistoricalDataItems");
                dynamicHistoricals.EventNotifier = EventNotifiers.SubscribeToEvents;
               
                FolderState staticHistoricals = CreateFolder(root, "StaticHistoricalDataItems");
                staticHistoricals.EventNotifier = EventNotifiers.SubscribeToEvents;
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "HistoricalAccess.HistoricalDataAccess.SampleHDANodeManager.CreateAddressSpace");
                throw;
            }
        }
        #endregion
    }
}