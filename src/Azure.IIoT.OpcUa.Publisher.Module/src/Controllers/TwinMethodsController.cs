// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Twin methods controller
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    [ControllerExceptionFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}")]
    [ApiController]
    public class TwinMethodsController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="certificates"></param>
        /// <param name="nodes"></param>
        public TwinMethodsController(IConnectionServices<ConnectionModel> endpoints,
            ICertificateServices<EndpointModel> certificates,
            INodeServices<ConnectionModel> nodes)
        {
            _certificates = certificates ??
                throw new ArgumentNullException(nameof(certificates));
            _nodes = nodes ??
                throw new ArgumentNullException(nameof(nodes));
            _endpoints = endpoints ??
                throw new ArgumentNullException(nameof(endpoints));
        }

        /// <summary>
        /// Get the capabilities of the server
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <c>null</c>.</exception>
        [HttpPost("capabilities")]
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            ConnectionModel connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return await _nodes.GetServerCapabilitiesAsync(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("browse/first")]
        public async Task<BrowseFirstResponseModel> BrowseAsync(
            RequestEnvelope<BrowseFirstRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.BrowseFirstAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("browse/next")]
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            RequestEnvelope<BrowseNextRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.BrowseNextAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("browse")]
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseStreamAsync(
            RequestEnvelope<BrowseStreamRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _nodes.BrowseAsync(request.Connection, request.Request);
        }

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("browse/path")]
        public async Task<BrowsePathResponseModel> BrowsePathAsync(
            RequestEnvelope<BrowsePathRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.BrowsePathAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("read")]
        public async Task<ValueReadResponseModel> ValueReadAsync(
            RequestEnvelope<ValueReadRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.ValueReadAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("write")]
        public async Task<ValueWriteResponseModel> ValueWriteAsync(
            RequestEnvelope<ValueWriteRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.ValueWriteAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get node metadata.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("metadata")]
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(
            RequestEnvelope<NodeMetadataRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.GetMetadataAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("call/$metadata")]
        public async Task<MethodMetadataResponseModel> MethodMetadataAsync(
            RequestEnvelope<MethodMetadataRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.GetMethodMetadataAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("call")]
        public async Task<MethodCallResponseModel> MethodCallAsync(
            RequestEnvelope<MethodCallRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.MethodCallAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read attributes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("read/attributes")]
        public async Task<ReadResponseModel> NodeReadAsync(
            RequestEnvelope<ReadRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.ReadAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Write attributes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("write/attributes")]
        public async Task<WriteResponseModel> NodeWriteAsync(
            RequestEnvelope<WriteRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.WriteAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("historyread/first")]
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            RequestEnvelope<HistoryReadRequestModel<VariantValue>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryReadAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next history
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("historyread/next")]
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            RequestEnvelope<HistoryReadNextRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryReadNextAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Update history
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("historyupdate")]
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            RequestEnvelope<HistoryUpdateRequestModel<VariantValue>> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryUpdateAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <c>null</c>.</exception>
        [HttpPost("certificate")]
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            return await _certificates.GetEndpointCertificateAsync(endpoint).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the historian capabilities of the server
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <c>null</c>.</exception>
        [HttpPost("history/capabilities")]
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return await _nodes.HistoryGetServerCapabilitiesAsync(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the historian configuration
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("history/configuration")]
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            RequestEnvelope<HistoryConfigurationRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryGetConfigurationAsync(request.Connection, request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Activate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <c>null</c>.</exception>
        [HttpPost("connect")]
        public async Task<bool> ConnectAsync(ConnectionModel connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            await _endpoints.ConnectAsync(connection).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Deactivate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <c>null</c>.</exception>
        [HttpPost("disconnect")]
        public async Task<bool> DisconnectAsync(ConnectionModel connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            await _endpoints.DisconnectAsync(connection).ConfigureAwait(false);
            return true;
        }

        private readonly ICertificateServices<EndpointModel> _certificates;
        private readonly IConnectionServices<ConnectionModel> _endpoints;
        private readonly INodeServices<ConnectionModel> _nodes;
    }
}
