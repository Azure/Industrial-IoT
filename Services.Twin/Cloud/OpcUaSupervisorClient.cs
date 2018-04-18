// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Implements node and publish services through edge command control against
    /// the OPC UA edge device module receiving service requests via device method
    /// call.
    /// </summary>
    public sealed class OpcUaSupervisorClient : IOpcUaAdhocBrowseServices, IOpcUaAdhocNodeServices {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="twin"></param>
        public OpcUaSupervisorClient(IIoTHubTwinServices twin, ILogger logger) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Browse a tree node, returns node properties and all child nodes if not excluded.
        /// </summary>
        /// <param name="endpoint">Endpoint url of the server to talk to</param>
        /// <param name="request">browse node and filters</param>
        /// <returns></returns>
        public async Task<BrowseResultModel> NodeBrowseAsync(EndpointModel endpoint,
            BrowseRequestModel request) {
            return await CallServiceOnSupervisor<BrowseRequestModel, BrowseResultModel>(
                "Browse_V1", endpoint, request);
        }

        /// <summary>
        /// Read a variable value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request">Read nodes</param>
        /// <returns></returns>
        public async Task<ValueReadResultModel> NodeValueReadAsync(EndpointModel endpoint,
            ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return await CallServiceOnSupervisor<ValueReadRequestModel, ValueReadResultModel>(
                "ValueRead_V1", endpoint, request);
        }

        /// <summary>
        /// Write variable value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(EndpointModel endpoint,
            ValueWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Node == null) {
                throw new ArgumentNullException(nameof(request.Node));
            }
            if (request.Value == null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            if (string.IsNullOrEmpty(request.Node.Id)) {
                throw new ArgumentException(nameof(request.Node.Id));
            }
            if (string.IsNullOrEmpty(request.Node.DataType)) {
                throw new ArgumentException(nameof(request.Node.DataType));
            }
            return await CallServiceOnSupervisor<ValueWriteRequestModel, ValueWriteResultModel>(
                "ValueWrite_V1", endpoint, request);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            EndpointModel endpoint, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            return await CallServiceOnSupervisor<MethodMetadataRequestModel, MethodMetadataResultModel>(
                "MethodMetadata_V1", endpoint, request);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            EndpointModel endpoint, MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            return await CallServiceOnSupervisor<MethodCallRequestModel, MethodCallResultModel>(
                "MethodCall_V1", endpoint, request);
        }

        /// <summary>
        /// helper to invoke service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="service"></param>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<R> CallServiceOnSupervisor<T, R>(string service,
            EndpointModel endpoint, T request) {
            if (string.IsNullOrEmpty(endpoint.SupervisorId)) {
                throw new ArgumentNullException(nameof(endpoint.SupervisorId));
            }
            var sw = Stopwatch.StartNew();
            var result = await _twin.CallMethodAsync(endpoint.SupervisorId,
                new MethodParameterModel {
                    Name = service,
                    JsonPayload = JsonConvertEx.SerializeObject(new {
                        endpoint,
                        request
                    })
                });
            _logger.Debug($"Supervisor call '{service}' took {sw.ElapsedMilliseconds} ms)!",
                () => { });
            if (result.Status != 200) {
                throw new MethodCallStatusException(result.Status, result.JsonPayload);
            }
            return JsonConvertEx.DeserializeObject<R>(result.JsonPayload);
        }

        private readonly IIoTHubTwinServices _twin;
        private readonly ILogger _logger;
    }
}
