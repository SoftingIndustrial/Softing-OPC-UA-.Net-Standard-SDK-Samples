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

using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Data;

namespace TestServer.HistoricalEvents
{
	/// <summary>
	/// A node manager for a server that exposes several variables.
	/// </summary>
	public class HistoricalEventsNodeManager : CustomNodeManager2
	{
		#region Constructors

		/// <summary>
		/// Initializes the node manager.
		/// </summary>
		public HistoricalEventsNodeManager(IServerInternal server, ApplicationConfiguration configuration, params string[] namespaceUris)
			: base(server, configuration, namespaceUris)
		{
			// look up the local timezone.
			TimeZoneInfo timeZone = TimeZoneInfo.Local;
			m_timeZone = new TimeZoneDataType();
			m_timeZone.Offset = (short) timeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
			m_timeZone.DaylightSavingInOffset = timeZone.IsDaylightSavingTime(DateTime.Now);
		}
		#endregion

		#region Historian Functions
		/// <summary>
		/// Reads history events.
		/// </summary>
		protected override void HistoryReadEvents(
			ServerSystemContext context,
			ReadEventDetails details,
			TimestampsToReturn timestampsToReturn,
			IList<HistoryReadValueId> nodesToRead,
			IList<HistoryReadResult> results,
			IList<ServiceResult> errors,
			List<NodeHandle> nodesToProcess,
			IDictionary<NodeId, NodeState> cache)
		{
			for(int ii = 0; ii < nodesToProcess.Count; ii++)
			{
				NodeHandle handle = nodesToProcess[ii];
				HistoryReadValueId nodeToRead = nodesToRead[handle.Index];
				HistoryReadResult result = results[handle.Index];

				HistoryReadRequest request = null;

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

				// create a new request.
				else
				{
					request = CreateHistoryReadRequest(
						context,
						details,
						handle,
						nodeToRead);
				}

				// process events until the max is reached.
				HistoryEvent events = new HistoryEvent();

				while(request.NumValuesPerNode == 0 || events.Events.Count < request.NumValuesPerNode)
				{
					if (request.Events.Count == 0)
					{
						break;
					}

					BaseEventState e = null;

					if (request.TimeFlowsBackward)
					{
						e = request.Events.Last.Value;
						request.Events.RemoveLast();
					}
					else
					{
						e = request.Events.First.Value;
						request.Events.RemoveFirst();
					}

					events.Events.Add(GetEventFields(request, e));
				}

				errors[handle.Index] = ServiceResult.Good;

				// check if a continuation point is requred.
				if (request.Events.Count > 0)
				{
					// only set if both end time and start time are specified.
					if (details.StartTime != DateTime.MinValue && details.EndTime != DateTime.MinValue)
					{
						result.ContinuationPoint = SaveContinuationPoint(context, request);
					}
				}

				// check if no data returned.
				if (events.Events.Count == 0)
				{
					errors[handle.Index] = StatusCodes.GoodNoData;
				}

				// return the data.
				result.HistoryData = new ExtensionObject(events);
			}
		}

		/// <summary>
		/// Updates or inserts events.
		/// </summary>
		protected override void HistoryUpdateEvents(
			ServerSystemContext context,
			IList<UpdateEventDetails> nodesToUpdate,
			IList<HistoryUpdateResult> results,
			IList<ServiceResult> errors,
			List<NodeHandle> nodesToProcess,
			IDictionary<NodeId, NodeState> cache)
		{
			for(int ii = 0; ii < nodesToProcess.Count; ii++)
			{
				NodeHandle handle = nodesToProcess[ii];
				UpdateEventDetails nodeToUpdate = nodesToUpdate[handle.Index];
				HistoryUpdateResult result = results[handle.Index];

				// validate the event filter.
				FilterContext filterContext = new FilterContext(context.NamespaceUris, context.TypeTable, context);
				EventFilter.Result filterResult = nodeToUpdate.Filter.Validate(filterContext);

				if (ServiceResult.IsBad(filterResult.Status))
				{
					errors[handle.Index] = filterResult.Status;
					continue;
				}

				// all done.
				errors[handle.Index] = StatusCodes.BadNotImplemented;
			}
		}

		/// <summary>
		/// Deletes history events.
		/// </summary>
		protected override void HistoryDeleteEvents(
			ServerSystemContext context,
			IList<DeleteEventDetails> nodesToUpdate,
			IList<HistoryUpdateResult> results,
			IList<ServiceResult> errors,
			List<NodeHandle> nodesToProcess,
			IDictionary<NodeId, NodeState> cache)
		{
			for(int ii = 0; ii < nodesToProcess.Count; ii++)
			{
				NodeHandle handle = nodesToProcess[ii];
				DeleteEventDetails nodeToUpdate = nodesToUpdate[handle.Index];
				HistoryUpdateResult result = results[handle.Index];

				// delete events.
				bool failed = false;

				for(int jj = 0; jj < nodeToUpdate.EventIds.Count; jj++)
				{
					try
					{
						string eventId = new Guid(nodeToUpdate.EventIds[jj]).ToString();

						if (!DeleteEvent(eventId))
						{
							result.OperationResults.Add(StatusCodes.BadEventIdUnknown);
							failed = true;
							continue;
						}

						result.OperationResults.Add(StatusCodes.Good);
					}
					catch
					{
						result.OperationResults.Add(StatusCodes.BadEventIdUnknown);
						failed = true;
					}
				}

				// check if diagnostics are required.
				if (failed)
				{
					if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0)
					{
						for(int jj = 0; jj < nodeToUpdate.EventIds.Count; jj++)
						{
							if (StatusCode.IsBad(result.OperationResults[jj]))
							{
								result.DiagnosticInfos.Add(ServerUtils.CreateDiagnosticInfo(Server, context.OperationContext, result.OperationResults[jj]));
							}
						}
					}
				}

				// all done.
				errors[handle.Index] = ServiceResult.Good;
			}
		}

