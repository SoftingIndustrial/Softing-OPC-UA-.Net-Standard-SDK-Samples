/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Opc.Ua.Server;
using Opc.Ua;
using Softing.Opc.Ua.Server;

namespace SampleServer.HistoricalDataAccess
{
    public abstract class HistoricalDataAccessNodeManager : NodeManager
    {
        #region Constructor
        /// <summary>
        /// Initializes the node manager
        /// </summary>
        protected HistoricalDataAccessNodeManager(IServerInternal server, ApplicationConfiguration configuration, params string[] namespaceUris) : base(server, configuration, namespaceUris)
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

            if(instance != null && instance.Parent != null)
            {
                // Parent must have a string identifier
                string parentId = instance.Parent.NodeId.Identifier as string;

                if(parentId == null)
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

        #region Public Methods - Overrides
        /// <summary>
        /// Create address space for current node manager
        /// Invoked during the initialization of the address space.
        /// </summary>
        /// <param name="externalReferences"></param>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            base.CreateAddressSpace(externalReferences);

            lock (Server.DiagnosticsLock)
            {
                // find HistoryServerCapabilities node.
                HistoryServerCapabilitiesState capabilities = Server.DiagnosticsNodeManager.FindPredefinedNode(ObjectIds.HistoryServerCapabilities, null) as HistoryServerCapabilitiesState;

                if (capabilities != null)
                {
                    capabilities.AccessHistoryDataCapability.Value = true;
                    capabilities.InsertDataCapability.Value = true;
                    capabilities.ReplaceDataCapability.Value = true;
                    capabilities.UpdateDataCapability.Value = true;
                    capabilities.DeleteRawCapability.Value = true;
                    capabilities.DeleteAtTimeCapability.Value = true;
                    capabilities.InsertAnnotationCapability.Value = true;
                }
            }            
        }

        #endregion

        internal static void SetTheRightSupportedAggregates(NodeState aggregateFunctions)
        {
            NodeId[] supportedAggregates = new NodeId[] {
                ObjectIds.AggregateFunction_Interpolative,  ObjectIds.AggregateFunction_Average,
                //ObjectIds.AggregateFunction_TimeAverage, 
                ObjectIds.AggregateFunction_TimeAverage2,
                ObjectIds.AggregateFunction_Total, ObjectIds.AggregateFunction_Total2,
                ObjectIds.AggregateFunction_Minimum, ObjectIds.AggregateFunction_Minimum2,
                ObjectIds.AggregateFunction_MinimumActualTime, ObjectIds.AggregateFunction_MinimumActualTime2,
                ObjectIds.AggregateFunction_Maximum, ObjectIds.AggregateFunction_Maximum2,
                ObjectIds.AggregateFunction_MaximumActualTime, ObjectIds.AggregateFunction_MaximumActualTime2,
                ObjectIds.AggregateFunction_Range, ObjectIds.AggregateFunction_Range2,
                //ObjectIds.AggregateFunction_AnnotationCount,
                ObjectIds.AggregateFunction_Count,
                ObjectIds.AggregateFunction_DurationInStateZero, ObjectIds.AggregateFunction_DurationInStateNonZero,
               // ObjectIds.AggregateFunction_NumberOfTransitions,
                ObjectIds.AggregateFunction_Start, ObjectIds.AggregateFunction_End,
                ObjectIds.AggregateFunction_Delta, 
                ObjectIds.AggregateFunction_StartBound,  ObjectIds.AggregateFunction_EndBound,
                ObjectIds.AggregateFunction_DeltaBounds,
                ObjectIds.AggregateFunction_DurationGood, ObjectIds.AggregateFunction_DurationBad,
                ObjectIds.AggregateFunction_PercentGood, ObjectIds.AggregateFunction_PercentBad,
                ObjectIds.AggregateFunction_WorstQuality, ObjectIds.AggregateFunction_WorstQuality2,
                //ObjectIds.AggregateFunction_StandardDeviationSample, ObjectIds.AggregateFunction_StandardDeviationPopulation,
                //ObjectIds.AggregateFunction_VarianceSample, ObjectIds.AggregateFunction_VariancePopulation
            };

            // rempve all references from aggregateFunctions
            aggregateFunctions.RemoveReferences(ReferenceTypeIds.Organizes, false);

            // add reference to supported aggregates
            foreach(NodeId aggregateId in supportedAggregates)
            {
                aggregateFunctions.AddReference(ReferenceTypeIds.Organizes, false, aggregateId);
                aggregateFunctions.AddReference(ReferenceTypeIds.HasComponent, false, aggregateId);
            }
        }

