// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Clients {
    using Microsoft.Azure.IIoT.Api.Jobs.Models;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job service client
    /// </summary>
    public class JobsServiceClient : IJobsServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        public JobsServiceClient(IHttpClient httpClient, IJobsServiceConfig config) :
            this(httpClient, config.JobServiceUrl, config.JobServiceResourceId) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        public JobsServiceClient(IHttpClient httpClient, string serviceUri, string resourceId) {
            _serviceUri = serviceUri ?? throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _resourceId = resourceId;
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/healthz",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<string>();
        }

        /// <inheritdoc/>
        public async Task<JobInfoListApiModel> ListJobsAsync(string continuation,
            int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs",
                _resourceId);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<JobInfoListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<JobInfoListApiModel> QueryJobsAsync(JobInfoQueryApiModel query,
            int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs",
                _resourceId);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            request.SetContent(query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<JobInfoListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<JobInfoApiModel> GetJobAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs/{jobId}",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<JobInfoApiModel>();
        }

        /// <inheritdoc/>
        public async Task CancelJobAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs/{jobId}/cancel",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task RestartJobAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs/{jobId}/restart",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task DeleteJobAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs/{jobId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<WorkerSupervisorInfoListApiModel> ListWorkerSupervisorsAsync(string continuation,
            int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/workers",
                _resourceId);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<WorkerSupervisorInfoListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<WorkerSupervisorInfoApiModel> GetWorkerSupervisorAsync(string workerSupervisorId, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerSupervisorId)) {
                throw new ArgumentNullException(nameof(workerSupervisorId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/workersupervisors/{workerSupervisorId}",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<WorkerSupervisorInfoApiModel>();
        }

        /// <inheritdoc/>
        public async Task DeleteWorkerSupervisorAsync(string workerSupervisorId, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerSupervisorId)) {
                throw new ArgumentNullException(nameof(workerSupervisorId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/workersupervisors/{workerSupervisorId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        private readonly string _serviceUri;
        private readonly IHttpClient _httpClient;
        private readonly string _resourceId;
    }
}