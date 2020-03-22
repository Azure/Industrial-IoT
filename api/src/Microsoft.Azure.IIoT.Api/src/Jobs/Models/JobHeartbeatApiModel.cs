// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
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
        [DataMember(Name = "jobId")]
        [Required]
        public string JobId { get; set; }

        /// <summary>
        /// Hash
        /// </summary>
        [DataMember(Name = "jobHash",
            EmitDefaultValue = false)]
        public string JobHash { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [DataMember(Name = "status",
            EmitDefaultValue = false)]
        public JobStatus Status { get; set; }

        /// <summary>
        /// Process mode
        /// </summary>
        [DataMember(Name = "processMode",
            EmitDefaultValue = false)]
        public ProcessMode ProcessMode { get; set; }

        /// <summary>
        /// Job state
        /// </summary>
        [DataMember(Name = "state",
            EmitDefaultValue = false)]
        public VariantValue State { get; set; }
    }
}