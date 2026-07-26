// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Core.Configuration;
    using Azure.IIoT.OpcUa.Core.Hosting;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
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
        public const string CreatePublishFileIfNotExistKey = "CreatePublishFileIfNotExist";
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
        public const string SendDataSetKeepAlivesAsKeyFrameKey = "SendDataSetKeepAlivesAsKeyFrame";
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
        public const string EnableCloudEventsKey = "EnableCloudEvents";
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
        public const string DataSetNameVariableName = "DataSetName";
        public const string DataSetTopicPathVariableName = "DataSetTopicPath";
        public const string DataSetWriterIdVariableName = "DataSetWriterId";
        public const string DataSetFieldIdVariableName = "DataSetFieldId";
        public const string DataSetClassIdVariableName = "DataSetClassId";
        public const string EventNameVariableName = "EventName";
        public const string EventContextVariableName = "EventContext";
        public const string EventSourceVariableName = "EventSource";
        public const string EncodingVariableName = "Encoding";
        public const string ClusterNamespaceVariableName = "ClusterNamespace";
        public const string ClusterHostVariableName = "ClusterHost";
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
            $"{{{RootTopicVariableName}}}/{{{EventSourceVariableName}}}/{{{EventNameVariableName}}}";
        public const string MetadataTopicTemplateDefault =
            $"{{{TelemetryTopicVariableName}}}/metadata";
        public const string DiagnosticsTopicTemplateDefault =
            $"{{{RootTopicVariableName}}}/diagnostics/{{{WriterGroupVariableName}}}";
        public const string RootTopicTemplateDefault =
            $"{{{PublisherIdVariableName}}}";
        public const string RootTopicTemplateCluster =
            $"{{{ClusterNamespaceVariableName}}}/{{{PublisherIdVariableName}}}";
        public const string SchemaTopicTemplateDefault =
            $"{{{TelemetryTopicVariableName}}}/schema";
        public const string PublishedNodesFileDefault = "publishednodes.json";
        public const string RuntimeStateRoutingInfoDefault = "runtimeinfo";
        public const bool EnableRuntimeStateReportingDefault = false;
        public const bool UseStandardsCompliantEncodingDefault = false;
        public const bool EnableDataSetRoutingInfoDefault = false;
        public const bool EnableCloudEventsDefault = false;
        public const MessageEncoding MessageEncodingDefault = MessageEncoding.Json;
        public const int MaxNodesPerDataSetDefault = 1000;
        public const int BatchSizeLegacyDefault = 50;
        public const int MaxNetworkMessageSendQueueSizeDefault = 4096;
        public const int BatchTriggerIntervalLLegacyDefaultMillis = 10 * 1000;
        public const int AsyncMetaDataLoadTimeoutDefaultMillis = 5 * 1000;
        public const int DiagnosticsIntervalDefaultMillis = 60 * 1000;
        public const int ScaleTestCountDefault = 1;
        public const bool IgnoreConfiguredPublishingIntervalsDefault = false;
        public const bool DisableSessionPerWriterGroupDefault = false;
        public static readonly int UnsecureHttpServerPortDefault = IsContainer && IsRunningAsRoot ? 80 : 9071;
        public static readonly int HttpServerPortDefault = IsContainer && IsRunningAsRoot ? 443 : 9072;
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
                var configuredMode = GetStringOrDefault(MessagingModeKey);
                ThrowIfRemovedMessagingMode(configuredMode);
                if (!Enum.TryParse<MessagingMode>(configuredMode, out var messagingMode))
                {
                    messagingMode = MessagingMode.PubSub;
                }

                if (GetBoolOrDefault(FullFeaturedMessageKey, false) &&
                    messagingMode == MessagingMode.PubSub)
                {
                    messagingMode = MessagingMode.FullNetworkMessages;
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
                    RootTopicTemplateKey, options.IsAzureIoTOperationsConnector != null ?
                        RootTopicTemplateCluster : RootTopicTemplateDefault);
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

            options.EnableCloudEvents ??= GetBoolOrDefault(
                    EnableCloudEventsKey, EnableCloudEventsDefault);
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
            var metaDataEnabled = options.SchemaOptions != null || options.DisableDataSetMetaData != true;
            if (metaDataEnabled)
            {
                // Always turn on complex type system for schema publishing
                options.DisableComplexTypeSystem = false;
            }
            if (options.DefaultMetaDataUpdateTime == null && metaDataEnabled)
            {
                options.DefaultMetaDataUpdateTime = GetDurationOrNull(DefaultMetaDataUpdateTimeKey);
            }
            if (options.AsyncMetaDataLoadTimeout == null && metaDataEnabled)
            {
                options.AsyncMetaDataLoadTimeout = GetDurationOrDefault(AsyncMetaDataLoadTimeoutKey,
                    TimeSpan.FromMilliseconds(AsyncMetaDataLoadTimeoutDefaultMillis));
            }
            options.EnableDataSetKeepAlives ??= GetBoolOrDefault(EnableDataSetKeepAlivesKey);
            options.SendDataSetKeepAlivesAsKeyFrame ??= GetBoolOrDefault(SendDataSetKeepAlivesAsKeyFrameKey);
            options.DefaultKeyFrameCount ??= (uint?)GetIntOrNull(DefaultKeyFrameCountKey);

            options.DisableSessionPerWriterGroup ??= GetBoolOrDefault(DisableSessionPerWriterGroupKey,
                    DisableSessionPerWriterGroupDefault);

            options.DefaultUseReverseConnect ??= GetBoolOrNull(DefaultUseReverseConnectKey);
            options.DisableSubscriptionTransfer ??= GetBoolOrNull(DisableSubscriptionTransferKey);
        }

        /// <inheritdoc/>
        protected override PublisherOptions Bind()
        {
            var topicTemplates = Configuration.GetSection(
                nameof(PublisherOptions.TopicTemplates)).Get<TopicTemplatesOptions>();
            var discovery = Configuration.GetSection(
                nameof(PublisherOptions.AioNetworkDiscovery)).Get<DiscoveryConfigModel>();
            var schemaOptions = NormalizeLegacyBooleanAliases(
                nameof(SchemaOptions.PreferAvroOverJsonSchema))
                .GetSection(nameof(PublisherOptions.SchemaOptions))
                .Get<SchemaOptions>();
            var options = new PublisherOptions
            {
                PublisherId = GetStringOrDefault(nameof(PublisherOptions.PublisherId)),
                SiteId = GetStringOrDefault(nameof(PublisherOptions.SiteId)),
                PublishedNodesFile = GetStringOrDefault(nameof(PublisherOptions.PublishedNodesFile)),
                UseFileChangePolling = GetBoolOrNull(nameof(PublisherOptions.UseFileChangePolling)),
                CreatePublishFileIfNotExist = GetBoolOrNull(
                    nameof(PublisherOptions.CreatePublishFileIfNotExist)),
                RenewTlsCertificateOnStartup = GetBoolOrNull(
                    nameof(PublisherOptions.RenewTlsCertificateOnStartup)),
                MaxNodesPerDataSet = GetIntOrDefault(nameof(PublisherOptions.MaxNodesPerDataSet)),
                BatchSize = GetIntOrNull(nameof(PublisherOptions.BatchSize)),
                BatchTriggerInterval = GetDurationOrNull(
                    nameof(PublisherOptions.BatchTriggerInterval)),
                RemoveDuplicatesFromBatch = GetBoolOrNull(
                    nameof(PublisherOptions.RemoveDuplicatesFromBatch)),
                MaxNetworkMessageSize = GetIntOrNull(
                    nameof(PublisherOptions.MaxNetworkMessageSize)),
                DiagnosticsInterval = GetDurationOrNull(
                    nameof(PublisherOptions.DiagnosticsInterval)),
                DiagnosticsTarget = GetEnumOrNull<PublisherDiagnosticTargetType>(
                    nameof(PublisherOptions.DiagnosticsTarget)),
                DebugLogNotifications = GetBoolOrNull(
                    nameof(PublisherOptions.DebugLogNotifications)),
                DebugLogNotificationsFilter = GetStringOrDefault(
                    nameof(PublisherOptions.DebugLogNotificationsFilter)),
                DebugLogNotificationsWithHeartbeat = GetBoolOrNull(
                    nameof(PublisherOptions.DebugLogNotificationsWithHeartbeat)),
                DebugLogEncodedNotifications = GetBoolOrNull(
                    nameof(PublisherOptions.DebugLogEncodedNotifications)),
                MaxNetworkMessageSendQueueSize = GetIntOrNull(
                    nameof(PublisherOptions.MaxNetworkMessageSendQueueSize)),
                DefaultWriterGroupPartitions = GetIntOrNull(
                    nameof(PublisherOptions.DefaultWriterGroupPartitions)),
                UseStandardsCompliantEncoding = GetBoolOrNull(
                    nameof(PublisherOptions.UseStandardsCompliantEncoding)),
                WriteValueWhenDataSetHasSingleEntry = GetBoolOrNull(
                    nameof(PublisherOptions.WriteValueWhenDataSetHasSingleEntry)),
                MessageTimestamp = GetEnumOrNull<MessageTimestamp>(
                    nameof(PublisherOptions.MessageTimestamp)),
                DefaultTransport = GetEnumOrNull<WriterGroupTransport>(
                    nameof(PublisherOptions.DefaultTransport)),
                DefaultQualityOfService = GetEnumOrNull<QoS>(
                    nameof(PublisherOptions.DefaultQualityOfService)),
                DefaultMessageTimeToLive = GetDurationOrNull(
                    nameof(PublisherOptions.DefaultMessageTimeToLive)),
                DefaultMessageRetention = GetBoolOrNull(
                    nameof(PublisherOptions.DefaultMessageRetention)),
                DefaultMaxDataSetMessagesPerPublish = GetUIntOrNull(
                    nameof(PublisherOptions.DefaultMaxDataSetMessagesPerPublish)),
                EnableRuntimeStateReporting = GetBoolOrNull(
                    nameof(PublisherOptions.EnableRuntimeStateReporting)),
                RuntimeStateRoutingInfo = GetStringOrDefault(
                    nameof(PublisherOptions.RuntimeStateRoutingInfo)),
                DisableComplexTypeSystem = GetBoolOrNull(
                    nameof(PublisherOptions.DisableComplexTypeSystem)),
                DisableDataSetMetaData = GetBoolOrNull(
                    nameof(PublisherOptions.DisableDataSetMetaData)),
                DefaultMetaDataUpdateTime = GetDurationOrNull(
                    nameof(PublisherOptions.DefaultMetaDataUpdateTime)),
                AsyncMetaDataLoadTimeout = GetDurationOrNull(
                    nameof(PublisherOptions.AsyncMetaDataLoadTimeout)),
                EnableCloudEvents = GetBoolOrNull(nameof(PublisherOptions.EnableCloudEvents)),
                EnableDataSetRoutingInfo = GetBoolOrNull(
                    nameof(PublisherOptions.EnableDataSetRoutingInfo)),
                EnableDataSetKeepAlives = GetBoolOrNull(
                    nameof(PublisherOptions.EnableDataSetKeepAlives)),
                SendDataSetKeepAlivesAsKeyFrame = GetBoolOrNull(
                    nameof(PublisherOptions.SendDataSetKeepAlivesAsKeyFrame)),
                DefaultKeyFrameCount = GetUIntOrNull(
                    nameof(PublisherOptions.DefaultKeyFrameCount)),
                DisableSessionPerWriterGroup = GetBoolOrNull(
                    nameof(PublisherOptions.DisableSessionPerWriterGroup)),
                DefaultUseReverseConnect = GetBoolOrNull(
                    nameof(PublisherOptions.DefaultUseReverseConnect)),
                DisableSubscriptionTransfer = GetBoolOrNull(
                    nameof(PublisherOptions.DisableSubscriptionTransfer)),
                ForceCredentialEncryption = GetBoolOrNull(
                    nameof(PublisherOptions.ForceCredentialEncryption)),
                DefaultNamespaceFormat = GetEnumOrNull<NamespaceFormat>(
                    nameof(PublisherOptions.DefaultNamespaceFormat)),
                DisableOpenApiEndpoint = GetBoolOrNull(
                    nameof(PublisherOptions.DisableOpenApiEndpoint)),
                ScaleTestCount = GetIntOrNull(nameof(PublisherOptions.ScaleTestCount)),
                IgnoreConfiguredPublishingIntervals = GetBoolOrNull(
                    nameof(PublisherOptions.IgnoreConfiguredPublishingIntervals)),
                ApiKeyOverride = GetStringOrDefault(nameof(PublisherOptions.ApiKeyOverride)),
                DefaultDataSetRouting = GetEnumOrNull<DataSetRoutingMode>(
                    nameof(PublisherOptions.DefaultDataSetRouting)),
                SchemaOptions = schemaOptions,
                DisableResourceMonitoring = GetBoolOrNull(
                    nameof(PublisherOptions.DisableResourceMonitoring)),
                UnsecureHttpServerPort = GetIntOrNull(
                    nameof(PublisherOptions.UnsecureHttpServerPort)),
                HttpServerPort = GetIntOrNull(nameof(PublisherOptions.HttpServerPort)),
                IsAzureIoTOperationsConnector = GetBoolOrNull(
                    nameof(PublisherOptions.IsAzureIoTOperationsConnector)),
                AioDiscoveredDeviceEndpointType = GetStringOrDefault(
                    nameof(PublisherOptions.AioDiscoveredDeviceEndpointType)),
                AioDiscoveredDeviceEndpointTypeVersion = GetStringOrDefault(
                    nameof(PublisherOptions.AioDiscoveredDeviceEndpointTypeVersion)),
                AioNetworkDiscoveryMode = GetEnumOrNull<DiscoveryMode>(
                    nameof(PublisherOptions.AioNetworkDiscoveryMode)),
                AioNetworkDiscoveryInterval = GetDurationOrNull(
                    nameof(PublisherOptions.AioNetworkDiscoveryInterval))
            };
            if (topicTemplates != null)
            {
                options.TopicTemplates.Root = topicTemplates.Root;
                options.TopicTemplates.Method = topicTemplates.Method;
                options.TopicTemplates.Events = topicTemplates.Events;
                options.TopicTemplates.Diagnostics = topicTemplates.Diagnostics;
                options.TopicTemplates.Telemetry = topicTemplates.Telemetry;
                options.TopicTemplates.DataSetMetaData = topicTemplates.DataSetMetaData;
                options.TopicTemplates.Schema = topicTemplates.Schema;
            }
            foreach (var transport in GetTransports())
            {
                options.AllowedEventAndDiagnosticsTransports.Add(transport);
            }
            if (discovery != null)
            {
                options.AioNetworkDiscovery.AddressRangesToScan = discovery.AddressRangesToScan;
                options.AioNetworkDiscovery.NetworkProbeTimeout = discovery.NetworkProbeTimeout;
                options.AioNetworkDiscovery.MaxNetworkProbes = discovery.MaxNetworkProbes;
                options.AioNetworkDiscovery.PortRangesToScan = discovery.PortRangesToScan;
                options.AioNetworkDiscovery.PortProbeTimeout = discovery.PortProbeTimeout;
                options.AioNetworkDiscovery.MaxPortProbes = discovery.MaxPortProbes;
                options.AioNetworkDiscovery.MinPortProbesPercent = discovery.MinPortProbesPercent;
                options.AioNetworkDiscovery.IdleTimeBetweenScans = discovery.IdleTimeBetweenScans;
                options.AioNetworkDiscovery.DiscoveryUrls = discovery.DiscoveryUrls;
                options.AioNetworkDiscovery.Locales = discovery.Locales;
            }
            return options;
        }

        /// <summary>
        /// The proprietary sample messaging modes were removed in 3.0 because
        /// they have no OPC UA PubSub representation and cannot be produced by
        /// the standard PubSub runtime. Reject them explicitly instead of
        /// silently publishing a different message format.
        /// </summary>
        /// <param name="configuredMode"></param>
        /// <exception cref="ConfigurationErrorsException"></exception>
        private static void ThrowIfRemovedMessagingMode(string? configuredMode)
        {
            if (string.IsNullOrWhiteSpace(configuredMode))
            {
                return;
            }
            var replacement = configuredMode.Trim() switch
            {
                var mode when StringComparer.OrdinalIgnoreCase.Equals(mode, "Samples")
                    => nameof(MessagingMode.PubSub),
                var mode when StringComparer.OrdinalIgnoreCase.Equals(mode, "FullSamples")
                    => nameof(MessagingMode.FullNetworkMessages),
                _ => null
            };
            if (replacement is null)
            {
                return;
            }
            throw new ConfigurationErrorsException(
                $"The messaging mode '{configuredMode.Trim()}' was removed in OPC Publisher 3.0. " +
                $"It emitted a proprietary message format that the OPC UA PubSub runtime " +
                $"cannot produce. Configure '{replacement}' instead, or set an explicit " +
                $"messaging profile.");
        }

        private TEnum? GetEnumOrNull<TEnum>(string key) where TEnum : struct, Enum
        {
            return Enum.TryParse<TEnum>(GetStringOrDefault(key), true, out var value)
                ? value : null;
        }

        private uint? GetUIntOrNull(string key)
        {
            return uint.TryParse(GetStringOrDefault(key), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var value) ? value : null;
        }

        private IEnumerable<WriterGroupTransport> GetTransports()
        {
            foreach (var value in Configuration.GetSection(
                nameof(PublisherOptions.AllowedEventAndDiagnosticsTransports)).GetChildren())
            {
                if (Enum.TryParse<WriterGroupTransport>(value.Value, true, out var transport))
                {
                    yield return transport;
                }
            }
        }

        /// <summary>
        /// Running as root
        /// </summary>
        public static bool IsRunningAsRoot => StringComparer.OrdinalIgnoreCase.Equals(
            Environment.UserName, "root");

        /// <summary>
        /// Running in container
        /// </summary>
        public static bool IsContainer => StringComparer.OrdinalIgnoreCase.Equals(
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
                .Append(typeof(ITransportChannel).Assembly.GetReleaseVersion().ToString())
                .Append(')')
                .ToString();

        private readonly IProcessIdentity? _identity;
    }
}
