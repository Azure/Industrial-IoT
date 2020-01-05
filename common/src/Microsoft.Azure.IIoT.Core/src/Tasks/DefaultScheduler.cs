// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks.Default {
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Default task scheduler
    /// </summary>
    public sealed class DefaultScheduler : ITaskScheduler {

        /// <inheritdoc/>
        public TaskFactory Factory => Task.Factory;

        /// <inheritdoc/>
        public void Dump(Action<Task> logger) {
            // No op
        }
    }
}
