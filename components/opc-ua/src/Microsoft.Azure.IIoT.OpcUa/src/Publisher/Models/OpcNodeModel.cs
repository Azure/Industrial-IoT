// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing an entry in the node list
    /// </summary>
    [DataContract]
    public class OpcNodeModel {

        /// <summary> Node Identifier </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string Id { get; set; }

        /// <summary> Expanded Node identifier </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string ExpandedNodeId { get; set; }

        /// <summary> Sampling interval in milliseconds </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int? OpcSamplingInterval { get; set; }

        /// <summary> OpcSamplingInterval as TimeSpan. </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public TimeSpan? OpcSamplingIntervalTimespan { get; set; }

        /// <summary> Publishing interval in milliseconds </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int? OpcPublishingInterval { get; set; }

        /// <summary> OpcPublishingInterval as TimeSpan. </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public TimeSpan? OpcPublishingIntervalTimespan { get; set; }

        /// <summary> DataSetFieldId </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string DataSetFieldId { get; set; }

        /// <summary> DataSetClassFieldId </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public Guid DataSetClassFieldId { get; set; }

        /// <summary> Display name </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string DisplayName { get; set; }

        /// <summary> Heartbeat interval in seconds </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int? HeartbeatInterval { get; set; }

        /// <summary> Heartbeat interval as TimeSpan. </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public TimeSpan? HeartbeatIntervalTimespan { get; set; }

        /// <summary> Skip first value </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public bool? SkipFirst { get; set; }

        /// <summary> Queue Size for the monitored item </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public uint? QueueSize { get; set; }

        /// <summary> Discard new values instead of old </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public bool? DiscardNew { get; set; }

        /// <summary> Data change filter </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public DataChangeTriggerType? DataChangeTrigger { get; set; }

        /// <summary> Deadband type </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public DeadbandType? DeadbandType { get; set; }

        /// <summary> Deadband value </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public double? DeadbandValue { get; set; }

        /// <summary>Event Filter</summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public EventFilterModel EventFilter { get; set; }

        /// <summary> Settings for pending condition handling </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public ConditionHandlingOptionsModel ConditionHandling { get; set; }
    }
}
