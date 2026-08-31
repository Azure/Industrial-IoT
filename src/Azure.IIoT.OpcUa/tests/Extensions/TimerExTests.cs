// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class TimerExTests
    {
        [Fact]
        public void IntervalRejectsNonPositiveValues()
        {
            using var timer = new TimerEx();

            Assert.Throws<ArgumentException>(() => timer.Interval = TimeSpan.Zero);
            Assert.Throws<ArgumentException>(() => timer.Interval = TimeSpan.FromMilliseconds(-1));
        }

        [Fact]
        public void StartCreatesTimerAndStopDisposesIt()
        {
            var provider = new ManualTimeProvider();
            using var timer = new TimerEx(TimeSpan.FromSeconds(1), provider);

            timer.Start();

            var created = Assert.Single(provider.Timers);
            Assert.Equal(TimeSpan.FromSeconds(1), created.DueTime);
            Assert.Equal(TimeSpan.FromSeconds(1), created.Period);

            timer.Stop();

            Assert.Equal(true, created.Disposed);
            Assert.Equal(false, timer.Enabled);
        }

        [Fact]
        public void ChangingIntervalAndAutoResetUpdatesActiveTimer()
        {
            var provider = new ManualTimeProvider();
            using var timer = new TimerEx(TimeSpan.FromSeconds(1), provider);
            timer.Start();
            var created = Assert.Single(provider.Timers);

            timer.Interval = TimeSpan.FromSeconds(2);
            timer.AutoReset = false;

            Assert.Equal(TimeSpan.FromSeconds(2), created.DueTime);
            Assert.Equal(Timeout.InfiniteTimeSpan, created.Period);
        }

        [Fact]
        public void TriggerRaisesElapsedWithProviderTime()
        {
            var provider = new ManualTimeProvider();
            using var timer = new TimerEx(TimeSpan.FromSeconds(1), provider);
            DateTimeOffset? signaled = null;
            timer.Elapsed += (_, e) => signaled = e.SignalTime;
            timer.Start();

            provider.Timers[0].Trigger();

            Assert.Equal(provider.UtcNow, signaled);
        }

        [Fact]
        public void NonAutoResetTriggerDisablesTimer()
        {
            var provider = new ManualTimeProvider();
            using var timer = new TimerEx(TimeSpan.FromSeconds(1), provider)
            {
                AutoReset = false
            };
            timer.Start();

            provider.Timers[0].Trigger();

            Assert.Equal(false, timer.Enabled);
        }

        [Fact]
        public void StoppedTimerIgnoresAlreadyQueuedCallback()
        {
            var provider = new ManualTimeProvider();
            using var timer = new TimerEx(TimeSpan.FromSeconds(1), provider);
            var calls = 0;
            timer.Elapsed += (_, _) => calls++;
            timer.Start();
            var created = provider.Timers[0];
            timer.Stop();

            created.Trigger();

            Assert.Equal(0, calls);
        }

        [Fact]
        public void ElapsedHandlerExceptionsAreSwallowed()
        {
            var provider = new ManualTimeProvider();
            using var timer = new TimerEx(TimeSpan.FromSeconds(1), provider);
            timer.Elapsed += (_, _) => throw new InvalidOperationException("boom");
            timer.Start();

            var exception = Record.Exception(() => provider.Timers[0].Trigger());

            Assert.Null(exception);
        }

        [Fact]
        public void StartAfterDisposeThrows()
        {
            var timer = new TimerEx(TimeSpan.FromSeconds(1), new ManualTimeProvider());
            timer.Dispose();

            Assert.Throws<ObjectDisposedException>(() => timer.Start());
        }

        private sealed class ManualTimeProvider : TimeProvider
        {
            public DateTimeOffset UtcNow { get; } =
                new(2026, 8, 3, 21, 25, 0, TimeSpan.Zero);
            public List<ManualTimer> Timers { get; } = [];

            public override DateTimeOffset GetUtcNow()
            {
                return UtcNow;
            }

            public override ITimer CreateTimer(TimerCallback callback,
                object? state, TimeSpan dueTime, TimeSpan period)
            {
                var timer = new ManualTimer(callback, state, dueTime, period);
                Timers.Add(timer);
                return timer;
            }
        }

        private sealed class ManualTimer : ITimer
        {
            public TimeSpan DueTime { get; private set; }
            public TimeSpan Period { get; private set; }
            public bool Disposed { get; private set; }

            public ManualTimer(TimerCallback callback, object? state,
                TimeSpan dueTime, TimeSpan period)
            {
                _callback = callback;
                _state = state;
                DueTime = dueTime;
                Period = period;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                DueTime = dueTime;
                Period = period;
                return true;
            }

            public void Dispose()
            {
                Disposed = true;
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public void Trigger()
            {
                _callback(_state);
            }

            private readonly TimerCallback _callback;
            private readonly object? _state;
        }

        //
        // Rearm postpones the next elapse without restarting a full countdown.
        // The heartbeat watchdog needs this to wait out the remainder of a
        // period after it elapsed while a value was still in flight; assigning
        // Interval instead would restart the countdown with a full interval and
        // double the heartbeat latency once values genuinely stop.
        //

        [Fact]
        public async Task RearmMakesTheTimerElapseBeforeTheConfiguredIntervalAsync()
        {
            using var elapsed = new SemaphoreSlim(0);
            using var timer = new TimerEx(TimeSpan.FromMinutes(5))
            {
                AutoReset = true
            };
            timer.Elapsed += (_, _) => elapsed.Release();
            timer.Enabled = true;

            timer.Rearm(TimeSpan.FromMilliseconds(50));

            Assert.True(await elapsed.WaitAsync(TimeSpan.FromSeconds(30)),
                "Timer did not elapse after being re-armed.");
        }

        [Fact]
        public void RearmDoesNotChangeTheConfiguredInterval()
        {
            using var timer = new TimerEx(TimeSpan.FromMinutes(5))
            {
                Enabled = true
            };

            timer.Rearm(TimeSpan.FromMilliseconds(50));

            Assert.Equal(TimeSpan.FromMinutes(5), timer.Interval);
        }

        [Fact]
        public async Task PeriodIsKeptAfterARearmedElapseAsync()
        {
            using var elapsed = new SemaphoreSlim(0);
            using var timer = new TimerEx(TimeSpan.FromMilliseconds(50))
            {
                AutoReset = true
            };
            timer.Elapsed += (_, _) => elapsed.Release();
            timer.Enabled = true;

            timer.Rearm(TimeSpan.FromMilliseconds(10));

            // First elapse comes from the re-arm, the following ones from the
            // unchanged period.
            for (var i = 0; i < 3; i++)
            {
                Assert.True(await elapsed.WaitAsync(TimeSpan.FromSeconds(30)),
                    $"Timer stopped elapsing after {i} elapse(s).");
            }
        }

        [Fact]
        public void RearmWithNonPositiveDueTimeThrows()
        {
            using var timer = new TimerEx(TimeSpan.FromMinutes(5))
            {
                Enabled = true
            };

            Assert.Throws<ArgumentException>(() => timer.Rearm(TimeSpan.Zero));
            Assert.Throws<ArgumentException>(
                () => timer.Rearm(TimeSpan.FromMilliseconds(-1)));
        }

        [Fact]
        public void RearmOnADisabledTimerIsANoop()
        {
            using var timer = new TimerEx(TimeSpan.FromMinutes(5));

            // Does not throw even though no underlying timer exists yet.
            timer.Rearm(TimeSpan.FromMilliseconds(50));
        }
    }
}
