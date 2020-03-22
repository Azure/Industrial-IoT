// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs {
    using Microsoft.Azure.IIoT.Api.Jobs.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements agent and job registry services on top of api
    /// </summary>
    public sealed class JobsServicesApiAdapter : IJobService, IWorkerRegistry {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        public JobsServicesApiAdapter(IJobsServiceApi client, ISerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<JobInfoListModel> ListJobsAsync(string continuationToken,
            int? maxResults, CancellationToken ct) {
            var result = await _client.ListJobsAsync(continuationToken, maxResults, ct);
            return _serializer.Map<JobInfoListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<JobInfoListModel> QueryJobsAsync(JobInfoQueryModel query,
            int? maxResults, CancellationToken ct) {
            var request = _serializer.Map<JobInfoQueryApiModel>(query);
            var result = await _client.QueryJobsAsync(request, maxResults, ct);
            return _serializer.Map<JobInfoListModel>(result);
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
            return _serializer.Map<JobInfoModel>(result);
        }

        /// <inheritdoc/>
        public async Task<WorkerInfoListModel> ListWorkersAsync(
            string continuationToken, int? maxResults, CancellationToken ct) {
            var result = await _client.ListWorkersAsync(continuationToken, maxResults, ct);
            return _serializer.Map<WorkerInfoListModel>(result);
        }

        /// <inheritdoc/>
        public async Task<WorkerInfoModel> GetWorkerAsync(string workerId, CancellationToken ct) {
            var result = await _client.GetWorkerAsync(workerId, ct);
            return _serializer.Map<WorkerInfoModel>(result);
        }

        /// <inheritdoc/>
        public async Task DeleteWorkerAsync(string workerId, CancellationToken ct) {
            await _client.DeleteWorkerAsync(workerId, ct);
        }

        private readonly ISerializer _serializer;
        private readonly IJobsServiceApi _client;
    }
}
