// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR.Services {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Services;
    using Microsoft.Azure.SignalR.Management;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Security.Claims;
    using Serilog;

    /// <summary>
    /// Publish subscriber service built using signalr
    /// </summary>
    public class SignalRServiceHost : IIdentityTokenGenerator, IEndpoint,
        ICallbackInvoker, IGroupRegistration, IHostProcess, IHealthCheck, IDisposable {

        /// <inheritdoc/>
        public string Resource { get; }

        /// <inheritdoc/>
        public Uri EndpointUrl => new Uri(_serviceManager.GetClientEndpoint(Resource));

        /// <summary>
        /// Create signalR event bus
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public SignalRServiceHost(ISignalRServiceConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(config?.SignalRConnString)) {
                throw new ArgumentNullException(nameof(config.SignalRConnString));
            }
            _serviceManager = new ServiceManagerBuilder().WithOptions(option => {
                option.ConnectionString = config.SignalRConnString;
                option.ServiceTransportType = ServiceTransportType.Persistent;
            }).Build();
            Resource = !string.IsNullOrEmpty(config.SignalRHubName) ?
                config.SignalRHubName : "default";
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken ct) {
            var hub = _hub;
            try {
                if (hub == null) {
                    hub = await _serviceManager.CreateHubContextAsync(Resource);
                }
                await hub.Clients.All.SendCoreAsync("ping", new object[0], ct);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex) {
                return new HealthCheckResult(context.Registration.FailureStatus,
                    exception: ex);
            }
            finally {
                if (hub != _hub) {
                    await hub.DisposeAsync();
                }
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            try {
                if (_hub != null) {
                    _logger.Debug("SignalR service host already running.");
                    return;
                }
                _logger.Debug("Starting SignalR service host...");
                _hub = await _serviceManager.CreateHubContextAsync(Resource);
                 _logger.Information("SignalR service host started.");
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to start SignalR service host.");
                throw ex;
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            try {
                if (_hub != null) {
                    _logger.Debug("Stopping SignalR service host...");
                    await _hub.DisposeAsync();
                    _logger.Information("SignalR service host stopped.");
                }
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Failed to stop SignalR service host.");
            }
            finally {
                _hub = null;
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Op(() => StopAsync().Wait());
        }

        /// <inheritdoc/>
        public IdentityTokenModel GenerateIdentityToken(string userId,
            IList<Claim> claims, TimeSpan? lifeTime) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            if (lifeTime == null) {
                lifeTime = TimeSpan.FromMinutes(5);
            }
            return new IdentityTokenModel {
                Identity = userId,
                Key = _serviceManager.GenerateClientAccessToken(
                    Resource, userId, claims, lifeTime),
                Expires = DateTime.UtcNow + lifeTime.Value
            };
        }

        /// <inheritdoc/>
        public Task BroadcastAsync(string method, object[] arguments,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(method)) {
                throw new ArgumentNullException(nameof(method));
            }
            return _hub.Clients.All.SendCoreAsync(method,
                arguments ?? new object[0], ct);
        }

        /// <inheritdoc/>
        public Task UnicastAsync(string target, string method, object[] arguments,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(method)) {
                throw new ArgumentNullException(nameof(method));
            }
            if (string.IsNullOrEmpty(target)) {
                throw new ArgumentNullException(nameof(target));
            }
            return _hub.Clients.User(target).SendCoreAsync(method,
                arguments ?? new object[0], ct);
        }

        /// <inheritdoc/>
        public Task MulticastAsync(string group, string method, object[] arguments,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(method)) {
                throw new ArgumentNullException(nameof(method));
            }
            if (string.IsNullOrEmpty(group)) {
                throw new ArgumentNullException(nameof(group));
            }
            return _hub.Clients.Group(group).SendCoreAsync(method,
                arguments ?? new object[0], ct);
        }

        /// <inheritdoc/>
        public Task SubscribeAsync(string group, string client,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(client)) {
                throw new ArgumentNullException(nameof(client));
            }
            if (string.IsNullOrEmpty(group)) {
                throw new ArgumentNullException(nameof(group));
            }
            return _hub.UserGroups.AddToGroupAsync(client, group, ct);
        }

        /// <inheritdoc/>
        public Task UnsubscribeAsync(string group, string client,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(client)) {
                throw new ArgumentNullException(nameof(client));
            }
            if (string.IsNullOrEmpty(group)) {
                throw new ArgumentNullException(nameof(group));
            }
            return _hub.UserGroups.RemoveFromGroupAsync(client, group, ct);
        }

        private IServiceHubContext _hub;
        private readonly ILogger _logger;
        private readonly IServiceManager _serviceManager;
    }
}