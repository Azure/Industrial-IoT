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
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using Nito.AsyncEx;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp;
    using System.Diagnostics.Metrics;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;

    public sealed partial class WriterGroupDataSource
    {
        /// <summary>
        /// Represents a data set writer inside a writer group
        /// </summary>
        private sealed class DataSetWriter : ISubscriber, IMetricsContext, IAsyncDisposable
        {
            /// <inheritdoc/>
            public TagList TagList { get; }

            /// <summary>
            /// Name of the data set writer
            /// </summary>
            public string? Name => _dataSetWriter.DataSetWriterName;

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
            /// <param name="dataSetWriter"></param>
            /// <param name="logger"></param>
            private DataSetWriter(WriterGroupDataSource group,
                DataSetWriterModel dataSetWriter, ILogger<DataSetWriter> logger)
            {
                _group = group;
                _logger = logger;
                _metaDataLoader = new Lazy<MetaDataLoader>(() => new MetaDataLoader(this), true);

                // Set the writer configuration
                _dataSetWriter = CloneWithUniqueName(dataSetWriter);
                if (_dataSetWriter?.DataSet?.DataSetSource == null)
                {
                    throw new ArgumentException("DataSet source missing", nameof(dataSetWriter));
                }

                // Not yet in subscription array and should not have a null name here
                Debug.Assert(_dataSetWriter.DataSetWriterName != null);
                Debug.Assert(!_group._dataSetWriters.ContainsKey(_dataSetWriter.DataSetWriterName));

                _logger.LogDebug("Creating new writer {Writer} in writer group {WriterGroup}...",
                    _dataSetWriter.DataSetWriterName, _group._writerGroup.Id);

                // Create monitored items
                MonitoredItems = _dataSetWriter.DataSet.DataSetSource.ToMonitoredItems(
                    _group._subscriptionConfig.Value, CreateMonitoredItemContext,
                    _dataSetWriter.DataSet.ExtensionFields);

                _routing = _dataSetWriter.DataSet.Routing ??
                    _group._options.Value.DefaultDataSetRouting ?? DataSetRoutingMode.None;
                _template = _dataSetWriter.DataSet.DataSetSource.ToSubscriptionModel(
                    _dataSetWriter.DataSet.DataSetMetaData, _group._subscriptionConfig.Value,
                    _routing != DataSetRoutingMode.None,
                    _group._options.Value.IgnoreConfiguredPublishingIntervals);
                _connection = _dataSetWriter.GetConnection(_group._writerGroup.Id,
                    _group._options.Value);

                var dataSetClassId = dataSetWriter.DataSet?.DataSetMetaData?.DataSetClassId
                    ?? Guid.Empty;
                var escWriterName = TopicFilter.Escape(
                    _dataSetWriter.DataSetWriterName ?? Constants.DefaultDataSetWriterName);
                var escWriterGroup = TopicFilter.Escape(
                    _group._writerGroup.Name ?? Constants.DefaultWriterGroupName);

                _variables = new Dictionary<string, string>
                {
                    [PublisherConfig.DataSetWriterIdVariableName] = _dataSetWriter.Id,
                    [PublisherConfig.DataSetWriterVariableName] = escWriterName,
                    [PublisherConfig.DataSetWriterNameVariableName] = escWriterName,
                    [PublisherConfig.DataSetClassIdVariableName] = dataSetClassId.ToString(),
                    [PublisherConfig.WriterGroupIdVariableName] = _group._writerGroup.Id,
                    [PublisherConfig.DataSetWriterGroupVariableName] = escWriterGroup,
                    [PublisherConfig.WriterGroupVariableName] = escWriterGroup
                    // ...
                };

                var builder = new TopicBuilder(_group._options.Value, _group._writerGroup.MessageType,
                    new TopicTemplatesOptions
                    {
                        Telemetry = _dataSetWriter.Publishing?.QueueName
                            ?? _group._writerGroup.Publishing?.QueueName,
                        DataSetMetaData = _dataSetWriter.MetaData?.QueueName
                    }, _variables);

                _topic = builder.TelemetryTopic;

                _qos = _dataSetWriter.Publishing?.RequestedDeliveryGuarantee
                    ?? _group._writerGroup.Publishing?.RequestedDeliveryGuarantee
                    ?? _group._options.Value.DefaultQualityOfService;
                _ttl = _dataSetWriter.Publishing?.Ttl
                    ?? _group._writerGroup.Publishing?.Ttl
                    ?? _group._options.Value.DefaultMessageTimeToLive;
                _retain = _dataSetWriter.Publishing?.Retain
                    ?? _group._writerGroup.Publishing?.Retain
                    ?? _group._options.Value.DefaultMessageRetention;

                _metadataTopic = builder.DataSetMetaDataTopic;
                if (string.IsNullOrWhiteSpace(_metadataTopic))
                {
                    _metadataTopic = _topic;
                }

                // TODO:    _contextSelector = _routing == DataSetRoutingMode.None
                // TODO:        ? n => n.Context
                // TODO:        : n => n.PathFromRoot == null || n.Context != null ? n.Context : new TopicContext(
                // TODO:            _topic, n.PathFromRoot, _qos, _retain, _ttl,
                // TODO:            _routing != DataSetRoutingMode.UseBrowseNames);

                TagList = new TagList(_group._metrics.TagList.ToArray().AsSpan())
                {
                    new KeyValuePair<string, object?>(Constants.DataSetWriterIdTag,
                        dataSetWriter.Id),
                    new KeyValuePair<string, object?>(Constants.DataSetWriterNameTag,
                        dataSetWriter.DataSetWriterName)
                };
            }

            /// <summary>
            /// Create subscription
            /// </summary>
            /// <param name="group"></param>
            /// <param name="dataSetWriter"></param>
            /// <param name="loggerFactory"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public async static ValueTask<DataSetWriter> CreateAsync(WriterGroupDataSource group,
                DataSetWriterModel dataSetWriter, ILoggerFactory loggerFactory, CancellationToken ct)
            {
                var writer = new DataSetWriter(group, dataSetWriter,
                    loggerFactory.CreateLogger<DataSetWriter>());

                writer.Subscription = await group._clients.CreateSubscriptionAsync(
                    writer._connection.Connection, writer._template, writer, ct).ConfigureAwait(false);

                writer.InitializeMetaDataTrigger();
                writer.InitializeKeepAlive();

                group._logger.LogInformation("New writer in writer group {WriterGroup} opened.",
                    group._writerGroup.Id);

                return writer;
            }

            /// <summary>
            /// Update subscription content
            /// </summary>
            /// <param name="dataSetWriter"></param>
            /// <param name="ct"></param>
            /// <exception cref="ArgumentException"></exception>
            public async ValueTask UpdateAsync(DataSetWriterModel dataSetWriter,
                CancellationToken ct)
            {
                _logger.LogDebug("Updating writer in writer group {WriterGroup}...",
                    _group._writerGroup.Id);

                _dataSetWriter = CloneWithUniqueName(dataSetWriter);
                if (_dataSetWriter?.DataSet?.DataSetSource == null)
                {
                    throw new ArgumentException("DataSet source missing", nameof(dataSetWriter));
                }

                _routing = _dataSetWriter.DataSet.Routing ??
                    _group._options.Value.DefaultDataSetRouting ?? DataSetRoutingMode.None;

                MonitoredItems = _dataSetWriter.DataSet.DataSetSource.ToMonitoredItems(
                    _group._subscriptionConfig.Value, CreateMonitoredItemContext,
                    _dataSetWriter.DataSet.ExtensionFields);

                var template = _dataSetWriter.DataSet.DataSetSource.ToSubscriptionModel(
                    _dataSetWriter.DataSet.DataSetMetaData, _group._subscriptionConfig.Value,
                    _routing != DataSetRoutingMode.None,
                    _group._options.Value.IgnoreConfiguredPublishingIntervals);
                var connection = _dataSetWriter.GetConnection(_group._writerGroup.Id,
                    _group._options.Value);

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

                    _logger.LogInformation(
                        "Recreated subscription in writer group {WriterGroup}...",
                       _group._writerGroup.Id);
                }
                else
                {
                    // Trigger reevaluation
                    Subscription.NotifyMonitoredItemsChanged();

                    _logger.LogInformation(
                        "Updated monitored items in writer group {WriterGroup}.",
                        _group._writerGroup.Id);
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
                if (_group._options.Value.DisableDataSetMetaData != true)
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
                    var keyFrameCount = _dataSetWriter.KeyFrameCount
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
            public void OnSubscriptionDataDiagnosticsChange(bool liveData, int valueChanges, int overflows,
                int heartbeats)
            {
                lock (_lock)
                {
                    _group._heartbeats.Count += heartbeats;
                    _group._overflows.Count += overflows;
                    if (liveData)
                    {
                        if (_group._dataChanges.Count >= kNumberOfInvokedMessagesResetThreshold ||
                            _group._valueChanges.Count >= kNumberOfInvokedMessagesResetThreshold)
                        {
                            _logger.LogDebug(
                                "Notifications counter has been reset to prevent" +
                                " overflow. So far, {DataChangesCount} data changes and {ValueChangesCount} " +
                                "value changes were invoked by message source.",
                                _group._dataChanges.Count, _group._valueChanges.Count);
                            _group._dataChanges.Count = 0;
                            _group._valueChanges.Count = 0;
                            _group._heartbeats.Count = 0;
                            _group.OnCounterReset?.Invoke(this, EventArgs.Empty);
                        }

                        _group._valueChanges.Count += valueChanges;
                        _group._dataChanges.Count++;
                    }
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionCyclicReadCompleted(OpcUaSubscriptionNotification notification)
            {
                CallMessageReceiverDelegates(notification);
            }

            /// <inheritdoc/>
            public void OnSubscriptionCyclicReadDiagnosticsChange(int valuesSampled, int overflows)
            {
                lock (_lock)
                {
                    _group._overflows.Count += overflows;

                    if (_group._dataChanges.Count >= kNumberOfInvokedMessagesResetThreshold ||
                        _group._sampledValues.Count >= kNumberOfInvokedMessagesResetThreshold)
                    {
                        _logger.LogDebug(
                            "Notifications counter has been reset to prevent" +
                            " overflow. So far, {ReadCount} data changes and {ValuesCount} " +
                            "value changes were invoked by message source.",
                            _group._cyclicReads.Count, _group._sampledValues.Count);
                        _group._cyclicReads.Count = 0;
                        _group._sampledValues.Count = 0;
                        _group.OnCounterReset?.Invoke(this, EventArgs.Empty);
                    }

                    _group._sampledValues.Count += valuesSampled;
                    _group._cyclicReads.Count++;
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionEventReceived(OpcUaSubscriptionNotification notification)
            {
                CallMessageReceiverDelegates(notification);
            }

            /// <inheritdoc/>
            public void OnSubscriptionEventDiagnosticsChange(bool liveData, int events, int overflows,
                int modelChanges)
            {
                lock (_lock)
                {
                    _group._modelChanges.Count += modelChanges;
                    _group._overflows.Count += overflows;

                    if (liveData)
                    {
                        if (_group._events.Count >= kNumberOfInvokedMessagesResetThreshold ||
                            _group._eventNotification.Count >= kNumberOfInvokedMessagesResetThreshold)
                        {
                            // reset both
                            _logger.LogDebug(
                                "Notifications counter has been reset to prevent" +
                                " overflow. So far, {EventChangesCount} event changes and {EventValueChangesCount} " +
                                "event value changes were invoked by message source.",
                                _group._events.Count, _group._eventNotification.Count);
                            _group._events.Count = 0;
                            _group._eventNotification.Count = 0;
                            _group._modelChanges.Count = 0;

                            _group.OnCounterReset?.Invoke(this, EventArgs.Empty);
                        }

                        _group._eventNotification.Count += events;
                        _group._events.Count++;
                    }
                }
            }

            /// <summary>
            /// Clones the writer configuration object and sets a unique name so that
            /// there are no naming conflicts between writers in the writer group.
            /// We keep the original name so we are not randomly assigning new names
            /// for writers that already have a unique name.
            /// </summary>
            /// <param name="dataSetWriter"></param>
            /// <returns></returns>
            private DataSetWriterModel CloneWithUniqueName(DataSetWriterModel dataSetWriter)
            {
                string uniqueName;
                if (_dataSetWriter != null &&
                    _originalWriterName == dataSetWriter.DataSetWriterName)
                {
                    //
                    // The name has not changed, the original is the same as previously so
                    // keep current unique name which is in the current writer configuration.
                    //
                    uniqueName = _dataSetWriter.DataSetWriterName ?? string.Empty;
                }
                else
                {
                    var originalName = uniqueName =
                        dataSetWriter.DataSetWriterName ?? string.Empty;

                    //
                    // Select a unique name inside the writer group even if there are more
                    // writers in this group with same names which the control plane allows.
                    //
                    for (var index = 1; ; index++)
                    {
                        if (!_group._dataSetWriters.Values.Any(e => e.Name == uniqueName))
                        {
                            break;
                        }
                        uniqueName = $"{originalName}{index}";
                    }
                }

                // Store origina writer name for later comparison
                _originalWriterName = dataSetWriter.DataSetWriterName;

                return dataSetWriter with
                {
                    DataSetWriterName = uniqueName,
                    DataSet = dataSetWriter.DataSet.Clone(),
                    MessageSettings = dataSetWriter.MessageSettings == null ? null :
                        dataSetWriter.MessageSettings with { }
                };
            }

            /// <summary>
            /// Create monitored item context
            /// </summary>
            /// <param name="settings"></param>
            /// <returns></returns>
            private object? CreateMonitoredItemContext(PublishingQueueSettingsModel? settings)
            {
                return settings?.QueueName == null ? null : new LazilyEvaluatedContext(this, settings);
            }

            /// <summary>
            /// Initialize sending of keep alive messages
            /// </summary>
            private void InitializeKeepAlive()
            {
                _sendKeepAlives = _dataSetWriter.DataSet?.SendKeepAlive
                    ?? _group._options.Value.EnableDataSetKeepAlives == true;
            }

            /// <summary>
            /// Initializes the Metadata triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeMetaDataTrigger()
            {
                var metaDataSendInterval = _dataSetWriter.MetaDataUpdateTime ?? TimeSpan.Zero;
                if (metaDataSendInterval > TimeSpan.Zero &&
                    _group._options.Value.DisableDataSetMetaData != true)
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
                        var itemContext = notification.Context as MonitoredItemContext;

                        var metadata = MetaData;
                        if (metadata == null && _group._options.Value.DisableDataSetMetaData != true)
                        {
                            // Block until we have metadata or just continue
                            _metaDataLoader.Value.BlockUntilLoaded(TimeSpan.FromSeconds(10));
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
                                    Context = CreateMessageContext(_metadataTopic, QoS.AtLeastOnce, true,
                                        _metadataTimer?.Interval ?? _dataSetWriter.MetaDataUpdateTime,
                                        () => Interlocked.Increment(ref _metadataSequenceNumber), metadata)
                                };
#pragma warning restore CA2000 // Dispose objects before losing scope
                                _group.OnMessage?.Invoke(this, metadataFrame);
                                InitializeMetaDataTrigger();
                            }
                        }

                        if (!sourceIsMetaDataTimer)
                        {
                            Debug.Assert(notification.Notifications != null);
                            notification.Context = CreateMessageContext(_topic, _qos, _retain, _ttl,
                                () => Interlocked.Increment(ref _dataSetSequenceNumber), metadata, itemContext);
                            _logger.LogTrace("Enqueuing notification: {Notification}",
                                notification.ToString());
                            _group.OnMessage?.Invoke(this, notification);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to produce message.");
                }

                DataSetWriterContext CreateMessageContext(string topic, QoS? qos, bool? retain,
                    TimeSpan? ttl, Func<uint> sequenceNumber, PublishedDataSetMessageSchemaModel? metadata,
                    MonitoredItemContext? item = null)
                {
                    _group.GetWriterGroup(out var writerGroup, out var networkMessageSchema);
                    return new DataSetWriterContext
                    {
                        PublisherId = _group._options.Value.PublisherId ?? Constants.DefaultPublisherId,
                        DataSetWriterId = (ushort)Index,
                        MetaData = metadata,
                        Writer = _dataSetWriter,
                        NextWriterSequenceNumber = sequenceNumber,
                        WriterGroup = writerGroup,
                        Schema = networkMessageSchema,
                        Retain = item?.Retain ?? retain,
                        Ttl = item?.Ttl ?? ttl,
                        Topic = item?.Topic ?? topic,
                        Qos = item?.Qos ?? qos
                    };
                }
            }

            /// <summary>
            /// Context used to split monitored item notification
            /// </summary>
            private abstract class MonitoredItemContext
            {
                /// <summary>
                /// Topic for the message if not metadata message
                /// </summary>
                public abstract string Topic { get; }

                /// <summary>
                /// Topic for the message if not metadata message
                /// </summary>
                public abstract QoS? Qos { get; }

                /// <summary>
                /// Time to live
                /// </summary>
                public abstract TimeSpan? Ttl { get; }

                /// <summary>
                /// Retain
                /// </summary>
                public abstract bool? Retain { get; }
            }

            /// <summary>
            /// Topic context
            /// </summary>
            private sealed class TopicContext : MonitoredItemContext
            {
                /// <inheritdoc/>
                public override string Topic { get; }
                /// <inheritdoc/>
                public override QoS? Qos { get; }
                /// <inheritdoc/>
                public override TimeSpan? Ttl { get; }
                /// <inheritdoc/>
                public override bool? Retain { get; }

                /// <summary>
                /// Create
                /// </summary>
                /// <param name="root"></param>
                /// <param name="subpath"></param>
                /// <param name="qos"></param>
                /// <param name="retain"></param>
                /// <param name="ttl"></param>
                /// <param name="includeNamespaceIndex"></param>
                public TopicContext(string root, RelativePath subpath, QoS? qos,
                    bool? retain, TimeSpan? ttl, bool includeNamespaceIndex)
                {
                    var sb = new StringBuilder().Append(root);
                    foreach (var path in subpath.Elements)
                    {
                        sb.Append('/');
                        if (path.TargetName.NamespaceIndex != 0 && includeNamespaceIndex)
                        {
                            sb.Append(path.TargetName.NamespaceIndex).Append(':');
                        }
                        sb.Append(TopicFilter.Escape(path.TargetName.Name));
                    }
                    Topic = sb.ToString();
                    Ttl = ttl;
                    Retain = retain;
                    Qos = qos;
                }

                /// <inheritdoc/>
                public override bool Equals(object? obj)
                {
                    return obj is TopicContext context &&
                        Topic == context.Topic && Qos == context.Qos;
                }

                /// <inheritdoc/>
                public override int GetHashCode()
                {
                    return HashCode.Combine(Topic, Qos);
                }
            }

            /// <summary>
            /// Lazy context
            /// </summary>
            private sealed class LazilyEvaluatedContext : MonitoredItemContext
            {
                /// <inheritdoc/>
                public override string Topic => _topic.Value;
                /// <inheritdoc/>
                public override QoS? Qos => _settings.RequestedDeliveryGuarantee;
                /// <inheritdoc/>
                public override TimeSpan? Ttl => _settings.Ttl;
                /// <inheritdoc/>
                public override bool? Retain => _settings.Retain;

                /// <summary>
                /// Create context
                /// </summary>
                /// <param name="subscription"></param>
                /// <param name="settings"></param>
                public LazilyEvaluatedContext(DataSetWriter subscription,
                    PublishingQueueSettingsModel settings)
                {
                    Debug.Assert(settings.QueueName != null);
                    _settings = settings;
                    _topic = new Lazy<string>(() =>
                    {
                        return new TopicBuilder(subscription._group._options.Value,
                            subscription._group._writerGroup.MessageType,
                            new TopicTemplatesOptions
                            {
                                Telemetry = settings.QueueName
                            }, subscription._variables).TelemetryTopic;
                    });
                }

                /// <inheritdoc/>
                public override bool Equals(object? obj)
                {
                    return obj is LazilyEvaluatedContext context && _settings == context._settings;
                }

                /// <inheritdoc/>
                public override int GetHashCode()
                {
                    return _settings.GetHashCode();
                }

                private readonly Lazy<string> _topic;
                private readonly PublishingQueueSettingsModel _settings;
            }

            /// <summary>
            /// Create observable metrics
            /// </summary>
            private void InitializeMetrics()
            {
                _group._meter.CreateObservableUpDownCounter("iiot_edge_publisher_good_metadata",
                    () => new Measurement<long>(_metadataLoadSuccess, _group._metrics.TagList),
                    description: "Number of successful metadata load operations.");
                _group._meter.CreateObservableUpDownCounter("iiot_edge_publisher_bad_metadata",
                    () => new Measurement<long>(_metadataLoadFailures, _group._metrics.TagList),
                    description: "Number of failed metadata load operations.");
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
                public MetaDataLoader(DataSetWriter subscription)
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
                            Interlocked.Increment(ref _writer._metadataLoadSuccess);
                        }
                        catch (OperationCanceledException)
                        {
                            _tcs.TrySetCanceled(ct);
                        }
                        catch (Exception ex)
                        {
                            _writer._logger.LogError(
                                "Failed to get metadata for {Subscription} with error {Error}",
                                this, ex.Message);

                            _tcs.TrySetException(ex);
                            Interlocked.Increment(ref _writer._metadataLoadFailures);
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
                    var dataSetMetaData = _writer._dataSetWriter.DataSet?.DataSetMetaData;
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
                    _writer._logger.LogDebug("Loading Metadata {Major}.{Minor} for {Writer}...",
                        dataSetMetaData.MajorVersion ?? 1, minor, _writer._dataSetWriter.Id);

                    var metaData = await subscription.CollectMetaDataAsync(_writer,
                        dataSetMetaData, minor, ct).ConfigureAwait(false);

                    _writer._logger.LogInformation(
                        "Loading Metadata {Major}.{Minor} for {Writer} took {Duration}.",
                        dataSetMetaData.MajorVersion ?? 1, minor, _writer._dataSetWriter.Id,
                        sw.Elapsed);

                    MetaData = new PublishedDataSetMessageSchemaModel
                    {
                        MetaData = metaData,
                        TypeName = null,
                        DataSetFieldContentFlags =
                            _writer._dataSetWriter.DataSetFieldContentMask,
                        DataSetMessageContentFlags =
                            _writer._dataSetWriter.MessageSettings?.DataSetMessageContentMask
                    };
                }

                private TaskCompletionSource _tcs;
                private readonly Task _loader;
                private readonly CancellationTokenSource _cts = new();
                private readonly AsyncAutoResetEvent _trigger = new();
                private readonly DataSetWriter _writer;
            }

            private readonly WriterGroupDataSource _group;
            private readonly ILogger _logger;
            private readonly object _lock = new();
            private volatile uint _frameCount;
            private readonly string _topic;
            private readonly QoS? _qos;
            private readonly TimeSpan? _ttl;
            private readonly bool? _retain;
            private readonly string _metadataTopic;
            private readonly Dictionary<string, string> _variables;
            private uint? _lastMajorVersion;
            private uint? _lastMinorVersion;
            private TimerEx? _metadataTimer;
            private DataSetRoutingMode _routing;
            private SubscriptionModel _template;
            private ConnectionIdentifier _connection;
            private string? _originalWriterName;
            private DataSetWriterModel _dataSetWriter;
            private readonly Lazy<MetaDataLoader> _metaDataLoader;
            private uint _dataSetSequenceNumber;
            private uint _metadataSequenceNumber;
            private bool _sendKeepAlives;
            private bool _disposed;
            private int _metadataLoadSuccess;
            private int _metadataLoadFailures;
        }
    }
}
