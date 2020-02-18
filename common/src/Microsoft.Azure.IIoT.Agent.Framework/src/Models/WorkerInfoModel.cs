// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System;

    /// <summary>
    /// Worker info
    /// </summary>
    public class WorkerSupervisorInfoModel {

        /// <summary>
        /// Identifier of the worker supervisor
        /// </summary>
        public string WorkerSupervisorId { get; set; }

        /// <summary>
        /// Worker status
        /// </summary>
        public SupervisorStatus Status { get; set; }

        /// <summary>
        /// Last seen
        /// </summary>
        public DateTime LastSeen { get; set; }
    }
}