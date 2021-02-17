// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Orchestrated {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using Newtonsoft.Json;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer("IIoTPlatform_E2E_Tests.TestExtensions.TestOrderer", TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue)]
    public class T_TwinBrowseOrchestratedTestTheory {
        private readonly IIoTMultipleNodesTestContext _context;

        public T_TwinBrowseOrchestratedTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output) {
            _context = context;
            _context.OutputHelper = output;
        }

        [Fact, PriorityOrder(0)]
        public void SwitchToOrchestratedMode() {
            TestHelper.SwitchToOrchestratedModeAsync(_context).GetAwaiter().GetResult();
        }

        [Fact, PriorityOrder(1)]
        public void Test_CollectOAuthToken() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var token = TestHelper.GetTokenAsync(_context, cts.Token).GetAwaiter().GetResult();
            Assert.NotEmpty(token);
        }

        [Fact, PriorityOrder(2)]
        public void D1_Discover_OPC_UA_Endpoints() {
            var endpoints = ListEndpointsAsync().GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);
        }

        [Fact, PriorityOrder(3)]
        public void T1_Browse_Address_Space() {
            var endpoints = ListEndpointsAsync().GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);

            var nodes = GetNodesAsync(endpoint.Id).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);
        }

        [Fact, PriorityOrder(4)]
        public void T1_1_BrowseSpecificNode() {
            var endpoints = ListEndpointsAsync().GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);

            var nodes = GetNodesAsync(endpoint.Id).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            var nodeId = nodes.First().NodeId;

            nodes = GetNodesAsync(endpoint.Id, nodeId).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);
        }

        [Fact, PriorityOrder(5)]
        public void T1_2_BrowseAllNodesOfTypeObject() {
            var endpoints = ListEndpointsAsync().GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);

            var nodes = GetAllNodesAsync(endpoint.Id, "Object").GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 150);
        }

        [Fact, PriorityOrder(6)]
        public void T1_3_BrowseAllNodesOfTypeVariable() {
            var endpoints = ListEndpointsAsync().GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);

            var nodes = GetAllNodesAsync(endpoint.Id, "Variable").GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 150);
        }

        /// <summary>
        /// Equivalent to GetSetOfUniqueNodesAsync.
        /// </summary>
        private async Task<List<(string NodeId, string NodeClass, bool Children)>> GetNodesAsync(string endpointId, string nodeId = null, string continuationToken = null) {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token).ConfigureAwait(false);
            var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

            if (continuationToken == null) {
                request.Resource = $"twin/v2/browse/{endpointId}";

                if (!string.IsNullOrEmpty(nodeId)) {
                    request.AddQueryParameter("nodeId", nodeId);
                }
            }
            else {
                request.Resource = $"twin/v2/browse/{endpointId}/next";
                request.AddQueryParameter("continuationToken", continuationToken);
            }

            var response = await client.ExecuteAsync(request, cts.Token).ConfigureAwait(false);
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            dynamic json = JsonConvert.DeserializeObject(response.Content);

            var result = new List<(string NodeId, string NodeClass, bool Children)>();

            foreach (var node in json.references) {
                result.Add(
                    (
                        node.target?.nodeId?.ToString(),
                        node.target?.nodeClass?.ToString(),
                        string.Equals(node.target?.children?.ToString(), "true", StringComparison.OrdinalIgnoreCase)));
            }

            var responseContinuationToken = json.continuationToken;

            if (responseContinuationToken != null) {
                result.AddRange(await GetNodesAsync(endpointId, null, responseContinuationToken).ConfigureAwait(false));
            }

            return result;
        }

        private async Task<List<(string NodeId, string NodeClass, bool Children)>> GetAllNodesAsync(string endpointId, string nodeClass = null, string nodeId = null) {
            var result = new List<(string NodeId, string NodeClass, bool Children)>();
            var nodes = await GetNodesAsync(endpointId, nodeId).ConfigureAwait(false);

            foreach (var node in nodes) {
                if (string.IsNullOrEmpty(nodeClass)
                    || string.Equals(nodeClass, node.NodeClass, StringComparison.OrdinalIgnoreCase)) {
                    result.Add(node);
                }
                if (node.Children) {
                    var childNodes = await GetAllNodesAsync(endpointId, nodeClass, node.NodeId).ConfigureAwait(false);
                    if (childNodes.Any())
                        _context.OutputHelper.WriteLine($"{node.NodeId}\t{node.NodeClass}\t{childNodes.Count}");
                    result.AddRange(childNodes);
                }
            }

            return result;
        }

        private struct NodeInfo {
            public NodeInfo(string nodeClass, string nodeId, bool children = false) : this() {
                NodeClass = nodeClass;
                NodeId = nodeId;
                Children = children;
            }

            public string NodeClass { get; }
            public string NodeId { get; }
            public bool Children { get; }
        }

        private async Task<List<(string Id, string Url, string ActivationState, string EndpointState)>> ListEndpointsAsync() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var accessToken = await TestHelper.GetTokenAsync(_context, cts.Token).ConfigureAwait(false);
            var client = new RestClient(_context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = "registry/v2/endpoints";

            var response = await client.ExecuteAsync(request, cts.Token).ConfigureAwait(false);
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            dynamic json = JsonConvert.DeserializeObject(response.Content);
            int count = json.items.Count;

            Assert.NotEqual(0, count);

            var result = new List<(string Id, string Url, string ActivationState, string EndpointState)>();

            foreach (var item in json.items) {
                var id = item.registration.id?.ToString();
                var endpointUrl = item.registration.endpointUrl?.ToString();
                var activationState = item.activationState?.ToString();
                var endpointState = item.endpointState?.ToString();
                result.Add((id, endpointUrl, activationState, endpointState));
            }

            return result;
        }
    }
}
