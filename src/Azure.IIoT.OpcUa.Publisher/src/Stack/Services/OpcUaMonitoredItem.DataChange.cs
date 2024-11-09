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
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    internal abstract partial class OpcUaMonitoredItem
    {
        /// <summary>
        /// Data item
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal class DataChange : OpcUaMonitoredItem
        {
            /// <inheritdoc/>
            public override (string NodeId, string[] Path, UpdateNodeId Update)? Resolve
                => Template.RelativePath != null &&
                    (TheResolvedNodeId == Template.StartNodeId || string.IsNullOrEmpty(TheResolvedNodeId)) ?
                    (Template.StartNodeId, Template.RelativePath.ToArray(),
                        (v, context) => TheResolvedNodeId = NodeId
                            = v.AsString(context, Template.NamespaceFormat) ?? string.Empty) : null;

            /// <inheritdoc/>
            public override (string NodeId, UpdateNodeId Update)? Register
                => Template.RegisterRead == true && !_registeredForReading &&
                    !string.IsNullOrEmpty(TheResolvedNodeId) ? (TheResolvedNodeId, (v, context) =>
                    {
                        NodeId = v.AsString(context, Template.NamespaceFormat) ?? string.Empty;
                        // We only want to register the node once for reading inside a session
                        _registeredForReading = true;
                    }
            ) : null;

            /// <inheritdoc/>
            public override (string NodeId, UpdateString Update)? GetDisplayName
                => Template.FetchDataSetFieldName == true && Template.DataSetFieldName != null &&
                    !string.IsNullOrEmpty(NodeId) ?
                    (NodeId, v => Template = Template with { DataSetFieldName = v }) : null;

            /// <inheritdoc/>
            public override (string NodeId, UpdateRelativePath Update)? GetPath
                => TheResolvedRelativePath == null &&
                !string.IsNullOrEmpty(TheResolvedNodeId) ? (TheResolvedNodeId, (path, context) =>
                {
                    if (path == null)
                    {
                        NodeId = string.Empty;
                    }
                    TheResolvedRelativePath = path;
                }
            ) : null;

            /// <summary>
            /// Monitored item as data
            /// </summary>
            public DataMonitoredItemModel Template { get; protected internal set; }

            /// <summary>
            /// Resolved node id
            /// </summary>
            protected string TheResolvedNodeId { get; private set; }

            /// <summary>
            /// Relative path
            /// </summary>
            protected RelativePath? TheResolvedRelativePath { get; private set; }

            /// <summary>
            /// Field identifier either configured or randomly assigned
            /// for data change items.
            /// </summary>
            public Guid DataSetClassFieldId
                => Template?.DataSetClassFieldId == Guid.Empty ?
                    _fieldId : Template?.DataSetClassFieldId ?? Guid.Empty;

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            /// <param name="timeProvider"></param>
            public DataChange(ISubscriber owner, DataMonitoredItemModel template,
                ILogger<DataChange> logger, TimeProvider timeProvider) :
                base(owner, logger, template.StartNodeId, timeProvider)
            {
                Template = template;

                //
                // We also track the resolved node id so we distinguish it
                // from the registered and thus effective node id
                //
                TheResolvedNodeId = template.StartNodeId;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            protected DataChange(DataChange item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                TheResolvedNodeId = item.TheResolvedNodeId;
                TheResolvedRelativePath = item.TheResolvedRelativePath;
                Template = item.Template;
                _fieldId = item._fieldId;
                _skipDataChangeNotification = item._skipDataChangeNotification;
                _registeredForReading = false;
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new DataChange(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not DataChange dataItem)
                {
                    return false;
                }
                if ((Template.DataSetFieldId ?? string.Empty) !=
                    (dataItem.Template.DataSetFieldId ?? string.Empty))
                {
                    return false;
                }
                if (!Template.RelativePath.SequenceEqualsSafe(
                    dataItem.Template.RelativePath))
                {
                    return false;
                }
                if (Template.StartNodeId != dataItem.Template.StartNodeId)
                {
                    return false;
                }
                if (Template.RegisterRead != dataItem.Template.RegisterRead)
                {
                    return false;
                }
                if (Template.IndexRange != dataItem.Template.IndexRange)
                {
                    return false;
                }
                if (Template.AttributeId != dataItem.Template.AttributeId)
                {
                    return false;
                }
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = 81523234;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldId ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<IReadOnlyList<string>>.Default.GetHashCode(
                        Template.RelativePath ?? Array.Empty<string>());
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(Template.StartNodeId);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(Template.IndexRange ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<bool>.Default.GetHashCode(Template.RegisterRead ?? false);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<NodeAttribute>.Default.GetHashCode(
                        Template.AttributeId ?? NodeAttribute.NodeId);
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                var str = $"Data Item '{Template.StartNodeId}'";
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
                var nodeId = NodeId.ToNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(nodeId))
                {
                    // Failed.
                    return;
                }
                try
                {
                    var node = await session.NodeCache.FindAsync(nodeId, ct).ConfigureAwait(false);
                    if (node is VariableNode variable)
                    {
                        await AddVariableFieldAsync(fields, dataTypes, session, typeSystem, variable,
                            Template.DisplayName, (Uuid)DataSetClassFieldId, ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogDebug("{Item}: Failed to get meta data for field {Field} " +
                        "with node {NodeId} with message {Message}.", this, Template.DisplayName,
                        nodeId, ex.Message);
                }
            }

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription, IOpcUaSession session,
                out bool metadataChanged)
            {
                var nodeId = NodeId.ToNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(nodeId))
                {
                    metadataChanged = false;
                    return false;
                }

                DisplayName = Template.DisplayName;
                AttributeId = (uint)(Template.AttributeId ??
                    (NodeAttribute)Attributes.Value);
                IndexRange = Template.IndexRange;
                StartNodeId = nodeId;
                MonitoringMode = Template.MonitoringMode.ToStackType()
                    ?? Opc.Ua.MonitoringMode.Reporting;
                SamplingInterval = (int)Template.SamplingInterval.
                    GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;
                UpdateQueueSize(subscription, Template);
                Filter = Template.DataChangeFilter.ToStackModel() ??
                    (MonitoringFilter?)Template.AggregateFilter.ToStackModel(
                        session.MessageContext);
                DiscardOldest = !(Template.DiscardNew ?? false);
                Valid = true;

                if (!TrySetSkipFirst(Template.SkipFirst ?? false))
                {
                    Debug.Fail("Unexpected: Failed to set skip first setting.");
                }
                return base.AddTo(subscription, session, out metadataChanged);
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription, ref bool applyChanges)
            {
                var msgContext = subscription.Session?.MessageContext;
                if (Filter is AggregateFilter &&
                    Status?.FilterResult is AggregateFilterResult afr && msgContext != null)
                {
                    if (Status.Error != null && ServiceResult.IsNotGood(Status.Error))
                    {
                        _logger.LogError("Aggregate filter applied with result {Result} for {Item}",
                            afr.AsJson(msgContext), this);
                    }
                    else
                    {
                        _logger.LogDebug("Aggregate filter applied with result {Result} for {Item}",
                            afr.AsJson(msgContext), this);
                    }
                }
                return base.TryCompleteChanges(subscription, ref applyChanges);
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(
                DateTimeOffset publishTime, IEncodeable evt, MonitoredItemNotifications notifications)
            {
                if (evt is not MonitoredItemNotification min)
                {
                    _logger.LogDebug("{Item}: Unexpected event type {Type} received.",
                        this, evt?.GetType().Name ?? "null");
                    return false;
                }
                if (!base.TryGetMonitoredItemNotifications(publishTime, evt, notifications))
                {
                    return false;
                }
                return ProcessMonitoredItemNotification(publishTime, min, notifications);
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session,
                 out bool metadataChanged)
            {
                metadataChanged = false;
                if (item is not DataChange model || !Valid)
                {
                    return false;
                }

                var itemChange = MergeWith(Template, model.Template, out var updated,
                    out metadataChanged);
                if (itemChange)
                {
                    Template = updated;
                }

                if ((Template.SamplingInterval ?? TimeSpan.FromSeconds(1)) !=
                    (Template.SamplingInterval ?? TimeSpan.FromSeconds(1)))
                {
                    _logger.LogDebug("{Item}: Changing sampling interval from {Old} to {New}",
                        this, Template.SamplingInterval.GetValueOrDefault(
                            TimeSpan.FromSeconds(1)).TotalMilliseconds,
                        model.Template.SamplingInterval.GetValueOrDefault(
                            TimeSpan.FromSeconds(1)).TotalMilliseconds);
                    Template = Template with { SamplingInterval = model.Template.SamplingInterval };
                    SamplingInterval =
                        (int)Template.SamplingInterval.GetValueOrDefault(
                            TimeSpan.FromSeconds(1)).TotalMilliseconds;
                    itemChange = true;
                }

                if (Template.DataSetClassFieldId != model.Template.DataSetClassFieldId)
                {
                    var previous = DataSetClassFieldId;
                    Template = Template with { DataSetClassFieldId = model.Template.DataSetClassFieldId };
                    _logger.LogDebug("{Item}: Changing dataset class field id from {Old} to {New}",
                        this, previous, DataSetClassFieldId);
                    metadataChanged = true;
                }

                // Update change filter
                if (!model.Template.DataChangeFilter.IsSameAs(Template.DataChangeFilter))
                {
                    Template = Template with { DataChangeFilter = model.Template.DataChangeFilter };
                    _logger.LogDebug("{Item}: Changing data change filter.", this);
                    Filter = Template.DataChangeFilter.ToStackModel();
                    itemChange = true;
                }

                // Update AggregateFilter
                else if (!model.Template.AggregateFilter.IsSameAs(Template.AggregateFilter))
                {
                    Template = Template with { AggregateFilter = model.Template.AggregateFilter };
                    _logger.LogDebug("{Item}: Changing aggregate change filter.", this);
                    Filter = Template.AggregateFilter.ToStackModel(session.MessageContext);
                    itemChange = true;
                }
                if ((model.Template.SkipFirst ?? false) != (Template.SkipFirst ?? false))
                {
                    Template = Template with { SkipFirst = model.Template.SkipFirst };

                    if (model.TrySetSkipFirst(model.Template.SkipFirst ?? false))
                    {
                        _logger.LogDebug("{Item}: Setting skip first setting to {New}", this,
                            model.Template.SkipFirst);
                    }
                    else
                    {
                        _logger.LogInformation("{Item}: Tried to set SkipFirst but it was set" +
                            "previously or first value was already processed.", this);
                    }
                    // No change, just updated internal state
                }
                return itemChange;
            }

            /// <inheritdoc/>
            protected override bool OnSamplingIntervalOrQueueSizeRevised(
                bool samplingIntervalChanged, bool queueSizeChanged)
            {
                Debug.Assert(Subscription != null);
                var applyChanges = base.OnSamplingIntervalOrQueueSizeRevised(
                    samplingIntervalChanged, queueSizeChanged);
                if (samplingIntervalChanged)
                {
                    applyChanges |= UpdateQueueSize(Subscription, Template);
                }
                return applyChanges;
            }

            /// <inheritdoc/>
            public override bool TryGetLastMonitoredItemNotifications(
                MonitoredItemNotifications notifications)
            {
                SkipMonitoredItemNotification(); // Key frames should always be sent
                return base.TryGetLastMonitoredItemNotifications(notifications);
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
                notifications.Add(Owner, ToMonitoredItemNotification(new DataValue(statusCode)));
                return true;
            }

            /// <summary>
            /// Process monitored item notification
            /// </summary>
            /// <param name="publishTime"></param>
            /// <param name="monitoredItemNotification"></param>
            /// <param name="notifications"></param>
            /// <returns></returns>
            protected virtual bool ProcessMonitoredItemNotification(DateTimeOffset publishTime,
                MonitoredItemNotification monitoredItemNotification,
                MonitoredItemNotifications notifications)
            {
                if (!SkipMonitoredItemNotification())
                {
                    notifications.Add(Owner, ToMonitoredItemNotification(
                        monitoredItemNotification.Value));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Convert to monitored item notifications
            /// </summary>
            /// <param name="dataValue"></param>
            /// <param name="overflow"></param>
            /// <returns></returns>
            protected MonitoredItemNotificationModel ToMonitoredItemNotification(
                DataValue dataValue, int? overflow = null)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                return new MonitoredItemNotificationModel
                {
                    Id = Template.DataSetFieldId ?? string.Empty,
                    DataSetFieldName = Template.DisplayName,
                    DataSetName = Template.DisplayName,
                    NodeId = NodeId,
                    PathFromRoot = TheResolvedRelativePath,
                    Value = dataValue,
                    Flags = 0,
                    Overflow = overflow ?? (dataValue.StatusCode.Overflow ? 1 : 0),
                    SequenceNumber = GetNextSequenceNumber()
                };
            }

            /// <summary>
            /// Whether to skip monitored item notification
            /// </summary>
            /// <returns></returns>
            public virtual bool SkipMonitoredItemNotification()
            {
                // This will update that first value has been processed.
                var last = Interlocked.Exchange(ref _skipDataChangeNotification,
                    (int)SkipSetting.DontSkip);
                return last == (int)SkipSetting.Skip;
            }

            /// <summary>
            /// Try set skip first setting. We allow updating while first value
            /// is not yet processed, which is the case if skip setting is unconfigured.
            /// </summary>
            /// <param name="skipFirst"></param>
            /// <returns></returns>
            public bool TrySetSkipFirst(bool skipFirst)
            {
                if (skipFirst)
                {
                    // We only allow updating first skip setting while unconfigured
                    return Interlocked.CompareExchange(ref _skipDataChangeNotification,
                        (int)SkipSetting.Skip,
                        (int)SkipSetting.Unconfigured) == (int)SkipSetting.Unconfigured;
                }
                // Unset skip setting if it was configured but first message was not yet processed
                Interlocked.CompareExchange(ref _skipDataChangeNotification,
                    (int)SkipSetting.Unconfigured, (int)SkipSetting.Skip);

                return true;
            }

            enum SkipSetting
            {
                /// <summary> Default </summary>
                DontSkip,
                /// <summary> Skip first value </summary>
                Skip,
                /// <summary> Configuration not applied yet </summary>
                Unconfigured,
            }

            private volatile int _skipDataChangeNotification = (int)SkipSetting.Unconfigured;
            private readonly Guid _fieldId = Guid.NewGuid();
            private bool _registeredForReading;
        }
    }
}
