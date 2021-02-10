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
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Standalone Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeStandaloneTraitValue)]
    public class A_PublishSingleNodeStandaloneTestTheory {
        private readonly ITestOutputHelper _output;
        private readonly IIoTStandaloneTestContext _context;

        public A_PublishSingleNodeStandaloneTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Fact, PriorityOrder(1)]
        public void Test_CreateEdgeBaseDeployment_Expect_Success() {
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
        public void Test_StartPublishingSingleNode_Expect_Success() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var simulatedPublishedNodesConfiguration = TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token).GetAwaiter().GetResult();

            var model = simulatedPublishedNodesConfiguration[simulatedPublishedNodesConfiguration.Keys.First()];
            model.OpcNodes = model.OpcNodes.Take(1).ToArray();

            TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(new[] { model }, _context, cts.Token).GetAwaiter().GetResult();

            Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).GetAwaiter().GetResult(); //wait some time till the updated pn.json is reflected
        }

        [Fact, PriorityOrder(4)]
        public void Test_WaitForModuleDeployed() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for module to be deployed.
            var exception = Record.Exception(() => _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token, new string[] { "publisher_standalone" }).GetAwaiter().GetResult());
            Assert.Null(exception);
        }

        [Fact, PriorityOrder(5)]
        public void Test_VerifyDataAvailableAtIoTHub_Expect_NumberOfValueChanges_GreaterThan_Zero() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).GetAwaiter().GetResult();

            // wait some time to generate events to process
            Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).GetAwaiter().GetResult();
            var json = TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).GetAwaiter().GetResult();
            Assert.True((int)json.totalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True((uint)json.DroppedValueCount == 0, "Dropped messages detected");
            Assert.True((uint)json.DuplicateValueCount == 0, "Duplicate values detected");
        }

        [Fact, PriorityOrder(6)]
        public void Test_StopPublishingAllNodes_Expect_Success() {
            TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(new PublishedNodesEntryModel[0], _context).GetAwaiter().GetResult();
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).GetAwaiter().GetResult(); //wait till the publishing has stopped
        }

        [Fact, PriorityOrder(7)]
        public void Test_VerifyNoDataIncomingAtIoTHub_Expected_NumberOfValueChanges_Equals_Zero() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            //use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).GetAwaiter().GetResult();
            // wait some time to generate events to process
            Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).GetAwaiter().GetResult();

            var json = TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).GetAwaiter().GetResult();
            Assert.True((int)json.totalValueChangesCount == 0, "Messages received at IoT Hub");
        }
    }
}
