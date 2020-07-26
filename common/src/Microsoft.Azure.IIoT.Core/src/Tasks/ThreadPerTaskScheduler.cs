// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks.Default {
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a task scheduler that dedicates a thread per task.
    /// </summary>
    public class ThreadPerTaskScheduler : TaskScheduler {

        /// <inheritdoc/>
        protected override IEnumerable<Task> GetScheduledTasks() {
            return Enumerable.Empty<Task>();
        }

        /// <inheritdoc/>
        protected override void QueueTask(Task task) {
            new Thread(() => TryExecuteTask(task)) {
                IsBackground = true
            }.Start();
        }

        /// <inheritdoc/>
        protected override bool TryExecuteTaskInline(Task task,
            bool taskWasPreviouslyQueued) {
            return TryExecuteTask(task);
        }
    }

}
