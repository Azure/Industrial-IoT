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
    public class WriterGroupMessageTrigger : IMessageTrigger, IDisposable {
        /// <inheritdoc/>
        public string Id => _subscriptions?.FirstOrDefault()?.Subscription?.Connection?.CreateConnectionId() ?? _writerGroup.WriterGroupId;

        /// <inheritdoc/>
        public int NumberOfConnectionRetries => _subscriptions?.Sum(x => x.Subscription?.NumberOfConnectionRetries) ?? 0;

        /// <inheritdoc/>
        public bool IsConnectionOk => (_subscriptions?.Count == 0 ||
            _subscriptions?.Where(x => x.Subscription?.IsConnectionOk == true).Count() < _subscriptions?.Count) ? false : true;

        /// <inheritdoc/>
        public int NumberOfGoodNodes => _subscriptions?.Sum(x => x.Subscription?.NumberOfGoodNodes) ?? 0;

        /// <inheritdoc/>
        public int NumberOfBadNodes => _subscriptions?.Sum(x => x.Subscription?.NumberOfBadNodes) ?? 0;

        /// <inheritdoc/>
        public Uri EndpointUrl => _subscriptions?.Count == 0 ? null : new Uri(_subscriptions?.First()?.Subscription?.Connection.Endpoint.Url);

        /// <inheritdoc/>
        public string DataSetWriterGroup => _subscriptions?.FirstOrDefault()?.Subscription?.Connection.Group;

        /// <inheritdoc/>
        public bool UseSecurity =>
            _subscriptions?.FirstOrDefault()?.Subscription?.Connection.Endpoint.SecurityMode != SecurityMode.None ?
                true : false;

        /// <inheritdoc/>
        public OpcAuthenticationMode AuthenticationMode =>
            _subscriptions?.FirstOrDefault()?.Subscription?.Connection?.User?.Value != null
            ? OpcAuthenticationMode.UsernamePassword
            : OpcAuthenticationMode.Anonymous;

        /// <inheritdoc/>
        public string AuthenticationUsername =>
            _subscriptions?.FirstOrDefault()?.Subscription?.Connection?.User?.Value != null
            ? _serializer.Deserialize<cred>(_subscriptions
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

        private void FireOnCounterResetEvent() {
            OnCounterReset?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Create trigger from writer group
        /// </summary>
        public WriterGroupMessageTrigger(IWriterGroupConfig writerGroupConfig,
            ISubscriptionManager subscriptionManager, ILogger logger, IJsonSerializer serializer) {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ??
                throw new ArgumentNullException(nameof(subscriptionManager));
            _writerGroup = writerGroupConfig?.WriterGroup?.Clone() ??
                throw new ArgumentNullException(nameof(writerGroupConfig.WriterGroup));
            _subscriptions = _writerGroup.DataSetWriters?
                .Select(g => new DataSetWriterSubscription(this, g, writerGroupConfig))
                .ToList();
            _publisherId = writerGroupConfig.PublisherId ?? Guid.NewGuid().ToString();
            _subscriptions.ForEach(async sc => await sc.OpenAsync().ConfigureAwait(false));
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken ct) {
            _subscriptions.ForEach(async sc => await sc.ActivateAsync().ConfigureAwait(false));
            await Task.Delay(-1, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void Reconfigure(object config) {

            var jobConfig = config as WriterGroupJobModel ?? throw new ArgumentNullException(nameof(config)); ;

            var writerGroupConfig = jobConfig?.ToWriterGroupJobConfiguration(_publisherId);
            _writerGroup = writerGroupConfig?.WriterGroup?.Clone();
            var newSubscriptions = jobConfig?.WriterGroup?.DataSetWriters?
                .Select(g => new DataSetWriterSubscription(this, g, writerGroupConfig))
                .ToList();
            _publisherId = writerGroupConfig?.PublisherId ?? Guid.NewGuid().ToString();
            var toAdd = newSubscriptions.Except(_subscriptions).ToList();
            var toUpdate = _subscriptions.Intersect(newSubscriptions).ToList();
            var toRemove = _subscriptions.Except(newSubscriptions).ToList();

            _subscriptions.Clear();
            _subscriptions.AddRange(toUpdate);
            _subscriptions.AddRange(toAdd);

            toRemove.ForEach(sc => sc.Dispose());

            toAdd.ForEach(async sc => await sc.OpenAsync().ConfigureAwait(false));
            toUpdate.ForEach(async sc => {
                var newSc = newSubscriptions.Find(s => s.Equals(sc));
                await sc.UpdateAsync(newSc);
            });
        }

        /// <inheritdoc/>
        public void Dispose() {
            _subscriptions.ForEach(async sc => await sc.DeactivateAsync().ConfigureAwait(false));
            _subscriptions.ForEach(sc => sc.Dispose());
            _subscriptions.Clear();
        }

        /// <summary>
        /// Helper to manage subscriptions
        /// </summary>
        private sealed class DataSetWriterSubscription : IDisposable {

            /// <summary>
            /// Active subscription
            /// </summary>
            public ISubscription Subscription { get; set; }

            /// <summary>
            /// Create subscription from a DataSetWriterModel template
            /// </summary>
            public DataSetWriterSubscription(WriterGroupMessageTrigger outer,
                DataSetWriterModel dataSetWriter, IWriterGroupConfig writerGroup) {

                _outer = outer ?? throw new ArgumentNullException(nameof(outer));
                _dataSetWriter = dataSetWriter?.Clone() ??
                    throw new ArgumentNullException(nameof(dataSetWriter));
                _subscriptionInfo = _dataSetWriter.ToSubscriptionModel(writerGroup);
                InitializeKeyframeTrigger(dataSetWriter);
                InitializeMetaDataTrigger(dataSetWriter);
            }

            /// <summary>
            /// Open subscription
            /// </summary>
            public async Task OpenAsync() {
                if (Subscription != null) {
                    _outer._logger.Warning("Subscription already exists");
                    return;
                }

                var sc = await _outer._subscriptionManager.GetOrCreateSubscriptionAsync(
                    _subscriptionInfo).ConfigureAwait(false);
                sc.OnSubscriptionDataChange += OnSubscriptionDataChangeNotification;
                sc.OnSubscriptionEventChange += OnSubscriptionEventNotification;
                sc.OnSubscriptionDataDiagnosticsChange += OnSubscriptionDataDiagnosticsChanged;
                sc.OnSubscriptionEventDiagnosticsChange += OnSubscriptionEventDiagnosticsChanged;
                await sc.ApplyAsync(_subscriptionInfo.MonitoredItems,
                    _subscriptionInfo.Configuration).ConfigureAwait(false);
                Subscription = sc;
            }

            /// <summary>
            /// Update subscription content
            /// </summary>
            public async Task UpdateAsync(DataSetWriterSubscription newInfo) {
                if (Subscription == null) {
                    _outer._logger.Warning("Subscription does not exist");
                    return;
                }

                _dataSetWriter = newInfo._dataSetWriter.Clone();
                _subscriptionInfo = newInfo._subscriptionInfo.Clone();

                InitializeKeyframeTrigger(newInfo._dataSetWriter);
                InitializeMetaDataTrigger(newInfo._dataSetWriter);

                await Subscription.ApplyAsync(_subscriptionInfo.MonitoredItems,
                    _subscriptionInfo.Configuration).ConfigureAwait(false);
            }

            /// <summary>
            /// Activate a subscription
            /// </summary>
            public async Task ActivateAsync() {
                if (Subscription == null) {
                    _outer._logger.Warning("Subscription not registered");
                    return;
                }
                // only try to activate if already enabled. Otherwise the activation
                // will be handled by the session's keep alive mechanism
                if (Subscription.Enabled) {
                    await Subscription.ActivateAsync(null).ConfigureAwait(false);
                }

                if (_metadataTimer != null) {
                    _metadataTimer.Start();
                }
            }

            /// <summary>
            /// Deactivate a subscription
            /// </summary>
            public async Task DeactivateAsync() {
                if (Subscription == null) {
                    _outer._logger.Warning("Subscription not registered");
                    return;
                }

                await Subscription.DeactivateAsync(null).ConfigureAwait(false);

                if (_metadataTimer != null) {
                    _metadataTimer.Stop();
                }
            }

            /// <inheritdoc/>
            public async Task CloseAsync() {
                if (Subscription == null) {
                    _outer._logger.Warning("Subscription not registered");
                    return;
                }
                await Subscription.CloseAsync().ConfigureAwait(false);
                Subscription.OnSubscriptionDataChange -= OnSubscriptionDataChangeNotification;
                Subscription.OnSubscriptionEventChange -= OnSubscriptionEventNotification;
                Subscription.OnSubscriptionDataDiagnosticsChange -= OnSubscriptionDataDiagnosticsChanged;
                Subscription.OnSubscriptionEventDiagnosticsChange -= OnSubscriptionEventDiagnosticsChanged;
                Subscription.Dispose();
                Subscription = null;
            }

            /// <inheritdoc/>
            public void Dispose() {
                if (Subscription != null) {
                    DeactivateAsync().GetAwaiter().GetResult();
                    CloseAsync().GetAwaiter().GetResult();
                }
                _metadataTimer?.Dispose();
                Subscription = null;
            }

            /// <summary>
            /// Initializes the key frame triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeKeyframeTrigger(DataSetWriterModel dataSetWriter) {
                _frameCount = 0;
                _keyFrameCount = dataSetWriter.KeyFrameCount ?? 0;
            }

            /// <summary>
            /// /// Initializes the Metadata triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeMetaDataTrigger(DataSetWriterModel dataSetWriter) {

                var metaDataSendInterval = dataSetWriter.DataSetMetaDataSendInterval
                    .GetValueOrDefault(TimeSpan.Zero)
                    .TotalMilliseconds;

                if (metaDataSendInterval > 0) {
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

                _outer._logger.Debug("Insert metadata message...");
                var notification = Subscription.CreateKeepAlive();
                if (notification != null) {
                    // This call udpates the message type, so no need to do it here.
                    CallMessageReceiverDelegates(this, notification, true);
                }
                else {
                    // Failed to send, try again later
                    InitializeMetaDataTrigger(_dataSetWriter);
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
                        _outer._logger.Debug("Notifications counter has been reset to prevent overflow. " +
                            "So far, {DataChangesCount} data changes and {ValueChangesCount} " +
                            "value changes were invoked by message source.",
                            _outer.DataChangesCount, _outer.ValueChangesCount);
                        _outer.DataChangesCount = 0;
                        _outer.ValueChangesCount = 0;
                        _outer.FireOnCounterResetEvent();
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
                        _outer._logger.Debug("Notifications counter has been reset to prevent overflow. " +
                            "So far, {EventChangesCount} event changes and {EventValueChangesCount} " +
                            "event value changes were invoked by message source.",
                            _outer.EventCount, _outer.EventNotificationCount);
                        _outer.EventCount = 0;
                        _outer.EventNotificationCount = 0;
                        _outer.FireOnCounterResetEvent();
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
                                InitializeMetaDataTrigger(_dataSetWriter);
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
                                InitializeKeyframeTrigger(_dataSetWriter);
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

            /// <inheritdoc/>
            public override bool Equals(object obj) {
                if (!(obj is DataSetWriterSubscription that)) {
                    return false;
                }
                return _subscriptionInfo.Connection.IsSameAs(that._subscriptionInfo.Connection) &&
                    _subscriptionInfo.Id == that._subscriptionInfo.Id;
            }

            /// <inheritdoc/>
            public static bool operator ==(DataSetWriterSubscription objA, DataSetWriterSubscription objB) =>
                EqualityComparer<DataSetWriterSubscription>.Default.Equals(objA, objB);

            /// <inheritdoc/>
            public static bool operator !=(DataSetWriterSubscription objA, DataSetWriterSubscription objB) =>
                !(objA == objB);

            /// <inheritdoc/>
            public override int GetHashCode() {
                var hashCode = 2082053542;
                hashCode = (hashCode * -1521134295) +
                    _subscriptionInfo.Connection.CreateConsistentHash();
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(_subscriptionInfo.Id);
                return hashCode;
            }

            private readonly WriterGroupMessageTrigger _outer;
            private readonly object _lock = new object();
            private DataSetWriterModel _dataSetWriter;
            private SubscriptionModel _subscriptionInfo;
            private Timer _metadataTimer;
            private uint _keyFrameCount;
            private volatile uint _frameCount;
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
        private readonly List<DataSetWriterSubscription> _subscriptions;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ulong[] _valueChangesBuffer = new ulong[_bucketWidth];
        private readonly ulong[] _dataChangesBuffer = new ulong[_bucketWidth];

        private WriterGroupModel _writerGroup;
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
