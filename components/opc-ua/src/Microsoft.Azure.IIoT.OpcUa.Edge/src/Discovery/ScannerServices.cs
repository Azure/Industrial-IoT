// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Provides continuous discovery services for the supervisor
    /// </summary>
    public class ScannerServices : IScannerServices, IDisposable {

        /// <inheritdoc/>
        public DiscoveryMode Mode {
            get => Request.Mode;
            set => Request = new DiscoveryRequest(value, Request.Configuration);
        }

        /// <inheritdoc/>
        public DiscoveryConfigModel Configuration {
            get => Request.Configuration;
            set => Request = new DiscoveryRequest(Request.Mode, value);
        }

        /// <summary>
        /// Current discovery options
        /// </summary>
        internal DiscoveryRequest Request { get; set; } = new DiscoveryRequest();

        /// <summary>
        /// Default idle time is 6 hours
        /// </summary>
        internal static TimeSpan DefaultIdleTime { get; set; } = TimeSpan.FromHours(6);

        /// <summary>
        /// Create services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public ScannerServices(IDiscoveryServices client, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _setupDelay = TimeSpan.FromSeconds(10);
            _lock = new SemaphoreSlim(1);
        }

        /// <inheritdoc/>
        public async Task ScanAsync() {
            await _lock.WaitAsync();
            try {
                await StopAsync();

                if (Mode != DiscoveryMode.Off) {
                    _discovery = new CancellationTokenSource();
                    _runner = Task.Run(() => RunAsync(
                        Request.Clone(), _setupDelay, _discovery.Token));
                    _setupDelay = null;
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _lock.Wait();
            try {
                StopAsync().Wait();
            }
            finally {
                _discovery?.Dispose();
                _discovery = null;
                _lock.Release();
                _lock.Dispose();
            }
        }

        /// <summary>
        /// Stop discovery
        /// </summary>
        /// <returns></returns>
        private async Task StopAsync() {
            Debug.Assert(_lock.CurrentCount == 0);

            // Try cancel discovery
            Try.Op(() => _discovery?.Cancel());
            // Wait for completion
            try {
                var completed = _runner;
                _runner = null;
                if (completed != null) {
                    await completed;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) {
                _logger.Error(ex, "Unexpected exception stopping current discover thread.");
            }
            finally {
                Try.Op(() => _discovery?.Dispose());
                _discovery = null;
            }
        }

        /// <summary>
        /// Scan and discover in continuous loop until stopped
        /// </summary>
        /// <param name="request"></param>
        /// <param name="delay"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(DiscoveryRequest request,
            TimeSpan? delay, CancellationToken ct) {

            if (delay != null) {
                try {
                    _logger.Debug("Delaying for {delay}...", delay);
                    await Task.Delay((TimeSpan)delay, ct);
                }
                catch (OperationCanceledException) {
                    _logger.Debug("Cancelled discovery start.");
                    return;
                }
            }

            _logger.Information("Starting discovery...");

            // Run scans until cancelled
            var retry = 0;
            while (!ct.IsCancellationRequested) {
                try {
                    await _client.DiscoverAsync(Request.Request, ct);
                    retry = 0;
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (TemporarilyBusyException) {
                    // Too many operations in progress - delay
                    _logger.Information("Too many operations in progress - retry later...");
                    retry++;
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Discovery error occurred - retry...");
                    retry++;
                }

                //
                // Delay next processing
                //
                try {
                    if (!ct.IsCancellationRequested) {
                        GC.Collect();
                        var idle = request.Configuration.IdleTimeBetweenScans ??
                            DefaultIdleTime;
                        if (retry > 0) {
                            // Insert a linear delay
                            var retryDelay = TimeSpan.FromMinutes(3 * retry);
                            if (idle > retryDelay) {
                                _logger.Information("Retry after {retryDelay}...", retryDelay);
                                idle = retryDelay;
                            }
                        }
                        if (idle.Ticks != 0) {
                            _logger.Debug("Idle for {idle}...", idle);
                            await Task.Delay(idle, ct);
                        }
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
            }

            _logger.Information("Cancelled discovery.");
        }

#pragma warning disable IDE0069 // Disposable fields should be disposed
        private CancellationTokenSource _discovery;
#pragma warning restore IDE0069 // Disposable fields should be disposed
        private Task _runner;
        private TimeSpan? _setupDelay;

        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private readonly IDiscoveryServices _client;
    }
}
