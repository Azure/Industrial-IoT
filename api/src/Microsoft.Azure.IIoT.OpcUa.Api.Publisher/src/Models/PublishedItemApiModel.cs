// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A monitored and published item
    /// </summary>
    [DataContract]
    public class PublishedItemApiModel {

        /// <summary>
        /// Node to monitor
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0)]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Display name of the node to monitor
        /// </summary>
        [DataMember(Name = "displayName", Order = 1,
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Publishing interval to use
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Sampling interval to use
        /// </summary>
        [DataMember(Name = "samplingInterval", Order = 3,
            EmitDefaultValue = false)]
        public TimeSpan? SamplingInterval { get; set; }

        /// <summary>
        /// Sampling interval to use
        /// </summary>
        [DataMember(Name = "heartbeatInterval", Order = 4,
            EmitDefaultValue = false)]
        public TimeSpan? HeartbeatInterval { get; set; }
    }
}
