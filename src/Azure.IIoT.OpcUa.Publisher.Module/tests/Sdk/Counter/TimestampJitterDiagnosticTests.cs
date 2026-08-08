// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.Counter
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
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
    /// Characterises the <em>distribution</em> of source timestamp deltas
    /// rather than asserting a pass/fail against a tolerance.
    /// </para>
    /// <para>
    /// A customer reported that with a 2 s sampling interval no value is lost
    /// or duplicated, but a large number of consecutive source timestamps sit
    /// outside a 2 % band around 2 s. The reported deltas come in
    /// self-correcting pairs - one sample roughly 680 ms late followed by one
    /// roughly 680 ms early, summing back to 4000 ms - which means a single
    /// sample is displaced on an otherwise regular grid and no time is lost.
    /// </para>
    /// <para>
    /// The existing soak tests cannot see this. The in-process counter server
    /// derives its timestamps from a strictly monotonic schedule, so it is
    /// incapable of exhibiting timer jitter, and the end to end test allows a
    /// 1 s tolerance on a 2 s interval - twenty five times looser than the
    /// customer's 2 % - so a 680 ms displacement passes trivially.
    /// </para>
    /// <para>
    /// This test closes that gap by running the counter server with
    /// <see cref="CounterServerOptions.UseScheduledTimestamps"/> disabled, so
    /// it stamps values from the wall clock at the moment the tick fires,
    /// exactly like a real server. It then reports the delta distribution so
    /// the jitter contributed by a timer driven source plus the publisher can
    /// be quantified and attributed.
    /// </para>
    /// </summary>
    [Trait(TestCategories.Name, TestCategories.LongRunning)]
    public sealed class TimestampJitterDiagnosticTests : PublisherIntegrationTestBase
    {
        public TimestampJitterDiagnosticTests(ITestOutputHelper output)
            : base(output, TimeSpan.FromMinutes(30), nameof(TimestampJitterDiagnosticTests))
        {
            _output = output;
        }

        /// <summary>
        /// Server stamps from the wall clock, like a real OPC UA server.
        /// </summary>
        [SkippableFact]
        public async Task WallClockServerJitterAsync()
        {
            Skip.IfNot(Enabled, $"Set {kVariable}=1 to run the jitter diagnostic.");
            await RunAsync("wall-clock source", useScheduledTimestamps: false);
        }

        /// <summary>
        /// Control: server stamps from a monotonic schedule. Any jitter seen
        /// here would have to have been introduced by the publisher, because
        /// the source timestamps are perfect by construction.
        /// </summary>
        [SkippableFact]
        public async Task MonotonicServerControlAsync()
        {
            Skip.IfNot(Enabled, $"Set {kVariable}=1 to run the jitter diagnostic.");
            await RunAsync("monotonic source (control)", useScheduledTimestamps: true);
        }

        /// <summary>
        /// <para>
        /// The customer's actual configuration: a monotonic (perfect) source,
        /// plus a two second heartbeat using
        /// <c>WatchdogLKVWithUpdatedTimestamps</c>.
        /// </para>
        /// <para>
        /// That behaviour is the one place in the product that synthesizes a
        /// source timestamp. It re-sends the last known value with
        /// </para>
        /// <code>
        /// SourceTimestamp += (timerFireTime - valueReceiveTime)
        /// </code>
        /// <para>
        /// which adds an interval measured on the publisher's wall clock to a
        /// timestamp taken from the server's clock. The constant offset
        /// between the two clocks cancels, but every bit of receive path
        /// jitter - network latency, publish cycle phase, processing delay -
        /// is injected straight into the emitted source timestamp.
        /// </para>
        /// <para>
        /// The source here is monotonic, so the source contributes exactly
        /// zero jitter. Anything this arm reports is therefore attributable to
        /// the publisher.
        /// </para>
        /// </summary>
        [SkippableFact]
        public async Task MonotonicSourceWithUpdatedTimestampHeartbeatAsync()
        {
            Skip.IfNot(Enabled, $"Set {kVariable}=1 to run the jitter diagnostic.");
            await RunAsync("monotonic source + WatchdogLKVWithUpdatedTimestamps",
                useScheduledTimestamps: true, heartbeatSeconds: 2,
                heartbeatBehavior: "WatchdogLKVWithUpdatedTimestamps");
        }

        private async Task RunAsync(string label, bool useScheduledTimestamps,
            int heartbeatSeconds = 0, string heartbeatBehavior = null)
        {
            var interval = TimeSpan.FromSeconds(2);
            var nodeCount = 50;

            using var loggerFactory = Log.ConsoleFactory(LogLevel.Warning);
            // The enclosing namespace is also called Counter, so the options
            // type has to be qualified against the global namespace.
            using var server = new CounterServer(new global::Counter.CounterServerOptions
            {
                NodeCount = nodeCount,
                UpdateInterval = interval,
                UseScheduledTimestamps = useScheduledTimestamps
            }, loggerFactory);
            EndpointUrl = server.EndpointUrl;

            var configuration = WriteConfiguration(nodeCount, interval, heartbeatSeconds);
            try
            {
                var args = new List<string>
                {
                    "--mm=FullSamples", "--me=Json", "--bs=50", "--bi=10000"
                };
                if (heartbeatBehavior != null)
                {
                    args.Add($"--hbb={heartbeatBehavior}");
                }
                StartPublisher(label, configuration, args.ToArray());

                // deltas per node, in milliseconds
                var last = new Dictionary<string, DateTime>();
                var deltas = new List<double>();
                var heartbeats = 0L;
                var total = 0L;

                //
                // Skip a warm up window. Monitored items are not all created
                // at once, so the first delta of a node spans its creation
                // and is legitimately larger than the interval. Counting it
                // would put a floor of one outlier per node under the very
                // metric this diagnostic reports.
                //
                var warmup = TimeSpan.FromSeconds(30);
                var stopWatch = Stopwatch.StartNew();
                await ConsumeMessagesAsync(warmup + TimeSpan.FromMinutes(4), message =>
                {
                    if (!message.TryGetProperty("NodeId", out var n) ||
                        !message.TryGetProperty("Value", out var v) ||
                        !v.TryGetProperty("SourceTimestamp", out var ts) ||
                        !ts.TryGetDateTime(out var parsed))
                    {
                        return;
                    }
                    if (stopWatch.Elapsed < warmup)
                    {
                        return;
                    }
                    total++;
                    if (message.TryGetProperty("Heartbeat", out var hb) &&
                        hb.ValueKind == JsonValueKind.True)
                    {
                        heartbeats++;
                    }
                    //
                    // Deliberately include heartbeats. Under
                    // WatchdogLKVWithUpdatedTimestamps a heartbeat carries a
                    // shifted, non-duplicate timestamp, so a consumer that
                    // does not inspect the Heartbeat indicator - which only
                    // exists in the full featured profiles - cannot tell it
                    // apart from a real sample. This is what the customer's
                    // query sees.
                    //
                    var id = n.GetString()!;
                    parsed = parsed.ToUniversalTime();
                    if (last.TryGetValue(id, out var prev))
                    {
                        deltas.Add((parsed - prev).TotalMilliseconds);
                    }
                    last[id] = parsed;
                }, Ct).ConfigureAwait(false);

                _output.WriteLine($"  samples={total}, heartbeats={heartbeats}");
                Report(label, deltas, interval);
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
        /// Print the delta distribution and the customer's own metric: how
        /// many consecutive deltas fall outside a 2 % band around the
        /// expected interval.
        /// </summary>
        private void Report(string label, List<double> deltas, TimeSpan interval)
        {
            _output.WriteLine($"=== {label}: {deltas.Count} delta(s) ===");
            if (deltas.Count == 0)
            {
                _output.WriteLine("  no telemetry");
                return;
            }
            var expected = interval.TotalMilliseconds;
            var sorted = deltas.OrderBy(d => d).ToList();

            double P(double q) => sorted[Math.Min(sorted.Count - 1,
                (int)(q * sorted.Count))];

            var outside2pct = deltas.Count(d => Math.Abs(d - expected) > expected * 0.02);
            var outside10pct = deltas.Count(d => Math.Abs(d - expected) > expected * 0.10);

            _output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  min={sorted[0]:F1}  p50={P(0.50):F1}  p95={P(0.95):F1}  " +
                $"p99={P(0.99):F1}  max={sorted[^1]:F1} ms"));
            _output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  mean={deltas.Average():F1} ms (expected {expected:F0})"));
            _output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  outside 2%  ({expected * 0.02:F0} ms): {outside2pct} " +
                $"({100.0 * outside2pct / deltas.Count:F2} %)   <-- customer's metric"));
            _output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  outside 10% ({expected * 0.10:F0} ms): {outside10pct} " +
                $"({100.0 * outside10pct / deltas.Count:F2} %)"));

            // Self-correcting pairs: a late delta immediately followed by an
            // early one that returns the running total to 2 x interval. That
            // signature means a single sample was displaced on a regular grid
            // rather than the grid itself drifting.
            var pairs = 0;
            for (var i = 0; i < deltas.Count - 1; i++)
            {
                if (deltas[i] > expected * 1.1 && deltas[i + 1] < expected * 0.9 &&
                    Math.Abs(deltas[i] + deltas[i + 1] - 2 * expected) < expected * 0.05)
                {
                    pairs++;
                }
            }
            _output.WriteLine($"  self-correcting late/early pairs: {pairs}");
        }

        private static string WriteConfiguration(int nodeCount, TimeSpan interval,
            int heartbeatSeconds)
        {
            var path = Path.Combine(Path.GetTempPath(),
                Path.GetRandomFileName() + ".pn.json");
            using (var stream = File.Create(path))
            {
                using var writer = new Utf8JsonWriter(stream);
                writer.WriteStartArray();
                writer.WriteStartObject();
                writer.WriteString("EndpointUrl", "{{EndpointUrl}}");
                writer.WriteBoolean("UseSecurity", false);
                writer.WriteString("DataSetWriterGroup", "{{DataSetWriterGroup}}");
                writer.WriteStartArray("OpcNodes");
                for (var i = 0; i < nodeCount; i++)
                {
                    writer.WriteStartObject();
                    writer.WriteString("Id", CounterServer.GetNodeId(i));
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
            }
            return path;
        }

        private static bool Enabled
            => Environment.GetEnvironmentVariable(kVariable) == "1";

        private const string kVariable = "IIOT_JITTER_DIAG";
        private readonly ITestOutputHelper _output;
    }
}