        #region Historian Functions
        /// <summary>
        /// Reads the raw data for an item
        /// </summary>
        protected override void HistoryReadRawModified(
            ServerSystemContext context,
            ReadRawModifiedDetails details,
            TimestampsToReturn timestampsToReturn,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for(int ii = 0; ii < nodesToRead.Count; ii++)
            {
                NodeHandle handle = nodesToProcess[ii];
                HistoryReadValueId nodeToRead = nodesToRead[handle.Index];
                HistoryReadResult result = results[handle.Index];
                HistoryReadRequest request = null;

                try
                {
                    // Validate node
                    NodeState source = ValidateNode(context, handle, cache);

                    if(source == null)
                    {
                        continue;
                    }

                    // Load an existing request
                    if(nodeToRead.ContinuationPoint != null)
                    {
                        request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                        if(request == null)
                        {
                            errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
                            continue;
                        }
                    }
                    else // Create a new request
                    {
                        request = CreateHistoryReadRequest(context, details, handle, nodeToRead);
                    }

                    // Process values until the max is reached
                    HistoryData data = (details.IsReadModified) ? new HistoryModifiedData() : new HistoryData();
                    HistoryModifiedData modifiedData = data as HistoryModifiedData;

                    while(request.NumValuesPerNode == 0 || data.DataValues.Count < request.NumValuesPerNode)
                    {
                        if(request.Values.Count == 0)
                        {
                            break;
                        }

                        DataValue value = request.Values.First.Value;
                        request.Values.RemoveFirst();
                        data.DataValues.Add(value);

                        if(modifiedData != null)
                        {
                            ModificationInfo modificationInfo = null;

                            if(request.ModificationInfos != null && request.ModificationInfos.Count > 0)
                            {
                                modificationInfo = request.ModificationInfos.First.Value;
                                request.ModificationInfos.RemoveFirst();
                            }

                            modifiedData.ModificationInfos.Add(modificationInfo);
                        }
                    }

                    errors[handle.Index] = ServiceResult.Good;

                    // Check if a continuation point is required
                    if(request.Values.Count > 0)
                    {
                        // Only set if both end time and start time are specified
                        if(details.StartTime != DateTime.MinValue && details.EndTime != DateTime.MinValue)
                        {
                            result.ContinuationPoint = SaveContinuationPoint(context, request);
                        }
                    }

                    // Check if no data returned
                    if(data.DataValues.Count == 0)
                    {
                        errors[handle.Index] = StatusCodes.GoodNoData;
                    }

                    // Return the data
                    result.HistoryData = new ExtensionObject(data);
                }
                catch(Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }
            }
        }

        /// <summary>
        /// Reads the processed data for an item
        /// </summary>
        protected override void HistoryReadProcessed(
            ServerSystemContext context,
            ReadProcessedDetails details,
            TimestampsToReturn timestampsToReturn,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            if (details.StartTime == details.EndTime)
            {
                foreach (HistoryReadResult result in results)
                {
                    result.StatusCode = StatusCodes.BadInvalidArgument;
                }
                for (int i = 0; i < errors.Count; i++)
                {
                    errors[i] = StatusCodes.BadInvalidArgument;
                }
                return;
            }


            for (int ii = 0; ii < nodesToRead.Count; ii++)
            {
                NodeHandle handle = nodesToProcess[ii];
                HistoryReadValueId nodeToRead = nodesToRead[handle.Index];
                HistoryReadResult result = results[handle.Index];
                HistoryReadRequest request = null;

                try
                {
                    // Validate node
                    NodeState source = ValidateNode(context, handle, cache);

                    if(source == null)
                    {
                        continue;
                    }

                    List<NodeId> supportsAll = new List<NodeId>
                    {
                        ObjectIds.AggregateFunction_AnnotationCount, ObjectIds.AggregateFunction_Count,
                        ObjectIds.AggregateFunction_Start, ObjectIds.AggregateFunction_StartBound,
                        ObjectIds.AggregateFunction_End, ObjectIds.AggregateFunction_EndBound,
                        ObjectIds.AggregateFunction_DurationGood, ObjectIds.AggregateFunction_DurationBad,
                        ObjectIds.AggregateFunction_PercentGood, ObjectIds.AggregateFunction_PercentBad,
                        ObjectIds.AggregateFunction_WorstQuality, ObjectIds.AggregateFunction_WorstQuality2,
                    };
                    List<NodeId> supportsBoolean = new List<NodeId>
                    {
                        ObjectIds.AggregateFunction_DurationInStateZero, 
                        ObjectIds.AggregateFunction_DurationInStateNonZero,
                        ObjectIds.AggregateFunction_NumberOfTransitions
                    };

                    // check if the aggregate is supported for node data type
                    BuiltInType builtInType = TypeInfo.GetBuiltInType(((BaseVariableState)source)?.DataType);
                    if (!TypeInfo.IsNumericType(builtInType))
                    {
                        // non numeric types have restrictions
                        if (!supportsAll.Contains(details.AggregateType[ii])
                            && (!(builtInType == BuiltInType.Boolean && supportsBoolean.Contains(details.AggregateType[ii]))))
                        {
                            errors[handle.Index] = StatusCodes.BadAggregateNotSupported;
                            continue;
                        }
                    }
                    // Load an existing request
                    if(nodeToRead.ContinuationPoint != null)
                    {
                        request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                        if(request == null)
                        {
                            errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
                            continue;
                        }
                    }
                    else // Create a new request
                    {
                        // Validate aggregate type
                        if(details.AggregateType.Count <= ii || !Server.AggregateManager.IsSupported(details.AggregateType[ii]))
                        {
                            errors[handle.Index] = StatusCodes.BadAggregateNotSupported;
                            continue;
                        }

                        request = CreateHistoryReadRequest(context, details, handle, nodeToRead, details.AggregateType[ii]);
                    }

                    // Process values until the max is reached
                    HistoryData data = new HistoryData();

                    while(request.NumValuesPerNode == 0 || data.DataValues.Count < request.NumValuesPerNode)
                    {
                        if(request.Values.Count == 0)
                        {
                            break;
                        }

                        DataValue value = request.Values.First.Value;
                        request.Values.RemoveFirst();
                        data.DataValues.Add(value);
                    }

                    errors[handle.Index] = ServiceResult.Good;

                    // Check if a continuation point is required
                    if(request.Values.Count > 0)
                    {
                        result.ContinuationPoint = SaveContinuationPoint(context, request);
                    }

                    // Check if no data returned
                    if(data.DataValues.Count == 0)
                    {
                        errors[handle.Index] = StatusCodes.GoodNoData;
                    }

                    // Return the data
                    result.HistoryData = new ExtensionObject(data);
                }
                catch(Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }
            }
        }

