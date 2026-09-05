// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Consumer;
    using Microsoft.Azure.Devices;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    [Trait("Category", "Unit")]
    public sealed class EventHubReaderTests
    {
        [Theory]
        [InlineData("JsonGzip", false)]
        [InlineData("JsonReversibleGzip", false)]
        [InlineData(null, true)]
        [InlineData(null, false)]
        public async Task FunctionalAndSoakReadersDecodeGzipAsync(
            string encoding, bool base64)
        {
            var compressed = Compress(kMessage);
            var body = base64
                ? BinaryData.FromString(Convert.ToBase64String(compressed))
                : new BinaryData(compressed);
            var message = CreateEvent(body);
            if (encoding is null)
            {
                message.Properties["$$ContentType"] = "application/json+gzip";
            }
            else
            {
                message.Properties["encoding"] = encoding;
            }
            var consumer = new BufferedConsumer(message);

            var actual = await consumer.ReadMessagesFromWriterIdAsync(
                "writer", 1, null, "publisher", CancellationToken.None)
                .ToListAsync();
            var payload = Assert.Single(actual);
            Assert.Equal(42, (int)payload.payload["value"]);

            var values = new List<int>();
            var count = await consumer.ConsumeAsync(
                "publisher", TimeSpan.FromSeconds(5),
                item => values.Add(item.GetProperty("Messages")[0]
                    .GetProperty("Payload").GetProperty("value").GetInt32()));

            Assert.Equal(1, count);
            Assert.Equal([42], values);
        }

        [Fact]
        public async Task ReadAfterCapturesPositionsBeforeTriggerAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var consumer = new DelayedConsumer(CreateEvent(
                BinaryData.FromString(kMessage)));
            var triggered = false;

            var read = TestHelper.ReadAfterAsync(
                consumer, (events, token) => events.ReadMessagesFromWriterIdAsync(
                    "writer", 1, null, "publisher", token),
                _ =>
                {
                    Assert.True(consumer.PositionsCaptured,
                        "The trigger ran before all partition positions were captured.");
                    triggered = true;
                    consumer.AllowReceiver.TrySetResult();
                    return Task.CompletedTask;
                },
                timeout.Token);

            Assert.False(triggered);
            consumer.AllowDiscovery.TrySetResult();
            var values = await read;

            Assert.True(triggered);
            Assert.Equal(42, (int)Assert.Single(values).payload["value"]);
            Assert.Equal(0, consumer.ActiveReceivers);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadAfterMergesAllPartitionsFromCapturedPositionsAsync(bool empty)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var consumer = new DelayedConsumer(CreateEvent(BinaryData.FromString(kMessage)), empty);
            consumer.AllowDiscovery.TrySetResult();

            var values = await TestHelper.ReadAfterAsync(
                consumer, (events, token) => events.ReadMessagesFromWriterIdAsync(
                    "writer", 2, null, "publisher", token),
                _ =>
                {
                    Assert.True(consumer.PositionsCaptured);
                    consumer.AllowReceiver.TrySetResult();
                    return Task.CompletedTask;
                }, timeout.Token);

            Assert.Equal(2, values.Count);
            Assert.All(values, value => Assert.Equal(42, (int)value.payload["value"]));
            Assert.Equal(0, consumer.ActiveReceivers);
        }

        [Fact]
        public async Task CaptureFailurePreventsTheTriggerAsync()
        {
            var expected = new InvalidOperationException("partition discovery failed");
            var consumer = new DelayedConsumer(CreateEvent(BinaryData.FromString(kMessage)))
            {
                CaptureError = expected
            };
            consumer.AllowDiscovery.TrySetResult();
            var triggered = false;

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                TestHelper.ReadAfterAsync(consumer,
                    (events, token) => events.ReadMessagesFromWriterIdAsync(
                        "writer", 1, null, "publisher", token),
                    _ =>
                    {
                        triggered = true;
                        return Task.CompletedTask;
                    }, CancellationToken.None));

            Assert.Same(expected, error);
            Assert.False(triggered);
        }

        [Fact]
        public async Task CancellationDuringCapturePreventsTheTriggerAsync()
        {
            using var stop = new CancellationTokenSource();
            var consumer = new DelayedConsumer(CreateEvent(BinaryData.FromString(kMessage)));
            var triggered = false;
            var read = TestHelper.ReadAfterAsync(consumer,
                (events, token) => events.ReadMessagesFromWriterIdAsync(
                    "writer", 1, null, "publisher", token),
                _ =>
                {
                    triggered = true;
                    return Task.CompletedTask;
                }, stop.Token);

            await stop.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
            Assert.False(triggered);
            Assert.Equal(0, consumer.ActiveReceivers);
        }

        [Fact]
        public async Task ReceiverFailureReachesTheCallerAndClosesOtherReceiversAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var expected = new InvalidOperationException("receiver failed");
            var consumer = new DelayedConsumer(CreateEvent(BinaryData.FromString(kMessage)))
            {
                ReceiverError = expected
            };
            consumer.AllowDiscovery.TrySetResult();
            consumer.AllowReceiver.TrySetResult();
            var boundary = await EventHubReadBoundary.CaptureAsync(consumer, timeout.Token);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await boundary.ReadAsync(timeout.Token).ToListAsync());

            Assert.Same(expected, error);
            Assert.Equal(0, consumer.ActiveReceivers);
        }

        [Fact]
        public async Task InvalidNativeGzipFailsBothReadersAsync()
        {
            var message = CreateEvent(BinaryData.FromString("not gzip"));
            message.Properties["encoding"] = "JsonGzip";
            var consumer = new BufferedConsumer(message);

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
                await consumer.ReadMessagesFromWriterIdAsync(
                    "writer", 1, null, "publisher", CancellationToken.None).ToListAsync());
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                consumer.ConsumeAsync("publisher", TimeSpan.FromSeconds(5), _ => { }));
        }

        [Theory]
        [InlineData("""{"MajorVersion":1}""")]
        [InlineData("""{"MinorVersion":0}""")]
        [InlineData("""{"MajorVersion":-1,"MinorVersion":0}""")]
        [InlineData("""{"MajorVersion":4294967296,"MinorVersion":0}""")]
        [InlineData("""{"MajorVersion":1,"MinorVersion":"0"}""")]
        [InlineData("""{"MajorVersion":1,"MinorVersion":0.5}""")]
        [InlineData("""{"MajorVersion":null,"MinorVersion":0}""")]
        [InlineData("null")]
        [InlineData("[]")]
        public void MatchedMessagesRejectMalformedMetadataVersions(string metadata)
        {
            var message = JObject.Parse(kMessage);
            message["Messages"][0]["MetaDataVersion"] = JToken.Parse(metadata);

            Assert.Throws<InvalidDataException>(() =>
                PubSubMessageMatcher.Match(message, "writer").ToArray());
        }

        [Fact]
        public void MatchedMessagesRequireMetadataVersion()
        {
            var message = JObject.Parse(kMessage);
            ((JObject)message["Messages"][0]).Remove("MetaDataVersion");

            Assert.Throws<InvalidDataException>(() =>
                PubSubMessageMatcher.Match(message, "writer").ToArray());
        }

        [Theory]
        [InlineData(0L, 0L)]
        [InlineData(841717520L, 841717521L)]
        [InlineData(4294967295L, 4294967295L)]
        public void MetadataVersionsNeedNotEqualOne(long major, long minor)
        {
            var message = JObject.Parse(kMessage);
            message["Messages"][0]["MetaDataVersion"] = new JObject
            {
                ["MajorVersion"] = major,
                ["MinorVersion"] = minor
            };

            Assert.Single(PubSubMessageMatcher.Match(message, "writer"));
        }

        [Fact]
        public void UnmatchedMessagesDoNotRequireMetadataVersion()
        {
            var message = JObject.Parse(kMessage);
            ((JObject)message["Messages"][0]).Remove("MetaDataVersion");

            Assert.Empty(PubSubMessageMatcher.Match(message, "another-writer"));
        }

        private static byte[] Compress(string text)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
            {
                gzip.Write(Encoding.UTF8.GetBytes(text));
            }
            return output.ToArray();
        }

        private static EventData CreateEvent(BinaryData body)
        {
            return EventHubsModelFactory.EventData(
                eventBody: body,
                properties: new Dictionary<string, object>
                {
                    ["$$MessageSchema"] = "application/x-network-message-json-v1"
                },
                systemProperties: new Dictionary<string, object>
                {
                    ["iothub-connection-module-id"] = "publisher",
                    [MessageSystemPropertyNames.EnqueuedTime] = DateTime.UnixEpoch
                },
                sequenceNumber: 11,
                offsetString: "11",
                enqueuedTime: DateTimeOffset.UnixEpoch);
        }

        private class BufferedConsumer(EventData message) : EventHubConsumerClient
        {
            public override async IAsyncEnumerable<PartitionEvent> ReadEventsAsync(
                bool startReadingAtEarliestEvent,
                ReadEventOptions readOptions = default,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.CompletedTask;
                yield return new PartitionEvent(
                    EventHubsModelFactory.PartitionContext(
                        "test.servicebus.windows.net", "hub", "tests", "0"),
                    message);
            }
        }

        private sealed class DelayedConsumer(EventData message, bool empty = false) :
            BufferedConsumer(message)
        {
            public TaskCompletionSource AllowDiscovery { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource AllowReceiver { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool PositionsCaptured => _captured == 2;
            public int ActiveReceivers => Volatile.Read(ref _active);
            public Exception CaptureError { get; init; }
            public Exception ReceiverError { get; init; }

            public override async Task<string[]> GetPartitionIdsAsync(
                CancellationToken cancellationToken = default)
            {
                await AllowDiscovery.Task.WaitAsync(cancellationToken);
                return ["0", "1"];
            }

            public override Task<PartitionProperties> GetPartitionPropertiesAsync(
                string partitionId, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (CaptureError is { } error)
                {
                    return Task.FromException<PartitionProperties>(error);
                }
                _captured++;
                return Task.FromResult(EventHubsModelFactory.PartitionProperties(
                    "hub", partitionId, empty, 0, 10, "10", DateTimeOffset.UnixEpoch));
            }

            public override async IAsyncEnumerable<PartitionEvent> ReadEventsAsync(
                bool startReadingAtEarliestEvent,
                ReadEventOptions readOptions = default,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await GetPartitionIdsAsync(cancellationToken);
                await GetPartitionPropertiesAsync("0", cancellationToken);
                await AllowReceiver.Task.WaitAsync(cancellationToken);
                await foreach (var item in base.ReadEventsAsync(
                    startReadingAtEarliestEvent, readOptions, cancellationToken))
                {
                    yield return item;
                }
            }

            public override async IAsyncEnumerable<PartitionEvent> ReadEventsFromPartitionAsync(
                string partitionId, EventPosition startingPosition,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref _active);
                try
                {
                    Assert.Equal(empty ? EventPosition.Earliest :
                        EventPosition.FromSequenceNumber(10, false), startingPosition);
                    await AllowReceiver.Task.WaitAsync(cancellationToken);
                    if (ReceiverError is { } error)
                    {
                        if (partitionId == "0")
                        {
                            throw error;
                        }
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }
                    await foreach (var item in base.ReadEventsAsync(false,
                        cancellationToken: cancellationToken))
                    {
                        yield return item;
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _active);
                }
            }

            private int _captured;
            private int _active;
        }

        private const string kMessage = """
            {
              "MessageType":"ua-data",
              "WriterGroupName":"writer-0",
              "Messages":[{
                "DataSetWriterId":123,
                "MetaDataVersion":{"MajorVersion":7,"MinorVersion":9},
                "MessageType":"ua-event",
                "Payload":{"value":42}
              }]
            }
            """;
    }
}
