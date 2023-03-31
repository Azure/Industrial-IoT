// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Subscription options configuration
    /// </summary>
    public sealed class SubscriptionConfig : PostConfigureOptionBase<SubscriptionOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string DefaultHeartbeatIntervalKey = "DefaultHeartbeatInterval";
        public const string DefaultSkipFirstKey = "DefaultSkipFirst";
        public const string DefaultDiscardNewKey = "DiscardNew";
        public const string DefaultSamplingIntervalKey = "DefaultSamplingInterval";
        public const string DefaultPublishingIntervalKey = "DefaultPublishingInterval";
        public const string DisableKeyFrames = "DisableKeyFrames";
        public const string DefaultKeyFrameCount = "DefaultKeyFrameCount";
        public const string DisableDataSetMetaData = "DisableDataSetMetaData";
        public const string DefaultMetaDataUpdateTime = "DefaultMetaDataUpdateTime";
        public const string DefaultDataChangeTrigger = "DefaulDataChangeTrigger";
        public const string FetchOpcNodeDisplayName = "FetchOpcNodeDisplayName";
        public const string DefaultQueueSize = "DefaultQueueSize";
        public const string MinSubscriptionLifetimeKey = "MinSubscriptionLifetime";
        public const string MaxKeepAliveCountKey = "MaxKeepAliveCount";

        /// <summary>
        /// Default values
        /// </summary>
        public const int MaxKeepAliveCountDefault = 10;
        public const bool ResolveDisplayNameDefault = false;
        public const int MinSubscriptionLifetimeDefaultSec = 10;
        public const int DefaultSamplingIntervalDefaultMillis = 1000;
        public const int DefaultPublishingIntervalDefaultMillis = 1000;
        public const bool DefaultSkipFirstDefault = false;
        public const bool DefaultDiscardNewDefault = false;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string? name, SubscriptionOptions options)
        {
            if (options.DefaultHeartbeatInterval == null)
            {
                options.DefaultHeartbeatInterval = GetDurationOrNull(
                    DefaultHeartbeatIntervalKey);
            }
            if (options.DefaultSkipFirst == null)
            {
                options.DefaultSkipFirst = GetBoolOrDefault(DefaultSkipFirstKey,
                    DefaultSkipFirstDefault);
            }
            if (options.DefaultDiscardNew == null)
            {
                options.DefaultDiscardNew = GetBoolOrDefault(DefaultDiscardNewKey,
                    DefaultDiscardNewDefault);
            }
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
            if (options.DefaultKeepAliveCount == null)
            {
                options.DefaultKeepAliveCount = (uint)GetIntOrDefault(MaxKeepAliveCountKey,
                    MaxKeepAliveCountDefault);
            }
            if (options.DefaultLifeTimeCount == null)
            {
                options.DefaultLifeTimeCount = (uint)GetIntOrDefault(MinSubscriptionLifetimeKey,
                    MinSubscriptionLifetimeDefaultSec) * 1000;
            }
            if (options.DisableDataSetMetaData == null)
            {
                options.DisableDataSetMetaData = GetBoolOrDefault(DisableDataSetMetaData);
            }
            if (options.DefaultMetaDataUpdateTime == null)
            {
                options.DefaultMetaDataUpdateTime = GetDurationOrNull(DefaultMetaDataUpdateTime);
            }
            if (options.DisableKeyFrames == null)
            {
                options.DisableKeyFrames = GetBoolOrDefault(DisableKeyFrames);
            }
            if (options.DefaultKeyFrameCount == null)
            {
                options.DefaultKeyFrameCount = (uint?)GetIntOrNull(DefaultKeyFrameCount);
            }
            if (options.ResolveDisplayName == null)
            {
                options.ResolveDisplayName = GetBoolOrDefault(FetchOpcNodeDisplayName,
                    ResolveDisplayNameDefault);
            }
            if (options.DefaultQueueSize == null)
            {
                options.DefaultQueueSize = (uint?)GetIntOrNull(DefaultQueueSize);
            }

            if (options.DefaultDataChangeTrigger == null &&
                Enum.TryParse<DataChangeTriggerType>(GetStringOrDefault(DefaultDataChangeTrigger),
                    out var trigger))
            {
                options.DefaultDataChangeTrigger = trigger;
            }
        }

        /// <summary>
        /// Create configurator
        /// </summary>
        /// <param name="configuration"></param>
        public SubscriptionConfig(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
