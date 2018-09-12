// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Access the publisher module via its OPC UA server and configures it
    /// to publish to its device identity endpoint. (V1 functionality)
    /// </summary>
    public class PublisherModuleServerClient : IPublishServices<EndpointModel> {

        /// <summary>
        /// Create client service to control publisher
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public PublisherModuleServerClient(IEndpointServices client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Returns the list of published nodes of the OPC UA server with the given endpointUrl
        /// </summary>
        public async Task<PublishedNodeListModel> ListPublishedNodesAsync(
            EndpointModel endpoint, string continuation) {
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
                                    JsonConvertEx.DeserializeObject<PublishedNodesCollection>(json);
                                return Task.FromResult(new PublishedNodeListModel {
                                    Items = nodelist.Select(s => new PublishedNodeModel {
                                        NodeId = s.NodeID.AsString(session.MessageContext),
                                        Enabled = true
                                    }).ToList()
                                });
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
                return Task.FromResult(new PublishedNodeListModel());
            });
        }

        /// <summary>
        /// Requests from publisher module to publish nodes on the specified station.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishResultModel> NodePublishAsync(EndpointModel endpoint,
            PublishRequestModel request) {
            var publisher = await FindPublisherForServerAsync(endpoint);
            return await _client.ExecuteServiceAsync(publisher, session => {
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = new NodeId("Methods", 2),
                        MethodId = (request.Enabled ?? false) ?
                            new NodeId("PublishNode", 2) :
                            new NodeId("UnpublishNode", 2),
                        InputArguments = new VariantCollection {
                            new Variant(request.NodeId.ToNodeId(session.MessageContext)),
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
        protected virtual Task<EndpointModel> FindPublisherForServerAsync(
            EndpointModel server) {

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
            return Task.FromResult(new EndpointModel {
                Url = publisherUri.ToString()
                // TODO
            });
        }

        internal class NodeLookup {
            public Uri EndPointURL { get; set; }
            public NodeId NodeID { get; set; }
        }
        internal class PublishedNodesCollection : List<NodeLookup> {
        }

        private readonly IEndpointServices _client;
        private readonly ILogger _logger;
    }
}
