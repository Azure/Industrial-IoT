// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing an entry in the node list
    /// </summary>
    [DataContract]
    public record class OpcNodeModel {

        /// <summary>
        /// Node Identifier
        /// </summary>
        [DataMember(Name = "Id", Order = 0,
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Sampling interval in milliseconds
        /// </summary>
        [DataMember(Name = "OpcSamplingInterval", Order = 1,
            EmitDefaultValue = false)]
        public int? OpcSamplingInterval { get; set; }

        /// <summary>
        /// OpcSamplingInterval as TimeSpan.
        /// </summary>
        [DataMember(Name = "OpcSamplingIntervalTimespan", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? OpcSamplingIntervalTimespan { get; set; }

        /// <summary>
        /// The identifier of the field in the dataset message.
        /// If not provided <see cref="DisplayName"/> is used.
        /// </summary>
        [DataMember(Name = "DataSetFieldId", Order = 3,
            EmitDefaultValue = false)]
        public string DataSetFieldId { get; set; }

        /// <summary>
        /// The identifier of the field in the dataset class.
        /// Allows correlation to the data set class.
        /// </summary>
        [DataMember(Name = "DataSetClassFieldId", Order = 4,
            EmitDefaultValue = false)]
        public Guid DataSetClassFieldId { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [DataMember(Name = "DisplayName", Order = 5,
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Queue Size for the monitored item on the server.
        /// Specifies how many values are queued on the server
        /// before undelivered ones are discarded.
        /// </summary>
        [DataMember(Name = "QueueSize", Order = 6,
            EmitDefaultValue = false)]
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Discard new values in the server queue instead of
        /// old values when no more room in queue.
        /// </summary>
        [DataMember(Name = "DiscardNew", Order = 7,
            EmitDefaultValue = false)]
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Data change trigger of the data change filter to apply.
        /// Does not apply to events.
        /// </summary>
        [DataMember(Name = "DataChangeTrigger", Order = 8,
            EmitDefaultValue = false)]
        public DataChangeTriggerType? DataChangeTrigger { get; set; }

        /// <summary>
        /// Deadband type of the data change filter to apply.
        /// Does not apply to events.
        /// </summary>
        [DataMember(Name = "DeadbandType", Order = 9,
            EmitDefaultValue = false)]
        public DeadbandType? DeadbandType { get; set; }

        /// <summary>
        /// Deadband value of the data change filter to apply.
        /// Does not apply to events
        /// </summary>
        [DataMember(Name = "DeadbandValue", Order = 10,
            EmitDefaultValue = false)]
        public double? DeadbandValue { get; set; }

        /// <summary>
        /// Event Filter to apply. When specified the node is
        /// assmed to be an event notifier node to subscribe to.
        /// </summary>
        [DataMember(Name = "EventFilter", Order = 11,
            EmitDefaultValue = false)]
        public EventFilterModel EventFilter { get; set; }

        /// <summary>
        /// Settings for pending condition handling
        /// </summary>
        [DataMember(Name = "ConditionHandling", Order = 12,
            EmitDefaultValue = false)]
        public ConditionHandlingOptionsModel ConditionHandling { get; set; }

        /// <summary>
        /// Heartbeat interval in seconds
        /// </summary>
        [DataMember(Name = "HeartbeatInterval", Order = 20,
            EmitDefaultValue = false)]
        public int? HeartbeatInterval { get; set; }

        /// <summary>
        /// Heartbeat interval as TimeSpan.
        /// </summary>
        [DataMember(Name = "HeartbeatIntervalTimespan", Order = 21,
            EmitDefaultValue = false)]
        public TimeSpan? HeartbeatIntervalTimespan { get; set; }

        /// <summary>
        /// Do not send the first value that is always provided
        /// by the server when the monitored item is created.
        /// </summary>
        [DataMember(Name = "SkipFirst", Order = 22,
            EmitDefaultValue = false)]
        public bool? SkipFirst { get; set; }

        /// <summary>
        /// Publishing interval in milliseconds
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
        /// Expanded Node identifier (same as <see cref="Id"/>)
        /// </summary>
        [DataMember(Name = "ExpandedNodeId", Order = 30,
            EmitDefaultValue = false)]
        public string ExpandedNodeId { get; set; }
    }
}
