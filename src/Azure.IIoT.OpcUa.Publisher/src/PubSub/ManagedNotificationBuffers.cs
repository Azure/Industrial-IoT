// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.PubSub.DataSets;
    using Opc.Ua.PubSub.Encoding;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using UaDataSetFieldContentMask = Opc.Ua.DataSetFieldContentMask;

    /// <summary>
    /// Managed source notification kind.
    /// </summary>
    public enum ManagedPubSubNotificationKind
    {
        Data,
        Event,
        Condition,

        /// <summary>
        /// A constant field the writer contributes to every payload, such as a
        /// configured extension field or the endpoint and application uri the
        /// full featured profile appends. It updates retained state but never
        /// produces a message of its own.
        /// </summary>
        Extension,

        /// <summary>
        /// The dataset metadata the writer resolved from the server. It
        /// replaces the description the source would otherwise infer from the
        /// values it has observed, and never produces a message of its own.
        /// </summary>
        MetaData
    }

    /// <summary>
    /// An individual source notification retained for managed PubSub
    /// processing.
    /// </summary>
    public sealed class ManagedPubSubNotification
    {
        /// <summary>
        /// Initializes a notification carrying a typed OPC UA value.
        /// </summary>
        /// <param name="dataSetName">Published dataset name.</param>
        /// <param name="fieldName">Dataset field name.</param>
        /// <param name="timestamp">Source timestamp.</param>
        /// <param name="value">Notification value.</param>
        /// <param name="kind">Notification kind.</param>
        public ManagedPubSubNotification(string dataSetName, string fieldName,
            DateTimeOffset timestamp, DataValue value,
            ManagedPubSubNotificationKind kind = ManagedPubSubNotificationKind.Data)
            : this(dataSetName, timestamp, kind,
                [new ManagedPubSubField(string.IsNullOrWhiteSpace(fieldName)
                    ? throw new ArgumentException("The field name must not be empty.",
                        nameof(fieldName))
                    : fieldName, value)])
        {
        }

        /// <summary>
        /// Initializes a notification carrying all fields of a single
        /// occurrence. An occurrence is the unit the writer path emits as one
        /// message, so the fields of one notification must never be split
        /// across messages: for an event or a condition snapshot the field set
        /// <em>is</em> the occurrence, and for data it is one publish.
        /// </summary>
        /// <param name="dataSetName">Published dataset name.</param>
        /// <param name="timestamp">Source timestamp.</param>
        /// <param name="kind">Notification kind.</param>
        /// <param name="fields">The fields of the occurrence.</param>
        public ManagedPubSubNotification(string dataSetName, DateTimeOffset timestamp,
            ManagedPubSubNotificationKind kind, IReadOnlyList<ManagedPubSubField> fields,
            PubSubDataSetMessageType frame = PubSubDataSetMessageType.KeyFrame)
        {
            ArgumentNullException.ThrowIfNull(fields);
            DataSetName = string.IsNullOrWhiteSpace(dataSetName)
                ? throw new ArgumentException("The dataset name must not be empty.", nameof(dataSetName))
                : dataSetName;
            if (fields.Count == 0)
            {
                throw new ArgumentException("The occurrence must carry at least one field.",
                    nameof(fields));
            }
            Fields = fields;
            Timestamp = timestamp;
            Kind = kind;
            Frame = frame;
        }

        /// <summary>
        /// Initializes a notification carrying an opaque payload. The payload
        /// is copied and surfaced as a byte string value.
        /// </summary>
        /// <param name="dataSetName">Published dataset name.</param>
        /// <param name="fieldName">Dataset field name.</param>
        /// <param name="timestamp">Source timestamp.</param>
        /// <param name="payload">Notification payload to copy.</param>
        /// <param name="kind">Notification kind.</param>
        public ManagedPubSubNotification(string dataSetName, string fieldName,
            DateTimeOffset timestamp, ReadOnlySpan<byte> payload,
            ManagedPubSubNotificationKind kind = ManagedPubSubNotificationKind.Data)
            : this(dataSetName, fieldName, timestamp,
                new DataValue(new Variant(payload.ToArray()), StatusCodes.Good,
                    DateTimeUtc.From(timestamp)), kind)
        {
        }

        /// <summary>
        /// Initializes a notification carrying the dataset metadata the writer
        /// resolved from the server.
        /// </summary>
        /// <param name="dataSetName">Published dataset name.</param>
        /// <param name="metaData">Resolved dataset metadata.</param>
        public ManagedPubSubNotification(string dataSetName, DataSetMetaDataType metaData)
        {
            ArgumentNullException.ThrowIfNull(metaData);
            DataSetName = string.IsNullOrWhiteSpace(dataSetName)
                ? throw new ArgumentException("The dataset name must not be empty.", nameof(dataSetName))
                : dataSetName;
            Kind = ManagedPubSubNotificationKind.MetaData;
            ResolvedMetaData = metaData;
        }

        /// <summary>
        /// Gets the dataset metadata the writer resolved from the server, when
        /// this notification carries it.
        /// </summary>
        public DataSetMetaDataType? ResolvedMetaData { get; }

        /// <summary>
        /// Gets the published dataset name.
        /// </summary>
        public string DataSetName { get; }

        /// <summary>
        /// Gets the fields of this occurrence.
        /// </summary>
        public IReadOnlyList<ManagedPubSubField> Fields { get; }
            = Array.Empty<ManagedPubSubField>();

        /// <summary>
        /// Gets the name of the first field of the occurrence.
        /// </summary>
        public string FieldName => Fields.Count == 0 ? string.Empty : Fields[0].Name;

        /// <summary>
        /// Gets the source timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets whether the notification updates current data state or is an
        /// individual event/condition occurrence.
        /// </summary>
        public ManagedPubSubNotificationKind Kind { get; }

        /// <summary>
        /// Gets the kind of message this occurrence is published as. The
        /// subscription already knows whether it produced a key frame, a delta
        /// or an event, so the native runtime is told rather than left to
        /// derive it by comparing unrelated occurrences.
        /// </summary>
        public PubSubDataSetMessageType Frame { get; }
            = PubSubDataSetMessageType.KeyFrame;

        /// <summary>
        /// Gets the value of the first field of the occurrence. Consumers must
        /// treat it as immutable.
        /// </summary>
        public DataValue Value => Fields.Count == 0 ? new DataValue() : Fields[0].Value;

        /// <summary>
        /// Creates another notification referring to the same values.
        /// </summary>
        /// <returns>A copy of this notification.</returns>
        public ManagedPubSubNotification Clone()
        {
            return _barrier is null
                ? ResolvedMetaData is { } metaData
                    ? new ManagedPubSubNotification(DataSetName, metaData)
                    : new ManagedPubSubNotification(DataSetName, Timestamp, Kind, Fields, Frame)
                : new ManagedPubSubNotification(DataSetName, _barrier);
        }

        internal static ManagedPubSubNotification CreateBarrier(string dataSetName,
            TaskCompletionSource barrier)
        {
            return new ManagedPubSubNotification(dataSetName, barrier);
        }

        internal bool IsBarrier => _barrier is not null;

        internal void CompleteBarrier()
        {
            _barrier?.TrySetResult();
        }

        private ManagedPubSubNotification(string dataSetName, TaskCompletionSource barrier)
        {
            DataSetName = dataSetName;
            Timestamp = default;
            _barrier = barrier;
        }

        private readonly TaskCompletionSource? _barrier;
    }

    /// <summary>
    /// A single field of a managed PubSub notification.
    /// </summary>
    /// <param name="Name">Dataset field name.</param>
    /// <param name="Value">Field value. Consumers must treat it as immutable.</param>
    public sealed record class ManagedPubSubField(string Name, DataValue Value);

    /// <summary>
    /// An ordered buffer for individual managed PubSub notifications. The
    /// buffer must retain intermediate notifications and may not collapse
    /// them to their latest value.
    /// </summary>
    public interface IManagedPubSubNotificationBuffer
    {
        /// <summary>
        /// Appends an individual notification to the buffer.
        /// </summary>
        /// <param name="notification">Notification to append.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes once the notification is buffered.</returns>
        ValueTask EnqueueAsync(ManagedPubSubNotification notification,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads buffered notifications in append order.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The sequence of individual buffered notifications.</returns>
        IAsyncEnumerable<ManagedPubSubNotification> ReadAllAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Reports bounded managed-notification queue health.
    /// </summary>
    public interface IManagedPubSubNotificationBufferDiagnostics
    {
        /// <summary>
        /// Gets the current queued notification count.
        /// </summary>
        int QueueDepth { get; }

        /// <summary>
        /// Gets the number of producers delayed by bounded-queue backpressure.
        /// </summary>
        long BackpressureCount { get; }
    }

    /// <summary>
    /// Configures the lossless bounded notification queue.
    /// </summary>
    public sealed class ManagedPubSubNotificationBufferOptions
    {
        /// <summary>
        /// Gets or sets the maximum queued notifications.
        /// </summary>
        public int Capacity { get; set; } = 1024;
    }

    /// <summary>
    /// Marker for a notification buffer whose entries represent source events.
    /// Event entries retain every occurrence, including repeated values.
    /// </summary>
    public interface IManagedPubSubEventBuffer : IManagedPubSubNotificationBuffer
    {
    }

    /// <summary>
    /// Provides a managed data source that can preserve each incoming
    /// notification until a later managed adapter consumes it.
    /// </summary>
    public interface IManagedPubSubDataSource
    {
        /// <summary>
        /// Gets the dataset metadata declared by the source, or
        /// <see langword="null"/> to derive it from observed values.
        /// </summary>
        DataSetMetaDataType? MetaData => null;

        /// <summary>
        /// Reads source notifications without coalescing intermediate values.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The ordered source notification sequence.</returns>
        IAsyncEnumerable<ManagedPubSubNotification> ReadNotificationsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Creates a managed data source for a public Publisher dataset model.
    /// Native OPC UA PubSub types deliberately do not cross this seam.
    /// </summary>
    public interface IManagedPubSubDataSourceProvider
    {
        /// <summary>
        /// Creates a source for a dataset, or returns <see langword="null"/>
        /// when the provider does not own that dataset.
        /// </summary>
        /// <remarks>
        /// The name is supplied by the caller rather than derived from
        /// <paramref name="dataSet"/>, because a dataset need not be named and
        /// the registry falls back to the writer group and writer identifiers.
        /// Deriving it twice would let the two disagree and silently route
        /// notifications to a source that does not exist.
        /// </remarks>
        /// <param name="dataSetName">Resolved dataset name.</param>
        /// <param name="dataSet">Public Publisher dataset model.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The managed source, if the provider can create one.</returns>
        ValueTask<IManagedPubSubDataSource?> CreateAsync(string dataSetName,
            PublishedDataSetModel dataSet, CancellationToken cancellationToken = default);
    }

    internal interface IManagedPubSubDataSourceLifecycle
    {
        ValueTask RemoveAsync(string dataSetName,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Test seam for deterministically observing the data publication
    /// boundary. Production does not register an observer.
    /// </summary>
    internal interface IManagedPubSubDataPublicationObserver
    {
        void AfterSequenceAllocated(long sequence);
    }

    internal sealed class ManagedPubSubNotificationBuffer :
        IManagedPubSubNotificationBuffer, IManagedPubSubEventBuffer,
        IManagedPubSubNotificationBufferDiagnostics
    {
        public ManagedPubSubNotificationBuffer(int capacity = 1024)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            _channel = Channel.CreateBounded<ManagedPubSubNotification>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    AllowSynchronousContinuations = false,
                    SingleReader = false,
                    SingleWriter = false
                });
        }

        public int QueueDepth => _channel.Reader.Count;

        public long BackpressureCount => Interlocked.Read(ref _backpressureCount);

        public async ValueTask EnqueueAsync(ManagedPubSubNotification notification,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);
            var copy = notification.Clone();
            if (!_channel.Writer.TryWrite(copy))
            {
                Interlocked.Increment(ref _backpressureCount);
                await _channel.Writer.WriteAsync(copy, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async IAsyncEnumerable<ManagedPubSubNotification> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var notification in _channel.Reader.ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return notification.Clone();
            }
        }

        private readonly Channel<ManagedPubSubNotification> _channel;
        private long _backpressureCount;
    }

    /// <summary>
    /// Adapts the ordered notification buffer to one managed source per
    /// published data set. The router is the sole buffer reader, so a burst
    /// cannot be coalesced by multiple consumers racing to read the channel.
    /// </summary>
    internal sealed class ManagedPubSubNotificationDataSourceProvider :
        IManagedPubSubDataSourceProvider, IManagedPubSubDataSourceLifecycle,
        IAsyncDisposable
    {
        public ManagedPubSubNotificationDataSourceProvider(
            IManagedPubSubNotificationBuffer notifications,
            IOptions<ManagedPubSubNotificationBufferOptions>? options = null)
        {
            _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            var capacity = options?.Value.Capacity ?? 1024;
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            _capacity = capacity;
        }

        public ValueTask<IManagedPubSubDataSource?> CreateAsync(string dataSetName,
            PublishedDataSetModel dataSet, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dataSet);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(dataSetName))
            {
                return new ValueTask<IManagedPubSubDataSource?>((IManagedPubSubDataSource?)null);
            }
            if (_sources.TryGetValue(dataSetName, out var active))
            {
                return new ValueTask<IManagedPubSubDataSource?>(active);
            }
            return new ValueTask<IManagedPubSubDataSource?>(
                new RoutedDataSource(_capacity, route => Activate(dataSetName, route)));
        }

        public async ValueTask RemoveAsync(string dataSetName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RoutedDataSource? stagedRoute;
            lock (_gate)
            {
                if (_dispatch is null)
                {
                    if (_sources.TryRemove(dataSetName, out stagedRoute))
                    {
                        stagedRoute.Complete();
                    }
                    return;
                }
            }
            if (!_sources.ContainsKey(dataSetName))
            {
                return;
            }
            var barrier = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await _notifications.EnqueueAsync(
                ManagedPubSubNotification.CreateBarrier(dataSetName, barrier),
                cancellationToken).ConfigureAwait(false);
            await barrier.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            if (_dispatch is not null)
            {
                try
                {
                    await _dispatch.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
            foreach (var source in _sources.Values)
            {
                source.Complete();
            }
            _sources.Clear();
            _stop.Dispose();
        }

        private void EnsureStarted()
        {
            lock (_gate)
            {
                _dispatch ??= Task.Run(DispatchAsync);
            }
        }

        private void Activate(string dataSetName, RoutedDataSource source)
        {
            if (_sources.TryAdd(dataSetName, source))
            {
                EnsureStarted();
                return;
            }
            if (_sources.TryGetValue(dataSetName, out var existing)
                && ReferenceEquals(existing, source))
            {
                EnsureStarted();
                return;
            }
            source.Complete();
            throw new InvalidOperationException(
                $"The managed PubSub source '{dataSetName}' was activated twice.");
        }

        private async Task DispatchAsync()
        {
            await foreach (var notification in _notifications.ReadAllAsync(_stop.Token)
                .ConfigureAwait(false))
            {
                if (notification.IsBarrier)
                {
                    if (_sources.TryRemove(notification.DataSetName, out var barrierRoute))
                    {
                        barrierRoute.Complete();
                    }
                    notification.CompleteBarrier();
                    continue;
                }
                if (_sources.TryGetValue(notification.DataSetName, out var source))
                {
                    // This copy is the buffer acknowledgement point. Once this
                    // returns, the adapter owns the payload independently.
                    try
                    {
                        await source.OfferAsync(notification, _stop.Token).ConfigureAwait(false);
                    }
                    catch (ChannelClosedException)
                    {
                        // A hot replacement removed the route after lookup.
                    }
                }
            }
        }

        private sealed class RoutedDataSource : IManagedPubSubDataSource
        {
            public RoutedDataSource(int capacity, Action<RoutedDataSource> activate)
            {
                _activate = activate ?? throw new ArgumentNullException(nameof(activate));
                _notifications = Channel.CreateBounded<ManagedPubSubNotification>(
                    new BoundedChannelOptions(capacity)
                    {
                        FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = true,
                        SingleWriter = true,
                        AllowSynchronousContinuations = false
                    });
            }

            public ValueTask OfferAsync(ManagedPubSubNotification notification,
                CancellationToken cancellationToken)
            {
                return _notifications.Writer.WriteAsync(notification.Clone(), cancellationToken);
            }

            public void Complete()
            {
                _notifications.Writer.TryComplete();
            }

            public async IAsyncEnumerable<ManagedPubSubNotification> ReadNotificationsAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                _activate(this);
                await foreach (var notification in _notifications.Reader.ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    yield return notification.Clone();
                }
            }

            private readonly Channel<ManagedPubSubNotification> _notifications;
            private readonly Action<RoutedDataSource> _activate;
        }

        private readonly Lock _gate = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly IManagedPubSubNotificationBuffer _notifications;
        private readonly ConcurrentDictionary<string, RoutedDataSource> _sources =
            new(StringComparer.Ordinal);
        private readonly int _capacity;
        private Task? _dispatch;
    }

    /// <summary>
    /// Native source provider used by the shadow host. It retains each
    /// managed notification in order and returns one source notification per
    /// native sample, so event and condition bursts are not reduced to a
    /// latest-value cache.
    /// </summary>
    internal sealed class ManagedPubSubDataSetSource : IPublishedDataSetSource,
        IMetaDataChangeNotifier, IAsyncDisposable
    {
        public ManagedPubSubDataSetSource(string dataSetName,
            IManagedPubSubDataSource source, int capacity = 1024,
            IManagedPubSubDataPublicationObserver? observer = null,
            PubSubFieldEncoding fieldEncoding = PubSubFieldEncoding.DataValue)
        {
            _dataSetName = string.IsNullOrWhiteSpace(dataSetName)
                ? throw new ArgumentException("A dataset name is required.", nameof(dataSetName))
                : dataSetName;
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _observer = observer;
            _fieldEncoding = fieldEncoding;
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            _pending = Channel.CreateBounded<ManagedPendingNotification>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = false
                });
        }

        public DataSetMetaDataType BuildMetaData()
        {
            lock (_stateGate)
            {
                //
                // The writer resolves the real dataset metadata from the server
                // - resolved type identifiers, namespaces, and structure and
                // enum definitions a consumer needs to decode a structure at
                // all - and in the configured field order, which raw data set
                // encoding depends on because it is positional. What is derived
                // from observed values below is only a fallback for a source
                // that never supplies it.
                //
                if (_resolvedMetaData is { } resolved)
                {
                    return resolved;
                }
                if (_source.MetaData is { } declared)
                {
                    return declared;
                }
                return new DataSetMetaDataType
                {
                    Name = _dataSetName,
                    Fields = _knownFields
                        .OrderBy(field => field.Key, StringComparer.Ordinal)
                        .Select(field => new FieldMetaData
                        {
                            Name = field.Key,
                            BuiltInType = (byte)field.Value.BuiltInType,
                            DataType = field.Value.DataType,
                            ValueRank = field.Value.ValueRank
                        })
                        .ToArray(),
                    ConfigurationVersion = new ConfigurationVersionDataType
                    {
                        MajorVersion = 1
                    }
                };
            }
        }

        public event EventHandler? MetaDataChanged;

        public ValueTask<PublishedDataSetSnapshot> SampleAsync(
            DataSetMetaDataType metaData, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(metaData);
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.Exchange(ref _keyFrameRequested, 0) != 0)
            {
                lock (_stateGate)
                {
                    Interlocked.Exchange(ref _keyFrameWatermark,
                        Interlocked.Read(ref _sequence));
                    return new ValueTask<PublishedDataSetSnapshot>(new PublishedDataSetSnapshot(
                        metaData.ConfigurationVersion ?? new ConfigurationVersionDataType
                        {
                            MajorVersion = 1
                        },
                        SnapshotCurrentData(),
                        DateTimeUtc.From(DateTimeOffset.UtcNow),
                        PubSubDataSetMessageType.KeyFrame));
                }
            }
            while (_pending.Reader.TryRead(out var pending))
            {
                Interlocked.Decrement(ref _pendingCount);
                if (pending.Notification.Kind == ManagedPubSubNotificationKind.Data
                    && pending.Sequence <= Interlocked.Read(ref _keyFrameWatermark))
                {
                    continue;
                }

                var notification = pending.Notification;
                return new ValueTask<PublishedDataSetSnapshot>(new PublishedDataSetSnapshot(
                    metaData.ConfigurationVersion ?? new ConfigurationVersionDataType
                    {
                        MajorVersion = 1
                    },
                    //
                    // Every field of the occurrence is published as one payload.
                    // Splitting them would destroy an event or condition
                    // occurrence and would emit one message per changed value
                    // for data, where the writer path emits one per publish.
                    //
                    WithExtensionFields(notification.Fields
                        .Select(field => ToField(field.Name, new ManagedRetainedField(
                            field, notification.Timestamp, notification.Kind)))
                        .ToList()),
                    DateTimeUtc.From(notification.Timestamp),
                    //
                    // The subscription already classified this occurrence, so
                    // the runtime is told rather than left to derive a delta by
                    // comparing two unrelated occurrences positionally.
                    //
                    notification.Frame));
            }

            return new ValueTask<PublishedDataSetSnapshot>(new PublishedDataSetSnapshot(
                metaData.ConfigurationVersion ?? new ConfigurationVersionDataType
                {
                    MajorVersion = 1
                },
                [], DateTimeUtc.From(DateTimeOffset.UtcNow)));
        }

        public void Start()
        {
            lock (_gate)
            {
                _pump ??= Task.Run(PumpAsync);
            }
        }

        internal int PendingCount => Volatile.Read(ref _pendingCount);

        internal void RequestKeyFrame()
        {
            Interlocked.Exchange(ref _keyFrameRequested, 1);
        }

        internal void BeginRemoval()
        {
            Interlocked.Exchange(ref _removing, 1);
            _pendingWrite.Cancel();
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _pendingWrite.Cancel();
            _pending.Writer.TryComplete();
            if (_pump is not null)
            {
                try
                {
                    await _pump.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
            _stop.Dispose();
            _pendingWrite.Dispose();
        }

        private async Task PumpAsync()
        {
            await foreach (var notification in _source.ReadNotificationsAsync(_stop.Token)
                .ConfigureAwait(false))
            {
                var copy = notification.Clone();
                if (Volatile.Read(ref _removing) != 0)
                {
                    continue;
                }
                long sequence;
                var metadataChanged = false;
                if (copy.Kind == ManagedPubSubNotificationKind.MetaData)
                {
                    //
                    // The writer resolved the dataset metadata, so it replaces
                    // whatever was derived from observed values. It describes
                    // the dataset rather than reporting a sample and so is
                    // never queued as pending.
                    //
                    lock (_stateGate)
                    {
                        metadataChanged = !ReferenceEquals(
                            _resolvedMetaData, copy.ResolvedMetaData);
                        _resolvedMetaData = copy.ResolvedMetaData;
                    }
                    if (metadataChanged)
                    {
                        MetaDataChanged?.Invoke(this, EventArgs.Empty);
                    }
                    continue;
                }
                if (copy.Kind == ManagedPubSubNotificationKind.Extension)
                {
                    //
                    // A constant field contributes to every payload rather than
                    // producing one, so it updates retained state and metadata
                    // and is never queued as pending. Queuing it would emit a
                    // delta frame carrying only that field, which the writer
                    // path never produces.
                    //
                    lock (_stateGate)
                    {
                        foreach (var field in copy.Fields)
                        {
                            _extensionFields[field.Name] = new ManagedRetainedField(
                                field, copy.Timestamp, copy.Kind);
                            metadataChanged |= _knownFields.TryAdd(field.Name,
                                ManagedFieldType.From(field.Value));
                        }
                    }
                    if (metadataChanged)
                    {
                        MetaDataChanged?.Invoke(this, EventArgs.Empty);
                    }
                    continue;
                }
                if (copy.Kind == ManagedPubSubNotificationKind.Data)
                {
                    lock (_stateGate)
                    {
                        sequence = Interlocked.Increment(ref _sequence);
                        _observer?.AfterSequenceAllocated(sequence);
                        //
                        // Retained current data is per field even though the
                        // occurrence is the published unit, so a key frame
                        // still reports the latest value of every field.
                        //
                        foreach (var field in copy.Fields)
                        {
                            var retained = new ManagedRetainedField(field,
                                copy.Timestamp, copy.Kind);
                            _currentData.AddOrUpdate(field.Name,
                                new ManagedCurrentData(retained, sequence), (_, _) =>
                                    new ManagedCurrentData(retained, sequence));
                            metadataChanged |= _knownFields.TryAdd(field.Name,
                                ManagedFieldType.From(field.Value));
                        }
                    }
                }
                else
                {
                    sequence = Interlocked.Increment(ref _sequence);
                    lock (_stateGate)
                    {
                        foreach (var field in copy.Fields)
                        {
                            metadataChanged |= _knownFields.TryAdd(field.Name,
                                ManagedFieldType.From(field.Value));
                        }
                    }
                }
                if (metadataChanged)
                {
                    MetaDataChanged?.Invoke(this, EventArgs.Empty);
                }
                // The source owns this clone before advancing its input iterator.
                // A full pending channel backpressures the router and, in turn,
                // the bounded managed notification buffer.
                Interlocked.Increment(ref _pendingCount);
                try
                {
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                        _stop.Token, _pendingWrite.Token);
                    await _pending.Writer.WriteAsync(
                        new ManagedPendingNotification(copy, sequence), linked.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (
                    Volatile.Read(ref _removing) != 0 && !_stop.IsCancellationRequested)
                {
                    Interlocked.Decrement(ref _pendingCount);
                }
                catch
                {
                    Interlocked.Decrement(ref _pendingCount);
                    throw;
                }
            }
        }

        /// <summary>
        /// Resolves the source timestamp a field is published with. The value's
        /// own source timestamp is authoritative; the notification timestamp is
        /// only the time the sample was routed, so it is a fallback for values
        /// the server did not stamp.
        /// </summary>
        /// <param name="retained"></param>
        private static DateTimeUtc ResolveSourceTimestamp(ManagedRetainedField retained)
        {
            var sourceTimestamp = retained.Field.Value.SourceTimestamp;
            return sourceTimestamp == DateTimeUtc.MinValue
                ? DateTimeUtc.From(retained.Timestamp)
                : sourceTimestamp;
        }

        /// <summary>
        /// Appends the constant fields the writer contributes to every payload.
        /// The writer path includes them in each message, so a delta that
        /// carried only the changed fields would not reproduce it.
        /// </summary>
        /// <param name="fields">The fields of the occurrence.</param>
        private ArrayOf<DataSetField> WithExtensionFields(List<DataSetField> fields)
        {
            lock (_stateGate)
            {
                if (_extensionFields.IsEmpty)
                {
                    return [.. fields];
                }
                foreach (var name in _extensionFields.Keys.OrderBy(name => name,
                    StringComparer.Ordinal))
                {
                    if (fields.Exists(field => field.Name == name) ||
                        !_extensionFields.TryGetValue(name, out var extension))
                    {
                        continue;
                    }
                    fields.Add(ToField(name, extension));
                }
                return [.. fields];
            }
        }

        private DataSetField ToField(string name, ManagedRetainedField? retained)
        {
            return new DataSetField
            {
                Name = name,
                Value = retained is null
                    ? new Variant()
                    : retained.Field.Value.WrappedValue,
                StatusCode = retained is null
                    ? StatusCodes.Good
                    : retained.Field.Value.StatusCode,
                //
                // A constant field has no sample time, so it keeps whatever the
                // value carries and never falls back to the routing time. The
                // writer path publishes extension fields as a bare value.
                //
                SourceTimestamp = retained is null
                    ? default
                    : retained.Kind == ManagedPubSubNotificationKind.Extension
                        ? retained.Field.Value.SourceTimestamp
                        : ResolveSourceTimestamp(retained),
                ServerTimestamp = retained is null
                    ? default
                    : retained.Field.Value.ServerTimestamp,
                Encoding = _fieldEncoding
            };
        }

        private ArrayOf<DataSetField> SnapshotCurrentData()
        {
            var fields = new List<DataSetField>();
            foreach (var name in _knownFields.Keys.OrderBy(name => name,
                StringComparer.Ordinal))
            {
                //
                // A constant field is retained separately from sampled data, so
                // both maps are consulted before the field is treated as one
                // that has not been observed yet.
                //
                _ = _currentData.TryGetValue(name, out var current);
                var retained = current?.Field;
                if (retained is null)
                {
                    _ = _extensionFields.TryGetValue(name, out retained);
                }
                fields.Add(ToField(name, retained));
            }
            return [.. fields];
        }

        private readonly Lock _gate = new();
        private readonly Lock _stateGate = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly CancellationTokenSource _pendingWrite = new();
        private readonly Channel<ManagedPendingNotification> _pending;
        private readonly ConcurrentDictionary<string, ManagedCurrentData> _currentData =
            new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, ManagedRetainedField> _extensionFields =
            new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, ManagedFieldType> _knownFields =
            new(StringComparer.Ordinal);
        private readonly string _dataSetName;
        private readonly IManagedPubSubDataSource _source;
        private readonly IManagedPubSubDataPublicationObserver? _observer;
        private readonly PubSubFieldEncoding _fieldEncoding;
        private DataSetMetaDataType? _resolvedMetaData;
        private Task? _pump;
        private int _keyFrameRequested;
        private int _pendingCount;
        private int _removing;
        private long _sequence;
        private long _keyFrameWatermark;

        private sealed record class ManagedPendingNotification(
            ManagedPubSubNotification Notification, long Sequence);

        private sealed record class ManagedCurrentData(
            ManagedRetainedField Field, long Sequence);

        /// <summary>
        /// A field retained by the source, together with the routing time and
        /// kind of the occurrence it arrived on.
        /// </summary>
        private sealed record class ManagedRetainedField(ManagedPubSubField Field,
            DateTimeOffset Timestamp, ManagedPubSubNotificationKind Kind);

        private sealed record class ManagedFieldType(BuiltInType BuiltInType,
            NodeId DataType, int ValueRank)
        {
            public static ManagedFieldType From(DataValue value)
            {
                var typeInfo = value.WrappedValue.TypeInfo;
                if (typeInfo.BuiltInType == BuiltInType.Null)
                {
                    return new ManagedFieldType(BuiltInType.Variant,
                        DataTypeIds.BaseDataType, ValueRanks.Scalar);
                }
                return new ManagedFieldType(typeInfo.BuiltInType,
                    new NodeId((uint)typeInfo.BuiltInType),
                    typeInfo.ValueRank == ValueRanks.Scalar
                        ? ValueRanks.Scalar : ValueRanks.OneDimension);
            }
        }
    }

    /// <summary>
    /// Transactional source map for hot native configuration replacement.
    /// Existing sources are kept when a dataset remains configured, retaining
    /// notifications accumulated during a replace. New sources do not begin
    /// consuming until the native replacement has committed.
    /// </summary>
    internal sealed class ManagedPubSubDataSetSourceRegistry : IDataSetSourceProvider,
        IAsyncDisposable
    {
        public ManagedPubSubDataSetSourceRegistry(
            IEnumerable<IManagedPubSubDataSourceProvider> providers,
            IOptions<ManagedPubSubNotificationBufferOptions>? options = null)
        {
            _providers = providers?.ToArray()
                ?? throw new ArgumentNullException(nameof(providers));
            _capacity = options?.Value.Capacity ?? 1024;
        }

        public bool TryGetSource(string publishedDataSetName,
            out IPublishedDataSetSource source)
        {
            lock (_gate)
            {
                return _sources.TryGetValue(publishedDataSetName, out source!);
            }
        }

        public async ValueTask<ManagedPubSubDataSetSourceTransaction> PrepareAsync(
            IEnumerable<WriterGroupModel> writerGroups,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writerGroups);
            Dictionary<string, IPublishedDataSetSource> current;
            lock (_gate)
            {
                current = new Dictionary<string, IPublishedDataSetSource>(_sources,
                    StringComparer.Ordinal);
            }

            var replacement = new Dictionary<string, IPublishedDataSetSource>(
                StringComparer.Ordinal);
            var created = new List<ManagedPubSubDataSetSource>();
            var createdNames = new List<string>();
            foreach (var dataSet in EnumerateDataSets(writerGroups))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = GetDataSetName(dataSet.Group, dataSet.Writer);
                if (replacement.ContainsKey(name))
                {
                    continue;
                }
                if (current.TryGetValue(name, out var existing))
                {
                    replacement.Add(name, existing);
                    continue;
                }

                IManagedPubSubDataSource? managedSource = null;
                foreach (var provider in _providers)
                {
                    managedSource = await provider.CreateAsync(name, dataSet.Writer.DataSet!,
                        cancellationToken).ConfigureAwait(false);
                    if (managedSource is not null)
                    {
                        break;
                    }
                }
                if (managedSource is null)
                {
                    continue;
                }
                var source = new ManagedPubSubDataSetSource(name, managedSource, _capacity,
                    fieldEncoding: ResolveFieldEncoding(dataSet.Writer.DataSetFieldContentMask));
                replacement.Add(name, source);
                created.Add(source);
                createdNames.Add(name);
            }
            return new ManagedPubSubDataSetSourceTransaction(this, current, replacement, created,
                createdNames);
        }

        public async ValueTask DisposeAsync()
        {
            ManagedPubSubDataSetSource[] sources;
            lock (_gate)
            {
                sources = _sources.Values.OfType<ManagedPubSubDataSetSource>().ToArray();
                _sources.Clear();
            }
            foreach (var source in sources)
            {
                await source.DisposeAsync().ConfigureAwait(false);
            }
        }

        private void Install(ManagedPubSubDataSetSourceTransaction transaction)
        {
            lock (_gate)
            {
                _sources = new Dictionary<string, IPublishedDataSetSource>(transaction.Replacement,
                    StringComparer.Ordinal);
            }
        }

        private async ValueTask CommitAsync(ManagedPubSubDataSetSourceTransaction transaction)
        {
            foreach (var source in transaction.Created)
            {
                source.Start();
            }
            var retained = new HashSet<IPublishedDataSetSource>(transaction.Replacement.Values);
            var removed = transaction.Previous
                .Where(entry => !retained.Contains(entry.Value))
                .ToArray();
            foreach (var entry in removed)
            {
                if (entry.Value is ManagedPubSubDataSetSource source)
                {
                    source.BeginRemoval();
                }
                foreach (var provider in _providers)
                {
                    if (provider is IManagedPubSubDataSourceLifecycle lifecycle)
                    {
                        await lifecycle.RemoveAsync(entry.Key).ConfigureAwait(false);
                    }
                }
            }
            foreach (var entry in removed)
            {
                if (entry.Value is ManagedPubSubDataSetSource source)
                {
                    await source.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        internal void RequestKeyFrame()
        {
            ManagedPubSubDataSetSource[] sources;
            lock (_gate)
            {
                sources = _sources.Values.OfType<ManagedPubSubDataSetSource>().ToArray();
            }
            foreach (var source in sources)
            {
                source.RequestKeyFrame();
            }
        }

        private async ValueTask RollbackAsync(ManagedPubSubDataSetSourceTransaction transaction)
        {
            lock (_gate)
            {
                _sources = new Dictionary<string, IPublishedDataSetSource>(transaction.Previous,
                    StringComparer.Ordinal);
            }
            foreach (var name in transaction.CreatedNames)
            {
                foreach (var provider in _providers)
                {
                    if (provider is IManagedPubSubDataSourceLifecycle lifecycle)
                    {
                        await lifecycle.RemoveAsync(name).ConfigureAwait(false);
                    }
                }
            }
        }

        private static IEnumerable<(WriterGroupModel Group, DataSetWriterModel Writer)>
            EnumerateDataSets(IEnumerable<WriterGroupModel> writerGroups)
        {
            foreach (var group in writerGroups)
            {
                ArgumentNullException.ThrowIfNull(group);
                foreach (var writer in group.DataSetWriters ?? [])
                {
                    if (writer.DataSet is not null)
                    {
                        yield return (group, writer);
                    }
                }
            }
        }

        private static string GetDataSetName(WriterGroupModel group, DataSetWriterModel writer)
        {
            return writer.DataSet?.Name
                ?? writer.DataSet?.DataSetMetaData?.Name
                ?? $"{group.Id}:{writer.Id}";
        }

        /// <summary>
        /// Selects how a field is written on the wire. The native encoders take
        /// this from the field rather than the writer, so a source that leaves it
        /// unset silently emits bare variants and drops the status code and
        /// source timestamp the writer's content mask asked for.
        /// </summary>
        /// <param name="mask">Configured field content mask.</param>
        /// <returns>The field encoding to stamp on produced fields.</returns>
        internal static PubSubFieldEncoding ResolveFieldEncoding(
            DataSetFieldContentFlags? mask)
        {
            var resolved = mask.ToStackType();
            if ((resolved & UaDataSetFieldContentMask.RawData) != 0)
            {
                return PubSubFieldEncoding.RawData;
            }
            return resolved == UaDataSetFieldContentMask.None
                ? PubSubFieldEncoding.Variant
                : PubSubFieldEncoding.DataValue;
        }

        private readonly Lock _gate = new();
        private readonly IManagedPubSubDataSourceProvider[] _providers;
        private readonly int _capacity;
        private Dictionary<string, IPublishedDataSetSource> _sources =
            new(StringComparer.Ordinal);

        internal sealed class ManagedPubSubDataSetSourceTransaction : IAsyncDisposable
        {
            internal ManagedPubSubDataSetSourceTransaction(ManagedPubSubDataSetSourceRegistry owner,
                Dictionary<string, IPublishedDataSetSource> previous,
                Dictionary<string, IPublishedDataSetSource> replacement,
                List<ManagedPubSubDataSetSource> created,
                List<string> createdNames)
            {
                _owner = owner;
                Previous = previous;
                Replacement = replacement;
                Created = created;
                CreatedNames = createdNames;
            }

            internal Dictionary<string, IPublishedDataSetSource> Previous { get; }
            internal Dictionary<string, IPublishedDataSetSource> Replacement { get; }
            internal List<ManagedPubSubDataSetSource> Created { get; }
            internal List<string> CreatedNames { get; }

            public void Install()
            {
                ThrowIfCompleted();
                _owner.Install(this);
                _installed = true;
            }

            public async ValueTask CommitAsync()
            {
                ThrowIfCompleted();
                if (!_installed)
                {
                    throw new InvalidOperationException(
                        "The managed source transaction must be installed before it commits.");
                }
                await _owner.CommitAsync(this).ConfigureAwait(false);
                _completed = true;
            }

            public async ValueTask DisposeAsync()
            {
                if (_completed)
                {
                    return;
                }
                if (_installed)
                {
                    await _owner.RollbackAsync(this).ConfigureAwait(false);
                }
                foreach (var source in Created)
                {
                    await source.DisposeAsync().ConfigureAwait(false);
                }
                _completed = true;
            }

            private void ThrowIfCompleted()
            {
                if (_completed)
                {
                    throw new InvalidOperationException(
                        "The managed source transaction is complete.");
                }
            }

            private readonly ManagedPubSubDataSetSourceRegistry _owner;
            private bool _installed;
            private bool _completed;
        }
    }
}
