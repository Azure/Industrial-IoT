// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Supervisor {
    using Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Twin;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Client;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Stack;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Runtime;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.Devices.Edge;
    using Microsoft.Azure.Devices.Edge.Services;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Autofac;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Twin supervisor service
    /// </summary>
    public class OpcUaSupervisorServices : IOpcUaSupervisorServices, IDisposable {

        /// <summary>
        /// Create twin supervisor creating and managing twin instances
        /// </summary>
        public OpcUaSupervisorServices(IOpcUaClient client, IHostConfig config, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _container = CreateTwinContainer(client, logger);
        }

        /// <summary>
        /// Start twin
        /// </summary>
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
                var host = twinScope.Resolve<IEdgeHost>();
                // Start host
                await host.StartAsync();

                // Update endpoint edge controller
                var events = twinScope.Resolve<IEventEmitter>();
                await events.SendAsync("endpoint",
                    new JObject { new JProperty("TwinId", id) });

                _logger.Info($"{id} twin started.", () => { });
            }
            catch (Exception ex) {
                try {
                    await _lock.WaitAsync();
                    _twinScopes.Remove(id, out var scope);
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
                if (!_twinScopes.Remove(id, out twinScope)) {
                    _logger.Debug($"{id} twin not running.", () => { });
                    return;
                }
            }
            finally {
                _lock.Release();
            }

            _logger.Debug($"{id} twin stopping...", () => { });
            var host = twinScope.Resolve<IEdgeHost>();
            try {
                // Clear endpoint edge controller
                var events = twinScope.Resolve<IEventEmitter>();
                await events.SendAsync("endpoint",
                    new JObject { new JProperty("TwinId", null) });

                // Stop host async
                await host.StopAsync();
            }
            catch (UnauthorizedException) {}
            catch (IotHubCommunicationException) {}
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
        /// Build di container for twins
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static IContainer CreateTwinContainer(IOpcUaClient client, ILogger logger) {
            // Create container for all twin level scopes...
            var builder = new ContainerBuilder();

            // Register logger singleton instance
            builder.RegisterInstance(logger)
                .AsImplementedInterfaces().SingleInstance();

            // Register edge host module and twin state for the lifetime of the host
            builder.RegisterType<OpcUaTwinServices>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterModule<EdgeHostModule>();

            // Register opc ua client singleton instance
            builder.RegisterInstance(client)
                .AsImplementedInterfaces().SingleInstance();
            // Register opc ua services
            builder.RegisterType<OpcUaNodeServices>()
                .AsImplementedInterfaces();
            builder.RegisterType<OpcUaJsonVariantCodec>()
                .AsImplementedInterfaces();

            // Register twin controllers for scoped host instance
            builder.RegisterType<v1.Controllers.OpcUaTwinMethods>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<v1.Controllers.OpcUaTwinSettings>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Build twin container
            return builder.Build();
        }

        /// <summary>
        /// Twin host configuration wrapper
        /// </summary>
        private class TwinConfig : IOpcUaServicesConfig, IHostConfig {

            /// <summary>
            /// Create twin configuration
            /// </summary>
            /// <param name="config"></param>
            /// <param name="secret"></param>
            public TwinConfig(IHostConfig config, string endpointId, string secret) {
                BypassCertVerification = config.BypassCertVerification;
                Transport = config.Transport;
                HubConnectionString = GetEdgeConnectionString(config, endpointId, secret);
            }

            /// <summary>
            /// Twin configuration
            /// </summary>
            public string HubConnectionString { get; }
            public bool BypassCertVerification { get; }
            public TransportOption Transport { get; }

            /// <summary>
            /// Dummy Service configuration
            /// </summary>
            public string IoTHubConnString => null;
            public bool BypassProxy => true;

            /// <summary>
            /// Create new connection string from existing edge connection string.
            /// </summary>
            /// <param name="config"></param>
            /// <param name="endpointId"></param>
            /// <param name="secret"></param>
            /// <returns></returns>
            private static string GetEdgeConnectionString(IHostConfig config,
                string endpointId, string secret) {
                return config.HubConnectionString.Split(';',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().Split('='))
                    .Where(kv => kv.Length == 2 &&
                        !kv[0].Equals("DeviceId") &&
                        !kv[0].Equals("SharedAccessKey"))
                    .Append(new string[] { "DeviceId", endpointId })
                    .Append(new string[] { "SharedAccessKey", secret })
                    .Select(kv => $"{kv[0]}={kv[1]}")
                    .Aggregate((a, b) => $"{a};{b}");
            }
        }

        private readonly IOpcUaClient _client;
        private readonly ILogger _logger;
        private readonly IHostConfig _config;
        private readonly IContainer _container;

        private readonly SemaphoreSlim _lock =
            new SemaphoreSlim(1);
        private readonly Dictionary<string, ILifetimeScope> _twinScopes =
            new Dictionary<string, ILifetimeScope>();
    }
}

