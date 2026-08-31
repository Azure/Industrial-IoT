// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Extensions
{
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="DataValueEx.GetValueOrDefaultEx{T}"/>.
    /// Note: <see cref="DataValue"/> is a readonly struct so it cannot be null.
    /// The null-guard code path in the extension method is unreachable; we only
    /// test the code paths that are actually reachable.
    /// </summary>
    public class DataValueExTests
    {
        // ─────────────────────── Variant.Null value ───────────────────────

        [Fact]
        public void DataValueWithNullVariant_ReturnsDefault()
        {
            // new DataValue() gives WrappedValue = Variant.Null → Value returns null
            var dv = new DataValue();
            var result = dv.GetValueOrDefaultEx<string>("fallback");
            Assert.Equal("fallback", result);
        }

        [Fact]
        public void DataValueWithNullVariant_ReturnsNullWhenDefaultIsNull()
        {
            var dv = new DataValue();
            var result = dv.GetValueOrDefaultEx<string?>(null);
            Assert.Null(result);
        }

        // ─────────────────────── direct cast ───────────────────────

        [Fact]
        public void DataValueWithStringVariant_ReturnsString()
        {
            var dv = new DataValue(new Variant("hello"));
            var result = dv.GetValueOrDefaultEx<string>(null);
            Assert.Equal("hello", result);
        }

        [Fact]
        public void DataValueWithIntVariant_ReturnsInt()
        {
            var dv = new DataValue(42);
            var result = dv.GetValueOrDefaultEx<int>(0);
            Assert.Equal(42, result);
        }

        [Fact]
        public void DataValueWithBoolVariant_ReturnsBool()
        {
            var dv = new DataValue(new Variant(true));
            var result = dv.GetValueOrDefaultEx<bool>(false);
            Assert.True(result);
        }

        // ─────────────────────── enum type ───────────────────────

        [Fact]
        public void DataValueWithEnumIntVariant_ReturnsEnum()
        {
            // int value 1 → DayOfWeek.Monday via Enum.ToObject
            var dv = new DataValue(1);
            var result = dv.GetValueOrDefaultEx<DayOfWeek>(DayOfWeek.Sunday);
            Assert.Equal(DayOfWeek.Monday, result);
        }

        [Fact]
        public void DataValueWithStringVariant_EnumType_ReturnsFallback()
        {
            // A string cannot be converted to enum via Enum.ToObject → fallback
            var dv = new DataValue(new Variant("Monday"));
            var result = dv.GetValueOrDefaultEx<DayOfWeek>(DayOfWeek.Sunday);
            Assert.Equal(DayOfWeek.Sunday, result);
        }

        // ─────────────────────── incompatible type ───────────────────────

        [Fact]
        public void DataValueWithIntVariant_StringRequested_ReturnsFallback()
        {
            // int boxed value cannot be cast to string → returns fallback
            var dv = new DataValue(99);
            var result = dv.GetValueOrDefaultEx<string>("fallback");
            Assert.Equal("fallback", result);
        }

        // ─────────────────────── convert overload ───────────────────────

        [Fact]
        public void ConvertOverload_TransformsExtractedValue()
        {
            var dv = new DataValue(new Variant("hello"));
            var result = dv.GetValueOrDefaultEx<string>(s => s?.ToUpperInvariant(), null);
            Assert.Equal("HELLO", result);
        }

        [Fact]
        public void ConvertOverload_NullVariant_ReturnsConvertedDefault()
        {
            var dv = new DataValue();
            var result = dv.GetValueOrDefaultEx<string>(s => s ?? "converted", null);
            Assert.Equal("converted", result);
        }

        [Fact]
        public void ConvertOverload_WithDefault_ReturnsConvertedDefault()
        {
            var dv = new DataValue();
            var result = dv.GetValueOrDefaultEx<string>(s => (s ?? "base") + "!", null);
            Assert.Equal("base!", result);
        }
    }
}
