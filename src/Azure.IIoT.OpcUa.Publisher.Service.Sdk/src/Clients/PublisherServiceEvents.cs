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
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher service events
    /// </summary>
    public class PublisherServiceEvents : IPublisherServiceEvents, IPublisherEventApi
    {
        /// <summary>
        /// Event client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="client"></param>
        /// <param name="options"></param>
        /// <param name="serializers"></param>
        public PublisherServiceEvents(IHttpClientFactory httpClient, ICallbackClient client,
            IOptions<ServiceSdkOptions> options, IEnumerable<ISerializer> serializers) :
            this(httpClient, client, options.Value.ServiceUrl!, options.Value.TokenProvider,
                serializers.Resolve(options.Value))
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="client"></param>
        /// <param name="serviceUri"></param>
        /// <param name="authorization"></param>
        /// <param name="serializer"></param>
        public PublisherServiceEvents(IHttpClientFactory httpClient, ICallbackClient client,
            string serviceUri, Func<Task<string?>>? authorization,
            ISerializer? serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the events micro service.");
            }
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _authorization = authorization;
            _serviceUri = serviceUri.TrimEnd('/');
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> NodePublishSubscribeByEndpointAsync(string endpointId,
            Func<MonitoredItemMessageModel?, Task> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            var hub = await _client.GetHubAsync($"{_serviceUri}/events/v2/publishers/events").ConfigureAwait(false);
            if (hub.ConnectionId == null)
            {
                throw new IOException("Hub not connected");
            }
            var registration = hub.Register(EventTargets.PublisherSampleTarget, callback);
            try
            {
                await NodePublishSubscribeByEndpointAsync(endpointId, hub.ConnectionId,
                    default).ConfigureAwait(false);
                return new AsyncDisposable(async () =>
                {
                    registration.Dispose();
                    await NodePublishUnsubscribeByEndpointAsync(endpointId,
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
        public async Task NodePublishSubscribeByEndpointAsync(string endpointId,
            string userId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }
            var uri = new Uri(
                $"{_serviceUri}/events/v2/telemetry/{Uri.EscapeDataString(endpointId)}/samples");
            await _httpClient.PutAsync(uri, userId, _serializer, authorization: _authorization,
                ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task NodePublishUnsubscribeByEndpointAsync(string endpointId,
            string userId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }
            var uri = new Uri(
                $"{_serviceUri}/events/v2/telemetry/{Uri.EscapeDataString(endpointId)}/samples/{userId}");
            await _httpClient.DeleteAsync(uri, authorization: _authorization, ct: ct).ConfigureAwait(false);
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
        private readonly ICallbackClient _client;
        private readonly Func<Task<string?>>? _authorization;
    }
}
