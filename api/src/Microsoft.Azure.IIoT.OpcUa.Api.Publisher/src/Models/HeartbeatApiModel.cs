// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Heart beat
    /// </summary>
    [DataContract]
    public class HeartbeatApiModel {

        /// <summary>
        /// Worker heartbeat
        /// </summary>
        [DataMember(Name = "worker", Order = 0,
            EmitDefaultValue = false)]
        public WorkerHeartbeatApiModel Worker { get; set; }

        /// <summary>
        /// Job heartbeat
        /// </summary>
        [DataMember(Name = "job", Order = 1,
            EmitDefaultValue = false)]
        public JobHeartbeatApiModel Job { get; set; }
    }
}