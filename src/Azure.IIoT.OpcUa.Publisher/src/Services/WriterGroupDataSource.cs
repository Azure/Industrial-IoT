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
    using Azure.IIoT.OpcUa.Encoders.PubSub.Schemas;
    using Autofac;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Triggers dataset writer messages on subscription changes
    /// </summary>
    public sealed class WriterGroupDataSource : IWriterGroupNotifications, IDisposable
    {
        /// <summary>
        /// Create trigger from writer group
        /// </summary>
        /// <param name="sink"></param>
        /// <param name="options"></param>
        /// <param name="subscriptionManager"></param>
        /// <param name="subscriptionConfig"></param>
        /// <param name="metrics"></param>
        /// <param name="logger"></param>
        public WriterGroupDataSource(INotificationSink sink,
            IOptions<PublisherOptions> options, IOpcUaSubscriptionManager subscriptionManager,
            IOptions<OpcUaSubscriptionOptions> subscriptionConfig,
            IMetricsContext? metrics, ILogger<WriterGroupDataSource> logger)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ??
                throw new ArgumentNullException(nameof(subscriptionManager));
            _subscriptionConfig = subscriptionConfig ??
                throw new ArgumentNullException(nameof(subscriptionConfig));
            _sink = sink ??
                throw new ArgumentNullException(nameof(sink));
            _metrics = metrics ??
                IMetricsContext.Empty;

            _defaultVersion = new ConfigurationVersionDataType
            {
                MajorVersion = (uint)(_options.Value.PublisherVersion ?? 1),
                MinorVersion = 0
            };
            _subscriptions = new Dictionary<SubscriptionIdentifier, DataSetWriterSubscription>();

            InitializeMetrics();
        }

        /// <inheritdoc/>
        public ValueTask OnUpdatedAsync(WriterGroupModel writerGroup)
        {
            if (writerGroup.DataSetWriters == null ||
                writerGroup.DataSetWriters.Count == 0)
            {
                foreach (var subscription in _subscriptions.Values)
                {
                    subscription.Dispose();
                }
                _logger.LogInformation(
                    "Removed all subscriptions from writer group {WriterGroup}.",
                        writerGroup.Id);
                _subscriptions.Clear();
                return ValueTask.CompletedTask;
            }

            //
            // Subscription identifier is the writer name, there should not be duplicate
            // writer names here, if there are we throw an exception.
            //
            var dataSetWriterSubscriptionMap =
                new Dictionary<SubscriptionIdentifier, DataSetWriterModel>();
            foreach (var writerEntry in writerGroup.DataSetWriters)
            {
                var id = writerEntry.ToSubscriptionId();
                if (!dataSetWriterSubscriptionMap.TryAdd(id, writerEntry))
                {
                    throw new ArgumentException(
                        $"Group {writerGroup.Id} contains duplicate writer {id}.");
                }
            }

            // Update or removed ones that were updated or removed.
            foreach (var id in _subscriptions.Keys.ToList())
            {
                if (!dataSetWriterSubscriptionMap.TryGetValue(id, out var writer))
                {
                    if (_subscriptions.Remove(id, out var s))
                    {
                        s.Dispose();
                    }
                }
                else
                {
                    // Update
                    if (_subscriptions.TryGetValue(id, out var s))
                    {
                        s.Update(writerGroup, writer);
                    }
                }
            }
            // Create any newly added ones
            foreach (var writer in dataSetWriterSubscriptionMap)
            {
                if (!_subscriptions.ContainsKey(writer.Key))
                {
                    // Add
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var writerSubscription =
                        new DataSetWriterSubscription(this, writerGroup, writer.Value);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    _subscriptions.AddOrUpdate(writerSubscription.Id, writerSubscription);
                }
            }

            _logger.LogInformation(
                "Successfully updated all subscriptions inside the writer group {WriterGroup}.",
                writerGroup.Id);

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask OnRemovedAsync(WriterGroupModel writerGroup)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                foreach (var s in _subscriptions.Values)
                {
                    s.Dispose();
                }
                _subscriptions.Clear();
            }
            finally
            {
                _meter.Dispose();
            }
        }

        /// <summary>
        /// Helper to manage subscriptions
        /// </summary>
        private sealed class DataSetWriterSubscription : IDisposable, ISubscriptionCallbacks,
            IMetricsContext
        {
            /// <inheritdoc/>
            public TagList TagList { get; }

            /// <summary>
            /// Subscription id
            /// </summary>
            public SubscriptionIdentifier Id => _subscriptionInfo.Id;

            /// <summary>
            /// Active subscription
            /// </summary>
            public ISubscriptionHandle? Subscription { get; set; }

            /// <summary>
            /// Create subscription from a DataSetWriterModel template
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="writerGroup"></param>
            /// <param name="dataSetWriter"></param>
            public DataSetWriterSubscription(WriterGroupDataSource outer,
                WriterGroupModel writerGroup, DataSetWriterModel dataSetWriter)
            {
                _outer = outer ?? throw new ArgumentNullException(nameof(outer));
                _options = new SubscriptionOptions(writerGroup, dataSetWriter,
                    _outer._options.Value);

                TagList = new TagList(outer._metrics.TagList.ToArray().AsSpan())
                {
                    new KeyValuePair<string, object?>(Constants.DataSetWriterIdTag,
                        dataSetWriter.Id),
                    new KeyValuePair<string, object?>(Constants.DataSetWriterNameTag,
                        dataSetWriter.DataSetWriterName)
                };

                _subscriptionInfo = _options.DataSetWriter.ToSubscriptionModel(
                      _outer._subscriptionConfig.Value, CreateMonitoredItemContext);

                _outer._logger.LogDebug(
                    "Open new writer with subscription {Id} in writer group {WriterGroup}...",
                    Id, writerGroup.Id);

                //
                // Creating inner OPC UA subscription object. This will create a session
                // if none already exist and transfer the subscription into the session
                // management realm
                //
                _outer._subscriptionManager.CreateSubscription(_subscriptionInfo, this, this);

                _frameCount = 0;
                InitializeMetaDataTrigger();
                InitializeKeepAlive();

                _metadataTimer?.Start();
                _outer._logger.LogInformation(
                    "New writer with subscription {Id} in writer group {WriterGroup} opened.",
                    Id, writerGroup.Id);
            }

            /// <summary>
            /// Update subscription content
            /// </summary>
            /// <param name="writerGroup"></param>
            /// <param name="dataSetWriter"></param>
            public void Update(WriterGroupModel writerGroup, DataSetWriterModel dataSetWriter)
            {
                _outer._logger.LogDebug(
                    "Updating writer with subscription {Id} in writer group {WriterGroup}...",
                    Id, writerGroup.Id);

                _options = new SubscriptionOptions(writerGroup, dataSetWriter,
                    _outer._options.Value);

                var subscription = Subscription;
                if (subscription == null)
                {
                    _outer._logger.LogWarning(
                        "Writer does not have a subscription to update yet!");
                    return;
                }

                _subscriptionInfo = _options.DataSetWriter.ToSubscriptionModel(
                    _outer._subscriptionConfig.Value, CreateMonitoredItemContext);

                _frameCount = 0;
                InitializeMetaDataTrigger();
                InitializeKeepAlive();

                // Apply changes
                subscription.Update(_subscriptionInfo);

                _outer._logger.LogInformation(
                    "Updated subscription for writer {Id} in writer group {WriterGroup}.",
                    Id, writerGroup.Id);
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
                    _outer._logger.LogInformation(
                        "Writer with subscription {Id} in group {WriterGroup} new subscription received.",
                        Id, _options.WriterGroup.Id);
                }
                else
                {
                    _outer._logger.LogInformation(
                        "Writer with subscription {Id} in group {WriterGroup} had subscription removed.",
                        Id, _options.WriterGroup.Id);
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
                    var keyFrameCount = _options.DataSetWriter.KeyFrameCount
                        ?? _outer._options.Value.DefaultKeyFrameCount ?? 0;
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
                            _outer._logger.LogDebug(
                                "Notifications counter in subscription {Id} has been reset to " +
                                "prevent overflow. So far, {DataChangesCount} data changes and " +
                                "{ValueChangesCount} value changes were invoked by message source.",
                                Id, _outer._dataChanges.Count, _outer._valueChanges.Count);
                            _outer._dataChanges.Count = 0;
                            _outer._valueChanges.Count = 0;
                            _outer._heartbeats.Count = 0;
                            _outer._sink.OnReset();
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
                        _outer._logger.LogDebug(
                            "Notifications counter in subscription {Id} has been reset to " +
                            "prevent overflow. So far, {ReadCount} data changes and " +
                            "{ValuesCount} value changes were invoked by source.",
                            Id, _outer._cyclicReads.Count, _outer._sampledValues.Count);
                        _outer._cyclicReads.Count = 0;
                        _outer._sampledValues.Count = 0;
                        _outer._sink.OnReset();
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
                            _outer._logger.LogDebug(
                                "Notifications counter in subscription {Id} has been reset to " +
                                "prevent overflow. So far, {EventChangesCount} event changes and " +
                                "{EventValueChangesCount} event value changes were invoked by source.",
                                Id, _outer._events.Count, _outer._eventNotification.Count);
                            _outer._events.Count = 0;
                            _outer._eventNotification.Count = 0;
                            _outer._modelChanges.Count = 0;

                            _outer._sink.OnReset();
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
                return settings?.QueueName == null ? null : new TopicContext(settings);
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

                _outer._logger.LogDebug(
                    "Closing writer with subscription {Id} in writer group {WriterGroup}...",
                    Id, _options.WriterGroup.Id);

                subscription.Close();

                _outer._logger.LogInformation(
                    "Writer with subscription {Id} in writer group {WriterGroup} closed.",
                    Id, _options.WriterGroup.Id);
            }

            /// <summary>
            /// Initialize sending of keep alive messages
            /// </summary>
            private void InitializeKeepAlive()
            {
                _sendKeepAlives = _options.DataSetWriter.DataSet?.SendKeepAlive
                    ?? _outer._options.Value.EnableDataSetKeepAlives == true;
            }

            /// <summary>
            /// Initializes the Metadata triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeMetaDataTrigger()
            {
                var metaDataSendInterval = _options.DataSetWriter.MetaDataUpdateTime
                    .GetValueOrDefault(TimeSpan.Zero)
                    .TotalMilliseconds;

                if (metaDataSendInterval > 0 &&
                    _outer._options.Value.DisableDataSetMetaData != true)
                {
                    if (_metadataTimer == null)
                    {
                        _metadataTimer = new Timer(metaDataSendInterval);
                        _metadataTimer.Elapsed += MetadataTimerElapsed;
                    }
                    else
                    {
                        _metadataTimer.Interval = metaDataSendInterval;
                    }
                }
                else if (_metadataTimer != null)
                {
                    _metadataTimer.Stop();
                    _metadataTimer.Dispose();
                    _metadataTimer = null;
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

                _outer._logger.LogDebug("Insert metadata message into Subscription {Id}...", Id);
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
                    var options = _options;
                    lock (_lock)
                    {
                        foreach (var notification in subscriptionNotification.Split(options.ContextSelector))
                        {
                            var notificationContext = notification.Context as DataSetContext;

                            if (options.MetaDataVersion != null)
                            {
                                var sendMetadata = metaDataTimer;

                                //
                                // Only send if called from metadata timer or if the metadata version changes.
                                // Metadata reference is owned by the notification/message, a new metadata is
                                // created when it changes so old one is not mutated and this should be safe.
                                //
                                if (_currentMetadataMajorVersion != options.MetaDataVersion.MajorVersion &&
                                    _currentMetadataMinorVersion != options.MetaDataVersion.MinorVersion)
                                {
                                    _currentMetadataMajorVersion = options.MetaDataVersion.MajorVersion;
                                    _currentMetadataMinorVersion = options.MetaDataVersion.MinorVersion;

                                    sendMetadata = true;
                                }
                                if (sendMetadata)
                                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                                    var metadataNotification = new MetadataNotificationModel(notification)
                                    {
                                        Context = CreateMessageContext(options, true,
                                            () => Interlocked.Increment(ref _metadataSequenceNumber))
                                    };
#pragma warning restore CA2000 // Dispose objects before losing scope
                                    _outer._sink.OnNotify(metadataNotification);
                                    InitializeMetaDataTrigger();
                                }
                            }

                            if (!metaDataTimer)
                            {
                                Debug.Assert(notification.Notifications != null);
                                notification.Context = CreateMessageContext(options, false,
                                    () => Interlocked.Increment(ref _dataSetSequenceNumber),
                                        notificationContext);
                                _outer._logger.LogTrace("Enqueuing notification: {Notification}",
                                    notification.ToString());
                                _outer._sink.OnNotify(notification);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _outer._logger.LogWarning(ex, "Failed to publish message to network sink.");
                }

                WriterGroupMessageContext CreateMessageContext(SubscriptionOptions options,
                    bool sendMetadata, Func<uint> sequenceNumber, DataSetContext? item = null)
                {
                    return new WriterGroupMessageContext
                    {
                        PublisherId = _outer._options.Value.PublisherId
                            ?? Constants.DefaultPublisherId,
                        Writer = options.DataSetWriter,
                        NextWriterSequenceNumber = sequenceNumber,
                        WriterGroup = options.WriterGroup,
                        SendMetaData = sendMetadata,
                        Schema = options.Schema,
                        // TODO: Make this a nullable meta data producer func to
                        // emit meta data for individual topics
                        MetaDataVersion = options.MetaDataVersion
                            ?? _outer._defaultVersion,
                        Topic = item?.Topic
                            ?? (!sendMetadata ? options.Topic : options.MetadataTopic),
                        Qos = item?.Qos
                            ?? options.Qos
                    };
                }
            }

            /// <summary>
            /// Data set metadata notification
            /// </summary>
            public sealed record class MetadataNotificationModel :
                IOpcUaSubscriptionNotification
            {
                /// <inheritdoc/>
                public uint SequenceNumber { get; }

                /// <inheritdoc/>
                public MessageType MessageType => MessageType.Metadata;

                /// <inheritdoc/>
                public string? SubscriptionName { get; }

                /// <inheritdoc/>
                public ushort SubscriptionId { get; }

                /// <inheritdoc/>
                public string? EndpointUrl { get; }

                /// <inheritdoc/>
                public string? ApplicationUri { get; }

                /// <inheritdoc/>
                public DateTime? PublishTimestamp { get; }

                /// <inheritdoc/>
                public DateTime CreatedTimestamp { get; } = DateTime.UtcNow;

                /// <inheritdoc/>
                public uint? PublishSequenceNumber => null;

                /// <inheritdoc/>
                public object? Context { get; set; }

                /// <inheritdoc/>
                public IVariantEncoder Codec { get; set; }

                /// <inheritdoc/>
                public IList<MonitoredItemNotificationModel> Notifications { get; }

                /// <inheritdoc/>
                public MetadataNotificationModel(IOpcUaSubscriptionNotification notification)
                {
                    SequenceNumber = notification.SequenceNumber;
                    Codec = notification.Codec;
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
            private abstract class DataSetContext
            {
                /// <summary>
                /// Meta data
                /// </summary>
                public DataSetMetaDataType? MetaData { get; } // TODO

                /// <summary>
                /// Topic for the message if not metadata message
                /// </summary>
                public abstract string Topic { get; }

                /// <summary>
                /// Topic for the message if not metadata message
                /// </summary>
                public abstract QoS? Qos { get; }
            }

            /// <summary>
            /// Topic context
            /// </summary>
            private sealed class TopicContext : DataSetContext
            {
                /// <inheritdoc/>
                public override string Topic { get; }
                /// <inheritdoc/>
                public override QoS? Qos { get; }

                /// <summary>
                /// Create context from settings
                /// </summary>
                /// <param name="settings"></param>
                public TopicContext(PublishingQueueSettingsModel settings)
                {
                    Topic = settings.QueueName ?? string.Empty;
                    Qos = settings.RequestedDeliveryGuarantee;
                }

                /// <summary>
                /// Create
                /// </summary>
                /// <param name="root"></param>
                /// <param name="subpath"></param>
                /// <param name="qos"></param>
                /// <param name="includeNamespaceIndex"></param>
                public TopicContext(string root, RelativePath subpath, QoS? qos,
                    bool includeNamespaceIndex)
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
            /// Options to compare
            /// </summary>
            private sealed class SubscriptionOptions
            {
                /// <summary>
                /// Writer
                /// </summary>
                public DataSetWriterModel DataSetWriter { get; }

                /// <summary>
                /// Writer group
                /// </summary>
                public WriterGroupModel WriterGroup { get; }

                /// <summary>
                /// Topic
                /// </summary>
                public string Topic { get; }

                /// <summary>
                /// Metadata topic
                /// </summary>
                public string MetadataTopic { get; }

                /// <summary>
                /// Qos
                /// </summary>
                public QoS? Qos { get; }

                /// <summary>
                /// Routing
                /// </summary>
                public DataSetRoutingMode Routing { get; }

                /// <summary>
                /// Selecting of context
                /// </summary>
                public Func<MonitoredItemNotificationModel, object?> ContextSelector { get; }

                /// <summary>
                /// Metadata version
                /// </summary>
                public ConfigurationVersionDataType? MetaDataVersion { get; }

                /// <summary>
                /// Schema
                /// </summary>
                public IEventSchema? Schema { get; }

                /// <summary>
                /// Create subscription options
                /// </summary>
                /// <param name="writerGroup"></param>
                /// <param name="dataSetWriter"></param>
                /// <param name="options"></param>
                public SubscriptionOptions(WriterGroupModel writerGroup,
                    DataSetWriterModel dataSetWriter, PublisherOptions options)
                {
                    DataSetWriter = dataSetWriter;
                    WriterGroup = writerGroup;

                    Routing = DataSetWriter.DataSet?.Routing ?? DataSetRoutingMode.None;
                    Topic = DataSetWriter.Publishing?.QueueName
                       ?? writerGroup.Publishing?.QueueName
                       ?? string.Empty;
                    Qos = DataSetWriter.Publishing?.RequestedDeliveryGuarantee
                        ?? writerGroup.Publishing?.RequestedDeliveryGuarantee;
                    MetadataTopic = DataSetWriter.MetaData?.QueueName ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(MetadataTopic))
                    {
                        MetadataTopic = Topic;
                    }
                    ContextSelector = Routing == DataSetRoutingMode.None ? _ => null
                        : n => n.PathFromRoot == null || n.Context != null ?
                          n.Context : new TopicContext(Topic, n.PathFromRoot, Qos,
                            Routing != DataSetRoutingMode.UseBrowseNames);

                    var messageEncoding = WriterGroup.MessageType ?? MessageEncoding.Json;
                    if (options.SchemaOptions != null ||
                        messageEncoding.HasFlag(MessageEncoding.Avro))
                    {
                        if (messageEncoding.HasFlag(MessageEncoding.Avro))
                        {
                            Schema = new AvroNetworkMessageSchema(WriterGroup,
                                options.SchemaOptions);
                            return; // Disable metadata
                        }
                        if (messageEncoding.HasFlag(MessageEncoding.Json))
                        {
                            Schema = new JsonNetworkMessageSchema(WriterGroup,
                                options.SchemaOptions,
                                options.UseStandardsCompliantEncoding ?? false);
                            return; // Disable metadata
                        }
                    }

                    var metaDataMajorVersion =
                        DataSetWriter.DataSet?.DataSetMetaData?.MajorVersion;
                    if (metaDataMajorVersion != null)
                    {
                        MetaDataVersion = new ConfigurationVersionDataType
                        {
                            MajorVersion = metaDataMajorVersion.Value,
                            MinorVersion =
                                DataSetWriter.DataSet?.GetMetaDataMinorVersion() ?? 0u
                        };
                    }
                }

                /// <inheritdoc/>
                public override bool Equals(object? obj)
                {
                    return obj is SubscriptionOptions options &&
                        Topic == options.Topic &&
                        Qos == options.Qos &&
                        MetadataTopic == options.MetadataTopic &&
                        Routing == options.Routing
                        ;
                }

                /// <inheritdoc/>
                public override int GetHashCode()
                {
                    return HashCode.Combine(Topic, Qos, MetadataTopic, Routing);
                }
            }

            private Timer? _metadataTimer;
            private SubscriptionModel _subscriptionInfo;
            private SubscriptionOptions _options;
            private readonly WriterGroupDataSource _outer;
            private readonly object _lock = new();
            private volatile uint _frameCount;
            private uint _dataSetSequenceNumber;
            private uint _metadataSequenceNumber;
            private uint? _currentMetadataMajorVersion;
            private uint? _currentMetadataMinorVersion;
            private bool _sendKeepAlives;
            private bool _disposed;
        }

        /// <summary>
        /// Runtime duration
        /// </summary>
        private double UpTime => (DateTime.UtcNow - _startTime).TotalSeconds;

        private IEnumerable<IOpcUaClientDiagnostics> UsedClients => _subscriptions.Values
            .Select(s => s.Subscription?.State!)
            .Where(s => s != null)
            .Distinct();

        private int ReconnectCount => UsedClients
            .Sum(s => s.ReconnectCount);

        private int ConnectedClients => UsedClients
            .Count(s => s.State == EndpointConnectivityState.Ready);

        private int DisconnectedClients => UsedClients
            .Count(s => s.State != EndpointConnectivityState.Ready);

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_heartbeats",
                () => new Measurement<long>(_heartbeats.Count, _metrics.TagList),
                description: "Total Heartbeats delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_heartbeats_per_second",
                () => new Measurement<double>(_heartbeats.Count / UpTime, _metrics.TagList),
                description: "Opc Cyclic reads/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_heartbeats_per_second_last_min",
                () => new Measurement<long>(_heartbeats.LastMinute, _metrics.TagList),
                description: "Opc Cyclic reads/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_sampledvalues",
                () => new Measurement<long>(_sampledValues.Count, _metrics.TagList),
                description: "Total sampled values delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_sampledvalues_per_second",
                () => new Measurement<double>(_sampledValues.Count / UpTime, _metrics.TagList),
                description: "Opc sampled values/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_sampledvalues_per_second_last_min",
                () => new Measurement<long>(_sampledValues.LastMinute, _metrics.TagList),
                description: "Opc sampled values/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_modelchanges",
                () => new Measurement<long>(_modelChanges.Count, _metrics.TagList),
                description: "Total Number of changes found in the address spaces of the connected servers.");
            _meter.CreateObservableGauge("iiot_edge_publisher_modelchanges_per_second",
                () => new Measurement<double>(_modelChanges.Count / UpTime, _metrics.TagList),
                description: "Address space Model changes/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_modelchanges_per_second_last_min",
                () => new Measurement<long>(_modelChanges.LastMinute, _metrics.TagList),
                description: "Address space Model changes/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_value_changes",
                () => new Measurement<long>(_valueChanges.Count, _metrics.TagList),
                description: "Total Opc Value changes delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_value_changes_per_second",
                () => new Measurement<double>(_valueChanges.Count / UpTime, _metrics.TagList),
                description: "Opc Value changes/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_value_changes_per_second_last_min",
                () => new Measurement<long>(_valueChanges.LastMinute, _metrics.TagList),
                description: "Opc Value changes/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_events",
                () => new Measurement<long>(_events.Count, _metrics.TagList),
                description: "Total Opc Events delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_events_per_second",
                () => new Measurement<double>(_events.Count / UpTime, _metrics.TagList),
                description: "Opc Events/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_events_per_second_last_min",
                () => new Measurement<long>(_events.LastMinute, _metrics.TagList),
                description: "Opc Events/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_event_notifications",
                () => new Measurement<long>(_eventNotification.Count, _metrics.TagList),
                description: "Total Opc Event notifications delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_event_notifications_per_second",
                () => new Measurement<double>(_eventNotification.Count / UpTime, _metrics.TagList),
                description: "Opc Event notifications/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_event_notifications_per_second_last_min",
                () => new Measurement<long>(_eventNotification.LastMinute, _metrics.TagList),
                description: "Opc Event notifications/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_data_changes",
                () => new Measurement<long>(_dataChanges.Count, _metrics.TagList),
                description: "Total Opc Data change notifications delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_data_changes_per_second",
                () => new Measurement<double>(_dataChanges.Count / UpTime, _metrics.TagList),
                description: "Opc Data change notifications/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_data_changes_per_second_last_min",
                () => new Measurement<long>(_dataChanges.LastMinute, _metrics.TagList),
                description: "Opc Data change notifications/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_cyclicreads",
                () => new Measurement<long>(_cyclicReads.Count, _metrics.TagList),
                description: "Total Cyclic reads delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_cyclicreads_per_second",
                () => new Measurement<double>(_cyclicReads.Count / UpTime, _metrics.TagList),
                description: "Opc Cyclic reads/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_cyclicreads_per_second_last_min",
                () => new Measurement<long>(_cyclicReads.LastMinute, _metrics.TagList),
                description: "Opc Cyclic reads/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_queue_overflows",
                () => new Measurement<long>(_overflows.Count, _metrics.TagList),
                description: "Total values received with a queue overflow indicator.");
            _meter.CreateObservableGauge("iiot_edge_publisher_queue_overflows_per_second",
                () => new Measurement<double>(_overflows.Count / UpTime, _metrics.TagList),
                description: "Values with overflow indicator/second received.");
            _meter.CreateObservableGauge("iiot_edge_publisher_queue_overflows_per_second_last_min",
                () => new Measurement<long>(_overflows.LastMinute, _metrics.TagList),
                description: "Values with overflow indicator/second received in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_keep_alive_notifications",
                () => new Measurement<long>(_keepAliveCount, _metrics.TagList),
                description: "Total Opc keep alive notifications delivered for processing.");

            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_subscriptions",
                () => new Measurement<long>(_subscriptions.Count, _metrics.TagList),
                description: "Number of Writers/Subscriptions in the writer group.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_connection_retries",
                () => new Measurement<long>(ReconnectCount, _metrics.TagList),
                description: "OPC UA connect retries.");
            _meter.CreateObservableGauge("iiot_edge_publisher_is_connection_ok",
                () => new Measurement<int>(ConnectedClients, _metrics.TagList),
                description: "OPC UA endpoints that are successfully connected.");
            _meter.CreateObservableGauge("iiot_edge_publisher_is_disconnected",
                () => new Measurement<int>(DisconnectedClients, _metrics.TagList),
                description: "OPC UA endpoints that are disconnected.");
        }

        private readonly ConfigurationVersionDataType _defaultVersion;
        private const long kNumberOfInvokedMessagesResetThreshold = long.MaxValue - 10000;
        private long _keepAliveCount;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private readonly ILogger _logger;
        private readonly Dictionary<SubscriptionIdentifier, DataSetWriterSubscription> _subscriptions;
        private readonly IOpcUaSubscriptionManager _subscriptionManager;
        private readonly IOptions<OpcUaSubscriptionOptions> _subscriptionConfig;
        private readonly INotificationSink _sink;
        private readonly IMetricsContext _metrics;
        private readonly IOptions<PublisherOptions> _options;
        private readonly RollingAverage _valueChanges = new();
        private readonly RollingAverage _dataChanges = new();
        private readonly RollingAverage _sampledValues = new();
        private readonly RollingAverage _cyclicReads = new();
        private readonly RollingAverage _eventNotification = new();
        private readonly RollingAverage _events = new();
        private readonly RollingAverage _modelChanges = new();
        private readonly RollingAverage _heartbeats = new();
        private readonly RollingAverage _overflows = new();
        private readonly DateTime _startTime = DateTime.UtcNow;
    }
}
