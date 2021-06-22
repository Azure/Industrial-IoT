// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using Azure.Messaging.EventHubs.Consumer;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeStandaloneTraitValue)]
    public class B_PublishMultipleNodesStandaloneTestTheory : IDisposable {
        private readonly ITestOutputHelper _output;
        private readonly IIoTStandaloneTestContext _context;
        private readonly EventHubConsumerClient _consumer;

        public B_PublishMultipleNodesStandaloneTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
            _consumer = _context.GetEventHubConsumerClient();
        }

        public void Dispose() {
            _consumer?.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Fact, PriorityOrder(1)]
        public void Test_CreateEdgeBaseDeployment_Expect_Success() {
            _context.Reset();
            var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);
            var result = _context.IoTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token).GetAwaiter().GetResult();
            _output.WriteLine("Created/Updated new EdgeBase deployment");
            Assert.True(result);
        }

        [Fact, PriorityOrder(2)]
        public void Test_CreatePublisherLayeredDeployment_Expect_Success() {
            var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);
            var result = _context.IoTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token).GetAwaiter().GetResult();
            _output.WriteLine("Created/Updated layered deployment for publisher module");
            Assert.True(result);
        }

        [Fact, PriorityOrder(3)]
        public void Test_StartPublishing250Nodes_Expect_Success() {
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));         
            _context.LoadSimulatedPublishedNodes(cts.Token).GetAwaiter().GetResult();

            PublishedNodesEntryModel nodesToPublish;
            if (_context.SimulatedPublishedNodes.Count > 1) {
                var testPlc = _context.SimulatedPublishedNodes.Skip(2).First().Value;
                nodesToPublish = _context.GetEntryModelWithoutNodes(testPlc);

                // We want to take one of the slow nodes that updates each 10 seconds.
                // To make sure that we will not have missing values because of timing issues,
                // we will set publishing and sampling intervals to a lower value than the publishing
                // interval of the simulated OPC PLC. This will eliminate false-positives.
                nodesToPublish.OpcNodes = testPlc.OpcNodes
                    .Take(250)
                    .Select(opcNode => {
                        var opcPlcPublishingInterval = opcNode.OpcPublishingInterval;
                        opcNode.OpcPublishingInterval = opcPlcPublishingInterval / 2;
                        opcNode.OpcSamplingInterval = opcPlcPublishingInterval / 4;
                        return opcNode;
                    })
                    .ToArray();

                _context.ConsumedOpcUaNodes.Add(testPlc.EndpointUrl, nodesToPublish);
            }
            else {
                var opcPlcIp = _context.OpcPlcConfig.Urls.Split(TestConstants.SimulationUrlsSeparator)[2];
                nodesToPublish = new PublishedNodesEntryModel {
                    EndpointUrl = $"opc.tcp://{opcPlcIp}:50000",
                    UseSecurity = false
                };

                var nodes = new List<OpcUaNodesModel>();
                for (int i = 0; i < 250; i++) {
                    nodes.Add(new OpcUaNodesModel {
                        Id = $"ns=2;s=SlowUInt{i+1}",
                        OpcPublishingInterval = 10000/2,
                        OpcSamplingInterval = 10000/4
                    });
                }

                nodesToPublish.OpcNodes = nodes.ToArray();
                _context.ConsumedOpcUaNodes.Add(opcPlcIp, nodesToPublish);
            }

            cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(new[] { nodesToPublish }, _context, cts.Token).GetAwaiter().GetResult();
        }

        [Fact, PriorityOrder(4)]
        public void Test_WaitForModuleDeployed() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token, new string[] { "publisher_standalone" }).GetAwaiter().GetResult());
            Assert.Null(exception);
        }

        [Fact, PriorityOrder(5)]
        public async Task Test_VerifyDataAvailableAtIoTHub_Expect_NumberOfValueChanges_GreaterThan_Zero() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            //wait some time till the updated pn.json is reflected
            Task.Delay(3 * 60 * 1000, cts.Token).GetAwaiter().GetResult();

            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            var messages = _consumer.ReadEventsAsync(false, cancellationToken: cts.Token);
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 250, 10_000, 90_000_000, cts.Token);

            // wait some time to generate events to process
            Task.Delay(90 * 1000, cts.Token).GetAwaiter().GetResult();
            var json = TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).GetAwaiter().GetResult();
            Assert.True((int)json.totalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True((uint)json.droppedValueCount == 0, "Dropped messages detected");
            Assert.True((uint)json.duplicateValueCount == 0, "Duplicate values detected");

            // check that every published node is sending data
            var expectedNodes = new HashSet<string>(_context.ConsumedOpcUaNodes.First().Value.OpcNodes.Select(n => n.Id));
            Assert.NotEmpty(expectedNodes);

            await foreach (var message in messages.WithCancellation(cts.Token)) {
                var batchedMessages = message.DeserializeJson<JArray>();
                foreach (dynamic batchedMessage in batchedMessages) {
                    foreach (var messageItem in batchedMessage.Messages) {
                        foreach (var payloadItem in messageItem.Payload.Properties()) {
                            var nodeId = (string)payloadItem.Name;
                            Assert.True(expectedNodes.Remove(nodeId), $"Publishing from unexpected node: {nodeId}");
                        }
                    }
                }

                if (!expectedNodes.Any()) {
                    break;
                }

            }
            Assert.Empty(expectedNodes);
        }

        [Fact, PriorityOrder(6)]
        public void Test_StopPublishingAllNodes_Expect_Success() {
            TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(new PublishedNodesEntryModel[0], _context).GetAwaiter().GetResult();
        }

        [Fact, PriorityOrder(7)]
        public void Test_VerifyNoDataIncomingAtIoTHub_Expected_NumberOfValueChanges_Equals_Zero() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            Task.Delay(90 * 1000, cts.Token).GetAwaiter().GetResult(); //wait till the publishing has stopped

            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).GetAwaiter().GetResult();
            // wait some time to generate events to process
            Task.Delay(90 * 1000, cts.Token).GetAwaiter().GetResult();

            var json = TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).GetAwaiter().GetResult();
            Assert.True((int)json.totalValueChangesCount == 0, "Messages received at IoT Hub");
        }
    }
}
