// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Furly.Extensions.Serializers;
    using Furly.Tunnel;
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
        public PublisherServicesClient(IEndpointRegistry endpoints, IMethodClient client,
            IJsonSerializer serializer)
        {
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string endpoint, CancellationToken ct)
        {
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new DiscoveryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.GetEndpointCertificateAsync(ep.Registration.Endpoint, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task ConnectAsync(string endpoint, CredentialModel credential,
            CancellationToken ct)
        {
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            await client.ConnectAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = credential
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(string endpoint, CredentialModel credential,
            CancellationToken ct)
        {
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            await client.DisconnectAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = credential
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(string endpoint,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.NodeBrowseFirstAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(string endpoint,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.NodeBrowseNextAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(string endpoint,
            BrowseStreamRequestModel request, CancellationToken ct)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(string endpoint,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.NodeBrowsePathAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(string endpoint,
            ValueReadRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.NodeValueReadAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(string endpoint,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.NodeValueWriteAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(string endpoint,
            MethodMetadataRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.NodeMethodGetMetadataAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(string endpoint,
            MethodCallRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.NodeMethodCallAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(string endpoint,
            ReadRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.NodeReadAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(string endpoint,
            WriteRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.NodeWriteAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(string endpoint,
            CancellationToken ct)
        {
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.GetServerCapabilitiesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(string endpoint,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.GetMetadataAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            string endpoint, CancellationToken ct)
        {
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryGetServerCapabilitiesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            string endpoint, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryGetConfigurationAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(string endpoint,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReadAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReadNextAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(string endpoint,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new TwinApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryUpdateAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseModel> PublishStartAsync(string endpoint,
            PublishStartRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new PublisherApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.PublishStartAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> PublishStopAsync(string endpoint,
            PublishStopRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new PublisherApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.PublishStopAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> PublishBulkAsync(string endpoint,
            PublishBulkRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new PublisherApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.PublishBulkAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> PublishListAsync(string endpoint,
            PublishedItemListRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new PublisherApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.PublishListAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReplaceEventsAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryInsertEventsAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryUpsertEventsAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(string endpoint,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryDeleteEventsAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryDeleteValuesAtTimesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            string endpoint, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryDeleteModifiedValuesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(string endpoint,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryDeleteValuesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReplaceValuesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryInsertValuesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(string endpoint,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryUpsertValuesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpoint, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReadEventsAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReadEventsNextAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReadValuesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpoint, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReadValuesAtTimesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReadProcessedValuesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpoint, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReadModifiedValuesAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var ep = await _endpoints.GetEndpointAsync(endpoint, true, ct).ConfigureAwait(false);
            var client = new HistoryApiClient(_client, ep.Registration.DiscovererId, _serializer);
            return await client.HistoryReadValuesNextAsync(new ConnectionModel
            {
                Endpoint = ep.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
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

        private readonly IEndpointRegistry _endpoints;
        private readonly IMethodClient _client;
        private readonly IJsonSerializer _serializer;
    }
}
