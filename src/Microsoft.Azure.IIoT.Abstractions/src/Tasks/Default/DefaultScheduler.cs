// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks.Default {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Tasks;
    using System.Threading.Tasks;

    /// <summary>
    /// Default task scheduler
    /// </summary>
    public class DefaultScheduler : ITaskScheduler {

        /// <summary>
        /// Provides a shared task factory
        /// </summary>
        public TaskFactory Factory => Task.Factory;

        /// <summary>
        /// Create debug dump
        /// </summary>
        /// <param name="logger"></param>
        public void Dump(ILogger logger) {
            // No op
        }
    }
}
