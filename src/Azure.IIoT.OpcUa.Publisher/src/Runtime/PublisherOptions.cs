﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
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
        public string? Site { get; set; }

        /// <summary>
        /// Configuration file
        /// </summary>
        public string? PublishedNodesFile { get; set; }

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
        /// Diagnostics interval
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
        /// Define the maximum number of messages in egress buffer,
        /// Default: 4096 messages with 256KB ends up in 1 GB memory consumed.
        /// </summary>
        public int? MaxNetworkMessageSendQueueSize { get; set; }

        /// <summary>
        /// Flag to use reversible encoding for messages
        /// </summary>
        public bool? UseStandardsCompliantEncoding { get; set; }

        /// <summary>
        /// The message timestamp to use
        /// </summary>
        public MessageTimestamp? MessageTimestamp { get; set; }

        /// <summary>
        /// Root topic template
        /// </summary>
        public string? RootTopicTemplate { get; set; }

        /// <summary>
        /// Method topic template
        /// </summary>
        public string? MethodTopicTemplate { get; set; }

        /// <summary>
        /// Events topic template
        /// </summary>
        public string? EventsTopicTemplate { get; set; }

        /// <summary>
        /// Diagnostics topic template
        /// </summary>
        public string? DiagnosticsTopicTemplate { get; set; }

        /// <summary>
        /// Telemetry topic template
        /// </summary>
        public string? TelemetryTopicTemplate { get; set; }

        /// <summary>
        /// Default metadata queue name
        /// </summary>
        public string? DataSetMetaDataTopicTemplate { get; set; }

        /// <summary>
        /// Default transport to use if not found
        /// </summary>
        public WriterGroupTransport? DefaultTransport { get; set; }

        /// <summary>
        /// Default quality of service for messages
        /// </summary>
        public QoS? DefaultQualityOfService { get; set; }

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
        /// Enable adding data set routing info to messages
        /// </summary>
        public bool? EnableDataSetRoutingInfo { get; set; }

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
    }
}
