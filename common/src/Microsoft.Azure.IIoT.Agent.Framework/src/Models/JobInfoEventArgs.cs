// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System;

    /// <summary>
    /// Job event
    /// </summary>
    public class JobInfoEventArgs : EventArgs {

        /// <summary>
        /// Create event
        /// </summary>
        /// <param name="job"></param>
        public JobInfoEventArgs(JobInfoModel job) {
            Job = job;
        }

        /// <summary>
        /// Job
        /// </summary>
        public JobInfoModel Job { get; }
    }
}