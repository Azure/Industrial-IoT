// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    using Azure.Core;
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using global::Azure.Messaging.EventHubs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="EventHubsClient.EventHubsEvent"/> covering
    /// the fluent-builder methods, <c>CreateMessage</c> and
    /// <c>SendAsync</c> early-exit when no buffers were added.
    /// </summary>
    public sealed class EventHubsClientEventTests : IAsyncDisposable
    {
        private readonly EventHubsClient _client;
        private const string kConnectionString =
            "Endpoint=sb://example.servicebus.windows.net/;" +
            "SharedAccessKeyName=test;SharedAccessKey=ZmFrZWtleQ==;EntityPath=hub";

        public EventHubsClientEventTests()
        {
            var options = Options.Create(new EventHubsClientOptions
            {
                ConnectionString = kConnectionString
            });
            _client = new EventHubsClient(
                options,
                new NoopCredentialProvider(),
                NullLogger<EventHubsClient>.Instance);
        }

        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync().ConfigureAwait(false);
        }

        // ── CreateEvent ───────────────────────────────────────────────────────

        [Fact]
        public void CreateEvent_ReturnsNonNullEvent()
        {
            using var ev = _client.CreateEvent();
            Assert.NotNull(ev);
        }

        // ── Fluent builder methods (all return the same IEvent instance) ──────

        [Fact]
        public void SetQoS_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.SetQoS(QoS.AtLeastOnce);
            Assert.Same(ev, result);
        }

        [Fact]
        public void SetContentType_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.SetContentType("application/json");
            Assert.Same(ev, result);
        }

        [Fact]
        public void SetContentEncoding_WithNonEmptyValue_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.SetContentEncoding("utf-8");
            Assert.Same(ev, result);
        }

        [Fact]
        public void SetContentEncoding_WithEmptyValue_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.SetContentEncoding(string.Empty);
            Assert.Same(ev, result);
        }

        [Fact]
        public void SetRetain_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.SetRetain(true);
            Assert.Same(ev, result);
        }

        [Fact]
        public void SetTtl_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.SetTtl(TimeSpan.FromMinutes(1));
            Assert.Same(ev, result);
        }

        [Fact]
        public void SetTimestamp_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.SetTimestamp(DateTimeOffset.UtcNow);
            Assert.Same(ev, result);
        }

        [Fact]
        public void SetTopic_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.SetTopic("device-1");
            Assert.Same(ev, result);
        }

        [Fact]
        public void AddProperty_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.AddProperty("x-custom", "val");
            Assert.Same(ev, result);
        }

        [Fact]
        public void AddBuffers_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var result = ev.AddBuffers([new ReadOnlySequence<byte>(
                Encoding.UTF8.GetBytes("data"))]);
            Assert.Same(ev, result);
        }

        [Fact]
        public void SetSchema_WithNonAvroType_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var schema = new TestEventSchema("application/json");
            var result = ev.SetSchema(schema);
            Assert.Same(ev, result);
        }

        [Fact]
        public void SetSchema_WithAvroType_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var schema = new TestEventSchema(ContentMimeType.AvroSchema);
            var result = ev.SetSchema(schema);
            Assert.Same(ev, result);
        }

        [Fact]
        public void AsCloudEvent_SetsHeaderFields_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var header = new CloudEventHeader
            {
                Id = "test-id",
                Source = new Uri("http://source/"),
                Type = "com.example.test",
                Time = DateTimeOffset.UtcNow,
                Subject = "subject",
                DataContentType = "application/json"
            };

            var result = ev.AsCloudEvent(header);
            Assert.Same(ev, result);
        }

        [Fact]
        public void AsCloudEvent_WithNullOptionals_ReturnsSameInstance()
        {
            using var ev = _client.CreateEvent();
            var header = new CloudEventHeader
            {
                Id = "id2",
                Source = new Uri("http://source2/"),
                Type = "com.example.test2"
                // Time = null, Subject = null, DataContentType = null
            };

            var result = ev.AsCloudEvent(header);
            Assert.Same(ev, result);
        }

        // ── SendAsync — early exit when no buffers were added ─────────────────

        [Fact]
        public async Task SendAsync_NoBufers_ReturnsWithoutSendingAsync()
        {
            using var ev = _client.CreateEvent();
            // No AddBuffers call → _buffers.Count == 0 → early return
            await ev.SendAsync(CancellationToken.None).ConfigureAwait(false);
        }

        // ── CreateMessage — builds EventData with the right properties ────────

        [Fact]
        public void CreateMessage_SingleSegmentBuffer_SetsContentType()
        {
            var inner = (EventHubsClient.EventHubsEvent)_client.CreateEvent();
            inner.SetContentType("application/octet-stream");
            inner.SetContentEncoding("gzip");
            inner.AddProperty("key1", "val1");
            inner.AddProperty("key2", null); // null value → remove

            var data = Encoding.UTF8.GetBytes("hello");
            var msg = inner.CreateMessage(new ReadOnlySequence<byte>(data));

            Assert.Equal("application/octet-stream", msg.ContentType);
        }

        [Fact]
        public void CreateMessage_MultiSegmentBuffer_ProducesEventData()
        {
            var inner = (EventHubsClient.EventHubsEvent)_client.CreateEvent();

            // Multi-segment sequence
            var seg1 = new byte[] { 1, 2 };
            var seg2 = new byte[] { 3, 4 };
            var seq = CreateMultiSegmentSequence(seg1, seg2);

            var msg = inner.CreateMessage(seq);
            Assert.NotNull(msg);
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        [Fact]
        public void Dispose_ClearsBuffers_DoesNotThrow()
        {
            var ev = _client.CreateEvent();
            ev.AddBuffers([new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("x"))]);

            var ex = Record.Exception(() => ev.Dispose());
            Assert.Null(ex);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static ReadOnlySequence<byte> CreateMultiSegmentSequence(
            byte[] first, byte[] second)
        {
            var seg1 = new MemorySegment<byte>(first);
            var seg2 = seg1.Append(second);
            return new ReadOnlySequence<byte>(seg1, 0, seg2, seg2.Memory.Length);
        }

        private sealed class MemorySegment<T> : ReadOnlySequenceSegment<T>
        {
            public MemorySegment(ReadOnlyMemory<T> memory)
            {
                Memory = memory;
            }

            public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
            {
                var segment = new MemorySegment<T>(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };
                Next = segment;
                return segment;
            }
        }

        private sealed class TestEventSchema : IEventSchema
        {
            public string Type { get; }
            public string Schema { get; } = "{}";
            public string Name { get; } = "test";
            public ulong Version { get; }
            public string Id { get; } = "test-schema-id";

            public TestEventSchema(string type) => Type = type;
        }

        private sealed class NoopCredentialProvider : ICredentialProvider
        {
            public TokenCredential Credential { get; } = new NoopCredential();
        }

        private sealed class NoopCredential : TokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext,
                CancellationToken cancellationToken)
                => new("token", DateTimeOffset.MaxValue);

            public override ValueTask<AccessToken> GetTokenAsync(
                TokenRequestContext requestContext, CancellationToken cancellationToken)
                => ValueTask.FromResult(GetToken(requestContext, cancellationToken));
        }
    }
}
