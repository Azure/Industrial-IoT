// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.PubSub.Encoding;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Routes writer notifications into the managed PubSub notification buffer
    /// so the native PubSub runtime publishes them.
    /// </summary>
    /// <remarks>
    /// The writer path raises notifications synchronously, while the buffer
    /// applies backpressure asynchronously. Notifications are therefore handed
    /// to a bounded channel and translated on a pump, matching how the custom
    /// encoder sink queues work, so a slow transport never blocks a
    /// subscription callback.
    /// </remarks>
    public sealed class PubSubNotificationSink : IMessageSink, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Create the sink.
        /// </summary>
        /// <param name="notifications">Buffer feeding the native sources.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="capacity">Bounded queue capacity.</param>
        public PubSubNotificationSink(IManagedPubSubNotificationBuffer notifications,
            ILogger<PubSubNotificationSink> logger, int capacity = 1024)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
            _notifications = notifications ??
                throw new ArgumentNullException(nameof(notifications));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queue = Channel.CreateBounded<OpcUaSubscriptionNotification>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });
            _pump = Task.Run(PumpAsync);
        }

        /// <inheritdoc/>
        public void OnMessage(OpcUaSubscriptionNotification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);
            if (!_queue.Writer.TryWrite(notification))
            {
                Interlocked.Increment(ref _dropped);
                _logger.NotificationDropped(_dropped);
                notification.Dispose();
            }
        }

        /// <inheritdoc/>
        public void OnCounterReset()
        {
            Interlocked.Exchange(ref _dropped, 0);
        }

        /// <summary>
        /// Gets the number of notifications dropped because the queue was full.
        /// </summary>
        public long Dropped => Interlocked.Read(ref _dropped);

        /// <inheritdoc/>
        public void Dispose()
        {
            //
            // Writer group scopes are disposed synchronously, so the sink must
            // support both disposal styles or the container throws.
            //
            if (!BeginDispose())
            {
                return;
            }
            try
            {
                _pump.Wait(kDisposeTimeout);
            }
            catch (AggregateException)
            {
            }
            _stop.Dispose();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (!BeginDispose())
            {
                return;
            }
            try
            {
                await _pump.WaitAsync(kDisposeTimeout).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
            {
            }
            _stop.Dispose();
        }

        private bool BeginDispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return false;
            }
            _queue.Writer.TryComplete();
            _stop.Cancel();
            return true;
        }

        /// <summary>
        /// Translate a writer notification into managed notifications, one per
        /// occurrence. One writer notification can carry several occurrences -
        /// several events raised in the same publish, or several queued values
        /// of the same variable - which appear as repeated field names. The
        /// writer path separates them by grouping on the field name, ordering
        /// each group by source timestamp, and taking one value from each group
        /// per message, so the same rule is applied here. The fields of one
        /// occurrence are never split across messages: for an event or a
        /// condition snapshot the field set <em>is</em> the occurrence.
        /// </summary>
        /// <param name="notification">Writer notification to translate.</param>
        /// <returns>The managed notifications to publish.</returns>
        internal static IEnumerable<ManagedPubSubNotification> Translate(
            OpcUaSubscriptionNotification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);
            if (notification.MessageType is MessageType.KeepAlive or MessageType.Metadata)
            {
                //
                // Keep alives carry no fields and metadata is produced by the
                // native runtime from the dataset metadata, so neither maps to
                // a field notification.
                //
                yield break;
            }
            var dataSetName = GetDataSetName(notification);
            if (dataSetName is null)
            {
                yield break;
            }
            var kind = notification.MessageType switch
            {
                MessageType.Event => ManagedPubSubNotificationKind.Event,
                MessageType.Condition => ManagedPubSubNotificationKind.Condition,
                _ => ManagedPubSubNotificationKind.Data
            };
            //
            // The subscription already knows what it produced, so the native
            // runtime is told rather than left to derive it. A condition
            // snapshot is an event occurrence on the wire; Part 14 §7.2.5.4
            // has no separate condition message type.
            //
            var frame = notification.MessageType switch
            {
                MessageType.Event or MessageType.Condition =>
                    PubSubDataSetMessageType.Event,
                MessageType.DeltaFrame => PubSubDataSetMessageType.DeltaFrame,
                _ => PubSubDataSetMessageType.KeyFrame
            };
            var fallback = notification.PublishTimestamp ?? notification.CreatedTimestamp;
            var queues = new List<(string Name, List<DataValue> Values)>();
            var byName = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in notification.Notifications)
            {
                var fieldName = item.DataSetFieldName ?? item.Id ?? item.NodeId;
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    continue;
                }
                if (!byName.TryGetValue(fieldName, out var position))
                {
                    position = queues.Count;
                    byName.Add(fieldName, position);
                    queues.Add((fieldName, []));
                }
                queues[position].Values.Add(item.Value ?? new DataValue(Variant.Null,
                    StatusCodes.BadNoData, DateTimeUtc.From(fallback)));
            }
            var ordered = queues
                .Select(queue => (queue.Name,
                    Values: queue.Values.OrderBy(value => value.SourceTimestamp).ToArray()))
                .ToArray();
            for (var round = 0; ; round++)
            {
                var fields = new List<ManagedPubSubField>(ordered.Length);
                //
                // The occurrence is stamped with the earliest source timestamp
                // its fields carry, so a message reports when the occurrence
                // happened rather than when the last of its fields was encoded.
                //
                var timestamp = fallback;
                var stamped = false;
                foreach (var (name, values) in ordered)
                {
                    if (round >= values.Length)
                    {
                        continue;
                    }
                    var value = values[round];
                    //
                    // An unset source timestamp is DateTimeUtc.MinValue, which
                    // is 1601 rather than DateTime.MinValue, so comparing
                    // against the latter never detects one and publishes 1601
                    // as if it were the sample time.
                    //
                    if (value.SourceTimestamp != DateTimeUtc.MinValue)
                    {
                        var sourceTimestamp = new DateTimeOffset(value.SourceTimestamp,
                            TimeSpan.Zero);
                        if (!stamped || sourceTimestamp < timestamp)
                        {
                            timestamp = sourceTimestamp;
                            stamped = true;
                        }
                    }
                    fields.Add(new ManagedPubSubField(name, value));
                }
                if (fields.Count == 0)
                {
                    break;
                }
                yield return new ManagedPubSubNotification(dataSetName, timestamp,
                    kind, fields, frame);
            }

            //
            // Extension fields belong to every payload the writer path emits,
            // and the context carries them already resolved, including the
            // synthetic EndpointUrl and ApplicationUri the full featured profile
            // appends. They are published as constants so the source retains
            // them and appends them to each message rather than emitting one of
            // their own.
            //
            if (notification.Context is DataSetWriterContext context)
            {
                foreach (var (fieldName, value) in context.ExtensionFields)
                {
                    if (string.IsNullOrWhiteSpace(fieldName) || value is not { } resolved)
                    {
                        continue;
                    }
                    yield return new ManagedPubSubNotification(dataSetName, fieldName,
                        fallback, resolved, ManagedPubSubNotificationKind.Extension);
                }
            }
        }

        /// <summary>
        /// Resolve the dataset name the native registry uses for the writer
        /// that produced this notification.
        /// </summary>
        /// <param name="notification"></param>
        internal static string? GetDataSetName(OpcUaSubscriptionNotification notification)
        {
            if (notification.Context is not DataSetWriterContext context)
            {
                return null;
            }
            var name = context.Writer.DataSet?.Name
                ?? context.Writer.DataSet?.DataSetMetaData?.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            return $"{context.WriterGroup.Id}:{context.Writer.Id}";
        }

        private async Task PumpAsync()
        {
            try
            {
                await foreach (var notification in _queue.Reader
                    .ReadAllAsync(_stop.Token).ConfigureAwait(false))
                {
                    try
                    {
                        foreach (var managed in Translate(notification))
                        {
                            await _notifications.EnqueueAsync(managed, _stop.Token)
                                .ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        notification.Dispose();
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.NotificationTranslationFailed(ex);
                    }
                    finally
                    {
                        notification.Dispose();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            while (_queue.Reader.TryRead(out var pending))
            {
                pending.Dispose();
            }
        }

        private readonly IManagedPubSubNotificationBuffer _notifications;
        private readonly ILogger<PubSubNotificationSink> _logger;
        private readonly Channel<OpcUaSubscriptionNotification> _queue;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _pump;
        private long _dropped;
        private int _disposed;

        private static readonly TimeSpan kDisposeTimeout = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Source-generated logging definitions for the notification sink.
    /// </summary>
    internal static partial class PubSubNotificationSinkLogging
    {
        private const int EventClass = 750;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Warning,
            Message = "Dropped a writer notification because the managed PubSub " +
            "queue is full. {Dropped} notifications dropped so far.")]
        public static partial void NotificationDropped(this ILogger logger, long dropped);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Error,
            Message = "Failed to translate a writer notification for managed PubSub.")]
        public static partial void NotificationTranslationFailed(this ILogger logger,
            Exception ex);
    }
}
