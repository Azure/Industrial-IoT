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
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Messaging.Clients;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Network message sink connected to the source. The sink consists of
    /// publish queue which is a dataflow engine to handle batching and
    /// encoding and other egress concerns.  The queues can be partitioned
    /// to handle multiple topics.
    /// </summary>
    public sealed class NetworkMessageSink : IWriterGroup
    {
        /// <inheritdoc/>
        public IMessageSource Source { get; }

        /// <summary>
        /// Create writer group network message sink
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="eventClients"></param>
        /// <param name="source"></param>
        /// <param name="encoder"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        /// <param name="diagnostics"></param>
        /// <param name="timeProvider"></param>
        public NetworkMessageSink(WriterGroupModel writerGroup,
            IEnumerable<IEventClient> eventClients, IMessageSource source,
            IMessageEncoder encoder, IOptions<PublisherOptions> options,
            ILogger<NetworkMessageSink> logger, IMetricsContext metrics,
            IWriterGroupDiagnostics? diagnostics = null, TimeProvider? timeProvider = null)
        {
            Source = source;

            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _messageEncoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _diagnostics = diagnostics;
            _timeProvider = timeProvider ?? TimeProvider.System;

            // Reverse the registration to have highest prio first.
            _eventClients = eventClients?.Reverse().ToList()
                ?? throw new ArgumentNullException(nameof(eventClients));
            if (_eventClients.Count == 0)
            {
                throw new ArgumentException("No transports registered.",
                    nameof(eventClients));
            }

            _logNotificationsFilter = _options.Value.DebugLogNotificationsFilter == null ?
                null : new Regex(_options.Value.DebugLogNotificationsFilter);
            _filterNotifications = _options.Value.DebugLogNotificationsWithHeartbeat == true
                ? Filter : FilterHeartbeat;
            _logNotifications = _options.Value.DebugLogNotifications
                ?? (_logNotificationsFilter != null);
            _queue = new NullPublishQueue();

            _transport = new TransportOptions(writerGroup, _eventClients, _options);

            _queue = _transport.MaxPublishQueuePartitions != 0
                ? new PublishQueue(this, _transport.MaxPublishQueuePartitions)
                : new PublishQueuePartition(this, 0, _logger);

            InitializeMetrics();

            Source.OnMessage += OnMessageReceived;
            Source.OnCounterReset += OnReset;
            _startTime = _timeProvider.GetTimestamp();
            _transport.Log(writerGroup, _logger);
        }

        /// <inheritdoc/>
        private void OnMessageReceived(object? sender, IOpcUaSubscriptionNotification args)
        {
            if (_dataFlowStartTime == 0)
            {
                _diagnostics?.ResetWriterGroupDiagnostics();
                _dataFlowStartTime = _timeProvider.GetTimestamp();
            }

            if (!_queue.TryPublish(args))
            {
                args.Dispose();
            }
        }

        /// <inheritdoc/>
        private void OnReset(object? sender, EventArgs e)
        {
            _dataFlowStartTime = 0;
            _queue.Reset();
        }

        /// <inheritdoc/>
        public async ValueTask UpdateAsync(WriterGroupModel writerGroup)
        {
            // TODO: Call update on sink
            var options = new TransportOptions(writerGroup, _eventClients, _options);
            if (options == _transport)
            {
                // Group change does not effect transport settings
                return;
            }

            _transport = options;
            // Todo: only check queue update

            await _queue.DisposeAsync().ConfigureAwait(false);
            _queue = options.MaxPublishQueuePartitions != 0
                ? new PublishQueue(this, options.MaxPublishQueuePartitions)
                : new PublishQueuePartition(this, 0, _logger);

            _transport.Log(writerGroup, _logger);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            try
            {
                Source.OnCounterReset -= OnReset;
                Source.OnMessage -= OnMessageReceived;

                await _queue.DisposeAsync().ConfigureAwait(false);

                _queue = new NullPublishQueue();
                _transport = new TransportOptions();
            }
            finally
            {
                _diagnostics?.Dispose();
                Source.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _meter.Dispose();
            }
            finally
            {
                DisposeAsync().AsTask().GetAwaiter().GetResult();
                _cts.Dispose();
            }
        }

        /// <summary>
        /// Publishing queue interface
        /// </summary>
        private interface IPublishQueue : IAsyncDisposable
        {
            int BufferOutput { get; }
            int EncodingInput { get; }
            int EncodingOutput { get; }
            int SendInput { get; }
            int PartitionCount { get; }
            int ActiveCount { get; }

            /// <summary>
            /// Reset the queue
            /// </summary>
            void Reset();

            /// <summary>
            /// Publish to the queue
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            bool TryPublish(IOpcUaSubscriptionNotification args);
        }

        /// <summary>
        /// Dummy publish queue
        /// </summary>
        private sealed class NullPublishQueue : IPublishQueue
        {
            /// <inheritdoc/>
            public int BufferOutput { get; }
            /// <inheritdoc/>
            public int EncodingInput { get; }
            /// <inheritdoc/>
            public int EncodingOutput { get; }
            /// <inheritdoc/>
            public int SendInput { get; }
            /// <inheritdoc/>
            public int PartitionCount { get; }
            /// <inheritdoc/>
            public int ActiveCount { get; }

            /// <inheritdoc/>
            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            public void Reset()
            {
            }

            /// <inheritdoc/>
            public bool TryPublish(IOpcUaSubscriptionNotification args)
            {
                return false;
            }
        }

        /// <summary>
        /// Partitioned publish queue
        /// </summary>
        private sealed class PublishQueue : IPublishQueue
        {
            /// <inheritdoc/>
            public int EncodingInput => _partitions.Sum(p => p.EncodingInput);
            /// <inheritdoc/>
            public int EncodingOutput => _partitions.Sum(p => p.EncodingOutput);
            /// <inheritdoc/>
            public int SendInput => _partitions.Sum(p => p.SendInput);
            /// <inheritdoc/>
            public int BufferOutput => _partitions.Sum(p => p.BufferOutput);
            /// <inheritdoc/>
            public int PartitionCount => _partitions.Length;
            /// <inheritdoc/>
            public int ActiveCount => _partitions.Sum(p => p.ActiveCount);

            /// <summary>
            /// Create publish queue partition
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="maxPartitions"></param>
            public PublishQueue(NetworkMessageSink outer, int maxPartitions)
            {
                maxPartitions = Math.Min(32, Math.Max(1, maxPartitions));
                _partitions = Enumerable
                    .Range(0, maxPartitions)
                    .Select(índex => new PublishQueuePartition(outer, índex, outer._logger))
                    .ToArray();
            }

            /// <inheritdoc/>
            public bool TryPublish(IOpcUaSubscriptionNotification args)
            {
                var hash = (args.Context as WriterGroupContext)?
                    .Topic?.GetHashCode(StringComparison.Ordinal) ?? 0;
                return _partitions[(uint)hash % _partitions.Length].TryPublish(args);
            }

            /// <inheritdoc/>
            public void Reset()
            {
                _partitions.ForEach(p => p.Reset());
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                foreach (var partition in _partitions)
                {
                    await partition.DisposeAsync().ConfigureAwait(false);
                }
            }
            private readonly PublishQueuePartition[] _partitions;
        }

        /// <summary>
        /// Partitioned publish queue
        /// </summary>
        private sealed class PublishQueuePartition : IPublishQueue
        {
            /// <inheritdoc/>
            public int EncodingInput => _encodingBlock.InputCount;
            /// <inheritdoc/>
            public int EncodingOutput => _encodingBlock.OutputCount;
            /// <inheritdoc/>
            public int SendInput => _sendBlock.InputCount;
            /// <inheritdoc/>
            public int BufferOutput => _notificationBufferBlock.OutputCount;
            /// <inheritdoc/>
            public int PartitionCount => 1;
            /// <inheritdoc/>
            public int ActiveCount => _started ? 1 : 0;

            /// <summary>
            /// Create publish queue partition
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="índex"></param>
            /// <param name="logger"></param>
            public PublishQueuePartition(NetworkMessageSink outer, int índex, ILogger logger)
            {
                _outer = outer;
                _índex = índex;
                _logger = logger;

                var maxBufferBlockCap = (int)Math.BigMul(_outer._transport.MaxPublishQueueSize,
                    _outer._transport.MaxNotificationsPerMessage);
                if (maxBufferBlockCap == 0)
                {
                    maxBufferBlockCap = DataflowBlockOptions.Unbounded;
                }
                var maxEncodingBlockCap = (int)Math.BigMul(_outer._transport.MaxPublishQueueSize,
                    _outer._options.Value.MaxNodesPerDataSet);
                if (maxEncodingBlockCap == 0)
                {
                    maxEncodingBlockCap = DataflowBlockOptions.Unbounded;
                }
                _batchTriggerIntervalTimer = _outer._timeProvider.CreateTimer(
                    BatchTriggerIntervalTimer_Elapsed, null,
                    Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                _notificationBufferBlock = new BatchBlock<IOpcUaSubscriptionNotification>(
                    Math.Max(1, _outer._transport.MaxNotificationsPerMessage), new GroupingDataflowBlockOptions
                    {
                        // BoundedCapacity = maxBufferBlockCap
                    });
                _encodingBlock =
                    new TransformManyBlock<IOpcUaSubscriptionNotification[], (IEvent, Action)>(
                        EncodeSubscriptionNotifications, new ExecutionDataflowBlockOptions
                        {
                            // BoundedCapacity = maxEncodingBlockCap,
                            MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
                        });
                _sendBlock = new ActionBlock<(IEvent, Action)>(
                    SendAsync, new ExecutionDataflowBlockOptions
                    {
                        BoundedCapacity = _outer._transport.MaxPublishQueueSize + 1,
                        MaxDegreeOfParallelism = 1,
                        EnsureOrdered = true
                    });

                _notificationBufferBlock.LinkTo(_encodingBlock);
                _encodingBlock.LinkTo(_sendBlock);
            }

            /// <inheritdoc/>
            public void Reset()
            {
                _started = false;
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    await _cts.CancelAsync().ConfigureAwait(false);
                    _batchTriggerIntervalTimer.Change(
                        Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    _batchTriggerIntervalTimer.Dispose();

                    //
                    // Do not change this it must be in the order of the data flow,
                    // complete and wait data to flow out to the next block which
                    // is then completed. If blocks are completed downstream first
                    // previous blocks will hang.
                    //
                    _notificationBufferBlock.Complete();
                    await _notificationBufferBlock.Completion.ConfigureAwait(false);
                    _encodingBlock.Complete();
                    await _encodingBlock.Completion.ConfigureAwait(false);
                    _sendBlock.Complete();
                    await _sendBlock.Completion.ConfigureAwait(false);
                }
                finally
                {
                    _cts.Dispose();
                }
            }

            /// <inheritdoc/>
            public bool TryPublish(IOpcUaSubscriptionNotification args)
            {
                if (!_started)
                {
                    _started = true;
                    if (_outer._transport.BatchTriggerInterval > TimeSpan.Zero)
                    {
                        _batchTriggerIntervalTimer.Change(_outer._transport.BatchTriggerInterval,
                            Timeout.InfiniteTimeSpan);
                    }
                    _logger.LogInformation(
                        "Partition #{Partition}: Started data flow from subscription {Name} on {Endpoint}.",
                        _índex, args.SubscriptionName, args.EndpointUrl);
                }

                if (_sendBlock.InputCount >= _outer._transport.MaxPublishQueueSize)
                {
                    Interlocked.Increment(ref _outer._sendBlockInputDroppedCount);

                    if (_outer._logNotifications)
                    {
                        _outer.LogNotification(args, true);
                    }
                    return false;
                }

                if (_outer._logNotifications)
                {
                    _outer.LogNotification(args);
                }

                Interlocked.Increment(ref _outer._notificationBufferInputCount);
                if (!_notificationBufferBlock.Post(args))
                {
                    Interlocked.Increment(ref _outer._dataflowInputDroppedCount);
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Send message
            /// </summary>
            /// <param name="message"></param>
            /// <returns></returns>
            private async Task SendAsync((IEvent Event, Action Complete) message)
            {
                if (_cts.IsCancellationRequested)
                {
                    message.Complete();
                    message.Event.Dispose();
                    return;
                }
                try
                {
                    // Do not give up and try to send the message until cancelled.
                    var sw = Stopwatch.StartNew();
                    for (var attempt = 1; !_cts.IsCancellationRequested; attempt++)
                    {
                        try
                        {
                            // Throws if cancelled
                            await message.Event.SendAsync(_cts.Token).ConfigureAwait(false);
                            _outer._logger.LogDebug("#{Attempt}: Network message sent.", attempt);
                            break;
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception e) when (e is not ObjectDisposedException)
                        {
                            _outer._errorCount++;

                            // Fail fast for authentication exceptions
                            var aux = e as AuthenticationException;
                            if (aux == null && e is AggregateException ag)
                            {
                                aux = ag
                                    .Flatten().InnerExceptions
                                    .OfType<AuthenticationException>()
                                    .FirstOrDefault();
                            }
                            if (aux?.Message.Equals("TLS authentication error.",
                                StringComparison.Ordinal) == true)
                            {
                                _logger.LogCritical(aux,
                                    "#{Attempt}: Wrong TLS certificate trust list " +
                                    "provisioned - trying to reset and reload configuration...",
                                    attempt);
                                Runtime.FailFast(aux.Message, aux);
                            }

                            var delay = TimeSpan.FromMilliseconds(attempt * 100);
                            const string error = "#{Attempt}: Error '{Error}' during " +
                                "sending network message. Retrying in {Delay}...";
                            if (_logger.IsEnabled(LogLevel.Debug))
                            {
                                _logger.LogDebug(e, error, attempt, e.Message, delay);
                            }
                            else if (attempt % 10 == 0)
                            {
                                _logger.LogError(e, error, attempt, e.Message, delay);
                            }
                            else
                            {
                                _logger.LogError(error, attempt, e.Message, delay);
                            }

                            // Throws if cancelled
                            await Task.Delay(delay, _cts.Token).ConfigureAwait(false);
                        }
                    }

                    // Message successfully published.
                    Interlocked.Increment(ref _outer._messagesSentCount);
                    kSendingDuration.Record(sw.ElapsedMilliseconds, _outer._metrics.TagList);
                    message.Complete();
                    message.Event.Dispose();
                    return;
                }
                catch (ObjectDisposedException) { }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error sending network message.");
                }
            }

            /// <summary>
            /// Encode notifications
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            private IEnumerable<(IEvent, Action)> EncodeSubscriptionNotifications(
                IOpcUaSubscriptionNotification[] input)
            {
                try
                {
                    Interlocked.Add(ref _outer._notificationBufferInputCount, -input.Length);
                    return _outer._messageEncoder.Encode(
                        _outer._transport.EventClient.CreateEvent, input,
                        _outer._transport.MaxNetworkMessageSize,
                        _outer._transport.MaxNotificationsPerMessage != 1);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Encoding failure on partition #{Partition}.", _índex);
                    input.ForEach(a => a.Dispose());
                    return Enumerable.Empty<(IEvent, Action)>();
                }
            }

            /// <summary>
            /// Batch trigger interval
            /// </summary>
            /// <param name="state"></param>
            private void BatchTriggerIntervalTimer_Elapsed(object? state)
            {
                if (_outer._transport.BatchTriggerInterval > TimeSpan.Zero)
                {
                    _batchTriggerIntervalTimer.Change(_outer._transport.BatchTriggerInterval,
                        Timeout.InfiniteTimeSpan);
                }
                _logger.LogTrace("Trigger notification batch (Interval:{Interval})...",
                    _outer._transport.BatchTriggerInterval);
                _notificationBufferBlock.TriggerBatch();
            }

            private bool _started;
            private readonly ILogger _logger;
            private readonly NetworkMessageSink _outer;
            private readonly int _índex;
            private readonly ITimer _batchTriggerIntervalTimer;
            private readonly BatchBlock<IOpcUaSubscriptionNotification> _notificationBufferBlock;
            private readonly TransformManyBlock<IOpcUaSubscriptionNotification[], (IEvent, Action)> _encodingBlock;
            private readonly ActionBlock<(IEvent, Action)> _sendBlock;
            private readonly CancellationTokenSource _cts = new();
        }

        private static IEnumerable<MonitoredItemNotificationModel> FilterHeartbeat(
            IList<MonitoredItemNotificationModel> notifications)
        {
            // Filter heartbeats and model changes
            return notifications
                .Where(n => (n.Flags &
                    (MonitoredItemSourceFlags.Heartbeat | MonitoredItemSourceFlags.ModelChanges)) == 0);
        }

        private static IEnumerable<MonitoredItemNotificationModel> Filter(
            IList<MonitoredItemNotificationModel> notifications)
        {
            // Filter model changes
            return notifications
                .Where(n => (n.Flags & MonitoredItemSourceFlags.ModelChanges) == 0);
        }

        /// <summary>
        /// Log notifications for debugging
        /// </summary>
        /// <param name="args"></param>
        /// <param name="dropped"></param>
        private void LogNotification(IOpcUaSubscriptionNotification args, bool dropped = false)
        {
            // Filter fields to log
            if (_logNotificationsFilter != null)
            {
                var matched = args.SubscriptionName != null &&
                    _logNotificationsFilter.IsMatch(args.SubscriptionName);

                for (var i = 0; i < args.Notifications.Count && !matched; i++)
                {
                    var itemName =
                        args.Notifications[i].DataSetFieldName ??
                        args.Notifications[i].DataSetName;
                    if (itemName != null)
                    {
                        matched = _logNotificationsFilter.IsMatch(itemName);
                    }
                }
                if (!matched)
                {
                    // Do not log anything
                    return;
                }
            }

            var notifications = Stringify(_filterNotifications(args.Notifications));
            if (!string.IsNullOrEmpty(notifications))
            {
                _logger.LogInformation(
                    "{Action}|{PublishTime:hh:mm:ss:ffffff}|#{Seq}:{PublishSeq}|{MessageType}|{Subscription}|{Items}",
                    dropped ? "!!!! Dropped !!!! " : string.Empty, args.PublishTimestamp, args.SequenceNumber,
                    args.PublishSequenceNumber?.ToString(CultureInfo.CurrentCulture) ?? "-", args.MessageType,
                    args.SubscriptionName, notifications);
            }

            static string Stringify(IEnumerable<MonitoredItemNotificationModel> notifications)
            {
                var sb = new StringBuilder();
                foreach (var item in notifications)
                {
                    sb
                        .AppendLine()
                        .Append("   |")
                        .Append(item.Value?.ServerTimestamp
                            .ToString("hh:mm:ss:ffffff", CultureInfo.CurrentCulture))
                        .Append('|')
                        .Append(item.DataSetFieldName ?? item.DataSetName)
                        .Append('|')
                        .Append(item.Value?.SourceTimestamp
                            .ToString("hh:mm:ss:ffffff", CultureInfo.CurrentCulture))
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
        /// Transport configuration
        /// </summary>
        internal record class TransportOptions
        {
            /// <summary>
            /// Event client selected
            /// </summary>
            public IEventClient EventClient { get; }

            /// <summary>
            /// Notifications per message
            /// </summary>
            public int MaxNotificationsPerMessage { get; }

            /// <summary>
            /// Max network messages
            /// </summary>
            public int MaxNetworkMessageSize { get; }

            /// <summary>
            /// Max publish queue size
            /// </summary>
            public int MaxPublishQueueSize { get; }

            /// <summary>
            /// Max batch trigger interval
            /// </summary>
            public TimeSpan BatchTriggerInterval { get; }

            /// <summary>
            /// Iot edge configured
            /// </summary>
            public bool IsIoTEdge
                => EventClient.Name.Equals(nameof(WriterGroupTransport.IoTHub),
                        StringComparison.OrdinalIgnoreCase);

            /// <summary>
            /// Max publish queue partitions
            /// </summary>
            public int MaxPublishQueuePartitions { get; }

            /// <summary>
            /// Create null options
            /// </summary>
            public TransportOptions()
            {
                EventClient = new NullEventClient();
            }

            /// <summary>
            /// Create options
            /// </summary>
            /// <param name="writerGroup"></param>
            /// <param name="eventClients"></param>
            /// <param name="options"></param>
            public TransportOptions(WriterGroupModel writerGroup,
                List<IEventClient> eventClients, IOptions<PublisherOptions> options)
            {
                EventClient = eventClients
                        .Find(e => e.Name.Equals(writerGroup.Transport?.ToString(),
                            StringComparison.OrdinalIgnoreCase))
                    ?? eventClients
                        .Find(e => e.Name.Equals(options.Value.DefaultTransport?.ToString(),
                            StringComparison.OrdinalIgnoreCase))
                    ?? eventClients[0];

                MaxNotificationsPerMessage = (int?)writerGroup.NotificationPublishThreshold
                    ?? options.Value.BatchSize ?? 0;
                MaxNetworkMessageSize = (int?)writerGroup.MaxNetworkMessageSize
                    ?? options.Value.MaxNetworkMessageSize ?? 0;

                if (MaxNetworkMessageSize <= 0)
                {
                    MaxNetworkMessageSize = int.MaxValue;
                }
                if (MaxNetworkMessageSize > EventClient.MaxEventPayloadSizeInBytes)
                {
                    MaxNetworkMessageSize = EventClient.MaxEventPayloadSizeInBytes;
                }

                BatchTriggerInterval = writerGroup.PublishingInterval
                    ?? options.Value.BatchTriggerInterval ?? TimeSpan.Zero;
                //
                // If the max notification per message is 1 then there is no need to
                // have an interval publishing as the messages are emitted as soon
                // as they arrive anyway
                //
                if (MaxNotificationsPerMessage == 1)
                {
                    BatchTriggerInterval = TimeSpan.Zero;
                }
                MaxPublishQueueSize = (int?)writerGroup.PublishQueueSize
                    ?? options.Value.MaxNetworkMessageSendQueueSize ?? kMaxQueueSize;

                //
                // If undefined, set notification buffer to 1 if no publishing interval
                // otherwise queue as much as reasonable
                //
                if (MaxNotificationsPerMessage <= 0)
                {
                    MaxNotificationsPerMessage = BatchTriggerInterval == TimeSpan.Zero ?
                        1 : MaxPublishQueueSize;
                }

                MaxPublishQueuePartitions = writerGroup.PublishQueuePartitions ??
                    options.Value.DefaultWriterGroupPartitions ?? 0;
            }

            /// <summary>
            /// Log the transportation options
            /// </summary>
            /// <param name="writerGroup"></param>
            /// <param name="logger"></param>
            public void Log(WriterGroupModel writerGroup, ILogger logger)
            {
                logger.LogInformation("Writer group {WriterGroup} set up to publish notifications " +
                    "{Interval} {Batching} with {MaxSize} to {Transport} with {HeaderLayout} layout and " +
                    "{MessageType} encoding (queuing at most {MaxQueueSize} subscription notifications)...",
                    writerGroup.Name ?? Constants.DefaultWriterGroupName,
                    BatchTriggerInterval == TimeSpan.Zero ?
                        "as soon as they arrive" : $"every {BatchTriggerInterval} (hh:mm:ss)",
                    MaxNotificationsPerMessage == 1 ?
                        "and individually" :
                $"or when a batch of {MaxNotificationsPerMessage} notifications is ready",
                    MaxNetworkMessageSize == int.MaxValue ?
                        "unlimited size" : $"at most {MaxNetworkMessageSize / 1024} kb",
                    EventClient.Name, writerGroup.HeaderLayoutUri ?? "unknown",
                    writerGroup.MessageType ?? MessageEncoding.Json, MaxPublishQueueSize);
            }

            /// <summary>
            /// With 256k limit this is 1 GB.
            /// TODO: Must be related to the actual limit size
            /// </summary>
            private const int kMaxQueueSize = 4096;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_partitions_count",
                () => new Measurement<int>(_queue.PartitionCount, _metrics.TagList),
                description: "Partition count of the writer queue.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_partitions_active",
                () => new Measurement<int>(_queue.ActiveCount, _metrics.TagList),
                description: "Active partitions pushing data inside the writer queue.");
            _meter.CreateObservableCounter("iiot_edge_publisher_send_queue_dropped_count",
                () => new Measurement<long>(_sendBlockInputDroppedCount, _metrics.TagList),
                description: "Telemetry messages dropped due to overflow.");
            _meter.CreateObservableCounter("iiot_edge_publisher_publish_queue_dropped_count",
                () => new Measurement<long>(_dataflowInputDroppedCount, _metrics.TagList),
                description: "Telemetry messages dropped due to overflow of the publish queue.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_send_queue_size",
                () => new Measurement<long>(_queue.SendInput, _metrics.TagList),
                description: "Telemetry messages queued for sending upstream.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_batch_input_queue_size",
                () => new Measurement<long>(_notificationBufferInputCount, _metrics.TagList),
                description: "Telemetry messages queued for sending upstream.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_encoding_input_queue_size",
                () => new Measurement<long>(_queue.EncodingInput, _metrics.TagList),
                description: "Telemetry messages queued for sending upstream.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_encoding_output_queue_size",
                () => new Measurement<long>(_queue.EncodingOutput, _metrics.TagList),
                description: "Telemetry messages queued for sending upstream.");
            _meter.CreateObservableCounter("iiot_edge_publisher_messages",
                () => new Measurement<long>(_messagesSentCount, _metrics.TagList),
                description: "Number of IoT messages successfully sent via transport.");
            _meter.CreateObservableGauge("iiot_edge_publisher_messages_per_second",
                () => new Measurement<double>(_messagesSentCount / UpTime, _metrics.TagList),
                description: "Messages/second sent via transport.");
            _meter.CreateObservableCounter("iiot_edge_publisher_message_send_failures",
                () => new Measurement<long>(_errorCount, _metrics.TagList),
                description: "Number of failures sending a network message.");

            _meter.CreateObservableCounter("iiot_edge_publisher_sent_iot_messages",
                () => new Measurement<long>(_transport.IsIoTEdge ? _messagesSentCount : 0, _metrics.TagList),
                description: "Number of IoT messages successfully sent via transport.");
            _meter.CreateObservableGauge("iiot_edge_publisher_sent_iot_messages_per_second",
                () => new Measurement<double>(_transport.IsIoTEdge ? _messagesSentCount / UpTime : 0d, _metrics.TagList),
                description: "Messages/second sent via transport.");
            _meter.CreateObservableCounter("iiot_edge_publisher_iothub_queue_dropped_count",
                () => new Measurement<long>(_transport.IsIoTEdge ? _sendBlockInputDroppedCount : 0, _metrics.TagList),
                description: "Telemetry messages dropped due to overflow of the send queue.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_iothub_queue_size",
                () => new Measurement<long>(_transport.IsIoTEdge ? _queue.SendInput : 0, _metrics.TagList),
                description: "Telemetry messages queued for sending upstream.");
            _meter.CreateObservableCounter("iiot_edge_publisher_failed_iot_messages",
                () => new Measurement<long>(_transport.IsIoTEdge ? _errorCount : 0, _metrics.TagList),
                description: "Number of failures sending a network message.");
        }

        static readonly Histogram<double> kSendingDuration = Diagnostics.Meter.CreateHistogram<double>(
            "iiot_edge_publisher_messages_duration", description: "Histogram of message sending durations.");

        private double UpTime => _timeProvider.GetElapsedTime(_startTime).TotalSeconds;
        private long _messagesSentCount;
        private long _errorCount;
        private long _sendBlockInputDroppedCount;
        private long _dataflowInputDroppedCount;
        private long _notificationBufferInputCount;
        private readonly IOptions<PublisherOptions> _options;
        private readonly IMessageEncoder _messageEncoder;
        private IPublishQueue _queue;
        private TransportOptions _transport;
        private readonly List<IEventClient> _eventClients;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
        private readonly IWriterGroupDiagnostics? _diagnostics;
        private readonly bool _logNotifications;
        private readonly Regex? _logNotificationsFilter;
        private readonly Func<IList<MonitoredItemNotificationModel>,
            IEnumerable<MonitoredItemNotificationModel>> _filterNotifications;
        private readonly long _startTime;
        private long _dataFlowStartTime;
        private readonly CancellationTokenSource _cts = new();
        private readonly IMetricsContext _metrics;
        private readonly Meter _meter = Diagnostics.NewMeter();
    }
}
