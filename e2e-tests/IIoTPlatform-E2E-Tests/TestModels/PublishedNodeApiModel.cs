// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestModels {
    using System.Runtime.Serialization;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A monitored and published node api model
    /// </summary>
    public class PublishedNodeApiModel {

        /// <summary> Node Identifier </summary>
        public string Id { get; set; }

        /// <summary> Expanded Node identifier </summary>
        public string ExpandedNodeId { get; set; }

        /// <summary> Sampling interval </summary>
        public int? OpcSamplingInterval { get; set; }

        /// <summary>
        /// OpcSamplingInterval as TimeSpan.
        /// </summary>
        public TimeSpan? OpcSamplingIntervalTimespan {
            get => OpcSamplingInterval.HasValue ?
                TimeSpan.FromMilliseconds(OpcSamplingInterval.Value) : (TimeSpan?)null;
            set => OpcSamplingInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
        }

        /// <summary> Publishing interval </summary>
        public int? OpcPublishingInterval { get; set; }

        /// <summary>
        /// OpcPublishingInterval as TimeSpan.
        /// </summary>
        public TimeSpan? OpcPublishingIntervalTimespan {
            get => OpcPublishingInterval.HasValue ?
                TimeSpan.FromMilliseconds(OpcPublishingInterval.Value) : (TimeSpan?)null;
            set => OpcPublishingInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
        }

        /// <summary> DataSetFieldId </summary>
        public string DataSetFieldId { get; set; }

        /// <summary> Display name </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Heartbeat interval as TimeSpan.
        /// </summary>
        public int? HeartbeatInterval {
            get => HeartbeatIntervalTimespan.HasValue ?
                (int)HeartbeatIntervalTimespan.Value.TotalSeconds : default(int?);
            set => HeartbeatIntervalTimespan = value.HasValue ?
                TimeSpan.FromSeconds(value.Value) : default(TimeSpan?);
        }

        /// <summary> Heartbeat </summary>
        public TimeSpan? HeartbeatIntervalTimespan { get; set; }

        /// <summary> Skip first value </summary>
        public bool? SkipFirst { get; set; }

        /// <summary> Queue size for monitored items </summary>
        public uint? QueueSize { get; set; }
    }
}
