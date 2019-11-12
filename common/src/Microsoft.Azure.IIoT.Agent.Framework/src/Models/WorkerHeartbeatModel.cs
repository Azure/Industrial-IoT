// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {

    /// <summary>
    /// Heartbeat information
    /// </summary>
    public class WorkerHeartbeatModel {

        /// <summary>
        /// Worker identifier
        /// </summary>
        public string WorkerId { get; set; }

        /// <summary>
        /// Agent identifier
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public WorkerStatus Status { get; set; }
    }
}