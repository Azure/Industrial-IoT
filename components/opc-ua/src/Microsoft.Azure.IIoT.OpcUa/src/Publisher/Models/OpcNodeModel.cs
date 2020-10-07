﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing an entry in the node list
    /// </summary>
    [DataContract]
    public class OpcNodeModel {

        /// <summary> Node Identifier </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary> Expanded Node identifier </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ExpandedNodeId { get; set; }

        /// <summary> Sampling interval </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? OpcSamplingInterval { get; set; }

        /// <summary>
        /// OpcSamplingInterval as TimeSpan.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan? OpcSamplingIntervalTimespan {
            get => OpcSamplingInterval.HasValue ?
                TimeSpan.FromMilliseconds(OpcSamplingInterval.Value) : (TimeSpan?)null;
            set => OpcSamplingInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
        }

        /// <summary> Publishing interval </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? OpcPublishingInterval { get; set; }

        /// <summary>
        /// OpcPublishingInterval as TimeSpan.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan? OpcPublishingIntervalTimespan {
            get => OpcPublishingInterval.HasValue ?
                TimeSpan.FromMilliseconds(OpcPublishingInterval.Value) : (TimeSpan?)null;
            set => OpcPublishingInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
        }

        /// <summary> DataSetFieldId </summary>
        [DataMember(EmitDefaultValue = false)]
        public string DataSetFieldId { get; set; }

        /// <summary> Display name </summary>
        [DataMember(EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary> Heartbeat </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? HeartbeatInterval {
            get => HeartbeatIntervalTimespan.HasValue ? (int)HeartbeatIntervalTimespan.Value.TotalSeconds : default(int?);
            set => HeartbeatIntervalTimespan = value.HasValue ? TimeSpan.FromSeconds(value.Value) : default(TimeSpan?);
        }

        /// <summary>
        /// Heartbeat interval as TimeSpan.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TimeSpan? HeartbeatIntervalTimespan { get; set; }

        /// <summary> Skip first value </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool? SkipFirst { get; set; }
    }
}
