// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Heartbeat information
    /// </summary>
    [DataContract]
    public class WorkerHeartbeatApiModel {

        /// <summary>
        /// Worker id
        /// </summary>
        [DataMember(Name = "workerId",
            EmitDefaultValue = false)]
        public string WorkerId { get; set; }

        /// <summary>
        /// Agent id
        /// </summary>
        [DataMember(Name = "agentId",
            EmitDefaultValue = false)]
        public string AgentId { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [DataMember(Name = "status",
            EmitDefaultValue = false)]
        public WorkerStatus Status { get; set; }
    }
}