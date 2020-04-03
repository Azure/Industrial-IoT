// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Worker info
    /// </summary>
    [DataContract]
    public class WorkerInfoApiModel {

        /// <summary>
        /// Identifier of the worker instance
        /// </summary>
        [DataMember(Name = "workerId", Order = 0,
            EmitDefaultValue = false)]
        public string WorkerId { get; set; }

        /// <summary>
        /// Identifier of the agent
        /// </summary>
        [DataMember(Name = "agentId", Order = 1,
            EmitDefaultValue = false)]
        public string AgentId { get; set; }

        /// <summary>
        /// Worker status
        /// </summary>
        [DataMember(Name = "status", Order = 2,
            EmitDefaultValue = false)]
        public WorkerStatus Status { get; set; }

        /// <summary>
        /// Last seen
        /// </summary>
        [DataMember(Name = "lastSeen", Order = 3,
            EmitDefaultValue = false)]
        public DateTime LastSeen { get; set; }
    }
}