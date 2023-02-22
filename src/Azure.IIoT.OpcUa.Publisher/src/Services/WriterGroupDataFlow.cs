// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services {
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Dataflow engine
    /// </summary>
    public class WriterGroupDataFlow : IWriterGroup {

        /// <inheritdoc/>
        public IMessageSource Source => _source;

        /// <summary>
        /// Create engine
        /// </summary>
        public WriterGroupDataFlow(IMessageSource source, IMessageEncoder encoder,
            IMessageSink sink, IEngineConfiguration config, ILogger logger,
            IMetricsContext metrics, IWriterGroupDiagnostics diagnostics = null)
            : this(metrics ?? throw new ArgumentNullException(nameof(metrics))) {

            _config = config;
            _source = source;
            _messageSink = sink;
            _messageEncoder = encoder;
            _logger = logger;
            _diagnostics = diagnostics;

            if (_config.BatchSize.HasValue && _config.BatchSize.Value > 1) {
                _notificationBufferSize = _config.BatchSize.Value;
            }
            if (_config.MaxMessageSize.HasValue && _config.MaxMessageSize.Value > 0) {
                _maxEncodedMessageSize = _config.MaxMessageSize.Value;
            }
            if (_maxEncodedMessageSize <= 0) {
                _maxEncodedMessageSize = int.MaxValue;
            }
            if (_maxEncodedMessageSize > _messageSink.MaxMessageSize) {
                _maxEncodedMessageSize = _messageSink.MaxMessageSize;
            }

            _batchTriggerInterval = _config.BatchTriggerInterval.GetValueOrDefault(TimeSpan.Zero);
            _batchTriggerIntervalTimer = new Timer(BatchTriggerIntervalTimer_Elapsed);
            _maxOutgressMessages = _config.MaxOutgressMessages.GetValueOrDefault(4096); // = 1 GB

            _encodingBlock = new TransformManyBlock<SubscriptionNotificationModel[], ITelemetryEvent>(
                input => {
                    try {
                        return _messageEncoder.Encode(_messageSink.CreateMessage,
                            input, _maxEncodedMessageSize, _notificationBufferSize != 1);
                    }
                    catch (Exception e) {
                        _logger.LogError(e, "Encoding failure.");
                        return Enumerable.Empty<ITelemetryEvent>();
                    }
                },
                new ExecutionDataflowBlockOptions());
            _batchDataSetMessageBlock = new BatchBlock<SubscriptionNotificationModel>(
                _notificationBufferSize,
                new GroupingDataflowBlockOptions());
            _sinkBlock = new ActionBlock<ITelemetryEvent>(
                input => _messageSink.SendAsync(input),
                new ExecutionDataflowBlockOptions());

            _batchDataSetMessageBlock.LinkTo(_encodingBlock);
            _encodingBlock.LinkTo(_sinkBlock);

            _source.OnMessage += OnMessageReceived;
            _source.OnCounterReset += MessageTriggerCounterResetReceived;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            try {
                _batchTriggerIntervalTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _source.OnCounterReset -= MessageTriggerCounterResetReceived;
                _source.OnMessage -= OnMessageReceived;
                _batchDataSetMessageBlock.Complete();
                await _batchDataSetMessageBlock.Completion;
                _encodingBlock.Complete();
                await _encodingBlock.Completion;
                _sinkBlock.Complete();
                await _sinkBlock.Completion;
                _batchTriggerIntervalTimer?.Dispose();
            }
            finally {
                _diagnostics?.Dispose();
                await _source.DisposeAsync();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Batch trigger interval
        /// </summary>
        private void BatchTriggerIntervalTimer_Elapsed(object state) {
            if (_batchTriggerInterval > TimeSpan.Zero) {
                _batchTriggerIntervalTimer.Change(_batchTriggerInterval, Timeout.InfiniteTimeSpan);
            }
            _batchDataSetMessageBlock?.TriggerBatch();
        }

        /// <summary>
        /// Message received handler
        /// </summary>
        private void OnMessageReceived(object sender, SubscriptionNotificationModel args) {
            _logger.LogDebug("Message source received message with sequenceNumber {SequenceNumber}",
                args.SequenceNumber);

            if (_dataFlowStartTime == DateTime.MinValue) {
                if (_batchTriggerInterval > TimeSpan.Zero) {
                    _batchTriggerIntervalTimer.Change(_batchTriggerInterval, Timeout.InfiniteTimeSpan);
                }
                _diagnostics?.ResetWriterGroupDiagnostics();
                _dataFlowStartTime = DateTime.UtcNow;
                _logger.LogInformation("Started data flow with message from subscription {Name} on {Endpoint}.",
                    args.SubscriptionName, args.EndpointUrl);
            }

            if (_sinkBlock.InputCount >= _maxOutgressMessages) {
                _sinkBlockInputDroppedCount++;
            }
            else {
                _batchDataSetMessageBlock.Post(args);
            }
        }

        private void MessageTriggerCounterResetReceived(object sender, EventArgs e) {
            _dataFlowStartTime = DateTime.MinValue;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        private WriterGroupDataFlow(IMetricsContext metrics) {
            Diagnostics.Meter.CreateObservableCounter("iiot_edge_publisher_iothub_queue_dropped_count",
                () => new Measurement<long>(_sinkBlockInputDroppedCount, metrics.TagList), "Messages",
                "Telemetry messages dropped due to overflow.");
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_iothub_queue_size",
                () => new Measurement<long>(_sinkBlock.InputCount, metrics.TagList), "Messages",
                "Telemetry messages queued for sending upstream.");
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_batch_input_queue_size",
                () => new Measurement<long>(_batchDataSetMessageBlock.OutputCount, metrics.TagList), "Notifications",
                "Telemetry messages queued for sending upstream.");
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_encoding_input_queue_size",
                () => new Measurement<long>(_encodingBlock.InputCount, metrics.TagList), "Notifications",
                "Telemetry messages queued for sending upstream.");
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_encoding_output_queue_size",
                () => new Measurement<long>(_encodingBlock.InputCount, metrics.TagList), "Messages",
                "Telemetry messages queued for sending upstream.");
        }

        private long _sinkBlockInputDroppedCount;
        private DateTime _dataFlowStartTime = DateTime.MinValue;
        private readonly int _notificationBufferSize = 1;
        private readonly int _maxEncodedMessageSize;
        private readonly int _maxOutgressMessages;
        private readonly Timer _batchTriggerIntervalTimer;
        private readonly TimeSpan _batchTriggerInterval;
        private readonly IEngineConfiguration _config;
        private readonly IMessageSink _messageSink;
        private readonly IMessageEncoder _messageEncoder;
        private readonly IMessageSource _source;
        private readonly ILogger _logger;
        private readonly IWriterGroupDiagnostics _diagnostics;
        private readonly BatchBlock<SubscriptionNotificationModel> _batchDataSetMessageBlock;
        private readonly TransformManyBlock<SubscriptionNotificationModel[], ITelemetryEvent> _encodingBlock;
        private readonly ActionBlock<ITelemetryEvent> _sinkBlock;
    }
}
