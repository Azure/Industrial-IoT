// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs {
    using Microsoft.Azure.IIoT.Hub;
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
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public JobOrchestratorEndpointSync(IIoTHubTwinServices twins,
            IJobOrchestratorEndpointConfig config, IJsonSerializer serializer, ILogger logger) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _twins = twins ?? throw new ArgumentNullException(nameof(twins));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updateTimer = new Timer(OnUpdateTimerFiredAsync);
        }

        /// <inheritdoc/>
        public void Dispose() {
            StopAsync().Wait();

            _updateTimer.Dispose();

            // _cts might be null if StartAsync() was never called.
            if (!(_cts is null)) {
                _cts.Dispose();
            }
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
            }

            lock(_timerLock) {
                // Disabling future invocation of timer callback.
                _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Timer operation
        /// </summary>
        /// <param name="sender"></param>
        private void OnUpdateTimerFiredAsync(object sender) {
            lock(_timerLock) {
                try {
                    _cts.Token.ThrowIfCancellationRequested();
                    _logger.Information("Updating orchestrator urls...");
                    UpdateOrchestratorUrlAsync(_cts.Token).Wait();
                    _logger.Information("Orchestrator Url update finished.");
                }
                catch (OperationCanceledException ex) {
                    if (_cts.IsCancellationRequested) {
                        // Cancel was called.
                        return;
                    }

                    // Some operation timed out.
                    _logger.Error(ex, "Failed to update orchestrator urls.");
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to update orchestrator urls.");
                }

                if (!_cts.IsCancellationRequested) {
                    _updateTimer.Change(_config.JobOrchestratorUrlSyncInterval, Timeout.InfiniteTimeSpan);
                }
            }
        }

        /// <summary>
        /// Update all identity tokens
        /// </summary>
        /// <returns></returns>
        public async Task UpdateOrchestratorUrlAsync(CancellationToken ct) {
            var url = _config.JobOrchestratorUrl?.TrimEnd('/');
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
            _logger.Information("Job orchestrator url updated to {url} on all twins.", url);
        }

        private readonly IJobOrchestratorEndpointConfig _config;
        private readonly IJsonSerializer _serializer;
        private readonly IIoTHubTwinServices _twins;
        private readonly ILogger _logger;
        private readonly Timer _updateTimer;
        private CancellationTokenSource _cts;
        private readonly object _timerLock = new object();
    }
}