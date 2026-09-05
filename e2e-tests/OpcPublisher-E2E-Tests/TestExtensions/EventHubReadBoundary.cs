// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using Azure.Messaging.EventHubs.Consumer;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    internal sealed class EventHubReadBoundary
    {
        public static async Task<EventHubReadBoundary> CaptureAsync(
            EventHubConsumerClient consumer, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(consumer);
            var partitionIds = await consumer.GetPartitionIdsAsync(ct).ConfigureAwait(false);
            if (partitionIds.Length == 0)
            {
                throw new InvalidOperationException("Event Hubs returned no partitions.");
            }
            var positions = new Dictionary<string, EventPosition>(StringComparer.Ordinal);
            foreach (var partitionId in partitionIds)
            {
                var properties = await consumer.GetPartitionPropertiesAsync(partitionId, ct)
                    .ConfigureAwait(false);
                positions.Add(partitionId, properties.IsEmpty
                    ? EventPosition.Earliest
                    : EventPosition.FromSequenceNumber(properties.LastEnqueuedSequenceNumber,
                        isInclusive: false));
            }
            return new EventHubReadBoundary(consumer, positions);
        }

        public async IAsyncEnumerable<PartitionEvent> ReadAsync(
            [EnumeratorCancellation] CancellationToken ct)
        {
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var channel = Channel.CreateBounded<PartitionEvent>(new BoundedChannelOptions(128)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                AllowSynchronousContinuations = false
            });
            var pumps = _positions.Select(ReadPartitionAsync).ToArray();
            var completion = CompleteAsync();
            try
            {
                await foreach (var item in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
            finally
            {
                await stop.CancelAsync().ConfigureAwait(false);
                await completion.ConfigureAwait(false);
            }

            async Task ReadPartitionAsync(KeyValuePair<string, EventPosition> partition)
            {
                try
                {
                    await foreach (var item in _consumer.ReadEventsFromPartitionAsync(
                        partition.Key, partition.Value, stop.Token).ConfigureAwait(false))
                    {
                        await channel.Writer.WriteAsync(item, stop.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (stop.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    // Transfer the receiver error to the consuming iterator.
                    channel.Writer.TryComplete(exception);
                }
            }

            async Task CompleteAsync()
            {
                await Task.WhenAll(pumps).ConfigureAwait(false);
                channel.Writer.TryComplete();
            }
        }

        private EventHubReadBoundary(EventHubConsumerClient consumer,
            IReadOnlyDictionary<string, EventPosition> positions)
        {
            _consumer = consumer;
            _positions = positions;
        }

        private readonly EventHubConsumerClient _consumer;
        private readonly IReadOnlyDictionary<string, EventPosition> _positions;
    }
}
