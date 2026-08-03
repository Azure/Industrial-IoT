// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Linq
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Xunit;

    public sealed class LinqExTests
    {
        [Fact]
        public void Batch_NonPositiveCount_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new[] { 1 }.Batch(0).ToList());

            Assert.Equal("Cannot create 0 or negative size batches", exception.Message);
        }

        [Fact]
        public void Batch_PositiveCount_ReturnsExpectedBatches()
        {
            var batches = new[] { 1, 2, 3, 4, 5 }
                .Batch(2)
                .Select(batch => batch.ToArray())
                .ToArray();

            Assert.Equal(3, batches.Length);
            Assert.Equal([1, 2], batches[0]);
            Assert.Equal([3, 4], batches[1]);
            Assert.Equal([5], batches[2]);
        }

        [Fact]
        public void MergeWith_NullSourceAndNonEmptyItems_ReturnsNewSet()
        {
            IReadOnlySet<int>? source = null;

            var result = source.MergeWith([1, 2]);

            Assert.Equal(new HashSet<int> { 1, 2 }, result);
        }

        [Fact]
        public void MergeWith_ExistingSourceAndNonEmptyItems_ReturnsUnion()
        {
            IReadOnlySet<int> source = new HashSet<int> { 1, 2 };

            var result = source.MergeWith([2, 3]);

            Assert.Equal(new HashSet<int> { 1, 2, 3 }, result);
        }

        [Fact]
        public void MergeWith_NullOrEmptyItems_ReturnsOriginalSource()
        {
            IReadOnlySet<int> source = new HashSet<int> { 1 };

            Assert.Same(source, source.MergeWith(null));
            Assert.Same(source, source.MergeWith([]));
        }

        [Fact]
        public void ToHashSetSafe_NullEnumerable_ReturnsNull()
        {
            Assert.Null(((IEnumerable<int>?)null).ToHashSetSafe());
        }

        [Fact]
        public void ToHashSetSafe_Enumerable_ReturnsHashSet()
        {
            var result = new[] { 1, 1, 2 }.ToHashSetSafe();

            Assert.Equal(new HashSet<int> { 1, 2 }, result);
        }

        [Fact]
        public void YieldReturn_ReturnsSingleValue()
        {
            var result = 42.YieldReturn();

            Assert.Equal([42], result);
        }

        [Fact]
        public void Flatten_NestedEnumerable_ReturnsFirstNestedSequenceFlattened()
        {
            IEnumerable nested = new object[]
            {
                new object[] { 1, new[] { 2, 3 } },
                4
            };

            var result = nested.Flatten().Cast<object>().ToArray();

            Assert.Equal([1, 2, 3], result);
        }

        [Fact]
        public void Flatten_FlatEnumerable_ReturnsAllItems()
        {
            IEnumerable flat = new object[] { 1, 2 };

            var result = flat.Flatten().Cast<object>().ToArray();

            Assert.Equal([1, 2], result);
        }
    }
}
