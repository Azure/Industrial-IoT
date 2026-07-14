// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Opc.Ua;
    using System.Text.Json.Nodes;
    using Xunit;

    public class VariantEncoderBooleanTests
    {
        [Fact]
        public void DecodeEncodeBooleanFromJValue()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(true);
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeFalseBooleanFromJValue()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var value = TestJson.FromObject(false);
            var variant = codec.Decode(value, BuiltInType.Boolean);
            var encoded = codec.Encode(variant);

            Assert.NotNull(encoded);
            Assert.True(JsonNode.DeepEquals(value, encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromJArray()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromArray(true, true, false);
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromJValueTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(true);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromJArrayTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromArray(true, true, false);
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.NotNull(encoded);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(str, encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "true";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "true, true, false";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[true, true, false]";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromString3()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(System.Array.Empty<bool>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "true";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }
        [Fact]
        public void DecodeEncodeBooleanArrayFromStringTypeNull1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "true, true, false";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromStringTypeNull2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[true, true, false]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromStringTypeNullIsNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "[]";
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = Variant.Null;
            var encoded = codec.Encode(variant);
            Assert.Null(encoded);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeBooleanFromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "\"true\"";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromSinglyQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "  'true'";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromQuotedString()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = "\"true\",'true',\"false\"";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromQuotedString2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            const string str = " [\"true\",'true',\"false\"] ";
            var variant = codec.Decode(str, BuiltInType.Boolean);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonTokenTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeVariant1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Boolean",
                Body = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeVariant2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Boolean",
                Body = System.Array.Empty<bool>()
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(System.Array.Empty<bool>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                Type = "Boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonStringTypeVariant()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                Type = "Boolean",
                Body = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonTokenTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeNull1()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                TYPE = "BOOLEAN",
                BODY = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeNull2()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                Type = "Boolean",
                Body = System.Array.Empty<bool>()
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(System.Array.Empty<bool>());
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                Type = "boolean",
                Body = true
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonStringTypeNull()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                type = "Boolean",
                body = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonTokenTypeNullMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = TestJson.FromObject(new
            {
                DataType = "Boolean",
                Value = true
            });
            var variant = codec.Decode(str, BuiltInType.Null);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanFromVariantJsonStringTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                DataType = "Boolean",
                Value = true
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant(true);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromObject(true), encoded));
        }

        [Fact]
        public void DecodeEncodeBooleanArrayFromVariantJsonTokenTypeVariantMsftEncoding()
        {
            var codec = new JsonVariantEncoder(new ServiceMessageContext());
            var str = Json.SerializeToString(new
            {
                dataType = "Boolean",
                value = new bool[] { true, true, false }
            });
            var variant = codec.Decode(str, BuiltInType.Variant);
            var expected = new Variant([true, true, false]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(JsonNode.DeepEquals(TestJson.FromArray(true, true, false), encoded));
        }

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
    }
}
