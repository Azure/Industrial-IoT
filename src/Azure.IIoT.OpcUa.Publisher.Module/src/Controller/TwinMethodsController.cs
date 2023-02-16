// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controller {
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
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
        public async Task<BrowseResponseModel> BrowseAsync(
            ConnectionModel connection, BrowseRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _browse.NodeBrowseFirstAsync(
                connection, request);
            return result;
        }

        /// <summary>
        /// Browse next
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            ConnectionModel connection, BrowseNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _browse.NodeBrowseNextAsync(
                connection, request);
            return result;
        }

        /// <summary>
        /// Browse by path
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowsePathResponseModel> BrowsePathAsync(
            ConnectionModel connection, BrowsePathRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _browse.NodeBrowsePathAsync(
                connection, request);
            return result;
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueReadResponseModel> ValueReadAsync(
            ConnectionModel connection, ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _nodes.NodeValueReadAsync(
                connection, request);
            return result;
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResponseModel> ValueWriteAsync(
            ConnectionModel connection, ValueWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _nodes.NodeValueWriteAsync(
                connection, request);
            return result;
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResponseModel> MethodMetadataAsync(
            ConnectionModel connection, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _nodes.NodeMethodGetMetadataAsync(
                connection, request);
            return result;
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResponseModel> MethodCallAsync(
            ConnectionModel connection, MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            var result = await _nodes.NodeMethodCallAsync(
                connection, request);
            return result;
        }

        /// <summary>
        /// Read attributes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ReadResponseModel> NodeReadAsync(
            ConnectionModel connection, ReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeReadAsync(
                connection, request);
            return result;
        }

        /// <summary>
        /// Write attributes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<WriteResponseModel> NodeWriteAsync(
            ConnectionModel connection, WriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _nodes.NodeWriteAsync(
                connection, request);
            return result;
        }

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            ConnectionModel connection, HistoryReadRequestModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryReadAsync(
               connection, request);
            return result;
        }

        /// <summary>
        /// Read next history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryReadNextAsync(
               connection, request);
            return result;
        }

        /// <summary>
        /// Update history
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _historian.HistoryUpdateAsync(
               connection, request);
            return result;
        }

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            var result = await _discovery.GetEndpointCertificateAsync(
                endpoint);
            return result;
        }

        /// <summary>
        /// Activate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<bool> ConnectAsync(ConnectionModel connection) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            await _clients.ConnectAsync(connection);
            return true;
        }

        /// <summary>
        /// Deactivate connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<bool> DisconnectAsync(ConnectionModel connection) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            await _clients.DisconnectAsync(connection);
            return true;
        }

        private readonly ICertificateServices<EndpointModel> _discovery;
        private readonly IConnectionServices<ConnectionModel> _clients;
        private readonly IBrowseServices<ConnectionModel> _browse;
        private readonly IHistoricAccessServices<ConnectionModel> _historian;
        private readonly INodeServices<ConnectionModel> _nodes;
    }
}
