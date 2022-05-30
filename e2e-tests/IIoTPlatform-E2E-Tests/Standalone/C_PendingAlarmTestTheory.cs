// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using Azure.Messaging.EventHubs.Consumer;
    using FluentAssertions;
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using IIoTPlatform_E2E_Tests.TestModels;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    public class C_PendingAlarmTestTheory : DynamicAciTestBase {
        public C_PendingAlarmTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output)
        : base(context, output) {
        }

        [Fact, PriorityOrder(10)]
        public async void Test_VerifyDataAvailableAtIoTHub_Expect_PendingAlarmsView() {

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --alm --pn=50000"},
                _timeoutToken).ConfigureAwait(false);

            var messages = _consumer.ReadPendingAlarmMessagesFromWriterIdAsync<ConditionTypePayload>(_writerId, _timeoutToken);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.PendingAlarmsForAlarmsView()
            );
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, pnJson, _timeoutToken).ConfigureAwait(false);
            // take any message
            var ev = await messages
                .FirstAsync(_timeoutToken);

            // Assert
            ValidatePendingAlarmsView(ev, false);
        }

        private static void ValidatePendingAlarmsView(PendingAlarmEventData<ConditionTypePayload> eventData, bool expectCompressedPayload) {
            Assert.Equal(expectCompressedPayload, eventData.IsPayloadCompressed);
            foreach (var pendingMessage in eventData.Messages.PendingMessages) {
                pendingMessage.ConditionId.Should().StartWith("http://microsoft.com/Opc/OpcPlc/AlarmsInstance#");
                eventData.Messages.PendingMessages.Where(x => x.ConditionId == pendingMessage.ConditionId).ToList().Should().HaveCount(1);
                pendingMessage.Retain.Should().BeTrue();
            }
        }
    }
}
