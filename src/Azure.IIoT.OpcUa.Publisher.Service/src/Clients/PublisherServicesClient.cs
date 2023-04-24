// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Clients
{
    using Azure.Core;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Furly.Tunnel;
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapt the api to ep identifiers which are looked up through the registry.
    /// </summary>
    public sealed class PublisherServicesClient : IConnectionServices<string>,
        ICertificateServices<string>, INodeServices<string>, IPublishServices<string>,
        IHistoryServices<string>, IDisposable
    {
        /// <summary>
        /// Create ep registry
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="cache"></param>
        public PublisherServicesClient(IEndpointRegistry endpoints, IMethodClient client,
            IJsonSerializer serializer, IMemoryCache cache)
        {
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _cache = cache ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <inheritdoc/>
        public Task<X509CertificateChainModel> GetEndpointCertificateAsync(string endpoint,
            CancellationToken ct)
        {
            return Execute("GetEndpointCertificate", endpoint, (publisherId, endpoint) =>
            {
                var client = new DiscoveryApiClient(_client, publisherId, _serializer);
                return client.GetEndpointCertificateAsync(endpoint, ct);
            }, ct);
        }
        /// <inheritdoc/>
        public Task<ConnectResponseModel> ConnectAsync(string endpoint,
            ConnectRequestModel request, CancellationToken ct)
        {
            return Execute("Connect", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.ConnectAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task DisconnectAsync(string endpoint, DisconnectRequestModel request,
            CancellationToken ct)
        {
            return Execute("Disconnect", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.DisconnectAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<BrowseFirstResponseModel> BrowseFirstAsync(string endpoint,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("NodeBrowseFirst", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.NodeBrowseFirstAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<BrowseNextResponseModel> BrowseNextAsync(string endpoint,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("NodeBrowseNext", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.NodeBrowseNextAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
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
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("NodeBrowsePath", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.NodeBrowsePathAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<ValueReadResponseModel> ValueReadAsync(string endpoint,
            ValueReadRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("NodeValueRead", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.NodeValueReadAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<ValueWriteResponseModel> ValueWriteAsync(string endpoint,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("NodeValueWrite", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.NodeValueWriteAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<MethodMetadataResponseModel> GetMethodMetadataAsync(string endpoint,
            MethodMetadataRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("NodeMethodGetMetadata", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.NodeMethodGetMetadataAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<MethodCallResponseModel> MethodCallAsync(string endpoint,
            MethodCallRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("NodeMethodCall", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.NodeMethodCallAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<ReadResponseModel> ReadAsync(string endpoint,
            ReadRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("NodeRead", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.NodeReadAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<WriteResponseModel> WriteAsync(string endpoint,
            WriteRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("NodeWrite", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.NodeWriteAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(string endpoint,
            CancellationToken ct)
        {
            return Execute("GetServerCapabilities", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.GetServerCapabilitiesAsync(new ConnectionModel
                {
                    Endpoint = endpoint
                }, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<NodeMetadataResponseModel> GetMetadataAsync(string endpoint,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("GetMetadata", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.GetMetadataAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            string endpoint, CancellationToken ct)
        {
            return Execute("HistoryGetServerCapabilities", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.HistoryGetServerCapabilitiesAsync(new ConnectionModel
                {
                    Endpoint = endpoint
                }, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            string endpoint, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryGetConfiguration", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.HistoryGetConfigurationAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(string endpoint,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryRead", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.HistoryReadAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReadNext", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.HistoryReadNextAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpdateAsync(string endpoint,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryUpdate", endpoint, (publisherId, endpoint) =>
            {
                var client = new TwinApiClient(_client, publisherId, _serializer);
                return client.HistoryUpdateAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<PublishStartResponseModel> PublishStartAsync(string endpoint,
            PublishStartRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("PublishStart", endpoint, (publisherId, endpoint) =>
            {
                var client = new PublisherApiClient(_client, publisherId, _serializer);
                return client.PublishStartAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<PublishStopResponseModel> PublishStopAsync(string endpoint,
            PublishStopRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("PublishStop", endpoint, (publisherId, endpoint) =>
            {
                var client = new PublisherApiClient(_client, publisherId, _serializer);
                return client.PublishStopAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<PublishBulkResponseModel> PublishBulkAsync(string endpoint,
            PublishBulkRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("PublishBulk", endpoint, (publisherId, endpoint) =>
            {
                var client = new PublisherApiClient(_client, publisherId, _serializer);
                return client.PublishBulkAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<PublishedItemListResponseModel> PublishListAsync(string endpoint,
            PublishedItemListRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("PublishList", endpoint, (publisherId, endpoint) =>
            {
                var client = new PublisherApiClient(_client, publisherId, _serializer);
                return client.PublishListAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReplaceEvents", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryReplaceEventsAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryInsertEvents", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryInsertEventsAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryUpsertEvents", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryUpsertEventsAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(string endpoint,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryDeleteEvents", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryDeleteEventsAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryDeleteValuesAtTimes", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryDeleteValuesAtTimesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryDeleteModifiedValues", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryDeleteModifiedValuesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(string endpoint,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryDeleteValues", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryDeleteValuesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReplaceValues", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryReplaceValuesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryInsertValues", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryInsertValuesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryUpsertValues", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryUpsertValuesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReadEvents", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryReadEventsAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReadEventsNext", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryReadEventsNextAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReadValues", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryReadValuesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReadValuesAtTimes", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryReadValuesAtTimesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReadProcessedValues", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryReadProcessedValuesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReadModifiedValues", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryReadModifiedValuesAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return Execute("HistoryReadValuesNext", endpoint, (publisherId, endpoint) =>
            {
                var client = new HistoryApiClient(_client, publisherId, _serializer);
                return client.HistoryReadValuesNextAsync(new ConnectionModel
                {
                    Endpoint = endpoint,
                    User = request.Header?.Elevation
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
                return await call(ep.DiscovererId!, ep.Endpoint!).ConfigureAwait(false);
            }
            catch (ResourceNotFoundException)
            {
                _cache.Remove(endpoint);
                throw;
            }
        }

        /// <summary>
        /// Execute on endpoint
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="endpoint"></param>
        /// <param name="call"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task Execute(string operation, string endpoint,
            Func<string, EndpointModel, Task> call, CancellationToken ct)
        {
            using var activity = _activitySource.StartActivity(operation);
            var ep = await GetEndpointAsync(endpoint, ct).ConfigureAwait(false);
            try
            {
                await call(ep.DiscovererId!, ep.Endpoint!).ConfigureAwait(false);
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
                    entry.SetSlidingExpiration(TimeSpan.FromSeconds(30));
                    return ep.Registration;
                }
                catch
                {
                    entry.SetAbsoluteExpiration(TimeSpan.Zero);
                    throw;
                }
            }).ConfigureAwait(false);
            return found!;
        }

        private readonly IEndpointRegistry _endpoints;
        private readonly IMethodClient _client;
        private readonly IJsonSerializer _serializer;
        private readonly IMemoryCache _cache;
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
    }
}
