/* ========================================================================
 * Copyright © 2011-2017 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * The Software is based on the OPC Foundation, Inc.’s software. This 
 * original OPC Foundation’s software can be found here:
 * http://www.opcfoundation.org
 * 
 * The original OPC Foundation’s software is subject to the OPC Foundation
 * MIT License 1.00, which can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using Opc.Ua;
using Opc.Ua.Server;

namespace Softing.OpcUa.Samples.UserAuthenticationServer
{
    /// <summary>
    /// A node manager for a server that exposes several variables
    /// </summary>
    public class UserAuthenticationNodeManager : CustomNodeManager2
    {
        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public UserAuthenticationNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, SampleServer.Namespaces.UserAuthentication)
        {
            SystemContext.NodeIdFactory = this;
        }

        #endregion
        
        #region IDisposable Members

        /// <summary>
        /// An overrideable version of the Dispose
        /// </summary>
        protected override void Dispose(bool disposing)
        {  
            if (disposing)
            {
                // TBD
            }
        }

        #endregion

        #region INodeIdFactory Members

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            return node.NodeId;
        }

        #endregion

        #region INodeManager Members

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // Create a object to represent the process being controlled
                BaseObjectState process = new BaseObjectState(null);

                process.NodeId = new NodeId(1, NamespaceIndex);
                process.BrowseName = new QualifiedName("UserAuthentication", NamespaceIndex);
                process.DisplayName = process.BrowseName.Name;
                process.TypeDefinitionId = ObjectTypeIds.BaseObjectType;
                process.Description = new LocalizedText("To test user authentication, try to change the value of LogFilePath. Anonymous will not be able to change the value, while an authenticated user can do this.", "en-US");

                // Ensure the process object can be found via the server object
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                process.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, process.NodeId));

                // A property to report the process state
                PropertyState<string> state = new PropertyState<string>(process);

                state.NodeId = new NodeId(2, NamespaceIndex);
                state.BrowseName = new QualifiedName("LogFilePath", NamespaceIndex);
                state.DisplayName = state.BrowseName.Name;
                state.TypeDefinitionId = VariableTypeIds.PropertyType;
                state.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                state.DataType = DataTypeIds.String;
                state.ValueRank = ValueRanks.Scalar;
                state.AccessLevel = AccessLevels.CurrentReadOrWrite;
                state.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                state.Value = ".\\Log.txt";

                process.AddChild(state);
                
                state.OnReadUserAccessLevel = OnReadUserAccessLevel;
                state.OnSimpleWriteValue = OnWriteValue;

                // Save in dictionary
                AddPredefinedNode(SystemContext, process);
            } 
        }

        public ServiceResult OnWriteValue(ISystemContext context, NodeState node, ref object value)
        {
            if (context.UserIdentity == null || context.UserIdentity.TokenType == UserTokenType.Anonymous)
            {
                TranslationInfo info = new TranslationInfo("BadUserAccessDenied", "en-US", "User cannot change value.");

                return new ServiceResult(StatusCodes.BadUserAccessDenied, new LocalizedText(info));
            }

            // Attempt to update file system
            try
            {
                string filePath = value as string;
                PropertyState<string> variable = node as PropertyState<string>;

                if (!String.IsNullOrEmpty(variable.Value))
                {
                    FileInfo file = new FileInfo(variable.Value);

                    if (file.Exists)
                    {
                        file.Delete();
                    }
                }

                if (!String.IsNullOrEmpty(filePath))
                {
                    FileInfo file = new FileInfo(filePath);

                    using (StreamWriter writer = file.CreateText())
                    {
                        writer.WriteLine(System.Security.Principal.WindowsIdentity.GetCurrent().Name);
                    }
                }

                value = filePath;
            }
            catch (Exception e)
            {
                return ServiceResult.Create(e, StatusCodes.BadUserAccessDenied, "Could not update file system.");
            }

            return ServiceResult.Good;
        }

        public ServiceResult OnReadUserAccessLevel(ISystemContext context, NodeState node, ref byte value)
        {
            if (context.UserIdentity == null || context.UserIdentity.TokenType == UserTokenType.Anonymous)
            {
                value = AccessLevels.CurrentRead;
            }
            else
            {
                value = AccessLevels.CurrentReadOrWrite;
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Frees any resources allocated for the address space
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                // TBD
            }
        }

        /// <summary>
        /// Returns a unique handle for the node
        /// </summary>
        protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                // Quickly exclude nodes that are not in the namespace 
                if (!IsNodeIdInNamespace(nodeId))
                {
                    return null;
                }

                NodeState node = null;

                if (!PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    return null;
                }

                NodeHandle handle = new NodeHandle();

                handle.NodeId = nodeId;
                handle.Node = node;
                handle.Validated = true;

                return handle;
            } 
        }

        /// <summary>
        /// Verifies that the specified node exists
        /// </summary>
        protected override NodeState ValidateNode(ServerSystemContext context, NodeHandle handle, IDictionary<NodeId, NodeState> cache)
        {
            // Not valid if no root
            if (handle == null)
            {
                return null;
            }

            // Check if previously validated
            if (handle.Validated)
            {
                return handle.Node;
            }

            return null;
        }
        
        #endregion
    }
}