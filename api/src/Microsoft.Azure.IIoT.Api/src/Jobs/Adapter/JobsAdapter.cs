// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs {
    using Microsoft.Azure.IIoT.Api.Jobs.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements agent and job registry services on top of api
    /// </summary>
    public sealed class JobsAdapter : IJobService, IWorkerSupervisorRegistry {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public JobsAdapter(IJobsServiceApi client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<JobInfoListModel> ListJobsAsync(string continuationToken,
            int? maxResults, CancellationToken ct) {
            var result = await _client.ListJobsAsync(continuationToken, maxResults, ct);
            return result.Map<JobInfoListModel>();
        }

        /// <inheritdoc/>
        public async Task<JobInfoListModel> QueryJobsAsync(JobInfoQueryModel query,
            int? maxResults, CancellationToken ct) {
            var request = query.Map<JobInfoQueryApiModel>();
            var result = await _client.QueryJobsAsync(request, maxResults, ct);
            return result.Map<JobInfoListModel>();
        }

        /// <inheritdoc/>
        public async Task CancelJobAsync(string jobId, CancellationToken ct) {
            await _client.CancelJobAsync(jobId, ct);
        }

        /// <inheritdoc/>
        public async Task RestartJobAsync(string jobId, CancellationToken ct) {
            await _client.RestartJobAsync(jobId, ct);
        }

        /// <inheritdoc/>
        public async Task DeleteJobAsync(string jobId, CancellationToken ct) {
            await _client.DeleteJobAsync(jobId, ct);
        }

        /// <inheritdoc/>
        public async Task<JobInfoModel> GetJobAsync(string jobId, CancellationToken ct) {
            var result = await _client.GetJobAsync(jobId, ct);
            return result.Map<JobInfoModel>();
        }

        /// <inheritdoc/>
        public async Task<WorkerSupervisorInfoListModel> ListWorkerSupervisorsAsync(
            string continuationToken, int? maxResults, CancellationToken ct) {
            var result = await _client.ListWorkerSupervisorsAsync(continuationToken, maxResults, ct);
            return result.Map<WorkerSupervisorInfoListModel>();
        }

        /// <inheritdoc/>
        public async Task<WorkerSupervisorInfoModel> GetWorkerSupervisorAsync(string workerSupervisorId, CancellationToken ct) {
            var result = await _client.GetWorkerSupervisorAsync(workerSupervisorId, ct);
            return result.Map<WorkerSupervisorInfoModel>();
        }

        /// <inheritdoc/>
        public async Task DeleteWorkerSupervisorAsync(string workerSupervisorId, CancellationToken ct) {
            await _client.DeleteWorkerSupervisorAsync(workerSupervisorId, ct);
        }

        private readonly IJobsServiceApi _client;
    }
}
