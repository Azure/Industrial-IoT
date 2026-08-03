// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic
{
    using System;
    using Xunit;

    public sealed class EnumerableExTests
    {
        [Fact]
        public void SequenceGetHashSafe_NullSequence_ReturnsSeedHash()
        {
            var hash = ((IEnumerable<string>?)null).SequenceGetHashSafe();

            Assert.Equal(-932366343, hash);
        }

        [Fact]
        public void SequenceGetHashSafe_WithCustomHash_UsesItemsInOrder()
        {
            var hash = new[] { 1, 2 }.SequenceGetHashSafe(value => value);
            var reverseHash = new[] { 2, 1 }.SequenceGetHashSafe(value => value);

            Assert.NotEqual(reverseHash, hash);
        }

        [Fact]
        public void SequenceEqualsSafe_SameReference_ReturnsTrue()
        {
            IEnumerable<int> values = [1, 2];

            Assert.True(values.SequenceEqualsSafe(values));
        }

        [Fact]
        public void SequenceEqualsSafe_NullAndEmpty_ReturnsTrue()
        {
            Assert.True(((IEnumerable<int>?)null).SequenceEqualsSafe(Array.Empty<int>()));
        }

        [Fact]
        public void SequenceEqualsSafe_NullAndNonEmpty_ReturnsFalse()
        {
            Assert.False(((IEnumerable<int>?)null).SequenceEqualsSafe([1]));
        }

        [Fact]
        public void SequenceEqualsSafe_WithComparer_UsesComparer()
        {
            var left = new[] { "A" };
            var right = new[] { "a" };

            Assert.True(left.SequenceEqualsSafe(right,
                (x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void SetEqualsSafe_SameReference_ReturnsTrue()
        {
            IEnumerable<int> values = [1, 2];

            Assert.True(values.SetEqualsSafe(values));
        }

        [Fact]
        public void SetEqualsSafe_OneNull_ReturnsFalse()
        {
            Assert.False(((IEnumerable<int>?)null).SetEqualsSafe([1]));
        }

        [Fact]
        public void SetEqualsSafe_SetImplementation_UsesSetSemantics()
        {
            var left = new HashSet<int> { 1, 2 };
            var right = new[] { 2, 1 };

            Assert.True(left.SetEqualsSafe(right));
        }

        [Fact]
        public void SetEqualsSafe_WithComparer_RequiresBothDirections()
        {
            var left = new[] { "A" };
            var right = new[] { "a" };

            Assert.True(left.SetEqualsSafe(right,
                (x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase)));
            Assert.False(left.SetEqualsSafe(["b"],
                (x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void ForEach_ExecutesActionForEveryItem()
        {
            var values = new List<int>();

            new[] { 1, 2 }.ForEach(values.Add);

            Assert.Equal([1, 2], values);
        }

        [Fact]
        public void AddRange_AddsAllItems()
        {
            var values = new List<int> { 1 };

            EnumerableEx.AddRange(values, [2, 3]);

            Assert.Equal([1, 2, 3], values);
        }

        [Fact]
        public void AddOrUpdate_AddsAndReplacesDictionaryValue()
        {
            var values = new Dictionary<string, int>();

            values.AddOrUpdate("key", 1);
            values.AddOrUpdate("key", 2);

            Assert.Equal(2, values["key"]);
        }

        [Fact]
        public void CompareUsing_UsesProvidedDelegate()
        {
            var comparer = Compare.Using<string>(
                (x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase));

            // Exercise the supplied equality delegate and the explicit null hash path.
            Assert.True(comparer.Equals("A", "a"));
            Assert.False(comparer.Equals("A", "b"));
            Assert.Equal(0, comparer.GetHashCode(null!));
        }
    }
}
