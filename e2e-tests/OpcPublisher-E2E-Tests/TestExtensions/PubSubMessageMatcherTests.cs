// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class PubSubMessageMatcherTests
    {
        [Fact]
        public void NativeMessageMatchesThroughWriterGroupName()
        {
            var message = JObject.Parse("""
                {
                  "MessageId": "m1",
                  "MessageType": "ua-data",
                  "WriterGroupName": "writer-guid-0",
                  "Messages": [
                    {
                      "DataSetWriterId": 11594,
                      "MetaDataVersion": {
                        "MajorVersion": 841717520,
                        "MinorVersion": 841717520
                      },
                      "MessageType": "ua-event",
                      "Payload": {
                        "EventId": {
                          "Value": "AQID"
                        }
                      }
                    }
                  ]
                }
                """);

            var match = Assert.Single(
                PubSubMessageMatcher.Match(message, "writer-guid"));

            Assert.Equal("writer-guid-0", match.WriterGroupName);
            Assert.Equal("ua-event", match.MessageType);
            Assert.Equal("AQID",
                (string)match.Payload["EventId"]?["Value"]);
        }

        [Fact]
        public void LegacyMessageMatchesThroughStringDataSetWriterId()
        {
            var message = JObject.Parse("""
                {
                  "MessageType": "ua-data",
                  "DataSetWriterGroup": "legacy-group",
                  "Messages": [
                    {
                      "DataSetWriterId": "writer-guid",
                      "MetaDataVersion": {
                        "MajorVersion": 1,
                        "MinorVersion": 0
                      },
                      "Payload": {
                        "Value": 42
                      }
                    }
                  ]
                }
                """);

            var match = Assert.Single(
                PubSubMessageMatcher.Match(message, "writer-guid"));

            Assert.Equal("legacy-group", match.WriterGroupName);
            Assert.Equal("writer-guid", match.DataSetWriterName);
        }

        [Fact]
        public void StrictMessageMatchesThroughDataSetWriterName()
        {
            var message = JObject.Parse("""
                {
                  "MessageType": "ua-data",
                  "WriterGroupName": "unrelated-group",
                  "Messages": [
                    {
                      "DataSetWriterId": 17,
                      "DataSetWriterName": "writer-guid",
                      "Payload": {
                        "Value": 42
                      }
                    }
                  ]
                }
                """);

            Assert.Single(PubSubMessageMatcher.Match(message, "writer-guid"));
        }

        [Fact]
        public void NumericWriterIdDoesNotMatchConfiguredString()
        {
            var message = JObject.Parse("""
                {
                  "MessageType": "ua-data",
                  "WriterGroupName": "other",
                  "Messages": [
                    {
                      "DataSetWriterId": 11594,
                      "Payload": {
                        "Value": 42
                      }
                    }
                  ]
                }
                """);

            Assert.Empty(PubSubMessageMatcher.Match(message, "11594"));
        }

        [Fact]
        public void BatchedNetworkMessagesAreFlattened()
        {
            var batch = JArray.Parse("""
                [
                  { "MessageType": "ua-metadata" },
                  { "MessageType": "ua-data", "Messages": [] }
                ]
                """);

            Assert.Equal(2,
                PubSubMessageMatcher.EnumerateNetworkMessages(batch).Count());
        }

        [Fact]
        public async Task ReadAfterStartsReaderBeforeTriggerAsync()
        {
            var readerStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var values = await global::OpcPublisherAEE2ETests.TestHelper
                .ReadAfterAsync(Read, Trigger, CancellationToken.None,
                    TimeSpan.Zero);

            Assert.Equal([42], values);

            async IAsyncEnumerable<int> Read(
                [EnumeratorCancellation] CancellationToken ct)
            {
                readerStarted.TrySetResult();
                await release.Task.WaitAsync(ct);
                yield return 42;
            }

            Task Trigger(CancellationToken _)
            {
                Assert.True(readerStarted.Task.IsCompleted,
                    "The trigger ran before the reader entered MoveNextAsync.");
                release.TrySetResult();
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task ReadAfterPreservesTriggerExceptionWhenReaderCleanupFailsAsync()
        {
            var expected = new InvalidOperationException("trigger failed");

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => global::OpcPublisherAEE2ETests.TestHelper.ReadAfterAsync(
                    Read, Trigger, CancellationToken.None, TimeSpan.Zero));

            Assert.Same(expected, actual);

            async IAsyncEnumerable<int> Read(
                [EnumeratorCancellation] CancellationToken ct)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                finally
                {
                    throw new ApplicationException("reader cleanup failed");
                }
#pragma warning disable CS0162 // Required to make this an async iterator.
                yield return 42;
#pragma warning restore CS0162
            }

            Task Trigger(CancellationToken _)
            {
                return Task.FromException(expected);
            }
        }

        [Fact]
        public async Task ReadAfterPreservesTriggerExceptionWhenCancellationCallbackFailsAsync()
        {
            var expected = new InvalidOperationException("trigger failed");

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => global::OpcPublisherAEE2ETests.TestHelper.ReadAfterAsync(
                    Read, Trigger, CancellationToken.None, TimeSpan.Zero));

            Assert.Same(expected, actual);

            async IAsyncEnumerable<int> Read(
                [EnumeratorCancellation] CancellationToken ct)
            {
                using var registration = ct.Register(
                    () => throw new ApplicationException(
                        "cancellation callback failed"));
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                yield return 42;
            }

            Task Trigger(CancellationToken _)
            {
                return Task.FromException(expected);
            }
        }
    }
}
