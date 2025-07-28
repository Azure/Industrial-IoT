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
    /// <para>
    /// Configuration model for OPC UA Publisher that defines how
    /// OPC UA nodes are published to messaging systems. Used to
    /// configure connections to OPC UA servers, setup node
    /// monitoring, and control message publishing.
    /// </para>
    /// <para>
    /// Key features:
    /// - Endpoint configuration and security settings
    /// - Writer group and dataset organization
    /// - Publishing intervals and sampling controls
    /// - Message batching and triggering
    /// - Subscription and monitoring options
    /// - Heartbeat and watchdog behaviors
    /// - Security modes and authentication
    /// </para>
    /// <para>
    /// For detailed configuration options, see individual
    /// properties.
    /// </para>
    /// </summary>
    [DataContract]
    public record class PublishedNodesEntryModel
    {
        /// <summary>
        /// A monotonically increasing number identifying the change
        /// version. At this point the version number is informational
        /// only, but should be provided in API requests if available.
        /// Not used inside file based configuration.
        /// </summary>
        [DataMember(Name = "Version", Order = 0,
            EmitDefaultValue = false)]
        public uint? Version { get; set; }

        /// <summary>
        /// The time the Publisher configuration was last updated.
        /// Read only and informational only.
        /// </summary>
        [DataMember(Name = "LastChangeDateTime", Order = 1,
            EmitDefaultValue = false)]
        public DateTimeOffset? LastChangeDateTime { get; set; }

        /// <summary>
        /// The unique identifier for a data set writer used to collect
        /// OPC UA nodes to be semantically grouped and published with
        /// the same publishing interval. When not specified, uses a
        /// string representing the common publishing interval of the
        /// nodes in the data set collection. This attribute uniquely
        /// identifies a data set within a DataSetWriterGroup. The
        /// uniqueness is determined using the provided DataSetWriterId
        /// and the publishing interval of the grouped OpcNodes.
        /// </summary>
        [DataMember(Name = "DataSetWriterId", Order = 2,
            EmitDefaultValue = false)]
        public string? DataSetWriterId { get; set; }

        /// <summary>
        /// The data set writer group collecting datasets defined for a
        /// certain endpoint. This attribute is used to identify the
        /// session opened into the server. The default value consists
        /// of the EndpointUrl string, followed by a deterministic hash
        /// composed of the EndpointUrl, UseSecurity,
        /// OpcAuthenticationMode, UserName and Password attributes.
        /// </summary>
        [DataMember(Name = "DataSetWriterGroup", Order = 3,
            EmitDefaultValue = false)]
        public string? DataSetWriterGroup { get; set; }

        /// <summary>
        /// The DataSet collection grouping the nodes to be published
        /// for the specific DataSetWriter. Each node can specify
        /// monitoring parameters including sampling intervals, deadband
        /// settings, and event filtering options. Contains variable
        /// nodes or event notifiers to monitor.
        /// </summary>
        [DataMember(Name = "OpcNodes", Order = 4,
            EmitDefaultValue = false)]
        public IList<OpcNodeModel>? OpcNodes { get; set; }

        /// <summary>
        /// The optional dataset class id as it shall appear in dataset
        /// messages and dataset metadata. Used to uniquely identify the
        /// type of dataset being published. Default: Guid.Empty
        /// </summary>
        [DataMember(Name = "DataSetClassId", Order = 5,
            EmitDefaultValue = false)]
        public Guid DataSetClassId { get; set; }

        /// <summary>
        /// The optional name of the dataset as it will appear in the
        /// dataset metadata. Used for identification and organization
        /// of datasets.
        /// </summary>
        [DataMember(Name = "DataSetName", Order = 6,
            EmitDefaultValue = false)]
        public string? DataSetName { get; set; }

        /// <summary>
        /// The publishing interval used for a grouped set of nodes
        /// under a certain DataSetWriter, expressed in milliseconds.
        /// When a specific node underneath DataSetWriter defines
        /// OpcPublishingInterval (or Timespan), its value will
        /// overwrite this interval and potentially split the data set
        /// writer into more than one subscription. Ignored when
        /// DataSetPublishingIntervalTimespan is present.
        /// </summary>
        [DataMember(Name = "DataSetPublishingInterval", Order = 7,
            EmitDefaultValue = false)]
        public int? DataSetPublishingInterval { get; set; }

        /// <summary>
        /// The publishing interval for a dataset writer, expressed as a
        /// TimeSpan value. Takes precedence over
        /// DataSetPublishingInterval if defined. Provides more precise
        /// control over timing than milliseconds. When overridden by
        /// node-specific intervals, the writer may split into multiple
        /// subscriptions.
        /// </summary>
        [DataMember(Name = "DataSetPublishingIntervalTimespan", Order = 8,
            EmitDefaultValue = false)]
        public TimeSpan? DataSetPublishingIntervalTimespan { get; set; }

        /// <summary>
        /// Controls key frame insertion frequency in the message
        /// stream. A key frame contains all current values, while
        /// delta frames only contain changes. Setting this ensures
        /// periodic complete state updates, useful for late-joining
        /// consumers or state synchronization. Key frames can also
        /// include configured DataSetExtensionFields for additional
        /// context. Default: 0 (key frames disabled)
        /// </summary>
        [DataMember(Name = "DataSetKeyFrameCount", Order = 9,
            EmitDefaultValue = false)]
        public uint? DataSetKeyFrameCount { get; set; }

        /// <summary>
        /// The interval in milliseconds at which metadata messages are
        /// sent, even when the metadata has not changed. Only applies
        /// when metadata messaging is supported or explicitly enabled.
        /// Ignored when MetaDataUpdateTimeTimespan is defined.
        /// </summary>
        [DataMember(Name = "MetaDataUpdateTime", Order = 10,
            EmitDefaultValue = false)]
        public int? MetaDataUpdateTime { get; set; }

        /// <summary>
        /// The interval as TimeSpan at which metadata messages are
        /// sent, even when metadata has not changed. Takes precedence
        /// over MetaDataUpdateTime if both are defined. Only applies
        /// when metadata messaging is supported or explicitly enabled.
        /// </summary>
        [DataMember(Name = "MetaDataUpdateTimeTimespan", Order = 11,
            EmitDefaultValue = false)]
        public TimeSpan? MetaDataUpdateTimeTimespan { get; set; }

        /// <summary>
        /// Controls whether to send keep alive messages for this
        /// dataset when a subscription keep alive notification is
        /// received. Keep alive messages help maintain connection
        /// status awareness. Only valid if the messaging mode supports
        /// keep alive messages. Default: false
        /// </summary>
        [DataMember(Name = "SendKeepAliveDataSetMessages", Order = 12,
            EmitDefaultValue = false)]
        public bool? SendKeepAliveDataSetMessages { get; set; }

        /// <summary>
        /// The required OPC UA server endpoint URL to connect to. This
        /// is the only required field in the configuration. Format:
        /// "opc.tcp://host:port/path"
        /// </summary>
        [DataMember(Name = "EndpointUrl", Order = 13)]
        [Required]
        public required string EndpointUrl { get; set; }

        /// <summary>
        /// Defines how many publishing timer expirations to wait
        /// before sending a keep-alive message when no notifications
        /// are pending. Works with SendKeepAliveDataSetMessages to
        /// maintain connection awareness. Keep-alive messages help
        /// detect connection issues even when no data changes are
        /// occurring.
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
        /// Optional key-value pairs inserted into key frame and
        /// metadata messages in the same data set. Values are
        /// formatted using OPC UA Variant JSON encoding. Used to add
        /// contextual information to datasets.
        /// </summary>
        [DataMember(Name = "DataSetExtensionFields", Order = 17,
            EmitDefaultValue = false)]
        public IDictionary<string, VariantValue>? DataSetExtensionFields { get; set; }

        /// <summary>
        /// The specific security mode to use for the specified
        /// endpoint. Overrides <see cref="UseSecurity"/> setting. If
        /// the security mode is not available with any configured
        /// security policy connectivity will fail. Default:
        /// <see cref="SecurityMode.NotNone"/> if <see cref="UseSecurity"/>
        /// is <c>true</c>, otherwise <see cref="SecurityMode.None"/>
        /// </summary>
        [DataMember(Name = "EndpointSecurityMode", Order = 18,
            EmitDefaultValue = false)]
        public SecurityMode? EndpointSecurityMode { get; set; }

        /// <summary>
        /// The security policy URI to use for the endpoint connection.
        /// Overrides UseSecurity setting and refines
        /// EndpointSecurityMode choice. Examples include
        /// "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256".
        /// If the specified policy is not available with the chosen
        /// security mode, connectivity will fail. This allows enforcing
        /// specific security requirements.
        /// </summary>
        [DataMember(Name = "EndpointSecurityPolicy", Order = 19,
            EmitDefaultValue = false)]
        public string? EndpointSecurityPolicy { get; set; }

        /// <summary>
        /// The messaging mode to use when publishing the data sets in
        /// the writer group. Supported modes include:
        /// - PubSub: OPC UA PubSub standard format
        /// - Samples: Simple JSON telemetry format (default)
        /// - FullNetworkMessages: Complete network message format
        /// - DataSetMessages: Dataset-only message format
        /// - RawDataSets: Minimal dataset format
        /// See messageformats.md documentation for details on each
        /// format.
        /// </summary>
        [DataMember(Name = "MessagingMode", Order = 20,
            EmitDefaultValue = false)]
        public MessagingMode? MessagingMode { get; set; }

        /// <summary>
        /// The encoding format to use for messages in the writer
        /// group. Options include:
        /// - Json: Standard JSON encoding
        /// - JsonReversible: Lossless JSON encoding
        /// - JsonGzip: Compressed JSON
        /// - JsonReversibleGzip: Compressed lossless JSON
        /// - Uadp: Binary OPC UA encoding (most efficient)
        /// Choose based on bandwidth requirements and receiver
        /// capabilities.
        /// </summary>
        [DataMember(Name = "MessageEncoding", Order = 21,
            EmitDefaultValue = false)]
        public MessageEncoding? MessageEncoding { get; set; }

        /// <summary>
        /// The number of notifications that are queued before a
        /// network message is generated. Controls message batching for
        /// optimizing network traffic vs latency. For historic reasons
        /// the default value is 50 unless otherwise configured via the
        /// --bs command line option.
        /// </summary>
        [DataMember(Name = "BatchSize", Order = 22,
            EmitDefaultValue = false)]
        public uint? BatchSize { get; set; }

        /// <summary>
        /// The interval at which batched network messages are
        /// published, in milliseconds. Messages are published when
        /// this interval elapses or when BatchSize is reached. For
        /// historic reasons the default is 10 seconds unless
        /// configured via --bi. Ignored when
        /// BatchTriggerIntervalTimespan is specified. Used with
        /// BatchSize to optimize network traffic vs latency.
        /// </summary>
        [DataMember(Name = "BatchTriggerInterval", Order = 23,
            EmitDefaultValue = false)]
        public int? BatchTriggerInterval { get; set; }

        /// <summary>
        /// The interval at which batched network messages are
        /// published, as a TimeSpan. Takes precedence over
        /// BatchTriggerInterval if both are defined. Messages are
        /// published when this interval elapses or when BatchSize is
        /// reached. Provides more precise control over publishing
        /// timing than millisecond interval. Used with BatchSize to
        /// optimize network traffic vs latency.
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
        /// Configure the dataset routing behavior for the contained
        /// nodes.
        /// </summary>
        [DataMember(Name = "DataSetRouting", Order = 26,
            EmitDefaultValue = false)]
        public DataSetRoutingMode? DataSetRouting { get; set; }

        /// <summary>
        /// Overrides the default writer group topic template for
        /// message routing. Used to customize where messages from this
        /// writer group are published. Particularly useful when
        /// publishing to MQTT topics or message queues where specific
        /// routing patterns are needed.
        /// </summary>
        [DataMember(Name = "WriterGroupQueueName", Order = 27,
            EmitDefaultValue = false)]
        public string? WriterGroupQueueName { get; set; }

        /// <summary>
        /// The quality of service level for message delivery. Options:
        /// - AtMostOnce: Fire and forget delivery
        /// - AtLeastOnce: Guaranteed delivery, possible duplicates
        ///   (default)
        /// - ExactlyOnce: Guaranteed single delivery
        /// QoS is only applied if supported by the chosen transport.
        /// </summary>
        [DataMember(Name = "WriterGroupQualityOfService", Order = 28,
            EmitDefaultValue = false)]
        public QoS? WriterGroupQualityOfService { get; set; }

        /// <summary>
        /// Specifies the transport technology to use for publishing
        /// messages. Controls how messages are delivered to the
        /// messaging system. Available transports can be found in
        /// transports.md documentation. Common options include Azure
        /// IoT Hub (default), MQTT, and MQTT v5.
        /// </summary>
        [DataMember(Name = "WriterGroupTransport", Order = 29,
            EmitDefaultValue = false)]
        public WriterGroupTransport? WriterGroupTransport { get; set; }

        /// <summary>
        /// Controls whether to use a secure OPC UA transport mode to
        /// establish a session. When true, defaults to
        /// SecurityMode.NotNone which requires signed or encrypted
        /// communication. When false, uses SecurityMode.None with no
        /// security. Can be overridden by EndpointSecurityMode and
        /// EndpointSecurityPolicy settings. Use encrypted
        /// communication whenever possible to protect credentials and
        /// data.
        /// </summary>
        [DataMember(Name = "UseSecurity", Order = 30)]
        public bool? UseSecurity { get; set; }

        /// <summary>
        /// Specifies the authentication mode for connecting to the OPC
        /// UA server. Supported modes:
        /// - Anonymous: No authentication (default)
        /// - UsernamePassword: Username and password authentication
        /// - Certificate: Certificate-based authentication using X.509
        ///   certificates
        /// When using credentials or certificates, encrypted
        /// communication should be enabled via UseSecurity or
        /// EndpointSecurityMode to protect secrets. For certificate
        /// auth, the certificate must be in the User certificate
        /// store.
        /// </summary>
        [DataMember(Name = "OpcAuthenticationMode", Order = 31)]
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// The encrypted username for authentication when
        /// OpcAuthenticationMode is UsernamePassword. Encrypted
        /// credentials at rest can be enforced using the --fce command
        /// line option. Version 2.6+ stores credentials in plain text
        /// by default, while 2.5 and below always encrypted them.
        /// </summary>
        [DataMember(Name = "EncryptedAuthUsername", Order = 32,
            EmitDefaultValue = false)]
        public string? EncryptedAuthUsername { get; set; }

        /// <summary>
        /// The encrypted password for authentication when
        /// OpcAuthenticationMode is UsernamePassword. For certificate
        /// authentication, contains the password to access the private
        /// key. Encrypted credentials at rest can be enforced using
        /// the --fce command line option. Version 2.6+ stores
        /// credentials in plain text by default, while 2.5 and below
        /// always encrypted them.
        /// </summary>
        [DataMember(Name = "EncryptedAuthPassword", Order = 33,
            EmitDefaultValue = false)]
        public string? EncryptedAuthPassword { get; set; }

        /// <summary>
        /// The plaintext username for UsernamePassword authentication,
        /// or the subject name of the certificate for Certificate
        /// authentication. When using Certificate mode, this refers to
        /// a certificate in the User certificate store of the PKI
        /// configuration.
        /// </summary>
        [DataMember(Name = "OpcAuthenticationUsername", Order = 34,
            EmitDefaultValue = false)]
        public string? OpcAuthenticationUsername { get; set; }

        /// <summary>
        /// The plaintext password for UsernamePassword authentication,
        /// or the password protecting the private key for Certificate
        /// authentication. For Certificate mode, this must match the
        /// password used when adding the certificate to the PKI store.
        /// </summary>
        [DataMember(Name = "OpcAuthenticationPassword", Order = 35,
            EmitDefaultValue = false)]
        public string? OpcAuthenticationPassword { get; set; }

        /// <summary>
        /// Overrides the writer group queue name at the individual
        /// writer level. When specified, causes network messages to be
        /// split across different queues. The split also takes QoS
        /// settings into account, allowing fine-grained control over
        /// message routing and delivery guarantees.
        /// </summary>
        [DataMember(Name = "QueueName", Order = 36,
            EmitDefaultValue = false)]
        public string? QueueName { get; set; }

        /// <summary>
        /// The queue name to use for metadata messages from this
        /// writer. Overrides the default metadata topic template.
        /// Allows routing metadata to specific destinations separate
        /// from data messages.
        /// </summary>
        [DataMember(Name = "MetaDataQueueName", Order = 37,
            EmitDefaultValue = false)]
        public string? MetaDataQueueName { get; set; }

        /// <summary>
        /// The QoS level for this writer's messages. Overrides the
        /// writer group QoS setting. When specified with a custom
        /// queue name, causes network messages to be split with
        /// different delivery guarantees.
        /// </summary>
        [DataMember(Name = "QualityOfService", Order = 38,
            EmitDefaultValue = false)]
        public QoS? QualityOfService { get; set; }

        /// <summary>
        /// Specifies how many partitions to split the writer group
        /// into when publishing to target topics. Used to distribute
        /// message load and enable parallel processing by consumers.
        /// Default is 1 partition. Particularly useful for
        /// high-throughput scenarios or when using partitioned
        /// queues/topics in the messaging system.
        /// </summary>
        [DataMember(Name = "WriterGroupPartitions", Order = 39,
            EmitDefaultValue = false)]
        public int? WriterGroupPartitions { get; set; }

        /// <summary>
        /// Controls whether subscription transfer is disabled during
        /// reconnect. When false (default), attempts to transfer
        /// subscriptions on reconnect to maintain data continuity. Set
        /// to true to fix interoperability issues with servers that
        /// don't support subscription transfer. Can be configured
        /// globally via command line options.
        /// </summary>
        [DataMember(Name = "DisableSubscriptionTransfer", Order = 40,
            EmitDefaultValue = false)]
        public bool? DisableSubscriptionTransfer { get; set; }

        /// <summary>
        /// Controls whether to republish missed values after a
        /// subscription is transferred during reconnect handling. Only
        /// applies when DisableSubscriptionTransfer is false. Helps
        /// ensure no data is lost during connection interruptions.
        /// Default: true
        /// </summary>
        [DataMember(Name = "RepublishAfterTransfer", Order = 41,
            EmitDefaultValue = false)]
        public bool? RepublishAfterTransfer { get; set; }

        /// <summary>
        /// The timeout duration used to monitor whether monitored
        /// items in the subscription are continuously reporting fresh
        /// data. This watchdog mechanism helps detect stale data or
        /// connectivity issues. When this timeout expires, the
        /// configured DataSetWriterWatchdogBehavior is triggered based
        /// on OpcNodeWatchdogCondition. Expressed as a TimeSpan value.
        /// </summary>
        [DataMember(Name = "OpcNodeWatchdogTimespan", Order = 42,
            EmitDefaultValue = false)]
        public TimeSpan? OpcNodeWatchdogTimespan { get; set; }

        /// <summary>
        /// Defines what action to take when the watchdog timer
        /// triggers. Available behaviors:
        /// - Diagnostic: Log the event only
        /// - Reset: Reset the subscription
        /// - FailFast: Terminate the connection
        /// - ExitProcess: Shut down the publisher
        /// The behavior is executed when watchdog conditions are met
        /// according to OpcNodeWatchdogCondition. Can be configured
        /// via --dwb command line option.
        /// </summary>
        [DataMember(Name = "DataSetWriterWatchdogBehavior", Order = 43,
            EmitDefaultValue = false)]
        public SubscriptionWatchdogBehavior? DataSetWriterWatchdogBehavior { get; set; }

        /// <summary>
        /// Specifies the condition that triggers the watchdog
        /// behavior. Options:
        /// - WhenAnyAreLate: Execute when any monitored item is late
        ///   (default)
        /// - WhenAllAreLate: Execute only when all items are late
        /// Can be configured via --mwc command line option. Used in
        /// conjunction with OpcNodeWatchdogTimespan and
        /// DataSetWriterWatchdogBehavior to implement monitoring and
        /// recovery strategies.
        /// </summary>
        [DataMember(Name = "OpcNodeWatchdogCondition", Order = 44,
            EmitDefaultValue = false)]
        public MonitoredItemWatchdogCondition? OpcNodeWatchdogCondition { get; set; }

        /// <summary>
        /// Default sampling interval in milliseconds for all monitored
        /// items in the dataset. Used if individual nodes don't
        /// specify their own sampling interval. Follows OPC UA
        /// specification for sampling behavior. Ignored when
        /// DataSetSamplingIntervalTimespan is present. Defaults to
        /// value configured via --oi command line option.
        /// </summary>
        [DataMember(Name = "DataSetSamplingInterval", Order = 45,
            EmitDefaultValue = false)]
        public int? DataSetSamplingInterval { get; set; }

        /// <summary>
        /// Default sampling interval as TimeSpan for all monitored
        /// items in the dataset. Takes precedence over
        /// DataSetSamplingInterval if both are defined. Used if
        /// individual nodes don't specify their own sampling interval.
        /// Provides more precise control over sampling timing. Follows
        /// OPC UA specification for sampling behavior.
        /// </summary>
        [DataMember(Name = "DataSetSamplingIntervalTimespan", Order = 46,
            EmitDefaultValue = false)]
        public TimeSpan? DataSetSamplingIntervalTimespan { get; set; }

        /// <summary>
        /// Controls whether to fetch display names of monitored
        /// variable nodes and use those inside messages as field
        /// names. When true, fetches display names for all nodes. If
        /// false, uses DisplayName value if provided; if not provided,
        /// uses the node id. Can be configured via --fd command line
        /// option.
        /// </summary>
        [DataMember(Name = "DataSetFetchDisplayNames", Order = 47,
            EmitDefaultValue = false)]
        public bool? DataSetFetchDisplayNames { get; set; }

        /// <summary>
        /// Time-to-live duration for messages sent through the writer
        /// group. Only applied if the transport technology supports
        /// message TTL. After this duration expires, messages may be
        /// discarded by the messaging system. Used to prevent stale
        /// data from being processed by consumers.
        /// </summary>
        [DataMember(Name = "WriterGroupMessageTtlTimepan", Order = 49,
            EmitDefaultValue = false)]
        public TimeSpan? WriterGroupMessageTtlTimepan { get; set; }

        /// <summary>
        /// Controls whether messages should be retained by the
        /// messaging system. Only applied if the transport technology
        /// supports message retention. When true, messages are kept by
        /// the broker even after delivery. Useful for late-joining
        /// subscribers to receive the last known values.
        /// </summary>
        [DataMember(Name = "WriterGroupMessageRetention", Order = 50,
            EmitDefaultValue = false)]
        public bool? WriterGroupMessageRetention { get; set; }

        /// <summary>
        /// Time-to-live duration for messages sent by this specific
        /// writer. Overrides WriterGroupMessageTtlTimespan at the
        /// individual writer level. Only applied if the transport
        /// technology supports message TTL. Allows different TTL
        /// settings for different types of data.
        /// </summary>
        [DataMember(Name = "MessageTtlTimespan", Order = 52,
            EmitDefaultValue = false)]
        public TimeSpan? MessageTtlTimespan { get; set; }

        /// <summary>
        /// Controls message retention for this specific writer.
        /// Overrides WriterGroupMessageRetention at the individual
        /// writer level. Only applied if the transport technology
        /// supports retention. Together with QueueName, allows
        /// splitting messages across different queues with different
        /// retention policies.
        /// </summary>
        [DataMember(Name = "MessageRetention", Order = 53,
            EmitDefaultValue = false)]
        public bool? MessageRetention { get; set; }

        /// <summary>
        /// The interval in milliseconds at which to publish heartbeat
        /// messages. Heartbeat acts like a watchdog that fires after
        /// this interval has passed and no new value has been
        /// received. A value of 0 disables heartbeat. Ignored when
        /// DefaultHeartbeatIntervalTimespan is defined. See
        /// heartbeat.md for detailed behavior documentation.
        /// </summary>
        [DataMember(Name = "DefaultHeartbeatInterval", Order = 54,
            EmitDefaultValue = false)]
        public int? DefaultHeartbeatInterval { get; set; }

        /// <summary>
        /// The heartbeat interval as TimeSpan for all nodes in this
        /// dataset. Takes precedence over DefaultHeartbeatInterval if
        /// defined. Controls how often heartbeat messages are
        /// published when no value changes occur.
        /// </summary>
        [DataMember(Name = "DefaultHeartbeatIntervalTimespan", Order = 55,
            EmitDefaultValue = false)]
        public TimeSpan? DefaultHeartbeatIntervalTimespan { get; set; }

        /// <summary>
        /// Configures how heartbeat messages are handled for all
        /// nodes. Supported behaviors:
        /// - WatchdogLKV: Last Known Value semantics (default)
        /// - WatchdogLKG: Last Known Good value semantics
        /// - PeriodicLKV: Continuous periodic sending of last known
        ///   value
        /// - PeriodicLKG: Continuous periodic sending of last good
        ///   value
        /// - PeriodicLKVDropValue: Periodic reporting only, drop
        ///   out-of-period values
        /// - PeriodicLKGDropValue: Periodic reporting only, drop
        ///   out-of-period values
        /// Can be configured via --hbb command line option.
        /// </summary>
        [DataMember(Name = "DefaultHeartbeatBehavior", Order = 56,
            EmitDefaultValue = false)]
        public HeartbeatBehavior? DefaultHeartbeatBehavior { get; set; }

        /// <summary>
        /// Contains an uri identifier that allows correlation of the writer
        /// data set source into other systems. Will be used as part of
        /// cloud events header if enabled.
        /// </summary>
        [DataMember(Name = "DataSetSourceUri", Order = 57,
            EmitDefaultValue = false)]
        public string? DataSetSourceUri { get; set; }

        /// <summary>
        /// Contains an identifier that allows correlation of the writer
        /// group into other systems in the context of the source. Will be
        /// used as part of cloud events header if enabled.
        /// </summary>
        [DataMember(Name = "DataSetSubject", Order = 58,
            EmitDefaultValue = false)]
        public string? DataSetSubject { get; set; }

        /// <summary>
        /// Additional properties of the writer group that should be retained
        /// with the configuration.
        /// </summary>
        [DataMember(Name = "WriterGroupProperties", Order = 59,
            EmitDefaultValue = false)]
        public Dictionary<string, VariantValue>? WriterGroupProperties { get; set; }

        /// <summary>
        /// A type definition id that references a well known opc ua type
        /// definition node for the dataset represented by this entry.
        /// If set it is used in context of cloud events to specify a concrete
        /// type of dataset message in the cloud events type header.
        /// </summary>
        [DataMember(Name = "DataSetType", Order = 60,
            EmitDefaultValue = false)]
        public string? DataSetType { get; set; }

        /// <summary>
        /// A root node that all nodes that use a non rooted browse paths in the
        /// dataset should start from.
        /// </summary>
        [DataMember(Name = "DataSetRootNodeId", Order = 61,
            EmitDefaultValue = false)]
        public string? DataSetRootNodeId { get; set; }

        /// <summary>
        /// A node that represents the writer group in the server address space.
        /// This is the instance id of the root node from which all datasets
        /// originate. It is informational only and would not need to be
        /// configured
        /// </summary>
        [DataMember(Name = "WriterGroupRootNodeId", Order = 62,
            EmitDefaultValue = false)]
        public string? WriterGroupRootNodeId { get; set; }

        /// <summary>
        /// A type that is attached to the writer group and explains the shape
        /// of the writer group. It is the type definition id of the writer
        /// group root node id. It is informational only and would not need
        /// to be configured
        /// </summary>
        [DataMember(Name = "WriterGroupType", Order = 63,
            EmitDefaultValue = false)]
        public string? WriterGroupType { get; set; }

        /// <summary>
        /// Metadata retention setting for the dataset writer.
        /// </summary>
        [DataMember(Name = "MetaDataRetention", Order = 65,
            EmitDefaultValue = false)]
        public bool? MetaDataRetention { get; set; }

        /// <summary>
        /// Metadata time-to-live duration for the dataset writer.
        /// </summary>
        [DataMember(Name = "MetaDataTtlTimespan", Order = 66,
            EmitDefaultValue = false)]
        public TimeSpan? MetaDataTtlTimespan { get; set; }

        /// <summary>
        /// When sending of keep alive messages is enabled, this
        /// flag controls whether the keep alive messages are sent
        /// as key frames. Key frames contain all current values.
        /// </summary>
        [DataMember(Name = "SendKeepAliveAsKeyFrameMessages", Order = 67,
            EmitDefaultValue = false)]
        public bool? SendKeepAliveAsKeyFrameMessages { get; set; }

        /// <summary>
        /// Set a publisher id to use that is different form the
        /// global publisher identity.
        /// </summary>
        [DataMember(Name = "PublisherId", Order = 68,
            EmitDefaultValue = false)]
        public string? PublisherId { get; set; }

        /// <summary>
        /// Enables detailed server diagnostics logging for the
        /// connection. When enabled, provides additional diagnostic
        /// information useful for troubleshooting connectivity,
        /// authentication, and subscription issues. The diagnostics
        /// data is included in the publisher's logs. Default: false
        /// </summary>
        [DataMember(Name = "DumpConnectionDiagnostics", Order = 98,
            EmitDefaultValue = false)]
        public bool? DumpConnectionDiagnostics { get; set; }

        /// <summary>
        /// Specifies a single node to monitor using namespace index
        /// syntax ("ns="). Alternative to using OpcNodes list for
        /// simple monitoring scenarios.
        /// </summary>
        [DataMember(Name = "NodeId", Order = 99,
            EmitDefaultValue = false)]
        public NodeIdModel? NodeId { get; set; }
    }
}
