// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.Standalone
{
    using OpcPublisherAEE2ETests.TestExtensions;
    using Azure.IIoT.OpcUa.Publisher.Testing.Telemetry;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// <para>
    /// The complement of
    /// <see cref="FTelemetryQualityCountersTestTheory"/>: nodes that change
    /// only every two minutes while the publisher is configured with a ten
    /// second heartbeat. Here heartbeats <em>must</em> fire - that is the
    /// whole point of the feature - and this test proves that the watchdog
    /// grace period did not break them.
    /// </para>
    /// <para>
    /// It also proves that firing heartbeats do not reproduce the two symptoms
    /// the customer reported. A heartbeat legitimately resends the last value
    /// with its original source timestamp, so a consumer that ignores the
    /// heartbeat indicator sees a repeated value and a zero length source
    /// timestamp gap. What must hold is that
    /// </para>
    /// <list type="bullet">
    /// <item>every repeat carries the <c>Heartbeat</c> indicator, so a
    /// consumer can filter them out,</item>
    /// <item>a heartbeat never carries a value or a timestamp older than one
    /// already delivered, and</item>
    /// <item>with heartbeats excluded, the real value stream is still
    /// complete, ordered and exactly two minutes apart.</item>
    /// </list>
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName,
        TestConstants.TraitConstants.PublisherModeSoakHeartbeatTraitValue)]
    public sealed class FTelemetryQualityHeartbeatTestTheory : SoakTestBase,
        IClassFixture<IIoTStandaloneTestContext>
    {
        public FTelemetryQualityHeartbeatTestTheory(IIoTStandaloneTestContext context,
            ITestOutputHelper output)
            : base(context, output, kModuleName, kDeploymentName,
                TestConstants.SoakHeartbeatConsumerGroupName)
        {
            _output = output;
        }

        [Fact, PriorityOrder(0)]
        public async Task TestDeployPublisher()
        {
            await DeployPublisherAsync();
        }

        [Fact, PriorityOrder(1)]
        public async Task TestHeartbeatsFireWithoutProducingStaleOrUnevenlySpacedValuesAsync()
        {
            var nodeCount = TestConstants.Soak.SlowNodeCount;
            var updateInterval = TestConstants.Soak.SlowUpdateInterval;
            var heartbeatInterval = TestConstants.Soak.SlowHeartbeatInterval;

            // Arrange - an OPC PLC whose slow nodes count up every two minutes.
            // The fast node family is left at its default and simply not
            // subscribed to.
            await TestHelper.CreateSimulationContainerAsync(Context, new List<string>
            {
                "/bin/sh", "-c", string.Join(' ',
                    "./opcplc", "--autoaccept", "--pn=50000",
                    $"--sn={nodeCount}",
                    $"--sr={(int)updateInterval.TotalSeconds}",
                    "--st=uint")
            }, TimeoutToken, nameDiscriminator: kSimulationName,
                cpuCores: 1.0, memoryInGB: 1.5);

            // Act
            var nodeIds = Enumerable.Range(1, nodeCount)
                .Select(TestConstants.Soak.SlowNodeId);
            await PublishNodesAsync(PublishedNodes(nodeIds, heartbeatInterval), TimeoutToken);

            var report = await ObserveAsync(new TelemetryQualityOptions
            {
                UpdateInterval = updateInterval,
                ExpectedNodeCount = nodeCount,
                //
                // OPC PLC derives its source timestamps from a wall clock
                // simulation timer, so over a two minute cycle a few seconds
                // of drift are expected and harmless.
                //
                Tolerance = TimeSpan.FromSeconds(5),
                HeartbeatInterval = heartbeatInterval,
                PublishingInterval = TestConstants.Soak.PublishingInterval,
                HeartbeatTolerance = TimeSpan.FromSeconds(5)
            }, kWarmup);

            // Assert
            Assert.True(report.TotalSamples > 0, "No telemetry arrived at all.");
            Assert.True(report.NodesMissing == 0,
                $"{report.NodesMissing} node(s) never reported.{Environment.NewLine}{report}");

            //
            // Heartbeats have to fire, on every node. Values arrive every
            // update interval and the watchdog waits one heartbeat interval
            // plus one publishing interval before declaring the node silent,
            // so this many heartbeats fit between two value changes. Half of
            // the resulting expectation is required, which still fails
            // decisively if heartbeats stopped firing while tolerating a
            // window that does not start and end on a value boundary.
            //
            var earliestHeartbeat = heartbeatInterval + TestConstants.Soak.PublishingInterval;
            var perValueChange = (int)((updateInterval - earliestHeartbeat) / heartbeatInterval);
            var valueChanges = (int)(Duration / updateInterval);
            var expectedHeartbeats = Math.Max(1, valueChanges * perValueChange);
            var minimumHeartbeats = Math.Max(1, expectedHeartbeats / 2);
            _output.WriteLine($"Expecting at least {minimumHeartbeats} heartbeat(s) per node " +
                $"({valueChanges} value change(s) x {perValueChange} heartbeat(s), halved).");
            Assert.True(report.MinHeartbeatsPerNode >= minimumHeartbeats,
                $"Expected at least {minimumHeartbeats} heartbeat(s) on every node but the " +
                $"quietest node produced {report.MinHeartbeatsPerNode}." +
                $"{Environment.NewLine}{report}");

            //
            // Correctness assertions - unaffected by timing jitter.
            //

            // (d) a repeat is only ever a heartbeat, and it says so.
            Assert.True(report.UnflaggedRepeats == 0,
                $"{report.UnflaggedRepeats} value(s) were repeated without the heartbeat " +
                $"indicator, so a consumer cannot tell them apart from real data." +
                $"{Environment.NewLine}{report}");

            // (d) nothing older than what was already delivered is ever sent.
            Assert.True(report.OutOfOrderIncludingHeartbeats == 0,
                $"{report.OutOfOrderIncludingHeartbeats} message(s) carried a value older " +
                $"than one already delivered.{Environment.NewLine}{report}");
            Assert.True(report.HeartbeatsWithChangedTimestamp == 0,
                $"{report.HeartbeatsWithChangedTimestamp} heartbeat(s) altered the source " +
                $"timestamp of the value they resend.{Environment.NewLine}{report}");

            // (a)/(b) the real value stream is unaffected by the heartbeats.
            Assert.True(report.MissingValues == 0,
                $"{report.MissingValues} value(s) were lost.{Environment.NewLine}{report}");
            Assert.True(report.OutOfOrderValues == 0,
                $"{report.OutOfOrderValues} value(s) arrived out of order." +
                $"{Environment.NewLine}{report}");
            Assert.True(report.SamplesWithoutSourceTimestamp == 0,
                $"{report.SamplesWithoutSourceTimestamp} message(s) had no source " +
                $"timestamp.{Environment.NewLine}{report}");

            //
            // A heartbeat that fired before the item had been silent for the
            // heartbeat interval plus one publishing interval is always a
            // defect, and unlike the counters below this is independent of
            // how loaded the machine is: a stall only ever makes the idle
            // time longer, never shorter. So this one is strict.
            //
            Assert.True(report.EarlyHeartbeats == 0,
                $"{report.EarlyHeartbeats} heartbeat(s) arrived before the watchdog grace " +
                $"period elapsed.{Environment.NewLine}{report}");

            //
            // The remaining timing assertions carry a budget: the simulator
            // derives its source timestamps from a wall clock timer and
            // shares the edge VM and IoT Hub with the other test jobs, so a
            // heartbeat or a value can legitimately arrive late.
            //
            AssertWithinBudget(report.HeartbeatCadenceViolations, report.HeartbeatSamples,
                $"gap(s) between consecutive heartbeats did not match the configured " +
                $"{heartbeatInterval}", report);

            // (c) the real value stream stays exactly one update interval apart.
            AssertWithinBudget(report.ValueIntervalViolations, report.ValueSamples,
                $"value(s) were not {updateInterval} apart from their predecessor", report);
        }

        /// <summary>
        /// Assert that an observation stayed inside a small fraction of the
        /// total, so an occasional hiccup on shared infrastructure does not
        /// fail the run while a systematic defect still does.
        /// </summary>
        /// <param name="observed"></param>
        /// <param name="total"></param>
        /// <param name="what"></param>
        /// <param name="report"></param>
        private static void AssertWithinBudget(long observed, long total, string what,
            TelemetryQualityReport report)
        {
            var budget = Math.Max(kMinimumBudget, total / 100);
            Assert.True(observed <= budget,
                $"{observed} {what}, which exceeds the budget of {budget} " +
                $"(1% of {total}).{Environment.NewLine}{report}");
        }

        [Fact, PriorityOrder(998)]
        public async Task TestCleanup()
        {
            await CleanupPublisherAsync();
        }

        /// <summary>
        /// One full value cycle of warm up, so the analysed window starts with
        /// every node having reported at least once and the heartbeat watchdog
        /// already armed.
        /// </summary>
        private static readonly TimeSpan kWarmup = TimeSpan.FromMinutes(3);
        private const string kModuleName = "publisher_soak_slow";
        private const string kDeploymentName = "__default-opcpublisher-soak-slow";
        private const string kSimulationName = "soakslow";

        /// <summary>
        /// Smallest budget granted regardless of stream size.
        /// </summary>
        private const int kMinimumBudget = 5;
        private readonly ITestOutputHelper _output;
    }
}
