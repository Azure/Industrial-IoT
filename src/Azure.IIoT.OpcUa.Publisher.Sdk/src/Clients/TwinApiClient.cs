// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients {
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin api.
    /// </summary>
    public sealed class TwinApiClient : ITwinApi {
        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="serializer"></param>
        public TwinApiClient(IMethodClient methodClient, string deviceId,
            string moduleId = null, IJsonSerializer serializer = null) {
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _methodClient = methodClient ?? throw new ArgumentNullException(nameof(methodClient));
            _moduleId = moduleId;
            _deviceId = deviceId;
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public TwinApiClient(IMethodClient methodClient, ISdkConfig config = null,
            IJsonSerializer serializer = null) :
            this(methodClient, config?.DeviceId, config?.ModuleId, serializer) {
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> NodeBrowseFirstAsync(ConnectionModel connection,
            BrowseFirstRequestModel request, CancellationToken ct) {
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
                "Browse_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<BrowseFirstResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> NodeBrowseNextAsync(ConnectionModel connection,
            BrowseNextRequestModel request, CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ContinuationToken == null) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "BrowseNext_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<BrowseNextResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> NodeBrowsePathAsync(ConnectionModel connection,
            BrowsePathRequestModel request, CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.BrowsePaths == null || request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Count == 0)) {
                throw new ArgumentNullException(nameof(request.BrowsePaths));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "BrowsePath_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<BrowsePathResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> NodeReadAsync(ConnectionModel connection,
            ReadRequestModel request, CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "NodeRead_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<ReadResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> NodeWriteAsync(ConnectionModel connection,
            WriteRequestModel request, CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "NodeWrite_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<WriteResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> NodeValueReadAsync(ConnectionModel connection,
            ValueReadRequestModel request, CancellationToken ct) {
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
                "ValueRead_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<ValueReadResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> NodeValueWriteAsync(ConnectionModel connection,
            ValueWriteRequestModel request, CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Value is null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "ValueWrite_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<ValueWriteResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(
            ConnectionModel connection, MethodMetadataRequestModel request, CancellationToken ct) {
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
                "MethodMetadata_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<MethodMetadataResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> NodeMethodCallAsync(
            ConnectionModel connection, MethodCallRequestModel request, CancellationToken ct) {
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
                "MethodCall_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<MethodCallResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(ConnectionModel connection,
            CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "GetServerCapabilities_V2", _serializer.SerializeToString(connection), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<ServerCapabilitiesModel>(response);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(ConnectionModel connection,
            NodeMetadataRequestModel request, CancellationToken ct) {
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
                "GetMetadata_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<NodeMetadataResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection, CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryGetServerCapabilities_V2", _serializer.SerializeToString(connection), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryServerCapabilitiesModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel connection, HistoryConfigurationRequestModel request, CancellationToken ct) {
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
                "HistoryGetConfiguration_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryConfigurationResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryRead_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadResponseModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request,
            CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadNext_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadNextResponseModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryUpdate_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task ConnectAsync(ConnectionModel connection, CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "Connect_V2", _serializer.SerializeToString(connection), null, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(ConnectionModel connection, CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "Disconnect_V2", _serializer.SerializeToString(connection), null, ct).ConfigureAwait(false);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
