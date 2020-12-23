// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {

    /// <summary>
    /// Heart beat
    /// </summary>
    public class HeartbeatModel {

        /// <summary>
        /// Worker heartbeat
        /// </summary>
        public WorkerHeartbeatModel Worker { get; set; }

        /// <summary>
        /// Job heartbeat
        /// </summary>
        public JobHeartbeatModel Job { get; set; }
    }
}