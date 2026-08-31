// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.Counter
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Telemetry;
    using Furly.Extensions.Logging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// <para>
    /// Long running telemetry quality tests. They reproduce the customer
    /// scenario of thousands of variables published with a two second
    /// publishing interval, a two second sampling interval, a queue size
    /// larger than one and a two second heartbeat, and assert that the
    /// resulting telemetry stream is
    /// </para>
    /// <list type="number">
    /// <item>complete - no counter value is lost,</item>
    /// <item>ordered - a counter never goes backwards,</item>
    /// <item>evenly spaced - consecutive source timestamps are exactly one
    /// update interval apart, and</item>
    /// <item>free of stale values - a value is never repeated after a newer
    /// one was already delivered.</item>
    /// </list>
    /// <para>
    /// The counter server makes this decidable: every variable counts up
    /// from zero by exactly one, so the value carries its own sequence and
    /// its own expected timestamp.
    /// </para>
    /// </summary>
    [Trait(TestCategories.Name, TestCategories.LongRunning)]
    public sealed class TelemetryQualityTests : PublisherIntegrationTestBase
    {
        public TelemetryQualityTests(ITestOutputHelper output)
            : base(output, kTestTimeout, nameof(TelemetryQualityTests))
        {
            _output = output;
        }

        /// <summary>
        /// The customer scenario in samples encoding: publishing interval,
        /// sampling interval and heartbeat interval all two seconds, queue
        /// size larger than one, against a server whose variables count up
        /// every two seconds.
        /// </summary>
        [SkippableFact]
        public async Task SamplesModeStreamIsCompleteOrderedAndEvenlySpacedAsync()
        {
            SkipUnlessEnabled();
            var report = await RunScenarioAsync(
                nameof(SamplesModeStreamIsCompleteOrderedAndEvenlySpacedAsync),
                MessagingMode.FullSamples, NodeCount, kInterval, kInterval, kInterval,
                kQueueSize, (int)kInterval.TotalSeconds, RunDuration).ConfigureAwait(false);

            AssertClean(report);
        }

        /// <summary>
        /// The same scenario in PubSub encoding, as a control that the
        /// behaviour is not specific to the legacy samples encoder.
        /// <see cref="MessagingMode.FullNetworkMessages"/> rather than
        /// <see cref="MessagingMode.PubSub"/> because only the full featured
        /// profiles carry the heartbeat indicator
        /// (<c>MessagingProfile.BuildDataSetFieldContentMask</c>); without it
        /// a heartbeat is indistinguishable from a repeated value and the
        /// assertions below could not tell the two apart.
        /// </summary>
        [SkippableFact]
        public async Task PubSubModeStreamIsCompleteOrderedAndEvenlySpacedAsync()
        {
            SkipUnlessEnabled();
            var report = await RunScenarioAsync(
                nameof(PubSubModeStreamIsCompleteOrderedAndEvenlySpacedAsync),
                MessagingMode.FullNetworkMessages, NodeCount, kInterval, kInterval, kInterval,
                kQueueSize, (int)kInterval.TotalSeconds, RunDuration).ConfigureAwait(false);

            AssertClean(report);
        }

        /// <summary>
        /// Same scenario but with the server producing values four times
        /// faster than the publishing interval, so every publish carries
        /// several queued values per monitored item. This is what a queue
        /// size larger than one is actually for and it exercises the
        /// ordering of queued values through the encoder.
        /// </summary>
        [SkippableFact]
        public async Task QueuedValuesArriveCompleteAndInOrderAsync()
        {
            SkipUnlessEnabled();
            var updateInterval = TimeSpan.FromMilliseconds(500);
            var report = await RunScenarioAsync(
                nameof(QueuedValuesArriveCompleteAndInOrderAsync),
                MessagingMode.FullSamples, NodeCount, updateInterval,
                publishingInterval: kInterval, samplingInterval: updateInterval,
                queueSize: kQueueSize, heartbeatSeconds: (int)kInterval.TotalSeconds,
                duration: RunDuration).ConfigureAwait(false);

            AssertClean(report);
        }

        /// <summary>
        /// <para>
        /// The field configuration behind the duplicate reports: a publishing
        /// interval <em>below</em> the rate at which the source changes.
        /// </para>
        /// <para>
        /// This matters because the watchdog grace period is
        /// <c>min(publishingInterval, heartbeatInterval)</c>. Every other arm
        /// in this suite publishes at the data rate, which yields a two
        /// second grace on a two second heartbeat. Halving the publishing
        /// interval halves the grace to one second, so a value only has to be
        /// one second late for the watchdog to win - the tightest deadline
        /// the product will produce for this heartbeat interval, and until
        /// now untested.
        /// </para>
        /// <para>
        /// A heartbeat here is not harmless: the default <c>WatchdogLKV</c>
        /// behaviour resends the last value with its original timestamps, so
        /// a consumer that cannot read the heartbeat indicator sees an exact
        /// duplicate - same value, same source timestamp, same server
        /// timestamp - and counts it as a data quality defect.
        /// </para>
        /// </summary>
        [SkippableFact]
        public async Task PublishingFasterThanTheSourceDoesNotTriggerHeartbeatsAsync()
        {
            SkipUnlessEnabled();
            var report = await RunScenarioAsync(
                nameof(PublishingFasterThanTheSourceDoesNotTriggerHeartbeatsAsync),
                MessagingMode.FullSamples, NodeCount, kInterval,
                publishingInterval: TimeSpan.FromMilliseconds(kInterval.TotalMilliseconds / 2),
                samplingInterval: kInterval, queueSize: kQueueSize,
                heartbeatSeconds: (int)kInterval.TotalSeconds,
                duration: RunDuration).ConfigureAwait(false);

            AssertClean(report);

            // A duplicate is what the consumer actually complains about.
            Assert.True(report.RepeatedValues == 0,
                $"{report.RepeatedValues} value(s) were delivered twice although the " +
                $"source produced a new value on every cycle.\n{report}");
        }

        /// <summary>
        /// The customer scenario at the reported scale of three thousand
        /// nodes. Opt in because it needs a machine that can host the server
        /// and the publisher side by side.
        /// </summary>
        [SkippableFact]
        public async Task CustomerScenarioAtFullScaleAsync()
        {
            Skip.IfNot(FullScaleEnabled,
                $"Set {kFullScaleVariable}=1 to run the full scale scenario.");

            var report = await RunScenarioAsync(
                nameof(CustomerScenarioAtFullScaleAsync),
                MessagingMode.FullSamples, kFullScaleNodeCount, kInterval, kInterval,
                kInterval, kQueueSize, (int)kInterval.TotalSeconds,
                RunDuration).ConfigureAwait(false);

            AssertClean(report);
        }

        /// <summary>
        /// <para>
        /// The customer scenario with the legacy
        /// <c>WatchdogLKVWithUpdatedTimestamps</c> heartbeat behaviour, which
        /// is the only behaviour in the product that <em>synthesizes</em> a
        /// source timestamp: it re-sends the last known value with
        /// </para>
        /// <code>
        /// SourceTimestamp += (timerFireTime - valueReceiveTime)
        /// </code>
        /// <para>
        /// The shift is measured on the publisher's wall clock while the
        /// timestamp it is added to comes from the server clock, so every bit
        /// of receive path jitter lands in the emitted source timestamp. The
        /// result is not a duplicate timestamp, which means a consumer that
        /// does not evaluate the heartbeat indicator cannot tell such a
        /// message apart from a real sample - it simply sees the source
        /// timestamp distance jump around, and where a heartbeat lands close
        /// to a real value, go backwards.
        /// </para>
        /// <para>
        /// Measured against the pre grace period watchdog this arm produced
        /// roughly three in ten samples as heartbeats and the same share of
        /// source timestamp distances outside a two percent band, including
        /// negative ones. It is therefore the sharpest available regression
        /// detector for the watchdog race and the reason it is exercised here
        /// even though the behaviour is legacy.
        /// </para>
        /// <para>
        /// Note that when the watchdog behaves correctly this arm sees no
        /// heartbeat at all, so the shift arithmetic never runs. Coverage of
        /// the arithmetic itself is the job of
        /// <see cref="SlowSourceUpdatedTimestampHeartbeatsShiftTimestampsAsync"/>;
        /// what this arm establishes is that the behaviour does not cause the
        /// watchdog to fire in the first place.
        /// </para>
        /// </summary>
        [SkippableFact]
        public async Task UpdatedTimestampHeartbeatDoesNotDisplaceTimestampsAsync()
        {
            SkipUnlessEnabled();
            var report = await RunScenarioAsync(
                nameof(UpdatedTimestampHeartbeatDoesNotDisplaceTimestampsAsync),
                MessagingMode.FullSamples, NodeCount, kInterval, kInterval, kInterval,
                kQueueSize, (int)kInterval.TotalSeconds, RunDuration,
                HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps).ConfigureAwait(false);

            AssertClean(report);
        }

        /// <summary>
        /// <para>
        /// A slowly changing counter, where heartbeats are expected and
        /// wanted. With the default <c>WatchdogLKV</c> behaviour the resent
        /// value carries the original source timestamp, so the timestamp
        /// sequence a consumer observes never moves backwards.
        /// </para>
        /// <para>
        /// This is deterministic rather than load dependent: the server
        /// simply does not produce a value for
        /// <see cref="kSlowUpdateSeconds"/> seconds, so the watchdog must
        /// report regardless of how busy the machine is. It is the
        /// counterpart to
        /// <see cref="UpdatedTimestampHeartbeatDoesNotDisplaceTimestampsAsync"/>,
        /// which asserts that a heartbeat configured at the sampling interval
        /// does <em>not</em> fire while data flows.
        /// </para>
        /// </summary>
        [SkippableFact]
        public async Task SlowSourceHeartbeatsKeepTimestampsOrderedAsync()
        {
            SkipUnlessEnabled();
            var report = await RunSlowSourceScenarioAsync(
                nameof(SlowSourceHeartbeatsKeepTimestampsOrderedAsync),
                heartbeatBehavior: null).ConfigureAwait(false);

            AssertValuesAreCleanAndHeartbeatsWellFormed(report);

            // The default behaviour resends the value untouched.
            Assert.True(report.HeartbeatsWithChangedTimestamp == 0,
                $"{report.HeartbeatsWithChangedTimestamp} heartbeat(s) changed the source " +
                $"timestamp of the value they resend although the default WatchdogLKV " +
                $"behaviour is configured.\n{report}");

            //
            // Symptom (d) as the customer observes it: an "old" message
            // arriving after a newer one. Must never happen with the default
            // behaviour, no matter how many heartbeats fire.
            //
            Assert.True(report.SourceTimestampRegressions == 0,
                $"{report.SourceTimestampRegressions} message(s) carried a source " +
                $"timestamp earlier than the message before it.\n{report}");
        }

        /// <summary>
        /// <para>
        /// The same slow counter with the legacy
        /// <c>WatchdogLKVWithUpdatedTimestamps</c> behaviour, which is where
        /// the shift arithmetic in <c>SendHeartbeatNotification</c> actually
        /// runs. It pins the two properties that make the behaviour
        /// observably different from the default:
        /// </para>
        /// <list type="number">
        /// <item>every heartbeat carries a <em>changed</em> source timestamp,
        /// so it is not a duplicate and a consumer that cannot evaluate the
        /// heartbeat indicator cannot recognise it;</item>
        /// <item>the source timestamp sequence can move <em>backwards</em>,
        /// which is customer symptom (d).</item>
        /// </list>
        /// <para>
        /// The second property is inherent to the design rather than a
        /// defect. The shift added is <c>now - receiveTime</c>, and
        /// <c>receiveTime</c> lags the value's own source timestamp by the
        /// publish cycle phase, the network round trip and processing. A
        /// heartbeat that fires shortly before the next real value therefore
        /// carries a timestamp slightly <em>beyond</em> the one that value
        /// will carry, and the consumer sees the sequence go backwards. This
        /// arm deliberately does not assert that away - it records it, so the
        /// documented recommendation to prefer <c>WatchdogLKV</c> when source
        /// timestamp spacing is analysed stays evidence backed.
        /// </para>
        /// </summary>
        [SkippableFact]
        public async Task SlowSourceUpdatedTimestampHeartbeatsShiftTimestampsAsync()
        {
            SkipUnlessEnabled();
            var report = await RunSlowSourceScenarioAsync(
                nameof(SlowSourceUpdatedTimestampHeartbeatsShiftTimestampsAsync),
                HeartbeatBehavior.WatchdogLKVWithUpdatedTimestamps).ConfigureAwait(false);

            //
            // Positive control: without this the assertions below would hold
            // vacuously on a build where the shift silently stopped running.
            //
            Assert.True(report.HeartbeatsWithChangedTimestamp == report.HeartbeatSamples,
                $"only {report.HeartbeatsWithChangedTimestamp} of {report.HeartbeatSamples} " +
                $"heartbeat(s) carried a shifted source timestamp, so the shift under test " +
                $"did not run for all of them.\n{report}");

            //
            // Whatever the shift does to the timestamps, the value stream
            // itself must stay complete, ordered and correctly flagged.
            //
            AssertValuesAreCleanAndHeartbeatsWellFormed(report);

            _output.WriteLine(
                $"Source timestamp regressions caused by the shift: " +
                $"{report.SourceTimestampRegressions} of {report.TotalSamples} sample(s)");
        }

        /// <summary>
        /// Run the slow source scenario: the server counts up several times
        /// slower than the heartbeat interval, so heartbeats are guaranteed
        /// to fire.
        /// </summary>
        /// <param name="test"></param>
        /// <param name="heartbeatBehavior"></param>
        private async Task<TelemetryQualityReport> RunSlowSourceScenarioAsync(string test,
            HeartbeatBehavior? heartbeatBehavior)
        {
            var report = await RunScenarioAsync(test, MessagingMode.FullSamples,
                NodeCount, TimeSpan.FromSeconds(kSlowUpdateSeconds),
                publishingInterval: kInterval, samplingInterval: kInterval,
                queueSize: kQueueSize, heartbeatSeconds: (int)kInterval.TotalSeconds,
                duration: RunDuration, heartbeatBehavior: heartbeatBehavior)
                .ConfigureAwait(false);

            Assert.True(report.HeartbeatSamples > 0,
                $"No heartbeat was emitted although the server only counted up every " +
                $"{kSlowUpdateSeconds} s with a {kInterval.TotalSeconds} s heartbeat.\n{report}");
            return report;
        }

        /// <summary>
        /// Run one scenario and return the resulting quality report.
        /// </summary>
        /// <param name="test"></param>
        /// <param name="mode"></param>
        /// <param name="nodeCount"></param>
        /// <param name="updateInterval">Rate at which the server counts up</param>
        /// <param name="publishingInterval"></param>
        /// <param name="samplingInterval"></param>
        /// <param name="queueSize"></param>
        /// <param name="heartbeatSeconds"></param>
        /// <param name="duration">How long telemetry is analysed</param>
        /// <param name="heartbeatBehavior">Optional <c>--hbb</c> value</param>
        private async Task<TelemetryQualityReport> RunScenarioAsync(string test,
            MessagingMode mode, int nodeCount, TimeSpan updateInterval,
            TimeSpan publishingInterval, TimeSpan samplingInterval, uint queueSize,
            int heartbeatSeconds, TimeSpan duration,
            HeartbeatBehavior? heartbeatBehavior = null)
        {
            using var loggerFactory = Log.ConsoleFactory(LogLevel.Warning);
            using var server = CounterServer.Create(nodeCount, updateInterval, loggerFactory);
            EndpointUrl = server.EndpointUrl;

            var configuration = WriteConfiguration(nodeCount, publishingInterval,
                samplingInterval, queueSize, heartbeatSeconds);
            try
            {
                var arguments = new List<string>
                {
                    $"--mm={mode}",
                    "--me=Json",
                    //
                    // Pin the batching defaults so the scenario does not
                    // silently change when the shipped defaults change.
                    //
                    "--bs=50",
                    "--bi=10000"
                };
                if (heartbeatBehavior != null)
                {
                    arguments.Add($"--hbb={heartbeatBehavior}");
                }
                StartPublisher(test, configuration, [.. arguments]);

                //
                // An unrecognised command line option is only warned about,
                // not rejected, so a renamed switch would silently leave the
                // default behaviour in place and every assertion below would
                // still hold - the test would pass while covering nothing.
                // Assert the option actually took effect.
                //
                if (heartbeatBehavior != null)
                {
                    var options = ResolveFromPublisher<IOptions<OpcUaSubscriptionOptions>>();
                    Assert.Equal(heartbeatBehavior,
                        options?.Value.DefaultHeartbeatBehavior);
                }

                var validator = new TelemetryQualityValidator(new TelemetryQualityOptions
                {
                    UpdateInterval = updateInterval,
                    ExpectedNodeCount = nodeCount,
                    HeartbeatInterval = TimeSpan.FromSeconds(heartbeatSeconds),
                    PublishingInterval = publishingInterval
                });
                var samples = mode is MessagingMode.Samples or MessagingMode.FullSamples;

                //
                // Skip the warm up. Creating thousands of monitored items
                // takes time and until all of them exist the stream is
                // legitimately partial.
                //
                var warmup = WarmupFor(nodeCount);
                var stopWatch = Stopwatch.StartNew();
                var total = await ConsumeMessagesAsync(warmup + duration, message =>
                {
                    if (stopWatch.Elapsed < warmup)
                    {
                        return;
                    }
                    if (samples)
                    {
                        validator.AddSamplesMessage(message);
                    }
                    else
                    {
                        validator.AddPubSubMessage(message);
                    }
                }, Ct).ConfigureAwait(false);

                var report = validator.CreateReport();
                _output.WriteLine($"--- {test} ({mode}, {nodeCount} nodes, " +
                    $"{updateInterval.TotalMilliseconds} ms update, {duration} analysed, " +
                    $"{total} messages) ---");
                _output.WriteLine(report.ToString());
                _output.WriteLine(DumpDiagnostics());
                _output.WriteLine($"Server produced values 0..{server.NodeManager.CurrentValue}");
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
        /// <para>
        /// Assert that none of the four reported symptoms occurred.
        /// </para>
        /// <para>
        /// Note what is deliberately <em>not</em> asserted: that no heartbeat
        /// fired at all. A heartbeat is correct behaviour whenever a value
        /// genuinely fails to arrive within the watchdog deadline, and on a
        /// loaded shared build agent that does happen - the agent runs the
        /// whole solution, and the counter server, the publisher and every
        /// other test compete for the same cores. Demanding zero heartbeats
        /// therefore asserts something about the machine, not about the
        /// product.
        /// </para>
        /// <para>
        /// The precise, load independent signal for the regression this suite
        /// guards is <see cref="TelemetryQualityReport.EarlyHeartbeats"/>: a
        /// heartbeat that fired <em>before</em> the item had been silent for
        /// the heartbeat interval plus one publishing interval is always a
        /// defect, because that is the earliest point at which the absence of
        /// data can be established. The bug produced heartbeats roughly two
        /// seconds after a value while the deadline was four, so every one of
        /// them counts here; a stalled machine, by contrast, produces
        /// heartbeats whose idle time genuinely exceeds the deadline and none
        /// of them count.
        /// </para>
        /// </summary>
        /// <param name="report"></param>
        private static void AssertClean(TelemetryQualityReport report)
        {
            AssertValuesAreCleanAndHeartbeatsWellFormed(report);

            //
            // Values arrive on every publish cycle, so heartbeats should be
            // rare. A generous ceiling still catches a systematic regression
            // (the bug emitted one on roughly six out of ten cycles) while
            // tolerating the occasional genuine stall on a busy agent.
            //
            var budget = Math.Max(kMinimumHeartbeatBudget, report.ValueSamples / 4);
            Assert.True(report.HeartbeatSamples <= budget,
                $"{report.HeartbeatSamples} heartbeat(s) were emitted although a value was " +
                $"produced on every publish cycle, which exceeds the ceiling of {budget}.\n{report}");
        }

        /// <summary>
        /// The subset of <see cref="AssertClean"/> that holds regardless of
        /// how often the server produces a value, and therefore also applies
        /// to scenarios in which heartbeats are expected to fire. It asserts
        /// that the value stream is complete, ordered and evenly spaced, and
        /// that every heartbeat is well formed - flagged as such and not
        /// emitted before the watchdog deadline.
        /// </summary>
        /// <param name="report"></param>
        private static void AssertValuesAreCleanAndHeartbeatsWellFormed(
            TelemetryQualityReport report)
        {
            Assert.True(report.TotalSamples > 0, "No telemetry was received at all.");
            Assert.True(report.NodesMissing == 0,
                $"{report.NodesMissing} node(s) never reported a value.\n{report}");

            // (a) values the customer expects but never sees
            Assert.True(report.MissingValues == 0,
                $"{report.MissingValues} value(s) were lost.\n{report}");

            // (b) values arriving out of order
            Assert.True(report.OutOfOrderValues == 0,
                $"{report.OutOfOrderValues} value(s) arrived out of order.\n{report}");
            Assert.True(report.OutOfOrderIncludingHeartbeats == 0,
                $"{report.OutOfOrderIncludingHeartbeats} message(s) carried a value older " +
                $"than one already delivered.\n{report}");

            // (c) real value changes are exactly one update interval apart
            Assert.True(report.SamplesWithoutSourceTimestamp == 0,
                $"{report.SamplesWithoutSourceTimestamp} message(s) had no source " +
                $"timestamp.\n{report}");
            Assert.True(report.ValueIntervalViolations == 0,
                $"{report.ValueIntervalViolations} value(s) were not one update interval " +
                $"apart from their predecessor.\n{report}");

            // (d) the watchdog never fires before it is allowed to
            Assert.True(report.EarlyHeartbeats == 0,
                $"{report.EarlyHeartbeats} heartbeat(s) fired before the item had been " +
                $"silent for the heartbeat interval plus one publishing interval.\n{report}");

            // (d) a repeated value always identifies itself as a heartbeat
            Assert.True(report.UnflaggedRepeats == 0,
                $"{report.UnflaggedRepeats} value(s) were repeated without the heartbeat " +
                $"indicator, so a consumer cannot tell them apart from real data.\n{report}");
        }

        /// <summary>
        /// Skip unless the soak was explicitly opted into. These tests run for
        /// minutes and are sensitive to how loaded the machine is, so they
        /// must not run as part of an ordinary solution wide test pass - in
        /// particular the internal build, which runs the whole solution and
        /// cannot be filtered from this repository.
        /// </summary>
        private static void SkipUnlessEnabled()
        {
            Skip.IfNot(SoakEnabled,
                $"Set {kSoakVariable}=1 to run the long running telemetry quality soak.");
        }

        /// <summary>
        /// Render the publisher's own view of the run so a failure can be
        /// attributed to a dropped notification or a server side queue
        /// overflow rather than to the encoder.
        /// </summary>
        private string DumpDiagnostics()
        {
            var collector = ResolveFromPublisher<IDiagnosticCollector>();
            if (collector == null)
            {
                return "No diagnostics available.";
            }
            var builder = new StringBuilder();
            foreach (var (writerGroup, diagnostic) in collector.EnumerateDiagnostics())
            {
                builder
                    .AppendLine(CultureInfo.InvariantCulture, $"Writer group '{writerGroup}':")
                    .AppendLine(CultureInfo.InvariantCulture, $"  ingress data changes      : {diagnostic.IngressDataChanges}")
                    .AppendLine(CultureInfo.InvariantCulture, $"  ingress value changes     : {diagnostic.IngressValueChanges}")
                    .AppendLine(CultureInfo.InvariantCulture, $"  ingress heartbeats        : {diagnostic.IngressHeartbeats}")
                    .AppendLine(CultureInfo.InvariantCulture, $"  ingress dropped           : {diagnostic.IngressNotificationsDropped}")
                    .AppendLine(CultureInfo.InvariantCulture, $"  encoder dropped           : {diagnostic.EncoderNotificationsDropped}")
                    .AppendLine(CultureInfo.InvariantCulture, $"  outgress buffer dropped   : {diagnostic.OutgressInputBufferDropped}")
                    .AppendLine(CultureInfo.InvariantCulture, $"  server queue overflows    : {diagnostic.ServerQueueOverflows}")
                    .AppendLine(CultureInfo.InvariantCulture, $"  messages sent             : {diagnostic.OutgressIoTMessageCount}")
                    .AppendLine(CultureInfo.InvariantCulture, $"  monitored nodes ok/bad    : {diagnostic.MonitoredOpcNodesSucceededCount}/{diagnostic.MonitoredOpcNodesFailedCount}");
            }
            return builder.Length == 0 ? "No writer group diagnostics." : builder.ToString();
        }

        /// <summary>
        /// Write a published nodes configuration reproducing the customer
        /// configuration for the given number of counter nodes.
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <param name="publishingInterval"></param>
        /// <param name="samplingInterval"></param>
        /// <param name="queueSize"></param>
        /// <param name="heartbeatSeconds"></param>
        private static string WriteConfiguration(int nodeCount, TimeSpan publishingInterval,
            TimeSpan samplingInterval, uint queueSize, int heartbeatSeconds)
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
                for (var index = 0; index < nodeCount; index++)
                {
                    writer.WriteStartObject();
                    writer.WriteString("Id", CounterServer.GetNodeId(index));
                    writer.WriteNumber("OpcPublishingInterval",
                        (int)publishingInterval.TotalMilliseconds);
                    writer.WriteNumber("OpcSamplingInterval",
                        (int)samplingInterval.TotalMilliseconds);
                    writer.WriteNumber("HeartbeatInterval", heartbeatSeconds);
                    writer.WriteNumber("QueueSize", queueSize);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteEndArray();
                writer.Flush();
            }
            return path;
        }

        /// <summary>
        /// Time given to the publisher to create every monitored item before
        /// telemetry is analysed.
        /// </summary>
        /// <param name="nodeCount"></param>
        private static TimeSpan WarmupFor(int nodeCount)
        {
            return TimeSpan.FromSeconds(30) +
                TimeSpan.FromMilliseconds(10 * nodeCount);
        }

        /// <summary>
        /// Number of counter nodes to publish
        /// </summary>
        private static int NodeCount
            => GetPositiveInt(kNodeCountVariable, kDefaultNodeCount);

        /// <summary>
        /// How long telemetry is analysed after the warm up
        /// </summary>
        private static TimeSpan RunDuration
            => TimeSpan.FromMinutes(GetPositiveInt(kDurationVariable,
                kDefaultDurationMinutes));

        /// <summary>
        /// Whether the soak was opted into. Setting the node count or the
        /// duration also opts in, so an operator who configures the run does
        /// not additionally have to remember the switch.
        /// </summary>
        private static bool SoakEnabled
            => Environment.GetEnvironmentVariable(kSoakVariable) == "1" ||
               Environment.GetEnvironmentVariable(kNodeCountVariable) != null ||
               Environment.GetEnvironmentVariable(kDurationVariable) != null ||
               FullScaleEnabled;

        /// <summary>
        /// Whether the full scale scenario was opted into
        /// </summary>
        private static bool FullScaleEnabled
            => Environment.GetEnvironmentVariable(kFullScaleVariable) == "1";

        private static int GetPositiveInt(string variable, int fallback)
        {
            return int.TryParse(Environment.GetEnvironmentVariable(variable),
                CultureInfo.InvariantCulture, out var value) && value > 0
                    ? value : fallback;
        }

        private const string kSoakVariable = "IIOT_TELEMETRY_SOAK";
        private const string kNodeCountVariable = "IIOT_TELEMETRY_SOAK_NODES";
        private const string kDurationVariable = "IIOT_TELEMETRY_SOAK_MINUTES";
        private const string kFullScaleVariable = "IIOT_TELEMETRY_SOAK_FULLSCALE";

        /// <summary>
        /// Heartbeats tolerated regardless of stream size, so a short run is
        /// not failed by a single genuine stall.
        /// </summary>
        private const int kMinimumHeartbeatBudget = 10;

        /// <summary>
        /// Rate at which the server counts up in the scenario that requires
        /// heartbeats to fire. Several multiples of the heartbeat interval so
        /// the watchdog must report regardless of machine load.
        /// </summary>
        private const int kSlowUpdateSeconds = 10;
        private const int kDefaultNodeCount = 500;        private const int kDefaultDurationMinutes = 2;
        private const int kFullScaleNodeCount = 3000;
        private const uint kQueueSize = 10;
        private static readonly TimeSpan kInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan kTestTimeout = TimeSpan.FromHours(4);
        private readonly ITestOutputHelper _output;
    }
}
