// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Tunnel;
    using Microsoft.Extensions.Options;
    using System;
    using System.Linq;
    using System.Text.Json.Nodes;
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
        /// <param name="timeout"></param>
        public TwinApiClient(IMethodClient methodClient, string target,
            TimeSpan? timeout = null)
        {
            _methodClient = methodClient ??
                throw new ArgumentNullException(nameof(methodClient));
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target));
            }
            _target = target;
            _timeout = timeout ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="options"></param>
        public TwinApiClient(IMethodClient methodClient, IOptions<SdkOptions> options) :
            this(methodClient, options.Value.Target!, options.Value.Timeout)
        {
        }

        /// <inheritdoc/>
        public async Task<TestConnectionResponseModel> TestConnectionAsync(
            ConnectionModel connection, TestConnectionRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "TestConnection_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<TestConnectionResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> NodeBrowseFirstAsync(ConnectionModel connection,
            BrowseFirstRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "Browse_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<BrowseFirstResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> NodeBrowseNextAsync(ConnectionModel connection,
            BrowseNextRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "BrowseNext_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<BrowseNextResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> NodeBrowsePathAsync(ConnectionModel connection,
            BrowsePathRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.BrowsePaths == null || request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Count == 0))
            {
                throw new ArgumentException("Browse paths missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "BrowsePath_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<BrowsePathResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> NodeReadAsync(ConnectionModel connection,
            ReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Attributes == null || request.Attributes.Count == 0)
            {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "NodeRead_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<ReadResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> NodeWriteAsync(ConnectionModel connection,
            WriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Attributes == null || request.Attributes.Count == 0)
            {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "NodeWrite_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<WriteResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> NodeValueReadAsync(ConnectionModel connection,
            ValueReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "ValueRead_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<ValueReadResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> NodeValueWriteAsync(ConnectionModel connection,
            ValueWriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Value is null)
            {
                throw new ArgumentException("Value missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "ValueWrite_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<ValueWriteResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> NodeMethodGetMetadataAsync(
            ConnectionModel connection, MethodMetadataRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "MethodMetadata_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<MethodMetadataResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> NodeMethodCallAsync(
            ConnectionModel connection, MethodCallRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "MethodCall_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<MethodCallResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            ConnectionModel connection, RequestHeaderModel? header, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "GetServerCapabilities_V2", Json.SerializeToMemory(new
                {
                    connection,
                    header
                }),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<ServerCapabilitiesModel>();
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(ConnectionModel connection,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "GetMetadata_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<NodeMetadataResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<QueryCompilationResponseModel> CompileQueryAsync(ConnectionModel connection,
            QueryCompilationRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "CompileQuery_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<QueryCompilationResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection, RequestHeaderModel? header, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryGetServerCapabilities_V2", Json.SerializeToMemory(new
                {
                    connection,
                    header
                }),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<HistoryServerCapabilitiesModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel connection, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryGetConfiguration_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<HistoryConfigurationResponseModel>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<JsonNode>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<JsonNode> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryRead_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<HistoryReadResponseModel<JsonNode>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<JsonNode>> HistoryReadNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrEmpty(request.ContinuationToken))
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReadNext_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<HistoryReadNextResponseModel<JsonNode>>();
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<JsonNode> request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryUpdate_V2", Json.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return response.DeserializeResponse<HistoryUpdateResponseModel>();
        }

        private readonly IMethodClient _methodClient;
        private readonly string _target;
        private readonly TimeSpan _timeout;
    }
}
