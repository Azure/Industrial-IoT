// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry service event client
    /// </summary>
    public class RegistryServiceEvents : IRegistryServiceEvents {

        /// <summary>
        /// Event client
        /// </summary>
        /// <param name="api"></param>
        /// <param name="client"></param>
        public RegistryServiceEvents(IRegistryServiceApi api, ICallbackClient client) {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeApplicationEventsAsync(
            string userId, Func<ApplicationEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetRegistrarAsync(userId);
            try {
                var registration = registrar.Register(EventTargets.ApplicationEventTarget, callback);
                try {
                    await _api.SubscribeApplicationEventsAsync(registrar.UserId);
                    return new AsyncDisposable(registration,
                        () => _api.UnsubscribeApplicationEventsAsync(registrar.UserId));
                }
                catch {
                    registration.Dispose();
                    throw;
                }
            }
            catch {
                registrar.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeEndpointEventsAsync(
            string userId, Func<EndpointEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetRegistrarAsync(userId);
            try {
                var registration = registrar.Register(EventTargets.EndpointEventTarget, callback);
                try {
                    await _api.SubscribeEndpointEventsAsync(registrar.UserId);
                    return new AsyncDisposable(registration,
                        () => _api.UnsubscribeEndpointEventsAsync(registrar.UserId));
                }
                catch {
                    registration.Dispose();
                    throw;
                }
            }
            catch {
                registrar.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeGatewayEventsAsync(
            string userId, Func<GatewayEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetRegistrarAsync(userId);
            try {
                var registration = registrar.Register(EventTargets.GatewayEventTarget, callback);
                try {
                    await _api.SubscribeGatewayEventsAsync(registrar.UserId);
                    return new AsyncDisposable(registration,
                        () => _api.UnsubscribeGatewayEventsAsync(registrar.UserId));
                }
                catch {
                    registration.Dispose();
                    throw;
                }
            }
            catch {
                registrar.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeSupervisorEventsAsync(
            string userId, Func<SupervisorEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetRegistrarAsync(userId);
            try {
                var registration = registrar.Register(EventTargets.SupervisorEventTarget, callback);
                try {
                    await _api.SubscribeSupervisorEventsAsync(registrar.UserId);
                    return new AsyncDisposable(registration,
                        () => _api.UnsubscribeSupervisorEventsAsync(registrar.UserId));
                }
                catch {
                    registration.Dispose();
                    throw;
                }
            }
            catch {
                registrar.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeDiscovererEventsAsync(
            string userId, Func<DiscovererEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetRegistrarAsync(userId);
            try {
                var registration = registrar.Register(EventTargets.DiscovererEventTarget, callback);
                try {
                    await _api.SubscribeDiscovererEventsAsync(registrar.UserId);
                    return new AsyncDisposable(registration,
                        () => _api.UnsubscribeDiscovererEventsAsync(registrar.UserId));
                }
                catch {
                    registration.Dispose();
                    throw;
                }
            }
            catch {
                registrar.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribePublisherEventsAsync(
            string userId, Func<PublisherEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetRegistrarAsync(userId);
            try {
                var registration = registrar.Register(EventTargets.PublisherEventTarget, callback);
                try {
                    await _api.SubscribePublisherEventsAsync(registrar.UserId);
                    return new AsyncDisposable(registration,
                        () => _api.UnsubscribePublisherEventsAsync(registrar.UserId));
                }
                catch {
                    registration.Dispose();
                    throw;
                }
            }
            catch {
                registrar.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeDiscoveryProgressByDiscovererIdAsync(
            string discovererId, string userId, Func<DiscoveryProgressApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetRegistrarAsync(userId);
            try {
                var registration = registrar.Register(EventTargets.DiscoveryProgressTarget, callback);
                try {
                    await _api.SubscribeDiscoveryProgressByDiscovererIdAsync(discovererId,
                        registrar.UserId);
                    return new AsyncDisposable(registration,
                        () => _api.UnsubscribeDiscoveryProgressByDiscovererIdAsync(discovererId,
                            registrar.UserId));
                }
                catch {
                    registration.Dispose();
                    throw;
                }
            }
            catch {
                registrar.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeDiscoveryProgressByRequestIdAsync(
            string requestId, string userId, Func<DiscoveryProgressApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetRegistrarAsync(userId);
            try {
                var registration = registrar.Register(EventTargets.DiscoveryProgressTarget, callback);
                try {
                    await _api.SubscribeDiscoveryProgressByRequestIdAsync(requestId, registrar.UserId);
                    return new AsyncDisposable(registration,
                        () => _api.UnsubscribeDiscoveryProgressByRequestIdAsync(requestId,
                            registrar.UserId));
                }
                catch {
                    registration.Dispose();
                    throw;
                }
            }
            catch {
                registrar.Dispose();
                throw;
            }
        }

        private readonly IRegistryServiceApi _api;
        private readonly ICallbackClient _client;
    }
}
