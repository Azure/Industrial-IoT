// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements node services as adapter on top of api.
    /// </summary>
    public sealed class TwinWebApiAdapter : INodeServices<string>
    {
        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="client"></param>
        public TwinWebApiAdapter(ITwinServiceApi client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            string endpoint, RequestHeaderModel header, CancellationToken ct)
        {
            return await _client.GetServerCapabilitiesAsync(endpoint, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseFirstResponseModel> BrowseFirstAsync(
            string endpoint, BrowseFirstRequestModel request, CancellationToken ct)
        {
            return await _client.NodeBrowseFirstAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            string endpoint, BrowseNextRequestModel request, CancellationToken ct)
        {
            return await _client.NodeBrowseNextAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseAsync(string endpoint,
            BrowseStreamRequestModel request, CancellationToken ct)
        {
            // TODO
            throw new NotSupportedException("Browse stream not supported.");
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(
            string endpoint, BrowsePathRequestModel request, CancellationToken ct)
        {
            return await _client.NodeBrowsePathAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(string endpoint,
            NodeMetadataRequestModel request, CancellationToken ct)
        {
            return await _client.NodeGetMetadataAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<QueryCompilationResponseModel> CompileQueryAsync(string endpoint,
            QueryCompilationRequestModel request, CancellationToken ct)
        {
            // TODO
            throw new NotSupportedException("Compling query not supported.");
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseModel> ValueReadAsync(
            string endpoint, ValueReadRequestModel request, CancellationToken ct)
        {
            return await _client.NodeValueReadAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(
            string endpoint, ValueWriteRequestModel request, CancellationToken ct)
        {
            return await _client.NodeValueWriteAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseModel> GetMethodMetadataAsync(
            string endpoint, MethodMetadataRequestModel request, CancellationToken ct)
        {
            return await _client.NodeMethodGetMetadataAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseModel> MethodCallAsync(
            string endpoint, MethodCallRequestModel request, CancellationToken ct)
        {
            return await _client.NodeMethodCallAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseModel> ReadAsync(
            string endpoint, ReadRequestModel request, CancellationToken ct)
        {
            return await _client.NodeReadAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseModel> WriteAsync(
            string endpoint, WriteRequestModel request, CancellationToken ct)
        {
            return await _client.NodeWriteAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            string endpoint, RequestHeaderModel header, CancellationToken ct)
        {
            return await _client.HistoryGetServerCapabilitiesAsync(endpoint, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            string endpoint, HistoryConfigurationRequestModel request, CancellationToken ct)
        {
            return await _client.HistoryGetConfigurationAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(string endpoint,
            HistoryReadRequestModel<VariantValue> request, CancellationToken ct)
        {
            return await _client.HistoryReadAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            string endpoint, HistoryReadNextRequestModel request, CancellationToken ct)
        {
            return await _client.HistoryReadNextAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(string endpoint,
            HistoryUpdateRequestModel<VariantValue> request, CancellationToken ct)
        {
            return await _client.HistoryUpdateAsync(endpoint, request, ct).ConfigureAwait(false);
        }

        private readonly ITwinServiceApi _client;
    }
}
