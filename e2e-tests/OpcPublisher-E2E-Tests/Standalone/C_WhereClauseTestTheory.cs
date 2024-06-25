// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.Standalone
{
    using OpcPublisherAEE2ETests.TestExtensions;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class CWhereClauseTestTheory : DynamicAciTestBase, IClassFixture<IIoTStandaloneTestContext>
    {
        public CWhereClauseTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
            : base(context, output)
        {
        }

        [Fact, PriorityOrder(10)]
        public async Task TestVerifyDataAvailableAtIoTHubExpectFieldsToMatchSimpleFilter()
        {
            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadMessagesFromWriterIdAsync(_writerId, -1, null, _timeoutToken);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("i=2041")); // OPC-UA BaseEventType
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);

            // take any message
            var (_, _, payload, _) = await messages.FirstAsync(_timeoutToken);

            // Assert
            ValidateBaseEventTypeFields(payload);
        }

        [Fact, PriorityOrder(11)]
        public async Task TestVerifyDataAvailableAtIoTHubExpectFieldsToMatchEventFilter()
        {
            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadMessagesFromWriterIdAsync(_writerId, -1, null, _timeoutToken);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("i=2041")); // OPC-UA BaseEventType
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);
            // take any message
            var (_, _, payload, _) = await messages
                .FirstAsync(_timeoutToken);

            // Assert
            ValidateBaseEventTypeFields(payload);
        }

        [Fact, PriorityOrder(12)]
        public async Task TestVerifyDataAvailableAtIoTHubExpectFieldsToSimpleEvents()
        {
            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadMessagesFromWriterIdAsync(_writerId, -1, null, _timeoutToken);

            // Act
            var pnJson = TestConstants.PublishedNodesConfigurations.SimpleEvents(_context.PlcAciDynamicUrls[0],
                50000,
                _writerId);
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);
            // take any message
            var (_, _, payload, _) = await messages
                .FirstAsync(_timeoutToken);

            // Assert
            ValidateSimpleEventFields(payload);
        }

        private static void ValidateBaseEventTypeFields(JObject ev)
        {
            // navigate to the event fields (nested several layers)
            var fields = ev.Children().ToList();
            Assert.Equal(13, fields.Count);
            Assert.Contains(fields, x => x.Path.EndsWith("EventId", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("EventType", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("SourceNode", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("SourceName", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("Time", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("ReceiveTime", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("LocalTime", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("Message", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("Severity", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("ConditionClassId", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("ConditionClassName", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("ConditionSubClassId", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("ConditionSubClassName", StringComparison.Ordinal));
        }

        private static void ValidateSimpleEventFields(JObject ev)
        {
            // navigate to the event fields (nested several layers)
            var fields = ev.Children();
            Assert.Equal(4, fields.Count());
            Assert.Contains(fields, x => x.Path.EndsWith("EventId", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.EndsWith("Message", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.Contains("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CycleId", StringComparison.Ordinal));
            Assert.Contains(fields, x => x.Path.Contains("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CurrentStep", StringComparison.Ordinal));
        }
    }
}
