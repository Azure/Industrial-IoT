﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    internal abstract partial class OpcUaMonitoredItem
    {
        /// <summary>
        /// Event monitored item
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal class Event : OpcUaMonitoredItem
        {
            /// <inheritdoc/>
            public override (string NodeId, string[] Path, UpdateNodeId Update)? Resolve
                => Template.RelativePath != null &&
                    (TheResolvedNodeId == Template.StartNodeId || string.IsNullOrEmpty(TheResolvedNodeId)) ?
                    (Template.StartNodeId, Template.RelativePath.ToArray(),
                        (v, context) => TheResolvedNodeId
                            = v.AsString(context, Template.NamespaceFormat) ?? string.Empty) : null;

            /// <summary>
            /// Resolved node id
            /// </summary>
            protected string TheResolvedNodeId { get; private set; }

            /// <summary>
            /// Monitored item as event
            /// </summary>
            public EventMonitoredItemModel Template { get; protected internal set; }

            /// <summary>
            /// List of field names.
            /// </summary>
            public List<(string? Name, Guid DataSetFieldId)> Fields { get; } = new();

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            public Event(EventMonitoredItemModel template, ILogger<Event> logger)
                : base(logger, template.Order)
            {
                Template = template;
                TheResolvedNodeId = template.StartNodeId;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            protected Event(Event item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                Fields = item.Fields;
                RelativePath = item.RelativePath;
                TheResolvedNodeId = item.TheResolvedNodeId;
                Template = item.Template;
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new Event(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not Event eventItem)
                {
                    return false;
                }
                if ((Template.Id ?? string.Empty) !=
                    (eventItem.Template.Id ?? string.Empty))
                {
                    return false;
                }
                if ((Template.Name ?? string.Empty) !=
                    (eventItem.Template.Name ?? string.Empty))
                {
                    return false;
                }
                if (!Template.RelativePath.SequenceEqualsSafe(eventItem.Template.RelativePath))
                {
                    return false;
                }
                if (Template.StartNodeId != eventItem.Template.StartNodeId)
                {
                    return false;
                }
                if (Template.AttributeId != eventItem.Template.AttributeId)
                {
                    return false;
                }
                return base.Equals(obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return HashCode.Combine(base.GetHashCode(),
                   nameof(Event),
                   Template.Name ?? string.Empty,
                   Template.Id ?? string.Empty,
                   Template.RelativePath ?? Array.Empty<string>(),
                   Template.StartNodeId,
                   Template.AttributeId ?? NodeAttribute.NodeId);
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Event Item '{Template.StartNodeId}' with server id {RemoteId} - " +
                    $"{(Status?.Created == true ? "" : "not ")}created";
            }

            public override Func<IOpcUaSession, CancellationToken, Task>? FinalizeAddTo
                => async (session, ct)
                => Filter = await GetEventFilterAsync(session, ct).ConfigureAwait(false);

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription,
                IOpcUaSession session)
            {
                var nodeId = TheResolvedNodeId.ToNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(nodeId))
                {
                    return false;
                }
                DisplayName = Template.GetFieldId();
                AttributeId = (uint)(Template.AttributeId
                    ?? (NodeAttribute)Attributes.EventNotifier);
                MonitoringMode = Template.MonitoringMode.ToStackType()
                    ?? Opc.Ua.MonitoringMode.Reporting;
                StartNodeId = nodeId;
                QueueSize = Template.QueueSize;
                SamplingInterval = 0;
                DiscardOldest = !(Template.DiscardNew ?? false);
                Valid = true;

                return base.AddTo(subscription, session);
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session)
            {
                if (item is not Event model || !Valid)
                {
                    return false;
                }

                var itemChange = MergeWith(Template, model.Template, out var updated);
                if (itemChange)
                {
                    Template = updated;
                }

                // Update event filter
                if (!model.Template.EventFilter.IsSameAs(Template.EventFilter))
                {
                    Template = Template with
                    {
                        EventFilter = model.Template.EventFilter
                    };
                    _logger.LogDebug("{Item}: Changing event filter.", this);
                    itemChange = true;
                }
                return itemChange;
            }

            public override Func<IOpcUaSession, CancellationToken, Task>? FinalizeMergeWith
                => async (session, ct)
                => Filter = await GetEventFilterAsync(session, ct).ConfigureAwait(false);

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber, DateTime timestamp,
                IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                if (evt is EventFieldList eventFields &&
                    base.TryGetMonitoredItemNotifications(sequenceNumber, timestamp, evt, notifications))
                {
                    return ProcessEventNotification(sequenceNumber, timestamp, eventFields, notifications);
                }
                return false;
            }

            /// <inheritdoc/>
            protected override IEnumerable<OpcUaMonitoredItem> CreateTriggeredItems(
                ILoggerFactory factory, IOpcUaClient? client = null)
            {
                if (Template.TriggeredItems != null)
                {
                    return Create(Template.TriggeredItems, factory, client);
                }
                return Enumerable.Empty<OpcUaMonitoredItem>();
            }

            /// <inheritdoc/>
            protected override bool TryGetErrorMonitoredItemNotifications(
                uint sequenceNumber, StatusCode statusCode,
                IList<MonitoredItemNotificationModel> notifications)
            {
                foreach (var (Name, _) in Fields)
                {
                    if (Name == null)
                    {
                        continue;
                    }
                    notifications.Add(new MonitoredItemNotificationModel
                    {
                        Order = Order,
                        MonitoredItemId = Template.GetMonitoredItemId(),
                        FieldId = Name,
                        Context = Template.Context,
                        NodeId = TheResolvedNodeId,
                        Value = new DataValue(statusCode),
                        Flags = MonitoredItemSourceFlags.Error,
                        SequenceNumber = sequenceNumber
                    });
                }
                return true;
            }

            /// <summary>
            /// Process event notifications
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <param name="timestamp"></param>
            /// <param name="eventFields"></param>
            /// <param name="notifications"></param>
            /// <returns></returns>
            protected virtual bool ProcessEventNotification(uint sequenceNumber, DateTime timestamp,
                EventFieldList eventFields, IList<MonitoredItemNotificationModel> notifications)
            {
                // Send notifications as event
                foreach (var n in ToMonitoredItemNotifications(sequenceNumber, eventFields)
                    .Where(n => !string.IsNullOrEmpty(n.FieldId)))
                {
                    notifications.Add(n);
                }
                return true;
            }

            /// <summary>
            /// Convert to monitored item notifications
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <param name="eventFields"></param>
            /// <returns></returns>
            protected IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
                uint sequenceNumber, EventFieldList eventFields)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                if (Fields.Count >= eventFields.EventFields.Count)
                {
                    for (var i = 0; i < eventFields.EventFields.Count; i++)
                    {
                        yield return new MonitoredItemNotificationModel
                        {
                            Order = Order,
                            MonitoredItemId = Template.GetMonitoredItemId(),
                            Context = Template.Context,
                            FieldId = Fields[i].Name ?? string.Empty,
                            NodeId = TheResolvedNodeId,
                            Flags = 0,
                            Value = new DataValue(eventFields.EventFields[i]),
                            SequenceNumber = sequenceNumber
                        };
                    }
                }
            }

            /// <summary>
            /// Get event filter
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            protected virtual async ValueTask<EventFilter> GetEventFilterAsync(IOpcUaSession session,
                CancellationToken ct)
            {
                var (eventFilter, internalSelectClauses) =
                    await BuildEventFilterAsync(session).ConfigureAwait(false);
                UpdateFieldNames(session, eventFilter, internalSelectClauses);
                return eventFilter;
            }

            /// <summary>
            /// Update field names
            /// </summary>
            /// <param name="session"></param>
            /// <param name="eventFilter"></param>
            /// <param name="internalSelectClauses"></param>
            protected void UpdateFieldNames(IOpcUaSession session, EventFilter eventFilter,
                List<SimpleAttributeOperand> internalSelectClauses)
            {
                // let's loop thru the final set of select clauses and setup the field names used
                Fields.Clear();
                foreach (var selectClause in eventFilter.SelectClauses)
                {
                    if (!internalSelectClauses.Any(x => x == selectClause))
                    {
                        var fieldName = string.Empty;
                        var definedSelectClause = Template.EventFilter.SelectClauses?
                            .ElementAtOrDefault(eventFilter.SelectClauses.IndexOf(selectClause));
                        var fieldGuid = definedSelectClause?.DataSetClassFieldId;
                        if (!string.IsNullOrEmpty(definedSelectClause?.DataSetFieldName))
                        {
                            fieldName = definedSelectClause.DataSetFieldName;
                        }
                        else if (selectClause.BrowsePath != null && selectClause.BrowsePath.Count != 0)
                        {
                            // Format as relative path string
                            fieldName = selectClause.BrowsePath
                                .Select(q => q.AsString(session.MessageContext, Template.NamespaceFormat))
                                .Aggregate((a, b) => $"{a}/{b}");
                        }

                        if (fieldName.Length == 0 &&
                            selectClause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                            selectClause.AttributeId == Attributes.NodeId)
                        {
                            fieldName = "ConditionId";
                        }
                        Fields.Add((fieldName, fieldGuid ?? Guid.NewGuid()));
                    }
                    else
                    {
                        // if a field's nameis empty, it's not written to the output
                        Fields.Add((null, Guid.Empty));
                    }
                }
                Debug.Assert(Fields.Count == eventFilter.SelectClauses.Count);
            }

            /// <summary>
            /// Build event filter
            /// </summary>
            /// <param name="session"></param>
            /// <returns></returns>
            protected ValueTask<(EventFilter, List<SimpleAttributeOperand>)> BuildEventFilterAsync(
                IOpcUaSession session)
            {
                var eventFilter = Template.EventFilter.SelectClauses == null
                    ? FilterEncoderEx.GetDefaultEventFilter()
                    : session.Codec.Decode(Template.EventFilter);

                // let's keep track of the internal fields we add so that they don't show up in the output
                var selectClauses = new List<SimpleAttributeOperand>();
                if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                    && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType))
                {
                    var selectClause = new SimpleAttributeOperand(
                        ObjectTypeIds.BaseEventType, BrowseNames.EventType);
                    eventFilter.SelectClauses.Add(selectClause);
                    selectClauses.Add(selectClause);
                }
                return ValueTask.FromResult((eventFilter, selectClauses));
            }
        }
    }
}