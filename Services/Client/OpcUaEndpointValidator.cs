// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Client {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Opc.Ua.Bindings.Proxy;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge validator opens a connection to the server to test connectivity.
    /// </summary>
    public class OpcUaEndpointValidator : IOpcUaEndpointValidator {

        /// <summary>
        /// Create edge endpoint validator
        /// </summary>
        /// <param name="client"></param>
        public OpcUaEndpointValidator(IOpcUaClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validate request by connecting to the server and filling in
        /// resulting details.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<ServerEndpointModel> ValidateAsync(
            EndpointModel endpoint) {

            var uri = new Uri(endpoint.Url);
            var result = new ServerEndpointModel();
            await _client.ValidateEndpointAsync(endpoint, (channel, ep) => {

                if (ep == null) {
                    throw new ConnectionException("Endpoint not be found.");
                }

                // Success - get remote hostname and update id
                GetAddressesFromChannel(channel, out var hostname, out var gateway);

                result.Endpoint = endpoint;
                result.Server = new ServerInfoModel {
                    ApplicationUri = ep.Server.ApplicationUri,
                    ServerCertificate = ep.ServerCertificate,
                    ApplicationName = ep.Server.ApplicationName.Text
                };

                result.Endpoint.Url = ep.EndpointUrl;
                result.Endpoint.SecurityMode = ep.SecurityMode.ToServiceType();
                result.Endpoint.SecurityPolicy = ep.SecurityPolicyUri;
            });
            return result;
        }

        /// <summary>
        /// Retrieve the host name or ip address from the session
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private void GetAddressesFromChannel(ITransportChannel channel,
            out string hostname, out string gateway) {
            // Access underlying proxy socket
            if (channel is IMessageSocketChannel proxyChannel &&
                proxyChannel.Socket is ProxyMessageSocket socket) {
                var proxySocket = socket.ProxySocket;
                if (proxySocket == null) {
                    throw new InvalidProgramException(
                        "Unexpected - current proxy socket is null.");
                }
                _logger.Debug($"Connected.", () => proxySocket.LocalEndPoint);

                var address = proxySocket.LocalEndPoint.AsProxySocketAddress();
                hostname = address.Host;
                gateway = proxySocket.InterfaceEndPoint.AsProxySocketAddress().Host;
            }
            else {
                hostname = Utils.GetHostName();
                gateway = null;
            }
        }

        private readonly IOpcUaClient _client;
        private readonly ILogger _logger;
    }
}
