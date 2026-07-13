// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// An individual source notification retained for managed PubSub
    /// processing. The instance owns a deep copy of its payload.
    /// </summary>
    public sealed class ManagedPubSubNotification
    {
        /// <summary>
        /// Initializes a notification with an owned copy of
        /// <paramref name="payload"/>.
        /// </summary>
        /// <param name="dataSetName">Published dataset name.</param>
        /// <param name="fieldName">Dataset field name.</param>
        /// <param name="timestamp">Source timestamp.</param>
        /// <param name="payload">Notification payload to copy.</param>
        public ManagedPubSubNotification(string dataSetName, string fieldName,
            DateTimeOffset timestamp, ReadOnlySpan<byte> payload)
        {
            DataSetName = string.IsNullOrWhiteSpace(dataSetName)
                ? throw new ArgumentException("The dataset name must not be empty.", nameof(dataSetName))
                : dataSetName;
            FieldName = string.IsNullOrWhiteSpace(fieldName)
                ? throw new ArgumentException("The field name must not be empty.", nameof(fieldName))
                : fieldName;
            Timestamp = timestamp;
            _payload = payload.ToArray();
        }

        /// <summary>
        /// Gets the published dataset name.
        /// </summary>
        public string DataSetName { get; }

        /// <summary>
        /// Gets the dataset field name.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Gets the source timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the owned payload. Consumers must treat this memory as immutable.
        /// </summary>
        public ReadOnlyMemory<byte> Payload => _payload;

        /// <summary>
        /// Creates another notification with its own payload copy.
        /// </summary>
        /// <returns>A deep copy of this notification.</returns>
        public ManagedPubSubNotification Clone()
        {
            return new ManagedPubSubNotification(DataSetName, FieldName, Timestamp, _payload);
        }

        private readonly byte[] _payload;
    }

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
        /// <param name="dataSet">Public Publisher dataset model.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The managed source, if the provider can create one.</returns>
        ValueTask<IManagedPubSubDataSource?> CreateAsync(PublishedDataSetModel dataSet,
            CancellationToken cancellationToken = default);
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

        public int QueueDepth => Volatile.Read(ref _queueDepth);

        public long BackpressureCount => Interlocked.Read(ref _backpressureCount);

        public async ValueTask EnqueueAsync(ManagedPubSubNotification notification,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);
            var copy = notification.Clone();
            if (!_channel.Writer.TryWrite(copy))
            {
                Interlocked.Increment(ref _backpressureCount);
                await _channel.Writer.WriteAsync(copy, cancellationToken).ConfigureAwait(false);
            }
            Interlocked.Increment(ref _queueDepth);
        }

        public async IAsyncEnumerable<ManagedPubSubNotification> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var notification in _channel.Reader.ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                Interlocked.Decrement(ref _queueDepth);
                yield return notification.Clone();
            }
        }

        private readonly Channel<ManagedPubSubNotification> _channel;
        private int _queueDepth;
        private long _backpressureCount;
    }
}
