// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapt the api to endpoint identifiers which are looked up through the registry.
    /// </summary>
    public sealed class PublisherServicesClient : IConnectionServices<string>,
        ICertificateServices<string>, INodeServices<string>, IPublishServices<string>,
        IHistoryServices<string>
    {
        /// <summary>
        /// Create endpoint registry
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
            string endpointId, CancellationToken ct)
        {
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new DiscoveryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.GetEndpointCertificateAsync(endpoint.Registration.Endpoint, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task ConnectAsync(string endpointId, CredentialModel credential,
            CancellationToken ct)
        {
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            await client.ConnectAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = credential
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(string endpointId, CredentialModel credential,
            CancellationToken ct)
        {
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            await client.DisconnectAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = credential
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(string endpointId,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeBrowseFirstAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(string endpointId,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeBrowseNextAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(string endpointId,
            BrowseStreamRequestModel request, CancellationToken ct)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(string endpointId,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeBrowsePathAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(string endpointId,
            ValueReadRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeValueReadAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(string endpointId,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeValueWriteAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(string endpointId,
            MethodMetadataRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeMethodGetMetadataAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(string endpointId,
            MethodCallRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeMethodCallAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(string endpointId,
            ReadRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeReadAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(string endpointId,
            WriteRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeWriteAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(string endpointId,
            CancellationToken ct)
        {
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.GetServerCapabilitiesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(string endpointId,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.GetMetadataAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            string endpointId, CancellationToken ct)
        {
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryGetServerCapabilitiesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            string endpointId, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryGetConfigurationAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(string endpointId,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            string endpointId, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadNextAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(string endpointId,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryUpdateAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseModel> NodePublishStartAsync(string endpointId,
            PublishStartRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new PublisherApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodePublishStartAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> NodePublishStopAsync(string endpointId,
            PublishStopRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new PublisherApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodePublishStopAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> NodePublishBulkAsync(string endpointId,
            PublishBulkRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new PublisherApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodePublishBulkAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> NodePublishListAsync(string endpointId,
            PublishedItemListRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new PublisherApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodePublishListAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReplaceEventsAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryInsertEventsAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryUpsertEventsAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryDeleteEventsAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            string endpointId, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryDeleteValuesAtTimesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            string endpointId, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryDeleteModifiedValuesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(string endpointId,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryDeleteValuesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReplaceValuesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryInsertValuesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(string endpointId,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryUpsertValuesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpointId, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadEventsAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpointId, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadEventsNextAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadValuesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadValuesAtTimesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadProcessedValuesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadModifiedValuesAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            string endpointId, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct).ConfigureAwait(false);
            var deviceId = PublisherModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadValuesNextAsync(new ConnectionModel
            {
                Endpoint = endpoint.Registration.Endpoint,
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
