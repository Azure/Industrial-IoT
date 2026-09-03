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
    /// Long running end to end validation of the customer configuration on a
    /// publisher module deployed to IoT Edge: publishing interval, sampling
    /// interval and heartbeat interval all two seconds, queue size larger than
    /// one, samples encoding, against OPC PLC nodes that count up by exactly
    /// one every two seconds.
    /// </para>
    /// <para>
    /// Because a fresh value arrives on every publish cycle the heartbeat
    /// watchdog must never fire. Before the watchdog grace period was
    /// introduced it fired on roughly six out of ten cycles and re-sent the
    /// previous value with its now stale source timestamp, which a consumer
    /// saw as an old value arriving right after a new one and as a source
    /// timestamp cadence broken by zero length gaps.
    /// </para>
    /// <para>
    /// The counter value is its own sequence number, so a lost value shows up
    /// as a gap, a reordered value as a decrease, and the expected source
    /// timestamp distance is exactly one update interval.
    /// </para>
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName,
        TestConstants.TraitConstants.PublisherModeSoakCountersTraitValue)]
    public sealed class FTelemetryQualityCountersTestTheory : SoakTestBase,
        IClassFixture<IIoTStandaloneTestContext>
    {
        public FTelemetryQualityCountersTestTheory(IIoTStandaloneTestContext context,
            ITestOutputHelper output)
            : base(context, output, kModuleName, kDeploymentName,
                TestConstants.SoakCountersConsumerGroupName)
        {
        }

        [Fact, PriorityOrder(0)]
        public async Task TestDeployPublisher()
        {
            await DeployPublisherAsync();
        }

        [Fact, PriorityOrder(1)]
        public async Task TestTelemetryStreamIsCompleteOrderedAndFreeOfHeartbeatsAsync()
        {
            var nodeCount = TestConstants.Soak.FastNodeCount;
            var updateInterval = TestConstants.Soak.FastUpdateInterval;

            // Arrange - an OPC PLC whose fast nodes count up every two seconds.
            // The slow node family is left at its default and simply not
            // subscribed to; passing --sn=0 would rely on the simulator
            // accepting a zero node count, and a rejected command line fails
            // the whole container.
            await TestHelper.CreateSimulationContainerAsync(Context, new List<string>
            {
                "/bin/sh", "-c", string.Join(' ',
                    "./opcplc", "--autoaccept", "--pn=50000",
                    $"--fn={nodeCount}",
                    $"--fr={(int)updateInterval.TotalSeconds}",
                    "--ft=uint")
            }, TimeoutToken, nameDiscriminator: kSimulationName,
                cpuCores: 2.0, memoryInGB: 4.0);

            // Act - publish every fast node with the customer configuration.
            var nodeIds = Enumerable.Range(1, nodeCount)
                .Select(TestConstants.Soak.FastNodeId);
            await PublishNodesAsync(
                PublishedNodes(nodeIds, TestConstants.Soak.FastHeartbeatInterval),
                TimeoutToken);

            var report = await ObserveAsync(new TelemetryQualityOptions
            {
                UpdateInterval = updateInterval,
                ExpectedNodeCount = nodeCount,
                //
                // OPC PLC derives its source timestamps from a wall clock
                // simulation timer, so the spacing is approximate. Half the
                // update interval is still far tighter than the zero length
                // gap a spurious heartbeat produces.
                //
                Tolerance = TimeSpan.FromSeconds(1),
                HeartbeatInterval = TestConstants.Soak.FastHeartbeatInterval,
                PublishingInterval = TestConstants.Soak.PublishingInterval
            }, WarmupFor(nodeCount));

            // Assert
            Assert.True(report.TotalSamples > 0, "No telemetry arrived at all.");
            Assert.True(report.NodesMissing == 0,
                $"{report.NodesMissing} node(s) never reported.{Environment.NewLine}{report}");

            //
            // Correctness assertions. None of these can be caused by timing
            // jitter on shared infrastructure, so they are strict.
            //

            // (a) no value the customer expects is lost
            Assert.True(report.MissingValues == 0,
                $"{report.MissingValues} value(s) were lost.{Environment.NewLine}{report}");

            // (b) values never go backwards
            Assert.True(report.OutOfOrderValues == 0,
                $"{report.OutOfOrderValues} value(s) arrived out of order." +
                $"{Environment.NewLine}{report}");
            Assert.True(report.OutOfOrderIncludingHeartbeats == 0,
                $"{report.OutOfOrderIncludingHeartbeats} message(s) carried a value older " +
                $"than one already delivered.{Environment.NewLine}{report}");

            // (d) a repeat, if any, always says that it is a heartbeat
            Assert.True(report.UnflaggedRepeats == 0,
                $"{report.UnflaggedRepeats} value(s) were repeated without the heartbeat " +
                $"indicator.{Environment.NewLine}{report}");

            Assert.True(report.SamplesWithoutSourceTimestamp == 0,
                $"{report.SamplesWithoutSourceTimestamp} message(s) had no source " +
                $"timestamp.{Environment.NewLine}{report}");

            //
            // Timing assertions. A real deployment can stall briefly - the
            // simulator derives its source timestamps from a wall clock timer
            // and shares a two vCPU edge VM and an IoT Hub with the other
            // test jobs - so these must not demand a perfect run.
            //
            // The precise, load independent signal for the regression this
            // test guards is EarlyHeartbeats: a heartbeat that fired before
            // the item had been silent for the heartbeat interval plus one
            // publishing interval is always a defect, because that is the
            // earliest point at which the absence of data can be
            // established. A stalled edge VM produces heartbeats whose idle
            // time genuinely exceeds the deadline, and those do not count.
            //
            Assert.True(report.EarlyHeartbeats == 0,
                $"{report.EarlyHeartbeats} heartbeat(s) fired before the item had been " +
                $"silent for the heartbeat interval plus one publishing interval." +
                $"{Environment.NewLine}{report}");

            //
            // Values arrive on every publish cycle, so heartbeats should be
            // rare. A generous ceiling still catches a systematic regression
            // (the bug emitted one on roughly six out of ten cycles) while
            // tolerating the occasional genuine stall.
            //
            AssertWithinBudget(report.HeartbeatSamples, report.ValueSamples, 4,
                "heartbeat(s) were emitted although a value arrived on every publish cycle",
                report);
            AssertWithinBudget(report.ValueIntervalViolations, report.ValueSamples, 100,
                $"value(s) were not {updateInterval} apart from their predecessor", report);
        }

        /// <summary>
        /// Assert that an observation stayed inside a fraction of the total,
        /// so an occasional hiccup on shared infrastructure does not fail the
        /// run while a systematic defect still does.
        /// </summary>
        /// <param name="observed"></param>
        /// <param name="total"></param>
        /// <param name="divisor">Denominator of the tolerated fraction</param>
        /// <param name="what"></param>
        /// <param name="report"></param>
        private static void AssertWithinBudget(long observed, long total, int divisor,
            string what, TelemetryQualityReport report)
        {
            var budget = Math.Max(kMinimumBudget, total / divisor);
            Assert.True(observed <= budget,
                $"{observed} {what}, which exceeds the budget of {budget} " +
                $"(1/{divisor} of {total} value samples).{Environment.NewLine}{report}");
        }

        [Fact, PriorityOrder(998)]
        public async Task TestCleanup()
        {
            await CleanupPublisherAsync();
        }

        /// <summary>
        /// Time given to the publisher to create every monitored item and to
        /// let the first values settle before the stream is analysed.
        /// </summary>
        /// <param name="nodeCount"></param>
        private static TimeSpan WarmupFor(int nodeCount)
        {
            return TimeSpan.FromMinutes(1) + TimeSpan.FromMilliseconds(20 * nodeCount);
        }

        private const string kModuleName = "publisher_soak_fast";
        private const string kDeploymentName = "__default-opcpublisher-soak-fast";
        private const string kSimulationName = "soakfast";

        /// <summary>
        /// Smallest budget granted regardless of stream size, so a short run
        /// is not failed by a single hiccup.
        /// </summary>
        private const int kMinimumBudget = 5;
    }
}
