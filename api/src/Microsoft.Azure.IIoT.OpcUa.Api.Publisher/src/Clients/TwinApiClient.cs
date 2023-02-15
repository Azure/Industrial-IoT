// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin api.
    /// </summary>
    public sealed class TwinApiClient : ITwinModuleApi {

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="serializer"></param>
        public TwinApiClient(IMethodClient methodClient, string deviceId,
            string moduleId = null, IJsonSerializer serializer = null) {
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
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
        public TwinApiClient(IMethodClient methodClient, IPublisherModuleConfig config = null,
            IJsonSerializer serializer = null) :
            this(methodClient, config?.DeviceId, config?.ModuleId, serializer) {
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseApiModel> NodeBrowseFirstAsync(ConnectionApiModel connection,
            BrowseRequestApiModel request, CancellationToken ct) {
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
                }), null, ct);
            return _serializer.Deserialize<BrowseResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseApiModel> NodeBrowseNextAsync(ConnectionApiModel connection,
            BrowseNextRequestApiModel request, CancellationToken ct) {
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
                }), null, ct);
            return _serializer.Deserialize<BrowseNextResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseApiModel> NodeBrowsePathAsync(ConnectionApiModel connection,
            BrowsePathRequestApiModel request, CancellationToken ct) {
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
                request.BrowsePaths.Any(p => p == null || p.Length == 0)) {
                throw new ArgumentNullException(nameof(request.BrowsePaths));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "BrowsePath_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct);
            return _serializer.Deserialize<BrowsePathResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseApiModel> NodeReadAsync(ConnectionApiModel connection,
            ReadRequestApiModel request, CancellationToken ct) {
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
                }), null, ct);
            return _serializer.Deserialize<ReadResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseApiModel> NodeWriteAsync(ConnectionApiModel connection,
            WriteRequestApiModel request, CancellationToken ct) {
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
                }), null, ct);
            return _serializer.Deserialize<WriteResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseApiModel> NodeValueReadAsync(ConnectionApiModel connection,
            ValueReadRequestApiModel request, CancellationToken ct) {
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
                }), null, ct);
            return _serializer.Deserialize<ValueReadResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseApiModel> NodeValueWriteAsync(ConnectionApiModel connection,
            ValueWriteRequestApiModel request, CancellationToken ct) {
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
                }), null, ct);
            return _serializer.Deserialize<ValueWriteResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            ConnectionApiModel connection, MethodMetadataRequestApiModel request, CancellationToken ct) {
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
                }), null, ct);
            return _serializer.Deserialize<MethodMetadataResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            ConnectionApiModel connection, MethodCallRequestApiModel request, CancellationToken ct) {
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
                }), null, ct);
            return _serializer.Deserialize<MethodCallResponseApiModel>(response);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
