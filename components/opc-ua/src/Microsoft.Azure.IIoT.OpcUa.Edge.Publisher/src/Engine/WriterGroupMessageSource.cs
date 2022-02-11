﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Collections.Generic;
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
        public string Id => _subscriptions?.First()?.Subscription?.Connection?.CreateConnectionId() ?? _writerGroup.WriterGroupId;

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
        public event EventHandler<DataSetMessageModel> OnMessage;

        /// <inheritdoc/>
        public event EventHandler<EventArgs> OnCounterReset;

        private void FireOnCounterResetEvent() {
            OnCounterReset?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Create trigger from writer group
        /// </summary>
        public WriterGroupMessageTrigger(IWriterGroupConfig writerGroupConfig,
            ISubscriptionManager subscriptionManager, ILogger logger) {

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

            toAdd.ForEach(async sc => await sc.OpenAsync().ConfigureAwait(false) );
            toUpdate.ForEach(async sc => {
                    var newSc = newSubscriptions.Find(s => s.Equals(sc));
                    await sc.UpdateAsync(newSc);
                }
            );
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
                sc.OnSubscriptionChange += OnSubscriptionChangedAsync;
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

                if (_keyframeTimer != null) {
                    _keyframeTimer.Start();
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

                if (_keyframeTimer != null) {
                    _keyframeTimer.Stop();
                }

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
                Subscription.OnSubscriptionChange -= OnSubscriptionChangedAsync;
                Subscription.Dispose();
                Subscription = null;

            }

            /// <inheritdoc/>
            public void Dispose() {
                if (Subscription != null) {
                    DeactivateAsync().GetAwaiter().GetResult();
                    CloseAsync().GetAwaiter().GetResult();
                }
                _keyframeTimer?.Dispose();
                _metadataTimer?.Dispose();
                Subscription = null;
            }

            /// <summary>
            /// Initializes the key frame triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeKeyframeTrigger(DataSetWriterModel dataSetWriter) {

                var keyframeTriggerInterval = dataSetWriter.KeyFrameInterval
                    .GetValueOrDefault(TimeSpan.Zero)
                    .TotalMilliseconds;

                if (keyframeTriggerInterval > 0) {
                    if (_keyframeTimer == null) {
                        _keyframeTimer = new Timer(keyframeTriggerInterval);
                        _keyframeTimer.Elapsed += KeyframeTimerElapsedAsync;
                    }
                    else {
                        _keyframeTimer.Interval = keyframeTriggerInterval;
                    }
                }
                else {
                    if (_keyframeTimer != null) {
                        _keyframeTimer.Stop();
                        _keyframeTimer.Dispose();
                        _keyframeTimer = null;
                    }
                    _keyFrameCount = dataSetWriter.KeyFrameCount;
                }
            }

            /// <summary>
            /// /// Initializes the Metadata triggering mechanism from the cconfiguration model
            /// </summary>
            private void InitializeMetaDataTrigger(DataSetWriterModel dataSetWriter) {

                var metaDataSendInterval = dataSetWriter.DataSetMetaDataSendInterval
                    .GetValueOrDefault(TimeSpan.Zero)
                    .TotalMilliseconds;

                if (metaDataSendInterval > 0) {
                    _metaData = dataSetWriter.DataSet?.DataSetMetaData ??
                        throw new ArgumentNullException(nameof(dataSetWriter.DataSet));
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
            /// Fire when keyframe timer elapsed to send keyframe message
            /// </summary>
            private async void KeyframeTimerElapsedAsync(object sender, ElapsedEventArgs e) {
                try {
                    _keyframeTimer.Enabled = false;

                    _outer._logger.Debug("Insert keyframe message...");
                    var sequenceNumber = (uint)Interlocked.Increment(ref _currentSequenceNumber);
                    var snapshot = await Subscription.GetSnapshotAsync().ConfigureAwait(false);
                    if (snapshot != null) {
                        CallMessageReceiverDelegates(this, sequenceNumber, snapshot);
                    }
                }
                catch (Exception ex) {
                    _outer._logger.Information(ex, "Failed to send keyframe.");
                }
                finally {
                    _keyframeTimer.Enabled = true;
                }
            }

            /// <summary>
            /// Fired when metadata time elapsed
            /// </summary>
            private void MetadataTimerElapsed(object sender, ElapsedEventArgs e) {
                // Send(_metaData)
            }

            /// <summary>
            /// Handle subscription change messages
            /// </summary>
            private async void OnSubscriptionChangedAsync(object sender,
                SubscriptionNotificationModel notification) {
                var sequenceNumber = (uint)Interlocked.Increment(ref _currentSequenceNumber);
                if (_keyFrameCount.HasValue && _keyFrameCount.Value != 0 &&
                    (sequenceNumber % _keyFrameCount.Value) == 0) {
                    var snapshot = await Try.Async(() => Subscription.GetSnapshotAsync()).ConfigureAwait(false);
                    if (snapshot != null) {
                        notification = snapshot;
                    }
                }
                CallMessageReceiverDelegates(sender, sequenceNumber, notification);
            }

            /// <summary>
            /// handle subscription change messages
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="sequenceNumber"></param>
            /// <param name="notification"></param>
            private void CallMessageReceiverDelegates(object sender, uint sequenceNumber,
                SubscriptionNotificationModel notification) {
                try {
                    var message = new DataSetMessageModel {
                        // TODO: Filter changes on the monitored items contained in the template
                        Notifications = notification.Notifications.ToList(),
                        ServiceMessageContext = notification.ServiceMessageContext,
                        SubscriptionId = notification.SubscriptionId,
                        SequenceNumber = sequenceNumber,
                        ApplicationUri = notification.ApplicationUri,
                        EndpointUrl = notification.EndpointUrl,
                        TimeStamp = notification.Timestamp,
                        PublisherId = _outer._publisherId,
                        Writer = _dataSetWriter,
                        WriterGroup = _outer._writerGroup
                    };
                    lock (_lock) {
                        if (_outer.DataChangesCount >= kNumberOfInvokedMessagesResetThreshold ||
                            _outer.ValueChangesCount >= kNumberOfInvokedMessagesResetThreshold) {
                            // reset both
                            _outer._logger.Information("Notifications counter has been reset to prevent overflow. " +
                                "So far, {DataChangesCount} data changes and {ValueChangesCount}" +
                                " value changes were invoked by message source.",
                                _outer.DataChangesCount, _outer.ValueChangesCount);
                            _outer.DataChangesCount = 0;
                            _outer.ValueChangesCount = 0;
                            _outer.FireOnCounterResetEvent();
                        }

                        _outer.ValueChangesCount += (ulong)message.Notifications.Count();
                        _outer.DataChangesCount++;
                        _outer.OnMessage?.Invoke(sender, message);
                    }
                }
                catch (Exception ex) {
                    _outer._logger.Debug(ex, "Failed to produce message");
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
            public static bool operator == (DataSetWriterSubscription objA, DataSetWriterSubscription objB) =>
                EqualityComparer<DataSetWriterSubscription>.Default.Equals(objA, objB);

            /// <inheritdoc/>
            public static bool operator != (DataSetWriterSubscription objA, DataSetWriterSubscription objB) =>
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
            private Timer _keyframeTimer;
            private Timer _metadataTimer;
            private DataSetMetaDataModel _metaData;
            private uint? _keyFrameCount;
            private long _currentSequenceNumber;
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
    }
}