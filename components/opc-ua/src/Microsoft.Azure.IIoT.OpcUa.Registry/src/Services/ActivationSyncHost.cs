// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Performs continous endpoint activation synchronization
    /// </summary>
    public sealed class ActivationSyncHost : IHostProcess, IDisposable {

        /// <summary>
        /// Create activation process
        /// </summary>
        /// <param name="activation"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public ActivationSyncHost(IEndpointActivation activation, IActivationSyncConfig config,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _activation = activation ?? throw new ArgumentNullException(nameof(activation));
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
                // Make it so that we run after first interval has expired.
                _updateTimer.Change(_config.SyncInterval, Timeout.InfiniteTimeSpan);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync() {
            if (_cts != null) {
                _cts.Cancel();
                _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
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
                _logger.Information("Running endpoint synchronization...");
                await _activation.SynchronizeActivationAsync(_cts.Token);
                _logger.Information("Endpoint synchronization finished.");
            }
            catch (OperationCanceledException) {
                // Cancel was called - dispose cancellation token
                _cts.Dispose();
                _cts = null;
                return;
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to run endpoint synchronization.");
            }
            _updateTimer.Change(_config.SyncInterval, Timeout.InfiniteTimeSpan);
        }

        private readonly ILogger _logger;
        private readonly Timer _updateTimer;
#pragma warning disable IDE0069 // Disposable fields should be disposed
        private CancellationTokenSource _cts;
#pragma warning restore IDE0069 // Disposable fields should be disposed
        private readonly IEndpointActivation _activation;
        private readonly IActivationSyncConfig _config;
    }
}
