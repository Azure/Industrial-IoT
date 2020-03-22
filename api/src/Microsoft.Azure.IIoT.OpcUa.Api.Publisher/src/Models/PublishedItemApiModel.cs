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
        [DataMember(Name = "nodeId")]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Display name of the node to monitor
        /// </summary>
        [DataMember(Name = "displayName",
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Publishing interval to use
        /// </summary>
        [DataMember(Name = "publishingInterval",
            EmitDefaultValue = false)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Sampling interval to use
        /// </summary>
        [DataMember(Name = "samplingInterval",
            EmitDefaultValue = false)]
        public TimeSpan? SamplingInterval { get; set; }
    }
}
