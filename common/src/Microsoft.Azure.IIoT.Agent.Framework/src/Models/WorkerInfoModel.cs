// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System;

    /// <summary>
    /// Worker info
    /// </summary>
    public class WorkerInfoModel {

        /// <summary>
        /// Identifier of the agent
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        /// Identifier of the worker instance
        /// </summary>
        public string WorkerId { get; set; }

        /// <summary>
        /// Worker status
        /// </summary>
        public WorkerStatus Status { get; set; }

        /// <summary>
        /// Last seen
        /// </summary>
        public DateTime LastSeen { get; set; }
    }
}