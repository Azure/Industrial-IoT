// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job service client
    /// </summary>
    public class PublisherJobServiceClient : IPublisherJobServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public PublisherJobServiceClient(IHttpClient httpClient, IPublisherConfig config,
            ISerializer serializer) :
            this(httpClient, config?.OpcUaPublisherServiceUrl, serializer) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public PublisherJobServiceClient(IHttpClient httpClient, string serviceUri,
            ISerializer serializer = null) {
            if (string.IsNullOrWhiteSpace(serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _serviceUri = serviceUri.TrimEnd('/');
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/healthz",
                Resource.Platform);
            try {
                var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
                response.Validate();
                return response.GetContentAsString();
            }
            catch (Exception ex) {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        public async Task<JobInfoListApiModel> ListJobsAsync(string continuation,
            int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs",
                Resource.Platform);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<JobInfoListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<JobInfoListApiModel> QueryJobsAsync(JobInfoQueryApiModel query,
            int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs",
                Resource.Platform);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, query);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<JobInfoListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<JobInfoApiModel> GetJobAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs/{jobId}",
                Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<JobInfoApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task CancelJobAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs/{jobId}/cancel",
                Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task RestartJobAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs/{jobId}/restart",
                Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task DeleteJobAsync(string jobId, CancellationToken ct) {
            if (string.IsNullOrEmpty(jobId)) {
                throw new ArgumentNullException(nameof(jobId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/jobs/{jobId}",
                Resource.Platform);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<WorkerInfoListApiModel> ListWorkersAsync(string continuation,
            int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/workers",
                Resource.Platform);
            if (continuation != null) {
                request.AddHeader(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<WorkerInfoListApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<WorkerInfoApiModel> GetWorkerAsync(string workerId, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException(nameof(workerId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/workers/{workerId}",
                Resource.Platform);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<WorkerInfoApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task DeleteWorkerAsync(string workerId, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException(nameof(workerId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/workers/{workerId}",
                Resource.Platform);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        private readonly string _serviceUri;
        private readonly IHttpClient _httpClient;
        private readonly ISerializer _serializer;
    }
}