// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Agent;
    using System;

    /// <summary>
    /// Job event
    /// </summary>
    public class JobInfoEventArgs : EventArgs {

        /// <summary>
        /// Create event
        /// </summary>
        /// <param name="job"></param>
        /// <param name="worker"></param>
        public JobInfoEventArgs(JobInfoModel job, IWorker worker) {
            Job = job;
            Worker = worker;
        }

        /// <summary>
        /// Job
        /// </summary>
        public JobInfoModel Job { get; }

        /// <summary>
        /// The worker that sent to event.
        /// </summary>
        public IWorker Worker { get; }
    }
}