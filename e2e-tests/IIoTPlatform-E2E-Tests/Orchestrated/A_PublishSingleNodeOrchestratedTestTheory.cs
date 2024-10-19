// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Orchestrated
{
    using IIoTPlatformE2ETests.TestEventProcessor;
    using Newtonsoft.Json;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue)]
    public sealed class APublishSingleNodeOrchestratedTestTheory
    {
        private readonly IIoTMultipleNodesTestContext _context;

        public APublishSingleNodeOrchestratedTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.SetOutputHelper(output);
        }

        [Fact, PriorityOrder(0)]
        public async Task TestSetUnmanagedTagFalse()
        {
            _context.Reset();
            await TestHelper.SwitchToOrchestratedModeAsync(_context);
        }

        [Fact, PriorityOrder(1)]
        public async Task TestCollectOAuthToken()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var token = await TestHelper.GetTokenAsync(_context, cts.Token);
        }

        [Fact, PriorityOrder(2)]
        public async Task TestReadSimulatedOpcUaNodes()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);
            Assert.NotNull(simulatedOpcServer);
            Assert.NotEmpty(simulatedOpcServer.Keys);
            Assert.NotEmpty(simulatedOpcServer.Values);
        }

        [Fact, PriorityOrder(3)]
        public async Task TestRegisterOPCServerExpectSuccess()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for microservices of IIoT platform to be healthy and modules to be deployed.
            await TestHelper.WaitForServicesAsync(_context, cts.Token);
            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token);

            var endpointUrl = TestHelper.GetSimulatedOpcServerUrls(_context).First();
            _context.OpcServerUrl = endpointUrl;

            var body = new
            {
                discoveryUrl = endpointUrl
            };

            const string route = TestConstants.APIRoutes.RegistryApplications;
            var response = await TestHelper.CallRestApi(_context, Method.Post, route, body, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} registering {endpointUrl} discovery url");
        }

        [Fact, PriorityOrder(4)]
        public async Task TestGetApplicationsFromRegistryExpectOneRegisteredApplication()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            dynamic json = await TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(
                _context, new HashSet<string> { _context.OpcServerUrl }, cts.Token);
            Assert.True(json != null, $"OPC Application with url {_context.OpcServerUrl} not found");
        }

        [Fact, PriorityOrder(5)]
        public async Task TestGetEndpointsExpectOneWithMultipleAuthentication()
        {
            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcServerUrl))
            {
                await TestRegisterOPCServerExpectSuccess();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcServerUrl));
            }

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var json = await TestHelper.Discovery.WaitForEndpointDiscoveryToBeCompletedAsync(
                _context, new HashSet<string> { _context.OpcServerUrl }, "SignAndEncrypt",
                "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", cts.Token);
            Assert.NotNull(json);

            var opcServerEndpoints = ((IEnumerable<dynamic>)json.items)
                .Where(item => item.registration.endpoint.url.Trim('/') == _context.OpcServerUrl.Trim('/')
                    && item.registration.endpoint.securityMode == "SignAndEncrypt"
                    && item.registration.endpoint.securityPolicy == "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256");

            Assert.Single(opcServerEndpoints);

            var result = opcServerEndpoints.FirstOrDefault().registration;
            var endpointId = (string)result.id;
            Assert.NotEmpty(endpointId);

            // Authentication Checks
            Assert.Equal("None", result.authenticationMethods[0].credentialType);
            Assert.Equal("UserName", result.authenticationMethods[1].credentialType);
            Assert.Equal("X509Certificate", result.authenticationMethods[2].credentialType);

            // Store id of endpoint for further interaction
            _context.OpcUaEndpointId = endpointId;
        }

        [Fact, PriorityOrder(6)]
        public async Task TestGetEndpointExpectSuccess()
        {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }
        }

        [Fact, PriorityOrder(8)]
        public async Task TestPublishNodeWithDefaultsExpectDataAvailableAtIoTHub()
        {
            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);

            var route = string.Format(CultureInfo.InvariantCulture, TestConstants.APIRoutes.PublisherStartFormat, _context.OpcUaEndpointId);
            var body = new
            {
                item = new
                {
                    nodeId = simulatedOpcServer.Values.First().OpcNodes
                    .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                    .Skip(2).First().Id,
                    samplingInterval = "00:00:00.250",
                    publishingInterval = "00:00:00.500",
                }
            };

            var response = await TestHelper.CallRestApi(_context, Method.Post, route, body, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} starting publishing");
            Assert.Equal("{}", response.Content);
        }

        [Fact, PriorityOrder(9)]
        public async Task TestGetListOfJobsExpectOneJobWithPublishingOneNode()
        {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);

            var route = string.Format(CultureInfo.InvariantCulture, TestConstants.APIRoutes.PublisherListFormat, _context.OpcUaEndpointId);
            var response = await TestHelper.CallRestApi(_context, Method.Post, route, new { }, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} Getting publishing jobs");
            dynamic json = JsonConvert.DeserializeObject(response.Content);
            Assert.Equal(1, json.items.Count);

            var publishedNode = json.items[0];
            Assert.NotNull(publishedNode);
            Assert.NotNull(publishedNode.nodeId);
            Assert.NotEmpty((string)publishedNode.nodeId);
            var publishedNodeId = (string)publishedNode.nodeId;
            Assert.Equal(simulatedOpcServer.Values.First().OpcNodes
                .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                .Skip(2).First().Id, publishedNodeId);
        }

        [Fact, PriorityOrder(10)]
        public async Task TestVerifyDataAvailableAtIoTHub()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            using var validator = TelemetryValidator.Start(_context, 0, 0, 0);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.AwaitDataInMilliseconds * 4, cts.Token);
            var result = await validator.StopAsync();
            Assert.True(result.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True(result.DroppedValueCount == 0, "Dropped messages detected");
            Assert.True(result.DuplicateValueCount == 0, "Duplicate values detected");
            Assert.True(result.DroppedSequenceCount == 0,
                $"Dropped Sequence detected: {result.DroppedSequenceCount}");
            Assert.Equal(0U, result.DuplicateSequenceCount);
            Assert.Equal(0U, result.ResetSequenceCount);
        }

        [Fact, PriorityOrder(11)]
        public async Task RemoveJobExpectSuccess()
        {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);
            var route = string.Format(CultureInfo.InvariantCulture, TestConstants.APIRoutes.PublisherStopFormat, _context.OpcUaEndpointId);
            var body = new
            {
                nodeId = simulatedOpcServer.Values.First().OpcNodes
                    .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                    .Skip(2).First().Id
            };

            var response = await TestHelper.CallRestApi(_context, Method.Post, route, body, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} deleting publishing job");
            Assert.Equal("{}", response.Content);

            // Wait untill the publishing has stopped
            await Task.Delay(TestConstants.AwaitNoDataInMilliseconds, cts.Token);
        }

        [Fact, PriorityOrder(12)]
        public async Task TestGetListOfJobsExpectNone()
        {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var route = string.Format(CultureInfo.InvariantCulture, TestConstants.APIRoutes.PublisherListFormat, _context.OpcUaEndpointId);
            var response = await TestHelper.CallRestApi(_context, Method.Post, route, new { }, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} Getting publishing jobs");
            dynamic json = JsonConvert.DeserializeObject(response.Content);
            Assert.Equal(0, json.items.Count);
        }

        [Fact, PriorityOrder(13)]
        public async Task TestVerifyNoDataIncomingAtIoTHub()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            using var validator = TelemetryValidator.Start(_context, 0, 0, 0);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.AwaitNoDataInMilliseconds, cts.Token);
            var result = await validator.StopAsync();
            Assert.True(result.TotalValueChangesCount == 0, $"{result.TotalValueChangesCount} Messages received at IoT Hub");
        }
    }
}
