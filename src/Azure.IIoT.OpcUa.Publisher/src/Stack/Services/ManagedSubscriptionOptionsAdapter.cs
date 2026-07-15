// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License. See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using Opc.Ua.Client.Subscriptions.MonitoredItems;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using ManagedSubscriptionOptions = Opc.Ua.Client.Subscriptions.SubscriptionOptions;

    /// <summary>
    /// Translates Publisher subscription templates to the public V2
    /// subscription options surface.
    /// </summary>
    internal static class ManagedSubscriptionOptionsAdapter
    {
        /// <summary>
        /// Documents every Publisher subscription option's V2 adapter
        /// behavior. Entries marked PublisherOwned remain in the Publisher
        /// composition because V2 deliberately has no equivalent public knob.
        /// </summary>
        internal static IReadOnlyDictionary<string, string> OptionBehaviors { get; } =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                [nameof(OpcUaSubscriptionOptions.DefaultHeartbeatBehavior)] =
                    "PublisherOwned: per-item heartbeat scheduler.",
                [nameof(OpcUaSubscriptionOptions.DefaultHeartbeatInterval)] =
                    "PublisherOwned: per-item heartbeat scheduler.",
                [nameof(OpcUaSubscriptionOptions.DefaultSkipFirst)] =
                    "Mapped: adapter runtime binding suppresses the first data change.",
                [nameof(OpcUaSubscriptionOptions.DefaultRepublishAfterTransfer)] =
                    "PublisherOwned: V2 exposes transfer state but no republish-after-transfer policy.",
                [nameof(OpcUaSubscriptionOptions.DefaultDiscardNew)] =
                    "Mapped: inverted into MonitoredItemOptions.DiscardOldest.",
                [nameof(OpcUaSubscriptionOptions.DefaultSamplingInterval)] =
                    "Mapped: MonitoredItemOptions.SamplingInterval.",
                [nameof(OpcUaSubscriptionOptions.DefaultPublishingInterval)] =
                    "Mapped: SubscriptionOptions.PublishingInterval.",
                [nameof(OpcUaSubscriptionOptions.MaxMonitoredItemPerSubscription)] =
                    "Mapped: SubscriptionOptions.MaxMonitoredItemsPerPartition.",
                [nameof(OpcUaSubscriptionOptions.MaxSubscriptionPartitions)] =
                    "Mapped: SubscriptionOptions.MaxPartitionCount; null maps to zero (unbounded).",
                [nameof(OpcUaSubscriptionOptions.DefaultKeepAliveCount)] =
                    "Mapped: SubscriptionOptions.KeepAliveCount.",
                [nameof(OpcUaSubscriptionOptions.DefaultLifeTimeCount)] =
                    "Mapped: SubscriptionOptions.LifetimeCount.",
                [nameof(OpcUaSubscriptionOptions.EnableImmediatePublishing)] =
                    "Gap: V2 PublishingEnabled is persistent; the cutover must serialize disabled creation, item synchronization, and active-item steady state.",
                [nameof(OpcUaSubscriptionOptions.EnableSequentialPublishing)] =
                    "PublisherOwned: V2 publishes in sequence without a public toggle.",
                [nameof(OpcUaSubscriptionOptions.ResolveDisplayName)] =
                    "PublisherOwned: Publisher resolution occurs before V2 item registration.",
                [nameof(OpcUaSubscriptionOptions.DefaultQueueSize)] =
                    "Mapped: MonitoredItemOptions.QueueSize.",
                [nameof(OpcUaSubscriptionOptions.AutoSetQueueSizes)] =
                    "Mapped: MonitoredItemOptions.AutoSetQueueSize.",
                [nameof(OpcUaSubscriptionOptions.UseDeferredAcknoledgements)] =
                    "PublisherOwned: V2 owns acknowledgement scheduling.",
                [nameof(OpcUaSubscriptionOptions.DefaultSamplingUsingCyclicRead)] =
                    "PublisherOwned: adapter routes data callbacks to cyclic-read subscribers.",
                [nameof(OpcUaSubscriptionOptions.DefaultCyclicReadMaxAge)] =
                    "PublisherOwned: cyclic reads are a Publisher sampling mode.",
                [nameof(OpcUaSubscriptionOptions.DefaultRebrowsePeriod)] =
                    "PublisherOwned: model-change rebrowse belongs to Publisher browsing.",
                [nameof(OpcUaSubscriptionOptions.DefaultDataChangeTrigger)] =
                    "Mapped: DataChangeFilter on MonitoredItemOptions.",
                [nameof(OpcUaSubscriptionOptions.FetchOpcBrowsePathFromRoot)] =
                    "PublisherOwned: path resolution occurs before V2 item registration.",
                [nameof(OpcUaSubscriptionOptions.DefaultWatchdogBehavior)] =
                    "PublisherOwned: Publisher watchdog consumes V2 lifecycle callbacks.",
                [nameof(OpcUaSubscriptionOptions.DefaultMonitoredItemWatchdogTimeout)] =
                    "PublisherOwned: Publisher watchdog consumes V2 lifecycle callbacks.",
                [nameof(OpcUaSubscriptionOptions.DefaultMonitoredItemWatchdogCondition)] =
                    "PublisherOwned: Publisher watchdog consumes V2 lifecycle callbacks.",
                [nameof(OpcUaSubscriptionOptions.SubscriptionErrorRetryDelay)] =
                    "PublisherOwned: V2 lifecycle state is the retry signal.",
                [nameof(OpcUaSubscriptionOptions.SubscriptionManagementIntervalDuration)] =
                    "PublisherOwned: V2 applies desired state asynchronously.",
                [nameof(OpcUaSubscriptionOptions.BadMonitoredItemRetryDelayDuration)] =
                    "PublisherOwned: V2 reacts to BadTooManyMonitoredItems by partitioning.",
                [nameof(OpcUaSubscriptionOptions.InvalidMonitoredItemRetryDelayDuration)] =
                    "PublisherOwned: Publisher controls invalid-item retry timing.",
                [nameof(OpcUaSubscriptionOptions.BadMonitoredItemRetryDelayDurationMax)] =
                    "PublisherOwned: Publisher controls retry backoff.",
                [nameof(OpcUaSubscriptionOptions.InvalidMonitoredItemRetryDelayDurationMax)] =
                    "PublisherOwned: Publisher controls retry backoff."
            });

        /// <summary>
        /// Translate a Publisher subscription template to V2 options.
        /// </summary>
        /// <param name="template">The Publisher subscription template.</param>
        /// <param name="options">The Publisher defaults.</param>
        /// <returns>The V2 options snapshot.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when an explicit partition cap is zero.
        /// </exception>
        internal static ManagedSubscriptionOptions ToManagedOptions(
            SubscriptionModel template, OpcUaSubscriptionOptions options)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(options);

            if (options.MaxSubscriptionPartitions is 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options),
                    "An explicit V2 partition cap must be positive. Use null for unbounded partitioning.");
            }

            var publishingInterval = template.PublishingInterval
                ?? options.DefaultPublishingInterval
                ?? TimeSpan.FromSeconds(1);
            var keepAliveCount = template.KeepAliveCount
                ?? options.DefaultKeepAliveCount
                ?? GetDefaultKeepAliveCount(publishingInterval);

            return new ManagedSubscriptionOptions
            {
                Disabled = false,
                PublishingInterval = publishingInterval,
                KeepAliveCount = keepAliveCount,
                LifetimeCount = template.LifetimeCount
                    ?? options.DefaultLifeTimeCount
                    ?? GetDefaultLifetimeCount(publishingInterval),
                Priority = template.Priority ?? 0,
                MaxNotificationsPerPublish = template.MaxNotificationsPerPublish ?? 0,
                PublishingEnabled = template.EnableImmediatePublishing
                    ?? options.EnableImmediatePublishing
                    ?? false,
                MinLifetimeInterval = TimeSpan.Zero,
                SendInitialValuesOnTransfer = false,
                DisableUnboundedItemMode = false,
                MaxMonitoredItemsPerPartition =
                    options.MaxMonitoredItemPerSubscription is 0 ? null :
                        options.MaxMonitoredItemPerSubscription,
                // V2 uses zero as its public unbounded sentinel. Its default is
                // deliberately not inherited: Publisher's null must not silently
                // become the V2 default cap of 32.
                MaxPartitionCount = options.MaxSubscriptionPartitions ?? 0
            };
        }

        /// <summary>
        /// Translate a Publisher item template to V2 monitored item options.
        /// </summary>
        /// <param name="template">The Publisher item template.</param>
        /// <param name="options">The Publisher defaults.</param>
        /// <param name="codec">The Publisher value codec.</param>
        /// <param name="affinity">The V2 partition affinity for this item.</param>
        /// <param name="triggeredByNames">The V2 names that trigger this item.</param>
        /// <returns>The V2 options snapshot.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the item node id cannot be converted.
        /// </exception>
        internal static MonitoredItemOptions ToManagedOptions(
            BaseMonitoredItemModel template, OpcUaSubscriptionOptions options,
            IVariantEncoder codec, string? affinity = null,
            IReadOnlyList<string>? triggeredByNames = null)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(codec);

            var effective = template.SetDefaults(options);
            var nodeId = effective.StartNodeId.ToNodeId(codec.Context);
            if (NodeIdCompat.IsNull(nodeId))
            {
                throw new ArgumentException("The monitored item has an invalid start node id.",
                    nameof(template));
            }

            var monitoredItemOptions = new MonitoredItemOptions
            {
                StartNodeId = nodeId,
                AttributeId = (uint)(effective.AttributeId ?? GetDefaultAttribute(effective)),
                MonitoringMode = effective.MonitoringMode.ToStackType()
                    ?? Opc.Ua.MonitoringMode.Reporting,
                QueueSize = effective.QueueSize ?? GetDefaultQueueSize(effective),
                AutoSetQueueSize = effective.AutoSetQueueSize ?? false,
                DiscardOldest = !(effective.DiscardNew ?? false),
                Affinity = affinity,
                TriggeredByNames = triggeredByNames ?? []
            };

            return effective switch
            {
                DataMonitoredItemModel data => monitoredItemOptions with
                {
                    IndexRange = data.IndexRange,
                    SamplingInterval = data.SamplingInterval
                        ?? options.DefaultSamplingInterval
                        ?? TimeSpan.FromSeconds(1),
                    Filter = (MonitoringFilter?)data.DataChangeFilter.ToStackModel()
                        ?? data.AggregateFilter.ToStackModel(codec.Context)
                },
                EventMonitoredItemModel events => monitoredItemOptions with
                {
                    SamplingInterval = TimeSpan.Zero,
                    Filter = events.ConditionHandling?.SnapshotInterval != null ?
                        CreateConditionFilter(codec.Decode(events.EventFilter)) :
                        codec.Decode(events.EventFilter)
                },
                MonitoredAddressSpaceModel => monitoredItemOptions with
                {
                    SamplingInterval = TimeSpan.Zero,
                    AttributeId = Attributes.EventNotifier,
                    Filter = CreateModelChangeFilter()
                },
                _ => monitoredItemOptions
            };
        }

        private static uint GetDefaultKeepAliveCount(TimeSpan publishingInterval)
        {
            return publishingInterval >= TimeSpan.FromSeconds(60) ? 1u :
                publishingInterval >= TimeSpan.FromSeconds(5) ? 2u : 0u;
        }

        private static uint GetDefaultLifetimeCount(TimeSpan publishingInterval)
        {
            return publishingInterval >= TimeSpan.FromSeconds(60) ? 2u :
                publishingInterval >= TimeSpan.FromSeconds(30) ? 3u :
                publishingInterval >= TimeSpan.FromSeconds(5) ? 5u : 0u;
        }

        private static NodeAttribute GetDefaultAttribute(BaseMonitoredItemModel template)
        {
            return template is DataMonitoredItemModel ? NodeAttribute.Value :
                NodeAttribute.EventNotifier;
        }

        private static uint GetDefaultQueueSize(BaseMonitoredItemModel template)
        {
            return template is DataMonitoredItemModel ? 1u : 0u;
        }

        private static EventFilter CreateModelChangeFilter()
        {
            return new EventFilter
            {
                SelectClauses =
                [
                    new SimpleAttributeOperand
                    {
                        BrowsePath = [new QualifiedName(BrowseNames.EventType)],
                        TypeDefinitionId = ObjectTypeIds.BaseModelChangeEventType,
                        AttributeId = Attributes.NodeId
                    },
                    new SimpleAttributeOperand
                    {
                        BrowsePath = [new QualifiedName(BrowseNames.Changes)],
                        TypeDefinitionId = ObjectTypeIds.GeneralModelChangeEventType,
                        AttributeId = Attributes.Value
                    }
                ],
                WhereClause = new ContentFilter
                {
                    Elements =
                    [
                        new ContentFilterElement
                        {
                            FilterOperator = FilterOperator.OfType,
                            FilterOperands =
                            [
                                new ExtensionObject(new LiteralOperand(
                                    new Variant(ObjectTypeIds.BaseModelChangeEventType)))
                            ]
                        }
                    ]
                }
            };
        }

        private static EventFilter CreateConditionFilter(EventFilter? filter)
        {
            filter ??= FilterEncoderEx.GetDefaultEventFilter();
            var clauses = filter.SelectClauses;
            AddIfMissing(ObjectTypeIds.BaseEventType,
                [new QualifiedName(BrowseNames.EventType)], Attributes.Value);
            AddIfMissing(ObjectTypeIds.ConditionType, [], Attributes.NodeId);
            AddIfMissing(ObjectTypeIds.ConditionType,
                [new QualifiedName(BrowseNames.Retain)], Attributes.Value);
            return filter;

            void AddIfMissing(NodeId typeDefinitionId, ArrayOf<QualifiedName> browsePath,
                uint attributeId)
            {
                foreach (var clause in clauses)
                {
                    if (clause.TypeDefinitionId == typeDefinitionId &&
                        clause.AttributeId == attributeId &&
                        HasSameBrowsePath(clause.BrowsePath, browsePath))
                    {
                        return;
                    }
                }
                filter.SelectClauses = filter.SelectClauses.AddItem(new SimpleAttributeOperand
                {
                    TypeDefinitionId = typeDefinitionId,
                    BrowsePath = browsePath,
                    AttributeId = attributeId
                });
                clauses = filter.SelectClauses;

                static bool HasSameBrowsePath(ArrayOf<QualifiedName> left,
                    ArrayOf<QualifiedName> right)
                {
                    if (left.Count != right.Count)
                    {
                        return false;
                    }
                    for (var index = 0; index < left.Count; index++)
                    {
                        if (left[index] != right[index])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }
    }
}
