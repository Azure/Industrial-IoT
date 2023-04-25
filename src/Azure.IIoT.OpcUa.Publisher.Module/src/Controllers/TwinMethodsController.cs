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
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

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
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/>
        /// is <c>null</c>.</exception>
        [HttpPost("capabilities")]
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            ConnectionModel connection, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return await _nodes.GetServerCapabilitiesAsync(connection,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("browse/first")]
        public async Task<BrowseFirstResponseModel> BrowseAsync(
            RequestEnvelope<BrowseFirstRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.BrowseFirstAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("browse/next")]
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            RequestEnvelope<BrowseNextRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.BrowseNextAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("browse")]
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseStreamAsync(
            RequestEnvelope<BrowseStreamRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _nodes.BrowseAsync(request.Connection, request.Request, ct);
        }

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("browse/path")]
        public async Task<BrowsePathResponseModel> BrowsePathAsync(
            RequestEnvelope<BrowsePathRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.BrowsePathAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("read")]
        public async Task<ValueReadResponseModel> ValueReadAsync(
            RequestEnvelope<ValueReadRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.ValueReadAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("write")]
        public async Task<ValueWriteResponseModel> ValueWriteAsync(
            RequestEnvelope<ValueWriteRequestModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.ValueWriteAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get node metadata.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("metadata")]
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(
            RequestEnvelope<NodeMetadataRequestModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.GetMetadataAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("call/$metadata")]
        public async Task<MethodMetadataResponseModel> MethodMetadataAsync(
            RequestEnvelope<MethodMetadataRequestModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.GetMethodMetadataAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("call")]
        public async Task<MethodCallResponseModel> MethodCallAsync(
            RequestEnvelope<MethodCallRequestModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.MethodCallAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read attributes
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("read/attributes")]
        public async Task<ReadResponseModel> NodeReadAsync(
            RequestEnvelope<ReadRequestModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.ReadAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Write attributes
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("write/attributes")]
        public async Task<WriteResponseModel> NodeWriteAsync(
            RequestEnvelope<WriteRequestModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.WriteAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("historyread/first")]
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            RequestEnvelope<HistoryReadRequestModel<VariantValue>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryReadAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next history
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("historyread/next")]
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            RequestEnvelope<HistoryReadNextRequestModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryReadNextAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Update history
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("historyupdate")]
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            RequestEnvelope<HistoryUpdateRequestModel<VariantValue>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryUpdateAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="endpoint"/>
        /// is <c>null</c>.</exception>
        [HttpPost("certificate")]
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            return await _certificates.GetEndpointCertificateAsync(endpoint,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the historian capabilities of the server
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/>
        /// is <c>null</c>.</exception>
        [HttpPost("history/capabilities")]
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return await _nodes.HistoryGetServerCapabilitiesAsync(connection,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the historian configuration
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("history/configuration")]
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            RequestEnvelope<HistoryConfigurationRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryGetConfigurationAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Connect
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("connect")]
        public async Task<ConnectResponseModel> ConnectAsync(
            RequestEnvelope<ConnectRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _endpoints.ConnectAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Test connection
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("test")]
        public async Task<TestConnectionResponseModel> TestConnectionAsync(
            RequestEnvelope<TestConnectionRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _endpoints.TestConnectionAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("disconnect")]
        public async Task DisconnectAsync(
            RequestEnvelope<DisconnectRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            await _endpoints.DisconnectAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        private readonly ICertificateServices<EndpointModel> _certificates;
        private readonly IConnectionServices<ConnectionModel> _endpoints;
        private readonly INodeServices<ConnectionModel> _nodes;
    }
}
