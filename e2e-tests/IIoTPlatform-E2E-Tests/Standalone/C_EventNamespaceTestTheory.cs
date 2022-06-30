// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Newtonsoft.Json.Linq;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    public class C_EventNamespaceTestTheory : DynamicAciTestBase {
        public C_EventNamespaceTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output)
            : base(context, output) {
        }

        [Fact, PriorityOrder(10)]
        public Task Test_DeployAci() {
            return TestHelper.CreateSimulationContainerAsync(_context, new List<string> { "/bin/sh", "-c", "./opcplc --autoaccept --ses --pn=50000" }, _timeoutToken);
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

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, pnJson, _timeoutToken).ConfigureAwait(false);
            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemCycleStatusEventTypePayload>(_writerId, _timeoutToken);

            // Act
            var payloads = await messages.FirstAsync(_timeoutToken);

            // Assert
            VerifyPayloads(payloads.Messages);
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

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, pnJson, _timeoutToken).ConfigureAwait(false);
            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemCycleStatusEventTypePayload>(_writerId, _timeoutToken);

            // Act
            var payloads = await messages.FirstAsync(_timeoutToken);

            // Assert
            VerifyPayloads(payloads.Messages);
        }

        [Fact, PriorityOrder(13)]
        public async Task Test_VerifyIntegerNamespace_Expect_FilteredSimpleEvents_InHub() {
            // Arrange
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("ns=5;i=2"));

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, pnJson, _timeoutToken).ConfigureAwait(false);
            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemCycleStatusEventTypePayload>(_writerId, _timeoutToken);

            // Act
            var payloads = await messages.FirstAsync(_timeoutToken);

            // Assert
            VerifyPayloads(payloads.Messages);
        }

        [Fact, PriorityOrder(14)]
        public async Task Test_VerifyStringNamespace_Expect_FilteredSimpleEvents_InHub() {
            // Arrange
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.SimpleEventFilter("nsu=http://microsoft.com/Opc/OpcPlc/SimpleEvents;i=2"));

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(_context, TestConstants.PublishedNodesFullName, pnJson, _timeoutToken).ConfigureAwait(false);
            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemCycleStatusEventTypePayload>(_writerId, _timeoutToken);

            // Act
            var payloads = await messages.FirstAsync(_timeoutToken);

            // Assert
            VerifyPayloads(payloads.Messages);
        }

        private static void VerifyPayloads(PubSubMessages<SystemCycleStatusEventTypePayload> payloads) {
            foreach (var payload in payloads.Select(x => x.Value)) {
                payload.Message.Should().Match("The system cycle '*' has started.");
                payload.CycleId.Should().MatchRegex("^\\d+$");
            }
        }

        private string SimpleEvents(string messageTypeDefinitionId, string messageBrowsePath, string cycleIdDefinitionId, string cycleIdBrowsePath, string filterTypeDefinitionId) {
            return _context.PublishedNodesJson(
                50000,
                _writerId,
                new JArray(
                            new JObject(
                              new JProperty("Id", "ns=0;i=2253"),
                              new JProperty("DisplayName", "SimpleEvents"),
                              new JProperty("QueueSize", 10),
                              new JProperty("EventFilter", new JObject(
                                new JProperty("SelectClauses", new JArray(
                                    new JObject(
                                        new JProperty("TypeDefinitionId", messageTypeDefinitionId),
                                        new JProperty("BrowsePath", new JArray(
                                            new JValue(messageBrowsePath)))),
                                    new JObject(
                                        new JProperty("TypeDefinitionId", cycleIdDefinitionId),
                                        new JProperty("BrowsePath", new JArray(
                                            new JValue(cycleIdBrowsePath)))))),
                                new JProperty("WhereClause", new JObject(
                                    new JProperty("Elements", new JArray(
                                        new JObject(
                                            new JProperty("FilterOperator", new JValue("OfType")),
                                            new JProperty("FilterOperands", new JArray(
                                                new JObject(
                                                    new JProperty("Value", filterTypeDefinitionId))))))))))))));
        }
    }
}