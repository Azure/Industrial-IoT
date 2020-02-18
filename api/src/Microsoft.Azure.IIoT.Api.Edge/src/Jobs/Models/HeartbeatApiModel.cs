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
        /// Supervisor heartbeat
        /// </summary>
        [JsonProperty(PropertyName = "supervisorheartbeat",
            NullValueHandling = NullValueHandling.Ignore)]
        public SupervisorHeartbeatApiModel SupervisorHeartbeat { get; set; }

        /// <summary>
        /// Job heartbeats
        /// </summary>
        [JsonProperty(PropertyName = "jobheartbeats",
            NullValueHandling = NullValueHandling.Ignore)]
        public JobHeartbeatApiModel[] JobHeartbeats { get; set; }
    }
}