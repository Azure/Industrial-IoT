// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Telemetry
{
    using System;
    using Xunit;

    /// <summary>
    /// Tests for the telemetry quality validator. The validator has to
    /// separate what a heartbeat legitimately does (resend the last value
    /// with its original source timestamp) from what would be a defect
    /// (losing a value, reordering, or repeating a value without saying so),
    /// and it has to survive at least once delivery through IoT Hub.
    /// </summary>
    public class TelemetryQualityValidatorTests
    {
        private static readonly DateTime kEpoch =
            new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan kTwoSeconds = TimeSpan.FromSeconds(2);

        [Fact]
        public void CleanCounterStreamProducesNoFindings()
        {
            var validator = Create(kTwoSeconds, nodes: 1);
            for (var i = 0; i < 10; i++)
            {
                validator.Add(Value(i));
            }

            var report = validator.CreateReport();
            Assert.True(report.IsClean, report.ToString());
            Assert.Equal(10, report.ValueSamples);
            Assert.Equal(0, report.HeartbeatSamples);
        }

        [Fact]
        public void MissingValueIsCountedAsGap()
        {
            var validator = Create(kTwoSeconds, nodes: 1);
            validator.Add(Value(0));
            validator.Add(Value(3));

            var report = validator.CreateReport();
            Assert.Equal(2, report.MissingValues);
        }

        [Fact]
        public void ValueGoingBackwardsIsCountedOutOfOrder()
        {
            var validator = Create(kTwoSeconds, nodes: 1);
            validator.Add(Value(5));
            validator.Add(Value(4));

            var report = validator.CreateReport();
            Assert.Equal(1, report.OutOfOrderValues);
            Assert.Equal(1, report.OutOfOrderIncludingHeartbeats);
        }

        [Fact]
        public void RepeatWithMatchingValueAndTimestampIsInferredAsHeartbeat()
        {
            // When a sample repeats both the value and the SourceTimestamp of its
            // predecessor and carries no wire indicator, it is structurally
            // indistinguishable from a WatchdogLKV heartbeat and is inferred as one.
            // A 3.0 publisher emits exactly this shape — no Heartbeat member — so
            // inference is the only way to classify these samples correctly.
            //
            // Before the structural inference was added, this test expected
            // UnflaggedRepeats=1 / RepeatedValuesFromHeartbeat=0 because the
            // wire indicator was the only discriminator. That encoded the broken
            // 3.0 behaviour: every heartbeat counted as an unflagged repeat.
            var validator = Create(kTwoSeconds, nodes: 1);
            validator.Add(Value(1));
            validator.Add(Value(1)); // same value AND same SourceTimestamp → inferred heartbeat

            var report = validator.CreateReport();
            Assert.Equal(1, report.RepeatedValues);
            Assert.Equal(1, report.RepeatedValuesFromHeartbeat); // inferred, not wire-flagged
            Assert.Equal(0, report.UnflaggedRepeats);
        }

        [Fact]
        public void HeartbeatRepeatIsAttributedToTheHeartbeatAndNotToTheValueStream()
        {
            var validator = Create(kTwoSeconds, nodes: 1,
                heartbeatInterval: TimeSpan.FromSeconds(10));
            validator.Add(Value(1));
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(2), kEpoch.AddSeconds(14)));
            validator.Add(Value(2, messageTimestamp: kEpoch.AddSeconds(24)));

            var report = validator.CreateReport();
            Assert.Equal(1, report.RepeatedValues);
            Assert.Equal(1, report.RepeatedValuesFromHeartbeat);
            Assert.Equal(0, report.UnflaggedRepeats);

            // The heartbeat neither opened a gap nor broke the value cadence.
            Assert.Equal(0, report.MissingValues);
            Assert.Equal(0, report.OutOfOrderValues);
            Assert.Equal(0, report.ValueIntervalViolations);
            Assert.Equal(1, report.HeartbeatSamples);
        }

        [Fact]
        public void HeartbeatMustResendTheOriginalSourceTimestamp()
        {
            var validator = Create(kTwoSeconds, nodes: 1,
                heartbeatInterval: TimeSpan.FromSeconds(10));
            validator.Add(Value(1));

            // A heartbeat that shifts the source timestamp forward.
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(12), kEpoch.AddSeconds(14)));

            var report = validator.CreateReport();
            Assert.Equal(1, report.HeartbeatsWithChangedTimestamp);
        }

        [Fact]
        public void HeartbeatBeforeTheWatchdogGracePeriodIsFlaggedEarly()
        {
            var validator = Create(kTwoSeconds, nodes: 1,
                heartbeatInterval: TimeSpan.FromSeconds(10),
                heartbeatTolerance: TimeSpan.FromSeconds(1));

            // Value produced at t=2s, so the earliest legitimate heartbeat is
            // at 10s + 2s publishing interval = t=14s.
            validator.Add(Value(1, messageTimestamp: kEpoch.AddSeconds(2)));
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(2), kEpoch.AddSeconds(10)));

            var report = validator.CreateReport();
            Assert.Equal(1, report.EarlyHeartbeats);
        }

        [Fact]
        public void HeartbeatAtTheWatchdogGracePeriodIsNotFlaggedEarly()
        {
            var validator = Create(kTwoSeconds, nodes: 1,
                heartbeatInterval: TimeSpan.FromSeconds(10),
                heartbeatTolerance: TimeSpan.FromSeconds(1));

            validator.Add(Value(1, messageTimestamp: kEpoch.AddSeconds(2)));
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(2), kEpoch.AddSeconds(14)));

            var report = validator.CreateReport();
            Assert.Equal(0, report.EarlyHeartbeats);
        }

        [Fact]
        public void HeartbeatCadenceIsMeasuredFromTheMessageTimestamp()
        {
            var validator = Create(kTwoSeconds, nodes: 1,
                heartbeatInterval: TimeSpan.FromSeconds(10),
                heartbeatTolerance: TimeSpan.FromSeconds(1));

            validator.Add(Value(1, messageTimestamp: kEpoch.AddSeconds(2)));
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(2), kEpoch.AddSeconds(14)));
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(2), kEpoch.AddSeconds(24)));  // ok
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(2), kEpoch.AddSeconds(44)));  // 20s

            var report = validator.CreateReport();
            Assert.Equal(1, report.HeartbeatCadenceViolations);
            Assert.Equal(3, report.HeartbeatSamples);
            Assert.Equal(3, report.MinHeartbeatsPerNode);
            Assert.Equal(3, report.MaxHeartbeatsPerNode);
        }

        [Fact]
        public void HeartbeatCountsArePerNode()
        {
            var validator = Create(kTwoSeconds, nodes: 2,
                heartbeatInterval: TimeSpan.FromSeconds(10));
            validator.Add(Value(1, node: "a"));
            validator.Add(Value(1, node: "b"));
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(2), kEpoch.AddSeconds(14), node: "a"));

            var report = validator.CreateReport();
            Assert.Equal(2, report.NodesSeen);
            Assert.Equal(0, report.NodesMissing);
            Assert.Equal(0, report.MinHeartbeatsPerNode);
            Assert.Equal(1, report.MaxHeartbeatsPerNode);
        }

        [Fact]
        public void RedeliveredMessageIsSuppressedInsteadOfBeingReportedAsAStaleValue()
        {
            var validator = Create(kTwoSeconds, nodes: 1);
            validator.Add(Value(1, sequenceNumber: 100));
            validator.Add(Value(2, sequenceNumber: 101));

            // IoT Hub delivers at least once - the same message again.
            validator.Add(Value(2, sequenceNumber: 101));

            var report = validator.CreateReport();
            Assert.Equal(1, report.DuplicateDeliveries);
            Assert.Equal(0, report.RepeatedValues);
            Assert.Equal(0, report.UnflaggedRepeats);
            Assert.Equal(2, report.ValueSamples);
        }

        [Fact]
        public void DuplicateSuppressionCanBeDisabled()
        {
            var validator = new TelemetryQualityValidator(new TelemetryQualityOptions
            {
                UpdateInterval = kTwoSeconds,
                ExpectedNodeCount = 1,
                SuppressDuplicates = false
            });
            validator.Add(Value(1, sequenceNumber: 100));
            validator.Add(Value(1, sequenceNumber: 100));

            var report = validator.CreateReport();
            Assert.Equal(0, report.DuplicateDeliveries);
            Assert.Equal(1, report.RepeatedValues);
        }

        [Fact]
        public void DuplicateWindowIsBounded()
        {
            var validator = new TelemetryQualityValidator(new TelemetryQualityOptions
            {
                UpdateInterval = kTwoSeconds,
                ExpectedNodeCount = 1,
                DuplicateWindow = 2
            });
            validator.Add(Value(1, sequenceNumber: 1));
            validator.Add(Value(2, sequenceNumber: 2));
            validator.Add(Value(3, sequenceNumber: 3));

            // Sequence number 1 has fallen out of the window, so it is no
            // longer recognized as a duplicate.
            validator.Add(Value(4, sequenceNumber: 1));

            var report = validator.CreateReport();
            Assert.Equal(0, report.DuplicateDeliveries);
        }

        [Fact]
        public void NodesThatNeverReportedAreCounted()
        {
            var validator = Create(kTwoSeconds, nodes: 5);
            validator.Add(Value(1, node: "a"));

            var report = validator.CreateReport();
            Assert.Equal(1, report.NodesSeen);
            Assert.Equal(4, report.NodesMissing);
        }

        [Fact]
        public void RepeatWithNewTimestampIsGenuineUnflaggedRepeat()
        {
            // A sample that repeats the value but carries a new SourceTimestamp is
            // NOT a heartbeat — inference requires both value and timestamp to match.
            // This test exists to prevent the inference from being over-broad: a
            // genuine duplicate value with a different timestamp must still be counted
            // so that UnflaggedRepeats remains a reliable signal.
            var validator = Create(kTwoSeconds, nodes: 1);
            validator.Add(Value(1));
            // Same value, but timestamp has advanced → genuine duplicate, not a heartbeat.
            validator.Add(new TelemetrySample("a", 1L, kEpoch.AddSeconds(5), false, null));

            var report = validator.CreateReport();
            Assert.Equal(1, report.RepeatedValues);
            Assert.Equal(0, report.RepeatedValuesFromHeartbeat);
            Assert.Equal(1, report.UnflaggedRepeats);
        }

        [Fact]
        public void EarlyHeartbeatIsDetectedByInferenceWithoutWireIndicator()
        {
            // This is the signal the soak_smoke CI job asserts on. Verify that
            // EarlyHeartbeats is non-zero when a 3.0-shaped message (no Heartbeat
            // member) repeats the last value and timestamp but arrives before the
            // watchdog grace period elapsed.
            var validator = Create(kTwoSeconds, nodes: 1,
                heartbeatInterval: TimeSpan.FromSeconds(10),
                heartbeatTolerance: TimeSpan.FromSeconds(1));

            // Value published at message-time t=2s.
            validator.Add(Value(1, messageTimestamp: kEpoch.AddSeconds(2)));

            // No-wire-flag repeat at message-time t=8s: only 6s after the value,
            // but the earliest legitimate heartbeat is heartbeatInterval + publishing
            // interval = 10s + 2s = 12s after the value.
            validator.Add(NoFlagHeartbeat(1, kEpoch.AddSeconds(2), kEpoch.AddSeconds(8)));

            var report = validator.CreateReport();
            Assert.Equal(1, report.EarlyHeartbeats);
            Assert.Equal(1, report.HeartbeatSamples);
            Assert.Equal(0, report.UnflaggedRepeats);
        }

        [Fact]
        public void WireIndicatorTakesEffectOnTwoPointNineShapedMessages()
        {
            // 2.9-shaped messages carry the Heartbeat wire member. Verify that
            // the wire indicator is still honoured so that existing behaviour is
            // preserved after the structural inference was added.
            var validator = Create(kTwoSeconds, nodes: 1,
                heartbeatInterval: TimeSpan.FromSeconds(10),
                heartbeatTolerance: TimeSpan.FromSeconds(1));

            validator.Add(Value(1, messageTimestamp: kEpoch.AddSeconds(2)));
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(2), kEpoch.AddSeconds(14)));

            var report = validator.CreateReport();
            Assert.Equal(1, report.HeartbeatSamples);
            Assert.Equal(0, report.UnflaggedRepeats);
            Assert.Equal(0, report.EarlyHeartbeats);
            Assert.Equal(0, report.HeartbeatsWithChangedTimestamp);
        }

        [Fact]
        public void WireIndicatorRecognizesHeartbeatWithUpdatedTimestamp()
        {
            // WatchdogLKVWithUpdatedTimestamps advances the SourceTimestamp on each
            // heartbeat. The structural inference cannot detect this (timestamps differ),
            // but the wire indicator makes it explicit. This test verifies the wire
            // indicator is the authoritative path and is not bypassed.
            var validator = Create(kTwoSeconds, nodes: 1,
                heartbeatInterval: TimeSpan.FromSeconds(10),
                heartbeatTolerance: TimeSpan.FromSeconds(1));

            validator.Add(Value(1, messageTimestamp: kEpoch.AddSeconds(2)));
            // Wire flag present; source timestamp advanced (inference would NOT fire).
            validator.Add(Heartbeat(1, kEpoch.AddSeconds(14), kEpoch.AddSeconds(14)));

            var report = validator.CreateReport();
            Assert.Equal(1, report.HeartbeatSamples);
            Assert.Equal(0, report.UnflaggedRepeats);
            Assert.Equal(1, report.HeartbeatsWithChangedTimestamp);
        }

        private static TelemetryQualityValidator Create(TimeSpan updateInterval, int nodes,
            TimeSpan? heartbeatInterval = null, TimeSpan? heartbeatTolerance = null)
        {
            return new TelemetryQualityValidator(new TelemetryQualityOptions
            {
                UpdateInterval = updateInterval,
                ExpectedNodeCount = nodes,
                HeartbeatInterval = heartbeatInterval,
                PublishingInterval = kTwoSeconds,
                HeartbeatTolerance = heartbeatTolerance
            });
        }

        private static TelemetrySample Value(long value, string node = "a",
            DateTime? messageTimestamp = null, uint? sequenceNumber = null)
        {
            return new TelemetrySample(node, value,
                kEpoch.AddTicks(kTwoSeconds.Ticks * value), false,
                messageTimestamp, sequenceNumber);
        }

        private static TelemetrySample Heartbeat(long value, DateTime sourceTimestamp,
            DateTime messageTimestamp, string node = "a")
        {
            return new TelemetrySample(node, value, sourceTimestamp, true,
                messageTimestamp);
        }

        /// <summary>
        /// Creates a sample that structurally looks like a heartbeat — same value
        /// and same SourceTimestamp as its predecessor — but carries no wire indicator.
        /// This is the shape a 3.0 publisher emits for WatchdogLKV heartbeats.
        /// </summary>
        private static TelemetrySample NoFlagHeartbeat(long value, DateTime sourceTimestamp,
            DateTime messageTimestamp, string node = "a")
        {
            return new TelemetrySample(node, value, sourceTimestamp, false,
                messageTimestamp);
        }
    }
}
