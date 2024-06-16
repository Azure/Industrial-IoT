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
        public const string EnableDataSetKeepAlivesKey = "EnableDataSetKeepAlives";
        public const string DefaultKeyFrameCountKey = "DefaultKeyFrameCount";
        public const string DisableDataSetMetaDataKey = "DisableDataSetMetaData";
        public const string DefaultMetaDataUpdateTimeKey = "DefaultMetaDataUpdateTime";
        public const string DefaultDataChangeTriggerKey = "DefaulDataChangeTrigger";
        public const string FetchOpcNodeDisplayNameKey = "FetchOpcNodeDisplayName";
        public const string FetchOpcBrowsePathFromRootKey = "FetchOpcBrowsePathFromRoot";
        public const string DefaultQueueSize = "DefaultQueueSize";
        public const string DefaultLifetimeCountKey = "DefaultLifetimeCount";
        public const string DefaultKeepAliveCountKey = "DefaultKeepAliveCount";
        public const string UseDeferredAcknoledgementsKey = "UseDeferredAcknoledgements";
        public const string DefaultSamplingUsingCyclicReadKey = "DefaultSamplingUsingCyclicRead";
        public const string DefaultUseReverseConnectKey = "DefaultUseReverseConnect";
        public const string AsyncMetaDataLoadThresholdKey = "AsyncMetaDataLoadThreshold";
        public const string EnableImmediatePublishingKey = "EnableImmediatePublishing";
        public const string DisableSessionPerWriterGroupKey = "DisableSessionPerWriterGroup";
        public const string EnableSequentialPublishingKey = "EnableSequentialPublishing";
        public const string DefaultRebrowsePeriodKey = "DefaultRebrowsePeriod";
        public const string DisableComplexTypeSystemKey = "DisableComplexTypeSystem";
        public const string DisableSubscriptionTransferKey = "DisableSubscriptionTransfer";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Default values
        /// </summary>
        public const int DefaultKeepAliveCountDefault = 10;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const bool ResolveDisplayNameDefault = false;
        public const int DefaultLifetimeCountDefault = 100;
        public const int DefaultSamplingIntervalDefaultMillis = 1000;
        public const int DefaultPublishingIntervalDefaultMillis = 1000;
        public const int AsyncMetaDataLoadThresholdDefault = 30;
        public const bool DefaultSkipFirstDefault = false;
        public const bool DefaultRepublishAfterTransferDefault = false;
        public const bool UseDeferredAcknoledgementsDefault = false;
        public const bool DefaultDiscardNewDefault = false;
        public const bool DisableSessionPerWriterGroupDefault = false;
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
            options.DefaultKeepAliveCount ??= (uint)GetIntOrDefault(DefaultKeepAliveCountKey,
                    DefaultKeepAliveCountDefault);
            options.DefaultLifeTimeCount ??= (uint)GetIntOrDefault(DefaultLifetimeCountKey,
                    DefaultLifetimeCountDefault);

            options.DisableComplexTypeSystem ??= GetBoolOrNull(DisableComplexTypeSystemKey);
            options.DisableDataSetMetaData = options.DisableComplexTypeSystem;
            // Set a default from the strict setting
            options.DisableDataSetMetaData ??= GetBoolOrDefault(DisableDataSetMetaDataKey,
                !(_options.Value.UseStandardsCompliantEncoding ?? false));
            if (_options.Value.SchemaOptions != null)
            {
                // Always turn on metadata for schema publishing
                options.DisableComplexTypeSystem = false;
                options.DisableDataSetMetaData = false;
            }
            options.AsyncMetaDataLoadThreshold ??= GetIntOrDefault(
                    AsyncMetaDataLoadThresholdKey, AsyncMetaDataLoadThresholdDefault);
            if (options.DefaultMetaDataUpdateTime == null && options.DisableDataSetMetaData != true)
            {
                options.DefaultMetaDataUpdateTime = GetDurationOrNull(DefaultMetaDataUpdateTimeKey);
            }
            options.EnableImmediatePublishing ??= GetBoolOrNull(EnableImmediatePublishingKey);
            options.EnableSequentialPublishing ??= GetBoolOrNull(EnableSequentialPublishingKey);
            options.DisableSessionPerWriterGroup ??= GetBoolOrDefault(DisableSessionPerWriterGroupKey,
                    DisableSessionPerWriterGroupDefault);
            options.EnableDataSetKeepAlives ??= GetBoolOrDefault(EnableDataSetKeepAlivesKey);
            options.DefaultKeyFrameCount ??= (uint?)GetIntOrNull(DefaultKeyFrameCountKey);
            options.ResolveDisplayName ??= GetBoolOrDefault(FetchOpcNodeDisplayNameKey,
                    ResolveDisplayNameDefault);
            options.DefaultQueueSize ??= (uint?)GetIntOrNull(DefaultQueueSize);

            var unsMode = _options.Value.DefaultDataSetRouting ?? DataSetRoutingMode.None;
            options.FetchOpcBrowsePathFromRoot ??= unsMode != DataSetRoutingMode.None
                ? true : GetBoolOrNull(FetchOpcBrowsePathFromRootKey);

            if (options.DefaultDataChangeTrigger == null &&
                Enum.TryParse<DataChangeTriggerType>(GetStringOrDefault(DefaultDataChangeTriggerKey),
                    out var trigger))
            {
                options.DefaultDataChangeTrigger = trigger;
            }

            options.DefaultUseReverseConnect ??= GetBoolOrNull(DefaultUseReverseConnectKey);
            options.DisableSubscriptionTransfer ??= GetBoolOrNull(DisableSubscriptionTransferKey);
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
