// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents the supervisor api surface for browse and node operations.
    /// </summary>
    public sealed class SupervisorClient : IBrowseServices<EndpointRegistrationModel>,
        IHistoricAccessServices<EndpointRegistrationModel>, INodeServices<EndpointRegistrationModel>,
        IPublishServices<EndpointRegistrationModel> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public SupervisorClient(IMethodClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            EndpointRegistrationModel registration, PublishStartRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Item == null) {
                throw new ArgumentNullException(nameof(request.Item));
            }
            var result = await CallServiceOnSupervisor<PublishStartRequestModel, PublishStartResultModel>(
                "PublishStart_V1", registration, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(
            EndpointRegistrationModel registration, PublishStopRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await CallServiceOnSupervisor<PublishStopRequestModel, PublishStopResultModel>(
                "PublishStop_V1", registration, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResultModel> NodePublishListAsync(
            EndpointRegistrationModel registration, PublishedItemListRequestModel request) {
            var result = await CallServiceOnSupervisor<PublishedItemListRequestModel, PublishedItemListResultModel>(
                "PublishList_V1", registration, request);
            return result;
        }

        /// <inheritdoc/>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(
            EndpointRegistrationModel registration, BrowseRequestModel request) {
            return await CallServiceOnSupervisor<BrowseRequestModel, BrowseResultModel>(
                "Browse_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(
            EndpointRegistrationModel registration, BrowseNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            return await CallServiceOnSupervisor<BrowseNextRequestModel, BrowseNextResultModel>(
                "BrowseNext_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResultModel> NodeBrowsePathAsync(
            EndpointRegistrationModel registration, BrowsePathRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.PathElements == null || request.PathElements.Length == 0) {
                throw new ArgumentNullException(nameof(request.PathElements));
            }
            return await CallServiceOnSupervisor<BrowsePathRequestModel, BrowsePathResultModel>(
                "BrowsePath_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResultModel> NodeValueReadAsync(
            EndpointRegistrationModel registration, ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnSupervisor<ValueReadRequestModel, ValueReadResultModel>(
                "ValueRead_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(
            EndpointRegistrationModel registration, ValueWriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Value == null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            return await CallServiceOnSupervisor<ValueWriteRequestModel, ValueWriteResultModel>(
                "ValueWrite_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            EndpointRegistrationModel registration, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnSupervisor<MethodMetadataRequestModel, MethodMetadataResultModel>(
                "MethodMetadata_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            EndpointRegistrationModel registration, MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnSupervisor<MethodCallRequestModel, MethodCallResultModel>(
                "MethodCall_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<ReadResultModel> NodeReadAsync(
            EndpointRegistrationModel registration, ReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0) {
                throw new ArgumentNullException(nameof(request.Attributes));
            }
            if (request.Attributes.Any(r => string.IsNullOrEmpty(r.NodeId))) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            return await CallServiceOnSupervisor<ReadRequestModel, ReadResultModel>(
                "NodeRead_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<WriteResultModel> NodeWriteAsync(
            EndpointRegistrationModel registration, WriteRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0) {
                throw new ArgumentNullException(nameof(request.Attributes));
            }
            if (request.Attributes.Any(r => string.IsNullOrEmpty(r.NodeId))) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            return await CallServiceOnSupervisor<WriteRequestModel, WriteResultModel>(
                "NodeWrite_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResultModel<JToken>> HistoryReadAsync(
            EndpointRegistrationModel registration, HistoryReadRequestModel<JToken> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await CallServiceOnSupervisor<HistoryReadRequestModel<JToken>, HistoryReadResultModel<JToken>>(
                "HistoryRead_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResultModel<JToken>> HistoryReadNextAsync(
            EndpointRegistrationModel registration, HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            return await CallServiceOnSupervisor<HistoryReadNextRequestModel, HistoryReadNextResultModel<JToken>>(
                "HistoryRead_V1", registration, request);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResultModel> HistoryUpdateAsync(
            EndpointRegistrationModel registration, HistoryUpdateRequestModel<JToken> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            return await CallServiceOnSupervisor<HistoryUpdateRequestModel<JToken>, HistoryUpdateResultModel>(
                "HistoryUpdate_V1", registration, request);
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
            EndpointRegistrationModel registration, T request) {
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
            var result = await _client.CallMethodAsync(deviceId, moduleId, service,
                JsonConvertEx.SerializeObject(new {
                    endpoint = registration.Endpoint,
                    request
                }));
            _logger.Debug("Calling supervisor service '{service}' on {deviceId}/{moduleId} " +
                "took {elapsed} ms and returned {result}!", service, deviceId, moduleId,
                sw.ElapsedMilliseconds, result);
            return JsonConvertEx.DeserializeObject<R>(result);
        }

        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
