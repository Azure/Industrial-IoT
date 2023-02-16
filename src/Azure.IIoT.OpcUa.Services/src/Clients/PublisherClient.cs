// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Clients {
    using Azure.IIoT.OpcUa.Api;
    using Azure.IIoT.OpcUa.Api.Clients;
    using Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapt the api to endpoint identifiers which are looked up through the registry.
    /// </summary>
    public sealed class PublisherClient : IConnectionServices<string>,
        ICertificateServices<string>, IBrowseServices<string>, INodeServices<string>,
        IHistoricAccessServices<string>, IPublishServices<string> {

        /// <summary>
        /// Create endpoint registry
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        public PublisherClient(IEndpointRegistry endpoints, IMethodClient client,
            IJsonSerializer serializer) {
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            string endpointId, CancellationToken ct) {
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new PublisherApiClient(_client, deviceId, moduleId, _serializer);
            return await client.GetEndpointCertificateAsync(endpoint.Registration.Endpoint, ct);
        }

        /// <inheritdoc/>
        public async Task ConnectAsync(string endpointId, CredentialModel credential,
            CancellationToken ct) {
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            await client.ConnectAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = credential
            }, ct);
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(string endpointId, CredentialModel credential,
            CancellationToken ct) {
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            await client.DisconnectAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = credential
            }, ct);
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseModel> NodeBrowseFirstAsync(string endpointId,
            BrowseRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeBrowseFirstAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> NodeBrowseNextAsync(string endpointId,
            BrowseNextRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeBrowseNextAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> NodeBrowsePathAsync(string endpointId,
            BrowsePathRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeBrowsePathAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeValueReadAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeValueWriteAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(string endpointId,
            MethodMetadataRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeMethodGetMetadataAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> NodeMethodCallAsync(string endpointId,
            MethodCallRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeMethodCallAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> NodeReadAsync(string endpointId,
            ReadRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeReadAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> NodeWriteAsync(string endpointId,
            WriteRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new TwinApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodeWriteAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(string endpointId,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadRawAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            string endpointId, HistoryReadNextRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryReadRawNextAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(string endpointId,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new HistoryApiClient(_client, deviceId, moduleId, _serializer);
            return await client.HistoryUpdateRawAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseModel> NodePublishStartAsync(string endpointId,
            PublishStartRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new PublisherApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodePublishStartAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> NodePublishStopAsync(string endpointId,
            PublishStopRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new PublisherApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodePublishStopAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> NodePublishBulkAsync(string endpointId,
            PublishBulkRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new PublisherApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodePublishBulkAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> NodePublishListAsync(string endpointId,
            PublishedItemListRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId, true, ct);
            var deviceId = DiscovererModelEx.ParseDeviceId(endpoint.Registration.DiscovererId,
                out var moduleId);
            var client = new PublisherApiClient(_client, deviceId, moduleId, _serializer);
            return await client.NodePublishListAsync(new ConnectionModel {
                Endpoint = endpoint.Registration.Endpoint,
                User = request.Header?.Elevation
            }, request, ct);
        }

        private readonly IEndpointRegistry _endpoints;
        private readonly IMethodClient _client;
        private readonly IJsonSerializer _serializer;
    }
}
