﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Events;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Registry service event client
    /// </summary>
    public class RegistryServiceEvents : IRegistryServiceEvents, IRegistryEventApi {

        /// <summary>
        /// Event client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        /// <param name="client"></param>
        public RegistryServiceEvents(IHttpClient httpClient, ICallbackClient client,
            IEventsConfig config, ISerializer serializer) : this(httpClient, client,
                config?.OpcUaEventsServiceUrl, config.OpcUaEventsServiceResourceId,
                serializer) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="client"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        /// <param name="serializer"></param>
        public RegistryServiceEvents(IHttpClient httpClient, ICallbackClient client,
            string serviceUri, string resourceId, ISerializer serializer = null) {
            if (string.IsNullOrEmpty(serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the events micro service.");
            }
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serviceUri = serviceUri;
            _resourceId = resourceId;
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeApplicationEventsAsync(
            Func<ApplicationEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetHubAsync($"{_serviceUri}/v2/applications/events", _resourceId);
            try {
                var registration = registrar.Register(EventTargets.ApplicationEventTarget, callback);
                try {
                    return new AsyncDisposable(registration);
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
            Func<EndpointEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetHubAsync($"{_serviceUri}/v2/endpoints/events", _resourceId);
            try {
                var registration = registrar.Register(EventTargets.EndpointEventTarget, callback);
                try {
                    return new AsyncDisposable(registration);
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
            Func<GatewayEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetHubAsync($"{_serviceUri}/v2/gateways/events", _resourceId);
            try {
                var registration = registrar.Register(EventTargets.GatewayEventTarget, callback);
                try {
                    return new AsyncDisposable(registration);
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
            Func<SupervisorEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetHubAsync($"{_serviceUri}/v2/supervisors/events", _resourceId);
            try {
                var registration = registrar.Register(EventTargets.SupervisorEventTarget, callback);
                try {
                    return new AsyncDisposable(registration);
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
            Func<DiscovererEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetHubAsync($"{_serviceUri}/v2/discovery/events", _resourceId);
            try {
                var registration = registrar.Register(EventTargets.DiscovererEventTarget, callback);
                try {
                    return new AsyncDisposable(registration);
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
            Func<PublisherEventApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetHubAsync($"{_serviceUri}/v2/publishers/events", _resourceId);
            try {
                var registration = registrar.Register(EventTargets.PublisherEventTarget, callback);
                try {
                    return new AsyncDisposable(registration);
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
            string discovererId, Func<DiscoveryProgressApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetHubAsync($"{_serviceUri}/v2/discovery/events", _resourceId);
            try {
                var registration = registrar.Register(EventTargets.DiscoveryProgressTarget, callback);
                try {
                    await SubscribeDiscoveryProgressByDiscovererIdAsync(discovererId,
                        registrar.ConnectionId, CancellationToken.None);
                    return new AsyncDisposable(registration,
                        () => UnsubscribeDiscoveryProgressByDiscovererIdAsync(discovererId,
                            registrar.ConnectionId, CancellationToken.None));
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
            string requestId, Func<DiscoveryProgressApiModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetHubAsync($"{_serviceUri}/v2/discovery/events", _resourceId);
            try {
                var registration = registrar.Register(EventTargets.DiscoveryProgressTarget, callback);
                try {
                    await SubscribeDiscoveryProgressByRequestIdAsync(requestId, registrar.ConnectionId,
                        CancellationToken.None);
                    return new AsyncDisposable(registration,
                        () => UnsubscribeDiscoveryProgressByRequestIdAsync(requestId,
                            registrar.ConnectionId, CancellationToken.None));
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
        public async Task SubscribeDiscoveryProgressByDiscovererIdAsync(string discovererId,
            string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/{discovererId}/events", _resourceId);
            _serializer.SerializeToRequest(request, userId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task SubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/requests/{requestId}/events", _resourceId);
            _serializer.SerializeToRequest(request, userId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribeDiscoveryProgressByDiscovererIdAsync(string discovererId,
            string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/{discovererId}/events/{userId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/requests/{requestId}/events/{userId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly string _resourceId;
        private readonly ISerializer _serializer;
        private readonly ICallbackClient _client;
    }
}
