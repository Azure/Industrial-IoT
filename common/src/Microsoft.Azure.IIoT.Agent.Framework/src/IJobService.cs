// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job service
    /// </summary>
    public interface IJobService {

        /// <summary>
        /// List all jobs or continue an existing query
        /// </summary>
        /// <param name="continuationToken"></param>
        /// <param name="maxResults"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoListModel> ListJobsAsync(string continuationToken = null,
            int? maxResults = null, CancellationToken ct = default);

        /// <summary>
        /// Query jobs
        /// </summary>
        /// <param name="query"></param>
        /// <param name="maxResults"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoListModel> QueryJobsAsync(JobInfoQueryModel query,
            int? maxResults = null, CancellationToken ct = default);

        /// <summary>
        /// Cancel a job
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CancelJobAsync(string jobId, CancellationToken ct = default);

        /// <summary>
        /// Cancel a job
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RestartJobAsync(string jobId, CancellationToken ct = default);

        /// <summary>
        /// Deletes a job
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteJobAsync(string jobId, CancellationToken ct = default);

        /// <summary>
        /// Get job or throws if not found.
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoModel> GetJobAsync(string jobId,
            CancellationToken ct = default);
    }
}