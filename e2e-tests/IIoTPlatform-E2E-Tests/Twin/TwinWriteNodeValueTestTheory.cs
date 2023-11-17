// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Twin
{
    using IIoTPlatformE2ETests.TestExtensions;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection(TwinTestCollection.CollectionName)]
    [Trait(TestConstants.TraitConstants.TwinModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class TwinWriteNodeValueTestTheory
    {
        private readonly TwinTestContext _context;

        public TwinWriteNodeValueTestTheory(TwinTestContext context, ITestOutputHelper output)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(0)]
        public async Task TestPrepareAsync()
        {
            await _context.AssertTestEnvironmentPreparedAsync();
        }

        [Fact, PriorityOrder(1)]
        public async Task BasicDataType()
        {
            const string expectedDataType = "Boolean";
            const string nodeId = "i=2025";

            using var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);

            // Don't check the value before first writing it
            await TestHelper.Twin.WriteNodeValueAsync(_context, _context.OpcUaEndpointId, nodeId, true, expectedDataType, cts.Token);

            // Check that the value has been set
            var (value, dataType) = await TestHelper.Twin.ReadNodeValueAsync(_context, _context.OpcUaEndpointId, nodeId, cts.Token);
            Assert.Equal(expectedDataType, dataType);
            Assert.True(value);

            // Change value
            await TestHelper.Twin.WriteNodeValueAsync(_context, _context.OpcUaEndpointId, nodeId, false, expectedDataType, cts.Token);

            // Check that the value has been set
            (value, _) = await TestHelper.Twin.ReadNodeValueAsync(_context, _context.OpcUaEndpointId, nodeId, cts.Token);
            Assert.False(value);
        }
    }
}
