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
        public const string DefaultDiscardNewKey = "DiscardNew";
        public const string DefaultSamplingIntervalKey = "DefaultSamplingInterval";
        public const string DefaultPublishingIntervalKey = "DefaultPublishingInterval";
        public const string DefaultDataChangeTriggerKey = "DefaulDataChangeTrigger";
        public const string DefaultQueueSize = "DefaultQueueSize";
        public const string DefaultLifetimeCountKey = "DefaultLifetimeCount";
        public const string DefaultKeepAliveCountKey = "DefaultKeepAliveCount";
        public const string UseDeferredAcknoledgementsKey = "UseDeferredAcknoledgements";
        public const string DefaultSamplingUsingCyclicReadKey = "DefaultSamplingUsingCyclicRead";
        public const string EnableImmediatePublishingKey = "EnableImmediatePublishing";
        public const string EnableSequentialPublishingKey = "EnableSequentialPublishing";
        public const string DefaultRebrowsePeriodKey = "DefaultRebrowsePeriod";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Default values
        /// </summary>
        public const int DefaultKeepAliveCountDefault = 10;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const int DefaultLifetimeCountDefault = 100;
        public const int DefaultSamplingIntervalDefaultMillis = 1000;
        public const int DefaultPublishingIntervalDefaultMillis = 1000;
        public const bool DefaultSkipFirstDefault = false;
        public const bool UseDeferredAcknoledgementsDefault = false;
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
            options.DefaultKeepAliveCount ??= (uint)GetIntOrDefault(DefaultKeepAliveCountKey,
                    DefaultKeepAliveCountDefault);
            options.DefaultLifeTimeCount ??= (uint)GetIntOrDefault(DefaultLifetimeCountKey,
                    DefaultLifetimeCountDefault);
            // Set a default from the strict setting

            options.EnableImmediatePublishing ??= GetBoolOrNull(EnableImmediatePublishingKey);
            options.EnableSequentialPublishing ??= GetBoolOrNull(EnableSequentialPublishingKey);
            options.DefaultQueueSize ??= (uint?)GetIntOrNull(DefaultQueueSize);

            var unsMode = _options.Value.DefaultDataSetRouting ?? DataSetRoutingMode.None;

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
