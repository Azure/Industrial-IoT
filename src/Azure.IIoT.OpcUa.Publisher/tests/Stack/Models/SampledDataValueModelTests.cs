// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using System;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SampledDataValueModel"/>.
    /// </summary>
    public sealed class SampledDataValueModelTests
    {
        // ── Constructor ────────────────────────────────────────────────────────

        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var value = new DataValue(new Variant(42));
            var sut = new SampledDataValueModel(value, 7u, 3);

            Assert.Equal(value, sut.Value);
            Assert.Equal(7u, sut.ClientHandle);
            Assert.Equal(3, sut.Overflow);
        }

        [Fact]
        public void Constructor_DefaultDataValue_SetsEmptyValue()
        {
            var sut = new SampledDataValueModel(new DataValue(), 0u, 0);

            Assert.Equal(0u, sut.ClientHandle);
            Assert.Equal(0, sut.Overflow);
        }

        // ── TypeId / encoding ids ──────────────────────────────────────────────

        [Fact]
        public void TypeId_IsNullNodeId()
        {
            var sut = new SampledDataValueModel(new DataValue(), 1u, 0);
            Assert.Equal(ExpandedNodeId.Null, sut.TypeId);
        }

        [Fact]
        public void BinaryEncodingId_IsNullNodeId()
        {
            var sut = new SampledDataValueModel(new DataValue(), 1u, 0);
            Assert.Equal(ExpandedNodeId.Null, sut.BinaryEncodingId);
        }

        [Fact]
        public void XmlEncodingId_IsNullNodeId()
        {
            var sut = new SampledDataValueModel(new DataValue(), 1u, 0);
            Assert.Equal(ExpandedNodeId.Null, sut.XmlEncodingId);
        }

        // ── Clone ──────────────────────────────────────────────────────────────

        [Fact]
        public void Clone_ReturnsSameValues()
        {
            var value = new DataValue(new Variant("hello"));
            var original = new SampledDataValueModel(value, 99u, 5);

            var clone = (SampledDataValueModel)original.Clone();

            Assert.Equal(original.ClientHandle, clone.ClientHandle);
            Assert.Equal(original.Overflow, clone.Overflow);
            Assert.True(original.IsEqual(clone));
        }

        [Fact]
        public void Clone_ReturnsNewInstance()
        {
            var original = new SampledDataValueModel(new DataValue(), 1u, 0);

            var clone = original.Clone();

            Assert.NotSame(original, clone);
        }

        // ── IsEqual ────────────────────────────────────────────────────────────

        [Fact]
        public void IsEqual_SameValues_ReturnsTrue()
        {
            var value = new DataValue(new Variant(123));
            var a = new SampledDataValueModel(value, 10u, 2);
            var b = new SampledDataValueModel(value, 10u, 2);

            Assert.True(a.IsEqual(b));
        }

        [Fact]
        public void IsEqual_DifferentClientHandle_ReturnsFalse()
        {
            var value = new DataValue(new Variant(1));
            var a = new SampledDataValueModel(value, 10u, 0);
            var b = new SampledDataValueModel(value, 99u, 0);

            Assert.False(a.IsEqual(b));
        }

        [Fact]
        public void IsEqual_DifferentOverflow_ReturnsFalse()
        {
            var value = new DataValue(new Variant(1));
            var a = new SampledDataValueModel(value, 10u, 0);
            var b = new SampledDataValueModel(value, 10u, 5);

            Assert.False(a.IsEqual(b));
        }

        [Fact]
        public void IsEqual_DifferentType_ReturnsFalse()
        {
            var sut = new SampledDataValueModel(new DataValue(), 1u, 0);
            // A MonitoredItemNotification is also IEncodeable but not a SampledDataValueModel
            Assert.False(sut.IsEqual(new MonitoredItemNotification()));
        }

        // ── Decode / Encode throw ──────────────────────────────────────────────

        [Fact]
        public void Decode_ThrowsNotSupportedException()
        {
            var sut = new SampledDataValueModel(new DataValue(), 1u, 0);
            Assert.Throws<NotSupportedException>(() =>
                sut.Decode(null!));
        }

        [Fact]
        public void Encode_ThrowsNotSupportedException()
        {
            var sut = new SampledDataValueModel(new DataValue(), 1u, 0);
            Assert.Throws<NotSupportedException>(() =>
                sut.Encode(null!));
        }
    }
}
