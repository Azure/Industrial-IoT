// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Twin {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection(TwinTestCollection.CollectionName)]
    [Trait(TestConstants.TraitConstants.TwinModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class TwinReadTestTheory {
        private readonly TwinTestContext _context;

        public TwinReadTestTheory(TwinTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(0)]
        public void ReadAttributes() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var attributes = new List<object> {
                new {
                    nodeId = "http://microsoft.com/Opc/OpcPlc/#s=FastUInt1",
                    attribute = "DisplayName"
                },
                new {
                    nodeId = "i=2253",
                    attribute = "BrowseName"
                }
            };

            var response = TestHelper.Twin.ReadNodeAttributesAsync(_context, _context.OpcUaEndpointId, attributes, cts.Token).GetAwaiter().GetResult();
            Assert.Equal("FastUInt1", response.results[0].value.Text.ToString());
            Assert.Equal("Server", response.results[1].value.ToString());
        }
    }
}
