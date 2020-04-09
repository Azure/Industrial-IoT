// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Synchronize job orchestrator endpoints on all agent twins
    /// </summary>
    public class JobOrchestratorEndpointSync : IHostProcess, IDisposable {

        /// <summary>
        /// Create writer
        /// </summary>
        /// <param name="twins"></param>
        /// <param name="endpoint"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public JobOrchestratorEndpointSync(IIoTHubTwinServices twins,
            IJobOrchestratorEndpoint endpoint, IJsonSerializer serializer, ILogger logger) {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _twins = twins ?? throw new ArgumentNullException(nameof(twins));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updateTimer = new Timer(OnUpdateTimerFiredAsync);
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Async(StopAsync).Wait();
            _updateTimer.Dispose();
        }

        /// <inheritdoc/>
        public Task StartAsync() {
            if (_cts == null) {
                _cts = new CancellationTokenSource();
                _updateTimer.Change(0, Timeout.Infinite);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync() {
            if (_cts != null) {
                _cts.Cancel();
                _updateTimer.Change(0, Timeout.Infinite);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Timer operation
        /// </summary>
        /// <param name="sender"></param>
        private async void OnUpdateTimerFiredAsync(object sender) {
            try {
                _cts.Token.ThrowIfCancellationRequested();
                _logger.Information("Updating orchestrator urls...");
                await UpdateOrchestratorUrlAsync(_cts.Token);
                _logger.Information("Orchestrator Url update finished.");
            }
            catch (OperationCanceledException) {
                // Cancel was called - dispose cancellation token
                _cts.Dispose();
                _cts = null;
                return;
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to update orchestrator urls.");
            }
            _updateTimer.Change(TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Update all identity tokens
        /// </summary>
        /// <returns></returns>
        public async Task UpdateOrchestratorUrlAsync(CancellationToken ct) {
            var url = _endpoint.JobOrchestratorUrl?.TrimEnd('/');
            if (string.IsNullOrEmpty(url)) {
                return;
            }
            var query = "SELECT * FROM devices.modules WHERE " +
                $"IS_DEFINED(properties.reported.{TwinProperties.JobOrchestratorUrl}) AND " +
                $"(NOT IS_DEFINED(properties.desired.{TwinProperties.JobOrchestratorUrl}) OR " +
                    $"properties.desired.{TwinProperties.JobOrchestratorUrl} != '{url}')";
            string continuation = null;
            do {
                var response = await _twins.QueryDeviceTwinsAsync(
                    query, continuation, null, ct);
                foreach (var moduleTwin in response.Items) {
                    try {
                        moduleTwin.Properties.Desired[TwinProperties.JobOrchestratorUrl] =
                            _serializer.FromObject(url);
                        await _twins.PatchAsync(moduleTwin, false, ct);
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Failed to update url for module {device} {module}",
                            moduleTwin.Id, moduleTwin.ModuleId);
                    }
                }
                continuation = response.ContinuationToken;
                ct.ThrowIfCancellationRequested();
            }
            while (continuation != null);
            _logger.Information("Identity Token update finished.");
        }

        private readonly IJobOrchestratorEndpoint _endpoint;
        private readonly IJsonSerializer _serializer;
        private readonly IIoTHubTwinServices _twins;
        private readonly ILogger _logger;
        private readonly Timer _updateTimer;
#pragma warning disable IDE0069 // Disposable fields should be disposed
        private CancellationTokenSource _cts;
#pragma warning restore IDE0069 // Disposable fields should be disposed
    }
}