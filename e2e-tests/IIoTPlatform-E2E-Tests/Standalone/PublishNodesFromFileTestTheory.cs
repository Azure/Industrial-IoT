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
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer("IIoTPlatform_E2E_Tests.TestExtensions.TestOrderer", TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeStandaloneTraitValue)]
    public class PublishNodesFromFileTestTheory {
        private readonly ITestOutputHelper _output;
        private readonly IIoTStandaloneTestContext _context;

        public PublishNodesFromFileTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Fact, PriorityOrder(1)]
        public async Task Test_ReadSimulatedOpcUaNodes() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync(_context, cts.Token);
            Assert.NotNull(simulatedOpcServer);
            Assert.NotEmpty(simulatedOpcServer.Keys);
            Assert.NotEmpty(simulatedOpcServer.Values);
        }

        [Fact, PriorityOrder(2)]
        public async Task Test_CreateEdgeBaseDeployment_Expect_Success() {
            var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);
            var result = await _context.IoTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            _output.WriteLine("Created new EdgeBase layered deployment and publisher_standalone");
            Assert.True(result);
        }

        [Fact, PriorityOrder(3)]
        public async Task Test_SwitchToStandaloneMode() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync(_context, cts.Token);

            //Create a published_nodes file with one node
            var opcPlcServerNodes = simulatedOpcServer[simulatedOpcServer.Keys.First()];
            var publishedNodes = opcPlcServerNodes;
            publishedNodes.OpcNodes = opcPlcServerNodes.OpcNodes.Take(1).ToArray();
            TestHelper.SavePublishedNodesFile(publishedNodes, _context);
            _output.WriteLine("Saved published_nodes.json file");

            await TestHelper.SwitchToStandaloneModeAsync(_context, cts.Token);
            TestHelper.LoadPublishedNodesFile(_context.PublishedNodesFileInternalFolder, TestConstants.PublishedNodesFolder + "/" + TestConstants.PublisherPublishedNodesFile, _context);
            _output.WriteLine("Switched to standalone mode and loaded published_nodes.json file");
        }

        [Fact, PriorityOrder(4)]
        public async Task Test_PublishFromPublishedNodesFile_Expect_Success() {
            var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);
            var result = await _context.IoTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(cts.Token);
            _output.WriteLine("Created new layered deployment and publisher_standalone");
            Assert.True(result);
        }

        [Fact, PriorityOrder(5)]
        public async Task Test_WaitForModuleDeployed() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for module to be deployed.
            var exception = await Record.ExceptionAsync(async () => await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token, new string[] { "publisher_standalone" }));
            Assert.Null(exception);
        }

        [Fact, PriorityOrder(6)]
        public async Task Test_VerifyDataAvailableAtIoTHub() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            //use test event processor to verify data send to IoT Hub
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 1, 1000, 10000, cts.Token);

            // wait some time to generate events to process
            await Task.Delay(90 * 1000, cts.Token);
            var json = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token);
            Assert.True((int)json.totalValueChangesCount > 0, "No messages received at IoT Hub");
        }

        [Fact, PriorityOrder(7)]
        public async Task SwitchToOrchestratedMode() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            await TestHelper.SwitchToOrchestratedModeAsync(
                TestConstants.PublishedNodesFolder + "/" + TestConstants.PublisherPublishedNodesFile,
                _context,
                cts.Token
            );
            _output.WriteLine("Switched to orchestrated mode and deleted published_nodes.json file");
        }
    }
}
