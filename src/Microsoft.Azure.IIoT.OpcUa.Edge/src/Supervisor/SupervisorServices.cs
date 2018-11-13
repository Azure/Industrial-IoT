// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Supervisor {
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Autofac;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin supervisor service
    /// </summary>
    public class SupervisorServices : IActivationServices<string>, IDisposable {

        /// <summary>
        /// Create twin supervisor creating and managing twin instances
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="config"></param>
        /// <param name="events"></param>
        /// <param name="logger"></param>
        public SupervisorServices(IContainerFactory factory, IModuleConfig config,
            IEventEmitter events, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _container = _factory.Create();
        }

        /// <summary>
        /// Start twin
        /// </summary>
        /// <param name="id"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public async Task ActivateTwinAsync(string id, string secret) {
            try {
                await _lock.WaitAsync();
                if (_twinHosts.TryGetValue(id, out var twin) && twin.Running) {
                    _logger.Debug($"{id} twin already running.");
                    return;
                }
                _logger.Debug($"{id} twin starting...");

                _twinHosts.Remove(id);
                _twinHosts.Add(id, new TwinHost(this,
                    new TwinConfig(_config, id, secret)));

                _logger.Info($"{id} twin started.");
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop twin by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DeactivateTwinAsync(string id) {
            TwinHost twin;
            try {
                await _lock.WaitAsync();
                if (!_twinHosts.TryGetValue(id, out twin)) {
                    _logger.Debug($"{id} twin not running.");
                    return;
                }
                _twinHosts.Remove(id);
            }
            finally {
                _lock.Release();
            }
            await StopOneTwinAsync(id, twin);
        }

        /// <inheritdoc/>
        public void Dispose() {
            try {
                StopAllTwinsAsync().Wait();
                _container.Dispose();
            }
            catch (Exception e) {
                _logger.Error("Failure in supervisor disposing.", e);
            }
        }

        /// <summary>
        /// Stop one twin
        /// </summary>
        /// <param name="id"></param>
        /// <param name="twin"></param>
        /// <returns></returns>
        private async Task StopOneTwinAsync(string id, TwinHost twin) {
            _logger.Debug($"{id} twin is stopped...");
            try {
                // Stop host async
                await twin.StopAsync();
            }
            catch (Exception ex) {
                // BUGBUG: IoT Hub client SDK throws general exceptions independent
                // of what actually happened.  Instead of parsing the message,
                // just continue.
                _logger.Debug($"{id} twin stopping raised exception, continue...",
                    () => ex);
            }
            finally {
                twin.Dispose();
            }
            _logger.Info($"{id} twin stopped.");
        }

        /// <summary>
        /// Stop all twins
        /// </summary>
        /// <returns></returns>
        private async Task StopAllTwinsAsync() {
            IList<KeyValuePair<string, TwinHost>> twins;
            try {
                await _lock.WaitAsync();
                twins = _twinHosts.ToList();
                _twinHosts.Clear();
            }
            finally {
                _lock.Release();
            }
            await Task.WhenAll(twins
                .Select(kv => StopOneTwinAsync(kv.Key, kv.Value))
                .ToArray());
        }

        /// <summary>
        /// Runs a twin device connected to transparent gateway
        /// </summary>
        private class TwinHost : IDisposable {

            /// <summary>
            /// Whether the host is running
            /// </summary>
            public bool Running => !(_runner?.IsCompleted ?? true);

            /// <summary>
            /// Create runner
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="config"></param>
            public TwinHost(SupervisorServices outer, IModuleConfig config) {
                _outer = outer;
                // Create twin scoped component context for the host
                _scope = outer._container.BeginLifetimeScope(builder => {
                    builder.RegisterInstance(config)
                        .AsImplementedInterfaces().SingleInstance();
                });
                _cts = new CancellationTokenSource();
                _runner = Task.Run(RunAsync);
            }

            /// <inheritdoc/>
            public void Dispose() => StopAsync().Wait();

            /// <summary>
            /// Shutdown twin host
            /// </summary>
            /// <returns></returns>
            public async Task StopAsync() {
                if (_scope != null) {
                    try {
                        // Cancel runner
                        _cts.Cancel();
                        await _runner;
                    }
                    catch (OperationCanceledException) { }
                    finally {
                        _scope.Dispose();
                        _scope = null;
                    }
                }
            }

            /// <summary>
            /// Run module host for the twin
            /// </summary>
            /// <returns></returns>
            private async Task RunAsync() {
                var host = _scope.Resolve<IModuleHost>();

                var retryCount = 0;
                var cancel = new TaskCompletionSource<bool>();
                _cts.Token.Register(() => cancel.TrySetResult(true));
                while (!_cts.Token.IsCancellationRequested) {
                    // Wait until the module unloads or is cancelled
                    var reset = new TaskCompletionSource<bool>();
                    try {
                        await host.StartAsync("twin", _outer._events.SiteId,
                            "OpcTwin", () => reset.TrySetResult(true));

                        // Reset retry counter on success
                        retryCount = 0;
                        await Task.WhenAny(cancel.Task, reset.Task);
                    }
                    catch (Exception ex) {
                        var logger = _scope.Resolve<ILogger>();

                        var notFound = ex.GetFirstOf<DeviceNotFoundException>();
                        if (notFound != null) {
                            logger.Info("Twin was deleted - exit host...",
                                notFound);
                            throw notFound;
                        }
                        var auth = ex.GetFirstOf<UnauthorizedException>();
                        if (auth != null) {
                            logger.Info("Twin not authorized using given " +
                                "secret - exit host...", auth);
                            throw auth;
                        }

                        // Linearly delay on exception since we get these when
                        // the twin was deleted.
                        await Try.Async(() =>
                            Task.Delay(kRetryDelayMs * retryCount, _cts.Token));
                        if (_cts.IsCancellationRequested) {
                            // Done.
                            break;
                        }
                        if (retryCount++ > kMaxRetryCount) {
                            logger.Error($"Error #{retryCount} in twin host - " +
                                $"exit host...", ex);
                            throw ex;
                        }
                        logger.Error($"Error #{retryCount} in twin host - " +
                            $"restarting...", ex);
                    }
                    finally {
                        await host.StopAsync();
                    }
                }
            }

            private const int kMaxRetryCount = 30;
            private const int kRetryDelayMs = 5000;

            private ILifetimeScope _scope;
            private readonly SupervisorServices _outer;
            private readonly CancellationTokenSource _cts;
            private readonly Task _runner;
        }

        /// <summary>
        /// Twin host configuration wrapper
        /// </summary>
        private class TwinConfig : IModuleConfig {

            /// <summary>
            /// Create twin configuration
            /// </summary>
            /// <param name="config"></param>
            /// <param name="endpointId"></param>
            /// <param name="secret"></param>
            public TwinConfig(IModuleConfig config, string endpointId, string secret) {
                BypassCertVerification = config.BypassCertVerification;
                Transport = config.Transport;
                EdgeHubConnectionString = GetEdgeHubConnectionString(config,
                    endpointId, secret);
            }

            /// <summary>
            /// Twin configuration
            /// </summary>
            public string EdgeHubConnectionString { get; }
            public bool BypassCertVerification { get; }
            public TransportOption Transport { get; }

            /// <summary>
            /// Create new connection string from existing EdgeHubConnectionString.
            /// </summary>
            /// <param name="config"></param>
            /// <param name="endpointId"></param>
            /// <param name="secret"></param>
            /// <returns></returns>
            private static string GetEdgeHubConnectionString(IModuleConfig config,
                string endpointId, string secret) {

                var cs = config.EdgeHubConnectionString;
                if (string.IsNullOrEmpty(cs)) {
                    // Retrieve information from environment
                    var hostName = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");
                    if (string.IsNullOrEmpty(hostName)) {
                        throw new InvalidConfigurationException(
                            "Missing IOTEDGE_IOTHUBHOSTNAME variable in environment");
                    }
                    cs = $"HostName={hostName};DeviceId={endpointId};SharedAccessKey={secret}";
                    var edgeName = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");
                    if (!string.IsNullOrEmpty(edgeName)) {
                        cs += $";GatewayHostName={edgeName}";
                    }
                }
                else {
                    // Use existing connection string as a master plan
                    var lookup = cs
                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().Split('='))
                        .ToDictionary(s => s[0].ToLowerInvariant(), v => v[1]);
                    if (!lookup.TryGetValue("hostname", out var hostName) ||
                        string.IsNullOrEmpty(hostName)) {
                        throw new InvalidConfigurationException(
                            "Missing HostName in connection string");
                    }
                    cs = $"HostName={hostName};DeviceId={endpointId};SharedAccessKey={secret}";
                    if (lookup.TryGetValue("gatewayhostname", out var edgeName) &&
                        !string.IsNullOrEmpty(edgeName)) {
                        cs += $";GatewayHostName={edgeName}";
                    }
                }
                return cs;
            }
        }

        private readonly ILogger _logger;
        private readonly IModuleConfig _config;
        private readonly IEventEmitter _events;
        private readonly IContainerFactory _factory;
        private readonly IContainer _container;

        private readonly SemaphoreSlim _lock =
            new SemaphoreSlim(1);
        private readonly Dictionary<string, TwinHost> _twinHosts =
            new Dictionary<string, TwinHost>();
    }
}

