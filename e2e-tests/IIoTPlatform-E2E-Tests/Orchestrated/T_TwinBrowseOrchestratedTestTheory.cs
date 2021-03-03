// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Orchestrated {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue)]
    public class T_TwinBrowseOrchestratedTestTheory {
        private readonly IIoTMultipleNodesTestContext _context;

        public T_TwinBrowseOrchestratedTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(100)]
        public void A1_SwitchToOrchestratedMode() {
            TestHelper.SwitchToOrchestratedModeAsync(_context).GetAwaiter().GetResult();
        }

        [Fact, PriorityOrder(101)]
        public void A2_RegisterOPCServer_And_ActivateEndpoint() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for microservices of IIoT platform to be healthy and modules to be deployed.
            TestHelper.WaitForServicesAsync(_context, cts.Token).GetAwaiter().GetResult();
            _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token).GetAwaiter().GetResult();

            var simulatedOpcServer = TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token).GetAwaiter().GetResult();
            var testPlc = simulatedOpcServer.Values.First();

            TestHelper.Registry_RegisterServerAsync(_context, testPlc.EndpointUrl, cts.Token).GetAwaiter().GetResult();

            _context.ConsumedOpcUaNodes[testPlc.EndpointUrl] = _context.GetEntryModelWithoutNodes(testPlc);
            dynamic json = TestHelper.WaitForDiscoveryToBeCompletedAsync(_context, cts.Token, new List<string> { testPlc.EndpointUrl }).GetAwaiter().GetResult();

            var numberOfItems = (int)json.items.Count;
            bool found = false;

            for (int indexOfTestPlc = 0; indexOfTestPlc < numberOfItems; indexOfTestPlc++) {

                var endpoint = ((string)json.items[indexOfTestPlc].discoveryUrls[0]).TrimEnd('/');
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

            TestHelper.Registry_ActivateEndpointAsync(_context, cts.Token).GetAwaiter().GetResult();
        }

        [Fact, PriorityOrder(102)]
        public void D1_Discover_OPC_UA_Endpoints() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var endpoints = TestHelper.Registry_GetEndpointsAsync(_context, cts.Token).GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.SingleOrDefault(e => string.Equals(_context.OpcUaEndpointId, e.Id));

            Assert.NotNull(endpoint.Id);
            Assert.Equal(TestConstants.StateConstants.ActivatedAndConnected, endpoint.ActivationState);
            Assert.Equal(TestConstants.StateConstants.Ready, endpoint.EndpointState);
        }

        [Fact, PriorityOrder(103)]
        public void T1_Browse_Address_Space() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin_GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);
        }

        [Fact, PriorityOrder(104)]
        public void T1_1_BrowseSpecificNode() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin_GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            var nodeId = nodes.First().NodeId;

            nodes = TestHelper.Twin_GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, nodeId).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);
        }

        [Fact, PriorityOrder(105)]
        public void T1_2_BrowseAllNodesOfTypeObject() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin_GetBrowseEndpoint_RecursiveAsync(_context, _context.OpcUaEndpointId, "Object", null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 150);
        }

        [Fact, PriorityOrder(106)]
        public void T1_3_BrowseAllNodesOfTypeVariable() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin_GetBrowseEndpoint_RecursiveAsync(_context, _context.OpcUaEndpointId, "Variable", null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 150);
        }

        [Fact, PriorityOrder(199)]
        public void Z_Cleanup() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                _context.OutputHelper.WriteLine("Server endpoint is not registered, nothing to unregister");
                return;
            }

            var applicationId = TestHelper.Registry_GetApplicationIdAsync(_context, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(applicationId);

            TestHelper.Registry_UnregisterServerAsync(_context, applicationId, cts.Token).GetAwaiter().GetResult();
            _context.OutputHelper.WriteLine("Server endpoint unregistered");
        }

        private void WaitForEndpointDiscoveryAndStoreInContext() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var testPlc = _context.ConsumedOpcUaNodes.First().Value;
            var json = TestHelper.WaitForEndpointDiscoveryToBeCompleted(_context, cts.Token, new List<string> { testPlc.EndpointUrl }).GetAwaiter().GetResult();

            var numberOfItems = (int)json.items.Count;
            _context.OpcUaEndpointId = null;

            for (int indexOfOpcUaEndpoint = 0; indexOfOpcUaEndpoint < numberOfItems; indexOfOpcUaEndpoint++) {

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
