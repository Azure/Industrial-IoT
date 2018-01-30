// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Client {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Opc.Ua.Bindings.Proxy;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Access the edge publisher via proxy and configures it to publish to its
    /// device identity endpoint. (V1 functionality)
    /// </summary>
    public class OpcUaEdgeProxy : IOpcUaPublisher, IOpcUaPublishServices,
        IOpcUaValidationServices {

        /// <summary>
        /// Create edge proxy service to control publisher
        /// </summary>
        /// <param name="client"></param>
        public OpcUaEdgeProxy(IOpcUaClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!_client.UsesProxy) {
                _logger.Warn("Bad configuration - client should be in remote proxy mode. " +
                    "If you did not intend to test the publisher module without proxy, " +
                    "this is likely the result of a missing IoT Hub connnection string " +
                    "in the services configuration or the bypass development setting set" +
                    "to true.", () => { });
            }
        }

        /// <summary>
        /// Validate request by connecting to the server and filling in
        /// resulting details.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ServerRegistrationRequestModel> ValidateAsync(
            ServerRegistrationRequestModel request) {

            var uri = new Uri(request.Endpoint.Url);
            await _client.TryConnectAsync(request.Endpoint, (channel, endpoints) => {

                // Success - get remote hostname and update id
                GetAddressesFromChannel(channel, uri.Port,
                    out var id, out var hostname, out var gateway);
                if (string.IsNullOrEmpty(request.Id)) {
                    request.Id = id;
                }

                foreach (var ep in endpoints) {
                    // Save server cert
                    if (ep.ServerCertificate != null) {
                        request.Endpoint.ServerCertificate =
                            new X509Certificate2(ep.ServerCertificate);
                        break;
                    }
                }
            });
            return request;
        }

        /// <summary>
        /// Returns the list of published nodes of the OPC UA server with the given endpointUrl
        /// </summary>
        public async Task<IEnumerable<string>> GetPublishedNodeIds(ServerEndpointModel endpoint) {
            var publisher = await FindPublisherForServerAsync(endpoint);
            return await _client.ExecuteServiceAsync(publisher, session => {
                var publishedNodes = new List<string>();
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = new NodeId("Methods", 2),
                        MethodId = new NodeId("GetPublishedNodes", 2),
                        InputArguments = new VariantCollection {
                            new Variant(endpoint.Url)
                        }
                    }
                };
                var responseHeader = session.Call(null, requests,
                    out var results, out var diagnosticInfos);
                if (results.Count > 0 && results[0] != null) {
                    if (StatusCode.IsGood(results[0].StatusCode)) {
                        if (results[0].OutputArguments?.Count == 1) {
                            var stringResult = results[0].OutputArguments[0].ToString();
                            var jsonStartIndex =
                                stringResult.IndexOf("[", StringComparison.Ordinal);
                            var jsonEndIndex =
                                stringResult.IndexOf("]", StringComparison.Ordinal);
                            if (jsonStartIndex >= 0 && jsonEndIndex >= 0) {
                                var json = stringResult.Substring(jsonStartIndex,
                                    jsonEndIndex - jsonStartIndex + 1);
                                var nodelist =
                                    JsonConvert.DeserializeObject<PublishedNodesCollection>(json);
                                return Task.FromResult(nodelist.Select(s => s.NodeID.ToString()));
                            }
                        }
                        else {
                            _logger.Error($"Result of GetPublishedNodes had unexpected " +
                                $"length {results[0].OutputArguments?.Count}",
                                () => results[0].OutputArguments);
                        }
                    }
                    else {
                        _logger.Error($"Bad GetPublishedNodes call result",
                            () => results[0]);
                    }
                }
                else {
                    _logger.Error($"Bad GetPublishedNodes call result ",
                        () => diagnosticInfos);
                }
                return Task.FromResult(Enumerable.Empty<string>());
            });
        }

        /// <summary>
        /// Requests from a edge publisher to publish nodes on the specified station.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishResultModel> NodePublishAsync(ServerEndpointModel endpoint,
            PublishRequestModel request) {
            var publisher = await FindPublisherForServerAsync(endpoint);
            return await _client.ExecuteServiceAsync(publisher, session => {
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = new NodeId("Methods", 2),
                        MethodId = request.Enabled ?
                            new NodeId("PublishNode", 2) :
                            new NodeId("UnpublishNode", 2),
                        InputArguments = new VariantCollection {
                            new Variant(new NodeId(request.NodeId)),
                            new Variant(endpoint.Url)
                        }
                    }
                };
                var responseHeader = session.Call(null, requests,
                    out var results, out var diagnosticInfos);
                var result = new PublishResultModel();
                if (results.Count > 0 && results[0] != null) {
                    if (StatusCode.IsBad(results[0].StatusCode)) {
                        _logger.Error($"Bad publish=>{request.Enabled} call result",
                            () => results[0]);
                    }
                }
                else {
                    _logger.Error($"Bad publish=>{request.Enabled} call result ",
                        () => diagnosticInfos);
                }
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Simple translate from a server endpoint to a publisher endpoint.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        protected virtual Task<ServerEndpointModel> FindPublisherForServerAsync(
            ServerEndpointModel server) {

            // TODO: Discover publisher for endpoint and cache here...

            var hostName = Utils.GetHostName();
            var stationUri = new Uri(server.Url);
            if (stationUri.Host != hostName) {
                var dnsLabels = stationUri.DnsSafeHost.Contains(".") ?
                    stationUri.DnsSafeHost.Substring(
                        stationUri.DnsSafeHost.IndexOf(".", StringComparison.Ordinal)) : "";
                hostName = $"publisher{dnsLabels}";
            }

            var publisherUri = new Uri(
                $"{stationUri.Scheme}://{hostName}:62222/UA/Publisher");
            return Task.FromResult(new ServerEndpointModel {
                Url = publisherUri.ToString(),
                IsTrusted = true // Trust implicitly
            });
        }

        internal class NodeLookup {
            public Uri EndPointURL { get; set; }
            public NodeId NodeID { get; set; }
        }
        internal class PublishedNodesCollection : List<NodeLookup> {
        }

        /// <summary>
        /// Retrieve the host name or ip address from the session
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private void GetAddressesFromChannel(ITransportChannel channel, int port,
            out string id, out string hostname, out string gateway) {
            // Access underlying proxy socket
            if (channel is IMessageSocketChannel proxyChannel) { 
                var socket = proxyChannel.Socket as ProxyMessageSocket;
                if (socket == null) {
                    throw new InvalidProgramException(
                        "Unexpected - although using proxy, underlying socket is not.");
                }
                var proxySocket = socket.ProxySocket;
                if (proxySocket == null) {
                    throw new InvalidProgramException(
                        "Unexpected - current proxy socket is null.");
                }
                _logger.Debug($"Connected.", () => proxySocket.LocalEndPoint);

                var address = proxySocket.LocalEndPoint.AsProxySocketAddress();
                id = $"{address.Host}:{port}";  // TODO: Retrieve from socket directly
                hostname = address.Host;
                gateway = proxySocket.InterfaceEndPoint.AsProxySocketAddress().Host;
            }
            else {
                hostname = Utils.GetHostName();
                id = $"{hostname}:{port}";
                gateway = null;
            }
        }

        private readonly IOpcUaClient _client;
        private readonly ILogger _logger;
    }
}
