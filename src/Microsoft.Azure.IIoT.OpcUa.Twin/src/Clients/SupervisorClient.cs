// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Represents the supervisor api surface for browse and node operations.
    /// </summary>
    public sealed class SupervisorClient :
        IBrowseServices<TwinRegistrationModel>, INodeServices<TwinRegistrationModel> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        public SupervisorClient(IIoTHubTwinServices twin, ILogger logger) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Browse a tree node, returns node properties and all child nodes
        /// if not excluded.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="request">browse node and filters</param>
        /// <returns></returns>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(
            TwinRegistrationModel registration, BrowseRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnSupervisor<BrowseRequestModel, BrowseResultModel>(
                "Browse_V1", registration, request);
        }

        /// <summary>
        /// Browse remainder of nodes from browser request using continuation
        /// token.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            TwinRegistrationModel registration, BrowseNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            return await CallServiceOnSupervisor<BrowseNextRequestModel, BrowseNextResultModel>(
                "BrowseNext_V1", registration, request);
        }

        /// <summary>
        /// Read a variable value
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="request">Read nodes</param>
        /// <returns></returns>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            TwinRegistrationModel registration, ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return await CallServiceOnSupervisor<ValueReadRequestModel, ValueReadResultModel>(
                "ValueRead_V1", registration, request);
        }

        /// <summary>
        /// Write variable value
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            TwinRegistrationModel registration, ValueWriteRequestModel request) {
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
                "ValueWrite_V1", registration, request);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            TwinRegistrationModel registration, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            return await CallServiceOnSupervisor<MethodMetadataRequestModel, MethodMetadataResultModel>(
                "MethodMetadata_V1", registration, request);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            TwinRegistrationModel registration, MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            return await CallServiceOnSupervisor<MethodCallRequestModel, MethodCallResultModel>(
                "MethodCall_V1", registration, request);
        }

        /// <summary>
        /// helper to invoke service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="service"></param>
        /// <param name="registration"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<R> CallServiceOnSupervisor<T, R>(string service,
            TwinRegistrationModel registration, T request) {
            if (registration == null) {
                throw new ArgumentNullException(nameof(registration));
            }
            if (registration.Endpoint == null) {
                throw new ArgumentNullException(nameof(registration.Endpoint));
            }
            if (string.IsNullOrEmpty(registration.SupervisorId)) {
                throw new ArgumentNullException(nameof(registration.SupervisorId));
            }
            var sw = Stopwatch.StartNew();
            var deviceId = SupervisorModelEx.ParseDeviceId(registration.SupervisorId,
                out var moduleId);
            var result = await _twin.CallMethodAsync(deviceId, moduleId,
                new MethodParameterModel {
                    Name = service,
                    JsonPayload = JsonConvertEx.SerializeObject(new {
                        endpoint = registration.Endpoint,
                        request
                    })
                });
            _logger.Debug($"Calling supervisor service '{service}' on {deviceId}/{moduleId} " +
                $"took {sw.ElapsedMilliseconds} ms and returned {result.Status}!",
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
