// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Abstractions.Serializers.Extensions;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using System;
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
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public PublisherServiceEvents(IHttpClient httpClient, ICallbackClient client,
            IServiceApiConfig config, ISerializer serializer) :
            this(httpClient, client, config?.ServiceUrl, serializer)
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="client"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public PublisherServiceEvents(IHttpClient httpClient, ICallbackClient client,
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
        public async Task<IAsyncDisposable> NodePublishSubscribeByEndpointAsync(string endpointId,
            Func<MonitoredItemMessageModel, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var hub = await _client.GetHubAsync($"{_serviceUri}/v2/publishers/events", Resource.Platform).ConfigureAwait(false);
            var registration = hub.Register(EventTargets.PublisherSampleTarget, callback);
            try
            {
                await NodePublishSubscribeByEndpointAsync(endpointId, hub.ConnectionId,
                    default).ConfigureAwait(false);
                return new AsyncDisposable(registration,
                    () => NodePublishUnsubscribeByEndpointAsync(endpointId,
                        hub.ConnectionId, default));
            }
            catch
            {
                registration.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task NodePublishSubscribeByEndpointAsync(string endpointId, string connectionId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/telemetry/{endpointId}/samples", Resource.Platform);
            _serializer.SerializeToRequest(request, connectionId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task NodePublishUnsubscribeByEndpointAsync(string endpointId, string connectionId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/telemetry/{endpointId}/samples/{connectionId}", Resource.Platform);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        private readonly IHttpClient _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
        private readonly ICallbackClient _client;
    }
}
