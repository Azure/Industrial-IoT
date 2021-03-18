// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Twin {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using Microsoft.CSharp.RuntimeBinder;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.TwinModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class TwinCallTestTheory {
        private readonly IIoTMultipleNodesTestContext _context;

        public TwinCallTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output) {
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
        public void Twin_Call_GetMethodMetadata() {
            var methodId = "i=13358"; // CreateFile
            var methodMetadata = TestHelper.Twin_GetMethodMetadataAsync(_context, _context.OpcUaEndpointId, methodId).GetAwaiter().GetResult();

            Assert.Equal(methodMetadata.objectId, "i=13354");
            Assert.Equal(methodMetadata.inputArguments.Count, 2);
            Assert.Equal(methodMetadata.outputArguments.Count, 2);

            Assert.Equal(methodMetadata.inputArguments[0].name, "FileName");
            Assert.Equal(methodMetadata.inputArguments[0].type.displayName, "String");
            Assert.Equal(methodMetadata.inputArguments[1].name, "RequestFileOpen");
            Assert.Equal(methodMetadata.inputArguments[1].type.displayName, "Boolean");

            Assert.Equal(methodMetadata.outputArguments[0].name, "FileNodeId");
            Assert.Equal(methodMetadata.outputArguments[0].type.displayName, "NodeId");
            Assert.Equal(methodMetadata.outputArguments[1].name, "FileHandle");
            Assert.Equal(methodMetadata.outputArguments[1].type.displayName, "UInt32");
        }


        [Fact, PriorityOrder(4)]
        public void Twin_Call_CallMethod() {
            // CreateFile method - not implemented
            var methodId = "i=13358";
            var arguments = new List<object> {
                new {dataType = "String", value = "TestFile"},
                new {dataType = "Boolean", value = "false"}
            };
            var methodMetadata = TestHelper.Twin_GetMethodMetadataAsync(_context, _context.OpcUaEndpointId, methodId).GetAwaiter().GetResult();
            var response = TestHelper.Twin_CallMethodAsync(_context, _context.OpcUaEndpointId, methodId, methodMetadata.objectId, arguments).GetAwaiter().GetResult();
            Assert.Equal("BadNotImplemented", GetErrorMessage(response));

            // ConditionRefresh method - wrong arguments
            methodId = "i=3875";
            arguments = new List<object> {
                new {dataType = "IntegerId", value = "0"}
            };

            methodMetadata = TestHelper.Twin_GetMethodMetadataAsync(_context, _context.OpcUaEndpointId, methodId).GetAwaiter().GetResult();
            response = TestHelper.Twin_CallMethodAsync(_context, _context.OpcUaEndpointId, methodId, methodMetadata.objectId, arguments).GetAwaiter().GetResult();
            Assert.True(GetErrorMessage(response).Contains("Cannot refresh conditions for a subscription that does not exist"));

            // HeaterOn method - no arguments expected
            methodId = "http://microsoft.com/Opc/OpcPlc/Boiler#s=HeaterOn";
            methodMetadata = TestHelper.Twin_GetMethodMetadataAsync(_context, _context.OpcUaEndpointId, methodId).GetAwaiter().GetResult();
            response = TestHelper.Twin_CallMethodAsync(_context, _context.OpcUaEndpointId, methodId, methodMetadata.objectId, new List<object>()).GetAwaiter().GetResult();
            Assert.Null(GetErrorMessage(response));
        }

        private string GetErrorMessage(dynamic content) {
            try {
                return content.errorInfo.errorMessage.ToString();
            }
            catch (RuntimeBinderException) {
                return null;
            }
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
