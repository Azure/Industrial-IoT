// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry;
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
                // Make it so that we run after first interval has expired.
                _updateTimer.Change(_config.SyncInterval, Timeout.InfiniteTimeSpan);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync() {
            if (_cts != null) {
                _cts.Cancel();
            }

            lock (_timerLock) {
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
            lock (_timerLock) {
                try {
                    _cts.Token.ThrowIfCancellationRequested();
                    _logger.Information("Running endpoint synchronization...");
                    _activation.SynchronizeActivationAsync(_cts.Token).Wait();
                    _logger.Information("Endpoint synchronization finished.");
                }
                catch (OperationCanceledException ex) {
                    if (_cts.IsCancellationRequested) {
                        // Cancel was called.
                        return;
                    }

                    // Some operation timed out.
                    _logger.Error(ex, "Failed to run endpoint synchronization.");
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to run endpoint synchronization.");
                }

                if (!_cts.IsCancellationRequested) {
                    _updateTimer.Change(_config.SyncInterval, Timeout.InfiniteTimeSpan);
                }
            }
        }

        private readonly IEndpointActivation _activation;
        private readonly IActivationSyncConfig _config;
        private readonly ILogger _logger;
        private readonly Timer _updateTimer;
        private CancellationTokenSource _cts;
        private readonly object _timerLock = new object();
    }
}
