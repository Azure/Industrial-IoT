// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Nito.AsyncEx;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed partial class WriterGroupDataSource
    {
        /// <summary>
        /// Represents a data set writer which acts as a partitioning mechanism
        /// depending on how the writers should be set up from the configuration.
        /// The partitioning uses the publishing interval - because we always did,
        /// as well as the routing and topic configuration. The data set writer
        /// key has the topics already resolved, so that comparison is straight
        /// forward. This replaces the previously used SubscriptionIdentifier
        /// and works in a similar way to manage a table of unique writers in
        /// the writer group.
        /// </summary>
        internal sealed class DataSetWriter
        {
            /// <summary>
            /// Publishing interval which is used to split subscriptions for
            /// supporting legacy behavior of writer per subscription.
            /// </summary>
            public TimeSpan? PublishingInterval { get; }

            /// <summary>
            /// Routed topic
            /// </summary>
            public string Topic => Writer.Publishing?.QueueName ?? "/";

            /// <summary>
            /// Quality of service to use
            /// </summary>
            public QoS? Qos => Writer.Publishing?.RequestedDeliveryGuarantee;

            /// <summary>
            /// Message time to live
            /// </summary>
            public TimeSpan? Ttl => Writer.Publishing?.Ttl;

            /// <summary>
            /// Retain support
            /// </summary>
            public bool? Retain => Writer.Publishing?.Retain;

            /// <summary>
            /// Topic to route metadata to
            /// </summary>
            public string MetadataTopic => Writer.MetaData?.QueueName ?? "/";

            /// <summary>
            /// Quality of service to use
            /// </summary>
            public QoS MetadataQos => Writer.MetaData?.RequestedDeliveryGuarantee
                ?? QoS.AtLeastOnce;

            /// <summary>
            /// Message time to live
            /// </summary>
            public TimeSpan? MetadataTtl => Writer.MetaData?.Ttl
                ?? Writer.MetaDataUpdateTime;

            /// <summary>
            /// Retain support
            /// </summary>
            public bool MetadataRetain => Writer.MetaData?.Retain
                ?? true;

            /// <summary>
            /// Resolved routing
            /// </summary>
            public DataSetRoutingMode Routing { get; }

            /// <summary>
            /// Full cloned configuration
            /// </summary>
            public DataSetWriterModel Writer { get; }

            /// <summary>
            /// Data set
            /// </summary>
            public PublishedDataSetModel DataSet => Writer.DataSet!;

            /// <summary>
            /// Data set source
            /// </summary>
            public PublishedDataSetSourceModel Source => DataSet.DataSetSource!;

            /// <summary>
            /// Split the writer in the group in its writer partitions depending on the publish
            /// settings.
            /// </summary>
            /// <param name="group"></param>
            /// <param name="dataSetWriter"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException"></exception>
            public static IEnumerable<DataSetWriter> GetDataSetWriters(WriterGroupDataSource group,
                DataSetWriterModel dataSetWriter)
            {
                var options = group._options.Value;
                if (dataSetWriter?.DataSet?.DataSetSource == null)
                {
                    throw new ArgumentException("DataSet source missing", nameof(dataSetWriter));
                }

                var dataset = dataSetWriter.DataSet;
                var source = dataset.DataSetSource;
                var routing = dataset.Routing ?? options.DefaultDataSetRouting
                    ?? DataSetRoutingMode.None;

                var dataSetClassId = dataset.DataSetMetaData?.DataSetClassId
                    ?? Guid.Empty;
                var escWriterName = TopicFilter.Escape(
                    dataSetWriter.DataSetWriterName ?? Constants.DefaultDataSetWriterName);
                var escWriterGroup = TopicFilter.Escape(
                    group._writerGroup.Name ?? Constants.DefaultWriterGroupName);

                var variables = new Dictionary<string, string>
                {
                    [PublisherConfig.DataSetWriterIdVariableName] = dataSetWriter.Id,
                    [PublisherConfig.DataSetWriterVariableName] = escWriterName,
                    [PublisherConfig.DataSetWriterNameVariableName] = escWriterName,
                    [PublisherConfig.DataSetClassIdVariableName] = dataSetClassId.ToString(),
                    [PublisherConfig.WriterGroupIdVariableName] = group.Id,
                    [PublisherConfig.DataSetWriterGroupVariableName] = escWriterGroup,
                    [PublisherConfig.WriterGroupVariableName] = escWriterGroup
                    // ...
                };

                // No auto routing - group variables and events by publish settings
                var data = source.PublishedVariables?.PublishedData?
                    .GroupBy(d => Resolve(options, group._writerGroup, dataSetWriter,
                            d.Publishing, d.Id, routing, variables));
                if (data != null)
                {
                    if (routing == DataSetRoutingMode.None)
                    {
                        foreach (var items in data)
                        {
                            var id = dataSetWriter.Id;
                            yield return CreateDataSetWriter(id, items.Key, items.ToList());
                        }
                    }
                    else
                    {
                        foreach (var (p, item) in data.SelectMany(d => d.Select(i => (d.Key, i))))
                        {
                            var id = $"{dataSetWriter.Id}_{item.Id
                                ?? item.GetHashCode().ToString(CultureInfo.InvariantCulture)}";
                            yield return CreateDataSetWriter(id, p, new[] { item });
                        }
                    }
                }
                var evts = source.PublishedEvents?.PublishedData?
                    .GroupBy(d => Resolve(options, group._writerGroup, dataSetWriter,
                            d.Publishing, d.Id, routing, variables));
                if (evts != null)
                {
                    if (routing == DataSetRoutingMode.None)
                    {
                        foreach (var items in evts)
                        {
                            var id = dataSetWriter.Id;
                            yield return CreateEventWriter(id, items.Key, items.ToList());
                        }
                    }
                    else
                    {
                        foreach (var (p, item) in evts.SelectMany(d => d.Select(i => (d.Key, i))))
                        {
                            var id = $"{dataSetWriter.Id}_{item.Id ?? item.GetHashCode().ToString(CultureInfo.InvariantCulture)}";
                            yield return CreateEventWriter(id, p, new[] { item });
                        }
                    }
                }

                DataSetWriter CreateDataSetWriter(string id,
                    (PublishingQueueSettingsModel?, PublishingQueueSettingsModel?) publishSettings,
                    IReadOnlyList<PublishedDataSetVariableModel> data)
                {
                    return new DataSetWriter(group, routing, dataSetWriter with
                    {
                        Id = id,
                        MetaData = publishSettings.Item1,
                        Publishing = publishSettings.Item2,
                        DataSet = dataset with
                        {
                            DataSetMetaData = dataset.DataSetMetaData.Clone(),
                            DataSetSource = source with
                            {
                                Connection = source.Connection.Clone(),
                                SubscriptionSettings = source.SubscriptionSettings.Clone(),

                                PublishedEvents = null,
                                PublishedVariables = new PublishedDataItemsModel
                                {
                                    PublishedData = data
                                }
                            }
                        }
                    });
                }

                DataSetWriter CreateEventWriter(string id,
                    (PublishingQueueSettingsModel?, PublishingQueueSettingsModel?) publishSettings,
                    IReadOnlyList<PublishedDataSetEventModel> data)
                {
                    return new DataSetWriter(group, routing, dataSetWriter with
                    {
                        Id = id,
                        MetaData = publishSettings.Item1,
                        Publishing = publishSettings.Item2,
                        DataSet = dataset with
                        {
                            DataSetMetaData = dataset.DataSetMetaData.Clone(),
                            DataSetSource = source with
                            {
                                Connection = source.Connection.Clone(),
                                SubscriptionSettings = source.SubscriptionSettings.Clone(),

                                PublishedEvents = new PublishedEventItemsModel
                                {
                                    PublishedData = data
                                },
                                PublishedVariables = null
                            }
                        }
                    });
                }

                // Resolve the publish queue settings with the data set writer provided settings.
                static (PublishingQueueSettingsModel?, PublishingQueueSettingsModel?) Resolve(
                    PublisherOptions options, WriterGroupModel group, DataSetWriterModel dataSetWriter,
                    PublishingQueueSettingsModel? settings, string? fieldId,
                    DataSetRoutingMode routing, Dictionary<string, string> variables)
                {
                    var builder = new TopicBuilder(options, group.MessageType,
                        new TopicTemplatesOptions
                        {
                            Telemetry = settings?.QueueName
                                ?? dataSetWriter.Publishing?.QueueName
                                ?? group.Publishing?.QueueName,
                            DataSetMetaData = dataSetWriter.MetaData?.QueueName
                        },
                        variables
                            .Append(KeyValuePair
                                .Create(PublisherConfig.DataSetFieldIdVariableName,
                                    TopicFilter.Escape(fieldId ?? string.Empty))));

                    var telemetryTopic = builder.TelemetryTopic;
                    var metadataTopic = builder.DataSetMetaDataTopic;
                    if (string.IsNullOrWhiteSpace(metadataTopic) || routing != DataSetRoutingMode.None)
                    {
                        metadataTopic = telemetryTopic;
                    }

                    var publishing = new PublishingQueueSettingsModel
                    {
                        QueueName = telemetryTopic,
                        Ttl = settings?.Ttl
                            ?? dataSetWriter.Publishing?.Ttl
                            ?? group.Publishing?.Ttl,
                        RequestedDeliveryGuarantee = settings?.RequestedDeliveryGuarantee
                            ?? dataSetWriter.Publishing?.RequestedDeliveryGuarantee
                            ?? group.Publishing?.RequestedDeliveryGuarantee,
                        Retain = settings?.Retain
                            ?? dataSetWriter.Publishing?.Retain
                            ?? group.Publishing?.Retain
                    };

                    var metadata = new PublishingQueueSettingsModel
                    {
                        QueueName = metadataTopic,
                        Ttl =
                               dataSetWriter.MetaData?.Ttl
                            ?? publishing.Ttl,
                        RequestedDeliveryGuarantee =
                               dataSetWriter.MetaData?.RequestedDeliveryGuarantee
                            ?? publishing.RequestedDeliveryGuarantee,
                        Retain =
                               dataSetWriter.MetaData?.Retain
                            ?? publishing.Retain
                    };
                    return (metadata, publishing);
                }
            }

            /// <summary>
            /// Create id from a DataSetWriterModel template
            /// </summary>
            /// <param name="group"></param>
            /// <param name="routing"></param>
            /// <param name="dataSetWriter"></param>
            private DataSetWriter(WriterGroupDataSource group, DataSetRoutingMode routing,
                DataSetWriterModel dataSetWriter)
            {
                Writer = dataSetWriter;
                Routing = routing;

                PublishingInterval =
                    group._options.Value.IgnoreConfiguredPublishingIntervals == true
                    ? null : Source.SubscriptionSettings?.PublishingInterval;
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is DataSetWriter writer &&
                    writer.Writer.Id == Writer.Id &&
                    writer.PublishingInterval == PublishingInterval &&
                    writer.Topic == Topic &&
                    writer.Qos == Qos &&
                    writer.Ttl == Ttl &&
                    writer.Retain == Retain &&
                    writer.MetadataTopic == MetadataTopic &&
                    writer.MetadataQos == MetadataQos &&
                    writer.MetadataTtl == MetadataTtl &&
                    writer.MetadataRetain == MetadataRetain &&
                    writer.Routing == Routing)
                {
                    return true;
                }
                return false;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                //
                // By default we partition on publishing interval and the
                // output configuration binding.
                //
                return HashCode.Combine(Writer.Id, PublishingInterval,
                    Topic,
                    HashCode.Combine(Qos, Ttl, Retain),
                    MetadataTopic,
                    HashCode.Combine(MetadataQos, MetadataTtl, MetadataRetain),
                    Routing);
            }

            /// <inheritdoc/>
            public override string? ToString()
            {
                return $"Writer {Writer.Id}->{Topic}@{PublishingInterval}";
            }
        }

        /// <summary>
        /// A data set writer subscription binding inside a writer group
        /// </summary>
        private sealed class DataSetWriterSubscription : ISubscriber, IAsyncDisposable
        {
            /// <summary>
            /// Name of the data set writer in the writer group (unique)
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Writer id
            /// </summary>
            public string Id => _writer.Writer.Id;

            /// <summary>
            /// Index of the data set writer in the group
            /// </summary>
            public int Index { get; set; }

            /// <summary>
            /// Meta data
            /// </summary>
            internal PublishedDataSetMessageSchemaModel? MetaData =>
                _metaDataLoader.IsValueCreated ? _metaDataLoader.Value.MetaData : null;

            /// <summary>
            /// Metadata disabled
            /// </summary>
            internal bool IsMetadataDisabled => _writer.DataSet?.DataSetMetaData == null
                || _group._options.Value.DisableDataSetMetaData == true;

            /// <summary>
            /// Subscription id
            /// </summary>
            public IEnumerable<BaseMonitoredItemModel> MonitoredItems { get; private set; }

            /// <summary>
            /// Active subscription
            /// </summary>
            public ISubscription? Subscription { get; private set; }

            /// <summary>
            /// Create subscription from a DataSetWriterModel template
            /// </summary>
            /// <param name="group"></param>
            /// <param name="writer"></param>
            /// <param name="writerNames"></param>
            /// <param name="logger"></param>
            private DataSetWriterSubscription(WriterGroupDataSource group, DataSetWriter writer,
                HashSet<string> writerNames, ILogger<DataSetWriterSubscription> logger)
            {
                _group = group;
                _writer = writer;
                _logger = logger;
                _metaDataLoader = new Lazy<MetaDataLoader>(() => new MetaDataLoader(this), true);

                Name = CreateUniqueWriterName(writer.Writer.DataSetWriterName, writerNames);

                logger.CreatingNewWriter(Id, Name, _group.Id);

                // Create monitored items
                var namespaceFormat =
                    _group._writerGroup.MessageSettings?.NamespaceFormat ??
                    _group._options.Value.DefaultNamespaceFormat ??
                    NamespaceFormat.Uri;
                MonitoredItems = _writer.Source.ToMonitoredItems(namespaceFormat);
                _extensionFields = new ExtensionFields(_group._serializer,
                    _writer.DataSet.ExtensionFields, _writer.Writer.DataSetFieldContentMask);
                _template = _writer.Source.SubscriptionSettings.ToSubscriptionModel(
                    _writer.Routing != DataSetRoutingMode.None,
                    _group._options.Value.IgnoreConfiguredPublishingIntervals);
                _connection = _writer.Writer.GetConnection(_group.Id, _group._options.Value);
            }

            /// <summary>
            /// Create subscription
            /// </summary>
            /// <param name="group"></param>
            /// <param name="dataSetWriter"></param>
            /// <param name="loggerFactory"></param>
            /// <param name="writerNames"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public async static ValueTask<DataSetWriterSubscription> CreateAsync(WriterGroupDataSource group,
                DataSetWriter dataSetWriter, ILoggerFactory loggerFactory, HashSet<string> writerNames,
                CancellationToken ct)
            {
                var writer = new DataSetWriterSubscription(group, dataSetWriter, writerNames,
                    loggerFactory.CreateLogger<DataSetWriterSubscription>());

                writer.Subscription = await group._clients.CreateSubscriptionAsync(
                    writer._connection.Connection, writer._template, writer, ct).ConfigureAwait(false);

                writer.InitializeMetaDataTrigger();
                writer.InitializeKeepAlive();

                group._logger.CreatedWriter(writer.Id, group.Id);

                return writer;
            }

            /// <summary>
            /// Update subscription content
            /// </summary>
            /// <param name="dataSetWriter"></param>
            /// <param name="writerNames"></param>
            /// <param name="ct"></param>
            /// <exception cref="ArgumentException"></exception>
            public async ValueTask UpdateAsync(DataSetWriter dataSetWriter, HashSet<string> writerNames,
                CancellationToken ct)
            {
                _logger.UpdatingWriter(Id, _group.Id);

                var previous = _writer;
                _writer = dataSetWriter;

                if (previous.Writer.DataSetWriterName != _writer.Writer.DataSetWriterName)
                {
                    writerNames.Remove(Name);
                    Name = CreateUniqueWriterName(_writer.Writer.DataSetWriterName, writerNames);
                }

                var namespaceFormat =
                    _group._writerGroup.MessageSettings?.NamespaceFormat ??
                    _group._options.Value.DefaultNamespaceFormat ??
                    NamespaceFormat.Uri;
                MonitoredItems = _writer.Source.ToMonitoredItems(namespaceFormat);
                _extensionFields = new ExtensionFields(_group._serializer,
                    _writer.DataSet.ExtensionFields, _writer.Writer.DataSetFieldContentMask);
                var template = _writer.Source.SubscriptionSettings.ToSubscriptionModel(
                    _writer.Routing != DataSetRoutingMode.None,
                    _group._options.Value.IgnoreConfiguredPublishingIntervals);
                var connection = _writer.Writer.GetConnection(_group.Id, _group._options.Value);

                if (template != _template || connection != _connection || Subscription == null)
                {
                    _template = template;
                    _connection = connection;

                    //
                    // Create or new subscription for the writer group. This will automatically
                    // dispose our older subscription or update it to comply if possible.
                    //
                    Subscription = await _group._clients.CreateSubscriptionAsync(
                        _connection.Connection, _template, this, ct).ConfigureAwait(false);

                    _logger.RecreatedSubscription(Id, _group.Id);
                }
                else
                {
                    // Trigger reevaluation
                    Subscription.NotifyMonitoredItemsChanged();

                    _logger.UpdatedMonitoredItems(Id, _group.Id);
                }

                _frameCount = 0;
                InitializeMetaDataTrigger();
                InitializeKeepAlive();
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;
                    _metadataTimer?.Stop();

                    if (Subscription != null)
                    {
                        await Subscription.DisposeAsync().ConfigureAwait(false);
                        Subscription = null;
                    }

                    _logger.ClosedWriter(Id, _group.Id);
                }
                finally
                {
                    _metadataTimer?.Dispose();
                    _metadataTimer = null;
                }
            }

            /// <inheritdoc/>
            public void OnMonitoredItemSemanticsChanged()
            {
                if (!IsMetadataDisabled)
                {
                    // Reload metadata
                    _metaDataLoader.Value.Reload();
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionKeepAlive(OpcUaSubscriptionNotification notification)
            {
                Interlocked.Increment(ref _group._keepAliveCount);
                if (_sendKeepAlives)
                {
                    CallMessageReceiverDelegates(notification);
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionDataChangeReceived(OpcUaSubscriptionNotification notification)
            {
                CallMessageReceiverDelegates(ProcessKeyFrame(notification));

                OpcUaSubscriptionNotification ProcessKeyFrame(OpcUaSubscriptionNotification notification)
                {
                    var keyFrameCount = _writer.Writer.KeyFrameCount
                        ?? _group._options.Value.DefaultKeyFrameCount ?? 0;
                    if (keyFrameCount > 0)
                    {
                        var frameCount = Interlocked.Increment(ref _frameCount);
                        if (((frameCount - 1) % keyFrameCount) == 0)
                        {
                            notification.TryUpgradeToKeyFrame(this);
                        }
                    }
                    return notification;
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionCyclicReadCompleted(OpcUaSubscriptionNotification notification)
            {
                CallMessageReceiverDelegates(notification);
            }

            /// <inheritdoc/>
            public void OnSubscriptionEventReceived(OpcUaSubscriptionNotification notification)
            {
                CallMessageReceiverDelegates(notification);
            }

            /// <inheritdoc/>
            public void OnSubscriptionDataDiagnosticsChange(bool liveData, int valueChanges, int overflow,
                int heartbeats)
            {
                lock (_lock)
                {
                    _group._heartbeats.Count += heartbeats;
                    _group._overflows.Count += overflow;
                    if (liveData)
                    {
                        if (_group._dataChanges.Count >= kNumberOfInvokedMessagesResetThreshold ||
                            _group._valueChanges.Count >= kNumberOfInvokedMessagesResetThreshold)
                        {
                            _logger.NotificationsCounterReset((int)_group._dataChanges.Count, (int)_group._valueChanges.Count);
                            _group._dataChanges.Count = 0;
                            _group._valueChanges.Count = 0;
                            _group._heartbeats.Count = 0;
                            _group._sink.OnCounterReset();
                        }

                        _group._valueChanges.Count += valueChanges;
                        _group._dataChanges.Count++;
                    }
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionCyclicReadDiagnosticsChange(int valuesSampled, int overflow)
            {
                lock (_lock)
                {
                    _group._overflows.Count += overflow;

                    if (_group._dataChanges.Count >= kNumberOfInvokedMessagesResetThreshold ||
                        _group._sampledValues.Count >= kNumberOfInvokedMessagesResetThreshold)
                    {
                        _logger.NotificationsCounterResetRead((int)_group._cyclicReads.Count, (int)_group._sampledValues.Count);
                        _group._cyclicReads.Count = 0;
                        _group._sampledValues.Count = 0;
                        _group._sink.OnCounterReset();
                    }

                    _group._sampledValues.Count += valuesSampled;
                    _group._cyclicReads.Count++;
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionEventDiagnosticsChange(bool liveData, int events, int overflow,
                int modelChanges)
            {
                lock (_lock)
                {
                    _group._modelChanges.Count += modelChanges;
                    _group._overflows.Count += overflow;

                    if (liveData)
                    {
                        if (_group._events.Count >= kNumberOfInvokedMessagesResetThreshold ||
                            _group._eventNotification.Count >= kNumberOfInvokedMessagesResetThreshold)
                        {
                            _logger.NotificationsCounterResetEvent((int)_group._events.Count, (int)_group._eventNotification.Count);
                            _group._events.Count = 0;
                            _group._eventNotification.Count = 0;
                            _group._modelChanges.Count = 0;

                            _group._sink.OnCounterReset();
                        }

                        _group._eventNotification.Count += events;
                        _group._events.Count++;
                    }
                }
            }

            /// <summary>
            /// Initialize sending of keep alive messages
            /// </summary>
            private void InitializeKeepAlive()
            {
                _sendKeepAlives = _writer.DataSet?.SendKeepAlive
                    ?? _group._options.Value.EnableDataSetKeepAlives == true;
            }

            /// <summary>
            /// Initializes the Metadata triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeMetaDataTrigger()
            {
                var metaDataSendInterval = _writer.Writer.MetaDataUpdateTime
                    ?? _group._options.Value.DefaultMetaDataUpdateTime
                    ?? TimeSpan.Zero;
                if (metaDataSendInterval > TimeSpan.Zero && !IsMetadataDisabled)
                {
                    if (_metadataTimer == null)
                    {
                        _metadataTimer = new TimerEx(metaDataSendInterval, _group._timeProvider);
                        _metadataTimer.Elapsed += MetadataTimerElapsed;
                        _metadataTimer.Start();
                    }
                    else
                    {
                        _metadataTimer.Interval = metaDataSendInterval;
                    }
                }
                else
                {
                    if (_metadataTimer != null)
                    {
                        _metadataTimer.Stop();
                        _metadataTimer.Dispose();
                        _metadataTimer = null;
                    }
                }
            }

            /// <summary>
            /// Fired when metadata time elapsed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void MetadataTimerElapsed(object? sender, ElapsedEventArgs e)
            {
                try
                {
                    var timer = _metadataTimer;
                    if (timer == null)
                    {
                        return;
                    }
                    timer.Enabled = false;
                    // Enabled again after calling message receiver delegate
                }
                catch (ObjectDisposedException)
                {
                    // Disposed while being invoked
                    return;
                }

                var notification = Subscription?.CreateKeepAlive();
                if (notification != null)
                {
                    // This call udpates the message type, so no need to do it here.
                    CallMessageReceiverDelegates(notification, true);
                }
                else
                {
                    // Failed to send, try again later
                    InitializeMetaDataTrigger();
                }
            }

            /// <summary>
            /// handle subscription change messages
            /// </summary>
            /// <param name="notification"></param>
            /// <param name="sourceIsMetaDataTimer"></param>
            private void CallMessageReceiverDelegates(OpcUaSubscriptionNotification notification,
                bool sourceIsMetaDataTimer = false)
            {
                try
                {
                    lock (_lock)
                    {
                        var metadata = MetaData;
                        var single = notification.Notifications?.Count == 1 ?
                            notification.Notifications[0] : null;
                        if (metadata == null && !IsMetadataDisabled)
                        {
                            if (_group._options.Value.AsyncMetaDataLoadTimeout != TimeSpan.Zero)
                            {
                                var sw = Stopwatch.StartNew();
                                // Block until we have metadata or just continue
                                _metaDataLoader.Value.BlockUntilLoaded(
                                    _group._options.Value.AsyncMetaDataLoadTimeout ?? TimeSpan.FromSeconds(5));
                                _logger.BlockedMessageForMetadata(sw.Elapsed, _writer);
                            }

                            metadata = MetaData;
                            if (metadata == null)
                            {
                                _logger.NoMetadataAvailable(_writer);
                                Interlocked.Increment(ref _group._messagesWithoutMetadata);
                                return;
                            }
                        }

                        if (metadata != null)
                        {
                            var sendMetadata = sourceIsMetaDataTimer;
                            //
                            // Only send if called from metadata timer or if the metadata version changes.
                            //
                            if (_lastMajorVersion != metadata.MetaData.DataSetMetaData.MajorVersion ||
                                _lastMinorVersion != metadata.MetaData.MinorVersion)
                            {
                                _lastMajorVersion = metadata.MetaData.DataSetMetaData.MajorVersion;
                                _lastMinorVersion = metadata.MetaData.MinorVersion;

                                Interlocked.Increment(ref _group._metadataChanges);
                                sendMetadata = true;
                            }
                            if (sendMetadata)
                            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                                var metadataFrame = new OpcUaSubscriptionNotification(notification)
                                {
                                    MessageType = MessageType.Metadata,
                                    EventTypeName = null,
                                    Context = CreateMessageContext(_writer.MetadataTopic,
                                        _writer.MetadataQos, _writer.MetadataRetain, _writer.MetadataTtl,
                                        () => Interlocked.Increment(ref _metadataSequenceNumber), metadata,
                                        single, true)
                                };
#pragma warning restore CA2000 // Dispose objects before losing scope
                                _group._sink.OnMessage(metadataFrame);
                                InitializeMetaDataTrigger();
                            }
                        }

                        if (!sourceIsMetaDataTimer)
                        {
                            Debug.Assert(notification.Notifications != null);
                            notification.Context = CreateMessageContext(_writer.Topic,
                                _writer.Qos, _writer.Retain, _writer.Ttl,
                                () => Interlocked.Increment(ref _dataSetSequenceNumber), metadata,
                                single, false);
                            _logger.EnqueuingNotification(notification.ToString());
                            _group._sink.OnMessage(notification);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.FailedToProduceMessage(ex);
                }

                DataSetWriterContext CreateMessageContext(string topic, QoS? qos, bool? retain,
                    TimeSpan? ttl, Func<uint> sequenceNumber, PublishedDataSetMessageSchemaModel? metadata,
                    MonitoredItemNotificationModel? single, bool isMetadata)
                {
                    _group.GetWriterGroup(out var writerGroup, out var networkMessageSchema);
                    return new DataSetWriterContext
                    {
                        PublisherId = _group._options.Value.PublisherId ?? Constants.DefaultPublisherId,
                        DataSetWriterId = (ushort)Index,
                        MetaData = metadata,
                        Writer = _writer.Writer,
                        ExtensionFields = _extensionFields.GetExtensionFieldData(notification),
                        WriterName = Name,
                        NextWriterSequenceNumber = sequenceNumber,
                        WriterGroup = writerGroup,
                        Schema = networkMessageSchema,
                        CloudEvent = GetCloudEventHeader(writerGroup, notification, isMetadata),
                        Topic = GetTopic(_writer.Routing, topic, single?.PathFromRoot),
                        Retain = retain,
                        Ttl = ttl,
                        Qos = qos
                    };

                    CloudEventHeader? GetCloudEventHeader(WriterGroupModel writerGroup,
                        OpcUaSubscriptionNotification notification, bool isMetadata)
                    {
                        if (_group._options.Value.EnableCloudEvents != true)
                        {
                            return null;
                        }
                        var type = isMetadata ? "Metadata" :
                            notification.MessageType == MessageType.Event ?
                                "Event" : "Dataset";
                        var typeName = _writer.Writer.DataSet?.Type
                            ?? notification.EventTypeName;
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            type += "/" + typeName;
                        }
                        var subject = writerGroup.ExternalId ?? string.Empty;
                        if (!string.IsNullOrEmpty(_writer.Writer.DataSet?.Name))
                        {
                            subject += "/" + _writer.Writer.DataSet.Name;
                        }
                        if (!Uri.TryCreate(notification.ApplicationUri ??
                            notification.EndpointUrl, UriKind.Absolute, out var source))
                        {
                            // Set a default source
                            source = new Uri("urn:" +
                                _group._options.Value.PublisherId ?? "publisher");
                        }
                        return new CloudEventHeader
                        {
                            Id = notification.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                            Time = notification.PublishTimestamp,
                            Type = type,
                            Source = source,
                            Subject = subject.Length == 0 ? null : subject
                        };
                    }

                    static string GetTopic(DataSetRoutingMode routing, string topic, Opc.Ua.RelativePath? subpath)
                    {
                        if (subpath == null || routing == DataSetRoutingMode.None)
                        {
                            return topic;
                        }
                        // Append subpath to topic (use browse names with namespace index if requested
                        var sb = new StringBuilder().Append(topic);
                        foreach (var path in subpath.Elements)
                        {
                            sb.Append('/');
                            if (path.TargetName.NamespaceIndex != 0 &&
                                routing == DataSetRoutingMode.UseBrowseNamesWithNamespaceIndex)
                            {
                                sb.Append(path.TargetName.NamespaceIndex).Append(':');
                            }
                            sb.Append(TopicFilter.Escape(path.TargetName.Name));
                        }
                        return sb.ToString();
                    }
                }
            }

            /// <summary>
            /// Make unique writer name
            /// </summary>
            /// <param name="str"></param>
            /// <param name="strings"></param>
            /// <returns></returns>
            private static string CreateUniqueWriterName(string? str, HashSet<string> strings)
            {
                var originalName = str ?? Constants.DefaultDataSetWriterName;
                var uniqueName = originalName;
                for (var index = 1; ; index++)
                {
                    if (strings.Add(uniqueName))
                    {
                        return uniqueName;
                    }
                    uniqueName = $"{originalName}{index}";
                }
            }

            /// <summary>
            /// Asynchronously load metadata after the subscription is created and metadata
            /// has changed event is received.
            /// </summary>
            private sealed class MetaDataLoader : IAsyncDisposable
            {
                /// <summary>
                /// Current meta data
                /// </summary>
                public PublishedDataSetMessageSchemaModel? MetaData { get; private set; }

                /// <summary>
                /// Create loader
                /// </summary>
                /// <param name="subscription"></param>
                public MetaDataLoader(DataSetWriterSubscription subscription)
                {
                    _writer = subscription;
                    _loader = StartAsync(_cts.Token);
                    _tcs = new TaskCompletionSource();
                }

                /// <inheritdoc/>
                public async ValueTask DisposeAsync()
                {
                    try
                    {
                        await _cts.CancelAsync().ConfigureAwait(false);
                        await _loader.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        _cts.Dispose();
                    }
                }

                /// <summary>
                /// Load meta data
                /// </summary>
                public void Reload()
                {
                    _trigger.Set();
                }

                /// <summary>
                /// Wait for metadata to be loaded or timeout after timeout
                /// </summary>
                /// <param name="timeout"></param>
                /// <returns></returns>
                public bool BlockUntilLoaded(TimeSpan timeout)
                {
                    try
                    {
                        return _tcs.Task.Wait(timeout);
                    }
                    catch
                    {
                        return false;
                    }
                }

                /// <summary>
                /// Meta data loader task
                /// </summary>
                /// <param name="ct"></param>
                /// <returns></returns>
                private async Task StartAsync(CancellationToken ct)
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await _trigger.WaitAsync(ct).ConfigureAwait(false);

                        try
                        {
                            await UpdateMetaDataAsync(ct).ConfigureAwait(false);
                            _tcs.TrySetResult();
                            Interlocked.Increment(ref _writer._group._metadataLoadSuccess);
                        }
                        catch (OperationCanceledException)
                        {
                            _tcs.TrySetCanceled(ct);
                        }
                        catch (Exception ex)
                        {
                            _writer._logger.FailedToGetMetadata(_writer._writer, ex.Message);
                            _tcs.TrySetException(ex);
                            Interlocked.Increment(ref _writer._group._metadataLoadFailures);
                        }
                        Interlocked.Exchange(ref _tcs, new TaskCompletionSource());
                    }
                }

                /// <summary>
                /// Update metadata
                /// </summary>
                /// <param name="ct"></param>
                /// <returns></returns>
                internal async Task UpdateMetaDataAsync(CancellationToken ct = default)
                {
                    var dataSetMetaData = _writer._writer.DataSet?.DataSetMetaData;
                    var subscription = _writer.Subscription;
                    if (dataSetMetaData == null || subscription == null)
                    {
                        // Metadata disabled
                        MetaData = null;
                        return;
                    }

                    //
                    // Use the date time to version across reboots. This could be done
                    // more elegantly by saving the last version to persistent storage
                    // such as twin, but this is ok for the sake of being able to have
                    // an incremental version number defining metadata changes.
                    //
                    var minor = (uint)_writer._group._timeProvider.GetUtcNow()
                        .UtcDateTime.ToBinary();

                    var sw = Stopwatch.StartNew();
                    _writer._logger.LoadingMetadata(dataSetMetaData.MajorVersion ?? 1, minor, _writer.Id);

                    var fieldMask = _writer._writer.Writer.DataSetFieldContentMask;
                    var metaData = await subscription.CollectMetaDataAsync(_writer, fieldMask,
                        dataSetMetaData, minor, ct).ConfigureAwait(false);

                    _writer._logger.LoadingMetadataTook(dataSetMetaData.MajorVersion ?? 1, minor, _writer.Id, sw.Elapsed);

                    var msgMask = _writer._writer.Writer.MessageSettings?.DataSetMessageContentMask;
                    MetaData = new PublishedDataSetMessageSchemaModel
                    {
                        MetaData = metaData with
                        {
                            Fields = _writer._extensionFields.AddMetadata(metaData.Fields)
                        },
                        TypeName = null,
                        DataSetFieldContentFlags = fieldMask,
                        DataSetMessageContentFlags = msgMask
                    };
                }

                private TaskCompletionSource _tcs;
                private readonly Task _loader;
                private readonly CancellationTokenSource _cts = new();
                private readonly AsyncAutoResetEvent _trigger = new();
                private readonly DataSetWriterSubscription _writer;
            }

            /// <summary>
            /// Extension fields of the writer
            /// </summary>
            private sealed class ExtensionFields
            {
                /// <summary>
                /// Get extension fields as configured
                /// </summary>
                /// <returns></returns>
                public IReadOnlyList<ExtensionFieldModel>? Fields
                {
                    get
                    {
                        if ((_fieldMask & (DataSetFieldContentFlags.EndpointUrl |
                                           DataSetFieldContentFlags.ApplicationUri)) == 0)
                        {
                            return _extensionFields;
                        }
                        var extensionFields = _extensionFields?.ToList() ?? [];
                        if ((_fieldMask & DataSetFieldContentFlags.EndpointUrl) != 0 &&
                            !extensionFields
                            .Any(f => f.DataSetFieldName == nameof(DataSetFieldContentFlags.EndpointUrl)))
                        {
                            extensionFields.Add(new ExtensionFieldModel
                            {
                                DataSetFieldName = nameof(DataSetFieldContentFlags.EndpointUrl),
                                Value = "{{EndpointUrl}}",
                                DataSetFieldDescription = "Endpoint Url of the data source."
                            });
                        }
                        if ((_fieldMask & DataSetFieldContentFlags.ApplicationUri) != 0 &&
                            !extensionFields
                            .Any(f => f.DataSetFieldName == nameof(DataSetFieldContentFlags.ApplicationUri)))
                        {
                            extensionFields.Add(new ExtensionFieldModel
                            {
                                DataSetFieldName = nameof(DataSetFieldContentFlags.ApplicationUri),
                                Value = "{{ApplicationUri}}",
                                DataSetFieldDescription = "Application Uri of the data source."
                            });
                        }
                        return extensionFields;
                    }
                }

                /// <summary>
                /// Create extension fields
                /// </summary>
                /// <param name="serializer"></param>
                /// <param name="extensionFields"></param>
                /// <param name="dataSetFieldContentMask"></param>
                public ExtensionFields(IJsonSerializer serializer,
                    IReadOnlyList<ExtensionFieldModel>? extensionFields,
                    DataSetFieldContentFlags? dataSetFieldContentMask)
                {
                    _serializer = serializer;
                    _fieldMask = dataSetFieldContentMask ?? 0;
                    _extensionFields = extensionFields;
                    _data = GenerateExtensionFieldData();
                }

                /// <summary>
                /// Get extension field data
                /// </summary>
                /// <param name="notification"></param>
                /// <returns></returns>
                public IReadOnlyList<(string, Opc.Ua.DataValue?)> GetExtensionFieldData(
                    OpcUaSubscriptionNotification notification)
                {
                    if ((_fieldMask & (DataSetFieldContentFlags.EndpointUrl |
                                       DataSetFieldContentFlags.ApplicationUri)) == 0)
                    {
                        return _data;
                    }
                    return _data
                        .Select(f => f.Item1 switch
                        {
                            nameof(DataSetFieldContentFlags.EndpointUrl) =>
                                (f.Item1, new Opc.Ua.DataValue(notification.EndpointUrl)),
                            nameof(DataSetFieldContentFlags.ApplicationUri) =>
                                (f.Item1, new Opc.Ua.DataValue(notification.ApplicationUri)),
                            _ => f
                        })
                        .ToList();
                }

                /// <summary>
                /// Add extension field metadata to the end of the metadata fields
                /// </summary>
                /// <param name="metadataFields"></param>
                /// <returns></returns>
                public IReadOnlyList<PublishedFieldMetaDataModel> AddMetadata(
                    IReadOnlyList<PublishedFieldMetaDataModel> metadataFields)
                {
                    var extensionFields = Fields;
                    if (extensionFields == null || extensionFields.Count == 0)
                    {
                        return metadataFields;
                    }
                    var fields = new List<PublishedFieldMetaDataModel>(metadataFields);
                    foreach (var field in extensionFields)
                    {
                        var builtInType = GetBuiltInType(field.Value);
                        fields.Add(new PublishedFieldMetaDataModel
                        {
                            Flags = 0, // Set to 1 << 1 for PromotedField fields.
                            Name = field.DataSetFieldName,
                            Id = field.DataSetClassFieldId,
                            Description = field.DataSetFieldDescription,
                            ValueRank = (int)(field.Value.IsArray ?
                                 NodeValueRank.OneDimension : NodeValueRank.Scalar),
                            ArrayDimensions = null,
                            MaxStringLength = 0,
                            // If the Property is EngineeringUnits, the unit of the Field Value
                            // shall match the unit of the FieldMetaData.
                            Properties = null, // TODO: Add engineering units etc. to properties
                            BuiltInType = (byte)builtInType
                        });
                    }
                    return fields;
                }

                /// <summary>
                /// Generate extension field data values
                /// </summary>
                /// <returns></returns>
                private IReadOnlyList<(string, Opc.Ua.DataValue?)> GenerateExtensionFieldData()
                {
                    var extensionFields = Fields;
                    if (extensionFields == null || extensionFields.Count == 0)
                    {
                        return Array.Empty<(string, Opc.Ua.DataValue?)>();
                    }
                    var extensions = new List<(string, Opc.Ua.DataValue?)>();
                    var encoder = new JsonVariantEncoder(new Opc.Ua.ServiceMessageContext(),
                        _serializer);
                    foreach (var field in extensionFields)
                    {
                        extensions.Add((field.DataSetFieldName,
                            new Opc.Ua.DataValue(encoder.Decode(field.Value,
                                (Opc.Ua.BuiltInType)GetBuiltInType(field.Value)))));
                    }
                    return extensions;
                }

                private static byte GetBuiltInType(VariantValue value)
                {
                    return value.GetTypeCode() switch
                    {
                        TypeCode.Empty => 0,
                        TypeCode.Boolean => 1,
                        TypeCode.SByte => 2,
                        TypeCode.Byte => 3,
                        TypeCode.Int16 => 4,
                        TypeCode.UInt16 => 5,
                        TypeCode.Int32 => 6,
                        TypeCode.UInt32 => 7,
                        TypeCode.Int64 => 8,
                        TypeCode.UInt64 => 9,
                        TypeCode.Single => 10,
                        TypeCode.Double => 11,
                        TypeCode.String or TypeCode.Char => 12,
                        TypeCode.DateTime => 13,
                        _ => 24
                    };
                }

                private readonly IJsonSerializer _serializer;
                private readonly DataSetFieldContentFlags _fieldMask;
                private readonly IReadOnlyList<ExtensionFieldModel>? _extensionFields;
                private readonly IReadOnlyList<(string, Opc.Ua.DataValue?)> _data;
            }

            private readonly WriterGroupDataSource _group;
            private readonly ILogger _logger;
            private readonly Lock _lock = new();
            private volatile uint _frameCount;
            private uint? _lastMajorVersion;
            private uint? _lastMinorVersion;
            private TimerEx? _metadataTimer;
            private SubscriptionModel _template;
            private ConnectionIdentifier _connection;
            private DataSetWriter _writer;
            private ExtensionFields _extensionFields;
            private readonly Lazy<MetaDataLoader> _metaDataLoader;
            private uint _dataSetSequenceNumber;
            private uint _metadataSequenceNumber;
            private bool _sendKeepAlives;
            private bool _disposed;
        }
    }

    internal static partial class DataSetWriterSubscriptionLogging
    {
        private const int EventClass = 130;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Debug,
            Message = "Creating new writer {Id} ({Writer}) in writer group {WriterGroup}...")]
        internal static partial void CreatingNewWriter(this ILogger logger, string id,
            string writer, string writerGroup);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Information,
            Message = "Created writer {Id} in writer group {WriterGroup}.")]
        internal static partial void CreatedWriter(this ILogger logger, string id,
            string writerGroup);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Debug,
            Message = "Updating writer {Id} in writer group {WriterGroup}...")]
        internal static partial void UpdatingWriter(this ILogger logger, string id,
            string writerGroup);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Information,
            Message = "Recreated subscription for writer {Id} in writer group {WriterGroup}...")]
        internal static partial void RecreatedSubscription(this ILogger logger, string id,
            string writerGroup);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Debug,
            Message = "Updated monitored items for writer {Id} in writer group {WriterGroup}.")]
        internal static partial void UpdatedMonitoredItems(this ILogger logger, string id,
            string writerGroup);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Information,
            Message = "Closed writer {Id} in writer group {WriterGroup}.")]
        internal static partial void ClosedWriter(this ILogger logger, string id,
            string writerGroup);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Debug,
            Message = "Notifications counter has been reset to prevent overflow. So far, {DataChangesCount} " +
            "data changes and {ValueChangesCount} value changes were invoked by message source.")]
        internal static partial void NotificationsCounterReset(this ILogger logger,
            int dataChangesCount, int valueChangesCount);

        [LoggerMessage(EventId = EventClass + 8, Level = LogLevel.Debug,
            Message = "Notifications counter has been reset to prevent overflow. So far, {ReadCount} " +
            "data changes and {ValuesCount} value changes were invoked by message source.")]
        internal static partial void NotificationsCounterResetRead(this ILogger logger,
            int readCount, int valuesCount);

        [LoggerMessage(EventId = EventClass + 9, Level = LogLevel.Debug,
            Message = "Notifications counter has been reset to prevent overflow. So far, {EventChangesCount} " +
            "event changes and {EventValueChangesCount} event value changes were invoked by message source.")]
        internal static partial void NotificationsCounterResetEvent(this ILogger logger,
            int eventChangesCount, int eventValueChangesCount);

        [LoggerMessage(EventId = EventClass + 10, Level = LogLevel.Information,
            Message = "Blocked message for {Duration} until metadata was loaded for {Writer}.")]
        internal static partial void BlockedMessageForMetadata(this ILogger logger,
            TimeSpan duration, WriterGroupDataSource.DataSetWriter writer);

        [LoggerMessage(EventId = EventClass + 11, Level = LogLevel.Warning,
            Message = "No metadata available for {Writer} - dropping notification.")]
        internal static partial void NoMetadataAvailable(this ILogger logger,
            WriterGroupDataSource.DataSetWriter writer);

        [LoggerMessage(EventId = EventClass + 12, Level = LogLevel.Trace,
            Message = "Enqueuing notification: {Notification}")]
        internal static partial void EnqueuingNotification(this ILogger logger, string notification);

        [LoggerMessage(EventId = EventClass + 13, Level = LogLevel.Error,
            Message = "Failed to get metadata for {Writer} with error {Error}")]
        internal static partial void FailedToGetMetadata(this ILogger logger,
            WriterGroupDataSource.DataSetWriter writer, string error);

        [LoggerMessage(EventId = EventClass + 14, Level = LogLevel.Debug,
            Message = "Loading Metadata {Major}.{Minor} for {Writer}...")]
        internal static partial void LoadingMetadata(this ILogger logger, uint major,
            uint minor, string writer);

        [LoggerMessage(EventId = EventClass + 15, Level = LogLevel.Information,
            Message = "Loading Metadata {Major}.{Minor} for {Writer} took {Duration}.")]
        internal static partial void LoadingMetadataTook(this ILogger logger, uint major,
            uint minor, string writer, TimeSpan duration);

        [LoggerMessage(EventId = EventClass + 16, Level = LogLevel.Warning,
            Message = "Failed to produce message.")]
        internal static partial void FailedToProduceMessage(this ILogger logger, Exception ex);
    }
}
