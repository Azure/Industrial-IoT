// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job manager services
    /// </summary>
    public interface IJobScheduler {

        /// <summary>
        /// Create completely new job
        /// </summary>
        /// <param name="model"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoModel> NewJobAsync(JobInfoModel model,
            CancellationToken ct = default);

        /// <summary>
        /// Update or create new job
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoModel> NewOrUpdateJobAsync(string jobId,
            Func<JobInfoModel, CancellationToken, Task<bool>> predicate,
            CancellationToken ct = default);
    }
}