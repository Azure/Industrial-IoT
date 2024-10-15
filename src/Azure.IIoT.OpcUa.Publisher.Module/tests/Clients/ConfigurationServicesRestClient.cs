// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using static Grpc.Core.Metadata;

    /// <summary>
    /// Implementation of file system services over http
    /// </summary>
    public sealed class ConfigurationServicesRestClient : IConfigurationServices,
        IAssetConfiguration<Stream>, IAssetConfiguration<byte[]>,
        IAssetConfiguration<VariantValue>
    {
        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public ConfigurationServicesRestClient(IHttpClientFactory httpClient,
            IOptions<SdkOptions> options, ISerializer serializer) :
            this(httpClient, options?.Value.Target, serializer)
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public ConfigurationServicesRestClient(IHttpClientFactory httpClient, string serviceUri,
            ISerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serviceUri = serviceUri.TrimEnd('/');
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> ExpandAsync(
            PublishedNodesEntryModel entry, PublishedNodeExpansionModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var uri = new Uri($"{_serviceUri}/v2/writer/expand");
            return _httpClient.PostStreamAsync<ServiceResponse<PublishedNodesEntryModel>>(uri,
                RequestBody(entry, request), _serializer, ct: ct);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAsync(
            PublishedNodesEntryModel entry, PublishedNodeExpansionModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var uri = new Uri($"{_serviceUri}/v2/writer");
            return _httpClient.PostStreamAsync<ServiceResponse<PublishedNodesEntryModel>>(uri,
                RequestBody(entry, request), _serializer, ct: ct);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAssetAsync(
            PublishedNodeCreateAssetRequestModel<Stream> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Entry);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetName);
            ArgumentNullException.ThrowIfNull(request.Configuration);
            var uri = new Uri($"{_serviceUri}/v2/writer/assets/create");
            var buffer = request.Configuration.ReadAsBuffer();
            var requestWithBuffer = new PublishedNodeCreateAssetRequestModel<byte[]>
            {
                Entry = request.Entry,
                Header = request.Header,
                WaitTime = request.WaitTime,
                Configuration = buffer.ToArray()
            };
            return await _httpClient.PostAsync<ServiceResponse<PublishedNodesEntryModel>>(uri,
                requestWithBuffer, _serializer, ct: ct).ConfigureAwait(false);
        }

        public async Task<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAssetAsync(
            PublishedNodeCreateAssetRequestModel<VariantValue> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Entry);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetName);
            var uri = new Uri($"{_serviceUri}/v2/writer/assets");
            return await _httpClient.PostAsync<ServiceResponse<PublishedNodesEntryModel>>(uri,
                request, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAssetAsync(
            PublishedNodeCreateAssetRequestModel<byte[]> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Entry);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetName);
            ArgumentNullException.ThrowIfNull(request.Configuration);
            var uri = new Uri($"{_serviceUri}/v2/writer/assets/create");
            return await _httpClient.PostAsync<ServiceResponse<PublishedNodesEntryModel>>(uri,
                request, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> GetAllAssetsAsync(
            PublishedNodesEntryModel entry, RequestHeaderModel header, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var uri = new Uri($"{_serviceUri}/v2/writer/assets/list");
            return _httpClient.PostStreamAsync<ServiceResponse<PublishedNodesEntryModel>>(uri,
                RequestBody(entry, header), _serializer, ct: ct);
        }

        /// <inheritdoc/>
        public async Task<ServiceResultModel> DeleteAssetAsync(
            PublishedNodeDeleteAssetRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Entry);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(request.Entry.DataSetWriterId);
            var uri = new Uri($"{_serviceUri}/v2/writer/assets/delete");
            return await _httpClient.PostAsync<ServiceResultModel>(uri,
                request, _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Create envelope
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entry"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static PublishedNodesEntryRequestModel<T> RequestBody<T>(PublishedNodesEntryModel entry,
            T request)
        {
            return new PublishedNodesEntryRequestModel<T> { Entry = entry, Request = request };
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
