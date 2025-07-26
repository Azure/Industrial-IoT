// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines configuration for monitoring an OPC UA node.
    /// Contains settings for sampling, filtering, publishing
    /// behavior, and message routing. This model allows
    /// fine-grained control over how each node's data is collected
    /// and transmitted. Part of a PublishedNodesEntryModel's
    /// OpcNodes collection.
    /// </summary>
    [DataContract]
    public record class OpcNodeModel
    {
        /// <summary>
        /// The OPC UA node identifier string in standard notation.
        /// Format: ns={namespace};{type}={value} Required field that
        /// uniquely identifies the node to monitor. Examples:
        /// "ns=2;s=MyTag", "ns=0;i=2258" See OPC UA Part 3 for node
        /// ID format specifications.
        /// </summary>
        [DataMember(Name = "Id", Order = 0,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Server-side sampling rate in milliseconds. Determines how
        /// often the server checks for value changes. Default from
        /// DataSetSamplingInterval if not specified. Should be less
        /// than or equal to OpcPublishingInterval for effective
        /// sampling. Ignored when OpcSamplingIntervalTimespan is
        /// defined.
        /// </summary>
        [DataMember(Name = "OpcSamplingInterval", Order = 1,
            EmitDefaultValue = false)]
        public int? OpcSamplingInterval { get; set; }

        /// <summary>
        /// Server-side sampling rate as a TimeSpan. Takes precedence
        /// over OpcSamplingInterval if both are defined. Provides
        /// more precise control over timing than milliseconds.
        /// Example: "00:00:00.100" for 100ms sampling. Should be
        /// less than or equal to OpcPublishingIntervalTimespan for
        /// effective sampling.
        /// </summary>
        [DataMember(Name = "OpcSamplingIntervalTimespan", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? OpcSamplingIntervalTimespan { get; set; }

        /// <summary>
        /// Custom identifier for this node in dataset messages. Used
        /// as field name in message payloads if specified. Falls back
        /// to DisplayName if not provided. Helps correlate data with
        /// specific measurements or tags. Must be unique within a
        /// dataset writer.
        /// </summary>
        [DataMember(Name = "DataSetFieldId", Order = 3,
            EmitDefaultValue = false)]
        public string? DataSetFieldId { get; set; }

        /// <summary>
        /// Unique identifier for correlating fields with dataset
        /// class metadata. Links monitored item data with dataset
        /// class field definitions. Used to provide context and type
        /// information for the field. Must match corresponding field
        /// ID in dataset class metadata. Important for proper message
        /// decoding by subscribers.
        /// </summary>
        [DataMember(Name = "DataSetClassFieldId", Order = 4,
            EmitDefaultValue = false)]
        public Guid DataSetClassFieldId { get; set; }

        /// <summary>
        /// Human-readable name for the monitored item. Used as field
        /// identifier if DataSetFieldId not specified. Can be
        /// overridden by actual node DisplayName if
        /// FetchDisplayName=true. Helps identify data sources in
        /// messages and logs. Should be unique within a dataset for
        /// clear identification.
        /// </summary>
        [DataMember(Name = "DisplayName", Order = 5,
            EmitDefaultValue = false)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Size of the server-side queue for this monitored item.
        /// Controls how many values can be buffered during slow
        /// connections. Values are discarded according to DiscardNew
        /// when queue is full. Default is 1 unless otherwise
        /// configured. Larger queues help prevent data loss but use
        /// more server memory.
        /// </summary>
        [DataMember(Name = "QueueSize", Order = 6,
            EmitDefaultValue = false)]
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Controls queue overflow behavior for monitored items.
        /// True: Discard newest values when queue is full (LIFO).
        /// False: Discard oldest values when queue is full (FIFO,
        /// default). Use True to preserve historical data during
        /// connection issues. Use False to maintain current value
        /// accuracy.
        /// </summary>
        [DataMember(Name = "DiscardNew", Order = 7,
            EmitDefaultValue = false)]
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Specifies what triggers value change notifications. Only
        /// applies to DataItems (not Events or Alarms). Options:
        /// - Status: Changes in status or quality
        /// - StatusValue: Changes in value or status (default)
        /// - StatusValueTimestamp: Any change including timestamp
        /// Controls sensitivity of change detection.
        /// </summary>
        [DataMember(Name = "DataChangeTrigger", Order = 8,
            EmitDefaultValue = false)]
        public DataChangeTriggerType? DataChangeTrigger { get; set; }

        /// <summary>
        /// Deadband type of the data change filter to apply. Does not
        /// apply to events.
        /// </summary>
        [DataMember(Name = "DeadbandType", Order = 9,
            EmitDefaultValue = false)]
        public DeadbandType? DeadbandType { get; set; }

        /// <summary>
        /// Deadband value of the data change filter to apply. Does
        /// not apply to events
        /// </summary>
        [DataMember(Name = "DeadbandValue", Order = 10,
            EmitDefaultValue = false)]
        public double? DeadbandValue { get; set; }

        /// <summary>
        /// Event Filter to apply. When specified the node is assmed
        /// to be an event notifier node to subscribe to.
        /// </summary>
        [DataMember(Name = "EventFilter", Order = 11,
            EmitDefaultValue = false)]
        public EventFilterModel? EventFilter { get; set; }

        /// <summary>
        /// Settings for pending condition handling
        /// </summary>
        [DataMember(Name = "ConditionHandling", Order = 12,
            EmitDefaultValue = false)]
        public ConditionHandlingOptionsModel? ConditionHandling { get; set; }

        /// <summary>
        /// Relative path through the address space to reach target
        /// node. Sequence of browse names from starting node to
        /// target. Example: ["Objects", "Server", "Data",
        /// "Dynamic", "Scalar"]. Allows referencing nodes through
        /// hierarchical structure. Alternative to direct node ID
        /// addressing.
        /// </summary>
        [DataMember(Name = "BrowsePath", Order = 14,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? BrowsePath { get; set; }

        /// <summary>
        /// The OPC UA attribute to monitor on the node. Default is
        /// Value (13) for variables. Common alternatives:
        /// - DisplayName (4): Human-readable name
        /// - Description (5): Detailed information
        /// - BrowseName (3): Browse name.
        /// See OPC UA Part 6 for complete attribute list.
        /// </summary>
        [DataMember(Name = "AttributeId", Order = 15,
            EmitDefaultValue = false)]
        public NodeAttribute? AttributeId { get; set; }

        /// <summary>
        /// Range specification for array or string values. Format:
        /// "start:end" or "index". Examples: "0:3" (first 4
        /// elements), "7" (8th element) Allows monitoring specific
        /// array elements. Default: null (entire value monitored)
        /// </summary>
        [DataMember(Name = "IndexRange", Order = 16,
            EmitDefaultValue = false)]
        public string? IndexRange { get; set; }

        /// <summary>
        /// Custom routing topic/queue for this node's messages.
        /// Overrides writer and writer group queue settings. Enables
        /// node-specific message routing patterns. Messages are split
        /// into separate network messages when nodes have different
        /// topics. Format depends on transport (e.g., MQTT topic
        /// syntax).
        /// </summary>
        [DataMember(Name = "Topic", Order = 17,
            EmitDefaultValue = false)]
        public string? Topic { get; set; }

        /// <summary>
        /// Quality of service to use for the node. Overrides the
        /// writer and Writer group quality of service and together
        /// with queue name causes network messages to be split.
        /// </summary>
        [DataMember(Name = "QualityOfService", Order = 18,
            EmitDefaultValue = false)]
        public QoS? QualityOfService { get; set; }

        /// <summary>
        /// Controls heartbeat message generation for this node.
        /// Overrides DefaultHeartbeatBehavior from parent
        /// configuration. See HeartbeatBehavior enum for available
        /// options. Node-specific heartbeat settings allow
        /// fine-grained control over connection monitoring and state
        /// maintenance.
        /// </summary>
        [DataMember(Name = "HeartbeatBehavior", Order = 19,
            EmitDefaultValue = false)]
        public HeartbeatBehavior? HeartbeatBehavior { get; set; }

        /// <summary>
        /// Node-specific heartbeat interval in milliseconds.
        /// Overrides DefaultHeartbeatInterval from parent
        /// configuration. Controls how often heartbeat messages are
        /// generated. Set to 0 to disable heartbeats for this node.
        /// Ignored when HeartbeatIntervalTimespan is defined.
        /// </summary>
        [DataMember(Name = "HeartbeatInterval", Order = 20,
            EmitDefaultValue = false)]
        public int? HeartbeatInterval { get; set; }

        /// <summary>
        /// Node-specific heartbeat interval as TimeSpan. Takes
        /// precedence over HeartbeatInterval if both defined.
        /// Overrides DefaultHeartbeatIntervalTimespan setting.
        /// Provides more precise control over timing. Example:
        /// "00:00:10" for 10-second interval.
        /// </summary>
        [DataMember(Name = "HeartbeatIntervalTimespan", Order = 21,
            EmitDefaultValue = false)]
        public TimeSpan? HeartbeatIntervalTimespan { get; set; }

        /// <summary>
        /// Controls handling of initial value notification. True:
        /// Suppress first value from monitored item. False: Publish
        /// initial value (default). Useful when only changes are
        /// relevant. Server always sends initial value on creation.
        /// </summary>
        [DataMember(Name = "SkipFirst", Order = 22,
            EmitDefaultValue = false)]
        public bool? SkipFirst { get; set; }

        /// <summary>
        /// Client-side publishing rate in milliseconds. Controls how
        /// often the server sends notifications. Must be >=
        /// OpcSamplingInterval for proper operation. Overrides
        /// DataSetPublishingInterval when specified. Ignored if
        /// OpcPublishingIntervalTimespan is defined.
        /// </summary>
        [DataMember(Name = "OpcPublishingInterval", Order = 23,
            EmitDefaultValue = false)]
        public int? OpcPublishingInterval { get; set; }

        /// <summary>
        /// OpcPublishingInterval as TimeSpan.
        /// </summary>
        [DataMember(Name = "OpcPublishingIntervalTimespan", Order = 24,
            EmitDefaultValue = false)]
        public TimeSpan? OpcPublishingIntervalTimespan { get; set; }

        /// <summary>
        /// Use periodic reads instead of monitored items. True: Sample
        /// using CyclicRead service calls False: Use standard
        /// subscription monitoring (default) Useful for nodes that
        /// don't support monitoring or when consistent sampling
        /// timing is required. Consider CyclicReadMaxAge when
        /// enabled.
        /// </summary>
        [DataMember(Name = "UseCyclicRead", Order = 25,
            EmitDefaultValue = false)]
        public bool? UseCyclicRead { get; set; }

        /// <summary>
        /// Optimize node access using RegisterNodes service. True:
        /// Register node for faster subsequent reads False: Use
        /// direct node access (default) Can improve performance for
        /// frequently accessed nodes. Server must support
        /// RegisterNodes service.
        /// </summary>
        [DataMember(Name = "RegisterNode", Order = 26,
            EmitDefaultValue = false)]
        public bool? RegisterNode { get; set; }

        /// <summary>
        /// Retrieve node's DisplayName attribute on startup. True:
        /// Query and use actual display name False: Use configured
        /// DisplayName (default) Overrides DataSetFetchDisplayNames
        /// setting. Used for human-readable field identification.
        /// </summary>
        [DataMember(Name = "FetchDisplayName", Order = 27,
            EmitDefaultValue = false)]
        public bool? FetchDisplayName { get; set; }

        /// <summary>
        /// Configuration for model change tracking nodes
        /// </summary>
        [DataMember(Name = "ModelChangeHandling", Order = 28,
            EmitDefaultValue = false)]
        public ModelChangeHandlingOptionsModel? ModelChangeHandling { get; set; }

        /// <summary>
        /// Collection of dependent nodes triggered by this node. Read
        /// atomically when parent node changes. Limited to one level
        /// of triggering (no cascading). Useful for maintaining data
        /// consistency between related measurements. Changes to
        /// triggered nodes must be made through parent node's API
        /// calls.
        /// </summary>
        [DataMember(Name = "TriggeredNodes", Order = 29,
            EmitDefaultValue = false)]
        public IReadOnlyList<OpcNodeModel>? TriggeredNodes { get; set; }

        /// <summary>
        /// Maximum age for cached values in cyclic reads. Specified in
        /// milliseconds. Default: 0 (no caching) Only applies when
        /// UseCyclicRead is true. Server may return cached value if
        /// within max age. Helps reduce server load in high-frequency
        /// reads. Ignored when CyclicReadMaxAgeTimespan is defined.
        /// </summary>
        [DataMember(Name = "CyclicReadMaxAge", Order = 31,
            EmitDefaultValue = false)]
        public int? CyclicReadMaxAge { get; set; }

        /// <summary>
        /// Maximum age for cached values in cyclic reads as TimeSpan.
        /// Takes precedence over CyclicReadMaxAge if both defined.
        /// Only applies when UseCyclicRead is true. Default:
        /// "00:00:00" (no caching) Example: "00:00:00.500" for 500ms
        /// max cache age. Helps optimize read performance vs data
        /// freshness.
        /// </summary>
        [DataMember(Name = "CyclicReadMaxAgeTimespan", Order = 32,
            EmitDefaultValue = false)]
        public TimeSpan? CyclicReadMaxAgeTimespan { get; set; }

        /// <summary>
        /// A type definition id that references a well known opc ua
        /// type definition node for the variable represented by this
        /// node entry.
        /// </summary>
        [DataMember(Name = "TypeDefinitionId", Order = 33,
            EmitDefaultValue = false)]
        public string? TypeDefinitionId { get; set; }

        /// <summary>
        /// If the node is a method this is the method metadata that
        /// represents the input and output arguments of the method.
        /// </summary>
        [DataMember(Name = "MethodMetadata", Order = 34,
            EmitDefaultValue = false)]
        public MethodMetadataModel? MethodMetadata { get; set; }

        /// <summary>
        /// Alternative node identifier with full namespace URI. Same
        /// as Id but uses complete namespace URI instead of index.
        /// Format: "nsu={uri};{type}={value}" Example:
        /// "nsu=http://opcfoundation.org/UA/;i=2258" Provides more
        /// portable node identification across servers.
        /// </summary>
        [DataMember(Name = "ExpandedNodeId", Order = 40,
            EmitDefaultValue = false)]
        public string? ExpandedNodeId { get; set; }
    }
}
