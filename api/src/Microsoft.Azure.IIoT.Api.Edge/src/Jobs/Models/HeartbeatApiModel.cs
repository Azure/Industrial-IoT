// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Heart beat
    /// </summary>
    public class HeartbeatApiModel {

        /// <summary>
        /// Worker heartbeat
        /// </summary>
        [JsonProperty(PropertyName = "worker",
            NullValueHandling = NullValueHandling.Ignore)]
        public WorkerHeartbeatApiModel Worker { get; set; }

        /// <summary>
        /// Job heartbeat
        /// </summary>
        [JsonProperty(PropertyName = "job",
            NullValueHandling = NullValueHandling.Ignore)]
        public JobHeartbeatApiModel Job { get; set; }
    }
}