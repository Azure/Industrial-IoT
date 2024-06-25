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
    public class BPublishMultipleNodesOrchestratedTestTheory
    {
        private readonly IIoTMultipleNodesTestContext _context;

        public BPublishMultipleNodesOrchestratedTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.SetOutputHelper(output);
        }

        [Fact, PriorityOrder(40)]
        public async Task TestSetUnmanagedTagFalse()
        {
            _context.Reset();
            await TestHelper.SwitchToOrchestratedModeAsync(_context);
        }

        /// <summary>
        /// <see cref="PublishSingleNodeOrchestratedTestTheory"/> has separated all the steps in different test cases
        /// For this test theory required preparation steps are combine in this single test case
        /// </summary>
        /// <returns></returns>
        [Fact, PriorityOrder(43)]
        public async Task TestRegisterOPCServerExpectSuccess()
        {
            using var cts1 = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for microservices of IIoT platform to be healthy and modules to be deployed.
            await TestHelper.WaitForServicesAsync(_context, cts1.Token);
            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts1.Token);
            await _context.LoadSimulatedPublishedNodesAsync(cts1.Token);

            // Use the second OPC PLC for testing
            using var cts2 = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var endpointUrl = TestHelper.GetSimulatedOpcServerUrls(_context).Skip(1).First();
            _context.OpcServerUrl = endpointUrl;
            var testPlc = _context.SimulatedPublishedNodes.Values.Skip(1).First();
            _context.ConsumedOpcUaNodes[testPlc.EndpointUrl] = IIoTMultipleNodesTestContext.GetEntryModelWithoutNodes(testPlc);
            var body = new
            {
                discoveryUrl = endpointUrl
            };

            const string route = TestConstants.APIRoutes.RegistryApplications;
            var response = await TestHelper.CallRestApi(_context, Method.Post, route, body, ct: cts2.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} registering {endpointUrl} discovery url");
        }

        [Fact, PriorityOrder(44)]
        public async Task TestGetApplicationsFromRegistryExpectOneRegisteredApplication()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            dynamic json = await TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(
                _context, new HashSet<string> { _context.OpcServerUrl }, cts.Token);
            Assert.True(json != null, $"OPC Application with url {_context.OpcServerUrl} not found");
        }

        [Fact, PriorityOrder(45)]
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

        [Fact, PriorityOrder(46)]
        public async Task TestGetEndpointExpectSuccess()
        {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }
        }

        [Fact, PriorityOrder(52)]
        public async Task TestPublishNodeWithDefaultsExpectDataAvailableAtIoTHub()
        {
            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var testPlc = _context.SimulatedPublishedNodes[_context.ConsumedOpcUaNodes.First().Key];

            // We will filter out bad fast and slow nodes as they drop messages by design.
            _context.ConsumedOpcUaNodes.First().Value.OpcNodes = testPlc.OpcNodes
                .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                .Skip(250).ToList();

            var body = new
            {
                NodesToAdd = _context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(node => new
                {
                    nodeId = node.Id,
                    samplingInterval = "00:00:00.250",
                    publishingInterval = "00:00:00.500",
                }).ToArray()
            };

            var route = string.Format(CultureInfo.InvariantCulture, TestConstants.APIRoutes.PublisherBulkFormat, _context.OpcUaEndpointId);
            var response = await TestHelper.CallRestApi(_context, Method.Post, route, body, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} starting publishing bulk");
        }

        [Fact, PriorityOrder(53)]
        public async Task TestGetListOfJobsExpectJobWithEndpointId()
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
            var publishedNode = ((IEnumerable<dynamic>)json.items).Select(n => (string)n.nodeId).ToHashSet();

            var testPlc = _context.SimulatedPublishedNodes[_context.ConsumedOpcUaNodes.First().Key];
            var subscribedNodes = testPlc.OpcNodes
                .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                .Skip(250)
                .Select(n => n.Id)
                .ToHashSet();
            Assert.Equal(subscribedNodes.Count, json.items.Count);
            Assert.True(subscribedNodes.SetEquals(publishedNode));
        }

        [Fact, PriorityOrder(54)]
        public async Task TestVerifyDataAvailableAtIoTHub()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            using var validator = TelemetryValidator.Start(_context, 50, 1000, 90_000_000);

            // Wait some time to generate events to process
            // On VM in the cloud 90 seconds were not sufficient to publish data for 250 slow nodes
            await Task.Delay(TestConstants.AwaitDataInMilliseconds * 4, cts.Token);
            var result = await validator.StopAsync();
            Assert.True(result.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True(result.DroppedValueCount == 0, "Dropped messages detected");
            Assert.True(result.DuplicateValueCount == 0, "Duplicate values detected");
            Assert.True(result.DroppedSequenceCount == 0,
                $"Dropped Sequence detected: {result.DroppedSequenceCount}");
            Assert.Equal(0U, result.DuplicateSequenceCount);
            Assert.Equal(0U, result.ResetSequenceCount);

            var unexpectedNodesThatPublish = new List<string>();
            // Check that every published node is sending data
            if (_context.ConsumedOpcUaNodes != null)
            {
                var expectedNodes = new List<string>(_context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(n => n.Id));
                foreach (var property in result.ValueChangesByNodeId)
                {
                    var propertyName = property.Key;
                    var nodeId = propertyName.Split('#').Last();
                    var expected = expectedNodes.Find(n => n.EndsWith(nodeId, StringComparison.Ordinal));
                    if (expected != null)
                    {
                        expectedNodes.Remove(expected);
                    }
                    else
                    {
                        unexpectedNodesThatPublish.Add(propertyName);
                    }
                }

                expectedNodes.ForEach(_context.OutputHelper.WriteLine);
                Assert.Empty(expectedNodes);

                unexpectedNodesThatPublish.ForEach(node => _context.OutputHelper.WriteLine($"Publishing from unexpected node: {node}"));
            }
        }

        [Fact, PriorityOrder(55)]
        public async Task TestBulkUnpublishedNodesExpectSuccess()
        {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var testPlc = _context.SimulatedPublishedNodes[_context.ConsumedOpcUaNodes.First().Key];

            // We will filter out bad fast and slow nodes as they drop messages by design.
            _context.ConsumedOpcUaNodes.First().Value.OpcNodes = testPlc.OpcNodes
                .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                .Skip(250).ToList();

            var body = new
            {
                NodesToRemove = _context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(node => node.Id).ToArray()
            };

            var route = string.Format(CultureInfo.InvariantCulture, TestConstants.APIRoutes.PublisherBulkFormat, _context.OpcUaEndpointId);
            var response = await TestHelper.CallRestApi(_context, Method.Post, route, body, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} starting publishing bulk");

            // Wait untill the publishing has stopped
            await Task.Delay(TestConstants.AwaitCleanupInMilliseconds, cts.Token);
        }

        [Fact, PriorityOrder(57)]
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

        [Fact, PriorityOrder(58)]
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

        [Fact, PriorityOrder(59)]
        public async Task TestRemoveAllApplications()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            await TestHelper.Registry.RemoveAllApplicationsAsync(_context, ct: cts.Token);
        }
    }
}
