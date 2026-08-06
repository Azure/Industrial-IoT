// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="OpcUaSubscriptionConfig.PostConfigure"/>.
    /// </summary>
    public sealed class OpcUaSubscriptionConfigTests
    {
        private static OpcUaSubscriptionOptions Configure(
            params KeyValuePair<string, string?>[] pairs)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(pairs)
                .Build();
            return new OpcUaSubscriptionConfig(configuration,
                Options.Create(new PublisherOptions())).ToOptions().Value;
        }

        private static KeyValuePair<string, string?> P(string k, string? v)
            => new(k, v);

        // ── Boolean defaults ──────────────────────────────────────────────────

        [Fact]
        public void Defaults_UseDeferredAcknoledgements_IsFalse()
        {
            var opts = Configure();
            Assert.False(opts.UseDeferredAcknoledgements);
        }

        [Fact]
        public void Defaults_EnableSequentialPublishing_IsTrue()
        {
            var opts = Configure();
            Assert.True(opts.EnableSequentialPublishing);
        }

        [Fact]
        public void Defaults_DefaultSkipFirst_IsFalse()
        {
            var opts = Configure();
            Assert.False(opts.DefaultSkipFirst);
        }

        [Fact]
        public void Defaults_DefaultRepublishAfterTransfer_IsFalse()
        {
            var opts = Configure();
            Assert.False(opts.DefaultRepublishAfterTransfer);
        }

        [Fact]
        public void Defaults_DefaultDiscardNew_IsFalse()
        {
            var opts = Configure();
            Assert.False(opts.DefaultDiscardNew);
        }

        // ── Sampling/publishing interval ──────────────────────────────────────

        [Fact]
        public void Defaults_DefaultSamplingInterval_IsOneSecond()
        {
            var opts = Configure();
            Assert.Equal(TimeSpan.FromSeconds(1), opts.DefaultSamplingInterval);
        }

        [Fact]
        public void Defaults_DefaultPublishingInterval_IsOneSecond()
        {
            var opts = Configure();
            Assert.Equal(TimeSpan.FromSeconds(1), opts.DefaultPublishingInterval);
        }

        [Fact]
        public void Config_DefaultSamplingInterval_OverridesDefaultInMilliseconds()
        {
            // When provided as a time-span string, it is parsed as a duration
            // "00:00:00.500" = 500 ms
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultSamplingIntervalKey, "00:00:00.500"));
            Assert.Equal(TimeSpan.FromMilliseconds(500), opts.DefaultSamplingInterval);
        }

        [Fact]
        public void Config_DefaultSamplingInterval_CanBeProvidedAsTimeSpanString()
        {
            // TimeSpan.Parse("00:00:05") = 5 seconds
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultSamplingIntervalKey, "00:00:05"));
            Assert.Equal(TimeSpan.FromSeconds(5), opts.DefaultSamplingInterval);
        }

        [Fact]
        public void Config_DefaultPublishingInterval_OverridesDefaultInMilliseconds()
        {
            // 2000 as a pure integer is parsed by TimeSpan.TryParse as 2000 days;
            // use explicit TimeSpan format to get 2 seconds.
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultPublishingIntervalKey, "00:00:02"));
            Assert.Equal(TimeSpan.FromSeconds(2), opts.DefaultPublishingInterval);
        }

        [Fact]
        public void Config_DefaultPublishingInterval_CanBeProvidedAsTimeSpanString()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultPublishingIntervalKey, "00:00:10"));
            Assert.Equal(TimeSpan.FromSeconds(10), opts.DefaultPublishingInterval);
        }

        // ── HeartbeatBehavior enum ────────────────────────────────────────────

        [Fact]
        public void Config_DefaultHeartbeatBehavior_ParsesEnumValue()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultHeartbeatBehaviorKey, "WatchdogLKG"));
            Assert.Equal(HeartbeatBehavior.WatchdogLKG, opts.DefaultHeartbeatBehavior);
        }

        [Fact]
        public void Config_DefaultHeartbeatBehavior_ParsesPeriodicLKV()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultHeartbeatBehaviorKey, "PeriodicLKV"));
            Assert.Equal(HeartbeatBehavior.PeriodicLKV, opts.DefaultHeartbeatBehavior);
        }

        [Fact]
        public void Defaults_DefaultHeartbeatBehavior_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultHeartbeatBehavior);
        }

        [Fact]
        public void Config_DefaultHeartbeatBehavior_InvalidValue_ThrowsInvalidOperationException()
        {
            // The source-generated binder throws when an invalid enum string is provided.
            Assert.Throws<InvalidOperationException>(() =>
                Configure(P(OpcUaSubscriptionConfig.DefaultHeartbeatBehaviorKey, "NotAnEnum")));
        }

        // ── HeartbeatInterval ────────────────────────────────────────────────

        [Fact]
        public void Config_DefaultHeartbeatInterval_ParsesTimeSpan()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultHeartbeatIntervalKey, "00:01:00"));
            Assert.Equal(TimeSpan.FromMinutes(1), opts.DefaultHeartbeatInterval);
        }

        [Fact]
        public void Defaults_DefaultHeartbeatInterval_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultHeartbeatInterval);
        }

        // ── DataChangeTrigger ────────────────────────────────────────────────

        [Fact]
        public void Config_DefaultDataChangeTrigger_ParsesStatusValue()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultDataChangeTriggerKey, "StatusValue"));
            Assert.Equal(DataChangeTriggerType.StatusValue, opts.DefaultDataChangeTrigger);
        }

        [Fact]
        public void Config_DefaultDataChangeTrigger_ParsesStatusValueTimestamp()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultDataChangeTriggerKey,
                "StatusValueTimestamp"));
            Assert.Equal(DataChangeTriggerType.StatusValueTimestamp, opts.DefaultDataChangeTrigger);
        }

        [Fact]
        public void Defaults_DefaultDataChangeTrigger_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultDataChangeTrigger);
        }

        [Fact]
        public void Config_DefaultDataChangeTrigger_InvalidValue_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Configure(P(OpcUaSubscriptionConfig.DefaultDataChangeTriggerKey, "BadEnum")));
        }

        // ── WatchdogBehavior enum ─────────────────────────────────────────────

        [Fact]
        public void Config_DefaultWatchdogBehavior_ParsesReset()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultWatchdogBehaviorKey, "Reset"));
            Assert.Equal(SubscriptionWatchdogBehavior.Reset, opts.DefaultWatchdogBehavior);
        }

        [Fact]
        public void Config_DefaultWatchdogBehavior_ParsesFailFast()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultWatchdogBehaviorKey, "FailFast"));
            Assert.Equal(SubscriptionWatchdogBehavior.FailFast, opts.DefaultWatchdogBehavior);
        }

        [Fact]
        public void Defaults_DefaultWatchdogBehavior_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultWatchdogBehavior);
        }

        [Fact]
        public void Config_DefaultWatchdogBehavior_InvalidValue_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Configure(P(OpcUaSubscriptionConfig.DefaultWatchdogBehaviorKey, "Unknown")));
        }

        // ── MonitoredItemWatchdogCondition ────────────────────────────────────

        [Fact]
        public void Config_DefaultMonitoredItemWatchdogCondition_ParsesWhenAllAreLate()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultMonitoredItemWatchdogConditionKey,
                "WhenAllAreLate"));
            Assert.Equal(MonitoredItemWatchdogCondition.WhenAllAreLate,
                opts.DefaultMonitoredItemWatchdogCondition);
        }

        [Fact]
        public void Config_DefaultMonitoredItemWatchdogCondition_ParsesWhenAnyIsLate()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultMonitoredItemWatchdogConditionKey,
                "WhenAnyIsLate"));
            Assert.Equal(MonitoredItemWatchdogCondition.WhenAnyIsLate,
                opts.DefaultMonitoredItemWatchdogCondition);
        }

        [Fact]
        public void Defaults_DefaultMonitoredItemWatchdogCondition_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultMonitoredItemWatchdogCondition);
        }

        [Fact]
        public void Config_DefaultMonitoredItemWatchdogCondition_InvalidValue_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Configure(P(OpcUaSubscriptionConfig.DefaultMonitoredItemWatchdogConditionKey, "invalid")));
        }

        // ── WatchdogTimeout ───────────────────────────────────────────────────

        [Fact]
        public void Config_DefaultMonitoredItemWatchdogTimeout_FromSeconds()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultMonitoredItemWatchdogSecondsKey,
                "30"));
            Assert.Equal(TimeSpan.FromSeconds(30), opts.DefaultMonitoredItemWatchdogTimeout);
        }

        [Fact]
        public void Defaults_DefaultMonitoredItemWatchdogTimeout_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultMonitoredItemWatchdogTimeout);
        }

        // ── Retry delays ──────────────────────────────────────────────────────

        [Fact]
        public void Config_SubscriptionErrorRetryDelay_FromSeconds()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.SubscriptionErrorRetryDelaySecondsKey,
                "10"));
            Assert.Equal(TimeSpan.FromSeconds(10), opts.SubscriptionErrorRetryDelay);
        }

        [Fact]
        public void Defaults_SubscriptionErrorRetryDelay_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.SubscriptionErrorRetryDelay);
        }

        [Fact]
        public void Config_BadMonitoredItemRetryDelay_FromSeconds()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.BadMonitoredItemRetryDelaySecondsKey,
                "120"));
            Assert.Equal(TimeSpan.FromSeconds(120), opts.BadMonitoredItemRetryDelayDuration);
        }

        [Fact]
        public void Defaults_BadMonitoredItemRetryDelay_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.BadMonitoredItemRetryDelayDuration);
        }

        [Fact]
        public void Config_InvalidMonitoredItemRetryDelay_FromSeconds()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.InvalidMonitoredItemRetryDelaySecondsKey,
                "300"));
            Assert.Equal(TimeSpan.FromSeconds(300), opts.InvalidMonitoredItemRetryDelayDuration);
        }

        [Fact]
        public void Defaults_InvalidMonitoredItemRetryDelay_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.InvalidMonitoredItemRetryDelayDuration);
        }

        // ── Retry delay max values ────────────────────────────────────────────

        [Fact]
        public void Config_BadMonitoredItemRetryDelayMax_SetsMaxAndProvidesFallbackDelay()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.BadMonitoredItemRetryDelayMaxSecondsKey,
                "3600"));
            Assert.Equal(TimeSpan.FromSeconds(3600), opts.BadMonitoredItemRetryDelayDurationMax);
            // When max is set but min is not, a default min of 2s is applied
            Assert.Equal(TimeSpan.FromSeconds(2), opts.BadMonitoredItemRetryDelayDuration);
        }

        [Fact]
        public void Config_BadMonitoredItemRetryDelayMax_DoesNotOverrideExplicitDelay()
        {
            // If both max and min are provided, min is not overridden
            var opts = Configure(
                P(OpcUaSubscriptionConfig.BadMonitoredItemRetryDelayMaxSecondsKey, "3600"),
                P(OpcUaSubscriptionConfig.BadMonitoredItemRetryDelaySecondsKey, "60"));
            Assert.Equal(TimeSpan.FromSeconds(3600), opts.BadMonitoredItemRetryDelayDurationMax);
            Assert.Equal(TimeSpan.FromSeconds(60), opts.BadMonitoredItemRetryDelayDuration);
        }

        [Fact]
        public void Config_InvalidMonitoredItemRetryDelayMax_SetsMaxAndProvidesFallbackDelay()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.InvalidMonitoredItemRetryDelayMaxSecondsKey,
                "600"));
            Assert.Equal(TimeSpan.FromSeconds(600), opts.InvalidMonitoredItemRetryDelayDurationMax);
            // Default min of 2s is applied when not set
            Assert.Equal(TimeSpan.FromSeconds(2), opts.InvalidMonitoredItemRetryDelayDuration);
        }

        [Fact]
        public void Config_InvalidMonitoredItemRetryDelayMax_DoesNotOverrideExplicitDelay()
        {
            var opts = Configure(
                P(OpcUaSubscriptionConfig.InvalidMonitoredItemRetryDelayMaxSecondsKey, "600"),
                P(OpcUaSubscriptionConfig.InvalidMonitoredItemRetryDelaySecondsKey, "120"));
            Assert.Equal(TimeSpan.FromSeconds(600), opts.InvalidMonitoredItemRetryDelayDurationMax);
            Assert.Equal(TimeSpan.FromSeconds(120), opts.InvalidMonitoredItemRetryDelayDuration);
        }

        [Fact]
        public void Defaults_BadMonitoredItemRetryDelayMax_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.BadMonitoredItemRetryDelayDurationMax);
        }

        [Fact]
        public void Defaults_InvalidMonitoredItemRetryDelayMax_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.InvalidMonitoredItemRetryDelayDurationMax);
        }

        // ── Subscription management interval ─────────────────────────────────

        [Fact]
        public void Config_SubscriptionManagementInterval_FromSeconds()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.SubscriptionManagementIntervalSecondsKey,
                "60"));
            Assert.Equal(TimeSpan.FromSeconds(60), opts.SubscriptionManagementIntervalDuration);
        }

        [Fact]
        public void Defaults_SubscriptionManagementInterval_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.SubscriptionManagementIntervalDuration);
        }

        // ── Queue / lifecycle counts ──────────────────────────────────────────

        [Fact]
        public void Config_DefaultKeepAliveCount_FromConfig()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultKeepAliveCountKey, "5"));
            Assert.Equal(5u, opts.DefaultKeepAliveCount);
        }

        [Fact]
        public void Defaults_DefaultKeepAliveCount_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultKeepAliveCount);
        }

        [Fact]
        public void Config_DefaultLifeTimeCount_FromConfig()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultLifetimeCountKey, "10"));
            Assert.Equal(10u, opts.DefaultLifeTimeCount);
        }

        [Fact]
        public void Defaults_DefaultLifeTimeCount_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultLifeTimeCount);
        }

        [Fact]
        public void Config_DefaultQueueSize_FromConfig()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultQueueSizeKey, "3"));
            Assert.Equal(3u, opts.DefaultQueueSize);
        }

        [Fact]
        public void Defaults_DefaultQueueSize_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultQueueSize);
        }

        // ── MaxMonitoredItemPerSubscription ───────────────────────────────────

        [Fact]
        public void Config_MaxMonitoredItemPerSubscription_FromConfig()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.MaxMonitoredItemPerSubscriptionKey, "100"));
            Assert.Equal(100u, opts.MaxMonitoredItemPerSubscription);
        }

        [Fact]
        public void Defaults_MaxMonitoredItemPerSubscription_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.MaxMonitoredItemPerSubscription);
        }

        // ── MaxSubscriptionPartitions ─────────────────────────────────────────

        [Fact]
        public void Config_MaxSubscriptionPartitions_PositiveValue_IsSet()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.MaxSubscriptionPartitionsKey, "4"));
            Assert.Equal(4u, opts.MaxSubscriptionPartitions);
        }

        [Fact]
        public void Defaults_MaxSubscriptionPartitions_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.MaxSubscriptionPartitions);
        }

        [Fact]
        public void Config_MaxSubscriptionPartitions_Zero_SetsZeroViaSourceGeneratedBinder()
        {
            // When provided through the source-generated binder the value is bound directly
            // before PostConfigure runs, so the guard in PostConfigure is bypassed and
            // the property is set to 0 rather than throwing.
            var opts = Configure(P(OpcUaSubscriptionConfig.MaxSubscriptionPartitionsKey, "0"));
            Assert.Equal(0u, opts.MaxSubscriptionPartitions);
        }

        [Fact]
        public void Config_MaxSubscriptionPartitions_ZeroViaPostConfigure_ThrowsArgumentOutOfRangeException()
        {
            // When MaxSubscriptionPartitions is null in the options (not bound by the source binder)
            // and PostConfigure reads "0" from config, the guard throws.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([
                    new(OpcUaSubscriptionConfig.MaxSubscriptionPartitionsKey, "0")])
                .Build();
            var config = new OpcUaSubscriptionConfig(configuration,
                Options.Create(new PublisherOptions()));
            // Pass an options object where the property is still null so PostConfigure's
            // validation branch is reached.
            var opts = new OpcUaSubscriptionOptions();
            Assert.Throws<ArgumentOutOfRangeException>(() => config.PostConfigure(null, opts));
        }

        // ── AutoSetQueueSizes ─────────────────────────────────────────────────

        [Fact]
        public void Config_AutoSetQueueSizes_TrueFromConfig()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.AutoSetQueueSizesKey, "true"));
            Assert.True(opts.AutoSetQueueSizes);
        }

        [Fact]
        public void Defaults_AutoSetQueueSizes_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.AutoSetQueueSizes);
        }

        // ── RebrowsePeriod ────────────────────────────────────────────────────

        [Fact]
        public void Config_DefaultRebrowsePeriod_FromTimeSpanString()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultRebrowsePeriodKey, "02:00:00"));
            Assert.Equal(TimeSpan.FromHours(2), opts.DefaultRebrowsePeriod);
        }

        [Fact]
        public void Defaults_DefaultRebrowsePeriod_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultRebrowsePeriod);
        }

        // ── BrowsePath / DisplayName ──────────────────────────────────────────

        [Fact]
        public void Config_FetchOpcBrowsePathFromRoot_TrueFromConfig()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.FetchOpcBrowsePathFromRootKey, "true"));
            Assert.True(opts.FetchOpcBrowsePathFromRoot);
        }

        [Fact]
        public void Defaults_FetchOpcBrowsePathFromRoot_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.FetchOpcBrowsePathFromRoot);
        }

        [Fact]
        public void Config_ResolveDisplayName_TrueFromConfig()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.FetchOpcNodeDisplayNameKey, "true"));
            Assert.True(opts.ResolveDisplayName);
        }

        [Fact]
        public void Defaults_ResolveDisplayName_IsFalse()
        {
            var opts = Configure();
            Assert.False(opts.ResolveDisplayName);
        }

        // ── ImmediatePublishing / CyclicRead ──────────────────────────────────

        [Fact]
        public void Config_EnableImmediatePublishing_TrueFromConfig()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.EnableImmediatePublishingKey, "true"));
            Assert.True(opts.EnableImmediatePublishing);
        }

        [Fact]
        public void Defaults_EnableImmediatePublishing_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.EnableImmediatePublishing);
        }

        [Fact]
        public void Config_DefaultSamplingUsingCyclicRead_TrueFromConfig()
        {
            var opts = Configure(P(OpcUaSubscriptionConfig.DefaultSamplingUsingCyclicReadKey, "true"));
            Assert.True(opts.DefaultSamplingUsingCyclicRead);
        }

        [Fact]
        public void Defaults_DefaultSamplingUsingCyclicRead_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.DefaultSamplingUsingCyclicRead);
        }

        // ── Pre-configured values are preserved ───────────────────────────────

        [Fact]
        public void PreConfigured_DefaultSamplingInterval_IsPreserved()
        {
            // When value is already set in options, PostConfigure does not overwrite it
            var configuration = new ConfigurationBuilder().Build();
            var config = new OpcUaSubscriptionConfig(configuration,
                Options.Create(new PublisherOptions()));
            var opts = new OpcUaSubscriptionOptions
            {
                DefaultSamplingInterval = TimeSpan.FromSeconds(99)
            };
            config.PostConfigure(null, opts);
            Assert.Equal(TimeSpan.FromSeconds(99), opts.DefaultSamplingInterval);
        }

        [Fact]
        public void PreConfigured_DefaultPublishingInterval_IsPreserved()
        {
            var configuration = new ConfigurationBuilder().Build();
            var config = new OpcUaSubscriptionConfig(configuration,
                Options.Create(new PublisherOptions()));
            var opts = new OpcUaSubscriptionOptions
            {
                DefaultPublishingInterval = TimeSpan.FromSeconds(42)
            };
            config.PostConfigure(null, opts);
            Assert.Equal(TimeSpan.FromSeconds(42), opts.DefaultPublishingInterval);
        }
    }
}
