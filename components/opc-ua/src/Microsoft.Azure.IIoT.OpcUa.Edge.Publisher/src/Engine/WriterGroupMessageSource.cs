// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
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
        public string Id => _writerGroup.WriterGroupId;

        /// <inheritdoc/>
        public int NumberOfConnectionRetries => _subscriptions?.FirstOrDefault()?.
            Subscription?.NumberOfConnectionRetries ?? 0;

        /// <inheritdoc/>
        public ulong ValueChangesCountLastMinute {
            get => CalculateSumForRingBuffer(_valueChangesBuffer, ref _lastPointerValueChanges, _bucketWidth, _lastWriteTimeValueChange);
            private set => IncreaseRingBuffer(_valueChangesBuffer, ref _lastPointerValueChanges, _bucketWidth, value, ref _lastWriteTimeValueChange);
        }

        /// <inheritdoc/>
        public ulong ValueChangesCount {
            get { return _valueChangesCount; }
            private set  {
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
            for(int index = 0; index< array.Length; index++) {
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
                .Select(g => new DataSetWriterSubscription(this, g))
                .ToList();
            _publisherId = writerGroupConfig.PublisherId ?? Guid.NewGuid().ToString();
        }

        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken ct) {

            _subscriptions.ForEach(sc => sc.OpenAsync().Wait());
            _subscriptions.ForEach(sc => sc.ActivateAsync(ct).Wait());
            try {
                await Task.Delay(-1, ct).ConfigureAwait(false);
            }
            finally {
                _subscriptions.ForEach(sc => sc.DeactivateAsync().Wait());
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
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
            /// Create subscription from template
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="dataSetWriter"></param>
            public DataSetWriterSubscription(WriterGroupMessageTrigger outer,
                DataSetWriterModel dataSetWriter) {

                _outer = outer ?? throw new ArgumentNullException(nameof(outer));
                _dataSetWriter = dataSetWriter.Clone() ??
                    throw new ArgumentNullException(nameof(dataSetWriter));
                _subscriptionInfo = _dataSetWriter.ToSubscriptionModel();

                if (dataSetWriter.KeyFrameInterval.HasValue &&
                   dataSetWriter.KeyFrameInterval.Value > TimeSpan.Zero) {
                    _keyframeTimer = new Timer(
                        dataSetWriter.KeyFrameInterval.Value.TotalMilliseconds);
                    _keyframeTimer.Elapsed += KeyframeTimerElapsedAsync;
                }
                else {
                    _keyFrameCount = dataSetWriter.KeyFrameCount;
                }

                if (dataSetWriter.DataSetMetaDataSendInterval.HasValue &&
                    dataSetWriter.DataSetMetaDataSendInterval.Value > TimeSpan.Zero) {
                    _metaData = dataSetWriter.DataSet?.DataSetMetaData ??
                        throw new ArgumentNullException(nameof(dataSetWriter.DataSet));

                    _metadataTimer = new Timer(
                        dataSetWriter.DataSetMetaDataSendInterval.Value.TotalMilliseconds);
                    _metadataTimer.Elapsed += MetadataTimerElapsed;
                }
            }

            /// <summary>
            /// Open subscription
            /// </summary>
            /// <returns></returns>
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
            /// activate a subscription
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            public async Task ActivateAsync(CancellationToken ct) {
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
                    ct.Register(() => _keyframeTimer.Stop());
                    _keyframeTimer.Start();
                }

                if (_metadataTimer != null) {
                    ct.Register(() => _metadataTimer.Stop());
                    _metadataTimer.Start();
                }
            }

            /// <summary>
            /// deactivate a subscription
            /// </summary>
            /// <returns></returns>
            public async Task DeactivateAsync() {

                if (Subscription == null) {
                    _outer._logger.Warning("Subscription not registered");
                    return;
                }

                await Subscription.CloseAsync().ConfigureAwait(false);

                if (_keyframeTimer != null) {
                    _keyframeTimer.Stop();
                }

                if (_metadataTimer != null) {
                    _metadataTimer.Stop();
                }
            }


            /// <inheritdoc/>
            public void Dispose() {
                if (Subscription != null) {
                    Subscription.OnSubscriptionChange -= OnSubscriptionChangedAsync;
                    Subscription.Dispose();
                }
                _keyframeTimer?.Dispose();
                _metadataTimer?.Dispose();
                Subscription = null;
            }

            /// <summary>
            /// Fire when keyframe timer elapsed to send keyframe message
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
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
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void MetadataTimerElapsed(object sender, ElapsedEventArgs e) {
                // Send(_metaData)
            }

            /// <summary>
            /// Handle subscription change messages
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="notification"></param>
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
                            _outer._logger.Debug("Notifications counter has been reset to prevent overflow. " +
                                "So far, {DataChangesCount} data changes and {ValueChangesCount}" +
                                " value changes were invoked by message source.",
                                _outer.DataChangesCount, _outer.ValueChangesCount);
                            _outer.DataChangesCount = 0;
                            _outer.ValueChangesCount = 0;
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

            private readonly Timer _keyframeTimer;
            private readonly Timer _metadataTimer;
            private readonly DataSetMetaDataModel _metaData;
            private readonly uint? _keyFrameCount;
            private long _currentSequenceNumber;
            private readonly WriterGroupMessageTrigger _outer;
            private readonly DataSetWriterModel _dataSetWriter;
            private readonly SubscriptionModel _subscriptionInfo;
            private readonly object _lock = new object();
        }

        private readonly ILogger _logger;
        private readonly string _publisherId;
        private readonly List<DataSetWriterSubscription> _subscriptions;
        private readonly WriterGroupModel _writerGroup;
        private readonly ISubscriptionManager _subscriptionManager;
        private const int kNumberOfInvokedMessagesResetThreshold = int.MaxValue - 10000;
        private const int _bucketWidth = 60;
        private readonly ulong[] _valueChangesBuffer = new ulong[_bucketWidth];
        private int _lastPointerValueChanges;
        private ulong _valueChangesCount;
        private readonly ulong[] _dataChangesBuffer = new ulong[_bucketWidth];
        private int _lastPointerDataChanges;
        private ulong _dataChangesCount;
        private DateTime _lastWriteTimeValueChange = DateTime.MinValue;
        private DateTime _lastWriteTimeDataChange = DateTime.MinValue;

    }
}