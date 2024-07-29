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
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// <para>
    /// Data set writer. A data set writer manages one or more subscriptions
    /// which can be shared across multiple writers. The subscriptions are
    /// reference counted with monitored items belong to the writer. The
    /// default use case is that one or more writers share one subscription.
    /// In case of publishing to topic space, a writer has only 1 item,
    /// but shared across a single subscription. On the other hand, if the
    /// writer contains items with multiple publishing intervals, many
    /// subscriptions must be created which part of the writer.
    /// </para>
    /// <para>
    /// The binding between items and writers is through the callback context
    /// registered with the subscription inside the session. A monitored item
    /// references the subscription callback which the subscription uses to
    /// group the value changes and route batches to the callback.
    /// </para>
    /// <para>
    /// The same subscription callback can be registered with multiple
    /// subscriptions, each with different monitored items.
    /// </para>
    /// <para>
    /// The keyframes are handled by packing all values across subscriptions
    /// into a notification. Otherwise, delta frames are split on subscription
    /// notification boundary which is fine.
    /// </para>
    /// </summary>
    public sealed partial class WriterGroupDataSource
    {
        private sealed class DataSetWriterSubscription : IDisposable, ISubscriptionCallbacks,
            IMetricsContext
        {
            /// <inheritdoc/>
            public TagList TagList { get; }

            /// <inheritdoc/>
            public IReadOnlyList<BaseMonitoredItemModel> MonitoredItems { get; private set; }

            /// <summary>
            /// Subscription id
            /// </summary>
            public SubscriptionIdentifier Id { get; }

            /// <summary>
            /// Active subscription
            /// </summary>
            public ISubscriptionHandle? Subscription { get; set; }

            /// <summary>
            /// Create subscription from a DataSetWriterModel template
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="dataSetWriter"></param>
            public DataSetWriterSubscription(WriterGroupDataSource outer, DataSetWriterModel dataSetWriter)
            {
                _outer = outer;
                _dataSetWriter = dataSetWriter;
                _logger = _outer._logger;

                _routing = _dataSetWriter.DataSet?.Routing ??
                    _outer._options.Value.DefaultDataSetRouting ?? DataSetRoutingMode.None;

                MonitoredItems = _dataSetWriter.GetMonitoredItems(
                    _outer._subscriptionConfig.Value, CreateMonitoredItemContext);
                Id = _dataSetWriter.ToSubscriptionId(_outer.WriterGroup.Name,
                    _outer._subscriptionConfig.Value, _outer._options.Value);

                _logger.LogDebug(
                    "Open new writer {Writer} with subscription {Id} in writer group {WriterGroup}...",
                        _dataSetWriter.DataSetWriterName, Id, _outer.WriterGroup.Id);

                var dataSetClassId = dataSetWriter.DataSet?.DataSetMetaData?.DataSetClassId
                    ?? Guid.Empty;
                var escWriterName = TopicFilter.Escape(
                    _dataSetWriter.DataSetWriterName ?? Constants.DefaultDataSetWriterName);
                var escWriterGroup = TopicFilter.Escape(
                    outer.WriterGroup.Name ?? Constants.DefaultWriterGroupName);

                _variables = new Dictionary<string, string>
                {
                    [PublisherConfig.DataSetWriterIdVariableName] = _dataSetWriter.Id,
                    [PublisherConfig.DataSetWriterVariableName] = escWriterName,
                    [PublisherConfig.DataSetWriterNameVariableName] = escWriterName,
                    [PublisherConfig.DataSetClassIdVariableName] = dataSetClassId.ToString(),
                    [PublisherConfig.WriterGroupIdVariableName] = outer.WriterGroup.Id,
                    [PublisherConfig.DataSetWriterGroupVariableName] = escWriterGroup,
                    [PublisherConfig.WriterGroupVariableName] = escWriterGroup
                    // ...
                };

                var builder = new TopicBuilder(_outer._options.Value, _outer.WriterGroup.MessageType,
                    new TopicTemplatesOptions
                    {
                        Telemetry = _dataSetWriter.Publishing?.QueueName
                            ?? _outer.WriterGroup.Publishing?.QueueName,
                        DataSetMetaData = _dataSetWriter.MetaData?.QueueName
                    }, _variables);

                _topic = builder.TelemetryTopic;

                _qos = _dataSetWriter.Publishing?.RequestedDeliveryGuarantee
                    ?? _outer.WriterGroup.Publishing?.RequestedDeliveryGuarantee
                    ?? _outer._options.Value.DefaultQualityOfService;
                _ttl = _dataSetWriter.Publishing?.Ttl
                    ?? _outer.WriterGroup.Publishing?.Ttl
                    ?? _outer._options.Value.DefaultMessageTimeToLive;
                _retain = _dataSetWriter.Publishing?.Retain
                    ?? _outer.WriterGroup.Publishing?.Retain
                    ?? _outer._options.Value.DefaultMessageRetention;

                _metadataTopic = builder.DataSetMetaDataTopic;
                if (string.IsNullOrWhiteSpace(_metadataTopic))
                {
                    _metadataTopic = _topic;
                }

                _contextSelector = _routing == DataSetRoutingMode.None
                    ? n => n.Context
                    : n => n.PathFromRoot == null || n.Context != null ? n.Context : new TopicContext(
                        _topic, n.PathFromRoot, _qos, _retain, _ttl,
                        _routing != DataSetRoutingMode.UseBrowseNames);

                TagList = new TagList(_outer._metrics.TagList.ToArray().AsSpan())
                {
                    new KeyValuePair<string, object?>(Constants.DataSetWriterIdTag,
                        dataSetWriter.Id),
                    new KeyValuePair<string, object?>(Constants.DataSetWriterNameTag,
                        dataSetWriter.DataSetWriterName)
                };
            }

            /// <summary>
            /// Start subscription
            /// </summary>
            public void Start()
            {
                //
                // Creating inner OPC UA subscription object. This will create a session
                // if none already exist and transfer the subscription into the session
                // management realm
                //
                _outer._subscriptionManager.RegisterSubscriptionCallbacks(
                    Id.Subscription, this, this);

                _frameCount = 0;
                InitializeMetaDataTrigger();
                InitializeKeepAlive();

                _metadataTimer?.Start();
                _logger.LogInformation(
                    "New writer with subscription {Id} in writer group {WriterGroup} opened.",
                    Id, _outer.WriterGroup.Id);
            }

            /// <summary>
            /// Update subscription content
            /// </summary>
            /// <param name="dataSetWriter"></param>
            public void Update(DataSetWriterModel dataSetWriter)
            {
                _logger.LogDebug(
                    "Updating writer with subscription {Id} in writer group {WriterGroup}...",
                    Id, _outer.WriterGroup.Id);

                _dataSetWriter = dataSetWriter.Clone();

                MonitoredItems = _dataSetWriter.GetMonitoredItems(
                    _outer._subscriptionConfig.Value, CreateMonitoredItemContext);

                var subscription = Subscription;
                if (subscription == null)
                {
                    _logger.LogWarning(
                        "Writer does not have a subscription to update yet!");
                    return;
                }
                _frameCount = 0;
                InitializeMetaDataTrigger();
                InitializeKeepAlive();

                // Apply changes
                subscription.Update();

                _logger.LogInformation(
                    "Updated subscription for writer {Id} in writer group {WriterGroup}.",
                    Id, _outer.WriterGroup.Id);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                try
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    _metadataTimer?.Stop();

                    Close();
                }
                finally
                {
                    _metadataTimer?.Dispose();
                    _metadataTimer = null;
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionUpdated(ISubscriptionHandle? subscription)
            {
                Subscription = subscription;

                if (subscription != null)
                {
                    _logger.LogInformation(
                        "Writer with subscription {Id} in writer group {WriterGroup} new subscription received.",
                        Id, _outer.WriterGroup.Id);
                }
                else
                {
                    _logger.LogInformation(
                        "Writer with subscription {Id} in writer group {WriterGroup} subscription removed.",
                        Id, _outer.WriterGroup.Id);
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionKeepAlive(IOpcUaSubscriptionNotification notification)
            {
                Interlocked.Increment(ref _outer._keepAliveCount);
                if (_sendKeepAlives)
                {
                    CallMessageReceiverDelegates(notification);
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionDataChangeReceived(IOpcUaSubscriptionNotification notification)
            {
                CallMessageReceiverDelegates(ProcessKeyFrame(notification));

                IOpcUaSubscriptionNotification ProcessKeyFrame(IOpcUaSubscriptionNotification notification)
                {
                    var keyFrameCount = _dataSetWriter.KeyFrameCount
                        ?? _outer._subscriptionConfig.Value.DefaultKeyFrameCount ?? 0;
                    if (keyFrameCount > 0)
                    {
                        var frameCount = Interlocked.Increment(ref _frameCount);
                        if (((frameCount - 1) % keyFrameCount) == 0)
                        {
                            notification.TryUpgradeToKeyFrame();
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
                    _outer._heartbeats.Count += heartbeats;
                    _outer._overflows.Count += overflows;
                    if (liveData)
                    {
                        if (_outer._dataChanges.Count >= kNumberOfInvokedMessagesResetThreshold ||
                            _outer._valueChanges.Count >= kNumberOfInvokedMessagesResetThreshold)
                        {
                            _logger.LogDebug(
                                "Notifications counter in subscription {Id} has been reset to prevent" +
                                " overflow. So far, {DataChangesCount} data changes and {ValueChangesCount} " +
                                "value changes were invoked by message source.",
                                Id, _outer._dataChanges.Count, _outer._valueChanges.Count);
                            _outer._dataChanges.Count = 0;
                            _outer._valueChanges.Count = 0;
                            _outer._heartbeats.Count = 0;
                            _outer.OnCounterReset?.Invoke(this, EventArgs.Empty);
                        }

                        _outer._valueChanges.Count += valueChanges;
                        _outer._dataChanges.Count++;
                    }
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionCyclicReadCompleted(IOpcUaSubscriptionNotification notification)
            {
                CallMessageReceiverDelegates(notification);
            }

            /// <inheritdoc/>
            public void OnSubscriptionCyclicReadDiagnosticsChange(int valuesSampled, int overflows)
            {
                lock (_lock)
                {
                    _outer._overflows.Count += overflows;

                    if (_outer._dataChanges.Count >= kNumberOfInvokedMessagesResetThreshold ||
                        _outer._sampledValues.Count >= kNumberOfInvokedMessagesResetThreshold)
                    {
                        _logger.LogDebug(
                            "Notifications counter in subscription {Id} has been reset to prevent" +
                            " overflow. So far, {ReadCount} data changes and {ValuesCount} " +
                            "value changes were invoked by message source.",
                            Id, _outer._cyclicReads.Count, _outer._sampledValues.Count);
                        _outer._cyclicReads.Count = 0;
                        _outer._sampledValues.Count = 0;
                        _outer.OnCounterReset?.Invoke(this, EventArgs.Empty);
                    }

                    _outer._sampledValues.Count += valuesSampled;
                    _outer._cyclicReads.Count++;
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionEventReceived(IOpcUaSubscriptionNotification notification)
            {
                CallMessageReceiverDelegates(notification);
            }

            /// <inheritdoc/>
            public void OnSubscriptionEventDiagnosticsChange(bool liveData, int events, int overflows,
                int modelChanges)
            {
                lock (_lock)
                {
                    _outer._modelChanges.Count += modelChanges;
                    _outer._overflows.Count += overflows;

                    if (liveData)
                    {
                        if (_outer._events.Count >= kNumberOfInvokedMessagesResetThreshold ||
                            _outer._eventNotification.Count >= kNumberOfInvokedMessagesResetThreshold)
                        {
                            // reset both
                            _logger.LogDebug(
                                "Notifications counter in subscription {Id} has been reset to prevent" +
                                " overflow. So far, {EventChangesCount} event changes and {EventValueChangesCount} " +
                                "event value changes were invoked by message source.",
                                Id, _outer._events.Count, _outer._eventNotification.Count);
                            _outer._events.Count = 0;
                            _outer._eventNotification.Count = 0;
                            _outer._modelChanges.Count = 0;

                            _outer.OnCounterReset?.Invoke(this, EventArgs.Empty);
                        }

                        _outer._eventNotification.Count += events;
                        _outer._events.Count++;
                    }
                }
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
            /// Close subscription
            /// </summary>
            /// <returns></returns>
            private void Close()
            {
                var subscription = Subscription;
                if (subscription == null)
                {
                    return;
                }

                _logger.LogDebug("Closing writer with subscription {Id} in writer group {WriterGroup}...",
                    Id, _outer.WriterGroup.Id);

                subscription.Close();

                _logger.LogInformation("Writer with subscription {Id} in writer group {WriterGroup} closed.",
                    Id, _outer.WriterGroup.Id);
            }

            /// <summary>
            /// Initialize sending of keep alive messages
            /// </summary>
            private void InitializeKeepAlive()
            {
                _sendKeepAlives = _dataSetWriter.DataSet?.SendKeepAlive
                    ?? _outer._subscriptionConfig.Value.EnableDataSetKeepAlives == true;
            }

            /// <summary>
            /// Initializes the Metadata triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeMetaDataTrigger()
            {
                var metaDataSendInterval = _dataSetWriter.MetaDataUpdateTime ?? TimeSpan.Zero;
                if (metaDataSendInterval > TimeSpan.Zero &&
                    _outer._subscriptionConfig.Value.DisableDataSetMetaData != true)
                {
                    if (_metadataTimer == null)
                    {
                        _metadataTimer = new TimerEx(metaDataSendInterval, _outer._timeProvider);
                        _metadataTimer.Elapsed += MetadataTimerElapsed;
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

                _logger.LogDebug("Insert metadata message into Subscription {Id}...", Id);
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
            /// <param name="subscriptionNotification"></param>
            /// <param name="metaDataTimer"></param>
            private void CallMessageReceiverDelegates(IOpcUaSubscriptionNotification subscriptionNotification,
                bool metaDataTimer = false)
            {
                try
                {
                    lock (_lock)
                    {
                        foreach (var notification in subscriptionNotification.Split(_contextSelector))
                        {
                            var itemContext = notification.Context as MonitoredItemContext;

                            if (notification.MetaData != null)
                            {
                                var sendMetadata = metaDataTimer;
                                //
                                // Only send if called from metadata timer or if the metadata version changes.
                                // Metadata reference is owned by the notification/message, a new metadata is
                                // created when it changes so old one is not mutated and this should be safe.
                                //
                                if (LastMetaData?.MetaData.DataSetMetaData.MajorVersion
                                        != notification.MetaData.DataSetMetaData.MajorVersion ||
                                    LastMetaData?.MetaData.MinorVersion
                                        != notification.MetaData.MinorVersion)
                                {
                                    LastMetaData = new PublishedDataSetMessageSchemaModel
                                    {
                                        MetaData = notification.MetaData,
                                        TypeName = null,
                                        DataSetFieldContentFlags =
                                            _dataSetWriter.DataSetFieldContentMask,
                                        DataSetMessageContentFlags =
                                            _dataSetWriter.MessageSettings?.DataSetMessageContentMask
                                    };
                                    Interlocked.Increment(ref _outer._metadataChanges);
                                    sendMetadata = true;
                                }
                                if (sendMetadata)
                                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                                    var metadata = new MetadataNotificationModel(notification, _outer._timeProvider)
                                    {
                                        Context = CreateMessageContext(_metadataTopic, QoS.AtLeastOnce, true,
                                            _metadataTimer?.Interval ?? _dataSetWriter.MetaDataUpdateTime,
                                            () => Interlocked.Increment(ref _metadataSequenceNumber))
                                    };
#pragma warning restore CA2000 // Dispose objects before losing scope
                                    _outer.OnMessage?.Invoke(this, metadata);
                                    InitializeMetaDataTrigger();
                                }
                            }

                            if (!metaDataTimer)
                            {
                                Debug.Assert(notification.Notifications != null);
                                notification.Context = CreateMessageContext(_topic, _qos, _retain, _ttl,
                                    () => Interlocked.Increment(ref _dataSetSequenceNumber), itemContext);
                                _logger.LogTrace("Enqueuing notification: {Notification}",
                                    notification.ToString());
                                _outer.OnMessage?.Invoke(this, notification);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to produce message.");
                }

                WriterGroupContext CreateMessageContext(string topic, QoS? qos, bool? retain, TimeSpan? ttl,
                    Func<uint> sequenceNumber, MonitoredItemContext? item = null)
                {
                    _outer.GetWriterGroup(out var writerGroup, out var networkMessageSchema);
                    return new WriterGroupContext
                    {
                        PublisherId = _outer._options.Value.PublisherId ?? Constants.DefaultPublisherId,
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
            /// Data set metadata notification
            /// </summary>
            public sealed record class MetadataNotificationModel : IOpcUaSubscriptionNotification
            {
                /// <inheritdoc/>
                public uint SequenceNumber { get; }

                /// <inheritdoc/>
                public MessageType MessageType => MessageType.Metadata;

                /// <inheritdoc/>
                public PublishedDataSetMetaDataModel? MetaData { get; }

                /// <inheritdoc/>
                public string? SubscriptionName { get; }

                /// <inheritdoc/>
                public ushort SubscriptionId { get; }

                /// <inheritdoc/>
                public string? DataSetName { get; }

                /// <inheritdoc/>
                public string? EndpointUrl { get; }

                /// <inheritdoc/>
                public string? ApplicationUri { get; }

                /// <inheritdoc/>
                public DateTimeOffset? PublishTimestamp { get; }

                /// <inheritdoc/>
                public DateTimeOffset CreatedTimestamp { get; }

                /// <inheritdoc/>
                public uint? PublishSequenceNumber => null;

                /// <inheritdoc/>
                public object? Context { get; set; }

                /// <inheritdoc/>
                public IServiceMessageContext ServiceMessageContext { get; set; }

                /// <inheritdoc/>
                public IList<MonitoredItemNotificationModel> Notifications { get; }

                /// <inheritdoc/>
                public MetadataNotificationModel(IOpcUaSubscriptionNotification notification,
                    TimeProvider timeProvider)
                {
                    SequenceNumber = notification.SequenceNumber;
                    DataSetName = notification.DataSetName;
                    ServiceMessageContext = notification.ServiceMessageContext;
                    MetaData = notification.MetaData;
                    CreatedTimestamp = timeProvider.GetUtcNow();
                    PublishTimestamp = notification.PublishTimestamp;
                    SubscriptionId = notification.SubscriptionId;
                    SubscriptionName = notification.SubscriptionName;
                    ApplicationUri = notification.ApplicationUri;
                    EndpointUrl = notification.EndpointUrl;
                    Notifications = Array.Empty<MonitoredItemNotificationModel>();
                }

                /// <inheritdoc/>
                public bool TryUpgradeToKeyFrame()
                {
                    // Not supported
                    return false;
                }

                /// <inheritdoc/>
                public IEnumerable<IOpcUaSubscriptionNotification> Split(
                    Func<MonitoredItemNotificationModel, object?> selector)
                {
                    return this.YieldReturn();
                }

                /// <inheritdoc/>
                public void Dispose()
                {
                    // Nothing to do
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
                        return new TopicBuilder(subscription._outer._options.Value,
                            subscription._outer.WriterGroup.MessageType,
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

            private const long kNumberOfInvokedMessagesResetThreshold = long.MaxValue - 10000;
            private readonly WriterGroupDataSource _outer;
            private readonly ILogger _logger;
            private readonly Func<MonitoredItemNotificationModel, object?> _contextSelector;
            private readonly object _lock = new();
            private TimerEx? _metadataTimer;
            private volatile uint _frameCount;
            private readonly string _topic;
            private readonly QoS? _qos;
            private readonly TimeSpan? _ttl;
            private readonly bool? _retain;
            private readonly string _metadataTopic;
            private readonly Dictionary<string, string> _variables;
            private readonly DataSetRoutingMode _routing;
            private DataSetWriterModel _dataSetWriter;
            private uint _dataSetSequenceNumber;
            private uint _metadataSequenceNumber;
            private bool _sendKeepAlives;
            private bool _disposed;
        }
    }
}
