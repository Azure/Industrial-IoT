// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.Transports;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// The handling used when the egress queue reaches its bound.
    /// </summary>
    internal enum PubSubShadowEgressOverflowPolicy
    {
        Wait,
        Reject
    }

    /// <summary>
    /// Egress transport options. This is deliberately not bound to Publisher
    /// configuration; production uses it only when native PubSub preview is enabled.
    /// </summary>
    internal sealed class PubSubShadowEgressOptions
    {
        public int QueueCapacity { get; set; } = 64;
        public PubSubShadowEgressOverflowPolicy OverflowPolicy { get; set; } =
            PubSubShadowEgressOverflowPolicy.Wait;
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(50);
        public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        /// <summary>
        /// Gets or sets the maximum total send attempts for a transient
        /// failure, including the initial attempt.
        /// </summary>
        public int MaxSendAttempts { get; set; } = 5;
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

    internal sealed record class PubSubShadowEgressSettings
    {
        public required string ConnectionName { get; init; }

        /// <summary>
        /// Transport this connection publishes through. Writer groups may
        /// select different transports, so the client is resolved per
        /// connection rather than shared application wide.
        /// </summary>
        public required IEventClient EventClient { get; init; }

        public required PubSubShadowEncoding Encoding { get; init; }
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
        public IReadOnlyList<PubSubShadowMetadataWriterSettings> MetadataWriters { get; init; } = [];

        /// <summary>
        /// Capabilities that only annotate the message and that the selected
        /// transport cannot carry. They are removed from the required set so
        /// the transport is used rather than refused. The values are still
        /// offered to the client, which drops what its protocol has no field
        /// for, exactly as the writer path does.
        /// </summary>
        public EventClientCapabilities DegradedCapabilities { get; init; }

        public EventClientCapabilities RequiredCapabilities
        {
            get
            {
                var capabilities = EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.ContentType;
                if (QualityOfService != QoS.AtMostOnce)
                {
                    //
                    // At most once is fire and forget, which every transport
                    // honours. Only a stronger delivery guarantee needs the
                    // client to actually implement quality of service.
                    //
                    capabilities |= EventClientCapabilities.QualityOfService;
                }
                if (Retain)
                {
                    capabilities |= EventClientCapabilities.Retain;
                }
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
                return capabilities & ~DegradedCapabilities;
            }
        }

        public PubSubShadowEgressSettings WithTransportSettings(
            string topic, PublishingQueueSettingsModel? publishing, bool defaultRetain)
        {
            return this with
            {
                Topic = topic,
                QualityOfService = publishing?.RequestedDeliveryGuarantee ?? QualityOfService,
                Retain = publishing?.Retain ?? defaultRetain,
                TimeToLive = publishing?.Ttl ?? TimeToLive
            };
        }
    }

    /// <summary>
    /// Public-model metadata transport configuration associated with one
    /// native data-set writer.
    /// </summary>
    internal sealed class PubSubShadowMetadataWriterSettings
    {
        public required string WriterName { get; init; }
        public PublishingQueueSettingsModel? Publishing { get; init; }

        /// <summary>
        /// Whether the writer announces its dataset metadata at all. The writer
        /// path suppresses the announcement when metadata is disabled, and the
        /// native runtime announces on its own schedule, so the egress has to
        /// drop what the configuration asked not to publish.
        /// </summary>
        public bool Enabled { get; init; } = true;
    }

    /// <summary>
    /// Selects the transport a writer group publishes through. Writer groups
    /// may name their own transport, so the native egress resolves a client per
    /// connection instead of sharing one application wide.
    /// </summary>
    internal interface IPubSubShadowEventClientSelector
    {
        /// <summary>
        /// Selects the client for a writer group.
        /// </summary>
        /// <param name="writerGroup">Writer group being configured.</param>
        /// <returns>The transport to publish the group through.</returns>
        IEventClient Select(WriterGroupModel writerGroup);
    }

    /// <summary>
    /// Selector that publishes every writer group through one client.
    /// </summary>
    internal sealed class PubSubShadowSingleEventClientSelector :
        IPubSubShadowEventClientSelector
    {
        public PubSubShadowSingleEventClientSelector(IEventClient eventClient)
        {
            _eventClient = eventClient ?? throw new ArgumentNullException(nameof(eventClient));
        }

        public IEventClient Select(WriterGroupModel writerGroup)
        {
            return _eventClient;
        }

        private readonly IEventClient _eventClient;
    }

    /// <summary>
    /// Atomically swaps the egress settings used by new native connections.
    /// The registry mirrors the encoding-generation rule: an old connection
    /// holds its resolved settings while a replacement receives a new snapshot.
    /// </summary>
    internal sealed class PubSubShadowEgressSettingsRegistry
    {
        public PubSubShadowEgressSettingsRegistry(
            IPubSubShadowEventClientSelector eventClients)
        {
            _eventClients = eventClients
                ?? throw new ArgumentNullException(nameof(eventClients));
        }

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
                var settings = CreateSettings(writerGroup, publisherOptions, options,
                    _eventClients.Select(writerGroup));
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

        /// <summary>
        /// Resolve the topic the writer group publishes to. An explicitly
        /// configured queue name wins, otherwise the Publisher topic templates
        /// are applied exactly as the writer path applies them, so the native
        /// runtime publishes where consumers already listen.
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="publisherOptions"></param>
        /// <param name="queueName"></param>
        /// <param name="publisherId"></param>
        private static string ResolveTelemetryTopic(WriterGroupModel writerGroup,
            PublisherOptions publisherOptions, string? queueName, string publisherId)
        {
            if (!string.IsNullOrWhiteSpace(queueName))
            {
                return queueName;
            }
            return CreateTopicBuilder(writerGroup, publisherOptions, queueName,
                publisherId, writer: null).TelemetryTopic;
        }

        /// <summary>
        /// Resolves the topic a writer publishes its dataset metadata to. The
        /// writer path applies the metadata topic template, so a configuration
        /// naming one through --mdt must reach the same topic here or every
        /// consumer of the metadata stops receiving it.
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="writer"></param>
        /// <param name="publisherOptions"></param>
        /// <param name="queueName"></param>
        /// <param name="publisherId"></param>
        private static string? ResolveMetaDataTopic(WriterGroupModel writerGroup,
            DataSetWriterModel writer, PublisherOptions publisherOptions,
            string? queueName, string publisherId)
        {
            if (!string.IsNullOrWhiteSpace(writer.MetaData?.QueueName))
            {
                return writer.MetaData.QueueName;
            }
            var topic = CreateTopicBuilder(writerGroup, publisherOptions, queueName,
                publisherId, writer).DataSetMetaDataTopic;
            return string.IsNullOrWhiteSpace(topic) ? null : topic;
        }

        private static TopicBuilder CreateTopicBuilder(WriterGroupModel writerGroup,
            PublisherOptions publisherOptions, string? queueName, string publisherId,
            DataSetWriterModel? writer)
        {
            var writerGroupName = TopicFilter.Escape(writerGroup.Name
                ?? Constants.DefaultWriterGroupName);
            var variables = new Dictionary<string, string>
            {
                [PublisherConfig.PublisherIdKey] = TopicFilter.Escape(publisherId),
                [PublisherConfig.WriterGroupIdVariableName] = writerGroup.Id,
                [PublisherConfig.DataSetWriterGroupVariableName] = writerGroupName,
                [PublisherConfig.WriterGroupVariableName] = writerGroupName
            };
            if (writer is not null)
            {
                var writerName = TopicFilter.Escape(writer.DataSetWriterName ?? writer.Id);
                variables[PublisherConfig.DataSetWriterIdVariableName] = writer.Id;
                variables[PublisherConfig.DataSetWriterVariableName] = writerName;
                variables[PublisherConfig.DataSetWriterNameVariableName] = writerName;
                variables[PublisherConfig.DataSetNameVariableName] = TopicFilter.Escape(
                    writer.DataSet?.Name ?? string.Empty);
            }
            return new TopicBuilder(publisherOptions, writerGroup.MessageType,
                new TopicTemplatesOptions
                {
                    Telemetry = queueName,
                    DataSetMetaData = writer?.MetaData?.QueueName
                }, variables);
        }

        private static PubSubShadowEgressSettings CreateSettings(WriterGroupModel writerGroup,
            PublisherOptions publisherOptions, PubSubShadowEgressOptions options,
            IEventClient eventClient)
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
            var publisherId = writerGroup.PublisherId ?? publisherOptions.PublisherId
                ?? Constants.DefaultPublisherId;
            var topic = ResolveTelemetryTopic(writerGroup, publisherOptions,
                queue?.QueueName, publisherId);
            var properties = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["writerGroupId"] = writerGroup.Id,
                ["encoding"] = encoding.ToString()
            };
            //
            // The writer path stamps the message schema on every telemetry
            // message, and routing on it is a standard way to wire an IoT Hub
            // deployment up. Publishing without it would silently stop those
            // routes matching, and no test would notice because the collector
            // only looks at the property when it is present.
            //
            properties[OpcUa.Constants.MessagePropertySchemaKey] =
                encoding is PubSubShadowEncoding.Uadp
                    ? Encoders.MessageSchemaTypes.NetworkMessageUadp
                    : Encoders.MessageSchemaTypes.NetworkMessageJson;
            if (publisherOptions.EnableDataSetRoutingInfo ?? false)
            {
                properties[OpcUa.Constants.MessagePropertyRoutingKey] =
                    writerGroup.Name ?? Constants.DefaultWriterGroupName;
            }
            if (writerGroup.Properties is not null)
            {
                foreach (var property in writerGroup.Properties)
                {
                    properties[property.Key] = ToPropertyValue(property.Value);
                }
            }

            var schema = options.IncludeSchema
                ? new PubSubShadowEventSchema(writerGroup.Id, encoding)
                : null;
            var metadataEnabled = publisherOptions.DisableDataSetMetaData != true;
            var metadataWriters = (writerGroup.DataSetWriters ?? [])
                .Select(writer => new PubSubShadowMetadataWriterSettings
                {
                    WriterName = writer.DataSetWriterName ?? writer.Id,
                    Enabled = metadataEnabled
                        && writer.DataSet?.DataSetMetaData is not null,
                    Publishing = ResolveMetaDataTopic(writerGroup, writer, publisherOptions,
                        queue?.QueueName, publisherId) is { } metadataTopic
                        ? (writer.MetaData ?? new PublishingQueueSettingsModel()) with
                        {
                            QueueName = metadataTopic
                        }
                        : writer.MetaData
                })
                .ToArray();
            return new PubSubShadowEgressSettings
            {
                ConnectionName = "shadow-" + writerGroup.Id,
                EventClient = eventClient,
                Encoding = encoding,
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
                Properties = new ReadOnlyDictionary<string, string?>(properties),
                MetadataWriters = metadataWriters
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
        private readonly IPubSubShadowEventClientSelector _eventClients;
        private Dictionary<string, PubSubShadowEgressSettings> _settings =
            new(StringComparer.Ordinal);
    }

    /// <summary>
    /// Explicit registration for event-client egress.
    /// </summary>
    internal sealed class PubSubShadowEgressRegistration : IDisposable, IAsyncDisposable
    {
        public PubSubShadowEgressRegistration(
            IPubSubShadowEventClientSelector eventClients,
            PubSubShadowEgressOptions options)
        {
            EventClients = eventClients
                ?? throw new ArgumentNullException(nameof(eventClients));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Settings = new PubSubShadowEgressSettingsRegistry(EventClients);
            Tombstones = new PubSubShadowTombstoneQueue(Options);
        }

        public IPubSubShadowEventClientSelector EventClients { get; }
        public PubSubShadowEgressOptions Options { get; }
        public PubSubShadowEgressSettingsRegistry Settings { get; }
        public PubSubShadowTombstoneQueue Tombstones { get; }

        /// <summary>
        /// Drain the tombstone queue when the container is disposed
        /// synchronously.
        /// </summary>
        /// <remarks>
        /// A service provider refuses to dispose synchronously if anything it
        /// owns is async-only, so a container-owned singleton has to carry
        /// both. Blocking is safe here because the queue's teardown is
        /// ConfigureAwait(false) throughout and does not post back.
        /// </remarks>
        public void Dispose()
        {
            Tombstones.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        public ValueTask DisposeAsync()
        {
            return Tombstones.DisposeAsync();
        }
    }

    /// <summary>
    /// Retains cleanup work independently of a native host replacement. A
    /// lock-protected pending set is the durable-in-composition journal: host
    /// replacement only persists work and signals this worker, so it never
    /// waits for an egress queue while holding the host lifecycle gate.
    /// </summary>
    internal sealed class PubSubShadowTombstoneQueue : IAsyncDisposable
    {
        public PubSubShadowTombstoneQueue(PubSubShadowEgressOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeProvider = TimeProvider.System;
            _worker = Task.Run(ProcessAsync);
        }

        public int PendingCount
        {
            get
            {
                lock (_gate)
                {
                    return _pending.Count;
                }
            }
        }

        public long RetryCount => Interlocked.Read(ref _retryCount);

        /// <summary>
        /// Gets a monotonically increasing configuration generation used to
        /// invalidate cleanup from an older configuration.
        /// </summary>
        public long NextGeneration()
        {
            return Interlocked.Increment(ref _nextGeneration);
        }

        /// <summary>
        /// Persists cleanup intent without awaiting a bounded send path.
        /// Repeated intent for the same topic is coalesced to the latest
        /// settings and remains available across host stop/start.
        /// </summary>
        public void Persist(PubSubShadowEgressSettings settings, string topic,
            long generation)
        {
            ArgumentNullException.ThrowIfNull(settings);
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("A tombstone topic is required.", nameof(topic));
            }
            lock (_gate)
            {
                if (_pending.TryGetValue(topic, out var previous))
                {
                    previous.SendCancellation?.Cancel();
                }
                _pending[topic] = new PendingTombstone(settings, topic, generation,
                    _timeProvider.GetUtcNow(), _options.InitialRetryDelay);
            }
            _wake.Release();
        }

        /// <summary>
        /// Cancels pending or in-flight cleanup from an older generation and
        /// waits for its send to finish before a newly retained topic is
        /// allowed to publish.
        /// </summary>
        public async ValueTask<PubSubShadowTombstoneReactivation?> ReactivateAsync(
            string topic, long generation)
        {
            Task? inFlight = null;
            PendingTombstone? removed = null;
            lock (_gate)
            {
                if (_pending.TryGetValue(topic, out var pending)
                    && pending.Generation < generation)
                {
                    _ = _pending.Remove(topic);
                    pending.SendCancellation?.Cancel();
                    inFlight = pending.SendCompleted?.Task;
                    removed = pending.Clone();
                }
            }
            if (inFlight is not null)
            {
                await inFlight.ConfigureAwait(false);
            }
            return removed is null ? null : new PubSubShadowTombstoneReactivation(removed);
        }

        /// <summary>
        /// Restores cleanup canceled by a replacement that later rolled back.
        /// A newer journal generation always wins over the restored entry.
        /// </summary>
        public void Restore(PubSubShadowTombstoneReactivation reactivation)
        {
            ArgumentNullException.ThrowIfNull(reactivation);
            lock (_gate)
            {
                if (_pending.TryGetValue(reactivation.Entry.Topic, out var current)
                    && current.Generation > reactivation.Entry.Generation)
                {
                    return;
                }
                _pending[reactivation.Entry.Topic] = reactivation.Entry.Clone();
            }
            _wake.Release();
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _wake.Release();
            try
            {
                await _worker.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _stop.Dispose();
                _wake.Dispose();
            }
        }

        private async Task ProcessAsync()
        {
            try
            {
                while (!_stop.IsCancellationRequested)
                {
                    PendingTombstone[] due;
                    TimeSpan wait;
                    lock (_gate)
                    {
                        var now = _timeProvider.GetUtcNow();
                        due = _pending.Values
                            .Where(tombstone => tombstone.NextAttempt <= now)
                            .ToArray();
                        wait = due.Length != 0 || _pending.Count == 0
                            ? Timeout.InfiniteTimeSpan
                            : _pending.Values.Min(tombstone =>
                                tombstone.NextAttempt - now);
                    }
                    if (due.Length == 0)
                    {
                        _ = await _wake.WaitAsync(wait, _stop.Token).ConfigureAwait(false);
                        continue;
                    }

                    foreach (var tombstone in due)
                    {
                        CancellationTokenSource? sendCancellation = null;
                        lock (_gate)
                        {
                            if (!_pending.TryGetValue(tombstone.Topic, out var current)
                                || !ReferenceEquals(current, tombstone))
                            {
                                continue;
                            }
                            sendCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                                _stop.Token);
                            tombstone.SendCompleted = new TaskCompletionSource(
                                TaskCreationOptions.RunContinuationsAsynchronously);
                            tombstone.SendCancellation = sendCancellation;
                        }
                        try
                        {
                            await EventClientPubSubTransportFactory.SendMetadataTombstoneAsync(
                                tombstone.Settings.EventClient, tombstone.Settings,
                                tombstone.Topic, _timeProvider,
                                sendCancellation!.Token).ConfigureAwait(false);
                            lock (_gate)
                            {
                                if (_pending.TryGetValue(tombstone.Topic, out var current)
                                    && ReferenceEquals(current, tombstone))
                                {
                                    _ = _pending.Remove(tombstone.Topic);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Reactivation invalidated this generation.
                        }
                        catch
                        {
                            Interlocked.Increment(ref _retryCount);
                            lock (_gate)
                            {
                                if (_pending.TryGetValue(tombstone.Topic, out var current)
                                    && ReferenceEquals(current, tombstone))
                                {
                                    var delay = TimeSpan.FromTicks(Math.Min(
                                        tombstone.RetryDelay.Ticks * 2,
                                        _options.MaximumRetryDelay.Ticks));
                                    tombstone.RetryDelay = delay;
                                    tombstone.NextAttempt = _timeProvider.GetUtcNow() + delay;
                                }
                            }
                        }
                        finally
                        {
                            lock (_gate)
                            {
                                tombstone.SendCancellation = null;
                                tombstone.SendCompleted?.TrySetResult();
                                tombstone.SendCompleted = null;
                            }
                            sendCancellation!.Dispose();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        internal sealed class PendingTombstone
        {
            public PendingTombstone(PubSubShadowEgressSettings settings,
                string topic, long generation, DateTimeOffset nextAttempt,
                TimeSpan retryDelay)
            {
                Settings = settings;
                Topic = topic;
                Generation = generation;
                NextAttempt = nextAttempt;
                RetryDelay = retryDelay;
            }

            public PubSubShadowEgressSettings Settings { get; }
            public string Topic { get; }
            public long Generation { get; }
            public DateTimeOffset NextAttempt { get; set; }
            public TimeSpan RetryDelay { get; set; }
            public CancellationTokenSource? SendCancellation { get; set; }
            public TaskCompletionSource? SendCompleted { get; set; }

            public PendingTombstone Clone()
            {
                return new PendingTombstone(Settings, Topic, Generation, NextAttempt,
                    RetryDelay);
            }
        }

        private readonly Lock _gate = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly SemaphoreSlim _wake = new(0);
        private readonly Dictionary<string, PendingTombstone> _pending =
            new(StringComparer.Ordinal);
        private readonly PubSubShadowEgressOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly Task _worker;
        private long _retryCount;
        private long _nextGeneration;
    }

    internal sealed class PubSubShadowTombstoneReactivation
    {
        internal PubSubShadowTombstoneReactivation(
            PubSubShadowTombstoneQueue.PendingTombstone entry)
        {
            Entry = entry;
        }

        internal PubSubShadowTombstoneQueue.PendingTombstone Entry { get; }
    }

    /// <summary>
    /// Factory that binds native PubSub connections to the IIoT
    /// <see cref="IEventClient"/> selected for the connection's writer group.
    /// </summary>
    internal sealed class EventClientPubSubTransportFactory : IPubSubTransportFactory
    {
        public EventClientPubSubTransportFactory(string transportProfileUri,
            PubSubShadowEgressSettingsRegistry settings,
            PubSubShadowEgressOptions options)
        {
            TransportProfileUri = transportProfileUri ??
                throw new ArgumentNullException(nameof(transportProfileUri));
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
            var eventClient = settings.EventClient;
            settings = DegradeUnsupportedCapabilities(eventClient, settings,
                telemetry.CreateLogger<EventClientPubSubTransportFactory>());
            ValidateCapabilities(eventClient, settings.RequiredCapabilities);
            var metadata = CreateMetadataRouting(connection, settings);
            foreach (var route in metadata.ByTopic.Values)
            {
                ValidateCapabilities(eventClient, route.RequiredCapabilities);
            }
            var direction = connection.WriterGroups.IsNull || connection.WriterGroups.Count == 0
                ? PubSubTransportDirection.None
                : PubSubTransportDirection.Send;
            return new EventClientPubSubTransport(TransportProfileUri, direction, eventClient,
                settings, metadata, _options, timeProvider);
        }

        /// <summary>
        /// Drops capabilities the selected transport cannot express when doing
        /// so does not lose a delivery guarantee, so a transport such as IoT
        /// Hub can publish telemetry rather than refusing to start. Retain and
        /// time to live are never dropped, because a message that is not
        /// retained, or that outlives its deadline, is a real loss of function
        /// that the caller asked for explicitly.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A content type, custom properties and a schema reference annotate a
        /// message rather than delivering it, and the writer path publishes
        /// over transports that carry none of them.
        /// </para>
        /// <para>
        /// Quality of service is subtler.
        /// <see cref="EventClientCapabilities.QualityOfService"/> means the
        /// client exposes a per-message delivery setting, not that it can
        /// deliver reliably: IoT Hub is a queued and acknowledged service with
        /// no per-message knob to set. Demanding the capability therefore
        /// refused the transport the module is normally deployed with, for a
        /// guarantee that transport already provides. It is dropped with a
        /// warning naming the transport, and the message is delivered with the
        /// transport's own semantics - which is exactly what the writer path
        /// has always done.
        /// </para>
        /// </remarks>
        /// <param name="eventClient"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        internal static PubSubShadowEgressSettings DegradeUnsupportedCapabilities(
            IEventClient eventClient, PubSubShadowEgressSettings settings, ILogger logger)
        {
            if (eventClient is not IEventClientCapabilities declared)
            {
                return settings;
            }
            if (settings.Schema is not null &&
                (declared.Capabilities & EventClientCapabilities.Schema) == 0)
            {
                logger.EgressCapabilityDegraded(settings.ConnectionName, eventClient.Name,
                    EventClientCapabilities.Schema);
                settings = settings with { Schema = null };
            }
            var degradable = settings.RequiredCapabilities & ~declared.Capabilities
                & (EventClientCapabilities.ContentType
                    | EventClientCapabilities.CustomProperties
                    | EventClientCapabilities.QualityOfService);
            if (degradable == 0)
            {
                return settings;
            }
            logger.EgressCapabilityDegraded(settings.ConnectionName, eventClient.Name,
                degradable);
            return settings with
            {
                DegradedCapabilities = settings.DegradedCapabilities | degradable
            };
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

        internal static void ValidateTombstoneCapability(IEventClient eventClient)
        {
            if (eventClient is not IEventClientRetainedTombstoneCapabilities tombstones
                || !tombstones.SupportsRetainedTombstones)
            {
                throw new NotSupportedException(
                    $"The selected event client '{eventClient.Name}' cannot remove retained "
                    + "PubSub metadata. Configuration replacement is rejected rather than "
                    + "leaving stale retained metadata.");
            }
        }

        internal static async ValueTask SendMetadataTombstoneAsync(IEventClient eventClient,
            PubSubShadowEgressSettings settings, string topic, TimeProvider timeProvider,
            CancellationToken cancellationToken)
        {
            ValidateTombstoneCapability(eventClient);
            ValidateCapabilities(eventClient, EventClientCapabilities.Payload
                | EventClientCapabilities.Topic | EventClientCapabilities.Retain
                | EventClientCapabilities.ContentType);
            using var @event = eventClient.CreateEvent();
            var configured = @event
                .SetTimestamp(timeProvider.GetUtcNow())
                .SetTopic(topic)
                .SetContentType(settings.ContentType)
                .SetRetain(true)
                .SetQoS(settings.QualityOfService)
                .AddBuffers([ReadOnlySequence<byte>.Empty]);
            if (settings.TimeToLive.HasValue)
            {
                configured = configured.SetTtl(settings.TimeToLive.Value);
            }
            await configured.SendAsync(cancellationToken).ConfigureAwait(false);
        }

        internal static PubSubShadowMetadataRouting CreateMetadataRouting(
            PubSubConnectionDataType connection, PubSubShadowEgressSettings settings)
        {
            //
            // Metadata is retained where retaining is possible, so a late
            // subscriber immediately receives the schema for the data it is
            // about to see. That is our default, not something the caller
            // asked for, so it must not become a requirement the transport has
            // to satisfy: demanding it refused IoT Hub outright, which has no
            // retain concept at all and simply ignores the flag - which is
            // exactly what the writer path relies on when it publishes
            // metadata over the same transport.
            //
            // An explicitly configured retain still flows through below and is
            // still refused when it cannot be honoured, because that one the
            // caller did ask for and silently dropping it loses function.
            //
            var defaultRetain = settings.EventClient is IEventClientCapabilities declared
                && (declared.Capabilities & EventClientCapabilities.Retain) != 0;
            var byTopic = new Dictionary<string, PubSubShadowEgressSettings>(
                StringComparer.Ordinal);
            var byWriter = new Dictionary<(ushort WriterGroupId, ushort DataSetWriterId), string>();
            var suppressed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in connection.WriterGroups)
            {
                foreach (var writer in group.DataSetWriters)
                {
                    var configured = settings.MetadataWriters.SingleOrDefault(candidate =>
                        string.Equals(candidate.WriterName, writer.Name, StringComparison.Ordinal));
                    var topic = configured?.Publishing?.QueueName;
                    if (string.IsNullOrWhiteSpace(topic))
                    {
                        topic = string.Concat(settings.Topic.TrimEnd('/'), "/metadata/",
                            group.WriterGroupId, "/", writer.DataSetWriterId);
                    }
                    var metadataSettings = settings.WithTransportSettings(topic,
                        configured?.Publishing, defaultRetain);
                    if (!byTopic.TryAdd(topic, metadataSettings))
                    {
                        throw new InvalidOperationException(
                            $"Metadata topic '{topic}' is configured more than once.");
                    }
                    byWriter.Add((group.WriterGroupId, writer.DataSetWriterId), topic);
                    if (configured is not null && !configured.Enabled)
                    {
                        //
                        // The topic is still resolved so the runtime can address
                        // the writer, but nothing is published to it.
                        //
                        _ = suppressed.Add(topic);
                    }
                }
            }
            return new PubSubShadowMetadataRouting(byTopic, byWriter, suppressed);
        }

        private readonly PubSubShadowEgressSettingsRegistry _settings;
        private readonly PubSubShadowEgressOptions _options;
    }

    internal sealed class PubSubShadowMetadataRouting
    {
        public PubSubShadowMetadataRouting(
            IReadOnlyDictionary<string, PubSubShadowEgressSettings> byTopic,
            IReadOnlyDictionary<(ushort WriterGroupId, ushort DataSetWriterId), string> byWriter,
            IReadOnlySet<string>? suppressed = null)
        {
            ByTopic = byTopic ?? throw new ArgumentNullException(nameof(byTopic));
            ByWriter = byWriter ?? throw new ArgumentNullException(nameof(byWriter));
            Suppressed = suppressed ?? new HashSet<string>(StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, PubSubShadowEgressSettings> ByTopic { get; }
        public IReadOnlyDictionary<(ushort WriterGroupId, ushort DataSetWriterId), string> ByWriter { get; }

        /// <summary>
        /// Metadata topics belonging to writers whose metadata is disabled.
        /// </summary>
        public IReadOnlySet<string> Suppressed { get; }
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
            : this(transportProfileUri, direction, eventClient, settings,
                new PubSubShadowMetadataRouting(
                    new Dictionary<string, PubSubShadowEgressSettings>(StringComparer.Ordinal),
                    new Dictionary<(ushort WriterGroupId, ushort DataSetWriterId), string>()),
                options, timeProvider)
        {
        }

        public EventClientPubSubTransport(string transportProfileUri,
            PubSubTransportDirection direction, IEventClient eventClient,
            PubSubShadowEgressSettings settings, PubSubShadowMetadataRouting metadata,
            PubSubShadowEgressOptions options, TimeProvider timeProvider)
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
            if (options.MaxSendAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options),
                    "The maximum number of send attempts must be positive.");
            }

            TransportProfileUri = transportProfileUri ??
                throw new ArgumentNullException(nameof(transportProfileUri));
            Direction = direction;
            _eventClient = eventClient ?? throw new ArgumentNullException(nameof(eventClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public string TransportProfileUri { get; }

        public PubSubTransportDirection Direction { get; }

        public bool IsConnected => Volatile.Read(ref _active) is not null;

        public event EventHandler<PubSubTransportStateChangedEventArgs>? StateChanged;

        public PubSubShadowEgressMetrics Metrics
        {
            get
            {
                var generation = Volatile.Read(ref _active);
                return new PubSubShadowEgressMetrics
                {
                    QueueDepth = generation?.Outbound.Reader.Count ?? 0,
                    BackpressureCount = Interlocked.Read(ref _backpressureCount),
                    OverflowCount = Interlocked.Read(ref _overflowCount),
                    RetryCount = Interlocked.Read(ref _retryCount),
                    SentCount = Interlocked.Read(ref _sentCount),
                    FailedCount = Interlocked.Read(ref _failedCount),
                    ChunkCount = Interlocked.Read(ref _chunkCount)
                };
            }
        }

        public async ValueTask OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (true)
            {
                Task closing;
                Task? notification = null;
                await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (_disposed != 0)
                    {
                        throw new ObjectDisposedException(nameof(EventClientPubSubTransport));
                    }
                    if (_active is not null)
                    {
                        return;
                    }
                    closing = _closingTask;
                    if (closing.IsCompletedSuccessfully)
                    {
                        var generation = new EgressGeneration(_options.QueueCapacity);
                        generation.SendLoop = Task.Run(() => ProcessAsync(generation));
                        Volatile.Write(ref _active, generation);
                        notification = QueueStateChanged(generation, true,
                            "Event-client PubSub egress transport opened.");
                    }
                }
                finally
                {
                    _lifecycleGate.Release();
                }
                if (notification is not null)
                {
                    await notification.ConfigureAwait(false);
                    return;
                }
                await closing.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EgressGeneration? generation;
            Task closing;
            TaskCompletionSource? closeStart = null;
            await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                generation = _active;
                if (generation is not null)
                {
                    Volatile.Write(ref _active, null);
                    closeStart = new TaskCompletionSource(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    closing = CloseGenerationAsync(generation, closeStart.Task, notify: true);
                    _closingTask = closing;
                }
                else
                {
                    closing = _closingTask;
                }
            }
            finally
            {
                _lifecycleGate.Release();
            }
            closeStart?.TrySetResult();
            await closing.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (generation is not null)
            {
                await generation.StateNotification.WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async ValueTask SendAsync(ReadOnlyMemory<byte> payload, string? topic = null,
            CancellationToken cancellationToken = default)
        {
            var generation = Volatile.Read(ref _active);
            if (generation is null)
            {
                throw new InvalidOperationException("The event-client PubSub transport is not open.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            var resolvedTopic = string.IsNullOrWhiteSpace(topic) ? _settings.Topic : topic!;
            if (_metadata.Suppressed.Contains(resolvedTopic))
            {
                //
                // The configuration disabled this writer's dataset metadata.
                // The native runtime announces on its own schedule and has no
                // per-writer switch, so the announcement is dropped here.
                //
                return;
            }
            var settings = _metadata.ByTopic.TryGetValue(resolvedTopic, out var metadata)
                ? metadata
                : _settings;
            var frame = new PendingFrame(payload.ToArray(), resolvedTopic, settings,
                cancellationToken);
            if (!generation.TryWrite(frame, out var accepting))
            {
                if (!accepting)
                {
                    throw new OperationCanceledException(
                        "The event-client PubSub transport closed while queuing the frame.",
                        null, generation.CancellationToken);
                }
                if (_options.OverflowPolicy == PubSubShadowEgressOverflowPolicy.Reject)
                {
                    Interlocked.Increment(ref _overflowCount);
                    throw new InvalidOperationException(
                        "The bounded event-client PubSub egress queue rejected a frame.");
                }
                Interlocked.Increment(ref _backpressureCount);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, generation.CancellationToken);
                try
                {
                    await generation.Outbound.Writer.WriteAsync(frame, linked.Token)
                        .ConfigureAwait(false);
                }
                catch (ChannelClosedException exception)
                    when (generation.IsStopping)
                {
                    throw new OperationCanceledException(
                        "The event-client PubSub transport closed while queuing the frame.",
                        exception, generation.CancellationToken);
                }
            }
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
            Task closing;
            TaskCompletionSource? closeStart = null;
            await _lifecycleGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed != 0)
                {
                    return;
                }
                _disposed = 1;
                var generation = _active;
                Volatile.Write(ref _active, null);
                if (generation is not null)
                {
                    closeStart = new TaskCompletionSource(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    closing = CloseGenerationAsync(generation, closeStart.Task, notify: false);
                    _closingTask = closing;
                }
                else
                {
                    closing = _closingTask;
                }
            }
            finally
            {
                _lifecycleGate.Release();
            }
            closeStart?.TrySetResult();
            await closing.ConfigureAwait(false);
        }

        public string BuildMetaDataTopic(PublisherId publisherId, ushort writerGroupId,
            ushort dataSetWriterId)
        {
            if (_metadata.ByWriter.TryGetValue((writerGroupId, dataSetWriterId), out var topic))
            {
                return topic;
            }
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

        private async Task ProcessAsync(EgressGeneration generation)
        {
            try
            {
                await foreach (var frame in generation.Outbound.Reader
                    .ReadAllAsync(generation.CancellationToken)
                    .ConfigureAwait(false))
                {
                    try
                    {
                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                            generation.CancellationToken, frame.CancellationToken);
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
                while (generation.Outbound.Reader.TryRead(out var frame))
                {
                    frame.Completion.TrySetCanceled(generation.CancellationToken);
                }
            }
        }

        private async Task CloseGenerationAsync(EgressGeneration generation, Task start,
            bool notify)
        {
            await start.ConfigureAwait(false);
            try
            {
                generation.StopAccepting();
                await generation.SendLoop.ConfigureAwait(false);
            }
            finally
            {
                generation.Dispose();
            }
            if (notify)
            {
                generation.StateNotification = QueueStateChanged(generation, false,
                    "Event-client PubSub egress transport closed.");
            }
        }

        private Task QueueStateChanged(EgressGeneration generation, bool isConnected,
            string reason)
        {
            lock (_stateNotificationGate)
            {
                _stateNotification = _stateNotification.ContinueWith(previous =>
                {
                    if (previous.IsFaulted)
                    {
                        _ = previous.Exception;
                    }
                    if (!isConnected
                        || ReferenceEquals(Volatile.Read(ref _active), generation))
                    {
                        StateChanged?.Invoke(this, new PubSubTransportStateChangedEventArgs(
                            isConnected, StatusCodes.Good, reason));
                    }
                }, CancellationToken.None, TaskContinuationOptions.None,
                    TaskScheduler.Default);
                return _stateNotification;
            }
        }

        private async ValueTask SendFrameAsync(PendingFrame frame, CancellationToken cancellationToken)
        {
            if (frame.Payload.Length == 0)
            {
                //
                // The shadow encoders return an empty buffer for a network
                // message that carries no field, because the native runtime
                // samples on its own timer and produces one whether or not the
                // sources had data. Publishing it would put an empty key frame
                // on the wire that the writer path never emits.
                //
                return;
            }
            var maximum = _eventClient.MaxEventPayloadSizeInBytes;
            if (maximum <= 0)
            {
                throw new InvalidOperationException(
                    $"The selected event client '{_eventClient.Name}' has an invalid maximum payload size.");
            }

            var payload = CompressIfRequired(frame.Payload, frame.Settings.Encoding);
            if (payload.Length > maximum)
            {
                throw new PubSubShadowPayloadTooLargeException(payload.Length, maximum);
            }
            await SendChunkWithRetryAsync(payload, frame.Topic, frame.Settings, cancellationToken)
                .ConfigureAwait(false);
            Interlocked.Increment(ref _chunkCount);
            Interlocked.Increment(ref _sentCount);
        }

        private async ValueTask SendChunkWithRetryAsync(ReadOnlyMemory<byte> payload,
            string topic, PubSubShadowEgressSettings settings,
            CancellationToken cancellationToken)
        {
            var delay = _options.InitialRetryDelay;
            for (var attempt = 1; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await SendChunkAsync(payload, topic, settings, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception) when (IsTerminal(exception))
                {
                    throw new PubSubShadowTerminalEgressException(exception);
                }
                catch (Exception exception)
                {
                    if (attempt >= _options.MaxSendAttempts)
                    {
                        throw new PubSubShadowRetryLimitExceededException(attempt, exception);
                    }
                    Interlocked.Increment(ref _retryCount);
                    await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
                    var nextTicks = Math.Min(delay.Ticks * 2, _options.MaximumRetryDelay.Ticks);
                    delay = TimeSpan.FromTicks(nextTicks);
                }
            }
        }

        private static bool IsTerminal(Exception exception)
        {
            return exception is AuthenticationException
                or UnauthorizedAccessException
                or ArgumentException
                or NotSupportedException
                or PubSubShadowPayloadTooLargeException;
        }

        private async ValueTask SendChunkAsync(ReadOnlyMemory<byte> payload, string topic,
            PubSubShadowEgressSettings settings, CancellationToken cancellationToken)
        {
            using var @event = _eventClient.CreateEvent();
            var configured = @event
                .SetTimestamp(_timeProvider.GetUtcNow())
                .SetTopic(topic)
                .SetContentType(settings.ContentType)
                .SetContentEncoding(settings.ContentEncoding)
                .SetQoS(settings.QualityOfService)
                .SetRetain(settings.Retain);
            if (settings.TimeToLive.HasValue)
            {
                configured = configured.SetTtl(settings.TimeToLive.Value);
            }
            foreach (var property in settings.Properties)
            {
                configured = configured.AddProperty(property.Key, property.Value);
            }
            if (settings.UseCloudEvents)
            {
                configured = configured.AsCloudEvent(new CloudEventHeader
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Source = settings.CloudEventSource,
                    Type = settings.CloudEventType,
                    Subject = settings.CloudEventSubject,
                    Time = _timeProvider.GetUtcNow(),
                    DataContentType = settings.ContentType
                });
            }
            if (settings.Schema is not null)
            {
                configured = configured.SetSchema(settings.Schema);
            }
            configured = configured.AddBuffers([new ReadOnlySequence<byte>(payload)]);
            await configured.SendAsync(cancellationToken).ConfigureAwait(false);
        }

        private static ReadOnlyMemory<byte> CompressIfRequired(ReadOnlyMemory<byte> payload,
            PubSubShadowEncoding encoding)
        {
            if (encoding is not (PubSubShadowEncoding.JsonGzip
                or PubSubShadowEncoding.JsonReversibleGzip))
            {
                return payload;
            }
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, true))
            {
                gzip.Write(payload.Span);
            }
            return output.ToArray();
        }

        private sealed class EgressGeneration : IDisposable
        {
            public EgressGeneration(int capacity)
            {
                Outbound = Channel.CreateBounded<PendingFrame>(
                    new BoundedChannelOptions(capacity)
                    {
                        FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = true,
                        SingleWriter = false,
                        AllowSynchronousContinuations = false
                    });
                CancellationToken = _stop.Token;
            }

            public Channel<PendingFrame> Outbound { get; }

            public CancellationToken CancellationToken { get; }

            public bool IsStopping => Volatile.Read(ref _stopping) != 0;

            public Task SendLoop { get; set; } = Task.CompletedTask;

            public Task StateNotification { get; set; } = Task.CompletedTask;

            public bool TryWrite(PendingFrame frame, out bool accepting)
            {
                lock (_gate)
                {
                    accepting = _accepting;
                    return accepting && Outbound.Writer.TryWrite(frame);
                }
            }

            public void StopAccepting()
            {
                var stop = false;
                lock (_gate)
                {
                    if (_accepting)
                    {
                        _accepting = false;
                        Volatile.Write(ref _stopping, 1);
                        stop = true;
                    }
                }
                if (stop)
                {
                    _stop.Cancel();
                    _ = Outbound.Writer.TryComplete();
                }
            }

            public void Dispose()
            {
                _stop.Dispose();
            }

            private readonly Lock _gate = new();
            private readonly CancellationTokenSource _stop = new();
            private bool _accepting = true;
            private int _stopping;
        }

        private sealed class PendingFrame
        {
            public PendingFrame(byte[] payload, string topic,
                PubSubShadowEgressSettings settings,
                CancellationToken cancellationToken)
            {
                Payload = payload;
                Topic = topic;
                Settings = settings;
                CancellationToken = cancellationToken;
            }

            public byte[] Payload { get; }
            public string Topic { get; }
            public PubSubShadowEgressSettings Settings { get; }
            public CancellationToken CancellationToken { get; }
            public TaskCompletionSource Completion { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly IEventClient _eventClient;
        private readonly PubSubShadowEgressSettings _settings;
        private readonly PubSubShadowMetadataRouting _metadata;
        private readonly PubSubShadowEgressOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        private readonly Lock _stateNotificationGate = new();
        private Task _closingTask = Task.CompletedTask;
        private Task _stateNotification = Task.CompletedTask;
        private EgressGeneration? _active;
        private int _disposed;
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

    /// <summary>
    /// A native PubSub frame cannot be split at arbitrary byte offsets:
    /// JSON and UADP fragments would not be independently decodable. Until
    /// the stack publishes a protocol-aware chunking contract, reject the
    /// encoded frame before it reaches the event client.
    /// </summary>
    internal sealed class PubSubShadowPayloadTooLargeException : InvalidOperationException
    {
        public PubSubShadowPayloadTooLargeException(int payloadSize, int maximumSize)
            : base($"The encoded PubSub frame is {payloadSize} bytes but the selected "
                + $"event client permits {maximumSize} bytes. Arbitrary PubSub "
                + "byte slicing is not supported.")
        {
            PayloadSize = payloadSize;
            MaximumSize = maximumSize;
        }

        public int PayloadSize { get; }
        public int MaximumSize { get; }
    }

    internal sealed class PubSubShadowTerminalEgressException : InvalidOperationException
    {
        public PubSubShadowTerminalEgressException(Exception innerException)
            : base("The selected event client rejected a PubSub frame permanently.",
                innerException)
        {
        }
    }

    internal sealed class PubSubShadowRetryLimitExceededException : InvalidOperationException
    {
        public PubSubShadowRetryLimitExceededException(int attempts, Exception innerException)
            : base($"The selected event client did not send a PubSub frame after {attempts} "
                + "transient attempts.", innerException)
        {
            Attempts = attempts;
        }

        public int Attempts { get; }
    }

    /// <summary>
    /// Source-generated logging definitions for the native PubSub egress.
    /// </summary>
    internal static partial class EventClientPubSubTransportLogging
    {
        private const int EventClass = 760;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Warning,
            Message = "Native PubSub connection {Connection} publishes without " +
            "{Capability} because the selected event client {Transport} cannot " +
            "carry it. Telemetry and delivery guarantees are unaffected.")]
        public static partial void EgressCapabilityDegraded(this ILogger logger,
            string connection, string transport, EventClientCapabilities capability);
    }
}
