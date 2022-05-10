// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;
    using static System.TimeSpan;

    /// <summary>
    /// The test theory submitting a high load of event messages
    /// </summary>
    public class C_EventsStressTestTheory : DynamicAciTestBase {
        public C_EventsStressTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output)
            : base(context, output) {
        }

        // ToDo: remove ´skip test´ when event and alarm are fully implemented
        [Fact(Skip = "PublishedNodesJobConverter does not parse OpcEvents now."), PriorityOrder(10)]
        public async void TestACI_VerifyEnd2EndThroughputAndLatency() {

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

            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemEventTypePayload>(_writerId, _timeoutToken);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("i=2041")); // OPC-UA BaseEventType
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, pnJson, _timeoutToken);

            const int nSecondsTotal = nSeconds + nSecondSkipFirst + nSecondSkipLast;
            var fullData = await messages
                .ConsumeDuring(_context, FromSeconds(nSecondsTotal))

                // Get time of event attached Server node
                .Select(e => (e.EnqueuedTime, e.Messages["i=2253"].ReceiveTime))
                .ToListAsync(_timeoutToken);

            // Assert throughput

            // Trim first few and last seconds of data, since Publisher polls PLCs
            // at different times
            var intervalStart = fullData.Select(d => d.ReceiveTime.Value).Min() + FromSeconds(nSecondSkipFirst);
            var intervalEnd = fullData.Select(d => d.ReceiveTime.Value).Max() - FromSeconds(nSecondSkipLast);
            var intervalDuration = intervalEnd - intervalStart;
            var eventData = fullData.Where(d => d.ReceiveTime > intervalStart && d.ReceiveTime < intervalEnd).ToList();

            // Bin events by 1-second interval to compute event rate histogram
            var eventRatesBySecond = eventData
                .GroupBy(s => s.ReceiveTime.Value.Truncate(FromSeconds(1)))
                .Select(g => g.Count())
                .ToList();

            _output.WriteLine($"Event rates per second, by second: {string.Join(',', eventRatesBySecond)} e/s");

            var eventRate = eventData.Count / intervalDuration.TotalSeconds;
            intervalDuration.Should().BeGreaterThan(FromSeconds(nSeconds));

            const int expectedEventsPerSecond = instances * eventInstances * 1000 / eventIntervalPerInstanceMs;
            eventData.Count().Should().BeGreaterThan(nSeconds * expectedEventsPerSecond,
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

            // Assert latency
            var end2EndLatency = eventData
                .Select(v => v.EnqueuedTime - v.ReceiveTime.Value)
                .ToList();
            end2EndLatency.Min().Should().BePositive();
            end2EndLatency.Average(v => v.TotalMilliseconds).Should().BeLessThan(8000);
        }

        private static (double average, double stDev) DescriptiveStats(IReadOnlyCollection<int> population) {
            var average = population.Average();
            var stDev = Math.Sqrt(population.Sum(v => (v - average) * (v - average)) / population.Count);
            return (average, stDev);
        }
    }
}