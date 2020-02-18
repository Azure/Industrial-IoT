// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {

    /// <summary>
    /// Heartbeat information
    /// </summary>
    public class SupervisorHeartbeatModel {

        /// <summary>
        /// Supervisor identifier
        /// </summary>
        public string SupervisorId { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public SupervisorStatus Status { get; set; }
    }
}