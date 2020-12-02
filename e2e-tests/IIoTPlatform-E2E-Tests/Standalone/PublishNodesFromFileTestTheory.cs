// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using TestExtensions;
    using Xunit.Abstractions;
    using System.Threading;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer("IIoTPlatform_E2E_Tests.TestExtensions.TestOrderer", TestConstants.TestAssemblyName)]
    [Collection("IIoT Platform Test Collection")]
    public class PublishNodesFromFileTestTheory {
        private readonly ITestOutputHelper _output;
        private readonly IIoTPlatformTestContext _context;

        public PublishNodesFromFileTestTheory(IIoTPlatformTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Fact, PriorityOrder(1)]
        public async Task Test_ReadSimulatedOpcUaNodes() {
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync(_context);
            Assert.NotNull(simulatedOpcServer);
            Assert.NotEmpty(simulatedOpcServer.Keys);
            Assert.NotEmpty(simulatedOpcServer.Values);
        }

        [Fact, PriorityOrder(2)]
        public async Task Test_SwitchToStandaloneMode() {
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync(_context);
            var numberOfNodes = 1;
            //Create and save a published_nodes.json file with numberOfNodes nodes
            TestHelper.SavePublishedNodesFile(simulatedOpcServer[simulatedOpcServer.Keys.First()], numberOfNodes, _context);
            _output.WriteLine("Saved published_nodes.json file");

            TestHelper.SwitchToStandaloneMode(_context);
            TestHelper.LoadPublishedNodesFile(_context.PublishedNodesFileInternalFolder, TestConstants.PublishedNodesFolder + "/" + TestConstants.PublisherPublishedNodesFile, _context);
            _output.WriteLine("Switched to standalone mode and loaded published_nodes.json file");
        }

        [Fact, PriorityOrder(3)]
        public async Task Test_PublishFromPublishedNodesFile_Expect_Success() {
            var deploy = new IoTHubPublisherDeployment(_context);
            Assert.NotNull(deploy);
            var result = await deploy.CreateOrUpdateLayeredDeploymentAsync();
            _output.WriteLine("Created new layered deployment and publisher_standalone");
            Assert.True(result);
        }

        [Fact, PriorityOrder(4)]
        public async Task Test_WaitForModuleDeployed() {
            var cts = new CancellationTokenSource(TestConstants.MaxDelayDeploymentToBeLoadedInMilliseconds);

            // We will wait for module to be deployed.
            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token, _context, new string[] { "publisher_standalone" });
        }

        [Fact, PriorityOrder(5)]
        public async void Test_VerifyDataAvailableAtIoTHub() {
            //use test event processor to verify data send to IoT Hub
            await TestHelper.StartMonitoringIncomingMessages(_context, 1, 1000, 10000);

            // wait some time to generate events to process
            await Task.Delay(90 * 1000);
            await TestHelper.StopMonitoringIncomingMessages(_context);
        }

        [Fact, PriorityOrder(6)]
        public void SwitchToOrchestratedMode() {
            TestHelper.SwitchToOrchestratedMode(TestConstants.PublishedNodesFolder + "/" + TestConstants.PublisherPublishedNodesFile, _context);
            _output.WriteLine("Switched to orchestrated mode and deleted published_nodes.json file");
        }
    }
}
