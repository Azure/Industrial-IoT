// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Clients {
    using Microsoft.Azure.IIoT.Api.Jobs.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job orchestrator client that connects to the cloud endpoint.
    /// </summary>
    public class JobOrchestratorClient : IJobOrchestrator {

        /// <summary>
        /// Create connector
        /// </summary>
        /// <param name="config"></param>
        /// <param name="httpClient"></param>
        /// <param name="tokenProvider"></param>
        /// <param name="serializer"></param>
        public JobOrchestratorClient(IHttpClient httpClient, IAgentConfigProvider config,
            IIdentityTokenProvider tokenProvider, ISerializer serializer) {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<JobProcessingInstructionModel> GetAvailableJobAsync(string workerId,
            JobRequestModel jobRequest, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException(nameof(workerId));
            }
            while (true) {
                var uri = _config?.Config?.JobOrchestratorUrl?.TrimEnd('/');
                if (uri == null) {
                    throw new InvalidConfigurationException("Job orchestrator not configured");
                }
                var request = _httpClient.NewRequest($"{uri}/v2/workers/{workerId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    _tokenProvider.IdentityToken.ToAuthorizationValue());
                _serializer.SerializeToRequest(request, jobRequest.ToApiModel());
                var response = await _httpClient.PostAsync(request, ct)
                    .ConfigureAwait(false);
                try {
                    response.Validate();
                    var result = _serializer.DeserializeResponse<JobProcessingInstructionApiModel>(
                        response);
                    return result.ToServiceModel();
                }
                catch (UnauthorizedAccessException) {
                    await _tokenProvider.ForceUpdate();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat,
            CancellationToken ct) {
            if (heartbeat == null) {
                throw new ArgumentNullException(nameof(heartbeat));
            }
            while (true) {
                var uri = _config?.Config?.JobOrchestratorUrl?.TrimEnd('/');
                if (uri == null) {
                    throw new InvalidConfigurationException("Job orchestrator not configured");
                }
                var request = _httpClient.NewRequest($"{uri}/v2/heartbeat");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    _tokenProvider.IdentityToken.ToAuthorizationValue());
                _serializer.SerializeToRequest(request, heartbeat.ToApiModel());
                var response = await _httpClient.PostAsync(request, ct)
                    .ConfigureAwait(false);
                try {
                    response.Validate();
                    var result = _serializer.DeserializeResponse<HeartbeatResponseApiModel>(
                        response);
                    return result.ToServiceModel();
                }
                catch (UnauthorizedAccessException) {
                    await _tokenProvider.ForceUpdate();
                }
            }
        }

        private readonly IIdentityTokenProvider _tokenProvider;
        private readonly ISerializer _serializer;
        private readonly IAgentConfigProvider _config;
        private readonly IHttpClient _httpClient;
    }
}