// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Contains the nodes which should be published
    /// </summary>
    [DataContract]
    public sealed record class PublishedNodesEntryModel
    {
        /// <summary>
        /// Version number of the entry
        /// </summary>
        [DataMember(Name = "Version", Order = 0,
            EmitDefaultValue = false)]
        public uint? Version { get; set; }

        /// <summary>
        /// Last change to the entry
        /// </summary>
        [DataMember(Name = "LastChangeDateTime", Order = 1,
            EmitDefaultValue = false)]
        public DateTimeOffset? LastChangeDateTime { get; set; }

        /// <summary>
        /// Name of the data set writer.
        /// </summary>
        [DataMember(Name = "DataSetWriterId", Order = 2,
            EmitDefaultValue = false)]
        public string? DataSetWriterId { get; set; }

        /// <summary>
        /// The Group the writer belongs to.
        /// </summary>
        [DataMember(Name = "DataSetWriterGroup", Order = 3,
            EmitDefaultValue = false)]
        public string? DataSetWriterGroup { get; set; }

        /// <summary>
        /// Nodes defined in the collection.
        /// </summary>
        [DataMember(Name = "OpcNodes", Order = 4,
            EmitDefaultValue = false)]
        public IList<OpcNodeModel>? OpcNodes { get; set; }

        /// <summary>
        /// A dataset class id.
        /// </summary>
        [DataMember(Name = "DataSetClassId", Order = 5,
            EmitDefaultValue = false)]
        public Guid DataSetClassId { get; set; }

        /// <summary>
        /// The optional short name of the dataset.
        /// </summary>
        [DataMember(Name = "DataSetName", Order = 6,
            EmitDefaultValue = false)]
        public string? DataSetName { get; set; }

        /// <summary>
        /// The Publishing interval for a dataset writer
        /// in miliseconds.
        /// </summary>
        [DataMember(Name = "DataSetPublishingInterval", Order = 7,
            EmitDefaultValue = false)]
        public int? DataSetPublishingInterval { get; set; }

        /// <summary>
        /// The Publishing interval for a dataset writer
        /// in timespan format. Takes precedence over
        /// <see cref="DataSetPublishingInterval"/> if defined.
        /// </summary>
        [DataMember(Name = "DataSetPublishingIntervalTimespan", Order = 8,
            EmitDefaultValue = false)]
        public TimeSpan? DataSetPublishingIntervalTimespan { get; set; }

        /// <summary>
        /// Insert a key frame every x messages
        /// </summary>
        [DataMember(Name = "DataSetKeyFrameCount", Order = 9,
            EmitDefaultValue = false)]
        public uint? DataSetKeyFrameCount { get; set; }

        /// <summary>
        /// Send metadata at the configured interval
        /// even when not changing expressed in milliseconds.
        /// </summary>
        [DataMember(Name = "MetaDataUpdateTime", Order = 10,
            EmitDefaultValue = false)]
        public int? MetaDataUpdateTime { get; set; }

        /// <summary>
        /// Send metadata at the configured interval even when not
        /// changing expressed as duration. Takes precedence over
        /// <see cref="MetaDataUpdateTime"/>if defined.
        /// </summary>
        [DataMember(Name = "MetaDataUpdateTimeTimespan", Order = 11,
            EmitDefaultValue = false)]
        public TimeSpan? MetaDataUpdateTimeTimespan { get; set; }

        /// <summary>
        /// Send a keep alive message when a subscription keep
        /// alive notification is received inside the writer. If keep
        /// alive messages are not supported by the messaging
        /// profile chosen this value is ignored.
        /// </summary>
        [DataMember(Name = "SendKeepAliveDataSetMessages", Order = 12,
            EmitDefaultValue = false)]
        public bool SendKeepAliveDataSetMessages { get; set; }

        /// <summary>
        /// The endpoint URL of the OPC UA server.
        /// </summary>
        [DataMember(Name = "EndpointUrl", Order = 13)]
        [Required]
        public required string EndpointUrl { get; set; }

        /// <summary>
        /// When the publishing timer has expired this number of
        /// times without requiring any Notification to be sent,
        /// to the writer a keep-alive message is sent.
        /// </summary>
        [DataMember(Name = "MaxKeepAliveCount", Order = 14,
            EmitDefaultValue = false)]
        public uint? MaxKeepAliveCount { get; set; }

        /// <summary>
        /// The optional description of the dataset.
        /// </summary>
        [DataMember(Name = "DataSetDescription", Order = 15,
            EmitDefaultValue = false)]
        public string? DataSetDescription { get; set; }

        /// <summary>
        /// Priority of the writer subscription.
        /// </summary>
        [DataMember(Name = "Priority", Order = 16,
            EmitDefaultValue = false)]
        public byte? Priority { get; set; }

        /// <summary>
        /// Optional field and value pairs to insert into the
        /// data sets emitted by data set writer.
        /// </summary>
        [DataMember(Name = "DataSetExtensionFields", Order = 17,
            EmitDefaultValue = false)]
        public IDictionary<string, VariantValue>? DataSetExtensionFields { get; set; }

        /// <summary>
        /// The specific security mode to use for the specified
        /// endpoint. Overrides <see cref="UseSecurity"/> setting.
        /// If the security mode is not available with any
        /// configured security policy connectivity will fail.
        /// Default: <see cref="SecurityMode.NotNone"/> if
        /// <see cref="UseSecurity"/> is <c>true</c>,
        /// otherwise <see cref="SecurityMode.None"/>
        /// </summary>
        [DataMember(Name = "EndpointSecurityMode", Order = 18,
            EmitDefaultValue = false)]
        public SecurityMode? EndpointSecurityMode { get; set; }

        /// <summary>
        /// The specific security policy to use for the specified
        /// endpoint. Overrides <see cref="UseSecurity"/> setting.
        /// If the security policy is not available with the
        /// specified security mode connectivity will fail.
        /// </summary>
        [DataMember(Name = "EndpointSecurityPolicy", Order = 19,
            EmitDefaultValue = false)]
        public string? EndpointSecurityPolicy { get; set; }

        /// <summary>
        /// The messaging mode to use for the data sets
        /// in hte writer group
        /// </summary>
        [DataMember(Name = "MessagingMode", Order = 20,
            EmitDefaultValue = false)]
        public MessagingMode? MessagingMode { get; set; }

        /// <summary>
        /// The messaging encoding to use for the data sets
        /// in hte writer group
        /// </summary>
        [DataMember(Name = "MessageEncoding", Order = 21,
            EmitDefaultValue = false)]
        public MessageEncoding? MessageEncoding { get; set; }

        /// <summary>
        /// Send network messages when the notification queue
        /// exceeds this number. Causes this many notifications
        /// to be added to network messages
        /// </summary>
        [DataMember(Name = "BatchSize", Order = 22,
            EmitDefaultValue = false)]
        public uint? BatchSize { get; set; }

        /// <summary>
        /// Send network messages at the specified publishing
        /// interval.
        /// </summary>
        [DataMember(Name = "BatchTriggerInterval", Order = 23,
            EmitDefaultValue = false)]
        public int? BatchTriggerInterval { get; set; }

        /// <summary>
        /// Send network messages at the specified publishing
        /// interval.
        /// Takes precedence over <see cref="BatchTriggerInterval"/>
        /// if defined.
        /// </summary>
        [DataMember(Name = "BatchTriggerIntervalTimespan", Order = 24,
            EmitDefaultValue = false)]
        public TimeSpan? BatchTriggerIntervalTimespan { get; set; }

        /// <summary>
        /// Use reverse connect to connect ot the endpoint
        /// </summary>
        [DataMember(Name = "UseReverseConnect", Order = 25,
            EmitDefaultValue = false)]
        public bool? UseReverseConnect { get; set; }

        /// <summary>
        /// Configure the dataset routing behavior for the
        /// contained nodes.
        /// </summary>
        [DataMember(Name = "DataSetRouting", Order = 26,
            EmitDefaultValue = false)]
        public DataSetRoutingMode? DataSetRouting { get; set; }

        /// <summary>
        /// Writer group queue overrides the default writer group
        /// topic template to use.
        /// </summary>
        [DataMember(Name = "WriterGroupQueueName", Order = 27,
            EmitDefaultValue = false)]
        public string? WriterGroupQueueName { get; set; }

        /// <summary>
        /// The quality of service to use
        /// </summary>
        [DataMember(Name = "WriterGroupQualityOfService", Order = 28,
            EmitDefaultValue = false)]
        public QoS? WriterGroupQualityOfService { get; set; }

        /// <summary>
        /// The writer group transport to use
        /// </summary>
        [DataMember(Name = "WriterGroupTransport", Order = 29,
            EmitDefaultValue = false)]
        public WriterGroupTransport? WriterGroupTransport { get; set; }

        /// <summary>
        /// Secure transport should be used to connect to
        /// the opc server.
        /// </summary>
        [DataMember(Name = "UseSecurity", Order = 30)]
        public bool? UseSecurity { get; set; }

        /// <summary>
        /// authentication mode
        /// </summary>
        [DataMember(Name = "OpcAuthenticationMode", Order = 31)]
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// encrypted username
        /// </summary>
        [DataMember(Name = "EncryptedAuthUsername", Order = 32,
            EmitDefaultValue = false)]
        public string? EncryptedAuthUsername { get; set; }

        /// <summary>
        /// encrypted password
        /// </summary>
        [DataMember(Name = "EncryptedAuthPassword", Order = 33,
            EmitDefaultValue = false)]
        public string? EncryptedAuthPassword { get; set; }

        /// <summary>
        /// plain username
        /// </summary>
        [DataMember(Name = "OpcAuthenticationUsername", Order = 34,
            EmitDefaultValue = false)]
        public string? OpcAuthenticationUsername { get; set; }

        /// <summary>
        /// plain password
        /// </summary>
        [DataMember(Name = "OpcAuthenticationPassword", Order = 35,
            EmitDefaultValue = false)]
        public string? OpcAuthenticationPassword { get; set; }

        /// <summary>
        /// Writer queue overrides the writer group queue name.
        /// Network messages are then split across queues with
        /// Qos also accounted for.
        /// </summary>
        [DataMember(Name = "QueueName", Order = 36,
            EmitDefaultValue = false)]
        public string? QueueName { get; set; }

        /// <summary>
        /// Meta data queue name to use for the writer. Overrides
        /// the default metadata topic template.
        /// </summary>
        [DataMember(Name = "MetaDataQueueName", Order = 37,
            EmitDefaultValue = false)]
        public string? MetaDataQueueName { get; set; }

        /// <summary>
        /// Quality of service to use for the writer. Overrides
        /// the Writer group quality of service and together with
        /// queue name causes network messages to be split.
        /// </summary>
        [DataMember(Name = "QualityOfService", Order = 38,
            EmitDefaultValue = false)]
        public QoS? QualityOfService { get; set; }

        /// <summary>
        /// Number of partitions to split the writer group into
        /// when publishing to target topics.
        /// </summary>
        [DataMember(Name = "WriterGroupPartitions", Order = 39,
            EmitDefaultValue = false)]
        public int? WriterGroupPartitions { get; set; }

        /// <summary>
        /// Disable subscription transfer on reconnect
        /// </summary>
        [DataMember(Name = "DisableSubscriptionTransfer", Order = 40,
            EmitDefaultValue = false)]
        public bool? DisableSubscriptionTransfer { get; set; }

        /// <summary>
        /// The timeout to use to monitor the monitored items in the
        /// subscription are continously reporting fresh data.
        /// </summary>
        [DataMember(Name = "OpcNodeWatchdogTimespan", Order = 42,
            EmitDefaultValue = false)]
        public TimeSpan? OpcNodeWatchdogTimespan { get; set; }

        /// <summary>
        /// Subscription watchdog behavior. This is the behavior of
        /// the watchdog on the subscription that is executed when
        /// any watchdog triggers and the conditions are met.
        /// </summary>
        [DataMember(Name = "DataSetWriterWatchdogBehavior", Order = 43,
            EmitDefaultValue = false)]
        public SubscriptionWatchdogBehavior? DataSetWriterWatchdogBehavior { get; set; }

        /// <summary>
        /// The watchdog condition that determines whether the watchdog
        /// behavior is executed, e.g. when all items are late or
        /// any item is late (default)
        /// </summary>
        [DataMember(Name = "OpcNodeWatchdogCondition", Order = 44,
            EmitDefaultValue = false)]
        public MonitoredItemWatchdogCondition? OpcNodeWatchdogCondition { get; set; }

        /// <summary>
        /// The default sampling interval for all items in a dataset writer
        /// in miliseconds if the nodes do not specify a sampling rate.
        /// </summary>
        [DataMember(Name = "DataSetSamplingInterval", Order = 45,
            EmitDefaultValue = false)]
        public int? DataSetSamplingInterval { get; set; }

        /// <summary>
        /// The Sampling interval for the nodes in the dataset writer
        /// in timespan format. Takes precedence over
        /// <see cref="DataSetSamplingInterval"/> if defined.
        /// </summary>
        [DataMember(Name = "DataSetSamplingIntervalTimespan", Order = 46,
            EmitDefaultValue = false)]
        public TimeSpan? DataSetSamplingIntervalTimespan { get; set; }

        /// <summary>
        /// Whether to fetch the display name and use it as
        /// data set id for all opc node items in the data set
        /// </summary>
        [DataMember(Name = "DataSetFetchDisplayNames", Order = 47,
            EmitDefaultValue = false)]
        public bool? DataSetFetchDisplayNames { get; set; }

        /// <summary>
        /// Default time to live for messages sent through
        /// the writer group if the transport supports it.
        /// </summary>
        [DataMember(Name = "WriterGroupMessageTtlTimepan", Order = 49,
            EmitDefaultValue = false)]
        public TimeSpan? WriterGroupMessageTtlTimepan { get; set; }

        /// <summary>
        /// Default message retention setting for messages sent
        /// through the writer group if the transport supports it.
        /// </summary>
        [DataMember(Name = "WriterGroupMessageRetention", Order = 50,
            EmitDefaultValue = false)]
        public bool? WriterGroupMessageRetention { get; set; }

        /// <summary>
        /// Message time to live for messages sent by the
        /// writer if the transport supports it.
        /// </summary>
        [DataMember(Name = "MessageTtlTimespan", Order = 52,
            EmitDefaultValue = false)]
        public TimeSpan? MessageTtlTimespan { get; set; }

        /// <summary>
        /// Message retention setting for messages sent by
        /// the writer if the transport supports it.
        /// </summary>
        [DataMember(Name = "MessageRetention", Order = 53,
            EmitDefaultValue = false)]
        public bool? MessageRetention { get; set; }

        /// <summary>
        /// Default heartbeat interval in milliseconds
        /// </summary>
        [DataMember(Name = "DefaultHeartbeatInterval", Order = 54,
            EmitDefaultValue = false)]
        public int? DefaultHeartbeatInterval { get; set; }

        /// <summary>
        /// Default heartbeat interval for all nodes as duration. Takes
        /// precedence over <see cref="DefaultHeartbeatInterval"/> if
        /// defined.
        /// </summary>
        [DataMember(Name = "DefaultHeartbeatIntervalTimespan", Order = 55,
            EmitDefaultValue = false)]
        public TimeSpan? DefaultHeartbeatIntervalTimespan { get; set; }

        /// <summary>
        /// Default heartbeat behavior for all nodes
        /// </summary>
        [DataMember(Name = "DefaultHeartbeatBehavior", Order = 56,
            EmitDefaultValue = false)]
        public HeartbeatBehavior? DefaultHeartbeatBehavior { get; set; }

        /// <summary>
        /// Dump server diagnostics for the connection to enable
        /// advanced troubleshooting scenarios.
        /// </summary>
        [DataMember(Name = "DumpConnectionDiagnostics", Order = 98,
            EmitDefaultValue = false)]
        public bool? DumpConnectionDiagnostics { get; set; }

        /// <summary>
        /// The node to monitor in "ns=" syntax.
        /// </summary>
        [DataMember(Name = "NodeId", Order = 99,
            EmitDefaultValue = false)]
        public NodeIdModel? NodeId { get; set; }
    }
}
