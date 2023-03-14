// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controller
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Furly.Tunnel.Router;
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Twin method controller
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
        [HttpPost("capabilities")]
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            ConnectionModel connection)
        {
            return await _nodes.GetServerCapabilitiesAsync(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("browse")]
        public async Task<BrowseFirstResponseModel> BrowseAsync(
            ConnectionModel connection, BrowseFirstRequestModel request)
        {
            return await _nodes.BrowseFirstAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("browse/next")]
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            ConnectionModel connection, BrowseNextRequestModel request)
        {
            return await _nodes.BrowseNextAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("browse/path")]
        public async Task<BrowsePathResponseModel> BrowsePathAsync(
            ConnectionModel connection, BrowsePathRequestModel request)
        {
            return await _nodes.BrowsePathAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("read")]
        public async Task<ValueReadResponseModel> ValueReadAsync(
            ConnectionModel connection, ValueReadRequestModel request)
        {
            return await _nodes.ValueReadAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("write")]
        public async Task<ValueWriteResponseModel> ValueWriteAsync(
            ConnectionModel connection, ValueWriteRequestModel request)
        {
            return await _nodes.ValueWriteAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get node metadata.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("metadata")]
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(
            ConnectionModel connection, NodeMetadataRequestModel request)
        {
            return await _nodes.GetMetadataAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("call/$metadata")]
        public async Task<MethodMetadataResponseModel> MethodMetadataAsync(
            ConnectionModel connection, MethodMetadataRequestModel request)
        {
            return await _nodes.GetMethodMetadataAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("call")]
        public async Task<MethodCallResponseModel> MethodCallAsync(
            ConnectionModel connection, MethodCallRequestModel request)
        {
            return await _nodes.MethodCallAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read attributes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("read/attributes")]
        public async Task<ReadResponseModel> NodeReadAsync(
            ConnectionModel connection, ReadRequestModel request)
        {
            return await _nodes.ReadAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Write attributes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("write/attributes")]
        public async Task<WriteResponseModel> NodeWriteAsync(
            ConnectionModel connection, WriteRequestModel request)
        {
            return await _nodes.WriteAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("historyread")]
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<VariantValue> request)
        {
            return await _nodes.HistoryReadAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("historyread/next")]
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request)
        {
            return await _nodes.HistoryReadNextAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Update history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("historyupdate")]
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<VariantValue> request)
        {
            return await _nodes.HistoryUpdateAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        [HttpPost("certificate")]
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint)
        {
            return await _certificates.GetEndpointCertificateAsync(endpoint).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the historian capabilities of the server
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [HttpPost("history/capabilities")]
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection)
        {
            return await _nodes.HistoryGetServerCapabilitiesAsync(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the historian configuration
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("history/configuration")]
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel connection, HistoryConfigurationRequestModel request)
        {
            return await _nodes.HistoryGetConfigurationAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Activate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [HttpPost("connect")]
        public async Task<bool> ConnectAsync(ConnectionModel connection)
        {
            await _endpoints.ConnectAsync(connection).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Deactivate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [HttpPost("disconnect")]
        public async Task<bool> DisconnectAsync(ConnectionModel connection)
        {
            await _endpoints.DisconnectAsync(connection).ConfigureAwait(false);
            return true;
        }

        private readonly ICertificateServices<EndpointModel> _certificates;
        private readonly IConnectionServices<ConnectionModel> _endpoints;
        private readonly INodeServices<ConnectionModel> _nodes;
    }
}
