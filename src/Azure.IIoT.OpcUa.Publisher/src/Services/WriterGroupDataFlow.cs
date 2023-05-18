// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Dataflow engine
    /// </summary>
    public sealed class WriterGroupDataFlow : IWriterGroup
    {
        /// <inheritdoc/>
        public IMessageSource Source { get; }

        /// <summary>
        /// Create engine
        /// </summary>
        /// <param name="source"></param>
        /// <param name="encoder"></param>
        /// <param name="sink"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        /// <param name="diagnostics"></param>
        public WriterGroupDataFlow(IMessageSource source, IMessageEncoder encoder,
            IMessageSink sink, IOptions<PublisherOptions> options,
            ILogger<WriterGroupDataFlow> logger, IMetricsContext metrics,
            IWriterGroupDiagnostics? diagnostics = null)
        {
            _options = options;
            Source = source;
            _messageSink = sink;
            _messageEncoder = encoder;
            _logger = logger;
            _diagnostics = diagnostics;
            _logNotifications = _options.Value.DebugLogNotifications ?? false;

            if (_options.Value.BatchSize > 1)
            {
                _notificationBufferSize = _options.Value.BatchSize.Value;
            }
            if (_options.Value.MaxMessageSize > 0)
            {
                _maxEncodedMessageSize = _options.Value.MaxMessageSize.Value;
            }
            if (_maxEncodedMessageSize <= 0)
            {
                _maxEncodedMessageSize = int.MaxValue;
            }
            if (_maxEncodedMessageSize > _messageSink.MaxMessageSize)
            {
                _maxEncodedMessageSize = _messageSink.MaxMessageSize;
            }

            _batchTriggerInterval = _options.Value.BatchTriggerInterval ?? TimeSpan.Zero;
            _batchTriggerIntervalTimer = new Timer(BatchTriggerIntervalTimer_Elapsed);
            _maxOutgressMessages = _options.Value.MaxEgressMessages ?? 4096; // = 1 GB

            _encodingBlock = new TransformManyBlock<SubscriptionNotificationModel[], IEvent>(
                input =>
                {
                    try
                    {
                        return _messageEncoder.Encode(_messageSink.CreateMessage,
                            input, _maxEncodedMessageSize, _notificationBufferSize != 1);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Encoding failure.");
                        return Enumerable.Empty<IEvent>();
                    }
                },
                new ExecutionDataflowBlockOptions());
            _batchDataSetMessageBlock = new BatchBlock<SubscriptionNotificationModel>(
                _notificationBufferSize,
                new GroupingDataflowBlockOptions());
            _sinkBlock = new ActionBlock<IEvent>(
                input => _messageSink.SendAsync(input),
                new ExecutionDataflowBlockOptions());

            _batchDataSetMessageBlock.LinkTo(_encodingBlock);
            _encodingBlock.LinkTo(_sinkBlock);

            Source.OnMessage += OnMessageReceived;
            Source.OnCounterReset += MessageTriggerCounterResetReceived;

            InitializeMetrics(metrics ?? throw new ArgumentNullException(nameof(metrics)));
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                _batchTriggerIntervalTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                Source.OnCounterReset -= MessageTriggerCounterResetReceived;
                Source.OnMessage -= OnMessageReceived;
                _batchDataSetMessageBlock.Complete();
                await _batchDataSetMessageBlock.Completion.ConfigureAwait(false);
                _encodingBlock.Complete();
                await _encodingBlock.Completion.ConfigureAwait(false);
                _sinkBlock.Complete();
                await _sinkBlock.Completion.ConfigureAwait(false);
                _batchTriggerIntervalTimer?.Dispose();
            }
            finally
            {
                _diagnostics?.Dispose();
                await Source.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _meter.Dispose();

            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Batch trigger interval
        /// </summary>
        /// <param name="state"></param>
        private void BatchTriggerIntervalTimer_Elapsed(object? state)
        {
            if (_batchTriggerInterval > TimeSpan.Zero)
            {
                _batchTriggerIntervalTimer.Change(_batchTriggerInterval, Timeout.InfiniteTimeSpan);
            }
            _batchDataSetMessageBlock?.TriggerBatch();
        }

        /// <summary>
        /// Message received handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnMessageReceived(object? sender, SubscriptionNotificationModel args)
        {
            if (_dataFlowStartTime == DateTime.MinValue)
            {
                if (_batchTriggerInterval > TimeSpan.Zero)
                {
                    _batchTriggerIntervalTimer.Change(_batchTriggerInterval, Timeout.InfiniteTimeSpan);
                }
                _diagnostics?.ResetWriterGroupDiagnostics();
                _dataFlowStartTime = DateTime.UtcNow;
                _logger.LogInformation("Started data flow with message from subscription {Name} on {Endpoint}.",
                    args.SubscriptionName, args.EndpointUrl);
            }

            if (_sinkBlock.InputCount >= _maxOutgressMessages)
            {
                _sinkBlockInputDroppedCount++;

                if (_logNotifications)
                {
                    LogNotification(args, true);
                }
            }
            else
            {
                _batchDataSetMessageBlock.Post(args);

                if (_logNotifications)
                {
                    LogNotification(args);
                }
            }
        }

        /// <summary>
        /// Counter reset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MessageTriggerCounterResetReceived(object? sender, EventArgs e)
        {
            _dataFlowStartTime = DateTime.MinValue;
        }

        /// <summary>
        /// Log notifications for debugging
        /// </summary>
        /// <param name="args"></param>
        /// <param name="dropped"></param>
        private void LogNotification(SubscriptionNotificationModel args, bool dropped = false)
        {
            _logger.LogInformation("{Action}Notification#{Seq} from Subscription {Subscription}{Items}",
                dropped ? "!!!! Dropped " : string.Empty, args.SequenceNumber,
                args.SubscriptionName, Stringify(args.Notifications));
            static string Stringify(IList<MonitoredItemNotificationModel> notifications)
            {
                var sb = new StringBuilder();
                foreach (var item in notifications)
                {
                    sb
                        .AppendLine()
                        .Append("|#")
                        .Append(item.SequenceNumber)
                        .Append('|')
                        .Append(item.DataSetFieldName ?? item.DisplayName)
                        .Append('|')
                        .Append(item.Value?.Value)
                        .Append('|')
                        .Append(item.Value?.StatusCode)
                        .Append('|')
                        ;
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        private void InitializeMetrics(IMetricsContext metrics)
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_iothub_queue_dropped_count",
                () => new Measurement<long>(_sinkBlockInputDroppedCount, metrics.TagList), "Messages",
                "Telemetry messages dropped due to overflow.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_iothub_queue_size",
                () => new Measurement<long>(_sinkBlock.InputCount, metrics.TagList), "Messages",
                "Telemetry messages queued for sending upstream.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_batch_input_queue_size",
                () => new Measurement<long>(_batchDataSetMessageBlock.OutputCount, metrics.TagList), "Notifications",
                "Telemetry messages queued for sending upstream.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_encoding_input_queue_size",
                () => new Measurement<long>(_encodingBlock.InputCount, metrics.TagList), "Notifications",
                "Telemetry messages queued for sending upstream.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_encoding_output_queue_size",
                () => new Measurement<long>(_encodingBlock.OutputCount, metrics.TagList), "Messages",
                "Telemetry messages queued for sending upstream.");
        }

        private long _sinkBlockInputDroppedCount;
        private DateTime _dataFlowStartTime = DateTime.MinValue;
        private readonly int _notificationBufferSize;
        private readonly int _maxEncodedMessageSize;
        private readonly int _maxOutgressMessages;
        private readonly Timer _batchTriggerIntervalTimer;
        private readonly TimeSpan _batchTriggerInterval;
        private readonly IOptions<PublisherOptions> _options;
        private readonly IMessageSink _messageSink;
        private readonly IMessageEncoder _messageEncoder;
        private readonly ILogger _logger;
        private readonly IWriterGroupDiagnostics? _diagnostics;
        private readonly bool _logNotifications;
        private readonly BatchBlock<SubscriptionNotificationModel> _batchDataSetMessageBlock;
        private readonly TransformManyBlock<SubscriptionNotificationModel[], IEvent> _encodingBlock;
        private readonly ActionBlock<IEvent> _sinkBlock;
        private readonly Meter _meter = Diagnostics.NewMeter();
    }
}
