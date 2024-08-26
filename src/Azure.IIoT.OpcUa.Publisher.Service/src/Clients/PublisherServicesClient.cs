// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Furly.Tunnel;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapt the api to ep identifiers which are looked up through the registry.
    /// </summary>
    public sealed class PublisherServicesClient : IConnectionServices<string>,
        ICertificateServices<string>, INodeServices<string>, IPublishServices<string>,
        IHistoryServices<string>, IPublisherServices<string>, IDisposable
    {
        /// <summary>
        /// Create ep registry
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        public PublisherServicesClient(IEndpointRegistry endpoints, IMethodClient client,
            IJsonSerializer serializer, IMemoryCache cache, ILogger<PublisherServicesClient> logger)
        {
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _cache = cache ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<PublishedNodesEntryModel> GetConfiguredEndpointsAsync(
            string publisherId, GetConfiguredEndpointsRequestModel request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            using var activity = _activitySource.StartActivity("GetConfiguredEndpoints");

            var client = new PublisherApiClient(_client, publisherId, kTimeout, _serializer);
            var result = await client.GetConfiguredEndpointsAsync(request, ct).ConfigureAwait(false);

            if (result.Endpoints != null)
            {
                foreach (var item in result.Endpoints)
                {
                    yield return item;
                }
            }
        }

        /// <inheritdoc/>
        public async Task SetConfiguredEndpointsAsync(string publisherId,
            SetConfiguredEndpointsRequestModel request, CancellationToken ct = default)
        {
            using var activity = _activitySource.StartActivity("SetConfiguredEndpoints");

            var client = new PublisherApiClient(_client, publisherId, kTimeout, _serializer);
            await client.SetConfiguredEndpointsAsync(request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<X509CertificateChainModel> GetEndpointCertificateAsync(string endpoint,
            CancellationToken ct)
        {
            return Execute("GetEndpointCertificate", endpoint, (publisherId, ep) =>
            {
                var client = new DiscoveryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.GetEndpointCertificateAsync(ep, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<TestConnectionResponseModel> TestConnectionAsync(string endpoint,
            TestConnectionRequestModel request, CancellationToken ct)
        {
            return Execute("TestConnection", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.TestConnectionAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<BrowseFirstResponseModel> BrowseFirstAsync(string endpoint,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("NodeBrowseFirst", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.NodeBrowseFirstAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<BrowseNextResponseModel> BrowseNextAsync(string endpoint,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("NodeBrowseNext", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.NodeBrowseNextAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(string endpoint,
            BrowseStreamRequestModel request, CancellationToken ct)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<BrowsePathResponseModel> BrowsePathAsync(string endpoint,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("NodeBrowsePath", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.NodeBrowsePathAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<ValueReadResponseModel> ValueReadAsync(string endpoint,
            ValueReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("NodeValueRead", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.NodeValueReadAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<ValueWriteResponseModel> ValueWriteAsync(string endpoint,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("NodeValueWrite", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.NodeValueWriteAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<MethodMetadataResponseModel> GetMethodMetadataAsync(string endpoint,
            MethodMetadataRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("NodeMethodGetMetadata", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.NodeMethodGetMetadataAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<MethodCallResponseModel> MethodCallAsync(string endpoint,
            MethodCallRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("NodeMethodCall", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.NodeMethodCallAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<ReadResponseModel> ReadAsync(string endpoint,
            ReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("NodeRead", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.NodeReadAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<WriteResponseModel> WriteAsync(string endpoint,
            WriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("NodeWrite", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.NodeWriteAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(string endpoint,
            RequestHeaderModel? header, CancellationToken ct)
        {
            return Execute("GetServerCapabilities", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.GetServerCapabilitiesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = header?.Elevation,
                    Group = endpoint
                }, header, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<NodeMetadataResponseModel> GetMetadataAsync(string endpoint,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("GetMetadata", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.GetMetadataAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<QueryCompilationResponseModel> CompileQueryAsync(string endpoint,
            QueryCompilationRequestModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("CompileQuery", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.CompileQueryAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            string endpoint, RequestHeaderModel? header, CancellationToken ct)
        {
            return Execute("HistoryGetServerCapabilities", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryGetServerCapabilitiesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    Group = endpoint
                }, header, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            string endpoint, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryGetConfiguration", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryGetConfigurationAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(string endpoint,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryRead", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReadAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReadNext", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReadNextAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpdateAsync(string endpoint,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryUpdate", endpoint, (publisherId, ep) =>
            {
                var client = new TwinApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryUpdateAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<PublishStartResponseModel> PublishStartAsync(string endpoint,
            PublishStartRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("PublishStart", endpoint, (publisherId, ep) =>
            {
                var client = new PublisherApiClient(_client, publisherId, kTimeout, _serializer);
                return client.PublishStartAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<PublishStopResponseModel> PublishStopAsync(string endpoint,
            PublishStopRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("PublishStop", endpoint, (publisherId, ep) =>
            {
                var client = new PublisherApiClient(_client, publisherId, kTimeout, _serializer);
                return client.PublishStopAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<PublishBulkResponseModel> PublishBulkAsync(string endpoint,
            PublishBulkRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("PublishBulk", endpoint, (publisherId, ep) =>
            {
                var client = new PublisherApiClient(_client, publisherId, kTimeout, _serializer);
                return client.PublishBulkAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<PublishedItemListResponseModel> PublishListAsync(string endpoint,
            PublishedItemListRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("PublishList", endpoint, (publisherId, ep) =>
            {
                var client = new PublisherApiClient(_client, publisherId, kTimeout, _serializer);
                return client.PublishListAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReplaceEvents", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReplaceEventsAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryInsertEvents", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryInsertEventsAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryUpsertEvents", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryUpsertEventsAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(string endpoint,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryDeleteEvents", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryDeleteEventsAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryDeleteValuesAtTimes", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryDeleteValuesAtTimesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryDeleteModifiedValues", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryDeleteModifiedValuesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(string endpoint,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryDeleteValues", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryDeleteValuesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReplaceValues", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReplaceValuesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryInsertValues", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryInsertValuesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryUpsertValues", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryUpsertValuesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReadEvents", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReadEventsAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReadEventsNext", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReadEventsNextAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReadValues", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReadValuesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReadValuesAtTimes", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReadValuesAtTimesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReadProcessedValues", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReadProcessedValuesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReadModifiedValues", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReadModifiedValuesAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Execute("HistoryReadValuesNext", endpoint, (publisherId, ep) =>
            {
                var client = new HistoryApiClient(_client, publisherId, kTimeout, _serializer);
                return client.HistoryReadValuesNextAsync(new ConnectionModel
                {
                    Endpoint = ep,
                    User = request.Header?.Elevation,
                    Group = endpoint
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAsync(string endpoint,
            HistoryReadRequestModel<ReadValuesDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamModifiedValuesAsync(string endpoint,
            HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamValuesAtTimesAsync(string endpoint,
            HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricValueModel> HistoryStreamProcessedValuesAsync(string endpoint,
            HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<HistoricEventModel> HistoryStreamEventsAsync(string endpoint,
            HistoryReadRequestModel<ReadEventsDetailsModel> request, CancellationToken ct)
        {
            // TODO:
            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute on endpoint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="endpoint"></param>
        /// <param name="call"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<T> Execute<T>(string operation, string endpoint,
            Func<string, EndpointModel, Task<T>> call, CancellationToken ct)
        {
            using var activity = _activitySource.StartActivity(operation);
            var ep = await GetEndpointAsync(endpoint, ct).ConfigureAwait(false);
            try
            {
                Debug.Assert(ep.DiscovererId != null);
                Debug.Assert(ep.Endpoint != null);

                var result = await call(ep.DiscovererId, ep.Endpoint).ConfigureAwait(false);

                _logger.LogDebug("Called {Operation} on publisher {Publisher}.",
                    operation, ep.DiscovererId);

                return result;
            }
            catch (ResourceNotFoundException)
            {
                _cache.Remove(endpoint);
                throw;
            }
        }

        /// <summary>
        /// Get endpoint registration
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<EndpointRegistrationModel> GetEndpointAsync(string endpoint,
            CancellationToken ct)
        {
            var found = await _cache.GetOrCreateAsync(endpoint, async entry =>
            {
                try
                {
                    var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
                    if (ep.Registration?.DiscovererId == null || ep.Registration.Endpoint == null)
                    {
                        throw new ResourceInvalidStateException("Failed to get valid endpoint.");
                    }
                    //
                    // Setting an expiration will cause entries in the cache to be evicted
                    // if they're not accessed within the expiration time allotment.
                    //
                    entry.SetSlidingExpiration(TimeSpan.FromSeconds(60));
                    return ep.Registration;
                }
                catch
                {
                    entry.SetAbsoluteExpiration(DateTimeOffset.UtcNow);
                    throw;
                }
            }).ConfigureAwait(false);
            return found!;
        }

        private static readonly TimeSpan kTimeout = TimeSpan.FromSeconds(60);
        private readonly IEndpointRegistry _endpoints;
        private readonly IMethodClient _client;
        private readonly IJsonSerializer _serializer;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PublisherServicesClient> _logger;
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
    }
}
