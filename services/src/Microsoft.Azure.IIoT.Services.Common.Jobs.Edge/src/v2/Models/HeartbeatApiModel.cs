// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Heart beat
    /// </summary>
    public class HeartbeatApiModel {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public HeartbeatModel ToServiceModel() {
            return new HeartbeatModel {
                Worker = Worker?.ToServiceModel(),
                Job = Job?.ToServiceModel()
            };
        }
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