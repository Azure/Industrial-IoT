// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Triggers dataset writer messages on subscription changes
    /// </summary>
    public class WriterGroupDataSource : IMessageSource, IDisposable {

        /// <inheritdoc/>
        public string Id => _writerGroup.WriterGroupId;

        /// <inheritdoc/>
        public int NumberOfConnectionRetries => _subscriptions.Values.Sum(x => x.Subscription?.NumberOfConnectionRetries) ?? 0;

        /// <inheritdoc/>
        public bool IsConnectionOk => (_subscriptions?.Count == 0 ||
            _subscriptions.Values.Where(x => x.Subscription?.IsConnectionOk == true).Count() < _subscriptions?.Count) ? false : true;

        /// <inheritdoc/>
        public int NumberOfGoodNodes => _subscriptions.Values.Sum(x => x.Subscription?.NumberOfGoodNodes) ?? 0;

        /// <inheritdoc/>
        public int NumberOfBadNodes => _subscriptions.Values.Sum(x => x.Subscription?.NumberOfBadNodes) ?? 0;

        /// <inheritdoc/>
        public Uri EndpointUrl => _subscriptions.Count == 0 ? null : new Uri(_subscriptions.Values.First()?.Subscription?.Connection.Endpoint.Url);

        /// <inheritdoc/>
        public string DataSetWriterGroup => _writerGroup?.WriterGroupId;

        /// <inheritdoc/>
        public bool UseSecurity =>
            _subscriptions.Values.FirstOrDefault()?.Subscription?.Connection.Endpoint.SecurityMode != SecurityMode.None ?
                true : false;

        /// <inheritdoc/>
        public OpcAuthenticationMode AuthenticationMode =>
            _subscriptions.Values.FirstOrDefault()?.Subscription?.Connection?.User?.Value != null
            ? OpcAuthenticationMode.UsernamePassword
            : OpcAuthenticationMode.Anonymous;

        /// <inheritdoc/>
        public string AuthenticationUsername =>
            _subscriptions.Values.FirstOrDefault()?.Subscription?.Connection?.User?.Value != null
            ? _serializer.Deserialize<cred>(_subscriptions.Values
                .First()
                .Subscription
                .Connection
                .User
                .Value
                .ToJson())?
                .user
            : null;

        /// <inheritdoc/>
        public ulong ValueChangesCountLastMinute {
            get => CalculateSumForRingBuffer(_valueChangesBuffer, ref _lastPointerValueChanges, _bucketWidth, _lastWriteTimeValueChange);
            private set => IncreaseRingBuffer(_valueChangesBuffer, ref _lastPointerValueChanges, _bucketWidth, value, ref _lastWriteTimeValueChange);
        }

        /// <inheritdoc/>
        public ulong ValueChangesCount {
            get { return _valueChangesCount; }
            private set {
                var difference = value - _valueChangesCount;
                _valueChangesCount = value;
                ValueChangesCountLastMinute = difference;
            }
        }

        /// <inheritdoc/>
        public ulong DataChangesCountLastMinute {
            get => CalculateSumForRingBuffer(_dataChangesBuffer, ref _lastPointerDataChanges, _bucketWidth, _lastWriteTimeDataChange);
            private set => IncreaseRingBuffer(_dataChangesBuffer, ref _lastPointerDataChanges, _bucketWidth, value, ref _lastWriteTimeDataChange);
        }

        /// <inheritdoc/>
        public ulong DataChangesCount {
            get {
                return _dataChangesCount;
            }
            private set {
                var difference = value - _dataChangesCount;
                _dataChangesCount = value;
                DataChangesCountLastMinute = difference;
            }
        }

        /// <summary>
        /// Iterates the array and add up all values
        /// </summary>
        private static ulong CalculateSumForRingBuffer(ulong[] array, ref int lastPointer, int bucketWidth, DateTime lastWriteTime) {
            // if IncreaseRingBuffer wasn't called for some time, maybe some stale values are included
            UpdateRingBufferBuckets(array, ref lastPointer, bucketWidth, ref lastWriteTime);

            // with cleaned buffer, we can just accumulate all buckets
            ulong sum = 0;
            for (int index = 0; index < array.Length; index++) {
                sum += array[index];
            }
            return sum;
        }

        /// <summary>
        /// Helper function to distribute values over array based on time
        /// </summary>
        private static void IncreaseRingBuffer(ulong[] array, ref int lastPointer, int bucketWidth, ulong difference, ref DateTime lastWriteTime) {
            var indexPointer = UpdateRingBufferBuckets(array, ref lastPointer, bucketWidth, ref lastWriteTime);

            array[indexPointer] += difference;
        }

        /// <summary>
        /// Empty the ring buffer buckets if necessary
        /// </summary>
        private static int UpdateRingBufferBuckets(ulong[] array, ref int lastPointer, int bucketWidth, ref DateTime lastWriteTime) {
            var now = DateTime.UtcNow;
            var indexPointer = now.Second % bucketWidth;

            // if last update was > bucketsize seconds in the past delete whole array
            if (lastWriteTime != DateTime.MinValue) {
                var deleteWholeArray = (now - lastWriteTime).TotalSeconds >= bucketWidth;
                if (deleteWholeArray) {
                    Array.Clear(array, 0, array.Length);
                    lastPointer = indexPointer;
                }
            }

            // reset all buckets, between last write and now
            while (lastPointer != indexPointer) {
                lastPointer = (lastPointer + 1) % bucketWidth;
                array[lastPointer] = 0;
            }

            lastWriteTime = now;

            return indexPointer;
        }

        /// <inheritdoc/>
        public ulong EventNotificationCount { get; private set; }

        /// <inheritdoc/>
        public ulong EventCount { get; private set; }

        /// <inheritdoc/>
        public event EventHandler<SubscriptionNotificationModel> OnMessage;

        /// <inheritdoc/>
        public event EventHandler<EventArgs> OnCounterReset;

        /// <inheritdoc/>
        // TODO: This should go
        public async Task RunAsync(CancellationToken ct) {
            try {
                await StartAsync(ct);
                await Task.Delay(-1, ct).ConfigureAwait(false);
            }
            finally {
                await DisposeAsync();
            }
        }

        /// <inheritdoc/>
        // TODO: This should go
        public ValueTask ReconfigureAsync(object config, CancellationToken ct) {
            var jobConfig = config as WriterGroupJobModel ?? throw new ArgumentNullException(nameof(config));
            return UpdateAsync(jobConfig, ct);
        }

        /// <summary>
        /// Create trigger from writer group
        /// </summary>
        public WriterGroupDataSource(IWriterGroupConfig writerGroupConfig,
            ISubscriptionManager subscriptionManager, ILogger logger, IJsonSerializer serializer) {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ??
                throw new ArgumentNullException(nameof(subscriptionManager));
            _writerGroup = writerGroupConfig?.WriterGroup?.Clone() ??
                throw new ArgumentNullException(nameof(writerGroupConfig.WriterGroup));
            _subscriptions = new Dictionary<SubscriptionIdentifier, DataSetWriterSubscription>();
            _publisherId = writerGroupConfig.PublisherId ?? Guid.NewGuid().ToString();
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async ValueTask StartAsync(CancellationToken ct) {
            await _lock.WaitAsync(ct);
            try {
                Debug.Assert(_subscriptions.Count == 0);

                foreach (var writer in _writerGroup.DataSetWriters) {
                    // Create writer subscriptions
                    var writerSubscription = await DataSetWriterSubscription.CreateAsync(
                        this, writer, ct);
                    _subscriptions.AddOrUpdate(writerSubscription.Id, writerSubscription);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask UpdateAsync(WriterGroupJobModel jobConfig, CancellationToken ct) {
            await _lock.WaitAsync();
            try {
                var writerGroupConfig = jobConfig.ToWriterGroupJobConfiguration(_publisherId);

                if (writerGroupConfig?.WriterGroup?.DataSetWriters == null ||
                    writerGroupConfig.WriterGroup.DataSetWriters.Count == 0) {
                    foreach (var subscription in _subscriptions.Values) {
                        await subscription.DisposeAsync();
                    }
                    _logger.Information("Removed all subscriptions from writer group {Name}.",
                        _writerGroup.WriterGroupId);
                    _subscriptions.Clear();
                    return;
                }

                var writerGroupId = writerGroupConfig.WriterGroup.WriterGroupId;
                var dataSetWriters = _writerGroup.DataSetWriters
                    .ToDictionary(w => w.ToSubscriptionId(writerGroupId), w => w);
                foreach (var id in _subscriptions.Keys.ToList()) {
                    if (!dataSetWriters.TryGetValue(id, out var writer)) {
                        if (_subscriptions.Remove(id, out var s)) {
                            await s.DisposeAsync();
                        }
                    }
                    else {
                        // Update
                        if (_subscriptions.TryGetValue(id, out var s)) {
                            await s.UpdateAsync(writer, ct);
                        }
                    }
                }
                foreach (var writer in dataSetWriters) {
                    if (!_subscriptions.ContainsKey(writer.Key)) {
                        // Add
                        var writerSubscription = await DataSetWriterSubscription.CreateAsync(
                            this, writer.Value, ct);
                        _subscriptions.AddOrUpdate(writerSubscription.Id, writerSubscription);
                    }
                }
                _logger.Information("Update all subscriptions inside writer group {Name}.",
                    _writerGroup.WriterGroupId);

                if (_writerGroup.WriterGroupId != writerGroupConfig.WriterGroup.WriterGroupId) {
                    _logger.Information("Update writer group from to writer group {Name}.",
                        _writerGroup.WriterGroupId, writerGroupConfig.WriterGroup.WriterGroupId);
                }
                _writerGroup = writerGroupConfig.WriterGroup.Clone();
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            foreach (var s in _subscriptions.Values) {
                await s.DisposeAsync();
            }
            _subscriptions.Clear();
        }

        /// <summary>
        /// Helper to manage subscriptions
        /// </summary>
        private sealed class DataSetWriterSubscription : IAsyncDisposable {

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
            private DataSetWriterSubscription(WriterGroupDataSource outer,
                DataSetWriterModel dataSetWriter) {

                _outer = outer ?? throw new ArgumentNullException(nameof(outer));
                _dataSetWriter = dataSetWriter?.Clone() ??
                    throw new ArgumentNullException(nameof(dataSetWriter));
                _subscriptionInfo = _dataSetWriter.ToSubscriptionModel(
                    _outer._subscriptionManager.Configuration, outer._writerGroup.WriterGroupId);
            }

            /// <summary>
            /// Create subscription
            /// </summary>
            public static async ValueTask<DataSetWriterSubscription> CreateAsync(
                WriterGroupDataSource outer, DataSetWriterModel dataSetWriter,
                CancellationToken ct) {

                var dataSetSubscription = new DataSetWriterSubscription(outer, dataSetWriter);

                // Open subscription which creates the underlying OPC UA subscription
                await dataSetSubscription.OpenAsync(ct).ConfigureAwait(false);

                return dataSetSubscription;
            }

            /// <summary>
            /// Update subscription content
            /// </summary>
            public async ValueTask UpdateAsync(DataSetWriterModel dataSetWriter, CancellationToken ct) {
                if (Subscription == null) {
                    _outer._logger.Warning("Subscription does not exist");
                    return;
                }
                _outer._logger.Debug("Updating subscription {Id} in writer group {Name}...",
                    Id, _outer._writerGroup.WriterGroupId);

                _dataSetWriter = dataSetWriter.Clone();
                _subscriptionInfo = _dataSetWriter.ToSubscriptionModel(
                    _outer._subscriptionManager.Configuration, _outer._writerGroup.WriterGroupId);

                InitializeKeyframeTrigger();
                InitializeMetaDataTrigger();

                // Apply changes
                await Subscription.UpdateAsync(_subscriptionInfo, ct).ConfigureAwait(false);

                _outer._logger.Information("Updated subscription {Id} in writer group {Name}.",
                    Id, _outer._writerGroup.WriterGroupId);
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync() {
                try {
                    if (Subscription == null) {
                        return;
                    }
                    if (_metadataTimer != null) {
                        _metadataTimer.Stop();
                    }

                    await CloseAsync().ConfigureAwait(false);
                }
                finally {
                    _metadataTimer?.Dispose();
                    _metadataTimer = null;
                }
            }

            /// <summary>
            /// Open subscription
            /// </summary>
            /// <returns></returns>
            private async ValueTask OpenAsync(CancellationToken ct) {
                _outer._logger.Debug("Creating new subscription {Id} in writer group {Name}...",
                    Id, _outer._writerGroup.WriterGroupId);

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

                if (_metadataTimer != null) {
                    _metadataTimer.Start();
                }
                _outer._logger.Information("Created new subscription {Id} in writer group {Name}.",
                    Id, _outer._writerGroup.WriterGroupId);
            }

            /// <summary>
            /// Close subscription
            /// </summary>
            /// <returns></returns>
            private async ValueTask CloseAsync() {
                _outer._logger.Debug("Removing subscription {Id} from writer group {Name}...",
                    Id, _outer._writerGroup.WriterGroupId);

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

                _outer._logger.Information("Removed subscription {Id} from writer group {Name}.",
                    Id, _outer._writerGroup.WriterGroupId);
            }

            /// <summary>
            /// Initializes the key frame triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeKeyframeTrigger() {
                _frameCount = 0;
                _keyFrameCount = _outer._subscriptionManager.Configuration.DisableKeyFrames == true
                    ? 0 : _dataSetWriter.KeyFrameCount
                        ?? _outer._subscriptionManager.Configuration.DefaultKeyFrameCount ?? 0;
            }

            /// <summary>
            /// /// Initializes the Metadata triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeMetaDataTrigger() {

                var metaDataSendInterval = _dataSetWriter.MetaDataUpdateTime
                    .GetValueOrDefault(TimeSpan.Zero)
                    .TotalMilliseconds;

                if (metaDataSendInterval > 0 && _outer._subscriptionManager
                        .Configuration.DisableDataSetMetaData != true) {
                    if (_metadataTimer == null) {
                        _metadataTimer = new Timer(metaDataSendInterval);
                        _metadataTimer.Elapsed += MetadataTimerElapsed;
                    }
                    else {
                        _metadataTimer.Interval = metaDataSendInterval;
                    }
                }
                else {
                    if (_metadataTimer != null) {
                        _metadataTimer.Stop();
                        _metadataTimer.Dispose();
                        _metadataTimer = null;
                    }
                }
            }

            /// <summary>
            /// Fired when metadata time elapsed
            /// </summary>
            private void MetadataTimerElapsed(object sender, ElapsedEventArgs e) {
                try {
                    _metadataTimer.Enabled = false;
                    // Enabled again after calling message receiver delegate
                }
                catch (ObjectDisposedException) {
                    // Disposed while being invoked
                    return;
                }

                _outer._logger.Debug("Insert metadata message into Subscription {id}...", Id);
                var notification = Subscription.CreateKeepAlive();
                if (notification != null) {
                    // This call udpates the message type, so no need to do it here.
                    CallMessageReceiverDelegates(this, notification, true);
                }
                else {
                    // Failed to send, try again later
                    InitializeMetaDataTrigger();
                }
            }

            /// <summary>
            /// Handle subscription data change messages
            /// </summary>
            private void OnSubscriptionDataChangeNotification(object sender, SubscriptionNotificationModel notification) {
                CallMessageReceiverDelegates(sender, ProcessKeyFrame(notification));

                SubscriptionNotificationModel ProcessKeyFrame(SubscriptionNotificationModel notification) {
                    if (_keyFrameCount > 0) {
                        var frameCount = Interlocked.Increment(ref _frameCount);
                        if ((frameCount % _keyFrameCount) == 0) {
                            Subscription.TryUpgradeToKeyFrame(notification);
                        }
                    }
                    return notification;
                }
            }

            /// <summary>
            /// Handle subscription data diagnostics change messages
            /// </summary>
            private void OnSubscriptionDataDiagnosticsChanged(object sender, int notificationCount) {
                lock (_lock) {
                    if (_outer.DataChangesCount >= kNumberOfInvokedMessagesResetThreshold ||
                        _outer.ValueChangesCount >= kNumberOfInvokedMessagesResetThreshold) {
                        // reset both
                        _outer._logger.Debug("Notifications counter in subscription {Id} has been reset to prevent" +
                            " overflow. So far, {DataChangesCount} data changes and {ValueChangesCount} " +
                            "value changes were invoked by message source.",
                            Id, _outer.DataChangesCount, _outer.ValueChangesCount);
                        _outer.DataChangesCount = 0;
                        _outer.ValueChangesCount = 0;
                        _outer.OnCounterReset?.Invoke(this, EventArgs.Empty);
                    }

                    _outer.ValueChangesCount += (ulong)notificationCount;
                    _outer.DataChangesCount++;
                }
            }

            /// <summary>
            /// Handle subscription change messages
            /// </summary>
            private void OnSubscriptionEventNotification(object sender, SubscriptionNotificationModel notification) {
                CallMessageReceiverDelegates(sender, notification);
            }

            /// <summary>
            /// Handle subscription event diagnostics change messages
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="notificationCount"></param>
            private void OnSubscriptionEventDiagnosticsChanged(object sender, int notificationCount) {
                lock (_lock) {
                    if (_outer.EventCount >= kNumberOfInvokedMessagesResetThreshold ||
                        _outer.EventNotificationCount >= kNumberOfInvokedMessagesResetThreshold) {
                        // reset both
                        _outer._logger.Debug("Notifications counter in subscription {Id} has been reset to prevent" +
                            " overflow. So far, {EventChangesCount} event changes and {EventValueChangesCount} " +
                            "event value changes were invoked by message source.",
                            Id, _outer.EventCount, _outer.EventNotificationCount);
                        _outer.EventCount = 0;
                        _outer.EventNotificationCount = 0;
                        _outer.OnCounterReset?.Invoke(this, EventArgs.Empty);
                    }

                    _outer.EventNotificationCount += (ulong)notificationCount;
                    _outer.EventCount++;
                }
            }

            /// <summary>
            /// handle subscription change messages
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="notification"></param>
            /// <param name="metaDataTimer"></param>
            private void CallMessageReceiverDelegates(object sender,
                SubscriptionNotificationModel notification, bool metaDataTimer = false) {
                try {
                    lock (_lock) {
                        if (notification.MetaData != null) {
                            var sendMetadata = metaDataTimer;
                            //
                            // Only send if called from metadata timer or if the metadata version changes.
                            // Metadata reference is owned by the notification/message, a new metadata is
                            // created when it changes so old one is not mutated and this should be safe.
                            //
                            if (_currentMetadataMajorVersion != notification.MetaData.ConfigurationVersion.MajorVersion &&
                                _currentMetadataMinorVersion != notification.MetaData.ConfigurationVersion.MinorVersion) {
                                _currentMetadataMajorVersion = notification.MetaData.ConfigurationVersion.MajorVersion;
                                _currentMetadataMinorVersion = notification.MetaData.ConfigurationVersion.MinorVersion;
                                sendMetadata = true;
                            }
                            if (sendMetadata) {
                                var metadata = new SubscriptionNotificationModel {
                                    Context = CreateMessageContext(ref _metadataSequenceNumber),
                                    MessageType = Opc.Ua.PubSub.MessageType.Metadata,
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

                        if (!metaDataTimer) {
                            Debug.Assert(notification.Notifications != null);
                            notification.Context = CreateMessageContext(ref _dataSetSequenceNumber);
                            _outer.OnMessage?.Invoke(sender, notification);

                            if (notification.MessageType != Opc.Ua.PubSub.MessageType.DeltaFrame &&
                                notification.MessageType != Opc.Ua.PubSub.MessageType.KeepAlive) {
                                // Reset keyframe trigger for events, keyframe, and conditions
                                // which are all key frame like messages
                                InitializeKeyframeTrigger();
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    _outer._logger.Warning(ex, "Failed to produce message.");
                }

                WriterGroupMessageContext CreateMessageContext(ref uint sequenceNumber) {
                    while (sequenceNumber == 0) {
                        unchecked { sequenceNumber++; }
                    }
                    return new WriterGroupMessageContext {
                        PublisherId = _outer._publisherId,
                        Writer = _dataSetWriter,
                        SequenceNumber = sequenceNumber,
                        WriterGroup = _outer._writerGroup
                    };
                }
            }

            private readonly WriterGroupDataSource _outer;
            private readonly object _lock = new object();
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
        /// Helper to deserialize credential info
        /// </summary>
        private class cred {
            public string user { get; set; }
            public string password { get; set; }
        }

        private const ulong kNumberOfInvokedMessagesResetThreshold = ulong.MaxValue - 10000;
        private const int _bucketWidth = 60;

        private readonly ILogger _logger;
        private readonly Dictionary<SubscriptionIdentifier, DataSetWriterSubscription> _subscriptions;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ulong[] _valueChangesBuffer = new ulong[_bucketWidth];
        private readonly ulong[] _dataChangesBuffer = new ulong[_bucketWidth];

        private WriterGroupModel _writerGroup;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private string _publisherId;
        private int _lastPointerValueChanges;
        private ulong _valueChangesCount;
        private int _lastPointerDataChanges;
        private ulong _dataChangesCount;
        private DateTime _lastWriteTimeValueChange = DateTime.MinValue;
        private DateTime _lastWriteTimeDataChange = DateTime.MinValue;
        private readonly IJsonSerializer _serializer;
    }
}
