// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="RollingAverage"/> — pure logic, no OPC UA server.
    /// </summary>
    public sealed class RollingAverageTests
    {
        // ── Initial state ─────────────────────────────────────────────────────

        [Fact]
        public void InitialState_CountAndLastMinuteAreZero()
        {
            var tp = new ManualTimeProvider();
            var ra = new RollingAverage(tp);

            Assert.Equal(0, ra.Count);
            Assert.Equal(0, ra.LastMinute);
        }

        // ── Count setter ──────────────────────────────────────────────────────

        [Fact]
        public void Count_SetOnce_LastMinuteReflectsValue()
        {
            var tp = new ManualTimeProvider();
            var ra = new RollingAverage(tp);

            ra.Count = 10;

            Assert.Equal(10, ra.Count);
            Assert.Equal(10, ra.LastMinute);
        }

        [Fact]
        public void Count_SetTwiceIncrementally_LastMinuteReflectsTotalWithinWindow()
        {
            var tp = new ManualTimeProvider();
            var ra = new RollingAverage(tp);

            ra.Count = 5;
            ra.Count = 15;

            Assert.Equal(15, ra.Count);
            // Both increments are within the 60-second window
            Assert.Equal(15, ra.LastMinute);
        }

        [Fact]
        public void Count_SetTwiceWithDecrease_LastMinuteReflectsNet()
        {
            var tp = new ManualTimeProvider();
            var ra = new RollingAverage(tp);

            ra.Count = 10;
            ra.Count = 6;   // Decrease by 4

            Assert.Equal(6, ra.Count);
            // 10 added, then -4 added → net +6 within window
            Assert.Equal(6, ra.LastMinute);
        }

        // ── Time-based expiry ─────────────────────────────────────────────────

        [Fact]
        public void LastMinute_AfterExactly60Seconds_ExpiresOldBuckets()
        {
            var tp = new ManualTimeProvider();
            var ra = new RollingAverage(tp);

            ra.Count = 100;

            // Advance by ≥ 60 seconds so all buckets are stale
            tp.Advance(TimeSpan.FromSeconds(61));

            // Force a read to clear stale buckets
            var lastMinute = ra.LastMinute;

            Assert.Equal(0, lastMinute);
        }

        [Fact]
        public void LastMinute_NewIncrementAfterExpiry_ReflectsOnlyNewValue()
        {
            var tp = new ManualTimeProvider();
            var ra = new RollingAverage(tp);

            ra.Count = 100;

            tp.Advance(TimeSpan.FromSeconds(61));

            ra.Count = 110;  // +10 in new window

            Assert.Equal(110, ra.Count);
            Assert.Equal(10, ra.LastMinute);
        }

        [Fact]
        public void LastMinute_WithinWindow_AccumulatesAcrossBuckets()
        {
            var tp = new ManualTimeProvider();
            var ra = new RollingAverage(tp);

            ra.Count = 5;

            tp.Advance(TimeSpan.FromSeconds(1));
            ra.Count = 15;  // +10

            tp.Advance(TimeSpan.FromSeconds(1));
            ra.Count = 30;  // +15

            // All 3 increments are within the 60-second window
            Assert.Equal(30, ra.LastMinute);
        }

        // ── LastMinute setter ─────────────────────────────────────────────────

        [Fact]
        public void LastMinute_SetDirectly_ReflectedInGetter()
        {
            var tp = new ManualTimeProvider();
            var ra = new RollingAverage(tp);

            ra.LastMinute = 42;

            Assert.Equal(42, ra.LastMinute);
        }

        [Fact]
        public void LastMinute_SetMultipleTimesInSameSecond_Accumulates()
        {
            var tp = new ManualTimeProvider();
            var ra = new RollingAverage(tp);

            ra.LastMinute = 10;
            ra.LastMinute = 20;

            Assert.Equal(30, ra.LastMinute);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// A controllable <see cref="TimeProvider"/> for deterministic tests.
        /// </summary>
        private sealed class ManualTimeProvider : TimeProvider
        {
            private DateTimeOffset _utcNow = new DateTimeOffset(2024, 1, 1, 0, 0, 0,
                TimeSpan.Zero);

            public void Advance(TimeSpan elapsed)
            {
                _utcNow += elapsed;
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;

            public override ITimer CreateTimer(TimerCallback callback, object? state,
                TimeSpan dueTime, TimeSpan period)
            {
                return new NoopTimer();
            }

            private sealed class NoopTimer : ITimer
            {
                public bool Change(TimeSpan dueTime, TimeSpan period) => true;
                public ValueTask DisposeAsync() => ValueTask.CompletedTask;
                public void Dispose() { }
            }
        }
    }
}