		#region History Helpers

		/// <summary>
		/// Fetches the requested event fields from the event.
		/// </summary>
		private HistoryEventFieldList GetEventFields(HistoryReadRequest request, IFilterTarget instance)
		{
			// fetch the event fields.
			HistoryEventFieldList fields = new HistoryEventFieldList();

			foreach(SimpleAttributeOperand clause in request.Filter.SelectClauses)
			{
				// get the value of the attribute (apply localization).
				object value = instance.GetAttributeValue(
					request.FilterContext,
					clause.TypeDefinitionId,
					clause.BrowsePath,
					clause.AttributeId,
					clause.ParsedIndexRange);

				// add the value to the list of event fields.
				if (value != null)
				{
					// translate any localized text.
					LocalizedText text = value as LocalizedText;

					if (text != null)
					{
						value = Server.ResourceManager.Translate(request.FilterContext.PreferredLocales, text);
					}

					// add value.
					fields.EventFields.Add(new Variant(value));
				}

				// add a dummy entry for missing values.
				else
				{
					fields.EventFields.Add(Variant.Null);
				}
			}

			return fields;
		}

		/// <summary>
		/// Creates a new history request.
		/// </summary>
		private HistoryReadRequest CreateHistoryReadRequest(
			ServerSystemContext context,
			ReadEventDetails details,
			NodeHandle handle,
			HistoryReadValueId nodeToRead)
		{
			FilterContext filterContext = new FilterContext(context.NamespaceUris, context.TypeTable, context.PreferredLocales);
			LinkedList<BaseEventState> events = new LinkedList<BaseEventState>();

			for(int iTable = 0; iTable <= 1; iTable++)
			{
				string stringFilter;

				if (handle.Node is WellState)
				{
					stringFilter = string.Format(@"({0}='{1}')", BrowseNames.UidWell, handle.Node.NodeId.Identifier);
				}
				else
				{
					stringFilter = string.Format(@"({0} LIKE '{1}*')", BrowseNames.NameWell, handle.Node.NodeId.Identifier);
				}

				string timeFilter = AddTimeIntervalFilter(details.StartTime, details.EndTime);

				if (timeFilter.Length > 0)
				{
					stringFilter += timeFilter;
				}

				DataView view = new DataView(
					m_dataset.Tables[iTable],
					stringFilter,
                    Opc.Ua.BrowseNames.Time,
					DataViewRowState.CurrentRows);

				LinkedListNode<BaseEventState> pos = events.First;
				bool sizeLimited = (details.StartTime == DateTime.MinValue || details.EndTime == DateTime.MinValue);

				foreach(DataRowView row in view)
				{
					// check if reached max results.
					if (sizeLimited)
					{
						if (events.Count >= details.NumValuesPerNode)
						{
							break;
						}
					}

					BaseEventState e = iTable == 0 ? GetFluidLevelTestReport(context, NamespaceIndex, row.Row) :
						GetInjectionTestReport(context, NamespaceIndex, row.Row);

					if (details.Filter.WhereClause != null && details.Filter.WhereClause.Elements.Count > 0)
					{
						if (!details.Filter.WhereClause.Evaluate(filterContext, e))
						{
							continue;
						}
					}

					bool inserted = false;

					for(LinkedListNode<BaseEventState> jj = pos; jj != null; jj = jj.Next)
					{
						if (jj.Value.Time.Value > e.Time.Value)
						{
							events.AddBefore(jj, e);
							pos = jj;
							inserted = true;
							break;
						}
					}

					if (!inserted)
					{
						events.AddLast(e);
						pos = null;
					}
				}
			}

			HistoryReadRequest request = new HistoryReadRequest();
			request.Events = events;
			request.TimeFlowsBackward = details.StartTime == DateTime.MinValue || (details.EndTime != DateTime.MinValue && details.EndTime < details.StartTime);
			request.NumValuesPerNode = details.NumValuesPerNode;
			request.Filter = details.Filter;
			request.FilterContext = filterContext;
			return request;
		}

