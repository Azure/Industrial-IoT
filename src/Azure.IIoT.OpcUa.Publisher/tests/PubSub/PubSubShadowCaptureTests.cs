// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="PubSubShadowCapture"/> and
    /// <see cref="InMemoryPubSubShadowCaptureSink"/>.
    /// </summary>
    public sealed class PubSubShadowCaptureTests
    {
        // ── PubSubShadowCapture ───────────────────────────────────────────────

        [Fact]
        public void Capture_StoresEncoding()
        {
            var payload = new byte[] { 1, 2, 3 };
            var capture = new PubSubShadowCapture(
                PubSubShadowEncoding.Json, DateTimeOffset.UtcNow, payload);

            Assert.Equal(PubSubShadowEncoding.Json, capture.Encoding);
        }

        [Fact]
        public void Capture_StoresCapturedAt()
        {
            var now = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
            var capture = new PubSubShadowCapture(
                PubSubShadowEncoding.Json, now, new byte[] { 1 });

            Assert.Equal(now, capture.CapturedAt);
        }

        [Fact]
        public void Capture_StoresPayloadCopy()
        {
            var original = new byte[] { 10, 20, 30 };
            var capture = new PubSubShadowCapture(
                PubSubShadowEncoding.Json, DateTimeOffset.UtcNow, original);

            // Mutate original — capture's Payload must be unaffected
            original[0] = 0xFF;
            Assert.Equal(10, capture.Payload.Span[0]);
        }

        [Theory]
        [InlineData(PubSubShadowEncoding.JsonGzip)]
        [InlineData(PubSubShadowEncoding.JsonReversibleGzip)]
        public void Capture_GzipEncodings_ContentEncodingIsGzip(PubSubShadowEncoding encoding)
        {
            var capture = new PubSubShadowCapture(
                encoding, DateTimeOffset.UtcNow, new byte[] { 1 });

            Assert.Equal("gzip", capture.ContentEncoding);
        }

        [Theory]
        [InlineData(PubSubShadowEncoding.Json)]
        [InlineData(PubSubShadowEncoding.JsonReversible)]
        [InlineData(PubSubShadowEncoding.Uadp)]
        public void Capture_NonGzipEncodings_ContentEncodingIsNull(PubSubShadowEncoding encoding)
        {
            var capture = new PubSubShadowCapture(
                encoding, DateTimeOffset.UtcNow, new byte[] { 1 });

            Assert.Null(capture.ContentEncoding);
        }

        [Fact]
        public void Capture_Clone_CreatesDeepCopy()
        {
            var original = new byte[] { 1, 2, 3 };
            var ts = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
            var capture = new PubSubShadowCapture(
                PubSubShadowEncoding.Uadp, ts, original);

            var clone = capture.Clone();

            Assert.Equal(capture.Encoding, clone.Encoding);
            Assert.Equal(capture.CapturedAt, clone.CapturedAt);
            Assert.Equal(capture.Payload.ToArray(), clone.Payload.ToArray());
        }

        [Fact]
        public void Capture_WithNullTransportProfileUri_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PubSubShadowCapture(PubSubShadowEncoding.Json,
                    DateTimeOffset.UtcNow, null!, ReadOnlySpan<byte>.Empty));
        }

        // ── InMemoryPubSubShadowCaptureSink ───────────────────────────────────

        [Fact]
        public void Sink_ZeroCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new InMemoryPubSubShadowCaptureSink(0));
        }

        [Fact]
        public void Sink_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new InMemoryPubSubShadowCaptureSink(-1));
        }

        [Fact]
        public async Task Sink_CaptureAsync_NullCapture_ThrowsArgumentNullException()
        {
            var sink = new InMemoryPubSubShadowCaptureSink(10);

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                sink.CaptureAsync(null!).AsTask());
        }

        [Fact]
        public async Task Sink_CaptureAsync_CancelledToken_ThrowsOperationCanceledException()
        {
            var sink = new InMemoryPubSubShadowCaptureSink(10);
            var capture = MakeCapture("payload");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                sink.CaptureAsync(capture, cts.Token).AsTask());
        }

        [Fact]
        public async Task Sink_EmptySink_HasNoCaptures()
        {
            var sink = new InMemoryPubSubShadowCaptureSink(10);

            Assert.Empty(sink.Captures);
            Assert.Equal(0, sink.DroppedCaptureCount);
        }

        [Fact]
        public async Task Sink_SingleCapture_ReturnsItInCaptures()
        {
            var sink = new InMemoryPubSubShadowCaptureSink(10);
            var capture = MakeCapture("hello");

            await sink.CaptureAsync(capture);

            Assert.Single(sink.Captures);
        }

        [Fact]
        public async Task Sink_MultipleCaptures_OrderedByInsertion()
        {
            var sink = new InMemoryPubSubShadowCaptureSink(10);
            await sink.CaptureAsync(MakeCapture("first"));
            await sink.CaptureAsync(MakeCapture("second"));
            await sink.CaptureAsync(MakeCapture("third"));

            var captures = sink.Captures;

            Assert.Equal(3, captures.Count);
        }

        [Fact]
        public async Task Sink_CapacityReached_OldestEvicted()
        {
            var sink = new InMemoryPubSubShadowCaptureSink(2);
            var first = MakeCapture("first");
            var second = MakeCapture("second");
            var third = MakeCapture("third");

            await sink.CaptureAsync(first);
            await sink.CaptureAsync(second);
            await sink.CaptureAsync(third);   // evicts 'first'

            Assert.Equal(2, sink.Captures.Count);
            Assert.Equal(1, sink.DroppedCaptureCount);
        }

        [Fact]
        public async Task Sink_MultipleEvictions_DroppedCountAccumulates()
        {
            var sink = new InMemoryPubSubShadowCaptureSink(1);

            await sink.CaptureAsync(MakeCapture("one"));
            await sink.CaptureAsync(MakeCapture("two"));
            await sink.CaptureAsync(MakeCapture("three"));

            Assert.Equal(1, sink.Captures.Count);
            Assert.Equal(2, sink.DroppedCaptureCount);
        }

        [Fact]
        public async Task Sink_Captures_ReturnsDeepCopies()
        {
            var sink = new InMemoryPubSubShadowCaptureSink(10);
            var capture = MakeCapture("data");
            await sink.CaptureAsync(capture);

            var list1 = sink.Captures;
            var list2 = sink.Captures;

            // Each call returns a fresh copy — different list instances
            Assert.NotSame(list1, list2);
        }

        private static PubSubShadowCapture MakeCapture(string text)
        {
            return new PubSubShadowCapture(
                PubSubShadowEncoding.Json,
                DateTimeOffset.UtcNow,
                Encoding.UTF8.GetBytes(text));
        }
    }
}
