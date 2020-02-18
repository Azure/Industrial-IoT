// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models
{
    using System;

    /// <summary>
    /// Heartbeat response
    /// </summary>
    public class HeartbeatResultEntryModel {
        /// <summary>
        /// The id of the job
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Instructions
        /// </summary>
        public HeartbeatInstruction HeartbeatInstruction { get; set; }

        /// <summary>
        /// Last active
        /// </summary>
        public DateTime? LastActiveHeartbeat { get; set; }

        /// <summary>
        /// Job continuation in case of updates
        /// </summary>
        public JobProcessingInstructionModel UpdatedJob { get; set; }
    }
}