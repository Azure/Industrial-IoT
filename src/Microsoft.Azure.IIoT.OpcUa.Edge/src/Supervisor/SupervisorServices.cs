// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Supervisor {
    using Autofac;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin supervisor service
    /// </summary>
    public class SupervisorServices : ISupervisorServices, IDisposable {

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
        public async Task StartTwinAsync(string id, string secret) {
            ILifetimeScope twinScope;
            try {
                await _lock.WaitAsync();
                if (_twinScopes.ContainsKey(id)) {
                    _logger.Debug($"{id} twin already running.", () => { });
                    return;
                }
                _logger.Debug($"{id} twin starting...", () => { });

                // Create twin scoped component context with twin host
                twinScope = _container.BeginLifetimeScope(builder => {
                    // Register twin scope level configuration...
                    var config = new TwinConfig(_config, id, secret);
                    builder.RegisterInstance(config)
                        .AsImplementedInterfaces().SingleInstance();
                });
                // host is disposed when twin scope is disposed...
                _twinScopes.Add(id, twinScope);
            }
            finally {
                _lock.Release();
            }

            try {
                var host = twinScope.Resolve<IModuleHost>();
                await host.StartAsync("twin", _events.SiteId, "OpcTwin");
                _logger.Info($"{id} twin started.", () => { });
            }
            catch (Exception ex) {
                try {
                    await _lock.WaitAsync();
                    _twinScopes.Remove(id);
                }
                finally {
                    _lock.Release();
                }
                twinScope.Dispose();
                _logger.Error($"{id} twin failed to start...", () => ex);
                throw ex;
            }
        }

        /// <summary>
        /// Stop twin
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task StopTwinAsync(string id) {
            ILifetimeScope twinScope;
            try {
                await _lock.WaitAsync();
                if (!_twinScopes.TryGetValue(id, out twinScope)) {
                    _logger.Debug($"{id} twin not running.", () => { });
                    return;
                }
                _twinScopes.Remove(id);
            }
            finally {
                _lock.Release();
            }

            _logger.Debug($"{id} twin stopping...", () => { });
            var host = twinScope.Resolve<IModuleHost>();
            try {
                // Clear endpoint module controller
                var events = twinScope.Resolve<IEventEmitter>();

                // Stop host async
                await host.StopAsync();
            }
            catch (Exception ex) {
                // _logger.Error($"{id} twin failed to stop...", () => ex);
                // throw ex;

                // BUGBUG: IoT Hub client SDK throws general exceptions independent
                // of what actually happened.  Instead of parsing the message,
                // just continue.
                _logger.Debug($"{id} twin stop raised exception, continue...",
                    () => ex);
            }
            finally {
                twinScope.Dispose();
            }
            _logger.Info($"{id} twin stopped.", () => { });
        }

        /// <summary>
        /// Dispose container and remaining active nested scopes...
        /// </summary>
        public void Dispose() {
            // _container.Dispose();
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
                EdgeHubConnectionString = GetEdgeHubConnectionString(config, endpointId, secret);
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
        private readonly Dictionary<string, ILifetimeScope> _twinScopes =
            new Dictionary<string, ILifetimeScope>();
    }
}

