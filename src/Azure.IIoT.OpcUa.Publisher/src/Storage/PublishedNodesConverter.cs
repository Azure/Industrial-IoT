// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Storage
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Azure.IoT.Edge.Services;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Published nodes
    /// </summary>
    public sealed class PublishedNodesConverter
    {
        /// <summary>
        /// Create converter
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serializer"></param>
        /// <param name="options"></param>
        /// <param name="cryptoProvider"></param>
        public PublishedNodesConverter(ILogger<PublishedNodesConverter> logger,
            IJsonSerializer serializer, IOptions<PublisherOptions> options,
            IIoTEdgeWorkloadApi? cryptoProvider = null)
        {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            _cryptoProvider = cryptoProvider;
            _forceCredentialEncryption =
                options.Value.ForceCredentialEncryption ?? false;
            _scaleTestCount =
                Math.Max(1, options.Value.ScaleTestCount ?? 0);
            _maxNodesPerDataSet = options.Value.MaxNodesPerDataSet <= 0
                ? int.MaxValue : options.Value.MaxNodesPerDataSet;
            _noPublishingIntervalGrouping =
                options.Value.IgnoreConfiguredPublishingIntervals ?? false;
        }

        /// <summary>
        /// Read monitored item job from reader
        /// </summary>
        /// <param name="publishedNodesContent"></param>
        /// <returns></returns>
        /// <exception cref="SerializerException"></exception>
        public IEnumerable<PublishedNodesEntryModel> Read(string publishedNodesContent)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogDebug("Reading and validating published nodes file...");
            try
            {
                var items = _serializer.Deserialize<List<PublishedNodesEntryModel>>(publishedNodesContent)
                    ?? throw new SerializerException("Published nodes files, malformed.");

                _logger.LogInformation("Read {Count} entry models from published nodes file in {Elapsed}",
                    items.Count, sw.Elapsed);
                return items;
            }
            finally
            {
                sw.Stop();
            }
        }

        /// <summary>
        /// Convert from writer group job model to published nodes entries
        /// </summary>
        /// <param name="version"></param>
        /// <param name="lastChanged"></param>
        /// <param name="items"></param>
        /// <param name="preferTimeSpan"></param>
        /// <returns></returns>
        public IEnumerable<PublishedNodesEntryModel> ToPublishedNodes(uint version, DateTimeOffset lastChanged,
            IEnumerable<WriterGroupModel> items, bool preferTimeSpan = true)
        {
            if (items == null)
            {
                return Enumerable.Empty<PublishedNodesEntryModel>();
            }
            var sw = Stopwatch.StartNew();
            try
            {
                var publishedNodesEntries = items
                    .Where(group => group?.DataSetWriters?.Count > 0)
                    .SelectMany(group => group.DataSetWriters!
                        .Where(writer =>
                               writer.DataSet?.DataSetSource?.PublishedVariables?.PublishedData != null
                            || writer.DataSet?.DataSetSource?.PublishedEvents?.PublishedData != null)
                        .Select(writer => (WriterGroup: group, Writer: writer)))
                    .Select(item => AddConnectionModel(item.Writer.DataSet?.DataSetSource?.Connection,
                        new PublishedNodesEntryModel
                        {
                            NodeId = null,
                            Version = version,
                            LastChangeDateTime = lastChanged,
                            DataSetClassId = item.Writer.DataSet?.DataSetMetaData?.DataSetClassId ?? Guid.Empty,
                            DataSetDescription = item.Writer.DataSet?.DataSetMetaData?.Description,
                            DataSetKeyFrameCount = item.Writer.KeyFrameCount,
                            MessagingMode = item.WriterGroup.HeaderLayoutUri == null ? null :
                                Enum.Parse<MessagingMode>(item.WriterGroup.HeaderLayoutUri), // TODO: Make safe
                            MessageEncoding = item.WriterGroup.MessageType,
                            WriterGroupTransport = item.WriterGroup.Transport,
                            WriterGroupQualityOfService = item.WriterGroup.Publishing?.RequestedDeliveryGuarantee,
                            WriterGroupMessageTtlTimepan = item.WriterGroup.Publishing?.Ttl,
                            WriterGroupMessageRetention = item.WriterGroup.Publishing?.Retain,
                            WriterGroupPartitions = item.WriterGroup.PublishQueuePartitions,
                            WriterGroupQueueName = item.WriterGroup.Publishing?.QueueName,
                            SendKeepAliveDataSetMessages = item.Writer.DataSet?.SendKeepAlive ?? false,
                            DataSetExtensionFields = item.Writer.DataSet?.ExtensionFields,
                            MetaDataUpdateTimeTimespan = item.Writer.MetaDataUpdateTime,
                            QueueName = item.Writer.Publishing?.QueueName,
                            QualityOfService = item.Writer.Publishing?.RequestedDeliveryGuarantee,
                            MessageTtlTimespan = item.Writer.Publishing?.Ttl,
                            MessageRetention = item.Writer.Publishing?.Retain,
                            MetaDataQueueName = item.Writer.MetaData?.QueueName,
                            MetaDataUpdateTime = null,
                            BatchTriggerIntervalTimespan = item.WriterGroup.PublishingInterval,
                            BatchTriggerInterval = null,
                            DataSetSamplingInterval = null,
                            DataSetSamplingIntervalTimespan =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.DefaultSamplingInterval,
                            DefaultHeartbeatInterval = null,
                            DefaultHeartbeatIntervalTimespan =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.DefaultHeartbeatInterval,
                            DefaultHeartbeatBehavior =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.DefaultHeartbeatBehavior,
                            Priority =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.Priority,
                            MaxKeepAliveCount =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.MaxKeepAliveCount,
                            DataSetFetchDisplayNames =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.ResolveDisplayName,
                            RepublishAfterTransfer =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.RepublishAfterTransfer,
                            OpcNodeWatchdogTimespan =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.MonitoredItemWatchdogTimeout,
                            OpcNodeWatchdogCondition =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.MonitoredItemWatchdogCondition,
                            DataSetWriterWatchdogBehavior =
                                item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.WatchdogBehavior,
                            BatchSize = item.WriterGroup.NotificationPublishThreshold,
                            DataSetName = item.Writer.DataSet?.Name,
                            DataSetWriterGroup =
                                item.WriterGroup.Name == Constants.DefaultWriterGroupName ? null : item.WriterGroup.Name,
                            DataSetWriterId = item.Writer.DataSetWriterName,
                            DataSetRouting = item.Writer.DataSet?.Routing,
                            DataSetPublishingInterval = null,
                            DataSetPublishingIntervalTimespan = null,
                            OpcNodes = ToOpcNodes(item.Writer.DataSet?.DataSetSource?.SubscriptionSettings,
                                    item.Writer.DataSet?.DataSetSource?.PublishedVariables,
                                    item.Writer.DataSet?.DataSetSource?.PublishedEvents, preferTimeSpan, false)?
                                .ToList() ?? new List<OpcNodeModel>(),
                            // ...

                            // Added by Add connection information
                            OpcAuthenticationMode = OpcAuthenticationMode.Anonymous,
                            OpcAuthenticationUsername = null,
                            OpcAuthenticationPassword = null,
                            EndpointUrl = string.Empty,
                            UseSecurity = false,
                            UseReverseConnect = null,
                            DisableSubscriptionTransfer = null,
                            DumpConnectionDiagnostics = null,
                            EndpointSecurityPolicy = null,
                            EndpointSecurityMode = null,
                            EncryptedAuthPassword = null,
                            EncryptedAuthUsername = null
                        }));

                // Coalesce into unique nodes entry data set groups
                // TODO: We should start with the grouping earlier
                return publishedNodesEntries
                    .GroupBy(item => item,
                        new FuncCompare<PublishedNodesEntryModel>((x, y) => x!.HasSameDataSet(y!)))
                    .Select(group =>
                    {
                        group.Key.OpcNodes = group
                            .Where(g => g.OpcNodes != null)
                            .SelectMany(g => g.OpcNodes!)
                            .ToList();
                        return group.Key;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed to convert the published nodes.");
                return Enumerable.Empty<PublishedNodesEntryModel>();
            }
            finally
            {
                _logger.LogInformation("Converted published nodes entry models to jobs in {Elapsed}",
                    sw.Elapsed);
                sw.Stop();
            }

            static IEnumerable<OpcNodeModel>? ToOpcNodes(PublishedDataSetSettingsModel? subscriptionSettings,
                PublishedDataItemsModel? publishedVariables, PublishedEventItemsModel? publishedEvents, bool preferTimeSpan,
                bool skipTriggeringNodes)
            {
                if (publishedVariables == null && publishedEvents == null)
                {
                    return null;
                }
                return (publishedVariables?.PublishedData ?? Enumerable.Empty<PublishedDataSetVariableModel>())
                    .Select(variable => new OpcNodeModel
                    {
                        DeadbandType = variable.DeadbandType,
                        DeadbandValue = variable.DeadbandValue,
                        DataSetClassFieldId = variable.DataSetClassFieldId,
                        Id = variable.PublishedVariableNodeId,
                        DisplayName = variable.PublishedVariableDisplayName,
                        DataSetFieldId = variable.Id,
                        AttributeId = variable.Attribute,
                        IndexRange = variable.IndexRange,
                        RegisterNode = variable.RegisterNodeForSampling,
                        FetchDisplayName = variable.ReadDisplayNameFromNode,
                        BrowsePath = variable.BrowsePath,
                        UseCyclicRead = variable.SamplingUsingCyclicRead,
                        CyclicReadMaxAge = preferTimeSpan ? null : (int?)variable.CyclicReadMaxAge?.TotalMilliseconds,
                        CyclicReadMaxAgeTimespan = !preferTimeSpan ? null : variable.CyclicReadMaxAge,
                        DiscardNew = variable.DiscardNew,
                        QueueSize = variable.ServerQueueSize,
                        DataChangeTrigger = variable.DataChangeTrigger,
                        HeartbeatBehavior = variable.HeartbeatBehavior,
                        HeartbeatInterval = preferTimeSpan ? null : (int?)variable.HeartbeatInterval?.TotalSeconds,
                        HeartbeatIntervalTimespan = !preferTimeSpan ? null : variable.HeartbeatInterval,
                        OpcSamplingInterval = preferTimeSpan ? null : (int?)variable.SamplingIntervalHint?.TotalMilliseconds,
                        OpcSamplingIntervalTimespan = !preferTimeSpan ? null : variable.SamplingIntervalHint,
                        OpcPublishingInterval = preferTimeSpan ? null : (int?)
                            subscriptionSettings?.PublishingInterval?.TotalMilliseconds,
                        OpcPublishingIntervalTimespan = !preferTimeSpan ? null :
                            subscriptionSettings?.PublishingInterval,
                        SkipFirst = variable.SkipFirst,
                        TriggeredNodes = skipTriggeringNodes ? null : ToOpcNodes(subscriptionSettings,
                            variable.Triggering?.PublishedVariables,
                            variable.Triggering?.PublishedEvents, preferTimeSpan, true)?.ToList(),
                        Topic = variable.Publishing?.QueueName,
                        QualityOfService = variable.Publishing?.RequestedDeliveryGuarantee,

                        // MonitoringMode = variable.MonitoringMode,
                        // ...

                        ExpandedNodeId = null,
                        ConditionHandling = null,
                        ModelChangeHandling = null,
                        EventFilter = null
                    })
                    .Concat((publishedEvents?.PublishedData ?? Enumerable.Empty<PublishedDataSetEventModel>())
                    .Select(evt => new OpcNodeModel
                    {
                        Id = evt.EventNotifier,
                        EventFilter = new EventFilterModel
                        {
                            TypeDefinitionId = evt.TypeDefinitionId,
                            SelectClauses = evt.SelectedFields?.Select(s => s.Clone()).ToList(),
                            WhereClause = evt.Filter.Clone()
                        },
                        ConditionHandling = evt.ConditionHandling.Clone(),
                        ModelChangeHandling = evt.ModelChangeHandling.Clone(),
                        DataSetFieldId = evt.Id,
                        DisplayName = evt.PublishedEventName,
                        FetchDisplayName = evt.ReadEventNameFromNode,
                        BrowsePath = evt.BrowsePath,
                        DiscardNew = evt.DiscardNew,
                        QueueSize = evt.QueueSize,
                        TriggeredNodes = skipTriggeringNodes ? null : ToOpcNodes(subscriptionSettings,
                            evt.Triggering?.PublishedVariables,
                            evt.Triggering?.PublishedEvents, preferTimeSpan, true)?.ToList(),
                        Topic = evt.Publishing?.QueueName,
                        QualityOfService = evt.Publishing?.RequestedDeliveryGuarantee,

                        // MonitoringMode = evt.MonitoringMode,
                        // ...
                        DeadbandType = null,
                        DataChangeTrigger = null,
                        DataSetClassFieldId = Guid.Empty,
                        DeadbandValue = null,
                        ExpandedNodeId = null,
                        HeartbeatInterval = null,
                        HeartbeatBehavior = null,
                        HeartbeatIntervalTimespan = null,
                        OpcSamplingInterval = null,
                        OpcSamplingIntervalTimespan = null,
                        CyclicReadMaxAgeTimespan = null,
                        CyclicReadMaxAge = null,
                        AttributeId = null,
                        RegisterNode = null,
                        UseCyclicRead = null,
                        IndexRange = null,
                        OpcPublishingInterval = preferTimeSpan ? null : (int?)
                            subscriptionSettings?.PublishingInterval?.TotalMilliseconds,
                        OpcPublishingIntervalTimespan = !preferTimeSpan ? null :
                            subscriptionSettings?.PublishingInterval,
                        SkipFirst = null
                    }));
            }
        }

        /// <summary>
        /// Convert published nodes configuration to Writer group jobs
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        public IEnumerable<WriterGroupModel> ToWriterGroups(IEnumerable<PublishedNodesEntryModel> entries)
        {
            if (entries == null)
            {
                return Enumerable.Empty<WriterGroupModel>();
            }
            var sw = Stopwatch.StartNew();
            try
            {
                if (!_noPublishingIntervalGrouping)
                {
                    //
                    // Split all entries by the publishing interval in the nodes using the entry publishing
                    // interval as default. To prevent entries with no nodes to be removed here a dummy
                    // entry is added to the list of nodes and removed again from the group entries selected.
                    //
                    entries = entries
                        .SelectMany(entry => GetNodeModels(entry, _scaleTestCount)
                            .DefaultIfEmpty(kDummyEntry)
                            .GroupBy(n => n.GetNormalizedPublishingInterval(
                                          entry.GetNormalizedDataSetPublishingInterval()))
                            .Select(g => entry with
                            {
                                // Set the publishing interval for this entry at the top
                                DataSetPublishingIntervalTimespan = g.Key,
                                DataSetPublishingInterval = null,
                                OpcNodes = g
                                    .Where(n => n != kDummyEntry)
                                    .Select(n => n with
                                    {
                                        // Unset all node specific settings.
                                        OpcPublishingIntervalTimespan = null,
                                        OpcPublishingInterval = null
                                    })
                                    .ToList()
                            }));
                }
                return entries
                    //
                    // Now we have entries with nodes that have no publishing interval, group all entries
                    // by group identifier
                    //
                    .Select(entry => (
                        Entry: entry,
                        UniqueGroupId: entry.GetUniqueWriterGroupId()
                     ))
                    .GroupBy(entry => entry.UniqueGroupId)
                    .Select(g => (g.Key, Entries: g.ToList()))
                    //
                    // In each group select the writers using the unique data set writer id which uses the
                    // publishing interval.
                    //
                    .Select(group => (
                        Id: group.Key,
                        Header: group.Entries[0].Entry,
                        Writers: group.Entries
                            .Select(entry => (
                                entry.Entry,
                                UniqueWriterId: entry.Entry.GetUniqueDataSetWriterId()
                             ))
                            .GroupBy(e => e.UniqueWriterId)
                            .Select(w => (w.Key, Writers: w.Select(e => e.Entry).ToList()))
                            .ToList()
                    ))
                    // Now bring it all together into a group with writers and settings
                    .Select(group => new WriterGroupModel
                    {
                        Id = group.Id,
                        MessageType = group.Header.MessageEncoding,
                        Transport = group.Header.WriterGroupTransport,
                        Publishing = new PublishingQueueSettingsModel
                        {
                            RequestedDeliveryGuarantee = group.Header.WriterGroupQualityOfService,
                            QueueName = group.Header.WriterGroupQueueName,
                            Retain = group.Header.WriterGroupMessageRetention,
                            Ttl = group.Header.WriterGroupMessageTtlTimepan
                        },
                        HeaderLayoutUri = group.Header.MessagingMode?.ToString(),
                        Name = group.Header.DataSetWriterGroup,
                        NotificationPublishThreshold = group.Header.BatchSize,
                        PublishQueuePartitions = group.Header.WriterGroupPartitions,
                        PublishingInterval = group.Header.GetNormalizedBatchTriggerInterval(),
                        DataSetWriters = group.Writers
                            .Select(w => (
                                WriterId: w.Key,
                                Header: w.Writers[0],
                                WriterBatches: w.Writers
                                    .SelectMany(w => w.OpcNodes!)
                                    .Distinct(OpcNodeModelEx.Comparer)
                                    .Batch(_maxNodesPerDataSet)
                            // Future: batch in service so it is centralized
                            ))
                            .SelectMany(b => b.WriterBatches // Do we need to materialize here?
                                .DefaultIfEmpty(kDummyEntry.YieldReturn())
                                .Select(n => n.ToList())
                                .Select((nodes, index) => new DataSetWriterModel
                                {
                                    Id = b.WriterId + "_" + index,
                                    DataSetWriterName = b.Header.DataSetWriterId,
                                    MetaDataUpdateTime = b.Header.GetNormalizedMetaDataUpdateTime(),
                                    KeyFrameCount = b.Header.DataSetKeyFrameCount,
                                    Publishing = new PublishingQueueSettingsModel
                                    {
                                        QueueName = b.Header.QueueName,
                                        RequestedDeliveryGuarantee = b.Header.QualityOfService,
                                        Retain = b.Header.MessageRetention,
                                        Ttl = b.Header.MessageTtlTimespan
                                    },
                                    MetaData = new PublishingQueueSettingsModel
                                    {
                                        QueueName = b.Header.MetaDataQueueName,
                                        RequestedDeliveryGuarantee = null,
                                        Retain = true,
                                        Ttl = null
                                    },
                                    DataSet = new PublishedDataSetModel
                                    {
                                        Name = b.Header.DataSetName,
                                        DataSetMetaData = new DataSetMetaDataModel
                                        {
                                            DataSetClassId = b.Header.DataSetClassId,
                                            Description = b.Header.DataSetDescription,
                                            Name = b.Header.DataSetName
                                        },
                                        ExtensionFields = b.Header.DataSetExtensionFields,
                                        SendKeepAlive = b.Header.SendKeepAliveDataSetMessages,
                                        Routing = b.Header.DataSetRouting,
                                        DataSetSource = new PublishedDataSetSourceModel
                                        {
                                            Connection = b.Header.ToConnectionModel(ToCredential),
                                            SubscriptionSettings = new PublishedDataSetSettingsModel
                                            {
                                                MaxKeepAliveCount = b.Header.MaxKeepAliveCount,
                                                RepublishAfterTransfer = b.Header.RepublishAfterTransfer,
                                                MonitoredItemWatchdogTimeout = b.Header.OpcNodeWatchdogTimespan,
                                                MonitoredItemWatchdogCondition = b.Header.OpcNodeWatchdogCondition,
                                                WatchdogBehavior = b.Header.DataSetWriterWatchdogBehavior,
                                                Priority = b.Header.Priority,
                                                ResolveDisplayName = b.Header.DataSetFetchDisplayNames,
                                                DefaultHeartbeatBehavior = b.Header.DefaultHeartbeatBehavior,
                                                DefaultHeartbeatInterval = b.Header.GetNormalizedDefaultHeartbeatInterval(),
                                                DefaultSamplingInterval = b.Header.GetNormalizedDataSetSamplingInterval(),
                                                PublishingInterval = b.Header.GetNormalizedDataSetPublishingInterval(),
                                                MaxNotificationsPerPublish = null,
                                                EnableImmediatePublishing = null,
                                                EnableSequentialPublishing = null,
                                                LifeTimeCount = null,
                                                UseDeferredAcknoledgements = null
                                                // ...
                                            },
                                            PublishedVariables = ToPublishedDataItems(nodes.Where(n => n != kDummyEntry), false),
                                            PublishedEvents = ToPublishedEventItems(nodes.Where(n => n != kDummyEntry), false)
                                        }
                                    },
                                    MessageSettings = null,
                                    DataSetFieldContentMask = null
                                }))
                                .ToList(),
                        KeepAliveTime = null,
                        MaxNetworkMessageSize = null,
                        MessageSettings = null,
                        Priority = null,
                        PublishQueueSize = null,
                        SecurityGroupId = null,
                        SecurityKeyServices = null,
                        SecurityMode = null,
                        LocaleIds = null
                    })
                    .ToList(); // Convert here or else we dont print conversion correctly
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed to convert the published nodes.");
                return Enumerable.Empty<WriterGroupModel>();
            }
            finally
            {
                _logger.LogInformation("Converted published nodes entry models to jobs in {Elapsed}",
                    sw.Elapsed);
                sw.Stop();
            }

            IEnumerable<OpcNodeModel> GetNodeModels(PublishedNodesEntryModel item, int scaleTestCount)
            {
                if (item.OpcNodes != null)
                {
                    foreach (var node in item.OpcNodes)
                    {
                        if (!node.TryGetId(out var id))
                        {
                            _logger.LogError("No node id was configured in the opc node entry - skipping...");
                            continue;
                        }
                        if (scaleTestCount <= 1)
                        {
                            yield return new OpcNodeModel
                            {
                                Id = id,
                                DisplayName = node.DisplayName,
                                DataSetClassFieldId = node.DataSetClassFieldId,
                                DataSetFieldId = node.DataSetFieldId,
                                ExpandedNodeId = node.ExpandedNodeId,
                                // The publishing interval item wins over dataset over global default
                                OpcPublishingIntervalTimespan = node.GetNormalizedPublishingInterval()
                                    ?? item.GetNormalizedDataSetPublishingInterval(),
                                OpcSamplingIntervalTimespan = node.GetNormalizedSamplingInterval(),
                                HeartbeatIntervalTimespan = node.GetNormalizedHeartbeatInterval(),
                                QueueSize = node.QueueSize,
                                DiscardNew = node.DiscardNew,
                                BrowsePath = node.BrowsePath,
                                AttributeId = node.AttributeId,
                                FetchDisplayName = node.FetchDisplayName,
                                IndexRange = node.IndexRange,
                                RegisterNode = node.RegisterNode,
                                UseCyclicRead = node.UseCyclicRead,
                                CyclicReadMaxAgeTimespan = node.GetNormalizedCyclicReadMaxAge(),
                                SkipFirst = node.SkipFirst,
                                DataChangeTrigger = node.DataChangeTrigger,
                                DeadbandType = node.DeadbandType,
                                DeadbandValue = node.DeadbandValue,
                                EventFilter = node.EventFilter,
                                HeartbeatBehavior = node.HeartbeatBehavior,
                                ConditionHandling = node.ConditionHandling,
                                TriggeredNodes = node.TriggeredNodes,
                                QualityOfService = node.QualityOfService,
                                Topic = node.Topic,
                                ModelChangeHandling = node.ModelChangeHandling
                            };
                        }
                        else
                        {
                            for (var i = 0; i < scaleTestCount; i++)
                            {
                                yield return new OpcNodeModel
                                {
                                    Id = id,
                                    DisplayName = !string.IsNullOrEmpty(node.DisplayName) ?
                                        $"{node.DisplayName}_{i}" : null,
                                    DataSetFieldId = node.DataSetFieldId,
                                    DataSetClassFieldId = node.DataSetClassFieldId,
                                    ExpandedNodeId = node.ExpandedNodeId,
                                    HeartbeatIntervalTimespan = node.GetNormalizedHeartbeatInterval(),
                                    // The publishing interval item wins over dataset over global default
                                    OpcPublishingIntervalTimespan = node.GetNormalizedPublishingInterval()
                                        ?? item.GetNormalizedDataSetPublishingInterval(),
                                    OpcSamplingIntervalTimespan = node.GetNormalizedSamplingInterval(),
                                    QueueSize = node.QueueSize,
                                    SkipFirst = node.SkipFirst,
                                    DataChangeTrigger = node.DataChangeTrigger,
                                    BrowsePath = node.BrowsePath,
                                    AttributeId = node.AttributeId,
                                    FetchDisplayName = node.FetchDisplayName,
                                    IndexRange = node.IndexRange,
                                    RegisterNode = node.RegisterNode,
                                    UseCyclicRead = node.UseCyclicRead,
                                    CyclicReadMaxAgeTimespan = node.GetNormalizedCyclicReadMaxAge(),
                                    DeadbandType = node.DeadbandType,
                                    DeadbandValue = node.DeadbandValue,
                                    DiscardNew = node.DiscardNew,
                                    HeartbeatBehavior = node.HeartbeatBehavior,
                                    EventFilter = node.EventFilter,
                                    TriggeredNodes = null,
                                    ConditionHandling = node.ConditionHandling,
                                    QualityOfService = node.QualityOfService,
                                    Topic = node.Topic,
                                    ModelChangeHandling = node.ModelChangeHandling
                                };
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(item.NodeId?.Identifier))
                {
                    yield return new OpcNodeModel
                    {
                        Id = item.NodeId.Identifier,
                        OpcPublishingIntervalTimespan = item.GetNormalizedDataSetPublishingInterval()
                    };
                }
            }

            static PublishedDataItemsModel ToPublishedDataItems(IEnumerable<OpcNodeModel> opcNodes, bool skipTriggering)
            {
                return new PublishedDataItemsModel
                {
                    PublishedData = opcNodes.Where(node => node.EventFilter == null && node.ModelChangeHandling == null)
                    .Select(node => new PublishedDataSetVariableModel
                    {
                        Id = node.DataSetFieldId,
                        PublishedVariableNodeId = node.Id,
                        DataSetClassFieldId = node.DataSetClassFieldId,

                        // At this point in time the next values are ensured to be filled in with
                        // the appropriate value: configured or default
                        PublishedVariableDisplayName = node.DisplayName,
                        SamplingIntervalHint = node.OpcSamplingIntervalTimespan,
                        HeartbeatInterval = node.HeartbeatIntervalTimespan,
                        HeartbeatBehavior = node.HeartbeatBehavior,
                        ServerQueueSize = node.QueueSize,
                        DiscardNew = node.DiscardNew,
                        SamplingUsingCyclicRead = node.UseCyclicRead,
                        CyclicReadMaxAge = node.CyclicReadMaxAgeTimespan,
                        Attribute = node.AttributeId,
                        IndexRange = node.IndexRange,
                        RegisterNodeForSampling = node.RegisterNode,
                        BrowsePath = node.BrowsePath,
                        ReadDisplayNameFromNode = node.FetchDisplayName,
                        MonitoringMode = null,
                        SubstituteValue = null,
                        SkipFirst = node.SkipFirst,
                        DataChangeTrigger = node.DataChangeTrigger,
                        DeadbandValue = node.DeadbandValue,
                        DeadbandType = node.DeadbandType,
                        Publishing = node.Topic == null && node.QualityOfService == null
                            ? null : new PublishingQueueSettingsModel
                            {
                                QueueName = node.Topic,
                                RequestedDeliveryGuarantee = node.QualityOfService,
                                Retain = null,
                                Ttl = null
                            },
                        Triggering = skipTriggering || node.TriggeredNodes == null
                            ? null : new PublishedDataSetTriggerModel
                            {
                                PublishedVariables = ToPublishedDataItems(node.TriggeredNodes, true),
                                PublishedEvents = ToPublishedEventItems(node.TriggeredNodes, true)
                            }
                    })
                    .ToList()
                };
            }

            static PublishedEventItemsModel ToPublishedEventItems(IEnumerable<OpcNodeModel> opcNodes, bool skipTriggering)
            {
                return new PublishedEventItemsModel
                {
                    PublishedData = opcNodes.Where(node => node.EventFilter != null || node.ModelChangeHandling != null)
                    .Select(node => new PublishedDataSetEventModel
                    {
                        Id = node.DataSetFieldId,
                        EventNotifier = node.Id,
                        QueueSize = node.QueueSize,
                        DiscardNew = node.DiscardNew,
                        PublishedEventName = node.DisplayName,
                        ReadEventNameFromNode = node.FetchDisplayName,
                        BrowsePath = node.BrowsePath,
                        MonitoringMode = null,
                        TypeDefinitionId = node.EventFilter?.TypeDefinitionId,
                        SelectedFields = node.EventFilter?.SelectClauses?.Select(s => s.Clone()).ToList(),
                        Filter = node.EventFilter?.WhereClause.Clone(),
                        ConditionHandling = node.ConditionHandling.Clone(),
                        ModelChangeHandling = node.ModelChangeHandling.Clone(),
                        Publishing = node.Topic == null && node.QualityOfService == null
                            ? null : new PublishingQueueSettingsModel
                            {
                                QueueName = node.Topic,
                                RequestedDeliveryGuarantee = node.QualityOfService,
                                Retain = null,
                                Ttl = null
                            },
                        Triggering = skipTriggering || node.TriggeredNodes == null
                            ? null : new PublishedDataSetTriggerModel
                            {
                                PublishedVariables = ToPublishedDataItems(node.TriggeredNodes, true),
                                PublishedEvents = ToPublishedEventItems(node.TriggeredNodes, true)
                            }
                    }).ToList()
                };
            }
        }

        /// <summary>
        /// Adds the credentials from the connection to the published nodes model
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="publishedNodesEntryModel"></param>
        /// <exception cref="NotSupportedException"></exception>
        private PublishedNodesEntryModel AddConnectionModel(ConnectionModel? connection,
            PublishedNodesEntryModel publishedNodesEntryModel)
        {
            publishedNodesEntryModel.OpcAuthenticationPassword = null;
            publishedNodesEntryModel.OpcAuthenticationUsername = null;
            publishedNodesEntryModel.EncryptedAuthPassword = null;
            publishedNodesEntryModel.EncryptedAuthUsername = null;

            var credential = connection?.User;
            switch (credential?.Type ?? CredentialType.None)
            {
                case CredentialType.X509Certificate:
                case CredentialType.UserName:
                    publishedNodesEntryModel.OpcAuthenticationMode =
                        credential?.Type == CredentialType.X509Certificate ?
                            OpcAuthenticationMode.Certificate :
                            OpcAuthenticationMode.UsernamePassword;
                    Debug.Assert(credential != null);
                    var (user, pw, encrypted) =
                        ToUserNamePasswordCredentialAsync(credential.Value).GetAwaiter().GetResult();
                    if (encrypted)
                    {
                        publishedNodesEntryModel.EncryptedAuthPassword = pw;
                        publishedNodesEntryModel.EncryptedAuthUsername = user;
                    }
                    else
                    {
                        publishedNodesEntryModel.OpcAuthenticationPassword = pw;
                        publishedNodesEntryModel.OpcAuthenticationUsername = user;
                    }
                    break;
                case CredentialType.None:
                    publishedNodesEntryModel.OpcAuthenticationMode = OpcAuthenticationMode.Anonymous;
                    break;
                default:
                    throw new NotSupportedException(
                        $"Credentials of type {credential?.Type} are not supported.");
            }
            if (connection != null)
            {
                if (connection.Endpoint != null)
                {
                    publishedNodesEntryModel.EndpointUrl = connection.Endpoint.Url;
                    publishedNodesEntryModel.EndpointSecurityPolicy = connection.Endpoint.SecurityPolicy;
                    if (connection.Endpoint.SecurityMode == SecurityMode.None)
                    {
                        // Fall back to let UseSecurity decide on security (legacy)
                        publishedNodesEntryModel.UseSecurity = false;
                        publishedNodesEntryModel.EndpointSecurityMode = null;
                    }
                    else if (connection.Endpoint.SecurityMode == SecurityMode.NotNone)
                    {
                        // Fall back to let UseSecurity decide on security (legacy)
                        publishedNodesEntryModel.UseSecurity = true;
                        publishedNodesEntryModel.EndpointSecurityMode = null;
                    }
                    else
                    {
                        publishedNodesEntryModel.UseSecurity = null;
                        publishedNodesEntryModel.EndpointSecurityMode = connection.Endpoint.SecurityMode;
                    }
                }
                publishedNodesEntryModel.UseReverseConnect =
                    connection.Options.HasFlag(ConnectionOptions.UseReverseConnect) ? true : null;
                publishedNodesEntryModel.DisableSubscriptionTransfer =
                    connection.Options.HasFlag(ConnectionOptions.NoSubscriptionTransfer) ? true : null;
                publishedNodesEntryModel.DumpConnectionDiagnostics =
                    connection.Options.HasFlag(ConnectionOptions.DumpDiagnostics) ? true : null;
            }
            return publishedNodesEntryModel;

            async Task<(string? user, string? password, bool encrypted)> ToUserNamePasswordCredentialAsync(
                UserIdentityModel? credential)
            {
                var userString = credential?.User ?? string.Empty;
                var passwordString = credential?.Password ?? string.Empty;

                if (_forceCredentialEncryption)
                {
                    if (_cryptoProvider != null)
                    {
                        try
                        {
                            const string kInitializationVector = "alKGJdfsgidfasdO"; // See previous publisher
                            var userBytes = await _cryptoProvider.EncryptAsync(kInitializationVector,
                                Encoding.UTF8.GetBytes(userString)).ConfigureAwait(false);
                            var passwordBytes = await _cryptoProvider.EncryptAsync(kInitializationVector,
                                Encoding.UTF8.GetBytes(passwordString)).ConfigureAwait(false);
                            return (Convert.ToBase64String(userBytes.Span),
                                Convert.ToBase64String(passwordBytes.Span), true);
                        }
                        catch (Exception ex)
                        {
                            Runtime.FailFast("Attempting to store a credential. " +
                                "Credential encryption is enforced but crypto provider failed to encrypt!", ex);
                        }
                    }
                    Runtime.FailFast("Attempting to store a credential. " +
                        "Credential encryption is enforced but no crypto provider present!", null);
                }
                return (userString, passwordString, false);
            }
        }

        /// <summary>
        /// Convert to credential model and take into account backwards compatibility
        /// by using the crypto provider to decrypt encrypted credentials.
        /// </summary>
        /// <param name="entry"></param>
        private CredentialModel ToCredential(PublishedNodesEntryModel entry)
        {
            switch (entry.OpcAuthenticationMode)
            {
                case OpcAuthenticationMode.UsernamePassword:
                case OpcAuthenticationMode.Certificate:
                    var user = entry.OpcAuthenticationUsername ?? string.Empty;
                    var password = entry.OpcAuthenticationPassword ?? string.Empty;
                    try
                    {
                        const string kInitializationVector = "alKGJdfsgidfasdO"; // See previous publisher
                        if (!string.IsNullOrEmpty(entry.EncryptedAuthUsername) && string.IsNullOrEmpty(user))
                        {
                            if (_cryptoProvider != null)
                            {
                                var userBytes = _cryptoProvider.DecryptAsync(kInitializationVector,
                                    Convert.FromBase64String(entry.EncryptedAuthUsername))
                                        .AsTask().GetAwaiter().GetResult();
                                user = Encoding.UTF8.GetString(userBytes.Span);
                            }
                            else
                            {
                                const string error =
                                    "No crypto provider to decrypt encrypted username in config.";
                                if (_forceCredentialEncryption)
                                {
                                    Runtime.FailFast("Credential encryption is enforced! " + error, null);
                                }
                                _logger.LogError(error + " - not using credential!");
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(entry.EncryptedAuthPassword) && string.IsNullOrEmpty(password))
                        {
                            if (_cryptoProvider != null)
                            {
                                var passwordBytes = _cryptoProvider.DecryptAsync(kInitializationVector,
                                    Convert.FromBase64String(entry.EncryptedAuthPassword))
                                        .AsTask().GetAwaiter().GetResult();
                                password = Encoding.UTF8.GetString(passwordBytes.Span);
                            }
                            else
                            {
                                const string error =
                                    "No crypto provider to decrypt encrypted password in config.";
                                if (_forceCredentialEncryption)
                                {
                                    Runtime.FailFast("Credential encryption is enforced! " + error, null);
                                }
                                _logger.LogError(error + " - not using credential!");
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        const string error =
                            "Failed to decrypt encrypted username and password in config.";
                        if (_forceCredentialEncryption)
                        {
                            Runtime.FailFast("Credential encryption is enforced! " + error, ex);
                        }
                        // There is no reason we should use the encrypted credential here as plain
                        // text so just use none and move on.
                        _logger.LogError(ex, error + " - not using credential!");
                        break;
                    }
                    return new CredentialModel
                    {
                        Type = entry.OpcAuthenticationMode == OpcAuthenticationMode.Certificate ?
                            CredentialType.X509Certificate :
                            CredentialType.UserName,
                        Value = new UserIdentityModel { User = user, Password = password }
                    };
            }
            return new CredentialModel
            {
                Type = CredentialType.None
            };
        }

        private static readonly OpcNodeModel kDummyEntry = new();
        private readonly bool _forceCredentialEncryption;
        private readonly int _scaleTestCount;
        private readonly int _maxNodesPerDataSet;
        private readonly bool _noPublishingIntervalGrouping;
        private readonly IIoTEdgeWorkloadApi? _cryptoProvider;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
    }
}
