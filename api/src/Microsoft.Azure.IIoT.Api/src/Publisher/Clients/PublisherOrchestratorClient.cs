// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Agent;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Prometheus;
    using Serilog;
    using System;
    using System.Diagnostics;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    /// <summary>
    /// Job orchestrator client that connects to the cloud endpoint.
    /// </summary>
    public class PublisherOrchestratorClient : IJobOrchestrator {

        /// <summary>
        /// Create connector
        /// </summary>
        /// <param name="config"></param>
        /// <param name="httpClient"></param>
        /// <param name="tokenProvider"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public PublisherOrchestratorClient(IHttpClient httpClient, IAgentConfigProvider config,
            IIdentityTokenProvider tokenProvider, ISerializer serializer, ILogger logger) {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ThreadPool.GetMinThreads(out var workerThreads, out var asyncThreads);
            if (_config.Config?.MaxWorkers > workerThreads ||
                _config.Config?.MaxWorkers > asyncThreads) {
                var result = ThreadPool.SetMinThreads(_config.Config.MaxWorkers.Value, _config.Config.MaxWorkers.Value);
                _logger.Information("Thread pool changed to worker {worker}, async {async} threads {success}",
                    _config.Config.MaxWorkers.Value, _config.Config.MaxWorkers.Value, result ? "succeeded" : "failed");
            }
        }

        /// <inheritdoc/>
        public async Task<JobProcessingInstructionModel> GetAvailableJobAsync(string workerId,
            JobRequestModel jobRequest, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException(nameof(workerId));
            }
            var sw = Stopwatch.StartNew();
            try {
                return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                    var uri = _config?.Config?.JobOrchestratorUrl?.TrimEnd('/');
                    if (uri == null) {
                        throw new InvalidConfigurationException("Job orchestrator not configured");
                    }
                    var request = _httpClient.NewRequest($"{uri}/v2/workers/{workerId}");
                    request.Options.Timeout = TimeSpan.FromMinutes(5);
                    request.Options.SuppressHttpClientLogging = true;
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                        _tokenProvider.IdentityToken.ToAuthorizationValue());
                    _serializer.SerializeToRequest(request, jobRequest.ToApiModel());
                    try {
                        var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
                        response.Validate();
                        var result = _serializer.DeserializeResponse<JobProcessingInstructionApiModel>(
                            response);
                        return result.ToServiceModel();
                    }
                    catch (UnauthorizedAccessException) {
                        await _tokenProvider.ForceUpdate();
                        throw;
                    }
                }, ex => {
                    if (!(ex is UnauthorizedAccessException) && !(ex is InvalidConfigurationException)) {
                        _logger.Debug("Attempt to get job instructions for {worker} failed: {message} - try again...",
                           workerId, ex.Message);
                    }
                    return true;
                }, 20);
            }
            catch (Exception ex) {
                _logger.Debug("Getting job instructions for worker {worker} failed: {message}.",
                    workerId, ex.Message);
                throw;
            }
            finally {
                var elapsed = sw.Elapsed;
                kAvailableJobCallDuration.WithLabels(workerId).Observe(elapsed.TotalMilliseconds);
                if (elapsed > TimeSpan.FromSeconds(30)) {
                    _logger.Warning("Getting job instructions for worker {worker} took longer than {elapsed}", workerId, sw.Elapsed);
                }
                else {
                    _logger.Debug("Getting job instructions for worker {worker} took {elapsed}", workerId, sw.Elapsed);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat,
            JobDiagnosticInfoModel info, CancellationToken ct) {
            if (heartbeat == null) {
                throw new ArgumentNullException(nameof(heartbeat));
            }
            var sw = Stopwatch.StartNew();
            try {
                return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                    var uri = _config?.Config?.JobOrchestratorUrl?.TrimEnd('/');
                    if (uri == null) {
                        throw new InvalidConfigurationException("Job orchestrator not configured");
                    }
                    var request = _httpClient.NewRequest($"{uri}/v2/heartbeat");
                    request.Options.Timeout = TimeSpan.FromMinutes(5);
                    request.Options.SuppressHttpClientLogging = true;
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                        _tokenProvider.IdentityToken.ToAuthorizationValue());
                    _serializer.SerializeToRequest(request, heartbeat.ToApiModel());
                    try {
                        var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
                        response.Validate();
                        var result = _serializer.DeserializeResponse<HeartbeatResponseApiModel>(
                            response);
                        return result.ToServiceModel();
                    }
                    catch (UnauthorizedAccessException) {
                        await _tokenProvider.ForceUpdate();
                        throw;
                    }
                }, ex => {
                    if (!(ex is UnauthorizedAccessException) && !(ex is InvalidConfigurationException)) {
                        _logger.Debug("Attempt to send worker {worker} heartbeat failed: {message} - try again...",
                           heartbeat.Worker.WorkerId, ex.Message);
                    }
                    return true;
                }, 20);
            }
            catch (Exception ex) {
                _logger.Debug("Sending worker {worker} heartbeat failed: {message}.",
                    heartbeat.Worker.WorkerId, ex.Message);
                throw;
            }
            finally {
                var elapsed = sw.Elapsed;
                var workerId = heartbeat.Worker.WorkerId;
                kHeartbeatCallDuration.WithLabels(workerId).Observe(elapsed.TotalMilliseconds);
                if (elapsed > TimeSpan.FromSeconds(30)) {
                    _logger.Warning("Sending worker {worker} heartbeat took longer than {elapsed}", workerId, sw.Elapsed);
                }
                else {
                    _logger.Debug("Sending worker {worker} heartbeat took {elapsed}", workerId, sw.Elapsed);
                }
            }
        }

        private readonly IIdentityTokenProvider _tokenProvider;
        private readonly ISerializer _serializer;
        private readonly IAgentConfigProvider _config;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        private static readonly Histogram kHeartbeatCallDuration = Metrics.CreateHistogram(
                "iiot_edge_publisher_heartbeat_call_duration",
                "Duration of heartbeat call", new HistogramConfiguration {
                    LabelNames = new[] { "workerid" }
                });
        private static readonly Histogram kAvailableJobCallDuration = Metrics.CreateHistogram(
                "iiot_edge_publisher_available_job_call_duration",
                "Duration of job call", new HistogramConfiguration {
                    LabelNames = new[] { "workerid" }
                });
    }
}