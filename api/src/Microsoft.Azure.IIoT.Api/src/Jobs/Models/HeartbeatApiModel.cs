// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Heart beat
    /// </summary>
    [DataContract]
    public class HeartbeatApiModel {

        /// <summary>
        /// Worker heartbeat
        /// </summary>
        [DataMember(Name = "worker",
            EmitDefaultValue = false)]
        public WorkerHeartbeatApiModel Worker { get; set; }

        /// <summary>
        /// Job heartbeat
        /// </summary>
        [DataMember(Name = "job",
            EmitDefaultValue = false)]
        public JobHeartbeatApiModel Job { get; set; }
    }
}