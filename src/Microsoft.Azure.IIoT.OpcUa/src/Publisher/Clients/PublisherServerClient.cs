// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Access the publisher module via its built in OPC UA server.
    /// (V1 functionality)
    /// </summary>
    public class PublisherServerClient : IPublishServices<EndpointModel> {

        /// <summary>
        /// Create client service to control publisher
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public PublisherServerClient(IEndpointServices client, ILogger logger) :
            this (client, null, logger) {
        }

        /// <summary>
        /// Create client service to control publisher
        /// </summary>
        /// <param name="client"></param>
        /// <param name="publisherHost"></param>
        /// <param name="logger"></param>
        public PublisherServerClient(IEndpointServices client, string publisherHost,
            ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publisherHost = string.IsNullOrEmpty(publisherHost) ? null : publisherHost;
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            EndpointModel endpoint, PublishStartRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Node == null) {
                throw new ArgumentNullException(nameof(request.Node));
            }
            if (string.IsNullOrEmpty(request.Node.NodeId)) {
                throw new ArgumentNullException(nameof(request.Node.NodeId));
            }
            return await _client.ExecuteServiceAsync(GetPublisherEndpoint(endpoint),
                null, async session => {
                var diagnostics = new List<OperationResult>();
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = new NodeId("Methods", 2),
                        MethodId = new NodeId("PublishNode", 2),
                        InputArguments = new VariantCollection {
                            new Variant(request.Node.NodeId
                                .ToExpandedNodeId(session.MessageContext)),
                            new Variant(endpoint.Url)
                        }
                    }
                };
                var response = await session.CallAsync(
                    request.Diagnostics.ToStackModel(), requests);
                OperationResultEx.Validate("PublishNode", diagnostics,
                    response.Results.Select(r => r.StatusCode), response.DiagnosticInfos);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);
                return new PublishStartResultModel {
                    ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                        session.MessageContext)
                };
            });
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(
            EndpointModel endpoint, PublishStopRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentNullException(nameof(request.NodeId));
            }
            return await _client.ExecuteServiceAsync(GetPublisherEndpoint(endpoint),
                null, async session => {
                    var diagnostics = new List<OperationResult>();
                var requests = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = new NodeId("Methods", 2),
                        MethodId = new NodeId("UnpublishNode", 2),
                        InputArguments = new VariantCollection {
                            new Variant(request.NodeId
                                .ToExpandedNodeId(session.MessageContext)),
                            new Variant(endpoint.Url)
                        }
                    }
                };
                var response = await session.CallAsync(
                    request.Diagnostics.ToStackModel(), requests);
                OperationResultEx.Validate("UnpublishNode", diagnostics,
                    response.Results.Select(r => r.StatusCode), response.DiagnosticInfos);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);
                return new PublishStopResultModel {
                    ErrorInfo = diagnostics.ToServiceModel(request.Diagnostics,
                        session.MessageContext)
                };
            });
        }

        /// <inheritdoc/>
        public async Task<PublishedNodeListResultModel> NodePublishListAsync(
            EndpointModel endpoint, PublishedNodeListRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            DiagnosticsModel config = null;
            return await _client.ExecuteServiceAsync(GetPublisherEndpoint(endpoint),
                null, async session => {
                    var diagnostics = new List<OperationResult>();
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
                var response = await session.CallAsync(config.ToStackModel(), requests);
                OperationResultEx.Validate("GetPublishedNodes", diagnostics,
                    response.Results.Select(r => r.StatusCode), response.DiagnosticInfos);
                SessionClientEx.Validate(response.Results, response.DiagnosticInfos);
                if (!StatusCode.IsGood(response.Results[0].StatusCode)) {
                    return new PublishedNodeListResultModel();
                }
                if (response.Results[0].OutputArguments?.Count == 1) {
                    var output = response.Results[0].OutputArguments[0].ToString();
                    var entries = JsonConvertEx.DeserializeObject<
                        List<PublisherConfigFileEntryModel>>(output);
                    return new PublishedNodeListResultModel {
                        Items = entries?
                            .SelectMany(e => e.OpcNodes?
                                .Select(s => new PublishedNodeModel {
                                    NodeId = ToNodeId(s.Id, session.MessageContext),
                                    PublishingInterval = s.OpcPublishingInterval,
                                    SamplingInterval = s.OpcSamplingInterval
                                }))
                            .ToList()
                    };
                }
                _logger.Error($"Result of GetPublishedNodes had unexpected " +
                    $"length {response.Results[0].OutputArguments?.Count}",
                        () => response.Results[0].OutputArguments);
                return new PublishedNodeListResultModel();
            });
        }

        /// <summary>
        /// Try convert id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string ToNodeId(string id, ServiceMessageContext context) {
            try {
                return ExpandedNodeId.Parse(id).AsString(context);
            }
            catch {
                return id;
            }
        }

        /// <summary>
        /// Simple translate from a server endpoint to a publisher endpoint.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        protected EndpointModel GetPublisherEndpoint(EndpointModel server) {
            var hostName = _publisherHost ?? Utils.GetHostName();
            var serverUri = new Uri(server.Url);
            if (_publisherHost == null && serverUri.Host != hostName) {
                var dnsLabels = serverUri.DnsSafeHost.Contains(".") ?
                    serverUri.DnsSafeHost.Substring(
                        serverUri.DnsSafeHost.IndexOf(".", StringComparison.Ordinal)) : "";
                hostName = $"publisher{dnsLabels}";
            }
            var publisherUri = new Uri(
                $"{serverUri.Scheme}://{hostName}:62222/UA/Publisher");
            return new EndpointModel { Url = publisherUri.ToString() };
        }

        private readonly IEndpointServices _client;
        private readonly ILogger _logger;
        private readonly string _publisherHost;
    }
}
