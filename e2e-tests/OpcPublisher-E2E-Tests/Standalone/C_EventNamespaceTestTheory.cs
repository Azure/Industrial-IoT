// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.Standalone {
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class C_EventNamespaceTestTheory : DynamicAciTestBase, IClassFixture<IIoTStandaloneTestContext> {
        public C_EventNamespaceTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
            : base(context, output) {
        }

        [Fact, PriorityOrder(10)]
        public Task Test_DeployAci() {
            return TestHelper.CreateSimulationContainerAsync(_context, new List<string> { "/bin/sh", "-c", "./opcplc --autoaccept -ses --pn=50000" }, _timeoutToken);
        }

        [Fact, PriorityOrder(11)]
        public async Task Test_VerifyIntegerNamespace_Expect_SimpleEvents_InHub() {
            // Arrange
            var pnJson = SimpleEvents(
                "ns=0;i=2041",
                "0:Message",
                "ns=5;i=2",
                "5:CycleId",
                "ns=5;i=2");

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);
            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemCycleStatusEventTypePayload>(_writerId, _timeoutToken, 1);

            // Act
            var payloads = await messages.Select(v => v.Payload).ToListAsync(_timeoutToken);

            // Assert
            VerifyPayloads(payloads);
        }

        [Fact, PriorityOrder(12)]
        public async Task Test_VerifyStringNamespace_Expect_SimpleEvents_InHub() {
            // Arrange
            var pnJson = SimpleEvents(
                "i=2041",
                "Message",
                "nsu=http://microsoft.com/Opc/OpcPlc/SimpleEvents;i=2",
                "http://microsoft.com/Opc/OpcPlc/SimpleEvents#CycleId",
                "nsu=http://microsoft.com/Opc/OpcPlc/SimpleEvents;i=2");

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);
            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemCycleStatusEventTypePayload>(_writerId, _timeoutToken, 1);

            // Act
            var payloads = await messages.Select(v => v.Payload).ToListAsync(_timeoutToken);

            // Assert
            VerifyPayloads(payloads);
        }

        [Fact, PriorityOrder(13)]
        public async Task Test_VerifyIntegerNamespace_Expect_FilteredSimpleEvents_InHub() {
            // Arrange
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("ns=5;i=2"));

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);
            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemCycleStatusEventTypePayload>(_writerId, _timeoutToken, 1);

            // Act
            var payloads = await messages.Select(v => v.Payload).ToListAsync(_timeoutToken);

            // Assert
            VerifyPayloads(payloads);
        }

        [Fact, PriorityOrder(14)]
        public async Task Test_VerifyStringNamespace_Expect_FilteredSimpleEvents_InHub() {
            // Arrange
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("nsu=http://microsoft.com/Opc/OpcPlc/SimpleEvents;i=2"));

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);
            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemCycleStatusEventTypePayload>(_writerId, _timeoutToken, 1);

            // Act
            var payloads = await messages.Select(v => v.Payload).ToListAsync(_timeoutToken);

            // Assert
            VerifyPayloads(payloads);
        }
    }
}