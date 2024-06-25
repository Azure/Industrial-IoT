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
            const int eventIntervalPerInstanceMs = 400;
            const int eventInstances = 40;
            const int instances = 10;
            const int nSeconds = 20;
            const int nSecondSkipFirst = 10;
            const int nSecondSkipLast = 6;

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context,
                new List<string> { "/bin/sh", "-c", $"./opcplc --autoaccept --ei={eventInstances} --er={eventIntervalPerInstanceMs} --pn=50000" },
                _timeoutToken,
                numInstances: instances);

            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemEventTypePayload>(_writerId, -1, _timeoutToken);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("i=2041")); // OPC-UA BaseEventType
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);

            const int nSecondsTotal = nSeconds + nSecondSkipFirst + nSecondSkipLast;
            var fullData = await messages
                .TakeWhile(_context, (first, current) => current.EnqueuedTime - first.EnqueuedTime <= FromSeconds(nSecondsTotal))

                // Get time of event attached Server node
                .Select(e => (e.EnqueuedTime, SourceTimestamp: e.Payload.ReceiveTime.Value))
                .ToListAsync(_timeoutToken);

            // Assert throughput

            // Trim first few and last seconds of data, since Publisher polls PLCs
            // at different times
            var intervalStart = fullData.Min(d => d.SourceTimestamp) + FromSeconds(nSecondSkipFirst);
            var intervalEnd = fullData.Max(d => d.SourceTimestamp) - FromSeconds(nSecondSkipLast);
            var intervalDuration = intervalEnd - intervalStart;
            var eventData = fullData.Where(d => d.SourceTimestamp > intervalStart && d.SourceTimestamp < intervalEnd).ToList();

            // Bin events by 1-second interval to compute event rate histogram
            var eventRatesBySecond = eventData
                .GroupBy(s => s.SourceTimestamp.Value.Truncate(FromSeconds(1)))
                .Select(g => g.Count())
                .ToArray()[1..^1];

            const int expectedEventsPerSecond = instances * eventInstances * 1000 / eventIntervalPerInstanceMs;
            _context.OutputHelper.WriteLine($"Event rates per second, by second: {string.Join(',', eventRatesBySecond)} e/s (expected {expectedEventsPerSecond} e/s)");

            // Assert latency
            var end2EndLatency = eventData
                .ConvertAll(v => v.EnqueuedTime - v.SourceTimestamp);
            end2EndLatency.Min().Should().BePositive();
            end2EndLatency.Average(v => v.Value.TotalMilliseconds).Should().BeLessThan(8000);

            // var eventRate = eventData.Count / intervalDuration.Value.TotalSeconds;
            var eventRate = eventRatesBySecond.Average();
            intervalDuration.Should().BeGreaterThan(FromSeconds(nSeconds));
            eventData.Count.Should().BeGreaterThan(nSeconds * expectedEventsPerSecond,
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
