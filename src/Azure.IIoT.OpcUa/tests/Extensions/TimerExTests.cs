// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Extensions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="TimerEx.Rearm(TimeSpan)"/>, which postpones the
    /// next elapse without restarting a full countdown. The heartbeat
    /// watchdog needs this to wait out the remainder of a period after it
    /// elapsed while a value was still in flight; assigning
    /// <see cref="TimerEx.Interval"/> instead would restart the countdown
    /// with a full interval and double the heartbeat latency once values
    /// genuinely stop.
    /// </summary>
    public class TimerExTests
    {
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
