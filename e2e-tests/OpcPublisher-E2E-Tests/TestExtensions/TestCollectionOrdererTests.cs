// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using System;
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public sealed class TestCollectionOrdererTests
    {
        [Fact]
        public void EventStressRunsFirstWithoutReorderingOtherCollections()
        {
            var first = new TestCollection("first");
            var stress = new TestCollection(
                "OpcPublisherAEE2ETests.Standalone.CEventsStressTestTheory");
            var last = new TestCollection("last");

            var ordered = new TestCollectionOrderer()
                .OrderTestCollections([first, stress, last])
                .ToArray();

            Assert.Same(stress, ordered[0]);
            Assert.Same(first, ordered[1]);
            Assert.Same(last, ordered[2]);
        }

        private sealed class TestCollection :
            LongLivedMarshalByRefObject, ITestCollection
        {
            public TestCollection()
                : this(string.Empty)
            {
            }

            public TestCollection(string displayName)
            {
                DisplayName = displayName;
                UniqueID = Guid.NewGuid();
            }

            public ITypeInfo CollectionDefinition => null;

            public string DisplayName { get; }

            public ITestAssembly TestAssembly => null;

            public Guid UniqueID { get; }

            public void Deserialize(IXunitSerializationInfo info)
            {
            }

            public void Serialize(IXunitSerializationInfo info)
            {
            }
        }
    }
}
