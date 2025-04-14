// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System;

    /// <summary>
    /// Subscription options configuration
    /// </summary>
    public sealed class OpcUaSubscriptionConfig : PostConfigureOptionBase<OpcUaSubscriptionOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string DefaultHeartbeatIntervalKey = "DefaultHeartbeatInterval";
        public const string DefaultHeartbeatBehaviorKey = "DefaultHeartbeatBehavior";
        public const string DefaultSkipFirstKey = "DefaultSkipFirst";
        public const string DefaultRepublishAfterTransferKey = "RepublishAfterTransfer";
        public const string DefaultDiscardNewKey = "DiscardNew";
        public const string DefaultSamplingIntervalKey = "DefaultSamplingInterval";
        public const string DefaultPublishingIntervalKey = "DefaultPublishingInterval";
        public const string DefaultDataChangeTriggerKey = "DefaultDataChangeTrigger";
        public const string FetchOpcNodeDisplayNameKey = "FetchOpcNodeDisplayName";
        public const string FetchOpcBrowsePathFromRootKey = "FetchOpcBrowsePathFromRoot";
        public const string DefaultQueueSizeKey = "DefaultQueueSize";
        public const string AutoSetQueueSizesKey = "AutoSetQueueSizes";
        public const string DefaultLifetimeCountKey = "DefaultLifetimeCount";
        public const string DefaultKeepAliveCountKey = "DefaultKeepAliveCount";
        public const string MaxMonitoredItemPerSubscriptionKey = "MaxMonitoredItemPerSubscription";
        public const string UseDeferredAcknoledgementsKey = "UseDeferredAcknoledgements";
        public const string DefaultSamplingUsingCyclicReadKey = "DefaultSamplingUsingCyclicRead";
        public const string EnableImmediatePublishingKey = "EnableImmediatePublishing";
        public const string EnableSequentialPublishingKey = "EnableSequentialPublishing";
        public const string DefaultRebrowsePeriodKey = "DefaultRebrowsePeriod";
        public const string DefaultWatchdogBehaviorKey = "DefaultWatchdogBehavior";
        public const string DefaultMonitoredItemWatchdogConditionKey = "DefaultMonitoredItemWatchdogCondition";
        public const string DefaultMonitoredItemWatchdogSecondsKey = "DefaultMonitoredItemWatchdogSeconds";
        public const string SubscriptionErrorRetryDelaySecondsKey = "SubscriptionErrorRetryDelaySeconds";
        public const string InvalidMonitoredItemRetryDelaySecondsKey = "InvalidMonitoredItemRetryDelaySeconds";
        public const string BadMonitoredItemRetryDelaySecondsKey = "BadMonitoredItemRetryDelaySeconds";
        public const string SubscriptionManagementIntervalSecondsKey = "SubscriptionManagementIntervalSeconds";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Default values
        /// </summary>
        public const bool ResolveDisplayNameDefault = false;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const int DefaultSamplingIntervalDefaultMillis = 1000;
        public const int DefaultPublishingIntervalDefaultMillis = 1000;
        public const bool DefaultSkipFirstDefault = false;
        public const bool DefaultRepublishAfterTransferDefault = false;
        public const bool EnableSequentialPublishingDefault = true;
        public const bool UseDeferredAcknoledgementsDefault = false;
        public const int SubscriptionErrorRetryDelayDefaultSec = 2;
        public const int InvalidMonitoredItemRetryDelayDefaultSec = 5 * 60;
        public const int BadMonitoredItemRetryDelayDefaultSec = 30 * 60;
        public const bool DefaultDiscardNewDefault = false;
        public static readonly TimeSpan DefaultRebrowsePeriodDefault = TimeSpan.FromHours(12);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string? name, OpcUaSubscriptionOptions options)
        {
            options.UseDeferredAcknoledgements ??= GetBoolOrDefault(
                    UseDeferredAcknoledgementsKey, UseDeferredAcknoledgementsDefault);
            if (options.DefaultHeartbeatInterval == null)
            {
                options.DefaultHeartbeatInterval = GetDurationOrNull(
                    DefaultHeartbeatIntervalKey);
            }
            if (options.DefaultHeartbeatBehavior == null &&
                Enum.TryParse<HeartbeatBehavior>(GetStringOrDefault(DefaultHeartbeatBehaviorKey),
                    out var behavior))
            {
                options.DefaultHeartbeatBehavior = behavior;
            }
            options.DefaultSamplingUsingCyclicRead ??= GetBoolOrNull(DefaultSamplingUsingCyclicReadKey);
            if (options.DefaultRebrowsePeriod == null)
            {
                options.DefaultRebrowsePeriod = GetDurationOrNull(DefaultRebrowsePeriodKey);
            }
            options.DefaultSkipFirst ??= GetBoolOrDefault(DefaultSkipFirstKey,
                    DefaultSkipFirstDefault);
            options.DefaultRepublishAfterTransfer ??= GetBoolOrDefault(DefaultRepublishAfterTransferKey,
                    DefaultRepublishAfterTransferDefault);
            options.DefaultDiscardNew ??= GetBoolOrDefault(DefaultDiscardNewKey,
                    DefaultDiscardNewDefault);
            if (options.DefaultSamplingInterval == null)
            {
                options.DefaultSamplingInterval = GetDurationOrNull(DefaultSamplingIntervalKey) ??
                    TimeSpan.FromMilliseconds(GetIntOrDefault(DefaultSamplingIntervalKey,
                    DefaultSamplingIntervalDefaultMillis));
            }
            if (options.DefaultPublishingInterval == null)
            {
                options.DefaultPublishingInterval = GetDurationOrNull(DefaultPublishingIntervalKey) ??
                    TimeSpan.FromMilliseconds(GetIntOrDefault(DefaultPublishingIntervalKey,
                        DefaultPublishingIntervalDefaultMillis));
            }
            if (options.DefaultMonitoredItemWatchdogTimeout == null)
            {
                var watchdogInterval = GetIntOrNull(DefaultMonitoredItemWatchdogSecondsKey);
                if (watchdogInterval.HasValue)
                {
                    options.DefaultMonitoredItemWatchdogTimeout =
                        TimeSpan.FromSeconds(watchdogInterval.Value);
                }
            }
            if (options.DefaultMonitoredItemWatchdogCondition == null &&
                Enum.TryParse<MonitoredItemWatchdogCondition>(
                    GetStringOrDefault(DefaultMonitoredItemWatchdogConditionKey),
                    out var watchdogCondition))
            {
                options.DefaultMonitoredItemWatchdogCondition = watchdogCondition;
            }
            if (options.DefaultWatchdogBehavior == null &&
                Enum.TryParse<SubscriptionWatchdogBehavior>(
                    GetStringOrDefault(DefaultWatchdogBehaviorKey),
                    out var watchdogBehavior))
            {
                options.DefaultWatchdogBehavior = watchdogBehavior;
            }

            if (options.SubscriptionErrorRetryDelay == null)
            {
                var retryTimeout = GetIntOrNull(SubscriptionErrorRetryDelaySecondsKey);
                if (retryTimeout.HasValue)
                {
                    options.SubscriptionErrorRetryDelay =
                        TimeSpan.FromSeconds(retryTimeout.Value);
                }
            }

            if (options.BadMonitoredItemRetryDelayDuration == null)
            {
                var retryTimeout = GetIntOrNull(BadMonitoredItemRetryDelaySecondsKey);
                if (retryTimeout.HasValue)
                {
                    options.BadMonitoredItemRetryDelayDuration =
                        TimeSpan.FromSeconds(retryTimeout.Value);
                }
            }

            if (options.InvalidMonitoredItemRetryDelayDuration == null)
            {
                var retryTimeout = GetIntOrNull(InvalidMonitoredItemRetryDelaySecondsKey);
                if (retryTimeout.HasValue)
                {
                    options.InvalidMonitoredItemRetryDelayDuration =
                        TimeSpan.FromSeconds(retryTimeout.Value);
                }
            }

            if (options.SubscriptionManagementIntervalDuration == null)
            {
                var managementInterval = GetIntOrNull(SubscriptionManagementIntervalSecondsKey);
                if (managementInterval.HasValue)
                {
                    options.SubscriptionManagementIntervalDuration =
                        TimeSpan.FromSeconds(managementInterval.Value);
                }
            }

            options.DefaultKeepAliveCount ??= (uint?)GetIntOrNull(DefaultKeepAliveCountKey);
            options.DefaultLifeTimeCount ??= (uint?)GetIntOrNull(DefaultLifetimeCountKey);

            options.EnableImmediatePublishing ??= GetBoolOrNull(EnableImmediatePublishingKey);
            options.EnableSequentialPublishing ??= GetBoolOrDefault(EnableSequentialPublishingKey,
                    EnableSequentialPublishingDefault);
            options.ResolveDisplayName ??= GetBoolOrDefault(FetchOpcNodeDisplayNameKey,
                    ResolveDisplayNameDefault);
            options.DefaultQueueSize ??= (uint?)GetIntOrNull(DefaultQueueSizeKey);
            options.AutoSetQueueSizes ??= GetBoolOrNull(AutoSetQueueSizesKey);

            options.MaxMonitoredItemPerSubscription ??= (uint?)GetIntOrNull(MaxMonitoredItemPerSubscriptionKey);

            var unsMode = _options.Value.DefaultDataSetRouting ?? DataSetRoutingMode.None;
            options.FetchOpcBrowsePathFromRoot ??= unsMode != DataSetRoutingMode.None
                ? true : GetBoolOrNull(FetchOpcBrowsePathFromRootKey);

            if (options.DefaultDataChangeTrigger == null &&
                Enum.TryParse<DataChangeTriggerType>(GetStringOrDefault(DefaultDataChangeTriggerKey),
                    out var trigger))
            {
                options.DefaultDataChangeTrigger = trigger;
            }
        }

        /// <summary>
        /// Create configurator
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        public OpcUaSubscriptionConfig(IConfiguration configuration,
            IOptions<PublisherOptions> options) : base(configuration)
        {
            _options = options;
        }
        private readonly IOptions<PublisherOptions> _options;
    }
}
