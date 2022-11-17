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
    /// Job repo
    /// </summary>
    public interface IJobRepository {

        /// <summary>
        /// List jobs
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuationToken"></param>
        /// <param name="maxResults"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoListModel> QueryAsync(JobInfoQueryModel query = null,
            string continuationToken = null, int? maxResults = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get job by identifier
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoModel> GetAsync(string jobId,
            CancellationToken ct = default);

        /// <summary>
        /// Add new job to repository. The created job is
        /// returned.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="ct"></param>
        /// <returns>The newly created job</returns>
        Task<JobInfoModel> AddAsync(JobInfoModel job,
            CancellationToken ct = default);

        /// <summary>
        /// Add a new one or update existing job
        /// </summary>
        /// <param name="jobId">Job to create or update
        /// </param>
        /// <param name="predicate">receives existing job or
        /// null if not exists, return null to cancel.
        /// </param>
        /// <param name="ct"></param>
        /// <returns>The existing or udpated job</returns>
        Task<JobInfoModel> AddOrUpdateAsync(string jobId,
             Func<JobInfoModel, CancellationToken, Task<JobInfoModel>> predicate,
             CancellationToken ct = default);

        /// <summary>
        /// Update job
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoModel> UpdateAsync(string jobId,
             Func<JobInfoModel, CancellationToken, Task<bool>> predicate,
             CancellationToken ct = default);

        /// <summary>
        /// Delete job
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoModel> DeleteAsync(string jobId,
            Func<JobInfoModel, CancellationToken, Task<bool>> predicate,
            CancellationToken ct = default);
    }
}