// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Task processor extensions
    /// </summary>
    public static class TaskProcessorEx {

        /// <summary>
        /// Try enqueue task
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static bool TrySchedule(this ITaskProcessor processor,
            Func<Task> task) {
            return processor.TrySchedule(task, () => Task.CompletedTask);
        }
    }
}
