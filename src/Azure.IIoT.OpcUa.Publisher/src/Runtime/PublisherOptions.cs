// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Furly.Extensions.Messaging;
    using System;

    /// <summary>
    /// Publisher configuration
    /// </summary>
    public sealed class PublisherOptions
    {
        /// <summary>
        /// Publisher id
        /// </summary>
        public string? PublisherId { get; set; }

        /// <summary>
        /// Site of the publisher
        /// </summary>
        public string? SiteId { get; set; }

        /// <summary>
        /// Configuration file
        /// </summary>
        public string? PublishedNodesFile { get; set; }

        /// <summary>
        /// Poll changes instead of using file watcher
        /// </summary>
        public bool? UseFileChangePolling { get; set; }

        /// <summary>
        /// Create the configuration file if it does not exist
        /// </summary>
        public bool? CreatePublishFileIfNotExist { get; set; }

        /// <summary>
        /// Create a new ssl certificate on startup
        /// </summary>
        public bool? RenewTlsCertificateOnStartup { get; set; }

        /// <summary>
        /// Max number of nodes per data set (publishing
        /// endpoint inside the configuration of publisher)
        /// </summary>
        public int MaxNodesPerDataSet { get; set; }

        /// <summary>
        /// Messaging profile to use as default inside the
        /// publisher.
        /// </summary>
        public MessagingProfile? MessagingProfile { get; set; }

        /// <summary>
        /// Default max notifications to queue up until a
        /// network message is generated. Defaults to 1 if
        /// no writer group publishing interval is specified.
        /// Otherwise 4096 which is heuristically large enough
        /// to let the publishing interval clear the queue.
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Writer group publishing interval cycle. This is
        /// the timeout until a network message is generated
        /// from the queue of notifications.
        /// </summary>
        public TimeSpan? BatchTriggerInterval { get; set; }

        /// <summary>
        /// Whether to remove duplicate values from the batch
        /// of monitored item samples if samples mode is used.
        /// </summary>
        public bool? RemoveDuplicatesFromBatch { get; set; }

        /// <summary>
        /// Default maximum network message size to use.
        /// </summary>
        public int? MaxNetworkMessageSize { get; set; }

        /// <summary>
        /// ChannelDiagnostics interval
        /// </summary>
        public TimeSpan? DiagnosticsInterval { get; set; }

        /// <summary>
        /// How to emit diagnostics
        /// </summary>
        public PublisherDiagnosticTargetType? DiagnosticsTarget { get; set; }

        /// <summary>
        /// Log ingress notifications to informational log
        /// </summary>
        public bool? DebugLogNotifications { get; set; }

        /// <summary>
        /// Filter to apply to the notifications before adding to log
        /// </summary>
        public string? DebugLogNotificationsFilter { get; set; }

        /// <summary>
        /// Include heartbeats in ingess logs
        /// </summary>
        public bool? DebugLogNotificationsWithHeartbeat { get; set; }

        /// <summary>
        /// Log encoded notifications to informational log
        /// </summary>
        public bool? DebugLogEncodedNotifications { get; set; }

        /// <summary>
        /// Define the maximum number of network messages in the
        /// send part of the publish queue of the writer group.
        /// </summary>
        public int? MaxNetworkMessageSendQueueSize { get; set; }

        /// <summary>
        /// Max number of publish queue partitions the writer group
        /// should be split into.
        /// </summary>
        public int? DefaultWriterGroupPartitions { get; set; }

        /// <summary>
        /// Flag to use reversible encoding for messages
        /// </summary>
        public bool? UseStandardsCompliantEncoding { get; set; }

        /// <summary>
        /// Instead of a dataset with a single entry, Write only the
        /// value without key when possible.
        /// </summary>
        public bool? WriteValueWhenDataSetHasSingleEntry { get; set; }

        /// <summary>
        /// The message timestamp to use
        /// </summary>
        public MessageTimestamp? MessageTimestamp { get; set; }

        /// <summary>
        /// Default topic templates
        /// </summary>
        public TopicTemplatesOptions TopicTemplates { get; } = new TopicTemplatesOptions();

        /// <summary>
        /// Default transport to use if not found
        /// </summary>
        public WriterGroupTransport? DefaultTransport { get; set; }

        /// <summary>
        /// Default quality of service for messages
        /// </summary>
        public QoS? DefaultQualityOfService { get; set; }

        /// <summary>
        /// Default message time to live
        /// </summary>
        public TimeSpan? DefaultMessageTimeToLive { get; set; }

        /// <summary>
        /// Default whether to set message retain flag
        /// </summary>
        public bool? DefaultMessageRetention { get; set; }

        /// <summary>
        /// Default Max data set messages per published network
        /// message.
        /// </summary>
        public uint? DefaultMaxDataSetMessagesPerPublish { get; set; }

        /// <summary>
        /// Configuration flag for enabling/disabling
        /// runtime state reporting.
        /// </summary>
        public bool? EnableRuntimeStateReporting { get; set; }

        /// <summary>
        /// The routing info to add to the runtime state
        /// events.
        /// </summary>
        public string? RuntimeStateRoutingInfo { get; set; }

        /// <summary>
        /// Never load the complex type system from any session.
        /// This disables metadata loading capability but also
        /// the ability to encode complex types.
        /// </summary>
        public bool? DisableComplexTypeSystem { get; set; }

        /// <summary>
        /// Whether to enable or disable data set metadata explicitly
        /// </summary>
        public bool? DisableDataSetMetaData { get; set; }

        /// <summary>
        /// Default metadata send interval.
        /// </summary>
        public TimeSpan? DefaultMetaDataUpdateTime { get; set; }

        /// <summary>
        /// Timeout to block the first message after a metadata
        /// change is causing the load of the new metadata.
        /// Default is block forever
        /// </summary>
        public TimeSpan? AsyncMetaDataLoadTimeout { get; set; }

        /// <summary>
        /// Enable adding data set routing info to messages
        /// </summary>
        public bool? EnableDataSetRoutingInfo { get; set; }

        /// <summary>
        /// Whether to enable or disable keep alive messages
        /// </summary>
        public bool? EnableDataSetKeepAlives { get; set; }

        /// <summary>
        /// Default keyframe count
        /// </summary>
        public uint? DefaultKeyFrameCount { get; set; }

        /// <summary>
        /// Disable creating a separate session per writer group. This
        /// will re-use sessions across writer groups. Default is to
        /// create a seperate session.
        /// </summary>
        public bool? DisableSessionPerWriterGroup { get; set; }

        /// <summary>
        /// Always default to use or not use reverse connect
        /// unless overridden by the configuration.
        /// </summary>
        public bool? DefaultUseReverseConnect { get; set; }

        /// <summary>
        /// Disable subscription transfer on reconnect.
        /// </summary>
        public bool? DisableSubscriptionTransfer { get; set; }

        /// <summary>
        /// Force encryption of credentials in publisher configuration
        /// or dont store credentials. Default is false.
        /// </summary>
        public bool? ForceCredentialEncryption { get; set; }

        /// <summary>
        /// Optional default node id and qualified name namespace
        /// format to use when serializing nodes in messages and
        /// responses.
        /// </summary>
        public NamespaceFormat? DefaultNamespaceFormat { get; set; }

        /// <summary>
        /// Disable open api endpoint
        /// </summary>
        public bool? DisableOpenApiEndpoint { get; set; }

        /// <summary>
        /// Scale test option
        /// </summary>
        public int? ScaleTestCount { get; set; }

        /// <summary>
        /// Ignore all publishing intervals set in the configuration.
        /// </summary>
        public bool? IgnoreConfiguredPublishingIntervals { get; set; }

        /// <summary>
        /// Allow setting or overriding the current api key
        /// </summary>
        public string? ApiKeyOverride { get; set; }

        /// <summary>
        /// Use auto routing based on the opc ua address space
        /// browse paths.
        /// </summary>
        public DataSetRoutingMode? DefaultDataSetRouting { get; set; }

        /// <summary>
        /// Schema generation options if schema generation is
        /// enabled.
        /// </summary>
        public SchemaOptions? SchemaOptions { get; set; }

        /// <summary>
        /// Disable resource monitoring
        /// </summary>
        public bool? DisableResourceMonitoring { get; set; }

        /// <summary>
        /// Unsecure port
        /// </summary>
        public int? UnsecureHttpServerPort { get; set; }

        /// <summary>
        /// Secure port
        /// </summary>
        public int? HttpServerPort { get; set; }
    }
}
