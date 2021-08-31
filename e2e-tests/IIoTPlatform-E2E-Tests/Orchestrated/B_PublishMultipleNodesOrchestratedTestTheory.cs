// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Orchestrated
{
    using System;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Newtonsoft.Json;
    using RestSharp;
    using TestExtensions;
    using Xunit.Abstractions;
    using System.Threading;
    using Newtonsoft.Json.Converters;
    using System.Collections.Generic;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue)]
    public class B_PublishMultipleNodesOrchestratedTestTheory {
        private readonly ITestOutputHelper _output;
        private readonly IIoTMultipleNodesTestContext _context;

        public B_PublishMultipleNodesOrchestratedTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Fact, PriorityOrder(50)]
        public void Test_SetUnmanagedTagFalse() {
            _context.Reset();
            TestHelper.SwitchToOrchestratedModeAsync(_context).GetAwaiter().GetResult();
        }

        /// <summary>
        /// <see cref="PublishSingleNodeOrchestratedTestTheory"/> has separated all the steps in different test cases
        /// For this test theory required preparation steps are combine in this single test case
        /// </summary>
        /// <returns></returns>
        [Fact, PriorityOrder(51)]
        public void Test_PrepareTestDeploymentForTestCase_Expect_Success() {

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for microservices of IIoT platform to be healthy and modules to be deployed.
            TestHelper.WaitForServicesAsync(_context, cts.Token).GetAwaiter().GetResult();
            _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token).GetAwaiter().GetResult();
            _context.LoadSimulatedPublishedNodes(cts.Token).GetAwaiter().GetResult();

            // Use the second OPC PLC for testing
            cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _context.OpcServerUrl = TestHelper.GetSimulatedOpcServerUrls(_context).Skip(1).First();
            var testPlc = _context.SimulatedPublishedNodes.Values.Skip(1).First();
            _context.ConsumedOpcUaNodes[testPlc.EndpointUrl] = _context.GetEntryModelWithoutNodes(testPlc);
            var body = new {
                discoveryUrl = _context.OpcServerUrl
            };

            var route = TestConstants.APIRoutes.RegistryApplications;
            TestHelper.CallRestApi(_context, Method.POST, route, body, ct: cts.Token);

            // Check that Application was registered
            cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            dynamic json = TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(_context, cts.Token, new List<string> { _context.OpcServerUrl }).GetAwaiter().GetResult();
            Assert.True(json != null, "OPC Application not activated");

            // Read OPC UA Endpoint ID
            var endpointId = TestHelper.Discovery.GetOpcUaEndpointId(_context, _context.OpcServerUrl, cts.Token).GetAwaiter().GetResult();
            Assert.True(endpointId != null, "Could not find endpoints of OPC Application");
            _context.OpcUaEndpointId = endpointId;

            // Activate OPC UA Endpoint and wait until it's activated
            cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            TestHelper.Registry.ActivateEndpointAsync(_context, _context.OpcUaEndpointId, cts.Token).GetAwaiter().GetResult();
        }

        [Fact, PriorityOrder(52)]
        public void Test_PublishNodeWithDefaults_Expect_DataAvailableAtIoTHub() {

            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                Test_PrepareTestDeploymentForTestCase_Expect_Success();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var testPlc = _context.SimulatedPublishedNodes[_context.ConsumedOpcUaNodes.First().Key];

            // We will filter out bad fast and slow nodes as they drop messages by design.
            _context.ConsumedOpcUaNodes.First().Value.OpcNodes = testPlc.OpcNodes
                .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                .Skip(250).ToArray();

            var body = new {
                NodesToAdd = _context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(node => new {
                    nodeId = node.Id,
                    samplingInterval = "00:00:00.250",
                    publishingInterval = "00:00:00.500",
                }).ToArray()
            };

            var route = string.Format(TestConstants.APIRoutes.PublisherBulkFormat, _context.OpcUaEndpointId);
            TestHelper.CallRestApi(_context, Method.POST, route, body, ct: cts.Token);
        }

        [Fact, PriorityOrder(53)]
        public void Test_GetListOfJobs_Expect_JobWithEndpointId() {

            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                Test_PrepareTestDeploymentForTestCase_Expect_Success();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var route = TestConstants.APIRoutes.PublisherJobs;
            var response = TestHelper.CallRestApi(_context, Method.GET, route, ct: cts.Token);
            dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());

            bool found = false;
            for (int jobIndex = 0; jobIndex < (int)json.jobs.Count; jobIndex++) {
                var id = (string)json.jobs[jobIndex].id;
                if (id == _context.OpcUaEndpointId) {
                    found = true;
                    break;
                }
            }
            Assert.True(found, "Publishing Job was not created!");
        }

        [Fact, PriorityOrder(54)]
        public void Test_VerifyDataAvailableAtIoTHub() {

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            TestHelper.StartMonitoringIncomingMessagesAsync(_context, 50, 1000, 90_000_000, cts.Token).GetAwaiter().GetResult();

            // Wait some time to generate events to process
            // On VM in the cloud 90 seconds were not sufficient to publish data for 250 slow nodes
            var delay = TestConstants.DefaultTimeoutInMilliseconds * 2;
            Task.Delay(delay, cts.Token).GetAwaiter().GetResult();
            var json = TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).GetAwaiter().GetResult();
            Assert.True((int)json.totalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True((uint)json.droppedValueCount == 0, "Dropped messages detected");
            Assert.True((uint)json.duplicateValueCount == 0, "Duplicate values detected");

            var unexpectedNodesThatPublish = new List<string>();
            // Check that every published node is sending data
            if (_context.ConsumedOpcUaNodes != null) {
                var expectedNodes = new List<string>(_context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(n => n.Id));
                foreach(dynamic property in json.valueChangesByNodeId) {
                    var propertyName = (string)property.Name;
                    var nodeId = propertyName.Split('#').Last();
                    var expected = expectedNodes.FirstOrDefault(n => n.EndsWith(nodeId));
                    if (expected != null) {
                        expectedNodes.Remove(expected);
                    } else {
                        unexpectedNodesThatPublish.Add(propertyName);
                    }
                }

                expectedNodes.ForEach(n => _context.OutputHelper.WriteLine(n));
                Assert.Empty(expectedNodes);

                unexpectedNodesThatPublish.ForEach(node => _context.OutputHelper.WriteLine($"Publishing from unexpected node: {node}"));
            }
        }

        [Fact, PriorityOrder(55)]
        public void Test_BulkUnpublishedNodes_Expect_Success() {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                Test_PrepareTestDeploymentForTestCase_Expect_Success();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var testPlc = _context.SimulatedPublishedNodes[_context.ConsumedOpcUaNodes.First().Key];

            // We will filter out bad fast and slow nodes as they drop messages by design.
            _context.ConsumedOpcUaNodes.First().Value.OpcNodes = testPlc.OpcNodes
                .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                .Skip(250).ToArray();

            var body = new {
                NodesToRemove = _context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(node => node.Id ).ToArray()
            };

            var route = string.Format(TestConstants.APIRoutes.PublisherBulkFormat, _context.OpcUaEndpointId);
            TestHelper.CallRestApi(_context, Method.POST, route, body, ct: cts.Token);
        }

        [Fact, PriorityOrder(56)]
        public void Test_VerifyNoDataIncomingAtIoTHub() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).GetAwaiter().GetResult(); //wait till the publishing has stopped
            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).GetAwaiter().GetResult();
            // wait some time to generate events to process
            Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).GetAwaiter().GetResult();
            var json = TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).GetAwaiter().GetResult();
            Assert.True((int)json.totalValueChangesCount == 0, "Unexpected Messages received at IoT Hub");
        }


        [Fact, PriorityOrder(57)]
        public void RemoveJob_Expect_Success() {

            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId)) {
                Test_PrepareTestDeploymentForTestCase_Expect_Success();
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var route = string.Format(TestConstants.APIRoutes.PublisherJobsFormat, _context.OpcUaEndpointId);
            TestHelper.CallRestApi(_context, Method.DELETE, route, ct: cts.Token);
        }

        [Fact, PriorityOrder(58)]
        public void Test_RemoveAllApplications() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var route = TestConstants.APIRoutes.RegistryApplications;
            TestHelper.CallRestApi(_context, Method.DELETE, route, ct: cts.Token);
        }
    }
}
