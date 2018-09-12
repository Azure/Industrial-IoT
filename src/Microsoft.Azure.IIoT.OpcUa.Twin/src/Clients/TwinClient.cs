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
    using System.Linq;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Implements node and publish services through command control against
    /// the OPC twin module receiving service requests via device method calls.
    /// </summary>
    public sealed class TwinClient : IBrowseServices<string>,
        INodeServices<string>, IPublishServices<string> {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        public TwinClient(IIoTHubTwinServices twin, ILogger logger) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publish node values
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PublishResultModel> NodePublishAsync(string twinId,
            PublishRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentNullException(nameof(request.NodeId));
            }
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            var result = await CallServiceOnTwin<PublishRequestModel, PublishResultModel>(
                "Publish_V1", twinId, request);
            if (request.Enabled == null) {
                await _twin.UpdatePropertyAsync(twinId, request.NodeId, request.Enabled);
            }
            return result;
        }

        /// <summary>
        /// Browse a tree node, returns node properties and all child nodes
        /// if not excluded.
        /// </summary>
        /// <param name="twinId">Id of twin to talk to</param>
        /// <param name="request">browse node and filters</param>
        /// <returns></returns>
        public async Task<BrowseResultModel> NodeBrowseFirstAsync(string twinId,
            BrowseRequestModel request) {
            return await CallServiceOnTwin<BrowseRequestModel, BrowseResultModel>(
                "Browse_V1", twinId, request);
        }

        /// <summary>
        /// Browse remainder of nodes from browser request using continuation
        /// token.
        /// </summary>
        /// <param name="twinId">Id of twin to talk to</param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BrowseNextResultModel> NodeBrowseNextAsync(string twinId,
            BrowseNextRequestModel request) {
            return await CallServiceOnTwin<BrowseNextRequestModel, BrowseNextResultModel>(
                "BrowseNext_V1", twinId, request);
        }

        /// <summary>
        /// Read a variable value
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request">Read nodes</param>
        /// <returns></returns>
        public async Task<ValueReadResultModel> NodeValueReadAsync(string twinId,
            ValueReadRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentException(nameof(request.NodeId));
            }
            return await CallServiceOnTwin<ValueReadRequestModel, ValueReadResultModel>(
                "ValueRead_V1", twinId, request);
        }

        /// <summary>
        /// Write variable value
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ValueWriteResultModel> NodeValueWriteAsync(string twinId,
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
            return await CallServiceOnTwin<ValueWriteRequestModel, ValueWriteResultModel>(
                "ValueWrite_V1", twinId, request);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            string twinId, MethodMetadataRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            return await CallServiceOnTwin<MethodMetadataRequestModel, MethodMetadataResultModel>(
                "MethodMetadata_V1", twinId, request);
        }

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<MethodCallResultModel> NodeMethodCallAsync(
            string twinId, MethodCallRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.MethodId)) {
                throw new ArgumentNullException(nameof(request.MethodId));
            }
            return await CallServiceOnTwin<MethodCallRequestModel, MethodCallResultModel>(
                "MethodCall_V1", twinId, request);
        }

        /// <summary>
        /// Get list of published nodes
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public async Task<PublishedNodeListModel> ListPublishedNodesAsync(
            string twinId, string continuation) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            var twin = await _twin.GetAsync(twinId);
            if (twin?.Properties?.Reported == null) {
                return new PublishedNodeListModel();
            }
            return new PublishedNodeListModel {
                Items = twin.Properties.Reported
                    .Where(kv => bool.TryParse(kv.Value.ToString(), out var tmp))
                    .Select(kv => new PublishedNodeModel {
                        NodeId = kv.Key,
                        Enabled = (bool)kv.Value
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// helper to invoke service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="service"></param>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<R> CallServiceOnTwin<T, R>(string service,
            string twinId, T request) {
            if (string.IsNullOrEmpty(twinId)) {
                throw new ArgumentNullException(nameof(twinId));
            }
            var sw = Stopwatch.StartNew();
            var result = await _twin.CallMethodAsync(twinId,
                new MethodParameterModel {
                    Name = service,
                    JsonPayload = JsonConvertEx.SerializeObject(request)
                });
            _logger.Debug($"Twin call '{service}' took {sw.ElapsedMilliseconds} ms)!",
                () => { });
            if (result.Status != 200 || string.IsNullOrEmpty(result.JsonPayload)) {
                throw new MethodCallStatusException(result.Status, result.JsonPayload);
            }
            return JsonConvertEx.DeserializeObject<R>(result.JsonPayload);
        }

        private readonly IIoTHubTwinServices _twin;
        private readonly ILogger _logger;
    }
}
