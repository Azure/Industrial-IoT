// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Options;
    using Nito.Disposables;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry service event client
    /// </summary>
    public class RegistryServiceEvents : IRegistryServiceEvents, IRegistryEventApi
    {
        /// <summary>
        /// Event client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="client"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public RegistryServiceEvents(IHttpClientFactory httpClient, ICallbackClient client,
            IOptions<ServiceSdkOptions> options, ISerializer serializer) :
            this(httpClient, client, options?.Value.ServiceUrl, serializer)
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="client"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public RegistryServiceEvents(IHttpClientFactory httpClient, ICallbackClient client,
            string serviceUri, ISerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the events micro service.");
            }
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serviceUri = serviceUri.TrimEnd('/');
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeApplicationEventsAsync(
            Func<ApplicationEventModel, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/events/v2/applications/events").ConfigureAwait(false);
            var registration = hub.Register(EventTargets.ApplicationEventTarget, callback);
            return new AsyncDisposable(() =>
            {
                registration.Dispose();
                return ValueTask.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeEndpointEventsAsync(
            Func<EndpointEventModel, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/events/v2/endpoints/events").ConfigureAwait(false);
            var registration = hub.Register(EventTargets.EndpointEventTarget, callback);
            return new AsyncDisposable(() =>
            {
                registration.Dispose();
                return ValueTask.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeGatewayEventsAsync(
            Func<GatewayEventModel, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/events/v2/gateways/events").ConfigureAwait(false);
            var registration = hub.Register(EventTargets.GatewayEventTarget, callback);
            return new AsyncDisposable(() =>
            {
                registration.Dispose();
                return ValueTask.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeSupervisorEventsAsync(
            Func<SupervisorEventModel, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/events/v2/supervisors/events").ConfigureAwait(false);
            var registration = hub.Register(EventTargets.SupervisorEventTarget, callback);
            return new AsyncDisposable(() =>
            {
                registration.Dispose();
                return ValueTask.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeDiscovererEventsAsync(
            Func<DiscovererEventModel, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/events/v2/discovery/events").ConfigureAwait(false);
            var registration = hub.Register(EventTargets.DiscovererEventTarget, callback);
            return new AsyncDisposable(() =>
            {
                registration.Dispose();
                return ValueTask.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribePublisherEventsAsync(
            Func<PublisherEventModel, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/events/v2/publishers/events").ConfigureAwait(false);
            var registration = hub.Register(EventTargets.PublisherEventTarget, callback);
            return new AsyncDisposable(() =>
            {
                registration.Dispose();
                return ValueTask.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeDiscoveryProgressByDiscovererIdAsync(
            string discovererId, Func<DiscoveryProgressModel, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/events/v2/discovery/events").ConfigureAwait(false);
            var registration = hub.Register(EventTargets.DiscoveryProgressTarget, callback);
            try
            {
                await SubscribeDiscoveryProgressByDiscovererIdAsync(discovererId,
                    hub.ConnectionId, default).ConfigureAwait(false);
                return new AsyncDisposable(async () =>
                {
                    registration.Dispose();
                    await UnsubscribeDiscoveryProgressByDiscovererIdAsync(discovererId,
                        hub.ConnectionId, default).ConfigureAwait(false);
                });
            }
            catch
            {
                registration.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> SubscribeDiscoveryProgressByRequestIdAsync(
            string requestId, Func<DiscoveryProgressModel, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/events/v2/discovery/events").ConfigureAwait(false);
            var registration = hub.Register(EventTargets.DiscoveryProgressTarget, callback);
            try
            {
                await SubscribeDiscoveryProgressByRequestIdAsync(requestId, hub.ConnectionId,
                    default).ConfigureAwait(false);
                return new AsyncDisposable(async () =>
                {
                    registration.Dispose();
                    await UnsubscribeDiscoveryProgressByRequestIdAsync(requestId,
                        hub.ConnectionId, default).ConfigureAwait(false);
                });
            }
            catch
            {
                registration.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task SubscribeDiscoveryProgressByDiscovererIdAsync(string discovererId,
            string connectionId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var uri = new Uri($"{_serviceUri}/events/v2/discovery/{discovererId}/events");
            await _httpClient.PutAsync(uri, connectionId, _serializer,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task SubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string connectionId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentNullException(nameof(requestId));
            }
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var uri = new Uri($"{_serviceUri}/events/v2/discovery/requests/{requestId}/events");
            await _httpClient.PutAsync(uri, connectionId, _serializer,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UnsubscribeDiscoveryProgressByDiscovererIdAsync(string discovererId,
            string connectionId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(discovererId))
            {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var uri = new Uri($"{_serviceUri}/events/v2/discovery/{discovererId}/events/{connectionId}");
            await _httpClient.DeleteAsync(uri, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UnsubscribeDiscoveryProgressByRequestIdAsync(string requestId,
            string connectionId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentNullException(nameof(requestId));
            }
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var uri = new Uri($"{_serviceUri}/events/v2/discovery/requests/{requestId}/events/{connectionId}");
            await _httpClient.DeleteAsync(uri, ct: ct).ConfigureAwait(false);
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly string _serviceUri;
        private readonly ISerializer _serializer;
        private readonly ICallbackClient _client;
    }
}