        /// <summary>
        /// Reads the data at the specified time for an item
        /// </summary>
        protected override void HistoryReadAtTime(
            ServerSystemContext context,
            ReadAtTimeDetails details,
            TimestampsToReturn timestampsToReturn,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for(int ii = 0; ii < nodesToRead.Count; ii++)
            {
                NodeHandle handle = nodesToProcess[ii];
                HistoryReadValueId nodeToRead = nodesToRead[handle.Index];
                HistoryReadResult result = results[handle.Index];

                HistoryReadRequest request = null;

                try
                {
                    // Validate node
                    NodeState source = ValidateNode(context, handle, cache);

                    if(source == null)
                    {
                        continue;
                    }

                    // Load an existing request
                    if(nodeToRead.ContinuationPoint != null)
                    {
                        request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                        if(request == null)
                        {
                            errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
                            continue;
                        }
                    }
                    else // Create a new request
                    {
                        request = CreateHistoryReadRequest(context, details, handle, nodeToRead);
                    }

                    // Process values until the max is reached
                    HistoryData data = new HistoryData();

                    while(request.NumValuesPerNode == 0 || data.DataValues.Count < request.NumValuesPerNode)
                    {
                        if(request.Values.Count == 0)
                        {
                            break;
                        }

                        DataValue value = request.Values.First.Value;
                        request.Values.RemoveFirst();
                        data.DataValues.Add(value);
                    }

                    errors[handle.Index] = ServiceResult.Good;

                    // Check if a continuation point is required
                    if(request.Values.Count > 0)
                    {
                        result.ContinuationPoint = SaveContinuationPoint(context, request);
                    }

                    // Check if no data returned
                    if(data.DataValues.Count == 0)
                    {
                        errors[handle.Index] = StatusCodes.GoodNoData;
                    }

                    // Return the data
                    result.HistoryData = new ExtensionObject(data);
                }
                catch(Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }
            }
        }

        /// <summary>
        /// Updates the data history for one or more nodes
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToUpdate"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryUpdateData(
            ServerSystemContext context,
            IList<UpdateDataDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for(int ii = 0; ii < nodesToProcess.Count; ii++)
            {
                NodeHandle handle = nodesToProcess[ii];
                UpdateDataDetails nodeToUpdate = nodesToUpdate[handle.Index];
                HistoryUpdateResult result = results[handle.Index];
                DataValueCollection oldValues = new DataValueCollection();

                try
                {
                    // Remove not supported
                    if(nodeToUpdate.PerformInsertReplace == PerformUpdateType.Remove)
                    {
                        continue;
                    }

                    // Validate node
                    NodeState source = ValidateNode(context, handle, cache);

                    if(source == null)
                    {
                        continue;
                    }

                    // Load the archive
                    ArchiveItemState item = handle.Node as ArchiveItemState;

                    if(item == null)
                    {
                        continue;
                    }

                    item.ReloadFromSource(context);

                    // Process each item
                    for (int jj = 0; jj < nodeToUpdate.UpdateValues.Count; jj++)
                    {
                        List<DataValue> readValues = item.ReadValues(nodeToUpdate.UpdateValues[jj].SourceTimestamp);
                        if (readValues.Count > 0)
                        {
                            oldValues.AddRange(readValues);
                        }

                        StatusCode error = item.UpdateHistory(context, nodeToUpdate.UpdateValues[jj], nodeToUpdate.PerformInsertReplace);
                        result.OperationResults.Add(error);
                    }

                    errors[handle.Index] = ServiceResult.Good;
                }
                catch(Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }

                Server.ReportAuditHistoryValueUpdateEvent(context, nodeToUpdate, oldValues.ToArray(), errors[handle.Index].StatusCode);
            }
        }

