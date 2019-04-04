// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Provides publishing services using publisher client.
    /// </summary>
    public sealed class PublisherServices : IPublishServices<EndpointModel>,
        IPublisher, IDisposable {

        /// <inheritdoc/>
        public bool IsRunning => _client != null;

        /// <summary>
        /// Create services connecting to publisher server
        /// </summary>
        /// <param name="server"></param>
        /// <param name="opc"></param>
        /// <param name="logger"></param>
        public PublisherServices(IPublisherServer server, IEndpointServices opc,
            ILogger logger) : this(null, server, opc, logger) {
            if (server == null) {
                throw new ArgumentNullException(nameof(server));
            }
            //
            // Give it 10 seconds in which we are expecting StartAsync to be
            // called. If it is not called within 10 seconds start is called 
            // from timer thread. This allows startup to await publisher
            // finding without module processing begins and publish calls
            // initially fail.
            //
            _pnp = new Timer(OnReconnectTimer, null, 10000, Timeout.Infinite);
        }

        /// <summary>
        /// Internal helper
        /// </summary>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <param name="opc"></param>
        /// <param name="logger"></param>
        private PublisherServices(IPublisherClient client, IPublisherServer server,
            IEndpointServices opc, ILogger logger) {
            _opc = opc ?? throw new ArgumentNullException(nameof(opc));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _server = server;
            _client = client;
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_client != null) {
                    _logger.Error("Start called, but already publishing.");
                    return;
                }
                // Start reconnect timer
                _pnp?.Change(5000, Timeout.Infinite);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            await _lock.WaitAsync();
            try {
                _pnp?.Change(Timeout.Infinite, Timeout.Infinite);
                _retries = 0;
                if (_client == null) {
                    _logger.Information("Publish services not started.");
                    return;
                }
                if (_client is IDisposable dispose) {
                    _logger.Debug("Stop publishing - disconnect from publisher.");
                    dispose.Dispose();
                }
                _client = null;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            StopAsync().Wait();
            _pnp?.Dispose();
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            EndpointModel endpoint, PublishStartRequestModel request) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
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

            var error = await TestPublishNodeAsync(endpoint, request);
            if (error != null) {
                return new PublishStartResultModel {
                    ErrorInfo = error
                };
            }

            GetUserNamePassword(endpoint.User, out var user, out var password);
            var content = new PublishNodesRequestModel {
                EndpointUrl = endpoint.Url,
                Password = password,
                UserName = user,
                UseSecurity = endpoint.SecurityMode != SecurityMode.None,
                OpcNodes = new List<PublisherNodeModel> {
                    new PublisherNodeModel {
                        Id = ToPublisherNodeId(request.Item.NodeId),
                        OpcPublishingInterval =
                            request.Item.PublishingInterval,
                        OpcSamplingInterval =
                            request.Item.SamplingInterval
                    }
                }
            };
            if (_client == null) {
                await StartAsync();
            }
            var (errorInfo, _) = await _client.CallMethodAsync("PublishNodes",
                JsonConvertEx.SerializeObject(content), request.Header?.Diagnostics);
            return new PublishStartResultModel {
                ErrorInfo = errorInfo
            };
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(
            EndpointModel endpoint, PublishStopRequestModel request) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentNullException(nameof(request.NodeId));
            }

            // Check whether publishing
            var publishing = await IsPublishingAsync(endpoint, request.NodeId);
            if (!publishing) {
                return new PublishStopResultModel {
                    ErrorInfo = new ServiceResultModel {
                        ErrorMessage = "Node is not published"
                    }
                };
            }

            var content = new PublishNodesRequestModel {
                EndpointUrl = endpoint.Url,
                OpcNodes = new List<PublisherNodeModel> {
                    new PublisherNodeModel {
                        Id = ToPublisherNodeId(request.NodeId)
                    }
                }
            };
            System.Diagnostics.Debug.Assert(_client != null);
            var (errorInfo, _) = await _client.CallMethodAsync("UnpublishNodes",
                JsonConvertEx.SerializeObject(content), request.Diagnostics);
            return new PublishStopResultModel {
                ErrorInfo = errorInfo
            };
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResultModel> NodePublishListAsync(
            EndpointModel endpoint, PublishedItemListRequestModel request) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            GetUserNamePassword(endpoint.User, out var user, out var password);
            if (_client == null) {
                await StartAsync();
            }
            var input = new GetNodesRequestModel {
                EndpointUrl = endpoint.Url,
                ContinuationToken = request.ContinuationToken == null ? (ulong?)null :
                    BitConverter.ToUInt64(request.ContinuationToken.DecodeAsBase64(), 0)
            };
            var (errorInfo, result) = await _client.CallMethodAsync(
                "GetConfiguredNodesOnEndpoint", JsonConvertEx.SerializeObject(input), null);
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
                        NodeId = FromPublisherNodeId(s.Id),
                        PublishingInterval = s.OpcPublishingInterval,
                        SamplingInterval = s.OpcSamplingInterval
                    }).ToList()
            };
        }

        /// <summary>
        /// Reconnect timer
        /// </summary>
        /// <param name="state"></param>
        private void OnReconnectTimer(object state) {
            try {
                ReconnectAsync().Wait();
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed connecting to publisher - retry...");
                Try.Op(() => _pnp.Change(10000, Timeout.Infinite));
            }
        }

        /// <summary>
        /// Connect to server and schedule retries if connection failed.
        /// </summary>
        /// <returns></returns>
        private async Task ReconnectAsync() {
            await _lock.WaitAsync();
            try {
                if (_client != null && _client != _publishUnsupported) {
                    _logger.Error("Publish services started.");
                    return;
                }
                IPublisherClient client = null;
                try {
                    _pnp?.Change(Timeout.Infinite, Timeout.Infinite);
                    _logger.Debug("Trying to connect to publisher...");
                    client = await _server.ConnectAsync();
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Error during publisher discovery.");
                }
                if (client == null) {
                    _retries++;
                    client = _publishUnsupported;
                    if (_retries > kMaxRetries) {
                        _logger.Information("No publisher found - Publish services not supported.");
                    }
                    else {
                        var delay = Retry.GetExponentialDelay(_retries, 40000, kMaxRetries);
                        _logger.Information("No publisher found - retrying in {delay} ms...", delay);
                        if (_pnp == null) {
                            _pnp = new Timer(OnReconnectTimer);
                        }
                        _pnp.Change(delay, Timeout.Infinite);
                    }
                }
                else {
                    _retries = 0;
                    _logger.Information("Publisher connected!");
                }
                _client = client;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Tests whether publishing was started for node in publisher.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private async Task<bool> IsPublishingAsync(EndpointModel endpoint,
            string nodeId) {
            var listRequest = new PublishedItemListRequestModel();
            while (true) {
                var published = await NodePublishListAsync(endpoint,
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
        /// Check whether valid node and return service result if not.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private Task<ServiceResultModel> TestPublishNodeAsync(EndpointModel endpoint,
            PublishStartRequestModel request) {
            // Test whether value exists and fail if not
            return _opc.ExecuteServiceAsync(endpoint, null,
                TimeSpan.FromSeconds(10), async session => {
                    var readNode = request.Item.NodeId.ToNodeId(session.MessageContext);
                    if (NodeId.IsNull(readNode)) {
                        throw new ArgumentException(nameof(request.Item.NodeId));
                    }
                    var diagnostics = new List<OperationResultModel>();
                    var response = await session.ReadAsync(
                        (request.Header?.Diagnostics).ToStackModel(),
                        0, TimestampsToReturn.Both, new ReadValueIdCollection {
                    new ReadValueId {
                        NodeId = readNode,
                        AttributeId = Attributes.Value
                    }
                    });
                    OperationResultEx.Validate("Publish_" + readNode, diagnostics,
                        response.Results.Select(r => r.StatusCode),
                        response.DiagnosticInfos, false);
                    SessionClientEx.Validate(response.Results, response.DiagnosticInfos);
                    if (response.Results == null || response.Results.Count == 0) {
                        return diagnostics.ToServiceModel(request.Header?.Diagnostics,
                            session.MessageContext);
                    }
                    return null;
                });
        }

        /// <summary>
        /// Convert to publisher compliant node id string
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private string ToPublisherNodeId(string nodeId) {
            try {
                // Publisher node id should be in expanded format with nsu=
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
                // Publisher node id should be in expanded format with nsu=
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

        /// <summary>
        /// Publisher stub
        /// </summary>
        internal sealed class PublishUnsupported : IPublisherClient {
            /// <inheritdoc/>
            public Task<(ServiceResultModel, string)> CallMethodAsync(
                string method, string request, DiagnosticsModel diagnostics) {
                throw new NotSupportedException("No publisher was found");
            }
        }

        private static readonly PublishUnsupported _publishUnsupported =
            new PublishUnsupported();
        private const int kMaxRetries = 6;

        private IPublisherClient _client;
        private Timer _pnp;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private readonly IEndpointServices _opc;
        private readonly IPublisherServer _server;
        private readonly ILogger _logger;
        private int _retries;
    }
}
