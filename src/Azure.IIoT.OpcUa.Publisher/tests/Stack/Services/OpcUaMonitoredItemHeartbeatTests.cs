// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using System;
    using Xunit;

    /// <summary>
    /// Tests for the heartbeat watchdog decision. A watchdog heartbeat may
    /// only be emitted when no value was received for the heartbeat interval.
    /// The countdown is restarted whenever a value is processed, so it
    /// elapses exactly one heartbeat interval later while the next value can
    /// only arrive one publishing interval after the previous one plus the
    /// round trip and processing time. Without an allowance for this the
    /// watchdog won that race on nearly every cycle whenever the heartbeat
    /// interval was at or below the publishing interval, and re-sent the
    /// previous value with its stale source timestamp - a consumer saw an
    /// old value arriving right after a new one and a source timestamp
    /// cadence broken by zero length gaps.
    /// </summary>
    public class OpcUaMonitoredItemHeartbeatTests
    {
        private static readonly TimeSpan kTwoSeconds = TimeSpan.FromSeconds(2);

        [Fact]
        public void ValueArrivingExactlyOnScheduleDoesNotTriggerHeartbeat()
        {
            //
            // The reported scenario: heartbeat interval equals the publishing
            // interval, so the countdown elapses exactly when the next value
            // is still in flight.
            //
            var due = OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: kTwoSeconds, heartbeatInterval: kTwoSeconds,
                publishingInterval: kTwoSeconds, out var remaining);

            Assert.False(due);
            Assert.Equal(kTwoSeconds, remaining);
        }

        [Fact]
        public void ValueArrivingSlightlyLateDoesNotTriggerHeartbeat()
        {
            var due = OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: kTwoSeconds + TimeSpan.FromMilliseconds(250),
                heartbeatInterval: kTwoSeconds, publishingInterval: kTwoSeconds,
                out var remaining);

            Assert.False(due);
            Assert.Equal(TimeSpan.FromMilliseconds(1750), remaining);
        }

        [Fact]
        public void HeartbeatIsDueOnceAFullPublishCycleWasMissed()
        {
            // Values genuinely stopped: heartbeat interval plus one
            // publishing interval have passed without data.
            var due = OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: TimeSpan.FromSeconds(4), heartbeatInterval: kTwoSeconds,
                publishingInterval: kTwoSeconds, out var remaining);

            Assert.True(due);
            Assert.Equal(TimeSpan.Zero, remaining);
        }

        [Fact]
        public void HeartbeatStaysDueWhileValuesRemainAbsent()
        {
            var due = OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: TimeSpan.FromMinutes(5), heartbeatInterval: kTwoSeconds,
                publishingInterval: kTwoSeconds, out _);

            Assert.True(due);
        }

        [Fact]
        public void GraceIsCappedAtTheHeartbeatIntervalWhenPublishingIsSlower()
        {
            //
            // A heartbeat much shorter than the publishing interval is a
            // deliberate configuration - the item is expected to heartbeat
            // between value changes. The grace must not grow with the
            // publishing interval or the requested cadence would be lost.
            //
            var due = OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: TimeSpan.FromSeconds(4), heartbeatInterval: kTwoSeconds,
                publishingInterval: TimeSpan.FromSeconds(60), out _);

            Assert.True(due);
        }

        [Fact]
        public void FastPublishingOnlyAddsItsOwnIntervalOfGrace()
        {
            // Publishing every 100 ms, heartbeat every 2 s: the heartbeat is
            // due at 2.1 s, not at 4 s.
            Assert.False(OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: TimeSpan.FromMilliseconds(2050), heartbeatInterval: kTwoSeconds,
                publishingInterval: TimeSpan.FromMilliseconds(100), out _));
            Assert.True(OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: TimeSpan.FromMilliseconds(2100), heartbeatInterval: kTwoSeconds,
                publishingInterval: TimeSpan.FromMilliseconds(100), out _));
        }

        [Fact]
        public void UnknownPublishingIntervalStillAllowsForProcessingJitter()
        {
            // Without a known publishing interval a minimum allowance for
            // delivery and processing jitter is still applied.
            Assert.False(OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: kTwoSeconds, heartbeatInterval: kTwoSeconds,
                publishingInterval: TimeSpan.Zero, out var remaining));
            Assert.Equal(TimeSpan.FromMilliseconds(100), remaining);

            Assert.True(OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: TimeSpan.FromMilliseconds(2100), heartbeatInterval: kTwoSeconds,
                publishingInterval: TimeSpan.Zero, out _));
        }

        [Fact]
        public void RemainingTimeIsNeverZeroSoTheTimerCanAlwaysBeRearmed()
        {
            var due = OpcUaMonitoredItem.Heartbeat.IsWatchdogDue(
                idle: TimeSpan.FromSeconds(4) - TimeSpan.FromTicks(1),
                heartbeatInterval: kTwoSeconds, publishingInterval: kTwoSeconds,
                out var remaining);

            Assert.False(due);
            Assert.True(remaining > TimeSpan.Zero);
        }
    }
}
