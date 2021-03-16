// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Twin {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.TwinModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class TwinBrowseTestTheory {
        private readonly IIoTMultipleNodesTestContext _context;

        public TwinBrowseTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(1)]
        public void A2_1_RegisterOPCServer_And_ActivateEndpoint() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for microservices of IIoT platform to be healthy and modules to be deployed.
            TestHelper.WaitForServicesAsync(_context, cts.Token).GetAwaiter().GetResult();
            _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token).GetAwaiter().GetResult();

            var simulatedOpcPlcs = TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token).GetAwaiter().GetResult();
            var testPlc = simulatedOpcPlcs.Values.First();

            TestHelper.Registry_RegisterServerAsync(_context, testPlc.EndpointUrl, cts.Token).GetAwaiter().GetResult();

            _context.ConsumedOpcUaNodes[testPlc.EndpointUrl] = _context.GetEntryModelWithoutNodes(testPlc);
            dynamic json = TestHelper.WaitForDiscoveryToBeCompletedAsync(_context, cts.Token, new List<string> { testPlc.EndpointUrl }).GetAwaiter().GetResult();

            int numberOfItems = json.items.Count;
            var found = false;

            for (var indexOfTestPlc = 0; indexOfTestPlc < numberOfItems; indexOfTestPlc++) {

                string endpoint = json.items[indexOfTestPlc].discoveryUrls[0].TrimEnd('/');
                if (TestHelper.IsUrlStringsEqual(endpoint, testPlc.EndpointUrl)) {
                    found = true;
                    break;
                }
            }

            Assert.True(found, "OPC Application not activated");

            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                WaitForEndpointDiscoveryAndStoreInContext();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId), "The endpoint was not set");
            }

            _context.DiscoveryUrl = testPlc.EndpointUrl;

            TestHelper.Registry_ActivateEndpointAsync(_context, _context.OpcUaEndpointId, cts.Token).GetAwaiter().GetResult();
        }

        [Fact, PriorityOrder(2)]
        public void A2_2_CheckEndpointActivation() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var endpoints = TestHelper.Registry_GetEndpointsAsync(_context, cts.Token).GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var (id, url, activationState, endpointState) = endpoints.SingleOrDefault(e => string.Equals(_context.OpcUaEndpointId, e.Id));

            Assert.False(id == null, "The endpoint was not found");
            Assert.Equal(TestConstants.StateConstants.ActivatedAndConnected, activationState);
            Assert.Equal(TestConstants.StateConstants.Ready, endpointState);
        }

        [Fact, PriorityOrder(3)]
        public void T1_0_Browse_Address_Space() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin_GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.Contains(nodes, n => string.Equals("i=85", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=86", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=87", n.NodeId));

            Assert.Equal(3, nodes.Count);
        }

        [Fact, PriorityOrder(4)]
        public void T1_1_BrowseSpecificNode() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin_GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            const string nodeId = "i=85";

            Assert.Contains(nodes, n => string.Equals(nodeId, n.NodeId));

            nodes = TestHelper.Twin_GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, nodeId).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.Contains(nodes, n => string.Equals("i=2253", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/Boiler#i=15070", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/#s=OpcPlc", n.NodeId));
        }

        [Fact, PriorityOrder(5)]
        public void T1_2_BrowseAllNodesOfTypeObject() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin_GetBrowseEndpoint_RecursiveAsync(_context, _context.OpcUaEndpointId, "Object", null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 150);

            Assert.Contains(nodes, n => string.Equals("i=85", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=2253", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/Boiler#i=15070", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/#s=OpcPlc", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=86", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=87", n.NodeId));
        }

        [Fact, PriorityOrder(6)]
        public void T1_3_BrowseAllNodesOfTypeVariable() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin_GetBrowseEndpoint_RecursiveAsync(_context, _context.OpcUaEndpointId, "Variable", null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 2000);

            Assert.Contains(nodes, n => string.Equals("i=2254", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=11312", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/#s=SlowUInt1", n.NodeId));
        }

        [Fact, PriorityOrder(99)]
        public void Z_Cleanup() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                _context.OutputHelper.WriteLine("Server endpoint is not registered, nothing to unregister");
                return;
            }

            TestHelper.Registry_UnregisterServerAsync(_context, _context.DiscoveryUrl, cts.Token).GetAwaiter().GetResult();
            _context.OutputHelper.WriteLine("Server endpoint unregistered");
        }

        private void WaitForEndpointDiscoveryAndStoreInContext() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var testPlc = _context.ConsumedOpcUaNodes.First().Value;
            var json = TestHelper.WaitForEndpointDiscoveryToBeCompleted(_context, cts.Token, new List<string> { testPlc.EndpointUrl }).GetAwaiter().GetResult();

            int numberOfItems = json.items.Count;
            _context.OpcUaEndpointId = null;

            for (var indexOfOpcUaEndpoint = 0; indexOfOpcUaEndpoint < numberOfItems; indexOfOpcUaEndpoint++) {

                var endpoint = ((string)json.items[indexOfOpcUaEndpoint].registration.endpointUrl).TrimEnd('/');
                if (endpoint == testPlc.EndpointUrl) {
                    var id = (string)json.items[indexOfOpcUaEndpoint].registration.id;

                    //store id of endpoint for further interaction
                    _context.OpcUaEndpointId = id;
                    break;
                }
            }

            Assert.True(_context.OpcUaEndpointId != null, "OPC UA Endpoint not found");
        }
    }
}
