﻿// ------------------------------------------------------------
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
    public sealed class OpcUaSubscriptionConfig : PostConfigureOptionBase<OpcUaSubscriptionOptions>
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
        public const string EnableDataSetKeepAlivesKey = "EnableDataSetKeepAlives";
        public const string DefaultKeyFrameCountKey = "DefaultKeyFrameCount";
        public const string DisableDataSetMetaDataKey = "DisableDataSetMetaData";
        public const string DefaultMetaDataUpdateTimeKey = "DefaultMetaDataUpdateTime";
        public const string DefaultDataChangeTriggerKey = "DefaulDataChangeTrigger";
        public const string FetchOpcNodeDisplayNameKey = "FetchOpcNodeDisplayName";
        public const string DefaultQueueSize = "DefaultQueueSize";
        public const string MinSubscriptionLifetimeKey = "MinSubscriptionLifetime";
        public const string MaxKeepAliveCountKey = "MaxKeepAliveCount";
        public const string UseDeferredAcknoledgementsKey = "UseDeferredAcknoledgements";
        public const string DefaultSamplingUsingCyclicReadKey = "DefaultSamplingUsingCyclicRead";
        public const string DefaultUseReverseConnectKey = "DefaultUseReverseConnect";

        /// <summary>
        /// Default values
        /// </summary>
        public const int MaxKeepAliveCountDefault = 10;
        public const bool ResolveDisplayNameDefault = false;
        public const int MinSubscriptionLifetimeDefaultSec = 10;
        public const int DefaultSamplingIntervalDefaultMillis = 1000;
        public const int DefaultPublishingIntervalDefaultMillis = 1000;
        public const bool DefaultSkipFirstDefault = false;
        public const bool DefaultSamplingUsingCyclicReadDefault = false;
        public const bool UseDeferredAcknoledgementsDefault = false;
        public const bool DefaultDiscardNewDefault = false;

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string? name, OpcUaSubscriptionOptions options)
        {
            if (options.UseDeferredAcknoledgements == null)
            {
                options.UseDeferredAcknoledgements = GetBoolOrDefault(
                    UseDeferredAcknoledgementsKey, UseDeferredAcknoledgementsDefault);
            }
            if (options.DefaultHeartbeatInterval == null)
            {
                options.DefaultHeartbeatInterval = GetDurationOrNull(
                    DefaultHeartbeatIntervalKey);
            }
            if (options.DefaultSamplingUsingCyclicRead == null)
            {
                options.DefaultSamplingUsingCyclicRead = GetBoolOrDefault(
                    DefaultSamplingUsingCyclicReadKey, DefaultSamplingUsingCyclicReadDefault);
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
                options.DisableDataSetMetaData = GetBoolOrDefault(DisableDataSetMetaDataKey);
            }
            if (options.DefaultMetaDataUpdateTime == null)
            {
                options.DefaultMetaDataUpdateTime = GetDurationOrNull(DefaultMetaDataUpdateTimeKey);
            }
            if (options.EnableDataSetKeepAlives == null)
            {
                options.EnableDataSetKeepAlives = GetBoolOrDefault(EnableDataSetKeepAlivesKey);
            }
            if (options.DefaultKeyFrameCount == null)
            {
                options.DefaultKeyFrameCount = (uint?)GetIntOrNull(DefaultKeyFrameCountKey);
            }
            if (options.ResolveDisplayName == null)
            {
                options.ResolveDisplayName = GetBoolOrDefault(FetchOpcNodeDisplayNameKey,
                    ResolveDisplayNameDefault);
            }
            if (options.DefaultQueueSize == null)
            {
                options.DefaultQueueSize = (uint?)GetIntOrNull(DefaultQueueSize);
            }

            if (options.DefaultDataChangeTrigger == null &&
                Enum.TryParse<DataChangeTriggerType>(GetStringOrDefault(DefaultDataChangeTriggerKey),
                    out var trigger))
            {
                options.DefaultDataChangeTrigger = trigger;
            }

            if (options.DefaultUseReverseConnect == null)
            {
                options.DefaultUseReverseConnect = GetBoolOrNull(DefaultUseReverseConnectKey);
            }
        }

        /// <summary>
        /// Create configurator
        /// </summary>
        /// <param name="configuration"></param>
        public OpcUaSubscriptionConfig(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
