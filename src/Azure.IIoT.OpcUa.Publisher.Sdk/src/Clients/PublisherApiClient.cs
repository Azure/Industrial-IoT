// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients {
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node and publish services through command control against
    /// the OPC Publihser module.
    /// </summary>
    public sealed class PublisherApiClient : IPublisherApi {

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="serializer"></param>
        public PublisherApiClient(IMethodClient methodClient, string deviceId,
            string moduleId = null, IJsonSerializer serializer = null) {
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _methodClient = methodClient ??
                throw new ArgumentNullException(nameof(methodClient));
            _moduleId = moduleId;
            _deviceId = deviceId;
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public PublisherApiClient(IMethodClient methodClient,
            ISdkConfig config = null, IJsonSerializer serializer = null) :
            this(methodClient, config?.DeviceId, config?.ModuleId, serializer) {
        }

        /// <inheritdoc/>
        public async Task<PublishStartResponseModel> NodePublishStartAsync(ConnectionModel connection,
            PublishStartRequestModel request, CancellationToken ct = default) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "PublishStart", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct);
            return _serializer.Deserialize<PublishStartResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishStopResponseModel> NodePublishStopAsync(ConnectionModel connection,
            PublishStopRequestModel request, CancellationToken ct = default) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "PublishStop", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct);
            return _serializer.Deserialize<PublishStopResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResponseModel> NodePublishBulkAsync(ConnectionModel connection,
            PublishBulkRequestModel request, CancellationToken ct = default) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "PublishBulk", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct);
            return _serializer.Deserialize<PublishBulkResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResponseModel> NodePublishListAsync(ConnectionModel connection,
            PublishedItemListRequestModel request, CancellationToken ct = default) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "PublishList", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct);
            return _serializer.Deserialize<PublishedItemListResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> PublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "PublishNodes", _serializer.SerializeToString(request), null, ct);
            return _serializer.Deserialize<PublishedNodesResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> UnpublishNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "UnpublishNodes", _serializer.SerializeToString(request), null, ct);
            return _serializer.Deserialize<PublishedNodesResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> UnpublishAllNodesAsync(
            PublishedNodesEntryModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "UnpublishAllNodes", _serializer.SerializeToString(request), null, ct);
            return _serializer.Deserialize<PublishedNodesResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodesResponseModel> AddOrUpdateEndpointsAsync(
            List<PublishedNodesEntryModel> request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
               "AddOrUpdateEndpoints", _serializer.SerializeToString(request), null, ct);
            return _serializer.Deserialize<PublishedNodesResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredEndpointsResponseModel> GetConfiguredEndpointsAsync(
            CancellationToken ct) {
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
               "GetConfiguredEndpoints", null, null, ct);
            return _serializer.Deserialize<GetConfiguredEndpointsResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<GetConfiguredNodesOnEndpointResponseModel> GetConfiguredNodesOnEndpointAsync(
            PublishedNodesEntryModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "GetConfiguredNodesOnEndpoint", _serializer.SerializeToString(request), null, ct);
            return _serializer.Deserialize<GetConfiguredNodesOnEndpointResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(CancellationToken ct) {
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "GetDiagnosticInfo", null, null, ct);
            return _serializer.Deserialize<List<PublishDiagnosticInfoModel>>(response);
        }


        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
