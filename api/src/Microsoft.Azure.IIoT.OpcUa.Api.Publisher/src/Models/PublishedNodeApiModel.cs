// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A monitored and published node api model
    /// </summary>
    [DataContract]
    public class PublishedNodeApiModel {

        /// <summary> Node Identifier </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public string Id { get; set; }

        /// <summary> Expanded Node identifier </summary>
        [DataMember(Name = "expandedNodeId", Order = 1,
            EmitDefaultValue = false)]
        public string ExpandedNodeId { get; set; }

        /// <summary> Sampling interval </summary>
        [DataMember(Name = "opcSamplingInterval", Order = 2,
            EmitDefaultValue = false)]
        public int? OpcSamplingInterval { get; set; }

        /// <summary>
        /// OpcSamplingInterval as TimeSpan.
        /// </summary>
        [DataMember(Name = "opcSamplingIntervalTimespan", Order = 3,
            EmitDefaultValue = false)]
        public TimeSpan? OpcSamplingIntervalTimespan { get; set; }

        /// <summary> Publishing interval </summary>
        [DataMember(Name = "opcPublishingInterval", Order = 4,
            EmitDefaultValue = false)]
        public int? OpcPublishingInterval { get; set; }

        /// <summary>
        /// OpcPublishingInterval as TimeSpan.
        /// </summary>
        [DataMember(Name = "opcPublishingIntervalTimespan", Order = 5,
            EmitDefaultValue = false)]
        public TimeSpan? OpcPublishingIntervalTimespan { get; set; }

        /// <summary> DataSetFieldId </summary>
        [DataMember(Name = "dataSetFieldId", Order = 6,
            EmitDefaultValue = false)]
        public string DataSetFieldId { get; set; }

        /// <summary> Display name </summary>
        [DataMember(Name = "displayName", Order = 7,
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary> Heartbeat interval in seconds. </summary>
        [DataMember(Name = "heartbeatInterval", Order = 8,
            EmitDefaultValue = false)]
        public int? HeartbeatInterval { get; set; }

        /// <summary> Heartbeat interval as TimeSpan. </summary>
        [DataMember(Name = "heartbeatIntervalTimespan", Order = 9,
            EmitDefaultValue = false)]
        public TimeSpan? HeartbeatIntervalTimespan { get; set; }

        /// <summary> Skip first value </summary>
        [DataMember(Name = "skipFirst", Order = 10,
            EmitDefaultValue = false)]
        public bool? SkipFirst { get; set; }

        /// <summary> Queue size for monitored items </summary>
        [DataMember(Name = "queueSize", Order = 11,
            EmitDefaultValue = false)]
        public uint? QueueSize { get; set; }

        /// <summary> Data change trigger </summary>
        [DataMember(Name = "dataChangeTrigger", Order = 12,
            EmitDefaultValue = false)]
        public DataChangeTriggerType? DataChangeTrigger { get; set; }

        /// <summary> Deadband type </summary>
        [DataMember(Name = "deadbandType", Order = 13,
            EmitDefaultValue = false)]
        public DeadbandType? DeadbandType { get; set; }

        /// <summary> Deadband value </summary>
        [DataMember(Name = "deadbandValue", Order = 14,
            EmitDefaultValue = false)]
        public double? DeadbandValue { get; set; }

        /// <summary>Event Filter</summary>
        [DataMember(Name = "eventFilter", Order = 15,
            EmitDefaultValue = false)]
        public EventFilterApiModel EventFilter { get; set; }

        /// <summary> Settings for condition reporting </summary>
        [DataMember(Name = "conditionHandling", Order = 16,
            EmitDefaultValue = false)]
        public ConditionHandlingOptionsApiModel ConditionHandling { get; set; }

        /// <summary> Discard new values if queue is full </summary>
        [DataMember(Name = "discardNew", Order = 17,
            EmitDefaultValue = false)]
        public bool? DiscardNew { get; set; }

        /// <summary> Field id in dataset class </summary>
        [DataMember(Name = "dataSetClassFieldId", Order = 18,
            EmitDefaultValue = false)]
        public Guid DataSetClassFieldId { get; set; }
    }
}
