// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v1 {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher client
    /// </summary>
    public sealed class PublisherMethodClient : IPublishServices<string> {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="publishers"></param>
        /// <param name="logger"></param>
        public PublisherMethodClient(IJsonMethodClient client, IPublisherEndpointQuery publishers,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _publishers = publishers ?? throw new ArgumentNullException(nameof(publishers));
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            string endpointId, PublishStartRequestModel request) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Item == null) {
                throw new ArgumentNullException(nameof(request.Item));
            }
            if (string.IsNullOrEmpty(request.Item.NodeId)) {
                throw new ArgumentNullException(nameof(request.Item.NodeId));
            }

            var (publisherId, endpoint) = await _publishers.FindPublisherEndpoint(
                endpointId);
            GetUserNamePassword(request.Header?.Elevation, out var user, out var password);
            var content = new PublishNodesRequestModel {
                EndpointUrl = endpoint.Url,
                EndpointId = endpointId,
                UseSecurity = endpoint.SecurityMode != SecurityMode.None,
                SecurityMode = endpoint.SecurityMode == null ||
                    endpoint.SecurityMode == SecurityMode.Best ? null :
                    endpoint.SecurityMode.ToString(),
                SecurityProfileUri = endpoint.SecurityPolicy,
                Password = password,
                UserName = user,
                OpcNodes = new List<PublisherNodeModel> {
                    new PublisherNodeModel {
                        Id = request.Item.NodeId,
                        DisplayName = null,
                        OpcPublishingInterval = (int?)
                            request.Item.PublishingInterval?.TotalMilliseconds,
                        OpcSamplingInterval = (int?)
                            request.Item.SamplingInterval?.TotalMilliseconds
                    }
                }
            };
            var (errorInfo, _) = await CallMethodOnPublisherAsync(publisherId, "PublishNodes",
                JsonConvertEx.SerializeObject(content));
            return new PublishStartResultModel {
                ErrorInfo = errorInfo
            };
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(
            string endpointId, PublishStopRequestModel request) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentNullException(nameof(request.NodeId));
            }

            // Check whether publishing
            var publishing = await IsPublishingAsync(endpointId, request.NodeId);
            if (!publishing) {
                return new PublishStopResultModel {
                    ErrorInfo = new ServiceResultModel {
                        ErrorMessage = "Node is not published"
                    }
                };
            }

            var (publisherId, endpoint) = await _publishers.FindPublisherEndpoint(endpointId);
            var content = new UnpublishNodesRequestModel {
                EndpointUrl = endpoint.Url,
                EndpointId = endpointId,
                OpcNodes = new List<PublisherNodeModel> {
                    new PublisherNodeModel {
                        Id = request.NodeId
                    }
                }
            };
            var (errorInfo, _) = await CallMethodOnPublisherAsync(publisherId,
                "UnpublishNodes", JsonConvertEx.SerializeObject(content));
            return new PublishStopResultModel {
                ErrorInfo = errorInfo
            };
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResultModel> NodePublishListAsync(
            string endpointId, PublishedItemListRequestModel request) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            var (publisherId, endpoint) = await _publishers.FindPublisherEndpoint(endpointId);
            var input = new GetNodesRequestModel {
                EndpointId = endpointId,
                EndpointUrl = endpoint.Url,
                ContinuationToken = request.ContinuationToken == null ? (ulong?)null :
                    BitConverter.ToUInt64(request.ContinuationToken.DecodeAsBase64(), 0)
            };

            var (errorInfo, result) = await CallMethodOnPublisherAsync(publisherId,
                "GetConfiguredNodesOnEndpoint", JsonConvertEx.SerializeObject(input));
            if (result == null) {
                return new PublishedItemListResultModel {
                    ErrorInfo = errorInfo
                };
            }
            var response = JsonConvertEx.DeserializeObject<GetNodesResponseModel>(result);
            return new PublishedItemListResultModel {
                ContinuationToken = response.ContinuationToken == null ? null :
                    BitConverter.GetBytes(response.ContinuationToken.Value)
                        .ToBase64String(),
                Items = response.OpcNodes?
                    .Select(s => new PublishedItemModel {
                        NodeId = s.Id,
                        PublishingInterval = s.OpcPublishingInterval == null ? (TimeSpan?)null :
                            TimeSpan.FromMilliseconds(s.OpcPublishingInterval.Value),
                        SamplingInterval = s.OpcSamplingInterval == null ? (TimeSpan?)null :
                            TimeSpan.FromMilliseconds(s.OpcSamplingInterval.Value),
                    }).ToList()
            };
        }

        /// <summary>
        /// Tests whether publishing was started for node in publisher.
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private async Task<bool> IsPublishingAsync(string endpointId, string nodeId) {
            var listRequest = new PublishedItemListRequestModel();
            while (true) {
                var published = await NodePublishListAsync(endpointId,
                    listRequest);
                if (published.Items.Any(e => e.NodeId == nodeId)) {
                    return true;
                }
                if (string.IsNullOrEmpty(published.ContinuationToken)) {
                    break;
                }
                listRequest.ContinuationToken = published.ContinuationToken;
            }
            return false;
        }

        /// <summary>
        /// Extract user name and password from default endpoint credentials
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        private void GetUserNamePassword(CredentialModel credential,
            out string user, out string password) {
            if (credential?.Type == CredentialType.UserName &&
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

        /// <inheritdoc/>
        public async Task<(ServiceResultModel, string)> CallMethodOnPublisherAsync(
            string publisherId, string method, string request) {
            try {
                _logger.Verbose("Calling method {method} with {request} on {publisherId}",
                    method, request, publisherId);
                var deviceId = PublisherModelEx.ParseDeviceId(publisherId, out var moduleId);
                var response = await _client.CallMethodAsync(deviceId, moduleId,
                    method, request);
                _logger.Verbose("Publisher {publisherId} method call {method} returned {response}",
                    publisherId, method, response);
                return (null, response);
            }
            catch (Exception ex) {
                return (new ServiceResultModel {
                    ErrorMessage = ex.Message,
                    Diagnostics = JToken.FromObject(ex,
                        JsonSerializer.Create(JsonConvertEx.GetSettings()))
                }, null);
            }
        }

        private readonly IJsonMethodClient _client;
        private readonly IPublisherEndpointQuery _publishers;
        private readonly ILogger _logger;
    }
}
