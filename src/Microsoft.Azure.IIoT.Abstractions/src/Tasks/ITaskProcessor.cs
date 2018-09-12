// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A task processor schedules processing of tasks using a
    /// scheduler.
    /// </summary>
    public interface ITaskProcessor : IDisposable {

        /// <summary>
        /// The processors task scheduler
        /// </summary>
        ITaskScheduler Scheduler { get; }

        /// <summary>
        /// Try enqueue task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="checkpoint"></param>
        /// <returns></returns>
        bool TrySchedule(Func<Task> task, Func<Task> checkpoint);
    }
}
