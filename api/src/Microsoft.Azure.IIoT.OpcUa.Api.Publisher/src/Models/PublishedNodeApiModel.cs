// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
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
        public TimeSpan? OpcSamplingIntervalTimespan {
            get => OpcSamplingInterval.HasValue ?
                TimeSpan.FromMilliseconds(OpcSamplingInterval.Value) : (TimeSpan?)null;
            set => OpcSamplingInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
        }

        /// <summary> Publishing interval </summary>
        [DataMember(Name = "opcPublishingInterval", Order = 4,
            EmitDefaultValue = false)]
        public int? OpcPublishingInterval { get; set; }

        /// <summary>
        /// OpcPublishingInterval as TimeSpan.
        /// </summary>
        [DataMember(Name = "opcPublishingIntervalTimespan", Order = 5,
            EmitDefaultValue = false)]
        public TimeSpan? OpcPublishingIntervalTimespan {
            get => OpcPublishingInterval.HasValue ?
                TimeSpan.FromMilliseconds(OpcPublishingInterval.Value) : (TimeSpan?)null;
            set => OpcPublishingInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
        }

        /// <summary> DataSetFieldId </summary>
        [DataMember(Name = "dataSetFieldId", Order = 6,
            EmitDefaultValue = false)]
        public string DataSetFieldId { get; set; }

        /// <summary> Display name </summary>
        [DataMember(Name = "displayName", Order = 7,
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Heartbeat interval as TimeSpan.
        /// </summary>
        [DataMember(Name = "heartbeatInterval", Order = 8,
            EmitDefaultValue = false)]
        public int? HeartbeatInterval {
            get => HeartbeatIntervalTimespan.HasValue ?
                (int)HeartbeatIntervalTimespan.Value.TotalSeconds : (int?)null;
            set => HeartbeatIntervalTimespan = value.HasValue ?
                TimeSpan.FromSeconds(value.Value) : (TimeSpan?)null;
        }

        /// <summary> Heartbeat </summary>
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
    }
}
