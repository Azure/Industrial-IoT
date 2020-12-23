// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using Microsoft.Azure.IIoT.Serializers;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Job heartbeat
    /// </summary>
    [DataContract]
    public class JobHeartbeatApiModel {

        /// <summary>
        /// Job id
        /// </summary>
        [DataMember(Name = "jobId", Order = 0)]
        [Required]
        public string JobId { get; set; }

        /// <summary>
        /// Hash
        /// </summary>
        [DataMember(Name = "jobHash", Order = 1,
            EmitDefaultValue = false)]
        public string JobHash { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [DataMember(Name = "status", Order = 2,
            EmitDefaultValue = false)]
        public JobStatus Status { get; set; }

        /// <summary>
        /// Process mode
        /// </summary>
        [DataMember(Name = "processMode", Order = 3,
            EmitDefaultValue = false)]
        public ProcessMode ProcessMode { get; set; }

        /// <summary>
        /// Job state
        /// </summary>
        [DataMember(Name = "state", Order = 4,
            EmitDefaultValue = false)]
        public VariantValue State { get; set; }
    }
}