// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Client {
    using Microsoft.Azure.IIoT.OpcUa.Services;
    using Microsoft.Azure.IIoT.OpcUa.Services.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services.Protocol;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Opc.Ua.Bindings.Proxy;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// Validator opens a connection to the server to test connectivity.
    /// </summary>
    public class OpcUaValidationServices : IOpcUaValidationServices {

        /// <summary>
        /// Site id under which validation is happening
        /// </summary>
        public string SiteId { get; set; } = string.Empty;

        /// <summary>
        /// Supervisor id under which validation is happening
        /// </summary>
        public string SupervisorId { get; set; } = string.Empty;

        /// <summary>
        /// Create edge endpoint validator
        /// </summary>
        /// <param name="client"></param>
        public OpcUaValidationServices(IOpcUaClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Discover / read application model from discovery url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationModel> DiscoverApplicationAsync(
            Uri discoveryUrl) {
            var discovered = new List<ApplicationRegistrationModel>();
            var results = await _client.DiscoverAsync(discoveryUrl, CancellationToken.None);
            if (results.Any()) {
                _logger.Info($"Found {results.Count()} endpoints on {discoveryUrl}.",
                    () => { });
            }

            string hostAddress = null; // TODO

            // Merge results...
            foreach (var result in results) {
                discovered.AddOrUpdate(result.ToServiceModel(hostAddress, SiteId,
                    SupervisorId));
            }

            // Check results
            if (discovered.Count == 0) {
                throw new ResourceNotFoundException("Unable to find application");
            }
            return discovered.First();
        }

        /// <summary>
        /// Validate request by connecting to the server and filling in
        /// resulting details.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<ApplicationRegistrationModel> ValidateEndpointAsync(
            EndpointModel endpoint) {

            var uri = new Uri(endpoint.Url);
            ApplicationRegistrationModel result = null;
            await _client.ValidateEndpointAsync(endpoint, (channel, ep) => {

                if (ep == null) {
                    throw new ConnectionException("Endpoint could not be found.");
                }

                // Success - get remote hostname and update id
                result = ep.ToServiceModel(GetAddressFromChannel(channel), SiteId, SupervisorId);

            });
            if (result == null) {
                throw new ResourceNotFoundException("Unable to validate endpoint.");
            }
            return result;
        }

        /// <summary>
        /// Retrieve the host name or ip address from the session
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private string GetAddressFromChannel(ITransportChannel channel) {
            // Access underlying proxy socket
            if (channel is IMessageSocketChannel proxyChannel) {
                if (proxyChannel.Socket is ProxyMessageSocket socket) {
                    var proxySocket = socket.ProxySocket;
                    if (proxySocket == null) {
                        throw new InvalidProgramException(
                            "Unexpected - current proxy socket is null.");
                    }
                    _logger.Debug($"Connected.", () => proxySocket.LocalEndPoint);

                    var address = proxySocket.RemoteEndPoint.AsProxySocketAddress();
                    return address?.Host;
                }
                if (proxyChannel.Socket is TcpMessageSocket tcp) {
                    // TODO
                }
            }
            return null;
        }

        private readonly IOpcUaClient _client;
        private readonly ILogger _logger;
    }
}
