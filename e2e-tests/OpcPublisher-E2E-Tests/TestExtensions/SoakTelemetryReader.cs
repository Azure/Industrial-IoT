// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using Azure.Messaging.EventHubs.Consumer;
    using Microsoft.Azure.Devices;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// Streams telemetry from the IoT Hub built-in Event Hubs endpoint into a
    /// sink for a bounded duration.
    /// </para>
    /// <para>
    /// The long running telemetry quality tests observe a stream for tens of
    /// minutes, so unlike <see cref="TestHelper.ReadMessagesFromWriterIdAsync(EventHubConsumerClient, string, int, IIoTPlatformTestContext, string, CancellationToken)"/>
    /// this reader must not buffer messages, must not write one line of test
    /// output per message, and must understand the legacy samples encoding
    /// (a bare monitored item message) in addition to PubSub network messages.
    /// It parses with <see cref="System.Text.Json"/> because that is what the
    /// shared telemetry quality validator consumes.
    /// </para>
    /// </summary>
    internal static class SoakTelemetryReader
    {
        /// <summary>
        /// Consume telemetry produced by a single publisher module.
        /// </summary>
        /// <param name="consumer">Event hub consumer to read from</param>
        /// <param name="moduleId">
        /// Identity of the publisher module whose telemetry is wanted. Every
        /// soak scenario deploys its own module, and IoT Hub stamps the
        /// sending module onto each message, so this isolates the scenarios
        /// from each other and from the A&amp;E tests without having to look
        /// inside the payload.
        /// </param>
        /// <param name="duration">How long to observe</param>
        /// <param name="sink">
        /// Invoked once per data set message. The element is only valid for
        /// the duration of the call.
        /// </param>
        /// <param name="onFirstMessage">
        /// Optional callback receiving the raw json of the first accepted
        /// message, so a failing run has one concrete example to look at
        /// without logging the whole stream.
        /// </param>
        /// <param name="ct"></param>
        /// <returns>Number of messages handed to the sink</returns>
        public static async Task<long> ConsumeAsync(this EventHubConsumerClient consumer,
            string moduleId, TimeSpan duration, Action<JsonElement> sink,
            Action<string> onFirstMessage = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(consumer);
            ArgumentNullException.ThrowIfNull(sink);

            var count = 0L;
            var first = true;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(duration);
            try
            {
                await foreach (var partitionEvent in consumer
                    .ReadEventsAsync(false, cancellationToken: cts.Token)
                    .WithCancellation(cts.Token))
                {
                    if (partitionEvent.Data == null)
                    {
                        continue;
                    }
                    if (!partitionEvent.Data.SystemProperties.TryGetValue(
                            kConnectionModuleId, out var moduleIdObj) ||
                        moduleIdObj is not string sender ||
                        !string.Equals(sender, moduleId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    byte[] body;
                    if (TestHelper.IsGzipPayload(partitionEvent.Data))
                    {
                        body = Decompress(Convert.FromBase64String(
                            partitionEvent.Data.EventBody.ToString()));
                    }
                    else
                    {
                        body = partitionEvent.Data.EventBody.ToArray();
                    }
                    if (body.Length == 0)
                    {
                        continue;
                    }

                    if (first)
                    {
                        first = false;
                        onFirstMessage?.Invoke(Encoding.UTF8.GetString(body));
                    }

                    using var document = JsonDocument.Parse(body);
                    var element = document.RootElement;
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            sink(item);
                            count++;
                        }
                    }
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        sink(element);
                        count++;
                    }
                }
            }
            catch (OperationCanceledException) { }
            return count;
        }

        /// <summary>
        /// Wait until the given module produces its first telemetry message,
        /// or the timeout elapses. Used to end the warm up phase as soon as
        /// the publisher is actually delivering rather than after a fixed
        /// guess.
        /// </summary>
        /// <param name="consumer"></param>
        /// <param name="moduleId"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns>How long it took, or null when nothing arrived</returns>
        public static async Task<TimeSpan?> WaitForFirstMessageAsync(
            this EventHubConsumerClient consumer, string moduleId, TimeSpan timeout,
            CancellationToken ct = default)
        {
            var stopWatch = Stopwatch.StartNew();
            var seen = false;
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
            await consumer.ConsumeAsync(moduleId, timeout, _ =>
            {
                seen = true;
                //
                // Cancelling the token that ConsumeAsync links against breaks
                // it out of the read loop on the next iteration; it swallows
                // the resulting cancellation itself.
                //
                stop.Cancel();
            }, onFirstMessage: null, stop.Token).ConfigureAwait(false);
            return seen ? stopWatch.Elapsed : null;
        }

        private static byte[] Decompress(byte[] compressed)
        {
            using var input = new MemoryStream(compressed);
            using var gs = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gs.CopyTo(output);
            return output.ToArray();
        }

        /// <summary>
        /// System property IoT Hub stamps onto every device to cloud message
        /// with the identity of the sending module. Not exposed as a constant
        /// by <c>Microsoft.Azure.Devices.MessageSystemPropertyNames</c>, which
        /// only carries the device id.
        /// </summary>
        private const string kConnectionModuleId = "iothub-connection-module-id";
    }
}
