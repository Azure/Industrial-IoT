// ------------------------------------------------------------
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
    using Opc.Ua.Client.ComplexTypes;
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
            public override (string NodeId, UpdateString Update)? GetDisplayName
                => Template.FetchDataSetFieldName == true &&
                    !string.IsNullOrEmpty(Template.EventFilter.TypeDefinitionId) &&
                    Template.DataSetFieldName == null ?
                    (Template.EventFilter.TypeDefinitionId!, v => Template = Template with
                    {
                        DataSetFieldName = v
                    }) : null;

            /// <inheritdoc/>
            public override (string NodeId, string[] Path, UpdateNodeId Update)? Resolve
                => Template.RelativePath != null &&
                    (NodeId == Template.StartNodeId || string.IsNullOrEmpty(NodeId)) ?
                    (Template.StartNodeId, Template.RelativePath.ToArray(),
                        (v, context) => NodeId
                            = v.AsString(context, Template.NamespaceFormat) ?? string.Empty) : null;

            /// <inheritdoc/>
            public override (string NodeId, UpdateRelativePath Update)? GetPath
                => TheResolvedRelativePath == null &&
                !string.IsNullOrEmpty(NodeId) ? (NodeId, (path, context) =>
                {
                    if (path == null)
                    {
                        NodeId = string.Empty;
                    }
                    TheResolvedRelativePath = path;
                }
            ) : null;

            /// <inheritdoc/>
            public override string? EventTypeName => Template.DisplayName;

            /// <summary>
            /// Relative path
            /// </summary>
            protected RelativePath? TheResolvedRelativePath { get; private set; }

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
            /// <param name="owner"></param>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            /// <param name="timeProvider"></param>
            public Event(ISubscriber owner, EventMonitoredItemModel template,
                ILogger<Event> logger, TimeProvider timeProvider) :
                base(owner, logger, template.StartNodeId, timeProvider)
            {
                Template = template;
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
                TheResolvedRelativePath = item.TheResolvedRelativePath;
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
                if ((Template.DataSetFieldId ?? string.Empty) !=
                    (eventItem.Template.DataSetFieldId ?? string.Empty))
                {
                    return false;
                }
                if ((Template.DataSetFieldName ?? string.Empty) !=
                    (eventItem.Template.DataSetFieldName ?? string.Empty))
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
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = 423444443;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldName ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldId ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<IReadOnlyList<string>>.Default.GetHashCode(
                        Template.RelativePath ?? Array.Empty<string>());
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.StartNodeId);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<NodeAttribute>.Default.GetHashCode(
                        Template.AttributeId ?? NodeAttribute.NodeId);
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                var str = $"Event Item '{Template.StartNodeId}'";
                if (RemoteId.HasValue)
                {
                    str += $" with server id {RemoteId} ({(Status?.Created == true ? "" : "not ")}created)";
                }
                return str;
            }

            /// <inheritdoc/>
            public override async ValueTask GetMetaDataAsync(IOpcUaSession session,
                ComplexTypeSystem? typeSystem, List<PublishedFieldMetaDataModel> fields,
                NodeIdDictionary<object> dataTypes, CancellationToken ct)
            {
                if (Filter is not EventFilter eventFilter)
                {
                    return;
                }
                try
                {
                    Debug.Assert(Fields.Count == eventFilter.SelectClauses.Count);
                    for (var i = 0; i < eventFilter.SelectClauses.Count; i++)
                    {
                        var selectClause = eventFilter.SelectClauses[i];
                        var fieldName = Fields[i].Name;
                        if (fieldName == null)
                        {
                            continue;
                        }
                        var dataSetClassFieldId = (Uuid)Fields[i].DataSetFieldId;
                        var targetNode = await FindNodeWithBrowsePathAsync(session,
                            selectClause.BrowsePath, selectClause.TypeDefinitionId,
                            ct).ConfigureAwait(false);
                        if (targetNode is VariableNode variable)
                        {
                            await AddVariableFieldAsync(fields, dataTypes, session,
                                typeSystem, variable, fieldName, dataSetClassFieldId,
                                ct).ConfigureAwait(false);
                        }
                        else
                        {
                            // Should this happen?
                            await AddVariableFieldAsync(fields, dataTypes, session,
                                typeSystem, new VariableNode
                                {
                                    DataType = (int)BuiltInType.Variant
                                }, fieldName, dataSetClassFieldId,
                                ct).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, "{Item}: Failed to get metadata for event.", this);
                    throw;
                }
            }

            public override Func<IOpcUaSession, CancellationToken, Task>? FinalizeAddTo
                => async (session, ct)
                => Filter = await GetEventFilterAsync(session, ct).ConfigureAwait(false);

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription,
                IOpcUaSession session, out bool metadataChanged)
            {
                var nodeId = NodeId.ToNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(nodeId))
                {
                    metadataChanged = false;
                    return false;
                }
                DisplayName = Template.DisplayName;
                AttributeId = (uint)(Template.AttributeId
                    ?? (NodeAttribute)Attributes.EventNotifier);
                MonitoringMode = Template.MonitoringMode.ToStackType()
                    ?? Opc.Ua.MonitoringMode.Reporting;
                StartNodeId = nodeId;
                SamplingInterval = 0;
                UpdateQueueSize(subscription, Template);
                DiscardOldest = !(Template.DiscardNew ?? false);
                Valid = true;

                return base.AddTo(subscription, session, out metadataChanged);
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                 out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not Event model || !Valid)
                {
                    return false;
                }

                var itemChange = MergeWith(Template, model.Template, out var updated,
                    out metadataChanged);
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
                    metadataChanged = true;
                    itemChange = true;
                }
                return itemChange;
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription, ref bool applyChanges)
            {
                var msgContext = subscription.Session?.MessageContext;
                if (Status?.FilterResult is EventFilterResult evr && msgContext != null)
                {
                    if (Status.Error != null && ServiceResult.IsNotGood(Status.Error))
                    {
                        _logger.LogError("Event filter applied with result {Result} for {Item}",
                            evr.AsJson(msgContext), this);
                    }
                    else
                    {
                        _logger.LogDebug("Event filter applied with result {Result} for {Item}",
                            evr.AsJson(msgContext), this);
                    }
                }
                return base.TryCompleteChanges(subscription, ref applyChanges);
            }

            /// <inheritdoc/>
            protected override bool OnSamplingIntervalOrQueueSizeRevised(
                bool samplingIntervalChanged, bool queueSizeChanged)
            {
                Debug.Assert(Subscription != null);
                var applyChanges = base.OnSamplingIntervalOrQueueSizeRevised(
                    samplingIntervalChanged, queueSizeChanged);
                if (samplingIntervalChanged && Status.SamplingInterval != 0)
                {
                    // Not necessary as sampling interval will likely always stay 0
                    applyChanges |= UpdateQueueSize(Subscription, Template);
                }
                return applyChanges;
            }

            public override Func<IOpcUaSession, CancellationToken, Task>? FinalizeMergeWith
                => async (session, ct)
                => Filter = await GetEventFilterAsync(session, ct).ConfigureAwait(false);

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(DateTimeOffset publishTime,
                IEncodeable evt, MonitoredItemNotifications notifications)
            {
                if (evt is EventFieldList eventFields &&
                    base.TryGetMonitoredItemNotifications(publishTime, evt, notifications))
                {
                    return ProcessEventNotification(publishTime, eventFields, notifications);
                }
                return false;
            }

            /// <inheritdoc/>
            protected override IEnumerable<OpcUaMonitoredItem> CreateTriggeredItems(
                ILoggerFactory factory, OpcUaClient client)
            {
                if (Template.TriggeredItems != null)
                {
                    return Create(client, Template.TriggeredItems.Select(i => (Owner, i)),
                        factory, TimeProvider);
                }
                return Enumerable.Empty<OpcUaMonitoredItem>();
            }

            /// <inheritdoc/>
            protected override bool TryGetErrorMonitoredItemNotifications(
                StatusCode statusCode, MonitoredItemNotifications notifications)
            {
                foreach (var (Name, _) in Fields)
                {
                    if (Name == null)
                    {
                        continue;
                    }
                    notifications.Add(Owner, new MonitoredItemNotificationModel
                    {
                        Id = Template.Id ?? string.Empty,
                        DataSetName = Template.DisplayName,
                        DataSetFieldName = Name,
                        NodeId = Template.StartNodeId,
                        PathFromRoot = TheResolvedRelativePath,
                        Value = new DataValue(statusCode),
                        Flags = MonitoredItemSourceFlags.Error,
                        SequenceNumber = GetNextSequenceNumber()
                    });
                }
                return true;
            }

            /// <summary>
            /// Process event notifications
            /// </summary>
            /// <param name="timestamp"></param>
            /// <param name="eventFields"></param>
            /// <param name="notifications"></param>
            /// <returns></returns>
            protected virtual bool ProcessEventNotification(DateTimeOffset timestamp,
                EventFieldList eventFields, MonitoredItemNotifications notifications)
            {
                // Send notifications as event
                foreach (var n in ToMonitoredItemNotifications(eventFields)
                    .Where(n => n.DataSetFieldName != null))
                {
                    notifications.Add(Owner, n);
                }
                return true;
            }

            /// <summary>
            /// Convert to monitored item notifications
            /// </summary>
            /// <param name="eventFields"></param>
            /// <returns></returns>
            protected IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
                EventFieldList eventFields)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                //
                // Important - so the event is properly batched during encoding the same
                // sequence number must be used for all monitored item notifications !
                //
                var sequenceNumber = GetNextSequenceNumber();
                if (Fields.Count >= eventFields.EventFields.Count)
                {
                    for (var i = 0; i < eventFields.EventFields.Count; i++)
                    {
                        yield return new MonitoredItemNotificationModel
                        {
                            Id = Template.Id ?? string.Empty,
                            DataSetName = Template.DisplayName,
                            DataSetFieldName = Fields[i].Name,
                            NodeId = Template.StartNodeId,
                            PathFromRoot = TheResolvedRelativePath,
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
                    await BuildEventFilterAsync(session, ct).ConfigureAwait(false);
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
                        if (!string.IsNullOrEmpty(definedSelectClause?.DisplayName))
                        {
                            fieldName = definedSelectClause.DisplayName;
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
                        Fields.Add((fieldName, Guid.NewGuid()));
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
            /// <param name="ct"></param>
            /// <returns></returns>
            protected async ValueTask<(EventFilter, List<SimpleAttributeOperand>)> BuildEventFilterAsync(
                IOpcUaSession session, CancellationToken ct)
            {
                EventFilter? eventFilter;
                if (!string.IsNullOrEmpty(Template.EventFilter.TypeDefinitionId))
                {
                    eventFilter = await GetSimpleEventFilterAsync(session, ct).ConfigureAwait(false);
                }
                else
                {
                    eventFilter = session.Codec.Decode(Template.EventFilter);
                }

                // let's keep track of the internal fields we add so that they don't show up in the output
                var selectClauses = new List<SimpleAttributeOperand>();
                if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                    && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType))
                {
                    var selectClause = new SimpleAttributeOperand(ObjectTypeIds.BaseEventType,
                        BrowseNames.EventType);
                    eventFilter.SelectClauses.Add(selectClause);
                    selectClauses.Add(selectClause);
                }
                return (eventFilter, selectClauses);
            }

            /// <summary>
            /// Builds select clause and where clause by using OPC UA reflection
            /// </summary>
            /// <param name="session"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async ValueTask<EventFilter> GetSimpleEventFilterAsync(IOpcUaSession session,
                CancellationToken ct)
            {
                Debug.Assert(Template != null);
                var typeDefinitionId = Template.EventFilter.TypeDefinitionId.ToNodeId(
                    session.MessageContext);
                var nodes = new List<Node>();
                ExpandedNodeId? superType = null;
                var typeDefinitionNode = await session.NodeCache.FetchNodeAsync(typeDefinitionId,
                    ct).ConfigureAwait(false);
                nodes.Insert(0, typeDefinitionNode);
                do
                {
                    superType = nodes[0].GetSuperType(session.TypeTree);
                    if (superType != null)
                    {
                        typeDefinitionNode = await session.NodeCache.FetchNodeAsync(superType,
                            ct).ConfigureAwait(false);
                        nodes.Insert(0, typeDefinitionNode);
                    }
                }
                while (superType != null);

                var fieldNames = new List<QualifiedName>();

                foreach (var node in nodes)
                {
                    await ParseFieldsAsync(session, fieldNames, node, string.Empty,
                        ct).ConfigureAwait(false);
                }
                fieldNames = fieldNames
                    .Distinct()
                    .OrderBy(x => x.Name).ToList();

                var eventFilter = new EventFilter();
                // Let's add ConditionId manually first if event is derived from ConditionType
                if (nodes.Any(x => x.NodeId == ObjectTypeIds.ConditionType))
                {
                    eventFilter.SelectClauses.Add(new SimpleAttributeOperand()
                    {
                        BrowsePath = new QualifiedNameCollection(),
                        TypeDefinitionId = ObjectTypeIds.ConditionType,
                        AttributeId = Attributes.NodeId
                    });
                }

                foreach (var fieldName in fieldNames)
                {
                    var selectClause = new SimpleAttributeOperand()
                    {
                        TypeDefinitionId = ObjectTypeIds.BaseEventType,
                        AttributeId = Attributes.Value,
                        BrowsePath = fieldName.Name
                            .Split('|')
                            .Select(x => new QualifiedName(x, fieldName.NamespaceIndex))
                            .ToArray()
                    };
                    eventFilter.SelectClauses.Add(selectClause);
                }
                eventFilter.WhereClause = new ContentFilter();
                eventFilter.WhereClause.Push(FilterOperator.OfType, typeDefinitionId);

                return eventFilter;
            }

            /// <summary>
            /// Find node by browse path
            /// </summary>
            /// <param name="session"></param>
            /// <param name="browsePath"></param>
            /// <param name="nodeId"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private static async ValueTask<INode?> FindNodeWithBrowsePathAsync(IOpcUaSession session,
                QualifiedNameCollection browsePath, ExpandedNodeId nodeId, CancellationToken ct)
            {
                INode? found = null;
                foreach (var browseName in browsePath)
                {
                    found = null;
                    while (found == null)
                    {
                        found = await session.NodeCache.FindAsync(nodeId, ct).ConfigureAwait(false);
                        if (found is not Node node)
                        {
                            return null;
                        }

                        //
                        // Get all hierarchical references of the node and
                        // match browse name
                        //
                        foreach (var reference in node.ReferenceTable.Find(
                            ReferenceTypeIds.HierarchicalReferences, false,
                                true, session.TypeTree))
                        {
                            var target = await session.NodeCache.FindAsync(reference.TargetId,
                                ct).ConfigureAwait(false);
                            if (target?.BrowseName == browseName)
                            {
                                nodeId = target.NodeId;
                                found = target;
                                break;
                            }
                        }

                        if (found == null)
                        {
                            // Try super type
                            nodeId = await session.TypeTree.FindSuperTypeAsync(nodeId,
                                ct).ConfigureAwait(false);
                            if (Opc.Ua.NodeId.IsNull(nodeId))
                            {
                                // Nothing can be found since there is no more super type
                                return null;
                            }
                        }
                    }
                    nodeId = found.NodeId;
                }
                return found;
            }

            /// <summary>
            /// Get all the fields of a type definition node to build the
            /// select clause.
            /// </summary>
            /// <param name="session"></param>
            /// <param name="fieldNames"></param>
            /// <param name="node"></param>
            /// <param name="browsePathPrefix"></param>
            /// <param name="ct"></param>
            protected static async ValueTask ParseFieldsAsync(IOpcUaSession session, List<QualifiedName> fieldNames,
                Node node, string browsePathPrefix, CancellationToken ct)
            {
                foreach (var reference in node.ReferenceTable)
                {
                    if (reference.ReferenceTypeId == ReferenceTypeIds.HasComponent &&
                        !reference.IsInverse)
                    {
                        var componentNode = await session.NodeCache.FetchNodeAsync(reference.TargetId,
                            ct).ConfigureAwait(false);
                        if (componentNode.NodeClass == Opc.Ua.NodeClass.Variable)
                        {
                            var fieldName = browsePathPrefix + componentNode.BrowseName.Name;
                            fieldNames.Add(new QualifiedName(
                                fieldName, componentNode.BrowseName.NamespaceIndex));
                            await ParseFieldsAsync(session, fieldNames, componentNode,
                                $"{fieldName}|", ct).ConfigureAwait(false);
                        }
                    }
                    else if (reference.ReferenceTypeId == ReferenceTypeIds.HasProperty)
                    {
                        var propertyNode = await session.NodeCache.FetchNodeAsync(reference.TargetId,
                            ct).ConfigureAwait(false);
                        var fieldName = browsePathPrefix + propertyNode.BrowseName.Name;
                        fieldNames.Add(new QualifiedName(
                            fieldName, propertyNode.BrowseName.NamespaceIndex));
                    }
                }
            }
        }
    }
}
