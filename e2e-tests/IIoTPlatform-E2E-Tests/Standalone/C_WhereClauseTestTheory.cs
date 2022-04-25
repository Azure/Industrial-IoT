// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    public class C_WhereClauseTestTheory : DynamicAciTestBase {
        public C_WhereClauseTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
            : base(context, output) {
        }

        [Fact, PriorityOrder(10)]
        public async void Test_VerifyDataAvailableAtIoTHub_Expect_FieldsToMatchSimpleFilter() {

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadMessagesFromWriterIdAsync(_writerId, _timeoutToken);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("i=2041")); // OPC-UA BaseEventType
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, pnJson, _timeoutToken);

            // take any message
            var ev = await messages
                .FirstAsync(_timeoutToken);

            // Assert
            ValidateBaseEventTypeFields(ev.messages);
        }

        [Fact, PriorityOrder(11)]
        public async void Test_VerifyDataAvailableAtIoTHub_Expect_FieldsToMatchEventFilter() {

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadMessagesFromWriterIdAsync(_writerId, _timeoutToken);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("i=2041")); // OPC-UA BaseEventType
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, pnJson, _timeoutToken);
            // take any message
            var ev = await messages
                .FirstAsync(_timeoutToken);

            // Assert
            ValidateBaseEventTypeFields(ev.messages);
        }

        [Fact, PriorityOrder(12)]
        public async void Test_VerifyDataAvailableAtIoTHub_Expect_FieldsToSimpleEvents() {

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadMessagesFromWriterIdAsync(_writerId, _timeoutToken);

            // Act
            var pnJson = TestConstants.PublishedNodesConfigurations.SimpleEvents(_context.PlcAciDynamicUrls[0],
                50000,
                _writerId);
            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, pnJson, _timeoutToken);
            // take any message
            var ev = await messages
                .FirstAsync(_timeoutToken);

            // Assert
            ValidateSimpleEventFields(ev.messages);
        }

        private void ValidateBaseEventTypeFields(JToken ev) {
            // navigate to the event fields (nested several layers)
            var fields = ev.Children()
                .First()
                    .Children()
                        .First()
                            .Children();
            Assert.NotNull(fields);
            Assert.Equal(8, fields.Count());
            Assert.True(fields.Where(x => x.Path.EndsWith("EventId")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("EventType")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("Message")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("ReceiveTime")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("Severity")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("SourceNode")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("SourceName")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("Time")).Any());
        }

        private void ValidateSimpleEventFields(JToken ev) {
            // navigate to the event fields (nested several layers)
            var fields = ev.Children()
                .First()
                    .Children()
                        .First()
                            .Children();
            Assert.NotNull(fields);
            Assert.Equal(4, fields.Count());
            Assert.True(fields.Where(x => x.Path.EndsWith("EventId")).Any());
            Assert.True(fields.Where(x => x.Path.EndsWith("Message")).Any());
            Assert.True(fields.Where(x => x.Path.Contains("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CycleId")).Any());
            Assert.True(fields.Where(x => x.Path.Contains("http://microsoft.com/Opc/OpcPlc/SimpleEvents#CurrentStep")).Any());
        }
    }
}
