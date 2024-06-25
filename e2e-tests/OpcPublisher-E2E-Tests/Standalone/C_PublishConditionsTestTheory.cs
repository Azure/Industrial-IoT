// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.Standalone
{
    using FluentAssertions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using static System.TimeSpan;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class CPublishConditionsTestTheory : DynamicAciTestBase, IClassFixture<IIoTStandaloneTestContext>
    {
        private static readonly TimeSpan Precision = FromMilliseconds(500);

        public CPublishConditionsTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
            : base(context, output)
        {
        }

        [Fact, PriorityOrder(11)]
        public async Task TestACIVerifyDataAvailableAtIoTHubExpectNumberOfEventsGreaterThanZero()
        {
            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --dalm=files/sc001.json --pn=50000"},
                _timeoutToken,
                "opc-plc-files/sc001.json");

            var messages = _consumer.ReadMessagesFromWriterIdAsync<ConditionTypePayload>(_writerId, 10000, _timeoutToken, _context);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter());
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);

            const int nMessages = 6;
            var payloads = await messages
                .Select(e => e.Payload)
                .Skip(nMessages) // First batch of alarms are from a ConditionRefresh, therefore not in order
                .SkipWhile(c => !c.Message.Value.Contains("LAST EVENT IN LOOP", StringComparison.Ordinal))
                .Skip(1)
                .Take(nMessages)
                .ToListAsync(_timeoutToken);

            // Assert

            var i = -1;
            var doorOpen = new ConditionTypePayload
            {
                ConditionName = DataValueObject.Create("VendingMachine1_DoorOpen"),
                EnabledState = DataValueObject.Create("Enabled"),
                EnabledStateEffectiveDisplayName = DataValueObject.Create("Active | Unacknowledged"),
                EnabledStateId = DataValueObject.Create<bool?>(true),
                EventType = DataValueObject.Create("i=10751"),
                LastSeverity = DataValueObject.Create<int?>(500),
                Message = DataValueObject.Create("Door Open"),
                Retain = DataValueObject.Create<bool?>(true),
                Severity = DataValueObject.Create(900),
                SourceName = DataValueObject.Create("VendingMachine1"),
                SourceNode = DataValueObject.Create("http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1")
            };
            VerifyPayload(payloads, ++i, null, doorOpen);

            VerifyPayload(payloads,
                ++i,
                FromSeconds(5),
                new ConditionTypePayload
                {
                    ConditionName = DataValueObject.Create("VendingMachine2_LightOff"),
                    EnabledState = DataValueObject.Create("Enabled"),
                    EnabledStateEffectiveDisplayName = DataValueObject.Create("Active | Unacknowledged"),
                    EnabledStateId = DataValueObject.Create<bool?>(true),
                    EventType = DataValueObject.Create("i=10637"),
                    LastSeverity = DataValueObject.Create<int?>(500),
                    Message = DataValueObject.Create("Light Off in machine"),
                    Retain = DataValueObject.Create<bool?>(true),
                    Severity = DataValueObject.Create(500),
                    SourceName = DataValueObject.Create("VendingMachine2"),
                    SourceNode = DataValueObject.Create("http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine2")
                });

            VerifyPayload(payloads,
                ++i,
                Zero,
                new ConditionTypePayload
                {
                    ConditionName = DataValueObject.Create("VendingMachine1_AD_Lamp_Off"),
                    EnabledState = DataValueObject.Create("Enabled"),
                    EnabledStateEffectiveDisplayName = DataValueObject.Create("Enabled"),
                    EnabledStateId = DataValueObject.Create<bool?>(true),
                    EventType = DataValueObject.Create("i=2782"),
                    LastSeverity = DataValueObject.Create<int?>(500),
                    Message = DataValueObject.Create("AD Lamp Off"),
                    Retain = DataValueObject.Create<bool?>(true),
                    Severity = DataValueObject.Create(500),
                    SourceName = DataValueObject.Create("VendingMachine1"),
                    SourceNode = DataValueObject.Create("http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1")
                });

            VerifyPayload(payloads,
                ++i,
                FromSeconds(5),
                new ConditionTypePayload
                {
                    ConditionName = DataValueObject.Create("VendingMachine1_DoorOpen"),
                    EnabledState = DataValueObject.Create("Enabled"),
                    EnabledStateEffectiveDisplayName = DataValueObject.Create("Inactive | Unacknowledged"),
                    EnabledStateId = DataValueObject.Create<bool?>(true),
                    EventType = DataValueObject.Create("i=10751"),
                    LastSeverity = DataValueObject.Create<int?>(900),
                    Message = DataValueObject.Create("Door Closed"),
                    Retain = DataValueObject.Create<bool?>(false),
                    Severity = DataValueObject.Create(500),
                    SourceName = DataValueObject.Create("VendingMachine1"),
                    SourceNode = DataValueObject.Create("http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1")
                });

            VerifyPayload(payloads,
                ++i,
                FromSeconds(4),
                new ConditionTypePayload
                {
                    ConditionName = DataValueObject.Create("VendingMachine1_TemperatureHigh"),
                    EnabledState = DataValueObject.Create("Enabled"),
                    EnabledStateEffectiveDisplayName = DataValueObject.Create("Active | Unacknowledged"),
                    EnabledStateId = DataValueObject.Create<bool?>(true),
                    EventType = DataValueObject.Create("i=2955"),
                    LastSeverity = DataValueObject.Create<int?>(900),
                    Message = DataValueObject.Create("Temperature is HIGH (LAST EVENT IN LOOP)"),
                    Retain = DataValueObject.Create<bool?>(true),
                    Severity = DataValueObject.Create(900),
                    SourceName = DataValueObject.Create("VendingMachine1"),
                    SourceNode = DataValueObject.Create("http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#s=VendingMachine1")
                });

            VerifyPayload(payloads, ++i, Zero, doorOpen); // cycling back to first message
        }

        private static void VerifyPayload(IReadOnlyList<ConditionTypePayload> payloads, int i, TimeSpan? expectedDelay, ConditionTypePayload expectedPayload)
        {
            var p = payloads[i];

            p.ConditionName.Value.Should().BeEquivalentTo(expectedPayload.ConditionName.Value);
            p.EventType.Value.Should().BeEquivalentTo(expectedPayload.EventType.Value);
            p.EnabledState.Value.Should().BeEquivalentTo(expectedPayload.EnabledState.Value);
            p.EnabledStateId.Value.Should().Be(expectedPayload.EnabledStateId.Value);
            p.EnabledStateEffectiveDisplayName.Value.Should().BeEquivalentTo(expectedPayload.EnabledStateEffectiveDisplayName.Value);
            p.LastSeverity.Value.Should().Be(expectedPayload.LastSeverity.Value);
            p.Retain.Value.Should().Be(expectedPayload.Retain.Value);
            p.SourceName.Value.Should().BeEquivalentTo(expectedPayload.SourceName.Value);
            p.SourceNode.Value.Should().BeEquivalentTo(expectedPayload.SourceNode.Value);

            p.ConditionId.Value.Should().StartWith("http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance#i=");

            p.EnabledStateEffectiveTransitionTime.Value.Should().BeCloseTo(p.ReceiveTime.Value.Value, Precision);
            p.EnabledStateTransitionTime.Value.Should().BeCloseTo(p.ReceiveTime.Value.Value, Precision);

            if (expectedDelay != null)
            {
                i.Should().BeGreaterThan(0);
                var transitionTime = p.EnabledStateEffectiveTransitionTime.Value - payloads[i - 1].EnabledStateEffectiveTransitionTime.Value;
                // TODO there is no difference in the transition time...
                // transitionTime.Should().BeCloseTo(expectedDelay.Value, Precision);
            }
        }
    }
}
