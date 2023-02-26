// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controller
{
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Module.Framework;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;

    /// <summary>
    /// Twin method controller
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [ExceptionsFilter]
    public class TwinMethodsController : IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="discovery"></param>
        /// <param name="nodes"></param>
        public TwinMethodsController(
            IConnectionServices<ConnectionModel> clients,
            ICertificateServices<EndpointModel> discovery,
            INodeServices<ConnectionModel> nodes)
        {
            _discovery = discovery ??
                throw new ArgumentNullException(nameof(discovery));
            _nodes = nodes ??
                throw new ArgumentNullException(nameof(nodes));
            _clients = clients ??
                throw new ArgumentNullException(nameof(clients));
        }

        /// <summary>
        /// Get the capabilities of the server
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <c>null</c>.</exception>
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            ConnectionModel connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.GetServerCapabilitiesAsync(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<BrowseFirstResponseModel> BrowseAsync(
            ConnectionModel connection, BrowseFirstRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.BrowseFirstAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            ConnectionModel connection, BrowseNextRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.BrowseNextAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(
            ConnectionModel connection, BrowsePathRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.BrowsePathAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<ValueReadResponseModel> ValueReadAsync(
            ConnectionModel connection, ValueReadRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.ValueReadAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(
            ConnectionModel connection, ValueWriteRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.ValueWriteAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get node metadata.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(
            ConnectionModel connection, NodeMetadataRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.GetMetadataAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<MethodMetadataResponseModel> MethodMetadataAsync(
            ConnectionModel connection, MethodMetadataRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.GetMethodMetadataAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<MethodCallResponseModel> MethodCallAsync(
            ConnectionModel connection, MethodCallRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.MethodCallAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read attributes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<ReadResponseModel> NodeReadAsync(
            ConnectionModel connection, ReadRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _nodes.ReadAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Write attributes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<WriteResponseModel> NodeWriteAsync(
            ConnectionModel connection, WriteRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _nodes.WriteAsync(
                connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<VariantValue> request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _nodes.HistoryReadAsync(
               connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _nodes.HistoryReadNextAsync(
               connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Update history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<VariantValue> request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _nodes.HistoryUpdateAsync(
               connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <c>null</c>.</exception>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            return await _discovery.GetEndpointCertificateAsync(
                endpoint).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the historian capabilities of the server
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <c>null</c>.</exception>
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            ConnectionModel connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.HistoryGetServerCapabilitiesAsync(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the historian configuration
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            ConnectionModel connection, HistoryConfigurationRequestModel request)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return await _nodes.HistoryGetConfigurationAsync(connection, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Activate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <c>null</c>.</exception>
        public async Task<bool> ConnectAsync(ConnectionModel connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            await _clients.ConnectAsync(connection).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Deactivate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <c>null</c>.</exception>
        public async Task<bool> DisconnectAsync(ConnectionModel connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            await _clients.DisconnectAsync(connection).ConfigureAwait(false);
            return true;
        }

        private readonly ICertificateServices<EndpointModel> _discovery;
        private readonly IConnectionServices<ConnectionModel> _clients;
        private readonly INodeServices<ConnectionModel> _nodes;
    }
}
