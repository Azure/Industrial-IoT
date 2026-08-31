// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.ReferenceServer
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Sustained-publishing checks for whichever telemetry path the run
    /// selects. These cover the part of the soak scope that does not need a
    /// container: that publishing keeps flowing rather than stalling after the
    /// first burst, and that a writer's sequence numbers stay ordered and
    /// gapless under continuous load.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two equal rounds are collected against one publisher so the second can
    /// be compared against the first without startup costs in the way. Heap,
    /// handle and thread counts are reported for both rounds rather than
    /// asserted: over a run this short a threshold would either be so loose it
    /// catches nothing or so tight it flakes. A leak conclusion needs the
    /// multi-hour run that is still outstanding; what is asserted here is the
    /// behaviour a short run can actually establish.
    /// </para>
    /// <para>
    /// The publishing interval comes from the fixture, so raising the round
    /// size is the way to lengthen these without changing what they assert.
    /// </para>
    /// </remarks>
    [Collection(MqttReferenceServerCollection.Name)]
    public class MqttPubSubSoakTests : PublisherIntegrationTestBase, IClassFixture<ReferenceServer>
    {
        private const int kRoundSize = 40;
        private static readonly TimeSpan kRoundTimeout = TimeSpan.FromMinutes(2);

        private readonly ReferenceServer _fixture;
        private readonly ITestOutputHelper _output;

        public MqttPubSubSoakTests(ReferenceServer fixture, ITestOutputHelper output) : base(output)
        {
            _output = output;
            _fixture = fixture;
            EndpointUrl = _fixture.EndpointUrl;
        }

        [Fact]
        public async Task SustainedPublishingKeepsFlowingAndStaysOrderedAsync()
        {
            StartPublisher(nameof(SustainedPublishingKeepsFlowingAndStaysOrderedAsync),
                "./Resources/DataItems.json",
                arguments: ["--mm=PubSub", "--dm=False"], version: MqttVersion.v5);
            try
            {
                var first = await CollectRoundAsync("first").ConfigureAwait(false);
                var second = await CollectRoundAsync("second").ConfigureAwait(false);

                //
                // A path that publishes its retained state once and then stops
                // would satisfy a single round. Requiring a second, equally
                // sized round after the first is what distinguishes sustained
                // publishing from an initial burst.
                //
                Assert.Equal(kRoundSize, first.Count);
                Assert.Equal(kRoundSize, second.Count);

                var sequences = first.Concat(second).ToList();
                AssertOrderedAndGapless(sequences);
            }
            finally
            {
                await StopPublisherAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Collects one round and reports what it cost.
        /// </summary>
        /// <param name="name">Round name for the report.</param>
        private async Task<List<uint>> CollectRoundAsync(string name)
        {
            var stopWatch = Stopwatch.StartNew();
            var (_, messages) = await WaitForMessagesAndMetadataAsync(kRoundTimeout,
                kRoundSize, null, "ua-data").ConfigureAwait(false);
            stopWatch.Stop();

            var process = Process.GetCurrentProcess();
            process.Refresh();
            _output.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0} round: {1} messages in {2:N1}s ({3:N1}/s), heap {4:N0} kB, " +
                "handles {5}, threads {6}",
                name, messages.Count, stopWatch.Elapsed.TotalSeconds,
                messages.Count / Math.Max(0.001, stopWatch.Elapsed.TotalSeconds),
                GC.GetTotalMemory(true) / 1024, process.HandleCount, process.Threads.Count));

            return [.. messages
                .Select(message => message.Message)
                .Select(GetSequenceNumber)
                .Where(sequence => sequence.HasValue)
                .Select(sequence => sequence!.Value)];
        }

        /// <summary>
        /// Asserts that a writer's sequence numbers rise by exactly one. A gap
        /// means a message was dropped between the source and the broker, and a
        /// repeat or an inversion means the egress reordered them.
        /// </summary>
        /// <param name="sequences">Observed sequence numbers, in arrival order.</param>
        private static void AssertOrderedAndGapless(List<uint> sequences)
        {
            Assert.NotEmpty(sequences);
            for (var index = 1; index < sequences.Count; index++)
            {
                Assert.True(sequences[index] == sequences[index - 1] + 1,
                    $"Sequence number {sequences[index]} followed {sequences[index - 1]} " +
                    $"at position {index}; the stream must rise by exactly one.");
            }
        }

        private static uint? GetSequenceNumber(JsonElement message)
        {
            if (message.TryGetProperty("SequenceNumber", out var direct) &&
                direct.ValueKind == JsonValueKind.Number)
            {
                return direct.GetUInt32();
            }
            if (!message.TryGetProperty("Messages", out var messages) ||
                messages.ValueKind != JsonValueKind.Array)
            {
                return null;
            }
            foreach (var dataSetMessage in messages.EnumerateArray())
            {
                if (dataSetMessage.TryGetProperty("SequenceNumber", out var sequence) &&
                    sequence.ValueKind == JsonValueKind.Number)
                {
                    return sequence.GetUInt32();
                }
            }
            return null;
        }
    }
}
