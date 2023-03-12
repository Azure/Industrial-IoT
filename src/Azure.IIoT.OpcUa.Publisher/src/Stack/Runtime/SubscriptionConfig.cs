// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Models;
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
        public const string HeartbeatIntervalDefault = "DefaultHeartbeatInterval";
        public const string SkipFirstDefault = "DefaultSkipFirst";
        public const string DiscardNewDefault = "DiscardNew";
        public const string OpcSamplingInterval = "DefaultSamplingInterval";
        public const string OpcPublishingInterval = "DefaultPublishingInterval";
        public const string DisableKeyFrames = "DisableKeyFrames";
        public const string DefaultKeyFrameCount = "DefaultKeyFrameCount";
        public const string DisableDataSetMetaData = "DisableDataSetMetaData";
        public const string DefaultKeepAliveCount = "DefaultKeepAliveCount";
        public const string DefaultLifeTimeCount = "DefaultLifeTimeCount";
        public const string DefaultMetaDataUpdateTime = "DefaultMetaDataUpdateTime";
        public const string DefaultDataChangeTrigger = "DefaulDataChangeTrigger";
        public const string FetchOpcNodeDisplayName = "FetchOpcNodeDisplayName";
        public const string LegacyCompatibility = "LegacyCompatibility";
        public const string EnableMetricsKey = "EnableMetrics";
        public const string DefaultQueueSize = "DefaultQueueSize";

        /// <summary>
        /// Default values
        /// </summary>
        public const string DefaultPublishedNodesFilename = "publishednodes.json";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string name, SubscriptionOptions options)
        {
            if (options.DefaultHeartbeatInterval == null)
            {
                options.DefaultHeartbeatInterval = GetDurationOrDefault(
                    HeartbeatIntervalDefault, TimeSpan.Zero);
            }
            if (options.DefaultSkipFirst == null)
            {
                options.DefaultSkipFirst = GetBoolOrDefault(SkipFirstDefault, false);
            }
            if (options.DefaultDiscardNew == null)
            {
                options.DefaultDiscardNew = GetBoolOrDefault(DiscardNewDefault, false);
            }
            if (options.DefaultSamplingInterval == null)
            {
                options.DefaultSamplingInterval = GetDurationOrDefault(OpcSamplingInterval,
                    TimeSpan.FromSeconds(1));
            }
            if (options.DefaultPublishingInterval == null)
            {
                options.DefaultPublishingInterval = GetDurationOrDefault(OpcPublishingInterval,
                    TimeSpan.FromSeconds(1));
            }
            if (options.DefaultKeepAliveCount == null)
            {
                options.DefaultKeepAliveCount = (uint?)GetIntOrNull(DefaultKeepAliveCount);
            }
            if (options.DefaultLifeTimeCount == null)
            {
                options.DefaultLifeTimeCount = (uint?)GetIntOrNull(DefaultLifeTimeCount);
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
                options.ResolveDisplayName = GetBoolOrDefault(FetchOpcNodeDisplayName, false);
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
