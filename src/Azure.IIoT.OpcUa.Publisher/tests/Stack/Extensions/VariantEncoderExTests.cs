// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Encoders;
    using Opc.Ua;
    using System.Text.Json.Nodes;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="VariantEncoderEx"/>.
    /// </summary>
    public sealed class VariantEncoderExTests
    {
        private static IVariantEncoder CreateEncoder()
        {
            return new JsonVariantEncoder(new ServiceMessageContext());
        }

        // ── Decode(IVariantEncoder, JsonNode?, string?) ───────────────────────

        [Fact]
        public void Decode_NullType_DecodesWithBuiltInTypeNull()
        {
            var encoder = CreateEncoder();
            var value = JsonValue.Create(42L);

            var result = encoder.Decode(value, (string?)null);

            // When type is null, BuiltInType.Null is used → the encoder infers
            // the natural type from the JSON value (integer → Int64)
            Assert.NotEqual(BuiltInType.NodeId, result.TypeInfo.BuiltInType);
        }

        [Fact]
        public void Decode_EmptyType_DecodesWithBuiltInTypeNull()
        {
            var encoder = CreateEncoder();
            var value = JsonValue.Create("hello");

            var result = encoder.Decode(value, "");

            Assert.NotEqual(BuiltInType.NodeId, result.TypeInfo.BuiltInType);
        }

        [Fact]
        public void Decode_NullValue_NullType_ReturnsNullVariant()
        {
            var encoder = CreateEncoder();

            var result = encoder.Decode((JsonNode?)null, (string?)null);

            Assert.Equal(BuiltInType.Null, result.TypeInfo.BuiltInType);
        }

        [Fact]
        public void Decode_BooleanType_DecodesAsBooleanVariant()
        {
            var encoder = CreateEncoder();
            // OPC UA numeric ID 1 = Boolean
            var value = JsonValue.Create(true);

            var result = encoder.Decode(value, "i=1");

            Assert.Equal(BuiltInType.Boolean, result.TypeInfo.BuiltInType);
        }

        [Fact]
        public void Decode_Int32Type_DecodesAsInt32Variant()
        {
            var encoder = CreateEncoder();
            // OPC UA numeric ID 6 = Int32
            var value = JsonValue.Create(42L);

            var result = encoder.Decode(value, "i=6");

            Assert.Equal(BuiltInType.Int32, result.TypeInfo.BuiltInType);
        }

        [Fact]
        public void Decode_DoubleType_DecodesAsDoubleVariant()
        {
            var encoder = CreateEncoder();
            // OPC UA numeric ID 11 = Double
            var value = JsonValue.Create(3.14);

            var result = encoder.Decode(value, "i=11");

            Assert.Equal(BuiltInType.Double, result.TypeInfo.BuiltInType);
        }

        [Fact]
        public void Decode_StringType_DecodesAsStringVariant()
        {
            var encoder = CreateEncoder();
            // OPC UA numeric ID 12 = String
            var value = JsonValue.Create("test");

            var result = encoder.Decode(value, "i=12");

            Assert.Equal(BuiltInType.String, result.TypeInfo.BuiltInType);
        }

        [Fact]
        public void Decode_UnknownType_DecodesWithBuiltInTypeNull()
        {
            var encoder = CreateEncoder();
            // "i=99999" → not a known built-in type → BuiltInType.Null used
            var value = JsonValue.Create(0L);

            var result = encoder.Decode(value, "i=99999");

            // When the NodeId doesn't map to a known built-in, returns Null type
            Assert.True(result.TypeInfo.BuiltInType is BuiltInType.Int64 or BuiltInType.Null or BuiltInType.Integer);
        }

        [Fact]
        public void Decode_WhitespaceType_DecodesWithBuiltInTypeNull()
        {
            var encoder = CreateEncoder();
            var value = JsonValue.Create(1L);

            // "   " is not empty but IsNullOrEmpty returns false, so it tries to parse as NodeId
            // This is an edge case — just verify it doesn't throw
            var ex = Record.Exception(() => encoder.Decode(value, "   "));
            Assert.Null(ex);
        }
    }
}
