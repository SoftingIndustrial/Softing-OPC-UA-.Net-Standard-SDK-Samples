using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Opc.Ua;
using Opc.Ua.Server;

namespace TestServer.TestModule
{
	partial class TestModuleNodeManager
	{
		/// <summary>
		/// Revises the aggregate configuration.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="item"></param>
		/// <param name="configurationToUse"></param>
		private void ReviseAggregateConfiguration(
			ServerSystemContext context,
			ArchiveItemState item,
			AggregateConfiguration configurationToUse)
		{
			// set configuration from defaults.
			if (configurationToUse.UseServerCapabilitiesDefaults)
			{
				AggregateConfiguration configuration = item.ArchiveItem.AggregateConfiguration;

				if (configuration == null || configuration.UseServerCapabilitiesDefaults)
				{
					configuration = Server.AggregateManager.GetDefaultConfiguration(null);
				}

				configurationToUse.UseSlopedExtrapolation = configuration.UseSlopedExtrapolation;
				configurationToUse.TreatUncertainAsBad = configuration.TreatUncertainAsBad;
				configurationToUse.PercentDataBad = configuration.PercentDataBad;
				configurationToUse.PercentDataGood = configuration.PercentDataGood;
			}

			// override configuration when it does not make sense for the item.
			configurationToUse.UseServerCapabilitiesDefaults = false;

			if (item.ArchiveItem.Stepped)
			{
				configurationToUse.UseSlopedExtrapolation = false;
			}
		}

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
				// parent must have a string identifier.
				string parentId = instance.Parent.NodeId.Identifier as string;

				if (parentId == null)
				{
					return null;
				}

				StringBuilder buffer = new StringBuilder();
				buffer.Append(parentId);

				// check if the parent is another component.
				bool isAntoherComponent = parentId.IndexOf('?') == -1;
				buffer.Append(isAntoherComponent ? '?' : '/');

				buffer.Append(node.SymbolicName);

				return new NodeId(buffer.ToString(), instance.Parent.NodeId.NamespaceIndex);
			}

