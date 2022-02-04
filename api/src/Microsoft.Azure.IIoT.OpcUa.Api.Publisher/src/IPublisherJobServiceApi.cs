// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job service api
    /// </summary>
    public interface IPublisherJobServiceApi {

        /// <summary>
        /// Returns status
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> GetServiceStatusAsync(CancellationToken ct = default);

        /// <summary>
        /// Lists all jobs
        /// </summary>
        /// <param name="continuationToken"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoListApiModel> ListJobsAsync(
            string continuationToken = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Query jobs - use the List api to continue paging.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoListApiModel> QueryJobsAsync(
            JobInfoQueryApiModel query, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Return job by id
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobInfoApiModel> GetJobAsync(string jobId,
            CancellationToken ct = default);

        /// <summary>
        /// Restart a job
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RestartJobAsync(string jobId, CancellationToken ct = default);

        /// <summary>
        /// Cancel a job
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CancelJobAsync(string jobId, CancellationToken ct = default);

        /// <summary>
        /// Delete a job
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteJobAsync(string jobId, CancellationToken ct = default);

        /// <summary>
        /// List workers
        /// </summary>
        /// <param name="continuationToken"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WorkerInfoListApiModel> ListWorkersAsync(
            string continuationToken = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Delete worker
        /// </summary>
        /// <param name="workerId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteWorkerAsync(
            string workerId, CancellationToken ct = default);

        /// <summary>
        /// Get worker by id
        /// </summary>
        /// <param name="workerId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WorkerInfoApiModel> GetWorkerAsync(
            string workerId, CancellationToken ct = default);
    }
}