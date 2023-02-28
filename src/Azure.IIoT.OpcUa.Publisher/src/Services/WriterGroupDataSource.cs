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
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Logging;
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
    public sealed class WriterGroupDataSource : IMessageSource, IDisposable
    {
        /// <inheritdoc/>
        public event EventHandler<SubscriptionNotificationModel> OnMessage;

        /// <inheritdoc/>
        public event EventHandler<EventArgs> OnCounterReset;

        /// <summary>
        /// Create trigger from writer group
        /// </summary>
        /// <param name="writerGroupConfig"></param>
        /// <param name="subscriptionManager"></param>
        /// <param name="subscriptionConfig"></param>
        /// <param name="metrics"></param>
        /// <param name="logger"></param>
        public WriterGroupDataSource(IWriterGroupConfig writerGroupConfig,
            ISubscriptionManager subscriptionManager, ISubscriptionConfig subscriptionConfig,
            IMetricsContext metrics, ILogger logger)
            : this(metrics ?? throw new ArgumentNullException(nameof(metrics)))
        {
            _writerGroup = writerGroupConfig?.WriterGroup?.Clone() ??
                throw new ArgumentNullException(nameof(writerGroupConfig.WriterGroup));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ??
                throw new ArgumentNullException(nameof(subscriptionManager));
            _subscriptionConfig = subscriptionConfig ??
                throw new ArgumentNullException(nameof(subscriptionConfig));

            _subscriptions = new Dictionary<SubscriptionIdentifier, DataSetWriterSubscription>();
            _publisherId = writerGroupConfig.PublisherId ?? Guid.NewGuid().ToString();
        }

        /// <inheritdoc/>
        public async ValueTask StartAsync(CancellationToken ct)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                Debug.Assert(_subscriptions.Count == 0);

                foreach (var writer in _writerGroup.DataSetWriters)
                {
                    // Create writer subscriptions
                    var writerSubscription = await DataSetWriterSubscription.CreateAsync(
                        this, writer, ct).ConfigureAwait(false);
                    _subscriptions.AddOrUpdate(writerSubscription.Id, writerSubscription);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask UpdateAsync(WriterGroupJobModel jobConfig, CancellationToken ct)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var writerGroupConfig = jobConfig.ToWriterGroupJobConfiguration(_publisherId);
                var writerGroup = writerGroupConfig.WriterGroup.Clone();

                if (writerGroup?.DataSetWriters == null ||
                    writerGroup.DataSetWriters.Count == 0)
                {
                    foreach (var subscription in _subscriptions.Values)
                    {
                        await subscription.DisposeAsync().ConfigureAwait(false);
                    }
                    _logger.LogInformation("Removed all subscriptions from writer group {Name}.",
                        writerGroup.WriterGroupId);
                    _subscriptions.Clear();
                    _writerGroup = writerGroup;
                    return;
                }

                var dataSetWriterSubscriptionMap = writerGroup.DataSetWriters
                    .ToDictionary(w => w.ToSubscriptionId(writerGroup.WriterGroupId), w => w);
                // Update or removed ones that were updated or removed.
                foreach (var id in _subscriptions.Keys.ToList())
                {
                    if (!dataSetWriterSubscriptionMap.TryGetValue(id, out var writer))
                    {
                        if (_subscriptions.Remove(id, out var s))
                        {
                            await s.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // Update
                        if (_subscriptions.TryGetValue(id, out var s))
                        {
                            await s.UpdateAsync(writer, ct).ConfigureAwait(false);
                        }
                    }
                }
                // Create any newly added ones
                foreach (var writer in dataSetWriterSubscriptionMap)
                {
                    if (!_subscriptions.ContainsKey(writer.Key))
                    {
                        // Add
                        var writerSubscription = await DataSetWriterSubscription.CreateAsync(
                            this, writer.Value, ct).ConfigureAwait(false);
                        _subscriptions.AddOrUpdate(writerSubscription.Id, writerSubscription);
                    }
                }
                if (_writerGroup.WriterGroupId != writerGroup.WriterGroupId)
                {
                    _logger.LogInformation("Update writer group from {Previous} to writer group {Name}.",
                        _writerGroup.WriterGroupId, writerGroup.WriterGroupId);
                }

                _logger.LogInformation("Update all subscriptions inside writer group {Name}.",
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
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            foreach (var s in _subscriptions.Values)
            {
                await s.DisposeAsync().ConfigureAwait(false);
            }
            _subscriptions.Clear();
        }

        /// <summary>
        /// Helper to manage subscriptions
        /// </summary>
        private sealed class DataSetWriterSubscription : IAsyncDisposable
        {
            /// <summary>
            /// Subscription id
            /// </summary>
            public SubscriptionIdentifier Id => _subscriptionInfo.Id;

            /// <summary>
            /// Active subscription
            /// </summary>
            public ISubscription Subscription { get; set; }

            /// <summary>
            /// Create subscription from a DataSetWriterModel template
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="dataSetWriter"></param>
            private DataSetWriterSubscription(WriterGroupDataSource outer,
                DataSetWriterModel dataSetWriter)
            {
                _outer = outer ?? throw new ArgumentNullException(nameof(outer));
                _dataSetWriter = dataSetWriter?.Clone() ??
                    throw new ArgumentNullException(nameof(dataSetWriter));
                _subscriptionInfo = _dataSetWriter.ToSubscriptionModel(
                    _outer._subscriptionConfig, outer._writerGroup.WriterGroupId);
            }

            /// <summary>
            /// Create subscription
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="dataSetWriter"></param>
            /// <param name="ct"></param>
            public static async ValueTask<DataSetWriterSubscription> CreateAsync(
                WriterGroupDataSource outer, DataSetWriterModel dataSetWriter,
                CancellationToken ct)
            {
                var dataSetSubscription = new DataSetWriterSubscription(outer, dataSetWriter);

                // Open subscription which creates the underlying OPC UA subscription
                await dataSetSubscription.OpenAsync(ct).ConfigureAwait(false);

                return dataSetSubscription;
            }

            /// <summary>
            /// Update subscription content
            /// </summary>
            /// <param name="dataSetWriter"></param>
            /// <param name="ct"></param>
            public async ValueTask UpdateAsync(DataSetWriterModel dataSetWriter, CancellationToken ct)
            {
                if (Subscription == null)
                {
                    _outer._logger.LogWarning("Subscription does not exist");
                    return;
                }
                _outer._logger.LogDebug("Updating subscription {Id} in writer group {Name}...",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);

                _dataSetWriter = dataSetWriter.Clone();
                _subscriptionInfo = _dataSetWriter.ToSubscriptionModel(
                    _outer._subscriptionConfig, _outer._writerGroup.WriterGroupId);

                InitializeKeyframeTrigger();
                InitializeMetaDataTrigger();

                // Apply changes
                await Subscription.UpdateAsync(_subscriptionInfo, ct).ConfigureAwait(false);

                _outer._logger.LogInformation("Updated subscription {Id} in writer group {Name}.",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    if (Subscription == null)
                    {
                        return;
                    }
                    _metadataTimer?.Stop();

                    await CloseAsync().ConfigureAwait(false);
                }
                finally
                {
                    _metadataTimer?.Dispose();
                    _metadataTimer = null;
                }
            }

            /// <summary>
            /// Open subscription
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async ValueTask OpenAsync(CancellationToken ct)
            {
                _outer._logger.LogDebug("Creating new subscription {Id} in writer group {Name}...",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);

                //
                // Creating inner OPC UA subscription object. This will create a session
                // if none already exist and transfer the subscription into the session
                // management realm
                //
                Subscription = await _outer._subscriptionManager.CreateSubscriptionAsync(
                    _subscriptionInfo, ct).ConfigureAwait(false);

                InitializeKeyframeTrigger();
                InitializeMetaDataTrigger();

                Subscription.OnSubscriptionDataChange
                    += OnSubscriptionDataChangeNotification;
                Subscription.OnSubscriptionEventChange
                    += OnSubscriptionEventNotification;
                Subscription.OnSubscriptionDataDiagnosticsChange
                    += OnSubscriptionDataDiagnosticsChanged;
                Subscription.OnSubscriptionEventDiagnosticsChange
                    += OnSubscriptionEventDiagnosticsChanged;

                _metadataTimer?.Start();
                _outer._logger.LogInformation("Created new subscription {Id} in writer group {Name}.",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);
            }

            /// <summary>
            /// Close subscription
            /// </summary>
            /// <returns></returns>
            private async ValueTask CloseAsync()
            {
                _outer._logger.LogDebug("Removing subscription {Id} from writer group {Name}...",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);

                await Subscription.CloseAsync().ConfigureAwait(false);

                Subscription.OnSubscriptionDataChange
                    -= OnSubscriptionDataChangeNotification;
                Subscription.OnSubscriptionEventChange
                    -= OnSubscriptionEventNotification;
                Subscription.OnSubscriptionDataDiagnosticsChange
                    -= OnSubscriptionDataDiagnosticsChanged;
                Subscription.OnSubscriptionEventDiagnosticsChange
                    -= OnSubscriptionEventDiagnosticsChanged;

                Subscription.Dispose();
                Subscription = null;

                _outer._logger.LogInformation("Removed subscription {Id} from writer group {Name}.",
                    Id, _outer._writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId);
            }

            /// <summary>
            /// Initializes the key frame triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeKeyframeTrigger()
            {
                _frameCount = 0;
                _keyFrameCount = _outer._subscriptionConfig.DisableKeyFrames == true
                    ? 0 : _dataSetWriter.KeyFrameCount
                        ?? _outer._subscriptionConfig.DefaultKeyFrameCount ?? 0;
            }

            /// <summary>
            /// /// Initializes the Metadata triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeMetaDataTrigger()
            {
                var metaDataSendInterval = _dataSetWriter.MetaDataUpdateTime
                    .GetValueOrDefault(TimeSpan.Zero)
                    .TotalMilliseconds;

                if (metaDataSendInterval > 0 && _outer._subscriptionConfig.DisableDataSetMetaData != true)
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
            private void MetadataTimerElapsed(object sender, ElapsedEventArgs e)
            {
                try
                {
                    _metadataTimer.Enabled = false;
                    // Enabled again after calling message receiver delegate
                }
                catch (ObjectDisposedException)
                {
                    // Disposed while being invoked
                    return;
                }

                _outer._logger.LogDebug("Insert metadata message into Subscription {Id}...", Id);
                var notification = Subscription.CreateKeepAlive();
                if (notification != null)
                {
                    // This call udpates the message type, so no need to do it here.
                    CallMessageReceiverDelegates(this, notification, true);
                }
                else
                {
                    // Failed to send, try again later
                    InitializeMetaDataTrigger();
                }
            }

            /// <summary>
            /// Handle subscription data change messages
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="notification"></param>
            private void OnSubscriptionDataChangeNotification(object sender, SubscriptionNotificationModel notification)
            {
                CallMessageReceiverDelegates(sender, ProcessKeyFrame(notification));

                SubscriptionNotificationModel ProcessKeyFrame(SubscriptionNotificationModel notification)
                {
                    if (_keyFrameCount > 0)
                    {
                        var frameCount = Interlocked.Increment(ref _frameCount);
                        if ((frameCount % _keyFrameCount) == 0)
                        {
                            Subscription.TryUpgradeToKeyFrame(notification);
                        }
                    }
                    return notification;
                }
            }

            /// <summary>
            /// Handle subscription data diagnostics change messages
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="notificationCount"></param>
            private void OnSubscriptionDataDiagnosticsChanged(object sender, int notificationCount)
            {
                lock (_lock)
                {
                    if (_outer.DataChangesCount >= kNumberOfInvokedMessagesResetThreshold ||
                        _outer.ValueChangesCount >= kNumberOfInvokedMessagesResetThreshold)
                    {
                        // reset both
                        _outer._logger.LogDebug("Notifications counter in subscription {Id} has been reset to prevent" +
                            " overflow. So far, {DataChangesCount} data changes and {ValueChangesCount} " +
                            "value changes were invoked by message source.",
                            Id, _outer.DataChangesCount, _outer.ValueChangesCount);
                        _outer.DataChangesCount = 0;
                        _outer.ValueChangesCount = 0;
                        _outer.OnCounterReset?.Invoke(this, EventArgs.Empty);
                    }

                    _outer.ValueChangesCount += notificationCount;
                    _outer.DataChangesCount++;
                }
            }

            /// <summary>
            /// Handle subscription change messages
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="notification"></param>
            private void OnSubscriptionEventNotification(object sender, SubscriptionNotificationModel notification)
            {
                CallMessageReceiverDelegates(sender, notification);
            }

            /// <summary>
            /// Handle subscription event diagnostics change messages
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="notificationCount"></param>
            private void OnSubscriptionEventDiagnosticsChanged(object sender, int notificationCount)
            {
                lock (_lock)
                {
                    if (_outer._eventCount >= kNumberOfInvokedMessagesResetThreshold ||
                        _outer._eventNotificationCount >= kNumberOfInvokedMessagesResetThreshold)
                    {
                        // reset both
                        _outer._logger.LogDebug("Notifications counter in subscription {Id} has been reset to prevent" +
                            " overflow. So far, {EventChangesCount} event changes and {EventValueChangesCount} " +
                            "event value changes were invoked by message source.",
                            Id, _outer._eventCount, _outer._eventNotificationCount);
                        _outer._eventCount = 0;
                        _outer._eventNotificationCount = 0;
                        _outer.OnCounterReset?.Invoke(this, EventArgs.Empty);
                    }

                    _outer._eventNotificationCount += notificationCount;
                    _outer._eventCount++;
                }
            }

            /// <summary>
            /// handle subscription change messages
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="notification"></param>
            /// <param name="metaDataTimer"></param>
            private void CallMessageReceiverDelegates(object sender,
                SubscriptionNotificationModel notification, bool metaDataTimer = false)
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
                                var metadata = new SubscriptionNotificationModel
                                {
                                    Context = CreateMessageContext(ref _metadataSequenceNumber),
                                    MessageType = Azure.IIoT.OpcUa.Encoders.PubSub.MessageType.Metadata,
                                    SequenceNumber = notification.SequenceNumber,
                                    ServiceMessageContext = notification.ServiceMessageContext,
                                    MetaData = notification.MetaData,
                                    Timestamp = notification.Timestamp,
                                    SubscriptionId = notification.SubscriptionId,
                                    SubscriptionName = notification.SubscriptionName,
                                    ApplicationUri = notification.ApplicationUri,
                                    EndpointUrl = notification.EndpointUrl
                                };
                                _outer.OnMessage?.Invoke(sender, metadata);
                                InitializeMetaDataTrigger();
                            }
                        }

                        if (!metaDataTimer)
                        {
                            Debug.Assert(notification.Notifications != null);
                            notification.Context = CreateMessageContext(ref _dataSetSequenceNumber);
                            _outer.OnMessage?.Invoke(sender, notification);

                            if (notification.MessageType != Azure.IIoT.OpcUa.Encoders.PubSub.MessageType.DeltaFrame &&
                                notification.MessageType != Azure.IIoT.OpcUa.Encoders.PubSub.MessageType.KeepAlive)
                            {
                                // Reset keyframe trigger for events, keyframe, and conditions
                                // which are all key frame like messages
                                InitializeKeyframeTrigger();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _outer._logger.LogWarning(ex, "Failed to produce message.");
                }

                WriterGroupMessageContext CreateMessageContext(ref uint sequenceNumber)
                {
                    while (sequenceNumber == 0)
                    {
                        unchecked { sequenceNumber++; }
                    }
                    return new WriterGroupMessageContext
                    {
                        PublisherId = _outer._publisherId,
                        Writer = _dataSetWriter,
                        SequenceNumber = sequenceNumber,
                        WriterGroup = _outer._writerGroup
                    };
                }
            }

            private readonly WriterGroupDataSource _outer;
            private readonly object _lock = new();
            private Timer _metadataTimer;
            private uint _keyFrameCount;
            private volatile uint _frameCount;
            private SubscriptionModel _subscriptionInfo;
            private DataSetWriterModel _dataSetWriter;
            private uint _dataSetSequenceNumber;
            private uint _metadataSequenceNumber;
            private uint _currentMetadataMajorVersion;
            private uint _currentMetadataMinorVersion;
        }

        /// <summary>
        /// Iterates the array and add up all values
        /// </summary>
        /// <param name="array"></param>
        /// <param name="lastPointer"></param>
        /// <param name="bucketWidth"></param>
        /// <param name="lastWriteTime"></param>
        private static long CalculateSumForRingBuffer(long[] array, ref int lastPointer,
            int bucketWidth, DateTime lastWriteTime)
        {
            // if IncreaseRingBuffer wasn't called for some time, maybe some stale values are included
            UpdateRingBufferBuckets(array, ref lastPointer, bucketWidth, ref lastWriteTime);
            // with cleaned buffer, we can just accumulate all buckets
            long sum = 0;
            for (var index = 0; index < array.Length; index++)
            {
                sum += array[index];
            }
            return sum;
        }

        /// <summary>
        /// Runtime duration
        /// </summary>
        private double UpTime => (DateTime.UtcNow - _startTime).TotalSeconds;

        /// <summary>
        /// Helper function to distribute values over array based on time
        /// </summary>
        /// <param name="array"></param>
        /// <param name="lastPointer"></param>
        /// <param name="bucketWidth"></param>
        /// <param name="difference"></param>
        /// <param name="lastWriteTime"></param>
        private static void IncreaseRingBuffer(long[] array, ref int lastPointer,
            int bucketWidth, long difference, ref DateTime lastWriteTime)
        {
            var indexPointer = UpdateRingBufferBuckets(array, ref lastPointer,
                bucketWidth, ref lastWriteTime);
            array[indexPointer] += difference;
        }

        /// <summary>
        /// Empty the ring buffer buckets if necessary
        /// </summary>
        /// <param name="array"></param>
        /// <param name="lastPointer"></param>
        /// <param name="bucketWidth"></param>
        /// <param name="lastWriteTime"></param>
        private static int UpdateRingBufferBuckets(long[] array, ref int lastPointer,
            int bucketWidth, ref DateTime lastWriteTime)
        {
            var now = DateTime.UtcNow;
            var indexPointer = now.Second % bucketWidth;

            // if last update was > bucketsize seconds in the past delete whole array
            if (lastWriteTime != DateTime.MinValue)
            {
                var deleteWholeArray = (now - lastWriteTime).TotalSeconds >= bucketWidth;
                if (deleteWholeArray)
                {
                    Array.Clear(array, 0, array.Length);
                    lastPointer = indexPointer;
                }
            }

            // reset all buckets, between last write and now
            while (lastPointer != indexPointer)
            {
                lastPointer = (lastPointer + 1) % bucketWidth;
                array[lastPointer] = 0;
            }

            lastWriteTime = now;

            return indexPointer;
        }

        /// <summary>
        /// Calculate value chnages in the last minute
        /// </summary>
        private long ValueChangesCountLastMinute
        {
            get => CalculateSumForRingBuffer(_valueChangesBuffer, ref _lastPointerValueChanges,
                _bucketWidth, _lastWriteTimeValueChange);
            set => IncreaseRingBuffer(_valueChangesBuffer, ref _lastPointerValueChanges,
                _bucketWidth, value, ref _lastWriteTimeValueChange);
        }

        /// <summary>
        /// Get/Update value changes
        /// </summary>
        private long ValueChangesCount
        {
            get => _valueChangesCount;
            set
            {
                var difference = value - _valueChangesCount;
                _valueChangesCount = value;
                ValueChangesCountLastMinute = difference;
            }
        }

        /// <summary>
        /// Datas changes last minute
        /// </summary>
        private long DataChangesCountLastMinute
        {
            get => CalculateSumForRingBuffer(_dataChangesBuffer,
                ref _lastPointerDataChanges, _bucketWidth, _lastWriteTimeDataChange);
            set => IncreaseRingBuffer(_dataChangesBuffer,
                ref _lastPointerDataChanges, _bucketWidth, value, ref _lastWriteTimeDataChange);
        }

        /// <summary>
        /// Date changes total
        /// </summary>
        private long DataChangesCount
        {
            get => _dataChangesCount;
            set
            {
                var difference = value - _dataChangesCount;
                _dataChangesCount = value;
                DataChangesCountLastMinute = difference;
            }
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        private WriterGroupDataSource(IMetricsContext metrics)
        {
            Diagnostics.Meter.CreateObservableCounter("iiot_edge_publisher_events",
                () => new Measurement<long>(_eventCount, metrics.TagList), "Events",
                "Total Opc Events delivered for processing.");
            Diagnostics.Meter.CreateObservableCounter("iiot_edge_publisher_value_changes",
                () => new Measurement<long>(ValueChangesCount, metrics.TagList), "Values",
                "Total Opc Value changes delivered for processing.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_value_changes_per_second",
                () => new Measurement<double>(ValueChangesCount / UpTime, metrics.TagList), "Values/sec",
                "Opc Value changes/second delivered for processing.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_value_changes_per_second_last_min",
                () => new Measurement<long>(ValueChangesCountLastMinute, metrics.TagList), "Values",
                "Opc Value changes/second delivered for processing in last 60s.");

            Diagnostics.Meter.CreateObservableCounter("iiot_edge_publisher_event_notifications",
                () => new Measurement<long>(_eventNotificationCount, metrics.TagList), "Notifications",
                "Total Opc Event notifications delivered for processing.");
            Diagnostics.Meter.CreateObservableCounter("iiot_edge_publisher_data_changes",
                () => new Measurement<long>(DataChangesCount, metrics.TagList), "Notifications",
                "Total Opc Data change notifications delivered for processing.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_data_changes_per_second",
                () => new Measurement<double>(DataChangesCount / UpTime, metrics.TagList), "Notifications/sec",
                "Opc Data change notifications/second delivered for processing.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_data_changes_per_second_last_min",
                () => new Measurement<long>(DataChangesCountLastMinute, metrics.TagList), "Notifications",
                "Opc Data change notifications/second delivered for processing in last 60s.");
        }

        private const long kNumberOfInvokedMessagesResetThreshold = long.MaxValue - 10000;
        private const int _bucketWidth = 60;
        private readonly long[] _valueChangesBuffer = new long[_bucketWidth];
        private readonly long[] _dataChangesBuffer = new long[_bucketWidth];
        private readonly ILogger _logger;
        private readonly Dictionary<SubscriptionIdentifier, DataSetWriterSubscription> _subscriptions;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ISubscriptionConfig _subscriptionConfig;
        private WriterGroupModel _writerGroup;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly string _publisherId;
        private int _lastPointerValueChanges;
        private long _valueChangesCount;
        private int _lastPointerDataChanges;
        private long _dataChangesCount;
        private long _eventNotificationCount;
        private long _eventCount;
        private DateTime _lastWriteTimeValueChange = DateTime.MinValue;
        private DateTime _lastWriteTimeDataChange = DateTime.MinValue;
        private readonly DateTime _startTime = DateTime.UtcNow;
    }
}
