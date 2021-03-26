// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Twin {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using System;
    using System.Threading;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection(TwinTestCollection.CollectionName)]
    [Trait(TestConstants.TraitConstants.TwinModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class TwinWriteNodeValueTestTheory {
        private readonly TwinTestContext _context;

        public TwinWriteNodeValueTestTheory(TwinTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(0)]
        public void BasicDataType() {
            const string expectedDataType = "Boolean";
            const string nodeId = "i=2025";

            using var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);

            // Don't check the value before first writing it
            TestHelper.Twin.WriteNodeValue(_context, _context.OpcUaEndpointId, nodeId, true, expectedDataType, cts.Token).GetAwaiter().GetResult();

            // Check that the value has been set
            var (value, dataType) = TestHelper.Twin.ReadNodeValue(_context, _context.OpcUaEndpointId, nodeId, cts.Token).GetAwaiter().GetResult();
            Assert.Equal(expectedDataType, dataType);
            Assert.True(value);

            // Change value
            TestHelper.Twin.WriteNodeValue(_context, _context.OpcUaEndpointId, nodeId, false, expectedDataType, cts.Token).GetAwaiter().GetResult();

            // Check that the value has been set
            (value, _) = TestHelper.Twin.ReadNodeValue(_context, _context.OpcUaEndpointId, nodeId, cts.Token).GetAwaiter().GetResult();
            Assert.False(value);
        }
    }
}
