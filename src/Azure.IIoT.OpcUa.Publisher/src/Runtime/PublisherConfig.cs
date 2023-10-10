// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Configuration;
    using Furly.Extensions.Hosting;
    using Furly.Extensions.Messaging;
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
        public const string PublisherIdKey = "PublisherId";
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string SiteIdKey = "SiteId";
        public const string PublishedNodesFileKey = "PublishedNodesFile";
        public const string CreatePublishFileIfNotExistKey = "CreatePublishFileIfNotExistKey";
        public const string MessagingModeKey = "MessagingMode";
        public const string MessageEncodingKey = "MessageEncoding";
        public const string FullFeaturedMessage = "FullFeaturedMessage";
        public const string UseStandardsCompliantEncodingKey = "UseStandardsCompliantEncoding";
        public const string MethodTopicTemplateKey = "MethodTopicTemplate";
        public const string RootTopicTemplateKey = "RootTopicTemplate";
        public const string TelemetryTopicTemplateKey = "TelemetryTopicTemplate";
        public const string EventsTopicTemplateKey = "EventsTopicTemplate";
        public const string DataSetMetaDataTopicTemplateKey = "DataSetMetaDataTopicTemplate";
        public const string DefaultMaxMessagesPerPublishKey = "DefaultMaxMessagesPerPublish";
        public const string MaxNetworkMessageSendQueueSizeKey = "MaxNetworkMessageSendQueueSize";
        public const string DiagnosticsIntervalKey = "DiagnosticsInterval";
        public const string BatchSizeKey = "BatchSize";
        public const string BatchTriggerIntervalKey = "BatchTriggerInterval";
        public const string IoTHubMaxMessageSizeKey = "IoTHubMaxMessageSize";
        public const string DebugLogNotificationsKey = "DebugLogNotifications";
        public const string MaxNodesPerDataSetKey = "MaxNodesPerDataSet";
        public const string ScaleTestCountKey = "ScaleTestCount";
        public const string DisableOpenApiEndpointKey = "DisableOpenApiEndpoint";
        public const string DefaultNamespaceFormatKey = "DefaultNamespaceFormat";
        public const string MessageTimestampKey = "MessageTimestamp";
        public const string EnableRuntimeStateReportingKey = "RuntimeStateReporting";
        public const string RuntimeStateRoutingInfoKey = "RuntimeStateRoutingInfo";
        public const string EnableDataSetRoutingInfoKey = "EnableRoutingInfo";
        public const string ForceCredentialEncryptionKey = "ForceCredentialEncryption";
        public const string DefaultTransportKey = "DefaultTransport";
        public const string DefaultQualityOfServiceKey = "DefaultQualityOfService";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Variables in templates
        /// </summary>
        public const string PublisherIdVariableName = "PublisherId";
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string RootTopicVariableName = "RootTopic";
        public const string DataSetWriterGroupVariableName = "DataSetWriterGroup";
        public const string DataSetWriterNameVariableName = "DataSetWriterName";
        public const string DataSetClassIdVariableName = "DataSetClassId";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Default values
        /// </summary>
        public const string TelemetryTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/messages/{{{DataSetWriterGroupVariableName}}}";
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string MethodTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/methods";
        public const string EventsTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/events";
        public const string RootTopicTemplateDefault =
            $"{{{PublisherIdVariableName}}}";
        public const string PublishedNodesFileDefault = "publishednodes.json";
        public const string RuntimeStateRoutingInfoDefault = "runtimeinfo";
        public const bool EnableRuntimeStateReportingDefault = false;
        public const bool UseStandardsCompliantEncodingDefault = false;
        public const bool EnableDataSetRoutingInfoDefault = false;
        public const MessageEncoding MessageEncodingDefault = MessageEncoding.Json;
        public const int MaxNodesPerDataSetDefault = 1000;
        public const int BatchSizeLegacyDefault = 50;
        public const int MaxNetworkMessageSendQueueSizeDefault = 4096;
        public const int BatchTriggerIntervalLLegacyDefaultMillis = 10 * 1000;
        public const int DiagnosticsIntervalDefaultMillis = 60 * 1000;
        public const int ScaleTestCountDefault = 1;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string? name, PublisherOptions options)
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
                options.PublishedNodesFile = GetStringOrDefault(PublishedNodesFileKey);
            }

            if (options.DefaultTransport == null && Enum.TryParse<WriterGroupTransport>(
                GetStringOrDefault(DefaultTransportKey), out var transport))
            {
                options.DefaultTransport = transport;
            }

            if (options.UseStandardsCompliantEncoding == null)
            {
                options.UseStandardsCompliantEncoding = GetBoolOrDefault(
                    UseStandardsCompliantEncodingKey, UseStandardsCompliantEncodingDefault);
            }

            if (options.CreatePublishFileIfNotExist == null)
            {
                options.CreatePublishFileIfNotExist = GetBoolOrNull(
                    CreatePublishFileIfNotExistKey);
            }

            if (options.MaxNodesPerDataSet == 0)
            {
                options.MaxNodesPerDataSet = GetIntOrDefault(MaxNodesPerDataSetKey,
                    MaxNodesPerDataSetDefault);
            }

            if (options.BatchSize == null)
            {
                //
                // Default to batch size of 50 if not using strict encoding and a
                // transport was not specified to support backcompat with 2.8
                //
                options.BatchSize = GetIntOrDefault(BatchSizeKey,
                        options.UseStandardsCompliantEncoding == true ||
                        options.DefaultTransport != null ? 0 : BatchSizeLegacyDefault);
            }

            if (options.BatchTriggerInterval == null)
            {
                //
                // Default to batch interval of 10 seconds if not using strict encoding
                // and a transport was not specified to support backcompat with 2.8
                //
                options.BatchTriggerInterval = GetDurationOrNull(BatchTriggerIntervalKey) ??
                    TimeSpan.FromMilliseconds(GetIntOrDefault(BatchTriggerIntervalKey,
                        options.UseStandardsCompliantEncoding == true ||
                        options.DefaultTransport != null ? 0 : BatchTriggerIntervalLLegacyDefaultMillis));
            }

            if (options.MaxNetworkMessageSendQueueSize == null)
            {
                options.MaxNetworkMessageSendQueueSize = GetIntOrDefault(MaxNetworkMessageSendQueueSizeKey,
                    MaxNetworkMessageSendQueueSizeDefault);
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

            if (options.EventsTopicTemplate == null)
            {
                options.EventsTopicTemplate = GetStringOrDefault(
                    EventsTopicTemplateKey, EventsTopicTemplateDefault);
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

            if (options.DisableOpenApiEndpoint == null)
            {
                options.DisableOpenApiEndpoint = GetBoolOrNull(DisableOpenApiEndpointKey);
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

            if (options.DebugLogNotifications == null)
            {
                options.DebugLogNotifications = GetBoolOrDefault(DebugLogNotificationsKey);
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

            if (options.ForceCredentialEncryption == null)
            {
                options.ForceCredentialEncryption = GetBoolOrDefault(
                    ForceCredentialEncryptionKey);
            }

            if (options.MaxNetworkMessageSize == null) // Max encoder message size
            {
                options.MaxNetworkMessageSize = GetIntOrNull(IoTHubMaxMessageSizeKey);
            }

            if (options.DefaultMaxDataSetMessagesPerPublish == null)
            {
                options.DefaultMaxDataSetMessagesPerPublish = (uint?)GetIntOrNull(
                    DefaultMaxMessagesPerPublishKey);
            }

            if (options.DefaultQualityOfService == null)
            {
                if (!Enum.TryParse<QoS>(GetStringOrDefault(DefaultQualityOfServiceKey),
                    out var qos))
                {
                    qos = QoS.AtLeastOnce;
                }
                options.DefaultQualityOfService = qos;
            }

            if (options.MessageTimestamp == null)
            {
                if (!Enum.TryParse<MessageTimestamp>(GetStringOrDefault(MessageTimestampKey),
                    out var messageTimestamp))
                {
                    messageTimestamp = MessageTimestamp.CurrentTimeUtc;
                }
                options.MessageTimestamp = messageTimestamp;
            }

            if (options.DefaultNamespaceFormat == null)
            {
                if (!Enum.TryParse<NamespaceFormat>(GetStringOrDefault(DefaultNamespaceFormatKey),
                    out var namespaceFormat))
                {
                    namespaceFormat = options.UseStandardsCompliantEncoding == true ?
                        NamespaceFormat.Expanded : NamespaceFormat.Uri;
                }
                options.DefaultNamespaceFormat = namespaceFormat;
            }

            if (options.MessagingProfile == null)
            {
                if (!Enum.TryParse<MessagingMode>(GetStringOrDefault(MessagingModeKey),
                    out var messagingMode))
                {
                    messagingMode = options.UseStandardsCompliantEncoding == true ?
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
        public PublisherConfig(IConfiguration configuration, IProcessIdentity? identity = null)
            : base(configuration)
        {
            _identity = identity;
        }

        private readonly IProcessIdentity? _identity;
    }
}
