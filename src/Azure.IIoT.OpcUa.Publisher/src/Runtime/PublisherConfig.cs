// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Furly.Extensions.Configuration;
    using Furly.Extensions.Hosting;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;

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
        public const string UseFileChangePollingKey = "UseFileChangePolling";
        public const string CreatePublishFileIfNotExistKey = "CreatePublishFileIfNotExistKey";
        public const string MessagingModeKey = "MessagingMode";
        public const string MessageEncodingKey = "MessageEncoding";
        public const string FullFeaturedMessageKey = "FullFeaturedMessage";
        public const string UseStandardsCompliantEncodingKey = "UseStandardsCompliantEncoding";
        public const string MethodTopicTemplateKey = "MethodTopicTemplate";
        public const string RootTopicTemplateKey = "RootTopicTemplate";
        public const string TelemetryTopicTemplateKey = "TelemetryTopicTemplate";
        public const string EventsTopicTemplateKey = "EventsTopicTemplate";
        public const string DiagnosticsTopicTemplateKey = "DiagnosticsTopicTemplate";
        public const string DataSetMetaDataTopicTemplateKey = "DataSetMetaDataTopicTemplate";
        public const string SchemaTopicTemplateKey = "SchemaTopicTemplate";
        public const string DefaultWriterGroupPartitionCountKey = "DefaultWriterGroupPartitionCount";
        public const string DefaultMaxMessagesPerPublishKey = "DefaultMaxMessagesPerPublish";
        public const string MaxNetworkMessageSendQueueSizeKey = "MaxNetworkMessageSendQueueSize";
        public const string DiagnosticsIntervalKey = "DiagnosticsInterval";
        public const string DiagnosticsTargetKey = "DiagnosticsTarget";
        public const string BatchSizeKey = "BatchSize";
        public const string BatchTriggerIntervalKey = "BatchTriggerInterval";
        public const string RemoveDuplicatesFromBatchKey = "RemoveDuplicatesFromBatch";
        public const string WriteValueWhenDataSetHasSingleEntryKey = "WriteValueWhenDataSetHasSingleEntry";
        public const string IoTHubMaxMessageSizeKey = "IoTHubMaxMessageSize";
        public const string DebugLogNotificationsKey = "DebugLogNotifications";
        public const string DebugLogEncodedNotificationsKey = "DebugLogEncodedNotifications";
        public const string DebugLogNotificationsFilterKey = "DebugLogNotificationsFilter";
        public const string DebugLogNotificationsWithHeartbeatKey = "DebugLogNotificationsWithHeartbeat";
        public const string MaxNodesPerDataSetKey = "MaxNodesPerDataSet";
        public const string DisableDataSetMetaDataKey = "DisableDataSetMetaData";
        public const string EnableDataSetKeepAlivesKey = "EnableDataSetKeepAlives";
        public const string DefaultKeyFrameCountKey = "DefaultKeyFrameCount";
        public const string DisableComplexTypeSystemKey = "DisableComplexTypeSystem";
        public const string DisableSessionPerWriterGroupKey = "DisableSessionPerWriterGroup";
        public const string DefaultUseReverseConnectKey = "DefaultUseReverseConnect";
        public const string DisableSubscriptionTransferKey = "DisableSubscriptionTransfer";
        public const string DefaultMetaDataUpdateTimeKey = "DefaultMetaDataUpdateTime";
        public const string ScaleTestCountKey = "ScaleTestCount";
        public const string IgnoreConfiguredPublishingIntervalsKey = "IgnoreConfiguredPublishingIntervals";
        public const string DisableOpenApiEndpointKey = "DisableOpenApiEndpoint";
        public const string DefaultNamespaceFormatKey = "DefaultNamespaceFormat";
        public const string MessageTimestampKey = "MessageTimestamp";
        public const string EnableRuntimeStateReportingKey = "RuntimeStateReporting";
        public const string RuntimeStateRoutingInfoKey = "RuntimeStateRoutingInfo";
        public const string EnableDataSetRoutingInfoKey = "EnableRoutingInfo";
        public const string ForceCredentialEncryptionKey = "ForceCredentialEncryption";
        public const string RenewTlsCertificateOnStartupKey = "RenewTlsCertificateOnStartup";
        public const string DefaultTransportKey = "DefaultTransport";
        public const string DefaultQualityOfServiceKey = "DefaultQualityOfService";
        public const string DefaultMessageTimeToLiveKey = "DefaultMessageTimeToLive";
        public const string DefaultMessageRetentionKey = "DefaultMessageRetention";
        public const string DefaultDataSetRoutingKey = "DefaultDataSetRouting";
        public const string ApiKeyOverrideKey = "ApiKey";
        public const string PublishMessageSchemaKey = "PublishMessageSchema";
        public const string AsyncMetaDataLoadTimeoutKey = "AsyncMetaDataLoadTimeout";
        public const string PreferAvroOverJsonSchemaKey = "PreferAvroOverJsonSchema";
        public const string SchemaNamespaceKey = "SchemaNamespace";
        public const string DisableResourceMonitoringKey = "DisableResourceMonitoring";
        public const string HttpServerPortKey = "HttpServerPort";
        public const string UnsecureHttpServerPortKey = "UnsecureHttpServerPort";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Variables in templates
        /// </summary>
        public const string PublisherIdVariableName = "PublisherId";
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string RootTopicVariableName = "RootTopic";
        public const string TelemetryTopicVariableName = "TelemetryTopic";
        public const string DataSetWriterGroupVariableName = "DataSetWriterGroup";
        public const string WriterGroupVariableName = "WriterGroup";
        public const string WriterGroupIdVariableName = "WriterGroupId";
        public const string DataSetWriterNameVariableName = "DataSetWriterName";
        public const string DataSetWriterVariableName = "DataSetWriter";
        public const string DataSetWriterIdVariableName = "DataSetWriterId";
        public const string DataSetFieldIdVariableName = "DataSetFieldId";
        public const string DataSetClassIdVariableName = "DataSetClassId";
        public const string EncodingVariableName = "Encoding";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Default values
        /// </summary>
        public const string TelemetryTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/messages/{{{WriterGroupVariableName}}}";
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string MethodTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/methods";
        public const string EventsTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/events";
        public const string MetadataTopicTemplateDefault =
            $"{{{TelemetryTopicVariableName}}}/metadata";
        public const string DiagnosticsTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/diagnostics/{{{WriterGroupVariableName}}}";
        public const string RootTopicTemplateDefault =
            $"{{{PublisherIdVariableName}}}";
        public const string SchemaTopicTemplateDefault =
            $"{{{TelemetryTopicVariableName}}}/schema";
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
        public const int AsyncMetaDataLoadTimeoutDefaultMillis = 5 * 1000;
        public const int DiagnosticsIntervalDefaultMillis = 60 * 1000;
        public const int AsyncMetaDataLoadThresholdDefault = 30;
        public const int ScaleTestCountDefault = 1;
        public const bool IgnoreConfiguredPublishingIntervalsDefault = false;
        public const bool DisableSessionPerWriterGroupDefault = false;
        public static readonly int UnsecureHttpServerPortDefault = IsContainer ? 80 : 9071;
        public static readonly int HttpServerPortDefault = IsContainer ? 443 : 9072;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void PostConfigure(string? name, PublisherOptions options)
        {
            options.DisableResourceMonitoring ??= GetBoolOrNull(DisableResourceMonitoringKey);
            options.PublisherId ??= GetStringOrDefault(PublisherIdKey,
                    _identity?.Identity ?? Dns.GetHostName());

            options.SiteId ??= GetStringOrDefault(SiteIdKey);

            options.PublishedNodesFile ??= GetStringOrDefault(PublishedNodesFileKey);
            options.UseFileChangePolling ??= GetBoolOrNull(UseFileChangePollingKey);

            if (options.DefaultTransport == null && Enum.TryParse<WriterGroupTransport>(
                GetStringOrDefault(DefaultTransportKey), out var transport))
            {
                options.DefaultTransport = transport;
            }

            options.UseStandardsCompliantEncoding ??= GetBoolOrDefault(
                    UseStandardsCompliantEncodingKey, UseStandardsCompliantEncodingDefault);

            if (options.MessagingProfile == null)
            {
                if (!Enum.TryParse<MessagingMode>(GetStringOrDefault(MessagingModeKey),
                    out var messagingMode))
                {
                    messagingMode = options.UseStandardsCompliantEncoding == true ?
                        MessagingMode.PubSub : MessagingMode.Samples;
                }

                if (GetBoolOrDefault(FullFeaturedMessageKey, false))
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

            options.CreatePublishFileIfNotExist ??= GetBoolOrNull(
                    CreatePublishFileIfNotExistKey);

            options.RenewTlsCertificateOnStartup ??= GetBoolOrNull(
                    RenewTlsCertificateOnStartupKey);

            if (options.MaxNodesPerDataSet == 0)
            {
                options.MaxNodesPerDataSet = GetIntOrDefault(MaxNodesPerDataSetKey,
                    MaxNodesPerDataSetDefault);
            }

            //
            // Default to batch size of 50 if not using strict encoding and a
            // transport was not specified to support backcompat with 2.8
            //
            options.BatchSize ??= GetIntOrDefault(BatchSizeKey,
                    options.UseStandardsCompliantEncoding == true ||
                    options.DefaultTransport != null ? 0 : BatchSizeLegacyDefault);

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

            options.WriteValueWhenDataSetHasSingleEntry
                ??= GetBoolOrNull(WriteValueWhenDataSetHasSingleEntryKey);
            options.RemoveDuplicatesFromBatch ??= GetBoolOrNull(RemoveDuplicatesFromBatchKey);

            options.MaxNetworkMessageSendQueueSize ??= GetIntOrDefault(MaxNetworkMessageSendQueueSizeKey,
                    MaxNetworkMessageSendQueueSizeDefault);

            options.DefaultWriterGroupPartitions ??= GetIntOrNull(DefaultWriterGroupPartitionCountKey);
            options.IgnoreConfiguredPublishingIntervals ??= GetBoolOrDefault(IgnoreConfiguredPublishingIntervalsKey,
                    IgnoreConfiguredPublishingIntervalsDefault);

            if (options.TopicTemplates.Root == null)
            {
                options.TopicTemplates.Root = GetStringOrDefault(
                    RootTopicTemplateKey, RootTopicTemplateDefault);
            }

            if (options.TopicTemplates.Method == null)
            {
                options.TopicTemplates.Method = GetStringOrDefault(
                    MethodTopicTemplateKey, MethodTopicTemplateDefault);
            }

            if (options.TopicTemplates.Events == null)
            {
                options.TopicTemplates.Events = GetStringOrDefault(
                    EventsTopicTemplateKey, EventsTopicTemplateDefault);
            }

            if (options.TopicTemplates.Diagnostics == null)
            {
                options.TopicTemplates.Diagnostics = GetStringOrDefault(
                    DiagnosticsTopicTemplateKey, DiagnosticsTopicTemplateDefault);
            }

            if (options.TopicTemplates.Telemetry == null)
            {
                options.TopicTemplates.Telemetry = GetStringOrDefault(
                    TelemetryTopicTemplateKey,
                        TelemetryTopicTemplateDefault);
            }

            if (options.TopicTemplates.DataSetMetaData == null)
            {
                options.TopicTemplates.DataSetMetaData = GetStringOrDefault(
                    DataSetMetaDataTopicTemplateKey);
            }

            if (options.TopicTemplates.Schema == null)
            {
                options.TopicTemplates.Schema = GetStringOrDefault(
                    SchemaTopicTemplateKey, SchemaTopicTemplateDefault);
            }

            options.DisableOpenApiEndpoint ??= GetBoolOrNull(DisableOpenApiEndpointKey);

            options.EnableRuntimeStateReporting ??= GetBoolOrDefault(
                    EnableRuntimeStateReportingKey, EnableRuntimeStateReportingDefault);

            options.RuntimeStateRoutingInfo ??= GetStringOrDefault(
                    RuntimeStateRoutingInfoKey, RuntimeStateRoutingInfoDefault);

            options.ScaleTestCount ??= GetIntOrDefault(ScaleTestCountKey,
                    ScaleTestCountDefault);

            if (options.DebugLogNotificationsFilter == null)
            {
                options.DebugLogNotificationsFilter =
                    GetStringOrDefault(DebugLogNotificationsFilterKey);
                options.DebugLogNotifications ??=
                    (options.DebugLogNotificationsFilter != null ? true : null);
            }

            if (options.DebugLogNotificationsWithHeartbeat == null)
            {
                options.DebugLogNotificationsWithHeartbeat =
                    GetBoolOrDefault(DebugLogNotificationsWithHeartbeatKey);
                options.DebugLogNotifications ??= options.DebugLogNotifications;
            }

            options.DebugLogNotifications ??= GetBoolOrDefault(DebugLogNotificationsKey);
            options.DebugLogEncodedNotifications ??= GetBoolOrDefault(DebugLogEncodedNotificationsKey);

            if (options.DiagnosticsInterval == null)
            {
                options.DiagnosticsInterval = GetDurationOrNull(DiagnosticsIntervalKey) ??
                   TimeSpan.FromMilliseconds(GetIntOrDefault(DiagnosticsIntervalKey,
                       DiagnosticsIntervalDefaultMillis));
            }

            if (options.DiagnosticsTarget == null)
            {
                if (!Enum.TryParse<PublisherDiagnosticTargetType>(
                    GetStringOrDefault(DiagnosticsTargetKey), out var target))
                {
                    target = PublisherDiagnosticTargetType.Logger;
                }
                options.DiagnosticsTarget = target;
            }

            options.EnableDataSetRoutingInfo ??= GetBoolOrDefault(
                    EnableDataSetRoutingInfoKey, EnableDataSetRoutingInfoDefault);

            options.ForceCredentialEncryption ??= GetBoolOrDefault(
                    ForceCredentialEncryptionKey);

            options.MaxNetworkMessageSize ??= GetIntOrNull(IoTHubMaxMessageSizeKey);

            options.DefaultMaxDataSetMessagesPerPublish ??= (uint?)GetIntOrNull(
                    DefaultMaxMessagesPerPublishKey);

            if (options.DefaultQualityOfService == null)
            {
                if (!Enum.TryParse<QoS>(GetStringOrDefault(DefaultQualityOfServiceKey),
                    out var qos))
                {
                    qos = QoS.AtLeastOnce;
                }
                options.DefaultQualityOfService = qos;
            }

            if (options.DefaultMessageTimeToLive == null)
            {
                var ttl = GetIntOrNull(DefaultMessageTimeToLiveKey);
                options.DefaultMessageTimeToLive = ttl.HasValue ?
                    TimeSpan.FromMilliseconds(ttl.Value) : GetDurationOrNull(
                        DefaultMessageTimeToLiveKey);
            }
            options.DefaultMessageRetention = GetBoolOrNull(DefaultMessageRetentionKey);

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

            options.UnsecureHttpServerPort ??= GetIntOrNull(
                    UnsecureHttpServerPortKey, UnsecureHttpServerPortDefault);
            options.HttpServerPort ??= GetIntOrNull(
                    HttpServerPortKey, HttpServerPortDefault);

            options.ApiKeyOverride ??= GetStringOrDefault(ApiKeyOverrideKey);

            if (options.DefaultDataSetRouting == null &&
                Enum.TryParse<DataSetRoutingMode>(GetStringOrDefault(DefaultDataSetRoutingKey),
                    out var routingMode))
            {
                options.DefaultDataSetRouting = routingMode;
            }

            var schemaNamespace = GetStringOrDefault(SchemaNamespaceKey);
            var avroPreferred = GetBoolOrNull(PreferAvroOverJsonSchemaKey);
            if (schemaNamespace != null || avroPreferred != null ||
                GetBoolOrDefault(PublishMessageSchemaKey))
            {
                options.SchemaOptions ??= new SchemaOptions();
            }
            if (options.SchemaOptions != null)
            {
                options.SchemaOptions.Namespace ??= schemaNamespace;
                options.SchemaOptions.PreferAvroOverJsonSchema ??= avroPreferred;
            }

            options.DisableComplexTypeSystem ??= GetBoolOrNull(DisableComplexTypeSystemKey);
            options.DisableDataSetMetaData = options.DisableComplexTypeSystem;
            // Set a default from the strict setting
            options.DisableDataSetMetaData ??= GetBoolOrDefault(DisableDataSetMetaDataKey,
                !(options.UseStandardsCompliantEncoding ?? false));
            if (options.SchemaOptions != null)
            {
                // Always turn on metadata for schema publishing
                options.DisableComplexTypeSystem = false;
                options.DisableDataSetMetaData = false;
            }
            if (options.DefaultMetaDataUpdateTime == null && options.DisableDataSetMetaData != true)
            {
                options.DefaultMetaDataUpdateTime = GetDurationOrNull(DefaultMetaDataUpdateTimeKey);
            }
            if (options.AsyncMetaDataLoadTimeout == null && options.DisableDataSetMetaData != true)
            {
                options.AsyncMetaDataLoadTimeout = GetDurationOrDefault(AsyncMetaDataLoadTimeoutKey,
                    TimeSpan.FromMilliseconds(AsyncMetaDataLoadTimeoutDefaultMillis));
            }
            options.EnableDataSetKeepAlives ??= GetBoolOrDefault(EnableDataSetKeepAlivesKey);
            options.DefaultKeyFrameCount ??= (uint?)GetIntOrNull(DefaultKeyFrameCountKey);

            options.DisableSessionPerWriterGroup ??= GetBoolOrDefault(DisableSessionPerWriterGroupKey,
                    DisableSessionPerWriterGroupDefault);

            options.DefaultUseReverseConnect ??= GetBoolOrNull(DefaultUseReverseConnectKey);
            options.DisableSubscriptionTransfer ??= GetBoolOrNull(DisableSubscriptionTransferKey);
        }

        /// <summary>
        /// Running in container
        /// </summary>
        private static bool IsContainer => StringComparer.OrdinalIgnoreCase.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")
                ?? string.Empty, "true");

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

        /// <summary>
        /// Publisher version
        /// </summary>
        public static string Version { get; } =
            new StringBuilder(ThisAssembly.AssemblyInformationalVersion)
#if DEBUG
                .Append(" [DEBUG]")
#endif
                .Append(" (")
                .Append(RuntimeInformation.FrameworkDescription)
                .Append('/')
                .Append(AppContext.GetData("RUNTIME_IDENTIFIER") as string
                    ?? RuntimeInformation.ProcessArchitecture.ToString())
                .Append("/OPC Stack ")
                .Append(typeof(SessionChannel).Assembly.GetReleaseVersion().ToString())
                .Append(')')
                .ToString();

        private readonly IProcessIdentity? _identity;
    }
}
