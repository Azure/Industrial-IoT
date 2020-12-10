// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using System;
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
    [TestCaseOrderer("IIoTPlatform_E2E_Tests.TestExtensions.TestOrderer", TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeStandaloneTraitValue)]
    public class PublishSingleNodeStandaloneTestTheory {
        private readonly ITestOutputHelper _output;
        private readonly IIoTStandaloneTestContext _context;

        public PublishSingleNodeStandaloneTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Fact, PriorityOrder(1)]
        public async Task Test_CreateEdgeBaseDeployment_Expect_Success() {
            var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);
            var result = await _context.IoTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            _output.WriteLine("Created/Updated new EdgeBase deployment");
            Assert.True(result);
        }

        [Fact, PriorityOrder(2)]
        public async Task Test_CreatePublisherLayeredDeployment_Expect_Success() {
            var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);
            var result = await _context.IoTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            _output.WriteLine("Created/Updated layered deployment for publisher module");
            Assert.True(result);
        }

        [Fact, PriorityOrder(3)]
        public async Task Test_WaitForModuleDeployed() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for module to be deployed.
            var exception = await Record.ExceptionAsync(async () => await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token, new string[] { "publisher_standalone" }));
            Assert.Null(exception);
        }

        [Fact, PriorityOrder(4)]
        public async Task Test_StartPublishingSingleNode_Expect_Success() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var simulatedPublishedNodesConfiguration = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token);

            var model = simulatedPublishedNodesConfiguration[simulatedPublishedNodesConfiguration.Keys.First()];
            model.OpcNodes = model.OpcNodes.Take(1).ToArray();

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(new[] { model }, _context, cts.Token);
        }

        [Fact, PriorityOrder(5)]
        public async Task Test_VerifyDataAvailableAtIoTHub_Expect_NumberOfValueChanges_GreaterThan_Zero() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);

            // wait some time to generate events to process
            await Task.Delay(90 * 1000, cts.Token);
            var json = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)json.totalValueChangesCount > 0, "No messages received at IoT Hub");
        }

        [Fact, PriorityOrder(6)]
        public async Task Test_StopPublishingAllNodes_Expect_Success() {
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(new PublishedNodesEntryModel[0], _context);
        }

        [Fact, PriorityOrder(7)]
        public async Task Test_VerifyNoDataIncomingAtIoTHub_Expected_NumberOfValueChanges_Equals_Zero() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            await Task.Delay(90 * 1000, cts.Token); //wait till the publishing has stopped

            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token);
            // wait some time to generate events to process
            await Task.Delay(90 * 1000, cts.Token);

            var json = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)json.totalValueChangesCount == 0, "Messages received at IoT Hub");
        }

        [Fact, PriorityOrder(8)]
        public async Task Test_SwitchToOrchestratedMode() {
            await TestHelper.SwitchToOrchestratedModeAsync(_context);
        }
    }
}
