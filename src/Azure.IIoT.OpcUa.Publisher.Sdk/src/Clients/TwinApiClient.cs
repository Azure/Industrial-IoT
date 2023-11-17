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
    using Microsoft.Extensions.Options;
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
        /// <param name="timeout"></param>
        /// <param name="serializer"></param>
        public TwinApiClient(IMethodClient methodClient, string target,
            TimeSpan? timeout = null, IJsonSerializer? serializer = null)
        {
            _serializer = serializer ??
                new NewtonsoftJsonSerializer();
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
        /// <param name="serializer"></param>
        public TwinApiClient(IMethodClient methodClient, IOptions<SdkOptions> options,
            IJsonSerializer? serializer = null) :
            this(methodClient, options.Value.Target!, options.Value.Timeout,
                serializer)
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
                "TestConnection_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<TestConnectionResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ConnectResponseModel> ConnectAsync(ConnectionModel connection,
            ConnectRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            var response = await _methodClient.CallMethodAsync(_target,
                "Connect_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<ConnectResponseModel>(response);
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
                "Browse_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<BrowseFirstResponseModel>(response);
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
                "BrowseNext_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<BrowseNextResponseModel>(response);
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
                "BrowsePath_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<BrowsePathResponseModel>(response);
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
                "NodeRead_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<ReadResponseModel>(response);
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
                "NodeWrite_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<WriteResponseModel>(response);
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
                "ValueRead_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<ValueReadResponseModel>(response);
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
                "ValueWrite_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<ValueWriteResponseModel>(response);
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
                "MethodMetadata_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<MethodMetadataResponseModel>(response);
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
                "MethodCall_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<MethodCallResponseModel>(response);
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
                "GetServerCapabilities_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    header
                }),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<ServerCapabilitiesModel>(response);
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
                "GetMetadata_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<NodeMetadataResponseModel>(response);
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
                "CompileQuery_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<QueryCompilationResponseModel>(response);
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
                "HistoryGetServerCapabilities_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    header
                }),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryServerCapabilitiesModel>(response);
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
                "HistoryGetConfiguration_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryConfigurationResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<VariantValue> request,
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
                "HistoryRead_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryReadResponseModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
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
                "HistoryReadNext_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryReadNextResponseModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<VariantValue> request,
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
                "HistoryUpdate_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(ConnectionModel connection,
            DisconnectRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            await _methodClient.CallMethodAsync(_target,
                "Disconnect_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }),
                ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _target;
        private readonly TimeSpan _timeout;
    }
}
