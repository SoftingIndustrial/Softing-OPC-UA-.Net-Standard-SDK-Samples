/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;
using SampleServer;

namespace SampleServer.UserAuthentication
{
    /// <summary>
    /// A node manager for a server that exposes several variables
    /// </summary>
    public class UserAuthenticationNodeManager : NodeManager
    {
        #region Constructors
        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public UserAuthenticationNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.UserAuthentication)
        {
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
                // Execute base class CreateAddressSpace
                base.CreateAddressSpace(externalReferences);

                // Create a root node and add a reference to external Server Objects Folder
                FolderState process = CreateFolder(null, "UserAuthentication");
                process.Description = new LocalizedText($"To test user authentication, try to change the value of LogFilePath. " +
                   $"Anonymous will not be able to change the value, while an authenticated user can do this.", "en-US");

                AddReference(process, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);                           

                // A property to report the process state
                PropertyState<string> state = CreateProperty<string>(process, "LogFilePath");               
                state.AccessLevel = AccessLevels.CurrentReadOrWrite;
                state.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                state.Value = ".\\Log.txt";

                state.OnSimpleWriteValue = OnWriteValue;
                state.OnReadUserAccessLevel = OnReadUserAccessLevel;               
            } 
        }

        #endregion

        #region PropertyState - Event Handlers
        /// <summary>
        /// Handler for OnWriteValue event of PropertyState node 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private ServiceResult OnWriteValue(ISystemContext context, NodeState node, ref object value)
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

        /// <summary>
        /// Handler for OnReadUserAccessLevel event of PropertyState node 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private ServiceResult OnReadUserAccessLevel(ISystemContext context, NodeState node, ref byte value)
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
        #endregion
    }
}