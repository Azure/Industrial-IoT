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
    public class TwinWriteTestTheory {
        private readonly TwinTestContext _context;

        public TwinWriteTestTheory(TwinTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(0)]
        public void WriteAttributes() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // The attribute that will be overwritten
            var attribute = "DisplayName";
            var nodeId = "http://microsoft.com/Opc/OpcPlc/#s=FastUInt1";

            var attributesToRead = new List<object> {
                new {
                    nodeId,
                    attribute
                }
            };

            // Read the current value of the attribute, expected "FastUInt1"
            var response = TestHelper.Twin.ReadNodeAttributesAsync(_context, _context.OpcUaEndpointId, attributesToRead, cts.Token).GetAwaiter().GetResult();
            Assert.Equal("FastUInt1", response.results[0].value.Text);

            // Write the new value "TestName"
            var attributesToWrite = new List<object> {
                new {
                    nodeId,
                    attribute,
                    value = "TestName"
                }
            };
            TestHelper.Twin.WriteNodeAttributesAsync(_context, _context.OpcUaEndpointId, attributesToWrite, cts.Token).GetAwaiter().GetResult();

            // Read the value of the attribute again, expected the updated value "TestName"
            response = TestHelper.Twin.ReadNodeAttributesAsync(_context, _context.OpcUaEndpointId, attributesToRead, cts.Token).GetAwaiter().GetResult();
            Assert.Equal("TestName", response.results[0].value.Text);

            // Write the original value again
            attributesToWrite = new List<object> {
                new {
                    nodeId,
                    attribute,
                    value = "FastUInt1"
                }
            };
            TestHelper.Twin.WriteNodeAttributesAsync(_context, _context.OpcUaEndpointId, attributesToWrite, cts.Token).GetAwaiter().GetResult();
        }
    }
}
