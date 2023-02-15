// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Filters;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin method controller
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [ExceptionsFilter]
    public class TwinMethodsController : IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="discovery"></param>
        /// <param name="nodes"></param>
        /// <param name="historian"></param>
        /// <param name="browse"></param>
        public TwinMethodsController(
            IConnectionServices<ConnectionModel> clients,
            ICertificateServices<EndpointModel> discovery,
            INodeServices<ConnectionModel> nodes,
            IHistoricAccessServices<ConnectionModel> historian,
            IBrowseServices<ConnectionModel> browse) {
            _discovery = discovery ??
                throw new ArgumentNullException(nameof(discovery));
            _browse = browse ??
                throw new ArgumentNullException(nameof(browse));
            _historian = historian ??
                throw new ArgumentNullException(nameof(historian));
            _nodes = nodes ??
                throw new ArgumentNullException(nameof(nodes));
            _clients = clients ??
                throw new ArgumentNullException(nameof(clients));
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseResponseApiModel> BrowseAsync(
            ConnectionApiModel connection, BrowseRequestInternalApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _browse.NodeBrowseFirstAsync(
                connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseNextResponseApiModel> BrowseNextAsync(
            ConnectionApiModel connection, BrowseNextRequestInternalApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _browse.NodeBrowseNextAsync(
                connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowsePathResponseApiModel> BrowsePathAsync(
            ConnectionApiModel connection, BrowsePathRequestInternalApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _browse.NodeBrowsePathAsync(
                connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResponseApiModel> ValueReadAsync(
            ConnectionApiModel connection, ValueReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _nodes.NodeValueReadAsync(
                connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResponseApiModel> ValueWriteAsync(
            ConnectionApiModel connection, ValueWriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _nodes.NodeValueWriteAsync(
                connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResponseApiModel> MethodMetadataAsync(
            ConnectionApiModel connection, MethodMetadataRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _nodes.NodeMethodGetMetadataAsync(
                connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResponseApiModel> MethodCallAsync(
            ConnectionApiModel connection, MethodCallRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _nodes.NodeMethodCallAsync(
                connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Read attributes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ReadResponseApiModel> NodeReadAsync(
            ConnectionApiModel connection, ReadRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeReadAsync(
                connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Write attributes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<WriteResponseApiModel> NodeWriteAsync(
            ConnectionApiModel connection, WriteRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeWriteAsync(
                connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponseApiModel<VariantValue>> HistoryReadAsync(
            ConnectionApiModel connection, HistoryReadRequestApiModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryReadAsync(
               connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Read next history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadNextResponseApiModel<VariantValue>> HistoryReadNextAsync(
            ConnectionApiModel connection, HistoryReadNextRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryReadNextAsync(
               connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Update history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateAsync(
            ConnectionApiModel connection, HistoryUpdateRequestApiModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryUpdateAsync(
               connection.ToServiceModel(), request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<byte[]> GetEndpointCertificateAsync(
            EndpointApiModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            var result = await _discovery.GetEndpointCertificateAsync(
                endpoint.ToServiceModel());
            return result;
        }

        /// <summary>
        /// Activate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<bool> ConnectAsync(ConnectionApiModel connection) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            await _clients.ConnectAsync(connection.ToServiceModel());
            return true;
        }

        /// <summary>
        /// Deactivate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<bool> DisconnectAsync(ConnectionApiModel connection) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            await _clients.DisconnectAsync(connection.ToServiceModel());
            return true;
        }

        private readonly ICertificateServices<EndpointModel> _discovery;
        private readonly IConnectionServices<ConnectionModel> _clients;
        private readonly IBrowseServices<ConnectionModel> _browse;
        private readonly IHistoricAccessServices<ConnectionModel> _historian;
        private readonly INodeServices<ConnectionModel> _nodes;
    }
}
