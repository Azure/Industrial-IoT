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
    public class TwinReadNodeValueTestTheory
    {
        private readonly TwinTestContext _context;

        public TwinReadNodeValueTestTheory(TwinTestContext context, ITestOutputHelper output)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(0)]
        public async Task TestPrepareAsync()
        {
            await _context.AssertTestEnvironmentPreparedAsync();
        }

        [Theory, PriorityOrder(1)]
        [InlineData("Boolean", "i=13851", true)]
        [InlineData("Byte", "i=2175", true)]
        [InlineData("ByteString", "i=7617", true)]
        [InlineData("DateTime", "i=19729", false)]
        [InlineData("Double", "i=19076", true)]
        [InlineData("Guid", "i=21120", false)]
        [InlineData("Int16", "i=16401", true)]
        [InlineData("Int32", "i=20094", true)]
        [InlineData("LocalizedText", "http://microsoft.com/Opc/OpcPlc/Boiler#i=6196", true)]
        [InlineData("NodeId", "i=2198", false)]
        [InlineData("QualifiedName", "i=2764", false)]
        [InlineData("StatusCode", "i=20085", false)]
        [InlineData("String", "i=2254", true)]
        [InlineData("UInt16", "i=13853", true)]
        [InlineData("UInt32", "http://microsoft.com/Opc/OpcPlc/#s=SlowUInt1", true)]
        [InlineData("UInt64", "i=13850", true)]
        public async Task BasicDataType(string expectedDataType, string nodeId, bool expectValue)
        {
            using var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);

            var (value, dataType) = await TestHelper.Twin.ReadNodeValueAsync(_context, _context.OpcUaEndpointId, nodeId, cts.Token);

            Assert.Equal(expectedDataType, dataType);

            if (expectValue)
            {
                Assert.NotNull(value);
            }
        }

        [Fact, PriorityOrder(2)]
        public async Task ComplexDataType()
        {
            const string expectedDataType = "ExtensionObject";
            const string nodeId = "http://microsoft.com/Opc/OpcPlc/Boiler#i=15013";

            using var cts = new CancellationTokenSource(TestConstants.DefaultTimeoutInMilliseconds);

            var (value, dataType) = await TestHelper.Twin.ReadNodeValueAsync(_context, _context.OpcUaEndpointId, nodeId, cts.Token);

            Assert.Equal(expectedDataType, dataType);

            Assert.NotNull(value);

            Assert.True(TestHelper.HasProperty(value, "TypeId"), "Unexpected data structure");
            Assert.NotNull(value.TypeId);
            Assert.Equal("http://microsoft.com/Opc/OpcPlc/Boiler#i=15032", value.TypeId);

            Assert.True(TestHelper.HasProperty(value, "Encoding"), "Unexpected data structure");
            Assert.NotNull(value.Encoding);
            Assert.Equal("Json", value.Encoding);

            Assert.True(TestHelper.HasProperty(value, "Body"), "Unexpected data structure");
            Assert.NotNull(value.Body);

            Assert.True(TestHelper.HasProperty(value.Body, "Temperature"), "Unexpected data structure");
            Assert.True(TestHelper.HasProperty(value.Body.Temperature, "Top"), "Unexpected data structure");
            Assert.True(TestHelper.HasProperty(value.Body.Temperature, "Bottom"), "Unexpected data structure");
            Assert.True(TestHelper.HasProperty(value.Body, "Pressure"), "Unexpected data structure");
            Assert.True(TestHelper.HasProperty(value.Body, "HeaterState"), "Unexpected data structure");
            Assert.Equal(1, value.Body.HeaterState);
        }
    }
}
