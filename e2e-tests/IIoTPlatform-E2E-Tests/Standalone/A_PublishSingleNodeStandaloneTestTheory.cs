// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using IIoTPlatform_E2E_Tests.Deploy;
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
    public class A_PublishSingleNodeStandaloneTestTheory {

        private readonly ITestOutputHelper _output;
        private readonly IIoTMultipleNodesTestContext _context;

        public A_PublishSingleNodeStandaloneTestTheory(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Theory]
        [InlineData(MessagingMode.Samples)]
        [InlineData(MessagingMode.PubSub)]
        async Task SubscribeUnsubscribeTest(MessagingMode messagingMode) {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var ioTHubPublisherDeployment = new IoTHubPublisherDeployment(_context, messagingMode);

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);

            // Clean publishednodes.json.
            await TestHelper.PublishNodesAsync(Array.Empty<PublishedNodesEntryModel>(), _context);

            // Create base edge deployment.
            var baseDeploymentResult = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _output.WriteLine("Created/Updated new edge base deployment.");

            // Create layered edge deployment.
            var layeredDeploymentResult = await ioTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            Assert.True(layeredDeploymentResult, "Failed to create/update layered deployment for publisher module.");
            _output.WriteLine("Created/Updated layered deployment for publisher module.");

            IDictionary<string, PublishedNodesEntryModel> simulatedPublishedNodesConfiguration =
                new Dictionary<string, PublishedNodesEntryModel>(0);

            // With the nested edge test servers don't have public IP addresses and cannot be accessed in this way
            if (_context.IoTEdgeConfig.NestedEdgeFlag != "Enable") {
                simulatedPublishedNodesConfiguration =
                    await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);
            }

            PublishedNodesEntryModel model;
            if (simulatedPublishedNodesConfiguration.Count > 0) {
                model = simulatedPublishedNodesConfiguration[simulatedPublishedNodesConfiguration.Keys.First()];
            }
            else {
                var opcPlcIp = _context.OpcPlcConfig.Urls.Split(TestConstants.SimulationUrlsSeparator)[0];
                model = new PublishedNodesEntryModel {
                    EndpointUrl = $"opc.tcp://{opcPlcIp}:50000",
                    UseSecurity = false,
                    OpcNodes = new OpcUaNodesModel[] {
                        new OpcUaNodesModel {
                            Id = "ns=2;s=SlowUInt1",
                            OpcPublishingInterval = 10000
                        }
                    }
                };
            }

            // We want to take one of the slow nodes that updates each 10 seconds.
            // To make sure that we will not have missing values because of timing issues,
            // we will set publishing and sampling intervals to a lower value than the publishing
            // interval of the simulated OPC PLC. This will eliminate false-positives.
            model.OpcNodes = model.OpcNodes
                .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                .Where(opcNode => opcNode.Id.Contains("SlowUInt"))
                .Take(1).Select(opcNode => {
                    var opcPlcPublishingInterval = opcNode.OpcPublishingInterval;
                    opcNode.OpcPublishingInterval = opcPlcPublishingInterval / 2;
                    opcNode.OpcSamplingInterval = opcPlcPublishingInterval / 4;
                    return opcNode;
                })
                .ToArray();

            await TestHelper.PublishNodesAsync(new[] { model }, _context);
            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token);

            // Wait some time till the updated pn.json is reflected.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(
                _context.DeviceConfig.DeviceId,
                cts.Token,
                new string[] { "publisher_standalone" }
            ).GetAwaiter().GetResult());
            Assert.Null(exception);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            var publishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)publishingMonitoringResultJson.totalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True((uint)publishingMonitoringResultJson.droppedValueCount == 0,
                $"Dropped messages detected: {(uint)publishingMonitoringResultJson.droppedValueCount}");
            Assert.True((uint)publishingMonitoringResultJson.duplicateValueCount == 0,
                $"Duplicate values detected: {(uint)publishingMonitoringResultJson.duplicateValueCount}");

            // Stop publishing nodes.
            await TestHelper.PublishNodesAsync(Array.Empty<PublishedNodesEntryModel>(), _context);
            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token);

            // Wait till the publishing has stopped.
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero
            // as data gap analysis is not part of this test case).
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token);

            // Stop monitoring and get the result.
            var unpublishingMonitoringResultJson = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)unpublishingMonitoringResultJson.totalValueChangesCount == 0,
                $"Messages received at IoT Hub: {(int)unpublishingMonitoringResultJson.totalValueChangesCount}");
        }
    }
}
