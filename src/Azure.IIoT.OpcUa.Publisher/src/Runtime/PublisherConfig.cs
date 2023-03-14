// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Configuration;
    using Furly.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Publisher configuration
    /// </summary>
    public sealed class PublisherConfig : PostConfigureOptionBase<PublisherOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string PublisherIdKey = "PublisherId";
        public const string SiteIdKey = "SiteId";
        public const string PublishedNodesFileKey = "PublishedNodesFile";
        public const string MessagingModeKey = "MessagingMode";
        public const string MessageEncodingKey = "MessageEncoding";
        public const string FullFeaturedMessage = "FullFeaturedMessage";
        public const string UseStandardsCompliantEncodingKey = "UseStandardsCompliantEncoding";
        public const string MethodTopicTemplateKey = "MethodTopicTemplate";
        public const string RootTopicTemplateKey = "RootTopicTemplate";
        public const string TelemetryTopicTemplateKey = "TelemetryTopicTemplate";
        public const string DataSetMetaDataTopicTemplateKey = "DataSetMetaDataTopicTemplate";
        public const string DefaultMaxMessagesPerPublishKey = "DefaultMaxMessagesPerPublish";
        public const string DiagnosticsIntervalKey = "DiagnosticsInterval";
        public const string BatchSizeKey = "BatchSize";
        public const string BatchTriggerIntervalKey = "BatchTriggerInterval";
        public const string IoTHubMaxMessageSize = "IoTHubMaxMessageSize";
        public const string MaxEgressMessagesKey = "MaxOutgressMessages";
        public const string MaxNodesPerDataSetKey = "MaxNodesPerDataSet";
        public const string ScaleTestCountKey = "ScaleTestCount";
        public const string EnableRuntimeStateReportingKey = "RuntimeStateReporting";
        public const string RuntimeStateRoutingInfoKey = "RuntimeStateRoutingInfo";
        public const string EnableDataSetRoutingInfoKey = "EnableRoutingInfo";

        /// <summary>
        /// Variables in templates
        /// </summary>
        public const string PublisherIdVariableName = "PublisherId";
        public const string SiteIdVariableName = "SiteId";
        public const string RootTopicVariableName = "RootTopic";
        public const string DataSetWriterGroupVariableName = "DataSetWriterGroup";
        public const string DataSetWriterNameVariableName = "DataSetWriterName";
        public const string DataSetClassIdVariableName = "DataSetClassId";
        public const string TelemetryTopicVariableName = "TelemetryTopic";

        /// <summary>
        /// Default values
        /// </summary>
        public const string TelemetryTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/{{{DataSetWriterGroupVariableName}}}";
        public const string MethodTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/methods";
        public const string RootTopicTemplateDefault =
            $"{{{PublisherIdVariableName}}}";
        public const string PublishedNodesFileDefault = "publishednodes.json";
        public const string RuntimeStateRoutingInfoDefault = "runtimeinfo";
        public const bool EnableRuntimeStateReportingDefault = false;
        public const bool UseStandardsCompliantEncodingDefault = false;
        public const bool EnableDataSetRoutingInfoDefault = false;
        public const bool EnableDataSetWriterIdSubTopicDefault = false;
        public const MessageEncoding MessageEncodingDefault = MessageEncoding.Json;
        public const int MaxNodesPerDataSetDefault = 1000;
        public const int BatchSizeDefault = 100;
        public const int MaxEgressMessagesDefault = 4096;
        public const int BatchTriggerIntervalDefaultMillis = 1 * 1000;
        public const int DiagnosticsIntervalDefaultMillis = 60 * 1000;
        public const int ScaleTestCountDefault = 1;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string name, PublisherOptions options)
        {
            if (options.PublisherId == null)
            {
                options.PublisherId = GetStringOrDefault(PublisherIdKey,
                    _identity?.Id ?? Dns.GetHostName());
            }
            if (options.Site == null)
            {
                options.Site = GetStringOrDefault(SiteIdKey);
            }
            if (options.PublishedNodesFile == null)
            {
                options.PublishedNodesFile = GetStringOrDefault(PublishedNodesFileKey,
                    PublishedNodesFileDefault);
            }
            if (options.MaxNodesPerPublishedEndpoint == 0)
            {
                options.MaxNodesPerPublishedEndpoint = GetIntOrDefault(MaxNodesPerDataSetKey,
                    MaxNodesPerDataSetDefault);
            }
            if (options.BatchSize == null)
            {
                options.BatchSize = GetIntOrDefault(BatchSizeKey,
                    BatchSizeDefault);
            }
            if (options.BatchTriggerInterval == null)
            {
                options.BatchTriggerInterval = GetDurationOrNull(BatchTriggerIntervalKey) ??
                    TimeSpan.FromMilliseconds(GetIntOrDefault(BatchTriggerIntervalKey,
                        BatchTriggerIntervalDefaultMillis));
            }
            if (options.MaxEgressMessages == null)
            {
                options.MaxEgressMessages = GetIntOrDefault(MaxEgressMessagesKey,
                    MaxEgressMessagesDefault);
            }

            if (options.RootTopicTemplate == null)
            {
                options.RootTopicTemplate = GetStringOrDefault(
                    RootTopicTemplateKey, RootTopicTemplateDefault);
            }

            if (options.MethodTopicTemplate == null)
            {
                options.MethodTopicTemplate = GetStringOrDefault(
                    MethodTopicTemplateKey, MethodTopicTemplateDefault);
            }

            if (options.TelemetryTopicTemplate == null)
            {
                options.TelemetryTopicTemplate = GetStringOrDefault(
                    TelemetryTopicTemplateKey,
                        TelemetryTopicTemplateDefault);
            }

            if (options.DataSetMetaDataTopicTemplate == null)
            {
                options.DataSetMetaDataTopicTemplate = GetStringOrDefault(
                    DataSetMetaDataTopicTemplateKey);
            }

            if (options.EnableRuntimeStateReporting == null)
            {
                options.EnableRuntimeStateReporting = GetBoolOrDefault(
                    EnableRuntimeStateReportingKey, EnableRuntimeStateReportingDefault);
            }
            if (options.RuntimeStateRoutingInfo == null)
            {
                options.RuntimeStateRoutingInfo = GetStringOrDefault(
                    RuntimeStateRoutingInfoKey, RuntimeStateRoutingInfoDefault);
            }

            if (options.ScaleTestCount == null)
            {
                options.ScaleTestCount = GetIntOrDefault(ScaleTestCountKey,
                    ScaleTestCountDefault);
            }

            if (options.DiagnosticsInterval == null)
            {
                options.DiagnosticsInterval = GetDurationOrNull(DiagnosticsIntervalKey) ??
                   TimeSpan.FromMilliseconds(GetIntOrDefault(DiagnosticsIntervalKey,
                       DiagnosticsIntervalDefaultMillis));
            }

            if (options.EnableDataSetRoutingInfo == null)
            {
                options.EnableDataSetRoutingInfo = GetBoolOrDefault(
                    EnableDataSetRoutingInfoKey, EnableDataSetRoutingInfoDefault);
            }
            if (options.MaxMessageSize == null) // Max encoder message size
            {
                options.MaxMessageSize = GetIntOrNull(IoTHubMaxMessageSize);
            }
            if (options.UseStandardsCompliantEncoding == null)
            {
                options.UseStandardsCompliantEncoding = GetBoolOrDefault(
                    UseStandardsCompliantEncodingKey, UseStandardsCompliantEncodingDefault);
            }
            if (options.DefaultMaxMessagesPerPublish == null)
            {
                options.DefaultMaxMessagesPerPublish = (uint?)GetIntOrNull(
                    DefaultMaxMessagesPerPublishKey);
            }
            if (options.MessagingProfile == null)
            {
                if (!Enum.TryParse<MessagingMode>(GetStringOrDefault(MessagingModeKey),
                    out var messagingMode))
                {
                    messagingMode = options.UseStandardsCompliantEncoding.Value ?
                        MessagingMode.PubSub : MessagingMode.Samples;
                }

                if (GetBoolOrDefault(FullFeaturedMessage, false))
                {
                    if (messagingMode == MessagingMode.PubSub)
                    {
                        messagingMode = MessagingMode.FullNetworkMessages;
                    }
                    if (messagingMode == MessagingMode.Samples)
                    {
                        messagingMode = MessagingMode.FullSamples;
                    }
                }

                if (!Enum.TryParse<MessageEncoding>(GetStringOrDefault(MessageEncodingKey),
                    out var messageEncoding))
                {
                    messageEncoding = MessageEncodingDefault;
                }

                if (!MessagingProfile.IsSupported(messagingMode, messageEncoding))
                {
                    var supported = MessagingProfile.Supported
                        .Select(p => $"\n(--mm {p.MessagingMode} and --me {p.MessageEncoding})")
                        .Aggregate((a, b) => $"{a}, {b}");
                    throw new ConfigurationErrorsException(
                        "The specified combination of --mm, and --me is not (yet) supported." +
                        $" Currently supported combinations are: {supported}");
                }
                options.MessagingProfile = MessagingProfile.Get(messagingMode, messageEncoding);
            }
        }

        /// <summary>
        /// Create configurator
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="identity"></param>
        public PublisherConfig(IConfiguration configuration,
            IProcessIdentity identity = null)
            : base(configuration)
        {
            _identity = identity;
        }

        private readonly IProcessIdentity _identity;
    }
}
