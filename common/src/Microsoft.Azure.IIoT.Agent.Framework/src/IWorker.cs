// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Handle job cancelled
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="jobEventArgs"></param>
    public delegate void JobCanceledEventHandler(object sender, JobInfoEventArgs jobEventArgs);

    /// <summary>
    /// Job finished
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="jobEventArgs"></param>
    public delegate void JobFinishedEventHandler(object sender, JobInfoEventArgs jobEventArgs);

    /// <summary>
    /// Job started event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="jobEventArgs"></param>
    public delegate void JobStartedEventHandler(object sender, JobInfoEventArgs jobEventArgs);

    /// <summary>
    /// Worker
    /// </summary>
    public interface IWorker {

        /// <summary>
        /// The job that is assigned to this worker.
        /// </summary>
        JobInfoModel Job { get; }

        /// <summary>
        /// Worker status
        /// </summary>
        JobStatus Status { get; }

        /// <summary>
        /// Finish event
        /// </summary>
        event JobFinishedEventHandler OnJobCompleted;

        /// <summary>
        /// Cancelled event
        /// </summary>
        event JobCanceledEventHandler OnJobCanceled;

        /// <summary>
        /// Started event
        /// </summary>
        event JobStartedEventHandler OnJobStarted;

        /// <summary>
        /// Processes to job.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ProcessAsync(CancellationToken ct);

        /// <summary>
        /// Gets an updated processing instruction to handle
        /// </summary>
        /// <param name="heartbeatResultEntryModel"></param>
        /// <returns></returns>
        Task ProcessHeartbeatResult(HeartbeatResultEntryModel heartbeatResultEntryModel);
    }
}