		private bool DeleteEvent(string eventId)
		{
			string filter = string.Format(@"({0}='{1}')", Opc.Ua.BrowseNames.EventId, eventId);

			for(int ii = 0; ii < m_dataset.Tables.Count; ii++)
			{
				DataView view = new DataView(m_dataset.Tables[ii], filter.ToString(), null, DataViewRowState.CurrentRows);

				if (view.Count > 0)
				{
					view[0].Delete();
					m_dataset.AcceptChanges();
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Stores a read history request.
		/// </summary>
		private class HistoryReadRequest
		{
			public byte[] ContinuationPoint;
			public LinkedList<BaseEventState> Events;
			public bool TimeFlowsBackward;
			public uint NumValuesPerNode;
			public EventFilter Filter;
			public FilterContext FilterContext;
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

		/// <summary>
		/// Reads the history for the specified time range.
		/// </summary>
		private string AddTimeIntervalFilter(DateTime startTime, DateTime endTime)
		{
			DateTime earlyTime = startTime;
			DateTime lateTime = endTime;
			string timeFilter = string.Empty;

			if (endTime < startTime && endTime != DateTime.MinValue)
			{
				earlyTime = endTime;
				lateTime = startTime;
			}

			if (earlyTime != DateTime.MinValue)
			{
				timeFilter = string.Format(@" AND ({0}>=#{1}#)", Opc.Ua.BrowseNames.Time, earlyTime);
			}

			if (lateTime != DateTime.MinValue)
			{
				timeFilter += string.Format(@" AND ({0}<#{1}#)", Opc.Ua.BrowseNames.Time, lateTime);
			}

			return timeFilter;
		}

		public BaseEventState GetFluidLevelTestReport(ISystemContext SystemContext, ushort namespaceIndex, DataRow row)
		{
			// construct translation object with default text.
			TranslationInfo info = new TranslationInfo(
				"FluidLevelTestReport",
				"en-US",
				"A fluid level test report is available.");

			// construct the event.
			FluidLevelTestReportState e = new FluidLevelTestReportState(null);

			e.Initialize(
				SystemContext,
				null,
				EventSeverity.Medium,
				new LocalizedText(info));

			// override event id and time.                
			e.EventId.Value = new Guid((string) row[Opc.Ua.BrowseNames.EventId]).ToByteArray();
			e.Time.Value = (DateTime) row[Opc.Ua.BrowseNames.Time];


			string nameWell = (string) row[BrowseNames.NameWell];
			string uidWell = (string) row[BrowseNames.UidWell];

			e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceName, nameWell, false);
			e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceNode, new NodeId(uidWell, namespaceIndex), false);
			e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.LocalTime, m_timeZone, false);

			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.NameWell, namespaceIndex), nameWell, false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.UidWell, namespaceIndex), uidWell, false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.TestDate, namespaceIndex), row[BrowseNames.TestDate], false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.TestReason, namespaceIndex), row[BrowseNames.TestReason], false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.TestedBy, namespaceIndex), row[BrowseNames.TestedBy], false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.FluidLevel, namespaceIndex), row[BrowseNames.FluidLevel], false);
			e.FluidLevel.SetChildValue(SystemContext, Opc.Ua.BrowseNames.EngineeringUnits, new EUInformation((string) row[Opc.Ua.BrowseNames.EngineeringUnits], Namespaces.HistoricalEvents), false);

			return e;
		}

		public BaseEventState GetInjectionTestReport(ISystemContext SystemContext, ushort namespaceIndex, DataRow row)
		{
			// construct translation object with default text.
			TranslationInfo info = new TranslationInfo(
				"InjectionTestReport",
				"en-US",
				"An injection test report is available.");

			// construct the event.
			InjectionTestReportState e = new InjectionTestReportState(null);

			e.Initialize(
				SystemContext,
				null,
				EventSeverity.Medium,
				new LocalizedText(info));

			// override event id and time.                
			e.EventId.Value = new Guid((string) row[Opc.Ua.BrowseNames.EventId]).ToByteArray();
			e.Time.Value = (DateTime) row[Opc.Ua.BrowseNames.Time];

			string nameWell = (string) row[BrowseNames.NameWell];
			string uidWell = (string) row[BrowseNames.UidWell];

			e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceName, nameWell, false);
			e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceNode, new NodeId(uidWell, namespaceIndex), false);
			e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.LocalTime, m_timeZone, false);

			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.NameWell, namespaceIndex), nameWell, false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.UidWell, namespaceIndex), uidWell, false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.TestDate, namespaceIndex), row[BrowseNames.TestDate], false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.TestReason, namespaceIndex), row[BrowseNames.TestReason], false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.InjectedFluid, namespaceIndex), row[BrowseNames.InjectedFluid], false);
			e.SetChildValue(SystemContext, new QualifiedName(BrowseNames.TestDuration, namespaceIndex), row[BrowseNames.TestDuration], false);
			e.TestDuration.SetChildValue(SystemContext, Opc.Ua.BrowseNames.EngineeringUnits, new EUInformation((string) row[Opc.Ua.BrowseNames.EngineeringUnits], Namespaces.HistoricalEvents), false);

			return e;
		}

		#endregion

		#endregion

		#region Data fields
		protected DataSet m_dataset;
		protected TimeZoneDataType m_timeZone;
		#endregion
	}
}
