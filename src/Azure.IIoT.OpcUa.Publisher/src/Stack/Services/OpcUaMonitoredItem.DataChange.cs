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
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;

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
                        (v, context) => TheResolvedNodeId = EffectiveNodeId
                            = v.AsString(context, Template.NamespaceFormat) ?? string.Empty) : null;

            /// <inheritdoc/>
            public override (string NodeId, UpdateNodeId Update)? Register
                => Template.RegisterRead && !string.IsNullOrEmpty(TheResolvedNodeId) ?
                    (TheResolvedNodeId, (v, context) => EffectiveNodeId
                            = v.AsString(context, Template.NamespaceFormat) ?? string.Empty) : null;

            /// <summary>
            /// Monitored item as data
            /// </summary>
            public DataMonitoredItemModel Template { get; protected internal set; }

            /// <summary>
            /// Effective node id
            /// </summary>
            protected string EffectiveNodeId { get; set; }

            /// <summary>
            /// Resolved node id
            /// </summary>
            protected string TheResolvedNodeId { get; private set; }

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
            /// <param name="template"></param>
            /// <param name="logger"></param>
            public DataChange(DataMonitoredItemModel template,
                ILogger<DataChange> logger)
                : base(logger, template.Order)
            {
                Template = template;

                //
                // We also track the resolved node id so we distinguish it
                // from the registered and thus effective node id
                //
                TheResolvedNodeId = template.StartNodeId;
                EffectiveNodeId = template.StartNodeId;
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
                EffectiveNodeId = item.EffectiveNodeId;
                Template = item.Template;
                _fieldId = item._fieldId;
                _skipDataChangeNotification = item._skipDataChangeNotification;
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
                if ((Template.Id ?? string.Empty) !=
                    (dataItem.Template.Id ?? string.Empty))
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
                return base.Equals(obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return HashCode.Combine(base.GetHashCode(),
                    nameof(DataChange),
                    Template.Id ?? string.Empty,
                    Template.RelativePath,
                    Template.StartNodeId,
                    Template.RegisterRead,
                    Template.IndexRange,
                    Template.AttributeId);
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Data Item '{Template.StartNodeId}' with server id {RemoteId} - " +
                    $"{(Status?.Created == true ? "" : "not ")}created";
            }

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription, IOpcUaSession session)
            {
                var nodeId = EffectiveNodeId.ToNodeId(session.MessageContext);
                if (Opc.Ua.NodeId.IsNull(nodeId))
                {
                    return false;
                }

                DisplayName = Template.GetMonitoredItemName();
                AttributeId = (uint)(Template.AttributeId ??
                    (NodeAttribute)Attributes.Value);
                IndexRange = Template.IndexRange;
                StartNodeId = nodeId;
                MonitoringMode = Template.MonitoringMode.ToStackType()
                    ?? Opc.Ua.MonitoringMode.Reporting;
                QueueSize = Template.QueueSize;
                SamplingInterval = (int)Template.SamplingInterval.
                    GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;
                Filter = Template.DataChangeFilter.ToStackModel() ??
                    (MonitoringFilter?)Template.AggregateFilter.ToStackModel(
                        session.MessageContext);
                DiscardOldest = !(Template.DiscardNew ?? false);
                Valid = true;

                if (!TrySetSkipFirst(Template.SkipFirst))
                {
                    Debug.Fail("Unexpected: Failed to set skip first setting.");
                }
                return base.AddTo(subscription, session);
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber,
                DateTime timestamp, IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                if (evt is MonitoredItemNotification min &&
                    base.TryGetMonitoredItemNotifications(sequenceNumber, timestamp, evt, notifications))
                {
                    return ProcessMonitoredItemNotification(sequenceNumber, timestamp, min, notifications);
                }
                return false;
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session)
            {
                if (item is not DataChange model || !Valid)
                {
                    return false;
                }

                var itemChange = MergeWith(Template, model.Template, out var updated);
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
                if (model.Template.SkipFirst != Template.SkipFirst)
                {
                    Template = Template with { SkipFirst = model.Template.SkipFirst };

                    if (model.TrySetSkipFirst(model.Template.SkipFirst))
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
            public override bool TryGetLastMonitoredItemNotifications(uint sequenceNumber,
                IList<MonitoredItemNotificationModel> notifications)
            {
                SkipMonitoredItemNotification(); // Key frames should always be sent
                return base.TryGetLastMonitoredItemNotifications(sequenceNumber,
                    notifications);
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
                notifications.Add(ToMonitoredItemNotification(sequenceNumber,
                    new DataValue(statusCode)));
                return true;
            }

            /// <summary>
            /// Process monitored item notification
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <param name="timestamp"></param>
            /// <param name="monitoredItemNotification"></param>
            /// <param name="notifications"></param>
            /// <returns></returns>
            protected virtual bool ProcessMonitoredItemNotification(uint sequenceNumber,
                DateTime timestamp, MonitoredItemNotification monitoredItemNotification,
                IList<MonitoredItemNotificationModel> notifications)
            {
                if (!SkipMonitoredItemNotification())
                {
                    notifications.Add(ToMonitoredItemNotification(sequenceNumber,
                        monitoredItemNotification.Value));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Convert to monitored item notifications
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <param name="dataValue"></param>
            /// <param name="overflow"></param>
            /// <returns></returns>
            protected MonitoredItemNotificationModel ToMonitoredItemNotification(
                uint sequenceNumber, DataValue dataValue, int? overflow = null)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                return new MonitoredItemNotificationModel
                {
                    Order = Order,
                    MonitoredItemId = Template.GetMonitoredItemId(),
                    FieldId = Template.GetMonitoredItemName(),
                    Context = Template.Context,
                    NodeId = TheResolvedNodeId,
                    Value = dataValue,
                    Flags = 0,
                    Overflow = overflow ?? (dataValue.StatusCode.Overflow ? 1 : 0),
                    SequenceNumber = sequenceNumber
                };
            }

            /// <summary>
            /// Whether to skip monitored item notification
            /// </summary>
            /// <returns></returns>
            public bool SkipMonitoredItemNotification()
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
        }
    }
}
