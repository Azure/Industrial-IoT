// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.Transports;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// The handling used when the test-only egress queue reaches its bound.
    /// </summary>
    internal enum PubSubShadowEgressOverflowPolicy
    {
        Wait,
        Reject
    }

    /// <summary>
    /// Test-only egress transport options. This is deliberately not bound to
    /// Publisher configuration: production continues to use NetworkMessageSink.
    /// </summary>
    internal sealed class PubSubShadowEgressOptions
    {
        public int QueueCapacity { get; set; } = 64;
        public PubSubShadowEgressOverflowPolicy OverflowPolicy { get; set; } =
            PubSubShadowEgressOverflowPolicy.Wait;
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(50);
        public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public bool IncludeSchema { get; set; } = true;
    }

    /// <summary>
    /// Snapshot of egress transport measurements that are bridged into the
    /// existing Publisher writer-group diagnostic model.
    /// </summary>
    internal sealed record class PubSubShadowEgressMetrics
    {
        public required int QueueDepth { get; init; }
        public required long BackpressureCount { get; init; }
        public required long OverflowCount { get; init; }
        public required long RetryCount { get; init; }
        public required long SentCount { get; init; }
        public required long FailedCount { get; init; }
        public required long ChunkCount { get; init; }
    }

    internal interface IPubSubShadowEgressMetricsProvider
    {
        PubSubShadowEgressMetrics Metrics { get; }
    }

    internal sealed class PubSubShadowEgressSettings
    {
        public required string ConnectionName { get; init; }
        public required string Topic { get; init; }
        public required string ContentType { get; init; }
        public string? ContentEncoding { get; init; }
        public required QoS QualityOfService { get; init; }
        public required bool Retain { get; init; }
        public TimeSpan? TimeToLive { get; init; }
        public required bool UseCloudEvents { get; init; }
        public required Uri CloudEventSource { get; init; }
        public required string CloudEventType { get; init; }
        public string? CloudEventSubject { get; init; }
        public IEventSchema? Schema { get; init; }
        public required IReadOnlyDictionary<string, string?> Properties { get; init; }

        public EventClientCapabilities RequiredCapabilities
        {
            get
            {
                var capabilities = EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.QualityOfService
                    | EventClientCapabilities.Retain
                    | EventClientCapabilities.ContentType;
                if (!string.IsNullOrEmpty(ContentEncoding))
                {
                    capabilities |= EventClientCapabilities.ContentEncoding;
                }
                if (TimeToLive.HasValue)
                {
                    capabilities |= EventClientCapabilities.TimeToLive;
                }
                if (Properties.Count != 0)
                {
                    capabilities |= EventClientCapabilities.CustomProperties;
                }
                if (UseCloudEvents)
                {
                    capabilities |= EventClientCapabilities.CloudEvents;
                }
                if (Schema is not null)
                {
                    capabilities |= EventClientCapabilities.Schema;
                }
                return capabilities;
            }
        }
    }

    /// <summary>
    /// Atomically swaps the egress settings used by new native connections.
    /// The registry mirrors the encoding-generation rule: an old connection
    /// holds its resolved settings while a replacement receives a new snapshot.
    /// </summary>
    internal sealed class PubSubShadowEgressSettingsRegistry
    {
        public void Replace(IEnumerable<WriterGroupModel> writerGroups,
            PublisherOptions publisherOptions, PubSubShadowEgressOptions options)
        {
            ArgumentNullException.ThrowIfNull(writerGroups);
            ArgumentNullException.ThrowIfNull(publisherOptions);
            ArgumentNullException.ThrowIfNull(options);

            var replacement = new Dictionary<string, PubSubShadowEgressSettings>(
                StringComparer.Ordinal);
            foreach (var writerGroup in writerGroups)
            {
                ArgumentNullException.ThrowIfNull(writerGroup);
                var settings = CreateSettings(writerGroup, publisherOptions, options);
                if (!replacement.TryAdd(settings.ConnectionName, settings))
                {
                    throw new ArgumentException(
                        $"The egress connection '{settings.ConnectionName}' occurs more than once.",
                        nameof(writerGroups));
                }
            }
            lock (_gate)
            {
                _settings = replacement;
            }
        }

        public Dictionary<string, PubSubShadowEgressSettings> Snapshot()
        {
            lock (_gate)
            {
                return new Dictionary<string, PubSubShadowEgressSettings>(_settings,
                    StringComparer.Ordinal);
            }
        }

        public void Restore(Dictionary<string, PubSubShadowEgressSettings> snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            lock (_gate)
            {
                _settings = new Dictionary<string, PubSubShadowEgressSettings>(snapshot,
                    StringComparer.Ordinal);
            }
        }

        public PubSubShadowEgressSettings Resolve(PubSubConnectionDataType connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            var name = connection.Name ?? string.Empty;
            lock (_gate)
            {
                if (_settings.TryGetValue(name, out var settings))
                {
                    return settings;
                }
            }
            throw new InvalidOperationException(
                $"No event-client egress settings were committed for connection '{name}'.");
        }

        private static PubSubShadowEgressSettings CreateSettings(WriterGroupModel writerGroup,
            PublisherOptions publisherOptions, PubSubShadowEgressOptions options)
        {
            if (string.IsNullOrWhiteSpace(writerGroup.Id))
            {
                throw new ArgumentException("An egress writer group requires an identifier.",
                    nameof(writerGroup));
            }

            var queue = writerGroup.Publishing;
            foreach (var writer in writerGroup.DataSetWriters ?? [])
            {
                var candidate = writer.Publishing ?? queue;
                if (candidate is null)
                {
                    continue;
                }
                if (queue is null)
                {
                    queue = candidate;
                    continue;
                }
                if (!Equivalent(queue, candidate))
                {
                    throw new InvalidOperationException(
                        $"Writer group '{writerGroup.Id}' has writer-specific egress settings. "
                        + "The native runtime emits a group network message, so it cannot "
                        + "silently split that message across topics or QoS queues.");
                }
            }

            var encoding = PubSubConfigurationTranslator.GetShadowEncoding(writerGroup.MessageType);
            var isJson = encoding != PubSubShadowEncoding.Uadp;
            var topic = queue?.QueueName;
            if (string.IsNullOrWhiteSpace(topic))
            {
                topic = "shadow/" + writerGroup.Id;
            }
            var properties = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["writerGroupId"] = writerGroup.Id,
                ["encoding"] = encoding.ToString()
            };
            if (writerGroup.Properties is not null)
            {
                foreach (var property in writerGroup.Properties)
                {
                    properties[property.Key] = ToPropertyValue(property.Value);
                }
            }

            var publisherId = writerGroup.PublisherId ?? publisherOptions.PublisherId
                ?? "publisher";
            var schema = options.IncludeSchema
                ? new PubSubShadowEventSchema(writerGroup.Id, encoding)
                : null;
            return new PubSubShadowEgressSettings
            {
                ConnectionName = "shadow-" + writerGroup.Id,
                Topic = topic,
                ContentType = isJson ? "application/json" : "application/octet-stream",
                ContentEncoding = encoding is PubSubShadowEncoding.JsonGzip
                    or PubSubShadowEncoding.JsonReversibleGzip ? "gzip" : null,
                QualityOfService = queue?.RequestedDeliveryGuarantee
                    ?? publisherOptions.DefaultQualityOfService ?? QoS.AtLeastOnce,
                Retain = queue?.Retain ?? publisherOptions.DefaultMessageRetention ?? false,
                TimeToLive = queue?.Ttl ?? publisherOptions.DefaultMessageTimeToLive,
                UseCloudEvents = publisherOptions.EnableCloudEvents == true,
                CloudEventSource = new Uri("urn:azure-iiot:publisher:" +
                    Uri.EscapeDataString(publisherId)),
                CloudEventType = "org.opcfoundation.ua.pubsub",
                CloudEventSubject = writerGroup.Name ?? writerGroup.Id,
                Schema = schema,
                Properties = new ReadOnlyDictionary<string, string?>(properties)
            };
        }

        private static bool Equivalent(PublishingQueueSettingsModel left,
            PublishingQueueSettingsModel right)
        {
            return string.Equals(left.QueueName, right.QueueName, StringComparison.Ordinal)
                && left.RequestedDeliveryGuarantee == right.RequestedDeliveryGuarantee
                && left.Retain == right.Retain
                && left.Ttl == right.Ttl;
        }

        private static string? ToPropertyValue(JsonNode? value)
        {
            return value switch
            {
                null => null,
                JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text) => text,
                _ => value.ToJsonString()
            };
        }

        private readonly Lock _gate = new();
        private Dictionary<string, PubSubShadowEgressSettings> _settings =
            new(StringComparer.Ordinal);
    }

    /// <summary>
    /// Explicit test registration for event-client egress. Keeping this
    /// registration separate from PublisherOptions prevents an accidental
    /// production cutover through application configuration.
    /// </summary>
    internal sealed class PubSubShadowEgressRegistration
    {
        public PubSubShadowEgressRegistration(IEventClient eventClient,
            PubSubShadowEgressOptions options)
        {
            EventClient = eventClient ?? throw new ArgumentNullException(nameof(eventClient));
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IEventClient EventClient { get; }
        public PubSubShadowEgressOptions Options { get; }
        public PubSubShadowEgressSettingsRegistry Settings { get; } = new();
    }

    /// <summary>
    /// Factory that binds native PubSub connections to an existing IIoT
    /// <see cref="IEventClient"/>. It is registered only by the test seam.
    /// </summary>
    internal sealed class EventClientPubSubTransportFactory : IPubSubTransportFactory
    {
        public EventClientPubSubTransportFactory(string transportProfileUri,
            IEventClient eventClient, PubSubShadowEgressSettingsRegistry settings,
            PubSubShadowEgressOptions options)
        {
            TransportProfileUri = transportProfileUri ??
                throw new ArgumentNullException(nameof(transportProfileUri));
            _eventClient = eventClient ?? throw new ArgumentNullException(nameof(eventClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string TransportProfileUri { get; }

        public IPubSubTransport Create(PubSubConnectionDataType connection,
            ITelemetryContext telemetry, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(telemetry);
            ArgumentNullException.ThrowIfNull(timeProvider);
            var settings = _settings.Resolve(connection);
            ValidateCapabilities(_eventClient, settings.RequiredCapabilities);
            var direction = connection.WriterGroups.IsNull || connection.WriterGroups.Count == 0
                ? PubSubTransportDirection.None
                : PubSubTransportDirection.Send;
            return new EventClientPubSubTransport(TransportProfileUri, direction, _eventClient,
                settings, _options, timeProvider);
        }

        internal static void ValidateCapabilities(IEventClient eventClient,
            EventClientCapabilities required)
        {
            if (eventClient is not IEventClientCapabilities declared)
            {
                throw new InvalidOperationException(
                    $"The selected event client '{eventClient.Name}' does not declare "
                    + "IEventClientCapabilities; egress activation is rejected rather "
                    + "than silently degrading PubSub semantics.");
            }
            var unsupported = required & ~declared.Capabilities;
            if (unsupported != 0)
            {
                throw new NotSupportedException(
                    $"The selected event client '{eventClient.Name}' does not support "
                    + $"the required PubSub egress capabilities: {unsupported}.");
            }
        }

        private readonly IEventClient _eventClient;
        private readonly PubSubShadowEgressSettingsRegistry _settings;
        private readonly PubSubShadowEgressOptions _options;
    }

    /// <summary>
    /// Serialized, bounded event-client egress transport. Queue acknowledgement
    /// occurs only after all payload chunks have been sent successfully; a
    /// transient failure therefore remains ahead of later frames for the same
    /// writer-group/topic.
    /// </summary>
    internal sealed class EventClientPubSubTransport : IPubSubTransport,
        IPubSubTopicProvider, IPubSubShadowEgressMetricsProvider
    {
        public EventClientPubSubTransport(string transportProfileUri,
            PubSubTransportDirection direction, IEventClient eventClient,
            PubSubShadowEgressSettings settings, PubSubShadowEgressOptions options,
            TimeProvider timeProvider)
        {
            if (options.QueueCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options),
                    "The egress queue capacity must be positive.");
            }
            if (options.InitialRetryDelay <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options),
                    "The initial retry delay must be positive.");
            }
            if (options.MaximumRetryDelay < options.InitialRetryDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(options),
                    "The maximum retry delay must not be smaller than the initial delay.");
            }

            TransportProfileUri = transportProfileUri ??
                throw new ArgumentNullException(nameof(transportProfileUri));
            Direction = direction;
            _eventClient = eventClient ?? throw new ArgumentNullException(nameof(eventClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _outbound = Channel.CreateBounded<PendingFrame>(
                new BoundedChannelOptions(options.QueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });
        }

        public string TransportProfileUri { get; }

        public PubSubTransportDirection Direction { get; }

        public bool IsConnected => Volatile.Read(ref _isConnected) != 0;

        public event EventHandler<PubSubTransportStateChangedEventArgs>? StateChanged;

        public PubSubShadowEgressMetrics Metrics => new()
        {
            QueueDepth = Volatile.Read(ref _queueDepth),
            BackpressureCount = Interlocked.Read(ref _backpressureCount),
            OverflowCount = Interlocked.Read(ref _overflowCount),
            RetryCount = Interlocked.Read(ref _retryCount),
            SentCount = Interlocked.Read(ref _sentCount),
            FailedCount = Interlocked.Read(ref _failedCount),
            ChunkCount = Interlocked.Read(ref _chunkCount)
        };

        public ValueTask OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.CompareExchange(ref _isConnected, 1, 0) == 0)
            {
                _sendLoop = Task.Run(ProcessAsync);
                StateChanged?.Invoke(this, new PubSubTransportStateChangedEventArgs(
                    true, StatusCodes.Good, "Event-client PubSub egress transport opened."));
            }
            return default;
        }

        public async ValueTask CloseAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _isConnected, 0) == 0)
            {
                return;
            }
            _outbound.Writer.TryComplete();
            if (_sendLoop is not null)
            {
                await _sendLoop.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            StateChanged?.Invoke(this, new PubSubTransportStateChangedEventArgs(
                false, StatusCodes.Good, "Event-client PubSub egress transport closed."));
        }

        public async ValueTask SendAsync(ReadOnlyMemory<byte> payload, string? topic = null,
            CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("The event-client PubSub transport is not open.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            var frame = new PendingFrame(payload.ToArray(),
                string.IsNullOrWhiteSpace(topic) ? _settings.Topic : topic!,
                cancellationToken);

            if (!_outbound.Writer.TryWrite(frame))
            {
                if (_options.OverflowPolicy == PubSubShadowEgressOverflowPolicy.Reject)
                {
                    Interlocked.Increment(ref _overflowCount);
                    throw new InvalidOperationException(
                        "The bounded event-client PubSub egress queue rejected a frame.");
                }
                Interlocked.Increment(ref _backpressureCount);
                await _outbound.Writer.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
            }
            Interlocked.Increment(ref _queueDepth);
            await frame.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<PubSubTransportFrame> ReceiveAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            yield break;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                _stop.Cancel();
                _stop.Dispose();
            }
        }

        public string BuildMetaDataTopic(PublisherId publisherId, ushort writerGroupId,
            ushort dataSetWriterId)
        {
            return string.Concat(_settings.Topic.TrimEnd('/'), "/metadata/",
                writerGroupId, "/", dataSetWriterId);
        }

        public string BuildDataTopic(PublisherId publisherId,
            WriterGroupDataType writerGroup, ushort? dataSetWriterId)
        {
            return _settings.Topic;
        }

        public string BuildDiscoveryTopic(PublisherId publisherId, string messageTypeSegment)
        {
            return string.Concat(_settings.Topic.TrimEnd('/'), "/discovery/",
                messageTypeSegment);
        }

        private async Task ProcessAsync()
        {
            try
            {
                await foreach (var frame in _outbound.Reader.ReadAllAsync(_stop.Token)
                    .ConfigureAwait(false))
                {
                    Interlocked.Decrement(ref _queueDepth);
                    try
                    {
                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                            _stop.Token, frame.CancellationToken);
                        await SendFrameAsync(frame, linked.Token).ConfigureAwait(false);
                        frame.Completion.TrySetResult();
                    }
                    catch (OperationCanceledException exception)
                    {
                        frame.Completion.TrySetCanceled(exception.CancellationToken);
                    }
                    catch (Exception exception)
                    {
                        Interlocked.Increment(ref _failedCount);
                        frame.Completion.TrySetException(exception);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                while (_outbound.Reader.TryRead(out var frame))
                {
                    Interlocked.Decrement(ref _queueDepth);
                    frame.Completion.TrySetCanceled(_stop.Token);
                }
            }
        }

        private async ValueTask SendFrameAsync(PendingFrame frame, CancellationToken cancellationToken)
        {
            var maximum = _eventClient.MaxEventPayloadSizeInBytes;
            if (maximum <= 0)
            {
                throw new InvalidOperationException(
                    $"The selected event client '{_eventClient.Name}' has an invalid maximum payload size.");
            }

            for (var offset = 0; offset < frame.Payload.Length || offset == 0; offset += maximum)
            {
                var length = Math.Min(maximum, frame.Payload.Length - offset);
                var chunk = frame.Payload.AsMemory(offset, length);
                await SendChunkWithRetryAsync(chunk, frame.Topic, cancellationToken)
                    .ConfigureAwait(false);
                Interlocked.Increment(ref _chunkCount);
                if (frame.Payload.Length == 0)
                {
                    break;
                }
            }
            Interlocked.Increment(ref _sentCount);
        }

        private async ValueTask SendChunkWithRetryAsync(ReadOnlyMemory<byte> payload,
            string topic, CancellationToken cancellationToken)
        {
            var delay = _options.InitialRetryDelay;
            for (;;)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await SendChunkAsync(payload, topic, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    Interlocked.Increment(ref _retryCount);
                    await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
                    var nextTicks = Math.Min(delay.Ticks * 2, _options.MaximumRetryDelay.Ticks);
                    delay = TimeSpan.FromTicks(nextTicks);
                }
            }
        }

        private async ValueTask SendChunkAsync(ReadOnlyMemory<byte> payload, string topic,
            CancellationToken cancellationToken)
        {
            using var @event = _eventClient.CreateEvent();
            var configured = @event
                .SetTimestamp(_timeProvider.GetUtcNow())
                .SetTopic(topic)
                .SetContentType(_settings.ContentType)
                .SetContentEncoding(_settings.ContentEncoding)
                .SetQoS(_settings.QualityOfService)
                .SetRetain(_settings.Retain || IsMetaDataTopic(topic));
            if (_settings.TimeToLive.HasValue)
            {
                configured = configured.SetTtl(_settings.TimeToLive.Value);
            }
            foreach (var property in _settings.Properties)
            {
                configured = configured.AddProperty(property.Key, property.Value);
            }
            if (_settings.UseCloudEvents)
            {
                configured = configured.AsCloudEvent(new CloudEventHeader
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Source = _settings.CloudEventSource,
                    Type = _settings.CloudEventType,
                    Subject = _settings.CloudEventSubject,
                    Time = _timeProvider.GetUtcNow(),
                    DataContentType = _settings.ContentType
                });
            }
            if (_settings.Schema is not null)
            {
                configured = configured.SetSchema(_settings.Schema);
            }
            configured = configured.AddBuffers([new ReadOnlySequence<byte>(payload)]);
            await configured.SendAsync(cancellationToken).ConfigureAwait(false);
        }

        private static bool IsMetaDataTopic(string topic)
        {
            return topic.Contains("/metadata/", StringComparison.Ordinal);
        }

        private sealed class PendingFrame
        {
            public PendingFrame(byte[] payload, string topic,
                CancellationToken cancellationToken)
            {
                Payload = payload;
                Topic = topic;
                CancellationToken = cancellationToken;
            }

            public byte[] Payload { get; }
            public string Topic { get; }
            public CancellationToken CancellationToken { get; }
            public TaskCompletionSource Completion { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly Channel<PendingFrame> _outbound;
        private readonly CancellationTokenSource _stop = new();
        private readonly IEventClient _eventClient;
        private readonly PubSubShadowEgressSettings _settings;
        private readonly PubSubShadowEgressOptions _options;
        private readonly TimeProvider _timeProvider;
        private Task? _sendLoop;
        private int _isConnected;
        private int _queueDepth;
        private long _backpressureCount;
        private long _overflowCount;
        private long _retryCount;
        private long _sentCount;
        private long _failedCount;
        private long _chunkCount;
    }

    internal sealed class PubSubShadowEventSchema : IEventSchema
    {
        public PubSubShadowEventSchema(string writerGroupId, PubSubShadowEncoding encoding)
        {
            Name = writerGroupId;
            Id = "urn:azure-iiot:pubsub:" + Uri.EscapeDataString(writerGroupId);
            Schema = "{\"type\":\"opcua-pubsub\",\"encoding\":\"" + encoding + "\"}";
        }

        public string Type => "application/schema+json";
        public string Name { get; }
        public ulong Version => 1;
        public string Schema { get; }
        public string Id { get; }
    }
}
