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
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapt the api to ep identifiers which are looked up through the registry.
    /// </summary>
    public sealed class PublisherServicesClient : IConnectionServices<string>,
        ICertificateServices<string>, INodeServices<string>, IPublishServices<string>,
        IHistoryServices<string>
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
        public Task<X509CertificateChainModel> GetEndpointCertificateAsync(string endpoint,
            CancellationToken ct)
        {
            return Execute("GetEndpointCertificate", endpoint, ep =>
            {
                var client = new DiscoveryApiClient(_client, ep.DiscovererId, _serializer);
                return client.GetEndpointCertificateAsync(ep.Endpoint, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task ConnectAsync(string endpoint, CredentialModel credential,
            CancellationToken ct)
        {
            return Execute("Connect", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.ConnectAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
                    User = credential
                }, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task DisconnectAsync(string endpoint, CredentialModel credential,
            CancellationToken ct)
        {
            return Execute("Disconnect", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.DisconnectAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
                    User = credential
                }, ct);
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
            return Execute("NodeBrowseFirst", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.NodeBrowseFirstAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("NodeBrowseNext", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.NodeBrowseNextAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("NodeBrowsePath", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.NodeBrowsePathAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("NodeValueRead", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.NodeValueReadAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("NodeValueWrite", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.NodeValueWriteAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("NodeMethodGetMetadata", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.NodeMethodGetMetadataAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("NodeMethodCall", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.NodeMethodCallAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("NodeRead", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.NodeReadAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("NodeWrite", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.NodeWriteAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(string endpoint,
            CancellationToken ct)
        {
            return Execute("GetServerCapabilities", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.GetServerCapabilitiesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint
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
            return Execute("GetMetadata", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.GetMetadataAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
                    User = request.Header?.Elevation
                }, request, ct);
            }, ct);
        }

        /// <inheritdoc/>
        public Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            string endpoint, CancellationToken ct)
        {
            return Execute("HistoryGetServerCapabilities", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryGetServerCapabilitiesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint
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
            return Execute("HistoryGetConfiguration", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryGetConfigurationAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryRead", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReadAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReadNext", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReadNextAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryUpdate", endpoint, ep =>
            {
                var client = new TwinApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryUpdateAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("PublishStart", endpoint, ep =>
            {
                var client = new PublisherApiClient(_client, ep.DiscovererId, _serializer);
                return client.PublishStartAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("PublishStop", endpoint, ep =>
            {
                var client = new PublisherApiClient(_client, ep.DiscovererId, _serializer);
                return client.PublishStopAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("PublishBulk", endpoint, ep =>
            {
                var client = new PublisherApiClient(_client, ep.DiscovererId, _serializer);
                return client.PublishBulkAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("PublishList", endpoint, ep =>
            {
                var client = new PublisherApiClient(_client, ep.DiscovererId, _serializer);
                return client.PublishListAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReplaceEvents", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReplaceEventsAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryInsertEvents", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryInsertEventsAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryUpsertEvents", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryUpsertEventsAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryDeleteEvents", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryDeleteEventsAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryDeleteValuesAtTimes", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryDeleteValuesAtTimesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryDeleteModifiedValues", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryDeleteModifiedValuesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryDeleteValues", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryDeleteValuesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReplaceValues", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReplaceValuesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryInsertValues", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryInsertValuesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryUpsertValues", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryUpsertValuesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReadEvents", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReadEventsAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReadEventsNext", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReadEventsNextAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReadValues", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReadValuesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReadValuesAtTimes", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReadValuesAtTimesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReadProcessedValues", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReadProcessedValuesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReadModifiedValues", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReadModifiedValuesAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            return Execute("HistoryReadValuesNext", endpoint, ep =>
            {
                var client = new HistoryApiClient(_client, ep.DiscovererId, _serializer);
                return client.HistoryReadValuesNextAsync(new ConnectionModel
                {
                    Endpoint = ep.Endpoint,
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
            Func<EndpointRegistrationModel, Task<T>> call, CancellationToken ct)
        {
            using var activity = Diagnostics.Activity.StartActivity(operation);
            var ep = await GetEndpointAsync(endpoint, ct).ConfigureAwait(false);
            try
            {
                return await call(ep).ConfigureAwait(false);
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
            Func<EndpointRegistrationModel, Task> call, CancellationToken ct)
        {
            using var activity = Diagnostics.Activity.StartActivity(operation);
            var ep = await GetEndpointAsync(endpoint, ct).ConfigureAwait(false);
            try
            {
                await call(ep).ConfigureAwait(false);
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
            return await _cache.GetOrCreateAsync(endpoint, async entry =>
            {
                //
                // Setting an expiration will cause entries in the cache to be evicted
                // if they're not accessed within the expiration time allotment.
                //
                entry.SetSlidingExpiration(TimeSpan.FromSeconds(30));
                var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
                return ep.Registration;
            }).ConfigureAwait(false);
        }

        private readonly IEndpointRegistry _endpoints;
        private readonly IMethodClient _client;
        private readonly IJsonSerializer _serializer;
        private readonly IMemoryCache _cache;
    }
}
