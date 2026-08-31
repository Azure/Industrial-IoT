// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class ManagedSubscriptionRetryPolicyTests
    {
        [Fact]
        public void ClassifiesRetryCategories()
        {
            Assert.Equal(ManagedItemRetryKind.None,
                ManagedSubscriptionRetryPolicy.Classify(StatusCodes.Good));
            Assert.Equal(ManagedItemRetryKind.None,
                ManagedSubscriptionRetryPolicy.Classify(
                    StatusCodes.BadTooManyMonitoredItems));
            Assert.Equal(ManagedItemRetryKind.Subscription,
                ManagedSubscriptionRetryPolicy.Classify(
                    StatusCodes.BadNotConnected));
            Assert.Equal(ManagedItemRetryKind.Invalid,
                ManagedSubscriptionRetryPolicy.Classify(
                    StatusCodes.BadNodeIdUnknown));
            Assert.Equal(ManagedItemRetryKind.Invalid,
                ManagedSubscriptionRetryPolicy.Classify(
                    StatusCodes.BadMonitoredItemIdInvalid));
            Assert.Equal(ManagedItemRetryKind.Bad,
                ManagedSubscriptionRetryPolicy.Classify(
                    StatusCodes.BadMonitoredItemIdInvalid,
                    localFailure: true));
        }

        [Fact]
        public void UsesClassicDefaultAndDisabledDelays()
        {
            var options = new OpcUaSubscriptionOptions();

            Assert.Equal(TimeSpan.FromSeconds(2),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Subscription, options, 1));
            Assert.Equal(TimeSpan.FromMinutes(5),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 1));
            Assert.Equal(TimeSpan.FromMinutes(30),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Bad, options, 1));

            options.InvalidMonitoredItemRetryDelayDuration = TimeSpan.Zero;
            Assert.Equal(TimeSpan.MaxValue,
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 1));
        }

        [Fact]
        public void PreservesConstantAndExponentialDelayBehavior()
        {
            var options = new OpcUaSubscriptionOptions
            {
                InvalidMonitoredItemRetryDelayDuration =
                    TimeSpan.FromSeconds(30)
            };
            Assert.Equal(TimeSpan.FromSeconds(30),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 5));

            options.InvalidMonitoredItemRetryDelayDuration =
                TimeSpan.FromSeconds(-2);
            options.InvalidMonitoredItemRetryDelayDurationMax =
                TimeSpan.FromSeconds(20);
            Assert.Equal(TimeSpan.FromSeconds(4),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 1));
            Assert.Equal(TimeSpan.FromSeconds(20),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 5));

            options.InvalidMonitoredItemRetryDelayDuration =
                TimeSpan.FromSeconds(-1);
            options.InvalidMonitoredItemRetryDelayDurationMax =
                TimeSpan.FromSeconds(2);
            Assert.Equal(TimeSpan.FromSeconds(10),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 4));
        }

        [Fact]
        public void PreservesClassicBadMaximumOptionPairing()
        {
            var options = new OpcUaSubscriptionOptions
            {
                BadMonitoredItemRetryDelayDuration = TimeSpan.FromSeconds(-2),
                BadMonitoredItemRetryDelayDurationMax = TimeSpan.FromSeconds(60),
                InvalidMonitoredItemRetryDelayDurationMax =
                    TimeSpan.FromSeconds(12)
            };

            Assert.Equal(TimeSpan.FromSeconds(12),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Bad, options, 5));
        }

        [Fact]
        public async Task SchedulerAdvancesAttemptsAfterFailedApply()
        {
            var requests = new List<ManagedRetryRequest>();
            await using var scheduler = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(), TimeProvider.System,
                (request, _) =>
                {
                    requests.Add(request);
                    return ValueTask.FromResult(ManagedRetryOutcome.Started);
                });
            var failed = new ManagedItemRetryTarget("item", 1, 7,
                ManagedItemRetryKind.Invalid, StatusCodes.BadNodeIdUnknown,
                Pending: false, Applied: false);
            scheduler.Update(failed);

            await scheduler.ProcessAsync(force: true);
            scheduler.Update(failed with { Pending = true });
            scheduler.Update(failed);
            await scheduler.ProcessAsync(force: true);
            scheduler.Update(failed with
            {
                Kind = ManagedItemRetryKind.None,
                Status = StatusCodes.Good,
                Applied = true
            });
            scheduler.Update(failed with { Generation = 8 });
            await scheduler.ProcessAsync(force: true);

            Assert.Equal([1, 2, 1],
                requests.ConvertAll(request => request.Attempt));
            Assert.Equal(1, scheduler.Count);
        }

        [Fact]
        public async Task SchedulerTracksSubscriptionRetry()
        {
            ManagedRetryRequest? observed = null;
            await using var scheduler = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(), TimeProvider.System,
                (request, _) =>
                {
                    observed = request;
                    return ValueTask.FromResult(ManagedRetryOutcome.Started);
                });
            scheduler.UpdateSubscription(failed: true);

            await scheduler.ProcessAsync(force: true);

            Assert.NotNull(observed);
            Assert.Null(observed.Value.Name);
            Assert.Equal(ManagedItemRetryKind.Subscription,
                observed.Value.Kind);
            scheduler.UpdateSubscription(failed: false);
            Assert.Equal(0, scheduler.Count);
        }

        [Fact]
        public async Task SchedulerReschedulesSynchronousFailure()
        {
            var requests = new List<ManagedRetryRequest>();
            var failed = new ManagedItemRetryTarget("item", 1, 7,
                ManagedItemRetryKind.Invalid, StatusCodes.BadNodeIdUnknown,
                Pending: false, Applied: false);
            ManagedSubscriptionRetryScheduler scheduler = null!;
            scheduler = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(), TimeProvider.System,
                (request, _) =>
                {
                    requests.Add(request);
                    scheduler.Update(failed);
                    return ValueTask.FromResult(ManagedRetryOutcome.Started);
                });
            await using var owned = scheduler;
            scheduler.Update(failed);

            await scheduler.ProcessAsync(force: true);
            await scheduler.ProcessAsync(force: true);

            Assert.Equal([1, 2],
                requests.ConvertAll(request => request.Attempt));
        }

        [Fact]
        public async Task SchedulerSkipsRemovedCapturedRetry()
        {
            var requests = new List<ManagedRetryRequest>();
            ManagedSubscriptionRetryScheduler scheduler = null!;
            scheduler = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(), TimeProvider.System,
                (request, _) =>
                {
                    requests.Add(request);
                    scheduler.Remove(request.Name == "first"
                        ? "second" : "first");
                    return ValueTask.FromResult(ManagedRetryOutcome.Started);
                });
            await using var owned = scheduler;
            scheduler.Update(new ManagedItemRetryTarget("first", 1, 1,
                ManagedItemRetryKind.Invalid, StatusCodes.BadNodeIdUnknown,
                Pending: false, Applied: false));
            scheduler.Update(new ManagedItemRetryTarget("second", 2, 1,
                ManagedItemRetryKind.Invalid, StatusCodes.BadNodeIdUnknown,
                Pending: false, Applied: false));

            await scheduler.ProcessAsync(force: true);

            Assert.Single(requests);
        }

        [Fact]
        public async Task DisabledRetryIsNotProcessed()
        {
            var requests = new List<ManagedRetryRequest>();
            var options = new OpcUaSubscriptionOptions
            {
                InvalidMonitoredItemRetryDelayDuration = TimeSpan.Zero
            };
            await using var scheduler = new ManagedSubscriptionRetryScheduler(
                options, TimeProvider.System,
                (request, _) =>
                {
                    requests.Add(request);
                    return ValueTask.FromResult(ManagedRetryOutcome.Started);
                });
            var failed = new ManagedItemRetryTarget("item", 1, 1,
                ManagedItemRetryKind.Invalid, StatusCodes.BadNodeIdUnknown,
                Pending: false, Applied: false);
            scheduler.Update(failed);

            await scheduler.ProcessAsync(force: true);

            Assert.Empty(requests);
            scheduler.Update(failed with
            {
                Kind = ManagedItemRetryKind.None,
                Status = StatusCodes.Good,
                Applied = true
            });
            Assert.Equal(0, scheduler.Count);
        }

        [Fact]
        public async Task DisposeClearsScheduledRetries()
        {
            var scheduler = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(), TimeProvider.System,
                (_, _) => ValueTask.FromResult(ManagedRetryOutcome.Started));
            scheduler.Update(new ManagedItemRetryTarget("item", 1, 1,
                ManagedItemRetryKind.Invalid, StatusCodes.BadNodeIdUnknown,
                Pending: false, Applied: false));
            Assert.Equal(1, scheduler.Count);

            await scheduler.DisposeAsync();

            Assert.Equal(0, scheduler.Count);
        }

        [Fact]
        public void ClassifiesAllSubscriptionStatusCodes()
        {
            // All communication-level codes should be Subscription
            Assert.Equal(ManagedItemRetryKind.Subscription,
                ManagedSubscriptionRetryPolicy.Classify(StatusCodes.BadCommunicationError));
            Assert.Equal(ManagedItemRetryKind.Subscription,
                ManagedSubscriptionRetryPolicy.Classify(StatusCodes.BadSecureChannelClosed));
            Assert.Equal(ManagedItemRetryKind.Subscription,
                ManagedSubscriptionRetryPolicy.Classify(StatusCodes.BadSessionClosed));
            Assert.Equal(ManagedItemRetryKind.Subscription,
                ManagedSubscriptionRetryPolicy.Classify(StatusCodes.BadSubscriptionIdInvalid));
        }

        [Fact]
        public void GetDelay_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, null!, 1));
        }

        [Fact]
        public void GetDelay_NoneKind_ReturnsTimeSpanMaxValue()
        {
            var options = new OpcUaSubscriptionOptions();
            Assert.Equal(TimeSpan.MaxValue,
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.None, options, 1));
        }

        [Fact]
        public void GetDelay_SubscriptionKindWithLargeExplicitDelay_ReturnsConstantDelay()
        {
            // delay > 10s and no maxDelay → constant (no backoff)
            var options = new OpcUaSubscriptionOptions
            {
                SubscriptionErrorRetryDelay = TimeSpan.FromSeconds(30)
            };
            Assert.Equal(TimeSpan.FromSeconds(30),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Subscription, options, 1));
            Assert.Equal(TimeSpan.FromSeconds(30),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Subscription, options, 5));
        }

        [Fact]
        public void GetDelay_MaximumLessThanMinimum_ReturnsMinimum()
        {
            // maxDelay < delay: maximum <= minimum → returns minimum
            var options = new OpcUaSubscriptionOptions
            {
                InvalidMonitoredItemRetryDelayDuration = TimeSpan.FromSeconds(20),
                InvalidMonitoredItemRetryDelayDurationMax = TimeSpan.FromSeconds(5)
            };
            // minimum = 20s, maximum = 5s → maximum <= minimum → return minimum
            Assert.Equal(TimeSpan.FromSeconds(20),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 1));
        }

        [Fact]
        public void GetDelay_AttemptZero_ClampedToOne()
        {
            // attempt = 0 → exponent clamped to 1
            var options = new OpcUaSubscriptionOptions
            {
                InvalidMonitoredItemRetryDelayDuration = TimeSpan.FromSeconds(-1),
                InvalidMonitoredItemRetryDelayDurationMax = TimeSpan.FromMinutes(10)
            };
            var delay0 = ManagedSubscriptionRetryPolicy.GetDelay(
                ManagedItemRetryKind.Invalid, options, 0);
            var delay1 = ManagedSubscriptionRetryPolicy.GetDelay(
                ManagedItemRetryKind.Invalid, options, 1);

            Assert.Equal(delay1, delay0);
        }

        [Fact]
        public void GetDelay_HighAttempt_ClampedAtTen()
        {
            // attempt > 10 → exponent clamped to 10
            var options = new OpcUaSubscriptionOptions
            {
                InvalidMonitoredItemRetryDelayDuration = TimeSpan.FromSeconds(-1),
                InvalidMonitoredItemRetryDelayDurationMax = TimeSpan.FromHours(24)
            };
            var delay10 = ManagedSubscriptionRetryPolicy.GetDelay(
                ManagedItemRetryKind.Invalid, options, 10);
            var delay100 = ManagedSubscriptionRetryPolicy.GetDelay(
                ManagedItemRetryKind.Invalid, options, 100);

            Assert.Equal(delay10, delay100);
        }

        [Fact]
        public void GetDelay_SmallPositiveDelay_WithMaxDelay_ExponentialBackoff()
        {
            // delay > 0, has maxDelay, delay <= 10s → exponential
            var options = new OpcUaSubscriptionOptions
            {
                InvalidMonitoredItemRetryDelayDuration = TimeSpan.FromSeconds(1),
                InvalidMonitoredItemRetryDelayDurationMax = TimeSpan.FromSeconds(60)
            };
            // attempt 1: 1s * 2^1 = 2s
            Assert.Equal(TimeSpan.FromSeconds(2),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 1));
            // attempt 5: 1s * 2^5 = 32s
            Assert.Equal(TimeSpan.FromSeconds(32),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 5));
            // attempt 10: 1s * 2^10 = 1024s > 60s → capped at 60s
            Assert.Equal(TimeSpan.FromSeconds(60),
                ManagedSubscriptionRetryPolicy.GetDelay(
                    ManagedItemRetryKind.Invalid, options, 10));
        }
    }
}