        /// <summary>
        /// Updates the data history for one or more nodes
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToUpdate"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryUpdateStructureData(
            ServerSystemContext context,
            IList<UpdateStructureDataDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for(int ii = 0; ii < nodesToProcess.Count; ii++)
            {
                NodeHandle handle = nodesToProcess[ii];
                UpdateStructureDataDetails nodeToUpdate = nodesToUpdate[handle.Index];
                HistoryUpdateResult result = results[handle.Index];
                DataValueCollection oldValues = new DataValueCollection();

                try
                {
                    // Validate node
                    NodeState source = ValidateNode(context, handle, cache);

                    if(source == null)
                    {
                        continue;
                    }

                    // Only support annotations
                    if(handle.Node.BrowseName != BrowseNames.Annotations)
                    {
                        continue;
                    }

                    // Load the archive
                    ArchiveItemState item = Reload(context, handle);

                    if(item == null)
                    {
                        continue;
                    }

                    oldValues = (DataValueCollection)nodeToUpdate.UpdateValues.MemberwiseClone();

                    // Process each item
                    for (int jj = 0; jj < nodeToUpdate.UpdateValues.Count; jj++)
                    {
                        List<DataValue> readValues = item.ReadValues(nodeToUpdate.UpdateValues[jj].SourceTimestamp);
                        if (readValues.Count > 0)
                        {
                            oldValues.AddRange(readValues);
                        }

                        Annotation annotation = ExtensionObject.ToEncodeable(nodeToUpdate.UpdateValues[jj].Value as ExtensionObject) as Annotation;

                        if(annotation == null)
                        {
                            result.OperationResults.Add(StatusCodes.BadTypeMismatch);
                            continue;
                        }

                        StatusCode error = item.UpdateAnnotations(context, annotation, nodeToUpdate.UpdateValues[jj], nodeToUpdate.PerformInsertReplace);

                        result.OperationResults.Add(error);
                    }

                    errors[handle.Index] = ServiceResult.Good;
                }
                catch(Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }

                Server.ReportAuditHistoryAnnotationUpdateEvent(context, nodeToUpdate, oldValues.ToArray(), errors[handle.Index].StatusCode);
            }
        }

        /// <summary>
        /// Deletes the data history for one or more nodes
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodesToUpdate"></param>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        /// <param name="nodesToProcess"></param>
        /// <param name="cache"></param>
        protected override void HistoryDeleteRawModified(
            ServerSystemContext context,
            IList<DeleteRawModifiedDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for(int ii = 0; ii < nodesToProcess.Count; ii++)
            {
                NodeHandle handle = nodesToProcess[ii];
                DeleteRawModifiedDetails nodeToUpdate = nodesToUpdate[handle.Index];
                HistoryUpdateResult result = results[handle.Index];
                DataValueCollection oldValues = new DataValueCollection();

                try
                {
                    // Validate node
                    NodeState source = ValidateNode(context, handle, cache);

                    if(source == null)
                    {
                        continue;
                    }

                    // Load the archive
                    ArchiveItemState item = handle.Node as ArchiveItemState;

                    if(item == null)
                    {
                        continue;
                    }

                    item.ReloadFromSource(context);

                    List<DataValue> readValues = item.ReadValues(nodeToUpdate.StartTime);
                    if (readValues.Count > 0)
                    {
                        oldValues.AddRange(readValues);
                    }

                    // Delete the history
                    errors[handle.Index] = item.DeleteHistory(context, nodeToUpdate.StartTime, nodeToUpdate.EndTime, nodeToUpdate.IsDeleteModified);
                }
                catch(Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Error deleting data from archive.");
                }

                Server.ReportAuditHistoryRawModifyDeleteEvent(context, nodeToUpdate, oldValues.ToArray(), errors[handle.Index].StatusCode);
            }
        }

        /// <summary>
        /// Deletes the data history for one or more nodes
        /// </summary>
        protected override void HistoryDeleteAtTime(
            ServerSystemContext context,
            IList<DeleteAtTimeDetails> nodesToUpdate,
            IList<HistoryUpdateResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for(int ii = 0; ii < nodesToProcess.Count; ii++)
            {
                NodeHandle handle = nodesToProcess[ii];
                DeleteAtTimeDetails nodeToUpdate = nodesToUpdate[handle.Index];
                HistoryUpdateResult result = results[handle.Index];
                DataValueCollection oldValues = new DataValueCollection();

                try
                {
                    // Validate node
                    NodeState source = ValidateNode(context, handle, cache);

                    if(source == null)
                    {
                        continue;
                    }

                    // Load the archive
                    ArchiveItemState item = handle.Node as ArchiveItemState;

                    if(item == null)
                    {
                        continue;
                    }

                    item.ReloadFromSource(context);

                    foreach (DateTime reqTime in nodeToUpdate.ReqTimes)
                    {
                        List<DataValue> readValues = item.ReadValues(reqTime);
                        if (readValues.Count > 0)
                        {
                            oldValues.AddRange(readValues);
                        }
                    }

                    // Process each item
                    for (int jj = 0; jj < nodeToUpdate.ReqTimes.Count; jj++)
                    {
                        StatusCode error = item.DeleteHistory(context, nodeToUpdate.ReqTimes[jj]);
                        result.OperationResults.Add(error);
                    }

                    errors[handle.Index] = ServiceResult.Good;
                }
                catch(Exception e)
                {
                    errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
                }

                Server.ReportAuditHistoryAtTimeDeleteEvent(context, nodeToUpdate, oldValues.ToArray(), errors[handle.Index].StatusCode);
            }
        }
        #endregion

        #region Private Methods - History Helpers
        /// <summary>
        /// Loads the archive item state from the underlying source
        /// </summary>
        private ArchiveItemState Reload(ServerSystemContext context, NodeHandle handle)
        {
            ArchiveItemState item = handle.Node as ArchiveItemState;

            if(item == null)
            {
                BaseInstanceState property = handle.Node as BaseInstanceState;

                if(property != null)
                {
                    item = property.Parent as ArchiveItemState;
                }
            }

            if(item != null)
            {
                item.ReloadFromSource(context);
            }

            return item;
        }

        /// <summary>
        /// Creates a new history request
        /// </summary>
        private HistoryReadRequest CreateHistoryReadRequest(ServerSystemContext context,ReadRawModifiedDetails details,NodeHandle handle,HistoryReadValueId nodeToRead)
        {
            bool sizeLimited = (details.StartTime == DateTime.MinValue || details.EndTime == DateTime.MinValue);
            bool applyIndexRangeOrEncoding = (nodeToRead.ParsedIndexRange != NumericRange.Empty || !QualifiedName.IsNull(nodeToRead.DataEncoding));
            bool returnBounds = !details.IsReadModified && details.ReturnBounds;
            bool timeFlowsBackward = (details.EndTime == DateTime.MinValue) || (details.EndTime != DateTime.MinValue && details.EndTime < details.StartTime);

            // Find the archive item
            ArchiveItemState item = Reload(context, handle);

            if(item == null)
            {
                throw new ServiceResultException(StatusCodes.BadNotSupported);
            }

            LinkedList<DataValue> values = new LinkedList<DataValue>();
            LinkedList<ModificationInfo> modificationInfos = null;

            if(details.IsReadModified)
            {
                modificationInfos = new LinkedList<ModificationInfo>();
            }

            // Read history
            DataView view = item.ReadHistory(details.StartTime, details.EndTime, details.IsReadModified, handle.Node.BrowseName);

            int startBound = -1;
            int endBound = -1;
            int ii = (timeFlowsBackward) ? view.Count - 1 : 0;
            DateTime lastTimeReturned = DateTime.MinValue;

            while(ii >= 0 && ii < view.Count)
            {
                try
                {
                    DateTime timestamp = (DateTime) view[ii].Row[0];

                    // Check if looking for start of data
                    if(values.Count == 0)
                    {
                        if(timeFlowsBackward)
                        {
                            if(details.StartTime != DateTime.MinValue && timestamp >= details.StartTime) 
                            {
                                startBound = ii;

                                if(timestamp > details.StartTime)
                                {
                                    continue;
                                }
                            }
                            else if(details.StartTime == DateTime.MinValue && timestamp >= details.EndTime)
                            {
                                startBound = ii;

                                if(timestamp > details.EndTime)
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            if(timestamp <= details.StartTime)
                            {
                                startBound = ii;

                                if(timestamp < details.StartTime)
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    // Check if absolute max values specified
                    if(sizeLimited)
                    {
                        if(details.NumValuesPerNode > 0 && details.NumValuesPerNode < values.Count)
                        {
                            break;
                        }
                    }

                    // Check for end bound
                    if(timeFlowsBackward)
                    {
                        if(timestamp <= details.EndTime && details.StartTime != DateTime.MinValue)
                        {
                            endBound = ii;
                            break;
                        }
                    }
                    else
                    {
                        if(timestamp >= details.EndTime && details.EndTime != DateTime.MinValue)
                        {
                            if(startBound != ii)
                            {
                                endBound = ii;
                                break;
                            }
                        }
                    }

                    // Check if the start bound needs to be returned
                    if(returnBounds && values.Count == 0 && startBound != ii)
                    {
                        // Add start bound
                        if(startBound == -1)
                        {
                            DateTime startBoundTime;
                            if(details.StartTime != DateTime.MinValue)
                                startBoundTime = details.StartTime;
                            else
                                startBoundTime = details.EndTime;

                            values.AddLast(new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, startBoundTime, startBoundTime));
                            lastTimeReturned = startBoundTime;
                        }
                        else
                        {
                            values.AddLast(RowToDataValue(context, nodeToRead, view[startBound], applyIndexRangeOrEncoding));
                            lastTimeReturned = (DateTime) view[startBound].Row[0];
                        }

                        // Check if absolute max values specified
                        if(sizeLimited)
                        {
                            if(details.NumValuesPerNode > 0 && details.NumValuesPerNode < values.Count)
                            {
                                break;
                            }
                        }
                    }

                    // Add value
                    values.AddLast(RowToDataValue(context, nodeToRead, view[ii], applyIndexRangeOrEncoding));
                    lastTimeReturned = timestamp;

                    if(modificationInfos != null)
                    {
                        modificationInfos.AddLast((ModificationInfo) view[ii].Row[6]);
                    }
                }
                finally
                {
                    if(timeFlowsBackward)
                    {
                        ii--;
                    }
                    else
                    {
                        ii++;
                    }
                }
            }

            // Add late bound
            while(returnBounds)
            {
                // Add start bound
                if(values.Count == 0)
                {
                    if(startBound == -1)
                    {
                        DateTime startBoundTime;
                        if(details.StartTime != DateTime.MinValue)
                            startBoundTime = details.StartTime;
                        else
                            startBoundTime = details.EndTime;

                        values.AddLast(new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, startBoundTime, startBoundTime));
                        lastTimeReturned = startBoundTime;
                    }
                    else
                    {
                        values.AddLast(RowToDataValue(context, nodeToRead, view[startBound], applyIndexRangeOrEncoding));
                        lastTimeReturned = (DateTime) view[startBound].Row[0];
                    }
                }

                // Check if absolute max values specified
                if(sizeLimited)
                {
                    if(details.NumValuesPerNode > 0 && details.NumValuesPerNode < values.Count)
                    {
                        break;
                    }
                }

                // Add end bound
                if(endBound == -1)
                {
                    DateTime endBoundTime = details.EndTime;
                    if(details.StartTime != DateTime.MinValue && details.EndTime != DateTime.MinValue)
                    {
                        endBoundTime = details.EndTime;
                    }
                    else
                    {
                        if (lastTimeReturned == DateTime.MinValue)
                        {
                            endBoundTime = DateTime.MinValue;
                        }
                        else
                        {
                            endBoundTime = lastTimeReturned.AddSeconds(timeFlowsBackward ? -1.0d : 1.0d);
                        }
                    }

                    values.AddLast(new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, endBoundTime, endBoundTime));
                }
                else
                {
                    values.AddLast(RowToDataValue(context, nodeToRead, view[endBound], applyIndexRangeOrEncoding));
                }

                break;
            }

            HistoryReadRequest request = new HistoryReadRequest();
            request.Values = values;
            request.ModificationInfos = modificationInfos;
            request.NumValuesPerNode = details.NumValuesPerNode;
            request.Filter = null;
            return request;
        }

        /// <summary>
        /// Creates a new history request
        /// </summary>
        private HistoryReadRequest CreateHistoryReadRequest(
            ServerSystemContext context,
            ReadProcessedDetails details,
            NodeHandle handle,
            HistoryReadValueId nodeToRead,
            NodeId aggregateId)
        {
            bool applyIndexRangeOrEncoding = (nodeToRead.ParsedIndexRange != NumericRange.Empty || !QualifiedName.IsNull(nodeToRead.DataEncoding));
            bool timeFlowsBackward = (details.EndTime < details.StartTime);

            ArchiveItemState item = handle.Node as ArchiveItemState;

            if(item == null)
            {
                throw new ServiceResultException(StatusCodes.BadNotSupported);
            }

            item.ReloadFromSource(context);

            LinkedList<DataValue> values = new LinkedList<DataValue>();

            // Read history.
            DataView view;
            if(aggregateId == ObjectIds.AggregateFunction_AnnotationCount)
                view = item.ReadHistory(details.StartTime, details.EndTime, false, BrowseNames.Annotations);
            else
                view = item.ReadHistory(details.StartTime, details.EndTime, false);

            // Choose the aggregate configuration
            AggregateConfiguration configuration = (AggregateConfiguration) details.AggregateConfiguration.MemberwiseClone();
            ReviseAggregateConfiguration(context, item, configuration);
          

            // Create the aggregate calculator
            IAggregateCalculator calculator = Server.AggregateManager.CreateCalculator(
                aggregateId,
                details.StartTime,
                details.EndTime,
                details.ProcessingInterval,
                !configuration.UseSlopedExtrapolation,
                configuration);

            int ii = (timeFlowsBackward) ? view.Count - 1 : 0;

            while(ii >= 0 && ii < view.Count)
            {
                try
                {
                    DataValue value = (DataValue)  ((DataValue) view[ii].Row[2]).MemberwiseClone();
                    calculator.QueueRawValue(value);

                    // Queue any processed values
                    QueueProcessedValues(
                        context,
                        calculator,
                        nodeToRead.ParsedIndexRange,
                        nodeToRead.DataEncoding,
                        applyIndexRangeOrEncoding,
                        false,
                        values);
                }
                finally
                {
                    if(timeFlowsBackward)
                    {
                        ii--;
                    }
                    else
                    {
                        ii++;
                    }
                }
            }

            // Queue any processed values beyond the end of the data
            QueueProcessedValues(
                context,
                calculator,
                nodeToRead.ParsedIndexRange,
                nodeToRead.DataEncoding,
                applyIndexRangeOrEncoding,
                true,
                values);

            HistoryReadRequest request = new HistoryReadRequest();
            request.Values = values;
            request.NumValuesPerNode = 0;
            request.Filter = null;
            return request;
        }

        /// <summary>
        /// Creates a new history request
        /// </summary>
        private HistoryReadRequest CreateHistoryReadRequest(
            ServerSystemContext context,
            ReadAtTimeDetails details,
            NodeHandle handle,
            HistoryReadValueId nodeToRead)
        {
            bool applyIndexRangeOrEncoding = (nodeToRead.ParsedIndexRange != NumericRange.Empty || !QualifiedName.IsNull(nodeToRead.DataEncoding));

            ArchiveItemState item = handle.Node as ArchiveItemState;

            if(item == null)
            {
                throw new ServiceResultException(StatusCodes.BadNotSupported);
            }

            item.ReloadFromSource(context);

            // Find the start and end times
            DateTime startTime = DateTime.MaxValue;
            DateTime endTime = DateTime.MinValue;

            for(int ii = 0; ii < details.ReqTimes.Count; ii++)
            {
                if(startTime > details.ReqTimes[ii])
                {
                    startTime = details.ReqTimes[ii];
                }

                if(endTime < details.ReqTimes[ii])
                {
                    endTime = details.ReqTimes[ii];
                }
            }

            DataView view = item.ReadHistory(startTime, endTime, false);

            LinkedList<DataValue> values = new LinkedList<DataValue>();

            for(int ii = 0; ii < details.ReqTimes.Count; ii++)
            {
                bool dataBeforeIgnored = false;
                bool dataAfterIgnored = false;

                // Find the value at the time
                int index = item.FindValueAtOrBefore(view, details.ReqTimes[ii], !details.UseSimpleBounds, out dataBeforeIgnored);

                if(index < 0)
                {
                    values.AddLast(new DataValue(Variant.Null, StatusCodes.BadNoData, details.ReqTimes[ii], details.ReqTimes[ii]));
                    continue;
                }

                // Nothing more to do if a raw value exists
                if((DateTime) view[index].Row[0] == details.ReqTimes[ii])
                {
                    DataValue dataValue = (DataValue)view[index].Row[2];
                    if (StatusCode.IsBad(dataValue.StatusCode))
                    {
                        dataValue.StatusCode = dataValue.StatusCode.SetCodeBits(StatusCodes.BadNoData); 
                    }
                    values.AddLast(dataValue);
                    continue;
                }

                DataValue before = (DataValue) view[index].Row[2];
                // Initialize data value with dummy value
                DataValue value = new DataValue();

                // Find the value after the time
                int afterIndex = item.FindValueAfter(view, index, !details.UseSimpleBounds, out dataAfterIgnored);

                if(afterIndex < 0)
                {
                    bool useStepped = true;
                    if (item.HistoricalDataConfiguration.AggregateConfiguration.UseSlopedExtrapolation.Value)
                    { 
                        // Take the previous value of the before value
                        int secondBeforeIndex = item.FindValueBefore(view, index, !details.UseSimpleBounds);
                        if (secondBeforeIndex >= 0)
                        {
                            useStepped = false;
                            value = AggregateCalculator.SlopedInterpolate(details.ReqTimes[ii],(DataValue)view[secondBeforeIndex].Row[2], before);
                        }
                    }

                    if (useStepped)
                    {
                        // Use stepped interpolation if no end bound exists
                        value = AggregateCalculator.SteppedInterpolate(details.ReqTimes[ii], before);
                    }

                    if(StatusCode.IsNotBad(value.StatusCode) && dataBeforeIgnored)
                    {
                        value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
                    }

                    // Take care of the requirement
                    // If the timestamp is after the end of the data then the bounding value is treated as extrapolated and the StatusCode is Uncertain_DataSubNormal
                    if (details.ReqTimes[ii] > (DateTime)view[view.Count - 1].Row[0])
                    {
                        value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
                    }
                    values.AddLast(value);
                    continue;
                }

                // Use stepped or slopped interpolation depending on the value
                if(item.ArchiveItem.Stepped)
                {
                    value = AggregateCalculator.SteppedInterpolate(details.ReqTimes[ii], before);

                    if(StatusCode.IsNotGood(value.StatusCode) || dataBeforeIgnored)
                    {
                        value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
                    }
                }
                else
                {
                    value = AggregateCalculator.SlopedInterpolate(details.ReqTimes[ii], before, (DataValue) view[afterIndex].Row[2]);

                    if(StatusCode.IsNotBad(value.StatusCode) && (dataBeforeIgnored || dataAfterIgnored))
                    {
                        value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
                    }
                }

                values.AddLast(value);
            }

            HistoryReadRequest request = new HistoryReadRequest();
            request.Values = values;
            request.NumValuesPerNode = 0;
            request.Filter = null;
            return request;
        }

        /// <summary>
        /// Extracts and queues any processed values
        /// </summary>
        private void QueueProcessedValues(
            ServerSystemContext context,
            IAggregateCalculator calculator,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            bool applyIndexRangeOrEncoding,
            bool returnPartial,
            LinkedList<DataValue> values)
        {
            DataValue proccessedValue = calculator.GetProcessedValue(returnPartial);

            while(proccessedValue != null)// && StatusCode.IsGood(proccessedValue.StatusCode))
            {
                // Apply any index range or encoding
                if(applyIndexRangeOrEncoding)
                {
                    object rawValue = proccessedValue.Value;
                    ServiceResult result = BaseVariableState.ApplyIndexRangeAndDataEncoding(context, indexRange, dataEncoding, ref rawValue);

                    if(ServiceResult.IsBad(result))
                    {
                        proccessedValue.Value = rawValue;
                    }
                    else
                    {
                        proccessedValue.Value = null;
                        proccessedValue.StatusCode = result.StatusCode;
                    }
                }

                // Queue the result
                values.AddLast(proccessedValue);
                proccessedValue = calculator.GetProcessedValue(returnPartial);
            }
        }

        /// <summary>
        /// Creates a new history request
        /// </summary>
        private DataValue RowToDataValue(
            ServerSystemContext context,
            HistoryReadValueId nodeToRead,
            DataRowView row,
            bool applyIndexRangeOrEncoding)
        {
            DataValue value = (DataValue) row[2];

            // Apply any index range or encoding
            if(applyIndexRangeOrEncoding)
            {
                object rawValue = value.Value;
                ServiceResult result = BaseVariableState.ApplyIndexRangeAndDataEncoding(context, nodeToRead.ParsedIndexRange, nodeToRead.DataEncoding, ref rawValue);

                if(ServiceResult.IsBad(result))
                {
                    value.Value = rawValue;
                }
                else
                {
                    value.Value = null;
                    value.StatusCode = result.StatusCode;
                }
            }

            return value;
        }

        /// <summary>
        /// Releases the history continuation point
        /// </summary>
        protected override void HistoryReleaseContinuationPoints(
            ServerSystemContext context,
            IList<HistoryReadValueId> nodesToRead,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            for(int ii = 0; ii < nodesToProcess.Count; ii++)
            {
                NodeHandle handle = nodesToProcess[ii];
                HistoryReadValueId nodeToRead = nodesToRead[handle.Index];

                // Find the continuation point
                HistoryReadRequest request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

                if(request == null)
                {
                    errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
                    continue;
                }

                // All done
                errors[handle.Index] = StatusCodes.Good;
            }
        }

        /// <summary>
        /// Loads a history continuation point
        /// </summary>
        private HistoryReadRequest LoadContinuationPoint(ServerSystemContext context, byte[] continuationPoint)
        {
            Session session = context.OperationContext.Session;

            if(session == null)
            {
                return null;
            }

            HistoryReadRequest request = session.RestoreHistoryContinuationPoint(continuationPoint) as HistoryReadRequest;

            if(request == null)
            {
                return null;
            }

            return request;
        }

        /// <summary>
        /// Saves a history continuation point
        /// </summary>
        private byte[] SaveContinuationPoint(ServerSystemContext context, HistoryReadRequest request)
        {
            Session session = context.OperationContext.Session;

            if(session == null)
            {
                return null;
            }

            Guid id = Guid.NewGuid();
            session.SaveHistoryContinuationPoint(id, request);
            request.ContinuationPoint = id.ToByteArray();
            return request.ContinuationPoint;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Revises the aggregate configuration
        /// </summary>
        /// <param name="context"></param>
        /// <param name="item"></param>
        /// <param name="configurationToUse"></param>
        private void ReviseAggregateConfiguration(ServerSystemContext context, ArchiveItemState item, AggregateConfiguration configurationToUse)
        {
            // Set configuration from defaults
            if (configurationToUse.UseServerCapabilitiesDefaults)
            {
                AggregateConfiguration configuration = item.ArchiveItem.AggregateConfiguration;

                if (configuration == null || configuration.UseServerCapabilitiesDefaults)
                {
                    configuration = Server.AggregateManager.GetDefaultConfiguration(null);
                }
            }

            // Override configuration when it does not make sense for the item
            configurationToUse.UseServerCapabilitiesDefaults = false;
        }
        #endregion

        #region HistoryReadRequest Private Class
        /// <summary>
        /// Stores a read history request
        /// </summary>
        private class HistoryReadRequest
        {
            public byte[] ContinuationPoint;
            public LinkedList<DataValue> Values;
            public LinkedList<ModificationInfo> ModificationInfos;
            public uint NumValuesPerNode;
            public AggregateFilter Filter;
        }
        #endregion
    }
}
