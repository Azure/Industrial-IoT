// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Opc.Ua;
    using Xunit;

    public sealed class NodeStateExTests
    {
        [Fact]
        public void GetValueOrDefaultExReturnsDefaultForMissingValueTypeState()
        {
            PropertyState<uint>? state = null;

            var value = state.GetValueOrDefaultEx(42u);

            Assert.Equal(42u, value);
        }

        [Fact]
        public void GetValueOrDefaultExPassesNullToReferenceTypeConverterForMissingState()
        {
            PropertyState<ArrayOf<string>>? state = null;

            var value = state.GetValueOrDefaultEx(values => values?.Count);

            Assert.Null(value);
        }

        [Fact]
        public void GetValueOrDefaultExValueTypeReturnsValueWhenStateGood()
        {
            var state = PropertyState<uint>.With<VariantBuilder>(null);
            state.Value = 77u;
            state.StatusCode = StatusCodes.Good;

            var value = state.GetValueOrDefaultEx(0u);

            Assert.Equal(77u, value);
        }

        [Fact]
        public void GetValueOrDefaultExValueTypeReturnsFallbackWhenStateBad()
        {
            var state = PropertyState<uint>.With<VariantBuilder>(null);
            state.Value = 77u;
            state.StatusCode = StatusCodes.BadInternalError;

            var value = state.GetValueOrDefaultEx(99u);

            Assert.Equal(99u, value);
        }

        [Fact]
        public void GetValueOrDefaultExClassTypeReturnsValueWhenStateGood()
        {
            var state = PropertyState<string>.With<VariantBuilder>(null);
            state.Value = "hello";
            state.StatusCode = StatusCodes.Good;

            var value = state.GetValueOrDefaultEx<string>(null);

            Assert.Equal("hello", value);
        }

        [Fact]
        public void GetValueOrDefaultExClassTypeReturnsFallbackWhenStateBad()
        {
            var state = PropertyState<string>.With<VariantBuilder>(null);
            state.Value = "hello";
            state.StatusCode = StatusCodes.BadInternalError;

            var value = state.GetValueOrDefaultEx("fallback");

            Assert.Equal("fallback", value);
        }

        [Fact]
        public void GetValueOrDefaultExClassTypeNullStateReturnsDefault()
        {
            PropertyState<string>? state = null;

            var value = state.GetValueOrDefaultEx("default");

            Assert.Equal("default", value);
        }

        [Fact]
        public void GetValueOrDefaultExConverterValueTypeReturnsConvertedValue()
        {
            var state = PropertyState<uint>.With<VariantBuilder>(null);
            state.Value = 5u;
            state.StatusCode = StatusCodes.Good;

            var value = state.GetValueOrDefaultEx(x => (int?)x * 2, 0u);

            Assert.Equal(10, value);
        }

        [Fact]
        public void GetValueOrDefaultExConverterValueTypeReturnsConvertedDefaultWhenNull()
        {
            PropertyState<uint>? state = null;

            var value = state.GetValueOrDefaultEx(x => (int?)x * 2, 3u);

            Assert.Equal(6, value);
        }

        [Fact]
        public void GetValueOrDefaultExConverterClassTypeReturnsConvertedValue()
        {
            var state = PropertyState<string>.With<VariantBuilder>(null);
            state.Value = "hello";
            state.StatusCode = StatusCodes.Good;

            var value = state.GetValueOrDefaultEx(s => s?.Length, null);

            Assert.Equal(5, value);
        }

        [Fact]
        public void GetValueOrDefaultExConverterClassTypeReturnsConvertedDefaultWhenNull()
        {
            PropertyState<string>? state = null;

            var value = state.GetValueOrDefaultEx(s => s?.Length, "def");

            Assert.Equal(3, value);
        }
    }
}
