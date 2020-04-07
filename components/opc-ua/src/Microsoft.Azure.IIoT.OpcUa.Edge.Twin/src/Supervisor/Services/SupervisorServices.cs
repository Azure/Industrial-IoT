// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Supervisor.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Autofac;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor services
    /// </summary>
    public class SupervisorServices : IActivationServices<string>, ISupervisorServices,
        IDisposable {

        /// <summary>
        /// Create supervisor creating and managing twin instances
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="config"></param>
        /// <param name="events"></param>
        /// <param name="process"></param>
        /// <param name="logger"></param>
        public SupervisorServices(IContainerFactory factory, IModuleConfig config,
            IEventEmitter events, IProcessControl process, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public async Task ActivateEndpointAsync(string id, string secret, CancellationToken ct) {
            await _lock.WaitAsync();
            try {
                if (_twinHosts.TryGetValue(id, out var twin) && twin.Running) {
                    _logger.Debug("{id} twin already running.", id);
                    return;
                }
                _logger.Debug("{id} twin starting...", id);
                _twinHosts.Remove(id);
                var host = new TwinHost(this, _config, id, secret, _logger);
                _twinHosts.Add(id, host);

                //
                // This starts and waits for the twin to be started - versus attaching which
                // represents the state of the actived and supervised twins in the supervisor
                // device twin.
                //
                await host.Started;
                _logger.Information("{id} twin started.", id);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeactivateEndpointAsync(string id, CancellationToken ct) {
            TwinHost twin;
            await _lock.WaitAsync();
            try {
                if (!_twinHosts.TryGetValue(id, out twin)) {
                    _logger.Debug("{id} twin not running.", id);
                    return;
                }
                _twinHosts.Remove(id);
            }
            finally {
                _lock.Release();
            }
            //
            // This stops and waits for the twin to be stopped - versus detaching which
            // represents the state of the supervised twins through the supervisor device
            // twin.
            //
            await StopOneTwinAsync(id, twin);
        }

        /// <inheritdoc/>
        public async Task AttachEndpointAsync(string id, string secret) {
            await _lock.WaitAsync();
            try {
                if (_twinHosts.TryGetValue(id, out var twin)) {
                    _logger.Debug("{id} twin already attached.", id);
                    return;
                }
                _logger.Debug("Attaching endpoint {id} twin...", id);
                var host = new TwinHost(this, _config, id, secret, _logger);
                _twinHosts.Add(id, host);

                _logger.Information("{id} twin attached to module.", id);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DetachEndpointAsync(string id) {
            TwinHost twin;
            await _lock.WaitAsync();
            try {
                if (!_twinHosts.TryGetValue(id, out twin)) {
                    _logger.Debug("{id} twin not attached.", id);
                    return;
                }

                // Test whether twin is in error state and only then remove
                if (!twin.Running) {
                    // Detach twin that is not running anymore
                    _twinHosts.Remove(id);
                }
                _logger.Information("{id} twin detached from module.", id);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<SupervisorStatusModel> GetStatusAsync(CancellationToken ct) {
            await _lock.WaitAsync();
            try {
                var endpoints = _twinHosts
                    .Select(h => new EndpointActivationStatusModel {
                        Id = h.Key,
                        ActivationState = h.Value.Status
                    });
                return new SupervisorStatusModel {
                    Endpoints = endpoints.ToList(),
                    DeviceId = _events.DeviceId,
                    ModuleId = _events.ModuleId,
                    SiteId = _events.SiteId
                };
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task ResetAsync(CancellationToken ct) {
            _process.Reset();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose() {
            try {
                StopAllTwinsAsync().Wait();
                _lock.Dispose();
            }
            catch (Exception e) {
                _logger.Error(e, "Failure in supervisor disposing.");
            }
        }

        /// <summary>
        /// Stop one twin
        /// </summary>
        /// <param name="id"></param>
        /// <param name="twin"></param>
        /// <returns></returns>
        private async Task StopOneTwinAsync(string id, TwinHost twin) {
            _logger.Debug("{id} twin is stopping...", id);
            try {
                // Stop host async
                await twin.StopAsync();
            }
            catch (Exception ex) {
                // BUGBUG: IoT Hub client SDK throws general exceptions independent
                // of what actually happened.  Instead of parsing the message,
                // just continue.
                _logger.Debug(ex,
                    "{id} twin stopping raised exception, continue...", id);
            }
            finally {
                twin.Dispose();
            }
            _logger.Information("{id} twin stopped.", id);
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
        private class TwinHost : IDisposable, IProcessControl, IModuleConfig {

            /// <summary>
            /// Whether the host is running
            /// </summary>
            public bool Running => !(_runner?.IsCompleted ?? true);

            /// <summary>
            /// Activation state
            /// </summary>
            public EndpointActivationState Status { get; private set; }

            /// <summary>
            /// Wait until running
            /// </summary>
            public Task Started => _started.Task;

            /// <inheritdoc/>
            public string EdgeHubConnectionString { get; }
            /// <inheritdoc/>
            public bool BypassCertVerification { get; }
            /// <inheritdoc/>
            public TransportOption Transport { get; }

            /// <summary>
            /// Create runner
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="config"></param>
            /// <param name="endpointId"></param>
            /// <param name="secret"></param>
            /// <param name="logger"></param>
            public TwinHost(SupervisorServices outer, IModuleConfig config,
                string endpointId, string secret, ILogger logger) {
                _outer = outer;
                _product = "OpcTwin_" + GetType().Assembly.GetReleaseVersion().ToString();
                _logger = (logger ?? Log.Logger).ForContext("SourceContext", new {
                    endpointId,
                    product = _product
                }, true);

                BypassCertVerification = config.BypassCertVerification;
                Transport = config.Transport;
                EdgeHubConnectionString = GetEdgeHubConnectionString(config,
                    endpointId, secret);

                // Create twin scoped component context for the host
                _container = outer._factory.Create(builder => {
                    builder.RegisterInstance(this)
                        .AsImplementedInterfaces().SingleInstance();
                });

                _cts = new CancellationTokenSource();
                _reset = new TaskCompletionSource<bool>();
                _started = new TaskCompletionSource<bool>();
                Status = EndpointActivationState.Activated;
                _runner = Task.Run(RunAsync);
            }

            /// <inheritdoc/>
            public void Dispose() {
                StopAsync().Wait();
                _cts.Dispose();
            }

            /// <inheritdoc/>
            public void Reset() {
                _reset?.TrySetResult(true);
            }

            /// <inheritdoc/>
            public void Exit(int exitCode) {
                _cts.Cancel();
            }

            /// <summary>
            /// Shutdown twin host
            /// </summary>
            /// <returns></returns>
            public async Task StopAsync() {
                if (_container != null) {
                    try {
                        // Cancel runner
                        _cts.Cancel();
                        await _runner;
                    }
                    catch (OperationCanceledException) { }
                    finally {
                        _container.Dispose();
                        _container = null;
                    }
                }
            }

            /// <summary>
            /// Run module host for the twin
            /// </summary>
            /// <returns></returns>
            private async Task RunAsync() {
                var host = _container.Resolve<IModuleHost>();

                var retryCount = 0;
                var cancel = new TaskCompletionSource<bool>();
                _cts.Token.Register(() => cancel.TrySetResult(true));
                var product = "OpcTwin_" +
                    GetType().Assembly.GetReleaseVersion().ToString();
                _logger.Information("Starting twin host...");
                while (!_cts.Token.IsCancellationRequested) {
                    // Wait until the module unloads or is cancelled
                    try {
                        await host.StartAsync("twin", _outer._events.SiteId,
                            product, this);
                        Status = EndpointActivationState.ActivatedAndConnected;
                        _started.TrySetResult(true);
                        _logger.Debug("Twin host (re-)started.");
                        // Reset retry counter on success
                        retryCount = 0;
                        await Task.WhenAny(cancel.Task, _reset.Task);
                        _reset = new TaskCompletionSource<bool>();
                        _logger.Debug("Twin reset requested...");
                    }
                    catch (Exception ex) {
                        Status = EndpointActivationState.Activated;

                        var notFound = ex.GetFirstOf<DeviceNotFoundException>();
                        if (notFound != null) {
                            _logger.Information(notFound,
                                "Twin was deleted - exit host...");
                            _started.TrySetException(notFound);
                            return;
                        }
                        var auth = ex.GetFirstOf<UnauthorizedException>();
                        if (auth != null) {
                            _logger.Information(auth,
                                "Twin not authorized using given secret - exit host...");
                            _started.TrySetException(auth);
                            return;
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
                            _logger.Error(ex,
                                "Error #{retryCount} in twin host - exit host...",
                                retryCount);
                            return;
                        }
                        _logger.Error(ex,
                            "Error #{retryCount} in twin host - restarting...",
                            retryCount);
                    }
                    finally {
                        _logger.Debug("Stopping twin...");
                        Status = EndpointActivationState.Activated;
                        await host.StopAsync(true);
                        _logger.Information("Twin stopped.");
                        _started.TrySetResult(false); // Cancelled before started
                    }
                }
                _logger.Information("Exiting twin host.");
            }

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
                    var edgeName = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");
                    if (string.IsNullOrEmpty(edgeName)) {
                        cs = $"HostName={hostName};DeviceId={endpointId};SharedAccessKey={secret}";
                    }
                    else {
                        cs = $"HostName={hostName};DeviceId={endpointId};GatewayHostName={edgeName};SharedAccessKey={secret}";
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

                    if (!lookup.TryGetValue("GatewayHostName", out var edgeName) ||
                         string.IsNullOrEmpty(edgeName)) {
                        cs = $"HostName={hostName};DeviceId={endpointId};SharedAccessKey={secret}";
                    }
                    else {
                        cs = $"HostName={hostName};DeviceId={endpointId};GatewayHostName={edgeName};SharedAccessKey={secret}";
                    }
                }
                return cs;
            }

            private const int kMaxRetryCount = 30;
            private const int kRetryDelayMs = 5000;

            private TaskCompletionSource<bool> _reset;
            private readonly TaskCompletionSource<bool> _started;
            private ILifetimeScope _container;
            private readonly string _product;
            private readonly SupervisorServices _outer;
            private readonly ILogger _logger;
            private readonly CancellationTokenSource _cts;
            private readonly Task _runner;
        }

        private readonly ILogger _logger;
        private readonly IModuleConfig _config;
        private readonly IEventEmitter _events;
        private readonly IProcessControl _process;
        private readonly IContainerFactory _factory;

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, TwinHost> _twinHosts =
            new Dictionary<string, TwinHost>();
    }
}

