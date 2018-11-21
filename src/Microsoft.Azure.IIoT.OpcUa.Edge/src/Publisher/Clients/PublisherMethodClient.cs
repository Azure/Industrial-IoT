// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Opc.Ua;
    using Opc.Ua.Extensions;

    /// <summary>
    /// Access the publisher module via its device method interface.
    /// (V2 functionality)
    /// </summary>
    public class PublisherMethodClient : IPublishServices<EndpointModel> {

        /// <summary>
        /// Create client that presumes the publisher module is named "publisher"
        /// and resides on the same gateway device as the one in the passed in
        /// identity.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="identity"></param>
        /// <param name="logger"></param>
        public PublisherMethodClient(IJsonMethodClient client, IIdentity identity,
            ILogger logger) : this(client, identity.DeviceId, "publisher", logger) {
            if (string.IsNullOrEmpty(identity.ModuleId)) {
                throw new ArgumentException("Identity is not a module identity",
                    nameof(identity));
            }
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="logger"></param>
        public PublisherMethodClient(IJsonMethodClient client, string deviceId,
            string moduleId, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _moduleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            EndpointModel endpoint, PublishStartRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Item == null) {
                throw new ArgumentNullException(nameof(request.Item));
            }
            if (string.IsNullOrEmpty(request.Item.NodeId)) {
                throw new ArgumentNullException(nameof(request.Item.NodeId));
            }
            GetUserNamePassword(endpoint.User, out var user, out var password);
            await _client.CallMethodAsync(_deviceId, _moduleId,
                "PublishNodes", JsonConvertEx.SerializeObject(
                    new PublishNodesRequestModel {
                        EndpointUrl = endpoint.Url,
                        Password = password,
                        UserName = user,
                        UseSecurity = endpoint.SecurityMode != SecurityMode.None,
                        Nodes = new List<PublisherNodeModel> {
                            new PublisherNodeModel {
                                Id = ToPublisherNodeId(request.Item.NodeId),
                                OpcPublishingInterval =
                                    request.Item.PublishingInterval,
                                OpcSamplingInterval =
                                    request.Item.SamplingInterval
                            }
                        }
                    }));
            return new PublishStartResultModel();
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
            GetUserNamePassword(endpoint.User, out var user, out var password);
            await _client.CallMethodAsync(_deviceId, _moduleId,
                "UnpublishNodes", JsonConvertEx.SerializeObject(
                    new PublishNodesRequestModel {
                        EndpointUrl = endpoint.Url,
                        Nodes = new List<PublisherNodeModel> {
                            new PublisherNodeModel {
                                Id = ToPublisherNodeId(request.NodeId)
                            }
                        }
                    }));
            return new PublishStopResultModel();
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResultModel> NodePublishListAsync(
            EndpointModel endpoint, PublishedItemListRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            GetUserNamePassword(endpoint.User, out var user, out var password);
            var result = await _client.CallMethodAsync(_deviceId, _moduleId,
                "GetConfiguredNodesOnEndpoint", JsonConvertEx.SerializeObject(
                    new GetNodesRequestModel {
                        EndpointUrl = endpoint.Url,
                        ContinuationToken = request.ContinuationToken == null ? (ulong?)null :
                            BitConverter.ToUInt64(request.ContinuationToken.DecodeAsBase64(), 0)
                    }));
            var response = JsonConvertEx.DeserializeObject<GetNodesResponseModel>(result);
            return new PublishedItemListResultModel {
                ContinuationToken = response.ContinuationToken == null ? null :
                    BitConverter.GetBytes(response.ContinuationToken.Value).ToBase64String(),
                Items = response.Nodes?
                    .Select(s => new PublishedItemModel {
                        NodeId = FromPublisherNodeId(s.Id),
                        PublishingInterval = s.OpcPublishingInterval,
                        SamplingInterval = s.OpcSamplingInterval
                    }).ToList()
            };
        }

        /// <summary>
        /// Convert to publisher compliant node id string
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private string ToPublisherNodeId(string nodeId) {
            try {
                // Publisher node id should be in expanded format with ns=
                var expanded = nodeId.ToExpandedNodeId(ServiceMessageContext.GlobalContext);
                return expanded.ToString();
            }
            catch {
                return nodeId;
            }
        }

        /// <summary>
        /// Convert to service compliant node id string
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private string FromPublisherNodeId(string nodeId) {
            try {
                // Publisher node id should be in expanded format with ns=
                var expanded = ExpandedNodeId.Parse(nodeId);
                return expanded.AsString(ServiceMessageContext.GlobalContext);
            }
            catch {
                return nodeId;
            }
        }

        /// <summary>
        /// Extract user name and password from default endpoint credentials
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        private void GetUserNamePassword(CredentialModel credential,
            out string user, out string password) {
            if (credential.Type == CredentialType.UserNamePassword &&
                credential.Value is JObject o &&
                o.TryGetValue("user", out var name) &&
                o.TryGetValue("password", out var pw)) {
                user = (string)name;
                password = (string)pw;
            }
            else {
                user = null;
                password = null;
            }
        }

        private readonly IJsonMethodClient _client;
        private readonly string _deviceId;
        private readonly string _moduleId;
        private readonly ILogger _logger;
    }
}
