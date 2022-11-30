// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.Standalone {
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
    [Collection("IIoT Standalone Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class C_PublishEventsTestTheory : DynamicAciTestBase {

        public C_PublishEventsTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
            : base(context, output) {
        }

        [Fact, PriorityOrder(10)]
        public async Task Test_VerifyDataAvailableAtIoTHub_Expect_AllEvents_ToBeSame() {
            var messages = _consumer.ReadMessagesFromWriterIdAsync<SystemCycleStatusEventTypePayload>(_writerId, _timeoutToken);

            var plcUrl = _context.OpcPlcConfig.Urls.Split(";")[0];
            var pnJson = TestConstants.PublishedNodesConfigurations.SimpleEvents(plcUrl, 50000, _writerId);

            await TestHelper.SwitchToStandaloneModeAndPublishNodesAsync(pnJson, _context, _timeoutToken);

            // Read one payload from IoT Hub
            var firstMessage = await messages
                .FirstAsync(_timeoutToken);

            var data = firstMessage.Messages["SimpleEvents"];
            Assert.NotEmpty(data.EventId);
            Assert.StartsWith("The system cycle '", data.Message);
            Assert.EndsWith("' has started.", data.Message);
            Assert.NotEmpty(data.CycleId);
            Assert.StartsWith("Step ", data.CurrentStep.Name);
            Assert.NotEqual(0, data.CurrentStep.Duration);
        }
    }
}