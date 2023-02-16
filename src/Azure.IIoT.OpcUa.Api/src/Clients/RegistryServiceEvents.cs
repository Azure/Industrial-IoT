// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Clients {
    using Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

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
            IEventsConfig config, ISerializer serializer) :
            this(httpClient, client, config?.OpcUaEventsServiceUrl, serializer) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="client"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public RegistryServiceEvents(IHttpClient httpClient, ICallbackClient client,
            string serviceUri, ISerializer serializer = null) {
            if (string.IsNullOrWhiteSpace(serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the events micro service.");
            }
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serviceUri = serviceUri.TrimEnd('/');
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeApplicationEventsAsync(
            Func<ApplicationEventModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/v2/applications/events",
                Resource.Platform);
            var registration = hub.Register(EventTargets.ApplicationEventTarget, callback);
            return new AsyncDisposable(registration);
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeEndpointEventsAsync(
            Func<EndpointEventModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/v2/endpoints/events",
                Resource.Platform);
            var registration = hub.Register(EventTargets.EndpointEventTarget, callback);
            return new AsyncDisposable(registration);
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeGatewayEventsAsync(
            Func<GatewayEventModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/v2/gateways/events",
                Resource.Platform);
            var registration = hub.Register(EventTargets.GatewayEventTarget, callback);
            return new AsyncDisposable(registration);
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeSupervisorEventsAsync(
            Func<SupervisorEventModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/v2/supervisors/events",
                Resource.Platform);
            var registration = hub.Register(EventTargets.SupervisorEventTarget, callback);
            return new AsyncDisposable(registration);
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeDiscovererEventsAsync(
            Func<DiscovererEventModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/v2/discovery/events",
                Resource.Platform);
            var registration = hub.Register(EventTargets.DiscovererEventTarget, callback);
            return new AsyncDisposable(registration);
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribePublisherEventsAsync(
            Func<PublisherEventModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/v2/publishers/events",
                Resource.Platform);
            var registration = hub.Register(EventTargets.PublisherEventTarget, callback);
            return new AsyncDisposable(registration);
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeDiscoveryProgressByDiscovererIdAsync(
            string discovererId, Func<DiscoveryProgressModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/v2/discovery/events",
                Resource.Platform);
            var registration = hub.Register(EventTargets.DiscoveryProgressTarget, callback);
            try {
                await SubscribeDiscoveryProgressByDiscovererIdAsync(discovererId,
                    hub.ConnectionId, CancellationToken.None);
                return new AsyncDisposable(registration,
                    () => UnsubscribeDiscoveryProgressByDiscovererIdAsync(discovererId,
                        hub.ConnectionId, CancellationToken.None));
            }
            catch {
                registration.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeDiscoveryProgressByRequestIdAsync(
            string requestId, Func<DiscoveryProgressModel, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/v2/discovery/events",
                Resource.Platform);
            var registration = hub.Register(EventTargets.DiscoveryProgressTarget, callback);
            try {
                await SubscribeDiscoveryProgressByRequestIdAsync(requestId, hub.ConnectionId,
                    CancellationToken.None);
                return new AsyncDisposable(registration,
                    () => UnsubscribeDiscoveryProgressByRequestIdAsync(requestId,
                        hub.ConnectionId, CancellationToken.None));
            }
            catch {
                registration.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task SubscribeDiscoveryProgressByDiscovererIdAsync(string discovererId,
            string connectionId, CancellationToken ct) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (string.IsNullOrEmpty(connectionId)) {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/{discovererId}/events", Resource.Platform);
            _serializer.SerializeToRequest(request, connectionId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task SubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string connectionId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            if (string.IsNullOrEmpty(connectionId)) {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/requests/{requestId}/events", Resource.Platform);
            _serializer.SerializeToRequest(request, connectionId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribeDiscoveryProgressByDiscovererIdAsync(string discovererId,
            string connectionId, CancellationToken ct) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (string.IsNullOrEmpty(connectionId)) {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/{discovererId}/events/{connectionId}",
                Resource.Platform);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task UnsubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string connectionId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            if (string.IsNullOrEmpty(connectionId)) {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/discovery/requests/{requestId}/events/{connectionId}",
                Resource.Platform);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly ISerializer _serializer;
        private readonly ICallbackClient _client;
    }
}
