// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.Standalone
{
    using FluentAssertions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using static System.TimeSpan;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory submitting a high load of event messages
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class CEventsStressTestTheory : DynamicAciTestBase, IClassFixture<IIoTStandaloneTestContext>
    {
        public CEventsStressTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
            : base(context, output)
        {
        }

        [Fact, PriorityOrder(10)]
        public async Task TestACIVerifyEnd2EndThroughputAndLatency()
        {
            // Settings
            const int eventIntervalPerInstanceMs = 1000;
            //
            // Native PubSub carries one event occurrence per acknowledged
            // send. Keep enough headroom below the transport ceiling that the
            // test measures sustained delivery rather than a growing backlog:
            // one event per second from each of ten independent endpoints.
            //
            const int eventInstances = 1;
            const int instances = 10;
            const int nSeconds = 20;
            const int nSecondWarmup = 180;
            const int nSecondSkipLast = 6;

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context,
                new List<string> { "/bin/sh", "-c", $"./opcplc --autoaccept --ei={eventInstances} --er={eventIntervalPerInstanceMs} --pn=50000" },
                _timeoutToken,
                numInstances: instances);

            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("i=2041")); // OPC-UA BaseEventType

            const int nSecondsTotal = nSecondWarmup + nSeconds + nSecondSkipLast;
            var configurationCompleted = new TaskCompletionSource<DateTime>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            // Act
            var fullData = await TestHelper.ReadAfterAsync(
                token => _consumer
                    .ReadMessagesFromWriterIdAsync<SystemEventTypePayload>(
                        _writerId, -1, token,
                        _context.IoTHubPublisherDeployment.ModuleName, _context)
                    // The reader is deliberately pre-armed before PublishNodes,
                    // but configuring ten endpoints takes long enough that a
                    // time window starting at the first connected endpoint
                    // measures rollout rather than steady state.
                    .SkipWhile(e => !configurationCompleted.Task.IsCompletedSuccessfully
                        || e.Payload.ReceiveTime.Value is not { } sourceTimestamp
                        || sourceTimestamp < configurationCompleted.Task.Result)
                    .TakeWhile(_context, (first, current) =>
                        current.EnqueuedTime - first.EnqueuedTime <=
                            FromSeconds(nSecondsTotal))
                    // Get time of event attached Server node.
                    .Select(e => (e.EnqueuedTime,
                        SourceTimestamp: e.Payload.ReceiveTime.Value)),
                async token =>
                {
                    await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(
                        pnJson, _context, token);
                    configurationCompleted.TrySetResult(DateTime.UtcNow);
                },
                _timeoutToken);

            // Assert throughput

            // Allow the per-endpoint queues to drain after the last endpoint
            // starts, then measure a complete enqueue-time window. Source time
            // remains the latency origin, but is not a reliable window clock
            // while older occurrences are being drained.
            var intervalEnd = fullData.Max(d => d.EnqueuedTime)
                - FromSeconds(nSecondSkipLast);
            var intervalStart = intervalEnd - FromSeconds(nSeconds);
            var eventData = fullData
                .Where(d => d.EnqueuedTime > intervalStart
                    && d.EnqueuedTime < intervalEnd)
                .ToList();
            eventData.Should().NotBeEmpty();
            var intervalDuration = eventData.Max(d => d.EnqueuedTime)
                - eventData.Min(d => d.EnqueuedTime);

            // Bin events by 1-second interval to compute event rate histogram
            var ratesBySecond = eventData
                .GroupBy(s => s.EnqueuedTime.Truncate(FromSeconds(1)))
                .ToDictionary(g => g.Key, g => g.Count());
            var firstSecond = ratesBySecond.Keys.Min();
            var lastSecond = ratesBySecond.Keys.Max();
            var eventRatesBySecond = Enumerable
                .Range(0, (int)(lastSecond - firstSecond).TotalSeconds + 1)
                .Select(offset => ratesBySecond.GetValueOrDefault(
                    firstSecond.AddSeconds(offset), 0))
                .ToArray()[1..^1];

            const int expectedEventsPerSecond = instances * eventInstances * 1000 / eventIntervalPerInstanceMs;
            _context.OutputHelper.WriteLine($"Event rates per second, by second: {string.Join(',', eventRatesBySecond)} e/s (expected {expectedEventsPerSecond} e/s)");

            // Assert latency
            var end2EndLatency = eventData
                .ConvertAll(v => v.EnqueuedTime - v.SourceTimestamp);
            var latencyMilliseconds = end2EndLatency
                .Select(v => v.Value.TotalMilliseconds)
                .Order()
                .ToArray();
            var p95Latency = latencyMilliseconds[
                (int)Math.Ceiling(latencyMilliseconds.Length * 0.95) - 1];
            _context.OutputHelper.WriteLine(
                $"End-to-end latency: min {end2EndLatency.Min()}, " +
                $"average {end2EndLatency.Average(v => v.Value.TotalMilliseconds):F0} ms, " +
                $"p95 {p95Latency:F0} ms, " +
                $"max {end2EndLatency.Max()}.");
            end2EndLatency.Min().Should().BePositive();
            end2EndLatency.Average(v => v.Value.TotalMilliseconds).Should().BeLessThan(8000);

            // var eventRate = eventData.Count / intervalDuration.Value.TotalSeconds;
            var eventRate = eventRatesBySecond.Average();
            intervalDuration.Should().BeGreaterThan(FromSeconds(nSeconds - 2));
            eventData.Count.Should().BeGreaterThan(
                nSeconds * expectedEventsPerSecond * 9 / 10,
                "Publisher should produce data continuously");
            eventRate.Should().BeApproximately(
                expectedEventsPerSecond,
                expectedEventsPerSecond / 10d,
                "Publisher should match PLC event rate");

            var (average, stDev) = DescriptiveStats(eventRatesBySecond);

            average.Should().BeApproximately(
                expectedEventsPerSecond,
                expectedEventsPerSecond / 10d,
                "Publisher should match PLC event rate");

            stDev.Should().BeLessThan(expectedEventsPerSecond / 3d, "Publisher should sustain PLC event rate");
        }

        private static (double average, double stDev) DescriptiveStats(IReadOnlyCollection<int> population)
        {
            var average = population.Average();
            var stDev = Math.Sqrt(population.Sum(v => (v - average) * (v - average)) / population.Count);
            return (average, stDev);
        }
    }
}
