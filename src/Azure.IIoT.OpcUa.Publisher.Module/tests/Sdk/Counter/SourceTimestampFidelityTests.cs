// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.Counter
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Telemetry;
    using Azure.IIoT.OpcUa.Core.Logging;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// <para>
    /// Establishes that OPC Publisher reproduces the source timestamp it
    /// received from the server byte for byte, including when that timestamp
    /// is irregular.
    /// </para>
    /// <para>
    /// A customer analysing a three thousand tag, two second stream reported
    /// that no value was ever lost, duplicated or reordered - the counter
    /// increment between consecutive samples was always exactly right - yet
    /// roughly 0.8 % of the distances between consecutive source timestamps
    /// fell outside a 2 % band around two seconds. The displacements were
    /// quantized at about 333 ms, which is one sixth of the scan interval,
    /// and came in self correcting pairs: one distance too long by 333 ms
    /// immediately followed by one too short by the same amount, summing back
    /// to 4000 ms to within a millisecond.
    /// </para>
    /// <para>
    /// That is the signature of a data source whose scan scheduler slips a
    /// slot, not of a pipeline that damages timestamps. The purpose of this
    /// test is to make that distinction provable rather than argued: the
    /// counter server is driven with exactly that defect
    /// (<see cref="global::Counter.CounterServerOptions.SlotSlip"/>) and the
    /// telemetry that comes out the far end of the publisher is compared
    /// against the timestamps the server recorded as having stamped.
    /// </para>
    /// <para>
    /// If the publisher were displacing timestamps, the two would differ. If
    /// the publisher is transparent, they are identical and the customer's
    /// metric reproduces downstream exactly as injected - which localises the
    /// defect upstream of the module.
    /// </para>
    /// </summary>
    [Trait(TestCategories.Name, TestCategories.LongRunning)]
    public sealed class SourceTimestampFidelityTests : PublisherIntegrationTestBase
    {
        public SourceTimestampFidelityTests(ITestOutputHelper output)
            : base(output, TimeSpan.FromMinutes(30), nameof(SourceTimestampFidelityTests))
        {
            _output = output;
        }

        /// <summary>
        /// A source whose scan scheduler slips one slot every so often,
        /// reproducing the reported field signature. The publisher must pass
        /// the displaced timestamps through unchanged: it may neither repair
        /// them nor add displacement of its own.
        /// </summary>
        [SkippableFact]
        public async Task SlippingSourceIsReproducedExactlyAsync()
        {
            SkipUnlessEnabled();
            var report = await RunAsync("slot slipping source", kSlotSlip);

            AssertPublisherIsTransparent(report);

            //
            // The injected defect must be visible downstream, otherwise the
            // comparison above is vacuous - a publisher that dropped every
            // displaced sample would also report zero mismatches.
            //
            Assert.True(report.SlippedPairs > 0,
                $"The source never slipped, so nothing was proven.\n{report}");

            //
            // This is the customer's metric. Every pair that straddles a
            // slipped sample must be outside their band, and no other pair
            // may be. Equality in both directions: the publisher neither
            // hides the defect nor adds to it.
            //
            Assert.Equal(report.SlippedPairs, report.PairsOutsideBand);
        }

        /// <summary>
        /// Control: the same run without the injected slip. It pins that the
        /// publisher contributes no displacement of its own, so a non zero
        /// result in the arm above can only come from the source.
        /// </summary>
        [SkippableFact]
        public async Task RegularSourceProducesNoDisplacementAsync()
        {
            SkipUnlessEnabled();
            var report = await RunAsync("regular source (control)", TimeSpan.Zero);

            AssertPublisherIsTransparent(report);

            Assert.Equal(0, report.SlippedPairs);
            Assert.Equal(0, report.PairsOutsideBand);
        }

        /// <summary>
        /// <para>
        /// Negative control for the two arms above. Their value rests
        /// entirely on the comparison against the server's record being able
        /// to see a difference at all, so this drives the publisher down the
        /// one code path that genuinely does synthesize a source timestamp -
        /// the legacy <c>WatchdogLKVWithUpdatedTimestamps</c> heartbeat,
        /// which resends the last known value with the timestamp shifted
        /// forward by the time elapsed since it was received.
        /// </para>
        /// <para>
        /// The source counts up far slower than the heartbeat interval so
        /// heartbeats are guaranteed to fire. Those messages must be reported
        /// as mismatches, because the timestamp they carry is one the server
        /// never stamped. If this arm reported zero the detector would be
        /// blind and the clean results above would mean nothing.
        /// </para>
        /// </summary>
        [SkippableFact]
        public async Task DetectorSeesSynthesizedTimestampsAsync()
        {
            SkipUnlessEnabled();
            var report = await RunAsync("synthesized timestamps (negative control)",
                TimeSpan.Zero, updateInterval: TimeSpan.FromSeconds(kSlowUpdateSeconds),
                heartbeatSeconds: (int)kInterval.TotalSeconds,
                heartbeatBehavior: "WatchdogLKVWithUpdatedTimestamps");

            Assert.True(report.Compared > 0,
                $"No sample could be compared against the server's record.\n{report}");
            Assert.True(report.TimestampMismatches > 0,
                $"The detector reported no mismatch even though the publisher was " +
                $"configured to synthesize source timestamps, so it is blind and the " +
                $"clean results of the other arms prove nothing.\n{report}");
        }

        /// <summary>
        /// The assertions that must hold whatever the source does.
        /// </summary>
        /// <param name="report"></param>
        private static void AssertPublisherIsTransparent(FidelityReport report)
        {
            Assert.True(report.Samples > 0, "No telemetry was received at all.");

            //
            // The heart of the test. Every source timestamp delivered must be
            // the one the server recorded for that counter value.
            //
            Assert.True(report.TimestampMismatches == 0,
                $"{report.TimestampMismatches} of {report.Compared} sample(s) carried a " +
                $"source timestamp the server never stamped, so the publisher altered " +
                $"it.\n{report}");
            Assert.True(report.Compared > 0,
                $"No sample could be compared against the server's record.\n{report}");

            // Every distance must be exactly what the injected schedule predicts.
            Assert.True(report.UnexplainedPairs == 0,
                $"{report.UnexplainedPairs} consecutive pair(s) were not spaced as the " +
                $"server's own schedule predicts.\n{report}");
        }

        /// <summary>
        /// Run one scenario and measure the telemetry against the server's
        /// record of what it stamped.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="slotSlip"></param>
        /// <param name="updateInterval"></param>
        /// <param name="heartbeatSeconds"></param>
        /// <param name="heartbeatBehavior"></param>
        private async Task<FidelityReport> RunAsync(string label, TimeSpan slotSlip,
            TimeSpan? updateInterval = null, int heartbeatSeconds = 0,
            string heartbeatBehavior = null)
        {
            var interval = updateInterval ?? kInterval;
            using var loggerFactory = Log.ConsoleFactory(LogLevel.Warning);
            // The enclosing namespace is also called Counter, so the options
            // type has to be qualified against the global namespace.
            using var server = new CounterServer(new global::Counter.CounterServerOptions
            {
                NodeCount = kNodeCount,
                UpdateInterval = interval,
                UseScheduledTimestamps = true,
                SlotSlip = slotSlip,
                SlotSlipPeriod = kSlipPeriod,
                SlotSlipDwell = 1
            }, loggerFactory);
            EndpointUrl = server.EndpointUrl;

            var configuration = WriteConfiguration(kNodeCount, kInterval, heartbeatSeconds);
            try
            {
                var arguments = new List<string>
                {
                    "--mm=FullNetworkMessages", "--me=Json", "--bs=50", "--bi=10000"
                };
                if (heartbeatBehavior != null)
                {
                    arguments.Add($"--hbb={heartbeatBehavior}");
                }
                StartPublisher(label, configuration, [.. arguments]);

                // Arrival order per node, so consecutive pairs can be formed.
                var received = new Dictionary<string, List<(long Value, DateTime Ts)>>();

                //
                // Skip a warm up window. Monitored items are not all created
                // at once, so early samples are legitimately partial.
                //
                var warmup = TimeSpan.FromSeconds(30);
                var stopWatch = Stopwatch.StartNew();
                await ConsumeMessagesAsync(warmup + Duration, message =>
                {
                    if (stopWatch.Elapsed < warmup)
                    {
                        return;
                    }
                    //
                    // 3.0 emits OPC UA PubSub network messages. The counter
                    // value and its source timestamp live in each data set
                    // message's payload, keyed by the field name, rather than
                    // in the flat NodeId/Value shape the removed Samples mode
                    // produced. Decoding is shared with TelemetryQualityValidator
                    // so both read the wire identically.
                    //
                    foreach (var (id, value, sourceTimestamp) in
                        TelemetryQualityValidator.ReadCounterSamples(message))
                    {
                        if (sourceTimestamp == null)
                        {
                            continue;
                        }
                        if (!received.TryGetValue(id, out var list))
                        {
                            received.Add(id, list = []);
                        }
                        list.Add((value, sourceTimestamp.Value));
                    }
                }, Ct).ConfigureAwait(false);

                var report = Evaluate(label, received, server, slotSlip, interval);
                _output.WriteLine(report.ToString());
                return report;
            }
            finally
            {
                await StopPublisherAsync().ConfigureAwait(false);
                if (File.Exists(configuration))
                {
                    File.Delete(configuration);
                }
            }
        }

        /// <summary>
        /// Compare the telemetry against the server's own record.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="received"></param>
        /// <param name="server"></param>
        /// <param name="slotSlip"></param>
        /// <param name="updateInterval"></param>
        private static FidelityReport Evaluate(string label,
            Dictionary<string, List<(long Value, DateTime Ts)>> received,
            CounterServer server, TimeSpan slotSlip, TimeSpan updateInterval)
        {
            var options = server.NodeManager.Options;
            var expected = updateInterval.TotalMilliseconds;
            var band = expected * 0.02;

            long samples = 0, compared = 0, mismatches = 0;
            long pairs = 0, slipped = 0, outside = 0, unexplained = 0;
            var worstMismatch = 0.0;
            var examples = new List<string>();

            foreach (var (nodeId, list) in received)
            {
                samples += list.Count;
                for (var i = 0; i < list.Count; i++)
                {
                    var (value, ts) = list[i];

                    // Ground truth: what did the server actually stamp?
                    if (server.NodeManager.TryGetEmitted(value, out var truth))
                    {
                        compared++;
                        var off = Math.Abs((ts - truth).TotalMilliseconds);
                        if (off > kTimestampTolerance)
                        {
                            mismatches++;
                            worstMismatch = Math.Max(worstMismatch, off);
                            AddExample(examples,
                                $"{nodeId}: value {value} arrived with {ts:O} but the " +
                                $"server stamped {truth:O} ({off:F3} ms off)");
                        }
                    }

                    if (i == 0)
                    {
                        continue;
                    }
                    var (prevValue, prevTs) = list[i - 1];
                    if (value != prevValue + 1)
                    {
                        // Not adjacent, so the distance is not comparable.
                        continue;
                    }
                    pairs++;
                    var delta = (ts - prevTs).TotalMilliseconds;

                    //
                    // A pair straddles a slip when exactly one of its two
                    // samples is displaced; then the distance is off by one
                    // slot, in one direction or the other.
                    //
                    var shift =
                        (options.IsSlipped(value) ? slotSlip.TotalMilliseconds : 0) -
                        (options.IsSlipped(prevValue) ? slotSlip.TotalMilliseconds : 0);
                    if (shift != 0)
                    {
                        slipped++;
                    }
                    if (Math.Abs(delta - expected) > band)
                    {
                        outside++;
                    }
                    if (Math.Abs(delta - (expected + shift)) > kTimestampTolerance)
                    {
                        unexplained++;
                        AddExample(examples,
                            $"{nodeId}: {prevValue} -> {value} spaced {delta:F3} ms, " +
                            $"the server's schedule predicts {expected + shift:F3} ms");
                    }
                }
            }

            return new FidelityReport
            {
                Label = label,
                Nodes = received.Count,
                Samples = samples,
                Compared = compared,
                TimestampMismatches = mismatches,
                WorstMismatchMs = worstMismatch,
                Pairs = pairs,
                SlippedPairs = slipped,
                PairsOutsideBand = outside,
                UnexplainedPairs = unexplained,
                Examples = examples
            };
        }

        private static void AddExample(List<string> examples, string example)
        {
            if (examples.Count < 10)
            {
                examples.Add(example);
            }
        }

        /// <summary>
        /// Outcome of one run.
        /// </summary>
        private sealed record class FidelityReport
        {
            public string Label { get; init; }
            public int Nodes { get; init; }
            public long Samples { get; init; }

            /// <summary> Samples checked against the server's record </summary>
            public long Compared { get; init; }

            /// <summary> Samples whose timestamp the server never stamped </summary>
            public long TimestampMismatches { get; init; }
            public double WorstMismatchMs { get; init; }

            /// <summary> Adjacent sample pairs, whose distance is comparable </summary>
            public long Pairs { get; init; }

            /// <summary> Pairs the injected schedule displaces </summary>
            public long SlippedPairs { get; init; }

            /// <summary> Pairs outside the customer's 2 % band </summary>
            public long PairsOutsideBand { get; init; }

            /// <summary> Pairs not spaced as the server's schedule predicts </summary>
            public long UnexplainedPairs { get; init; }

            // The report is rendered, never indexed, so the abstraction costs
            // nothing here and keeps the collection immutable to callers.
#pragma warning disable CA1859
            public IReadOnlyList<string> Examples { get; init; } = [];
#pragma warning restore CA1859

            public override string ToString()
            {
                var pct = Pairs == 0 ? 0 : PairsOutsideBand * 100.0 / Pairs;
                var text =
$@"--- {Label} ---
  nodes / samples        : {Nodes} / {Samples}
  compared to server     : {Compared}
  timestamp mismatches   : {TimestampMismatches} (worst {WorstMismatchMs:F3} ms)
  adjacent pairs         : {Pairs}
  displaced by the source: {SlippedPairs}
  outside 2 % band       : {PairsOutsideBand} ({pct:F2} %)   <-- customer's metric
  unexplained            : {UnexplainedPairs}";
                return Examples.Count == 0 ? text : text + Environment.NewLine +
                    string.Join(Environment.NewLine, Examples.Select(e => "    ! " + e));
            }
        }

        /// <summary>
        /// Write a published nodes configuration for the counter nodes.
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <param name="interval"></param>
        /// <param name="heartbeatSeconds"></param>
        private static string WriteConfiguration(int nodeCount, TimeSpan interval,
            int heartbeatSeconds)
        {
            var path = Path.Combine(Path.GetTempPath(),
                Path.GetRandomFileName() + ".pn.json");
            using var stream = File.Create(path);
            using var writer = new Utf8JsonWriter(stream,
                new JsonWriterOptions { Indented = true });
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("EndpointUrl", "{{EndpointUrl}}");
            writer.WriteBoolean("UseSecurity", false);
            writer.WriteString("DataSetWriterGroup", "{{DataSetWriterGroup}}");
            writer.WriteStartArray("OpcNodes");
            for (var index = 0; index < nodeCount; index++)
            {
                writer.WriteStartObject();
                writer.WriteString("Id", CounterServer.GetNodeId(index));
                writer.WriteString("DataSetFieldId",
                    global::Counter.CounterNodeManager.GetBrowseName(index));
                writer.WriteNumber("OpcPublishingInterval",
                    (int)interval.TotalMilliseconds);
                writer.WriteNumber("OpcSamplingInterval",
                    (int)interval.TotalMilliseconds);
                writer.WriteNumber("QueueSize", 10);
                if (heartbeatSeconds > 0)
                {
                    writer.WriteNumber("HeartbeatInterval", heartbeatSeconds);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.Flush();
            return path;
        }

        private static void SkipUnlessEnabled()
        {
            Skip.IfNot(Environment.GetEnvironmentVariable(kVariable) == "1",
                $"Set {kVariable}=1 to run the source timestamp fidelity test.");
        }

        /// <summary>
        /// How long telemetry is analysed after the warm up. Honours the same
        /// variable as the other long running tests so the pull request smoke
        /// job can run this at a smaller size.
        /// </summary>
        private static TimeSpan Duration
            => TimeSpan.FromMinutes(
                int.TryParse(Environment.GetEnvironmentVariable(kDurationVariable),
                    CultureInfo.InvariantCulture, out var minutes) && minutes > 0
                        ? minutes : 2);

        private const string kVariable = "IIOT_TELEMETRY_SOAK";
        private const string kDurationVariable = "IIOT_TELEMETRY_SOAK_MINUTES";

        /// <summary>
        /// Tolerance for comparing two timestamps that should be identical.
        /// Only covers the serialization round trip, not scheduling.
        /// </summary>
        private const double kTimestampTolerance = 1.0;

        private const int kNodeCount = 50;
        private const int kSlipPeriod = 7;

        /// <summary>
        /// Rate at which the source counts up in the negative control, well
        /// above the heartbeat interval so heartbeats must fire.
        /// </summary>
        private const int kSlowUpdateSeconds = 10;

        /// <summary>
        /// The displacement reported from the field, one sixth of the two
        /// second scan interval.
        /// </summary>
        private static readonly TimeSpan kSlotSlip = TimeSpan.FromMilliseconds(333);
        private static readonly TimeSpan kInterval = TimeSpan.FromSeconds(2);
        private readonly ITestOutputHelper _output;
    }
}
