// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Options;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin service api.
    /// </summary>
    public sealed class PublisherServiceClient : IPublisherServiceApi
    {
        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public PublisherServiceClient(IHttpClientFactory httpClient,
            IOptions<ServiceSdkOptions> options, ISerializer serializer) :
            this(httpClient, options?.Value.ServiceUrl, serializer)
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public PublisherServiceClient(IHttpClientFactory httpClient, string serviceUri,
            ISerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _serviceUri = serviceUri.TrimEnd('/') + "/publisher";
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct)
        {
            var httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri($"{_serviceUri}/healthz")
            };
            try
            {
                using var response = await _httpClient.GetAsync(httpRequest,
                    ct).ConfigureAwait(false);
                response.ValidateResponse();
                return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseModel> NodePublishStartAsync(string endpointId,
            PublishStartRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Item == null)
            {
                throw new ArgumentException("Item missing", nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/publish/{endpointId}/start");
            return await _httpClient.PostAsync<PublishStartResponseModel>(
                uri, request, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> NodePublishBulkAsync(string endpointId,
            PublishBulkRequestModel request, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/publish/{endpointId}/bulk");
            return await _httpClient.PostAsync<PublishBulkResponseModel>(
                uri, request, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> NodePublishListAsync(
            string endpointId, PublishedItemListRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            var uri = new Uri($"{_serviceUri}/v2/publish/{endpointId}");
            return await _httpClient.PostAsync<PublishedItemListResponseModel>(
                uri, request, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> NodePublishStopAsync(string endpointId,
            PublishStopRequestModel request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var uri = new Uri($"{_serviceUri}/v2/publish/{endpointId}/stop");
            return await _httpClient.PostAsync<PublishStopResponseModel>(
                uri, request, _serializer, ct: ct).ConfigureAwait(false);
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
