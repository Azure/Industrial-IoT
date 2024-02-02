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
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Triggers dataset writer messages on subscription changes
    /// </summary>
    public sealed class WriterGroupDataSource : IMessageSource
    {
        /// <inheritdoc/>
        public event EventHandler<IOpcUaSubscriptionNotification>? OnMessage;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? OnCounterReset;

        /// <summary>
        /// Create trigger from writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="subscriptionManager"></param>
        /// <param name="subscriptionConfig"></param>
        /// <param name="metrics"></param>
        /// <param name="logger"></param>
        public WriterGroupDataSource(WriterGroupModel writerGroup,
            IOptions<PublisherOptions> options, IOpcUaSubscriptionManager subscriptionManager,
            IOptions<OpcUaSubscriptionOptions> subscriptionConfig,
            IMetricsContext? metrics, ILogger<WriterGroupDataSource> logger)
        {
            ArgumentNullException.ThrowIfNull(writerGroup, nameof(writerGroup));

            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ??
                throw new ArgumentNullException(nameof(subscriptionManager));
            _subscriptionConfig = subscriptionConfig ??
                throw new ArgumentNullException(nameof(subscriptionConfig));
            _metrics = metrics ??
                IMetricsContext.Empty;
            _subscriptions = new Dictionary<SubscriptionIdentifier, DataSetWriterSubscription>();
            _writerGroup = Copy(writerGroup);
            InitializeMetrics();
        }

        /// <inheritdoc/>
        public async ValueTask StartAsync(CancellationToken ct)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                Debug.Assert(_subscriptions.Count == 0);

                foreach (var writer in _writerGroup.DataSetWriters ?? Enumerable.Empty<DataSetWriterModel>())
                {
                    // Create writer subscriptions
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var writerSubscription = new DataSetWriterSubscription(this, writer);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    _subscriptions.AddOrUpdate(writerSubscription.Id, writerSubscription);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask UpdateAsync(WriterGroupModel writerGroup, CancellationToken ct)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                writerGroup = Copy(writerGroup);

                if (writerGroup.DataSetWriters == null ||
                    writerGroup.DataSetWriters.Count == 0)
                {
                    foreach (var subscription in _subscriptions.Values)
                    {
                        subscription.Dispose();
                    }
                    _logger.LogInformation("Removed all subscriptions from writer group {Name}.",
                        writerGroup.WriterGroupId);
                    _subscriptions.Clear();
                    _writerGroup = writerGroup;
                    return;
                }

                //
                // Subscription identifier is the writer name, there should not be duplicate
                // writer names here, if there are we throw an exception.
                //
                var dataSetWriterSubscriptionMap =
                    new Dictionary<SubscriptionIdentifier, DataSetWriterModel>();
                foreach (var writerEntry in writerGroup.DataSetWriters)
                {
                    var id = writerEntry.ToSubscriptionId(writerGroup.WriterGroupId,
                        _subscriptionConfig.Value);
                    if (!dataSetWriterSubscriptionMap.TryAdd(id, writerEntry))
                    {
                        throw new ArgumentException(
                            $"Group {writerGroup.Name} contains duplicate writer {id}.");
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
                            s.Update(writer);
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
                        var writerSubscription = new DataSetWriterSubscription(this, writer.Value);
#pragma warning restore CA2000 // Dispose objects before losing scope
                        _subscriptions.AddOrUpdate(writerSubscription.Id, writerSubscription);
                    }
                }
                if (_writerGroup.WriterGroupId != writerGroup.WriterGroupId)
                {
                    _logger.LogInformation("Update writer group from {Previous} to writer group {Name}.",
                        _writerGroup.WriterGroupId, writerGroup.WriterGroupId);
                }

                _logger.LogInformation(
                    "Successfully updated all subscriptions inside the writer group {Name}.",
                    writerGroup.WriterGroupId);
                _writerGroup = writerGroup;
            }
            finally
            {
                _lock.Release();
            }
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
                _lock.Dispose();
                _meter.Dispose();
            }
        }

        /// <summary>
        /// Safe clone the writer group model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private WriterGroupModel Copy(WriterGroupModel model)
        {
            var writerGroup = model with
            {
                DataSetWriters = model.DataSetWriters == null ?
                    new List<DataSetWriterModel>() :
                    model.DataSetWriters.Select(f => f.Clone()).ToList(),
                LocaleIds = model.LocaleIds?.ToList(),
                MessageSettings = model.MessageSettings.Clone(),
                SecurityKeyServices = model.SecurityKeyServices?
                    .Select(c => c.Clone())
                    .ToList()
            };

            // Set the messaging profile settings
            var defaultMessagingProfile = _options.Value.MessagingProfile ??
                MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            if (writerGroup.HeaderLayoutUri != null)
            {
                defaultMessagingProfile = MessagingProfile.Get(
                    Enum.Parse<MessagingMode>(writerGroup.HeaderLayoutUri),
                    writerGroup.MessageType ?? defaultMessagingProfile.MessageEncoding);
            }

            if (writerGroup.MessageType == null)
            {
                writerGroup.MessageType = defaultMessagingProfile.MessageEncoding;
            }

            // Set the messaging settings for the encoder
            if (writerGroup.MessageSettings?.NetworkMessageContentMask == null)
            {
                writerGroup.MessageSettings ??= new WriterGroupMessageSettingsModel();
                writerGroup.MessageSettings.NetworkMessageContentMask =
                    defaultMessagingProfile.NetworkMessageContentMask;
            }

            foreach (var dataSetWriter in writerGroup.DataSetWriters)
            {
                if (dataSetWriter.MessageSettings?.DataSetMessageContentMask == null)
                {
                    dataSetWriter.MessageSettings ??= new DataSetWriterMessageSettingsModel();
                    dataSetWriter.MessageSettings.DataSetMessageContentMask =
                        defaultMessagingProfile.DataSetMessageContentMask;
                }
                if (dataSetWriter.DataSetFieldContentMask == null)
                {
                    dataSetWriter.DataSetFieldContentMask =
                        defaultMessagingProfile.DataSetFieldContentMask;
                }
            }

            return writerGroup;
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
            /// <param name="dataSetWriter"></param>
            public DataSetWriterSubscription(WriterGroupDataSource outer,
                DataSetWriterModel dataSetWriter)
            {
                _outer = outer ?? throw new ArgumentNullException(nameof(outer));
                _dataSetWriter = dataSetWriter?.Clone() ??
                    throw new ArgumentNullException(nameof(dataSetWriter));
                _subscriptionInfo = _dataSetWriter.ToSubscriptionModel(
                    _outer._subscriptionConfig.Value, outer._writerGroup.WriterGroupId);

                _outer._logger.LogDebug("Open new writer with subscription {Id} in writer group {Name}...",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);

                var dataSetClassId = dataSetWriter.DataSet?.DataSetMetaData?.DataSetClassId ?? Guid.Empty;
                var builder = new TopicBuilder(_outer._options, new Dictionary<string, string>
                {
                    [PublisherConfig.DataSetWriterNameVariableName] =
                        dataSetWriter.DataSetWriterName ?? Constants.DefaultDataSetWriterName,
                    [PublisherConfig.DataSetClassIdVariableName] =
                        dataSetClassId.ToString(),
                    [PublisherConfig.DataSetWriterGroupVariableName] =
                        outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId
                    // ...
                });

                _topic = builder.TelemetryTopic;
                _metadataTopic = builder.DataSetMetaDataTopic;
                if (string.IsNullOrWhiteSpace(_metadataTopic))
                {
                    _metadataTopic = _topic;
                }

                TagList = new TagList(outer._metrics.TagList.ToArray().AsSpan())
                {
                    new KeyValuePair<string, object?>(Constants.DataSetWriterIdTag,
                        dataSetWriter.DataSetWriterName)
                };

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
                    "New writer with subscription {Id} in writer group {Name} opened.",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);
            }

            /// <summary>
            /// Update subscription content
            /// </summary>
            /// <param name="dataSetWriter"></param>
            public void Update(DataSetWriterModel dataSetWriter)
            {
                _outer._logger.LogDebug("Updating writer with subscription {Id} in writer group {Name}...",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);

                _dataSetWriter = dataSetWriter.Clone();
                _subscriptionInfo = _dataSetWriter.ToSubscriptionModel(
                    _outer._subscriptionConfig.Value, _outer._writerGroup.WriterGroupId);

                var subscription = Subscription;
                if (subscription == null)
                {
                    _outer._logger.LogWarning("Writer does not have a subscription to update yet!");
                    return;
                }
                _frameCount = 0;
                InitializeMetaDataTrigger();
                InitializeKeepAlive();

                // Apply changes
                subscription.Update(_subscriptionInfo);

                _outer._logger.LogInformation("Updated subscription for writer {Id} in writer group {Name}.",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);
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
                    _outer._logger.LogInformation("Writer received updated subscription!");
                }
                else
                {
                    _outer._logger.LogInformation("Writer subscription removed after close!");
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionDataChange(IOpcUaSubscriptionNotification notification)
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
            public void OnSubscriptionKeepAlive(IOpcUaSubscriptionNotification notification)
            {
                Interlocked.Increment(ref _outer._keepAliveCount);
                if (_sendKeepAlives)
                {
                    CallMessageReceiverDelegates(notification);
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionDataDiagnosticsChange(bool liveData, int notificationCounts, int overflows,
                int heartbeat, int cyclic)
            {
                lock (_lock)
                {
                    _outer._heartbeats.Count += heartbeat;
                    _outer._cyclicReads.Count += cyclic;
                    _outer._overflows.Count += overflows;
                    if (liveData)
                    {
                        if (_outer._dataChanges.Count >= kNumberOfInvokedMessagesResetThreshold ||
                            _outer._valueChanges.Count >= kNumberOfInvokedMessagesResetThreshold)
                        {
                            // reset both
                            _outer._logger.LogDebug(
                                "Notifications counter in subscription {Id} has been reset to prevent" +
                                " overflow. So far, {DataChangesCount} data changes and {ValueChangesCount} " +
                                "value changes were invoked by message source.",
                                Id, _outer._dataChanges.Count, _outer._valueChanges.Count);
                            _outer._dataChanges.Count = 0;
                            _outer._valueChanges.Count = 0;
                            _outer._heartbeats.Count = 0;
                            _outer._cyclicReads.Count = 0;
                            _outer.OnCounterReset?.Invoke(this, EventArgs.Empty);
                        }

                        _outer._valueChanges.Count += notificationCounts;
                        _outer._dataChanges.Count++;
                    }
                }
            }

            /// <inheritdoc/>
            public void OnSubscriptionEventChange(IOpcUaSubscriptionNotification notification)
            {
                CallMessageReceiverDelegates(notification);
            }

            /// <inheritdoc/>
            public void OnSubscriptionEventDiagnosticsChange(bool liveData, int notificationCounts, int overflows,
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
                                "Notifications counter in subscription {Id} has been reset to prevent" +
                                " overflow. So far, {EventChangesCount} event changes and {EventValueChangesCount} " +
                                "event value changes were invoked by message source.",
                                Id, _outer._events.Count, _outer._eventNotification.Count);
                            _outer._events.Count = 0;
                            _outer._eventNotification.Count = 0;
                            _outer._modelChanges.Count = 0;

                            _outer.OnCounterReset?.Invoke(this, EventArgs.Empty);
                        }

                        _outer._eventNotification.Count += notificationCounts;
                        _outer._events.Count++;
                    }
                }
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

                _outer._logger.LogDebug("Closing writer with subscription {Id} in writer group {Name}...",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);

                subscription.Close();

                _outer._logger.LogInformation("Writer with subscription {Id} in writer group {Name} closed.",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);
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
                var metaDataSendInterval = _dataSetWriter.MetaDataUpdateTime
                    .GetValueOrDefault(TimeSpan.Zero)
                    .TotalMilliseconds;

                if (metaDataSendInterval > 0 &&
                    _outer._subscriptionConfig.Value.DisableDataSetMetaData != true)
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
            /// <param name="notification"></param>
            /// <param name="metaDataTimer"></param>
            private void CallMessageReceiverDelegates(IOpcUaSubscriptionNotification notification,
                bool metaDataTimer = false)
            {
                try
                {
                    lock (_lock)
                    {
                        if (notification.MetaData != null)
                        {
                            var sendMetadata = metaDataTimer;
                            //
                            // Only send if called from metadata timer or if the metadata version changes.
                            // Metadata reference is owned by the notification/message, a new metadata is
                            // created when it changes so old one is not mutated and this should be safe.
                            //
                            if (_currentMetadataMajorVersion != notification.MetaData.ConfigurationVersion.MajorVersion &&
                                _currentMetadataMinorVersion != notification.MetaData.ConfigurationVersion.MinorVersion)
                            {
                                _currentMetadataMajorVersion = notification.MetaData.ConfigurationVersion.MajorVersion;
                                _currentMetadataMinorVersion = notification.MetaData.ConfigurationVersion.MinorVersion;
                                sendMetadata = true;
                            }
                            if (sendMetadata)
                            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                                var metadata = new MetadataNotificationModel(notification)
                                {
                                    Context = CreateMessageContext(
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
                            notification.Context = CreateMessageContext(
                                () => Interlocked.Increment(ref _dataSetSequenceNumber));
                            _outer._logger.LogTrace("Enqueuing notification: {Notification}",
                                notification.ToString());
                            _outer.OnMessage?.Invoke(this, notification);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _outer._logger.LogWarning(ex, "Failed to produce message.");
                }

                WriterGroupMessageContext CreateMessageContext(Func<uint> sequenceNumber)
                {
                    return new WriterGroupMessageContext
                    {
                        PublisherId = _outer._options.Value.PublisherId ?? Constants.DefaultPublisherId,
                        Writer = _dataSetWriter,
                        NextWriterSequenceNumber = sequenceNumber,
                        WriterGroup = _outer._writerGroup,
                        Topic = _topic,
                        MetaDataTopic = _metadataTopic
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
                public DataSetMetaDataType? MetaData { get; }

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
                public DateTime? PublishTimestamp { get; }

                /// <inheritdoc/>
                public DateTime CreatedTimestamp { get; } = DateTime.UtcNow;

                /// <inheritdoc/>
                public uint? PublishSequenceNumber => null;

                /// <inheritdoc/>
                public object? Context { get; set; }

                /// <inheritdoc/>
                public IServiceMessageContext ServiceMessageContext { get; set; }

                /// <inheritdoc/>
                public IList<MonitoredItemNotificationModel> Notifications { get; }

                /// <inheritdoc/>
                public MetadataNotificationModel(IOpcUaSubscriptionNotification notification)
                {
                    SequenceNumber = notification.SequenceNumber;
                    DataSetName = notification.DataSetName;
                    ServiceMessageContext = notification.ServiceMessageContext;
                    MetaData = notification.MetaData;
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
                public void Dispose()
                {
                    // Nothing to do
                }
            }

            private readonly WriterGroupDataSource _outer;
            private readonly object _lock = new();
            private Timer? _metadataTimer;
            private volatile uint _frameCount;
            private readonly string _topic;
            private readonly string _metadataTopic;
            private SubscriptionModel _subscriptionInfo;
            private DataSetWriterModel _dataSetWriter;
            private uint _dataSetSequenceNumber;
            private uint _metadataSequenceNumber;
            private uint _currentMetadataMajorVersion;
            private uint _currentMetadataMinorVersion;
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
                () => new Measurement<long>(_heartbeats.Count, _metrics.TagList), "Heartbeats",
                "Total Heartbeats delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_heartbeats_per_second",
                () => new Measurement<double>(_heartbeats.Count / UpTime, _metrics.TagList), "Heartbeats/sec",
                "Opc Cyclic reads/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_heartbeats_per_second_last_min",
                () => new Measurement<long>(_heartbeats.LastMinute, _metrics.TagList), "Heartbeats",
                "Opc Cyclic reads/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_cyclicreads",
                () => new Measurement<long>(_cyclicReads.Count, _metrics.TagList), "Reads",
                "Total Cyclic reads delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_cyclicreads_per_second",
                () => new Measurement<double>(_cyclicReads.Count / UpTime, _metrics.TagList), "Reads/sec",
                "Opc Cyclic reads/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_cyclicreads_per_second_last_min",
                () => new Measurement<long>(_cyclicReads.LastMinute, _metrics.TagList), "Reads",
                "Opc Cyclic reads/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_modelchanges",
                () => new Measurement<long>(_modelChanges.Count, _metrics.TagList), "Changes",
                "Total Number of changes found in the address spaces of the connected servers.");
            _meter.CreateObservableGauge("iiot_edge_publisher_modelchanges_per_second",
                () => new Measurement<double>(_modelChanges.Count / UpTime, _metrics.TagList), "Changes/sec",
                "Address space Model changes/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_modelchanges_per_second_last_min",
                () => new Measurement<long>(_modelChanges.LastMinute, _metrics.TagList), "Changes",
                "Address space Model changes/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_value_changes",
                () => new Measurement<long>(_valueChanges.Count, _metrics.TagList), "Values",
                "Total Opc Value changes delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_value_changes_per_second",
                () => new Measurement<double>(_valueChanges.Count / UpTime, _metrics.TagList), "Values/sec",
                "Opc Value changes/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_value_changes_per_second_last_min",
                () => new Measurement<long>(_valueChanges.LastMinute, _metrics.TagList), "Values",
                "Opc Value changes/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_events",
                () => new Measurement<long>(_events.Count, _metrics.TagList), "Events",
                "Total Opc Events delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_events_per_second",
                () => new Measurement<double>(_events.Count / UpTime, _metrics.TagList), "Events/sec",
                "Opc Events/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_events_per_second_last_min",
                () => new Measurement<long>(_events.LastMinute, _metrics.TagList), "Events",
                "Opc Events/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_event_notifications",
                () => new Measurement<long>(_eventNotification.Count, _metrics.TagList), "Notifications",
                "Total Opc Event notifications delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_event_notifications_per_second",
                () => new Measurement<double>(_eventNotification.Count / UpTime, _metrics.TagList), "Notifications/sec",
                "Opc Event notifications/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_event_notifications_per_second_last_min",
                () => new Measurement<long>(_eventNotification.LastMinute, _metrics.TagList), "Notifications",
                "Opc Event notifications/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_data_changes",
                () => new Measurement<long>(_dataChanges.Count, _metrics.TagList), "Notifications",
                "Total Opc Data change notifications delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_data_changes_per_second",
                () => new Measurement<double>(_dataChanges.Count / UpTime, _metrics.TagList), "Notifications/sec",
                "Opc Data change notifications/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_data_changes_per_second_last_min",
                () => new Measurement<long>(_dataChanges.LastMinute, _metrics.TagList), "Notifications",
                "Opc Data change notifications/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_queue_overflows",
                () => new Measurement<long>(_overflows.Count, _metrics.TagList), "Values",
                "Total values received with a queue overflow indicator.");
            _meter.CreateObservableGauge("iiot_edge_publisher_queue_overflows_per_second",
                () => new Measurement<double>(_overflows.Count / UpTime, _metrics.TagList), "Values/sec",
                "Values with overflow indicator/second received.");
            _meter.CreateObservableGauge("iiot_edge_publisher_queue_overflows_per_second_last_min",
                () => new Measurement<long>(_overflows.LastMinute, _metrics.TagList), "Values",
                "Values with overflow indicator/second received in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_keep_alive_notifications",
                () => new Measurement<long>(_keepAliveCount, _metrics.TagList), "Notifications",
                "Total Opc keep alive notifications delivered for processing.");

            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_subscriptions",
                () => new Measurement<long>(_subscriptions.Count, _metrics.TagList), "Subscriptions",
                "Number of Writers/Subscriptions in the writer group.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_connection_retries",
                () => new Measurement<long>(ReconnectCount, _metrics.TagList), "Attempts",
                "OPC UA connect retries.");
            _meter.CreateObservableGauge("iiot_edge_publisher_is_connection_ok",
                () => new Measurement<int>(ConnectedClients, _metrics.TagList), "Endpoints",
                "OPC UA endpoints that are successfully connected.");
            _meter.CreateObservableGauge("iiot_edge_publisher_is_disconnected",
                () => new Measurement<int>(DisconnectedClients, _metrics.TagList), "Endpoints",
                "OPC UA endpoints that are disconnected.");
        }

        private const long kNumberOfInvokedMessagesResetThreshold = long.MaxValue - 10000;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private readonly ILogger _logger;
        private readonly Dictionary<SubscriptionIdentifier, DataSetWriterSubscription> _subscriptions;
        private readonly IOpcUaSubscriptionManager _subscriptionManager;
        private readonly IOptions<OpcUaSubscriptionOptions> _subscriptionConfig;
        private readonly IMetricsContext _metrics;
        private readonly IOptions<PublisherOptions> _options;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly RollingAverage _valueChanges = new();
        private readonly RollingAverage _dataChanges = new();
        private readonly RollingAverage _cyclicReads = new();
        private readonly RollingAverage _eventNotification = new();
        private readonly RollingAverage _events = new();
        private readonly RollingAverage _modelChanges = new();
        private readonly RollingAverage _heartbeats = new();
        private readonly RollingAverage _overflows = new();
        private long _keepAliveCount;
        private WriterGroupModel _writerGroup;
        private readonly DateTime _startTime = DateTime.UtcNow;
    }
}
