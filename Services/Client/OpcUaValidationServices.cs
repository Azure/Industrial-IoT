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
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
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
        public async Task<ApplicationModel> DiscoverApplicationAsync(
            Uri discoveryUrl) {
            var discovered = new List<ApplicationModel>();
            var results = await _client.DiscoverAsync(discoveryUrl, CancellationToken.None);
            if (results.Any()) {
                _logger.Info($"Found {results.Count()} endpoints on {discoveryUrl}.",
                    () => { });
            }

            // Merge results...
            foreach (var result in results) {
                discovered.AddOrUpdate(result.ToServiceModel(SupervisorId));
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
        public async Task<ApplicationModel> ValidateEndpointAsync(
            EndpointModel endpoint) {

            var uri = new Uri(endpoint.Url);
            ApplicationModel result = null;
            await _client.ValidateEndpointAsync(endpoint, (channel, ep) => {

                if (ep == null) {
                    throw new ConnectionException("Endpoint could not be found.");
                }

                // Success - get remote hostname and update id
                GetAddressesFromChannel(channel, out var hostname, out var gateway);

                endpoint.Url = ep.EndpointUrl;
                endpoint.SecurityMode = ep.SecurityMode.ToServiceType();
                endpoint.SecurityPolicy = ep.SecurityPolicyUri;

                result = new ApplicationModel {
                    Endpoints = new List<EndpointModel> {
                        endpoint
                    },
                    Application = new ApplicationInfoModel {
                        ApplicationId = ApplicationModelEx.CreateApplicationId(
                            SupervisorId, ep.Server.ApplicationUri),
                        ApplicationUri = ep.Server.ApplicationUri,
                        DiscoveryUrls = ep.Server.DiscoveryUrls,
                        DiscoveryProfileUri = ep.Server.DiscoveryProfileUri,
                        ApplicationType = ep.Server.ApplicationType.ToServiceType() ?? 
                            Models.ApplicationType.Server,
                        ProductUri = ep.Server.ProductUri,
                        Certificate = ep.ServerCertificate,
                        ApplicationName = ep.Server.ApplicationName.Text,
                    }
                };

                if (ep.Server.ApplicationType == Opc.Ua.ApplicationType.DiscoveryServer) {
                    result.Application.Capabilities = new List<string> { "LDS" };
                }
                else {
                    result.Application.Capabilities = new List<string>();
                }
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
