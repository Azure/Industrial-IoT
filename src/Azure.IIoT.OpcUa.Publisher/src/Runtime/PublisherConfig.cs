// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Configuration;
    using System.Linq;

    /// <summary>
    /// Publisher configuration
    /// </summary>
    public sealed class PublisherConfig : PostConfigureOptionBase<PublisherOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string PublisherSite = "site";
        public const string PublishedNodesFile = "PublishedNodesFile";
        public const string MessagingModeKey = "MessagingMode";
        public const string MessageEncodingKey = "MessageEncoding";
        public const string FullFeaturedMessage = "FullFeaturedMessage";
        public const string UseStandardsCompliantEncoding = "UseStandardsCompliantEncoding";
        public const string DefaultDataSetMetaDataQueueName = "DefaultDataSetMetaDataQueueName";
        public const string DefaultMaxMessagesPerPublish = "DefaultMaxMessagesPerPublish";
        public const string DiagnosticsInterval = "DiagnosticsInterval";
        public const string BatchSize = "BatchSize";
        public const string BatchTriggerInterval = "BatchTriggerInterval";
        public const string IoTHubMaxMessageSize = "IoTHubMaxMessageSize";
        public const string MaxOutgressMessages = "MaxOutgressMessages";
        public const string MaxNodesPerPublishedEndpoint = "MaxNodesPerDataSet";
        public const string ScaleTestCount = "ScaleTestCount";
        public const string RuntimeStateReporting = "RuntimeStateReporting";
        public const string RuntimeStateRoutingInfo = "RuntimeStateRoutingInfo";

        /// <summary>
        /// Default values
        /// </summary>
        public const string DefaultPublishedNodesFilename = "publishednodes.json";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string name, PublisherOptions options)
        {
            if (options.Site == null)
            {
                options.Site = GetStringOrDefault(PublisherSite);
            }
            if (options.PublishedNodesFile == null)
            {
                options.PublishedNodesFile = GetStringOrDefault(
                    PublishedNodesFile, DefaultPublishedNodesFilename);
            }
            if (options.MaxNodesPerPublishedEndpoint == 0)
            {
                options.MaxNodesPerPublishedEndpoint = GetIntOrDefault(
                    MaxNodesPerPublishedEndpoint, 1000);
            }

            if (options.BatchSize == null)
            {
                options.BatchSize = GetIntOrDefault(BatchSize, 100);
            }
            if (options.BatchTriggerInterval == null)
            {
                options.BatchTriggerInterval = GetDurationOrDefault(
                    BatchTriggerInterval, TimeSpan.FromSeconds(1));
            }
            if (options.MaxOutgressMessages == null)
            {
                options.MaxOutgressMessages = GetIntOrDefault(
                    MaxOutgressMessages, 4096);
            }

            if (options.DefaultMetaDataQueueName == null)
            {
                options.DefaultMetaDataQueueName = GetStringOrDefault(
                    DefaultDataSetMetaDataQueueName);
            }

            if (options.EnableRuntimeStateReporting == null)
            {
                options.EnableRuntimeStateReporting = GetBoolOrDefault(
                    RuntimeStateReporting, true);
            }

            if (options.RuntimeStateRoutingInfo == null)
            {
                options.RuntimeStateRoutingInfo = GetStringOrDefault(
                    RuntimeStateRoutingInfo, "runtimeinfo");
            }
            if (options.ScaleTestCount == null)
            {
                options.ScaleTestCount = GetIntOrDefault(ScaleTestCount, 1);
            }
            if (options.DiagnosticsInterval == null)
            {
                options.DiagnosticsInterval = GetDurationOrDefault(
                    DiagnosticsInterval, TimeSpan.FromSeconds(60));
            }
            if (options.MaxMessageSize == null) // Max encoder message size
            {
                options.MaxMessageSize = GetIntOrNull(IoTHubMaxMessageSize);
            }
            if (options.UseStandardsCompliantEncoding == null)
            {
                options.UseStandardsCompliantEncoding = GetBoolOrDefault(
                    UseStandardsCompliantEncoding, false);
            }
            if (options.DefaultMaxMessagesPerPublish == null)
            {
                options.DefaultMaxMessagesPerPublish = (uint?)GetIntOrNull(
                    DefaultMaxMessagesPerPublish);
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
                    messageEncoding = MessageEncoding.Json;
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
        public PublisherConfig(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