			return node.NodeId;
		}
		#endregion

		#region Historian Functions
		/// <summary>
		/// Reads the raw data for an item.
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
					// validate node.
					NodeState source = ValidateNode(context, handle, cache);

					if (source == null)
					{
						continue;
					}

					if (context.UserIdentity.DisplayName == "usr")
					{
						errors[handle.Index] = StatusCodes.BadUserAccessDenied;
						continue;
					}

					// load an exising request.
					if (nodeToRead.ContinuationPoint != null)
					{
						request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

						if (request == null)
						{
							errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
							continue;
						}
					}
					else	// create a new request.
					{
						request = CreateHistoryReadRequest(
							context,
							details,
							handle,
							nodeToRead);
					}

					// process values until the max is reached.
					HistoryData data = (details.IsReadModified) ? new HistoryModifiedData() : new HistoryData();
					HistoryModifiedData modifiedData = data as HistoryModifiedData;

					while(request.NumValuesPerNode == 0 || data.DataValues.Count < request.NumValuesPerNode)
					{
						if (request.Values.Count == 0)
						{
							break;
						}

						DataValue value = request.Values.First.Value;
						request.Values.RemoveFirst();
						data.DataValues.Add(value);

						if (modifiedData != null)
						{
							ModificationInfo modificationInfo = null;

							if (request.ModificationInfos != null && request.ModificationInfos.Count > 0)
							{
								modificationInfo = request.ModificationInfos.First.Value;
								request.ModificationInfos.RemoveFirst();
							}

							modifiedData.ModificationInfos.Add(modificationInfo);
						}
					}

					errors[handle.Index] = ServiceResult.Good;

					// check if a continuation point is required.
					if (request.Values.Count > 0)
					{
						// only set if both end time and start time are specified.
						if (details.StartTime != DateTime.MinValue && details.EndTime != DateTime.MinValue)
						{
							result.ContinuationPoint = SaveContinuationPoint(context, request);
						}
					}

					// check if no data returned.
					if (data.DataValues.Count == 0)
					{
						errors[handle.Index] = StatusCodes.GoodNoData;
					}

					// return the data.
					result.HistoryData = new ExtensionObject(data);
				}
				catch(Exception e)
				{
					errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
				}
			}
		}

		/// <summary>
		/// Reads the processed data for an item.
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
			for(int ii = 0; ii < nodesToRead.Count; ii++)
			{
				NodeHandle handle = nodesToProcess[ii];
				HistoryReadValueId nodeToRead = nodesToRead[handle.Index];
				HistoryReadResult result = results[handle.Index];
				HistoryReadRequest request = null;

				try
				{
					// validate node.
					NodeState source = ValidateNode(context, handle, cache);

					if (source == null)
					{
						continue;
					}

					if (context.UserIdentity.DisplayName == "usr")
					{
						errors[handle.Index] = StatusCodes.BadUserAccessDenied;
						continue;
					}

					// load an exising request.
					if (nodeToRead.ContinuationPoint != null)
					{
						request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

						if (request == null)
						{
							errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
							continue;
						}
					}
					else	// create a new request.
					{
						// validate aggregate type.
						if (details.AggregateType.Count <= ii || !Server.AggregateManager.IsSupported(details.AggregateType[ii]))
						{
							errors[handle.Index] = StatusCodes.BadAggregateNotSupported;
							continue;
						}

						request = CreateHistoryReadRequest(
							context,
							details,
							handle,
							nodeToRead,
							details.AggregateType[ii]);
					}

					// process values until the max is reached.
					HistoryData data = new HistoryData();

					while(request.NumValuesPerNode == 0 || data.DataValues.Count < request.NumValuesPerNode)
					{
						if (request.Values.Count == 0)
						{
							break;
						}

						DataValue value = request.Values.First.Value;
						request.Values.RemoveFirst();
						data.DataValues.Add(value);
					}

					errors[handle.Index] = ServiceResult.Good;

					// check if a continuation point is required.
					if (request.Values.Count > 0)
					{
						result.ContinuationPoint = SaveContinuationPoint(context, request);
					}

					// check if no data returned.
					if (data.DataValues.Count == 0)
					{
						errors[handle.Index] = StatusCodes.GoodNoData;
					}

					// return the data.
					result.HistoryData = new ExtensionObject(data);
				}
				catch(Exception e)
				{
					errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
				}
			}
		}

		/// <summary>
		/// Reads the data at the specified time for an item.
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
					// validate node.
					NodeState source = ValidateNode(context, handle, cache);

					if (source == null)
					{
						continue;
					}

					if (context.UserIdentity.DisplayName == "usr")
					{
						errors[handle.Index] = StatusCodes.BadUserAccessDenied;
						continue;
					}

					// load an exising request.
					if (nodeToRead.ContinuationPoint != null)
					{
						request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

						if (request == null)
						{
							errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
							continue;
						}
					}
					else	// create a new request.
					{
						request = CreateHistoryReadRequest(
							context,
							details,
							handle,
							nodeToRead);
					}

					// process values until the max is reached.
					HistoryData data = new HistoryData();

					while(request.NumValuesPerNode == 0 || data.DataValues.Count < request.NumValuesPerNode)
					{
						if (request.Values.Count == 0)
						{
							break;
						}

						DataValue value = request.Values.First.Value;
						request.Values.RemoveFirst();
						data.DataValues.Add(value);
					}

					errors[handle.Index] = ServiceResult.Good;

					// check if a continuation point is requred.
					if (request.Values.Count > 0)
					{
						result.ContinuationPoint = SaveContinuationPoint(context, request);
					}

					// check if no data returned.
					if (data.DataValues.Count == 0)
					{
						errors[handle.Index] = StatusCodes.GoodNoData;
					}

					// return the data.
					result.HistoryData = new ExtensionObject(data);
				}
				catch(Exception e)
				{
					errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
				}
			}
		}

		/// <summary>
		/// Updates the data history for one or more nodes.
		/// </summary>
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

				try
				{
					// remove not supported.
					if (nodeToUpdate.PerformInsertReplace == PerformUpdateType.Remove)
					{
						continue;
					}

					// validate node.
					NodeState source = ValidateNode(context, handle, cache);

					if (source == null)
					{
						continue;
					}

					if (context.UserIdentity.DisplayName == "usr")
					{
						errors[handle.Index] = StatusCodes.BadUserAccessDenied;
						continue;
					}

					// load the archive.
					ArchiveItemState item = handle.Node as ArchiveItemState;

					if (item == null)
					{
						continue;
					}

					item.ReloadFromSource(context);

					// process each item.
					for(int jj = 0; jj < nodeToUpdate.UpdateValues.Count; jj++)
					{
						StatusCode error = item.UpdateHistory(context, nodeToUpdate.UpdateValues[jj], nodeToUpdate.PerformInsertReplace);
						result.OperationResults.Add(error);
					}

					errors[handle.Index] = ServiceResult.Good;
				}
				catch(Exception e)
				{
					errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
				}
			}
		}

		/// <summary>
		/// Updates the data history for one or more nodes.
		/// </summary>
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

				try
				{
					// validate node.
					NodeState source = ValidateNode(context, handle, cache);

					if (source == null)
					{
						continue;
					}

					if (context.UserIdentity.DisplayName == "usr")
					{
						errors[handle.Index] = StatusCodes.BadUserAccessDenied;
						continue;
					}

					// only support annotations.
					if (handle.Node.BrowseName != BrowseNames.Annotations)
					{
						continue;
					}

					// load the archive.
					ArchiveItemState item = Reload(context, handle);

					if (item == null)
					{
						continue;
					}

					// process each item.
					for(int jj = 0; jj < nodeToUpdate.UpdateValues.Count; jj++)
					{
						Annotation annotation = ExtensionObject.ToEncodeable(nodeToUpdate.UpdateValues[jj].Value as ExtensionObject) as Annotation;

						if (annotation == null)
						{
							result.OperationResults.Add(StatusCodes.BadTypeMismatch);
							continue;
						}

						StatusCode error = item.UpdateAnnotations(
							context,
							annotation,
							nodeToUpdate.UpdateValues[jj],
							nodeToUpdate.PerformInsertReplace);

						result.OperationResults.Add(error);
					}

					errors[handle.Index] = ServiceResult.Good;
				}
				catch(Exception e)
				{
					errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Unexpected error processing request.");
				}
			}
		}

		/// <summary>
		/// Deletes the data history for one or more nodes.
		/// </summary>
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

				try
				{
					// validate node.
					NodeState source = ValidateNode(context, handle, cache);

					if (source == null)
					{
						continue;
					}

					if (context.UserIdentity.DisplayName == "usr")
					{
						errors[handle.Index] = StatusCodes.BadUserAccessDenied;
						continue;
					}

					// load the archive.
					ArchiveItemState item = handle.Node as ArchiveItemState;

					if (item == null)
					{
						continue;
					}

					item.ReloadFromSource(context);

					// delete the history.
					errors[handle.Index] = item.DeleteHistory(context, nodeToUpdate.StartTime, nodeToUpdate.EndTime, nodeToUpdate.IsDeleteModified);
				}
				catch(Exception e)
				{
					errors[handle.Index] = ServiceResult.Create(e, StatusCodes.BadUnexpectedError, "Error deleting data from archive.");
				}
			}
		}

		/// <summary>
		/// Deletes the data history for one or more nodes.
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

				try
				{
					// validate node.
					NodeState source = ValidateNode(context, handle, cache);

					if (source == null)
					{
						continue;
					}

					if (context.UserIdentity.DisplayName == "usr")
					{
						errors[handle.Index] = StatusCodes.BadUserAccessDenied;
						continue;
					}

					// load the archive.
					ArchiveItemState item = handle.Node as ArchiveItemState;

					if (item == null)
					{
						continue;
					}

					item.ReloadFromSource(context);

					// process each item.
					for(int jj = 0; jj < nodeToUpdate.ReqTimes.Count; jj++)
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
			}
		}

		#region History Helpers
		/// <summary>
		/// Loads the archive item state from the underlying source.
		/// </summary>
		private ArchiveItemState Reload(ServerSystemContext context, NodeHandle handle)
		{
			ArchiveItemState item = handle.Node as ArchiveItemState;

			if (item == null)
			{
				BaseInstanceState property = handle.Node as BaseInstanceState;

				if (property != null)
				{
					item = property.Parent as ArchiveItemState;
				}
			}

			if (item != null)
			{
				item.ReloadFromSource(context);
			}

			return item;
		}

		/// <summary>
		/// Creates a new history request.
		/// </summary>
		private HistoryReadRequest CreateHistoryReadRequest(
			ServerSystemContext context,
			ReadRawModifiedDetails details,
			NodeHandle handle,
			HistoryReadValueId nodeToRead)
		{
			bool sizeLimited = (details.StartTime == DateTime.MinValue || details.EndTime == DateTime.MinValue);
			bool applyIndexRangeOrEncoding = (nodeToRead.ParsedIndexRange != NumericRange.Empty || !QualifiedName.IsNull(nodeToRead.DataEncoding));
			bool returnBounds = !details.IsReadModified && details.ReturnBounds;
			bool timeFlowsBackward = (details.StartTime == DateTime.MinValue) || (details.EndTime != DateTime.MinValue && details.EndTime < details.StartTime);

			// find the archive item.
			ArchiveItemState item = Reload(context, handle);

			if (item == null)
			{
				throw new ServiceResultException(StatusCodes.BadNotSupported);
			}

			LinkedList<DataValue> values = new LinkedList<DataValue>();
			LinkedList<ModificationInfo> modificationInfos = null;

			if (details.IsReadModified)
			{
				modificationInfos = new LinkedList<ModificationInfo>();
			}

			// read history. 
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

					// check if looking for start of data.
					if (values.Count == 0)
					{
						if (timeFlowsBackward)
						{
							if (details.StartTime != DateTime.MinValue && timestamp >= details.StartTime) 
							{
								startBound = ii;

								if (timestamp > details.StartTime)
								{
									continue;
								}
							}
							else if (details.StartTime == DateTime.MinValue && timestamp >= details.EndTime)
							{
								startBound = ii;

								if (timestamp > details.EndTime)
								{
									continue;
								}
							}
						}
						else
						{
							if (timestamp <= details.StartTime)
							{
								startBound = ii;

								if (timestamp < details.StartTime)
								{
									continue;
								}
							}
						}
					}

					// check if absolute max values specified.
					if (sizeLimited)
					{
						if (details.NumValuesPerNode > 0 && details.NumValuesPerNode < values.Count)
						{
							break;
						}
					}

					// check for end bound.
					if (timeFlowsBackward)
					{
						if (timestamp <= details.EndTime && details.StartTime != DateTime.MinValue)
						{
							endBound = ii;
							break;
						}
					}
					else
					{
						if (timestamp >= details.EndTime && details.EndTime != DateTime.MinValue)
						{
							if (startBound != ii)
							{
								endBound = ii;
								break;
							}
						}
					}

					// check if the start bound needs to be returned.
					if (returnBounds && values.Count == 0 && startBound != ii)
					{
						// add start bound.
						if (startBound == -1)
						{
							DateTime startBoundTime;
							if (details.StartTime != DateTime.MinValue)
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

						// check if absolute max values specified.
						if (sizeLimited)
						{
							if (details.NumValuesPerNode > 0 && details.NumValuesPerNode < values.Count)
							{
								break;
							}
						}
					}

					// add value.
					values.AddLast(RowToDataValue(context, nodeToRead, view[ii], applyIndexRangeOrEncoding));
					lastTimeReturned = timestamp;

					if (modificationInfos != null)
					{
						modificationInfos.AddLast((ModificationInfo) view[ii].Row[6]);
					}
				}
				finally
				{
					if (timeFlowsBackward)
					{
						ii--;
					}
					else
					{
						ii++;
					}
				}
			}

			// add late bound.
			while(returnBounds)
			{
				// add start bound.
				if (values.Count == 0)
				{
					if (startBound == -1)
					{
						DateTime startBoundTime;
						if (details.StartTime != DateTime.MinValue)
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

				// check if absolute max values specified.
				if (sizeLimited)
				{
					if (details.NumValuesPerNode > 0 && details.NumValuesPerNode < values.Count)
					{
						break;
					}
				}

				// add end bound.
				if (endBound == -1)
				{
					DateTime endBoundTime = details.EndTime;
					if (details.StartTime != DateTime.MinValue && details.EndTime != DateTime.MinValue)
					{
						endBoundTime = details.EndTime;
					}
					else
					{
						if (lastTimeReturned == DateTime.MinValue)
							endBoundTime = DateTime.MinValue;
						else
							endBoundTime = lastTimeReturned.AddSeconds(timeFlowsBackward ? -1.0d : 1.0d);
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
		/// Creates a new history request.
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

			if (item == null)
			{
				throw new ServiceResultException(StatusCodes.BadNotSupported);
			}

			item.ReloadFromSource(context);

			LinkedList<DataValue> values = new LinkedList<DataValue>();

			// read history. 
			DataView view;
			if (aggregateId == ObjectIds.AggregateFunction_AnnotationCount)
				view = item.ReadHistory(details.StartTime, details.EndTime, false, BrowseNames.Annotations);
			else
				view = item.ReadHistory(details.StartTime, details.EndTime, false);

			// choose the aggregate configuration.
			AggregateConfiguration configuration = (AggregateConfiguration) details.AggregateConfiguration.MemberwiseClone();
			ReviseAggregateConfiguration(context, item, configuration);

			// create the aggregate calculator.
			IAggregateCalculator calculator = Server.AggregateManager.CreateCalculator(
				aggregateId,
				details.StartTime,
				details.EndTime,
				details.ProcessingInterval,
				item.ArchiveItem.Stepped,
				configuration);

			int ii = (timeFlowsBackward) ? view.Count - 1 : 0;

			while(ii >= 0 && ii < view.Count)
			{
				try
				{
					DataValue value = (DataValue)  ((DataValue) view[ii].Row[2]).MemberwiseClone();
					calculator.QueueRawValue(value);

					// queue any processed values.
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
					if (timeFlowsBackward)
					{
						ii--;
					}
					else
					{
						ii++;
					}
				}
			}

			// queue any processed values beyond the end of the data.
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
		/// Creates a new history request.
		/// </summary>
		private HistoryReadRequest CreateHistoryReadRequest(
			ServerSystemContext context,
			ReadAtTimeDetails details,
			NodeHandle handle,
			HistoryReadValueId nodeToRead)
		{
			bool applyIndexRangeOrEncoding = (nodeToRead.ParsedIndexRange != NumericRange.Empty || !QualifiedName.IsNull(nodeToRead.DataEncoding));

			ArchiveItemState item = handle.Node as ArchiveItemState;

			if (item == null)
			{
				throw new ServiceResultException(StatusCodes.BadNotSupported);
			}

			item.ReloadFromSource(context);

			// find the start and end times.
			DateTime startTime = DateTime.MaxValue;
			DateTime endTime = DateTime.MinValue;

			for(int ii = 0; ii < details.ReqTimes.Count; ii++)
			{
				if (startTime > details.ReqTimes[ii])
				{
					startTime = details.ReqTimes[ii];
				}

				if (endTime < details.ReqTimes[ii])
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

				// find the value at the time.
				int index = item.FindValueAtOrBefore(view, details.ReqTimes[ii], !details.UseSimpleBounds, out dataBeforeIgnored);

				if (index < 0)
				{
                    values.AddLast(new DataValue(Variant.Null, StatusCodes.BadNoData, details.ReqTimes[ii], details.ReqTimes[ii]));
					continue;
				}

				// nothing more to do if a raw value exists.
				if ((DateTime) view[index].Row[0] == details.ReqTimes[ii])
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
				//init data value with dummy value
                DataValue value = new DataValue();

				// find the value after the time.
				int afterIndex = item.FindValueAfter(view, index, !details.UseSimpleBounds, out dataAfterIgnored);

				if (afterIndex < 0)
				{
                    bool useStepped = true;
                    if (item.HistoricalDataConfiguration.AggregateConfiguration.UseSlopedExtrapolation.Value)
                    { 
                        //take the previous value of the before value
                        int secondBeforeIndex = item.FindValueBefore(view, index, !details.UseSimpleBounds);
                        if (secondBeforeIndex >= 0)
                        {
                            useStepped = false;
                            value = AggregateCalculator.SlopedInterpolate(details.ReqTimes[ii],(DataValue)view[secondBeforeIndex].Row[2], before);
                        }
                    }

                    if (useStepped)
                    {
                        // use stepped interpolation if no end bound exists.
                        value = AggregateCalculator.SteppedInterpolate(details.ReqTimes[ii], before);
                    }

					if (StatusCode.IsNotBad(value.StatusCode) && dataBeforeIgnored)
					{
						value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
					}

                    //take care of the requirement
                    //if the timestamp is after the end of the data then the bounding value is treated as extrapolated and the StatusCode is Uncertain_DataSubNormal
                    if (details.ReqTimes[ii] > (DateTime)view[view.Count - 1].Row[0])
                    {
                        value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
                    }
					values.AddLast(value);
					continue;
				}

				// use stepped or slopped interpolation depending on the value.
				if (item.ArchiveItem.Stepped)
				{
					value = AggregateCalculator.SteppedInterpolate(details.ReqTimes[ii], before);

					if (StatusCode.IsNotGood(value.StatusCode) || dataBeforeIgnored)
					{
						value.StatusCode = value.StatusCode.SetCodeBits(StatusCodes.UncertainDataSubNormal);
					}
				}
				else
				{
					value = AggregateCalculator.SlopedInterpolate(details.ReqTimes[ii], before, (DataValue) view[afterIndex].Row[2]);

					if (StatusCode.IsNotBad(value.StatusCode) && (dataBeforeIgnored || dataAfterIgnored))
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
		/// Extracts and queues any processed values.
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

			while(proccessedValue != null)
			{
				// apply any index range or encoding.
				if (applyIndexRangeOrEncoding)
				{
					object rawValue = proccessedValue.Value;
					ServiceResult result = BaseVariableState.ApplyIndexRangeAndDataEncoding(context, indexRange, dataEncoding, ref rawValue);

					if (ServiceResult.IsBad(result))
					{
						proccessedValue.Value = rawValue;
					}
					else
					{
						proccessedValue.Value = null;
						proccessedValue.StatusCode = result.StatusCode;
					}
				}

				// queue the result.
				values.AddLast(proccessedValue);
				proccessedValue = calculator.GetProcessedValue(returnPartial);
			}
		}

		/// <summary>
		/// Creates a new history request.
		/// </summary>
		private DataValue RowToDataValue(
			ServerSystemContext context,
			HistoryReadValueId nodeToRead,
			DataRowView row,
			bool applyIndexRangeOrEncoding)
		{
			DataValue value = (DataValue) row[2];

			// apply any index range or encoding.
			if (applyIndexRangeOrEncoding)
			{
				object rawValue = value.Value;
				ServiceResult result = BaseVariableState.ApplyIndexRangeAndDataEncoding(context, nodeToRead.ParsedIndexRange, nodeToRead.DataEncoding, ref rawValue);

				if (ServiceResult.IsBad(result))
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
		/// Stores a read history request.
		/// </summary>
		private class HistoryReadRequest
		{
			public byte[] ContinuationPoint;
			public LinkedList<DataValue> Values;
			public LinkedList<ModificationInfo> ModificationInfos;
			public uint NumValuesPerNode;
			public AggregateFilter Filter;
		}

		/// <summary>
		/// Releases the history continuation point.
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

				// find the continuation point.
				HistoryReadRequest request = LoadContinuationPoint(context, nodeToRead.ContinuationPoint);

				if (request == null)
				{
					errors[handle.Index] = StatusCodes.BadContinuationPointInvalid;
					continue;
				}

				// all done.
				errors[handle.Index] = StatusCodes.Good;
			}
		}

		/// <summary>
		/// Loads a history continuation point.
		/// </summary>
		private HistoryReadRequest LoadContinuationPoint(
			ServerSystemContext context,
			byte[] continuationPoint)
		{
			Session session = context.OperationContext.Session;

			if (session == null)
			{
				return null;
			}

			HistoryReadRequest request = session.RestoreHistoryContinuationPoint(continuationPoint) as HistoryReadRequest;

			if (request == null)
			{
				return null;
			}

			return request;
		}

		/// <summary>
		/// Saves a history continuation point.
		/// </summary>
		private byte[] SaveContinuationPoint(
			ServerSystemContext context,
			HistoryReadRequest request)
		{
			Session session = context.OperationContext.Session;

			if (session == null)
			{
				return null;
			}

			Guid id = Guid.NewGuid();
			session.SaveHistoryContinuationPoint(id, request);
			request.ContinuationPoint = id.ToByteArray();
			return request.ContinuationPoint;
		}
		#endregion
		#endregion

	}
}