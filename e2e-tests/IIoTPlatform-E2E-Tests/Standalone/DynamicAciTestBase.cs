// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using Azure.Messaging.EventHubs.Consumer;
    using IIoTPlatform_E2E_Tests.Deploy;
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Base class for standalone tests using dynamic ACI
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeStandaloneTraitValue)]
    public abstract class DynamicAciTestBase : IDisposable {
        protected readonly ITestOutputHelper _output;
        protected readonly IIoTMultipleNodesTestContext _context;
        protected readonly CancellationToken _timeoutToken;
        protected readonly EventHubConsumerClient _consumer;
        protected readonly string _writerId;
        private readonly CancellationTokenSource _timeoutTokenSource;

        protected DynamicAciTestBase(IIoTMultipleNodesTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
            _timeoutTokenSource = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _timeoutToken = _timeoutTokenSource.Token;
            _consumer = _context.GetEventHubConsumerClient();
            _writerId = Guid.NewGuid().ToString();
        }

        public void Dispose() {
            _consumer?.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
            _timeoutTokenSource?.Dispose();
        }

        [Fact, PriorityOrder(1)]
        public async Task Test_CreateEdgeBaseDeployment_Expect_Success() {
            var ioTHubEdgeBaseDeployment = new IoTHubEdgeBaseDeployment(_context);
            var result = await ioTHubEdgeBaseDeployment.CreateOrUpdateLayeredDeploymentAsync(_timeoutToken);
            _output.WriteLine("Created/Updated new EdgeBase deployment");
            Assert.True(result);
        }

        [Fact, PriorityOrder(2)]
        public async Task Test_CreatePublisherLayeredDeployment_Expect_Success() {
            var ioTHubPublisherDeployment = new IoTHubPublisherDeployment(_context, MessagingMode.PubSub);
            var result = await ioTHubPublisherDeployment.CreateOrUpdateLayeredDeploymentAsync(_timeoutToken);
            _output.WriteLine("Created/Updated layered deployment for publisher module");
            Assert.True(result);
        }

        [Fact, PriorityOrder(3)]
        public async Task Test_WaitForModuleDeployed() {
            // We will wait for module to be deployed.
            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId,
                _timeoutToken, new[] { "publisher_standalone" });
        }

        [Fact, PriorityOrder(998)]
        public async Task Test_StopPublishingAllNodes_Expect_Success() {
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, new PublishedNodesEntryModel[0], _timeoutToken);
        }

        [Fact, PriorityOrder(999)]
        public void Test_DeleteAci() {
            TestHelper.DeleteSimulationContainer(_context);
        }
    }
}
