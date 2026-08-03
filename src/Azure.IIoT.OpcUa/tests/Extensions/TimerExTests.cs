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
    }
}
