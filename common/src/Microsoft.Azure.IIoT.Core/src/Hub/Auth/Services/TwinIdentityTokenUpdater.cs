// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IoTHub {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Auth.Models;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Writes Identity Token into twin
    /// </summary>
    public class TwinIdentityTokenUpdater : IHostProcess, IDisposable {

        /// <summary>
        /// Create writer
        /// </summary>
        /// <param name="passwordGenerator"></param>
        /// <param name="ioTHubTwinServices"></param>
        /// <param name="serializer"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public TwinIdentityTokenUpdater(IPasswordGenerator passwordGenerator,
            IIoTHubTwinServices ioTHubTwinServices, IIdentityTokenUpdaterConfig config,
            IJsonSerializer serializer, ILogger logger) {
            _passwordGenerator = passwordGenerator;
            _ioTHubTwinServices = ioTHubTwinServices;
            _serializer = serializer;
            _config = config;
            _logger = logger;
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
                _logger.Information("Updating identity tokens...");
                await UpdateIdentityTokensAsync(true, _cts.Token);
                await UpdateIdentityTokensAsync(false, _cts.Token);
                _logger.Information("Identity Token update finished.");
            }
            catch (OperationCanceledException) {
                // Cancel was called - dispose cancellation token
                _cts.Dispose();
                _cts = null;
                return;
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to update identity tokens.");
            }
            _updateTimer.Change(_config.UpdateInterval,
                Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Update all identity tokens
        /// </summary>
        /// <returns></returns>
        public async Task UpdateIdentityTokensAsync(bool modules, CancellationToken ct) {
            var query = $"SELECT * FROM devices{ (modules ? ".modules" : "") } WHERE " +
                $"IS_DEFINED(properties.reported.{Constants.IdentityTokenPropertyName})";
            string continuation = null;
            do {
                var response = await _ioTHubTwinServices.QueryDeviceTwinsAsync(
                    query, continuation, null, ct);
                foreach (var deviceTwin in response.Items) {
                    try {
                        await UpdateIdentityTokenInTwinAsync(deviceTwin, ct);
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Failed to update Identity Token for '{identity}'",
                            GetIdentity(deviceTwin));
                    }
                }
                continuation = response.ContinuationToken;
                ct.ThrowIfCancellationRequested();
            }
            while (continuation != null);
        }

        /// <summary>
        /// Update Identity Token
        /// </summary>
        /// <param name="deviceTwin"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task UpdateIdentityTokenInTwinAsync(DeviceTwinModel deviceTwin,
            CancellationToken ct) {
            var identity = GetIdentity(deviceTwin);
            _logger.Verbose("Updating Identity Token for '{identity}'...", identity);
            var key = await _passwordGenerator.GeneratePassword(_config.TokenLength,
                AllowedChars.All, true);
            var expires = DateTime.UtcNow.Add(_config.TokenLifetime);
            deviceTwin.Properties.Desired[Constants.IdentityTokenPropertyName] =
                _serializer.FromObject(new IdentityTokenTwinModel {
                    Key = key,
                    Identity = identity,
                    Expires = expires
                });
            await _ioTHubTwinServices.PatchAsync(deviceTwin, false, ct);
            _logger.Debug("Updated Identity Token for '{identity}'.", identity);
        }

        /// <summary>
        /// Get identity of device twin
        /// </summary>
        /// <param name="deviceTwin"></param>
        /// <returns></returns>
        private static string GetIdentity(DeviceTwinModel deviceTwin) {
            var identity = deviceTwin.Id;
            if (!string.IsNullOrEmpty(deviceTwin.ModuleId)) {
                identity += $"/{deviceTwin.ModuleId}";
            }
            return identity;
        }

        private readonly IIdentityTokenUpdaterConfig _config;
        private readonly IIoTHubTwinServices _ioTHubTwinServices;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly IPasswordGenerator _passwordGenerator;
        private readonly Timer _updateTimer;
#pragma warning disable IDE0069 // Disposable fields should be disposed
        private CancellationTokenSource _cts;
#pragma warning restore IDE0069 // Disposable fields should be disposed
    }
}