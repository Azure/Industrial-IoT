// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Tunnel;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin api.
    /// </summary>
    public sealed class TwinApiClient : ITwinApi
    {
        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="target"></param>
        /// <param name="serializer"></param>
        public TwinApiClient(IMethodClient methodClient, string target,
            IJsonSerializer serializer = null)
        {
            _serializer = serializer ??
                new NewtonsoftJsonSerializer();
            _methodClient = methodClient ??
                throw new ArgumentNullException(nameof(methodClient));
            _target = target;
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public TwinApiClient(IMethodClient methodClient, ISdkConfig config = null,
            IJsonSerializer serializer = null) :
            this(methodClient, config?.Target, serializer)
        {
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> NodeBrowseFirstAsync(ConnectionModel connection,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "Browse_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<BrowseFirstResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> NodeBrowseNextAsync(ConnectionModel connection,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "BrowseNext_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<BrowseNextResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> NodeBrowsePathAsync(ConnectionModel connection,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.BrowsePaths == null || request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Count == 0))
            {
                throw new ArgumentException("Browse paths missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "BrowsePath_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<BrowsePathResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> NodeReadAsync(ConnectionModel connection,
            ReadRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0)
            {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "NodeRead_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<ReadResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> NodeWriteAsync(ConnectionModel connection,
            WriteRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0)
            {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "NodeWrite_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<WriteResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> NodeValueReadAsync(ConnectionModel connection,
            ValueReadRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "ValueRead_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<ValueReadResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> NodeValueWriteAsync(ConnectionModel connection,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Value is null)
            {
                throw new ArgumentException("Value missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "ValueWrite_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<ValueWriteResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(
            ConnectionModel connection, MethodMetadataRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "MethodMetadata_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<MethodMetadataResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> NodeMethodCallAsync(
            ConnectionModel connection, MethodCallRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "MethodCall_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<MethodCallResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(ConnectionModel connection,
            CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "GetServerCapabilities_V2", _serializer.SerializeToMemory(connection),
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<ServerCapabilitiesModel>(response);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(ConnectionModel connection,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "GetMetadata_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<NodeMetadataResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryGetServerCapabilities_V2", _serializer.SerializeToMemory(connection),
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryServerCapabilitiesModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel connection, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryGetConfiguration_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryConfigurationResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<VariantValue> request,
            CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryRead_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadResponseModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request,
            CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReadNext_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadNextResponseModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<VariantValue> request,
            CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryUpdate_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task ConnectAsync(ConnectionModel connection, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            await _methodClient.CallMethodAsync(_target,
                "Connect_V2", _serializer.SerializeToMemory(connection),
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(ConnectionModel connection, CancellationToken ct)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            await _methodClient.CallMethodAsync(_target,
                "Disconnect_V2", _serializer.SerializeToMemory(connection),
                ContentMimeType.Json, null, ct).ConfigureAwait(false);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _target;
    }
}
