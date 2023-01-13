// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.Standalone {
    using OpcPublisher_AE_E2E_Tests.TestExtensions;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class C_WhereClauseTestTheory : DynamicAciTestBase, IClassFixture<IIoTStandaloneTestContext> {
        public C_WhereClauseTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
            : base(context, output) {
        }

        [Fact, PriorityOrder(10)]
        public async void Test_VerifyDataAvailableAtIoTHub_Expect_FieldsToMatchSimpleFilter() {

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadMessagesFromWriterIdAsync(_writerId, _timeoutToken, -1);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("i=2041")); // OPC-UA BaseEventType
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);

            // take any message
            var ev = await messages
                .FirstAsync(_timeoutToken);

            // Assert
            ValidateBaseEventTypeFields(ev.payload);
        }

        [Fact, PriorityOrder(11)]
        public async void Test_VerifyDataAvailableAtIoTHub_Expect_FieldsToMatchEventFilter() {

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadMessagesFromWriterIdAsync(_writerId, _timeoutToken, -1);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("i=2041")); // OPC-UA BaseEventType
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);
            // take any message
            var ev = await messages
                .FirstAsync(_timeoutToken);

            // Assert
            ValidateBaseEventTypeFields(ev.payload);
        }

        [Fact, PriorityOrder(12)]
        public async void Test_VerifyDataAvailableAtIoTHub_Expect_FieldsToSimpleEvents() {

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadMessagesFromWriterIdAsync(_writerId, _timeoutToken, -1);

            // Act
            var pnJson = TestConstants.PublishedNodesConfigurations.SimpleEvents(_context.PlcAciDynamicUrls[0],
                50000,
                _writerId);
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);
            // take any message
            var ev = await messages
                .FirstAsync(_timeoutToken);

            // Assert
            ValidateSimpleEventFields(ev.payload);
        }

        private static void ValidateBaseEventTypeFields(JToken ev) {
            // navigate to the event fields (nested several layers)
            var fields = ev.Children();
            Assert.NotNull(fields);
            Assert.Equal(9, fields.Count());
            Assert.True(fields.Where(x => x.Path.EndsWith("EventId")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("EventType")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("Message")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("ReceiveTime")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("Severity")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("SourceNode")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("SourceName")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("Time")).Any());
        }

        private static void ValidateSimpleEventFields(JToken ev) {
            // navigate to the event fields (nested several layers)
            var fields = ev.Children();
            Assert.NotNull(fields);
            Assert.Equal(4, fields.Count());
            Assert.True(fields.Where(x => x.Path.EndsWith("EventId")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("Message")).Any());
            Assert.True(fields.Where(x => x.Path.Contains("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CycleId")).Any());
            Assert.True(fields.Where(x => x.Path.Contains("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CurrentStep")).Any());
        }
    }
}
