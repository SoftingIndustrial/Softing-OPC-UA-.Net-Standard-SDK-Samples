/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.Methods
{
    /// <summary>
    /// A node manager for a server that provides an implementation of the OPC UA features
    /// </summary>
    public class MethodsNodeManager : NodeManager
    {
        #region Constructors

        /// <summary>
        /// Initializes the node manager
        /// </summary>
        public MethodsNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.Methods)
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
                FolderState root = CreateFolder(null, "Methods");
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);               

                #region Create Add Method
                Argument[] inputArgumentsAdd = new Argument[]
                {
                    new Argument() {Name = "Float value", Description = "Float value", DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar},
                    new Argument() {Name = "UInt32 value", Description = "UInt32 value", DataType = DataTypeIds.UInt32, ValueRank = ValueRanks.Scalar}
                };

                Argument[] outputArgumentsAdd = new Argument[]
                {
                 new Argument() {Name = "Add Result", Description = "Add Result", DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar}
                };

                CreateMethod(root, "Add", inputArgumentsAdd, outputArgumentsAdd, OnAddCall);
                #endregion

                #region Create Multiply Method
                Argument[] inputArgumentsMultiply = new Argument[]
                {
                    new Argument() {Name = "Int16 value", Description = "Int16 value", DataType = DataTypeIds.Int16, ValueRank = ValueRanks.Scalar},
                    new Argument() {Name = "UInt16 value", Description = "UInt16 value", DataType = DataTypeIds.UInt16, ValueRank = ValueRanks.Scalar}
                };

                Argument[] outputArgumentsMultiply = new Argument[]
                {
                 new Argument() {Name = "Multiply Result", Description = "Multiply Result", DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar}
                };

                CreateMethod(root, "Multiply", inputArgumentsMultiply, outputArgumentsMultiply, OnMultiplyCall);
                #endregion                
            }
        }

        #endregion

        #region Private Methods - OnCall Event Handlers
        /// <summary>
        /// Handles the method call
        /// </summary>
        private ServiceResult OnAddCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            // All arguments must be provided
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                float floatValue = (float)inputArguments[0];
                UInt32 uintValue = (UInt32)inputArguments[1];

                // Set output parameter
                outputArguments[0] = (float)(floatValue + uintValue);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        /// <summary>
        /// Handles the method call
        /// </summary>
        private ServiceResult OnMultiplyCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            // All arguments must be provided
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                Int16 op1 = (Int16)inputArguments[0];
                UInt16 op2 = (UInt16)inputArguments[1];

                // Set output parameter
                outputArguments[0] = (Int32)(op1 * op2);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        #endregion

    }
}