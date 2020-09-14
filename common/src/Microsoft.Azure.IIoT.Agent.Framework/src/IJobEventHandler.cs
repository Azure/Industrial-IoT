// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Job repository events
    /// </summary>
    public interface IJobEventHandler {

        /// <summary>
        /// Job creating
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        Task OnJobCreatingAsync(IJobService manager, JobInfoModel job);

        /// <summary>
        /// Job deleting
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        Task OnJobDeletingAsync(IJobService manager, JobInfoModel job);

        /// <summary>
        /// Job created
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        Task OnJobCreatedAsync(IJobService manager, JobInfoModel job);

        /// <summary>
        /// Job deleted
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        Task OnJobDeletedAsync(IJobService manager, JobInfoModel job);

        /// <summary>
        /// Assign a job to an edge device scope
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="job"></param>
        /// <param name="deviceScope"></param>
        /// <returns></returns>
        Task OnJobAssignmentAsync(IJobService manager, JobInfoModel job, string deviceScope);
    }
}