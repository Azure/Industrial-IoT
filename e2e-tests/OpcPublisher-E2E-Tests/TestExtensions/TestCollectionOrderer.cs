// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Runs the clean-state-sensitive event stress collection first while
    /// retaining xUnit's discovered order for every other collection.
    /// </summary>
    public sealed class TestCollectionOrderer : ITestCollectionOrderer
    {
        public const string FullName =
            "OpcPublisherAEE2ETests.TestExtensions.TestCollectionOrderer";

        public IEnumerable<ITestCollection> OrderTestCollections(
            IEnumerable<ITestCollection> testCollections)
        {
            var collections = testCollections.ToList();
            foreach (var collection in collections.Where(IsEventStress))
            {
                yield return collection;
            }
            foreach (var collection in collections.Where(
                collection => !IsEventStress(collection)))
            {
                yield return collection;
            }
        }

        private static bool IsEventStress(ITestCollection collection)
        {
            return collection.DisplayName.Contains(
                "CEventsStressTestTheory", StringComparison.Ordinal);
        }
    }
}
