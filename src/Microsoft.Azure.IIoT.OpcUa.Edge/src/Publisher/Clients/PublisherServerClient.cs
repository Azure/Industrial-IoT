// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Serilog;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Access the publisher module via its built in OPC UA server.
    /// </summary>
    public class PublisherServerClient : IPublisherClient {

        /// <summary>
        /// Create client service to control publisher
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public PublisherServerClient(IEndpointServices client, ILogger logger) :
            this(client, new Uri($"opc.tcp://{Utils.GetHostName()}:62222/UA/Publisher"), 
                logger) {
        }

        /// <summary>
        /// Create client service to control publisher
        /// </summary>
        /// <param name="client"></param>
        /// <param name="publisherUri"></param>
        /// <param name="logger"></param>
        public PublisherServerClient(IEndpointServices client, Uri publisherUri,
            ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (publisherUri == null) {
                throw new ArgumentNullException(nameof(publisherUri));
            }
            _endpoint = new EndpointModel { Url = publisherUri.ToString() };
        }

        // See https://github.com/Azure/iot-edge-opc-publisher
        private const string kMethodCallObject = "Methods";
        private const string kMethodCallMethod = "IoTHubDirectMethod";

        /// <inheritdoc/>
        public async Task<(ServiceResultModel, string)> CallMethodAsync(
            string method, string request, DiagnosticsModel diagnostics) {
            if (string.IsNullOrEmpty(request)) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(method)) {
                throw new ArgumentNullException(nameof(method));
            }
            return await _client.ExecuteServiceAsync(_endpoint,
                null, async session => {
                var results = new List<OperationResultModel>();
                var methodCalls = new CallMethodRequestCollection {
                    new CallMethodRequest {
                        ObjectId = new NodeId(kMethodCallObject, 2),
                        MethodId = new NodeId(kMethodCallMethod, 2),
                        InputArguments = new VariantCollection {
                            new Variant(method),
                            new Variant(request)
                        }
                    }
                };
                var result = await session.CallAsync(diagnostics.ToStackModel(),
                    methodCalls);
                OperationResultEx.Validate("CallMethod", results,
                    result.Results.Select(r => r.StatusCode), result.DiagnosticInfos,
                    methodCalls, false);
                var diagResult = results.ToServiceModel(diagnostics,
                    session.MessageContext);
                if (!StatusCode.IsGood(result.Results[0].StatusCode)) {
                    _logger.Error("Call returned with error {status}",
                        result.Results[0].StatusCode);
                    return (diagResult, null);
                }
                if (result.Results[0].OutputArguments?.Count == 1) {
                    var response = result.Results[0].OutputArguments[0].ToString();
                    return (diagResult, response);
                }
                return (diagResult, null);
            });
        }

        private readonly IEndpointServices _client;
        private readonly ILogger _logger;
        private readonly EndpointModel _endpoint;
    }
}
