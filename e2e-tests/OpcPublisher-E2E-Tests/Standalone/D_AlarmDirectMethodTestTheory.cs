// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.Standalone {
    using FluentAssertions;
    using OpcPublisher_AE_E2E_Tests.TestExtensions;
    using OpcPublisher_AE_E2E_Tests.TestModels;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public class D_AlarmDirectMethodTestTheory : DynamicAciTestBase, IClassFixture<IIoTStandaloneTestContext> {
        public D_AlarmDirectMethodTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
        : base(context, output) {
        }

        [Fact, PriorityOrder(10)]
        public async void Test_VerifyDataAvailableAtIoTHub_Expect_PendingConditionsView() {

            // Arrange
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --alm --pn=50000"},
                _timeoutToken);

            var messages = _consumer.ReadConditionMessagesFromWriterIdAsync<ConditionTypePayload>(_writerId, _timeoutToken, 1);

            // Act
            var pnJson = _context.PublishedNodesJson(
                50000,
                _writerId,
                TestConstants.PublishedNodesConfigurations.PendingConditionForAlarmsView());
            await PublishNodesAsync(pnJson, _timeoutToken);

            // take any message
            var payloads = await messages.Select(v => v.Payload).ToListAsync(_timeoutToken);
            await UnpublishAllNodesAsync(_timeoutToken);

            // Assert
            ValidatePendingConditionsView(payloads);
        }
    }
}
